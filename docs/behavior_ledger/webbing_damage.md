# Web fluid, suits, damage and health

Suit IDs below are the original numeric costume IDs; their names are not yet mapped.

## Web fluid
Evidence: `decomp:spidey.cpp#CPlayer::IncreaseWebbing@f8a0392 (@Ok)`,
`#DecreaseWebbing (@NotOk)`, `#SetFireWebbing (@Ok)`. Confidence: recovered (increase,
fire), partial (decrease).

- The meter holds 0..4096 (one cartridge = 4096) plus a count of spare cartridges.
- Spare cartridge cap: **10**, or **2** in suits 6, 9 and 10.
- Gaining fluid while full adds a spare cartridge (if under the cap) and keeps the
  overflow; otherwise the meter clamps at 4096. Pickups are refused at the cap.
- Spending: suits 3 and 4 (and two global no-cost flags) never spend fluid. The cost
  is scaled by a global setting: setting 0 spends cost/32, setting 1 spends cost/2.
- When the meter cannot cover the cost and a spare exists, a spare is consumed
  (meter += 4096) and SFX 30 plays at the player.
- When fully empty, the shot fails and an "out of webbing" voice line plays:
  XA bank 33, random track 2..4 (unless suppressed).
- Fire webbing (a pickup/suit state) fills the meter and sets the fire-web flag;
  domes then spawn as fire domes (`decomp:web.cpp#CDome::CDome@f8a0392 (@Ok)`).

## Damage scaling
Evidence: `decomp:spidey.cpp#CPlayer::GetDamageInflictedFromDifficulty@f8a0392 (@Ok)`.
Confidence: recovered (values), partial (direction: inferred from the name and the
easier-is-stronger ordering, callers not decompiled).

| Difficulty | 0 Easy | 1 Normal | 2 Hard | 3 Hardest |
|---|---|---|---|---|
| Multiplier on damage Spider-Man deals | x2 | x1.5 | x1 | x0.75 |

Suits 2, 3 and 4 double the base amount before the difficulty multiplier.
Per-difficulty values elsewhere follow the same four-value table pattern
(`Utils_GetValueFromDifficultyLevel(easy, normal, hard, hardest)`), for example
Carnage's damage intake factors (4096, 1024, 256, 256)/4096 and Rhino's attack
timer (40, 30, 21, 21) frames.

## Health
Evidence: `decomp:spidey.cpp#CPlayer::IncHealth@f8a0392 (@Ok)`. Health increases only
while alive and below the maximum, and clamps at the maximum.

## Remake specification (Phase 2 / combat)

1. `WebFluid` resource: meter 0..1 (from 0..4096), spares, cap 10 or 2 per suit,
   no-cost suits {3, 4}; cost scale per global setting (1/32 or 1/2).
2. Auto-swap to a spare with SFX 30; empty triggers the XA 33 bark via DialogueManager.
3. `DifficultyTable` resource holding four-value rows; damage dealt uses
   (x2, x1.5, x1, x0.75) with x2 for suits {2, 3, 4}.
4. Health clamps to max; no healing while dead.

## Planned tests

Implemented as WebFluid and DifficultyTable resources with synthetic coverage in
tests/scenes/test_webbing.tscn. Numeric SFX/XA requests go through Dialogue.
The current private audio registry does not identify `native_sfx_id` or
`native_bank`/`native_track`: those recovered requests emit observable signals,
but return false until verified clip bindings exist. No filename was guessed as
SFX 9/30 or XA33. Thus cartridge/empty-shot behavior is covered, while playback of
those particular original identities remains unresolved.

- Cap: 10 spares (2 in suits 6/9/10); overflow carries into the meter.
- Spend at setting 0 costs 1/32; an empty meter with spares swaps and plays SFX 30.
- Suits 3/4 never spend.
- Damage table returns 2.0/1.5/1.0/0.75 and doubles for suits 2-4.
