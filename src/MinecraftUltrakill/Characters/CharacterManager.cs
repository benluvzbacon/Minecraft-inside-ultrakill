using System;
using System.Collections.Generic;
using MinecraftUltrakill.Integration;
using MinecraftUltrakill.Presentation;
using MinecraftUltrakill.Core;
using UnityEngine;

namespace MinecraftUltrakill.Characters
{
    public sealed class CharacterManager : IDisposable
    {
        readonly Plugin plugin;
        readonly List<Action> restore = new List<Action>();
        public PlayerContext Player { get; private set; }
        public SteveModel Model { get; private set; }
        public bool IsSteve { get; private set; }
        public CharacterManager(Plugin owner) { plugin = owner; }
        public void Toggle(PlayerContext candidate)
        {
            if (IsSteve) { Restore(); return; }
            if (!candidate.Playable) return;
            // Do not enter during native dash/slide/slam physics transitions.
            if (GameBridge.Flag(candidate.Movement, "boost") || GameBridge.Flag(candidate.Movement, "sliding") || GameBridge.Flag(candidate.Movement, "ridingRocket"))
            { plugin.Notice("Finish sliding/dashing/rocket riding before switching."); return; }
            Player = candidate;
            try
            {
                Model = new SteveModel(Player, plugin.Art);
                SnapshotField(Player.Movement, "walkSpeed", (float)GameBridge.Read(Player.Movement, "walkSpeed") * plugin.Settings.SteveSpeed.Value);
                SnapshotField(Player.Movement, "modNoDashSlide", true);
                SetEnabled(Player.Gun, false); SetEnabled(Player.Fist, false);
                Hide(GameBridge.Read(Player.Gun, "currentWeapon") as GameObject);
                Hide(GameBridge.Read(Player.Fist, "currentArmObject") as GameObject);
                // Hide only native weapon-icon graphics, never health/style HUD or the HUD camera.
                var hudType = GameBridge.FindType("WeaponHUD");
                var nativeHud = hudType == null ? null : GameBridge.Find(hudType);
                if (nativeHud)
                {
                    var icon = GameBridge.Read(nativeHud, "img") as Behaviour;
                    var glow = GameBridge.Read(nativeHud, "glowImg") as Behaviour;
                    if (icon) SetEnabled(icon, false); if (glow) SetEnabled(glow, false);
                }
                Model.Tick(plugin.SelectedItem, plugin.SelectedBlock, false);
                foreach (var r in Player.Transform.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.transform.IsChildOf(Player.Camera.transform) || Model.Owns(r.transform) || r is ParticleSystemRenderer) continue;
                    bool was = r.enabled; restore.Add(() => { if (r) r.enabled = was; }); r.enabled = false;
                }
                IsSteve = true;
                plugin.Notice("Steve — E: blocks  R: ignite TNT  F: V1");
                plugin.LoggerInfo("Character switched to Steve; native movement/camera retained.");
            }
            catch (Exception error)
            {
                Restore(); plugin.LogError("Steve transition rolled back", error); plugin.Notice("Switch failed; V1 restored. See BepInEx log.");
            }
        }
        void SnapshotField(object target, string field, object next)
        { object old = GameBridge.Read(target, field); restore.Add(() => { if ((target as UnityEngine.Object)) GameBridge.Write(target, field, old); }); GameBridge.Write(target, field, next); }
        void SetEnabled(Behaviour b, bool next)
        { bool old = b.enabled; restore.Add(() => { if (b) b.enabled = old; }); b.enabled = next; }
        void Hide(GameObject obj)
        { if (!obj) return; bool old = obj.activeSelf; restore.Add(() => { if (obj) obj.SetActive(old); }); obj.SetActive(false); }
        public void Tick(ItemKind item, BlockKind block, bool mining)
        {
            if (!IsSteve) return;
            if (!Player.Alive || !Player.Movement.gameObject.activeInHierarchy) { Restore(); return; }
            Model.Tick(item, block, mining && Player.Playable);
        }
        public void Restore()
        {
            bool wasSteve = IsSteve;
            IsSteve = false;
            for (int i = restore.Count - 1; i >= 0; i--)
            { try { restore[i](); } catch (Exception e) { plugin.LogError("Restore snapshot", e); } }
            restore.Clear();
            if (Model != null) { Model.Dispose(); Model = null; }
            Player = null;
            plugin.ResetInteraction();
            if (wasSteve) plugin.LoggerInfo("Character restored to V1; native controllers, weapons, HUD and movement snapshot restored.");
        }
        public void Dispose() { Restore(); }
    }
}
