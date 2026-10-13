# Architecture and evidence (before implementation)

## Environment audit — 2026-10-13

The starting checkout at b840989 contains only a 29-byte README. The Linux sandbox has no Steam installation, ULTRAKILL executable, installed Managed directory, BepInEx installation, Unity editor, or .NET SDK. It cannot run the Windows game. Therefore **the installed game's version, train collision behaviour and in-game compatibility are not verified**. This is a release candidate, not a claim of completed game testing.

## API evidence

Public working C# mods were inspected in an ignored research directory:

- Hydraxous/UltraFunGuns, commit b50c59448d193724957afd5c14565a90d238c5c4: `Components/Weapons/ColossalHammer/ColossalHammer.cs` uses `EnemyIdentifier.DeliverDamage(GameObject, Vector3, Vector3, float, bool, float, GameObject)` and real game enemies; `Components/Weapons/FingerGun/FingerGun.cs` resolves `EnemyIdentifierIdentifier.eid`.
- wafflethings/UltraTweaker, commit 642900cd9fdc9cbfa612a84ef822fc78bf3e213e: `Tweaks/Impl/Speed.cs` reads/restores `NewMovement.walkSpeed`; `ViewmodelTransform.cs` shows the existing main/HUD camera hierarchy. Public reference metadata in that project confirms `modNoDashSlide`, `dead`, `activated`, `WallJump`, `Dodge`, `StartSlide`, `Slamdown`, `GunControl.currentWeapon`, `FistControl.currentArmObject`. Those historical references are **not** an installed/current Steam API guarantee.
- PITR-DEV/ukbutt-mod `UKButt.csproj` references BepInEx.Core 5.x and UnityEngine.Modules 2022.3.29. BepInEx v5.4.23.2 has an official Windows x64 distribution. The inspected historical references contain BepInEx 5.4.21 and HarmonyX assembly 2.9.0.

Sources: https://github.com/Hydraxous/UltraFunGuns ; https://github.com/wafflethings/UltraTweaker ; https://github.com/PITR-DEV/ukbutt-mod ; https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.2

No game assemblies or assets are distributed, committed, or required for CI. Compilation uses public BepInEx/Unity reference packages; optional local-reference builds use the user's installation. A narrowly scoped reflection adapter checks the actual game's API at startup. Missing mandatory members disable character switching and leave V1 untouched; unknown damage signatures are never guessed. No substitute game classes are compiled.

## Modules

- Plugin/config: BepInEx 5 Mono plugin, .NET Framework 4.7.2, Harmony supplied by BepInEx. Installs only scoped ability patches. Scene events drive cleanup. No replacement executable.
- GameInput: runtime-checked NewBlood.LegacyInput buttons and Mouse scroll, native InputSource Fire1/Fire2; fallback for older legacy-enabled Unity. No compile dependency on input/game DLLs.
- GameBridge: verified reflected game types/fields/methods. Finds the real active `NewMovement`, its Rigidbody, existing camera, GunControl/FistControl. Resolves real enemy limb identifiers and exact damage signature; never writes enemy health.
- CharacterManager: transactional state snapshot, own Steve presentation, disable native weapon/arm objects and controllers, retain ONE existing movement controller/camera/player. Snapshot restores on switching, death, exceptions, disable and scene change. Retained NewMovement handles gravity/jumps/ramps/damage/triggers. Steve uses reduced walkSpeed and scoped dash/slide/slam/wall-jump suppression rather than a competing physics controller.
- Presentation/Art: original procedural pixel textures, Steve cuboids and pivot animation, voxel sword/axe; no Mojang/game assets. World body plus camera-local viewmodel. No additional camera. F5 offsets the original camera into an occlusion-aware shoulder view and restores its eye position on exit; first-person body casts shadows without obscuring view.
- BlockWorld: static colliders on the actual floor layer by default (Environment/Default fallback, explicit layer override available); scene-owned root, integer cell dictionary, surface raycasts, occupancy checks, bounds, cap. Placement collision is genuine Unity physics; scripted/teleporting trains or navmesh-only AI are not promised to stop. No runtime navmesh rebake.
- Inventory/UI: nine creative-mode slots, number/scroll selection; E toggles block palette without releasing game cursor, choose with numbers/arrows; all eight blocks available, sword/axe. IMGUI hotbar and progress only in Steve.
- BuildingSystem/GridPlacement: testable outward surface snapping and exact six-face neighbor placement; held secondary fire has a bounded interval/fresh-press gate, cached preview materials and explicit on-screen rejection reasons. Collision checks remain real Unity physics.
- Mining/combat: held-input mining with material hardness/tool multiplier and unbreakable bedrock. Cooldown attacks, sorted physics hits, target-line occlusion and per-swing deduplication. DeliverDamage delegates enemy-specific logic to the game. Damage units are ULTRAKILL units, not Minecraft hearts.
- TNT: explicit R ignition, timed flash/fuse, bounded pooled-by-lifetime visual effects, occluded enemy damage and compatible Rigidbody force, block destruction/chain ignition; bedrock excluded. Player damage uses verified GetHurt signature.
- Persistence: optional versioned JSON in BepInEx/config/MinecraftUltrakill/worlds, separate from vanilla saves; per campaign scene key, size/cap/schema/finite-coordinate validation, atomic replace with backup. Cleanup never destroys game objects. No DontDestroyOnLoad blocks; only plugin persists.

## Build/validation strategy

Normal build: .NET SDK 8, net472 reference package, BepInEx.Core 5.4.21, HarmonyX 2.9.0, UnityEngine.Modules 2022.3.29 compile references. CI tests pure grid/mining/inventory policies, compiles DLL and packages only own DLL/docs. Local references may be provided instead, no private dependencies in Actions. Sandbox restricted-network fallback: real Roslyn compiler obtained from the npm SDK distribution, public historical Unity/BepInEx reference assemblies kept ignored, never packaged. This fallback verifies C# compilation against real assemblies but does not verify NuGet restoration or installed-game loading. These evidence levels are reported separately.

In-game release gate: repeat character switches while firing/moving; death/restart; floors/walls/slopes; collider layer matrix; bedrock vs a genuinely physical train; Filth/Stray/Schism/Maurice/boss damage and deaths; TNT player/enemy damage and occlusion; pause/terminal input; scene transition/persistence/corrupt save; coexistence with other mods. See TESTING.md. A successful compilation is not this gate.
