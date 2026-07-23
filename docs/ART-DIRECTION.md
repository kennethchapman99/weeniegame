# Cheddar & Cocoa Art Direction

This is the global visual bible for the playable prototypes. It applies across the full game, not
only Backyard Mission. The current Unity arena proves the direction with generated placeholder
shapes; authored sprites and animation should preserve these readability rules.

For the external asset intake list, folder structure, naming rules, technical specs, and tracking
template, use [ASSET-CATALOG.md](ASSET-CATALOG.md).

## Style Contract

> Added for V3.1 (2026-07-19): one visual language, enforced by a checklist instead of vibes. Grade
> every new asset against these five checks before it ships, and grade every `--arena-art-review`
> capture frame against them during a V3.x audit pass. The graded results and the ranked fix list
> from the first audit live in `ASSET-PRODUCTION-CATALOG.md`'s "V3.1 Style-Contract Audit" section —
> this section is the durable rule, that one is the dated report.

1. **Outline weight** — dogs, mission props, and actors carry a dark outline roughly **4–12px thick
   at a 512×512 source canvas** (about 1–2.5% of canvas width); 3–4px is acceptable for small
   interior details, 14–20px only for a single large silhouette shape (a lever, a piece of
   furniture). Do not mix a hairline (<2px) outline and a heavy (>12px) outline in the same sprite
   set — pick one weight class per asset and stay in it.
2. **Palette family** — warm, saturated, storybook color: golden-yellow/orange/brown earth tones
   for fur, wood, and dirt; grass/leaf green for the yard; teal/blue as the one deliberate "cool"
   family, reserved for Cocoa's identity accent and water. Avoid desaturated, muted, or
   photographic/naturalistic color grading anywhere a player is meant to read gameplay state.
   Cheddar and Cocoa's own body/accent colors (`## Color And Accent Rules` above) are the anchor —
   every other prop's palette should feel like it was mixed from the same paint set.
3. **Shading style** — flat/cel-shaded fill with at most one soft accent highlight, rim-light, or
   drop shadow. No photoreal rendering and no painterly airbrush blending on characters, mission
   actors, or interactable props. Environment ground plates are the one deliberate exception
   (`Photo-Inspired Backyard Reskin Layer` below uses soft painterly texture on purpose) — but
   characters and props must stay flat-shaded so they pop off any background, painterly or not.
4. **Silhouette-first** — cover/hide test: at the arena camera's default gameplay zoom, with color
   removed, a player must still be able to name the object from its outline alone. Anything that
   reads as a bare colored rectangle, an anonymous dot, or requires reading a text label to identify
   fails this check and needs a real silhouette before it ships.
5. **No baked text** — no letters, numerals, or words drawn into sprite pixel data, ever. All
   player-facing text renders through the shared TextMesh/TMP `AddWorldLabel`/HUD path (see the
   Shared Label/Prompt Rule in `VISUAL-READABILITY-CONTRACT.md`) so it can be resized, relocalized,
   and gated by the existing F1/proximity rules. `ReferenceOnly` contact-sheet tooling output
   (thumbnail captions on a review grid) is exempt — it documents source art, it never ships.

## Visual Promise

Cheddar & Cocoa should look like bold, funny, readable miniature dachshunds living through dog-life
adventures at human scale. The style is simple and chunky: long low bodies, oversized emotional
markers, bright accents, direct pose reads, and props that explain the joke at gameplay zoom.

The goal is not realistic dog anatomy. The goal is instant co-op recognition:

- "That is Cheddar, the golden chaos puppy."
- "That is Cocoa, the chocolate spot queen."
- "That object is food, threat, rope, squirrel, rescue, score, or replay."

## Character Silhouette Rules

Both dogs:

- Always read as long, low dachshunds before labels are considered.
- Body should be a horizontal sausage shape, with tiny feet low on the silhouette.
- Snout should be visible and forward-facing in gameplay sprites.
- Tail should act as a pose/excitement flag.
- Ears should be floppy and simple; avoid perky generic-dog triangles.
- Keep the silhouette clean. No tiny costume clutter that disappears at gameplay zoom.

Cheddar:

- Golden, bright, restless, slightly messy.
- Reads like forward momentum: head up, tail busy, body leaning into trouble.
- Use a warm red/orange collar or accent as the consistent player read.
- Markings can be chaotic: forelock, bright flash, warm ear, mischief stripe.
- Cheddar should look lovable even when failing. His danger state should feel like "oops, too far."

Cocoa:

- Chocolate, grounded, expressive, capable, slightly over it.
- Reads like control and judgment: steadier posture, cleaner stance, queenly markers.
- Use a cool teal/blue collar or accent as the consistent player read.
- Markings stay tonal: warm-brown muzzle, eyebrow points, and lower paws against the deep-chocolate
  coat. Do not add cream or white patches.
