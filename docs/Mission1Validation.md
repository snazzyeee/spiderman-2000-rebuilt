# Mission 1 validation — 2026-10-20

This records the earlier mission-only baseline. Current menu/audio/save-load
validation is in [FRONTEND_VALIDATION.md](FRONTEND_VALIDATION.md).

## Environment and reproduction

Unity 6000.6.6f1 (d20c9ac777da), Windows x64 development build with Mono,
URP 17.6.0, Input System 1.20.0, Cinemachine 6.6.0 and bundled Timeline 6.6.0.
Rendered standalone verification ran on AMD Radeon RX 580 2048SP.

From the repository root, after both private conversion commands in MISSION1.md:

    .\Tools\Build\build_mission1.ps1 -RunTests -SmokeTest

The builder preserves the existing private scene and layout. A fresh assembly
creates them from the converted art and authored scene-builder source.

## Automated results

24 tests passed: five EditMode and 19 PlayMode, with zero failures or skips in
this local run. The suites cover:

- Jump buffering, coyote timing and pendulum constraint/damping behavior.
- Box motor movement, wall collision/sliding, jumping/steps and camera input scaling.
- Original character rig/material/camera integration.
- Swing release momentum, wall crawl/jump-off and collision-checked roof mantle.
- Timeline subtitles, intro pause/skip and control restoration.
- Ordered/spatial checkpoint acceptance and living-enemy completion gate.
- Death during combat, restored approach/guards, and recovery after combat that
  preserves already defeated guards.
- Traced web damage/stun, dome energy/protection, original effect placement,
  queued three-stage melee, telegraphed enemy melee and ranged bullet damage.

Private integration tests explicitly skip when the required local scenes are
absent. A public source-only clone must not treat those skips as gameplay passes.

The rendered standalone smoke passed 17 checks, with no recorded Error,
Exception or Assert messages. It exercised:

1. Intro control lock and authored Timeline subtitle.
2. Pause retaining the cinematic lock, then skip restoring gameplay.
3. Zip, wall latch/crawl and roof mantle across three ordered original-geometry
   rooftop checkpoints, with no route teleportation.
4. Bank arena lock and rejection of the skylight while guards remain alive.
5. Dome energy/protection and collision-traced web shots capturing all guards.
6. Three captured guards' wrapping instances active, and the final defeat
   releasing the arena.
7. Movement into entry range, the interaction completing the mission, and a
   rendered completion screen captured locally.

The smoke route drives gameplay APIs with scripted input. Its zip anchors are
selected from ground raycasts at the authored checkpoint destinations, and its
web shots aim through the same collision path at guard positions. It gives the
player temporary invulnerability to isolate mission progression; separate tests
verify actual enemy damage. It does not validate human aiming, complete input
accessibility, every camera angle, or gamepad hardware.

## Local evidence and output

- TestResults/mission-editmode.xml and mission-playmode.xml: Unity test results.
- TestResults/mission-smoke.json: standalone checks, GPU and recorded errors.
- TestResults/mission-private.png: private rendered completion preview.
- Logs/mission-scene.log, mission-editmode.log, mission-playmode.log,
  mission-build.log and mission-smoke.log.
- Builds/Mission1/SpiderManMission1.exe and accompanying data/runtime files.

These outputs contain or depend on private art and remain ignored by Git.
Standalone logs include warnings about unused depth-of-field/Panini shader
passes stripped from this renderer. Those post effects are not enabled in the
scene. A passing smoke report is not a claim that all engine warnings disappear.

## Limits requiring further work

The route, guard placement, bank entry role and gameplay tuning are authored;
the original L1A1 trigger-script semantics and actual bank/skylight identity
remain unverified. Original numeric animation clip bindings are provisional.
They demonstrate Mecanim integration without establishing matching original
poses, hit timing or polished defeat animation. The original narration has not
been identified; the intro deliberately uses newly authored subtitle text.

This is a functional vertical slice using original art, not a verified 100%
reconstruction of the shipping mission or a finished production release.
No long-session soak, frame-time benchmark, memory budget, save/load persistence,
full accessibility review or broad hardware test matrix has been completed.
The 60 FPS cap is configuration, not measured performance evidence.

All authored implementations, tests, setup instructions and conversion recipes
can be tracked; proprietary art, converted assets, generated scenes/prefabs,
Timeline assets, private layouts, builds and screenshots must stay local.
