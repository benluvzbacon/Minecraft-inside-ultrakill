using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftUltrakill.Core;
using MinecraftUltrakill.Integration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MinecraftUltrakill.Blocks
{
    public sealed class PlacedBlock : MonoBehaviour
    {
        public Cell Cell;
        public BlockKind Kind;
        public float DetonateAt = -1f;
        public GameObject FuseVisual;
    }
    public sealed class BlockWorld : IDisposable
    {
        readonly Plugin plugin;
        public readonly Scene Scene;
        public readonly string SceneName, ScenePath;
        // Keep plain layout data alive through Unity scene destruction so unload saves are valid.
        public readonly Dictionary<Cell, BlockKind> Layout = new Dictionary<Cell, BlockKind>();
        public readonly float Size;
        public Vector3 Origin { get; private set; }
        public readonly Dictionary<Cell, PlacedBlock> Blocks = new Dictionary<Cell, PlacedBlock>();
        readonly GameObject root, preview;
        readonly int layer;
        public Transform EffectsRoot { get { return root.transform; } }
        public bool Dirty { get; private set; }
        public bool PreviewValid { get; private set; }
        public Cell PreviewCell { get; private set; }
        public BlockWorld(Plugin owner, PlayerContext player)
        {
            plugin = owner; Scene = LevelScene(player); SceneName = Scene.name; ScenePath = Scene.path; Size = plugin.Settings.Grid.Value;
            root = new GameObject("MCUK_LevelBlocks"); SceneManager.MoveGameObjectToScene(root, Scene);
            layer = LayerMask.NameToLayer("Environment"); if (layer < 0) layer = 0;
            RaycastHit floor;
            Origin = Physics.Raycast(player.Collider.bounds.center, Vector3.down, out floor, player.Collider.bounds.extents.y + 5f, ~0, QueryTriggerInteraction.Ignore)
                ? new Vector3(0, floor.point.y - Mathf.Floor(floor.point.y / Size) * Size, 0) : Vector3.zero;
            preview = plugin.Art.Block("MCUK_PlacementPreview", root.transform, BlockKind.Glass, Vector3.zero, Size * .99f, false);
            preview.SetActive(false);
            plugin.LoggerInfo("Block world: " + Scene.name + ", grid " + Size + ", collision layer " + layer + ". Physics matrix applies; scripted movers are not guaranteed to stop.");
        }
        public static Scene LevelScene(PlayerContext player)
        {
            var scene = player.Movement.gameObject.scene;
            return scene.name == "DontDestroyOnLoad" ? SceneManager.GetActiveScene() : scene;
        }
        public Cell CellAt(Vector3 position)
        { var v = position - Origin; return new Cell(Rules.Snap(v.x, Size), Rules.Snap(v.y, Size), Rules.Snap(v.z, Size)); }
        public Vector3 Center(Cell c) { return Origin + new Vector3(c.X + .5f, c.Y + .5f, c.Z + .5f) * Size; }
        public bool Aim(PlayerContext p, float range, out RaycastHit hit)
        {
            var ray = new Ray(p.Camera.transform.position, p.Camera.transform.forward);
            foreach (var h in Physics.RaycastAll(ray, range, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance))
            { if (!p.Owns(h.collider)) { hit = h; return true; } }
            hit = default(RaycastHit); return false;
        }
        public bool Free(Cell cell, PlayerContext p)
        {
            if (!Rules.ValidCell(cell.X, cell.Y, cell.Z) || Blocks.ContainsKey(cell) || Blocks.Count >= plugin.Settings.MaxBlocks.Value) return false;
            Vector3 center = Center(cell);
            // Conservative occupied-space test also catches player colliders on ignored physics layers.
            var box = new Bounds(center, Vector3.one * Size * .98f);
            if (p != null && box.Intersects(p.Collider.bounds)) return false;
            return Physics.OverlapBox(center, Vector3.one * Size * .485f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Length == 0;
        }
        public void UpdatePreview(PlayerContext p, bool show)
        {
            PreviewValid = false;
            RaycastHit hit;
            if (!show || !Aim(p, plugin.Settings.Reach.Value, out hit)) { preview.SetActive(false); return; }
            if (plugin.Bridge.Enemy(hit.collider) || hit.rigidbody) { preview.SetActive(false); return; }
            PreviewCell = CellAt(hit.point + hit.normal * Size * .02f);
            PreviewValid = Free(PreviewCell, p);
            preview.transform.localPosition = Center(PreviewCell);
            preview.GetComponent<Renderer>().sharedMaterials = Enumerable.Repeat(PreviewValid ? plugin.Art.GhostValid : plugin.Art.GhostInvalid, 6).ToArray();
            preview.SetActive(true);
        }
        public bool PlacePreview(PlayerContext p, BlockKind kind)
        {
            // Re-query at click time; do not trust a previous frame's preview.
            UpdatePreview(p, true);
            if (!PreviewValid) { plugin.Notice("Placement blocked — collision, occupied cell, or block limit."); return false; }
            Add(PreviewCell, kind); return true;
        }
        public PlacedBlock Add(Cell cell, BlockKind kind)
        {
            if (Blocks.ContainsKey(cell)) throw new InvalidOperationException("Duplicate block cell");
            var go = plugin.Art.Block("MCUK_" + kind + "_" + cell, root.transform, kind, Center(cell), Size, true);
            go.layer = layer; go.isStatic = true;
            var block = go.AddComponent<PlacedBlock>(); block.Cell = cell; block.Kind = kind;
            Blocks.Add(cell, block); Layout.Add(cell, kind); Dirty = true;
            Physics.SyncTransforms(); plugin.DebugLog("Placed " + kind + " at " + cell); return block;
        }
        public void Remove(PlacedBlock block)
        {
            if (!block || block.Kind == BlockKind.Bedrock) return;
            Blocks.Remove(block.Cell); Layout.Remove(block.Cell); block.gameObject.SetActive(false); UnityEngine.Object.Destroy(block.gameObject); Dirty = true;
        }
        public void ClearPreview() { if (preview) preview.SetActive(false); PreviewValid = false; }
        public void MarkSaved() { Dirty = false; }
        public void LoadOrigin(Vector3 origin) { if (Blocks.Count != 0) throw new InvalidOperationException(); Origin = origin; }
        public void Dispose()
        {
            Blocks.Clear(); Layout.Clear(); if (root) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
        }
    }
}
