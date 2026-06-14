/**
 * core/math.ts — small geometry/color helpers ported faithfully from the prototype.
 * Logic randomness is NOT here (see core/rng.ts); these are pure functions.
 */

export interface Point {
  x: number;
  y: number;
}

/** clamp v into [lo, hi] */
export function clamp(v: number, lo: number, hi: number): number {
  return v < lo ? lo : v > hi ? hi : v;
}

/** linear interpolation */
export function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}

/** euclidean distance between two points — prototype's `dist(a, b)` */
export function dist(a: Point, b: Point): number {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

/**
 * Trace a rounded rectangle path — prototype's `rounded(g, x, y, w, h, r)`.
 * Caller is responsible for fill()/stroke() afterwards.
 */
export function rounded(
  g: CanvasRenderingContext2D,
  x: number,
  y: number,
  w: number,
  h: number,
  r: number,
): void {
  g.beginPath();
  g.moveTo(x + r, y);
  g.arcTo(x + w, y, x + w, y + h, r);
  g.arcTo(x + w, y + h, x, y + h, r);
  g.arcTo(x, y + h, x, y, r);
  g.arcTo(x, y, x + w, y, r);
  g.closePath();
}

/**
 * Lighten/darken a `#rrggbb` hex by `amt` per channel — prototype's `shade(hex, amt)`.
 * Used to derive the wet (darker) coat palettes, etc.
 */
export function shade(hex: string, amt: number): string {
  const n = parseInt(hex.slice(1), 16);
  let r = (n >> 16) + amt;
  let g = ((n >> 8) & 255) + amt;
  let b = (n & 255) + amt;
  r = clamp(r, 0, 255);
  g = clamp(g, 0, 255);
  b = clamp(b, 0, 255);
  return '#' + (((r << 16) | (g << 8) | b) >>> 0).toString(16).padStart(6, '0');
}
