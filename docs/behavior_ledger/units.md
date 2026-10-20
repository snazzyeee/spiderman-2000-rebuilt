# Units and coordinate conventions

Implemented by src/core/psx_units.gd (runtime) and tools/reference/psx_units.py
(tools). Both reproduce tools/reference/tests/data/units_vectors.json.

## Constants

| Convention | Value | Evidence | Confidence |
|---|---|---|---|
| Fixed point | 4.12 for vectors/normals, 20.12 for positions; 4096 = 1.0 | `decomp:quat.h#CQuat` (unit w = 4096); `decomp:spidey.cpp#CPlayer::CalculateSwingWebParameters@f8a0392 (@Ok)` (`>>12` from position to world) | recovered |
| Angle | 4096 units per turn; signed deltas wrap at +/-2048 | `decomp:camera.cpp#CalcTheta@f8a0392` | recovered |
| World Y axis | Points down (PSX convention) | `decomp:web.cpp#Web_GetGroundY@f8a0392 (@Ok)` probes +Y (20000 units) to find the ground | recovered |
| Position units | TRG node positions are raw i32 world units; entity mPos is the same value << 12 | Raw TRG bytes: every decoded position is found as an i32 triple inside its node (`tools/reference/export_trg_graph.py` cross-check, 844/844 Level 1 nodes) | recovered |
| Axis signs to Godot | (x, -y, -z) | Hypothesis: a rotation of 180 degrees about X turns Y-down into Y-up and keeps handedness | **unknown**: not confirmed by the landmark check below |
| Meters per model unit | 0.019792303442955017 | Authored calibration: the 18-bone player rig's bind pose (~90.9 units) stands 1.8 m (src/world/asset_library.gd fit_character) | authored |
| Original tick | 30 Hz assumed | Converter exports animations at 30 fps; frame-count camera blends | partial |

`per_frame_to_per_second()` scales per-frame constants by the tick. Godot physics
runs at 60 Hz, so ported per-frame rates must be converted, never copied.

## Level 1 landmark check (open)

src/diagnostics/verify_units.gd loads the converted l1a1_g/l1a1_o geometry at
the calibrated scale, then tests whether L1A1 TRG restart, baddy, powerup and
point nodes rest on that geometry, for every axis-sign combination and for
power-of-two TRG:model ratios from 1/4 to 8. Result on 2026-10-20: **fails**.
No hypothesis places any landmark within the support window (0.3 m below to
1.5 m above a surface), while collision is live.

Offline analysis against the exact triangles:

- `L1A1_L.psx` contains no mesh, so `_G` really is the level geometry.
- Under X kept and Z negated, an XZ footprint correlation finds a best
  TRG:model ratio of 4 with an unexplained offset of about (-3023, +4377)
  model units (95.7% of player-area nodes over geometry; next best 89%).
- Under that alignment the vertical relation is inconsistent (inter-quartile
  spread of about 820 model units, about 16 m, for either Y sign).

So converted level geometry and TRG positions are not related by one uniform
scale, sign and offset. The converter's level-chunk placement is the leading
suspect: chunk node translations carry ninth fractions (e.g. -10204.889).

Consequences until resolved:

- `position_godot` in trg_graph.json is computed with the hypothesis above and is
  flagged `position_godot_status: unverified`. Do not place gameplay from it.
- The authored mission route (assets/environments/mission1_layout.json) remains
  the source of Level 1 placement.
- Next step: decode PSX level-region/chunk origins independently (decomp m3d
  region structures plus raw `_G.psx` headers), compare with the converter's node
  translations, and re-run verify_units.gd. Its pass criteria stay unchanged:
  configured mapping best, at least 80% supported, and at least 30 points ahead
  of every alternative.
