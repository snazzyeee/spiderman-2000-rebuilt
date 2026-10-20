# Intro, briefing and gameplay lifecycle

MissionManager owns a lifecycle independent of rooftop objective progression:

    IntroCinematic -> MissionBriefing -> GameplayActive

New Game enters IntroCinematic before starting the PlayableDirector. The mission
control lock and the cutscene's own control lock overlap, so releasing the
cutscene cannot briefly enable movement. The orbital camera and combat already
respect the session control lock. Intro HUD headers, objective markers, controls,
vitals and subtitle/briefing popups are suppressed. Pause/skip remain cinematic
transport controls, not gameplay commands.

CutsceneSequence acquires exclusive dialogue ownership using its presenter.
DialogueManager rejects unrelated Play/PlayEvent/BeginTimeline calls while that
owner is active. Other presenters/triggers cannot acquire competing dialogue.
The intro Timeline contains original narration only; authored instructions are
removed from its cues by both assembly recipes.

OnIntroFinished is emitted once after the cutscene releases its camera,
narration and control ownership. Natural completion and Skip emit it; Abort does
not. MissionManager subscribes to this event to begin MissionBriefing. The
presenter then displays BriefingText under MISSION BRIEFING for BriefingSeconds
(default five seconds), respecting pause. Enter/A advances after a 0.2-second
debounce. CompleteBriefing is also available to deterministic automation.

Only briefing completion enters GameplayActive and starts RooftopRoute. Mission
checkpoint saves and dialogue proximity triggers are gated on GameplayActive.
Continue and Training explicitly bypass intro/briefing and restore gameplay.
Recovery requests during either opening phase are ignored. Replay cancels any
old briefing coroutine before restarting the sequence; teardown releases owners.

Validated on 2026-10-20: 39 Unity tests passed and 27 rendered standalone checks
passed. Coverage includes pause, natural completion, skip, abort, competing
dialogue rejection, blocked commands, briefing completion, save/load and menu
entry. Original asset requirements and build commands remain in MAIN_MENU.md.
