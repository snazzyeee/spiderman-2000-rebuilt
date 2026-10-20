# Gemini design brief: faithful frontend and in-game UI (T9 menus, T10 HUD and screens)

This brief extends [GEMINI_BRIEF.md](GEMINI_BRIEF.md). **All hard rules in section 1 of that
brief still apply**: write only inside `staging/gemini/`, no git write commands, no Godot, no
scripts, installers or downloads, never fabricate, log everything in
`staging/gemini/WORKLOG.md`. Read both files fully before starting.

T7/T8 (art enhancement) are blocked and handed to a local pipeline; do not retry them. Do T9,
then T10. T6 stays pending.

---

## 1. Goal

The current remake menu is not faithful: a left-aligned button column over a stretched
background, typed title text instead of the original logo art, generic buttons, and a floating
Spider-Man model. Your job is to work out **what the original PC game's frontend and in-game UI
looked like and how they behaved**, then write precise, implementable specs for:

- **Classic**: a faithful reconstruction (original layout, art, fonts, flow), shown 4:3 inside
  a 16:9 screen.
- **Enhanced** (the default): the same structure and identity adapted to native 1920x1080,
  with modern polish that never contradicts the original.

Another agent (Codex) implements your specs. Write so that it can build each screen without
guessing.

## 2. Evidence rules (stricter than before)

Your previous style guide mixed researched facts with generic comic-UI ideas, invented sound
filenames and links that could not be confirmed. This time:

1. **Every factual claim carries evidence** with one of these ids:
   - `owner:<file>`: a screenshot the owner placed in `staging/reference/menu_original/` (if any).
     This outranks everything else.
   - `web:<url>#<what you saw>`: a page, screenshot or video **you actually opened in this
     session**. For videos, give the timestamp. Record the access date. Never list a URL you did
     not open.
   - `decomp:<file>#<function or struct>`: read-only facts from
     `tools/third_party/spidey-decomp/` (see section 3). Describe facts in your own words;
     never copy code.
   - `asset:<path>`: an original image in `assets/ui/` or another `assets/` file you viewed.
   - `inferred`: your reasoning from the above (say from what).
   - `authored`: a design choice with no evidence (Enhanced polish, or gaps). Label it plainly.
2. **Confidence per claim**: `confirmed` (two independent sources, or owner screenshot),
   `likely` (one good source), `uncertain`, `authored`.
3. **Platform matters.** The canonical version is the **PC port** (LTI Gray Matter). The PC frontend
   may differ from the PlayStation one (mouse cursor, PC button art, options). When a source shows
   PlayStation or another version, label it as such; do not present it as PC.
4. No invented filenames, sound names, menu items, strings or unlock rules. Unknown is fine; say so.
5. Do not copy text from guides or wikis. Menu labels and on-screen strings may be recorded
   exactly as they appear in a source (they are short UI strings), with the source.

## 3. Inputs available to you

**Original art you may view** (private, do not share outside this session):

| File | What it is |
|---|---|
| `assets/ui/Title.png` | 512x240 launch title: jagged "SPIDER-MAN" logo, Spider-Man swinging between buildings, "PRESS ENTER" |
| `assets/ui/shell_bg.png` | frontend background (spider emblem) |
| `assets/ui/cb00.png` … `cb31.png` | comic cover / panel art used by the gallery |
| `assets/ui/fonts/*_original.png` + `.fnt` | the original bitmap fonts (sp_fnt00–03, font_big); see `docs/behavior_ledger/fonts.md` |
| `staging/validation/smoke/godot-menu.png` | the current (unfaithful) remake menu, for contrast |

Also present in the original archive but not yet converted (mention them where they belong;
another agent converts them): `logo.rle`, `load00.rle` (loading graphic), `L1Cov`–`L8Cov` level
covers and `L*S*_*` story pages, `LegalPC.bmp`, `DemoPC.bmp`, `credits.txt`, and the PC-port
frontend set under `data/lti/` (buttons `butesc`, `butlarrow`, `butminus`, `butplus`,
`butrarrow`, `cursor`, `new_cursor`, and several `Tex_*` textures).

