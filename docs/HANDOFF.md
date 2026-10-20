# Handoff — 2026-10-20

## Current repository state

Branch: main, pushed without force. HEAD at handoff preparation:
`574739f3d3a5da48546be7a837da1f5b69ebbdbe`. Runtime/pipeline audio milestone:
`000df6b`. This handoff is the following docs-only commit; `git rev-parse HEAD`
identifies its final commit. No task changes are left uncommitted.

An unrelated untracked `docs/gemini/GEMINI_BRIEF.md` appeared during the run.
It was not read, modified, staged or deleted. Consequently final status contains
`?? docs/gemini/`; do not discard it to manufacture a clean workspace.
The section 1 completion checkpoint was clean before this folder appeared.

The requested sections were handled in order through section 3. New sections
were stopped to preserve context/time for this handoff, as instructed.

| Section | Result |
|---|---|
| 0 Sync | Completed from 6e59f82 |
| 1 Unity deletion | Completed, gated and pushed; legacy tool removal is a separate commit |
| 2 Landmarks | Diagnostics/bank attribution completed; original alignment gate still fails |
| 3 Audio identities | Implemented and gated; missing/invalid catalog joins remain unresolved |
| 4 Asset pipeline | Not started |
| 5 MissionRuntime | Not started |
| 6 Playability/debug overlay/human checklist | Not started |
| 7 Recorder/goldens/CI | Not started |
| 8 Hygiene | Partially covered: superseded units heading, audio ledger index, README and migration commands updated |
| 9 Handoff | This document |

## Exact final validation

Final evidence: `staging/validation/regression/section3-final/summary.json` and
its per-target logs/reports. Python: **38 reference + 9 pipeline = 47 passing**.

| Target | Smoke | Traversal | Engine/script errors |
|---|---:|---:|---:|
| Headless, forward_plus | 42/42 | 150/150 | 0 |
| Real Vulkan Forward+ | 42/42 | 150/150 | 0 |
| Real Vulkan Mobile | 42/42 | 150/150 | 0 |

Import passes. GPU logs identify Vulkan 1.3.260 on AMD Radeon RX 580 2048SP.
The original **40 smoke** and **143 traversal** assertions are all retained;
`retained-checks.json` proves their labels remain in the larger suites. New smoke
checks exercise exact XA and SFX playback; seven new traversal assertions test
identity lookup and absent private registries. Python fixtures remain synthetic.
No human playtest, material visual QA or cutscene verification has been claimed.

Run the regression matrix with:

```powershell
./tools/pipeline/run_regression.ps1 -Milestone next-section
```

This runs both Python suites, import, and smoke/traversal on all three targets;
it checks report freshness/counts, requested renderer, real Vulkan evidence and
zero engine/script errors. It does not treat the known landmark failure as a
passing regression. Run and report that separate gate as described below.

## Completed Unity deletion

All 22,579 preservation entries were checked: 22,573 exact matches and six
accepted regenerated reports/captures. Each of the five PNG exceptions first
passed exact inventory identity, valid bounded PNG/CRC/decompression, 1280x720
viewport dimensions and smoke writer filename proof. Synthetic tests reject a
sixth PNG, changed paths, invalid PNG bytes and preserved-output collisions.

Inventory SHA-256 remains
`70f05392d00b6d67a52f4e0413dc07fd26b8690e490c37ef77738f119a90e892`.
The private `final_verification.json` and `purge_operations.json` record success.
The removed list is exactly Game, Builds, Logs, tools/Unity, tools/Build and
tools/third_party/unity-cli. No extra sweep removals were needed outside those
trees. Deleted tracked paths and the Unity-era ignore block were removed.
Shared fingerprint: `tools/reference/pinned_tools.py`. The three Unity-only
prepare_* scripts were removed separately. Backup remains
origin/archive/unity-pre-migration at 8dfe6ddab0b508daa0a34de2a76f630b15c5a5db.

