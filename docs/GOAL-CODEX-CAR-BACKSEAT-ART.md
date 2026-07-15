# GOAL (for codex): Replace the placeholder Car Ride backseat art pack with genuinely great art

> Authored 2026-07-14 by Ken (via Claude), immediately after the Car Ride backseat redesign
> landed on `main` as `7823a99`. Delete this file when the goal is complete.

Pull latest `origin/main` first (commit `7823a99` "Redesign Car Ride into backseat chaos"
landed 2026-07-14 — rebase your local work onto it before touching anything).

The mission is now a backseat stage set: the whole cabin tilts during turns, dogs and
loose junk slide across the bench, a cooler and toy bin sweep past (jump them), and the
dashboard driver telegraphs turns/brakes. The current sprites are flat PIL shapes from
`tools/art/generate_car_backseat_pack.py` and they look weak. Make this the best-looking
level in the game while keeping all 535 PlayMode tests green.

## Replace these six sprites

Exact paths + filenames are load-bearing — `FinalGameplayArt.cs` constants and
`FinalArtIntegrationPlayModeTests` assert the sprite names. All under
`unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/Props/CarRide/`:

- `backseat_cabin_shell.png` — full interior framing: doors w/ armrests + handles,
  windshield band up top, front-seat headrests, rear shelf. Rendered 56x33 world units;
  must fully cover the camera frame so zero backyard bleeds through.
- `backseat_bench.png` — the leather bench lane (36x11 world units). This is the floor
  the dogs read against: keep midtones/low-contrast so dogs, cooler, toy bin, and world
  pops stay readable on it.
- `backseat_windshield_scenery.png` — passing neighborhood strip (sky, houses, trees,
  road). **Hard contracts:** (1) must tile seamlessly horizontally — three copies sit
  side by side and scroll-wrap by exactly one plate width
  (`MissionLevelAreaArt.SceneryWrapDistance`); (2) must be **fully opaque edge to edge** —
  this same sprite's alpha is used as the `SpriteMask` shape for the windshield opening.
  If you want a differently-shaped opening, add a dedicated mask sprite and update
  `MissionLevelAreaArt.BuildWindshieldScroller`.
- `car_dashboard_driver.png` — back-of-head driver + wheel + rear-view mirror with eyes.
  This is a signal-badge actor: leave clear space above it for the badge/label.
- `seat_cooler.png`, `seat_toy_bin.png` — the sliding hazards. These must read as DANGER
  at a glance while sliding (`docs/VISUAL-READABILITY-CONTRACT.md`): chunky silhouettes,
  dark outlines, ~2.5 world units.

## Also refresh

`unity/CheddarAndCocoa/Assets/Art/Resources/ArenaFinal/UI/MissionTiles/carride.png` — the
mission-select tile still sells the old "balance" fantasy; redraw it as backseat chaos
(tilting cabin, flying cooler, bracing dogs).

## Style

Match the established flat-cartoon language (dark `#33251c` outlines, soft shadows) but
push way past the current shape-primitive quality: real shading, fabric texture on the
bench, depth in the scenery, personality in the driver. Look at the strongest existing
packs (Kitchen, Pee Break living room) as the bar to clear.

## Pipeline rule

Art must be reproducible — either upgrade `tools/art/generate_car_backseat_pack.py`, or
add a new export/derivation script under `tools/art/` and document the source in
`Assets/Art/ReferenceOnly/GeneratedCarBackseat/`. No one-off untracked Photoshop files.

## Do not touch gameplay

`CarRideMissionController.cs` geometry (seat rect, obstacle sizes, plate world sizes in
`MissionLevelAreaArt.CreateCarRideArea`) only changes if the art truly demands it, and
every change must keep the suite green.

## Done means

- `./unity/run-playmode-tests.sh` exits 0 with 535/535 passed.
- A before/after contact sheet exists in `Assets/Art/ReferenceOnly/GeneratedCarBackseat/`.
- A screenshot of the mission running (`ArenaArtReviewCapture` covers CarRide) shows the
  new set with the cabin tilted mid-turn.

## Heads-up

You (codex) have uncommitted work in this repo including a pressure-HUD edit to
`CarRideMissionController` that reads a `Balance` field which no longer exists — reconcile
that against the new controller (the meter should read slide intensity or
`Tumbles/MaxTumbles`, and `CouchFeedbackPlayModeTests` must use
`ForceBeginRoadEvent`/`ForceTurnSlide`, not `ForceCarBalance`) before or as part of this
goal.
