using System.Reflection;
using HarmonyLib;

namespace MinecraftUltrakill.Integration
{
    public static class AbilityPatches
    {
        public static void Install(Harmony harmony, GameBridge bridge)
        {
            var prefix = new HarmonyMethod(typeof(AbilityPatches).GetMethod("MovementPrefix"));
            foreach (string name in new[] { "Dodge", "StartSlide", "WallJump", "Slamdown" })
                harmony.Patch(AccessTools.Method(bridge.MovementType, name), prefix: prefix);
            // HookArm is independent from the currently visible fist in some game builds.
            var hook = GameBridge.FindType("HookArm");
            if (hook != null && AccessTools.Method(hook, "Update") != null)
                harmony.Patch(AccessTools.Method(hook, "Update"), prefix: new HarmonyMethod(typeof(AbilityPatches).GetMethod("HookPrefix")));
        }
        public static bool MovementPrefix(object __instance)
        {
            var p = Plugin.Instance;
            return p == null || p.Characters == null || !p.Characters.IsSteve || !object.ReferenceEquals(p.Characters.Player.Movement, __instance);
        }
        public static bool HookPrefix()
        { return Plugin.Instance == null || Plugin.Instance.Characters == null || !Plugin.Instance.Characters.IsSteve; }
    }
}
