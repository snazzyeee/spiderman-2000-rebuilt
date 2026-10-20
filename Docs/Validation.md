# Traversal laboratory validation

Date: 2026-10-20. Platform: local Windows x64.

## Environment and reproducibility

- Editor: 6000.6.6f1, revision d20c9ac777da.
- Official installed Unity.exe publisher signature checked as valid.
- Editor batch creation, script compilation, and test runs resolved the license.
- URP 17.6.0; Input System 1.20.0; Cinemachine 6.6.0; Test Framework 1.8.0.
- Manifest versions agree with resolved direct dependencies in packages-lock.json.
- No original installer, game executable, or original DLL was launched.
- Original/derived game data remains outside production Assets in LocalReference/.

## Automated checks

| Suite | Result | Evidence |
|---|---|---|
| EditMode | 3/3 passed | TestResults/editmode.xml |
| PlayMode | 8/8 passed | TestResults/playmode.xml |
| Windows x64 development build | Succeeded; 173,326,607 bytes across the build folder | Logs/unity-build.log |
| Standalone smoke | 11 checks passed; zero recorded errors | TestResults/standalone-smoke.json |
| Rendered preview | Captured and visually inspected at 1280x720 | TestResults/traversal-lab.png |

Unit checks cover buffered input consumption, expiry and clearing. Scene checks
exercise diagonal speed, one jump per ground opportunity, momentum reset, coyote
time, buffered landing, ceiling collision, pause/resume, Cinemachine obstruction
avoidance, and keyboard W/Shift binding through the command reader.

The keyboard test temporarily configures the headless editor to accept synthetic
keyboard events without a focused Game View and restores those settings afterward.
An initial run failed that test under the editor's normal focus filtering; all
eight scene tests passed after the harness correction.

The standalone ran on AMD Radeon RX 580 2048SP using Unity 6000.6.6f1
at 2026-10-20 06:45 UTC. Its route checked grounded spawn, movement, jumping,
rejection of a second airborne jump, reset pose/velocity, pause/resume,
camera rotation, HUD creation, and preview output. The captured frame shows
the red capsule, roofs, obstacle shapes, route markers, goal, and readable HUD.

The first standalone route passed its gameplay checks but could not capture
ScreenCapture output in batch mode. The final run opened a rendered window
briefly, avoided cursor capture/focus pausing during automation, then closed
automatically. Normal interactive play retains focus-loss pausing.

The preview's HUD read 60 FPS / P95 16.7 ms after warm-up on this simple,
stationary view. This short development-build observation is not a benchmark
or a guarantee for the course, future assets, or the complete game.

## Actual first-milestone layout

The implementation uses Runtime/{Core,Input,Player,Camera,World,UI,Diagnostics},
an Editor assembly, and EditMode/PlayMode test assemblies under Assets/_Game/.
TraversalLab.unity is the only enabled build scene.

TraversalRig.prefab composes the player, input/session, camera, spawn, HUD, retry
and diagnostics together so their serialized references remain internal. This
replaces the plan's initial separate Player/Camera/HUD prefabs for this milestone.
World geometry and goal/kill triggers remain in the scene. Tuning and HUD panel
assets are in _Game/Settings; authored shared materials are in _Game/Materials.
The template's URP rendering assets remain in Assets/Settings.

The Create Missing Traversal Laboratory editor command preserves an existing
scene rather than overwriting scene edits. A Windows build can be regenerated
with Tools/Build/build_windows.ps1; -RunTests and -SmokeTest opt into validation.
Close this project in the editor before running the batch script.

## Limits

This milestone validates a simple capsule/box traversal environment. It does not
establish original-game parity, full-level performance, hitch-free behavior,
complete keyboard/mouse usability, or long-session reliability. The standalone
smoke route is short. Human control/play-feel evaluation remains necessary.

Wall crawling, web swinging, combat, animation, sound, original missions and
modern replacement art are not implemented. No original GLBs are imported into
production assets. The user authorized source pushes to
https://github.com/snazzyeee/spiderman-2000-revamp.git; the first authored-source
checkpoint is a0233d6 on main. Original content and local build/test output were
excluded. No binary release was published.
