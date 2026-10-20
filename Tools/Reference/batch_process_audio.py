"""Resumable local faster-whisper transcription and evidence-preserving TRG joins.

Only model weights are downloaded. Audio/transcripts are never uploaded.
ASR output is a draft, not a human-verified transcript or speaker diarization.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path
import re
import time


PREFIXES = {"na": "Stan Lee", "stan": "Stan Lee", "sp": "Spider-Man",
            "sm": "Spider-Man", "th": "Thug", "co": "Police"}


def digest(path: Path) -> str:
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    return value.hexdigest()


def write_json(path: Path, value: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    with temporary.open("w", encoding="utf-8") as stream:
        json.dump(value, stream, ensure_ascii=False, indent=2, allow_nan=False)
        stream.flush()
        os.fsync(stream.fileno())
    # Windows readers/antivirus may briefly open a file without delete sharing.
    for attempt in range(8):
        try:
            os.replace(temporary, path)
            break
        except PermissionError:
            if attempt == 7:
                raise
            time.sleep(min(.05 * 2 ** attempt, .5))


def safe_audio(root: Path, name: str) -> Path:
    path = (root / name).resolve()
    if not path.is_relative_to(root.resolve()) or not path.is_file():
        raise ValueError(f"Missing or unsafe audio member: {name}")
    return path


def annotate(entry: dict, result: dict, events: list[dict]) -> None:
    stem = Path(entry["source_path"]).stem
    prefix = stem.split("_")[0].lower()
    speaker = PREFIXES.get(prefix)
    if speaker and entry.get("mapping_status") != "verified":
        entry["speaker"] = speaker
        entry["speaker_evidence"] = f"Filename prefix {prefix}_; no speaker diarization"
    level = re.match(r"^(L\d+[AM]\d+[A-Z]?)(?:_|$)", stem, re.I)
    entry["level_id"] = level.group(1).upper() if level else entry.get("scene_or_mission_id", "")
    entry["level_id_status"] = "filename_prefix" if level else "existing_annotation" if entry["level_id"] else "unresolved"
    entry["transcript"] = result["transcript"]
    entry["transcript_status"] = "asr_draft" if result["transcript"] else "no_speech_detected"
    entry["asr"] = result
    # Numeric PlayXA operands do not establish which voice filename is played.
    related = [event for event in events if event.get("scene_or_mission_id", "").upper() == entry["level_id"]
               and event.get("command", {}).get("name") in ("PlayXA", "RunCinema")]
    entry["level_trigger_candidates"] = related
    direct = entry.get("trigger_references", [])
    verified = [mapping for mapping in entry.get("mappings", []) if mapping.get("status") == "verified"
                and mapping.get("playback_trigger")]
    entry["trigger_condition"] = verified[0]["playback_trigger"] if verified else (
        json.dumps(direct, sort_keys=True) if direct else None)
    entry["trigger_condition_status"] = "verified_annotation" if verified else "direct_reference" if direct else "unresolved_numeric_link"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=Path("Game/Assets/_LocalReference/Audio/dialogue_manifest.json"))
    parser.add_argument("--evidence", type=Path, default=Path("Game/Assets/_LocalReference/Audio/extraction_evidence.json"))
    parser.add_argument("--cache", type=Path, default=Path("LocalReference/Dialogue/ASR"))
    parser.add_argument("--model", default="small.en")
    parser.add_argument("--model-cache", type=Path, default=Path("Tools/ThirdParty/audio-models"))
    parser.add_argument("--threads", type=int, default=6)
    parser.add_argument("--beam-size", type=int, default=5)
    parser.add_argument("--limit", type=int, default=0, help="Smoke-test prefix; zero processes the entire corpus")
    parser.add_argument("--offline", action="store_true")
    args = parser.parse_args()
    if args.threads < 1 or args.beam_size < 1 or args.limit < 0:
        parser.error("threads/beam-size must be positive; limit must be nonnegative")
    from faster_whisper import WhisperModel
    from faster_whisper.audio import decode_audio
    from faster_whisper.utils import download_model
    document = json.loads(args.manifest.read_text(encoding="utf-8"))
    if document.get("schema_version") != 1:
        raise ValueError("Unsupported manifest schema")
    entries = [entry for entry in document["clips"] if entry.get("category") == "Dialogue"]
    identities = [entry["clip_id"] for entry in entries]
    if len(set(identities)) != len(entries):
        raise ValueError("Duplicate clip identities")
    if not entries:
        raise ValueError("No dialogue clips in input manifest")
    # Fail once on an incompatible decoder instead of recording 991 copies.
    decode_audio(str(safe_audio(args.manifest.parent, entries[0]["file_name"])), sampling_rate=16000)
    evidence = json.loads(args.evidence.read_text(encoding="utf-8"))
    args.cache.mkdir(parents=True, exist_ok=True)
    model_path = Path(args.model) if Path(args.model).is_dir() else Path(
        download_model(args.model, cache_dir=str(args.model_cache), local_files_only=args.offline))
    weights = model_path / "model.bin"
    signature = {"model": args.model, "weights_sha256": digest(weights), "language": "en",
                 "beam_size": args.beam_size, "vad_filter": True, "compute_type": "int8",
                 "faster_whisper": importlib.metadata.version("faster-whisper")}
    print(json.dumps({"event": "model_ready", "config": signature, "clips": len(entries)}), flush=True)
    model = WhisperModel(str(model_path), device="cpu", compute_type="int8", cpu_threads=args.threads, num_workers=1)
    selected = entries[:args.limit] if args.limit else entries
    report = {"total": len(entries), "selected": len(selected), "completed": 0, "reused": 0,
              "failures": [], "config": signature, "started_unix": time.time()}
    began = time.monotonic()
    for index, entry in enumerate(selected):
        try:
            path = safe_audio(args.manifest.parent, entry["file_name"])
            source_hash = digest(path)
            if source_hash != entry["wav_sha256"]:
                raise ValueError("WAV hash differs from extraction manifest")
            key = hashlib.sha256(entry["clip_id"].encode()).hexdigest()
            cache_path = args.cache / (key + ".json")
            try:
                cached = json.loads(cache_path.read_text(encoding="utf-8")) if cache_path.exists() else None
            except (OSError, ValueError):
                cached = None
            if cached and cached.get("wav_sha256") == source_hash and cached.get("config") == signature \
                    and isinstance(cached.get("segments"), list) and isinstance(cached.get("transcript"), str):
                result = cached; report["reused"] += 1
            else:
                segments, info = model.transcribe(str(path), language="en", task="transcribe",
                    beam_size=args.beam_size, condition_on_previous_text=False, vad_filter=True,
                    vad_parameters={"min_silence_duration_ms": 250}, word_timestamps=True)
                rows = [{"start": segment.start, "end": segment.end, "text": segment.text.strip(),
                         "avg_logprob": segment.avg_logprob, "no_speech_prob": segment.no_speech_prob,
                         "words": [{"start": word.start, "end": word.end, "word": word.word,
                                    "probability": word.probability} for word in segment.words or []]}
                        for segment in segments]
                result = {"clip_id": entry["clip_id"], "wav_sha256": source_hash, "config": signature,
                          "transcript": " ".join(row["text"] for row in rows), "segments": rows,
                          "duration": info.duration, "speech_duration": info.duration_after_vad}
                write_json(cache_path, result)
            annotate(entry, result, evidence.get("trigger_commands", []))
            report["completed"] += 1
        except Exception as error:
            report["failures"].append({"clip_id": entry["clip_id"], "error": str(error)})
        report["elapsed_seconds"] = round(time.monotonic() - began, 2)
        write_json(args.cache / "progress.json", report)
        if (index + 1) % 25 == 0 or index + 1 == len(selected):
            document["asr_processing"] = report
            write_json(args.manifest, document)
            print(json.dumps({"event": "progress", "completed": report["completed"],
                              "failed": len(report["failures"]), "elapsed": report["elapsed_seconds"]}), flush=True)
    report["finished_unix"] = time.time()
    report["no_speech"] = sum(not entry.get("transcript") for entry in selected if "asr" in entry)
    report["speaker_unknown"] = sum(entry.get("speaker", "Unknown") == "Unknown" for entry in selected)
    report["unresolved_triggers"] = sum(entry.get("trigger_condition") is None for entry in selected)
    document["asr_processing"] = report
    write_json(args.manifest, document)
    write_json(args.cache / "progress.json", report)
    return 1 if report["failures"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
