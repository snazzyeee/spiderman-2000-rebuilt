# Unity purge and Godot promotion report (2026-10-20)

## Outcome

| Step | Status |
|---|---|
| Preservation copy (11,526 files, SHA-256 verified) | Done earlier (`copied_only: true`) |
| Promotion moves (Godot project to root, assets, staging, docs, tools) | **Done** by tools/migration/purge_unity.ps1 |
| Path rewrite (res://, autoloads, launchers, pipeline inputs/outputs) | **Done**: commit f9f4f32 |
| Clean .godot cache rebuild | **Done**: 10,624 imported resources, 0 import errors |
| Forward+ default renderer, Mobile fallback | **Done**: commit c3dc2b2 |
| Final preserved-data verification | **Stopped**: 22,578 of 22,579 entries match; 1 mismatch (below) |
| Deletion of Game/, Builds/, Logs/, tools/Unity, tools/Build, tools/third_party/unity-cli and serialized Unity files | **Pending your decision**. Nothing has been deleted. |

## The one verification mismatch

`TestResults/godot-smoke.json` → `staging/validation/godot-smoke.json`. The pre-purge
baseline smoke run (40/40 passed) regenerated this validation report after the
inventory was taken. It is regenerated test output, not original or authored data,
but the guard refuses to delete Unity files while any inventory entry differs, and
the inventory was deliberately left unedited. The old file's content is gone; its
hash (2978b63f…) remains in staging/migration_audit/preservation_inventory.json.

To finish, choose one:

1. Approve an explicit, recorded exception for this one regenerated report. The
   verifier would list it as accepted in final_verification.json, without editing the
   inventory. Then the purge runs.
2. Run `tools/migration/purge_unity.ps1` yourself after resolving the entry as you see fit.

The script is now resumable (completed moves are no-ops; missing Godot/ paths are
skipped) and skips tools/third_party in its serialized-file sweep.

## Validation of the promoted layout

| Check | Result |
|---|---|
| Python pipeline + reference tests | 6 + 27 passed (23 pre-existing + 10 new synthetic) |
| Godot smoke, real Vulkan Forward+ (default launcher) | 40/40, 0 log errors |
| Godot smoke, real Vulkan Mobile | 40/40, 0 log errors |
| Godot smoke, headless | 40/40 |
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
