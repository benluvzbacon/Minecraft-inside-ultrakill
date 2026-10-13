#!/usr/bin/env python3
"""Stdlib-only PE/CLI and archive validation; does not execute Unity or certify gameplay."""
import pathlib, struct, sys, zipfile
path = pathlib.Path(sys.argv[1])
expected = 'BepInEx/plugins/MinecraftUltrakill/MinecraftUltrakill.dll'
with zipfile.ZipFile(path) as z:
    bad = z.testzip()
    assert bad is None, 'Invalid archive checksum: ' + str(bad)
    names = z.namelist()
    assert expected in names, 'Missing mod DLL'
    assert [n for n in names if n.lower().endswith('.dll')] == [expected], 'Do not ship game/Unity/BepInEx dependencies'
    assert not any(n.startswith('/') or '..' in pathlib.PurePosixPath(n).parts for n in names), 'Unsafe archive paths'
    data = z.read(expected)
assert data[:2] == b'MZ', 'Not a PE file'
pe = struct.unpack_from('<I', data, 0x3c)[0]
assert data[pe:pe+4] == b'PE\0\0', 'Invalid PE header'
machine, sections = struct.unpack_from('<HH', data, pe+4)
optional_size, flags = struct.unpack_from('<HH', data, pe+20)
assert flags & 0x2000, 'PE is not a DLL'
opt = pe + 24
magic = struct.unpack_from('<H', data, opt)[0]
assert magic in (0x10b, 0x20b), 'Unsupported optional header'
directories = opt + (96 if magic == 0x10b else 112)
cli_rva, cli_size = struct.unpack_from('<II', data, directories + 14 * 8)
assert cli_rva and cli_size >= 72, 'Missing CLR header'
section_table = opt + optional_size
for i in range(sections):
    row = section_table + i * 40
    virtual_size, virtual_addr, raw_size, raw_pointer = struct.unpack_from('<IIII', data, row + 8)
    if virtual_addr <= cli_rva < virtual_addr + max(virtual_size, raw_size):
        cli = raw_pointer + cli_rva - virtual_addr
        clr_flags = struct.unpack_from('<I', data, cli+16)[0]
        assert clr_flags & 1, 'Assembly contains native code'
        assert not clr_flags & 2, '32-bit-only assembly will not load in x64 ULTRAKILL'
        break
else:
    raise AssertionError('Cannot locate CLR header')
print('PASS: ZIP integrity, install layout, PE DLL, CLR IL-only AnyCPU, no proprietary/dependency DLLs.')
print('DLL bytes:', len(data), '| Files:', len(names))
