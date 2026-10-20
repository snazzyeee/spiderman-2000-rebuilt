# Handoff — 2026-10-21

## Current state and scope

Branch main; implementation HEAD at handoff preparation:
`85fba40d1725324c1dd99c2ff756b2a2fafdcd6a`. This handoff is the following
docs-only commit; `git rev-parse HEAD` identifies its final hash. Commits were
pushed normally, without force. Status was clean before this handoff edit.

This run completed section 0 and the original-font pipeline/theme milestone.
New sections were stopped to reserve context for the required handoff.
Sections 2–7 remain pending; do not infer their completion from font progress.

| Requested section | Result |
|---|---|
| 0 Sync / CI | Authenticated gh session works; prior and new Linux runs are green; README badge added |
| Gemini brief exception | Committed once, as-is, in ad207fd; no other owner/Gemini file edited |
| 1 Fonts | Six fonts decoded/exported, five sheets inventoried, Faithful/Enhanced resources, optional theme/options and tests implemented |
| 2 Texture enhancement | Not started; no upscaler or model downloaded |
| 3 Display / 4 Dynamic menu | Not started; current viewport/layout and existing menu flow retained |
| 5 Human transcript review | Not started; no Gemini proposal approved or promoted |
| 6 Landmark investigation | Not started; unchanged existing gate rerun and reported below |
| 7 Carryovers | Not started this run |
| 8 Docs | Font commands, controls, current menu status, licenses, ledger/index, CI badge and this handoff updated |
| 9 Final gate / handoff | Local matrix and runtime CI pass; known landmark failure and limitations retained |

Gemini brief SHA-256 before and after the as-is commit:
`89c62f79ae23ef4547d2103849ce280b861a67ee78089a7e44038fb1abb5ffb6`.
Never edit/delete docs/gemini/ or staging/gemini/. No staging/gemini input or
optional menu style guide was read or used by this milestone.

## Exact gates

Final local evidence: `staging/validation/regression/fonts-section-final/`.
Python: **47 reference + 11 pipeline = 58 passing**.

| Target | Smoke | Traversal | Fonts | Engine/script errors |
|---|---:|---:|---:|---:|
| Headless, forward_plus | 44/44 | 150/150 | 29/29 | 0 |
| Real Vulkan Forward+ | 44/44 | 150/150 | 29/29 | 0 |
| Real Vulkan Mobile | 44/44 | 150/150 | 29/29 | 0 |

Import passes in the final matrix. Real GPU logs identify the RX 580 2048SP.
All previous 42 smoke labels are retained; two original-font assertions are
added. Evidence: staging/validation/fonts/retained-smoke-checks.json.
The original 150 traversal checks and every existing gate stay in place.
No human playtest, new dynamic-menu suite, 1080p layout certification or steady
60 fps benchmark has been claimed.

CI is green for runtime commit 85fba40:
[run 39480489642](https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39480489642).
Downloaded reports: `staging/validation/ci-fonts-runtime/`. Linux CI passes all
58 Python tests, 150 traversal checks and 10 font variant/fallback checks.
Private-font loading/metrics and the 44-check private smoke suite are explicitly
skipped with reasons; they remain mandatory locally. The final docs-only
handoff push triggers another run, checked after pushing and reported in chat.

The owner's gh login resolved the old authentication blocker. Only the gh
session was used; no stored Git credential was accessed. Earlier runs for
98e5b52, the brief/badge and font-pipeline commits were also confirmed green.

```powershell
./tools/pipeline/run_regression.ps1 -Milestone next-section
./tools/pipeline/run_godot.ps1 -Headless -Script res://src/diagnostics/font_tests.gd
```

Regression now includes the font suite on all three targets. It verifies fresh
reports, existing renderer/Vulkan evidence and zero engine/script errors.

## Recovered font facts and authored presentation

New ledger: docs/behavior_ledger/fonts.md, added before implementation.
FNT is self-contained: count, sixteen-byte glyph records, sixteen-color palette
and aligned packed four-bit bitmaps. Width/height/baseline/slice widths are
bounded and checked; file accounting must be exact. The five BMP companions
are independently CRC-checked paletted authoring sheets; their per-glyph sheet
mapping remains unverified and is not guessed from names.

A bounded static read of the canonical fingerprinted executable recovered the
normal ASCII index map and nominal advances/space width. The decomp's
getCharIndex body is a stub despite its tag, so it was not used as proof.
Private static audits remain staging/reference/font-{charmap,width,constructor}-static.txt.
No original program or decomp build was executed, and no disassembly or raw
original-program data was copied into src/, tests or comments.

Faithful: decoded RGBA pixels in original and integer nearest-neighbor 4x
BMFont atlases. Enhanced: union boundaries traced independently per visible
palette color into layered COLR/CPAL TrueType outlines. Every layer's polygon
area must equal its original pixel count; all advances are checked exactly
against source metrics. Per-glyph trace failures fail conversion and are
recorded in the private manifest. Final conversion: six fonts, five sheets,
zero tracing failures, exact variant metrics. All derived fonts/art stay private.

