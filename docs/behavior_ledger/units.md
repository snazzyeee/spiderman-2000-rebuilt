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

## Level offset files investigation

Evidence and format are in [offset_files.md](offset_files.md). A CRC-checked
archive audit decoded nine files / 34 records. No L1A1 offset file exists.
The loader stores a key and two signed-short parameters, not a decoded XYZ
translation. Their consumer and units remain unknown; all transforms stay
unchanged. The first-column values fit both the object and model ranges of
their respective G banks, which does not distinguish the index space.
L1A3/L1A4 with/without spatial correction is blocked by that missing meaning;
applying the fields as axes would invent a transform. The original L1A1 gate
continues unchanged, and the O-bank attribution already accounts for all seven
former unmatched chunk origins. TRG placement remains unverified.

## Round 2 diagnostic contract

Evidence `remake:landmark-diagnostics-v2`, confidence authored. The original
21-check gate and its support window, hypothesis set, 80% threshold and 30-point
margin stay unchanged. Per-landmark reporting adds original node identity,
type/subtype/flags, current converted coordinates, a full-height XZ collision
probe, nearest vertical surface above/below, nearest mesh chunk and summaries by
type/subtype. Classification is descriptive: outside footprint, over geometry
with an unsupported gap, or near a previously unmatched name candidate. A 2 m
chunk-nearness radius is authored diagnostic grouping, not a placement correction.
Vertical gaps are reported, never subtracted from node coordinates.

Evidence `trg:L1A1:node12`, confidence partial (converter command decode): the
startup references G/O/L banks plus character/effect banks. Nodes 14 and 257
reference two background model checksums; nodes 224/282/283 reference following
TRG areas. Several later environment arguments decode to control characters;
these are unresolved and must not be interpreted as geometry filenames.

Evidence `psx:L1A1_O:object-table` + `glb:l1a1_g:node-transforms`, confidence
recovered: six of the seven former G-name mismatches match O-bank object origins
directly, using the already decoded divisor/signs. The seventh matches O object
7's origin while its exported mesh name reuses index 5. Thus an exported mesh
index does not uniquely identify a source-bank object. No origin offset or
converter placement defect follows from those seven candidates. G object words
at +16/+18/+20 are zero for all 137 objects; no rotation correction is inferred.

Evidence `decomp:thug.cpp#CThug::CThug/SetThugType@f8a0392 (@Ok)` and
`decomp:baddy.cpp#CBaddy::SetHeight@f8a0392 (@Ok, AlmostMatching)`, confidence
recovered: the constructor consumes a node position via the common position
reader; height updates probe from a type-specific root-height displacement and
settle the entity root that distance above ground in Y-down coordinates. Thug
and henchman types set that displacement to 100 original world units. This does
**not** prove every spawn node was authored at the settled ground root height.
`decomp:trig.cpp#Trig_ExecuteRestart@f8a0392 (@Ok, AlmostMatching)` copies a
restart position directly to the player root before its command list. Neither
fact proves a universal feet/root/center correction for TRG node classes.

Evidence `remake:extra-geometry-diagnostic`, confidence authored: referenced
following-area TRG graphs can supply additional G/O bank names for a separate
diagnostic collision world. Convert only named local banks with the pinned MIT
converter, record source/tool/output hashes privately, and load those GLBs with
GLTFDocument in a standalone diagnostic. L banks with zero model counts are
reported as non-renderable. Additional-area results never replace or modify the
original L1A1 gate. A transition reference does not establish simultaneous
geometry residency. No supplemental grounded gate is justified yet, since
node-class grounding conventions remain insufficiently decoded.

## Level 1 landmark check (open)

Round 2 evidence `remake:units-landmark-v2` (private per-node report): 28 supported,
82 outside the G/O XZ collision footprint, 36 over geometry with unsupported
vertical gaps; zero unsupported nodes are within the authored 2 m nearness test
of a former unmatched chunk. By class: BADDY 12/99 (81 are script-only), POINT
11/38, POWERUP 5/7, RESTART 0/2. Gaps differ even within actor classes; no common
offset is inferred. These counts are diagnostic observations, not new criteria.

