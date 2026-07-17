# Generated Car Backseat Art

`Sources/` contains the tracked authored bitmap masters for the Car Ride backseat set. They were
generated with the built-in OpenAI image generation tool on 2026-07-14, using the existing Car Ride
tile and the project's Kitchen/Pee Break painterly-cartoon language as visual references. The
chroma masters deliberately use a flat green field so the runtime props can be derived with alpha.

Run this deterministic exporter from any working directory:

```sh
python3 tools/art/generate_car_backseat_pack.py
```

The exporter writes the six load-bearing `ArenaFinal/Props/CarRide` sprites and
`ArenaFinal/UI/MissionTiles/carride.png`. It owns the windshield opening alignment, chroma cleanup,
runtime dimensions, and review sheets. The neighborhood strip is code-authored so its first and
last columns are byte-identical and its alpha is fully opaque, satisfying the scrolling/SpriteMask
contract in `MissionLevelAreaArt`.

Review provenance:

- `car_backseat_pack_before.png` and `carride_tile_before.png` preserve the replaced placeholder
  pack/tile for comparison.
- `car_backseat_pack_contact_sheet.png` is regenerated from the seven current exports.
- `car_backseat_before_after_contact_sheet.png` is the required side-by-side evidence sheet.

Do not hand-edit the runtime PNGs. Update a tracked source master or the exporter and regenerate.
