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
import { AudioBus } from './core/audio.js';
import { makeGameState, player, ai } from './state/gameState.js';
import { startGame, updateGame, currentScene, coopHasNext, advanceCoop, retryScene } from './state/sceneManager.js';
import { drawDog } from './render/dog.js';
import {
  drawHUD,
  drawWrestleButton,
  wrestleButtonHit,
  drawJumpButton,
  jumpButtonHit,
} from './render/hud.js';
import { drawParticles, drawPopups } from './systems/particles.js';
import { drawTitle, drawInterstitial, drawEnd, titleHit, endHit } from './render/overlays.js';
import { loadBest, saveBest } from './core/scores.js';
import type { Best } from './core/scores.js';
import { Backdrop } from './render/backdrop.js';

const canvas = document.getElementById('game') as HTMLCanvasElement | null;
if (!canvas) throw new Error('main: #game canvas not found');
const ctx = canvas.getContext('2d');
if (!ctx) throw new Error('main: 2d context unavailable');

const camera = new Camera(canvas, ctx);
const input = new Input();
input.attach(canvas, camera);
const backdrop = new Backdrop();
const audio = new AudioBus();
const state = makeGameState(makeRng(0xc0ffee));

// Dev-only inspection hook (stripped from production builds): lets the headless
// verification harness read/poke live game state without shipping a global.
if (import.meta.env.DEV) {
  (globalThis as unknown as { __game: typeof state }).__game = state;
}

addEventListener('resize', () => camera.fit());

function selectDog(id: 'cheddar' | 'cocoa'): void {
  state.playerId = id;
  state.aiId = id === 'cheddar' ? 'cocoa' : 'cheddar';
}

/** Activate the end-screen button (NEXT / RETRY / back to title). */
function endAction(): void {
  if (state.mode === 'coop' && state.mission) {
    const st = state.mission.status;
    if (st === 'success' && coopHasNext(state)) advanceCoop(state); // onward in the campaign
    else if (st === 'fail') retryScene(state); // retry the current mission
    else state.phase = 'title'; // campaign cleared
  } else {
    state.phase = 'title';
  }
}

// Discrete taps for title / end screens (movement drag is handled by Input).
canvas.addEventListener('pointerdown', (e) => {
  audio.resume(); // first user gesture unlocks Web Audio (autoplay policy)
  const p = camera.screenToWorld(e.clientX, e.clientY);
  if (state.phase === 'title') {
    const hit = titleHit(p);
    if (hit.pick) selectDog(hit.pick);
    if (hit.mode) state.partner = hit.mode;
    if (hit.gameMode) state.mode = hit.gameMode;
    if (hit.mute) audio.toggleMuted();
    if (hit.play) startGame(state);
  } else if (state.phase === 'play') {
    if (wrestleButtonHit(p)) {
      input.queueWrestle();
      input.touch = null; // a button tap shouldn't also set a move target
    } else if (jumpButtonHit(p)) {
      input.queueJump();
      input.touch = null;
    }
  } else if (state.phase === 'end') {
    if (endHit(p)) endAction();
  }
});

// Title focus row for controller navigation: 0=mode, 1=players, 2=dog, 3=PLAY.
let titleFocus = 3;

/** Drive the title overlay from P1's controller (couch/TV play with no pointer). */
function handleTitleNav(): void {
  const n = input.menuNav;
  if (n.down) titleFocus = (titleFocus + 1) % 4;
  if (n.up) titleFocus = (titleFocus + 3) % 4;
  const change = n.left || n.right || (n.confirm && titleFocus !== 3);
  if (change) {
    if (titleFocus === 0) state.mode = state.mode === 'versus' ? 'coop' : 'versus';
    else if (titleFocus === 1) state.partner = state.partner === 'ai' ? 'human' : 'ai';
    else if (titleFocus === 2) selectDog(state.playerId === 'cheddar' ? 'cocoa' : 'cheddar');
  }
  if (n.confirm && titleFocus === 3) {
    audio.resume();
    startGame(state);
  }
}

// best score/stars for the just-finished mission, recorded once when it ends (host-side, so
// the deterministic sim stays storage-free).
let endBest: Best | null = null;
let recorded = false;

function update(dt: number): void {
  input.poll(); // read both gamepads before computing intents / draining actions

  // Title: controller navigation + "press a button on pad 2 to join" two-player co-op.
  if (state.phase === 'title') {
    handleTitleNav();
    if (input.takeP2Join()) state.partner = 'human';
  } else if (state.phase === 'end' && input.menuNav.confirm) {
    endAction(); // confirm the end-screen button from a controller
  }

  const twoPlayer = state.partner === 'human';
  const p1 = input.p1Command(player(state), twoPlayer);
  const p2 = twoPlayer ? input.p2Command(ai(state)) : null;
  updateGame(state, p1.intent, p1.wrestle, p1.jump, dt, p2);

  // record a co-op mission result once, when it lands on the end screen
  if (state.phase === 'end' && state.mode === 'coop' && state.mission) {
    if (!recorded) {
      recorded = true;
      const m = state.mission;
      endBest =
        m.status === 'success'
          ? saveBest(m.key, m.combinedScore, m.stars)
          : loadBest(m.key);
    }
  } else {
    recorded = false;
    endBest = null;
  }
}

function render(): void {
  if (!ctx) return;
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  ctx.clearRect(0, 0, canvas!.width, canvas!.height);

  const def = currentScene(state);
  const bg = backdrop.get(def.bgKey(state), def.paint(state), camera.view);
  ctx.drawImage(bg, 0, 0);

  camera.applyTransform();

  if (state.phase === 'title') {
    drawTitle(ctx, state, input.padCount, audio.isMuted, input.gamepadActive ? titleFocus : -1);
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

  // draw the visible dogs (room-filtered for the house) back-to-front by y
  const dogs = def.visibleDogs(state).sort((a, b) => a.y - b.y);
  for (const d of dogs) drawDog(ctx, d, state.elapsedMs);

  drawParticles(ctx, state);
  drawPopups(ctx, state);

  drawHUD(ctx, state);
  // On-screen WRESTLE/JUMP are touch affordances — hide them when a controller is driving.
  if (state.phase === 'play' && !input.gamepadActive) {
    drawWrestleButton(ctx, state);
    drawJumpButton(ctx);
  }

  if (state.phase === 'inter') drawInterstitial(ctx, state);
  if (state.phase === 'end') drawEnd(ctx, state, coopHasNext(state), endBest);

  // drain queued sounds (host layer plays them; systems stay audio-free)
  if (state.sounds.length) {
    for (const id of state.sounds) audio.play(id);
    state.sounds.length = 0;
  }
}

startLoop({ update, render });