Evidence `remake:units-extra-geometry` and referenced TRG nodes in the private
manifest: startup and following-area graph references yielded five convertible
G/O banks and three L banks with no models. The additional collision world still
supports 28/146 and selects the same configured mapping. It neither improves nor
replaces the original gate. Character/effect banks are not assumed to be placed
environment geometry. Background checksums resolve to existing O-bank models;
all seven former mismatches have exact O-origin attribution, including exported
mesh-index reuse at O object 7. No converter post-process fix is justified.

Remaining blocker: several dynamic environment arguments are unresolved control
characters in the existing command decode; no proven missing geometry target or
class-specific TRG height convention has been recovered. Placement remains
unverified. No grounded-only gate was added on an unproven node selection.

After separating converter model units from world units, the unchanged gate on
2026-10-20 scores the configured mapping best: **28/146 (19.18%)** supported;
runner-up **1/146 (0.68%)**. It still fails the >=80% and >=30-point-margin
criteria (2 of 21 checks fail). Geometry and the mission route are unchanged in
meters; no fitted offset or search expansion was introduced. The 40 smoke checks
pass headless, Forward+ and Mobile. The remaining mismatch is unresolved; private
TRG placement stays unverified. The historical analysis below predates the unit
distinction and must not be used to justify a fitted transform.

### Independent region audit

Evidence `psx:L1A1_G:header/object-table/model-table` (SHA-256 recorded by
tools/reference/decode_psx_regions.py in private staging/reference/l1a1-regions.json):
v6 (0x00020006), 137 objects at byte 12 with 36-byte stride, 137 model pointers,
packet table at 174160. Origins are signed 20.12 triples at object offsets 4/8/12;
model index is u16 at +22. Packet records have tag/length/payload and a -1
terminator. The bounded decoder rejects bad sizes, pointers, indices and versions.
Confidence: recovered (exact data decode). `decomp:spool.h#SPSXRegion/SModel@f8a0392`
and `decomp:spool.cpp#Spool_SkipPackets@f8a0392 (@NotOk)` support the model/packet
structure, but the region is an in-memory structure, not the file header.

Converter evidence `multitool:3c9028e#PsxMeshHeaderReader/PsxMeshSemantics` (MIT,
https://github.com/slfx77/neversoft-multitool): static mesh vertices and decoded
object origins are divided by **2.25** in the exported model; axes become
(x,-y,-z). Independently, object 19 origin (-22961,4926,-54358) divided by 2.25
and axis-flipped equals exported mesh_00000013 translation
(-10204.889,-2189.3333,24159.111). Raw mesh 19 bounds divided by 2.25 reproduce
the GLB accessor bounds. Ninth fractions are explained by division by 9/4,
not evidence of arbitrary placement. Confidence: verified for this comparison.

The full GLB audit finds 116 plain mesh-name candidates: 109 translations agree
with their G-file object origin /2.25 within 0.01 exported unit; seven do not.
Exported nodes do not identify their source bank, so these seven candidates are
reported as unmatched, not treated as proven bad placements. Additional mesh
names have suffixes and are not assigned by this audit. Raw origin bounds for the
G file are X [-25557,-8148], Y [-3466,7526], Z [-56614,-186]; TRG landmarks also
extend into positive X. The decoder reports per-candidate origin deltas and raw
versus exported vertex bounds. These facts narrow the investigation but do not
establish a complete geometry/TRG relation.

World units and converted model units were conflated. Keep the authored character
calibration as METERS_PER_MODEL_UNIT; original world units use that value /2.25.
Geometry imports use model units. This is a decoded converter factor, without a
landmark-fitted offset. The landmark gate below remains required before accepting
the mapping as verified.

## Historical (superseded)

The following analysis predates the decoded 2.25 converter unit distinction.
Its zero-support result and fitted offsets are superseded and are not runtime
specifications. The original gate criteria remain current.

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
