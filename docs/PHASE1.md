> Historical Unity workflow: its prepare_* scripts have been removed after preservation and purge. Use tools/pipeline/prepare_godot.ps1 and docs/GODOT_MIGRATION.md for the active Godot pipeline.

# Phase 1: exact import and rig procedure

Unity 6000.6.6f1, URP 17.6.0, Cinemachine 6.6.0, Input System 1.20.0,
glTFast 6.20.0. Run the commands below from the repository root with the Unity
project closed. No Blender, FBX SDK, game source, installer, or original
game executable is needed.

## 1. Prepare the converter and original archive

Place the PC data.pkr at LocalReference/PC/app/data.pkr. Use the installed
archive, not the installer EXE, as input.

Run Tools/Reference/setup_converter.ps1 in PowerShell. It downloads the
official Neversoft Multitool Windows CLI 1.3.4 release only when missing and
checks both the ZIP digest and the extracted executable digest. Downloaded
binaries stay ignored.

Official release:
https://github.com/slfx77/neversoft-multitool/releases/tag/v1.3.4

ZIP SHA-256:
062c89e00382e7b7c4b73015f228c88e7be34e50a4c48654d39e3142bd633d35

## 2. Convert the actual model, textures, skeleton and animations

Run: python Tools/Reference/prepare_phase1_assets.py

The script reads the PKR3 directory, selects exactly data/spidey.PSX,
data/spidey.psh, data/L1A1_G.psx, and data/L1A1_T.trg, and verifies each
decompressed member's length and CRC32. It deliberately avoids the separate
data/lowres/spidey.PSX variant. Only these data files are written.

It executes these converter subcommands with absolute paths:

    NeversoftMultitool.exe psx-anim-export spidey.PSX --anim -1 --fps 30 -o spidey.glb
    NeversoftMultitool.exe psx-mesh L1A1_G.psx -o Converted

The .psh must remain beside spidey.PSX so the converter can resolve the named
hierarchy. The original archive, executable and DLLs are never executed.
The first command keeps all original numeric animation slots. Thirty FPS is the
converter's native animation time base, not the render frame-rate cap.

The Python stage reads the GLB JSON/BIN chunks and writes:

    Game/Assets/_LocalReference/Phase1/
      SpiderMan/
        spidey.gltf
        spidey.bin
        Textures/image_000.png ... image_007.png
      L1A1/
        L1A1_G.gltf
        L1A1_G.bin
        Textures/                        98 PNG textures
      conversion-manifest.json

Node hierarchy, 18 joints, vertex weights, inverse-bind matrices and all
300 animation clips remain in the glTF. PNG bytes are extracted unchanged.
Geometry is not retopologized or replaced. The converter's unlit material
extension is removed so the Unity builder can assign URP Lit.

## 3. Assemble the Unity scene

Run: .\Tools\Build\build_windows.ps1 -RunTests -SmokeTest

Alternatively, open Game in Hub with the pinned editor and choose
Rebuild > Phase 1 > Assemble Authentic Assets.

glTFast imports glTF/BIN/PNG from the private Asset folder. The builder:

1. Instantiates Spider-Man, verifies 18 bound joints and 300 imported clips,
   creates a valid Generic avatar and disables imported automatic animation.
2. Copies all clips to private native Unity .anim assets with legacy disabled.
3. Assigns actual Universal Render Pipeline/Lit materials and the original
   textures. Original cut-out rendering is retained where reported by import.
4. Normalizes the character bind-pose bounds to 1.8 metres and applies the same
   scale to L1A1. The root stays at identity scale/rotation; only Visual is scaled.
5. Adds the box-sweep motor, input, session, Cinemachine camera and diagnostics.
6. Uses the original level meshes as static MeshColliders; selects a clear roof
   by downward queries rather than inventing a floor or platform.
7. Adds downsampled SSAO, directional soft shadows and exponential-squared fog.
   This is exponential fog, not an implemented volumetric-scattering renderer.
8. Creates Master > SFX, Music, Dialogue in the authored Traversal AudioMixer.
   Web-impact emitters have custom 3D attenuation and altitude/velocity-driven
   AudioLowPassFilter cutoff. No guessed sound/voice sample is auto-played.

