# Arena Playable — Mission Variety Spike

`unity/CheddarAndCocoa/Assets/Scenes/ArenaScene.unity` is now a small co-op vertical slice instead of a flat treat loop. The scene still builds itself from `ArenaBootstrap`, but the arena can run multiple small mission variants through one lightweight mission definition path. A cold start now opens a generated in-scene mission select so a new player can choose Backyard Rescue, Snack Heist, or Sock Panic without a developer explaining debug keys.

For current global character art direction, read `docs/ART-DIRECTION.md`. Backyard Mission is the
playable proof of that direction, not the only place the direction applies.

## Cold-start flow

1. Open `unity/CheddarAndCocoa` in Unity 6 LTS, open `Assets/Scenes/ArenaScene.unity`, and press Play. `ArenaScene` is also the scripted local build entry point.
2. The mission picker appears immediately. Use **Up/Down** or gamepad **D-pad** to highlight a mission, then press **Enter**, **Space**, gamepad **Start**, or gamepad **South** to start. Keyboard **1 / 2 / 3** also starts Backyard Rescue, Snack Heist, or Sock Panic directly.
3. Read the one-line mission briefing at the start of the round. The HUD keeps the current mission name, objective, score, timer, controls, modifier, and latest score event visible during play.
4. When a mission ends, choose **Replay**, **Next Mission**, or **Mission Select** with the on-screen buttons, keyboard, or gamepad:
   - **R / Enter / Start / South** replays the current mission.
   - **N / Right Arrow / Right Shoulder / D-pad Right** starts the next unfinished mission.
   - **M / Escape / East button / D-pad Left** returns to mission select.
5. After all three mission variants have ended at least once in the session, the Next action opens a simple **Session Summary** with missions played, total score, stars, and ranks earned.

No progress is saved; the session loop is intentionally local and lightweight.

## Mission variants

### Backyard Rescue

This is the existing mission loop and remains the default when `ArenaScene` starts. Clear the mission by completing all required objectives before the `90` second timer expires:

1. Recover enough **Breakfast/Weenies** (`6` items in the current prototype).
2. Keep the **Squirrel** from stealing too many items (`3` stolen food ends the run).
3. Resolve the **Predator Warning / Predator Attack** with a united-front bark or a rescue.
4. Complete the **Rope/Tug** shared-object objective.

Unique scoring/events include **+50 WEENIE SAVED**, **+25 SQUIRREL SCARED**, **+300 PREDATOR YEETED**, **+250 PARTNER RESCUE**, **+200 TUG COMPLETE**, and **+500 LEVEL CLEAR** plus time bonus.

### Snack Heist

Snack Heist is a compact protect-and-collect mission using the existing item and squirrel systems without predator or tug requirements. Cheddar and Cocoa must stash `4` forbidden snacks before the squirrel steals `2` within `70` seconds.

Readable differences:

- Collectibles are round orange snack placeholders with plate/crumb markers and **Snack!** labels.
- Objective text starts as **Stash snacks 0/4**.
- Squirrel pressure uses snack-specific labels like **SQUIRREL SNACK HEIST - BARK!** and **SQUIRREL STOLE A SNACK!**.
- Unique scoring/events include **+60 SNACK STASHED**, **+35 SNACK GUARD BARK**, **-90 SNACK THIEF**, and **SNACK HEIST CLEAR**.
- Clear banner: **SNACK STASH SAVED!**.
- Fail reason calls out the squirrel union escaping with forbidden snacks.

Manual check: press **2**, collect one snack, confirm **+60 SNACK STASHED** and **Stash snacks 1/4**. Let or force the squirrel to steal twice and confirm GameOver/replay uses Snack Heist copy.

### Sock Panic

Sock Panic is a time-boxed collect mission using the same arena, dogs, score/replay loop, and generated prop path, but with squirrel, predator, and rope actors disabled. Cheddar and Cocoa must retrieve `5` scattered socks before the `55` second timer expires.

Readable differences:

- Collectibles are blue sock placeholders with cuff/toe/stripe markers and **Sock!** labels.
- Objective text starts as **Return socks 0/5**.
- Unique scoring/events include **+40 SOCK RESCUED** and **SOCK PANIC CLEAR**.
- Clear banner: **SOCKS SORTED!**.
- Time failure reason: **Laundry order returned before the final sock was rescued.**

Manual check: press **3**, collect one sock, confirm **+40 SOCK RESCUED** and **Return socks 1/5**. Let the timer expire and confirm Sock Panic replay/fail copy.

## Objective

The round can end in **LevelClear** or **GameOver**, and either result can be restarted. Current pacing is hand-tuned for a first two-player playtest: `90 / 70 / 55` second mission timers, a 5-second mission intro banner, delayed first squirrel pressure, an ~25-second predator telegraph in Backyard Rescue, and a short but readable tug charge so both players have to stay committed for a moment.

