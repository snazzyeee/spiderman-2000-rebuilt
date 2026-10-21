# Handoff — polish, texture/font/video run, 2026-10-21

## Validated HEAD and scope

Implementation HEAD: `e020d46fc24a8459d910480ada40d7c4ab0f90ef` on main.
This handoff follows as a docs-only commit; final HEAD and its checked CI URL
are recorded in `staging/validation/final-run.json` and reported in chat.
Implementation CI is green:
https://github.com/snazzyeee/spiderman-2000-revamp/actions/runs/39540814070.
Earlier milestones also passed CI: f238787 / run 39538061382 and dfdda24 /
run 39539724190. All pushes were normal fast-forwards to main.

Section 0 synced clean at 5df8e54 and read the requested handoff/menu/ledger.
Section 1 shipped a verified resource binding and a polish foundation, with
visual acceptance gaps below. Section 2 shipped the offline pipeline, initial
59-image batch, Remastered font and live video options. Section 3 (transcript
review) was **not started**; section 4 research/renders was not started. Stopped
starting sections to preserve context for this handoff, as requested. This is
not a claim that the whole requested run is complete.

Commits: f238787 frontend/resource polish; dfdda24 offline enhancement/video;
e020d46 physical capture and 16:10 correction. No Gemini files edited, no
original program executed, no original/derived media or game text committed.
All original assets remain private; public fixtures are synthetic.

## Exact final gate

`tools/pipeline/run_regression.ps1 -Milestone physical-capture-final`
Evidence: `staging/validation/regression/physical-capture-final/`.
Python: **52 reference + 26 pipeline = 78 passing**.

| Target | Smoke | Traversal | Fonts | Menu audio | Layout/video | Design | Loading | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Headless Forward+ | 51 | 150 | 46 | 32 | 90 | 1039 | 7 | 0 |
| Real Vulkan Forward+ | 51 | 150 | 46 | 32 | 90 | 1393 | 7 | 0 |
| Real Vulkan Mobile | 51 | 150 | 46 | 32 | 90 | 1393 | 7 | 0 |

Real device: AMD Radeon RX 580 2048SP. Default 1080p menu frame-interval P95:
Forward+ **1.082 ms**, Mobile **0.960 ms**. Godot-reported video memory:
**94,349,696 / 89,387,088 bytes** respectively. Conservative all-original plus
first-batch enhanced RGBA8+mipmap estimate: **292,334,680 bytes**, below 4 GiB.
These measurements exclude some driver/OS allocations; they do not certify
worst-case residency or performance with every optional effect enabled.

All retained assertions remain, including five menu entries/order, keyboard,
mouse, traversal, font advances, Original/Short/Off audio/no-stacking, safe areas,
contrast >=4.5, non-colour focus and frame interval. New checks add correctable
mask handling, bounded PSX metadata, tool archive guards, cache identity, font
cell scaling, live/persisted renderer options, texture priority and rig pixel
joins, main-caption advances and actual capture sizes at every real target.

## Recovered versus authored

**menubg was found and independently bound.** CRC-checked shell.psx animation
metadata has one frame referencing texture index 88; the texture header at
327388 independently declares index 88, 512x512. Classic uses that raster with
the recovered 512x240 virtual draw and 4:3 presentation. It is a static frame,
not a moving background. See frontend.md for hashes/evidence and parser bounds.

Recovered components: original private title image, menu/fonts and their
advances, rig and numeric animation slots, comic art, SFX IDs, CMenu centring
formula, menubg binding and previously verified loading-cover resources.
Authored: caller line separation (16 virtual pixels), page rectangles/unknown
page placeholders, Enhanced skyline/halftone, caption ink/skew/shadow, radial
contact shadow, device hint labels, teaser rotation, animation staging/motion,
texture enhancement/category choices, font variant and modern video settings.
No unverified original strings/unlock rules or Gemini layout claims adopted.

Classic main list is centred within 2 physical pixels on all tested sizes and
normal items are transparent text. Its authored focus underline retains the
mandatory non-colour gate; this deviates from the requested colour-only fallback.
Other Classic focus boxes have a thinner top border and keep their left marker.
Enhanced uses ink caption plates; no extra theme tracking is introduced. Main
caption advances match converter metrics within 2 pixels. The grey ground mesh
is replaced with an unlit radial-alpha shadow; existing punch cue is retained.

## Open acceptance gates and incomplete work

- **Logo remains incomplete.** The private automatic mask removes visible colour
  regions but does not certify absence of every background/hand-outline pixel.
  The hand occludes lettering. Existing correction masks always win on rerun.
  Candidate is not installed: the old cropped banner remains live. High-res
  alpha review rasters and a traced alpha SVG exist; sampled-colour filled
  high-res reconstruction is not implemented. The existing font tracer is an
  in-repo contour tracer; there was no pinned Potrace installation to reuse.
