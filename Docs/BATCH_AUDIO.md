# Local batch transcription and trigger evidence

The local pipeline uses faster-whisper 1.2.1 on CPU with int8 compute, small.en,
six threads, English transcription, beam size five, Silero VAD and word timing.
PyAV is pinned to 16.0.1 because version 19 removes the audio-open argument used
by faster-whisper 1.2.1. Decoder preflight runs before model loading or batch
processing. Atomic JSON writes retry temporary Windows reader/antivirus locks.
The RX 580 does not provide the NVIDIA CUDA backend used by faster-whisper.
No audio, transcripts or original game data are uploaded. Only public model
weights and Python packages are downloaded.

## Install and execute

From the repository root, after extraction in AUDIO_PIPELINE.md:

    .\Tools\Reference\setup_audio_asr.ps1
    & .\Tools\ThirdParty\audio-python\Scripts\python.exe Tools/Reference/batch_process_audio.py --model Tools/ThirdParty/audio-models/local-small.en --offline

The setup uses an isolated ignored venv, binary-only pinned requirements, and
resumable HTTPS downloads from the original Systran model repository at revision
d1d751a5f8271d482d14ca55d9e2deeebbae577f. It verifies each model file's size and
SHA-256. The model.bin is 483,545,366 bytes; its SHA-256 is
62b2a45b05ee59acb4a5341b33ee35e041395d378d418a18acfe4c9e768ee37a.
Pass -PythonPath for a different compatible 64-bit Python; this machine uses
Python 3.12.14. Full dependency pins are in requirements-audio.txt.

The Python script also accepts --model small.en for normal Hub downloading,
--threads, --beam-size, --manifest, --evidence, --cache and --model-cache.
--limit N is explicitly a partial smoke run. Zero/default processes every
Dialogue category entry, including movie soundtracks and silent logo carriers.

## Outputs, resuming and meaning

The existing private Audio/dialogue_manifest.json retains all extracted SFX,
music and dialogue entries and stable identities. Each processed dialogue entry
gains clip_id, speaker, level_id, transcript and trigger_condition, plus evidence
and status fields. Source paths, WAV hashes, mappings and verified subtitles
remain intact. Runtime DialogueEntry exposes these fields for queries.

LocalReference/Dialogue/ASR contains an atomic per-clip cache and progress.json.
Each cache is keyed by clip identity and validated against the actual WAV hash,
model weight hash, faster-whisper version and inference settings before reuse.
Malformed caches are regenerated. The manifest is checkpointed every 25 clips,
and all writes use replace-after-flush. Rerun the same command to resume. A
per-clip failure is recorded and causes a nonzero final exit status.

Each ASR result retains timestamped segments, average log probability,
no-speech probability and word probabilities. Silence/no speech produces an
empty transcript with no_speech_detected status. Recognized text is asr_draft;
ASR cannot establish exact wording or human verification. Drafts do not overwrite
subtitle_text or satisfy the existing verified-complete mapping gate.

Requested filename rules are case-insensitive: Na_/stan_ -> Stan Lee,
Sp_/SM_ -> Spider-Man, Th_ -> Thug, Co_ -> Police. Existing verified speaker
annotations take precedence. A level prefix such as L1A1_ supplies level_id,
not a character identity. Ambiguous speakers retain Unknown.

The join retains matching-level PlayXA/RunCinema commands as candidates.
trigger_condition is populated only from a verified annotation or a direct
filename reference in the extraction evidence. Same-level numeric operands
alone do not prove the clip or the condition that invokes it. The current TRG
parser recovered 384 named commands, including 29 PlayXA and 33 RunCinema
commands, but no nonempty node names. Inventing ON_LEVEL_START or ON_COMBAT_START
from those numbers would conceal missing evidence.

All clips, model/dependency binaries, caches, transcripts, expanded manifests and
reports remain ignored. Only code, tests, pins and setup documentation are pushed.

Implementation references: [faster-whisper](https://github.com/SYSTRAN/faster-whisper)
and [the pinned original model](https://huggingface.co/Systran/faster-whisper-small.en/tree/d1d751a5f8271d482d14ca55d9e2deeebbae577f).
