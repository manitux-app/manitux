#!/usr/bin/env python3
import struct
import sys
from pathlib import Path

LC_RPATH = 0x8000001C
REPLACEMENT = b"@loader_path"


def patch_file(path: Path) -> tuple[int, bool]:
    data = bytearray(path.read_bytes())
    if len(data) < 32:
        return 0, False

    magic = struct.unpack_from(">I", data, 0)[0]
    if magic == 0xFEEDFACF:
        endian = ">"
    elif magic == 0xCFFAEDFE:
        endian = "<"
    else:
        return 0, False

    ncmds = struct.unpack_from(endian + "I", data, 16)[0]
    offset = 32
    patched = 0

    for _ in range(ncmds):
        if offset + 8 > len(data):
            raise ValueError(f"{path}: invalid Mach-O load command table")

        cmd, cmdsize = struct.unpack_from(endian + "II", data, offset)
        if cmdsize < 8 or offset + cmdsize > len(data):
            raise ValueError(f"{path}: invalid Mach-O load command size")

        if cmd == LC_RPATH:
            path_offset = struct.unpack_from(endian + "I", data, offset + 8)[0]
            start = offset + path_offset
            end = data.find(b"\0", start, offset + cmdsize)
            if end == -1:
                raise ValueError(f"{path}: unterminated LC_RPATH")

            current = bytes(data[start:end])
            if current.startswith(b"/nix/store/"):
                if len(REPLACEMENT) > len(current):
                    raise ValueError(f"{path}: replacement rpath is too long")
                data[start:end] = REPLACEMENT + b"\0" * (len(current) - len(REPLACEMENT))
                patched += 1

        offset += cmdsize

    if patched:
        path.write_bytes(data)

    return patched, True


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: patch-macho-rpaths.py <libs-dir>", file=sys.stderr)
        return 2

    libs_dir = Path(sys.argv[1])
    if not libs_dir.is_dir():
        print(f"missing libs directory: {libs_dir}", file=sys.stderr)
        return 1

    total = 0
    machos = 0
    for path in sorted(libs_dir.glob("*.dylib")):
        patched, is_macho = patch_file(path)
        machos += 1 if is_macho else 0
        total += patched
        if patched:
            print(f"patched {patched} LC_RPATH entr{'y' if patched == 1 else 'ies'}: {path.name}")

    if machos == 0:
        print(f"no Mach-O dylibs found in {libs_dir}", file=sys.stderr)
        return 1

    print(f"patched {total} /nix/store LC_RPATH entr{'y' if total == 1 else 'ies'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
