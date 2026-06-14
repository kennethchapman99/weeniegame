/**
 * systems/predators.ts — backyard coyote + eagle FSMs (the co-op centerpiece). Ported from the
 * prototype's spawnPredator/unitedFront/scareOff/updatePredator/addScorePenalty/drawPredator.
 *
 * Survival is cooperative: huddle with your sibling (united front) to bark predators off; a lone
 * dog gets targeted, charged/dived, and can be grabbed and carried off (−1). Dodge by jumping at
 * the strike, zooming, or being belly-rub immune. Rescue a grabbed sibling by getting close.
 *
 * Backyard ONLY — never spawns in pool/house; reset on scene change (beginScene).
 * The bark-off is intentionally over-the-top (owner ask): see comicBark().
 */

import type { GameState, Predator } from '../state/gameState.js';
import { cap, playSound } from '../state/gameState.js';
import type { Dog } from '../state/dog.js';
import { busy, jumpHeight } from '../state/dog.js';
import { PREDATOR, JUMP, WORLD } from '../config/balance.js';
import { popup } from './particles.js';
import { rounded } from '../core/math.js';

type G = CanvasRenderingContext2D;
const W = WORLD.w;
const dist = (ax: number, ay: number, bx: number, by: number): number => Math.hypot(ax - bx, ay - by);
const BARKS = ['WOOF!', 'BARK!', 'BORK!', 'WUF!', 'GRRBARK!', 'YAP!'];

export function spawnPredator(s: GameState): void {
  const rng = s.rng;
  const kind = rng.next() < 0.5 ? 'coyote' : 'eagle';
  const target = rng.next() < 0.5 ? s.dogs.cheddar : s.dogs.cocoa;
  const base: Predator = {
    kind,
    x: 0,
    y: 0,
    state: 'enter',
    t: 0,
    targetId: target.id,
    grabId: null,
    warn: 0,
    seed: rng.range(0, 9),
    face: 1,
    cx: 0,
    cy: 0,
    ang: 0,
    carry: 0,
    dx: target.x,
    dy: target.y,
  };
  if (kind === 'coyote') {
    const fromLeft = rng.next() < 0.5;
    s.predator = {
      ...base,
      x: fromLeft ? -40 : W + 40,
      y: rng.range(250, 520),
      state: 'enter',
      warn: PREDATOR.coyote.warn,
      face: fromLeft ? 1 : -1,
    };
    s.toast = '🐺 A coyote slinks in! Stick together!';
    playSound(s, 'bark');
  } else {
    s.predator = {
      ...base,
      x: target.x,
      y: -60,
      state: 'circle',
      warn: PREDATOR.eagle.warn,
      cx: rng.range(300, 660),
      cy: rng.range(260, 420),
    };
    s.toast = '🦅 An eagle circles overhead! Don’t get caught alone!';
    playSound(s, 'screech');
  }
}

/** Both dogs close & free = a defensive stance that repels an un-committed predator. */
export function unitedFront(s: GameState): boolean {
  const a = s.dogs.cheddar;
  const b = s.dogs.cocoa;
  return !s.carriedDog && dist(a.x, a.y, b.x, b.y) < PREDATOR.unitedFrontRange && !busy(a) && !busy(b);
}

/** The HILARIOUS bark-off: both dogs lunge and let rip; the predator flees. */
function comicBark(s: GameState, d: Dog, towardX: number, towardY: number): void {
  d.barkT = 0.55;
  d.face = towardX > d.x ? 1 : -1;
  const mouthX = d.x + d.face * 46;
  const mouthY = d.y - 18;
  // big comic speech burst
  s.popups.push({
    x: mouthX + d.face * 14,
    y: mouthY - 8,
    text: BARKS[(s.rng.next() * BARKS.length) | 0]!,
    col: ['#fff', '#ffe24a'][(s.rng.next() * 2) | 0]!,
    life: 1,
    burst: true,
    rot: s.rng.range(-0.25, 0.25),
  });
  // spittle specks flying toward the predator
  for (let i = 0; i < 7; i++) {
    const a = Math.atan2(towardY - mouthY, towardX - mouthX) + s.rng.range(-0.5, 0.5);
    const spd = s.rng.range(2, 5);
    s.particles.push({
      x: mouthX,
      y: mouthY,
      vx: Math.cos(a) * spd,
      vy: Math.sin(a) * spd - 0.5,
      life: s.rng.range(0.4, 0.8),
      size: s.rng.range(1.5, 3),
      col: 'rgba(230,245,255,.9)',
    });
  }
}