The opening HUD banner says: **Cheddar + Cocoa must protect the weenies together.** For the first few seconds, the squirrel is labeled **Squirrel: WAITING**, the predator is **Predator: OFFSCREEN**, and the rope is **Rope/Tug - BOTH DOGS**. This is intentional: players should first read their spawn, dog identity, first weenie arrows, and shared fantasy before the first threat competes for attention.

The end loop is intentionally simple: players see current score, the latest score swing, a short reason line, session totals, and Replay / Next Mission / Mission Select actions. Score deltas now appear both as a brief HUD pop and as small world text near the action so cause/effect is easier to read during chaos. The exposed deterministic state includes `CurrentFlow`, `MissionSelectVisible`, `SelectedMissionVariant`, `Score`, `LastScoreDelta`, `LastScoreEventLabel`, `LastScorePopLabel`, `ScorePopVisible`, `ObjectiveLabel`, `Outcome`, `EndRank`, `EndSummaryLabel`, `EndReasonLabel`, `ReplayPromptVisible`, `EndReplayAvailable`, `EndNextMissionAvailable`, `EndMissionSelectAvailable`, `SessionMissionsPlayed`, `SessionTotalScore`, `SessionStarsEarned`, `SessionUniqueMissionsCompleted`, `SessionSummaryLabel`, `LastJuiceFeedback`, and `LastJuiceLabel`.

## Controls

| Player | Dog | Controller | Keyboard | Bark | Interact |
| --- | --- | --- | --- | --- | --- |
| P1 | Cheddar | Gamepad slot 0 | WASD | Space / X button | Y button |
| P2 | Cocoa | Gamepad slot 1 | Arrow keys | Enter / Right Shift / X button | Y button |

Mission flow controls:

- Mission select: **Up/Down** or gamepad **D-pad** changes mission; **Enter**, **Space**, gamepad **Start**, or gamepad **South** starts; **1 / 2 / 3** starts a mission directly.
- During a run: keyboard **1 / 2 / 3** still restarts the arena into Backyard Rescue, Snack Heist, or Sock Panic for quick manual comparison.
- End screen: **R / Enter / Start / South** replays; **N / Right Arrow / Right Shoulder / D-pad Right** advances; **M / Escape / East / D-pad Left** returns to mission select.
- Session Summary: **Enter**, **Space**, **Start**, **South**, **M**, or **Escape** returns to mission select.
- Playtest Mode: click the bottom-left **Playtest Mode: On/Off** button or press **F1** / **`**. It toggles a compact top-right diagnostics overlay and does not pause or block normal play.

## First playtest protocol

Use this protocol before changing tuning again:

1. Open `unity/CheddarAndCocoa` in Unity 6 LTS, open `Assets/Scenes/ArenaScene.unity`, press Play, and turn **Playtest Mode** on.
2. Start on mission select. Do not explain the game at first; let the two players try to identify their dogs, choose a mission, move, bark, and react.
3. Watch silently for the first run unless they are fully blocked by hardware/input confusion.
4. After the first end screen, ask short questions, then have them use **Replay** and **Next Mission** without a mouse.
5. Run all three missions at least once: **Backyard Rescue**, **Snack Heist**, and **Sock Panic**.
6. After all three have ended, use **Next / Session Summary** and confirm the players understand the session totals.
7. Write down confusion points using the playtest overlay and event log rather than fixing during the session.

What to observe:

- Can each player identify whether they are Cheddar or Cocoa without only reading labels?
- Do players understand the current objective label before the first squirrel/predator pressure arrives?
- Does bark feel like a useful verb, especially for squirrel, united-front defense, and rescue?
- Does the rope/tug objective cause communication or just confusion?
- Do score pops and world pops explain why score changed?
- Do players naturally find Replay, Next Mission, and Mission Select?
- Are failures funny/recoverable, or do they feel arbitrary?
- Does the camera keep both dogs, objectives, and hazards readable?

Ask testers:

1. What did you think the goal was when the mission started?
2. Which dog were you, and how could you tell?
3. When did barking feel useful?
4. Was there a moment where you did not know what to do next?
5. What did you think the squirrel was doing?
6. What did you think the predator warning wanted from both players?
7. Did tugging the rope feel cooperative?
8. Did the end screen make Replay and Next Mission clear?
9. Which mission would you replay first?
10. What was funny or personal, and what felt generic?

Known rough edges for this playtest:

- Placeholder art and generated tones are still prototype-only.
- There is no final tutorial; the objective label, arrows, labels, and playtest observer have to carry clarity.
- Keyboard two-player controls share one keyboard, so controller play is the better test if two gamepads are available.
- The visible Playtest Mode button is an IMGUI debug control and may overlap a tiny corner of the play space.
- Local session totals reset when Play mode or the app exits.
- Event logs are in-memory only; there is no file export yet.

## Local Mac build path

Use this only for a quick couch playtest build; no signing, installer, store, or distribution work is needed.

From the repo root:

```sh
./unity/build-dev.sh
```

Expected result:

- Unity runs in batch mode with `CheddarAndCocoa.EditorTools.ArenaDevBuild.BuildDevMac`.
- The only scene in the build player options is `Assets/Scenes/ArenaScene.unity`.
- The output app is written to `unity/builds/dev/CheddarAndCocoa-Arena.app`.
- The script ends with `Development build ready: .../unity/builds/dev/CheddarAndCocoa-Arena.app`.

Manual fallback if batch mode is unavailable:

1. Open Unity Hub and launch `unity/CheddarAndCocoa` with Unity 6 LTS.
2. Open `Assets/Scenes/ArenaScene.unity`.
3. Confirm `File -> Build Profiles...` (or `File -> Build Settings...` on older Unity UI) includes `Assets/Scenes/ArenaScene.unity`; it is already listed in `ProjectSettings/EditorBuildSettings.asset`.
4. Select **macOS** as the target platform and enable **Development Build**.
5. Choose **Build** or **Build And Run**.
6. Save the output somewhere local and ignored, for example `unity/builds/dev/CheddarAndCocoa-Arena.app`.
7. Launch the built app, start each mission from mission select, and press **F1** / **`** or the bottom-left Playtest Mode button to verify the overlay is available.

