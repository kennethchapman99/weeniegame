# Generated Character Motion Review Boards

Reference-only AI-assisted exploration belongs here. These files are not included by Unity's `Resources` build pipeline and must not be used directly by gameplay.

Generated first batch:

- `cheddar_turnaround_v01.png`
- `cheddar_action_keyposes_v01.png`
- `cocoa_turnaround_v01.png`
- `cocoa_action_keyposes_v01.png`
- `cheddar_tier_a_east_v01.png` — idle/run/bark, four frames per row
- `cocoa_tier_a_east_v01.png` — idle/run/bark, four frames per row
- `cheddar_run_diagonals_v01.png` — southeast/northeast run, four frames per row
- `cocoa_run_diagonals_v01.png` — southeast/northeast run, four frames per row
- `cheddar_run_straights_v01.png` — south/north run, four frames per row
- `cocoa_run_straights_v01.png` — south/north run, four frames per row
- `cheddar_tug_east_v01.png` — east tug brace/pull/recovery strip
- `cocoa_tug_east_v01.png` — east tug brace/pull/recovery strip
- `cheddar_bark_diagonals_v01.png` / `cocoa_bark_diagonals_v01.png` — SE/NE bark boards
- `cheddar_bark_straights_v01.png` / `cocoa_bark_straights_v01.png` — S/N bark boards
- `cheddar_idle_diagonals_v01.png` / `cocoa_idle_diagonals_v01.png` — SE/NE idle boards
- `cheddar_idle_straights_v01.png` / `cocoa_idle_straights_v01.png` — S/N idle boards

See `docs/CHARACTER-MOTION-PACK.md` for approval gates and runtime export naming.

Status: **reference-only / needs review**. The built-in generator returned RGB PNGs with a baked
checkerboard instead of true alpha. Keep these boards out of ArenaFinal; use them to approve identity,
angles, and pose language before per-frame generation/background extraction.

The Tier-A boards deliberately use flat near-white backgrounds and are deterministic extraction
sources. Their 126 normalized true-alpha cells are the generated motion art currently promoted to
ArenaFinal.
