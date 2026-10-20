# Handoff — 2026-10-21

## Repository and scope

Branch main; latest implementation HEAD at handoff preparation:
`a3bdc28778e9a5d98c485ff021954a94202beb40`. This document is committed next.
Use `git rev-parse HEAD` for the final handoff commit. All task commits are
pushed normally; no force-push. No task changes should remain after handoff.

Unrelated untracked `docs/gemini/GEMINI_BRIEF.md` remains untouched, unread and
unstaged. Status therefore contains `?? docs/gemini/`; the requested completely
clean status is not claimed. Do not delete user material to manufacture it.

This run completed the editor-folder/CI infrastructure milestone and the sound
part of the offset investigation. New sections were stopped to preserve time
and context for the required handoff. Fonts, texture enhancement, presentation,
dynamic menu and stretch lighting have NOT been implemented.

| Current request | Result |
|---|---|
| 0 Sync / Unity purge | Prior deletion verified complete; no duplicate deletion or move |
| 0 Editor folders | Cause documented; self-contained editor paths; empty leftovers removed |
| 0 CI | Workflow pushed, downloads verified, source-only local import/traversal pass; remote run status blocked by authentication |
| 0.4 / 6 previous carryovers | Audio done previously; other outstanding work listed below |
| 1 Offset files | Bounded CRC-checked decoder and synthetic tests committed; spatial correction blocked by missing semantics |
| 2 Fonts | Not started |
| 3 Textures | Not started; no enhancement tools/models fetched |
| 4 Display / 5 Dynamic menu | Not started; existing menu and viewport retained |
| 7 Lighting | Not started |
| 8 Docs | Commands, tool licenses/pins, migration guide, ledger/index and this handoff updated for completed work |
| 9 Final gate | Local regression passes; landmark gate fails, remote CI unverified, unrelated untracked user folder remains |
| 10 Handoff | This document |

## Exact validation

Final matrix: `staging/validation/regression/offsets-final/summary.json` and
per-target logs/reports. Python: **41 reference + 11 pipeline = 52 passing**.

| Target | Smoke | Traversal | Engine/script errors |
|---|---:|---:|---:|
| Headless, forward_plus | 42/42 | 150/150 | 0 |
| Real Vulkan Forward+ | 42/42 | 150/150 | 0 |
| Real Vulkan Mobile | 42/42 | 150/150 | 0 |

Import passes. Real renderer logs identify Vulkan on AMD Radeon RX 580 2048SP.
All original 40 smoke and 143 traversal checks remain; no threshold was lowered.
No new menu suite, human playtest, 1080p performance capture or visual QA is claimed.

The source-only copy in `staging/ci-clean-room/` contains tracked source and the
pinned remake engine, without assets or private level graphs. Its Windows
headless import and 150/150 traversal passed with zero engine/script errors.
Evidence: `staging/validation/ci-{import,traversal}.log` and `ci-summary.json`.
This verifies missing private assets locally, not Linux/GitHub execution.

An initial direct Python test invocation hit the known sandbox TEMP permission
problem. The successful final matrix used TEMP/TMP=`staging/test_tmp` via the
regression launcher. The failed invocation is not reported as passing.

```powershell
./tools/pipeline/run_regression.ps1 -Milestone next-section
```

## CI status and tool pins