Before handing off a build, run:

```sh
./unity/run-playmode-tests.sh
```

For one command that checks project structure, scene wiring, PlayMode tests, and the local dev build:

```sh
./unity/validate-demo.sh
```

Use `./unity/validate-demo.sh --skip-build` when validating on a machine that can run tests but does not have macOS build support installed.

## Playtest/debug visibility

`GameManager` owns a lightweight in-memory `PlaytestEventLog` for the current Unity Play session. The log is exposed through `PlaytestLog`, `PlaytestEvents`, and `LastPlaytestEvent`, so PlayMode tests or a temporary inspector can read it without scraping UI. Events are numbered and deterministic rather than timestamped. Captured event types include mission select/start, objective changes, bark, collection, squirrel pressure/scare/steal, tug/rescue, score deltas, clear/fail, replay, next mission, overlay toggles, and session summary.

The playtest overlay shows mission, flow/phase, timer, score, last score event, current objective, fail pressure, Cheddar/Cocoa positions, current-round friction counters, failures by mission, session totals, outcome/rank, and the latest event. Friction counters are intentionally small and local: `BarksUsed`, `FailedInteractions`, `ObjectiveChangeCount`, `MissionDurationSeconds`, `MissionReplayCount`, and `FailuresForMission(...)`. They are meant to flag confusion for a human observer, not to become analytics.

Manual check: press **F1** during mission select and during play, or click the bottom-left Playtest Mode button. Confirm the overlay appears in the top-right, does not hide the dogs or end buttons, and updates after collecting an item, barking, missing an interaction, forcing a fail/clear, replaying, and choosing Next Mission.

Cheddar is the chaos puppy and Cocoa is the steadier veteran. The placeholder sprites are still simple generated shapes, but the dogs now have a reusable global identity direction proven in the arena: both read as long, low miniature dachshunds with visible head, long snout, floppy ear, tiny feet, tail, collar, and expression markers. Cheddar reads as **CHEDDAR CHAOS PUP** with a golden body, red collar, bright chaos tuft/flash, faster wag, and more explosive bark/proud motion. Cocoa reads as **COCOA SPOT QUEEN** with a chocolate body, teal collar, cream chest, spot markings, steadier expression, and tiny queen marker. Idle, run, bark, tug, stunned, rescued, proud, and sad states are exposed through body squash/rotation, tail/head/ear motion, color-shifted labels, and deterministic PlayMode assertions.

Current pose labels:

- Cheddar idle/run: **WIGGLE READY** / **CHAOS ZOOM**.
- Cocoa idle/run: **QUEEN READY** / **SPOT PATROL**.
- Shared action states: **WOOF!**, **TUG!**, **STUNNED**, **RESCUED!**, **PROUD!**, **SAD FLOP**.

Each dog also gets a small objective arrow that appears only when useful. It points to the next actionable target and hides when the dog is already close enough, so it should guide without becoming permanent screen noise. Each dog also has a tiny movement intent marker while running: Cheddar uses a red chaos arrow and Cocoa uses a teal veteran arrow. The intent marker is deliberately smaller than collars/labels and exists only to keep facing/movement readable at gameplay zoom. `FacingIntentLabel` exposes the deterministic left/right direction for tests.

