# Validation and in-game release gate

## Executed in the development sandbox

- Actual Roslyn C# compilation against real Unity, BepInEx and Harmony references, warnings treated as errors: **PASS**.
- Pure policy console tests: **43 assertions PASS** (grid positive/negative boundaries and sizes; invalid floats; coordinate limits; cell equality/hash; all eight material durations; indestructible bedrock; axe bonus; inventory slots/assignment/wrapping).
- Final archive/PE/CLI checks and dependency inspection: recorded in BUILD-EVIDENCE.md.

These tests DO NOT instantiate Unity, run Harmony patches or exercise the game. No fabricated Unity/game stubs are used to pretend gameplay was tested.

## Not executed — requires Windows Steam ULTRAKILL

Record Steam build ID, Unity version, BepInEx/Harmony version, other mods and test level for each result. Start on a clean mod profile and keep LogOutput.log.

| Test | Required observation | Status |
|---|---|---|
| Load/startup | One plugin, runtime API verified, no loader/errors; vanilla works if gate rejects API | NOT RUN |
| Switching ×50 | Position/velocity/enemy state unchanged; one player/Rigidbody/movement/camera; correct models, weapons and HUD restore | NOT RUN |
| Switch during fight | Steve cannot fire guns/punch/hook; V1 regains prior gun/arm; no stale melee on switch | NOT RUN |
| Transition/death/restart | V1 restored; no orphan models/camera offsets; new player binds; level blocks cleaned | NOT RUN |
| Walk/jump/ramp/stairs | Ground checks, triggers, gravity and landing work; Steve speed reasonable; dash/slide/slam/walljump blocked only for Steve | NOT RUN |
| First-/third-person | Steve proportions and limbs correct; viewmodel not clipped; shoulder camera avoids walls; camera restores on exit/death | NOT RUN |
| Floors/walls/slopes | Green/red preview matches placement, grid calibration comfortable; invalid intersections, enemies/moving props/player rejected | NOT RUN |
| All eight block types | Recognizable generated textures; blocks stationary; glass transparency; physical collisions and adjacency | NOT RUN |
| Mining | Hold progress, release/target/selection resets, material times differ, axe wood bonus, collider gone immediately | NOT RUN |
| Bedrock | Cannot mine/blast; player collision; physics-layer matrix checked | NOT RUN |
| Train obstruction | Inspect Rigidbody/colliders/layers and whether position is scripted; test real physical obstruction, not hiding/disabling train | BLOCKED: no game installed |
| Filth/Stray/Schism/Maurice | Sword and axe register once per swing, correct cooldowns/damage, death/kill counters/gore/AI unchanged | NOT RUN |
| Specialized bosses/idols | Native damage handles protection/phase/death normally; identify exact incompatibilities before adding patches | NOT RUN |
| Wall attack | Enemy behind wall, edge of spherecast, multiple limb colliders, point-blank overlap; no through-wall/double damage | NOT RUN |
| TNT | R fuse, visible flash/explosion, falloff/player/enemy damage, line-of-sight, chain delay, max four explosions/frame and 16 effects, bedrock survives | NOT RUN |
| Palette/hotbar | 1–9/wheel/item icons sync; E then 1–8 selects all blocks incl. bedrock; V1 selection unaffected | NOT RUN |
| Pause/terminal/menu | No placement/mining/attack/switch when paused or cursor unlocked; fire rebinding respected | NOT RUN |
| Layout persistence | Opt-in save/reload same scene/grid, scene unload autosave, no cross-level leak, no duplicate cells; TNT unprimed on reload | NOT RUN |
| Bad save | >4MiB, too many cells, wrong schema/grid, NaN origin, extreme ints, unknown IDs, duplicate/occupied cells rejected | NOT RUN |
| Performance/conflicts | Cap load, GC/physics costs, other mods; no per-frame log flood or growing resource/effect count | NOT RUN |

A train cannot be guaranteed to stop just because a collider exists. Kinematic/script-controlled movers and NavMesh agents may not use physical collision. Inspect that exact game's mover before claiming train compatibility.

## Reporting a failure

Include test row, level, Steam build ID, full initialization/error log, configuration and other mods. If the runtime gate rejects the game API, inspect that installation's assemblies locally; do not weaken the gate or substitute an imaginary signature. Do not publish an in-game-validated release until the above gate is exercised.
