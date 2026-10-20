# Implementation plan after editor installation

Status: initial traversal laboratory implemented; validation results are recorded in Docs/Validation.md.
This document retains the broader proposed architecture; some future files are not yet needed.
Target editor: Unity 6000.6.6f1, installed and verified through Unity Hub.
Platform: Windows x64, keyboard/mouse first. Renderer: URP.
Reference machine: Ryzen 5 5500, RX 580 8 GB 2048SP, 16 GB RAM, NVMe.
Complete original PC game remains the destination. The immediate milestone is a
small playable traversal laboratory, followed by climbing/swinging and one level.

## First actions after installation

1. Locate the actual editor executable and confirm version. Check for an active
   Unity Personal license without reading/exporting account credentials.
2. Create a local-only Universal 3D project at `Game/`. Avoid automatic cloud
   project creation. Do not open, launch, or import the original EXE/DLLs.
3. Keep the template's compatible URP version. Add compatible Input System,
   Cinemachine, and Test Framework versions; inspect their resolved dependencies.
   Commit `ProjectVersion.txt`, `manifest.json`, and `packages-lock.json` once
   validated. Exact package versions are selected in this installed editor, not
   guessed from documentation for a different release.
4. Set Windows x64 as the first build target, configure input and visible `.meta`
   files, and make an empty standalone build before adding gameplay complexity.
5. Build the test scene with original placeholder geometry and a capsule player.
   Keep extracted GLBs separate until their scale, axes, and collision are checked.

If elevation, account sign-in, or license activation requires the user, stop the
dependent operation and state the required action. Automatic 6.3 installation and
its old follow-up were removed; never restart that obsolete workflow.

## Repository layout

Paths below are relative to the repository root. `Game/` is the Unity project;
the repository root is not itself a Unity project. The tree describes intended
files, most of which do not exist yet.

```text
spider-man-2000-rebuild/
  README.md
  .gitignore
  Docs/
    IMPLEMENTATION_PLAN.md
    Controls.md                         [during prototype implementation]
    Validation.md                       [results, routes, limits]
    ReferenceBehavior.md                [observations and deliberate changes]
  Game/
    Assets/
      _Game/
        Runtime/
          SpiderManRebuild.Runtime.asmdef
          Core/
            GameBootstrap.cs
            GameSession.cs
          Input/
            GameControls.inputactions
            PlayerInputReader.cs
            PlayerCommand.cs
          Player/
            PlayerController.cs
            CharacterMotor.cs
            LocomotionState.cs
            MovementTuning.cs
            JumpBuffer.cs
          Camera/
            ThirdPersonCameraRig.cs
            CameraTuning.cs
          World/
            SpawnPoint.cs
            RespawnController.cs
            KillVolume.cs
          UI/
            PrototypeHud.cs
          Diagnostics/
            FrameTimeRecorder.cs
        Editor/
          SpiderManRebuild.Editor.asmdef
          LabSceneBuilder.cs
          BuildWindows.cs
        Tests/
          EditMode/
            SpiderManRebuild.Tests.EditMode.asmdef
            JumpBufferTests.cs
            LocomotionRulesTests.cs
          PlayMode/
            SpiderManRebuild.Tests.PlayMode.asmdef
            PlayerMovementTests.cs
            RespawnAndPauseTests.cs
            CameraObstructionTests.cs
        Scenes/
          TraversalLab.unity
        Prefabs/
          Player.prefab
          CameraRig.prefab
          PrototypeHud.prefab
        Data/
          DefaultMovement.asset
          DefaultCamera.asset
        UI/
          PrototypeHud.uxml
          PrototypeHud.uss
        Art/
          Materials/                    [new placeholder materials]
        Settings/
          Rendering/                    [URP assets from template]
      _LocalReference/                  [optional later, ignored]
    Packages/
      manifest.json
      packages-lock.json
    ProjectSettings/
      ProjectVersion.txt
      ...                               [Unity-authored settings]
    Library/                            [generated, ignored]
    Temp/                               [generated, ignored]
    UserSettings/                       [local, ignored]
  Tools/
    Reference/
      pkr_index.py                      [exists]
      verify_extraction.py              [exists]
      export_reference.ps1              [conversion wrapper, later]
    Build/
      build_windows.ps1                 [after first build works]
    ThirdParty/                         [downloaded binaries ignored]
  LocalReference/                       [exists, ignored]
    PC/app/
    Extracted/data/
    Converted/
    Previews/
  Builds/Windows/                       [generated, ignored]
  TestResults/                          [generated, ignored]
  ArtSource/                            [new source art, later]
  Licenses/                             [dependency/asset notices as added]
  research_notes/                       [existing inventories and evidence]
  reports/                              [existing architecture blueprint]
```