Authored: scalable palette-outline presentation; theme/default sizes; UI-role
selection, Enhanced default, user-setting policy and filtering. Title/menu/HUD/
captions use sp_fnt00; subtitles use sp_fnt02. Shell font helper choices are
recovered, but exact original title/HUD/subtitle/button roles remain partly
unknown. Nominal metrics are preserved; original global scale is a separate
runtime setting and is not claimed as the remake's UI scale.

Options → Font variant selects Faithful / Enhanced, persists under video in
user://settings.cfg and applies live to registered controls. Missing private
fonts and unsupported characters retain the existing fallback. Tests restore
the user's settings file after checking persistence.

## Limitations and failed evidence retained

- The initial monochrome luminance-threshold experiment lost dark underline
  glyphs. It failed; the threshold was not lowered and no glyph was omitted.
  Palette-layer tracing replaces that approach and preserves all visible colors.
  The failed trial log and explanation remain private / in the ledger.
- Visual QA found font_big's generic map inappropriate for the mixed title/year:
  its specialized glyph binding remains unresolved. That bank's raw/outline
  exports support structural/metric diagnostics, not a claim of correct imagery
  for every generic codepoint. It is not used in the live theme. The title uses
  sp_fnt00 without guessing font_big indices. Other unused specialized mappings,
  accents and dynamic button prompts still require evidence.
- Palette high bits are retained in the manifest. Color-key transparency is
  preserved; original semi-transparency behavior beyond that remains partial.
- The first import of newly created font resources emitted two dummy-renderer
  texture errors. Failed evidence is in regression/fonts-pipeline/import.log.
  An unchanged import recheck and subsequent full matrices passed with zero
  errors. The cold-cache cause is unresolved; do not claim first-import
  cleanliness. Reproduce and fix it without suppressing engine errors.
- The original landmark gate was rerun unchanged and remains **19/21**:
  28/146 supported (19.18%) versus runner-up 1/146 (0.68%). The >=80% support
  and >=30-point margin criteria fail. TRG placement stays unverified.
  Current reports: units-fonts-final.log, units-landmark.json and
  units-fonts-analysis.log under staging/validation/. No spatial correction
  was applied and no new landmark subset/gate was substituted.

## Private outputs and regeneration