export function scareOff(s: GameState, px: number, py: number): void {
  if (!s.predator) return;
  s.predator.state = 'flee';
  s.predator.t = 0;
  comicBark(s, s.dogs.cheddar, px, py);
  comicBark(s, s.dogs.cocoa, px, py);
  // a couple of extra woofs right in the predator's face
  for (let i = 0; i < 3; i++) {
    s.popups.push({
      x: px + s.rng.range(-30, 30),
      y: py - s.rng.range(10, 50),
      text: BARKS[(s.rng.next() * BARKS.length) | 0]!,
      col: ['#fff', '#ffe24a'][i % 2]!,
      life: 1,
      burst: true,
      rot: s.rng.range(-0.3, 0.3),
    });
  }
  playSound(s, 'bark');
  s.toast = '💪 Together they scared it off!';
}

function addScorePenalty(s: GameState, d: Dog): void {
  if (d.score > 0) {
    d.score = Math.max(0, d.score - 1);
    popup(s, d.x, d.y - 30, '-1 😔', '#ff9d9d');
  }
}

function grab(d: Dog, stun: number): void {
  d.mode = 'stunned';
  d.stunT = stun;
}

export function updatePredator(s: GameState, dt: number): void {
  if (s.sceneKey !== 'yard') {
    s.predator = null;
    return;
  }
  if (!s.predator) {
    s.predatorTimer -= dt;
    if (s.predatorTimer <= 0 && s.timeLeft > PREDATOR.minTimeLeft) spawnPredator(s);
    return;
  }
  const pr = s.predator;
  const tg = s.dogs[pr.targetId];
  const a = s.dogs.cheddar;
  const b = s.dogs.cocoa;
  pr.t += dt;

  // united front repels any un-committed predator
  if (
    unitedFront(s) &&
    pr.state !== 'grab' &&
    pr.state !== 'flee' &&
    pr.state !== 'carry' &&
    dist(pr.x, pr.y, (a.x + b.x) / 2, (a.y + b.y) / 2) < PREDATOR.scareRange
  ) {
    scareOff(s, pr.x, pr.y);
  }

  if (pr.kind === 'coyote') {
    const C = PREDATOR.coyote;
    if (pr.state === 'enter') {
      pr.warn -= dt;
      const dx = tg.x - pr.x;
      const dy = tg.y - pr.y;
      const m = Math.hypot(dx, dy) || 1;
      pr.x += (dx / m) * C.enterSpeed * dt * 60;
      pr.y += (dy / m) * C.enterSpeed * dt * 60;
      pr.face = dx > 0 ? 1 : -1;
      if (pr.warn <= 0) {
        pr.state = 'charge';
        pr.t = 0;
        playSound(s, 'bark');
      }
    } else if (pr.state === 'charge') {
      const dx = tg.x - pr.x;
      const dy = tg.y - pr.y;
      const m = Math.hypot(dx, dy) || 1;
      pr.x += (dx / m) * C.chargeSpeed * dt * 60;
      pr.y += (dy / m) * C.chargeSpeed * dt * 60;
      pr.face = dx > 0 ? 1 : -1;
      if (m < C.grabRange) {
        if (didDodge(tg)) {
          pr.state = 'flee';
          pr.t = 0;
          popup(s, tg.x, tg.y - 50, 'dodged!', '#9effa0');
          playSound(s, 'yip');
        } else {
          pr.state = 'grab';
          pr.grabId = tg.id;
          pr.t = 0;
          grab(tg, C.grabStun);
          s.carriedDog = tg.id;
          popup(s, tg.x, tg.y - 50, `${cap(tg.id)} caught!`, '#ff7a7a');
          playSound(s, 'bark');
        }
      }
      if (pr.t > C.missAfter) {
        pr.state = 'flee';
        pr.t = 0;
      }
    } else if (pr.state === 'grab') {
      const g = s.dogs[pr.grabId!]!;
      pr.x += pr.face * C.dragSpeed * dt * 60;
      g.x = pr.x;
      g.y = pr.y + 4;
      grab(g, C.dragStun);
      const other = g.id === 'cheddar' ? b : a;
      if (!busy(other) && dist(other.x, other.y, g.x, g.y) < PREDATOR.rescueRange) {
        scareOff(s, pr.x, pr.y);
        g.stunT = PREDATOR.rescueStun;
        s.carriedDog = null;
        popup(s, pr.x, pr.y - 40, 'rescued!', '#9effa0');
      }
      if (pr.t > C.dragTime) {
        g.stunT = C.dropStun;
        s.carriedDog = null;
        addScorePenalty(s, g);
        pr.state = 'flee';
        pr.t = 0;
      }
    } else if (pr.state === 'flee') {
      pr.x += pr.face * C.fleeSpeed * dt * 60;
      pr.y += 2 * dt * 60;
      if (pr.x < -80 || pr.x > W + 80) {
        s.predator = null;
        s.predatorTimer = s.rng.range(PREDATOR.respawn[0], PREDATOR.respawn[1]);
      }
    }
  } else {
    const E = PREDATOR.eagle;
    if (pr.state === 'circle') {
      pr.warn -= dt;
      pr.ang += dt * E.orbitSpeed;
      pr.cx += (tg.x - pr.cx) * 0.02;
      pr.cy += (tg.y - 120 - pr.cy) * 0.02;
      pr.x = pr.cx + Math.cos(pr.ang) * E.orbitRX;
      pr.y = pr.cy + Math.sin(pr.ang) * E.orbitRY;
      if (pr.warn <= 0) {
        pr.state = 'dive';
        pr.t = 0;
        pr.dx = tg.x;
        pr.dy = tg.y;
        playSound(s, 'screech');
      }
    } else if (pr.state === 'dive') {
      const dx = tg.x - pr.x;
      const dy = tg.y - pr.y;
      const m = Math.hypot(dx, dy) || 1;
      pr.x += (dx / m) * E.diveSpeed * dt * 60;
      pr.y += (dy / m) * E.diveSpeed * dt * 60;
      if (m < E.grabRange) {
        if (didDodge(tg) || unitedFront(s)) {
          pr.state = 'climb';
          pr.t = 0;
          popup(s, tg.x, tg.y - 50, 'dodged!', '#9effa0');
          playSound(s, 'yip');
        } else {
          pr.state = 'carry';
          pr.grabId = tg.id;
          pr.carry = 0;
          s.carriedDog = tg.id;
          grab(tg, E.grabStun);
          popup(s, tg.x, tg.y - 50, `${cap(tg.id)} grabbed!`, '#ff7a7a');
          playSound(s, 'screech');
        }
      }
      if (pr.t > E.diveWindow) {
        pr.state = 'climb';
        pr.t = 0;
      }
    } else if (pr.state === 'carry') {
      const g = s.dogs[pr.grabId!]!;
      pr.carry += dt;
      pr.y -= E.liftSpeed * dt;
      pr.x += (pr.x < W / 2 ? -1 : 1) * E.driftSpeed * dt;
      g.x = pr.x;
      g.y = pr.y + 24;
      grab(g, E.carryStun);
      const other = g.id === 'cheddar' ? b : a;
      if (!busy(other) && dist(other.x, other.y, g.x, g.y) < PREDATOR.rescueRange) {
        scareOff(s, pr.x, pr.y);
        g.y = Math.min(520, g.y + 40);
        g.stunT = E.dropStun;
        s.carriedDog = null;
        popup(s, pr.x, pr.y - 30, 'rescued!', '#9effa0');
      }
      if (pr.carry > E.carryTime) {
        g.stunT = E.dropStun;
        s.carriedDog = null;
        addScorePenalty(s, g);
        g.y = Math.min(520, g.y + 60);
        pr.state = 'climb';
        pr.t = 0;
      }
    } else if (pr.state === 'climb' || pr.state === 'flee') {
      pr.y -= E.climbSpeed * dt * 60;
      pr.x += (pr.x < W / 2 ? -3 : 3) * dt * 60;
      if (pr.y < -80) {
        s.predator = null;
        s.predatorTimer = s.rng.range(PREDATOR.respawn[0], PREDATOR.respawn[1]);
      }
    }
  }
}

