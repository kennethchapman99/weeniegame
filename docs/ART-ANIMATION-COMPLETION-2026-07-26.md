# Art and Animation Completion Pass — 2026-07-26

## Completion decision

The active Unity roster now has a production art and animation pass across all 26 playable
missions and all gameplay character families. “Complete” here means every shipped mission has a
picture tile, environment treatment, readable before/after state art, character acting, and a
deterministic fallback; it does not mean that every generated sprite has become bespoke
hand-painted launch art. A two-player couch review remains the human quality gate.

The frozen TypeScript and prototype folders were not changed.

## Roster-wide level coverage

- All 26 `MissionVariant` entries have a non-placeholder mission-select picture resource.
- Indoor missions suppress backyard scenery and use the living-room, dining-room, kitchen, or
  backseat plates appropriate to the mission.
- Outdoor missions use the reviewed backyard plate, landmarks, mission-scoped prop states, cue
  skins, and ambient visual motion.
- Every mission has start/main/payoff staging in `ArenaArtReviewCapture`.
- Mission actors use `MissionActorFeedback` state acting: calm breathing, attention, celebration,
  alarm, and defeat. This gives human and prop actors readable changes without moving colliders,
  objective anchors, or controller-owned state.
- Squirrel, eagle, coyote, and skunk use their authored motion-strip system. Missing frames still
  fall back safely.

## Cheddar and Cocoa completion pack

The V02 Cheddar/Cocoa model sheets remain the identity authority. The completion atlas adds one
approved, transparent 512×384 key pose per dog for:

- swim;
- comfort;
- dramatic belly flop;
- beg;
- head tilt;
- paw tap;
- push/pull;
- hide;
- wet shake;
- trapped/held;
- sleepy.

The existing four-frame jump remains the live hop animation; the atlas also retains a matching
single jump key as reference. Runtime promotion is reproducible through
`tools/art/export_character_completion_poses.py`.

Live gameplay wiring:

- pool entry selects the authored Swim pose while preserving the water band and blue tint;
- pool exit shaking selects WetShake;
- Thunderstorm Comfort selects the distinct Comfort pose instead of aliasing Proud;
- Table Stealth sustains Cocoa's authored Flop pose for the whole belly-rub distraction;
- the remaining poses are stable `CharacterMotionArt` seams available to mission controllers
  without new `GameManager` branches.

All single-key completion clips pin frame 0 at any elapsed time. Direction fallback mirrors the
approved east-facing silhouette for west-facing play.

## Generation and promotion record

The two 4×3 source atlases were created with the built-in image-generation tool, using the V02
Cheddar/Cocoa sheets as strict identity references. The prompt requested polished storybook
dachshunds with bold navy contours, warm cel shading, exact collar/coat identity, twelve named
gameplay poses, an even grid, no text or props, and a flat `#ff00ff` chroma background.

The imagegen skill's `remove_chroma_key.py` helper produced the alpha boards. The deterministic
exporter removes adjacent-cell fragments, retains nearby water/motion accents, trims each subject,
and places it on the project's shared 512×384 canvas at paw baseline Y=360. Source and alpha boards
are tracked under `Assets/Art/ReferenceOnly/GeneratedCharacterCompletion/`; runtime sprites live
under each dog's `ArenaFinal/.../Motion/` folder. The visual review sheet is
`captures/character-completion-pose-contact-sheet.jpg`.

## Automated acceptance

- `FinalArtIntegrationPlayModeTests` requires all 22 live completion sprites, their 512×384
  normalized canvas, and frame-0 pinning.
- `BackyardPoolPlayModeTests` requires the authored Swim clip instead of a dry-land frame.
- `CoopTableStealthPlayModeTests` requires Cocoa's distinct Flop pose during the sustained decoy.
- `ActorSignalBadgePlayModeTests` pins the shared state-to-acting grammar.
- Focused completion coverage passed **46/46**.
- The complete PlayMode suite passed **822/822**.
- The macOS development player built successfully and completed the graphics-enabled
  `--arena-art-review` run: **78/78** frames (26 missions × start/main/payoff) plus the manifest,
  with no logged exceptions.
- Full-roster and affected-mission contact sheets are stored under
  `captures/art-animation-completion-2026-07-26/`.

## Manual couch review

At a normal TV distance, verify:

1. Cheddar and Cocoa remain instantly distinguishable in every new pose.
2. Swim and wet-shake read before the water tint or HUD is noticed.
3. Cocoa's Table Stealth flop stays visible while Cheddar sneaks.
4. Thunderstorm comfort reads as reassurance, not generic celebration.
5. Human attention/alarm/success transitions are noticeable but do not make objective locations
   wobble.
6. No completion sprite shows chroma fringe, neighboring-atlas fragments, clipped anatomy, or a
   shifted paw baseline.
