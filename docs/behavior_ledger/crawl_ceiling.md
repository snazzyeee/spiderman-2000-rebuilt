# Wall crawl, ceiling crawl and ground loss

All normal components below are 4.12 fixed point in the original Y-down frame
(4096 = 1.0). "Up-facing" for a ceiling means the surface normal points down
toward the player.

## Recovered behavior

### Stick to ceiling
Evidence: `decomp:spidey.cpp#CPlayer::CheckStickToCeiling@f8a0392 (@Ok)`. Confidence: recovered.

Attaches only when all hold:
1. the player is not moving down (vertical velocity <= 0 in the Y-down frame);
2. a ceiling contact was reported this frame (collision flag 0x100) with a contact record;
3. the contacted surface allows crawling (per-surface flag);
4. the ceiling normal component exceeds **3400/4096 = 0.830**, i.e. the surface is
   within about 34 degrees of facing straight down;
5. the surface is not marked no-stick (flag 0x40000).

On attach: enter the crawl state, store the surface normal and orient to it, snap the
position to the contact point plus normal x player height, play slot **227**
(or **234** when the current slot is **232**), play SFX 9, and clear the
held-jump and secondary state flags.

### Run into wall
Evidence: `decomp:spidey.cpp#CPlayer::CheckRunIntoWall@f8a0392 (@NotOk)`. Confidence: partial.

- Ignored while holding an object.
- A wall contact counts when its normal Y component is within **[-2600, 3400]/4096**
  (about [-0.635, 0.830]) and the surface is not no-stick.
- While the push direction stays aligned with the wall (summed component difference
  above 3800/4096), a counter accumulates. Above **20** frames the player plays
  slot **14** and enters the bump state. Releasing resets the counter.
- So pushing into a wall bumps the player off it rather than auto-sticking.

### Knocked off a wall or ceiling
Evidence: `decomp:spidey.cpp#CPlayer::KnockSpideyFromCrawlPosition@f8a0392 (@Ok)`. Confidence: recovered.

Only while crawling on a wall or ceiling: leave look-around mode, clear crawling,
reset the up vector to world up, play slot **175**, set the knocked-off state, and
re-orient using the stored wall normal (wall) or ceiling normal (ceiling).

### Ground loss (coyote time)
Evidence: `decomp:spidey.cpp#CPlayer::CheckGroundGone@f8a0392 (@Ok)`. Confidence: recovered.

Without floor contact, a grace counter decrements each frame; falling starts only
when it reaches zero. Then: drop any held object (with 4x current velocity), record
the fall-start height, play slot **212**, and, if crawling, reset the up vector
to world up and re-orient forward. The player enters the falling state.

### Ceiling region test used by scripts
Evidence: `decomp:spidey.cpp#CPlayer::IfPlayerCeilingCheck@f8a0392 (@Ok)`. True when the
player's height is inside a script-given [min, max] range and the player is on a
ceiling, or on a wall whose normal component exceeds 3400.

## Unknowns

| Item | Status |
|---|---|
| Wall attach rule | `CheckStickToWall` is a stub |
| Orientation blend to a new normal | `OrientToNormal` is a stub (BIGTODO) |
| Convex (exterior), concave (interior) and fence edge transitions | Three separate stubs; their existence is recovered, their rules are not |
| Grace counter length | Set elsewhere; not recovered |
| Crawl speed | Not recovered |

## Remake specification (Phase 2)

1. Ceiling attach uses the five conditions above with threshold 0.830 against the
   Godot surface normal (`-normal.y > 0.830` for a ceiling seen from below).
2. Wall contact classification uses Godot normal.y in [-0.830, +0.635] (the original
   Y-down range [-2600, 3400]/4096 with its sign flipped).
   Pushing into a wall for 20 original frames (0.667 s at 30 Hz) without a stick
   input triggers the bump reaction, not an attach.
3. Ground loss uses a grace timer (authored length, default 4 frames = 0.133 s)
   before the fall state.
4. Knock-off and ground loss restore world up through the shared orientation blend.
5. The orientation blend and the three edge-transition probes are authored (see
   blueprint Phase 2) and named after the original cases: exterior, interior, fence.
6. Animation slots 14, 175, 212, 227, 232 and 234 bind to these events through
   data/animation_semantics.json, never through hard-coded numbers in src/.

## Planned tests (tests/scenes/test_wallcrawl.tscn)

Implementation: MotorContext plus Crawl/Ceiling/Ground/Air/Stunned state nodes.
The original wall attach rule remains unknown: **the remake requires explicit
crawl input**, a stickable contact and authored reach. This is a deliberate
deviation, not a recovered auto-attach rule. Exterior/interior/fence reach probes,
orientation blending, speed and four-frame grace are authored in crawl.tres.
Recovered event slots resolve through the ledger JSON `runtime_bindings` table.

- A ceiling tilted 30 degrees attaches; one tilted 40 degrees does not.
- Moving downward never attaches to a ceiling.
- A no-stick surface never attaches.
- Pushing into a wall for 0.70 s bumps; 0.60 s does not.
- Walking off a ledge falls only after the grace timer.
- Knock-off from a wall and from a ceiling ends upright with no orientation flip.
