# Web swing

## Recovered behavior

- **Rope frame.** When a swing starts, the player builds an orthonormal frame
  from the rope: `u` = unit vector from anchor to player, `v` = unit(u x r)
  where `r` is a stored player reference direction, and `w` = u x v. The frame
  and the rope's verticality |u.y| are stored for the swing. This implies the
  swing is solved in a plane spanned by the rope and one horizontal direction,
  not as a free spherical pendulum.
  Evidence: `decomp:spidey.cpp#CPlayer::CalculateSwingWebParameters@f8a0392 (@Ok)`. Confidence: recovered (frame), unknown (meaning of `r`).
- **Phase-driven animation.** The swing controller holds a phase from 0 to 4096,
  centered at 2048. The player's animation frame is the phase offset from center,
  scaled to +/-k frames: `frame = k * (phase - 2048) / 2048`. When phase reaches
  4096 the swing controller ends (one arc per swing).
  Evidence: `decomp:web.cpp#CSwinger::SetSpideyAnimFrame@f8a0392 (@Ok, matching)` and
  `decomp:web.cpp#CSwinger::IsOneTimeToDie@f8a0392 (@Ok)`. Confidence: recovered
  (controller), partial (that every player swing uses it).
- **Camera.** Starting a swing applies the Swing camera preset and marks player
  camera state 4 (see camera.md). Evidence: `decomp:spidey.cpp#CPlayer::SetSwingCamera@f8a0392 (@Ok)`.

## Unknowns

| Item | Status |
|---|---|
| Anchor selection: geometry hit vs. virtual sky point | `CheckSwingWebAvailability` takes a line query but its body is a stub (MEDIUMTODO) |
| Swing start conditions and input mapping | `CheckJumpingSwingWeb` is a stub |
| Phase speed, gravity, pump and release velocity | Not recovered |
| Rope length rule | Not recovered |
| Swing animation slot numbers | Not present in decompiled player code (harvest found none) |

## Remake specification (Phase 2)

1. A swing is a planar pendulum in the rope frame above: state is angle and angular
   rate in the plane of `u` and the player's horizontal heading at attach time.
2. Expose a normalized phase in 0..1 (original 0..4096, center 0.5). Drive the swing
   animation by phase, not time, matching the frame mapping above.
3. Anchor policy is a tuning value: `GEOMETRY` (current: a real collider above the
   player with clear line of sight) or `SKY_HOOK` (virtual point). GEOMETRY stays the
   default until the availability rule is recovered.
4. A swing ends at the end of its arc (phase 1.0), on release input, or on swept
   collision. Release keeps tangential velocity.
5. All rates are tuning resources authored in seconds; any per-frame original value
   is converted with `PsxUnits.per_frame_to_per_second`.

## Deliberate deviations

- Phase 2 implementation: `src/player/states/swing.gd`; all numerical tuning and
  the explicitly authored animation binding are in `src/player/tuning/swing.tres`.
  HYBRID uses verified geometry when available and otherwise the authored sky hook.
  A phase-driven synthetic animation verifies real AnimationPlayer seeking, not
  only an internal phase variable. Test geometry and animation are synthetic.

- Continuous re-attach (chaining arcs without landing) is a remake choice until the
  original availability rule is recovered.

## Planned tests (tests/scenes/test_swing.tscn)

- Rope length stays within +/-1% for a full arc.
- Phase increases monotonically from attach to arc end; the animation frame follows it.
- The swing ends at phase 1.0 without input.
- Release velocity equals tangential speed within 2%.
- Swept collision ends the swing before any penetration.
- With GEOMETRY policy, no anchor is accepted without a live collider and line of sight.
