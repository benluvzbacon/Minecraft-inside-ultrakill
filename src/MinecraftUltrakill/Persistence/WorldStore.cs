using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using MinecraftUltrakill.Blocks;
using MinecraftUltrakill.Core;
using MinecraftUltrakill.Integration;
using UnityEngine;

namespace MinecraftUltrakill.Persistence
{
    [Serializable] public sealed class BlockRecord { public int x, y, z, kind; }
    [Serializable] public sealed class WorldRecord
    {
        public int version;
        public string scene;
        public float size, originX, originY, originZ;
        public BlockRecord[] blocks;
    }
    public sealed class WorldStore
    {
        readonly Plugin plugin;
        public WorldStore(Plugin p) { plugin = p; }
        string PathFor(BlockWorld world)
        {
            string key = world.ScenePath + "|" + world.SceneName + "|" + world.Size.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            using (var hash = SHA256.Create()) key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", "").Substring(0, 24);
            return Path.Combine(Paths.ConfigPath, "MinecraftUltrakill", "worlds", key + ".json");
        }
        public void Save(BlockWorld world)
        {
            if (world == null || !plugin.Settings.Persistence.Value || !world.Dirty) return;
            string path = PathFor(world), temp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var data = new WorldRecord { version = Rules.SaveVersion, scene = world.SceneName, size = world.Size,
                    originX = world.Origin.x, originY = world.Origin.y, originZ = world.Origin.z,
                    blocks = world.Layout.Select(b => new BlockRecord { x = b.Key.X, y = b.Key.Y, z = b.Key.Z, kind = (int)b.Value }).ToArray() };
                File.WriteAllText(temp, JsonUtility.ToJson(data, true), Encoding.UTF8);
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
                world.MarkSaved(); plugin.DebugLog("Saved " + data.blocks.Length + " blocks separately from vanilla saves.");
            }
            catch (Exception e) { plugin.LogError("Block save failed (previous save retained)", e); }
        }
        public void Load(BlockWorld world, PlayerContext player)
        {
            if (!plugin.Settings.Persistence.Value) return;
            string path = PathFor(world); if (!File.Exists(path)) return;
            try
            {
                if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("Save larger than 4 MiB.");
                var data = JsonUtility.FromJson<WorldRecord>(File.ReadAllText(path));
                if (data == null || data.version != Rules.SaveVersion || data.scene != world.SceneName || data.size != world.Size || data.blocks == null || data.blocks.Length > plugin.Settings.MaxBlocks.Value)
                    throw new InvalidDataException("Save schema/scene/grid/cap mismatch.");
                if (!Rules.Finite(data.originX) || !Rules.Finite(data.originY) || !Rules.Finite(data.originZ) || Mathf.Abs(data.originX) > world.Size || Mathf.Abs(data.originY) > world.Size || Mathf.Abs(data.originZ) > world.Size)
                    throw new InvalidDataException("Invalid grid origin.");
                if (data.blocks.Any(b => b == null || b.kind < 0 || b.kind > 7 || !Rules.ValidCell(b.x, b.y, b.z))) throw new InvalidDataException("Invalid block record.");
                world.LoadOrigin(new Vector3(data.originX, data.originY, data.originZ)); int loaded = 0;
                foreach (var b in data.blocks)
                {
                    var cell = new Cell(b.x, b.y, b.z);
                    if (world.Free(cell, player)) { world.Add(cell, (BlockKind)b.kind); loaded++; }
                }
                world.MarkSaved(); plugin.LoggerInfo("Loaded " + loaded + "/" + data.blocks.Length + " blocks; occupied/duplicate cells rejected. Fuses are not resumed.");
            }
            catch (Exception e) { plugin.LogError("Block save rejected; level remains playable", e); }
        }
    }
}
