/**
 * core/loop.ts — fixed-timestep game loop with a strict update/render split (CLAUDE.md
 * hard rule #3/#4). update(dt) runs at a constant dt (default 1/60) via an accumulator so
 * simulation is deterministic regardless of frame rate; render(alpha) draws once per frame.
 *
 * Headless tests drive the systems directly at a fixed dt and never construct this loop, so
 * it stays browser-only (uses requestAnimationFrame/performance.now).
 */

export interface LoopHandlers {
  update: (dt: number) => void;
  render: (alpha: number) => void;
  /** fixed simulation step in seconds (default 1/60) */
  step?: number;
  /** clamp on a single frame's elapsed time, to avoid spiral-of-death (default 0.25s) */
  maxFrame?: number;
}

export function startLoop(h: LoopHandlers): () => void {
  const step = h.step ?? 1 / 60;
  const maxFrame = h.maxFrame ?? 0.25;
  let last = performance.now() / 1000;
  let acc = 0;
  let raf = 0;

  const frame = (nowMs: number): void => {
    const now = nowMs / 1000;
    let dt = now - last;
    last = now;
    if (dt > maxFrame) dt = maxFrame;
    acc += dt;
    while (acc >= step) {
      h.update(step);
      acc -= step;
    }
    h.render(acc / step);
    raf = requestAnimationFrame(frame);
  };
  raf = requestAnimationFrame(frame);

  return () => cancelAnimationFrame(raf);
}
