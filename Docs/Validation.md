# Phase 1 validation

Date: 2026-10-20. Unity 6000.6.6f1; Windows x64; AMD Radeon RX 580 2048SP.
The earlier primitive/capsule laboratory is retired and its production source,
scene, materials and prefab have been removed.

## Measured results

| Check | Result | Local evidence |
|---|---|---|
| EditMode | 3/3 passed | TestResults/editmode.xml |
| PlayMode | 8/8 passed | TestResults/playmode.xml |
| Original skeleton | 18 bound joints, valid Generic avatar | private unity-import-report.json |
| Original animation export/import | 300 clips preserved | private conversion/import reports |
| Original textures | 8 character and 98 level PNGs | private converted assets |
| Original L1A1 geometry | 115 mesh objects | private import report |
| Windows x64 development build | Succeeded, 178,418,420 bytes reported by Unity | Logs/unity-build.log |
| Standalone smoke | 12 checks passed; zero recorded errors | TestResults/standalone-smoke.json |
| Rendered private scene | 1280x720, visually inspected | TestResults/phase1-private.png |

The source-only test suite covers jump-buffer expiry/consumption/clearing,
box sweep obstruction, low steps, ceiling contact, jumping/reset, diagonal speed,
gamepad left/right stick bindings and camera rate at 60/120 Hz. The private
integration test loads the original scene, checks textured URP Lit materials,
bone count, absence of capsule components, and camera obstruction correction.
That private test explicitly skips when local assets are missing.

The standalone route grounded the box motor on original L1A1 geometry, moved,
jumped, reset, paused/resumed, checked the original skin/Generic avatar,
confirmed zero capsule colliders/controllers, and verified three mixer groups
and twelve 3D low-pass-filtered impact emitters before taking the screenshot.

The preview showed 60 FPS / P95 16.7 ms after warm-up in the simple stationary
view. This is not a full-level benchmark or proof of hitch-free play.
The build log includes warnings for unused stripped depth-of-field/Panini
postprocessing shaders; no such postprocessing is enabled on the scene camera.
SSAO, directional soft shadows and exponential-squared fog are configured.
No volumetric-scattering renderer is claimed.

## Integrity and privacy

The conversion recipe selects exact high-resolution PC archive members, checks
CRC/length, verifies the pinned converter executable, and exports original data.
A repeat conversion and GLB/BIN/PNG integrity comparison are recorded locally.
Original game executables/installers/DLLs are never executed.

The converted glTF/BIN/PNG, 300 native .anim clips, avatar, URP materials,
private scene, manifests and screenshots are ignored. Public source contains
only scripts, settings, package pins, tests and procedures.

## Unverified fidelity gates

The 1.8 m normalization uses 0.01979230344 metres per export unit. This preserves
the exporter-relative scale between the character and level, but the metre
mapping is an integration choice. The automatically selected roof spawn is recorded in the private import report;
it is not a verified original mission start.

The motor uses a box body and render-mesh-derived static collision. Original
collision flags, movement constants, animation slot semantics/event timing,
wall crawling, web swinging, combat, mission scripts and sound cue mappings
are not verified/implemented here. Locomotion animation fields remain unassigned
until slots are identified; the imported character retains its authentic bind
pose. No fabricated animation or audio substitutes are included.

The AudioMixer hierarchy, attenuation and filter plumbing are present. This
phase does not identify or automatically bind Stan Lee or web-impact samples.
Physical gamepad feel, animation behavior and long-session reliability still
need manual evaluation. The 100% mechanical-fidelity target is not achieved.