Movement now has a first feel pass instead of instant velocity snaps. Cheddar is a little faster and more impulsive; Cocoa is slightly steadier, brakes harder, and cuts more cleanly. Both dogs accelerate, decelerate, snap tiny drift to a stop, lean into motion, squash/stretch subtly while running, and leave short-lived accent-color paw trail placeholders. These are prototype readability marks, not final effects.

Current movement defaults in ArenaScene:

- Cheddar: base speed `4.8` prototype units (`5.76` Unity units/sec after conversion), acceleration `34`, deceleration `31`, turn response `46`, zoomies multiplier `1.85`.
- Cocoa: base speed `4.55` prototype units (`5.46` Unity units/sec after conversion), acceleration `29`, deceleration `39`, turn response `52`, zoomies multiplier `1.75`.
- Both: input deadzone `0.25`, stop snap `0.08` Unity units/sec, and run feedback threshold `0.22`.

Manual movement feel check: move each dog from rest, reverse direction, circle around a weenie, and release the stick/keys near the rope. Confirm the dogs feel quick and cute, Cheddar reads a little more chaotic, Cocoa reads more controlled, and neither dog slides past small targets in a frustrating way. The paw trails should appear only while moving and should not compete with objective arrows, labels, or score pops.

The shared camera remains a 2D orthographic backyard camera matching `docs/ART-DIRECTION.md`, but its arena framing is now tuned for readability across all three current missions. It frames dog bounds using horizontal/vertical margins instead of diagonal distance, with a narrow zoom band so the arena, mission props, and HUD context stay readable while still letting dog animation read.

Current camera defaults in ArenaScene:

- Initial orthographic size `7.1`.
- Min/max orthographic size `6.8 / 8.4`.
- Horizontal/vertical framing margin `3.0 / 2.2`.
- Follow/zoom lerp `9 / 7`.
- Bounds clamping is off for this generated arena so the full backyard context remains stable.

Manual camera check: play Backyard Rescue, Snack Heist, and Sock Panic from mission select. Confirm the dogs, nearest collectibles, squirrel pressure, rope, predator warning, score pop area, and end card are readable without losing the 2.5D/isometric-ish backyard feel. The HUD can occupy the top of the screen, but it should not hide the active dog work or mission props.

The HUD objective label is derived from game state, not hand-authored timing. Current objective states include **Save weenies X/6**, **Bark to scare squirrel**, **Huddle + bark at the shadow**, **Rescue Cheddar/Cocoa**, **Both dogs tug the rope**, **Clear the yard together**, and the clear/fail replay states.

Current arrow priorities are:

1. **BARK RESCUE / PARTNER BARK** during predator grabs.
2. **HUDDLE + BARK** during predator warning.
3. **BARK SQUIRREL** while the squirrel is actively stealing.
4. **BOTH TUG** once enough food has been recovered to make rope coordination the next likely bottleneck.
5. **WEENIE** for nearest breakfast recovery during normal play.

Manual first-20-seconds check: start the scene with two players and do nothing for three seconds. Confirm no squirrel steal happens yet, the intro banner is visible, the HUD objective says to save weenies, both dog identity labels are readable, and the arrows are primarily helping players find breakfast/weenies. Move each dog left and right and confirm the small intent marker appears only while running and does not compete with the identity label. By roughly 6-8 seconds the first squirrel pressure may begin depending on modifier, and the predator should not compete until the later warning.

## Squirrel pressure

A visible, labeled **Squirrel** periodically picks a breakfast/weenie and runs to steal it. The placeholder now has a tail/nose/eye silhouette so it reads as a small thief before labels are considered. If it reaches the item, the squirrel escapes with food, the team loses score, and the stolen-food counter rises. A single nearby bark interrupts/scares the squirrel briefly; a united bark scares it longer and adds teamwork score.

Current squirrel labels/cues:

- **SQUIRREL STEALING - BARK!** when it has picked a target.
- **SQUIRREL DROPPED IT!** after a successful solo bark scare.
- **SQUIRREL HID FROM DOUBLE WOOF** after a united bark scare.
- **SQUIRREL GOT A WEENIE!** when players miss the steal.

Manual readability check: let the squirrel start stealing once, then bark near it with one dog and confirm the HUD objective changes to **Bark to scare squirrel**, the label/cue clearly changes from stealing to dropped/scared, and a small **DROP!** world pop appears. Let it steal once on a later run and confirm the label says it got a weenie, the score shows a signed negative pop, a **MISS! -WEENIE** world pop appears near the squirrel, the stolen counter rises, and the fail state is distinct if stolen food reaches `3`.

