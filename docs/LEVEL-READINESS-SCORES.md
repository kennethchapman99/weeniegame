# Level Readiness Scores

Status date: 2026-07-01

This scorecard rates the current Unity arena mission roster for the first couch-test push. It is not
a final art review. Scores reflect the playable Unity implementation, documentation in
`docs/ARENA-PLAYABLE.md`, controller coverage in `MissionControllerRegistry`, the mission-readiness
set-dressing pass, the squirrel/eagle/coyote motion pass, the targeted generated-environment overlay
pass-2 evidence (`2/2` filtered PlayMode tests passing on 2026-06-29), the targeted gameplay-cue
evidence (`2/2` filtered PlayMode tests passing on 2026-06-29), the targeted dog-FX evidence
(`6/6` filtered PlayMode tests passing on 2026-06-29), the targeted Kitchen cue evidence
(`5/5` filtered Kitchen tests plus `1/1` resource and `1/1` mission-prop art contract tests passing
on 2026-06-29), the targeted Chaos Machine prop evidence (`9/9` filtered Chaos Machine tests and
`8/8` final-art integration tests passing on 2026-06-29), the targeted building prop evidence
(`11/11` filtered environment tests and `1/1` final-art resource test passing on 2026-06-29), and
the targeted HUD skin evidence (`12/12` filtered environment/HUD tests and `1/1` final-art resource
test passing on 2026-06-29), the targeted world-label skin evidence (`13/13` filtered environment/
world-label tests and `1/1` final-art resource test passing on 2026-06-29), the targeted generated
Arena SFX evidence (`1/1` catalog profile test and `1/1` event-driven audio/rumble test passing on
2026-06-29), the targeted collectible-art stability evidence (`1/1` final-art integration test
passing on 2026-06-29), the targeted generated P0 mission-state art evidence (`8/8` final-art
integration tests passing on 2026-07-01), and the latest full PlayMode evidence after that P0 pass
(`400/400` passing on 2026-07-01). Each mission definition now also carries a reusable couch-test
presentation profile for role copy, mechanic family, scene cue, Cheddar/Cocoa presentation guidance,
and required readability flags, with the selected/active mission's readability gate surfaced in the
mission picker and playtest overlay. The shared arena also has a runtime animated set-dressing layer
for background glow, fence lights, pawprints, sparkle accents, mission-reactive spotlight color, and
reusable animated motif families keyed to the selected or active mission. Those motifs now use a
generated transparent cartoon sprite pack in `ArenaFinal/Props/Wow` instead of square-only
placeholder compositions. Operation Pee Break also now uses generated transparent couch-test props
in `ArenaFinal/Props/PeeBreak` for the couch, Teenager, phone/charger, door, leash, hydrant payoff,
bladder gauge, and first misread tennis ball, reducing the active deep slice's visible reliance on
colored-square silhouettes. The non-Pee roster now also uses Generated Mission Prop Pack Pass 2 under
`ArenaFinal/Props/Missions` for its visible focus props, hazards, pickups, and payoff stations while
leaving dimmed fallback pads and debug/UI labels as intentional readability primitives. Generated
P0 mission-state packs now add state-specific transparent cartoon sprites under each mission's
`ArenaFinal/Props/<Mission>/` folder for trap gaps, guard lanes, baskets, cutoffs, threat states,
roundup cargo, dig patches, storm cues, yard zones, checkpoints, vehicle lurches, gates, humans,
decoys, leashes, bone mounds, escape stations, blankets, Kitchen food states, and Chaos Machine
lever states while preserving controller-owned gameplay objects as the source of truth. Generated
mission-specific collectible overlays now remain stable after the dynamic treat-art enhancer scans
spawned treats, so Snack Heist snack plates, Sock Panic socks, and Blanket Catch falling snacks do
not revert to Backyard weenie art during play. Generated
Gameplay Cue Pack now gives objective arrows and bark/tug/rescue range indicators transparent
cartoon cue sprites instead of generic range geometry. Generated Dog FX Pack now gives dog action
particles, paw trails, ground glow, sparks, queen glints, and collar glints transparent cartoon
sprites instead of white-square-only geometry. Generated Kitchen Cue Pack now gives Kitchen Falling
Food Frenzy gold-food and purple-onion counter telegraphs plus landing warnings generated
transparent cartoon sprites instead of generic warning geometry. Generated Chaos Machine Prop Pack
now gives The Rube Goldberg towel-drop, basket-tip, and toy-launch junctions distinct generated
transparent cartoon station sprites instead of one shared junction prop. Generated Environment Prop Pack now adds transparent cartoon overlays for
the patio/back door, fence rails, Pee Break outdoor route, snack/laundry districts, scent/leash
routes, lawn landmark, pond, shade tree, garden bed, flowers, picnic blanket, sandbox,
stepping-stone path, and eagle/coyote threat lane, so the broad yard districts no longer read as
square-only scenery blocks. Generated Building Prop Pack now gives the backyard house facade,
back-porch entry, and yard shed transparent cartoon sprites instead of square-first building
silhouettes. Generated HUD Skin Pack now gives the mission picker, mission briefing, pause screen,
end cards, session summary, selected-mission showcase, playtest overlay, and debug toggle generated
transparent panel/tile/badge/button surfaces instead of flat IMGUI boxes; IMGUI text and hitboxes
remain the current couch-test UI layer. Generated World Label Skin Pack now gives shared mission
object labels, command/warning labels, and score pops transparent bubble/ribbon/warning/burst skins
instead of raw floating TextMesh-only presentation while preserving the label strings as the current
couch-test copy layer. Generated Arena SFX now gives the major feedback slots named procedural
dog-life profiles instead of one generic tone/noise generator; authored recordings, final mix, and
platform haptics remain future polish. Mark the Yard's reclaim squirrel now uses generated squirrel
art, authored idle/scared/steal motion, contextual/debug label visibility, and a short readable
reaction beat before it resumes stealing zones. The latest 66-frame art-review capture at
`unity/builds/art-review-current/arena-art-review-contact-sheet.jpg` verifies the roster renders
nonblank at 1920x1080, with the Weenie Roundup, Leash Walk, and Chaos Machine review harness frames
now staged around their active dog/objective beats. Table Stealth and Walk Campaign humans now also
use contextual actor feedback, tint/pulse state changes, and readable success/fail/misread states so
their NPCs no longer read as inert focus props.
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
| 10 | Mark the Yard | 4 | 4 | 4 | Territory control creates useful route planning; zone markers, central lawn dressing, and animated generated squirrel idle/scared/steal states make reclaim cause/effect readable. |
| 11 | Walkies on the Leash | 4 | 4 | 4 | Tethered movement naturally forces communication; route stones/dashes and checkpoint dressing make the path legible. |
| 12 | Car Ride Balance | 4 | 4 | 4 | Balance/lurch concept is distinct; generated car silhouette, lurch feedback, and counter-lean cues are sufficient for first testing. |
| 13 | Gate Crash | 4 | 4 | 4 | Hold/release and gate timing are good couch-test mechanics; gate, toy, and role-marker staging now meet the readable greybox bar. |
| 14 | Table Stealth | 4 | 4 | 4 | Human distraction plus stealing is funny and dog-specific; table/human/steak silhouettes now include watching-table, watching-Cocoa, spotted, steak-gone, and caught human states for testing. |
| 15 | The Ol' Switcheroo | 4 | 4 | 4 | Bait/decoy/raid structure has good co-op intent; squirrel motion plus stash/decoy markers make the scene readable. |
| 16 | The Walk Campaign | 4 | 4 | 4 | Strong precursor to Operation Pee Break with route pressure; leash/human/route cues now include confused, getting-it, misread, walkies, and gave-up human states for couch iteration. |
| 17 | The Bone Detail | 4 | 4 | 4 | Sniff/dig asymmetry is on-theme; scent, mound, bone, and dig-spot presentation clear the first-test bar. |
| 18 | The Great Escape | 4 | 4 | 4 | Sequential escape-chain teamwork is readable; contraption station markers and role color-coding support useful couch play. |
| 19 | The Rube Goldberg | 4 | 4 | 4 | Replayable timing concept has clear machine-stage markers and cause/effect pops for first-test iteration. |
| 20 | The Blanket Catch | 4 | 4 | 4 | Shared-object catch is now a useful support slice; blanket/catch feedback and falling-object cues are readable enough to test. |
| 21 | Kitchen Falling Food Frenzy | 5 | 4 | 4 | Strong extracted slice with clear role split and dinner-rush chaos; food/kitchen assets are serviceable and now clear the asset floor. |
| 22 | Operation Pee Break | 5 | 4 | 4 | Best current deep-slice candidate: readable roles, yard route, pressure, recovery, replay hooks, and generated cartoon prop coverage for its couch/door/phone/leash beats; still needs the second human couch pass. |

