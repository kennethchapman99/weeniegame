/**
 * render/overlays.ts — title, interstitial, and end screens, drawn in world space, plus tap
 * hit-testing for the title dog-picker and the end "play again" button. Ported in spirit from
 * the prototype's DOM overlays; rendered on-canvas so input stays in one coordinate space.
 */

import type { GameState, Partner, GameMode } from '../state/gameState.js';
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

const PICK = { y: 316, dx: 150, r: 64 };
const PLAY = { x: W / 2 - 90, y: 500, w: 180, h: 52 };
// Game-mode segmented toggle: Versus rounds vs Co-op missions (M12).
const GMODE = { y: 196, w: 150, h: 32, gap: 12 };
const GMODE_VS = { x: W / 2 - GMODE.w - GMODE.gap / 2, y: GMODE.y, w: GMODE.w, h: GMODE.h };
const GMODE_COOP = { x: W / 2 + GMODE.gap / 2, y: GMODE.y, w: GMODE.w, h: GMODE.h };
// 1P / 2P segmented toggle (couch co-op lobby, M10).
const MODE = { y: 438, w: 156, h: 38, gap: 14 };
const MODE_AI = { x: W / 2 - MODE.w - MODE.gap / 2, y: MODE.y, w: MODE.w, h: MODE.h };
const MODE_HUMAN = { x: W / 2 + MODE.gap / 2, y: MODE.y, w: MODE.w, h: MODE.h };
// mute toggle (top-right corner of the title)
const MUTE = { x: W - 46, y: 40, r: 22 };

function scrim(g: G, a = 0.72): void {
  g.fillStyle = `rgba(18,14,10,${a})`;
  g.fillRect(0, 0, W, H);
}

export function drawTitle(g: G, s: GameState, padCount = 0, muted = false): void {
  scrim(g, 0.82);

  // mute toggle (top-right)
  g.save();
  g.fillStyle = 'rgba(255,255,255,.10)';
  g.beginPath();
  g.arc(MUTE.x, MUTE.y, MUTE.r, 0, 7);
  g.fill();
  g.fillStyle = '#f6e6c8';
  g.textAlign = 'center';
  g.font = '20px -apple-system, sans-serif';
  g.fillText(muted ? '🔇' : '🔊', MUTE.x, MUTE.y + 7);
  g.restore();
  g.textAlign = 'center';
  g.fillStyle = '#f6e6c8';
  g.font = "900 56px Georgia, 'Times New Roman', serif";
  g.fillText('Cheddar & Cocoa', W / 2, 142);
  g.fillStyle = '#cfe0ea';
  g.font = '600 17px -apple-system, sans-serif';
  g.fillText('Two dachshunds. One couch.', W / 2, 172);

  // game mode: competitive rounds vs cooperative missions
  modeButton(g, GMODE_VS, 'VERSUS', s.mode === 'versus');
  modeButton(g, GMODE_COOP, 'CO-OP', s.mode === 'coop');

  const twoP = s.partner === 'human';
  // P1 picks a dog; the other dog goes to P2 (co-op) or the CPU (solo).
  const cheddarRole = s.playerId === 'cheddar' ? 'P1' : twoP ? 'P2' : 'CPU';
  const cocoaRole = s.playerId === 'cocoa' ? 'P1' : twoP ? 'P2' : 'CPU';
  drawPortrait(g, 'cheddar', W / 2 - PICK.dx, PICK.y, s.playerId === 'cheddar', s.elapsedMs, cheddarRole);
  drawPortrait(g, 'cocoa', W / 2 + PICK.dx, PICK.y, s.playerId === 'cocoa', s.elapsedMs, cocoaRole);

  // 1P / 2P toggle
  modeButton(g, MODE_AI, '1 PLAYER', !twoP);
  modeButton(g, MODE_HUMAN, '2 PLAYERS', twoP);

  // press-to-join hint when a second controller is plugged in but P2 hasn't joined yet
  if (padCount >= 2 && !twoP) {
    g.fillStyle = '#9effa0';
    g.font = '700 13px -apple-system, sans-serif';
    g.fillText('Controller 2 ready — press Ⓐ to join', W / 2, 580);
  }

  // play button
  g.fillStyle = '#f4d3a4';
  rounded(g, PLAY.x, PLAY.y, PLAY.w, PLAY.h, 14);
  g.fill();
  g.fillStyle = '#3a2c20';
  g.font = '800 22px -apple-system, sans-serif';
  g.fillText('PLAY', W / 2, PLAY.y + 35);
}

function modeButton(g: G, r: { x: number; y: number; w: number; h: number }, label: string, on: boolean): void {
  g.fillStyle = on ? '#f4d3a4' : 'rgba(255,255,255,.08)';
  rounded(g, r.x, r.y, r.w, r.h, 12);
  g.fill();
  if (on) {
    g.strokeStyle = '#f4d3a4';
    g.lineWidth = 2.5;
    rounded(g, r.x, r.y, r.w, r.h, 12);
    g.stroke();
  }
  g.fillStyle = on ? '#3a2c20' : '#cfe0ea';
  g.textAlign = 'center';
  g.font = '800 17px -apple-system, sans-serif';
  g.fillText(label, r.x + r.w / 2, r.y + r.h / 2 + 6);
}

