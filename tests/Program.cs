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
        PlacementPolicies();
        RepeatPolicies();
        Console.WriteLine("PASS: " + assertions + " policy assertions. No Unity/gameplay tests were run.");
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            Console.WriteLine("::notice title=Policy test results::" + assertions + " policy assertions passed. No Unity or in-game tests were run.");
    }
    static void PlacementPolicies()
    {
        var cell = new Cell(-3, 7, 2);
        Check(GridPlacement.Adjacent(cell, 1, 0, 0).Equals(new Cell(-2, 7, 2)), "east adjacent");
        Check(GridPlacement.Adjacent(cell, -1, 0, 0).Equals(new Cell(-4, 7, 2)), "west adjacent");
        Check(GridPlacement.Adjacent(cell, 0, 1, 0).Equals(new Cell(-3, 8, 2)), "top adjacent");
        Check(GridPlacement.Adjacent(cell, 0, -1, 0).Equals(new Cell(-3, 6, 2)), "bottom adjacent");
        Check(GridPlacement.Adjacent(cell, 0, 0, 1).Equals(new Cell(-3, 7, 3)), "front adjacent");
        Check(GridPlacement.Adjacent(cell, 0, 0, -1).Equals(new Cell(-3, 7, 1)), "back adjacent");
        Check(GridPlacement.Adjacent(cell, 1, 1, 0).Equals(new Cell(-3, 8, 2)), "slope tie chooses vertical");
        Check(GridPlacement.Surface(.3f, .2f, .3f, 0, 1, 0, 0, .2f, 0, 1).Equals(new Cell(0, 0, 0)), "calibrated floor");
        Check(GridPlacement.Surface(.3f, 2.45f, .3f, 0, 1, 0, 0, 0, 0, 1).Equals(new Cell(0, 3, 0)), "non-grid floor snaps outward");
        Check(GridPlacement.Surface(-.2f, .3f, .3f, -1, 0, 0, 0, 0, 0, 1).Equals(new Cell(-2, 0, 0)), "negative wall snaps outward");
        Check(GridPlacement.Surface(.000001f, .3f, .3f, 1, 0, 0, 0, 0, 0, 1).X == 0, "float noise at positive boundary");
        Check(GridPlacement.Surface(-.000001f, .3f, .3f, -1, 0, 0, 0, 0, 0, 1).X == -1, "float noise at negative boundary");
        bool rejected = false;
        try { GridPlacement.Adjacent(cell, 0, 0, 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "zero normal rejected");
        rejected = false;
        try { GridPlacement.Adjacent(cell, float.NaN, 1, 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "NaN normal rejected");
        rejected = false;
        try { GridPlacement.Surface(1, 1, 1, 0, 1, 0, 0, float.NaN, 0, 1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "NaN origin rejected");
        foreach (float size in new[] { .25f, .5f, 1f, 2f, 4f })
        foreach (float position in new[] { -2.75f, -2f, -.2f, 0, .15f, 1f, 3.9999f })
        for (int axis = 0; axis < 3; axis++)
        foreach (float sign in new[] { -1f, 1f })
        {
            float origin = size * .17f;
            var result = GridPlacement.Surface(position, position, position,
                axis == 0 ? sign : 0, axis == 1 ? sign : 0, axis == 2 ? sign : 0,
                origin, origin, origin, size);
            int index = axis == 0 ? result.X : axis == 1 ? result.Y : result.Z;
            float edge = origin + (index + (sign < 0 ? 1 : 0)) * size;
            float gap = (edge - position) * sign;
            Check(gap >= -size * .0002f && gap <= size * 1.0002f, "outward plane at every axis/scale/sign");
        }
        foreach (PlacementFailure failure in Enum.GetValues(typeof(PlacementFailure)))
            Check(!string.IsNullOrWhiteSpace(GridPlacement.Explain(failure)), "placement reason " + failure);
    }
    static void RepeatPolicies()
    {
        var repeat = new ActionRepeater();
        Check(!repeat.Take(0, false, true, .2f), "held before entering does not build");
        Check(repeat.Take(0, true, true, .2f), "fresh press builds");
        Check(!repeat.Take(.1f, false, true, .2f), "cooldown prevents repeat");
        Check(repeat.Take(.2f, false, true, .2f), "held repeat at interval");
        Check(!repeat.Take(.2f, false, true, .2f), "no duplicate action same frame");
        Check(repeat.Take(5, false, true, .2f), "long frame yields one action");
        Check(!repeat.Take(5, false, true, .2f), "no burst catch-up");
        Check(!repeat.Take(6, false, false, .2f), "release disarms");
        Check(!repeat.Take(7, false, true, .2f), "fresh press required after release");
        repeat.Reset();
        Check(!repeat.Take(8, false, true, .2f), "reset requires fresh press");
        Check(repeat.Take(8, true, true, .2f), "fresh press after reset");
        bool rejected = false;
        try { repeat.Take(8, true, true, 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "zero interval rejected");
    }

}
