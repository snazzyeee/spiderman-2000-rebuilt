# Unity dialogue registry and routing

Run SpiderManRebuild.Editor.DialogueAssetBuilder.BuildDatabase in batch mode,
or Rebuild > Audio > Import Manifest and Build Database in the editor, after
the extraction command in AUDIO_PIPELINE.md. The private generated assets are:

- Assets/_LocalReference/Audio/DialogueDatabase.asset
- Assets/_LocalReference/Audio/DialogueService.prefab

DialogueDatabase parses the JSON schema, validates unique identities and safe
audio paths, and indexes Unity-imported AudioClip bindings. The player build
contains those imported audio assets; no editor-only filesystem path or runtime
FFmpeg process is required. FindEvent excludes authored/unresolved mappings by
default. TryFindSource rejects ambiguous basenames.

DialogueAssetBuilder configures source-rate-preserving Vorbis imports: streaming
music, compressed dialogue, and decompressed short SFX. Source WAV files remain
16-bit PCM on disk. The private database binds every imported clip by stable ID.

DialogueManager owns the persistent audio service, a dedicated dialogue source,
looping music source and 16 reusable 2D/3D SFX voices. Assign the shared Traversal
AudioMixer and its Dialogue/SFX/Music groups. The mixer exposes MasterVolume,
DialogueVolume, SFXVolume and MusicVolume. SetVolume stores normalized preferences
and converts gain to decibels, including a -80 dB mute floor.

Play publishes speaker/subtitle callbacks, loads audio data before playback,
supports spatial speech positions and emits finished/failed callbacks. Spatial
sources use a custom distance curve with 1 m near and 35 m far attenuation.
Music ducks while speech owns the dialogue source. AudioListener.pause and
Timeline ownership prevent paused narration being mistaken for a finished clip.

DialogueCueClip optionally stores DatabaseClipId. DialoguePresenter resolves
that cue through DialogueManager and synchronizes the shared narration source
to Timeline time, while preserving its original camera-priority/control logic.
Skip, cue changes and destruction release only the presenter's narration owner.
Existing cues with direct AudioClip references still work.

When an original clip has no verified transcript, an authored mission instruction
can remain in the Timeline. It is labeled MISSION BRIEFING, not attributed as
the narrator's spoken words. Extracted narration and authored UI instructions
are separate data, even when they appear during the same cutscene.

Validation checkpoint: eight Python extraction tests and 29 Unity tests passed
on 2026-10-20 (eight EditMode, 21 PlayMode). Import registered all 3,661 clips.
This checks implementation/integration, not the unknown original trigger table
or transcription accuracy. Generated assets, source audio and manifests remain
private and excluded from Git.
