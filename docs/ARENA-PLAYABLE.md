# Arena Playable — Backyard Mission: Breakfast Rescue

`unity/CheddarAndCocoa/Assets/Scenes/ArenaScene.unity` is now a small co-op vertical slice instead of a flat treat loop. The scene still builds itself from `ArenaBootstrap`, but the round objective is a backyard rescue mission: Cheddar and Cocoa must recover breakfast/weenies, stop a squirrel from stealing too much food, complete a shared rope tug, and stand together against one predator scare before time runs out.

For current global character art direction, read `docs/ART-DIRECTION.md`. Backyard Mission is the
playable proof of that direction, not the only place the direction applies.

## Objective

Clear the mission by completing all required objectives before the timer expires:

1. Recover enough **Breakfast/Weenies** (`6` items in the current prototype).
2. Keep the **Squirrel** from stealing too many items (`3` stolen food ends the run).
3. Resolve the **Predator Warning / Predator Attack** with a united-front bark or a rescue.
4. Complete the **Rope/Tug** shared-object objective.

The round can end in **LevelClear** or **GameOver**, and either result can be restarted. Current pacing is hand-tuned for a first two-player playtest: a 75-second timer, a 5-second mission intro banner, delayed first squirrel pressure, an ~18-second predator telegraph, and slower tug charge so both players have to stay committed for a moment.

The opening HUD banner says: **Cheddar + Cocoa must protect the weenies together.** For the first few seconds, the squirrel is labeled **Squirrel: WAITING**, the predator is **Predator: OFFSCREEN**, and the rope is **Rope/Tug - BOTH DOGS**. This is intentional: players should first read their spawn, dog identity, first weenie arrows, and shared fantasy before the first threat competes for attention.

The replay loop is intentionally simple: players see current score, the latest score swing, and an end-of-run summary with outcome, final score, funny rank, and replay prompt. The exposed deterministic state is `Score`, `LastScoreDelta`, `LastScoreEventLabel`, `Outcome`, `EndRank`, `EndSummaryLabel`, and `ReplayPromptVisible`.

## Controls

| Player | Dog | Controller | Keyboard | Bark | Interact |
| --- | --- | --- | --- | --- | --- |
| P1 | Cheddar | Gamepad slot 0 | WASD | Space / X button | Y button |
| P2 | Cocoa | Gamepad slot 1 | Arrow keys | Enter / Right Shift / X button | Y button |

After LevelClear or GameOver, replay with **R**, **Enter**, gamepad **Start**, gamepad **South button**, or the on-screen **Replay** button.

Cheddar is the chaos puppy and Cocoa is the steadier veteran. The placeholder sprites are still simple generated shapes, but the dogs now have a reusable global identity direction proven in the arena: both read as long, low miniature dachshunds with visible head, long snout, floppy ear, tiny feet, tail, collar, and expression markers. Cheddar reads as **CHEDDAR CHAOS PUP** with a golden body, red collar, bright chaos tuft/flash, faster wag, and more explosive bark/proud motion. Cocoa reads as **COCOA SPOT QUEEN** with a chocolate body, teal collar, cream chest, spot markings, steadier expression, and tiny queen marker. Idle, run, bark, tug, stunned, rescued, proud, and sad states are exposed through body squash/rotation, tail/head/ear motion, color-shifted labels, and deterministic PlayMode assertions.

Current pose labels:

- Cheddar idle/run: **WIGGLE READY** / **CHAOS ZOOM**.
- Cocoa idle/run: **QUEEN READY** / **SPOT PATROL**.
- Shared action states: **WOOF!**, **TUG!**, **STUNNED**, **RESCUED!**, **PROUD!**, **SAD FLOP**.

Each dog also gets a small objective arrow that appears only when useful. It points to the next actionable target and hides when the dog is already close enough, so it should guide without becoming permanent screen noise. Current arrow priorities are:

1. **BARK RESCUE / PARTNER BARK** during predator grabs.
2. **HUDDLE + BARK** during predator warning.
3. **BARK SQUIRREL** while the squirrel is actively stealing.
4. **BOTH TUG** once enough food has been recovered to make rope coordination the next likely bottleneck.
5. **WEENIE** for nearest breakfast recovery during normal play.

Manual first-20-seconds check: start the scene with two players and do nothing for three seconds. Confirm no squirrel steal happens yet, the intro banner is visible, both dog identity labels are readable, and the arrows are primarily helping players find breakfast/weenies. By roughly 6-8 seconds the first squirrel pressure may begin depending on modifier, and the predator should not compete until the later warning.

