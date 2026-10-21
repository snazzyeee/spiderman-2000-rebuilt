# Handoff — scoped frontend run, 2026-10-21

## Current polish run (supersedes implementation status below)

Section 0 synced clean at 5df8e54. Section 1 is a passing regression foundation,
but its full visual acceptance is **incomplete**: the logo candidate still needs
background/occlusion correction and filled high-resolution reconstruction;
caption focus halftone/pop and exact menu-string width QA remain. Existing font
tests still check converted advances exactly. ASCII device hints are implemented,
not original glyph art. Classic uses an authored underline in addition to colour
to preserve the mandatory non-colour focus gate. No gate was dropped.

menubg **was found and independently bound**: one frame in shell.psx points to
texture index 88; the texture header at 327388 declares index 88. Classic now
uses it with the recovered virtual-frame mapping. See frontend ledger for exact
CRC/hash and regeneration. Other changes: centered Classic list, transparent
normal item styles, authored Enhanced skyline/caption plates, radial contact
shadow, last-device hints and rotating comic teaser. Full original Title is kept.

Section-1 gate: staging/validation/regression/polish-menubg/; Python **52 reference
+ 22 pipeline = 74**. Per headless / real Vulkan Forward+ / real Vulkan Mobile:
smoke **51/51/51**, traversal **150/150/150**, fonts **39/39/39**, menu audio
**32/32/32**, layout **58/58/58**, design **676/803/803**, loading **7/7/7**;
zero engine/script/shader errors. New tests cover synthetic animation metadata,
segmentation boundaries and correction-mask preservation, centering and hints.

Private review: assets/ui/frontend/title_logo_mask.png (existing masks win),
staging/validation/menu/logo-review/ (candidate alpha zooms/report),
staging/validation/menu/polish_before/ (preserved input captures),
staging/validation/menu/polish_before_after.png, refreshed screenshot/contact-sheet
and overlays folders. Tracked wireframes: docs/design/wireframes/index.html.
Regenerate resource with tools/reference/find_frontend_background.py, candidate
with tools/pipeline/extract_title_logo.py, and diagrams/overlays with
tools/design/render_wireframes.py --overlays. No media or Gemini files tracked.

The sections below describe the preceding run; this run's final section will
replace these provisional counts/HEAD when later milestones are complete.

## HEAD and scope

Branch main; implementation HEAD: `3ebb7e39f37025ca6ccd9471327df411a9365a5a`.
This handoff is the following docs-only commit; `git rev-parse HEAD` gives its
final hash. Normal pushes only. Final docs CI is checked after pushing and its
exact URL is reported in chat and saved privately with the downloaded reports.

| Section | Result |
|---|---|
| 0 Sync | Clean at 83a6822; ten commits, prior handoff and preliminary frontend ledger read |
| 1 Audio decision | Original/Short/Off persisted; recovered sounds play full length by default; no stacking retained |
| 2 Claim audit/layout | Both proposal schemas/normalized rects checked; all 48 element claims re-graded; 21 authored screen plans in two styles; unsupported content not adopted |
| 3 Wireframes | 42 SVGs/index generated from layout data, per-action focus arrows; 252 aligned 40% exact-SVG private overlays |
| 4 Frontend | Shared loader, original title/fonts/rig/sounds, style/preferences, staged animation/lighting/shadow, motion/attract and automated QA implemented; historical-fidelity gaps below |
| 5 Pause/loading | Shared layout pause and asynchronous original-cover loading; synthetic transition tests and retained cinematic locks; HUD unchanged |
| 6 Final gate | All local suites pass on all targets; implementation CI checked before final handoff push |

The result is a working authored frontend foundation with recovered components.
It is NOT a claim that every original page/rectangle/unlock has been recovered.
Unknown screens remain explicit proposal regions; full historical fidelity is
blocked on the evidence listed below, not silently substituted.

Commits this run:
- 0fc26f7: persistent Original/Short/Off menu sound modes.
- 1fd65c1: claim audit, layout data, preference storage and layout gates.
- 7a9918f: generated annotated blueprints and completeness/XML tests.
- c7c6dc9: responsive frontend, staged actor, private-art conversion and design QA.
- 3ebb7e3: shared pause/loading, strengthened final validation and artifacts.

No docs/gemini/ or staging/gemini/ files edited. No original program executed.
No original/derived media, transcripts, private graph/registry or raw functions
tracked. No new executable/model downloaded. Public fixtures are synthetic.

## Exact gates and CI

Final local evidence: `staging/validation/regression/scoped-handoff/`.
Python: **50 reference + 19 pipeline = 69 passing**. Generator/audit refinements
also rerun through the entire Python pipeline suite, with logs under
staging/validation/last-pipeline.log and final-audit-pipeline.log.

