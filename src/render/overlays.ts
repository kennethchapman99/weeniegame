/**
 * render/overlays.ts — title, interstitial, and end screens, drawn in world space, plus tap
 * hit-testing for the title dog-picker and the end "play again" button. Ported in spirit from
 * the prototype's DOM overlays; rendered on-canvas so input stays in one coordinate space.
 */

import type { GameState } from '../state/gameState.js';
import type { DogId } from '../state/dog.js';
import type { Point } from '../core/math.js';
import { drawDog } from './dog.js';
import { makeDog } from '../state/dog.js';
import { WORLD } from '../config/balance.js';
import { cap } from '../state/gameState.js';
import { rounded } from '../core/math.js';

type G = CanvasRenderingContext2D;
const W = WORLD.w;
const H = WORLD.h;

const PICK = { y: 320, dx: 150, r: 70 };
const PLAY = { x: W / 2 - 90, y: 470, w: 180, h: 54 };

function scrim(g: G, a = 0.72): void {
  g.fillStyle = `rgba(18,14,10,${a})`;
  g.fillRect(0, 0, W, H);
}

export function drawTitle(g: G, s: GameState): void {
  scrim(g, 0.82);
  g.textAlign = 'center';
  g.fillStyle = '#f6e6c8';
  g.font = "900 58px Georgia, 'Times New Roman', serif";
  g.fillText('Cheddar & Cocoa', W / 2, 180);
  g.fillStyle = '#cfe0ea';
  g.font = '600 18px -apple-system, sans-serif';
  g.fillText('Two dachshunds. One couch. Pick your dog.', W / 2, 220);

  drawPortrait(g, 'cheddar', W / 2 - PICK.dx, PICK.y, s.playerId === 'cheddar', s.elapsedMs);
  drawPortrait(g, 'cocoa', W / 2 + PICK.dx, PICK.y, s.playerId === 'cocoa', s.elapsedMs);

  // play button
  g.fillStyle = '#f4d3a4';
  rounded(g, PLAY.x, PLAY.y, PLAY.w, PLAY.h, 14);
  g.fill();
  g.fillStyle = '#3a2c20';
  g.font = '800 22px -apple-system, sans-serif';
  g.fillText('PLAY', W / 2, PLAY.y + 35);
}

function drawPortrait(g: G, id: DogId, x: number, y: number, selected: boolean, t: number): void {
  g.save();
  g.beginPath();
  g.arc(x, y, PICK.r, 0, 7);
  g.fillStyle = selected ? 'rgba(244,211,164,.25)' : 'rgba(255,255,255,.06)';
  g.fill();
  if (selected) {
    g.strokeStyle = '#f4d3a4';
    g.lineWidth = 4;
    g.stroke();
  }
  // mini dog
  const d = makeDog(id, x, y + 6, id === 'cheddar' ? 3.2 : 6.1);
  d.face = id === 'cheddar' ? 1 : -1;
  g.save();
  g.translate(x, y);
  g.scale(0.8, 0.8);
  g.translate(-x, -y);
  drawDog(g, d, t);
  g.restore();
  g.fillStyle = '#f6e6c8';
  g.textAlign = 'center';
  g.font = '800 14px -apple-system, sans-serif';
  g.fillText(cap(id), x, y + PICK.r + 22);
  g.restore();
}

export function drawInterstitial(g: G, s: GameState): void {
  scrim(g, 0.6);
  g.textAlign = 'center';
  g.fillStyle = '#f6e6c8';
  g.font = "900 46px Georgia, 'Times New Roman', serif";
  g.fillText(sceneName(s.sceneKey), W / 2, H / 2 - 16);
  g.fillStyle = '#cfe0ea';
  g.font = '600 18px -apple-system, sans-serif';
  g.fillText(sceneSub(s.sceneKey), W / 2, H / 2 + 24);
}

export function drawEnd(g: G, s: GameState): void {
  scrim(g, 0.82);
  const a = s.dogs.cheddar.score;
  const b = s.dogs.cocoa.score;
  g.textAlign = 'center';
  g.fillStyle = '#f6e6c8';
  g.font = "900 52px Georgia, 'Times New Roman', serif";
  g.fillText('Final Score', W / 2, 150);
  g.font = '800 30px -apple-system, sans-serif';
  g.fillStyle = '#f4d3a4';
  g.fillText(`Cheddar ${a}   ·   Cocoa ${b}`, W / 2, 220);

  let line: string;
  if (a === b) line = 'A tie. They curl up in the same spot anyway. 💕';
  else {
    const wid = a > b ? 'cheddar' : 'cocoa';
    const st = s.steals[wid];
    line = `${cap(wid)} wins${st > 0 ? ` — and stole the spot ${st} time${st > 1 ? 's' : ''}` : ''}! 🏆`;
  }
  g.fillStyle = '#cfe0ea';
  g.font = "700 22px Georgia, 'Times New Roman', serif";
  g.fillText(line, W / 2, 270);

  g.fillStyle = '#f4d3a4';
  rounded(g, PLAY.x, PLAY.y, PLAY.w, PLAY.h, 14);
  g.fill();
  g.fillStyle = '#3a2c20';
  g.font = '800 20px -apple-system, sans-serif';
  g.fillText('PLAY AGAIN', W / 2, PLAY.y + 35);
}

/** Title-screen tap: returns the picked dog (and whether PLAY was pressed). */
export function titleHit(p: Point): { pick?: DogId; play: boolean } {
  if (Math.hypot(p.x - (W / 2 - PICK.dx), p.y - PICK.y) < PICK.r) return { pick: 'cheddar', play: false };
  if (Math.hypot(p.x - (W / 2 + PICK.dx), p.y - PICK.y) < PICK.r) return { pick: 'cocoa', play: false };
  if (inRect(p, PLAY)) return { play: true };
  return { play: false };
}

/** End-screen tap: true if PLAY AGAIN pressed. */
export function endHit(p: Point): boolean {
  return inRect(p, PLAY);
}

function inRect(p: Point, r: { x: number; y: number; w: number; h: number }): boolean {
  return p.x >= r.x && p.x <= r.x + r.w && p.y >= r.y && p.y <= r.y + r.h;
}

function sceneName(key: string): string {
  if (key === 'pool') return 'The Pool';
  if (key === 'house') return 'The House';
  return 'The Backyard';
}
function sceneSub(key: string): string {
  if (key === 'pool') return 'Round 2 of 3 — floaters only. Water is SLOW.';
  if (key === 'house') return 'Round 3 of 3 — race the halls. Steal the couch.';
  return 'Round 1 of 3 — grab toys, hold the spot. Stick together!';
}
