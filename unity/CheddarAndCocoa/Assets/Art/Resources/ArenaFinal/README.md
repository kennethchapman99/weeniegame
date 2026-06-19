# ArenaFinal Runtime Sprites

This folder contains individual transparent gameplay sprites generated from the repository's existing draft art. Do not place source sheets here.

Regenerate the sprites and source inventory from the repository root:

```sh
python3 tools/art/export_arena_final.py
```

Unity imports these PNGs as single sprites at 256 pixels per unit, with input alpha, transparency processing, bilinear filtering, no mipmaps, and high-quality compression. Unity's default tight sprite mesh is retained. Runtime code must treat these sprites as cosmetic overlays: generated objects and colliders remain the gameplay authority, and missing sprites must fall back safely.

See `docs/ART-INTEGRATION-SLICE.md` for the live mappings, source limitations, and review checklist.
