using System;

namespace MinecraftUltrakill.Core
{
    public enum PlacementFailure
    {
        None, NoSurface, MovingSurface, EnemySurface, OutOfBounds,
        BlockLimit, OccupiedCell, PlayerSpace, LevelCollision, PlayerCollisionDisabled
    }

    // Pure grid policy: actual raycasts/collision checks remain in BlockWorld.
    public static class GridPlacement
    {
        static int Axis(float nx, float ny, float nz)
        {
            if (!Rules.Finite(nx) || !Rules.Finite(ny) || !Rules.Finite(nz) ||
                Math.Max(Math.Abs(nx), Math.Max(Math.Abs(ny), Math.Abs(nz))) < .001f)
                throw new ArgumentOutOfRangeException("normal");
            // Prefer vertical in ties, so a 45-degree ramp has predictable upward placement.
            if (Math.Abs(ny) >= Math.Abs(nx) && Math.Abs(ny) >= Math.Abs(nz)) return 1;
            return Math.Abs(nx) >= Math.Abs(nz) ? 0 : 2;
        }
        public static Cell Adjacent(Cell block, float nx, float ny, float nz)
        {
            int axis = Axis(nx, ny, nz);
            checked
            {
                return new Cell(block.X + (axis == 0 ? Math.Sign(nx) : 0),
                    block.Y + (axis == 1 ? Math.Sign(ny) : 0),
                    block.Z + (axis == 2 ? Math.Sign(nz) : 0));
            }
        }
        public static Cell Surface(float x, float y, float z, float nx, float ny, float nz,
            float ox, float oy, float oz, float size)
        {
            // Snap tangents normally; choose the first whole grid cell OUTSIDE the hit plane.
            // A tiny outward point offset chose intersecting cells on non-grid-aligned game walls.
            float rx = x - ox, ry = y - oy, rz = z - oz;
            int cx = Rules.Snap(rx, size), cy = Rules.Snap(ry, size), cz = Rules.Snap(rz, size);
            int axis = Axis(nx, ny, nz);
            float relative = (axis == 0 ? rx : axis == 1 ? ry : rz) / size;
            float normal = axis == 0 ? nx : axis == 1 ? ny : nz;
            const float tolerance = .0001f;
            int outside = checked((int)(normal > 0 ? Math.Ceiling(relative - tolerance) : Math.Floor(relative + tolerance) - 1));
            return new Cell(axis == 0 ? outside : cx, axis == 1 ? outside : cy, axis == 2 ? outside : cz);
        }
        public static string Explain(PlacementFailure failure)
        {
            switch (failure)
            {
                case PlacementFailure.None: return "Right-click: place block";
                case PlacementFailure.NoSurface: return "Aim at a level surface within reach";
                case PlacementFailure.MovingSurface: return "Cannot build on moving physics objects";
                case PlacementFailure.EnemySurface: return "Cannot place blocks on enemies";
                case PlacementFailure.OutOfBounds: return "Outside supported world coordinates";
                case PlacementFailure.BlockLimit: return "Level block limit reached";
                case PlacementFailure.OccupiedCell: return "That grid cell already contains a block";
                case PlacementFailure.PlayerSpace: return "Block would overlap the player";
                case PlacementFailure.LevelCollision: return "Block would intersect level geometry or an object";
                case PlacementFailure.PlayerCollisionDisabled: return "Block layer cannot collide with player; check config/log";
                default: return "Invalid placement";
            }
        }
    }
}