function didDodge(tg: Dog): boolean {
  return jumpHeight(tg, JUMP.duration) > PREDATOR.dodgeJumpHeight || tg.zoom > 0 || tg.immune > 0;
}

export function drawPredator(g: G, s: GameState): void {
  const pr = s.predator;
  if (!pr) return;
  const T = s.elapsedMs;
  if (pr.kind === 'coyote') drawCoyote(g, pr, T, s.dogs[pr.targetId]);
  else drawEagle(g, pr, T, s.dogs[pr.targetId]);
}

function drawCoyote(g: G, pr: Predator, T: number, target: Dog): void {
  g.save();
  g.translate(pr.x, pr.y + 22);
  g.scale(1, 0.3);
  g.fillStyle = 'rgba(20,14,8,.3)';
  g.beginPath();
  g.arc(0, 0, 40, 0, 7);
  g.fill();
  g.restore();
  g.save();
  g.translate(pr.x, pr.y);
  g.scale(pr.face, 1);
  const run = Math.sin(T * 0.03 + pr.seed) * (pr.state === 'charge' ? 1 : 0.4);
  g.strokeStyle = '#8a6b48';
  g.lineWidth = 10;
  g.lineCap = 'round';
  g.beginPath();
  g.moveTo(-30, -4);
  g.quadraticCurveTo(-46, -2, -52, 8);
  g.stroke();
  g.fillStyle = '#3a2a1c';
  g.beginPath();
  g.arc(-52, 8, 5, 0, 7);
  g.fill();
  const bg = g.createLinearGradient(0, -18, 0, 16);
  bg.addColorStop(0, '#a8835c');
  bg.addColorStop(1, '#7d6044');
  g.fillStyle = bg;
  rounded(g, -32, -14, 60, 30, 14);
  g.fill();
  g.strokeStyle = '#6e5238';
  g.lineWidth = 5;
  for (const lx of [-20, -8, 12, 22]) {
    g.beginPath();
    g.moveTo(lx, 10);
    g.lineTo(lx + run * 4, 24);
    g.stroke();
  }
  g.save();
  g.translate(28, -8);
  g.fillStyle = '#9c7a54';
  g.beginPath();
  g.moveTo(-6, -8);
  g.quadraticCurveTo(20, -10, 26, 2);
  g.quadraticCurveTo(20, 10, -6, 10);
  g.closePath();
  g.fill();
  g.beginPath();
  g.moveTo(-8, -8);
  g.lineTo(-14, -20);
  g.lineTo(-2, -12);
  g.closePath();
  g.fill();
  g.beginPath();
  g.moveTo(2, -9);
  g.lineTo(-2, -22);
  g.lineTo(8, -11);
  g.closePath();
  g.fill();
  g.fillStyle = '#2a1c10';
  g.beginPath();
  g.arc(24, 2, 2.6, 0, 7);
  g.fill();
  g.fillStyle = pr.state === 'charge' ? '#ffd23a' : '#caa24a';
  g.beginPath();
  g.arc(8, -2, 2.4, 0, 7);
  g.fill();
  g.restore();
  g.restore();
  if (pr.state === 'enter' || pr.state === 'charge') reticle(g, target, T);
}