- Enhanced focus halftone/scale pop and larger actor framing remain. Device
  hints switch ASCII labels by last-used device, not recovered glyph artwork.
- First enhancement batch is 59 images: menu/comics, eight rig textures and six
  original font atlases. The rest of the inventoried library is not enhanced;
  use --all explicitly. Original-only override list currently has no exclusions.
  Category/art/face/glyph-edge review is outstanding. Packed atlas inference can
  affect gutters even though cell coordinates and advances are exact.
- Fullscreen uses the desktop mode. The resolution selector is disabled there;
  it applies live to Windowed/Borderless. Exclusive display-mode switching is
  not implemented. FSR2 reduced scaling, TAA, SSAO and volumetric fog are limited
  to Forward+; Mobile offers its supported choices. No HUD rewrite performed.
- Historical exact main/page flow, highlight visuals and unsupported HUD/comic/
  death proposals remain unverified as described in the existing frontend ledger.
- Transcript review UI/decision merge is still needed. No human approvals were
  created or inferred; Gemini proposals remain unverified.

Failed intermediate evidence is retained. An initial native-window startup
change disturbed keyboard focus; unnecessary native calls were removed and the
unchanged smoke checks pass. A ZIP fixture initially normalized a malicious
name; corrected raw-name/link fixtures pass. Overlay validation then rejected
nominal 1920x1200 files that were actually 1920x1080: root aspect expansion and
an explicit capture-size gate fixed that. True 16:10 exposed six border samples
below 4.5. The UI border was thinned and the original contrast point/threshold
retained. Automatic approval review rejected an attempted sampling-point change
as a possible gate bypass; that change was removed. The safer rendering fix and
full final matrix passed. No approval or gate relaxation is pending.

## Owner review and regeneration

- Logo mask: `assets/ui/frontend/title_logo_mask.png`; candidate PNG/alpha SVG
  beside it. Alpha 4x/6x zooms and report: `staging/validation/menu/logo-review/`.
  Run `tools/pipeline/extract_title_logo.py`; existing masks are never replaced.
- Before/after: `staging/validation/menu/polish_before_after.png`; preserved
  initial 1080p captures under `polish_before/`.
- Screenshots: `staging/validation/menu/<screen>_<style>_<renderer>_<size>.png`.
  **352** real captures: 22 screens x two styles x four sizes x two renderers.
  Eight `contact-sheet-<renderer>-<size>.png` files in that folder.
- **352 exact-SVG 40% overlays**: `staging/validation/menu/overlays/`; manifest
  checks physical dimensions. Tracked **44** diagrams and review index:
  `docs/design/wireframes/index.html`.
- Texture sheets: `staging/validation/textures/illustration-contact-sheet.png`
  and `general-contact-sheet.png`; inventory.json and vram.json beside them.
- `find_frontend_background.py` CRC-extracts, bounds/parses and decodes menubg
  through the hash-pinned tool; private evidence in reference/frontend-background.
- `setup_texture_tools.py` installs waifu2x 20250915 with published archive SHA
  and verifies installed models/binaries. `enhance_textures.py` runs offline;
  --all processes remaining PNGs; repeat --tiling / --scissor with exact
  assets-relative names to opt into wrap padding / binary alpha. Originals and
  `assets/hd_overrides/` are preserved. Cache: staging/cache/textures; generated
  images/manifest: assets/hd. Remastered is built from convert_fonts.py output
  metrics and sits beside existing private fonts. License/pin: THIRD_PARTY.md.
- Run the regression above before `render_wireframes.py --overlays` so diagrams,
  private captures, sheets and overlays correspond to current implementation.

## Decisions/defaults retained

Enhanced menu default; Classic switchable. Original menu sounds default, Short
caps all at 0.6 s, Off silences; no stacking. Original texture bypasses generated
and curated art; Enhanced (default) chooses curated then generated then original.
Faithful/Enhanced/Remastered fonts selectable. Wings/Black side fill, Hybrid/
Visible cursor, Carousel/Comics attract, reduced motion, and Phased/Practice
training choices persist. Cheat persistence is only a stored preference until
verified cheat-table recovery. Attract timeout remains 60 s, transitions 0.25 s
or 0.1 s reduced-motion and skippable. Original cursor artwork is unverified.

## Next run, in order

First close this run's incomplete work: logo mask/filled reconstruction and
remaining caption/actor polish; remaining texture batch and art/glyph quality
review; then build the transcript review tool and human-approved-only merge
(section 3 of the current request). These are the next three tasks.

Then the carried queue: research checks (68-TRG act graph, cheat/suit table);
300 animation renders for Gemini T6; landmark RESTART investigation; animation
alias/provenance import post-processor; L1A2-L1A4 conversion/collision;
Bink-to-Theora; opcode ledger and default-off logic-only MissionRuntime;
debug overlay; Controls/playtest documentation; InputRecorder/replayer with
explicit --update-goldens. No optional section-4 output was generated in this run.