When the squirrel is actively stealing, a temporary **BARK RANGE** ring appears around it. The ring uses the same placeholder ring language as bark feedback and hides outside the actionable state.

## Predator scare

Once per round, a **Predator Warning** telegraphs danger and targets one dog. The placeholder is a dark/red wing-and-eye shadow, not a normal arena object. If both dogs are close together and bark within the united-bark timing window, the predator is driven away for a large score reward. If the team fails the warning, **Predator Attack** grabs/stuns the target dog. The other dog can rescue by coming close and barking; failure costs score/time pressure but does not instantly end the game.

Manual readability check: when the warning starts, the HUD objective says **Huddle + bark at the shadow**, the predator label becomes **HUDDLE + BARK!**, and both dogs' arrows should point toward each other with **HUDDLE + BARK** if they are separated. A successful huddle bark should show **DOUBLE WOOF drove the predator away!**, create a **DOUBLE WOOF!** success pop, and move the predator to **PREDATOR YEETED** offscreen. During an attack, the grabbed dog should read **STUNNED**, the HUD objective should say **Rescue Cheddar/Cocoa**, the partner arrow should say **BARK RESCUE**, and a successful partner bark should show a distinct rescue cue plus **RESCUED!** world pop. The rescue cue is deliberately separate from united bark feedback.

During a predator grab, the grabbed dog shows a temporary **RESCUE BARK** range ring. This is meant to explain rescue distance without adding permanent clutter.

## Rope/Tug shared-object mechanic

The labeled, pulsing **Rope/Tug** object is a required co-op objective. Its placeholder is a horizontal yellow/brown striped tug rope with visible ends so it is not confused with food. Either dog can interact near the rope for progress, but the main completion path is both dogs standing together at the rope to charge the tug meter. Finishing tug awards a major score bonus and is required for LevelClear.

Manual readability check: after early food recovery, the HUD objective and objective arrows should switch to **BOTH TUG** while dogs are away from the rope. If only one dog reaches the rope, the rope label should call out **WAITING FOR CHEDDAR** or **WAITING FOR COCOA** and the HUD should say both dogs must commit together. When both dogs stand on the rope, their labels briefly read **TUG!**, the rope label changes to **BOTH TUGGING X%**, and completion flips the rope label to **ROPE COMPLETE!** with a **TUG POP!** world pop.

After enough food recovery makes tug the next likely bottleneck, the rope shows a temporary **BOTH DOGS** range ring. It uses the current `1.6` tug-together radius and hides on non-tug missions.

## United bark

Bark remains visible through expanding bark rings and a short comic **BARK!** burst, but now affects gameplay:

- scares or interrupts the squirrel;
- resolves the predator warning/attack when both dogs are close and timed;
- rescues a grabbed/stunned dog when the partner is close;
- awards teamwork score with a cooldown so it cannot be spammed every frame.

Manual readability check: each bark should pop the dog into a **WOOF!** pose label and show both the expanding bark ring and comic bark burst. A solo bark away from targets gives a joking solo-bark cue and sets `LastJuiceFeedback` to `BarkBurst`. A successful united bark gives a **DOUBLE WOOF** cue and is protected for a short moment so the second same-moment bark does not visually downgrade it back to solo feedback. During predator warning, the united bark should drive the predator away immediately.

## Scoring, ranks, and replay

The score model is deliberately readable and arcade-simple. Score changes appear as short HUD labels and world pops like **+100 UNITED BARK** or **-50 SQUIRREL GOT ONE**:

- breakfast/weenie recovery: **+50 WEENIE SAVED**;
- single-dog squirrel scare: **+25 SQUIRREL SCARED**;
- united bark teamwork: **+100 UNITED BARK**;
- predator defended: **+300 PREDATOR YEETED**;
- rescue after failed predator attack: **+250 PARTNER RESCUE**;
- tug objective: **+200 TUG COMPLETE**;
- LevelClear: **+500 LEVEL CLEAR** plus `5 x remaining seconds`;
- squirrel steal: **-50 SQUIRREL GOT ONE** or **-80 SQUIRREL GOT ONE** during Pancake Panic;
- predator hit after missed warning: **-150 PREDATOR HIT**;
- GameOver: **-100 GAME OVER**.

Ranks are deterministic and intentionally funny. Thresholds are mission-specific so shorter compact missions can still award sensible stars:

- **Backyard Rescue**: Pawfect Yard `1500+`, Backyard Heroes `1050+`, Snack Survivors `350+`.
- **Snack Heist**: Pawfect Yard `950+`, Backyard Heroes `700+`, Snack Survivors `250+`.
- **Sock Panic**: Pawfect Yard `800+`, Backyard Heroes `600+`, Snack Survivors `200+`.
- **Needs More Bark** — any score below the mission's survivor threshold.

