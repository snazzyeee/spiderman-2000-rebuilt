"""Extract selected PC data, export animated glTF, and externalize its PNGs.

Requires Python 3.11+ and Neversoft Multitool CLI 1.3.4. Never executes game code.
Outputs are deliberately confined to the ignored Assets/_LocalReference tree.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import zlib

from pkr_index import inspect

ROOT = Path(__file__).resolve().parents[2]
CONVERTER_SHA256 = "b83b49333dc70a58a7ff730548287b0a719800eb03408823255f68f3e330b95a"


def extract_selected(archive, destination):
    selected = {"spidey.psx", "spidey.psh", "l1a1_g.psx", "l1a1_t.trg"}
    index = inspect(archive)
    found = {}
    with archive.open("rb") as stream:
        for entry in index["entries"]:
            name = Path(entry["path"]).name
            if entry["path"].lower() not in {"data/" + value for value in selected}:
                continue
            if name.lower() in found:
                raise ValueError("Ambiguous archive member: " + name)
            stream.seek(entry["offset"])
            payload = stream.read(entry["stored_bytes"])
            if entry["compressed"]:
                decoder = zlib.decompressobj()
                payload = decoder.decompress(payload, entry["size_bytes"] + 1)
                if not decoder.eof or decoder.unconsumed_tail:
                    raise ValueError("Invalid/oversized compressed member: " + name)
            if len(payload) != entry["size_bytes"] or f"{zlib.crc32(payload):08x}" != entry["crc32"]:
                raise ValueError("Member CRC/length mismatch: " + name)
            target = destination / name
            target.write_bytes(payload)
            found[name.lower()] = target
    if set(found) != selected:
        raise ValueError("Missing required PC members: " + repr(selected - set(found)))
    return found, index["sha256"]


def externalize_glb(source, destination):
    """Preserve binary geometry, inverse binds, hierarchy, weights and keyframes."""
    blob = source.read_bytes()
    magic, version, total = struct.unpack_from("<III", blob)
    if magic != 0x46546C67 or version != 2 or total != len(blob):
        raise ValueError("Invalid GLB header")
    offset, document, binary = 12, None, None
    while offset < len(blob):
        length, kind = struct.unpack_from("<II", blob, offset)
        offset += 8
        chunk = blob[offset:offset + length]
        if len(chunk) != length:
            raise ValueError("Truncated GLB chunk")
        if kind == 0x4E4F534A:
            document = json.loads(chunk)
        elif kind == 0x004E4942:
            binary = chunk
        offset += length
    if document is None or binary is None or len(document["buffers"]) != 1:
        raise ValueError("Expected a single-buffer GLB")
    destination.mkdir(parents=True, exist_ok=True)
    textures = destination / "Textures"
    textures.mkdir(exist_ok=True)
    images = document.get("images", [])
    for number, image in enumerate(images):
        view = document["bufferViews"][image["bufferView"]]
        start = view.get("byteOffset", 0)
        png = binary[start:start + view["byteLength"]]
        if image["mimeType"] != "image/png" or not png.startswith(b"\x89PNG\r\n\x1a\n"):
            raise ValueError("Expected converter-produced PNG")
        relative = f"Textures/image_{number:03}.png"
        (destination / relative).write_bytes(png)
        image.clear()
        image["uri"] = relative
    # Imported materials will be remapped to actual URP/Lit assets by the builder.
    for material in document.get("materials", []):
        material.get("extensions", {}).pop("KHR_materials_unlit", None)
        material.setdefault("pbrMetallicRoughness", {}).update(metallicFactor=0, roughnessFactor=.85)
    for key in ["extensionsUsed", "extensionsRequired"]:
        if key in document:
            document[key] = [v for v in document[key] if v != "KHR_materials_unlit"]
    document["buffers"][0]["uri"] = source.stem + ".bin"
    (destination / (source.stem + ".bin")).write_bytes(binary[:document["buffers"][0]["byteLength"]])
    target = destination / (source.stem + ".gltf")
    target.write_text(json.dumps(document, separators=(",", ":")), encoding="utf-8")
    return {
        "gltf": target.relative_to(ROOT).as_posix(),
        "meshes": len(document.get("meshes", [])),
        "skins": len(document.get("skins", [])),
        "joints": [len(s["joints"]) for s in document.get("skins", [])],
        "textures": len(images),
        "animations": len(document.get("animations", [])),
        "animation_names": [a.get("name") for a in document.get("animations", [])],
        "animation_semantics_verified": False,
        "sha256": hashlib.sha256(target.read_bytes()).hexdigest(),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", type=Path, default=ROOT / "LocalReference/PC/app/data.pkr")
    parser.add_argument("--converter", type=Path, default=ROOT / "Tools/ThirdParty/neversoft-multitool-1.3.4/NeversoftMultitool-Windows-CLI-x64/NeversoftMultitool.exe")
    args = parser.parse_args()
    if not args.archive.is_file() or not args.converter.is_file():
        parser.error("PC data.pkr and the verified Multitool CLI must exist; see Docs/PHASE1.md.")
    if hashlib.sha256(args.converter.read_bytes()).hexdigest() != CONVERTER_SHA256:
        parser.error("Converter hash differs from the pinned Windows CLI 1.3.4. Do not substitute the game executable.")
    staging = ROOT / "LocalReference/Phase1Conversion"
    raw = staging / "Raw"
    raw.mkdir(parents=True, exist_ok=True)
    members, archive_hash = extract_selected(args.archive, raw)
    converted = staging / "Converted"
    converted.mkdir(exist_ok=True)
    for command in [
        [str(args.converter.resolve()), "psx-anim-export", str(members["spidey.psx"]),
         "--anim", "-1", "--fps", "30", "-o", str(converted / "spidey.glb")],
        [str(args.converter.resolve()), "psx-mesh", str(members["l1a1_g.psx"]), "-o", str(converted)],
    ]:
        subprocess.run(command, check=True, cwd=ROOT)
    target = ROOT / "Game/Assets/_LocalReference/Phase1"
    results = [
        externalize_glb(converted / "spidey.glb", target / "SpiderMan"),
        externalize_glb(converted / "L1A1_G.glb", target / "L1A1"),
    ]
    manifest = {"archive_sha256": archive_hash, "tool_version": "1.3.4", "assets": results}
    (target / "conversion-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(json.dumps([{k: v for k, v in r.items() if k != "animation_names"} for r in results], indent=2))


if __name__ == "__main__":
    main()