**Decomp files (read-only reference)** in `tools/third_party/spidey-decomp/`:
- `front.cpp` / `front.h`: the menu object (item list, width, centering, highlight, layout
  arithmetic in a 512-wide virtual screen).
- `shell.cpp` / `shell.h`, `pshell.cpp` / `pshell.h`: frontend flow, pages, cheats, name entry,
  the frontend Spider-Man actor (`CRudeWordHitterSpidey`, which animates and plays punch sounds).
- `panel.cpp` / `panel.h`: in-game panel/HUD elements.
- `docs/behavior_ledger/frontend.md` and `fonts.md`, if present, already record confirmed facts.
  Prefer them.
Functions marked `@MEDIUMTODO`/`@BIGTODO` are stubs; say "not recovered" rather than guessing.

**Owner screenshots**: if `staging/reference/menu_original/` exists, view every image there first
and treat it as the primary reference. If it does not exist, note that in WORKLOG.md and proceed.

## 4. T9: Frontend (title, main menu and every submenu)

### 4.1 Research: `staging/gemini/T9_menu/original_frontend.md`
For each screen, describe what the original PC version shows and does, with evidence ids.

**Screens:** boot/legal/logo sequence; title ("PRESS ENTER"); main menu; Game branch (new game,
difficulty, load/save, level select if any); Options (every sub-page: audio, controls, video if
any); Training; Special (costumes, cheats, movie viewer, credits, anything else); Gallery (comics,
character viewer, anything else); name entry; pause menu; loading screens.

**For each screen record:**
- background art;
- logo/title placement;
- item list (exact labels and order);
- alignment and position (in the original 4:3 frame, as fractions of width/height);
- font and colours for normal, highlighted and disabled items;
- the highlight/cursor style (PC mouse cursor? arrow buttons?);
- any 3D character or animation and what triggers it;
- music;
- sound events (move/confirm/back/error, by event only, not filename);
- input (keyboard, mouse, gamepad);
- transitions;
- what Back does;
- unknowns.

### 4.2 Spec: `staging/gemini/T9_menu/menu_spec.json`
A machine-readable spec Codex can implement directly. Write valid JSON (check brackets and commas
carefully). Structure:

```json
{
  "schema_version": 1,
  "reference_frame": {"classic": [640, 480], "enhanced": [1920, 1080]},
  "screens": [
    {
      "id": "main_menu",
      "title": "Main menu",
      "evidence": ["decomp:front.cpp#CMenu layout", "web:<url>#t=1m02s"],
      "confidence": "likely",
      "background": {"asset": "assets/ui/shell_bg.png", "fit": "classic: fill 4:3; enhanced: see layout"},
      "music": "menu theme (event name only)",
      "elements": [
        {"id": "logo", "type": "image", "asset": "assets/ui/Title.png#logo-region-or-logo.rle",
         "classic_rect": [0.0, 0.0, 0.0, 0.0], "enhanced_rect": [0.0, 0.0, 0.0, 0.0],
         "anchor": "top_center", "z": 2, "evidence": ["..."], "confidence": "uncertain"},
        {"id": "items", "type": "menu_list",
         "items": ["Game", "Options", "Training", "Special", "Gallery"],
         "font": "sp_fnt00", "align": "center", "line_spacing_px_classic": 0,
         "colours": {"normal": "#RRGGBB", "focus": "#RRGGBB", "disabled": "#RRGGBB"},
         "focus_style": "describe", "classic_rect": [0, 0, 0, 0], "enhanced_rect": [0, 0, 0, 0],
         "evidence": ["..."], "confidence": "likely"}
      ],
      "navigation": {"focus_order": ["items"], "back": "title", "wrap": true},
      "sounds": {"move": "event", "confirm": "event", "back": "event", "error": "event"},
      "transitions": {"in": "describe + duration", "out": "describe + duration"},
      "enhanced_changes": ["each change from classic, labeled authored"],
      "unknowns": ["..."]
    }
  ]
}
```

