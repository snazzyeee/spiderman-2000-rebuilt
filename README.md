# Spider-Man 2000 PC revamp

Unity/C#/URP reimplementation using locally extracted original PC art.
The whitebox prototype is retired. The current Phase 1 scene uses the original
Spider-Man model and L1A1 meshes, with a custom box-sweep player motor.
Mechanical fidelity is a target, not an achieved or verified claim.

## Phase 1 files and exact commands

[Docs/PHASE1.md](Docs/PHASE1.md) contains the complete extraction, import,
controller installation, camera configuration, animation binding and audio APIs.

From this repository root, with the Unity project closed:

    .\Tools\Reference\setup_converter.ps1
    python Tools/Reference/prepare_phase1_assets.py
    .\Tools\Build\build_windows.ps1 -RunTests -SmokeTest

Place your PC archive at LocalReference/PC/app/data.pkr first. These commands
never execute the game's installer, executable or compatibility DLLs.

Open Game in Unity Hub with editor 6000.6.6f1. The private scene is
Assets/_LocalReference/Phase1/SpiderManPhase1.unity. The local build is
Builds/Windows/SpiderManPhase1.exe; retain its accompanying data/runtime files.

## Concrete deliverables

- Tools/Reference/prepare_phase1_assets.py: exact PKR-member selection, CRC/length
  verification, animated PSX/PSH conversion, glTF/BIN plus unchanged PNG export.
- SpiderManCharacterController.cs: kinematic box sweeps, sliding, ground/step
  handling, jump buffering, coyote timing and reset. No capsule-based component.
- SpiderManOrbitCamera.cs: separately scaled mouse and right-stick look,
  smoothed yaw/pitch, framed tracking target and Cinemachine collision handling.
- SpiderManPlayerInstaller.cs: drop onto an identity-transform root, assign the
  imported visual child and movement tuning, then press Play.
- Phase1SceneBuilder.cs: private original-asset scene assembly, URP Lit material
  remapping, Generic avatar, SSAO, soft shadows and exponential-squared fog.
- TraversalAudioRig.cs: separate SFX/Music/Dialogue buses, 3D impact emitter pool,
  custom distance attenuation and altitude/velocity-driven low-pass filters.

Runtime source is under Game/Assets/_Game/Runtime; editor assembly/build tools
are under Game/Assets/_Game/Editor. See [Docs/Validation.md](Docs/Validation.md).

## Current asset facts and remaining fidelity work

The local conversion preserves 18 character joints, eight character textures,
300 original animation slots, and L1A1's 115 mesh objects and 98 textures.
The Generic avatar and imported textured materials are checked in Unity.

Animation slot meanings and gameplay event timing are not yet established.
Assign identified native clips to SpiderManAnimationDriver; no guessed idle/run
mapping is shipped. Wall crawling, web swinging, combat, mission scripts and
original audio cue identification remain unimplemented. Current movement tuning,
1.8 m normalization, box shape, roof spawn and mesh-derived world collision are
integration choices, not verified original mechanics.

## Private assets and public source

All original and derived content is ignored under LocalReference/ and
Game/Assets/_LocalReference/. This includes converted meshes, textures, clips,
avatars and private scenes. Builds, screenshots, tests, caches, downloaded tools,
reference indexes and conversion logs are ignored as well.

A source clone opens without private assets. Rebuilding the playable scene
requires a local PC archive and the conversion command. Do not commit or upload
proprietary derivatives or screenshots. GitHub source checkpoints go to
https://github.com/snazzyeee/spiderman-2000-revamp.

The PKR tools and conversion workflow read data only. Antivirus no-detection
results do not establish the original package is safe to execute.

## Pinned environment

Unity 6000.6.6f1 (d20c9ac777da), URP 17.6.0, Cinemachine 6.6.0,
Input System 1.20.0, glTFast 6.20.0, Test Framework 1.8.0, UGUI 2.6.0.
Manifest, lockfile, project settings and authored .meta files are tracked.

The old 6.3 download/install worker and automation remain removed. No installer
or game launch is needed for Phase 1. REA remains optional for later, narrowly
scoped behavioral research; it is not required by this asset pipeline.
