# Audio Direction

Goal: audio should make gameplay clearer and funnier.

## Tone

- playful
- warm
- polished
- not babyish
- not chaotic noise

## Dog Audio

Cheddar:
- eager bark
- chaos bark
- tiny panic
- victory yip

Cocoa:
- confident bark
- royal warning
- calm comfort
- proud huff

## Gameplay Cues

Required:
- bark
- united bark
- pickup
- score gain
- score loss
- warning
- success
- fail
- replay/select

Current Unity implementation: `ArenaFeedbackCatalog` now maps the major feedback events to generated
procedural dog-life SFX profiles for bark, team success, crunch collect, squirrel alarm, score
sparkle, penalty thunk, victory fanfare, failure sigh, UI blip, and threat rattle. These are
replaceable runtime cue slots for couch-test clarity, not authored recordings or a final mix.

## Threat Cues

Squirrel:
- scamper
- fake-out
- taunt

Eagle:
- shadow whoosh
- danger pulse

Coyote:
- low warning
- fence scrape
- retreat

Human/NPC:
- footsteps
- vacuum pass
- phone notification
- chair move

## Music

Mission music should be light and loopable.

Avoid:
- heavy orchestral drama;
- scary horror tone;
- repetitive high-frequency irritation.

## Rule

Every important gameplay state should have an audio cue, but no cue should compete with objective clarity.
