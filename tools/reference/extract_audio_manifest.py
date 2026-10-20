"""Extract PC PKR3, Bink, WAV, VAG and KAT audio without running game code.

Python 3.11+, FFmpeg/FFprobe; KAT/TRG conversion uses the pinned Multitool.
All generated content belongs in a private directory. Unknown semantics remain
unknown: an audio filename or numeric PlayXA argument is not a transcript.
"""
from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed
from collections import Counter
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import wave
import zlib

from pinned_tools import CONVERTER_SHA256
AUDIO = {".wav", ".bik", ".vag", ".pss", ".adx", ".xa", ".vab", ".kat"}
METADATA = {".trg", ".sfx", ".dat", ".txt", ".seq", ".exe"}
MAX_MEMBER = 256 * 1024 * 1024


def digest(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def atomic_json(path, document):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(document, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    temporary.replace(path)


def safe_member(value):
    value = value.replace("\\", "/")
    path = PurePosixPath(value)
    if not value or path.is_absolute() or ".." in path.parts or ":" in value or "\0" in value:
        raise ValueError("Unsafe member path: " + repr(value))
    if any(part.rstrip(". ") != part or part.upper().split(".")[0] in
           {"CON", "PRN", "AUX", "NUL", *("COM" + str(i) for i in range(10)),
            *("LPT" + str(i) for i in range(10))} for part in path.parts):
        raise ValueError("Invalid Windows member path: " + value)
    return path


def pkr_entries(path):
    size = path.stat().st_size
    with path.open("rb") as stream:
        header = stream.read(8)
        if len(header) != 8:
            raise ValueError("Truncated PKR header")
        magic, offset = struct.unpack("<4sI", header)
        if magic != b"PKR3" or not 8 <= offset <= size - 12:
            raise ValueError("Invalid PKR3 directory")
        stream.seek(offset)
        _, directories, files = struct.unpack("<III", stream.read(12))
        if directories > 100000 or files > 1000000 or offset + 12 + directories * 40 + files * 52 > size:
            raise ValueError("PKR table exceeds bounds")
        directory_rows = [struct.unpack("<32sII", stream.read(40)) for _ in range(directories)]
        if sum(row[2] for row in directory_rows) != files:
            raise ValueError("PKR table counts disagree")
        entries, seen = [], set()
        for directory, _, count in directory_rows:
            for _ in range(count):
                name, crc, flag, start, unpacked, packed = struct.unpack("<32sIIIII", stream.read(52))
                if b"\0" not in directory or b"\0" not in name:
                    raise ValueError("Unterminated PKR name")
                member = safe_member(directory.split(b"\0")[0].decode("ascii") + "/" + name.split(b"\0")[0].decode("ascii"))
                stored = packed if flag == 2 else unpacked
                if flag not in (2, 0xFFFFFFFE) or start < 8 or start + stored > offset or unpacked > MAX_MEMBER:
                    raise ValueError("PKR member bounds: " + str(member))
                if str(member).casefold() in seen:
                    raise ValueError("Duplicate PKR member")
                seen.add(str(member).casefold())
                entries.append((member, crc, flag == 2, start, unpacked, stored))
    return entries


def unpack_archive(path, destination):
    records = []
    with path.open("rb") as stream:
        for member, crc, compressed, offset, length, stored in pkr_entries(path):
            if member.suffix.lower() not in AUDIO | METADATA | {".psx"}:
                continue
            stream.seek(offset)
            data = stream.read(stored)
            if compressed:
                decoder = zlib.decompressobj()
                data = decoder.decompress(data, length + 1)
                if not decoder.eof or decoder.unconsumed_tail or decoder.unused_data:
                    raise ValueError("Invalid compressed member " + str(member))
            if len(data) != length or zlib.crc32(data) != crc:
                raise ValueError("PKR CRC/length mismatch " + str(member))
            target = destination.joinpath(*member.parts).resolve()
            if not target.is_relative_to(destination.resolve()):
                raise ValueError("Archive output escapes destination")
            target.parent.mkdir(parents=True, exist_ok=True)
            if not target.exists() or digest(target) != hashlib.sha256(data).hexdigest():
                target.write_bytes(data)
            records.append((target, path.stem.lower() + "/" + str(member)))
    return records


def carve_psx(path, destination):
    """Carve only self-describing, bounds-checked VAGp/RIFF streams."""
    data = path.read_bytes()
    result = []
    for match in re.finditer(b"VAGp|RIFF", data):
        offset = match.start()
        if match.group() == b"VAGp" and offset + 48 <= len(data):
            length, rate = struct.unpack_from(">II", data, offset + 12)
            total, extension = 48 + length, ".vag"
            if not 4000 <= rate <= 192000 or not length or length % 16:
                continue
        elif offset + 12 <= len(data) and data[offset + 8:offset + 12] == b"WAVE":
            total, extension = struct.unpack_from("<I", data, offset + 4)[0] + 8, ".wav"
        else:
            continue
        if total < 44 or total > MAX_MEMBER or offset + total > len(data):
            continue
        destination.mkdir(parents=True, exist_ok=True)
        output = destination / (path.stem + "_" + format(offset, "08x") + extension)
        output.write_bytes(data[offset:offset + total])
        result.append((output, offset))
    return result


def run(command, timeout=120):
    return subprocess.run([str(x) for x in command], capture_output=True, text=True,
                          encoding="utf-8", errors="replace", timeout=timeout, check=False)


def pcm_info(path):
    with wave.open(str(path), "rb") as source:
        if source.getsampwidth() != 2 or source.getcomptype() != "NONE" or source.getnframes() == 0:
            raise ValueError("Expected nonempty 16-bit PCM WAV")
        return dict(sample_rate=source.getframerate(), channels=source.getnchannels(),
                    duration_seconds=source.getnframes() / source.getframerate())


def clip_id(source):
    # The archive namespace prevents duplicate raw/voice/movie basenames colliding.
    return re.sub(r"[^a-z0-9]+", ".", str(PurePosixPath(source).with_suffix("")).lower()).strip(".")


def describe(source):
    stem = PurePosixPath(source).stem.lower()
    scene = re.match(r"(l[0-9a-h]a[0-9]+)", stem, re.I)
    voice = "/voice/" in source.lower()
    kind = "Music" if stem.startswith("song_") else "Dialogue" if voice or "/movie/" in source.lower() else "SFX"
    speaker = "Stan Lee" if stem.startswith("stan_") else "Spider-Man" if stem.startswith(("spid_", "spi_", "sp_alt")) else "Unknown"
    return dict(speaker=speaker, scene_or_mission_id=scene.group(1).upper() if scene else "",
                subtitle_text="", playback_trigger="", category=kind,
                mapping_status="unresolved", speaker_evidence="filename convention" if speaker != "Unknown" else "",
                scene_evidence="filename prefix" if scene else "", subtitle_status="unresolved",
                mappings=[], trigger_references=[])


def decode_one(record, output, ffmpeg, ffprobe, cache):
    path, source = record
    probe = run([ffprobe, "-v", "error", "-show_streams", "-of", "json", path])
    if probe.returncode:
        raise ValueError(source + ": probe failed: " + probe.stderr[-500:])
    streams = [s for s in json.loads(probe.stdout).get("streams", []) if s.get("codec_type") == "audio"]
    result = []
    source_hash = digest(path)
    for stream in streams:
        identifier = clip_id(source) + (".track" + str(stream["index"]) if len(streams) > 1 else "")
        file_name = identifier.replace(".", "_")[:120] + "_" + hashlib.sha256(identifier.encode()).hexdigest()[:10] + ".wav"
        target = output / "Wav" / file_name
        previous = cache.get(identifier)
        valid = False
        if previous and previous.get("source_sha256") == source_hash and target.exists():
            valid = digest(target) == previous.get("wav_sha256")
        if not valid:
            temporary = target.with_suffix(".tmp.wav")
            command = [ffmpeg, "-v", "error", "-nostdin", "-y", "-protocol_whitelist", "file,pipe",
                       "-i", path, "-map", "0:" + str(stream["index"]), "-vn", "-c:a", "pcm_s16le", temporary]
            decoded = run(command)
            if decoded.returncode:
                temporary.unlink(missing_ok=True)
                raise ValueError(source + ": decode failed: " + decoded.stderr[-500:])
            pcm_info(temporary)
            temporary.replace(target)
        result.append(dict(clip_id=identifier, file_name="Wav/" + file_name, source_path=source,
                           source_sha256=source_hash, wav_sha256=digest(target), stream_index=stream["index"],
                           source_codec=stream.get("codec_name", ""), **pcm_info(target), **describe(source)))
    return result


def metadata_report(records, converter, work):
    strings, triggers, diagnostics = [], [], []
    trg_records = [(p, name) for p, name in records if p.suffix.lower() == ".trg"]
    if converter and trg_records:
        trg_input = work / "trg_input"
        trg_output = work / "trg_json"
        trg_input.mkdir(exist_ok=True)
        for path, source in trg_records:
            shutil.copyfile(path, trg_input / (hashlib.sha256(source.encode()).hexdigest()[:10] + "_" + path.name))
        parsed = run([converter, "trg", trg_input, "-o", trg_output], timeout=180)
        if parsed.returncode:
            diagnostics.append("TRG parser: " + parsed.stderr[-500:])
        for path in sorted(trg_output.rglob("*.json")):
            try:
                document = json.loads(path.read_text(encoding="utf-8-sig"))
                for node in document.get("nodes", []):
                    commands = node.get("commands", [])
                    for command in commands:
                        name = command.get("name", "")
                        if any(k in name.lower() for k in ("playxa", "sound", "voice", "cinema", "textmessage")):
                            triggers.append(dict(file=path.name, scene_or_mission_id=re.sub(r"_t.*$", "", path.stem[11:], flags=re.I).upper(),
                                                 node_id=node["index"], node_name=node.get("name", ""),
                                                 links=node.get("links", []), command=command))
            except (ValueError, KeyError) as error:
                diagnostics.append(str(path.name) + ": " + str(error))
    elif trg_records:
        diagnostics.append("TRG files found; pass the pinned --converter to parse their commands")
    for path, source in records:
        if path.suffix.lower() not in METADATA:
            continue
        data = path.read_bytes()
        matches = [(m.start(), m.group().decode("ascii")) for m in re.finditer(rb"[ -~]{5,300}", data)]
        relevant = [(offset, value) for offset, value in matches if re.search(r"\.(?:bik|wav|vag|pss)\b", value, re.I)]
        if relevant:
            strings.append(dict(source=source, audio_strings=[dict(offset=o, value=v) for o, v in relevant]))
    return dict(audio_string_tables=strings, trigger_commands=triggers, diagnostics=diagnostics)


def apply_mappings(clips, evidence, mappings):
    by_name = {}
    for clip in clips:
        by_name.setdefault(PurePosixPath(clip["source_path"]).name.lower(), []).append(clip)
    for event in evidence["trigger_commands"]:
        for value in event["command"].get("args", []):
            if not isinstance(value, str):
                continue
            matches = by_name.get(PurePosixPath(value.replace("\\", "/")).name.lower(), [])
            if len(matches) == 1:
                matches[0]["trigger_references"].append(event)
    by_id = {clip["clip_id"]: clip for clip in clips}
    for item in mappings.get("clips", []):
        identifier = item["clip_id"]
        if identifier not in by_id:
            raise ValueError("Mapping names missing clip: " + identifier)
        if not item.get("evidence") or item.get("status") not in ("verified", "authored"):
            raise ValueError("Mappings require evidence and status verified/authored")
        clip = by_id[identifier]
        for field in ("speaker", "scene_or_mission_id", "subtitle_text", "playback_trigger"):
            if field in item:
                clip[field] = item[field]
        clip["mapping_status"] = item["status"]
        clip["mappings"] = [old for old in clip["mappings"] if
                            (old.get("scene_or_mission_id"), old.get("playback_trigger")) !=
                            (item.get("scene_or_mission_id"), item.get("playback_trigger"))]
        clip["mappings"].append(item)
        if item.get("subtitle_text"):
            clip["subtitle_status"] = item["status"]
    return clips


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("inputs", nargs="*", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--ffmpeg", default=shutil.which("ffmpeg"))
    parser.add_argument("--ffprobe", default=shutil.which("ffprobe"))
    parser.add_argument("--converter", type=Path)
    parser.add_argument("--mapping", type=Path, help="Private evidence-backed metadata overrides")
    parser.add_argument("--apply-mapping-only", action="store_true", help="Update an existing manifest without re-decoding")
    parser.add_argument("--workers", type=int, default=2)
    parser.add_argument("--require-complete-mapping", action="store_true")
    args = parser.parse_args(argv)
    if args.apply_mapping_only:
        if not args.mapping:
            parser.error("--apply-mapping-only requires --mapping")
        manifest_path = args.output.resolve() / "dialogue_manifest.json"
        document = json.loads(manifest_path.read_text(encoding="utf-8"))
        overrides = json.loads(args.mapping.read_text(encoding="utf-8"))
        apply_mappings(document["clips"], {"trigger_commands": []}, overrides)
        document["statistics"]["unresolved_dialogue"] = sum(
            c["category"] == "Dialogue" and c["mapping_status"] != "verified" for c in document["clips"])
        atomic_json(manifest_path, document)
        print(json.dumps(document["statistics"], indent=2))
        return 1 if args.require_complete_mapping and document["statistics"]["unresolved_dialogue"] else 0
    if not args.inputs:
        parser.error("Supply inputs, or use --apply-mapping-only")
    if not args.ffmpeg or not args.ffprobe:
        parser.error("FFmpeg and FFprobe are required; pass their paths or add them to PATH")
    if not 1 <= args.workers <= 8:
        parser.error("workers must be between 1 and 8")
    converter = args.converter.resolve() if args.converter else None
    if converter and digest(converter) != CONVERTER_SHA256:
        raise ValueError("Multitool executable digest does not match pinned 1.3.4")
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    work = output / "_work~"  # Unity ignores folders ending with ~; raw carriers are never imported.
    work.mkdir(exist_ok=True)
    (output / "Wav").mkdir(exist_ok=True)
    records, archives = [], []
    for input_path in args.inputs:
        input_path = input_path.resolve(strict=True)
        if input_path == output or output in input_path.parents or input_path in output.parents:
            raise ValueError("Keep input and output trees disjoint")
        paths = sorted(p for p in input_path.rglob("*") if p.is_file()) if input_path.is_dir() else [input_path]
        for path in paths:
            if path.suffix.lower() == ".pkr":
                records += unpack_archive(path, work / path.stem)
                archives.append(dict(name=path.name, sha256=digest(path)))
            elif path.suffix.lower() in AUDIO | METADATA | {".psx"}:
                records.append((path, (input_path.name + "/" + str(path.relative_to(input_path)) if input_path.is_dir() else path.name).replace("\\", "/")))
    # Refuse overlapping input directories/archives producing ambiguous identities.
    identities = [name.casefold() for _, name in records]
    if len(identities) != len(set(identities)):
        raise ValueError("Inputs produce duplicate source paths")
    evidence = metadata_report(records, converter, work)
    audio_records, unsupported, failures = [], [], []
    for path, source in records:
        extension = path.suffix.lower()
        if extension == ".psx":
            carved = carve_psx(path, work / "carved" / hashlib.sha256(source.encode()).hexdigest()[:12])
            audio_records += [(p, source + "/embedded_" + format(offset, "08x") + p.suffix) for p, offset in carved]
        elif extension == ".kat":
            if not converter:
                unsupported.append(dict(source=source, reason="KAT needs --converter"))
                continue
            bank_dir = work / "banks" / hashlib.sha256(source.encode()).hexdigest()[:12]
            bank_dir.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, bank_dir / path.name)
            bank_output = bank_dir / "Decoded"
            converted = run([converter, "audio", bank_dir, "-o", bank_output])
            wavs = sorted(bank_output.rglob("*.wav"))
            if converted.returncode or not wavs:
                failures.append(dict(source=source, error="KAT conversion failed: " + converted.stderr[-500:]))
            else:
                audio_records += [(wav, source + "/" + wav.name) for wav in wavs]
        elif extension in AUDIO:
            audio_records.append((path, source))
    manifest_path = output / "dialogue_manifest.json"
    previous = json.loads(manifest_path.read_text(encoding="utf-8")) if manifest_path.exists() else {}
    cache = {c["clip_id"]: c for c in previous.get("clips", [])}
    clips, silent = [], []
    with ThreadPoolExecutor(max_workers=args.workers) as pool:
        futures = {pool.submit(decode_one, record, output, args.ffmpeg, args.ffprobe, cache): record for record in audio_records}
        for completed, future in enumerate(as_completed(futures), 1):
            _, source = futures[future]
            try:
                decoded = future.result()
                clips += decoded
                if not decoded:
                    silent.append(source)
            except (ValueError, OSError, subprocess.TimeoutExpired) as error:
                failures.append(dict(source=source, error=str(error)))
            if completed % 200 == 0:
                print(f"Decoded {completed}/{len(futures)} inputs", flush=True)
    clips.sort(key=lambda c: c["clip_id"])
    identifiers = [c["clip_id"] for c in clips]
    if len(identifiers) != len(set(identifiers)):
        raise ValueError("Clip identity collision")
    mapping = json.loads(args.mapping.read_text(encoding="utf-8")) if args.mapping else {}
    apply_mappings(clips, evidence, mapping)
    statistics = dict(clips=len(clips), categories=dict(Counter(c["category"] for c in clips)),
                      unresolved_dialogue=sum(c["category"] == "Dialogue" and c["mapping_status"] != "verified" for c in clips),
                      failures=len(failures), unsupported=len(unsupported), no_audio_stream=len(silent))
    atomic_json(manifest_path, dict(schema_version=1, archives=archives, statistics=statistics, clips=clips))
    atomic_json(output / "extraction_evidence.json", dict(**evidence, failures=failures, unsupported=unsupported, no_audio_stream=silent))
    print(json.dumps(statistics, indent=2))
    return 1 if failures or unsupported or (args.require_complete_mapping and statistics["unresolved_dialogue"]) else 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, OSError, subprocess.TimeoutExpired) as error:
        print("Audio extraction failed: " + str(error), file=sys.stderr)
        raise SystemExit(1)
