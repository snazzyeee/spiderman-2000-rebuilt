# Gemini claim audit

Every original source claim is re-graded below. Exact per-screen rectangles, strings, unlocks and flow are unverified; authored layout rectangles do not promote them. See frontend.md for checked helper facts.

| Screen/element | Source claim disposition | Grade | Gemini citation (not validation) |
|---|---|---|---|
| boot_legal/legal_banner | LegalPC.bmp and 180-vblank wait recovered; rectangle/skip claim unverified | Gemini proposal (unverified) | decomp:shell.cpp#Shell_LegalScreen |
| title/title_illustration | No independent verification of this screen-specific claim | Gemini proposal (unverified) | asset:assets/ui/Title.png |
| title/press_enter_label | No independent verification of this screen-specific claim | Gemini proposal (unverified) | asset:assets/ui/Title.png#bottom-text |
| main_menu/background_emblem | No independent verification of this screen-specific claim | Gemini proposal (unverified) | asset:assets/ui/shell_bg.png; decomp:shell.cpp#Shell_DrawBackground |
| main_menu/items | Font helper behaviour recovered; screen-specific use and position unverified | Gemini proposal (unverified) | decomp:front.cpp#CMenu::CMenu; decomp:pshell.cpp#PShell_DefaultText |
| main_menu/spidey_actor | AI cycle and punch sounds recovered; constructor placement partial; main-menu association unverified | Gemini proposal (unverified) | decomp:shell.cpp#CRudeWordHitterSpidey; staging/validation/smoke/godot-menu.png |
| game_menu/title_bar | Helper colours/conditional font recovered; caller coordinates and screen text unverified | Gemini proposal (unverified) | decomp:shell.cpp#Shell_DrawTitleBar |
| game_menu/items | CMenu helpers recovered; this item list and rectangle are not established by the helper | Gemini proposal (unverified) | decomp:front.cpp#CMenu; docs/MAIN_MENU.md#L75 |
| difficulty_select/title_bar | Helper colours/conditional font recovered; caller coordinates and screen text unverified | Gemini proposal (unverified) | decomp:shell.cpp#Shell_DrawTitleBar |
| difficulty_select/items | CMenu helpers recovered; this item list and rectangle are not established by the helper | Gemini proposal (unverified) | decomp:front.cpp#CMenu |
| difficulty_select/description | Font helper behaviour recovered; screen-specific use and position unverified | Gemini proposal (unverified) | decomp:pshell.cpp#PShell_InstructionalText |
| load_game/title_bar | Helper colours/conditional font recovered; caller coordinates and screen text unverified | Gemini proposal (unverified) | decomp:shell.cpp#Shell_DrawTitleBar |
| load_game/slot_list | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:shell.cpp#Shell_AddGameSlots |
| options_menu/title_bar | Helper colours/conditional font recovered; caller coordinates and screen text unverified | Gemini proposal (unverified) | decomp:shell.cpp#Shell_DrawTitleBar |
| options_menu/items | CMenu helpers recovered; this item list and rectangle are not established by the helper | Gemini proposal (unverified) | decomp:front.cpp#CMenu; decomp:PCShell.cpp |
| controls_options/title_bar | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:PCShell.cpp#STR_KB_CONFIG |
| controls_options/actions_column | Recovered column origins (30,60)/(332,60), line spacing 15; content/extents unverified | Gemini proposal (unverified) | decomp:PCShell.cpp#resetActionMaps (X=30 in 512 space) |
| controls_options/bindings_column | Recovered column origins (30,60)/(332,60), line spacing 15; content/extents unverified | Gemini proposal (unverified) | decomp:PCShell.cpp#resetActionMaps (X=332 in 512 space) |
| special_menu/title_bar | Helper colours/conditional font recovered; caller coordinates and screen text unverified | Gemini proposal (unverified) | decomp:shell.cpp#Shell_DrawTitleBar |
| special_menu/items | CMenu helpers recovered; this item list and rectangle are not established by the helper | Gemini proposal (unverified) | decomp:front.cpp#CMenu |
| cheats_screen/title_bar | Helper colours/conditional font recovered; caller coordinates and screen text unverified | Gemini proposal (unverified) | decomp:shell.cpp#Shell_DrawTitleBar |
| cheats_screen/input_prompt | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:shell.h#INPUT_MAX_SIZE |
| cheats_screen/rude_spidey_actor | AI cycle and punch sounds recovered; constructor placement partial; main-menu association unverified | Gemini proposal (unverified) | decomp:shell.cpp#CRudeWordHitterSpidey |
| gallery_menu/title_bar | Helper colours/conditional font recovered; caller coordinates and screen text unverified | Gemini proposal (unverified) | decomp:shell.cpp#Shell_DrawTitleBar |
| gallery_menu/items | CMenu helpers recovered; this item list and rectangle are not established by the helper | Gemini proposal (unverified) | decomp:front.cpp#CMenu |
| pause_menu/paused_header | Recovered text origin (256,50) and RGB (127,25,33); font and box extents unverified | Gemini proposal (unverified) | decomp:front.cpp#PrintPaused (X=256, Y=50, RGB=(127,25,33)) |
| pause_menu/items | CMenu helpers recovered; this item list and rectangle are not established by the helper | Gemini proposal (unverified) | decomp:front.cpp#CMenu |
| hud/health_gauge | Helper stretches caller-supplied coordinates; does not prove radial health shape or placement | Gemini proposal (unverified) | decomp:panel.cpp#Panel_SetStretchedScreenCoords |
| hud/spidey_portrait | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:panel.cpp#Panel_Init |
| hud/web_fluid_meter | No independent verification of this screen-specific claim | Gemini proposal (unverified) | docs/behavior_ledger/webbing_damage.md#L10 |
| hud/web_spares_counter | No independent verification of this screen-specific claim | Gemini proposal (unverified) | docs/behavior_ledger/webbing_damage.md#L11; decomp:panel.cpp#Panel_Init |
| hud/spider_sense_indicators | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:spidey.h#SIndicator; decomp:spidey.cpp#BuildOffscreenSpideySenseIndicatorList |
| hud/compass_pointer | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:panel.cpp#Panel_CreateCompass |
| hud/boss_health_bar | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:panel.cpp#Panel_CreateHealthBar |
| hud/hostage_health_bar | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:panel.cpp#Panel_CreateHealthBar (Scorpion/Jonah, Venom/MJ) |
| hud/mission_timer | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:panel.cpp#Panel_UpdateTimer; decomp:pshell.cpp#DisplayScore |
| hud/trg_objective_message | No independent verification of this screen-specific claim | Gemini proposal (unverified) | decomp:mess.cpp#Mess_Display (RGB=(96,96,128)); docs/behavior_ledger/trg.md |
| hud/subtitles | Font helper behaviour recovered; screen-specific use and position unverified | Gemini proposal (unverified) | docs/behavior_ledger/fonts.md#L71; decomp:pshell.cpp#PShell_SmallFont |
| hud/stan_lee_caption | No independent verification of this screen-specific claim | Gemini proposal (unverified) | docs/MAIN_MENU.md#L101-L109 |
| pause/paused_header | Recovered text origin (256,50) and RGB (127,25,33); font and box extents unverified | Gemini proposal (unverified) | decomp:front.cpp#PrintPaused |
| pause/pause_items | CMenu helpers recovered; this item list and rectangle are not established by the helper | Gemini proposal (unverified) | decomp:front.cpp#CMenu |
| loading/level_cover_art | No independent verification of this screen-specific claim | Gemini proposal (unverified) | pkr:archive-index.json#data/L1Cov.bmp |
| loading/loading_spinner | No independent verification of this screen-specific claim | Gemini proposal (unverified) | internal design brief (not published) |
| loading/hint_label | No independent verification of this screen-specific claim | Gemini proposal (unverified) | inferred; confidence: likely |
| comic_intro/comic_page | No independent verification of this screen-specific claim | Gemini proposal (unverified) | pkr:archive-index.json#data/L1S1_1.bmp |
| comic_intro/page_advance_prompt | No independent verification of this screen-specific claim | Gemini proposal (unverified) | inferred; confidence: confirmed |
| death_restart/death_header | No independent verification of this screen-specific claim | Gemini proposal (unverified) | inferred from classic failure flow; confidence: confirmed |
| death_restart/restart_options | No independent verification of this screen-specific claim | Gemini proposal (unverified) | docs/behavior_ledger/trg.md#L43 (Re_Start_Death) |