Smoke output now lives exclusively under **staging/validation/smoke/**. Old
preserved output paths remain unchanged. The launcher checks all six new smoke
paths against preservation targets before every smoke launch.

## Gates still failing and remaining evidence gaps

The unchanged original units gate is **19/21**: configured signs/ratio are best,
but only **28/146 (19.18%)** are supported versus **1/146 (0.68%)** runner-up.
The >=80% and >=30-point margin criteria still fail. No thresholds, support
window, hypothesis set or positions were changed to pass it. TRG placement stays
unverified and must not be enabled in mission1.

Per-node diagnostics: **82 outside XZ geometry, 36 over geometry but unsupported,
28 supported; 0 unsupported nodes within the authored 2 m nearness test of a
former unmatched chunk**. By type: BADDY 12/99 (81 script-only), POINT 11/38,
POWERUP 5/7, RESTART 0/2. Gaps vary; no fitted vertical correction was applied.
The 100-world-unit settled henchman root-height displacement is recovered, but
does not prove a corresponding spawn-node convention. No grounded-only gate was
invented from an unproven selection.

All seven former chunk mismatches match decoded O-bank origins. One exported
mesh name reuses index 5 at O object 7's origin. No placement defect is established.
Five explicitly referenced G/O banks were converted with the pinned MIT tool;
three referenced L banks have no models. Adding those following-area banks in a
separate diagnostic world still supports 28/146. Transition references do not
prove simultaneous residency. Several later SpoolEnv arguments are control
characters in the current converter decode and remain unresolved. Background
model checksums point into the existing O bank; their full placement semantics
have not been recovered.

```powershell
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/verify_units.gd
# The command above intentionally exits nonzero while the original gate fails.
./tools/third_party/audio-python/Scripts/python.exe tools/reference/analyze_landmarks.py
./tools/third_party/audio-python/Scripts/python.exe tools/reference/prepare_landmark_geometry.py
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/verify_extra_geometry.gd
```

The analyzer enriches the original private report without changing its result.
It uses position_world; cached position_godot fields in private TRG graphs must
not be treated as verified placements.

## Audio identity results and limits

Exact source filename/path plus extraction WAV SHA-256 joins bind **901 XA
registry entries / 1,249 catalog identities** and **1,993 SFX entries**.
Shared XA slots are preserved, not collapsed into one playable identity.
Global SFX 9/30 and XA33 track requests resolve and play in private smoke.

The bounded decoder parses all 63 SFX files. Five matching KAT files are absent;
one bank has an out-of-range sample reference and is entirely excluded. Their
identities remain unresolved, with explicit dispositions in the private catalog.
No filename was guessed as a sound ID. Level aliases require an explicit or
active bank; the future level runtime must select that bank. The authored mission
currently needs the recovered global IDs. Missing registries return false cleanly.

Rebuild catalogs/registry with the commands in GODOT_MIGRATION.md. Asset
preparation now generates both identity catalogs before building the registry,
and uses run_godot.ps1 for guarded import and route generation.

## Recovered versus authored

Recovered: numeric XA catalog indexing; SFX record/alias/asset selection; KAT
metadata layout; converter sample-index filenames; original traversal predicates,
event identities and camera command semantics already recorded in the ledger.
The settled actor root-height rule and exact O-bank origin attribution are new
evidence. Some upstream functions remain tagged AlmostMatching/partial; see ledger.

Authored: exact-join registry policy, unresolved/ambiguous lookup rejection,
diagnostic grouping/radius and additional-geometry harness. Existing pendulum
rates, anchors, wall attach input, grace/orientation/edge probes, zip tuning,
most camera defaults, input assignments and placeholder swing binding remain
authored. The 2.25 geometry export divisor is decoded; meter calibration is
authored. No new original-game spatial rules were inferred from fitting.

## Private files required next

- `staging/raw_extracted/PC/app/{data.pkr,SpideyPC.exe}`: canonical archive and
  fingerprint-gated static executable input; never execute the latter.
- `staging/raw_extracted/Extracted/data/data/`: raw PSX/SFX/KAT and other assets.
- `staging/raw_extracted/audio_source/{dialogue_manifest.json,Wav/}`: extracted
  source identities and verified WAVs; its _work~ tree holds decoded TRGs.
- `staging/raw_extracted/Dialogue/native_voice_catalog.json` and
  `staging/reference/native_sfx_catalog.json`: exact audio identity catalogs.
- `assets/` including characters, environments, audio registry/WAVs and UI.
- `levels/L1A*/trg_graph.json`: private trigger graphs; placement unverified.
- `staging/reference/{l1a1-regions.json,l1a1-o-regions.json,landmark_geometry/}`
  and `staging/validation/{units-landmark.json,units-extra-geometry.json}`.
- `staging/migration_audit/`: immutable inventory, proof and purge audit.
- `tools/third_party/`: pinned Godot 4.7.2, converter, audio Python/FFmpeg and
  read-only decomp f8a039248908e085f8fba8106769aea7f8f9de67.

None of these private inputs or outputs were committed. No original program or
decomp build was run, and no tools/functions/*.bin data was vendored.

## Next three recommended tasks

1. Resume requested section 4: alias/provenance post-processing, authored material
   ledger and visual QA, remaining Level 1 G/O banks/collision, and TRG-evidenced
   Bink-to-Theora conversion with cinematic ownership. Do not claim these exist yet.
2. Implement section 5 from a new opcode ledger, using a logic-only synthetic TRG
   harness and a default-off mission flag. Decode the unresolved dynamic spool
   arguments and spatial grounding conventions before enabling TRG placement.
3. Complete section 6 playability integration checks, debug overlay, Controls.md
   and human checklist; then section 7 recorder/goldens/secret-free CI and finish
   section 8 hygiene. Human playtesting remains a required separate human action.

## Permissions and operational notes

Elevated exec was needed for Git index writes/commits/push/network access, guarded
Unity deletion, Godot import/runtime access to its normal user log/save directory,
and process inspection. The MIT KAT exporter source was fetched read-only at
3c9028e to prove bulk WAV index naming; the local converter executable remained
fingerprint-gated. There were no automatic-approval rejections to override.

Set TEMP/TMP to staging/test_tmp for Python tests (run_regression.ps1 does this).
Keep the Godot editor closed before imports. One post-purge run was rejected by
the unchanged report-freshness guard; rerunning the same criterion passed.
Final evidence uses successful complete matrices, not sandbox log/save failures
or stale reports. Keep every existing gate and report the landmark failure.