| Target | Smoke | Traversal | Fonts | Audio | Layout | Design | Loading | Engine/script errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Headless Forward+ | 51 | 150 | 39 | 32 | 58 | 670 | 7 | 0 |
| Real Vulkan Forward+ | 51 | 150 | 39 | 32 | 58 | 797 | 7 | 0 |
| Real Vulkan Mobile | 51 | 150 | 39 | 32 | 58 | 797 | 7 | 0 |

All prior smoke labels, traversal/font/audio assertions retained. Duration-cap
assertions now explicitly run in Short mode, matching the owner's revised rule.
Smoke completeness strengthened to 51. Layout/design/loading suites added
alongside old gates, on all targets and headless Linux CI. Import passes.

Real Vulkan logs identify AMD Radeon RX 580 2048SP. At 1920x1080, after 60 warm-up
frames, 180 uncapped rendered-frame wall-clock samples give 95th percentile
Forward+ **1.075 ms**, Mobile **1.013 ms**, against <=16.667 ms. This is a local
menu benchmark, not a gameplay or whole-system latency claim.

Implementation CI: https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39522540911.
Downloaded evidence: staging/validation/ci-scoped-runtime/. Linux passes
69 Python, 150 traversal, 14 font, 24 audio, 58 layout, 665 design and 6 loading
checks, with the explicit private-asset skips recorded in ci-summary.json.
Earlier milestones green:
- https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39518623846 (audio)
- https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39518993322 (layout)
- https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39519449596 (wireframes)
- https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39520424073 (frontend)

CI has no private assets: private smoke/fonts/native audio/actor/cover checks
are explicitly skipped there. Local real/private checks above remain mandatory.
CI exact counts and skips are in the downloaded ci-summary.json; no skip is
presented as a private-asset pass.

```powershell
./tools/pipeline/run_regression.ps1 -Milestone next-section
```

## Recovered versus authored

Ledger: docs/behavior_ledger/frontend.md. Claim matrix:
docs/design/claim_audit.md. Private proposal audit:
staging/validation/menu/proposal-audit.json.

Both Gemini schemas and all normalized rectangles pass structural validation.
Frontend assets resolve either as files or exact CRC-verified archive members;
8 in-game symbolic assets remain unresolved as usable paths. A cited helper
proves only that helper, not a screen's rectangle/list/flow. All unproven
screen-specific claims stay Gemini proposal (unverified). No owner screenshots
were present at staging/reference/menu_original/.

Recovered: 512x240 virtual coordinate mapping; generic menu centering; font
helpers and selected palette facts; native move/select/back/reject IDs;
frontend actor cycle 0/100, punch cue/family; original source asset identity/CRC.

Authored: screen rectangles/working remake branch flow; normal/small UI roles;
header crop; three lights, camera, ground/contact shadow, rig-only breathing,
parallax/ink backdrop and focus border; transitions, preferences and carousel.
The model and original animation slots are preserved; slot names, not array
ordinals, select animations. Actor reactions continue across compatible pages.

Original sounds: 0x29 move (0.303333 s), 0x1F confirm (1.367 s), 0x23 back
(1.111 s), 0x1B reject. Original mode plays recovered clips at full length;
Short caps all at 0.6 s; Off stops/plays none. Unknown, looping, empty or
non-finite streams are rejected; every distinct event stops previous SFX.
Initial focus and repeated hover stay silent. No bank-order guesses or trimming.

Title/Legal art uses the recovered virtual raster mapped to a 4:3 display frame,
not its raw 512x240 pixel aspect. Full Title remains intact; a transparent input
target uses its baked prompt. The authored main header is a separate crop of
original title lettering. Decoded logo.rle is the Neversoft emblem, not the
game logo, and is not substituted. Six BMP level covers exist (1,2,4,5,7,8);
no cover fabricated for 3/6. The current L1A1 loading path uses L1Cov.

## Owner-question defaults taken

All have persisted switchable choices under Options > Accessibility:

| Decision | Default / alternative | Qualification |
|---|---|---|
| Widescreen fill | Authored Wings / Black 4:3 frame | Original art geometry retained; exact menubg association pending |
| Cursor | Hybrid / Visible | Uses OS pointer; original cursor art not yet verified |
| Cheat persistence | On / Off | Preference stored; no cheat activation until verified table recovery |
| Attract media | Character/comic Carousel / Comics | 60 s owner timeout, immediate wake; no unverified biographies/FMVs |
| Training scope | Phased / Practice | Only the existing converted practice slice is functional; no invented challenges |

