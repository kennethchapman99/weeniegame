# Family Showcase Manual Test

> **Status: ACTIVE RUNBOOK.** This is the family-facing manual test for the current Unity arena.
> It is not a roster expansion plan. Use it to make the first hands-on session feel polished,
> joyful, readable, and personal while still gathering the acceptance evidence required by the
> Operation Pee Break couch gate.

## Purpose

This session is not a strict blind usability test. It is a lightly hosted family showcase that should
make Cheddar and Cocoa feel like the stars immediately, then gather honest evidence about whether the
deep slice is clear, funny, and worth replaying.

The success bar is emotional and practical:

- players recognize Cheddar and Cocoa as different dogs with different jobs;
- players talk to each other instead of silently reading UI;
- failures create laughter or "oh, I get it" recovery, not embarrassment;
- at least one person reacts to a mission premise as a real Cheddar/Cocoa joke;
- Operation Pee Break lands as the finale, not as a confusing prototype stress test.

## Showtime Card

Use this when people are in the room and you do not want to scan the full runbook.

1. **Say:** "Cheddar is chaos; Cocoa is the queen. Left stick moves, X barks, Y interacts. Follow
   your dog's arrow and talk to each other."
2. **Backyard Rescue** - press `F7`/`B`, start. Goal: learn dogs, bark, rescue/teamwork.
3. **Kitchen Falling Food Frenzy** - press `F6`/`K`, start. Goal: first big laugh and role split.
4. **Confidence reset if needed** - press `F8`/`W` for Weenie Roundup if overwhelmed, or `F9`/`L`
   for Walkies if they are ready for physical comedy.
5. **Operation Pee Break finale** - press `F5`/`P`, start. Goal: personal joke, role flip, united
   bark payoff.
6. **Stop on appetite.** End with one replay choice while they still want more.

Only use these hints if the room stalls:

- "Follow the arrow on your dog."
- "Try barking near the thing bothering you."
- "Gold wants the bowl; purple is suspicious."
- "The human needs more than one dog clue."
- "The phone is the boss now."
- "This is a two-dog bark emergency."

## Hard Rules

- Use the Unity project only: `unity/CheddarAndCocoa/`.
- Keep the roster frozen.
- Keep the F1 playtest overlay off during family play unless the host needs to diagnose a problem.
- Do not explain every mechanic up front. Give a premise, the controls, and one dog-authentic goal.
- Do not chase a full-roster tour. Stop while people still want one more round.
- If a mission loses the room for more than 90 seconds, switch to the fallback plan.

## Preflight

Run this before anyone sits down:

1. Confirm the latest PlayMode suite is green or that you are intentionally using the latest green
   build recorded in `docs/ARENA-PLAYABLE.md`.
2. Launch `unity/builds/dev/CheddarAndCocoa-Arena.app` on the shared display.
3. Connect two physical controllers.
4. Confirm controller 1 moves Cheddar and controller 2 moves Cocoa.
5. Confirm audio is on and rumble is on unless the room requires otherwise.
6. Confirm the mission picker shows the family showcase shortcut line:
   `F7 Backyard`, `F6 Kitchen`, `F8 Weenies`, `F9 Walkies`, `F5 Pee`.
7. Reset to mission select before players arrive.

Host fallback controls:

- `F7` or `B`: highlight Backyard Rescue.
- `F6` or `K`: highlight Kitchen Falling Food Frenzy.
- `F8` or `W`: highlight Weenie Roundup.
- `F9` or `L`: highlight Walkies on the Leash.
- `F5`, `P`, or controller `Y/North`: highlight Operation Pee Break.
- `Enter`, `Space`, `Start`, or controller `South`: start the highlighted mission.
- `M`, `Escape`, controller `East`, or D-pad left on an end card: return to mission select.
- `F1`: show or hide the playtest overlay if something needs diagnosis.
- `F4`: mark a cold-read question if a player asks "what do I do?"

## Host Opening

Say this, then stop talking for the first minute:

> "You are Cheddar and Cocoa. Cheddar is chaos; Cocoa is the queen. Left stick moves, X barks, Y
> interacts. Follow your dog's arrow, talk to each other, and treat every mission like something the
> dogs think is the most important emergency in the world."

Do not explain scoring, ranks, hidden tests, controller implementation, generated art, or the mission
controller migration.

## Showpiece Mission Order

The planned session is 22-30 minutes. Use the short path if the room is tired; use the full path if
people are laughing and asking for another.

| Slot | Mission | Target time | Host intent |
| --- | --- | ---: | --- |
| 1 | Backyard Rescue | 4-6 min | Meet the dogs, bark, squirrel pressure, rescue/teamwork baseline. |
| 2 | Kitchen Falling Food Frenzy | 5-7 min | First big laugh: falling food, roles, readable chaos. |
| 3 | Walkies on the Leash or Weenie Roundup | 3-5 min | Optional palate cleanser: physical dog comedy with an easy read. |
| 4 | Operation Pee Break | 8-12 min | Finale: personal joke, role flip, human puzzle, united-bark payoff. |
| 5 | Replay choice | 2-5 min | Let the family pick a favorite for one more run. |

### Slot 1 - Backyard Rescue

Host setup:

> "First round: protect the backyard breakfast. Cheddar and Cocoa are both useful, and barking
> matters."

What to watch:

- Do players know which dog they are within 10 seconds?
- Do they use bark without being told a second time?
- Do they call out squirrel, predator, rope, or rescue moments?
- Does the camera keep both dogs understandable from couch distance?

Good host intervention:

- If they freeze for 20 seconds: "Follow the arrow on your dog."
- If they miss bark: "Try barking near the thing bothering you."

Move on after one clear, one funny fail, or six minutes.

