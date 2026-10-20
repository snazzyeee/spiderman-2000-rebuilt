# Zip web

## Recovered behavior

- **Two airborne zip variants** exist, one per shoulder button (R1 and R2), plus a
  separate availability check that takes a line query and a mode argument.
  Evidence: `decomp:spidey.cpp#CPlayer::CheckJumpingR1ZipWeb`, `#CheckJumpingR2ZipWeb`
  (stubs) and `#CheckZipWebAvailability` (stub). Confidence: partial (names only).
- **Landing clean-up.** After a zip lands, the player runs eight line probes
  around the landing point, one every 45 degrees (512 angle units), in the plane of
  the landing surface, each of a caller-given radius. Any probe that hits geometry
  pushes the player out along that hit's normal by the same radius.
  Evidence: `decomp:spidey.cpp#CPlayer::TidyUpZipWebLandingPosition@f8a0392 (@NotOk)`.
  Confidence: partial (decompiled, not byte-matched).

## Unknowns

| Item | Status |
|---|---|
| Which variant targets a point and which fires straight ahead | Not recovered |
| Zip speed, travel limit, arrival test | Not recovered |
| Landing clean-up radius | Passed by the caller; not recovered |

## Remake specification (Phase 2)

1. Two zip modes: `ZIP_TARGET` (to the aimed hit point) and `ZIP_FORWARD`
   (forward/up burst). They map to R1/R2 by an input binding, not hard-coded.
2. Movement is swept at a fixed speed (authored tuning) and ends on arrival or contact.
3. Landing runs the eight-probe ring in the landing surface's tangent plane and
   resolves penetration along each hit normal, matching the recovered clean-up.

## Planned tests (tests/scenes/test_zip.tscn)

- Both modes arrive within 0.25 m of target or stop at first contact.
- No tunneling through a 0.1 m wall at maximum zip speed.
- Landing next to a pillar and a wall corner leaves no penetration (all eight probes clear).
