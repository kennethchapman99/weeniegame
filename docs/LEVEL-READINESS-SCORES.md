# Level Readiness Scores

Status date: 2026-07-19

Pre-launch queue update (2026-07-17..07-19): the full `docs/AGENT-WORK-QUEUE-PRELAUNCH.md` (all rows
DONE) landed on top of the scores below without touching mission rules, tuning, or the frozen 23-
mission roster. Highlights that raise the roster's *couch-readiness* floor without changing any
row's underlying design: every mission now has a stall-aware guidance escalation ladder (a stuck pair
gets progressively stronger nudges instead of quitting), the three hard-handoff puzzles (Great
Escape, Chaos Machine, Bone Relay) show a role-turn beacon, mid-mission role flips get a shared
baton-swoosh-plus-chime flourish, every wrong-role attempt across all 23 missions was audited for a
visible+audible coach reaction (3 were previously silent, now fixed), held-payoff poses were audited
roster-wide (14 missions were freezing both dogs during their own success hold; fixed), a first-
mission control-reminder strip now covers cold-start strangers, and 9 previously-silent feedback
moments across 6 missions now have audio. See that queue doc's per-task write-ups for full detail and
evidence; this scorecard's per-mission rows below were not rewritten, since none of this queue's work
changed any mission's core design, difficulty, or asset floor rating.

Showcase-order update (2026-07-15): the selector's first five are now **Operation Pee Break**,
**Kitchen Falling Food Frenzy**, **Car Ride Chaos**, **Baby Bird Bedlam**, and **Gate Crash**, in
descending current production quality. This reflects the deep-slice work and the later authored
Car Ride/Baby Bird/Gate Crash polish that postdates the score snapshot below. It does not claim the
pending two-human retest passed, and it does not change deterministic mission tuning.

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
| 2 | Snack Heist | 4 | 4 | 4 | Cheddar's theft now provokes Cocoa's required active-heist guard before the watched final snack; wrong roles recover cleanly and the secured stash holds live before clear. |
| 3 | Sock Panic | 4 | 4 | 4 | Cocoa now continuously anchors the basket while Cheddar performs the timed sock dive; broken holds recover cleanly and the fifth sock earns a held live payoff. |
| 4 | The Great Backyard Squirrel Conspiracy | 4 | 4 | 4 | Cheddar's herd now advances only through Cocoa's physically held cutoff; Cocoa owns the stash inspection, solo attempts recover cleanly, and the cracked-case payoff stays live before clear. |
| 5 | Eagle Shadow Panic | 4 | 4 | 4 | Full-yard sweeps now require both dogs genuinely in cover; Cheddar-wiggle/Cocoa-pull rescue timing and the held united-bark retreat create a complete authored arc. |
| 6 | Coyotes at the Fence | 4 | 4 | 4 | Cocoa's in-range timed bark pin now creates Cheddar's exclusive dirt-fill opening; breach targets advance correctly and the united-bark retreat stays live before clear. |
| 7 | Weenie Roundup | 4 | 4 | 4 | Four fast split-yard carries now climax in Cocoa steadying Cheddar's jumbo haul; separation fumbles recover cleanly and a held bowl-full beat keeps the finish live. |
| 8 | Scent Search | 4 | 4 | 4 | Cheddar's broad direction now hands off to Cocoa's precise hot-patch call and back to Cheddar's dig; role misreads coach cleanly and the final cache stays live before clear. |
| 9 | Thunderstorm Comfort | 4 | 4 | 4 | Passive parking is replaced by Cocoa's timed reassurance, Cheddar's answer, and a held huddle; missed beats raise panic but recover next clap, and the passed storm stays live before clear. |
| 10 | Mark the Yard | 4 | 4 | 4 | Deliberate Interact marking now hands off from Cheddar's route to Cocoa's squirrel-repel bark, with recoverable steals and a held all-marked payoff. |
| 11 | Walkies on the Leash | 4 | 4 | 4 | Tethered movement now alternates named scout bark calls before the pair can bank each checkpoint; route dressing, snap pressure, and a live finish keep the handoff legible. |
| 12 | Car Ride Balance | 4 | 4 | 4 | Turn/jump chaos retains asymmetric slide physics, while brakes now require Cocoa's planted anchor and Cheddar's nearby tuck; failed handoffs recover next stop and arrival stays live. |
| 13 | Gate Crash | 4 | 4 | 4 | Cocoa must deliberately Interact to anchor before Cheddar can cross; role/range coaching, readable snap-and-retry recovery, gate/toy feedback, and the held rescue payoff make the handoff couch-test ready. |
| 14 | Table Stealth | 4 | 4 | 4 | Cocoa's deliberate Interact-flop opens a sustained Cheddar sneak, while Cheddar's live Bark-burp opens a burst Cocoa sneak; role/range coaching, recoverable exposures, state acting, and the held steak payoff make both routes couch-readable. |
| 15 | The Ol' Switcheroo | 4 | 4 | 4 | Cheddar deliberately Bark-baits and peels away before Cocoa's one Interact raid; guarded bonks, over-bait backfires, role/range coaching, dual credit, and the held cracked-stash payoff make the deception readable and recoverable. |
| 16 | The Walk Campaign | 4 | 4 | 4 | Cocoa and Cheddar must deliberately Interact into their distinct stare/leash poses and stay planted together; broken-pose recovery, specific wrong-item gags, human state acting, tactile feedback, and the held WALKIES payoff make the con couch-readable. |
| 17 | The Bone Detail | 4 | 4 | 4 | Cocoa now barks an explicit scent-post call for Cheddar's dig, with visible wrong-dig recovery and a held three-bone payoff; ready for two-player tuning. |
| 18 | The Great Escape | 4 | 4 | 4 | Four named dog-authentic steps now require alternating owner-only Interacts; wrong-paws clanks, tactile settle-back recovery, non-farmable repeat handling, command signals, and a held FREE DOGS payoff make the sequence couch-readable. |
| 19 | The Rube Goldberg | 4 | 4 | 4 | Cheddar's deliberate lever Interact starts an owner-only timed junction relay while the partner pre-positions; wrong-paws coaching, exact jam/re-pull recovery, distinct action props, tactile feedback, and the held toy-launch payoff make the machine couch-readable. |
| 20 | The Blanket Catch | 4 | 4 | 4 | Taut-span teamwork now hands off into Cocoa's bark-called drop, visible catch/splat recovery, and a held full-blanket payoff; ready for two-player tuning. |
| 21 | Kitchen Falling Food Frenzy | 5 | 4 | 4 | Strong extracted slice with clear role split and dinner-rush chaos; food/kitchen assets are serviceable and now clear the asset floor. |
| 22 | Operation Pee Break | 5 | 4 | 4 | Best current deep-slice candidate: readable roles, yard route, pressure, recovery, replay hooks, and generated cartoon prop coverage for its couch/door/phone/leash beats; still needs the second human couch pass. |
| 23 | Baby Bird Bedlam | 4 | 4 | 4 | Authored chick/parent states and the nest landmark carry a hard feast/fend split; wrong-role and distant-defense inputs now coach players back into the still-live dive window. |

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
