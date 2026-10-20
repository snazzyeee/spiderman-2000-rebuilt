# Read-only PC voice ID recovery

Run with the local PC executable present; it is opened only as bytes:

    python Tools/Reference/extract_native_voice_catalog.py

Output is private LocalReference/Dialogue/native_voice_catalog.json. The ASR
pipeline automatically reads it on its next run; cached transcripts are reused.
No disassembler or game executable is launched by the extraction script.

## Evidence and bounds

The inspected local binary fingerprint is
e4e221b168ccf52130c8c56a1535289afacc1906fb55d54545e33d50e43eec86.
Other binaries fail closed until their layout is independently inspected. This
fingerprint identifies the examined file; it is not a publisher signature.

Bounded static inspection established the first two playback arguments as bank
and track, flattened as bank * 16 + track. The native bank bound is 79. A PC
wrapper passes that index to a filename lookup over 1,264 32-bit pointers at
RVA 0x163fd8. The table contains 916 distinct filenames, including blank slots
and music. PE32 section bounds, pointer addresses, ASCII strings and filename
format are checked before producing the catalog.

Relevant inspection RVAs are recorded in the private output: bank/track entry
0x79ee0, index calculation 0x7a0a7, PC wrapper 0x116050 and lookup 0x104650.
Original executable instructions are not copied into the public repository.

All 29 PlayXA commands recovered by the current TRG parser resolve to 28 distinct
voice files. This includes cross-level reuse. Numeric suffixes are not a reliable
substitute: bank 1 / track 10 resolves through the table to L1A1_20.bik, rather
than a filename guessed from the number 10.

## Manifest meaning

Each resolved entry retains native_trigger_references with the exact source
file, level, node, bank, track, flat ID and binary fingerprint. trigger_condition
contains a concrete event reference such as TRG:L1A1:node45:PlayXA(1,10), with
native_indexed_event status. Multiple references and trigger levels are retained.

This establishes which file the indexed command requests. It does not establish
the complete guard logic leading to the node. trigger_semantics_status makes
that distinction explicit. Unsupported/unrecovered commands, unnamed conditions,
actor-script speech, unused files and cutscene soundtracks are not assigned
invented ON_COMBAT_START or ON_LEVEL_START semantics.

The catalog, original program, full tables and generated references remain
private. Public tests use synthetic PE/table fixtures and check malformed input,
uninspected fingerprints, bounds, bank/track indexing and out-of-range operands.
