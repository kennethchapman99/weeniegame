/**
 * main.ts — M2 bootstrap. A full single-round game: title → backyard → end.
 * Wires camera + loop + input + scene manager + renderers. Player drives one dog; the AI
 * sibling is static until M3. Scoring (toys + cuddle spot), HUD, particles and scene flow
 * are live.
 */

import { Camera } from './core/camera.js';
import { startLoop } from './core/loop.js';
import { Input } from './core/input.js';
import { makeRng } from './core/rng.js';
import { makeGameState, player } from './state/gameState.js';
import { startGame, updateGame, currentScene } from './state/sceneManager.js';
import { drawDog } from './render/dog.js';
import { drawHUD, drawWrestleButton, wrestleButtonHit } from './render/hud.js';
import { drawParticles, drawPopups } from './systems/particles.js';
import { drawTitle, drawInterstitial, drawEnd, titleHit, endHit } from './render/overlays.js';
import { Backdrop } from './render/backdrop.js';

const canvas = document.getElementById('game') as HTMLCanvasElement | null;
if (!canvas) throw new Error('main: #game canvas not found');
const ctx = canvas.getContext('2d');
if (!ctx) throw new Error('main: 2d context unavailable');

const camera = new Camera(canvas, ctx);
const input = new Input();
input.attach(canvas, camera);
const backdrop = new Backdrop();
const state = makeGameState(makeRng(0xc0ffee));

// Dev-only inspection hook (stripped from production builds): lets the headless
// verification harness read/poke live game state without shipping a global.
if (import.meta.env.DEV) {
  (globalThis as unknown as { __game: typeof state }).__game = state;
}

addEventListener('resize', () => camera.fit());

// Discrete taps for title / end screens (movement drag is handled by Input).
canvas.addEventListener('pointerdown', (e) => {
  const p = camera.screenToWorld(e.clientX, e.clientY);
  if (state.phase === 'title') {
    const hit = titleHit(p);
    if (hit.pick) {
      state.playerId = hit.pick;
      state.aiId = hit.pick === 'cheddar' ? 'cocoa' : 'cheddar';
    }
    if (hit.play) startGame(state);
  } else if (state.phase === 'play') {
    if (wrestleButtonHit(p)) {
      input.queueWrestle();
      input.touch = null; // a button tap shouldn't also set a move target
    }
  } else if (state.phase === 'end') {
    if (endHit(p)) {
      state.phase = 'title';
    }
  }
});

function update(dt: number): void {
  const intent = input.intentFor(player(state));
  const wrestle = input.consumeWrestle();
  updateGame(state, intent, wrestle, dt);
}

function render(): void {
  if (!ctx) return;
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  ctx.clearRect(0, 0, canvas!.width, canvas!.height);

  const sceneKey = state.sceneKey || 'yard';
  const def = currentScene(state);
  const bg = backdrop.get(sceneKey, def.painter, camera.view);
  ctx.drawImage(bg, 0, 0);

  camera.applyTransform();

  if (state.phase === 'title') {
    drawTitle(ctx, state);
    return;
  }

  // world: scene props, dogs, particles
  def.drawWorld(ctx, state);

  // touch target marker (during play)
  if (state.phase === 'play' && input.touch) {
    ctx.strokeStyle = 'rgba(255,255,255,.5)';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.arc(input.touch.x, input.touch.y, 12 + Math.sin(state.elapsedMs * 0.012) * 3, 0, 7);
    ctx.stroke();
  }

  // draw both dogs back-to-front by y
  const dogs = [state.dogs.cheddar, state.dogs.cocoa].sort((a, b) => a.y - b.y);
  for (const d of dogs) drawDog(ctx, d, state.elapsedMs);

  drawParticles(ctx, state);
  drawPopups(ctx, state);

  drawHUD(ctx, state);
  if (state.phase === 'play') drawWrestleButton(ctx, state);

  if (state.phase === 'inter') drawInterstitial(ctx, state);
  if (state.phase === 'end') drawEnd(ctx, state);
}

startLoop({ update, render });
