# Original audio identity bindings

## XA identity

Evidence `exe:0x563fd8` via the existing fingerprint-gated voice catalog tool,
confidence recovered: the canonical build has 79 banks of 16 track slots. A
slot's flat identity is bank*16+track and points to a Bink source filename.
The private catalog records the canonical executable SHA-256. A filename may
occupy several slots; preserve every identity, including shared filler slots.
No original program is executed. Join catalog source basename to the extraction
manifest source path, then require the registry/extraction WAV SHA-256 to agree.
An exact source identity and content hash are mandatory; fuzzy transcript or
filename similarity never establishes an audio binding.

## SFX record and bank layout

Evidence `decomp:ps2lowsfx.cpp#SFX_ParseSFXFile@f8a0392 (@Ok, Matching, Validated)`,
confidence recovered. A .sfx file is at most 2048 bytes. Records are 16 bytes;
a u32 all-ones terminator occurs at a record boundary. Remaining padding in the
observed padded file is zero. Each record contains four little-endian words:
control, pitch/volume/falloff parameters, alias, reserved. The parser consumes
the first three and skips the last. The control word's second byte selects a KAT
asset index (the eventual playback lookup masks it to seven bits); its low byte
marks streaming when 254. Other parameter bits are retained as evidence and are
not interpreted as filenames. There are at most 64 records in the runtime arrays.
Our decoder rejects malformed extents, unsupported flags, missing terminators,
nonzero reserved/padding and duplicate level aliases rather than guessing.

Evidence `decomp:ps2lowsfx.cpp#SFX_Play/SFX_PlayPos/translateLevelSpecificAliasToIndex/playSFX@f8a0392
(@Ok, some AlmostMatching)`, confidence recovered: global requests use one-based
record order; zero is no sound and valid global indices are below 64. Request bit
15 selects a level alias lookup, bit 14 is a playback option, and those two bits
are removed before alias lookup. Level aliases depend on the active level bank;
the same alias in different banks is not one identity. The asset index selects
the corresponding KAT table entry. Global initialization selects the spidey bank
(`decomp:init.cpp#SFX_Init call@f8a0392`); level bank initialization uses a separate
bank and marks its parsed records as level-specific.

Evidence `decomp:ps2lowsfx.cpp#SFX_LoadBank@f8a0392 (@Ok, AlmostMatching)` and
`multitool:3c9028e#KatExtractor.ReadEntry/ExtractToWavFromStream` (MIT), confidence
verified for the metadata structure and naming: KAT begins with u32 asset count,
followed by 44-byte entries. Channel count, byte offset, byte length, sample rate,
loop flag and bit depth occupy the first six u32 fields. Remaining bytes retain
unknown metadata. Bounds must stay inside the file. Supported original depths
are 4/8/16 bits; the converter's bulk exporter names sample index N as N padded
to three digits followed by .wav. This is a decoded index mapping, not a guessed
sound name. Source bank path + sample index + verified extraction WAV hash yields
the registry entry. Empty/missing extracted samples remain unresolved.

## Remake integration

Confidence authored (`remake:audio-identity-registry`): registry entries carry
native_bank/native_track/native_flat_id and all native_xa_identities when a
filename has multiple catalog slots. SFX entries carry native_sfx_bank,
native_sfx_id and all native_sfx_identities. Primary fields describe the first
exact identity; lookup checks the complete lists. Level alias requests require
an explicit or active bank; ambiguous/no-bank lookups return false. Global IDs
ignore level selection and use the global bank. The existing request signals
remain observable. Missing entries or missing private registries return false
without loading a guessed resource. Gameplay cinematic ownership still applies.

Synthetic tests exercise source/hash joins, shared identities, bank separation,
malformed binary bounds and unresolved lookup behavior. Private smoke additionally
checks a recovered XA request and recovered global SFX requests with real assets.

## Current audit

Evidence `remake:audio-identity-join-v1`, confidence verified for exact joins:
all 63 local SFX files parse; 57 have fully validated KAT references, five lack a
matching local KAT and one has an out-of-range sample reference. That entire bank
is excluded from binding and recorded as rejected; no bounds were widened.
The private catalog produces 1,993 exact SFX registry joins. The XA catalog yields
901 exact registry entries; shared catalog slots remain attached to their one
source entry. Unmatched catalog files and source/hash ambiguities remain unresolved.
Global requests 9 and 30 select KAT indices 8 and 29 by the decoded one-based
record rule. Their real playback and a bank-33 request pass private smoke checks.
The registry population and original transcript status counts are unchanged.