LevelClear displays a 1-3 star rating based on final score, the center banner reads **BACKYARD SAVED! [rank]**, and both dogs hold a **PROUD!** pose. GameOver displays **MISSION FAILED! [rank]**, applies the game-over penalty, and both dogs hold a **SAD FLOP** pose. The end card includes `Outcome: Score - Rank`, one short funny `EndReasonLabel`, the last score swing, stars, session totals, and Replay / Next Mission / Mission Select actions.

Manual score/replay readability check: during both clear and fail, confirm the final dog poses are still visible behind/around the end card, the score swing label remains signed and cause-first, the funny rank and reason line are readable, and Replay / Next Mission / Mission Select are all usable without a mouse.

## Non-developer playtest script

Use this when handing the prototype to someone who has not seen the code:

1. Start `ArenaScene` and say nothing. Confirm the player can identify that the first screen is mission select and can start a mission using keyboard or controller only.
2. Ask them to read the controls line and mission briefing out loud. Confirm they can answer: how do I move, bark, tug/rescue, replay, and go next?
3. Have them play **Backyard Rescue** until either clear or fail. Confirm they understand the current mission name, objective label, dog identities, score pop, and end choices.
4. On the end screen, ask them to choose **Next Mission** without using a mouse. Confirm the next unfinished mission starts and shows its own briefing.
5. Have them finish **Snack Heist** and **Sock Panic** by any outcome. Confirm the session totals increment after each ended mission.
6. After all three missions have ended, choose **Next / Session Summary** and confirm missions played, total score, stars, and ranks are readable.
7. Return to mission select and replay any mission. Confirm session totals are not persistent across stopping Play mode.

## Two-player playtest script

Use this short script before starting a new level:

1. Start `ArenaScene` with two players and read the opening at gameplay zoom. Confirm both players can answer: who am I, where is my first weenie, what is the shared mission?
2. Before moving, confirm Cheddar and Cocoa are distinct without reading only text: long low bodies, Cheddar's golden/red chaos read, Cocoa's chocolate/teal spot-queen read, visible snouts, ears, tiny feet, and collars.
3. Move both dogs and bark with each. Confirm Cheddar's run reads like **CHAOS ZOOM**, Cocoa's like **SPOT PATROL**, tiny facing/intent arrows appear only during movement, and both bark poses still pop without covering objective arrows.
4. Let the first squirrel steal attempt start. Have one player bark near it; confirm the tail/nose/eye squirrel silhouette, HUD objective, steal/scare labels, and drop/miss pops make cause and effect obvious.
5. Recover at least three weenies. Confirm the weenie bun/mustard marker reads as food and arrows switch toward **BOTH TUG** without feeling noisy once players are close to targets.
6. Send only one dog to the rope. Confirm the striped rope object and waiting-for-partner label make the required cooperation obvious.
7. Send both dogs to the rope. Confirm both dog pose labels and rope progress communicate a shared tug.
8. On the predator warning, first try the correct huddle + bark. Restart and then intentionally fail the warning once to see the grab/rescue path. Confirm the predator reads as a red/dark wing-and-eye threat, the grabbed dog reads **STUNNED**, and the partner rescue arrow is more important than decorative motion.
9. Watch score event labels and world pops during each major action: weenie, squirrel scare/steal, united bark, predator hit/defense, rescue, tug, clear, and fail.
10. Finish a clear run and a failed run. Confirm the clear/fail banners, proud/sad dog poses, final score, funny rank, one-line reason, and replay instructions are impossible to miss.
11. Press **R**, **Enter**, gamepad **Start**, gamepad **South**, or the **Replay** button. Confirm the run resets to score `0`, `Outcome: InProgress`, no replay prompt, and the intro banner returns.

## Round modifiers

Each restart deterministically selects one seeded modifier for tests/HUD:

- **Squirrel Trouble** — squirrel acts faster.
- **Zoomies Surge** — periodic dog speed bursts make control livelier.
- **Pancake Panic** — stolen food hurts more, representing faster pressure buildup.

## Tuning guide

Mission, camera, and interaction-range tuning is centralized in `unity/CheddarAndCocoa/Assets/Scripts/Game/ArenaMissionTuning.cs`. Per-dog movement feel fields live on `unity/CheddarAndCocoa/Assets/Scripts/Data/DogTuning.cs` and the current ArenaScene runtime values are assigned in `ArenaBootstrap` until authored dog tuning assets exist. Adjust those files first for playtest balancing; avoid scattering one-off numbers in `GameManager`.

Current key defaults:

