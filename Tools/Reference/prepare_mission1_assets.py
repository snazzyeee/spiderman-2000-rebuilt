"""Private original mission enemies, web effects, objects and identified SFX."""
import hashlib
import json
from pathlib import Path
import subprocess
import zlib
from pkr_index import inspect
from prepare_phase1_assets import ROOT, CONVERTER_SHA256, externalize_glb

def main():
    tool = ROOT / "Tools/ThirdParty/neversoft-multitool-1.3.4/NeversoftMultitool-Windows-CLI-x64/NeversoftMultitool.exe"
    if hashlib.sha256(tool.read_bytes()).hexdigest() != CONVERTER_SHA256: raise ValueError("Converter digest mismatch")
    archive = ROOT / "LocalReference/PC/app/data.pkr"
    staging = ROOT / "LocalReference/Mission1Conversion"
    raw, converted = staging / "Raw", staging / "Converted"
    raw.mkdir(parents=True, exist_ok=True); converted.mkdir(exist_ok=True)
    wanted = {
        "data/henchman.psx", "data/henchman.psh", "data/webdome2.psx",
        "data/scimpact.psx", "data/l1a1_o.psx",
        "data/audio/punchbig.wav", "data/audio/punchmedium.wav", "data/audio/webhit4.wav",
        "data/audio/webshot3.wav", "data/audio/webdome.wav", "data/audio/webloop1.wav"
    }
    found = {}
    with archive.open("rb") as stream:
        for entry in inspect(archive)["entries"]:
            key = entry["path"].replace("\\", "/").lower()
            if key not in wanted: continue
            stream.seek(entry["offset"]); payload = stream.read(entry["stored_bytes"])
            if entry["compressed"]:
                decoder = zlib.decompressobj()
                payload = decoder.decompress(payload, entry["size_bytes"] + 1)
                if not decoder.eof or decoder.unconsumed_tail: raise ValueError("Compressed member bounds")
            if len(payload) != entry["size_bytes"] or f"{zlib.crc32(payload):08x}" != entry["crc32"]:
                raise ValueError("CRC/length mismatch " + key)
            target = raw / Path(entry["path"]).name; target.write_bytes(payload); found[key] = target
    if wanted != set(found): raise ValueError("Missing members: " + str(wanted - set(found)))
    destination = ROOT / "Game/Assets/_LocalReference/Mission1"
    records = []
    for stem, animate in [("henchman", True), ("webdome2", False), ("scimpact", False), ("l1a1_o", False)]:
        source = found["data/" + stem + ".psx"]; output = converted / (stem + ".glb")
        command = [str(tool), "psx-anim-export", str(source), "--anim", "-1", "-o", str(output)] if animate else [
            str(tool), "psx-mesh", str(source), "-o", str(converted)]
        subprocess.run(command, check=True, cwd=ROOT)
        actual = next(p for p in converted.glob("*.glb") if p.stem.lower() == stem)
        records.append(externalize_glb(actual, destination / stem))
    audio = destination / "Audio"; audio.mkdir(exist_ok=True)
    for key, path in found.items():
        if key.endswith(".wav"): (audio / path.name).write_bytes(path.read_bytes())
    (destination / "conversion-manifest.json").write_text(json.dumps(records, indent=2))
    print(json.dumps([{k: v for k, v in r.items() if k != "animation_names"} for r in records], indent=2))

if __name__ == "__main__": main()
