using System;
using MinecraftUltrakill.Core;

public static class Program
{
    static int assertions;
    static void Check(bool condition, string name) { assertions++; if (!condition) throw new Exception("FAIL: " + name); }
    public static void Main()
    {
        Check(Rules.Snap(0, 1) == 0, "zero grid");
        Check(Rules.Snap(.99f, 1) == 0, "positive cell");
        Check(Rules.Snap(1, 1) == 1, "boundary");
        Check(Rules.Snap(-.01f, 1) == -1, "negative floor not truncate");
        Check(Rules.Snap(-1, 1) == -1, "negative boundary");
        Check(Rules.Snap(1.3f, .5f) == 2, "variable grid");
        bool rejected = false; try { Rules.Snap(float.NaN, 1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "NaN rejected");
        rejected = false; try { Rules.Snap(1, 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "zero grid rejected");
        Check(!Rules.Finite(float.PositiveInfinity), "infinity rejected");
        Check(Rules.ValidCell(-100000, 0, 100000), "valid save coordinate");
        Check(!Rules.ValidCell(int.MinValue, 0, 0), "malicious min int");
        Check(!Rules.ValidCell(100001, 0, 0), "out of world bound");
        Check(new Cell(-1, 2, 3).Equals(new Cell(-1, 2, 3)), "cell equality");
        Check(!new Cell(-1, 2, 3).Equals(new Cell(1, 2, 3)), "cell distinction");
        Check(new Cell(-1, 2, 3).GetHashCode() == new Cell(-1, 2, 3).GetHashCode(), "stable hash");
        Check(Rules.Slot(0, -1) == 8, "scroll wraps left");
        Check(Rules.Slot(8, 1) == 0, "scroll wraps right");
        Check(Rules.Slot(0, -19) == 8, "multi-wrap");
        foreach (BlockKind block in Enum.GetValues(typeof(BlockKind)))
        {
            Check(Rules.Hardness(block) > 0, "positive hardness " + block);
            Check(Rules.MineSeconds(block, ItemKind.Axe, 1, 4) > 0, "positive duration " + block);
        }
        Check(float.IsPositiveInfinity(Rules.MineSeconds(BlockKind.Bedrock, ItemKind.Axe, 20, 20)), "bedrock always infinite");
        Check(Rules.MineSeconds(BlockKind.OakPlanks, ItemKind.Axe, 1, 4) < Rules.MineSeconds(BlockKind.OakPlanks, ItemKind.Sword, 1, 4), "axe mines wood faster");
        Check(Rules.MineSeconds(BlockKind.Dirt, ItemKind.Block, 2, 4) == Rules.Hardness(BlockKind.Dirt) / 2, "global speed");
        var bar = new Hotbar(); Check(bar.Item == ItemKind.Block && bar.Block == BlockKind.Grass, "initial selection");
        bar.Assign(BlockKind.Bedrock); Check(bar.Block == BlockKind.Bedrock, "palette assignment");
        bar.Select(7); Check(bar.Item == ItemKind.Sword, "sword slot");
        bar.Assign(BlockKind.Dirt); bar.Select(0); Check(bar.Block == BlockKind.Bedrock, "weapon cannot overwrite block slot");
        bar.Select(8); Check(bar.Item == ItemKind.Axe, "axe slot");
        bar.Scroll(1); Check(bar.Selected == 0, "hotbar scroll wraps");
        Console.WriteLine("PASS: " + assertions + " policy assertions. No Unity/gameplay tests were run.");
    }
}
