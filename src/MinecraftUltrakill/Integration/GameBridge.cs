using System;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace MinecraftUltrakill.Integration
{
    // Deliberately no compile reference to Assembly-CSharp and no guessed overload arguments.
    public sealed class GameBridge
    {
        public readonly Type MovementType, GunType, FistType, EnemyType, LimbType;
        readonly MethodInfo deliver, hurt;
        readonly ManualLogSource log;
        public GameBridge(ManualLogSource logger)
        {
            log = logger;
            MovementType = RequireType("NewMovement"); GunType = RequireType("GunControl"); FistType = RequireType("FistControl");
            EnemyType = RequireType("EnemyIdentifier"); LimbType = RequireType("EnemyIdentifierIdentifier");
            RequireField(MovementType, "walkSpeed", typeof(float)); RequireField(MovementType, "modNoDashSlide", typeof(bool));
            RequireField(MovementType, "dead", typeof(bool)); RequireField(MovementType, "activated", typeof(bool));
            RequireField(GunType, "currentWeapon", typeof(GameObject));
            if (FistType.GetProperty("currentArmObject", All) == null) throw new MissingMemberException("FistControl.currentArmObject");
            RequireField(LimbType, "eid", EnemyType);
            foreach (string method in new[] { "Dodge", "WallJump", "StartSlide" })
                if (MovementType.GetMethod(method, All, null, Type.EmptyTypes, null) == null) throw new MissingMethodException("NewMovement", method);
            if (MovementType.GetMethod("Slamdown", All, null, new[] { typeof(float) }, null) == null) throw new MissingMethodException("NewMovement", "Slamdown");
            deliver = EnemyType.GetMethod("DeliverDamage", All, null, new[] { typeof(GameObject), typeof(Vector3), typeof(Vector3), typeof(float), typeof(bool), typeof(float), typeof(GameObject) }, null);
            hurt = MovementType.GetMethod("GetHurt", All, null, new[] { typeof(int), typeof(bool), typeof(float), typeof(bool), typeof(bool) }, null);
            if (deliver == null || deliver.ReturnType != typeof(void) || hurt == null) throw new MissingMethodException("Required damage signatures differ; Steve disabled.");
            log.LogInfo("Runtime API verified: NewMovement, native weapon controls, EnemyIdentifier seven-argument DeliverDamage.");
        }
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public static Type FindType(string name)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            { if (a.GetName().Name == "Assembly-CSharp") return a.GetType(name, false); }
            return null;
        }
        static Type RequireType(string name) { return FindType(name) ?? throw new TypeLoadException("Game API missing: " + name); }
        static void RequireField(Type t, string name, Type kind)
        { var f = t.GetField(name, All); if (f == null || f.FieldType != kind) throw new MissingFieldException(t.Name, name); }
        public static object Read(object obj, string name)
        {
            if (obj == null) return null;
            var t = obj.GetType(); var field = t.GetField(name, All);
            if (field != null) return field.GetValue(obj);
            var property = t.GetProperty(name, All); return property == null ? null : property.GetValue(obj, null);
        }
        public static void Write(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name, All);
            if (f == null) throw new MissingFieldException(obj.GetType().Name, name);
            f.SetValue(obj, value);
        }
        public static bool Flag(object obj, string name) { var v = Read(obj, name); return v is bool && (bool)v; }
        public static Behaviour Find(Type type)
        { return UnityEngine.Object.FindObjectsOfType(type).OfType<Behaviour>().FirstOrDefault(x => x && x.gameObject.activeInHierarchy); }
        public bool TryPlayer(out PlayerContext player)
        {
            player = null;
            var movement = Find(MovementType); if (!movement) return false;
            var rb = movement.GetComponent<Rigidbody>();
            var controller = FindType("CameraController");
            var cameraController = controller == null ? null : Find(controller);
            var cam = cameraController ? cameraController.GetComponent<Camera>() : null;
            if (!cam && cameraController) cam = Read(cameraController, "cam") as Camera;
            if (!cam) cam = movement.GetComponentsInChildren<Camera>(true).FirstOrDefault(x => x.name == "Main Camera");
            var gun = Find(GunType); var fist = Find(FistType);
            var collider = movement.GetComponent<Collider>();
            if (!rb || !cam || !gun || !fist || !collider) return false;
            player = new PlayerContext(movement, rb, cam, gun, fist, collider, cameraController);
            return true;
        }
        public Component Enemy(Collider collider)
        {
            var limb = collider.GetComponent(LimbType);
            if (limb) { var e = Read(limb, "eid") as Component; if (e) return e; }
            return collider.GetComponentInParent(EnemyType);
        }
        public bool Damage(Component enemy, Collider limb, Vector3 direction, Vector3 point, float amount, GameObject source)
        {
            if (!enemy || Flag(enemy, "dead") || amount <= 0) return false;
            try { deliver.Invoke(enemy, new object[] { limb.gameObject, direction * 100f, point, amount, false, 1f, source }); return true; }
            catch (Exception e) { log.LogError("Enemy damage rejected for " + enemy.name + ": " + (e.InnerException ?? e).Message); return false; }
        }
        public void HurtPlayer(PlayerContext player, int amount)
        { hurt.Invoke(player.Movement, new object[] { amount, false, 1f, false, true }); }
    }
    public sealed class PlayerContext
    {
        public readonly Behaviour Movement, Gun, Fist, CameraController;
        public readonly Rigidbody Body;
        public readonly Camera Camera;
        public readonly Collider Collider;
        public Transform Transform { get { return Movement.transform; } }
        public PlayerContext(Behaviour movement, Rigidbody rb, Camera cam, Behaviour gun, Behaviour fist, Collider collider, Behaviour cc)
        { Movement = movement; Body = rb; Camera = cam; Gun = gun; Fist = fist; Collider = collider; CameraController = cc; }
        public bool Alive { get { return Movement && !GameBridge.Flag(Movement, "dead"); } }
        public bool Playable { get { return Alive && Movement.enabled && GameBridge.Flag(Movement, "activated") && Time.timeScale > .001f && Cursor.lockState == CursorLockMode.Locked; } }
        public bool Owns(Collider c) { return c && (c.attachedRigidbody == Body || c.transform.IsChildOf(Transform)); }
    }
}
