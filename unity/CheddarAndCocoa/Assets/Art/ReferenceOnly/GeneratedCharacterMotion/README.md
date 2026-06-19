# Generated Character Motion Review Boards

Reference-only AI-assisted exploration belongs here. These files are not included by Unity's `Resources` build pipeline and must not be used directly by gameplay.

Generated first batch:

- `cheddar_turnaround_v01.png`
- `cheddar_action_keyposes_v01.png`
- `cocoa_turnaround_v01.png`
- `cocoa_action_keyposes_v01.png`
- `cheddar_tier_a_east_v01.png` — idle/run/bark, four frames per row
- `cocoa_tier_a_east_v01.png` — idle/run/bark, four frames per row

See `docs/CHARACTER-MOTION-PACK.md` for approval gates and runtime export naming.

Status: **reference-only / needs review**. The built-in generator returned RGB PNGs with a baked
checkerboard instead of true alpha. Keep these boards out of ArenaFinal; use them to approve identity,
angles, and pose language before per-frame generation/background extraction.

The two Tier-A east boards deliberately use flat near-white backgrounds and are deterministic
extraction sources. Their 24 normalized true-alpha cells are the only generated motion art currently
promoted to ArenaFinal.
