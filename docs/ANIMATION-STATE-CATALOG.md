# Animation State Catalog

Purpose: define required animation states before final asset production.

## Dog Locomotion

Cheddar and Cocoa both need:
- idle
- walk
- run
- zoomies
- skid
- turn
- jump / hop
- land
- swim
- shake off

## Dog Actions

- bark
- united bark
- tug
- carry
- push
- pull
- sniff
- dig
- hide
- rescue
- comfort
- beg
- head tilt
- paw tap
- dramatic flop

## Dog Status

- stunned
- grabbed
- scared
- wet
- proud
- sad
- trapped
- held
- sleepy

## Squirrel

- idle
- run
- route turn
- fake-out
- steal
- stash guard
- taunt
- escape

Current Unity first-test coverage: Resources-backed runtime strips exist for idle, run, steal, and
scared/fake-out reads under `ArenaFinal/Characters/Squirrel/Motion/`. Remaining named states can map
to those strips until final animation boards exist.

## Coyote

- patrol
- test fence
- threaten
- lure
- retreat

Current Unity first-test coverage: Resources-backed runtime strips exist for patrol, threaten, and
retreat under `ArenaFinal/Characters/Coyote/Motion/`. Test-fence and lure reads currently reuse
threaten.

## Eagle

- sweep / shadow pass
- attack / snatch

Current Unity first-test coverage: Resources-backed runtime strips exist for sweep and attack under
`ArenaFinal/Characters/Eagle/Motion/`.

## Human/NPC

- walk
- turn
- notice
- distracted
- annoyed
- put-away
- vacuum
- hold dog

## Rule

Every new animation must support gameplay readability at small 2.5D camera scale.
