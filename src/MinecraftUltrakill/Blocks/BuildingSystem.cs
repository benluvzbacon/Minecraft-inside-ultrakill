using MinecraftUltrakill.Core;
using MinecraftUltrakill.Integration;
using UnityEngine;

namespace MinecraftUltrakill.Blocks
{
    public sealed class BuildingSystem
    {
        readonly Plugin plugin;
        readonly ActionRepeater repeat = new ActionRepeater();
        public BuildingSystem(Plugin p) { plugin = p; }
        public void Reset() { repeat.Reset(); }
        public void Tick(PlayerContext player, BlockKind kind, bool pressed, bool held)
        {
            if (!plugin.Settings.HoldToPlace.Value) held = false;
            if (!repeat.Take(Time.time, pressed, held, plugin.Settings.PlacementInterval.Value)) return;
            if (plugin.World.PlacePreview(player, kind, pressed))
                plugin.Characters.Model.Swing(.25f);
        }
    }
}
