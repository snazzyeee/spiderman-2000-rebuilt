# Spider-Man 2000 PC revamp

Unity/C#/URP reimplementation using privately extracted original PC art.
The current private player opens with a textured main menu, original extracted
audio, settings, save/load and a narrated Mission 1 rooftop/combat slice.
Mechanical fidelity remains an unverified target.

## Build the menu and narrated Mission 1

[Docs/MAIN_MENU.md](Docs/MAIN_MENU.md) covers the complete build and Inspector
setup. [Docs/AUDIO_PIPELINE.md](Docs/AUDIO_PIPELINE.md) documents extraction,
manifest evidence and unresolved mappings. [Docs/DIALOGUE_ARCHITECTURE.md](Docs/DIALOGUE_ARCHITECTURE.md)
covers the runtime registry, mixer, subtitles and Timeline ownership.

Place data.pkr and media.pkr under LocalReference/PC/app. With Unity closed:

    .\Tools\Reference\setup_converter.ps1
    python Tools/Reference/prepare_phase1_assets.py
    python Tools/Reference/prepare_mission1_assets.py
    .\Tools\Build\build_frontend.ps1 -ExtractAudio -RunTests -SmokeTest

Python, FFmpeg/FFprobe and the licensed pinned Unity editor must be available.
Subsequent builds can omit -ExtractAudio. Open the private scene
Assets/_LocalReference/Frontend/MainMenu.unity, or launch
Builds/Frontend/SpiderManRevamp.exe with its accompanying data/runtime files.
All source scripts, tests and assembly recipes are tracked; the private assets
required to reproduce this player are excluded from Git.

The local audio run decoded 3,661 clips without reported failures: 2,663 SFX,
seven music streams and 991 dialogue/movie entries. Local faster-whisper processing
finished all 991 with zero failures: 918 contain draft recognized text and 73
are marked no_speech_detected. Filename rules identify 74 speakers; 917 remain
Unknown. Native bank/track tables resolve 29 recovered TRG playback commands to
28 clips; surrounding guard logic and complete verified semantic mappings remain
unresolved. See [Docs/BATCH_AUDIO.md](Docs/BATCH_AUDIO.md) for execution, cache
and evidence details. Menu sound roles and Stan_00's remake intro binding
are authored associations. The original recording plays; the visible mission
briefing is authored text, not a verified transcript.

The opening now follows IntroCinematic -> MissionBriefing -> GameplayActive.
The recording has exclusive dialogue ownership and a hidden objective HUD;
the separate briefing starts only after the intro completion event. See
[Docs/MISSION_LIFECYCLE.md](Docs/MISSION_LIFECYCLE.md).
[Docs/MENU_AND_PENDULUM.md](Docs/MENU_AND_PENDULUM.md) describes the enforced
five-item menu and fixed-length kinematic swing/collision rules.

See [Docs/FRONTEND_VALIDATION.md](Docs/FRONTEND_VALIDATION.md) for current test
results, rendered checks and remaining limits.

## Build and play Mission 1

[Docs/MISSION1.md](Docs/MISSION1.md) contains the scripts/component guide,
Inspector references, input bindings and exact state-machine flows.
[Docs/PHASE1.md](Docs/PHASE1.md) documents the model conversion, box-sweep motor,
orbital camera, URP materials/lighting and audio mixer.

Place your PC archive at LocalReference/PC/app/data.pkr. From this repository
root, with the Unity project closed:

    .\Tools\Reference\setup_converter.ps1
    python Tools/Reference/prepare_phase1_assets.py
    python Tools/Reference/prepare_mission1_assets.py
    .\Tools\Build\build_mission1.ps1 -RunTests -SmokeTest

The conversion verifies archive member CRCs/lengths and the pinned converter.
It never runs the original installer, game executable or compatibility DLLs.

Open Game in Unity Hub with editor 6000.6.6f1. Open the private scene
Assets/_LocalReference/Mission1/BankHeistRooftops.unity, or run the local build
Builds/Mission1/SpiderManMission1.exe with its accompanying data/runtime files.
The Rebuild > Mission 1 editor menu also assembles and builds the scene.

## Implemented systems

- PlayerStateMachine: pendulum swing, release momentum, geometry zip, surface
  latch/crawl, clearance-checked roof mantle, hit stun and control transitions.
- SpiderManCharacterController: swept box motion, sliding, steps, buffered jumps,
  coyote timing and recovery; no capsule or Unity CharacterController component.
- SpiderManOrbitCamera: mouse/right-stick orbit, damping, framing and Cinemachine
  third-person collision handling.
- PlayerCombat / Health: traced web shots, web energy, buffered three-hit melee,
  web dome, damage, invulnerability and defeat events.
- MissionManager / EncounterDirector: ordered rooftop checkpoints, living-enemy
  encounter gate, arena constraints, skylight completion, checkpoint recovery/replay.
- ThugAI / SurfaceSteeringMotor: patrol, alert, melee/ranged wind-ups, obstacle
  steering, hit reactions, web stun and full web capture.
- CutsceneSequence / DialogueCueTrack / DialogueTrigger: Timeline/Cinemachine
  intro, authored subtitles, optional synchronized audio, pause/skip and control locks.
- TraversalAudioRig: SFX/Music/Dialogue mixer buses, spatial web impacts,
  distance curves and altitude/velocity-driven low-pass filters.

Runtime: Game/Assets/_Game/Runtime. Editor assembly/build:
Game/Assets/_Game/Editor. Validation: [Docs/Mission1Validation.md](Docs/Mission1Validation.md).
Controls: [Docs/Controls.md](Docs/Controls.md).

## Asset facts and fidelity limits

The conversion preserves Spider-Man's 18 joints, eight textures and 300 numeric
animation slots, plus L1A1's 115 mesh objects and 98 textures. The mission also
uses original henchman, web-dome, level object and named combat SFX assets.
All visible production geometry comes from private original assets.

The rooftop route, bank encounter placement, entry role, animation bindings and
combat/traversal tuning are authored integration choices. The original mission
script, bank/skylight identity, animation semantics and gameplay timing have not
been verified. The frontend extends the original subtitle-only slice with a
32.35-second original Stan stream and an authored mission briefing. This is a
playable authored slice on original art, with animation polish, fidelity research
and performance work remaining. The menu uses the original character/albedo and
comic art with procedural modern detail; exact original layout/pose parity and
high-resolution repainting of every source texture are not claimed.

## Private assets and public source

Original/derived content stays ignored under LocalReference/ and
Game/Assets/_LocalReference/: models, textures, clips, avatars, SFX, scenes,
prefabs, Timeline assets and layouts. Builds, screenshots, test artifacts,
downloaded tools and Unity caches remain private too.

A source clone opens without the proprietary data. Rebuilding the playable
scene requires a local PC archive and the conversion steps above. Private
integration tests explicitly skip if those assets are missing.

Source checkpoints go to https://github.com/snazzyeee/spiderman-2000-revamp.
Do not commit proprietary derivatives or screenshots. See
[Docs/RepositoryWorkflow.md](Docs/RepositoryWorkflow.md).

## Pinned environment

Unity 6000.6.6f1 (d20c9ac777da), URP 17.6.0, Cinemachine 6.6.0,
Input System 1.20.0, Timeline 6.6.0 (editor-bundled), glTFast 6.20.0,
Test Framework 1.8.0, UGUI 2.6.0. Manifest, lockfile, project settings and
.meta files are tracked.

The older Unity 6.3 worker and automation remain removed. REA is optional for
future scoped behavioral research and is not required by this asset pipeline.
