/**
 * core/input.ts — touch-drag (primary) + WASD/arrows → a steering Intent in world space.
 *
 * Ported from the prototype's pointer/keyboard handlers + the input block of update():
 *   - touch target is converted to WORLD units via the camera (the coordinate-space bug
 *     CLAUDE.md warns about — done once, here).
 *   - arrive easing: beyond arriveRadius, arrive ramps 0..1 over arriveFalloff; inside it,
 *     arrive = 0 so the dog eases to a hard stop with no on-spot jitter.
 */

import type { Point } from './math.js';
import type { Camera } from './camera.js';
import { SPEED, INPUT } from '../config/balance.js';
import { GamepadSource } from './gamepad.js';

export interface Intent {
  ax: number;
  ay: number;
  arrive: number;
}

export class Input {
  readonly keys: Record<string, boolean> = {};
  touch: Point | null = null;
  /** True while a gamepad is connected — lets the host hide the on-screen touch buttons. */
  gamepadActive = false;
  private readonly gamepad = new GamepadSource();
  private padIntent: Intent | null = null;
  private wrestleQueued = false;
  private jumpQueued = false;

  /**
   * Poll the gamepad once per fixed update step (call before intentFor / consume*).
   * Gamepad button edges queue the same actions as keys; its stick intent (when pushed past
   * the deadzone) takes precedence over touch/keyboard for that step.
   */
  poll(): void {
    const r = this.gamepad.poll(INPUT.gamepadDeadzone);
    this.gamepadActive = r.connected;
    this.padIntent = r.intent;
    if (r.wrestle) this.queueWrestle();
    if (r.jump) this.queueJump();
  }

  /** Queue a wrestle action (from a key edge or the on-screen WRESTLE button). */
  queueWrestle(): void {
    this.wrestleQueued = true;
  }

  /** Read-and-clear the queued wrestle action (one per press). */
  consumeWrestle(): boolean {
    const q = this.wrestleQueued;
    this.wrestleQueued = false;
    return q;
  }

  /** Queue a jump action (J key or the on-screen JUMP button). */
  queueJump(): void {
    this.jumpQueued = true;
  }

  /** Read-and-clear the queued jump action (one per press). */
  consumeJump(): boolean {
    const q = this.jumpQueued;
    this.jumpQueued = false;
    return q;
  }

  /** Attach DOM listeners. Returns a detach function. */
  attach(canvas: HTMLCanvasElement, camera: Camera): () => void {
    const onKeyDown = (e: KeyboardEvent): void => {
      const k = e.key.toLowerCase();
      this.keys[k] = true;
      if (k === ' ' || k === 'e') {
        e.preventDefault();
        this.queueWrestle();
      }
      if (k === 'j') {
        e.preventDefault();
        this.queueJump();
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

  /** Compute the steering intent for a dog at `pos` (the player). */
  intentFor(pos: Point): Intent {
    // A pushed stick wins over touch/keyboard for this step; otherwise fall back to them.
    if (this.padIntent) return this.padIntent;
    return computeIntent(this.keys, this.touch, pos);
  }
}

/** Pure intent computation — directly testable without DOM. */
export function computeIntent(
  keys: Record<string, boolean>,
  touch: Point | null,
  pos: Point,
): Intent {
  let ax = 0;
  let ay = 0;
  let arrive = 1;
  if (keys['arrowleft'] || keys['a']) ax -= 1;
  if (keys['arrowright'] || keys['d']) ax += 1;
  if (keys['arrowup'] || keys['w']) ay -= 1;
  if (keys['arrowdown'] || keys['s']) ay += 1;
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
