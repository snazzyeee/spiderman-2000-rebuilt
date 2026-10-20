# Private audio extraction and evidence-backed mapping

Run from the repository root with Python 3.11+, FFmpeg and FFprobe on PATH.
The pinned converter is prepared by Tools/Reference/setup_converter.ps1.

    python Tools/Reference/extract_audio_manifest.py LocalReference/PC/app/data.pkr LocalReference/PC/app/media.pkr --output Game/Assets/_LocalReference/Audio --converter Tools/ThirdParty/neversoft-multitool-1.3.4/NeversoftMultitool-Windows-CLI-x64/NeversoftMultitool.exe
    python -m unittest discover -s Tools/Reference/tests -v

Inputs may also be loose directories, WAV/Bink/VAG streams or PSX files. Keep
input/output trees disjoint. The standalone script uses only Python's standard
library plus the two named conversion executables; it never executes game code.

## Outputs and guarantees

- Wav/: stable, uniquely named, nonempty 16-bit PCM WAVs, preserving source
  sample rate/channels. Every audio stream in each Bink carrier is decoded,
  including dialogue-only Bink videos and movie soundtracks.
- dialogue_manifest.json: stable clip IDs, source/output hashes, stream index,
  codec, duration, category, required dialogue fields and mapping provenance.
- extraction_evidence.json: audio strings, parsed TRG commands/node IDs/links,
  decode failures, unsupported banks and carriers with no audio stream.
- _work~/: private CRC-verified archive members, decoded banks, bounded carved
  embedded streams and TRG parser outputs. Unity ignores this working directory.

PKR paths, counts, lengths, compression and CRCs are checked before writing.
The converter executable must match the pinned SHA-256. FFmpeg receives fixed
argument arrays, local-file/pipe protocols and a timeout. Existing WAVs are
reused only when their recorded source and output hashes both match.

Self-describing VAGp/RIFF streams can be carved from PSX files. Headerless ADPCM
cannot be reconstructed without its sample rate/channel/interleave metadata;
the script does not assign arbitrary parameters to produce plausible noise.
KAT banks use the pinned decoder. Files with no embedded audio are inventoried,
not mislabeled as decoded clips. Failed/unsupported audio makes the command
exit nonzero after writing its evidence report.

## Mapping semantics

The PC package contains individually named data/Voice/*.bik streams. A filename
such as L1A1_00 identifies a level association. A stan_ prefix supplies a speaker
convention; that is recorded as filename evidence, not as a verified transcript.
Movie soundtracks can contain several speakers and remain Unknown unless mapped.

TRG PlayXA commands contain numeric operands. The inspected data does not expose
a complete audio-ID-to-subtitle/speaker table. A numeric command is retained
with its node, links and operands; it is never guessed into a spoken line or an
ON_LEVEL_START event. TextMessage commands are UI text and are not automatically
treated as adjacent audio transcripts. Direct filename references are attached
only when the matching audio identity is unambiguous.

Each required field always exists, but unresolved speaker/scene/trigger/subtitle
metadata is explicit. This is lossless extraction plus an auditable resolver,
not a claim that missing semantics can be generated exactly from filenames.

Use --mapping LocalReference/Dialogue/dialogue_overrides.json for private,
evidence-backed annotations. Its shape is:

    {"clips":[{"clip_id":"your.existing.clip.id","status":"verified",
      "speaker":"Speaker","scene_or_mission_id":"L1A1",
      "subtitle_text":"Transcribed and checked audio",
      "playback_trigger":"ON_LEVEL_START","evidence":"Reference and verification method"}]}

Use status authored for remake-specific bindings. Verified means the annotation
was actually checked, not that the pipeline inferred it. Multiple annotations
for a clip are retained in mappings. --require-complete-mapping exits nonzero if
any dialogue clip still lacks verified mapping. Source clips/transcripts and all
generated manifests remain ignored under the private asset/reference trees.

To apply annotations without repeating extraction/decoding:

    python Tools/Reference/extract_audio_manifest.py --apply-mapping-only --output Game/Assets/_LocalReference/Audio --mapping LocalReference/Dialogue/dialogue_overrides.json

Updating the same scene/trigger annotation is idempotent. Authored mappings do
not satisfy the verified-complete gate.

The local full run on 2026-10-20 produced 3,661 clips: 2,663 SFX, seven music
streams and 991 dialogue/movie entries, with zero reported decode failures or
unsupported inputs. All 991 dialogue entries still require complete verified
semantic annotation; successful decoding does not mean exact mapping is known.

Decoder reference: [Neversoft Multitool](https://github.com/slfx77/neversoft-multitool).