## Squirrel pressure

A visible, labeled **Squirrel** periodically picks a breakfast/weenie and runs to steal it. The placeholder now has a tail/nose/eye silhouette so it reads as a small thief before labels are considered. If it reaches the item, the squirrel escapes with food, the team loses score, and the stolen-food counter rises. A single nearby bark interrupts/scares the squirrel briefly; a united bark scares it longer and adds teamwork score.

Current squirrel labels/cues:

- **SQUIRREL STEALING - BARK!** when it has picked a target.
- **SQUIRREL DROPPED IT!** after a successful solo bark scare.
- **SQUIRREL HID FROM DOUBLE WOOF** after a united bark scare.
- **SQUIRREL GOT A WEENIE!** when players miss the steal.

Manual readability check: let the squirrel start stealing once, then bark near it with one dog and confirm the label/cue clearly changes from stealing to dropped/scared. Let it steal once on a later run and confirm the label says it got a weenie, the stolen counter rises, and the fail state is distinct if stolen food reaches `3`.

## Predator scare

Once per round, a **Predator Warning** telegraphs danger and targets one dog. The placeholder is a dark/red wing-and-eye shadow, not a normal arena object. If both dogs are close together and bark within the united-bark timing window, the predator is driven away for a large score reward. If the team fails the warning, **Predator Attack** grabs/stuns the target dog. The other dog can rescue by coming close and barking; failure costs score/time pressure but does not instantly end the game.

Manual readability check: when the warning starts, the HUD says a shadow is over the targeted dog, the predator label becomes **HUDDLE + BARK!**, and both dogs' arrows should point toward each other with **HUDDLE + BARK** if they are separated. A successful huddle bark should show **DOUBLE WOOF drove the predator away!** and move the predator to **PREDATOR YEETED** offscreen. During an attack, the grabbed dog should read **STUNNED**, the partner arrow should say **BARK RESCUE**, and a successful partner bark should show a distinct rescue cue: **bark-rescued their sibling - heroic nonsense!** The rescue cue is deliberately separate from united bark feedback.

## Rope/Tug shared-object mechanic

The labeled, pulsing **Rope/Tug** object is a required co-op objective. Its placeholder is a horizontal yellow/brown striped tug rope with visible ends so it is not confused with food. Either dog can interact near the rope for progress, but the main completion path is both dogs standing together at the rope to charge the tug meter. Finishing tug awards a major score bonus and is required for LevelClear.

Manual readability check: after early food recovery, objective arrows should switch to **BOTH TUG** while dogs are away from the rope. If only one dog reaches the rope, the rope label should call out **WAITING FOR CHEDDAR** or **WAITING FOR COCOA** and the HUD should say both dogs must commit together. When both dogs stand on the rope, their labels briefly read **TUG!**, the rope label changes to **BOTH TUGGING X%**, and completion flips the rope label to **ROPE COMPLETE!**.

## United bark

Bark remains visible through expanding bark rings, but now affects gameplay:

- scares or interrupts the squirrel;
- resolves the predator warning/attack when both dogs are close and timed;
- rescues a grabbed/stunned dog when the partner is close;
- awards teamwork score with a cooldown so it cannot be spammed every frame.

Manual readability check: each bark should pop the dog into a **WOOF!** pose label and still show the expanding bark ring. A solo bark away from targets gives a joking solo-bark cue. A successful united bark gives a **DOUBLE WOOF** cue and is protected for a short moment so the second same-moment bark does not visually downgrade it back to solo feedback. During predator warning, the united bark should drive the predator away immediately.

## Scoring, ranks, and replay

The score model is deliberately readable and arcade-simple. Score changes appear as short HUD labels like **+100 UNITED BARK** or **-50 SQUIRREL GOT ONE**:

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

Ranks are deterministic and intentionally funny:

- **Pawfect Yard** — clear with `1500+` score.
- **Backyard Heroes** — clear with `1000+` score.
- **Snack Survivors** — any run with `350+` score that does not hit the higher clear ranks.
- **Needs More Bark** — low-score clear or fail.

