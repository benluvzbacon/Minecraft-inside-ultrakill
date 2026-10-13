#!/usr/bin/env python3
import pathlib, sys, zipfile, hashlib
root = pathlib.Path(__file__).resolve().parents[1]
dll = pathlib.Path(sys.argv[1]).resolve()
if not dll.is_file() or dll.stat().st_size < 1024 or dll.read_bytes()[:2] != b'MZ':
    raise SystemExit('Expected a real compiled Windows-compatible DLL, not a source file.')
release = root / 'release'; release.mkdir(exist_ok=True)
archive = release / 'MinecraftUltrakill-0.1.0.zip'
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
    z.write(dll, 'BepInEx/plugins/MinecraftUltrakill/MinecraftUltrakill.dll')
    for name in ['README.md', 'LICENSE', 'docs/TESTING.md', 'docs/ARCHITECTURE.md', 'docs/BUILD-EVIDENCE.md', 'docs/CONFIGURATION.md']:
        z.write(root / name, 'BepInEx/plugins/MinecraftUltrakill/' + name)
    # Documentation/example only; do NOT overwrite a user's config on extraction.
    z.write(root / 'config/benluvzbacon.minecraft.ultrakill.cfg.example', 'BepInEx/plugins/MinecraftUltrakill/config.example')
checksum = hashlib.sha256(archive.read_bytes()).hexdigest()
(archive.with_suffix('.zip.sha256')).write_text(checksum + '  ' + archive.name + '\n')
print('Packaged:', archive)
print('SHA256:', checksum)
