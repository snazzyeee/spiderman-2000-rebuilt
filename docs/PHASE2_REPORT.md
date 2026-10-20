> Historical checkpoint at 6e59f82. Phase 0 deletion and numeric audio bindings have since progressed; see HANDOFF.md for current results.

# Autonomous migration and Phase 2 report — 2026-10-20

## Passed

| Gate | Result |
|---|---|
| Python reference / migration tests | 31 passed |
| Python pipeline tests | 6 passed |
| Existing smoke, headless | 40/40 |
| Existing smoke, Vulkan Forward+ | 40/40 |
| Existing smoke, Vulkan Mobile | 40/40 |
| Synthetic traversal, headless | 143/143 |
| Synthetic traversal, Vulkan Forward+ | 143/143 |
| Synthetic traversal, Vulkan Mobile | 143/143 |
| Final Godot import | No errors |

Real renderer logs identify Vulkan 1.3.260 and AMD Radeon RX 580 2048SP.
Final elevated runs allow Godot's normal log/save directory and contain no
engine/script errors or warnings. Earlier sandbox-only runs could not write
Godot logs/checkpoints; their reports are not the final gate evidence.
Private per-renderer reports and logs are under staging/validation/ as
smoke-{headless,forward,mobile}.json, traversal-{headless,forward,mobile}.json
and phase2-*.log. None are committed.

Traversal includes eight state nodes through MotorContext, the SpiderPlayer
compatibility API, pendulum phase/animation/release/collision/anchor checks,
five ceiling conditions, wall classification and bump timing, ledge grace,
knock-off orientation, both zip modes and eight-probe landing cleanup, exact
camera command interpolation and collision, web fluid and difficulty/health.
Five suite-completeness assertions supplement 138 behavioral assertions.
Fixtures synthesize geometry and animation and skip the private audio registry.

## Gates intentionally left failing

Unity preservation: the single authorized smoke JSON exception records original
SHA-256 2978b63f1c4bb758437f726c2a256d98180c82e5023aa8d84acde758084925d8
and a reason. The inventory was not edited. The purge then found five changed
PNG reports: godot-briefing, godot-intro, godot-menu, godot-mission,
godot-rooftop. It stopped before deletion; purge_operations.json is absent.
Game/, Builds/, Logs/, Unity tools, tracked Unity paths and their ignore block
remain. Legacy prepare_* tools remain because they supply the converter digest.
Phase 0 is **not complete**. See migration/PURGE_REPORT.md.

Units: bounded independent v6 object/model/packet decoding explains the ninth
fractions through Multitool's 2.25 divisor. Runtime/tool units now distinguish
converted model units and original world units. Geometry and mission route are
unchanged in meters. The unchanged landmark verifier now selects the configured
mapping best, but supports only 28/146 (19.18%) against 1/146 (0.68%) runner-up:
the >=80% and >=30-point margin gates still fail (19/21 checks pass). No offset,
scale fitted to landmarks, expanded search or weaker criteria was introduced.
TRG-derived gameplay placement remains unverified and unused. Full chunk audit
has 109/116 plain mesh-name candidate translations matching; seven unmatched
candidates need bank/placement identity investigation. See behavior_ledger/units.md.

## Recovered, partial and authored

Recovered: rope-frame construction and centered animation phase convention;
ceiling predicates and event identities; ground-loss/knock-off event identities;
five camera presets, mode/parameter mapping and linear frame-duration semantics;
web caps/no-cost suits/damage factors/health rules. Wall bump and zip landing
cleanup remain partial evidence; implementation does not upgrade their confidence.

Authored: pendulum gravity/pump/damping/phase rate, anchor policies, swing slot
binding, input assignments, ground locomotion, explicit wall attach, grace length,
orientation/edge reach probes, zip speeds/limits/radius, camera collision lengths
and all camera defaults except recovered Wall XZ 700 and Ceiling XZ 800.
Tuning lives in .tres resources. Per-frame timing uses PsxUnits conversions.

Recovered SFX 9/30 and XA33 track 2..4 are observable Dialogue requests. The
private clip registry lacks verified numeric identity fields, so those particular
sounds are **not bound for playback**. No filename is represented as a recovered
binding. Broader original camera follow/anchor/edge rules remain unknown as listed
in their ledger entries; tests validate the specified remake, not original-game parity.

## Ledger additions

- units.md: decoded file structure, source evidence IDs, converter-unit distinction,
  full candidate transform audit and retained failing landmark evidence.
- traversal_runtime.md: authored runtime/API contract and tuning boundaries.
- swing.md, crawl_ceiling.md, webbing_damage.md: implementation/deviation and audio
  binding status. animation_semantics.json adds reviewed event bindings; its
  harvesting tool preserves them only while their player-slot evidence remains.

All milestones were committed conventionally and pushed to main without force.
Private assets, audit files, TRG graphs and third-party tools remain ignored.
