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
- `cheddar_outcomes_east_v01.png` / `cocoa_outcomes_east_v01.png` — stunned/rescued/proud/sad pairs
- `cheddar_carry_east_v01.png` / `cocoa_carry_east_v01.png` — prop-free persistent carry pair

Active flat-storybook identity and locomotion boards:

- `cheddar_storybook_model_sheet_v02.png`
- `cocoa_storybook_model_sheet_v02.png`
- `cheddar_storybook_run_east_v02.png`
- `cocoa_storybook_run_east_v02.png`

The V02 model sheets are the identity authority for new character art. The run boards use those
sheets as hard references and the original pose sheets only for movement language. Cocoa's canonical
coat is uniform deep chocolate with warm-brown points and no cream or white markings.

See `docs/CHARACTER-MOTION-PACK.md` for approval gates and runtime export naming.

Status: **reference-only / needs review**. The built-in generator returned RGB PNGs with a baked
checkerboard instead of true alpha. Keep these boards out of ArenaFinal; use them to approve identity,
angles, and pose language before per-frame generation/background extraction.

The V01 Tier-A boards deliberately use flat near-white backgrounds and remain deterministic
extraction sources for the legacy directional fallback. V02 run boards are transparent review
sources sliced by `tools/art/export_storybook_run.py`.
