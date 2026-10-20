# Unity purge and Godot promotion report (2026-10-20)

Phase 0 is **complete**. Godot remains at the repository root with Forward+ as
default and Mobile as fallback. No promotion moves or path rewrites were repeated.

## Preservation and deletion evidence

All 22,579 inventory entries were checked before deletion: 22,573 exact matches
and six explicit regenerated validation-output exceptions. The inventory remains
byte-for-byte unchanged (SHA-256
70f05392d00b6d67a52f4e0413dc07fd26b8690e490c37ef77738f119a90e892).
Each accepted record retains original/current SHA-256 and reason. The five PNGs
also passed bounded PNG CRC/decompression checks, 1280x720 viewport equality,
exact source/target inventory identity and exact smoke writer filename proof.
Synthetic tests reject an extra PNG, another source/target and invalid PNG bytes.

Private evidence: staging/migration_audit/capture_exception_proof.json,
final_verification.json and purge_operations.json. The removed list is exactly:
Game/, Builds/, Logs/, tools/Unity, tools/Build, tools/third_party/unity-cli.
No additional serialized-file removals were needed outside those Unity trees.
The pre-migration archive remains at origin/archive/unity-pre-migration
(8dfe6ddab0b508daa0a34de2a76f630b15c5a5db). Temporary .gdignore markers were
removed with their directories; the Unity-era .gitignore block was removed.

## Preventing recurrence

Smoke reports and captures now use staging/validation/smoke/. The launcher
checks all six output identities against every preservation target. Historical
capture/report destinations remain available for the private audit and are never
rewritten by smoke runs. Public assets, staging data, TRG graphs and third-party
tools remain ignored. No original game program was executed.

## Validation

The output-isolation and shared-converter-pin milestones each pass 39 Python
tests, 40 smoke checks and 143 traversal checks on headless, real Vulkan Forward+
and Mobile with zero engine/script errors. Import passes. Per-milestone evidence
is under staging/validation/regression/. The unchanged landmark gate remains a
separate 19/21 result; no claim of verified TRG geometry placement is made.

CONVERTER_SHA256 now lives in tools/reference/pinned_tools.py. The Unity-only
prepare_* tools were removed in a separate commit after the deletion milestone; their callers now use the shared pin and all regression gates pass.