Prerequisites: canonical staging/raw_extracted/PC/app/data.pkr; the canonical
SpideyPC.exe only for bounded fingerprint-gated static audits; raw extracted
PSX/SFX/KAT/fonts, private assets/ and levels/*/trg_graph.json, existing audio
source WAV manifest/tree and exact native catalogs. Keep staging/migration_audit/
and the prior landmark evidence. Existing Unity deletion remains complete.

Use the pinned audio Python interpreter:

```powershell
./tools/third_party/audio-python/Scripts/python.exe tools/pipeline/setup_font_tools.py
./tools/third_party/audio-python/Scripts/python.exe tools/pipeline/convert_fonts.py
./tools/pipeline/run_godot.ps1 -Import
./tools/pipeline/run_regression.ps1 -Milestone fonts-section-final
```

prepare_godot.ps1 includes setup/conversion before import. Setup fetches only
version/hash-pinned wheels into tools/third_party/font-tools/: Pillow 11.3.0,
fontTools 4.60.1, Capstone 5.0.6. Licenses and SHA pins are in THIRD_PARTY.md and
the setup script. Conversion itself is entirely local/offline.

New private outputs:

- assets/ui/fonts/: six original/4x BMFont resources, palette-layer TTFs,
  source-sheet PNGs and font_manifest.json with source hashes, palette words,
  glyph metrics, generic mapping and variant advances.
- staging/validation/fonts/: comparison renders for each font at 1280x720,
  1920x1080 and 2560x1440, plus retained-smoke-label evidence. Comparison text
  is neutral test text; the original authoring sheets are exported separately.
- staging/validation/smoke/: menu/mission captures now show original fonts.
- staging/validation/regression/fonts-section-final/: exact final logs/reports;
  staging/validation/ci-fonts-runtime/: downloaded green CI evidence.

No original or derived art/font/audio/video/game text was committed. All public
fixtures are synthetic. No enhancement model or cloud service was used.

## Remaining sections and next three tasks

1. Reproduce the font cold-cache import errors and fix their cause, then start
   section 2: private texture inventory, pinned offline RX 580 upscaler/models,
   wrap/alpha processing, HD/hand-curated override provenance, runtime texture
   choice and VRAM/visual QA. No part of this enhancement pipeline exists yet.
2. Section 3: 1080p/adaptive UI, live persisted renderer-aware Video options and
   layout checks at the four requested resolutions. Current 1280x720 viewport
   and authored menu placement have not been changed by typography work.
3. Section 4: dynamic menu/transition/attract/gallery/costume/accessibility work
   with honest frame-time evidence. Then section 5's local human transcript
   review tool and approved-only registry merge, followed by the time-boxed
   restart/SCRIPTONLYBADDY investigation and ordered section 7 carryovers.

Carryovers still include alias/provenance import processing, remaining Level 1
asset conversion/collision, Theora cinematics, opcode ledger/MissionRuntime
logic-only/default-off integration, debug overlay, human playtest checklist and
InputRecorder/replayer. Audio XA/SFX identity binding was completed previously;
Gemini transcript proposals must never become verified without human decisions.
BATCH_AUDIO.md remains the existing audio workflow; no review-tool command is
advertised as implemented.

## Permissions and operational notes

Elevated execution was used for gh auth/status/run/download, normal Git
commit/push, public PyPI metadata and pinned setup downloads, process inspection
and Godot import/runtime matrices. No automatic-review rejection occurred this
run; the historical stored-credential action was not repeated.

Use run_regression.ps1 so TEMP/TMP point to staging/test_tmp. Close the editor
before import. Leave owner/Gemini files alone; the single authorized as-is
brief commit is complete. Keep every existing gate and distinguish the passing
warm-cache regression from unresolved cold-cache and landmark limitations.

## Current run: section 1 review (2026-10-21)

Owner briefs committed as-is in 11114a9. All Gemini WORKLOG session summaries
and T4 review read; T7/T8 produced no usable upscales. No Gemini output edited.
Findings 1, 2, 4 reproduced before fixes: dead weakrefs accumulate, null binding
raises a script error, save failure prevents a live update, unsupported setup
hosts reach extraction. Logs: staging/validation/review-reproduction/.
Findings 1/2 fixed with runtime regression tests; setup now rejects unsupported
OS, ABI and architecture before filesystem/network effects (synthetic tests).
The optional settings path allows failure tests without touching real settings.
A valid variant applies live even if persistence fails; return false and warning
report that failure, and malformed existing settings are never overwritten.
Finding 3 disproved as a metric assertion: all six exports explicitly map the
sample and space advance; original and outline advances match in Godot.
Added explicit export-map assertions while retaining the original metric gate.
This does not verify font_big imagery or its original specialized mapping.

Section 1 matrix: staging/validation/regression/review-fonts/summary.json:
47 reference + 13 pipeline Python tests pass; headless Forward+, real Vulkan
Forward+ and Mobile each pass smoke 44, traversal 150, fonts 39, zero engine or
script errors. Original gates retained. CI status is recorded after push.

Section 1 CI confirmed green for exact commit
479dcf83699b6e4e5669699b8a3f9b52048eccef:
https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39506502708.

## Current run: section 2 menu audio

Recovered global move/select/back IDs 0x29/0x1F/0x23 with bounded static
frontend scan (110 functions, 91 positive call sites). Evidence and authored
runtime policy were ledgered in frontend.md before src changes. Regenerate:
`tools/third_party/audio-python/Scripts/python.exe tools/reference/scan_frontend_sfx.py`.
Private output: staging/reference/frontend-sfx-static.json; deeper branch and
WAV-duration audit: staging/reference/frontend-branches-static.txt. Setup uses
the already pinned Capstone toolchain; no new tools/models downloaded.

Removed bank-order guesses; distinct confirm/back events; hover changes focus
only on a new enabled control; first focus stays silent; previous menu playback
stops before every distinct event. Reject absent, looping, empty or overlong
streams. Actual select/back durations are 1.367/1.111 s: both remain silent to
honor the owner's <=0.6 s rule. Move is 0.303333 s and plays via exact native ID.
No samples truncated or substituted. Synthetic tests cover the boundary,
non-overlap, loops, absent sounds, focus/hover and separate event IDs. Private
checks require select/back native resolution and duration rejection.
All original smoke labels retained, five mouse/native-ID/duration checks added.
Regression adds a menu-audio suite on all targets and Linux CI alongside all
existing suites; no gate relaxed.

Final section 2 local evidence: staging/validation/regression/menu-audio-section-final/.
Python: 50 reference + 14 pipeline = 64 passing.
Each of headless Forward+, real Vulkan Forward+ and real Vulkan Mobile:
49 smoke, 150 traversal, 39 fonts, 23 menu-audio checks, zero engine/script errors.
The first integration run failed because smoke callers still passed booleans;
named-event callers fixed and full matrix rerun. Failed evidence retained in
regression/menu-audio/. Direct Python invocations outside the runner hit sandbox
TEMP permissions; the configured runner's complete Python suites pass.

Approval review briefly failed due to a reviewer usage limit (no command ran,
not a safety determination). After the owner's resume, normal approval review
worked for regression and CI checks; no bypass used.
