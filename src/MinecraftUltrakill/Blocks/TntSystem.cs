using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftUltrakill.Combat;
using MinecraftUltrakill.Core;
using MinecraftUltrakill.Integration;
using UnityEngine;

namespace MinecraftUltrakill.Blocks
{
    public sealed class TntSystem
    {
        readonly Plugin plugin;
        int activeEffects;
        public TntSystem(Plugin p) { plugin = p; }
        public void IgniteAimed(PlayerContext p)
        {
            RaycastHit hit; if (!plugin.World.Aim(p, plugin.Settings.Reach.Value, out hit)) return;
            var block = hit.collider.GetComponent<PlacedBlock>(); if (block && block.Kind == BlockKind.TNT) Ignite(block, plugin.Settings.Fuse.Value);
        }
        void Ignite(PlacedBlock block, float delay)
        {
            if (block.DetonateAt >= 0) return;
            block.DetonateAt = Time.time + delay;
            block.FuseVisual = plugin.Art.Box("BurningFuse", block.transform, new Vector3(0, .56f, 0), new Vector3(.06f, .14f, .06f), plugin.Art.ColorMaterial(Color.yellow));
            plugin.DebugLog("TNT fuse ignited at " + block.Cell);
        }
        public void Tick(PlayerContext player)
        {
            if (plugin.World == null) return;
            int explosions = 0;
            foreach (var b in plugin.World.Blocks.Values.ToArray())
            {
                if (!b || b.DetonateAt < 0) continue;
                if (b.FuseVisual) b.FuseVisual.SetActive(Mathf.FloorToInt(Time.time * 10) % 2 == 0);
                if (Time.time >= b.DetonateAt && explosions < 4) { Explode(b, player); explosions++; }
            }
        }
        void Explode(PlacedBlock tnt, PlayerContext player)
        {
            Vector3 origin = tnt.transform.position; float radius = plugin.Settings.BlastRadius.Value;
            // Remove source collider before checking explosion visibility.
            plugin.World.Remove(tnt); Physics.SyncTransforms();
            var enemies = new HashSet<int>(); var bodies = new HashSet<int>(); bool hurtPlayer = false;
            foreach (var c in Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 point = c.ClosestPoint(origin); float falloff = Mathf.Clamp01(1 - Vector3.Distance(origin, point) / radius);
                var block = c.GetComponent<PlacedBlock>();
                if (block)
                {
                    if (block.Kind == BlockKind.TNT) Ignite(block, .2f + UnityEngine.Random.value * .3f);
                    else if (block.Kind != BlockKind.Bedrock) plugin.World.Remove(block);
                    continue;
                }
                if (!PhysicsQuery.Visible(origin, point, c, null, plugin.Bridge)) continue;
                if (player != null && player.Alive && player.Owns(c))
                {
                    if (!hurtPlayer) { hurtPlayer = true; plugin.Bridge.HurtPlayer(player, Mathf.CeilToInt(plugin.Settings.BlastDamage.Value * 10 * falloff)); }
                    continue;
                }
                var enemy = plugin.Bridge.Enemy(c);
                if (enemy && enemies.Add(enemy.GetInstanceID()))
                    plugin.Bridge.Damage(enemy, c, (point - origin).normalized, point, plugin.Settings.BlastDamage.Value * falloff, plugin.gameObject);
                var rb = c.attachedRigidbody;
                if (rb && !rb.isKinematic && bodies.Add(rb.GetInstanceID())) rb.AddExplosionForce(500f, origin, radius, .2f, ForceMode.Impulse);
            }
            if (activeEffects < 16)
            {
                activeEffects++;
                var effect = plugin.Art.Box("MCUK_Explosion", plugin.World.EffectsRoot, origin, Vector3.one * .2f, plugin.Art.ColorMaterial(new Color(1, .55f, .05f)));
                var anim = effect.AddComponent<ExplosionVisual>(); anim.Radius = radius; anim.Done = () => { activeEffects = Math.Max(0, activeEffects - 1); };
            }
            plugin.DebugLog("TNT exploded at " + origin);
        }
    }
    public sealed class ExplosionVisual : MonoBehaviour
    {
        public float Radius;
        public Action Done;
        float age;
        void Update()
        {
            age += Time.deltaTime; transform.localScale = Vector3.one * Mathf.Lerp(.2f, Radius * 1.5f, age / .25f);
            transform.Rotate(40 * Time.deltaTime, 70 * Time.deltaTime, 25 * Time.deltaTime);
            if (age >= .25f) Destroy(gameObject);
        }
        void OnDestroy() { if (Done != null) Done(); }
    }
}
