/**
 * core/input.ts — touch-drag (primary) + WASD/arrows + gamepad → a steering Intent in world
 * space, for up to two local players (M10).
 *
 * Ported from the prototype's pointer/keyboard handlers + the input block of update():
 *   - touch target is converted to WORLD units via the camera (the coordinate-space bug
 *     CLAUDE.md warns about — done once, here).
 *   - arrive easing: beyond arriveRadius, arrive ramps 0..1 over arriveFalloff; inside it,
 *     arrive = 0 so the dog eases to a hard stop with no on-spot jitter.
 *
 * Two-player mapping (couch co-op):
 *   - P1: touch + WASD (+ arrows when solo) + gamepad slot 0; space/E = wrestle, J = jump.
 *   - P2: gamepad slot 1, or arrow keys for movement with '/' = wrestle, '.' = jump. In
 *     two-player mode P1 drops the arrow keys so they belong to P2 (no keyboard conflict).
 * The `computeIntent` core stays pure (DOM-free, directly testable); adapters just feed it.
 */

import type { Point } from './math.js';
import type { Camera } from './camera.js';
import { SPEED, INPUT } from '../config/balance.js';
import { GamepadSource, type GamepadResult } from './gamepad.js';

export interface Intent {
  ax: number;
  ay: number;
  arrive: number;
}

/** A full per-dog command for one fixed step: where to steer + discrete action edges. */
export interface DogCmd {
  intent: Intent;
  wrestle: boolean;
  jump: boolean;
}

const NO_PAD: GamepadResult = { intent: null, wrestle: false, jump: false, connected: false };

interface Queue {
  wrestle: boolean;
  jump: boolean;
}

export class Input {
  readonly keys: Record<string, boolean> = {};
  touch: Point | null = null;
  /** Connected gamepad count from the last poll (HUD button hiding + lobby join prompts). */
  padCount = 0;

  private readonly padP1 = new GamepadSource(0);
  private readonly padP2 = new GamepadSource(1);
  private r1: GamepadResult = NO_PAD;
  private r2: GamepadResult = NO_PAD;
  // discrete actions queued from key edges / on-screen buttons / pad edges, per player
  private readonly q1: Queue = { wrestle: false, jump: false };
  private readonly q2: Queue = { wrestle: false, jump: false };

  /**
   * Poll both gamepads once per fixed update step (call before p1Command / p2Command).
   * Button edges queue the same actions as keys; a pushed stick takes precedence over
   * touch/keyboard for that player's step.
   */
  poll(): void {
    this.r1 = this.padP1.poll(INPUT.gamepadDeadzone);
    this.r2 = this.padP2.poll(INPUT.gamepadDeadzone);
    this.padCount = (this.r1.connected ? 1 : 0) + (this.r2.connected ? 1 : 0);
    if (this.r1.wrestle) this.q1.wrestle = true;
    if (this.r1.jump) this.q1.jump = true;
    if (this.r2.wrestle) this.q2.wrestle = true;
    if (this.r2.jump) this.q2.jump = true;
  }

  /** True while any gamepad is connected — lets the host hide the on-screen touch buttons. */
  get gamepadActive(): boolean {
    return this.padCount > 0;
  }

  /** Queue a P1 wrestle (space/E key edge or the on-screen WRESTLE button). */
  queueWrestle(): void {
    this.q1.wrestle = true;
  }

  /** Queue a P1 jump (J key or the on-screen JUMP button). */
  queueJump(): void {
    this.q1.jump = true;
  }

  /**
   * P1 command. A pushed pad-0 stick wins; otherwise touch + WASD (+ arrows when solo).
   * @param twoPlayer when true, arrow keys are reserved for P2.
   */
  p1Command(pos: Point, twoPlayer = false): DogCmd {
    const intent = this.r1.intent ?? computeIntent(this.keys, this.touch, pos, { arrows: !twoPlayer });
    return this.drain(this.q1, intent);
  }