function drawPortrait(
  g: G,
  id: DogId,
  x: number,
  y: number,
  selected: boolean,
  t: number,
  role: string,
): void {
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
  // role tag (P1 / P2 / CPU)
  const isCpu = role === 'CPU';
  g.fillStyle = isCpu ? '#cfe0ea' : '#9effa0';
  g.font = '800 12px -apple-system, sans-serif';
  g.fillText(role, x, y + PICK.r + 40);
  g.restore();
}

export function drawInterstitial(g: G, s: GameState): void {
  scrim(g, 0.6);
  g.textAlign = 'center';
  g.fillStyle = '#f6e6c8';
  g.font = "900 46px Georgia, 'Times New Roman', serif";
  const coop = s.mode === 'coop' && s.mission;
  g.fillText(coop ? s.mission!.title : sceneName(s.sceneKey), W / 2, H / 2 - 16);
  g.fillStyle = '#cfe0ea';
  g.font = '600 18px -apple-system, sans-serif';
  g.fillText(coop ? 'Co-op mission — work together! 🐾' : sceneSub(s.sceneKey), W / 2, H / 2 + 24);
}

export function drawEnd(g: G, s: GameState, coopHasNext = false): void {
  if (s.mode === 'coop' && s.mission) {
    drawMissionEnd(g, s, coopHasNext);
    return;
  }
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

/** Co-op mission result: SUCCESS (with stars) or FAILED, combined score, and replay. */
function drawMissionEnd(g: G, s: GameState, hasNext: boolean): void {
  const m = s.mission!;
  const win = m.status === 'success';
  scrim(g, 0.84);
  g.textAlign = 'center';
  g.fillStyle = '#f6e6c8';
  g.font = "900 46px Georgia, 'Times New Roman', serif";
  g.fillText(m.title, W / 2, 130);

  const finale = win && !hasNext; // cleared the last mission in the campaign
  g.font = '800 40px -apple-system, sans-serif';
  g.fillStyle = win ? '#9effa0' : '#ff9d7a';
  g.fillText(finale ? '🏆 CAMPAIGN COMPLETE!' : win ? 'MISSION COMPLETE!' : 'MISSION FAILED', W / 2, 200);

  if (win) {
    // stars
    g.font = '44px -apple-system, sans-serif';
    let stars = '';
    for (let i = 0; i < 3; i++) stars += i < m.stars ? '★' : '☆';
    g.fillStyle = '#ffd98c';
    g.fillText(stars, W / 2, 262);
    g.fillStyle = '#cfe0ea';
    g.font = '700 22px -apple-system, sans-serif';
    g.fillText(`Combined score: ${m.combinedScore}`, W / 2, 312);
    if (finale) {
      g.fillStyle = '#ffd98c';
      g.font = "700 20px Georgia, 'Times New Roman', serif";
      g.fillText('Cheddar & Cocoa did it — together. 💕', W / 2, 348);
    }
  } else {
    g.fillStyle = '#cfe0ea';
    g.font = "700 20px Georgia, 'Times New Roman', serif";
    g.fillText('The pups couldn’t crack it in time. Try again — together!', W / 2, 262);
  }

  g.fillStyle = '#f4d3a4';
  rounded(g, PLAY.x, PLAY.y, PLAY.w, PLAY.h, 14);
  g.fill();
  g.fillStyle = '#3a2c20';
  g.font = '800 20px -apple-system, sans-serif';
  const label = win ? (hasNext ? 'NEXT MISSION ▸' : 'PLAY AGAIN') : 'RETRY';
  g.fillText(label, W / 2, PLAY.y + 34);
}

/** Title-screen tap: returns the picked dog, the toggles hit, and whether PLAY was pressed. */
export function titleHit(p: Point): {
  pick?: DogId;
  mode?: Partner;
  gameMode?: GameMode;
  mute?: boolean;
  play: boolean;
} {
  if (Math.hypot(p.x - MUTE.x, p.y - MUTE.y) < MUTE.r + 6) return { mute: true, play: false };
  if (Math.hypot(p.x - (W / 2 - PICK.dx), p.y - PICK.y) < PICK.r) return { pick: 'cheddar', play: false };
  if (Math.hypot(p.x - (W / 2 + PICK.dx), p.y - PICK.y) < PICK.r) return { pick: 'cocoa', play: false };
  if (inRect(p, GMODE_VS)) return { gameMode: 'versus', play: false };
  if (inRect(p, GMODE_COOP)) return { gameMode: 'coop', play: false };
  if (inRect(p, MODE_AI)) return { mode: 'ai', play: false };
  if (inRect(p, MODE_HUMAN)) return { mode: 'human', play: false };
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
