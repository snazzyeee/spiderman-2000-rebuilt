# Gemini art-enhancement brief (T7 fonts and title art, T8 character textures)

This brief extends [GEMINI_BRIEF.md](GEMINI_BRIEF.md). **All hard rules in section 1 of that
brief still apply**: write only inside `staging/gemini/`, no git write commands, no Godot, no
scripts or installers, never fabricate, log everything in `staging/gemini/WORKLOG.md` using the
same entry format. Read both files fully before starting.

Do T7, then T8. T6 (animation labeling) comes after these and has its own instructions in
GEMINI_BRIEF.md.

---

## 1. What this is for

The remake uses the original game's low-resolution art: bitmap fonts, a 512x240 title
illustration, and 732 character textures, mostly 32–128 px. You produce **4x enhanced
redraws** that stay faithful to the originals. Another agent later checks each output
automatically (size, transparency, layout and colour against the source) and the owner
approves it before anything enters the game. Your outputs are proposals, never final.

## 2. Capability check (do this first)

These tasks need an image model that can **take an image as input and return an edited image**
at a size you control. Try it on one small item: the first row of `T8_textures/targets.csv`.

- If you cannot produce image files, or cannot control the output size: log
  `T7/T8 blocked: no image editing capability (details)`, write the session summary, and stop.
- Record in WORKLOG.md which image model/tool you used. Use the same one for the whole task.

## 3. Image rules (apply to every output)

1. **Exact size.** Output width and height are exactly `out_width` x `out_height` from the
   targets file (4x the source). If your tool cannot hit the exact size, produce a larger
   image with the same aspect ratio and say so in the manifest; never crop or pad
   differently.
2. **Same layout.** Every feature stays at the same relative position. These images are UV
   texture atlases and glyph sheets: a seam, eye, logo stripe or glyph that moves by even a
   few source pixels breaks the 3D model or the font. Do not rotate, recenter, reframe, zoom,
   mirror or "complete" partially visible shapes.
3. **Same transparency.** Every source has an alpha channel. Output a PNG with alpha.
   Transparent source regions stay transparent; opaque regions stay opaque. Only edges may
   become smoother (anti-aliased within about one source pixel). Never fill transparent
   areas with a background.
4. **Same colours.** Keep hues, saturation and overall brightness. No colour grading, no
   new lighting, no added shading direction that conflicts with the original, no
   desaturation.
5. **Same design.** Sharpen and refine what is there: cleaner lines, smoother gradients,
   plausible fine detail such as fabric weave, web lines that are already drawn, or brick
   texture already implied. Do not add new elements: no extra costume details, logos,
   emblems, text, signatures, watermarks, frames or realism that changes the comic style.
6. **Edges and seams.** Pixels along each texture's border meet other textures on the
   model. Keep border colours continuous with the source border.
7. **Consistency.** Within one model, treat every texture the same way: same sharpness,
   same detail level, same palette handling. Finish one model before starting the next.
8. **Refusals.** If the tool declines an image (for example because of trademarked
   characters or logos), record `declined` with the tool's reason and move on. Do not reword
   prompts to get around a refusal.
9. **Self-check.** After each output, view source and output side by side and fill in the
   manifest check columns honestly. If any check fails, retry at most twice, then record
   `failed` with what went wrong.

## 4. T7: Fonts and title art

**Targets:** `staging/gemini/T7_fonts/targets.csv` (prepared for you). Columns:
`item, kind, source, metadata, width, height, has_alpha, scale, out_width, out_height,
glyphs, output`.

### Font atlases (kind = font_atlas): sp_fnt00, sp_fnt01, sp_fnt02, sp_fnt03, font_big
- `source` is the original glyph atlas (e.g. `assets/ui/fonts/sp_fnt00_original.png`).
- `metadata` is its BMFont file. Each `char` line gives the glyph's cell:
  `id` (character code), `x y width height` (cell in source pixels) and offsets/advance.
  At 4x, a glyph's cell is `4x, 4y, 4*width, 4*height` in your output.
- **Every glyph must stay inside its own 4x cell**, keeping the same baseline position and
  the same proportions. Pixels outside all cells stay fully transparent.
- Keep each font's style: its outline colour and thickness (some fonts have a coloured
  outline or palette-based edge), weight, slant and letter shapes. Smoother curves are the
  goal, not a new typeface.
