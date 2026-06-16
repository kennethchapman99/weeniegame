/**
 * core/camera.ts — maps the 960x600 logical world to a DPR-scaled, letterboxed canvas.
 *
 * Ported faithfully from the prototype's `fit()` / `toWorld()`:
 *   dpr   = min(devicePixelRatio, 3)
 *   scale = min(viewportW / W, viewportH / H)        // letterbox fit
 *   cssW  = round(W * scale), cssH = round(H * scale)
 *   offX  = (viewportW - cssW) / 2                    // centering (CSS px)
 *   pixel canvas = round(cssW * dpr) x round(cssH * dpr)
 *   ctx transform = (dpr*scale) so 1 world unit -> dpr*scale device px
 *   toWorld(clientX, clientY) = ((client - off) / scale)
 *
 * The viewport math is pure (computeViewport) so it can be unit-tested without a DOM;
 * apply()/applyTransform() do the DOM/ctx mutation.
 */

import { WORLD } from '../config/balance.js';
import type { Point } from './math.js';

export interface Viewport {
  dpr: number;
  scale: number;
  cssW: number;
  cssH: number;
  pxW: number;
  pxH: number;
  offX: number;
  offY: number;
}

/** Pure: given viewport size + device pixel ratio, compute the letterbox transform. */
export function computeViewport(
  viewportW: number,
  viewportH: number,
  devicePixelRatio: number,
  world: { w: number; h: number; dprCap: number } = WORLD,
): Viewport {
  const dpr = Math.min(devicePixelRatio || 1, world.dprCap);
  const scale = Math.min(viewportW / world.w, viewportH / world.h);
  const cssW = Math.round(world.w * scale);
  const cssH = Math.round(world.h * scale);
  return {
    dpr,
    scale,
    cssW,
    cssH,
    pxW: Math.round(cssW * dpr),
    pxH: Math.round(cssH * dpr),
    offX: (viewportW - cssW) / 2,
    offY: (viewportH - cssH) / 2,
  };
}

export class Camera {
  view: Viewport;

  constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly ctx: CanvasRenderingContext2D,
  ) {
    this.view = computeViewport(window.innerWidth, window.innerHeight, window.devicePixelRatio);
    this.apply();
  }

  /** Recompute from the current window size and resize/position the canvas. */
  fit(): void {
    this.view = computeViewport(window.innerWidth, window.innerHeight, window.devicePixelRatio);
    this.apply();
  }

  private apply(): void {
    const v = this.view;
    this.canvas.style.width = `${v.cssW}px`;
    this.canvas.style.height = `${v.cssH}px`;
    this.canvas.style.left = `${v.offX}px`;
    this.canvas.style.top = `${v.offY}px`;
    this.canvas.width = v.pxW;
    this.canvas.height = v.pxH;
    this.applyTransform();
  }

  /**
   * Set the ctx transform so subsequent draws use world units. Call after any save/restore.
   * `ox`/`oy` are an optional camera offset in WORLD units (used for screen shake).
   */
  applyTransform(g: CanvasRenderingContext2D = this.ctx, ox = 0, oy = 0): void {
    const k = this.view.dpr * this.view.scale;
    g.setTransform(k, 0, 0, k, ox * k, oy * k);
  }

  /** Pointer/client (CSS px, viewport-relative) -> world units. */
  screenToWorld(clientX: number, clientY: number): Point {
    return screenToWorld(clientX, clientY, this.view);
  }

  /** World units -> client/CSS px (viewport-relative). */
  worldToScreen(p: Point): Point {
    return worldToScreen(p, this.view);
  }
}

export function screenToWorld(clientX: number, clientY: number, v: Viewport): Point {
  return { x: (clientX - v.offX) / v.scale, y: (clientY - v.offY) / v.scale };
}

export function worldToScreen(p: Point, v: Viewport): Point {
  return { x: p.x * v.scale + v.offX, y: p.y * v.scale + v.offY };
}
