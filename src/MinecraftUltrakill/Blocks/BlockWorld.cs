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
        readonly Material[] validPreviewMaterials, invalidPreviewMaterials;
        public PlacementFailure Failure { get; private set; }
        public int CollisionLayer { get { return layer; } }
        public Transform EffectsRoot { get { return root.transform; } }
        public bool Dirty { get; private set; }
        public bool PreviewValid { get; private set; }
        public Cell PreviewCell { get; private set; }
        public BlockWorld(Plugin owner, PlayerContext player)
        {
            plugin = owner; Scene = LevelScene(player); SceneName = Scene.name; ScenePath = Scene.path; Size = plugin.Settings.Grid.Value;
            root = new GameObject("MCUK_LevelBlocks"); SceneManager.MoveGameObjectToScene(root, Scene);
            // Ignore the player/arms and moving objects when calibrating the real floor.
            RaycastHit? floor = null;
            foreach (var hit in Physics.RaycastAll(player.Collider.bounds.center, Vector3.down,
                player.Collider.bounds.extents.y + 5f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (player.Owns(hit.collider) || hit.rigidbody || plugin.Bridge.Enemy(hit.collider)) continue;
                floor = hit; break;
            }
            int environment = LayerMask.NameToLayer("Environment");
            layer = plugin.Settings.CollisionLayer.Value >= 0 ? plugin.Settings.CollisionLayer.Value :
                floor.HasValue ? floor.Value.collider.gameObject.layer : environment >= 0 ? environment : 0;
            Origin = floor.HasValue ? new Vector3(0, floor.Value.point.y - Mathf.Floor(floor.Value.point.y / Size) * Size, 0) : Vector3.zero;
            validPreviewMaterials = Enumerable.Repeat(plugin.Art.GhostValid, 6).ToArray();
            invalidPreviewMaterials = Enumerable.Repeat(plugin.Art.GhostInvalid, 6).ToArray();
            Failure = PlacementFailure.NoSurface;
            if (Physics.GetIgnoreLayerCollision(layer, player.Collider.gameObject.layer))
                plugin.LoggerInfo("WARNING: block layer " + layer + " ignores player layer " + player.Collider.gameObject.layer + "; placements will be rejected. Set Building.BlockCollisionLayer to the level's solid floor layer.");
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
        public PlacementFailure Check(Cell cell, PlayerContext p)
        {
            if (!Rules.ValidCell(cell.X, cell.Y, cell.Z)) return PlacementFailure.OutOfBounds;
            if (Blocks.ContainsKey(cell)) return PlacementFailure.OccupiedCell;
            if (Blocks.Count >= plugin.Settings.MaxBlocks.Value) return PlacementFailure.BlockLimit;
            if (p != null && Physics.GetIgnoreLayerCollision(layer, p.Collider.gameObject.layer)) return PlacementFailure.PlayerCollisionDisabled;
            Vector3 center = Center(cell);
            var box = new Bounds(center, Vector3.one * Size * .995f);
            if (p != null && box.Intersects(p.Collider.bounds)) return PlacementFailure.PlayerSpace;
            // Tolerate contact only, not visible interpenetration. The query includes every solid layer.
            foreach (var overlap in Physics.OverlapBox(center, Vector3.one * Size * .499f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                return p != null && p.Owns(overlap) ? PlacementFailure.PlayerSpace : PlacementFailure.LevelCollision;
            return PlacementFailure.None;
        }
        public bool Free(Cell cell, PlayerContext p) { return Check(cell, p) == PlacementFailure.None; }
        public void UpdatePreview(PlayerContext p, bool show)
        {
            PreviewValid = false; Failure = PlacementFailure.NoSurface;
            RaycastHit hit;
            if (!show || !Aim(p, plugin.Settings.Reach.Value, out hit)) { preview.SetActive(false); return; }
            if (plugin.Bridge.Enemy(hit.collider)) { Failure = PlacementFailure.EnemySurface; preview.SetActive(false); return; }
            if (hit.rigidbody) { Failure = PlacementFailure.MovingSurface; preview.SetActive(false); return; }
            var placed = hit.collider.GetComponent<PlacedBlock>();
            PreviewCell = placed ? GridPlacement.Adjacent(placed.Cell, hit.normal.x, hit.normal.y, hit.normal.z) :
                GridPlacement.Surface(hit.point.x, hit.point.y, hit.point.z, hit.normal.x, hit.normal.y, hit.normal.z,
                    Origin.x, Origin.y, Origin.z, Size);
            Failure = Check(PreviewCell, p); PreviewValid = Failure == PlacementFailure.None;
            preview.transform.localPosition = Center(PreviewCell);
            preview.GetComponent<Renderer>().sharedMaterials = PreviewValid ? validPreviewMaterials : invalidPreviewMaterials;
            preview.SetActive(true);
        }
        public bool PlacePreview(PlayerContext p, BlockKind kind, bool showError = true)
        {
            // Re-query at click time; do not trust a previous frame's preview.
            UpdatePreview(p, true);
            if (!PreviewValid) { if (showError) plugin.Notice(GridPlacement.Explain(Failure)); return false; }
            Add(PreviewCell, kind); return true;
        }
        public PlacedBlock Add(Cell cell, BlockKind kind)
        {
            if (!Rules.ValidCell(cell.X, cell.Y, cell.Z) || !Enum.IsDefined(typeof(BlockKind), kind))
                throw new ArgumentOutOfRangeException("cell or kind");
            if (Blocks.Count >= plugin.Settings.MaxBlocks.Value) throw new InvalidOperationException("Block limit reached");
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
