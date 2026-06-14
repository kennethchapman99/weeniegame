/**
 * main.ts — M1 bootstrap. A single controllable dog on the backyard:
 *   Camera (DPR letterbox) + fixed-timestep loop + input + movement + dog/yard renderers.
 *
 * No scene flow, scoring, AI or other dogs yet (M2/M3+). This proves the core loop,
 * movement feel (arrival easing, no on-spot jitter) and yard render parity.
 */

import { Camera } from './core/camera.js';
import { startLoop } from './core/loop.js';
import { Input } from './core/input.js';
import { makeDog } from './state/dog.js';
import { moveDog } from './systems/movement.js';
import { drawDog } from './render/dog.js';
import { Backdrop } from './render/backdrop.js';
import { paintYard } from './scenes/yard.js';

const canvas = document.getElementById('game') as HTMLCanvasElement | null;
if (!canvas) throw new Error('main: #game canvas not found');
const ctx = canvas.getContext('2d');
if (!ctx) throw new Error('main: 2d context unavailable');

const camera = new Camera(canvas, ctx);
const input = new Input();
input.attach(canvas, camera);
const backdrop = new Backdrop();

const player = makeDog('cheddar', 300, 400, 3.2);

addEventListener('resize', () => camera.fit());

let elapsedMs = 0;

function update(dt: number): void {
  elapsedMs += dt * 1000;
  const intent = input.intentFor(player);
  moveDog(player, intent.ax, intent.ay, dt, intent.arrive);
}

function render(): void {
  if (!ctx) return;
  // backdrop is pre-rendered at device resolution; blit it 1:1 then draw world-space.
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  ctx.clearRect(0, 0, canvas!.width, canvas!.height);
  const bg = backdrop.get('yard', paintYard, camera.view);
  ctx.drawImage(bg, 0, 0);

  camera.applyTransform();

  // touch target marker
  if (input.touch) {
    ctx.strokeStyle = 'rgba(255,255,255,.5)';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.arc(input.touch.x, input.touch.y, 12 + Math.sin(elapsedMs * 0.012) * 3, 0, 7);
    ctx.stroke();
  }

  drawDog(ctx, player, elapsedMs);
}

startLoop({ update, render });