## Ranking Snapshot

- Best first couch-test anchors: Operation Pee Break, Kitchen Falling Food Frenzy, Backyard Rescue.
- Best supporting tests: Coyotes at the Fence, Eagle Shadow Panic, Scent Search, Walkies on the Leash.
- Scenery/assets floor: every roster mission now rates at least `4` for first couch-test readiness
  after the generated mission-district pass and squirrel/eagle/coyote motion promotion.
- Consistency floor: every roster mission now exposes shared role, mechanic, scene, replay, warning,
  and dog-identity readability metadata before play.
- Runtime audit floor: couch testers can see the selected or active mission's readiness gate without
  opening code or docs.
- First-impression floor: every roster mission now inherits the animated arena wow layer before
  mission-specific final environment art exists, and each mission resolves to a tested reusable motif
  family made from generated cartoon sprites instead of a blank generic arena.
- Prop-art floor: the non-Pee roster's snack, sock, stash, gate, toy, human, leash, car, dig, scent,
  territory, reclaim squirrel, checkpoint, contraption, blanket, and kitchen focus objects now have tested generated
  cartoon overlays with quieter fallback pads instead of visible square-only props in the player
  focus area.
- Human-state acting floor: Table Stealth and Walk Campaign human NPCs now expose couch-test idle,
  reaction, success, and fail reads through contextual actor feedback and tint/pulse changes.