Rects are `[x, y, width, height]` as fractions (0–1) of the reference frame. Colours come from the
original art or font palettes you viewed (say which pixel/region), or are marked authored.

### 4.3 Wireframes: `staging/gemini/T9_menu/wireframes/<screen>_<classic|enhanced>.svg`
One SVG per screen and style (viewBox `0 0 640 480` for classic, `0 0 1920 1080` for enhanced):
labeled rectangles for every element from menu_spec.json, a 4:3 safe-frame outline on enhanced, and
text labels in a neutral font. No embedded or linked original images. The SVGs must match
menu_spec.json exactly; they are for review, not art.

### 4.4 Enhanced design notes: `staging/gemini/T9_menu/enhanced_design.md`
How Enhanced modernizes each screen without breaking faithfulness: 16:9 composition (what fills the
sides of the 4:3 layout), where the 3D Spider-Man stands and what he does (based on the original
frontend actor where evidence exists), lighting/staging, motion budget (transitions ≤ 0.35 s,
skippable), attract mode, reduced-motion behaviour, mouse + gamepad + keyboard focus rules, and
accessibility (contrast ≥ 4.5:1, focus visible beyond colour). Label every item `authored` unless
evidence supports it. Optional: concept images from your image tool, saved as
`staging/gemini/T9_menu/concepts/<screen>_concept_<n>.jpg` and labeled "concept only, not a spec".

## 5. T10: In-game UI and presentation screens

### 5.1 Research: `staging/gemini/T10_ui/original_ingame_ui.md`
With the same evidence rules, cover:
- **HUD:** health, web fluid and spare cartridges, spider-sense indicators, compass or objective
  pointer, boss health, timers, collectibles counter, button prompts.
- **On-screen text:** objective messages (the TRG `TextMessage` command), subtitles and captions,
  Stan Lee narration captions.
- **Pause menu.**
- **Loading screens:** level covers, load graphic, hints.
- **Level intro and outro comic pages** (L*Cov / L*S*_* story pages): when they show and how they
  advance.
- **Death/restart and game-over flow.**
- **Results screens**, if any.

For each element: position in the 4:3 frame, art, font, colour, animation, and when it appears.
Use `decomp:panel.cpp`, the `CPlayer` indicator list (spider-sense uses six screen indicators,
recorded in `docs/behavior_ledger`), and `webbing_damage.md` for meter behaviour.

### 5.2 Spec: `staging/gemini/T10_ui/ui_spec.json`
Same structure as menu_spec.json, with `"screens"` such as `hud`, `pause`, `loading`,
`comic_intro`, `death_restart`. Each element gets classic and enhanced rects, a `"data_binding"`
field naming the game value it shows (e.g. `player.health`, `web_fluid.meter`,
`web_fluid.spares`), and `"show_when"`.

### 5.3 Wireframes: `staging/gemini/T10_ui/wireframes/*.svg` (same rules as 4.3).

## 6. Questions for the owner: `staging/gemini/T9_menu/owner_questions.md`
List decisions only the owner can make, each with options, your recommendation, and why. Examples:
- what fills the 16:9 sides in Enhanced;
- whether Enhanced keeps the PC mouse cursor;
- whether the cheat screen uses the original passwords;
- which screenshots would settle the remaining unknowns. Name exact screens so the owner can
  capture them from their own copy of the game.

## 7. Output layout

```text
staging/gemini/
  T9_menu/
    original_frontend.md
    menu_spec.json
    enhanced_design.md
    owner_questions.md
    wireframes/*.svg
    concepts/*.jpg            (optional, concept only)
  T10_ui/
    original_ingame_ui.md
    ui_spec.json
    wireframes/*.svg
```

## 8. Logging and finish

One WORKLOG entry per screen researched or specified (inputs, sources opened, outputs, unknowns).
Finish with `Session summary (design)`:
- screens covered per task, with counts of confirmed/likely/uncertain/authored claims;
- the 5 biggest unknowns;
- the screenshots that would resolve them;
- any source that contradicted another.
Then stop.
