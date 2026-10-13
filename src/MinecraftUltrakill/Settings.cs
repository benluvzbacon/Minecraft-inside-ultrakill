using BepInEx.Configuration;
using UnityEngine;

namespace MinecraftUltrakill
{
    public sealed class Settings
    {
        public readonly ConfigEntry<KeyCode> SwitchKey, PaletteKey, IgniteKey, ViewKey;
        public readonly ConfigEntry<float> Reach, Grid, MineSpeed, SwordDamage, SwordCooldown, AxeDamage, AxeCooldown, AxeMining, SteveSpeed, MeleeRange, Fuse, BlastRadius, BlastDamage;
        public readonly ConfigEntry<int> MaxBlocks;
        public readonly ConfigEntry<bool> Persistence, Debug;
        public Settings(ConfigFile c)
        {
            SwitchKey = c.Bind("Controls", "SwitchCharacter", KeyCode.F, "Switch V1/Steve. Remap vanilla F punch to avoid a shared key.");
            PaletteKey = c.Bind("Controls", "BlockPalette", KeyCode.E, "Open creative block palette; 1-8 choose a block for the current building slot.");
            IgniteKey = c.Bind("Controls", "IgniteTNT", KeyCode.R, "Ignite the targeted TNT block.");
            ViewKey = c.Bind("Controls", "ThirdPersonView", KeyCode.F5, "Toggle Steve third-person shoulder view using the existing game camera.");
            Reach = Number(c, "Building", "PlacementDistance", 6f, 1f, 20f, "Maximum surface targeting distance.");
            Grid = Number(c, "Building", "GridSize", 1f, .25f, 4f, "Unity units per cube. Change only between levels; saves are keyed by grid size.");
            MineSpeed = Number(c, "Building", "MiningSpeed", 1f, .1f, 20f, "Global mining multiplier.");
            MaxBlocks = c.Bind("Building", "MaxBlocks", 2048, new ConfigDescription("Per-level cap including loaded blocks.", new AcceptableValueRange<int>(1, 10000)));
            SwordDamage = Number(c, "Combat", "SwordDamage", 2f, .01f, 100f, "ULTRAKILL damage units, not hearts.");
            SwordCooldown = Number(c, "Combat", "SwordCooldown", .45f, .1f, 5f, "Seconds per swing.");
            AxeDamage = Number(c, "Combat", "AxeDamage", 3.5f, .01f, 100f, "ULTRAKILL damage units.");
            AxeCooldown = Number(c, "Combat", "AxeCooldown", .9f, .1f, 5f, "Seconds per swing.");
            AxeMining = Number(c, "Combat", "AxeMiningSpeed", 4f, 1f, 20f, "Oak-plank mining multiplier.");
            MeleeRange = Number(c, "Combat", "MeleeRange", 3f, .5f, 8f, "Melee reach in world units.");
            SteveSpeed = Number(c, "Character", "SteveMovementSpeed", .45f, .1f, 1.5f, "Multiplier of native walkSpeed; keep the original physics controller.");
            Persistence = c.Bind("Persistence", "SaveBlocks", false, "Opt-in creative layout persistence, separate from game saves; keyed by scene and grid.");
            Debug = c.Bind("Diagnostics", "DebugLogging", false, "Log successful operations; never log each frame.");
            Fuse = Number(c, "TNT", "FuseSeconds", 3f, .5f, 10f, "Fuse before detonation.");
            BlastRadius = Number(c, "TNT", "Radius", 4f, 1f, 10f, "Explosion radius.");
            BlastDamage = Number(c, "TNT", "Damage", 6f, .01f, 100f, "Maximum ULTRAKILL enemy damage units; player damage is units x10.");
        }
        static ConfigEntry<float> Number(ConfigFile c, string section, string key, float value, float min, float max, string description)
        { return c.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max))); }
    }
}
