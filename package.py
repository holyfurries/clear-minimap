import argparse
import json
import shutil
import struct
import zlib
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--prepare",
        action="store_true",
        help="Refresh the release DLL from the local Release build",
    )
    args = parser.parse_args()
    root = Path(__file__).resolve().parent
    manifest = json.loads((root / "package/manifest.json").read_text())
    assert len(manifest["description"]) <= 250
    assembly = root / "package/ClearMinimap.dll"
    if args.prepare:
        shutil.copyfile(root / "bin/Release/net6.0/ClearMinimap.dll", assembly)
    if not assembly.is_file():
        raise FileNotFoundError("Run a Release build, then python package.py --prepare")
    pixels = bytearray()
    for y in range(256):
        pixels.append(0)
        for x in range(256):
            panel = 32 <= x < 224 and 32 <= y < 224
            border = panel and (x < 37 or x >= 219 or y < 37 or y >= 219)
            road = panel and (
                abs(x - 84) < 5 or abs(y - 168) < 5 or abs(x + y - 250) < 7
            )
            horizontal = (x - 128) / 55
            vertical = (128 - y) / 55
            arrow = (
                -0.7 < vertical < 0.85 and abs(horizontal) < (0.85 - vertical) * 0.48
            )
            arrow = arrow and (
                vertical > -0.15 or abs(horizontal) > (-0.15 - vertical) * 0.4
            )
            marker = (x - 178) ** 2 + (y - 80) ** 2 < 64
            color = (17, 24, 31)
            if panel:
                color = (26, 49, 53)
            if road or border:
                color = (65, 98, 103)
            if marker:
                color = (84, 210, 170)
            if arrow:
                color = (239, 244, 250)
            pixels.extend(color)
    icon = b"\x89PNG\r\n\x1a\n"
    for kind, data in (
        (b"IHDR", struct.pack(">2I5B", 256, 256, 8, 2, 0, 0, 0)),
        (b"IDAT", zlib.compress(bytes(pixels))),
        (b"IEND", b""),
    ):
        icon += (
            struct.pack(">I", len(data))
            + kind
            + data
            + struct.pack(">I", zlib.crc32(kind + data))
        )
    target = root / "dist" / f"ClearMinimap-{manifest['version_number']}.zip"
    target.parent.mkdir(exist_ok=True)
    with ZipFile(target, "w", ZIP_DEFLATED) as archive:
        archive.write(root / "package/manifest.json", "manifest.json")
        archive.write(root / "README.md", "README.md")
        archive.write(root / "LICENSE", "LICENSE")
        archive.writestr("icon.png", icon)
        archive.write(assembly, "Mods/ClearMinimap.dll")
    with ZipFile(target) as archive:
        assert archive.testzip() is None
        assert set(archive.namelist()) == {
            "manifest.json",
            "README.md",
            "LICENSE",
            "icon.png",
            "Mods/ClearMinimap.dll",
        }
    print(target)


if __name__ == "__main__":
    main()
