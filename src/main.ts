/**
 * main.ts — M0 bootstrap. Opens the DPR-scaled letterboxed canvas via Camera and draws
 * a single static frame in world units (no game loop yet — that arrives in M1).
 *
 * Proves the foundational transform: the play-bounds rectangle and centre marker land
 * correctly regardless of viewport size / devicePixelRatio.
 */

import { Camera } from './core/camera.js';
import { rounded } from './core/math.js';
import { WORLD, BOUNDS } from './config/balance.js';

const canvas = document.getElementById('game') as HTMLCanvasElement | null;
if (!canvas) throw new Error('main: #game canvas not found');
const ctx = canvas.getContext('2d');
if (!ctx) throw new Error('main: 2d context unavailable');

const camera = new Camera(canvas, ctx);

function renderStaticFrame(): void {
  if (!ctx) return;
  camera.applyTransform();
  ctx.clearRect(0, 0, WORLD.w, WORLD.h);

  // World backdrop
  ctx.fillStyle = '#1c1812';
  ctx.fillRect(0, 0, WORLD.w, WORLD.h);

  // Play bounds (x∈[50,910], y∈[215,555])
  ctx.strokeStyle = 'rgba(180,150,110,.6)';
  ctx.lineWidth = 2;
  rounded(ctx, BOUNDS.minX, BOUNDS.minY, BOUNDS.maxX - BOUNDS.minX, BOUNDS.maxY - BOUNDS.minY, 14);
  ctx.stroke();

  // Centre marker
  const cx = WORLD.w / 2;
  const cy = WORLD.h / 2;
  ctx.fillStyle = '#e3ab63';
  ctx.beginPath();
  ctx.arc(cx, cy, 10, 0, Math.PI * 2);
  ctx.fill();

  // Label
  ctx.fillStyle = '#cfe0ea';
  ctx.font = '20px Georgia, serif';
  ctx.textAlign = 'center';
  ctx.fillText('Cheddar & Cocoa — M0 scaffold', cx, BOUNDS.minY - 24);
}

window.addEventListener('resize', () => {
  camera.fit();
  renderStaticFrame();
});

renderStaticFrame();