### Slot 2 - Kitchen Falling Food Frenzy

Host setup:

> "Now the kitchen is doing what dogs believe kitchens do: food is falling from heaven. Cheddar makes
> the mess; Cocoa makes it useful."

What to watch:

- Does Cheddar discover the counter bark?
- Does Cocoa read the warning circle and safe bowl?
- Do wrong catches/floor splats feel funny and recoverable?
- Do players naturally say "you stay there" or "I got the bowl"?

Good host intervention:

- If Cheddar never barks: "Cheddar can bark at the counter."
- If Cocoa chases every object: "Gold wants the bowl; purple is suspicious."

Move on after the dinner-rush idea is understood, even if they do not clear it.

### Optional Slot 3 - Walkies or Weenies

Choose based on the room:

- **Walkies on the Leash** if players are comfortable and laughing at physical coordination.
  Press `F9`/`L`. Host line: "Now just try to walk like normal dogs. That is already impossible."
- **Weenie Roundup** if anyone looks overloaded.
  Press `F8`/`W`. Host line: "Reset round: carry the sacred weenies home."

This slot is a pressure valve. Keep it short. Its job is to rebuild confidence before the finale.

### Slot 4 - Operation Pee Break

Host setup:

> "This is the one. The dogs need to pee, and the only human home is a teenager fused to a phone.
> You cannot open the door. You have to make the human understand dog logic."

Beat-by-beat hosting:

- Beat 1: say nothing after setup. Cocoa should find the door stare.
- Beat 2: if they ask, say "The human needs more than one dog clue."
- Beat 3: if they ask, say "The phone is the boss now. One dog blocks, one dog handles the charger."
- Beat 4: if they are close but not barking, say "This feels like a two-dog bark emergency."

What to watch:

- Does Cocoa's door stare read as a job?
- Does Cheddar's leash/present job read as comedy, not busywork?
- Does the charger role flip create communication?
- Does a misread make people laugh?
- Does the door-open/hydrant payoff land as relief?
- Do players ask to replay for fewer misreads?

Acceptance target:

- The mission does not need a perfect clear.
- It does need a visible "we understand the joke now" moment before the session ends.

## Fallback Plan

Use fallbacks quickly. The goal is a great family session, not proving a point.

| Problem | Action |
| --- | --- |
| Controller pairing fails | Pause the session. Fix hardware. Restart from mission select. Do not troubleshoot in front of players for more than two minutes. |
| Players cannot identify their dogs | Restart Backyard Rescue and restate: "Cheddar is P1, Cocoa is P2." Watch labels before moving on. |
| Backyard feels too busy | Skip to Weenie Roundup for a simpler carry loop, then Kitchen. |
| Kitchen becomes unreadable | Say the gold/purple hint once. If still stuck, return to mission select and use Weenie Roundup. |
| Pee Break Beat 1 stalls | Say: "Cocoa is the stare specialist. Try the door." |
| Pee Break Beat 2 stalls | Say: "One dog clue is not enough. What else says walk?" |
| Pee Break Beat 3 stalls | Say: "The teenager will not move while the phone wins." |
| Pee Break finale stalls | Say: "This is a two-dog bark emergency." |
| Any mission creates frustration instead of laughter | End the round from the end card or mission select. Ask which mission they want to replay. |
| Build glitch or visual bug breaks the mood | Switch to showing the art-review contact sheet and explain it as a work-in-progress build; do not keep forcing play. |

## Observation Checklist

Record short notes after each mission. Do not interrupt play unless the session is stuck.

### Reaction

- Biggest laugh:
- First "that is Cheddar/Cocoa" comment:
- Moment someone leaned forward:
- Moment someone checked out:
- Favorite mission:
- Mission they wanted to replay:

### Readability

- Could each player identify their dog?
- Could players read the current objective from the couch?
- Did arrows and labels help without covering the dogs?
- Were important props visible before text was parsed?
- Did success/failure feedback explain what just happened?

### Co-op

- Did players talk to each other?
- Did each player have a job?
- Did either player wait with nothing useful to do?
- Did at least one mission produce "you do X while I do Y" communication?
- Did Operation Pee Break's role flip make sense?

### Controls and Flow

- Did both controllers work immediately?
- Did bark feel important?
- Did interact feel discoverable enough?
- Could players replay or return to mission select?
- Did the host need keyboard shortcuts to keep the session smooth?

### Family Wow

Pass this only if most are true:

- Someone laughed at a failure.
- Someone recognized a real dog-life behavior.
- Someone understood Cheddar/Cocoa asymmetry without a technical explanation.
- Someone asked to replay, improve, or show another person.
- Operation Pee Break produced a memorable reaction.

## Post-Session Debrief

Ask these in order, casually:

1. "Which mission felt most like Cheddar and Cocoa?"
2. "Where did you know exactly what your dog should do?"
3. "Where did you feel lost?"
4. "What made you laugh?"
5. "What would you want to play again?"
6. "What should I fix before showing this to someone else?"

Then write three lists:

- **Keep:** moments that already landed.
- **Fix before next family session:** blockers or vibe-killers only.
- **Later:** art polish, extra missions, balance wish-list.

## Completion Criteria

The family showcase gate is ready to call successful when:

- two physical controllers worked for the whole session;
- players completed or meaningfully understood Backyard, Kitchen, and Operation Pee Break;
- the host used no more than one hint per Pee Break beat;
- at least one funny failure was understood without a post-hoc explanation;
- at least one player asked for a replay or another mission;
- all critical confusion was recorded with mission, moment, and player quote.

If those are not true, do not call the deep-slice gate passed. Convert the smallest blocking findings
into Unity-scoped fixes, keep PlayMode green, and rerun this runbook.