- Use the existing `*_faithful.png` (nearest-neighbour 4x of the same atlas) as a
  layout reference: your output must line up with it glyph for glyph.
- Output: `staging/gemini/T7_fonts/out/<font>_gemini_4x.png`.

### Title illustration (kind = title_art): assets/ui/Title.png
This is the image shown at launch: the jagged "SPIDER-MAN" logo, Spider-Man swinging
between buildings, and "PRESS ENTER". (The logo is part of this illustration, not a font.)
Produce up to three files:
1. `Title_gemini_4x.png`: the whole illustration at 2048x960, same composition.
2. `Title_logo_gemini.png` (optional): only the "SPIDER-MAN" lettering, cut out on a
   transparent background, at the highest resolution your tool supports, with the same
   letter shapes, jagged edges, colours and outline.
3. `Title_press_enter_gemini.png` (optional): only "PRESS ENTER" on transparency.

If the tool declines the logo or the character, record it (rule 8) and continue.

### T7 manifest: `staging/gemini/T7_fonts/manifest.csv`
```text
item,output,status,out_width,out_height,size_ok,alpha_ok,layout_ok,colour_ok,style_ok,model_used,notes
```
`status` is one of `done`, `declined`, `failed`, `blocked`. The `*_ok` columns are
`yes`/`no` from your side-by-side self-check. In `notes`, list glyphs or regions that look
wrong.

## 5. T8: Character and costume textures

**Targets:** `staging/gemini/T8_textures/targets.csv` (prepared for you). Columns:
`priority, model, source, width, height, has_alpha, scale, out_width, out_height, output`.
732 textures across 64 models, ordered by priority:

| Priority | Models | Textures |
|---|---|---|
| 1 | Spider-Man and every costume (spidey, sp2099, sparmor, spbagman, spquick, spreilly, spscar, spsymbi, spuniv, spunlim, sppark, sp_alt01/02/04/05) | 151 |
| 2 | Bosses (scorpion, rhino, venom, venom2, carnage, docock, ock_suit, mysterio, superock, lizard) | 133 |
| 3 | Named allies and NPCs (blackcat, jameson, mj, punisher, daredevl, captain, torch, hostages, parker, brock, mariner, bc2, jjjj) | 190 |
| 4 | Everything else (thugs, police, swat, lizard men, symbiotes, viewers, effects) | 258 |

Do not process `assets/characters/lowres/` or any `source_archive` folder.

**Method**
1. Work model by model in table order. Before redrawing a model's textures, view all of its
   source textures together so you understand which body parts they cover (face, chest,
   arms, legs, emblem). Note this in the WORKLOG entry.
2. Many textures are tiny (32x32 or 64x64) pieces of a UV layout. Treat them as flat
   texture pieces, not as pictures to compose. Sharpen and refine; never reinterpret.
3. Costume variants share Spider-Man's layout. Keep each costume's own colours and details
   exactly (e.g. Scarlet Spider's hoodie, 2099's markings, Symbiote's black). Do not copy
   one costume's details onto another.
4. Write each output to its `output` path (create `out/<model>/` folders as needed).
5. Resuming: skip any target that already has a manifest row with `done`, `declined` or
   `failed`, so an interrupted session continues where it stopped.
6. Per session, finish complete models only. Stop after about 150 images or when your
   limits approach, write the session summary, and leave the rest for the next session.

### T8 manifest: `staging/gemini/T8_textures/manifest.csv`
```text
model,source,output,status,out_width,out_height,size_ok,alpha_ok,layout_ok,colour_ok,seams_ok,consistent_with_model,model_used,notes
```

## 6. What you must not do

- Do not overwrite or edit anything under `assets/`, including `assets/hd/` and
  `assets/hd_overrides/`. Promotion into the game is done later by another agent after
  automatic checks and owner approval.
- Do not generate new characters, costumes, poses or scenes.
- Do not upscale with tricks that bypass the rules (for example generating a new image
  "inspired by" the source).
- Do not create contact sheets or composites; the validating agent makes those.

## 7. Work log and session summary

Log each model (T8) or item (T7) as one WORKLOG entry: inputs, outputs, counts by status,
model/tool used, problems. End every session with a `Session summary (art)` entry: totals by
status per task and priority, the models finished, the 5 outputs a human should look at
first (best and worst), anything declined, and where to resume.
