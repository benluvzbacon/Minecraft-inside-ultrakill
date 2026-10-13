# Configuration

The plugin binds and generates `BepInEx/config/benluvzbacon.minecraft.ultrakill.cfg` at startup. `config/*.example` documents defaults and is packaged as documentation, not copied over a user's config.

| Section / key | Default | Meaning |
|---|---:|---|
| Controls / SwitchCharacter | F | Unity KeyCode; remap vanilla punch if shared |
| Controls / BlockPalette | E | Palette toggle, 1–8 choose blocks |
| Controls / IgniteTNT | R | Targeted TNT ignition |
| Controls / ThirdPersonView | F5 | Existing-camera shoulder view |
| Building / PlacementDistance | 6 | 1–20 world units |
| Building / GridSize | 1 | 0.25–4 units; fixed when level world is created |
| Building / MiningSpeed | 1 | 0.1–20 multiplier |
| Building / BlockCollisionLayer | -1 | Actual floor layer automatically; otherwise Environment/Default. Set 0–31 to override; must collide with player |
| Building / HoldToPlace | true | Hold secondary fire after a fresh press to build repeatedly |
| Building / PlacementInterval | 0.2 | 0.1–2 seconds between placements; no catch-up bursts |
| Building / MaxBlocks | 2048 | 1–10000 hard cap including loaded cells |
| Combat / SwordDamage | 2 | Native game damage units, 0.01–100 |
| Combat / SwordCooldown | 0.45 | 0.1–5 seconds |
| Combat / AxeDamage | 3.5 | Native damage units, 0.01–100 |
| Combat / AxeCooldown | 0.9 | 0.1–5 seconds |
| Combat / AxeMiningSpeed | 4 | 1–20 wood multiplier; other blocks get 1.5 |
| Combat / MeleeRange | 3 | 0.5–8 world units |
| Character / SteveMovementSpeed | 0.45 | 0.1–1.5 **multiplier** of snapshotted native walkSpeed, not m/s |
| Persistence / SaveBlocks | false | Opt-in separate JSON layouts, autosave every 15s plus cleanup |
| Diagnostics / DebugLogging | false | Successful placement/mining/damage/fuse/save events; no per-frame logging |
| TNT / FuseSeconds | 3 | 0.5–10 seconds |
| TNT / Radius | 4 | 1–10 units |
| TNT / Damage | 6 | 0.01–100 enemy damage units; player receives max units × 10 with falloff |

Edit with the game closed. Speed changes apply on entering Steve; grid changes on world creation. The grid Y origin is calibrated from a ground ray near spawn. On non-grid-aligned surfaces the placement cell snaps outward, potentially leaving a gap of less than one grid unit. Slopes/complex geometry still reject intersecting cells rather than force overlapping colliders. Placing onto another mod block uses its exact adjacent cell on the hit face. Placement is creative mode with no resource count.

World files live at `BepInEx/config/MinecraftUltrakill/worlds/<scene-path-and-grid-hash>.json`. A previous version is retained as `.bak`; restore manually with the game closed. Invalid files are logged and ignored. Saves never reference vanilla save paths. Scene destruction does not discard the plain layout records before the unload save.
