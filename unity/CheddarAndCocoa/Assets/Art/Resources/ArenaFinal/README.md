# ArenaFinal Runtime Art

Drop final transparent PNG gameplay sprites here when they are ready. Files under this `Resources/ArenaFinal` tree are preferred by `FinalGameplayArt`, `FinalDogPoseArt`, `RuntimeArtSpriteFactory`, and `ArenaDogPoseSprites` before draft-sheet crops.

Expected paths:

```text
ArenaFinal/
  Characters/
    Dogs/
      Cheddar/
        cheddar_idle.png
        cheddar_run.png
        cheddar_bark.png
        cheddar_tug.png
        cheddar_stunned.png
        cheddar_rescued.png
        cheddar_proud.png
        cheddar_sad.png
      Cocoa/
        cocoa_idle.png
        cocoa_run.png
        cocoa_bark.png
        cocoa_tug.png
        cocoa_stunned.png
        cocoa_rescued.png
        cocoa_proud.png
        cocoa_sad.png
    Squirrel/
      squirrel_idle.png
      squirrel_steal.png
      squirrel_scared.png
    Eagle/
      eagle_threat.png
      eagle_action.png
    Coyote/
      coyote_threat.png
  Props/
    Backyard/
      bush.png
      fence_section.png
      rock.png
      grass_patch.png
      dig_spot.png
    Mission/
      rope_tug.png
      rope_complete.png
      weenie_collectible.png
      dog_bowl.png
    Missions/
      snack_plate.png
      sock_bundle.png
      laundry_basket.png
      laundry_basket_open.png
      squirrel_stash.png
      escape_gap.png
      gate.png
      squeaky_toy.png
      steak_plate.png
      table_human.png
      decoy_toy.png
      walk_human.png
      walk_leash.png
      car_balance.png
      dig_mound.png
      buried_bone.png
      scent_post.png
      territory_zone.png
      leash_checkpoint.png
      bone_mound.png
      chaos_lever.png
      chaos_junction.png
      escape_station.png
      catch_blanket.png
      falling_snack.png
      kitchen_counter.png
      kitchen_safe_bowl.png
      kitchen_good_food.png
      kitchen_bad_food.png
      kitchen_warning.png
    PeeBreak/
      pee_break_couch.png
      pee_break_teenager.png
      pee_break_phone_charger.png
      pee_break_open_door.png
      pee_break_leash.png
      pee_break_hydrant_relief.png
      pee_break_bladder_meter.png
      pee_break_misread_tennis_ball.png
    Wow/
      cheddar_cocoa_duo.png
      motif_pee_break.png
      motif_food_heist.png
      motif_threat_watch.png
      motif_adventure_route.png
      motif_backyard_props.png
  VFX/
    bark_burst.png
    bark_ring.png
    pickup_sparkle.png
    success_pop.png
    warning_alert.png
    rescue_burst.png
    fail_puff.png
```

Import guidance:

- Texture Type: Sprite (2D and UI)
- Alpha Source: Input Texture Alpha
- Mesh Type: Tight where safe, Full Rect if slicing creates artifacts
- Pixels Per Unit: tune visually, but keep RuntimeArtSpriteFactory overlay scales stable after final art lands
- Background: transparent, not keyed white
- Crop: tight around the gameplay object, not a full design sheet

Do not place pose/expression sheets here. This folder is for individual runtime sprites only.
