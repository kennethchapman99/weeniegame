import { describe, it, expect, beforeAll } from 'vitest';
import { installDom, makeCtx2d, makeCanvas } from './harness.js';

describe('headless harness stubs', () => {
  beforeAll(() => installDom());

  it('installs document / window / AudioContext globals', () => {
    expect(typeof document.getElementById).toBe('function');
    expect((window as Window).innerWidth).toBe(960);
    expect(typeof (globalThis as { AudioContext?: unknown }).AudioContext).toBe('function');
  });

  it('lets real Canvas2D-shaped code run without throwing', () => {
    const ctx = makeCtx2d();
    // Exercise the no-op proxy + the real-shape returns the systems rely on.
    ctx.fillRect(0, 0, 10, 10);
    ctx.save();
    ctx.restore();
    const g = ctx.createLinearGradient(0, 0, 1, 1);
    g.addColorStop(0, '#fff');
    expect(ctx.measureText('x').width).toBe(50);
    const img = ctx.createImageData(4, 4);
    expect(img.data.length).toBe(4 * 4 * 4);
  });

  it('canvas stub reports dimensions and yields a context', () => {
    const c = makeCanvas(960, 600);
    expect(c.width).toBe(960);
    expect(c.getContext('2d')).toBeTruthy();
  });
});
