using System;
using BepInEx;
using HarmonyLib;
using MinecraftUltrakill.Blocks;
using MinecraftUltrakill.Characters;
using MinecraftUltrakill.Combat;
using MinecraftUltrakill.Core;
using MinecraftUltrakill.Integration;
using MinecraftUltrakill.Persistence;
using MinecraftUltrakill.Presentation;
using MinecraftUltrakill.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MinecraftUltrakill
{
    [BepInPlugin(Guid, "Minecraft x ULTRAKILL", "0.1.0")]
    [BepInProcess("ULTRAKILL.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "benluvzbacon.minecraft.ultrakill";
        public static Plugin Instance { get; private set; }
        public Settings Settings { get; private set; }
        public GameInput InputKeys { get; private set; }
        public GameBridge Bridge { get; private set; }
        public ProceduralArt Art { get; private set; }
        public CharacterManager Characters { get; private set; }
        public BlockWorld World { get; private set; }
        public MiningSystem Mining { get; private set; }
        public MeleeSystem Melee { get; private set; }
        public string NoticeText { get; private set; }
        public float NoticeUntil { get; private set; }
        public ItemKind SelectedItem { get { return inventory.Item; } }
        public BlockKind SelectedBlock { get { return inventory.Block; } }
        readonly Hotbar inventory = new Hotbar();
        Harmony harmony;
        SteveHud hud;
        TntSystem tnt;
        BuildingSystem building;
        WorldStore store;
        PlayerContext candidate;
        float nextFind, nextSave;
        bool ready;
        void Awake()
        {
            Instance = this;
            try
            {
                Settings = new Settings(Config); InputKeys = new GameInput(); Bridge = new GameBridge(Logger);
                Art = new ProceduralArt(); Mining = new MiningSystem(this); Melee = new MeleeSystem(this);
                Characters = new CharacterManager(this); hud = new SteveHud(this, inventory); tnt = new TntSystem(this); building = new BuildingSystem(this); store = new WorldStore(this);
                harmony = new Harmony(Guid); AbilityPatches.Install(harmony, Bridge);
                SceneManager.activeSceneChanged += ActiveSceneChanged; SceneManager.sceneUnloaded += SceneUnloaded;
                ready = true; Logger.LogInfo("Minecraft x ULTRAKILL 0.1.0 initialized. F switches when the player is active. Runtime compatibility gate passed; this does not certify gameplay.");
            }
            catch (Exception e)
            { ready = false; if (harmony != null) harmony.UnpatchSelf(); if (Art != null) Art.Dispose(); Logger.LogError("Initialization failed; V1 untouched. " + e); }
        }
        void Update()
        {
            if (!ready) return;
            try
            {
                if (Time.unscaledTime >= nextFind || candidate == null || !candidate.Movement)
                { nextFind = Time.unscaledTime + .5f; Bridge.TryPlayer(out candidate); }
                if (Characters.IsSteve && (candidate == null || candidate.Movement != Characters.Player.Movement)) Characters.Restore();
                if (candidate != null && candidate.Playable)
                {
                    EnsureWorld(candidate);
                    if (InputKeys.Down(Settings.SwitchKey.Value)) Characters.Toggle(candidate);
                }
                var p = Characters.IsSteve ? Characters.Player : candidate;
                if (p != null && p.Alive) tnt.Tick(p); // Fuses continue in V1; damage reaches real enemies/player.
                if (Characters.IsSteve)
                {
                    if (Characters.Player.Playable)
                    {
                        int previousSlot = inventory.Selected; BlockKind previousBlock = inventory.Block;
                        hud.InputTick();
                        if (previousSlot != inventory.Selected || previousBlock != inventory.Block || hud.Palette) building.Reset();
                        if (!hud.Palette && InputKeys.Down(Settings.ViewKey.Value)) Characters.Model.ToggleView();
                        bool held = !hud.Palette && InputKeys.FireHeld(Characters.Player);
                        bool mining = Mining.Tick(Characters.Player, held, inventory.Item);
                        bool attack = !hud.Palette && !mining && InputKeys.FireDown(Characters.Player);
                        Melee.Tick(Characters.Player, inventory.Item, attack);
                        World.UpdatePreview(Characters.Player, inventory.Item == ItemKind.Block && !hud.Palette);
                        if (!hud.Palette && inventory.Item == ItemKind.Block)
                            building.Tick(Characters.Player, inventory.Block, InputKeys.FireDown(Characters.Player, true), InputKeys.FireHeld(Characters.Player, true));
                        else building.Reset();
                        if (!hud.Palette && InputKeys.Down(Settings.IgniteKey.Value)) tnt.IgniteAimed(Characters.Player);
                    }
                    else { Mining.Reset(); Melee.Reset(); building.Reset(); World.ClearPreview(); }
                    Characters.Tick(inventory.Item, inventory.Block, Mining.Active);
                }
                else if (World != null) World.ClearPreview();
                if (Time.unscaledTime >= nextSave) { nextSave = Time.unscaledTime + 15; store.Save(World); }
            }
            catch (Exception e)
            {
                LogError("Gameplay failure; reverting to V1 and disabling mod until restart", e);
                ready = false; if (Characters != null) Characters.Restore(); if (World != null) World.ClearPreview();
            }
        }
        void EnsureWorld(PlayerContext p)
        {
            if (World != null && World.Scene.handle == BlockWorld.LevelScene(p).handle) return;
            CleanupWorld(); World = new BlockWorld(this, p); store.Load(World, p);
        }
        void OnGUI() { if (ready && hud != null) hud.Draw(); }
        void ActiveSceneChanged(Scene before, Scene after)
        { CleanupWorld(); candidate = null; nextFind = 0; }
        void SceneUnloaded(Scene scene)
        { if (World != null && World.Scene.handle == scene.handle) CleanupWorld(); candidate = null; }
        void CleanupWorld()
        {
            if (Characters != null) Characters.Restore();
            if (store != null) store.Save(World);
            if (World != null) { World.Dispose(); World = null; }
        }
        public void ResetInteraction()
        { if (Mining != null) Mining.Reset(); if (Melee != null) Melee.Reset(); if (hud != null) hud.Reset(); if (building != null) building.Reset(); }
        public void Notice(string text) { NoticeText = text; NoticeUntil = Time.unscaledTime + 4; }
        public void LoggerInfo(string text) { Logger.LogInfo(text); }
        public void DebugLog(string text) { if (Settings.Debug.Value) Logger.LogInfo(text); }
        public void LogError(string context, Exception e) { Logger.LogError(context + ": " + e); }
        void OnDisable() { if (Characters != null) Characters.Restore(); if (World != null) World.ClearPreview(); }
        void OnDestroy()
        {
            ready = false; SceneManager.activeSceneChanged -= ActiveSceneChanged; SceneManager.sceneUnloaded -= SceneUnloaded;
            CleanupWorld(); if (harmony != null) harmony.UnpatchSelf(); if (Art != null) Art.Dispose(); Instance = null;
        }
    }
}