Options also offers Classic/Enhanced (Enhanced default), Faithful/Enhanced
fonts, audio levels and Original/Short/Off menu sounds. Reduced motion restores
the rig's rest scale/position, stops animation/parallax and uses 0.1 s fades;
normal transitions are 0.25 s and skippable. No owner confirmation was required.

## Review outputs and regeneration

- Tracked blueprint index: `docs/design/wireframes/index.html` (42 SVGs, no original art).
- Private screenshots: `staging/validation/menu/<screen>_<style>_<renderer>_<resolution>.png`.
  21 screens x 2 styles x 3 resolutions x 2 real renderers = **252 captures**.
- Private contact sheets: `staging/validation/menu/contact-sheet-<renderer>-<resolution>.png`.
  Six sheets; the Forward+ 1920x1080 sheet is the convenient starting point.
- Exact-SVG 40% overlays: `staging/validation/menu/overlays/`, **252** outputs;
  Classic is aligned to its centered 4:3 frame, not stretched across pillarboxes.
  The overlay manifest also validates each capture's actual requested dimensions.
- In-context mission pause: `staging/validation/smoke/godot-pause.png` (last real target).
- Art/hash/CRC manifests: `assets/ui/frontend/manifest.json`; inputs/audits under
  staging/reference/frontend-art/ and staging/validation/frontend-art.log.

```powershell
./tools/third_party/audio-python/Scripts/python.exe tools/pipeline/prepare_frontend_art.py
./tools/third_party/audio-python/Scripts/python.exe tools/design/layout.py --audit
./tools/third_party/audio-python/Scripts/python.exe tools/design/render_wireframes.py --overlays
```

Art conversion requires the canonical private data.pkr, existing SHA-pinned
MIT multitool and pinned font Python dependencies. Inputs <=4 MiB/member,
CRC/length checked before conversion, decoder timeout 30 s, output dimensions
<=4096 and <=16 Mi pixels. All outputs remain private. The already bundled
runtime 26.1015.12040 supplies Sharp 0.35.5 for SVG rasterization/contact sheets;
no download. See THIRD_PARTY.md. Run regression before regenerating overlays
so captures and layout refer to the same implementation.

## Failing fidelity gates, limitations and failed evidence

- Exact original page flow and screen-specific rectangles/unlocks remain
  unverified: several cited shell functions are TODO stubs, generic helpers
  do not establish their callers, and owner reference screenshots are absent.
  Unsupported screens show proposal regions rather than invented game content.
- Shell_DrawBackground references menubg. A verified converted texture binding
  is still missing. The old stretched shell_bg guess is removed; present
  dark/halftone backdrop is authored. Classic is not certified pixel-faithful.
- The contrast gate uses specified foreground colours against actual captured
  backdrop pixels; original palette outline/antialiasing and baked image text
  are outside that measurement. Full original-art typography contrast is not
  certified. Caption/subtitle role recovery remains partial as in fonts.md.
- Remembered cheats and phased challenge rollout have settings but cannot
  activate unverified code/unlock tables or unconverted levels. Original cursor
  art and extended original branches remain pending evidence/conversion.
- Initial QA caught compact control overflow and detached-view resize callbacks;
  fixed without lowering bounds/safe-area thresholds. Initial contrast sampling
  hit focus borders; now samples the actual text backdrop inside borders.
- Initial audio teardown retained headless mixer resources; tests now let mixer
  updates drain. Initial loading diagnostic compiled a dependency before the
  autoload name existed; runtime node lookup fixes it. Failed logs remain under
  staging/validation/ (sound-modes*, design-*, loading-first.log). No errors hidden.
- Historical landmark gate remains 19/21: 28/146 support (19.18%) versus
  runner-up 1/146 (0.68%). Both >=80% support and >=30-point margin fail;
  not reinvestigated. Original thresholds unchanged.
- Prior cold-cache font import errors remain unresolved. Current imports/matrices
  pass; no clean-first-import claim. No human playtest or owner visual approval.

No requested downstream texture/audio-research/import/gameplay milestone was
started in this scoped run. No automatic approval-review rejection occurred.

## Remaining queue for the next run

Resolve the documented frontend fidelity gaps when evidence becomes available;
do not mistake authored placeholders for recovered functionality. The owner's
remaining queue, in order:
1. Textures + Remastered font + 1080p display options.
2. Human transcript review tool.
3. Gemini research checks: act graph and cheat/suit table.
4. Animation renders for Gemini T6.
5. Landmark RESTART investigation (gate unchanged).
6. Animation alias/provenance import post-processor.
7. L1A2–L1A4 conversion/collision.
8. Bink to Theora cinematics.
9. Opcode ledger + default-off logic-only MissionRuntime.
10. Debug overlay.
11. Controls and human playtest docs.
12. InputRecorder/replayer with explicit --update-goldens.