- Cocoa should look competent even when annoyed. Her danger state should feel like "I warned you."

## Color And Accent Rules

- Cheddar body: golden/yellow-orange family.
- Cheddar accents: red/orange collar, brighter yellow mood sparks, warm ear/tuft.
- Cocoa body: dark chocolate/brown family.
- Cocoa accents: teal/blue collar, warm-brown tonal points, small gold queen marker when used as a
  UI/role motif rather than a body marking.
- UI arrows and dog-specific callouts should echo collar/accent colors when possible.
- Do not make Cheddar and Cocoa differ only by label. Body color, collar, markings, and silhouette
  energy must all carry identity.
- Avoid one-hue screens. Backyard grass, food reds/yellows, predator reds, teal Cocoa accents, and
  Cheddar gold should all separate cleanly.

## Expression And Pose Language

Idle:

- Cheddar: "WIGGLE READY" - slightly eager, tail busy, head searching for trouble.
- Cocoa: "QUEEN READY" - calm stance, readable eyes, controlled tail.

Run:

- Cheddar: "CHAOS ZOOM" - body tilt, fast wag, loose ear/tuft motion.
- Cocoa: "SPOT PATROL" - purposeful trot, steadier head, alert spot-queen read.

Bark:

- Both dogs pop larger and show WOOF clearly.
- Cheddar's bark is explosive and enthusiastic.
- Cocoa's bark is authoritative and unimpressed.
- Bark effects must remain readable as gameplay feedback, not only personality animation.

Tug:

- Both dogs should look locked into the same rope problem.
- Cheddar reads as overcommitted excitement.
- Cocoa reads as anchor strength.
- The rope must show progress and partner waiting state clearly.

Stunned:

- Body compresses or tilts; eye/expression marker changes.
- The grabbed dog should be instantly readable as unable to act.
- Partner rescue callout should be more visually important than decorative distress.

Rescued:

- Rescued dog gets a hopeful pop.
- Rescuer gets a proud moment.
- The state should feel funny and warm, not grim.

Proud:

- Cheddar: bright, bouncing victory.
- Cocoa: royal satisfaction, "obviously."
- Used for clear and major teamwork wins.

Sad:

- Low, flopped, readable fail pose.
- Should be funny and recoverable. Avoid harsh punishment visuals.

## Personality Feedback Rules

- Cheddar feedback should be noisy in shape language but not unreadable: fast wag, bright sparks,
  overexcited pose labels, and silly score/cue copy.
- Cocoa feedback should be expressive and declarative: steadier motion, cooler accent, spot/crown
  identity, and cue copy that suggests capable judgment.
- Failure and rescue should reinforce co-op affection. The game is about two dogs recovering from
  nonsense together.
- Bark, rescue, tug, squirrel, predator, clear, fail, score, and replay must each have a distinct
  visual state.

## UI And Callout Tone

Cheddar callouts:

- Short, eager, impulsive.
- Good examples: "CHAOS ZOOM", "WOOF!", "WEENIE!", "oops still heroic".
- Avoid generic hero language unless it is undercut by puppy nonsense.

Cocoa callouts:

- Capable, dry, slightly queenly.
- Good examples: "SPOT PATROL", "QUEEN READY", "BARK RESCUE", "obviously saved it".
- Avoid making Cocoa merely slow or grumpy. She is competent and expressive.

Shared callouts:

- Should make co-op intent obvious: "HUDDLE + BARK", "BOTH TUG", "PARTNER BARK".
- Use funny phrases after the action is understood, not instead of clarity.
- Score labels should stay arcade-readable: signed number plus plain cause.

## Audio Style Direction

Future authored audio should be cute, comic, toy-like, and readable at couch co-op volume. The
current Unity arena uses generated procedural dog-life cue profiles for major feedback events. Those
clips are replaceable cue slots, not authored recordings or a final mixed sound palette.

- Dog barks should be funny and expressive, not realistic, aggressive, or annoying.
- Cheddar's bark can be brighter, eager, and a little chaotic.
- Cocoa's bark can be lower, confident, and queenly, but still warm.
- Squirrel cues should sound mischievous: sneaky taps, tiny scurries, comic warning chirps, or toy
  percussion beats.
- Tug, rescue, and united-bark wins should feel warm and satisfying, with playful teamwork pops.
- Win feedback should be celebratory and silly, not epic or generic.
- Fail feedback should be playful and recoverable, not harsh, scary, or punitive.
- Score gain/penalty cues should be short enough to stack during chaos without masking objective
  readability.
- Avoid realistic distress sounds, aggressive dog snarls, harsh predator audio, or loops that make
  bark feel like punishment.

## Backyard Mission Implementation