  /** P2 command. A pushed pad-1 stick wins; otherwise arrow keys (no touch for P2). */
  p2Command(pos: Point): DogCmd {
    const intent = this.r2.intent ?? computeIntent(this.keys, null, pos, { wasd: false });
    return this.drain(this.q2, intent);
  }

  /** Title-screen "press to join": true (and clears) if P2 hit any action this poll. */
  takeP2Join(): boolean {
    const joined = this.q2.wrestle || this.q2.jump;
    this.q2.wrestle = false;
    this.q2.jump = false;
    return joined;
  }

  private drain(q: Queue, intent: Intent): DogCmd {
    const cmd = { intent, wrestle: q.wrestle, jump: q.jump };
    q.wrestle = false;
    q.jump = false;
    return cmd;
  }

  /** Attach DOM listeners. Returns a detach function. */
  attach(canvas: HTMLCanvasElement, camera: Camera): () => void {
    const onKeyDown = (e: KeyboardEvent): void => {
      const k = e.key.toLowerCase();
      this.keys[k] = true;
      if (k === ' ' || k === 'e') {
        e.preventDefault();
        this.q1.wrestle = true;
      }
      if (k === 'j') {
        e.preventDefault();
        this.q1.jump = true;
      }
      if (k === '/') {
        e.preventDefault();
        this.q2.wrestle = true;
      }
      if (k === '.') {
        e.preventDefault();
        this.q2.jump = true;
      }
    };
    const onKeyUp = (e: KeyboardEvent): void => {
      this.keys[e.key.toLowerCase()] = false;
    };
    const setTouch = (e: PointerEvent): void => {
      this.touch = camera.screenToWorld(e.clientX, e.clientY);
    };
    const onDown = (e: PointerEvent): void => {
      setTouch(e);
      canvas.setPointerCapture?.(e.pointerId);
    };
    const onMove = (e: PointerEvent): void => {
      if (this.touch) setTouch(e);
    };
    const clear = (): void => {
      this.touch = null;
    };

    addEventListener('keydown', onKeyDown);
    addEventListener('keyup', onKeyUp);
    canvas.addEventListener('pointerdown', onDown);
    canvas.addEventListener('pointermove', onMove);
    canvas.addEventListener('pointerup', clear);
    canvas.addEventListener('pointercancel', clear);

    return () => {
      removeEventListener('keydown', onKeyDown);
      removeEventListener('keyup', onKeyUp);
      canvas.removeEventListener('pointerdown', onDown);
      canvas.removeEventListener('pointermove', onMove);
      canvas.removeEventListener('pointerup', clear);
      canvas.removeEventListener('pointercancel', clear);
    };
  }
}

/**
 * Pure intent computation — directly testable without DOM.
 * `opts.wasd` / `opts.arrows` gate which key set steers (both on by default = solo P1).
 */
export function computeIntent(
  keys: Record<string, boolean>,
  touch: Point | null,
  pos: Point,
  opts: { wasd?: boolean; arrows?: boolean } = {},
): Intent {
  const wasd = opts.wasd ?? true;
  const arrows = opts.arrows ?? true;
  let ax = 0;
  let ay = 0;
  let arrive = 1;
  if ((arrows && keys['arrowleft']) || (wasd && keys['a'])) ax -= 1;
  if ((arrows && keys['arrowright']) || (wasd && keys['d'])) ax += 1;
  if ((arrows && keys['arrowup']) || (wasd && keys['w'])) ay -= 1;
  if ((arrows && keys['arrowdown']) || (wasd && keys['s'])) ay += 1;
  if (touch) {
    const dx = touch.x - pos.x;
    const dy = touch.y - pos.y;
    const td = Math.hypot(dx, dy);
    if (td > SPEED.arriveRadius) {
      ax = dx;
      ay = dy;
      arrive = Math.min(1, (td - SPEED.arriveRadius) / SPEED.arriveFalloff);
    } else {
      arrive = 0;
    }
  }
  return { ax, ay, arrive };
}
