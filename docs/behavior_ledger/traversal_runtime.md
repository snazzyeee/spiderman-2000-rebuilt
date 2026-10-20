# Authored Phase 2 integration contract

Evidence `remake:phase2-runtime-contract`, confidence **authored**. This entry
specifies remake integration only; recovered mechanics remain in swing.md,
crawl_ceiling.md, zip.md, camera.md and webbing_damage.md.

PlayerMotor is a CharacterBody3D with MotorContext and Ground, Air, Swing, Zip,
Crawl, Ceiling, Stunned, Cinematic child state nodes. SpiderPlayer preserves
mission/enemy/smoke APIs, including begin_swing, release_anchor, _step_swing,
controls_enabled, health, camera, yaw and target_yaw. Cinematics freeze traversal
and damage. Recovery detaches ropes. Authored movement uses 10 m/s ground speed,
38 m/s² acceleration, 25 m/s² gravity and 12 m/s jump. A box is 0.65x1.7x0.55 m
with center 0.88 m above the feet. Character visual height is authored 1.8 m.

All adjustable numbers live in .tres resources. MovementTuning also carries zip,
combat and collision sizes. SwingTuning carries pendulum gravity, damping, pump,
arc phase rate and anchor policy (GEOMETRY default, SKY_HOOK, HYBRID). Phase is
elapsed normalized arc progress, independent of pendulum angle, with center 0.5.
Rope frame is built at attach; a vertical-plane basis projects gravity into that
frame. Integrate the pendulum then sweep to the exact rope-radius point. Release
velocity is the analytic tangent at the new angle, plus optional authored boost.
Authored animation binding selects spidey_anim_1 by default, scrubs centered phase
into clip time, and makes no recovered swing-slot claim. Ground/air placeholders
use authored animation bindings. Recovered crawl events resolve function labels
and their conditional roles from animation_semantics.json. No runtime slot literals.

Wall attach requires the crawl input and a crawl-allowed, stickable wall contact;
this is an explicit deviation because original wall attach is unknown. Ceiling
attach needs upward/stationary motion, this-frame contact+record, crawl permission,
strict normal threshold and absence of no-stick. Godot collider metadata
`crawl_allowed` (default true), `no_stick` (default false) represents these flags.
Geometry contacts are ray/shape results, not inferred from proximity. Orientation
uses a quaternion blend, retaining visual scale. Interior/exterior/fence probes
are authored reach tests. Loss of crawl contact uses the same authored four-frame
grace; loss restores upright orientation and the recovered falling event.
Bump threshold is 20 original frames; bump enters Stunned and plays its binding.
Knock-off also enters Stunned with its own binding and surface-normal impulse.

Zip target uses an aimed surface point with authored clearance; forward uses an
authored forward/up direction and distance. Both use constant-speed body sweeps,
then eight probes in the landing tangent plane, resolving along hit normals with
body sweeps. R1 targets and R2 goes forward are authored input assignments. RMB
swings; R2 is no longer overloaded with swing. Q targets, T goes forward.

CameraRig: yaw/pitch pivot, spherical SpringArm, Camera3D. Five resources store
original world-unit values; Wall XZ=700 and Ceiling XZ=800 are recovered defaults,
all others authored. The director stores signed-16-bit command values immediately;
active apply_now starts linear interpolation at 30 Hz, inactive changes stay stored.
Each parameter blends independently; leaving a profile cancels its active blends.
Left/right/back collision rays are authored; spring handles the final sphere sweep.
No private staging data is read at runtime.

Existing authored mission combat is retained: melee/web range and cooldowns in
MovementTuning, health clamps, cinematic immunity, dialogue SFX and traversal
filter calls. WebFluid and DifficultyTable implement webbing_damage.md with suit
and global modifiers, spare swap events and empty bark; no original audio IDs are
guessed beyond the ledger's SFX 30 / XA bank 33 track 2..4. A visual-only web line
connects the authored hand offset to the anchor. These integration choices are
not evidence of original movement, animation content, audio or input mapping.

## Validation output ownership

Evidence `remake:smoke-output-isolation`, confidence authored. Smoke screenshots
and godot-smoke.json belong under staging/validation/smoke/. Their six exact
paths must not intersect any preservation inventory target. Historical captures
remain unchanged at their old destinations for the migration audit. This changes
diagnostic output ownership only, not gameplay or any smoke criterion.
