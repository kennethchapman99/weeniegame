# Level Readiness Scores

Status date: 2026-06-28

This scorecard rates the current Unity arena mission roster for the first couch-test push. It is not
a final art review. Scores reflect the playable Unity implementation, documentation in
`docs/ARENA-PLAYABLE.md`, controller coverage in `MissionControllerRegistry`, the mission-readiness
set-dressing pass, the squirrel/eagle/coyote motion pass, and the latest known PlayMode evidence
(`369/369` passing on 2026-06-28).
Human two-player couch validation is still the remaining gate.

Scale:

- 5: strong first-test candidate; distinctive, readable, and currently fun on paper.
- 4: playable and useful for couch testing; needs tuning or presentation polish.
- 3: playable but likely needs visual, pacing, or co-op clarity work before it shines.
- 2: implemented as a functional greybox; scenery/assets or readability are still thin.
- 1: not ready to hand to couch testers.

| # | Level | Level design | Scenery and assets | Readiness to play | Current read |
|---:|---|---:|---:|---:|---|
| 1 | Backyard Rescue | 4 | 4 | 4 | Strong broad rescue loop with animated squirrel pressure, eagle/coyote reads, toy, wet, and mud beats; may be busy for a cold first test. |
| 2 | Snack Heist | 4 | 4 | 4 | Compact food-steal objective is readable and dog-authentic; snack district cues plus squirrel steal motion now support couch testing. |
| 3 | Sock Panic | 4 | 4 | 4 | Tip/dive timing creates a clear shared beat; laundry district dressing and basket/sock markers are readable enough for first testing. |
| 4 | The Great Backyard Squirrel Conspiracy | 4 | 4 | 4 | Chase/cutoff pressure has clearer squirrel motion, stash/cutoff labels, and yard dressing for a useful couch read. |
| 5 | Eagle Shadow Panic | 4 | 4 | 4 | Hide, rescue, and united-front play is strong; animated eagle sweep/attack frames and cover bands improve threat clarity. |
| 6 | Coyotes at the Fence | 4 | 4 | 4 | Bark-pin, fence-filling, and fake-lure roles work well; coyote patrol/threat/retreat motion and fence lane dressing make the pressure readable. |
| 7 | Weenie Roundup | 4 | 4 | 4 | Carry/deliver loop is easy to understand; bowl/weenie assets and persistent carry visuals make it couch-test useful. |
| 8 | Scent Search | 4 | 4 | 4 | Sniff, dig, and choice tension fit the dogs well; scent patches and mound/grass language now clear the first-test bar. |
| 9 | Thunderstorm Comfort | 4 | 4 | 4 | Comfort fantasy and calm/panic loop are testable; storm/emotion labels, huddle feedback, and VFX make the emotional state readable. |
| 10 | Mark the Yard | 4 | 4 | 4 | Territory control creates useful route planning; zone markers, central lawn dressing, and readable reclaim feedback support couch testing. |
| 11 | Walkies on the Leash | 4 | 4 | 4 | Tethered movement naturally forces communication; route stones/dashes and checkpoint dressing make the path legible. |
| 12 | Car Ride Balance | 4 | 4 | 4 | Balance/lurch concept is distinct; generated car silhouette, lurch feedback, and counter-lean cues are sufficient for first testing. |
| 13 | Gate Crash | 4 | 4 | 4 | Hold/release and gate timing are good couch-test mechanics; gate, toy, and role-marker staging now meet the readable greybox bar. |
| 14 | Table Stealth | 4 | 4 | 4 | Human distraction plus stealing is funny and dog-specific; table/human/steak silhouettes and exposure feedback are useful for testing. |
| 15 | The Ol' Switcheroo | 4 | 4 | 4 | Bait/decoy/raid structure has good co-op intent; squirrel motion plus stash/decoy markers make the scene readable. |
| 16 | The Walk Campaign | 4 | 4 | 4 | Strong precursor to Operation Pee Break with route pressure; leash/human/route cues are now clear enough for couch iteration. |
| 17 | The Bone Detail | 4 | 4 | 4 | Sniff/dig asymmetry is on-theme; scent, mound, bone, and dig-spot presentation clear the first-test bar. |
| 18 | The Great Escape | 4 | 4 | 4 | Sequential escape-chain teamwork is readable; contraption station markers and role color-coding support useful couch play. |
| 19 | The Rube Goldberg | 4 | 4 | 4 | Replayable timing concept has clear machine-stage markers and cause/effect pops for first-test iteration. |
| 20 | The Blanket Catch | 4 | 4 | 4 | Shared-object catch is now a useful support slice; blanket/catch feedback and falling-object cues are readable enough to test. |
| 21 | Kitchen Falling Food Frenzy | 5 | 4 | 4 | Strong extracted slice with clear role split and dinner-rush chaos; food/kitchen assets are serviceable and now clear the asset floor. |
| 22 | Operation Pee Break | 5 | 4 | 4 | Best current deep-slice candidate: readable roles, yard route, pressure, recovery, and replay hooks; still needs the second human couch pass. |

## Ranking Snapshot

- Best first couch-test anchors: Operation Pee Break, Kitchen Falling Food Frenzy, Backyard Rescue.
- Best supporting tests: Coyotes at the Fence, Eagle Shadow Panic, Scent Search, Walkies on the Leash.
- Scenery/assets floor: every roster mission now rates at least `4` for first couch-test readiness
  after the generated mission-district pass and squirrel/eagle/coyote motion promotion.
- Do not expand the roster based on this table. Per the active gate, use it to decide what to polish
  or hide for the next couch session.