function drawEagle(g: G, pr: Predator, T: number, target: Dog): void {
  const high = pr.state === 'circle' || pr.state === 'climb' || pr.state === 'carry';
  if (pr.state !== 'carry') {
    g.save();
    g.translate(pr.x, Math.min(540, pr.dy || target.y));
    g.scale(1, 0.3);
    g.globalAlpha = high ? 0.18 : 0.34;
    g.fillStyle = 'rgba(20,14,8,1)';
    g.beginPath();
    g.arc(0, 0, 34, 0, 7);
    g.fill();
    g.restore();
  }
  g.save();
  g.translate(pr.x, pr.y);
  const flap = Math.sin(T * 0.02 + pr.seed) * 0.5;
  g.fillStyle = '#5a4632';
  g.beginPath();
  g.moveTo(0, 0);
  g.quadraticCurveTo(-44, -20 - flap * 30, -58, 6);
  g.quadraticCurveTo(-40, 6, 0, 8);
  g.closePath();
  g.fill();
  g.beginPath();
  g.moveTo(0, 0);
  g.quadraticCurveTo(44, -20 - flap * 30, 58, 6);
  g.quadraticCurveTo(40, 6, 0, 8);
  g.closePath();
  g.fill();
  g.fillStyle = '#4a3826';
  rounded(g, -9, -8, 18, 30, 8);
  g.fill();
  g.fillStyle = '#f3efe6';
  g.beginPath();
  g.arc(0, -12, 8, 0, 7);
  g.fill();
  g.fillStyle = '#e8a83a';
  g.beginPath();
  g.moveTo(0, -8);
  g.lineTo(6, -4);
  g.lineTo(0, -2);
  g.closePath();
  g.fill();
  g.fillStyle = '#222';
  g.beginPath();
  g.arc(3, -13, 1.5, 0, 7);
  g.fill();
  if (pr.state === 'dive' || pr.state === 'carry') {
    g.strokeStyle = '#e8a83a';
    g.lineWidth = 2.5;
    g.lineCap = 'round';
    for (const tx of [-5, 0, 5]) {
      g.beginPath();
      g.moveTo(tx, 20);
      g.lineTo(tx, 28);
      g.stroke();
    }
  }
  g.restore();
  if (pr.state === 'circle' || pr.state === 'dive') reticle(g, target, T);
}

function reticle(g: G, target: Dog, T: number): void {
  g.strokeStyle = 'rgba(255,90,90,.7)';
  g.lineWidth = 2;
  g.beginPath();
  g.arc(target.x, target.y, 30 + Math.sin(T * 0.02) * 4, 0, 7);
  g.stroke();
}
