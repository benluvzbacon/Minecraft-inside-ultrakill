#!/usr/bin/env python3
"""Restricted-network build using REAL cached references, never API stubs.
Not the supported SDK build. Cache setup/provenance is recorded in docs/BUILD-EVIDENCE.md.
"""
import os, pathlib, subprocess, json
root = pathlib.Path(__file__).resolve().parents[1]
cache = root / '.cache'
dotnet = cache / 'python/dotnetcore2/bin/dotnet'
csc = cache / 'sdk/package/sdk/2.0.0/Roslyn/csc.exe'
libs = cache / 'research/UltraTweaker/UltraTweaker Extension Example/Libs'
output = root / 'release/BepInEx/plugins/MinecraftUltrakill'
output.mkdir(parents=True, exist_ok=True)
refs = [libs / 'Managed' / (x + '.dll') for x in ['mscorlib', 'System', 'System.Core', 'netstandard', 'UnityEngine', 'UnityEngine.CoreModule', 'UnityEngine.PhysicsModule', 'UnityEngine.InputLegacyModule', 'UnityEngine.IMGUIModule', 'UnityEngine.JSONSerializeModule', 'UnityEngine.ParticleSystemModule']]
refs += [libs / 'BepInEx' / (x + '.dll') for x in ['BepInEx', '0Harmony']]
for f in [dotnet,csc] + refs:
    if not f.exists(): raise SystemExit('Missing sandbox cache prerequisite: ' + str(f))
env = dict(os.environ, DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1', DOTNET_ROLL_FORWARD='Major', LD_LIBRARY_PATH=str(cache / 'openssl-OpenSSL_1_1_1w'))
args = ['-noconfig', '-nostdlib+', '-target:library', '-langversion:7', '-optimize+', '-deterministic+', '-warnaserror+', '-pathmap:' + str(root) + '=/_/', '-out:' + str(output / 'MinecraftUltrakill.dll')]
args += ['-r:' + str(f) for f in refs]
args += [str(f) for f in sorted((root / 'src/MinecraftUltrakill').rglob('*.cs')) if 'obj' not in f.parts and 'bin' not in f.parts]
subprocess.run([str(dotnet), str(csc)] + args, env=env, check=True)
# Run pure policy code against actual Core 3.1 runtime, with a console entrypoint.
testout = cache / 'tests'; testout.mkdir(exist_ok=True)
runtime = dotnet.parent / 'shared/Microsoft.NETCore.App/3.1.23'
args = ['-noconfig','-nostdlib+','-target:exe','-langversion:7','-warnaserror+', '-out:' + str(testout / 'PolicyTests.dll')]
args += ['-r:' + str(f) for f in runtime.glob('*.dll')]
args += [str(f) for f in sorted((root / 'src/MinecraftUltrakill/Core').glob('*.cs'))] + [str(root / 'tests/Program.cs')]
subprocess.run([str(dotnet),str(csc)] + args, env=env, check=True)
(testout / 'PolicyTests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'netcoreapp3.1', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '3.1.23'}}}))
subprocess.run([str(dotnet),str(testout / 'PolicyTests.dll')], env=env, check=True)
