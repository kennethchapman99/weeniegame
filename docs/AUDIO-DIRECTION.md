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

Current Unity implementation: `ArenaFeedbackCatalog` maps the major feedback events to replaceable
cue slots for bark, team success, crunch collect, squirrel alarm, score sparkle, penalty thunk,
victory fanfare, failure sigh, UI blip, and threat rattle. The couch-test authored MP3 bank from
`Assets/Audio/Resources/AuthoredSfx/` now feeds those slots through `AuthoredAudioCatalog`; every
imported clip must be reachable from a runtime bank. Generated procedural SFX remain only as a
fallback if an authored resource is missing.

The named MP3s also have semantic runtime slots for menu focus, button confirm, menu open/close,
disabled-button bonk, star appear, acceleration skid, eating gulp, squirrel chatter/escape/stunned,
bunny hop, and toy squeak. Older broad mission cues request those semantic companions centrally so
controller-owned missions get the same authored audio without mission-specific audio branches.

Cheddar and Cocoa bark impact audio is dog-local and identity-specific: Cheddar bark phases use
`p0_cheddar_bark_01..08`, Cocoa bark phases use `p0_cocoa_bark_01..08`. Non-bark dog action phases
still use the lightweight procedural layer until authored tug/carry/rescue/zoomies recordings exist.

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
