# Player camera

## Recovered behavior

### Per-surface presets
The follow camera has five presets: **Floor, Wall, Ceiling, Swing, Falling**.
Each has five parameters in world units: X offset, Y offset, Z offset, XZ
(horizontal) distance and Y (vertical) distance. Entering a surface state applies
its preset and records a player camera state (Swing = 4, Falling = 5).
Evidence: `decomp:spidey.cpp#CPlayer::Set{Floor,Wall,Ceiling,Swing,Falling}Camera@f8a0392 (@Ok)`;
binding of each global to its parameter by `exe:0x4bf5d0..0x4bf8a0` (tools/reference/extract_camera_presets.py).
Confidence: recovered.

### Presets are script data, not constants
All 25 preset values live in zero-filled (BSS) data in the canonical build; none is
a static initializer. Two writers exist: the player constructor (defaults; of these,
only Wall XZ distance 700 and Ceiling XZ distance 800 are immediate constants) and
`CPlayer_SetSpideyCamValue` at `exe:0x4b97d0`.

Level scripts drive that writer through the TRG command **SetSpideyCamValue**
(opcode 180) with arguments `(mode, param, value, frames, apply_now)`:

| mode | preset | | param | meaning |
|---|---|---|---|---|
| 0 | Floor | | 0 | X offset |
| 1 | Wall | | 1 | Y offset |
| 2 | Ceiling | | 2 | Z offset |
| 3 | invalid (error path) | | 3 | XZ distance |
| 4 | Swing | | 4 | Y distance |
| 5 | Falling | | | |

`value` is a signed 16-bit world-unit amount, stored as the preset; when `apply_now`
is set and the player is currently in that surface state, the camera blends to the
new value over `frames` frames immediately. The mapping is decoded from the
function's two jump-table levels and confirmed by address identity with the five
Set*Camera functions (`exe:0x4b97d0`, outer table `exe:0x4b9d88`). Confidence: recovered.

Script usage is heavy and per area: 115 commands in L1A1, 95 in L1A3, 275 in L7A2.
Typical values across the game (full census: staging/reference/camera_presets.json):
Floor XZ distance 528 (most common), Wall XZ distance 700, Ceiling XZ distance 370,
Swing XZ distance 350 to 1800, Falling Y distance -125 to -950; blends usually take
32 frames (8 and 64 also occur).

### Blending
Every camera distance/offset/angle setter takes `(value, frames)`. With frames > 0
the camera moves linearly by (target - current) / frames each frame; with 0 it snaps.
Evidence: `decomp:camera.cpp#CCamera::SetCamXZDistance@f8a0392 (@Ok)` and siblings. Confidence: recovered.

### Other camera facts
| Fact | Value | Evidence | Confidence |
|---|---|---|---|
| Follow mode used by scripts | camera mode 3 (`SetCamMode(3)`; the upstream enum name is a leftover from an older engine) | trg:L1A1 command points; `decomp:spidey.cpp#CPlayer::Set*Camera` require mode 3 | recovered |
| Tripod smoothing factors | 4, 8, 4 (each at most 16) | `decomp:camera.cpp#CCamera::CCamera`, `#SetTripodInterpolation@f8a0392 (@Ok)` | recovered |
| Default zoom | 2365 | `decomp:camera.cpp#CCamera::CCamera@f8a0392 (@Ok)` | recovered |
| Collision probes | separate left/right and back ray lengths and angles | `decomp:camera.cpp#CCamera::SetCollisionRay*`, `#SetCollisionAng*` (@Ok setters) | partial (values not recovered) |
| Shake presets | Big (25, 0, 50), Medium (0, 0, 30), Small (0, 0, 25), fourth (0, 0, 16); decay (3, 0, 3); speed (600, 0, 600) | `decomp:camera.cpp#CCamera::Shake@f8a0392 (@Ok)` | recovered |
| Put camera behind | Unless angle-locked: on the floor, yaw = player heading; on a wall, yaw faces the wall from outside (from its normal), and in follow mode the Y distance comes from the Wall preset, modulated by crawl orientation | `decomp:spidey.cpp#CPlayer::PutCameraBehind@f8a0392 (@NotOk)` | partial |
| Angle lock | Script argument 0 locks, non-zero unlocks | `decomp:spidey.cpp#CPlayer::SetCamAngleLock@f8a0392 (@Ok)` | recovered |

## Unknowns

| Item | Status |
|---|---|
| Main follow update | `CCamera::CM_Normal` is a stub (MEDIUMTODO) |
| Constructor default values other than the two immediates | Written from registers; not recovered |
| Collision ray lengths/angles in use | Setters recovered; values not |

## Remake specification (Phase 2)

1. `CameraProfile` resources for Floor, Wall, Ceiling, Swing and Falling, each with
   offset (Vector3) and xz/y distance in world units, converted with `PsxUnits`.
2. A `CameraDirector` executes SetSpideyCamValue semantics exactly: set the
   per-mode value; blend now only when `apply_now` is set and the player is in
   that state; linear blend over `frames` original frames (converted to seconds).
   Mode 3 is rejected.
3. Level start uses constructor defaults as a fallback, then TRG autoexec and command
   points override them, as in the original.
4. Spring-arm sphere cast plus left/right and back whisker probes model the
   recovered collision ray set; lengths/angles are authored tuning.
5. Shake presets use the recovered amplitude/decay/speed triples.

## Planned tests (tests/scenes/test_camera.tscn)

- A SetSpideyCamValue with frames = 32 reaches its target in exactly 32 original
  frames (1.067 s) with linear progress; frames = 0 snaps.
- `apply_now` = 0 changes the stored preset without moving the camera.
- Mode 3 is rejected and logged.
- Floor -> Wall -> Ceiling -> Swing -> Falling transitions select the matching profile.
- Camera never ends inside geometry (camera-to-pivot ray clear) across the transition loop.
