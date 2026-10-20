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
| Windows build | Pending first standalone validation | Logs/unity-build.log |
| Standalone smoke | Pending | TestResults/standalone-smoke.json |

Unit checks cover buffered input consumption, expiry and clearing. Scene checks
exercise diagonal speed, one jump per ground opportunity, momentum reset, coyote
time, buffered landing, ceiling collision, pause/resume, Cinemachine obstruction
avoidance, and keyboard W/Shift binding through the command reader.

The keyboard test temporarily configures the headless editor to accept synthetic
keyboard events without a focused Game View and restores those settings afterward.
An initial run failed that test under the editor's normal focus filtering; all
eight scene tests passed after the harness correction.

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
production assets. No repository was published, pushed or uploaded.
