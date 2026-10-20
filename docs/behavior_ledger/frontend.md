# Frontend evidence and remake policy

## Menu SFX

Evidence `exe:frontend-sfx-static-20261021`, confidence **recovered**. Canonical
executable SHA-256 e4e221b168ccf52130c8c56a1535289afacc1906fb55d54545e33d50e43eec86;
decomp names at f8a039248908e085f8fba8106769aea7f8f9de67. A fingerprint-gated,
read-only x86 scan bounded each of 110 named frontend functions by the next
names.json function (less than 64 KiB each), finding 91 direct SFX calls.
Private contexts: staging/reference/frontend-sfx-static.json. No executable ran.
Some functions contain data/jump tables: absence of a decoded call is not proof
of absence. Positive call sites were inspected with their controlling branches.

| Event | Global ID | Positive call-site evidence |
|---|---:|---|
| Changed selection | 0x29 | CMenu_Update 0x44080b after comparing previous/new selection; Shell_MainMenu 0x494046 after changed-index check |
| Confirm selection | 0x1F | Shell_MainMenu 0x4941de reached from valid confirm branches; Shell_Options 0x49755c |
| Back/cancel | 0x23 | Shell_Options 0x497538; repeated exit branches in Shell_Difficulty and PCSHELL_DoDisplayOptions |
| Reject unavailable selection | 0x1B | Shell_MainMenu 0x493ead on unavailable-entry branches |

Cheat accept/reject/exit 0x1D/0x1B/0x23 are corroborated by
`decomp:shell.cpp#Shell_Cheats@f8a0392` and static calls. They are not proof of
generic confirm ID. Actor punch IDs 0xE..0x11 are corroborated by
`decomp:shell.cpp#CRudeWordHitterSpidey::AI@f8a0392` and four calls in
0x48db10..0x48dc10. These are actor events, not UI button sounds.

Use audio_ids.md exact global bindings, never menu.kat bank order. Private
WAV frame counts / sample rates resolve move to 0.303333 s, confirm to 1.367 s,
back to 1.111 s (`staging/reference/frontend-branches-static.txt`). The owner's
maximum 0.6 s takes precedence over historical clip length: confirm/back stay
bound but silent. Do not truncate or substitute an unrelated sound.

Evidence `remake:menu-audio-policy-20261021`, confidence **authored**: stop the
previous menu sound before a new distinct event, reject absent, looping,
zero-length, non-finite or overlong streams. Initial focus is silent; hover
grabs focus only when it changes; repeated hover/focus on the same item is
silent. Confirm and Back use distinct events. Test with synthetic streams;
private checks require exact native ID resolution but honor duration rejection.
