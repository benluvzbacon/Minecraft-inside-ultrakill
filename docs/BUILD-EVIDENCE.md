# Build evidence — 2026-10-13

## Actual environment and limitations

Linux sandbox at `/home/user/Minecraft-inside-ultrakill`, starting commit b840989ecaaf58fb17712e051fa56d26fb08f429. No installed ULTRAKILL, Steam, Unity runtime/editor or .NET SDK. Outbound hosts are restricted to GitHub/npm/PyPI; the normal Microsoft SDK/NuGet download hosts are unavailable here. No installed-game version is claimed.

## Compilation actually executed

```bash
python scripts/compile-sandbox.py
```

This restricted-environment fallback invokes **real Roslyn** 2.3.2.61921 with `-nostdlib+ -langversion:7 -target:library -optimize+ -deterministic+ -warnaserror+`, using existing public-reference binaries kept only in `.cache`. It does NOT synthesize fake game/Unity API stubs, and does NOT reference Assembly-CSharp for compilation.

Tool provenance, not distributable mod dependencies:
- npm `dotnet-sdk-2.0.0-linux@1.0.5`: compiler distribution, unpacked under `.cache/sdk`.
- PyPI `dotnetcore2==3.1.23`: temporary compiler/test host, `.cache/python` (invariant globalization + major runtime roll-forward).
- OpenSSL OpenSSL_1_1_1w source from openssl/openssl GitHub tag, built shared only in `.cache` because the old temporary test host requires libssl 1.1. Never installed into system libraries, never packaged.
- Real Unity 2022-era, Mono reference libraries, BepInEx 5.4.21 and HarmonyX assembly 2.9.0 in public wafflethings/UltraTweaker commit 642900cd9fdc9cbfa612a84ef822fc78bf3e213e, ignored research cache. The reference repository also contains game DLLs: those are not build inputs, committed, packaged or distributed. Historical metadata was inspected only for API evidence.

SDK 8/public-NuGet builds are the supported clean-checkout path in README. **Update: the full clean-checkout SDK 8/NuGet path has now executed successfully on GitHub Actions**, not inside this restricted sandbox. The first run was https://github.com/benluvzbacon/Minecraft-inside-ultrakill/actions/runs/38382306911 (commit 5442c8d); tests, restore/compile/package/inspection and artifact upload all passed. Latest building refinements are validated in subsequent runs. Do not install the old compiler host/OpenSSL as runtime dependencies for the mod or as the normal build setup.

## Initial sandbox results (before the building refinements)

- Real C# mod compilation with warnings as errors: **PASS**, zero diagnostics.
- Pure policy executable: **PASS — 43 assertions**.
- Clean **source-copy** compilation/tests: **PASS** in `.cache/clean-source`, using the same real references; the path-mapped DLL is byte-identical across both source roots. This is not a claim of a NuGet clean-checkout restore.
- Assembly identity: `MinecraftUltrakill`, version `0.1.0.0`, target-framework attribute `.NETFramework,Version=v4.7.2`.
- Plugin metadata: BepInPlugin `benluvzbacon.minecraft.ultrakill`, version `0.1.0`, process filter `ULTRAKILL.exe`.
- Assembly references: mscorlib/System/System.Core 4.0.0.0; BepInEx 5.4.21.0; 0Harmony 2.9.0.0; UnityEngine Core/Physics/IMGUI/JSON/InputLegacy/ParticleSystem modules 0.0.0.0. **No Assembly-CSharp reference**, no bundled game or Minecraft assets.
- Final ZIP validation: checks archive integrity, safe paths, correct plugin layout, only one own DLL, PE DLL header, CLR IL-only and no 32-bit-required flag. SHA256 is supplied next to the ZIP.

## Deliverable paths

Sandbox-compiled DLL:
`release/BepInEx/plugins/MinecraftUltrakill/MinecraftUltrakill.dll`

Installable ZIP:
`release/MinecraftUltrakill-0.1.0.zip`

Supported SDK build output:
`src/MinecraftUltrakill/bin/Release/net472/MinecraftUltrakill.dll`

**In-game load/playability testing: NOT RUN.** Real Windows Steam/Unity/native physics, loader patch application, multiple enemy damage/deaths, train handling, first-/third-person appearance and transitions remain the release gate in TESTING.md. A compiled installable candidate is not a certified playable release.

## Building refinements

- Grid policy now selects cells outside non-grid-aligned level surfaces and exact neighbors on placed cubes.
- Floor calibration filters player, enemy and moving colliders; automatic block layer uses the real floor. An explicit collision-layer config is available. Player-ignored layers reject placement instead of silently creating pass-through blocks.
- The preview shows rejection reasons and current block count. Preview materials are cached, not reallocated each frame.
- Held secondary fire repeats placements at a configurable bounded rate, requires a fresh press after switches/menus/selection changes, and never emits catch-up bursts.
- New pure tests cover six faces, slopes, offset origins, every axis/sign across five grid sizes, float-boundary tolerance and repeat safety. See the latest Actions run's policy-test notice for the assertion count.

The Actions DLL is now the preferred artifact, built by the supported SDK pipeline. The historical sandbox output path above documents the first turn; ignored caches and generated files are not a persistent build environment.