- Timers: Backyard Rescue `90s`, Snack Heist `70s`, Sock Panic `55s`.
- Rewards: item scores `50 / 60 / 40`, united bark `+100`, predator defended `+300`, partner rescue `+250`, tug complete `+200`, clear bonus `+500`, time bonus `5 x remaining seconds`.
- Penalties: Backyard squirrel `-50`, Snack squirrel `-90`, Pancake Panic squirrel `-80`, predator hit `-150`, game over `-100`.
- Squirrel pressure: first steal delay `9.0s` (`7.0s` on Squirrel Trouble), repeat delay `3.4s` (`2.2s` on Squirrel Trouble), move speed `1.9`.
- Bark/rescue/tug: united bark window `0.8s`, united bark range `3.0`, single bark squirrel range `4.0`, rescue bark range `2.0`, tug together distance `1.6`, tug charge `0.5` per second, interact tug bump `0.2`.
- Camera: initial ortho `7.1`, min/max ortho `6.8 / 8.4`, horizontal/vertical margins `3.0 / 2.2`, follow/zoom lerp `9 / 7`.
- Arena dog movement: Cheddar base speed `4.8`, acceleration `34`, deceleration `31`, turn response `46`, zoomies `1.85`; Cocoa base speed `4.55`, acceleration `29`, deceleration `39`, turn response `52`, zoomies `1.75`; both use input deadzone `0.25`, stop snap `0.08`, and run-feedback threshold `0.22`.
- Range hints: squirrel bark ring `4.0`, rescue bark ring `2.0`, tug together ring `1.6`.
- Mission counts: Backyard Rescue spawns `5` items and needs `6` recoveries because collected items respawn; Snack Heist spawns/needs `4`; Sock Panic spawns/needs `5`.

After tuning, run:

```sh
./unity/run-playmode-tests.sh
```

Expected pass signal:

- the shell script exits `0`;
- the Unity log includes a test summary with `failed=0`;
- `unity/playmode-results.xml` is written.

Expected failure signal:

- the shell script exits non-zero;
- the Unity log or XML names the failing PlayMode test;
- if the editor cannot start because of licensing, open Unity Hub, sign in once, and re-run the command.

Known non-fatal Unity log noise:

- Unity may print package import, domain reload, asset refresh, or graphics-device messages during batch mode.
- Generated audio clips, IMGUI overlays, placeholder sprites, and PlayMode event-log messages are expected prototype output.
- The important failure signals are compiler errors, Safe Mode, test failures, missing `ArenaScene`, missing `ArenaBootstrap`, or a failed build result.

The PlayMode tests assert tuning defaults, movement feel defaults, independent dog movement, camera config, interaction range indicator state, the 30-90 second mission timing target, reachable top-rank scoring, cold-start mission select, all three mission ids, replay/next/select/session-summary reachability, distinct Cheddar/Cocoa identity tuning and art slots, overlay state, and deterministic playtest log entries.

## Build/share readiness

For a local development build, use Unity 6 LTS and run:

```sh
./unity/build-dev.sh
```

Output location: `unity/builds/dev/CheddarAndCocoa-Arena.app`.

For the full local demo validation pass, run:

```sh
./unity/validate-demo.sh
```

That script confirms the Unity project path exists, `ArenaScene` exists and is listed in `EditorBuildSettings.asset`, `ArenaScene` contains the `ArenaBootstrap` GameObject and script reference, PlayMode tests pass, and the local development build can be created. Use `--skip-build` only when the machine is intentionally test-only.

No installer, signing, notarization, icon, store packaging, external analytics, or distribution polish is expected for this slice.

Demo readiness checklist:

- `./unity/run-playmode-tests.sh` passes with `failed=0`.
- `./unity/build-dev.sh` creates `unity/builds/dev/CheddarAndCocoa-Arena.app`.
- Cold start opens mission select, not a live round.
- Backyard Rescue, Snack Heist, and Sock Panic start from mission select.
- Replay, Next Mission, Mission Select, and Session Summary are reachable without a mouse.
- Cheddar and Cocoa spawn with distinct identity/readability slots and asymmetric tuning.
- The shared camera initializes and keeps both dogs framed.
- Playtest Mode can be toggled and does not block normal mission flow.
- Keyboard and gamepad controls match the Controls table above.
- Known limitations below are acceptable for this demo handoff.

## Five-minute playtest instructions

Start from `ArenaScene`, press Play, and hand controls to two players without explaining the code. Ask them to play Backyard Rescue, then use **Next Mission** to reach Snack Heist and Sock Panic, and finally open Session Summary after all three have ended. Turn on the **F1** overlay only when the observer needs to inspect state; leave it off for first-read usability.

Observe whether players can identify their dog, start a mission, read the current objective, recover from squirrel/predator/tug pressure, understand clear/fail, replay, and use Next Mission without a mouse. Watch especially for moments where score pops or objective arrows are missed during chaos.

Suggested feedback questions:

