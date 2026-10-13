using System;

namespace MinecraftUltrakill.Core
{
    public enum BlockKind { Grass, Dirt, Stone, Cobblestone, OakPlanks, Glass, TNT, Bedrock }
    public enum ItemKind { Block, Sword, Axe }
    public struct Cell : IEquatable<Cell>
    {
        public readonly int X, Y, Z;
        public Cell(int x, int y, int z) { X = x; Y = y; Z = z; }
        public bool Equals(Cell other) { return X == other.X && Y == other.Y && Z == other.Z; }
        public override bool Equals(object obj) { return obj is Cell && Equals((Cell)obj); }
        public override int GetHashCode() { unchecked { return (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791); } }
        public override string ToString() { return X + "," + Y + "," + Z; }
    }
    public static class Rules
    {
        public const int SaveVersion = 1;
        public static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
        public static int Snap(float coordinate, float size)
        {
            if (!Finite(coordinate) || !Finite(size) || size < 0.25f || size > 4f) throw new ArgumentOutOfRangeException();
            return checked((int)Math.Floor(coordinate / size));
        }
        public static int Slot(int current, int delta) { return ((current + delta) % 9 + 9) % 9; }
        public static float Hardness(BlockKind kind)
        {
            switch (kind)
            {
                case BlockKind.Grass: return 0.7f;
                case BlockKind.Dirt: return 0.5f;
                case BlockKind.Stone: return 2.2f;
                case BlockKind.Cobblestone: return 2.5f;
                case BlockKind.OakPlanks: return 1.5f;
                case BlockKind.Glass: return 0.3f;
                case BlockKind.TNT: return 0.4f;
                case BlockKind.Bedrock: return float.PositiveInfinity;
                default: throw new ArgumentOutOfRangeException("kind");
            }
        }
        public static float MineSeconds(BlockKind kind, ItemKind tool, float globalSpeed, float axeSpeed)
        {
            float multiplier = tool == ItemKind.Axe ? (kind == BlockKind.OakPlanks ? axeSpeed : 1.5f) : 1f;
            return Hardness(kind) / Math.Max(0.1f, globalSpeed * multiplier);
        }
        public static bool ValidCell(int x, int y, int z)
        { return Math.Abs((long)x) <= 100000 && Math.Abs((long)y) <= 100000 && Math.Abs((long)z) <= 100000; }
    }
    public sealed class Hotbar
    {
        public int Selected { get; private set; }
        public readonly BlockKind[] Blocks = { BlockKind.Grass, BlockKind.Dirt, BlockKind.Stone, BlockKind.Cobblestone, BlockKind.OakPlanks, BlockKind.Glass, BlockKind.TNT };
        public ItemKind Item { get { return Selected == 7 ? ItemKind.Sword : Selected == 8 ? ItemKind.Axe : ItemKind.Block; } }
        public BlockKind Block { get { return Blocks[Math.Min(Selected, 6)]; } }
        public void Select(int slot) { Selected = Rules.Slot(slot, 0); }
        public void Scroll(int delta) { Selected = Rules.Slot(Selected, delta); }
        public void Assign(BlockKind kind) { if (Item == ItemKind.Block) Blocks[Selected] = kind; }
    }
}