The current Unity proof lives in `unity/CheddarAndCocoa/Assets/Scenes/ArenaScene.unity`, built by
`ArenaBootstrap`. It uses generated runtime shapes as the primary gameplay read, with the first
imported DRAFT art pass layered in as small reference badges.

Current implementation rules:

- Runtime placeholder visual slots are centralized in
  `unity/CheddarAndCocoa/Assets/Scripts/Game/ArenaArtCatalog.cs`. Future authored sprites should
  replace the named slots there first instead of adding one-off child names, colors, or scales in
  gameplay code.
- Imported DRAFT art is organized under
  `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaDraft/` and loaded through
  `ArenaDraftArt`. These are draft/reference sheets with backgrounds and broad pose layouts, so they
  should not be treated as final transparent gameplay sprites yet.
- Dog bodies are longer and lower than generic rectangles.
- Both dogs have generated heads, long snouts, floppy ears, tiny feet, tails, collars, and expression
  eyes.
- Cheddar has a golden body, red collar, bright chaos tuft/flash, and "CHEDDAR CHAOS PUP" identity.
- Cocoa has a uniform chocolate body, teal collar, warm-brown tonal points, tiny crown marker, and
  "COCOA SPOT QUEEN" identity. Older geometric fallbacks may retain a chest-patch shape, but new
  authored art follows `CHARACTER-ART-MODEL-SHEETS.md`.
- Cheddar and Cocoa currently also show small imported portrait badges behind their generated
  gameplay bodies. The badge is a readability/reference aid, not a replacement for final pose
  animation.
- Pose labels are intentionally direct: WIGGLE READY, QUEEN READY, CHAOS ZOOM, SPOT PATROL, WOOF,
  TUG, STUNNED, RESCUED, PROUD, SAD FLOP.
- Objective arrows stay dog-accent colored and hide when the target is close.

### Shared Backyard Plate

The shared yard uses one full-opacity flat-cel storybook plate,
`yard_backyard_plate_v02`, behind runtime characters and mission cues. Its approved source is
`ReferenceOnly/GeneratedBackyardMegapass/yard_backyard_plate_storybook_v03.png`. The resource name
remains V02 to preserve the existing runtime contract.

- Preserve the exact 1672×941 footprint and landmark map: house stoop, perimeter fence and beds,
  tree/swing, stepping stones, sandbox, picnic blanket, stone patio, hedges, and broad central lawn.
- Keep foliage and materials in the same warm outlined, simplified two-tone language as indoor
  room plates and mission props. Avoid realistic leaf microdetail and photo-crop collage layers.
- Concentrate detail and saturation at the perimeter. Central and lower-center co-op lanes stay
  quiet enough for dog silhouettes, threats, pickups, and world cues.
- The plate is scenery only: no frozen dogs, people, squirrels, threats, collectibles, text, or UI.
- Independent environment overlays remain collider-free and lower-priority than mission actors.
  Retire any overlay that duplicates a landmark already painted into the plate or obscures play.

The legacy `yard_photo_*` resource slots remain available for adventure thumbnails and the
controller-owned pool water treatment, but `BackyardRescueArtEnhancer` must not rebuild the retired
photo-crop yard collage from them.

## Backyard Prop Readability

Weenie/breakfast:

- Reads red/yellow at small size with bun and mustard markers.
- Label remains "Weenie" until authored food art can carry the read alone.
- Must never be confused with squirrel or rope.

Squirrel:

- Small thief with tail, nose, eye, grab paws, loot/acorn marker, draft squirrel badge, and clear
  steal/scare labels.
- Stealing state must be more urgent than idle/waiting.
- Bark response must visibly change the squirrel state.

Predator:

- Red/dark warning shadow with eagle wing sweep, coyote ears/eyes, imported eagle/coyote badges, and
  offscreen/warning/attack/yeeted labels.
- Should read as a pressure event, not a normal collectible.
- During attack, grabbed dog pose plus partner rescue arrow must dominate the read.

Rope:

- Yellow/brown striped horizontal tug object with distinct ends, center knot, bite marks, and a
  draft backyard-prop badge until a real rope sprite is exported.
- Labels must name whether it needs both dogs, is waiting for one dog, is charging, or is complete.
- Tug should feel like shared dog work, not a generic progress bar.

## Future Art Replacement Contract

AI-assisted character motion exploration is now active as a staged, review-first workflow. Generated
turnaround and key-pose boards remain under `ReferenceOnly/GeneratedCharacterMotion`; they do not
replace runtime sprites until a human approves identity, camera, silhouette, and consistency. See
`docs/CHARACTER-MOTION-PACK.md` for the direction/clip matrix and promotion gates.

### Camera And Perspective

- Current gameplay uses a 2D orthographic, top-down/three-quarter readable arena. Assets should read
  from this camera without needing per-object camera billboarding.
