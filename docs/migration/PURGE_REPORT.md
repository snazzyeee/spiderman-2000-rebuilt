# Unity purge and Godot promotion report (2026-10-20)

## Follow-up verification (2026-10-20)

The verifier now accepts only the explicitly authorized regenerated
`TestResults/godot-smoke.json` -> `staging/validation/godot-smoke.json` identity,
recording its original inventory SHA-256, current SHA-256 and reason in
`accepted_exceptions`. The inventory is unchanged. Synthetic coverage proves a
different source or a missing destination still fails. All 34 Python tests pass
with TEMP/TMP under ignored staging/test_tmp (the sandbox's default temp path
denied writes).

Running the resumable purge revealed **five additional mismatches**:
`TestResults/godot-{briefing,intro,menu,mission,rooftop}.png`. The full private
evidence is staging/migration_audit/final_verification.json. These screenshots
are outside the authorized exception. Verification failed before deletion;
purge_operations.json was not produced and the Unity directories remain.
Phase 0 deletion is therefore still blocked. Neither the allowlist nor the
preservation criteria was broadened. The 40-check headless smoke passes.

## Current outcome

| Step | Status |
|---|---|
| Preservation copy (11,526 files, SHA-256 verified) | Done earlier (`copied_only: true`) |
| Promotion moves (Godot project to root, assets, staging, docs, tools) | **Done** by tools/migration/purge_unity.ps1 |
| Path rewrite (res://, autoloads, launchers, pipeline inputs/outputs) | **Done**: commit f9f4f32 |
| Clean .godot cache rebuild | **Done**: 10,624 imported resources, 0 import errors |
| Forward+ default renderer, Mobile fallback | **Done**: commit c3dc2b2 |
| Final preserved-data verification | **Failed**: 22,573 exact matches, 1 accepted regenerated JSON report, 5 screenshot mismatches out of 22,579 |
| Deletion of Game/, Builds/, Logs/, tools/Unity, tools/Build, tools/third_party/unity-cli and serialized Unity files | **Blocked by preservation gate**. Nothing has been deleted. |

## The accepted report exception

`TestResults/godot-smoke.json` → `staging/validation/godot-smoke.json`. The pre-purge
baseline smoke run (40/40 passed) regenerated this validation report after the
inventory was taken. It is regenerated test output, not original or authored data,
and the inventory was deliberately left unedited. The old file's content is gone; its
hash (2978b63f…) remains in staging/migration_audit/preservation_inventory.json.

This one identity is now accepted and recorded. The five screenshot mismatches
must be resolved against the original inventory before deletion can proceed.
No screenshot exception has been added.

The script is now resumable (completed moves are no-ops; missing Godot/ paths are
skipped) and skips tools/third_party in its serialized-file sweep.

## Validation of the promoted layout

| Check | Result |
|---|---|
| Python pipeline + reference tests | 6 + 31 passed |
| Godot smoke, real Vulkan Forward+ (default launcher) | 40/40, 0 log errors |
| Godot smoke, real Vulkan Mobile | 40/40, 0 log errors |
| Godot smoke, headless | 40/40 |
| Synthetic traversal, headless / Forward+ / Mobile | 143/143 each |
| Units landmark check (Phase 1) | Fails as documented in docs/behavior_ledger/units.md; not a regression |

## Temporary measures until deletion

- `Game/.gdignore`, `Builds/.gdignore`, `Logs/.gdignore` stop Godot from importing
  Unity content now that the project root contains those folders (untracked).
- `.gitignore` keeps a marked "Unity-era generated output" block; remove it with the
  folders.
- The legacy tools/reference/prepare_{frontend,mission1,phase1}_assets.py scripts
  only fed the Unity project. Their outputs now default to staging/legacy_unity_import.
  They are candidates for removal; prepare_phase1_assets.py still supplies the pinned
  converter digest to convert_characters.py.

## New layout

```text
project.godot, src/, scenes/       Godot project (tracked)
assets/                            Private converted assets (ignored)
staging/                           Raw extraction, validation, logs, reference outputs (ignored, .gdignore)
levels/<LEVEL>/trg_graph.json      Private derived trigger graphs (ignored)
tools/{pipeline,reference,migration}  Tracked tools; tools/third_party ignored
docs/                              Documentation, behavior ledger, gameplay references
```
