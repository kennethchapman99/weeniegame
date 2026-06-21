# Save and Profile Architecture

> **Status: DEFERRED.** Do not expand save/profile scope until Operation Pee Break passes the
> second couch-playtest gate. Existing runtime behavior may be maintained, but this is not an
> active implementation plan.

Goal: save progress simply and locally.

## Save Data

Track:
- unlocked mission packs
- unlocked missions
- stars per mission
- best score per mission
- best rank per mission
- completed challenge objectives
- unlocked cosmetics
- selected cosmetics
- collectible sets
- lifetime stats

## Profiles

Support one local profile first.

Future optional:
- multiple local profiles
- guest mode

## No Cloud Requirement

Do not require network access for save data.

## Mission Record

Each mission record should include:
- mission id
- best score
- best stars
- best rank
- clears
- failures
- challenge flags

## Lifetime Stats

Examples:
- barks
- united barks
- rescues
- squirrels scared
- fake-outs suffered
- toys rescued
- snacks collected
- pool falls

## Rule

Do not build a complex economy before missions are fun.

Save system should preserve mastery and cosmetics, not create grind.
