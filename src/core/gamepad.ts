/**
 * core/gamepad.ts — Gamepad input adapter (M9).
 *
 * Produces the same `Intent` shape as touch/keyboard (see core/input.ts) so a player can be
 * driven by a controller with zero changes downstream: the left stick's *direction* feeds
 * ax/ay (moveDog normalises it, magnitude ignored) and its *deflection* beyond the deadzone
 * feeds `arrive` (0..1 speed scalar), mirroring touch's arrive ramp for analog speed control.
 *
 * Face buttons are edge-detected (one action per press, like a key edge):
 *   A (button 0) → wrestle    ·    B (button 1) → jump
 *
 * The pure helpers (stickIntent, pressedEdges) are DOM-free and unit-tested; only
 * GamepadSource.poll() touches `navigator`, guarded for headless/non-browser environments.
 */

import type { Intent } from './input.js';

/** Standard-mapping face-button indices we react to. */
export const PAD = { wrestle: 0, jump: 1 } as const;

/**
 * Left-stick deflection → steering Intent.
 * Below the deadzone there's no input (arrive 0); above it, arrive ramps 0..1 across the
 * remaining travel so a half-pushed stick walks and a full push runs.
 */
export function stickIntent(x: number, y: number, deadzone: number): Intent {
  const mag = Math.hypot(x, y);
  if (mag < deadzone) return { ax: 0, ay: 0, arrive: 0 };
  const arrive = Math.min(1, (mag - deadzone) / (1 - deadzone));
  return { ax: x, ay: y, arrive };
}

/** Rising edges: true at index i when the button is pressed now but was not last poll. */
export function pressedEdges(prev: boolean[], cur: boolean[]): boolean[] {
  return cur.map((c, i) => c && !prev[i]);
}

export interface GamepadResult {
  /** Steering intent, or null when the stick is inside the deadzone (no movement input). */
  intent: Intent | null;
  wrestle: boolean;
  jump: boolean;
  /** True while at least one gamepad is connected (drives HUD button hiding). */
  connected: boolean;
}

const IDLE: GamepadResult = { intent: null, wrestle: false, jump: false, connected: false };

/** Per-frame poller for the first connected gamepad. Holds button state for edge detection. */
export class GamepadSource {
  private prev: boolean[] = [];

  /** Poll once per fixed update step. Safe to call when no gamepad / no navigator exists. */
  poll(deadzone: number): GamepadResult {
    const pads =
      typeof navigator !== 'undefined' && navigator.getGamepads ? navigator.getGamepads() : [];
    let pad: Gamepad | null = null;
    for (const p of pads) {
      if (p) {
        pad = p;
        break;
      }
    }
    if (!pad) {
      this.prev = [];
      return IDLE;
    }

    const buttons = pad.buttons.map((b) => b.pressed);
    // On the first poll after a (re)connect, button counts differ or prev is empty — adopt the
    // current state without firing edges, so a button held at connect-time isn't a phantom press.
    const edges =
      this.prev.length === buttons.length ? pressedEdges(this.prev, buttons) : buttons.map(() => false);
    this.prev = buttons;

    const intent = stickIntent(pad.axes[0] ?? 0, pad.axes[1] ?? 0, deadzone);
    return {
      intent: intent.arrive > 0 ? intent : null,
      wrestle: !!edges[PAD.wrestle],
      jump: !!edges[PAD.jump],
      connected: true,
    };
  }
}