- Which dog were you, and how could you tell without reading only the name label?
- What did bark do in each mission?
- When did you know what to do next?
- Did the squirrel/predator/timer feel fair, too slow, or too punishing?
- Did Replay, Next Mission, and Session Summary make sense?
- What was funny or personal, and what felt like generic collecting?

## Visual readability checklist

Use this after any placeholder-art, authored-art, or sprite import change:

- Cheddar and Cocoa still read as different long, low dachshunds at gameplay zoom without relying
  only on text labels.
- Cheddar keeps golden/red chaos reads; Cocoa keeps chocolate/teal spot-queen reads.
- Dog pose states are still distinct: idle, run, bark, tug, stunned, rescued, proud, sad.
- Bark ring/text, objective arrows, score pops, and playtest overlay remain readable but do not hide
  the dogs or core mission objects.
- Movement lean, squash/stretch, and paw trails are readable during motion but do not become constant noise.
- Camera framing keeps all three current missions readable from mission select without changing the orthographic backyard direction.
- Temporary bark, rescue, and tug range rings appear only in actionable states and hide on mission select, end screens, and non-relevant missions.
- Weenie, snack, and sock collectibles are visually distinct from each other and from the squirrel
  and rope.
- Squirrel, predator, and rope expose their expected replacement slots from
  `ArenaArtCatalog` and keep their current role reads: thief, threat, shared tug prop.
- Mission select, end actions, and session summary remain legible with placeholder IMGUI styling.

## Known limitations

- All mission actors use placeholder sprites/text labels generated at runtime; there are no external art assets yet.
- Mission select, end actions, and session summary are generated IMGUI placeholders, not authored production UI.
- The playtest overlay and event log are debug/playtest aids only. They are not analytics, persistence, telemetry, or player-facing production UI.
- Snack Heist and Sock Panic are architecture proofs inside the existing arena, not finished level designs. They reuse the same camera, spawn bounds, scoring HUD, restart flow, and generated placeholder prop system.
- Dog identity art, pose labels, collars, expression markers, prop silhouettes, and objective arrows are generated placeholders. They are intentionally readable and easy to delete once authored sprites/animation exist.
- The squirrel and predator use intentionally simple movement/state rules so the PlayMode tests remain deterministic.
- The intro, bark, squirrel, predator, tug, clear, fail, score-pop, and objective feedback are still text/scale/audio-placeholder driven; they are designed to be replaced by authored animation/SFX later.
- `ForceSquirrelStealAttempt()` exists as a deterministic PlayMode test hook and is not intended as a player-facing control.
- Scoring is intentionally flat and session-local only. There is no save file, leaderboard, unlock economy, or persistent progression yet.
- The end rank is based only on final score and clear/fail state; it does not yet account for style, dog-specific contributions, or advanced co-op medals.
- Tug is proximity/progress based, not a full physics rope.
- Predator targeting and modifier selection are seeded but still prototype-simple.
- The scene now has basic procedural sound cues, an arena `AudioListener`, and simple placeholder animation, but real prefab art, authored animation, better SFX, and richer rescue/tug feel are still future work.
- Camera feel is tuned for the current generated backyard arena only. A larger authored level may need level-specific camera anchors or bounds.
- Paw trails and range rings are placeholder geometry/text. They are useful for playtest readability, not a final VFX style.

## Test coverage

`unity/Assets/Tests/PlayMode/ArenaGameLoopPlayModeTests.cs` loads ArenaScene and verifies mission select initialization, centralized tuning defaults, movement/camera tuning defaults, mission balance invariants, starting each mission through the new flow, end-screen Replay / Next Mission / Mission Select availability, session totals across multiple missions, session summary after all three variants have ended, dogs, independent movement response, camera component/config, interaction range indicator state, mission state, intro prompt/banner, deterministic objective labels, delayed first squirrel steal window, initial score state, item recovery scoring, HUD score-pop state, world score/miss/success pops, playtest overlay state, playtest event log entries, squirrel steal/scare labels and score events, solo/united bark feedback and scoring, bark burst state, predator defense scoring, failed predator hit and rescue scoring, tug waiting/together feedback and scoring, LevelClear score/rank/summary/reason, GameOver score/rank/summary/reason, replay prompt visibility, restart reset state, exposed modifier state, dog identity labels, dog pose labels, movement intent labels/arrows, objective-arrow labels, and the generated arena audio listener.

The same test file also verifies **Snack Heist** and **Sock Panic** can initialize, update objective labels, score unique mission events, reach clear/fail outcomes, and expose replay state. `ControllerCoopPlayModeTests` still verifies the baseline two-pad movement/bark proof and now clears scene objects before constructing its own bootstrap so it does not accidentally inspect leftover ArenaScene dogs from previous tests.
