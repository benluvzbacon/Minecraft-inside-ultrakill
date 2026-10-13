using MinecraftUltrakill.Integration;
using UnityEngine;

namespace MinecraftUltrakill.Presentation
{
    // Uses the game's existing camera/controller; no second camera or movement input handler.
    public sealed class SteveCameraRig : MonoBehaviour
    {
        public PlayerContext Player;
        public SteveModel Model;
        Vector3 eyeLocal;
        bool thirdPerson;
        public void Initialize(PlayerContext p, SteveModel model) { Player = p; Model = model; eyeLocal = p.Camera.transform.localPosition; }
        public void Toggle()
        {
            if (!thirdPerson) eyeLocal = Player.Camera.transform.localPosition;
            thirdPerson = !thirdPerson; Model.SetWorldVisible(thirdPerson); Model.SetViewVisible(!thirdPerson);
            if (!thirdPerson) RestoreEye();
        }
        void LateUpdate()
        {
            if (!thirdPerson || Player == null || !Player.Camera || !Player.Alive) return;
            var cam = Player.Camera.transform;
            var parent = cam.parent;
            Vector3 eye = parent ? parent.TransformPoint(eyeLocal) : eyeLocal;
            Vector3 desired = eye - cam.forward * 3f + cam.right * .55f;
            Vector3 delta = desired - eye;
            float distance = delta.magnitude;
            foreach (var hit in Physics.SphereCastAll(eye, .15f, delta.normalized, distance, ~0, QueryTriggerInteraction.Ignore))
            { if (!Player.Owns(hit.collider)) distance = Mathf.Min(distance, Mathf.Max(.1f, hit.distance - .1f)); }
            cam.position = eye + delta.normalized * distance;
        }
        void RestoreEye() { if (Player != null && Player.Camera) Player.Camera.transform.localPosition = eyeLocal; }
        public void Release() { if (thirdPerson) RestoreEye(); thirdPerson = false; enabled = false; }
        void OnDestroy() { Release(); }
    }
}
