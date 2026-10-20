# External references

The user supplied the [Transcripts Fanon Wiki transcript](https://transcripts-fanon.fandom.com/wiki/Spider-Man_%282000_Video_Game%29/Transcript),
inspected on 2026-10-20. It supplies cutscene order, scene descriptions and
candidate speaker labels across platform versions. It does not supply PC voice
filenames or TRG node/audio indices. It is a secondary community reference;
it does not verify all local ASR results or gameplay barks.

Use it to compare cutscene context against the extracted recordings. Do not
silently replace local ASR text with this page, mark draft transcripts verified,
or infer numeric playback bindings from the scene order. No bulk transcript
text from the page is copied into this repository or the generated manifest.

Native audio-ID associations are instead recovered from the inspected local
PC binary's bounded data table. Original speech remains local and ASR-generated
text remains explicitly marked as a draft. See BATCH_AUDIO.md.

The Godot migration's executable scraper/matcher is
Tools/pipeline/build_dialogue_manifest.py. The parsed reference contains 225
dialogue rows and supplies five accepted whole-clip candidates for the current
ASR corpus. It preserves the four user-supplied Level/Act display names as
user_supplied metadata. It does not assign cutscene headings to gameplay acts
or infer automatic trigger guards from numeric file order. See GODOT_MIGRATION.md.