Workflow: [Synthetic regression](https://github.com/snazzyeee/spiderman-2000-revamp/actions/workflows/synthetic-regression.yml).
**CI run link unavailable; no green remote run has been observed.**
`gh run list` reports no authenticated session. An unauthenticated public API
request cannot see this private repository. Remote evidence is recorded in
`staging/validation/ci-remote-status.log`.

Automatic approval review rejected an attempt to repurpose Git's stored
credential for GitHub API access, calling it credential probing from an
unintended source. That action did not execute and was not retried or bypassed.
An authorized authenticated `gh` session is required to finish remote inspection;
then run `gh run list` and `gh run view <id> --log-failed`, fix any failure,
and check the final pushed commit. No secrets are required by the workflow.

The workflow runs Python synthetic fixtures and guarded Godot headless import /
all synthetic traversal suites on Ubuntu 24.04. It explicitly records the
42-check private-asset smoke suite as skipped, with a reason, in its JSON
summary. The full local private gate remains unchanged. Artifacts contain logs
and reports. Actions use immutable commit pins. The hosted Python/PowerShell
are used directly; there is no extra interpreter download. Python dependencies
have exact universal-wheel hashes; a complete `pip download --require-hashes`
succeeded. Download log: `staging/validation/ci-dependency-download.log`.

`tools/pipeline/setup_godot_linux.py` downloaded and verified the official Linux
4.7.2 archive, SHA-256
`cadd3204e728a35d3f13adb7fd0d7902636b79f6b95c40c265eb73b6c35329e4`.
It bounds the archive/binary and accepts only the expected single member.
Linux execution itself has not been tested on this Windows host. Tool/setup
pins and licenses are in `docs/THIRD_PARTY.md`.

## Unity purge and editor folders

The existing purge audit records exactly Game, Builds, Logs, tools/Unity,
tools/Build and tools/third_party/unity-cli. All six remain absent, tracked
Unity deletions were already committed, and no temporary Unity .gdignore or
Unity-era ignore block remains. Phase 0 is complete in PURGE_REPORT.md.

Prior preservation evidence: 22,579 entries, 22,573 exact matches plus six
accepted regenerated reports/captures. Immutable inventory SHA-256:
`70f05392d00b6d67a52f4e0413dc07fd26b8690e490c37ef77738f119a90e892`.
Private audits remain in staging/migration_audit/. Smoke writes only under
staging/validation/smoke/ and the launcher checks preservation collisions.

Godot 4.7.2's EditorPaths constructor creates the four observed subfolder names
relative to its working directory if changing to the host config/data path
fails. The launchers now put the supported `_sc_` marker beside the pinned
editor: editor data goes under the ignored installation's editor_data/.
All four verified-empty root folders were removed and did not recur through
both full matrices. Their exact fallback names are explicitly ignored.
Existing host editor settings were not moved or deleted. Details/source link:
GODOT_MIGRATION.md, “Editor paths and public CI”.

## Offset investigation and failing landmark gate

New ledger: `docs/behavior_ledger/offset_files.md`, indexed and linked from
units.md. Decoder: `tools/reference/decode_level_offsets.py`; three synthetic
tests cover signed field order, malformed/overflow/duplicate input, compression,
CRC, lengths and read budgets. Private report: `staging/reference/level-offsets.json`.

Nine archive members / 34 records passed CRC and bounded decoding. Each is a
count plus triples. The reference loader stores an integer key and two signed
short parameters, swapping their storage order relative to the text. All keys
fit both the matching G object and model ranges. There is no L1A1 member.
The consumer, units and spatial interpretation remain unknown; the available
decomp exposes loading/freeing but no consuming C++ body. Treating the triples
as XYZ would invent a transform. No geometry or TRG coordinate was modified.
L1A3/L1A4 with/without geometric-offset testing is blocked by that missing
interpretation; it was not run using an invented mapping.

The original L1A1 gate was rerun unchanged: **19/21**. Configured mapping is
best, but support is **28/146 (19.18%)** versus runner-up **1/146 (0.68%)**;
80% support and a 30-point margin both fail. Classification remains 82 outside
XZ geometry, 36 over geometry with unsupported gaps, 28 supported. TRG-derived
placement remains unverified. All seven former G-origin mismatches already
match O-bank objects; the offset files do not justify changing those origins.

```powershell
./tools/third_party/audio-python/Scripts/python.exe tools/reference/decode_level_offsets.py
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/verify_units.gd
# The unchanged landmark gate intentionally exits nonzero.
./tools/third_party/audio-python/Scripts/python.exe tools/reference/analyze_landmarks.py
```

## Recovered versus authored; existing carryovers

Recovered observations this run: CRC-checked offset grammar/field sizes/order,
archive coverage and key ranges. Loader semantics are partial (@NotOk), and
the signed-token helper's assertions conflict with negative input; the ledger
records this rather than claiming full recovery. Authored: decoder budgets,
fail-closed policy, CI evidence checks and editor installation isolation.
No new game mechanic, art/font transformation or gameplay tuning was added.

Previously completed: modular PlayerMotor/state resources, CameraRig/Director,
synthetic traversal and exact XA/SFX identity binding. Audio joins bind 901 XA
registry entries / 1,249 catalog identities and 1,993 SFX entries. Five KATs
are absent and one bank has an invalid sample index; they remain unresolved.
Level SFX aliases require an explicit/active bank. Missing registries fail cleanly.

Still outstanding from the previous request: animation alias/provenance import
postprocessor, materials ledger/visual QA, private Level 1 L1A2–L1A4 asset pipeline
and collision, Bink-to-Theora cinematic playback, opcode ledger/MissionRuntime
with logic-only harness/default-off integration, debug overlay, controls/human
playtest checklist expansion, InputRecorder/replayer and explicit-update goldens.
Existing authored wall attach, pendulum tuning, input assignments, probe/grace
rules, swing binding and meter calibration remain labeled authored in the ledger.
No unknown placement or mission rule has been promoted to recovered.

## Private inputs and regeneration

- `staging/raw_extracted/PC/app/{data.pkr,SpideyPC.exe}`; executable is static,
  bounded and fingerprint-gated only. Never run it.
- `staging/raw_extracted/Extracted/data/data/` including raw PSX/OFF/SFX/KAT/fonts.
- `assets/`, all private level graphs, native voice/SFX catalogs and audio source
  WAV manifest/tree. See GODOT_MIGRATION.md for exact audio rebuild commands.
- `staging/reference/{l1a1-regions.json,l1a1-o-regions.json,landmark_geometry/}`,
  original units reports, and migration preservation/purge audit.
- New: `staging/reference/level-offsets.json`, generated by the decoder command
  above; full matrix regenerated with run_regression.ps1 -Milestone <name>.
- `tools/third_party/` contains pinned Godot/converter/Python/FFmpeg/read-only
  decomp. New Linux Godot is regenerated by setup_godot_linux.py; dependency
  wheels by pip download --require-hashes -r tools/pipeline/ci-requirements.txt
  --dest tools/third_party/ci-wheels. No game data is needed by CI.

No original/derived game art, fonts, text, audio or video was committed. No
original executable/decomp build ran and no tools/functions/*.bin was vendored.

## Next three tasks

1. Provide an authorized `gh` login, observe the pushed workflow, fix any Linux
   failure without weakening gates, and record a green run URL for the final commit.
2. Resume requested section 2: decode original font metrics/sheets into a ledger,
   build private faithful/enhanced resources, integrate the theme/options and add
   synthetic decoder and original-font smoke checks with full renderer gates.
3. Proceed through texture enhancement, display options and dynamic menu in the
   requested order, with pinned offline tools, private QA and honest RX 580
   performance measurements. Carry over the remaining Phase 3/runtime/recorder
   work afterward; do not enable TRG placement while its gate fails.

## Permissions and operational notes

Elevated execution was used for public release/source/PyPI reads, pinned tool
and dependency downloads, Git index/commit/push, process inspection and full
Godot import/runtime matrices. The credential-reuse action was rejected by
automatic review as described above; no other approval was solicited.
Keep the editor closed before import. Set TEMP/TMP to staging/test_tmp (the
regression launcher does this). Do not count sandbox permission failures,
stale output, local source-only execution or an unobserved CI run as a pass.