Start with one runtime assembly, one editor assembly, and two test assemblies.
Folders are module boundaries; do not create a separate package/assembly for
every small class. Split runtime assemblies only when a real dependency or build
constraint warrants it. Keep editor APIs out of runtime code.

## Runtime modules and their contracts

| Module | Files | Ownership and output |
|---|---|---|
| Composition/session | `GameBootstrap`, `GameSession` | Wire scene references; own pause/restart session state. No global service locator or all-purpose manager. |
| Input | `GameControls`, `PlayerInputReader`, `PlayerCommand` | Resolve actions once, capture device-neutral move/look/button intent, clear stale input on disable/focus loss. Never move the player directly. |
| Player rules | `PlayerController`, `LocomotionState`, `JumpBuffer` | Accept commands, choose grounded/airborne behavior, implement jump buffering and coyote time, and request motor updates. |
| Movement | `CharacterMotor`, `MovementTuning` | Own velocity, collision, ground contacts and capsule displacement. Start with a kinematic CharacterController backend; establish a replaceable boundary before wall/ceiling work. |
| Camera | `ThirdPersonCameraRig`, `CameraTuning` | Update an orbit/follow target and use Cinemachine for the final camera. Configure obstruction handling and pitch limits. Avoid two scripts writing the camera transform. |
| World/retry | `SpawnPoint`, `RespawnController`, `KillVolume` | Define safe reset poses, reset velocity and input, and restore the test session after falling or pressing reset. |
| HUD | `PrototypeHud`, UXML/USS | Display controls, pause/reset state, movement state and a sampled performance readout. Observe gameplay instead of deciding it. |
| Diagnostics | `FrameTimeRecorder` | Maintain a fixed-size timing buffer, count hitches, and export route measurements. Avoid per-frame strings/logging on the movement path. |

Data flow:

```text
Keyboard/mouse -> PlayerInputReader -> PlayerCommand
                                       |
                                 PlayerController
                                       |
                              CharacterMotor + collision
                                       |
                              final pose and contacts
                                 /             \
                       camera follow target    HUD/diagnostics
```

**One motor owns normal player movement.** Respawn, future knockback, root motion,
and scripted moves go through its explicit methods. The camera never moves the
player. Animation and UI observe state. Combat damage will be gameplay-authoritative.

Choose and document one simulation update phase. Capture input consistently and
have camera follow consume the final player pose. Test at different frame rates;
a fixed timestep alone is not a determinism guarantee. Mouse delta and future
controller look require different sensitivity/time scaling.

Keep authored tuning in ScriptableObjects and mutable session state in runtime
instances. Settings/checkpoint files will use separate versioned records; never
save mutable progression by editing a shared tuning asset.

## Scene and controls

`TraversalLab.unity` is the first entry scene. It contains a composition root,
player/camera prefabs, ground, ramps, rooftop-sized platforms, gaps, low ceilings,
corners, a kill plane, spawn marker and HUD. `LabSceneBuilder` can regenerate its
owned placeholder scene with a clear overwrite check; it must not overwrite
user-created scenes. A separate bootstrap/menu scene is added when there are
multiple playable scenes, rather than requiring it for the first test.

| Control | Initial behavior |
|---|---|
| WASD | Camera-relative movement |
| Mouse | Orbit/look with pitch limits |
| Shift | Run |
| Space | Jump; tunable buffer and coyote time |
| R | Reset to safe test spawn |
| Escape | Pause and release cursor; explicit resume restores capture |

Focus loss should release the cursor and avoid stuck actions. Reset must restore
pose, grounded/contact state, vertical velocity, buffered jump, and camera framing.
Test diagonal movement, slopes, ceiling contact and variable frame rates before
adding traversal mechanics.

## Modules added after the first laboratory

Do not create empty stubs for the entire game on day one. Add these as milestones
require them, keeping the same runtime/presentation boundary.