- Dog sprites should face primarily left/right with clear snout direction. A slight three-quarter
  read is fine; avoid front-only portraits that hide dachshund length.
- Props should be readable at the current gameplay zoom, not only in close-up UI.
- Shadow direction should be consistent across sprites: use a soft down/right grounding shadow or no
  baked shadow. Avoid dramatic shadows that imply a different light source than the arena.

### Scale

- The dog replacement envelope should match the current long-low body footprint: about `1.6 x 0.62`
  Unity units for the base body before pose squash/stretch.
- Cheddar and Cocoa should use the same collision footprint unless gameplay deliberately changes.
  Visual differences should come from markings, collars, pose, and animation energy, not collider
  size.
- Weenie/snack/sock props should remain small enough to collect cleanly with the current `0.6`
  trigger radius and large enough to read under objective arrows.
- Indoor floor plates use a shared `48 x 36` Unity-unit stage envelope. Perimeter furniture may be
  human-scale, but central lanes stay quiet and small dog toys should render roughly `0.55–1.6`
  units wide so they still look carryable beside a roughly two-unit dog.
- Mission-state illustrations that sit beside the dogs should normally remain `2–4` units wide.
  Author important cues by world size rather than source-pixel scale so a regenerated 512/1024px
  texture cannot vanish or become room-sized.
- Squirrel, predator warning, and rope replacements should preserve their rough current gameplay
  scale: squirrel small thief, predator larger warning/shadow, rope horizontal shared-object.

### Sprite Import Expectations

- Use transparent-background sprites for dogs, props, mission actors, VFX, markers, and UI icons.
- Keep pivots centered unless a slot explicitly needs an end/feet pivot. Dog body sprites should
  pivot around the body center so existing squash, bark, proud, sad, and tug transforms remain
  usable.
- Use pixels-per-unit consistently across a sprite set. Do not compensate for inconsistent source
  scale with arbitrary scene transforms.
- Keep sprite bounds tight but leave enough padding for ears, tail, rope ends, and bark rings so
  animation does not clip.

### Naming Conventions

Named replacement slots should map to `ArenaArtCatalog` first:

- Dogs: `DachshundHead`, `LongDogSnout`, `CheddarRedCollar`, `CocoaTealCollar`,
  `CheddarIntentArrow`, `CocoaIntentArrow`, `CocoaQueenSpotA`, `CheddarChaosTuft`,
  `DogReadabilityLabel`.
- Collectibles: `WeenieMustard`, `SnackPlate`, `SnackCrumbA`, `SockToe`, `SockStripeA`.
- Mission actors: `SquirrelFlagTail`, `SquirrelPointNose`, `PredatorWarningEyeA`,
  `PredatorWingLeft`, `RopeStripeA`, `RopeEndLeft`.
- Feedback/UI: `BarkRing`, `BarkBurst`, `ObjectiveArrowLabel`, `MissionPop_*`.

If a future prefab uses fewer child objects because the art is a single sprite, keep a stable slot
component or prefab child with the same logical name so PlayMode tests and replacement tooling can
still find it.

### Animation Pose Expectations

Future dog animation should preserve these state reads:

- Idle: Cheddar wiggle-ready, Cocoa queen-ready.
- Run: Cheddar chaos zoom, Cocoa spot patrol.
- Bark: visible pop plus bark effect; bark remains gameplay feedback.
- Tug: both dogs visibly commit to one shared rope problem.
- Stunned: dog is clearly unable to act.
- Rescued: warm recovery pop.
- Proud: clear/teamwork victory pose.
- Sad: recoverable fail pose, not grim punishment.

Animation controllers, sprite sheets, or prefab swaps can arrive later, but they must expose these
poses or equivalent state hooks before replacing the generated pose labels.

## Score And Replay Visual Tone

- Score swings are plain, signed, and cause-first: "+100 UNITED BARK", "-50 SQUIRREL GOT ONE".
- Clear is celebratory and silly: proud dog poses, bright banner, funny rank, replay prompt.
- Fail is recoverable: sad flop poses, clear reason, replay prompt.
- Replay should feel like "try the weenie rescue again," not a menu-heavy reset.

## Prototype Limitations

This is not final art:

- Primary gameplay readability still comes from generated runtime sprites and TextMesh labels, with
  local imported DRAFT sheets used as badges/reference accents.
- Shapes are intentionally chunky and replaceable.
- There are no paid or remote assets in the Unity project; the current imported art is local DRAFT
  material and still needs final transparent gameplay exports.
- There is no full character rig, sprite sheet, animation controller, VFX pipeline, or final UI skin.
- Text labels are still carrying some clarity that authored art and SFX should eventually carry.
- Future levels should reuse the same dog identity rules before inventing new costumes or UI
  language.

Until final art exists, every placeholder must remain playable, readable, and aligned with the dog
fantasy.
