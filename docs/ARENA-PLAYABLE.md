# Arena Playable — Backyard Mission: Breakfast Rescue

`unity/CheddarAndCocoa/Assets/Scenes/ArenaScene.unity` is now a small co-op vertical slice instead of a flat treat loop. The scene still builds itself from `ArenaBootstrap`, but the round objective is a backyard rescue mission: Cheddar and Cocoa must recover breakfast/weenies, stop a squirrel from stealing too much food, complete a shared rope tug, and stand together against one predator scare before time runs out.

## Objective

Clear the mission by completing all required objectives before the timer expires:

1. Recover enough **Breakfast/Weenies** (`6` items in the current prototype).
2. Keep the **Squirrel** from stealing too many items (`3` stolen food ends the run).
3. Resolve the **Predator Warning / Predator Attack** with a united-front bark or a rescue.
4. Complete the **Rope/Tug** shared-object objective.

The round can end in **LevelClear** or **GameOver**, and either result can be restarted. Current pacing is hand-tuned for a first manual pass: a 75-second timer, an early predator telegraph, and slower tug charge so both players have to stay committed for a moment.

## Controls

| Player | Dog | Controller | Keyboard | Bark | Interact |
| --- | --- | --- | --- | --- | --- |
| P1 | Cheddar | Gamepad slot 0 | WASD | Space / X button | Y button |
| P2 | Cocoa | Gamepad slot 1 | Arrow keys | Enter / Right Shift / X button | Y button |

Cheddar is the chaos puppy and Cocoa is the steadier veteran. The placeholder sprites are still simple generated shapes, but the dogs now have generated identity markings and pose labels: Cheddar reads as **CHEDDAR CHAOS PUP** with warmer golden markings, a faster wag, and louder bark/proud poses; Cocoa reads as **COCOA SPOT QUEEN** with darker chocolate body, chest/spot markings, and a tiny crown marker. Idle, run, bark, tug, stunned, rescued, proud, and sad states are exposed through body squash/rotation, tail motion, color-shifted labels, and deterministic PlayMode assertions.

Each dog also gets a small objective arrow that appears only when useful. It points to the next actionable target and hides when the dog is already close enough, so it should guide without becoming permanent screen noise. Current arrow priorities are:

1. **BARK RESCUE / PARTNER BARK** during predator grabs.
2. **HUDDLE + BARK** during predator warning.
3. **BARK SQUIRREL** while the squirrel is actively stealing.
4. **BOTH TUG** once enough food has been recovered to make rope coordination the next likely bottleneck.
5. **WEENIE** for nearest breakfast recovery during normal play.

## Squirrel pressure

A visible, labeled **Squirrel** periodically picks a breakfast/weenie and runs to steal it. If it reaches the item, the squirrel escapes with food, the team loses score, and the stolen-food counter rises. A single nearby bark interrupts/scares the squirrel briefly; a united bark scares it longer and adds teamwork score.

## Predator scare

Once per round, a **Predator Warning** telegraphs danger and targets one dog. If both dogs are close together and bark within the united-bark timing window, the predator is driven away for a large score reward. If the team fails the warning, **Predator Attack** grabs/stuns the target dog. The other dog can rescue by coming close and barking; failure costs score/time pressure but does not instantly end the game.

Manual readability check: when the warning starts, the HUD says to huddle and bark, the predator label turns bright red, and both dogs' arrows should point toward each other with **HUDDLE + BARK** if they are separated. During an attack, the grabbed dog should read **STUNNED**, the partner arrow should say **BARK RESCUE**, and a successful partner bark should flip the grabbed dog into **RESCUED!** briefly.

## Rope/Tug shared-object mechanic

The labeled, pulsing **Rope/Tug** object is a required co-op objective. Either dog can interact near the rope for progress, but the main completion path is both dogs standing together at the rope to charge the tug meter. Finishing tug awards a major score bonus and is required for LevelClear.

Manual readability check: after early food recovery, objective arrows should switch to **BOTH TUG** while dogs are away from the rope. When both dogs stand on the rope, their labels briefly read **TUG!**, the rope label shows progress, and completion flips the rope label to **ROPE COMPLETE!**.

## United bark

Bark remains visible through expanding bark rings, but now affects gameplay:

- scares or interrupts the squirrel;
- resolves the predator warning/attack when both dogs are close and timed;
- rescues a grabbed/stunned dog when the partner is close;
- awards teamwork score with a cooldown so it cannot be spammed every frame.

Manual readability check: each bark should pop the dog into a **WOOF!** pose label and still show the expanding bark ring. A successful united bark should update the HUD cue and, during predator warning, drive the predator away immediately.

## Scoring and stars

Score is no longer flat +1 only:

- breakfast/weenie recovery: +10;
- single-dog squirrel scare: +3;
- united bark teamwork: +5;
- predator defended: +30;
- rescue after failed predator attack: +8;
- tug objective: +25;
- LevelClear time remaining bonus: remaining seconds;
- squirrel steal / predator failure: score penalties.

LevelClear displays a 1–3 star rating based on the final score. On clear, both dogs hold a **PROUD!** pose; on fail, both dogs hold a **SAD FLOP** pose so the two end states read differently even before final art.

## Round modifiers

Each restart deterministically selects one seeded modifier for tests/HUD:

- **Squirrel Trouble** — squirrel acts faster.
- **Zoomies Surge** — periodic dog speed bursts make control livelier.
- **Pancake Panic** — stolen food hurts more, representing faster pressure buildup.

## Known limitations

- All mission actors use placeholder sprites/text labels generated at runtime; there are no external art assets yet.
- Dog identity art, pose labels, and objective arrows are generated placeholders. They are intentionally readable and easy to delete once authored sprites/animation exist.
- The squirrel and predator use intentionally simple movement/state rules so the PlayMode tests remain deterministic.
- Tug is proximity/progress based, not a full physics rope.
- Predator targeting and modifier selection are seeded but still prototype-simple.
- The scene now has basic procedural sound cues, an arena `AudioListener`, and simple placeholder animation, but real prefab art, authored animation, better SFX, and richer rescue/tug feel are still future work.

## Test coverage

`unity/Assets/Tests/PlayMode/ArenaGameLoopPlayModeTests.cs` loads ArenaScene and verifies dogs, mission state, item recovery, squirrel steal/scare, united bark timing/range, predator defense, failed predator rescue, tug completion, LevelClear, GameOver/restart, exposed modifier state, dog identity labels, dog pose labels, objective-arrow labels, and the generated arena audio listener.
