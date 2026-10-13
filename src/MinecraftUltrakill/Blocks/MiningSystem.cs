using MinecraftUltrakill.Core;
using MinecraftUltrakill.Integration;
using UnityEngine;

namespace MinecraftUltrakill.Blocks
{
    public sealed class MiningSystem
    {
        readonly Plugin plugin;
        PlacedBlock target;
        float elapsed;
        public float Progress { get; private set; }
        public bool Bedrock { get { return target && target.Kind == BlockKind.Bedrock; } }
        public bool Active { get { return target; } }
        public MiningSystem(Plugin owner) { plugin = owner; }
        public void Reset() { target = null; elapsed = 0; Progress = 0; }
        public bool Tick(PlayerContext p, bool held, ItemKind tool)
        {
            RaycastHit hit;
            var next = held && plugin.World != null && plugin.World.Aim(p, plugin.Settings.Reach.Value, out hit) ? hit.collider.GetComponent<PlacedBlock>() : null;
            if (!next) { Reset(); return false; }
            if (next != target) { Reset(); target = next; }
            if (target.Kind == BlockKind.Bedrock) { Progress = 0; return true; }
            elapsed += Time.deltaTime;
            float duration = Rules.MineSeconds(target.Kind, tool, plugin.Settings.MineSpeed.Value, plugin.Settings.AxeMining.Value);
            Progress = Mathf.Clamp01(elapsed / duration);
            if (Progress >= 1) { plugin.DebugLog("Mined " + target.Kind); plugin.World.Remove(target); Reset(); }
            return true;
        }
    }
}
