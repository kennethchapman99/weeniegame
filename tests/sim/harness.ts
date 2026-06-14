/**
 * tests/sim/harness.ts — headless browser stubs so the REAL game systems run unmodified
 * in Node (Vitest). Port of the prototype's external validation harness (see docs/TESTING.md).
 *
 * It stubs: a Canvas2D context (Proxy of no-ops + gradient/measureText/imageData),
 * <canvas> elements, document.getElementById/createElement, window, and AudioContext.
 *
 * Usage:  installDom();  // before importing modules that touch the DOM at import time
 */

const noop = (): void => {};
const grad = { addColorStop: noop };

/** A Canvas2D context where every property read is a no-op, with a few real-shape returns. */
export function makeCtx2d(): CanvasRenderingContext2D {
  return new Proxy(
    {},
    {
      get: (_t, p) => {
        if (p === 'createLinearGradient' || p === 'createRadialGradient') return () => grad;
        if (p === 'createPattern') return () => ({});
        if (p === 'measureText') return () => ({ width: 50 });
        if (p === 'getImageData' || p === 'createImageData')
          return (w: number, h: number) => ({ data: new Uint8ClampedArray(w * h * 4) });
        if (p === 'putImageData') return noop;
        if (p === 'canvas') return makeCanvas();
        return noop;
      },
      set: () => true,
    },
  ) as unknown as CanvasRenderingContext2D;
}

export function makeCanvas(width = 960, height = 600): HTMLCanvasElement {
  const el: Record<string, unknown> = {
    width,
    height,
    style: {},
    getContext: () => makeCtx2d(),
    addEventListener: noop,
    removeEventListener: noop,
    setPointerCapture: noop,
    getBoundingClientRect: () => ({ left: 0, top: 0, width, height, right: width, bottom: height }),
  };
  return el as unknown as HTMLCanvasElement;
}

class StubAudioContext {
  destination = {};
  currentTime = 0;
  state = 'running';
  createOscillator(): unknown {
    return {
      type: 'sine',
      frequency: { value: 0, setValueAtTime: noop, linearRampToValueAtTime: noop },
      connect: () => ({ connect: noop }),
      start: noop,
      stop: noop,
    };
  }
  createGain(): unknown {
    return { gain: { value: 1, setValueAtTime: noop, linearRampToValueAtTime: noop }, connect: noop };
  }
  resume(): Promise<void> {
    return Promise.resolve();
  }
}

/** Install the stubs onto globalThis. Idempotent. */
export function installDom(): void {
  const g = globalThis as unknown as Record<string, unknown>;
  const sharedCanvas = makeCanvas();
  const doc = {
    getElementById: () => sharedCanvas,
    createElement: (tag: string) => (tag === 'canvas' ? makeCanvas() : { style: {} }),
    addEventListener: noop,
  };
  g.document = doc;
  g.window = {
    innerWidth: 960,
    innerHeight: 600,
    devicePixelRatio: 2,
    addEventListener: noop,
    removeEventListener: noop,
  };
  g.innerWidth = 960;
  g.innerHeight = 600;
  g.devicePixelRatio = 2;
  g.AudioContext = StubAudioContext as unknown;
  g.requestAnimationFrame = (cb: FrameRequestCallback): number => {
    cb(0);
    return 0;
  };
  g.cancelAnimationFrame = noop;
}
