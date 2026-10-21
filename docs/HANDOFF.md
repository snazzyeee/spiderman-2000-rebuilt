# Handoff — 2026-10-21

## HEAD, scope and commits

Branch: main. Implementation HEAD:
`0d18ee205d0a0278dd52f932a3f2526c7822c0c4`.
This handoff and the preliminary ledger audit are the following docs-only
commit; resolve its final hash with `git rev-parse HEAD`. Normal pushes only.
The final docs commit's CI result is checked after pushing and reported in chat.

| Section | Result this run |
|---|---|
| 0 Sync | Status/log/prior handoff read; owner briefs committed as-is; all Gemini WORKLOG summaries and T4 review read |
| 1 Review findings | Findings 1/2/4 reproduced then fixed; finding 3 disproved as a metric assertion; tests added |
| 2 Menu audio | Global move/select/back recovered, bank-order guesses removed, duration/non-overlap/hover policy and tests implemented |
| 3 Layout | Preliminary source audit only; both JSON proposals inspected, no complete schema/asset/claim validation or layout adopted |
| 4 Wireframes | Not started; no generated wireframes or overlays |
| 5 Frontend/pause/loading rebuild | Not started; current presentation retained apart from audio/focus fixes |
| 6 Texture/display | Not started; no upscaler/model download, Remastered font, HD tree or video-options work |
| 7 Transcript review | Not started; no proposal promoted or human decision created |
| 8 Campaign/costume research | Not started; no inferred title, objective or suit-name changes |
| 9 Carryovers | Not started; animation renders and all listed follow-ons remain pending |
| 10 Final gate | All-target regression passes for implemented scope; remaining requested work explicitly deferred |

Stopped starting sections to preserve context for this handoff, per owner
instruction. Sections 3–9 are pending, not certified or declared blocked.

- `11114a9 docs(gemini): add art and design briefs` — the three owner files as-is.
- `479dcf8 fix(fonts): guard bindings and preserve live switching on save failure`.
- `0d18ee2 fix(ui): recover menu sound ids and enforce bounded non-overlapping playback`.

No edits to staging/gemini/ or further edits to docs/gemini/. Gemini T7/T8
produced no usable enhancement assets: exact-size RGBA capability was blocked.
All texture/font enhancement remains Codex's section 6 responsibility.

## Final local and CI gates

Final local evidence: `staging/validation/regression/handoff-final/`.
Section 2 evidence: `staging/validation/regression/menu-audio-section-final/`.
Python: **50 reference + 14 pipeline = 64 passing**.

| Target | Smoke | Traversal | Fonts | Menu audio | Engine/script errors |
|---|---:|---:|---:|---:|---:|
| Headless Forward+ | 49/49 | 150/150 | 39/39 | 23/23 | 0 |
| Real Vulkan Forward+ | 49/49 | 150/150 | 39/39 | 23/23 | 0 |
| Real Vulkan Mobile | 49/49 | 150/150 | 39/39 | 23/23 | 0 |

RX 580 2048SP identified in real Vulkan logs. Import passes. All 44 prior smoke
labels, all 150 traversal checks and prior font assertions retained; five smoke
checks and a separate menu-audio suite added. No existing gate relaxed.

CI green on implementation HEAD 0d18ee2:
[run 39508563480](https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39508563480).
Private downloaded evidence: `staging/validation/ci-menu-audio-runtime/`.
Linux CI passes 64 Python, 150 traversal, 14 fallback/font-policy and 17 synthetic
menu-audio checks. Original-font metrics, private audio identities/durations and
full private smoke are explicitly skipped in CI; they pass locally as above.
Section 1 also green for exact 479dcf83699b6e4e5669699b8a3f9b52048eccef:
[run 39506502708](https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39506502708).

```powershell
./tools/pipeline/run_regression.ps1 -Milestone next-section
```

The runner sets TEMP/TMP to staging/test_tmp, validates fresh reports, renderer
and real Vulkan evidence, and rejects engine/script errors. Do not infer a
1080p design certificate, 60 fps benchmark or human playtest from these checks.

## Section 1: verified findings

Private test-first evidence: `staging/validation/review-reproduction/`.
1. Confirmed: repeated binding retained dead weakrefs; null binding raised a
   script error. Bind now prunes dead references and ignores invalid controls.
