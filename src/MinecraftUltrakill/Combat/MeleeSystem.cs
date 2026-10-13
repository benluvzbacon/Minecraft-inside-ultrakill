using System.Collections.Generic;
using System.Linq;
using MinecraftUltrakill.Core;
using MinecraftUltrakill.Integration;
using UnityEngine;

namespace MinecraftUltrakill.Combat
{
    public sealed class MeleeSystem
    {
        readonly Plugin plugin;
        float nextAttack, hitAt;
        bool pending;
        ItemKind queued;
        public float FeedbackUntil { get; private set; }
        public MeleeSystem(Plugin p) { plugin = p; }
        public void Reset() { pending = false; } // Preserve cooldown across character switches.
        public void Tick(PlayerContext p, ItemKind item, bool down)
        {
            if (pending && (queued != item || !p.Playable)) pending = false;
            if (pending && Time.time >= hitAt) { pending = false; Hit(p, queued); }
            if (!down || pending || item == ItemKind.Block || Time.time < nextAttack) return;
            float cooldown = item == ItemKind.Sword ? plugin.Settings.SwordCooldown.Value : plugin.Settings.AxeCooldown.Value;
            queued = item; nextAttack = Time.time + cooldown; hitAt = Time.time + Mathf.Min(.18f, cooldown * .25f); pending = true;
            plugin.Characters.Model.Swing(cooldown);
        }
        void Hit(PlayerContext p, ItemKind kind)
        {
            float damage = kind == ItemKind.Sword ? plugin.Settings.SwordDamage.Value : plugin.Settings.AxeDamage.Value;
            Vector3 origin = p.Camera.transform.position, direction = p.Camera.transform.forward;
            var done = new HashSet<int>();
            foreach (var hit in Physics.SphereCastAll(origin, .32f, direction, plugin.Settings.MeleeRange.Value, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance))
            {
                if (p.Owns(hit.collider)) continue;
                var enemy = plugin.Bridge.Enemy(hit.collider);
                if (!enemy || done.Contains(enemy.GetInstanceID())) continue;
                Vector3 point = hit.collider.ClosestPoint(origin + direction * Mathf.Max(hit.distance, .1f));
                if (!PhysicsQuery.Visible(origin, point, hit.collider, p, plugin.Bridge)) continue;
                done.Add(enemy.GetInstanceID());
                if (plugin.Bridge.Damage(enemy, hit.collider, direction, point, damage, p.Movement.gameObject))
                { FeedbackUntil = Time.time + .18f; plugin.DebugLog(kind + " hit " + enemy.name + " for " + damage); }
            }
        }
    }
}
