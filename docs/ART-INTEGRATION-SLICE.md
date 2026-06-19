# Art Integration Vertical Slice

Status: first additive runtime art pass is committed. Unity still needs to import and run tests before treating this as green.

## Priority

The current product priority is in-game visual quality, not menu/progression polish. The target slice is **Backyard Rescue** because it exercises the core fantasy: Cheddar + Cocoa moving, barking, rescuing, stopping squirrels, facing a threat, and completing shared objectives in the yard.

## What Was Added

### Runtime sprite extraction

`unity/CheddarAndCocoa/Assets/Scripts/Game/RuntimeArtSpriteFactory.cs`

- Prefers final transparent sprites under `Resources/ArenaFinal` via `FinalGameplayArt`.
- Falls back to the existing draft art sheets in `Resources/ArenaDraft`.
- Applies simple background keying to remove near-flat sheet backgrounds from draft sheets.
- Provides IDs for squirrel, eagle/coyote threat, backyard props, rope, weenie, bark burst, pickup sparkle, success pop, and warning alert.
- Returns `null` safely when art is missing or unreadable so generated gameplay fallback remains intact.

### Final art path contract

`unity/CheddarAndCocoa/Assets/Scripts/Game/FinalGameplayArt.cs`

- Defines stable `Resources/ArenaFinal/...` paths for runtime-ready transparent PNGs.
- Lets you drop final exported sprites into the project without changing gameplay code.
- Documented in `unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/README.md`.

### Art overlays

`unity/CheddarAndCocoa/Assets/Scripts/Game/ArtSpriteOverlay.cs`

- Adds an `ActualArtOverlay` child to gameplay objects.
- Keeps original generated objects, labels, colliders, and tests intact.
- Adds a simple shadow child.
- Supports pulse feedback.

### VFX pulse

`unity/CheddarAndCocoa/Assets/Scripts/Game/BackyardArtVfxPulse.cs`

- Spawns short-lived art-driven VFX sprites from final art or the draft VFX sheet.
- Used for bark bursts, warning alerts, success pops, and pickup sparkle.

### Arena auto-enhancer

`unity/CheddarAndCocoa/Assets/Scripts/Game/BackyardRescueArtEnhancer.cs`

- Installs automatically when `ArenaScene` loads.
- Waits for `GameManager`.
- Adds overlays to:
  - Squirrel
  - Predator/threat
  - Rope
  - Cheddar/Cocoa shadows
  - a few backyard set-dressing accents
- Watches `GameManager.LastFeedback` and `LastScoreEventLabel` to spawn art VFX without modifying gameplay rules.
- Backyard Rescue gets ambient sparkle/leaf-style pops for more life.

### Dynamic treat art

`unity/CheddarAndCocoa/Assets/Scripts/Game/DynamicTreatArtEnhancer.cs`

- Installs automatically when `ArenaScene` loads.
- Scans for newly spawned `Treat` objects.
- Adds final/draft weenie art overlays when available.
- Keeps `Treat`, colliders, scoring, and respawn logic unchanged.

### Tests

`unity/CheddarAndCocoa/Assets/Tests/PlayMode/BackyardArtIntegrationPlayModeTests.cs`

Covers:

- runtime sprite factory is safe to query whether draft/final art exists or not;
- final-art resource paths remain stable;
- overlays do not break fallback objects when no sprite is available;
- generated runtime sprite reports correctly;
- VFX spawn path is safe;
- dynamic treat art scanning is safe.

## Current Visual Architecture

The current in-game art stack is now:

1. **Generated gameplay geometry** remains the collision/readability fallback.
2. **Final transparent sprites** under `Resources/ArenaFinal` are preferred when present.
3. **Draft-art crops/overlays** sit visually above generated objects when final art is not present.
4. **Art VFX pulses** react to gameplay feedback events.
5. **Cheddar/Cocoa pose sprites** continue to use `ArenaDogPoseSprites` when draft pose sheets are readable.
6. **Labels/debug readability** remain available until final sprites are strong enough to reduce text reliance.

This is the right intermediate stage: more beautiful, still safe.

## ArenaFinal Paths

Expected final sprite locations:

```text
Assets/Art/Resources/ArenaFinal/
  Characters/
    Squirrel/squirrel_idle.png
    Eagle/eagle_threat.png
    Coyote/coyote_threat.png
  Props/
    Backyard/bush.png
    Backyard/fence_section.png
    Backyard/rock.png
    Mission/rope_tug.png
    Mission/weenie_collectible.png
  VFX/
    bark_burst.png
    pickup_sparkle.png
    success_pop.png
    warning_alert.png
```

## Important Guardrails

- Do not remove generated gameplay silhouettes until final transparent sprites are validated at gameplay zoom.
- Do not make colliders depend on art bounds.
- Do not let art overlays block tests.
- Do not hand-tune final scale from one screenshot; verify in full-yard zoom and local camera zoom.
- Do not spend more effort on AdventureMap visuals yet.

## Next Best Work

1. Open Unity and validate compile/import.
2. Run PlayMode tests.
3. Play Backyard Rescue and adjust overlay scales/positions.
4. Replace approximate sheet slice rects in `RuntimeArtSpriteFactory` with exact rects after viewing source sheets in Unity.
5. Export actual tightly cropped transparent sprites to `Resources/ArenaFinal/...`; no code change should be required for the mapped sprite IDs.
6. Add final dog sprite path support for individual Cheddar/Cocoa states, replacing the current pose-sheet crop path.
7. Add final sprite slots for:
   - Cheddar idle/run/bark/tug/stunned/proud/sad;
   - Cocoa idle/run/bark/tug/stunned/proud/sad;
   - squirrel idle/run/scared/steal;
   - predator warning/attack;
   - rope/tug complete;
   - weenie collectible;
   - pickup sparkle/bark burst/warning/success.

## Asset Export Checklist

For the next manual art export pass, prioritize transparent PNGs, tightly cropped, readable at gameplay scale:

### Characters

- `cheddar_idle.png`
- `cheddar_run.png`
- `cheddar_bark.png`
- `cheddar_tug.png`
- `cheddar_stunned.png`
- `cheddar_proud.png`
- `cheddar_sad.png`
- `cocoa_idle.png`
- `cocoa_run.png`
- `cocoa_bark.png`
- `cocoa_tug.png`
- `cocoa_stunned.png`
- `cocoa_proud.png`
- `cocoa_sad.png`
- `squirrel_idle.png`
- `squirrel_steal.png`
- `squirrel_scared.png`

### Backyard props

- `weenie_collectible.png`
- `rope_tug.png`
- `dog_bowl.png`
- `fence_section.png`
- `bush.png`
- `grass_patch.png`
- `dig_spot.png`

### VFX

- `bark_burst.png`
- `bark_ring.png`
- `pickup_sparkle.png`
- `success_pop.png`
- `warning_alert.png`
- `rescue_burst.png`
- `fail_puff.png`

## Success Criteria

Backyard Rescue should feel meaningfully more like a game:

- Cheddar/Cocoa remain readable and charming.
- Squirrel and threat have actual art overlays where source art exists.
- Bark/success/warning moments have visible art VFX.
- Treats/weenies get real-art overlays when final/draft art exists.
- The yard has enough set dressing to feel intentional without cluttering objectives.
- Existing gameplay still passes tests.