2. Confirmed: failed persistence aborted live switching. A valid variant now
   applies to live controls even on save failure; false return plus a warning
   reports persistence failure. Invalid existing configs are not overwritten.
   Optional settings-path injection tests failure without replacing user config.
3. Disproved as an exported-metric test: all six fonts explicitly map the
   sampled codepoints, including space advance, and both exports preserve them.
   Added mapping assertions without removing the metric gate. This does NOT
   verify font_big imagery or its original specialized character binding;
   font_big remains unused by the live theme.
4. Confirmed: setup had no runtime guard. Unsupported OS, Python ABI, machine
   architecture or pointer width now fail before filesystem/network effects.
   Synthetic tests cover non-Windows, Python 3.11/3.13, ARM64 and 32-bit cases.

These are authored runtime policies recorded in fonts.md, not new original-game
behavior. Existing Faithful/Enhanced resources, fallback and role choices remain.

## Section 2: recovered sounds and authored policy

Ledger: `docs/behavior_ledger/frontend.md`; exact joins: audio_ids.md.
Read-only, canonical fingerprint-gated scan of 110 named frontend functions
found 91 positive direct SFX call sites. Each function bounded by the next
names.json address, at most 65535 bytes; total scan at most 2 MiB.
Source: decomp f8a039248908e085f8fba8106769aea7f8f9de67. No original program ran.
Positive branch contexts establish move 0x29, select 0x1F, back 0x23. Cheat
accept/reject/exit and actor punch events are separately identified, never used
to guess generic selection. Jump-table gaps mean absence of decoded calls is
not proof of absence.

| Event | Native global ID | Private duration | Remake policy |
|---|---:|---:|---|
| Changed focus | 0x29 | 0.303333 s | Plays |
| Confirm | 0x1F | 1.367 s | Silent: exceeds owner's 0.6 s maximum |
| Back | 0x23 | 1.111 s | Silent: exceeds owner's 0.6 s maximum |

Removed menu.kat/000 and /001 guesses. Menu requests use Dialogue.play_sfx_id;
stop previous playback first; reject absent, looping, empty, non-finite or
long streams. No trimming or unrelated substitution. Initial focus and repeated
hover on the same enabled item are silent; mouse hover changes focus when needed.
Confirm and Back have separate events. Tests cover boundaries, overlap, hover,
loop rejection, native bindings and deliberate private-duration suppression.

Synthetic scanner tests use no original bytes; all private artifacts stay ignored.
Regenerate static inventory with the existing pinned Python/Capstone setup:

```powershell
./tools/third_party/audio-python/Scripts/python.exe tools/reference/scan_frontend_sfx.py
```

Output: `staging/reference/frontend-sfx-static.json`. Deeper manually inspected
branch/WAV audit: `staging/reference/frontend-branches-static.txt`; private
helper scripts remain beside it. Setup uses already pinned tools; no new
models or libraries were downloaded. Runtime/test code uses ledger facts only.

## Preliminary section 3 observations and owner decisions

No owner reference screenshots found at staging/reference/menu_original/.
Both Gemini JSON specs inspected: 12 frontend and 5 in-game screens. They are
still **Gemini proposal (unverified)**; the complete validation report, per-claim
audit and layout single source of truth are NOT implemented.

Independently checked generic CMenu centering, output-coordinate scaling,
control-column origins, title helper, normal/small font helpers, background,
legal wait and rude-word actor. Own-word facts/confidence now in frontend.md.
Significant corrections: title helper takes its Y from the caller; several
flow functions are TODO stubs; shell background references menubg, not proven
shell_bg equivalence; font helpers do not establish a subtitle role. Legal
body does not implement the proposal's any-button early skip. Constructor
placement for the rude-word actor is NotOk and does not prove main-menu staging.

No section 3 owner-question defaults implemented yet. Recommended future
switchable defaults from owner_questions.md (recommendations, not owner decisions):

| Question | Recommended default | Qualification |
|---|---|---|
| Enhanced side-fill | Centered 4:3 art with authored side wings; strict pillarbox alternative | Preserve original art geometry; no generated art yet |
| Cursor | Show on mouse movement, hide on keyboard/controller; always-visible alternative | Original cursor mapping still needs evidence |
| Cheats | Verified classic code entry with optional remembered toggles | Verify table/unlocks first; do not invent entries |
| Attract | Character/comic carousel, optional converted video | Owner's 60 s trigger overrides Gemini's 50 s; reduced-motion choice |
| Training | Phased rollout with current traversal practice available | Do not expose invented challenge lists or unlock rules |

