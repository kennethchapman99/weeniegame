# Cheddar & Cocoa Character Art Model Sheets

This is the identity contract for new Unity character art. The production authority is:

1. `Assets/Art/ReferenceOnly/GeneratedCharacterMotion/*_storybook_model_sheet_v02.png`
2. the original owner portraits and pose sheets under `DRAFT assets/`
3. the V01 turnarounds for pose history only
4. existing runtime frames for action reference only

If a runtime frame conflicts with a V02 sheet, the sheet wins. New generation or paintover work must
include the relevant V02 sheet as its identity reference; an older action frame may be supplied only
as a pose reference.

| Trait | Cheddar | Cocoa |
| --- | --- | --- |
| silhouette | extra-long, low dachshund; springy, forward-leading | extra-long, low dachshund; planted, controlled |
| coat | golden-orange | uniform deep chocolate |
| secondary color | pale cream chest and toe tips | subtle warm-brown muzzle, eyebrow points, and lower paws |
| collar | red-orange | teal |
| motion | head leads, loose ears/tail, joyful overshoot | clean arcs, lower bounce, deliberate weight |
| forbidden drift | brown/chocolate recolor, missing cream identity marks | cream/white bib, socks, toe tips, blaze, belly patch, or tail tip |

Both dogs use compact color shapes, a dark navy contour, restrained warm highlights, readable
expressions, and no photoreal fur texture. Collar color, body length, paw baseline, ear volume, and
the location of identity markings must remain stable between frames. At couch distance, silhouette
and collar must identify the dog before facial detail.

The V02 sheets show neutral side, three-quarter, front, and rear views on a review background. They
are reference-only and never load through Unity `Resources`. Approved runtime cutouts use true alpha
on the shared 512×384 motion canvas with paw baseline Y=360.

Before promoting a new clip:

- compare every frame against the matching V02 sheet, not only against the previous generated frame;
- review the full strip as a contact sheet for body-volume, collar, marking, and baseline drift;
- confirm Cocoa has no accidental cream/white body markings;
- confirm the action remains asymmetric: Cheddar energetic, Cocoa deliberate;
- keep the previous clip as a runtime fallback until the replacement has passed PlayMode and rendered
  gameplay review.