| Milestone | New runtime files/modules | Readiness gate |
|---|---|---|
| Surface traversal | `Traversal/SurfaceProbe.cs`, `SurfaceFrame.cs`, `SurfaceTraversalController.cs`, `TraversalTuning.cs` | Wall entry/exit, wall-to-ceiling transitions, corners, recovery, camera comfort; replace motor backend if needed. |
| Swinging/web movement | `Traversal/SwingController.cs`, `SwingTuning.cs`, `Webs/WebVisual.cs` | Compare attach availability, arc, steering and release to PC reference; visual web does not own motion. |
| Combat/enemy loop | `Combat/CombatController.cs`, `AttackDefinition.cs`, `DamageReceiver.cs`, `AI/EnemyBrain.cs`, `EnemyMotor.cs`, `Presentation/PlayerAnimator.cs` | One enemy can attack, be hit/webbed and die; hits deduplicate, interruptions work, motor authority preserved. |
| First faithful level | `Missions/MissionDirector.cs`, `MissionDefinition.cs`, `ObjectiveState.cs`, `World/Checkpoint.cs`, `Persistence/SaveService.cs`, `SaveData.cs` | Layout/flow and progression specification, checkpoints, death/retry, completion and save restoration verified. |
| Modern visuals/UX | authored art/animation/audio, settings/remap UI, readability/accessibility options | Establish sustainable art quality and meet measured performance budgets. |
| Full game | per-level scenes/content definitions, boss extensions, complete coverage matrix | Each original level/mechanic/boss passes its own parity, progression and performance checks. |

Once combat is added, traversal, action and life/control state remain separate
axes with explicit interruption rules. Avoid one enum containing every possible
combination of swinging, attacking, climbing, stun and death.

## Asset and reference pipeline

The existing local reference includes a static skinned Spider-Man GLB and L1A1
render geometry. They are useful research exports, not proven animation, collision,
or complete mission implementations. The trigger parser's 292 nodes need semantic
validation. No character animation clips have been exported in the current GLB.

Keep all original archives, extracted members and converted proprietary data in
`LocalReference/`. If a reference copy is later imported into Unity, use the ignored
`Game/Assets/_LocalReference/` boundary and ignore its adjacent folder `.meta` too.
Record source file/hash, converter version/command, output hash, axis/scale choices,
and limitations. New production art lives in `_Game/Art` with source files in
`ArtSource/` and provenance/license records for third-party additions.

Use a compatible glTF importer only when importing the reference exports. It is
not required for the initial capsule-and-box laboratory. Do not bring original
EXEs, DLLs, installers, or disassemblers into the Unity project. REA remains an
optional offline research tool for specific unanswered behavior questions; it is
neither a runtime dependency nor a prerequisite to build the first prototype.

## Build, tests and practical acceptance gates

`BuildWindows.cs` owns the exact included scenes, Windows x64 target, build flags,
output path, and failure checks. Its command-line entry point should fail clearly
if compilation or BuildPipeline fails. `Tools/Build/build_windows.ps1` invokes the
pinned editor and preserves logs; it never downloads or executes original game code.

Validate in this order:

1. Empty URP scene compiles and builds locally with the pinned toolchain.
2. EditMode tests cover buffered-input expiry/consumption and locomotion transition
   rules. PlayMode scenarios cover jump/landing, obstacle/ceiling contact, safe
   reset, pause/focus transitions and camera obstruction. Use tolerances where
   simulation warrants them; do not assume bit-identical input replay.
3. Manually play the same route: spawn, walk, run, jump gaps, hit a ceiling, rotate
   the camera against an obstacle, fall, respawn, pause/resume and reset.
4. Repeat movement checks at low/normal/high frame rates. Confirm input and mouse
   look feel consistent and that transitions do not strand the player.
5. Profile a Windows player on the named PC; report CPU/GPU frame timing, GC,
   p95/p99/max frame times and hitch counts with settings/resolution recorded.
   1080p60 is a provisional target; no performance claim before measurement.

The first milestone is done when a reproducible Windows test build supports all
listed controls, collision/reset scenarios pass, no compile errors remain, and
known limitations are documented. It does not yet claim faithful swinging, combat,
complete levels, modern character art, or full-game performance.

## Source control and output boundaries

Track authored code, scenes, prefabs, tuning, rendering settings, project settings,
package manifests/locks and Unity `.meta` files. Add ignores before first import
for `Game/Library`, `Game/Temp`, `Game/Obj`, `Game/Logs`, `Game/UserSettings`, IDE
generated files, `Builds`, `TestResults`, and local proprietary reference copies.
Do not blanket-ignore JSON or all asset files. Add Git LFS when introducing large
new binary source art. Publishing/pushing and licensing the public project are
separate later steps; this plan does not authorize a public upload.

## Official package references

- [Unity Input Action assets](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/manual/ActionAssets.html)
- [Cinemachine](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html)
- [Unity package dependency locking](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-conflicts-auto.html)

The linked package manuals explain capabilities. Their URL versions are not a
promise of compatibility with the installed editor; resolve and pin that at setup.