The scene is Assets/_LocalReference/Phase1/SpiderManPhase1.unity.
It and all proprietary derivatives remain ignored. Existing scene edits are
preserved on subsequent assembly calls.

## 4. Drop the scripts onto another imported character

Create an empty player root at world rotation (0,0,0) and scale (1,1,1).
Parent the imported glTF instance underneath it as Visual. Scale only that child
to the desired height, and offset it so the soles sit at root Y=0.

Add SpiderManPlayerInstaller to the root. Assign ImportedVisual to Visual and
Movement to Assets/_Game/Settings/Movement.asset. Press Play. The installer
creates TraversalInput, TraversalSession, SpiderManCharacterController, the
kinematic BoxCollider/Rigidbody and the Cinemachine orbit setup. It reuses a
tagged Main Camera when one exists. Do not add a Unity CharacterController,
CapsuleCollider, dynamic ragdoll or another camera transform driver.

The direct editor assembly uses SpiderManRigSetup.Install(root, visual, tuning)
to serialize the same setup in the generated scene. A complete inspector-authored
setup can instead assign the public references in the controller and camera.

## Input and motor contracts

| Action | Keyboard/mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Orbit | Mouse delta | Right stick |
| Jump | Space | South button / A / Cross |
| Run (hold) | Left Shift | Left shoulder / LB / L1 |
| Pause | Escape | Start |
| Reset | R | Select / Back |

The Input System's stick deadzone is retained. Mouse displacement is measured
in pixels per frame; stick look is degrees per second. The two paths are scaled
separately and then smoothed. The motor reads the smoothed camera heading.
Cinemachine Third Person Follow alone writes the final camera pose and performs
obstacle correction with a 0.24 m camera radius and 0.05 m near clip.

The motor performs box sweeps, contact sliding, ground projection, overlap
recovery, low-step negotiation, jump buffering/coyote timing, and reset. It uses
bounded substeps and reuses query arrays. Saturated query buffers fall back to
complete allocating queries to preserve collision correctness in dense geometry.
Moving platforms, wall/ceiling attachment, swing constraints, combat and scripted
mission transitions are not implemented by this locomotion component.

WorldMask defaults to layer 0. The player is on layer 2, excluded from its own
motor/camera queries. Keep level collision on a layer included in both masks.
The player root must retain identity rotation/scale; rotate the visual child.

## Animation binding and fidelity gates

The clips are named spidey_anim_0 through spidey_anim_299. These names are
original slot identifiers, not verified meanings such as Idle or Punch.
The builder does not guess their meanings or silently play slot zero as idle.

Inspect the clips in Unity's Animation preview. After establishing each slot's
role, assign the private native clips to SpiderManAnimationDriver's Idle, Walk,
Run, Rising and Falling fields on OriginalSpiderMan. Its PlayableGraph blends
those exact clips with root motion disabled. An unassigned profile retains the
original bind pose; it does not synthesize animations.

The 1.8 m scale, movement tuning, box collision shape, automatically chosen roof
spawn and mesh-derived level collision are integration choices. Original trigger
collision flags, mission start, animation event timing, speeds and jump arcs
have not been matched to original gameplay measurements. This phase is an
authentic-asset integration scene, not proof of 100% mechanical fidelity.

## Audio clip assignment

TraversalAudioRig exposes Sfx/Music/Dialogue mixer groups, Player and WebImpact.
Assign an identified extracted web-impact AudioClip to WebImpact, then call
PlayWebImpact(worldPosition) from an actual web-contact event. The fixed pool
has twelve 3D emitters, 1 m minimum distance and 35 m maximum distance.
Call PlayMusic(clip) for the looping music bus and PlayDialogue(clip) for
unfiltered 2D narration. Stan Lee lines are not automatically identified or
renamed. Existing extraction of models does not establish audio cue semantics.

## Build and private/public split

The local executable is Builds/Windows/SpiderManPhase1.exe. The complete folder
must remain together. The smoke test briefly opens a rendered window, validates
the private scene, captures TestResults/phase1-private.png, and closes itself.
All these output files are ignored.

Public source consists of the scripts, settings, package pins, tests and recipes.
A fresh clone needs the user's PC archive and the local conversion step before
building the private scene. Never commit or upload converted models, textures,
animations, sounds, private scenes or screenshots containing those assets.

Importer documentation:
https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/ImportEditor.md
