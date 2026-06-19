# Co-op Puzzle — Artwork Needs

Art the co-op puzzle beats need so a later agent (or a future pass) can swap the placeholder shapes for real visuals. Each beat works today on generated geometry + text labels; this lists the **final art slots** to fill, with the gameplay-readability requirement each must preserve. Follow the existing draft-art badge pattern (`docs/ART-INTEGRATION-SLICE.md`, `Assets/Art/Resources/`): generated silhouette stays for collision-safe readability, imported art layers behind/near it.

> Status: the beats are pure-logic primitives + drivers (`CoopHumanDistractionPuzzle`, `CoopHoldReleasePuzzle`, etc.) with deterministic tests; none ship final art yet. All art below is **additive** — gameplay must stay legible with art missing.

## Dual-method human distraction (`CoopHumanDistractionPuzzle`)

The marquee beat: Cheddar distracts a human with a **burp cloud** (burst), Cocoa distracts by **rolling belly-up for a rub** (sustain). The same human-attention outcome, two signature dog moves. Art must make *which method is active* and *whether the human is currently distracted* instantly readable.

### Cheddar — burp cloud (burst distractor)
- **Burp cloud VFX**: a small greenish puff/cloud that pops above/in-front of Cheddar on `Burp()`, expands and fades over ~0.5s. Needs an obvious "on cooldown / recharged" read (e.g. a faint recharging ring or a greyed cloud icon near Cheddar while `BurpReady == false`).
- **Cheddar burp pose**: a 1-frame "head-back puff" expression to pair with the cloud (reuse/extend the existing dog expression sheet).
- **Cooldown pip**: a tiny HUD/worldspace indicator showing burp readiness (binary ready/charging is enough).

### Cocoa — belly-up flop (sustain distractor)
- **Cocoa belly-up pose sprite**: Cocoa rolled onto her back, legs up, content — the "rub me" pose. This is a new dog state pose (the current pose set has idle/run/bark/tug/stunned/rescued/proud/sad; add **bellyUp**).
- **Belly-rub feedback**: a floating heart / "♥" or hand-pat motif over Cocoa while `BellyFlopped == true`, plus a **stamina read** (a small draining bar/ring) so players see the flop won't last forever; when `FlopStamina` hits 0 she pops to an indignant get-up pose for ~0.4s.

### Human (shared distraction target)
- **Human NPC sprite** with at least two readable states: **distracted** (looking at the dogs, charmed/laughing — used while `HumanDistracted`) and **alert/neutral** (looking toward the objective lane — used while not distracted). A simple attention-cone or gaze indicator helps; this also serves the generic `CoopDistractSneakPuzzle`.
- **"Caught looking" reaction** for when the sneaker is exposed (`Exposures` ticks) — a brief "?" or head-turn, not a punishing animation.

### Shared sneak objective
- **Sneak lane / objective marker**: the thing being snuck (a snack, a sock, a forbidden item) with a "carry/steal" readout, and a subtle safe-lane highlight that brightens while `HumanDistracted`.

## Other beats (already in the toolkit)

- **Hold-and-Release** (`CoopHoldReleasePuzzle`): anchor "holding" pose (Cocoa braced against a gate/cushion), a **hold-patience meter** (draining), a cross corridor highlight, and a **snap-back** VFX (gate flaps, object flings) on `Snaps`.
- **Distract-and-Sneak** (`CoopDistractSneakPuzzle`): enemy gaze cone + a watchfulness/annoyance meter pair, segment **checkpoint** markers that light when banked, and a "spotted" startle on the enemy.
- **Sequence chain** (`CoopSequenceChainPuzzle`): per-step **station markers** (latch, gate, gap) with a clear "next step here" highlight on the current owner's station, a wrong-dog **fumble** puff, and a **settle** animation when the contraption eases back.
- **Rescue timing** (`CoopRescueTimingPuzzle`): the held dog's **wiggle** tell that opens the window (a shake + a brief glowing "pull now!" cue), the captor grip visibly loosening, and a **miss** spark for a mistimed pull.
- **Scent relay** (`CoopScentRelayPuzzle`): a **scent source** prop the reader noses; identical-looking **dig-spot stations** (so the digger genuinely can't tell them apart); a clear **reader's call** signal on the revealed station (a glowing scent puff / arrow that only shows while `Known`), driven by `RevealedTarget`; a "bone!" reveal on a find and a sneeze/decoy puff on a `WrongDig`.
- **Stretch span** (`CoopStretchSpanPuzzle`): a **stretched blanket / span sprite** drawn between the two dogs that visibly sags when `Slack`, pulls **taut** in band, and tears with a rip VFX when `Overstretched`; the dogs gripping a corner each; falling-item sprites with a **catch** pop when caught and a splat when `Missed`. A subtle taut/slack tint sells the spacing read.
- **Chaos machine** (`CoopChaosMachinePuzzle`): a Rube-Goldberg **contraption sprite per junction** (towel, basket, toy-launcher, route gate) with a "fired" vs "waiting" state; a **lever** prop for `Trigger`; a travelling cascade spark/ball that moves junction→junction while `Running`; and a clear **misfire/stall** VFX (smoke puff + "stuck here" marker) on the `StalledStage` so players see exactly which step failed.

## Acceptance for the art pass
- Gameplay stays fully legible if the art is absent (generated shapes + labels remain the source of truth).
- Each new pose/VFX maps to an existing exposed state on the primitive (`BellyFlopped`, `BurpReady`, `HumanDistracted`, `Snaps`, `Spotted`, `FlopStamina`, …) so it can be driven without new logic.
- Keep transparent, tightly-bounded sprites at the current gameplay zoom; register them through the established `ArenaArtCatalog` / final-art slot pattern.