- Collectible-art stability floor: mission-specific Snack Heist, Sock Panic, and Blanket Catch
  collectible sprites stay active after dynamic treat scans instead of being overwritten by Backyard
  weenie art.
- Environment-art floor: the broad patio, back-door, fence, threat-lane, route, scent, snack,
  laundry, lawn, pond, tree, garden, flower, picnic, sandbox, and stepping-stone cues now have tested
  transparent cartoon overlays with dimmed square fallback renderers underneath.
- Building-art floor: the house facade, back-porch entry, and yard shed now have tested transparent
  cartoon overlays while the original nonblocking layout objects remain available underneath.
- HUD-skin floor: mission select, briefing, pause, end cards, session summary, selected showcase,
  playtest overlay, and debug toggle now have tested generated panel/tile/badge/button surfaces while
  IMGUI text and hitboxes remain the current non-final UI implementation.
- Label-skin floor: shared mission object labels and score pops now have tested generated bubble,
  command, warning, and burst skins while the `TextMesh` strings remain the current non-final copy
  implementation.
- Audio floor: bark, success, collect, squirrel, score, penalty, win, fail, UI, and threat feedback
  now use tested generated dog-life SFX profiles while authored recordings, final mix, and tuned
  haptics remain future production work.
- Do not expand the roster based on this table. Per the active gate, use it to decide what to polish
  or hide for the next couch session.
