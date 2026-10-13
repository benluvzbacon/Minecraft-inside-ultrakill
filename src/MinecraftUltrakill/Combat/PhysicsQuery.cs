using System.Linq;
using MinecraftUltrakill.Integration;
using UnityEngine;

namespace MinecraftUltrakill.Combat
{
    public static class PhysicsQuery
    {
        public static bool Visible(Vector3 from, Vector3 point, Collider target, PlayerContext player, GameBridge bridge)
        {
            Vector3 delta = point - from; if (delta.sqrMagnitude < .0001f) return true;
            var targetEnemy = bridge.Enemy(target);
            foreach (var hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude + .02f, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance))
            {
                if (player != null && player.Owns(hit.collider)) continue;
                if (hit.collider == target || (targetEnemy && bridge.Enemy(hit.collider) == targetEnemy)) return true;
                return false;
            }
            return true; // Endpoint may lie on the surface and have no ray intersection.
        }
    }
}
