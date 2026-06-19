# ArenaFinal Runtime Art

Drop final transparent PNG gameplay sprites here when they are ready. Files under this `Resources/ArenaFinal` tree are preferred by `FinalGameplayArt` / `RuntimeArtSpriteFactory` before draft-sheet crops.

Expected paths:

```text
ArenaFinal/
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

Import guidance:

- Texture Type: Sprite (2D and UI)
- Alpha Source: Input Texture Alpha
- Mesh Type: Tight where safe, Full Rect if slicing creates artifacts
- Pixels Per Unit: tune visually, but keep RuntimeArtSpriteFactory overlay scales stable after final art lands
- Background: transparent, not keyed white
- Crop: tight around the gameplay object, not a full design sheet

Do not place pose/expression sheets here. This folder is for individual runtime sprites only.
