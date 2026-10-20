"""Copy CRC-verified original menu/gallery art and local fonts to private assets."""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import shutil
import tarfile
import zlib
from pkr_index import inspect

ROOT = Path(__file__).resolve().parents[2]


def prepare_tmp_resources(package, project):
    """Read named assets from Unity's bundled package; never extract tar paths."""
    project = project.resolve()
    allowed = project / "Assets/TextMesh Pro"
    seen = set()
    with tarfile.open(package, "r:gz") as archive:
        members = archive.getmembers()
        if len(members) > 10000:
            raise ValueError("Unexpected TMP package size")
        for member in members:
            if not member.name.endswith("/pathname"):
                continue
            source = archive.extractfile(member)
            name = source.read(4096).decode("utf-8").strip()
            path = PurePosixPath(name)
            if path.parts[:2] != ("Assets", "TextMesh Pro") or ".." in path.parts or ":" in name:
                raise ValueError("Unexpected TMP asset path: " + name)
            destination = project.joinpath(*path.parts).resolve()
            if destination != allowed and not destination.is_relative_to(allowed):
                raise ValueError("TMP asset escapes allowed root")
            if name.casefold() in seen:
                raise ValueError("Duplicate TMP package path")
            seen.add(name.casefold())
            prefix = member.name.rsplit("/", 1)[0]
            metadata = archive.extractfile(prefix + "/asset.meta").read()
            if b"folderAsset: yes" in metadata:
                destination.mkdir(parents=True, exist_ok=True)
            else:
                asset = archive.getmember(prefix + "/asset")
                if asset.size > 64 * 1024 * 1024 or not asset.isfile():
                    raise ValueError("Invalid TMP asset payload")
                destination.parent.mkdir(parents=True, exist_ok=True)
                payload = archive.extractfile(asset).read()
                if not destination.exists() or destination.read_bytes() != payload:
                    destination.write_bytes(payload)
            meta_path = destination.with_name(destination.name + ".meta")
            if not meta_path.exists():
                meta_path.write_bytes(metadata)
    return len(seen)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", type=Path, default=ROOT / "staging/raw_extracted/PC/app/data.pkr")
    parser.add_argument("--output", type=Path, default=ROOT / "staging/legacy_unity_import/Frontend")
    parser.add_argument("--font", type=Path, default=Path("C:/Windows/Fonts/impact.ttf"))
    parser.add_argument("--tmp-package", type=Path, default=Path(
        "C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage"))
    args = parser.parse_args()
    prepare_tmp_resources(args.tmp_package, ROOT / "Game")
    expected = {"data/title.bmp", "data/shell_bg.bmp", "data/credits.txt"} | {f"data/cb{i:02}.bmp" for i in range(32)}
    found, evidence = set(), []
    with args.archive.open("rb") as stream:
        for entry in inspect(args.archive)["entries"]:
            key = entry["path"].lower()
            if key not in expected:
                continue
            stream.seek(entry["offset"])
            payload = stream.read(entry["stored_bytes"])
            if entry["compressed"]:
                decoder = zlib.decompressobj()
                payload = decoder.decompress(payload, entry["size_bytes"] + 1)
                if not decoder.eof or decoder.unconsumed_tail or decoder.unused_data:
                    raise ValueError("Compressed member bounds")
            if len(payload) != entry["size_bytes"] or f"{zlib.crc32(payload):08x}" != entry["crc32"]:
                raise ValueError("CRC/length mismatch: " + key)
            destination = args.output / "Art" / Path(entry["path"]).name
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(payload)
            evidence.append(dict(source=entry["path"], sha256=hashlib.sha256(payload).hexdigest()))
            found.add(key)
    if found != expected:
        raise ValueError("Missing menu art: " + str(expected - found))
    font_dir = args.output / "Fonts"
    font_dir.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(args.font, font_dir / "MenuDisplay.ttf")
    (args.output / "asset-provenance.json").write_text(json.dumps(dict(
        source_art=evidence, font_source=str(args.font), font_sha256=hashlib.sha256(args.font.read_bytes()).hexdigest(),
        font_redistribution="Private local copy only; font is not included in source control"), indent=2), encoding="utf-8")
    print(f"Prepared {len(found)} original art/text members and one private local font")


if __name__ == "__main__":
    main()
