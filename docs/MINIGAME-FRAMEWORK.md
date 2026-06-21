# Minigame Framework

> **Status: DEFERRED.** Do not add minigame scope before the deep-slice couch-playtest gate passes.

Do not build minigames as unique systems.

Every minigame should plug into a common framework.

Required states:
- Intro
- Countdown
- Playing
- Sudden Death (optional)
- Results
- Replay

Required outputs:
- winner
- loser
- score
- duration
- funny summary label

## Minigame Set 1

### Tug-of-War Supreme

Uses:
- Shared Object module
- Rhythm inputs

### Sunbeam King / Queen

Uses:
- Territory Control module

### Couch Claim

Uses:
- Territory Control module

### Treat Drop Duel

Uses:
- Falling food system

### Zoomies Tag

Uses:
- movement and tagging

### Toy Hoarder

Uses:
- Territory Control
- Shared Object

### Best Bark Battle

Uses:
- Rhythm Panic module

### Who Can Be More Pathetic

Uses:
- Begging meter
- Human attention system

### Blanket Burrow Race

Uses:
- Navigation modifiers

### Squirrel Dash

Uses:
- Herding module

Success criteria:
- starts in under 3 seconds;
- ends in under 3 minutes;
- replay is instant;
- can be played repeatedly without explanation.
