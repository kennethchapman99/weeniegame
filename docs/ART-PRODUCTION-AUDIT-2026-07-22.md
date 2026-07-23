# Production Art Audit — 2026-07-22

## Scope and decision

This pass audited the active Unity project only: `unity/CheddarAndCocoa/`. Frozen TypeScript and
prototype folders were not changed. The review covered the live `ArenaFinal` resource tree,
`FinalGameplayArt`, mission-controller visual attachments, character-motion resources, mission
tiles, world/HUD skins, PlayMode art checks, and the explicit placeholder limits in
`ARENA-PLAYABLE.md` and `VISUAL-READABILITY-CONTRACT.md`.

The highest-impact uncovered gap was the newly registered **Tick Invasion** mission. It had a
mission-select tile, but the playable interaction reused `dog_bowl` as its pool, displayed no tick
silhouette on either dog, and communicated groom/rinse success through labels and generic juice.
That violated the silhouette-first and state-change parts of the visual readability contract.

## Ranked remaining production needs

1. **Character style convergence (P0):** Cheddar/Cocoa and several threat cutouts still lean toward
   rendered/realistic fur while the mission props use flat cel-shaded cartoons. Re-author the two
   dog master turnarounds first, then regenerate directional motion from one approved silhouette.
2. **Authored character animation (P0):** current run/bark/dig/sniff and threat strips are readable,
   but many frames are transform-derived. Replace the most repeated verbs—run, bark, groom, tug,
   stunned/rescue—with hand-authored key poses and consistent timing.
3. **Mission-state pack review (P1):** the 25-mission roster has broad generated state coverage, but
   each mission still needs a couch-distance review for scale, silhouette, palette, and before/after
   state contrast. Prioritize any prop that still needs a world label to identify it.
4. **Environment cohesion (P1):** the backyard mixes painterly/photo-inspired plates, flat prop
   overlays, and generated geometry. Produce a single approved ground/landmark paintover and tune
   overlay saturation/outline weight against it before adding more decoration.
5. **HUD and transition motion (P1):** mission select and end cards have a generated skin but remain
   runtime IMGUI/TMP surfaces. Add authored focus, confirm, lock, clear, fail, and next-mission
   transitions while preserving controller navigation and accessibility toggles.
6. **Interaction VFX and physical response (P2):** bark, pickup, rescue, and several mission beats
   have generic bursts. Continue adding verb-specific effects only where they communicate a rule:
   hold, transfer, catch, rinse, comfort, distract, or defend.
7. **Final mix and capture review (P2):** presentation still needs a two-player couch mix and a
   fresh `--arena-art-review` capture audit at representative gameplay zooms.

## Implemented in this pass: Tick Invasion production pack

Five transparent, no-text sprites now live under `ArenaFinal`:

- `Props/TickInvasion/tick_invasion_pool`
- `Props/TickInvasion/tick_invasion_swarm`
- `Props/TickInvasion/tick_invasion_super_tick`
- `VFX/TickInvasion/tick_invasion_groom_burst`
- `VFX/TickInvasion/tick_invasion_rinse_burst`

`TickInvasionMissionController` now owns their runtime use. The pool replaces the dog-bowl stand-in
and pulses as a proximity affordance. Each dog gets a mission-local infestation overlay that grows,
brightens, and pulses with pressure; the Super Tick swaps to a distinct urgent silhouette; wet dogs
receive a cool tint. Successful grooming and pool dives spawn different short-lived art bursts.
Gameplay state, collision, score, timing, and mission outcome logic are unchanged.

## Generation record

The assets were created with the built-in image-generation path, then converted from a flat
`#00ff00` chroma background with the imagegen skill's `remove_chroma_key.py` helper. All outputs were
validated as RGBA PNGs with transparent corners and visually inspected after key removal.

Shared prompt contract: polished flat cel-shaded storybook Unity sprite; warm saturated palette;
thick dark-navy outline; clean couch-distance silhouette; no baked text, logo, watermark, cast
shadow, background texture, floor plane, or photorealism. Asset-specific subjects were: top-down
kiddie pool with orange bone and teal paw accents; compact seven-tick swarm; oversized magenta Super
Tick; brush/paws/ticks grooming burst; and open-center turquoise rinse splash throwing ticks away.

## Acceptance evidence

- Every path is centralized in `FinalGameplayArt.TickInvasionArtPack`.
- PlayMode coverage checks that all five resources load, the pool uses its authored sprite, clean
  dogs hide the overlay, pressure reveals the swarm, Super Tick swaps the sprite, and groom/rinse
  actions spawn distinct VFX objects.
- Manual couch check: confirm the pool footprint matches its interaction radius, infestation never
  obscures dog identity, the Super Tick reads as comic rather than frightening, and both bursts are
  legible without reading the HUD.

The macOS dev build succeeded and its updated `--arena-art-review` harness produced all 75 expected
frames (25 missions × start/main/payoff). The three Tick Invasion frames were inspected at the
player's 1920×1080 target: clean dogs remain unobscured, the rising swarms read at body scale, the
Super Tick is unmistakable, the pool fits both dogs, and draw order keeps dogs inside the water while
the urgent tick remains above character art. Focused Tick Invasion PlayMode evidence: **19/19**
passing on 2026-07-22.
