/**
 * render/backdrop.ts — pre-rendered scene background with painterly paper-grain + vignette,
 * cached to an offscreen canvas (ported from the prototype's `sceneBG` + grain block).
 *
 * Painters draw in WORLD units; the cache canvas is sized in device pixels and pre-loaded
 * with the dpr*scale transform so painters need no awareness of DPR. Cache key includes the
 * pixel size so a resize repaints. The grain noise is seeded (deterministic, lint-clean).
 */

import type { Rng } from '../core/rng.js';
import { makeRng } from '../core/rng.js';
import type { Viewport } from '../core/camera.js';
import { WORLD } from '../config/balance.js';

type G = CanvasRenderingContext2D;
export type ScenePainter = (g: G, rng: Rng) => void;

const W = WORLD.w;
const H = WORLD.h;
const GRAIN_SEED = 0x6d617074; // "mapt"
const SCATTER_SEED = 0x79617264; // "yard"

function makeGrain(): HTMLCanvasElement {
  const c = document.createElement('canvas');
  c.width = c.height = 256;
  const g = c.getContext('2d');
  if (!g) return c;
  const rng = makeRng(GRAIN_SEED);
  const im = g.createImageData(256, 256);
  for (let i = 0; i < im.data.length; i += 4) {
    const v = 120 + rng.next() * 135;
    im.data[i] = v;
    im.data[i + 1] = v;
    im.data[i + 2] = v;
    im.data[i + 3] = rng.next() * 16;
  }
  g.putImageData(im, 0, 0);
  return c;
}

export class Backdrop {
  private grain = makeGrain();
  private cache: HTMLCanvasElement | null = null;
  private key = '';

  /** Get (or repaint) the cached backdrop for `key`, sized to the current viewport. */
  get(key: string, painter: ScenePainter, view: Viewport): HTMLCanvasElement {
    const ck = `${key}@${view.pxW}x${view.pxH}`;
    if (this.cache && this.key === ck) return this.cache;

    const c = document.createElement('canvas');
    c.width = view.pxW;
    c.height = view.pxH;
    const g = c.getContext('2d');
    if (g) {
      const k = view.dpr * view.scale;
      g.setTransform(k, 0, 0, k, 0, 0);
      painter(g, makeRng(SCATTER_SEED));

      // paper grain overlay
      const pat = g.createPattern(this.grain, 'repeat');
      if (pat) {
        g.globalAlpha = 0.55;
        g.fillStyle = pat;
        g.fillRect(0, 0, W, H);
        g.globalAlpha = 1;
      }
      // vignette
      const v = g.createRadialGradient(W / 2, H / 2, H * 0.45, W / 2, H / 2, H * 0.95);
      v.addColorStop(0, 'rgba(0,0,0,0)');
      v.addColorStop(1, 'rgba(20,14,10,.32)');
      g.fillStyle = v;
      g.fillRect(0, 0, W, H);
    }

    this.cache = c;
    this.key = ck;
    return c;
  }
}