LevelClear displays a 1-3 star rating based on final score, the center banner reads **BACKYARD SAVED! [rank]**, and both dogs hold a **PROUD!** pose. GameOver displays **MISSION FAILED! [rank]**, applies the game-over penalty, and both dogs hold a **SAD FLOP** pose. The end card includes `Outcome: Score - Rank`, the last score swing, stars, and **Press R / Enter / Start to replay the weenie rescue**.

Manual score/replay readability check: during both clear and fail, confirm the final dog poses are still visible behind/around the end card, the score swing label remains signed and cause-first, the funny rank is readable, and the replay prompt clearly refers back to the weenie rescue.

## Two-player playtest script

Use this short script before starting a new level:

1. Start `ArenaScene` with two players and read the opening at gameplay zoom. Confirm both players can answer: who am I, where is my first weenie, what is the shared mission?
2. Before moving, confirm Cheddar and Cocoa are distinct without reading only text: long low bodies, Cheddar's golden/red chaos read, Cocoa's chocolate/teal spot-queen read, visible snouts, ears, tiny feet, and collars.
3. Move both dogs and bark with each. Confirm Cheddar's run reads like **CHAOS ZOOM**, Cocoa's like **SPOT PATROL**, and both bark poses still pop without covering objective arrows.
4. Let the first squirrel steal attempt start. Have one player bark near it; confirm the tail/nose/eye squirrel silhouette and steal/scare labels make cause and effect obvious.
5. Recover at least three weenies. Confirm the weenie bun/mustard marker reads as food and arrows switch toward **BOTH TUG** without feeling noisy once players are close to targets.
6. Send only one dog to the rope. Confirm the striped rope object and waiting-for-partner label make the required cooperation obvious.
7. Send both dogs to the rope. Confirm both dog pose labels and rope progress communicate a shared tug.
8. On the predator warning, first try the correct huddle + bark. Restart and then intentionally fail the warning once to see the grab/rescue path. Confirm the predator reads as a red/dark wing-and-eye threat, the grabbed dog reads **STUNNED**, and the partner rescue arrow is more important than decorative motion.
9. Watch score event labels during each major action: weenie, squirrel scare/steal, united bark, predator hit/defense, rescue, tug, clear, and fail.
10. Finish a clear run and a failed run. Confirm the clear/fail banners, proud/sad dog poses, final score, funny rank, and replay instructions are impossible to miss.
11. Press **R**, **Enter**, gamepad **Start**, gamepad **South**, or the **Replay** button. Confirm the run resets to score `0`, `Outcome: InProgress`, no replay prompt, and the intro banner returns.

## Round modifiers

Each restart deterministically selects one seeded modifier for tests/HUD:

- **Squirrel Trouble** — squirrel acts faster.
- **Zoomies Surge** — periodic dog speed bursts make control livelier.
- **Pancake Panic** — stolen food hurts more, representing faster pressure buildup.

## Known limitations

- All mission actors use placeholder sprites/text labels generated at runtime; there are no external art assets yet.
- Dog identity art, pose labels, collars, expression markers, prop silhouettes, and objective arrows are generated placeholders. They are intentionally readable and easy to delete once authored sprites/animation exist.
- The squirrel and predator use intentionally simple movement/state rules so the PlayMode tests remain deterministic.
- The intro, bark, squirrel, predator, tug, clear, and fail feedback are still text/scale/audio-placeholder driven; they are designed to be replaced by authored animation/SFX later.
- Scoring is intentionally flat and local-only. There is no save file, leaderboard, unlock economy, or persistent progression yet.
- The end rank is based only on final score and clear/fail state; it does not yet account for style, dog-specific contributions, or advanced co-op medals.
- Tug is proximity/progress based, not a full physics rope.
- Predator targeting and modifier selection are seeded but still prototype-simple.
- The scene now has basic procedural sound cues, an arena `AudioListener`, and simple placeholder animation, but real prefab art, authored animation, better SFX, and richer rescue/tug feel are still future work.

## Test coverage

`unity/Assets/Tests/PlayMode/ArenaGameLoopPlayModeTests.cs` loads ArenaScene and verifies dogs, mission state, intro prompt/banner, delayed first squirrel steal window, initial score state, item recovery scoring, squirrel steal/scare labels and score events, solo/united bark feedback and scoring, predator defense scoring, failed predator hit and rescue scoring, tug waiting/together feedback and scoring, LevelClear score/rank/summary, GameOver score/rank/summary, replay prompt visibility, restart reset state, exposed modifier state, dog identity labels, dog pose labels, objective-arrow labels, and the generated arena audio listener.