## Failed gates, limitations and retained evidence

- Historical landmark gate remains **19/21**, 28/146 support (19.18%) versus
  runner-up 1/146 (0.68%); >=80% support and >=30-point margin still fail.
  Not reinvestigated in this run. No spatial correction or substitute gate.
  Prior evidence remains staging/validation/units-landmark.json and related logs.
- Prior cold-cache font import error remains unresolved; current imports/matrices
  pass, which does not certify the first import of a cleared cache.
- Initial menu integration failed because retained smoke callers still used
  boolean sound arguments; callers now use named events. Failed log remains
  regression/menu-audio/. Complete subsequent matrices pass.
- Direct Python tests outside the runner hit sandbox TEMP permissions. Those
  logs remain staging/validation/menu-audio-python-*.log. Complete configured
  runner suites pass; no fixture or gate weakened to work around the failure.
- Automatic approval review briefly failed due to reviewer usage limits; no
  rejected command executed and it was not a safety determination. After owner
  resume, normal approval review succeeded. No bypass or unresolved approval.

No new art, fonts, audio, video, game transcripts, raw functions or private
registry/graph data tracked. No Gemini proposal promoted to verified dialogue.
Historical fonts, private conversion and source requirements remain described
in fonts.md, BATCH_AUDIO.md, THIRD_PARTY.md and Git history of this handoff.

## Review outputs and remaining work

- Current menu smoke screenshots: `staging/validation/smoke/godot-menu.png`
  and other godot-*.png captures. Existing single-size presentation evidence,
  NOT the requested per-screen/style/resolution design QA matrix.
- New menu contact sheet, full screenshot matrix, private overlays and tracked
  wireframe index: **not generated**. Intended locations remain
  staging/validation/menu/ and docs/design/wireframes/index.html.
- Existing font comparison sheets: staging/validation/fonts/; private fonts:
  assets/ui/fonts/. Regenerate through setup_font_tools.py / convert_fonts.py.
- No HD textures, Remastered atlas, loading covers, transcript-review page,
  act graph, cheat table recovery or animation renders created this run.

Next three tasks, in requested section order:
1. Finish section 3: validate every proposal element/assets/evidence; audit all
   remaining decomp claims; build classic/enhanced screens.json with nonoverlapping
   actor/menu regions, verified labels only and explicit authored/proposal grades.
2. Section 4: generate all blueprint SVGs/index from that data; add bounds/overlap/
   SVG-completeness checks and honest overlays only where real screens exist.
3. Section 5: layout-driven frontend plus pause/loading, private bounded logo/
   cover conversion, style/reduced-motion/attract options and full objective QA.
   Then section 6 textures/fonts/display, 7 human transcript review, 8 campaign/
   cheats and all ordered section 9 carryovers. Do not jump to animation renders
   or edit staging/gemini/ except the explicitly authorized future renders folder.

## Current scoped run: sections 0–2

Synced clean at 83a6822. Section 1 committed 0fc26f7; CI green:
https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39518623846.
Original/Short/Off now persisted; recovered sounds play full length by default.
Final sound-mode gate: 50+14 Python; each target 49 smoke,150 traversal,39 fonts,
32 audio, zero engine/script errors. Headless audio tests allow mixer processing
before replacing/stopping streams; failed teardown evidence preserved.

Section 2: 27 frontend +21 in-game element claims audited in
 docs/design/claim_audit.md, raw schema/rect/asset report private at
 staging/validation/menu/proposal-audit.json (1 frontend and 8 in-game unresolved
 asset identities). All source-specific screen claims remain Gemini proposal
 unless the ledger explicitly recovers the helper property; authored rectangles
 do not promote original content. Layout data represents all 17 proposal screens
 plus training/audio/accessibility/attract (21 total), both styles, and isolates
 actor/menu regions. Unknown labels/unlocks are not adopted. Preferences have
 validated persisted switchable choices; UI integration follows in section 4.
Owner-question defaults and qualifications are recorded in frontend.md.
