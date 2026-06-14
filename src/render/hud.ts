/**
 * render/hud.ts — in-world HUD: score pills (per dog), round timer bar, scene name.
 * Drawn in world units (camera transform already applied). The prototype used DOM nodes;
 * we render on-canvas so the whole game UI lives in one coordinate space (webview-friendly).
 */

import type { GameState } from '../state/gameState.js';
import type { Point } from '../core/math.js';
import { DOGS } from '../config/dogs.js';
import { WORLD } from '../config/balance.js';
import { rounded } from '../core/math.js';
import { wrestleOnCooldown } from '../systems/wrestle.js';

type G = CanvasRenderingContext2D;
const W = WORLD.w;

/** On-screen action buttons (world coords, bottom-right). */
export const WRESTLE_BTN = { x: 888, y: 552, r: 40 } as const;
export const JUMP_BTN = { x: 792, y: 556, r: 32 } as const;

export function wrestleButtonHit(p: Point): boolean {
  return Math.hypot(p.x - WRESTLE_BTN.x, p.y - WRESTLE_BTN.y) < WRESTLE_BTN.r + 6;
}

export function jumpButtonHit(p: Point): boolean {
  return Math.hypot(p.x - JUMP_BTN.x, p.y - JUMP_BTN.y) < JUMP_BTN.r + 6;
}

export function drawJumpButton(g: G): void {
  g.save();
  g.fillStyle = '#a9d08a';
  g.beginPath();
  g.arc(JUMP_BTN.x, JUMP_BTN.y, JUMP_BTN.r, 0, 7);
  g.fill();
  g.strokeStyle = 'rgba(47,74,34,.5)';
  g.lineWidth = 2;
  g.stroke();
  g.fillStyle = '#2f4a22';
  g.textAlign = 'center';
  g.font = '18px -apple-system, sans-serif';
  g.fillText('⬆', JUMP_BTN.x, JUMP_BTN.y - 1);
  g.font = '800 8px -apple-system, sans-serif';
  g.fillText('JUMP', JUMP_BTN.x, JUMP_BTN.y + 14);
  g.restore();
}

export function drawWrestleButton(g: G, s: GameState): void {
  const cd = wrestleOnCooldown(s);
  g.save();
  g.globalAlpha = cd ? 0.45 : 1;
  g.fillStyle = '#f4c87a';
  g.beginPath();
  g.arc(WRESTLE_BTN.x, WRESTLE_BTN.y, WRESTLE_BTN.r, 0, 7);
  g.fill();
  g.strokeStyle = 'rgba(74,48,21,.5)';
  g.lineWidth = 2;
  g.stroke();
  g.fillStyle = '#4a3015';
  g.textAlign = 'center';
  g.font = '22px -apple-system, sans-serif';
  g.fillText('🤼', WRESTLE_BTN.x, WRESTLE_BTN.y - 2);
  g.font = '800 9px -apple-system, sans-serif';
  g.fillText('WRESTLE', WRESTLE_BTN.x, WRESTLE_BTN.y + 16);
  g.restore();
}

export function drawHUD(g: G, s: GameState): void {
  // timer bar across the very top
  const pct = Math.max(0, s.timeLeft / s.sceneTime);
  g.fillStyle = 'rgba(0,0,0,.25)';
  g.fillRect(0, 0, W, 6);
  g.fillStyle = pct < 0.2 ? '#e07a5f' : '#f4d3a4';
  g.fillRect(0, 0, W * pct, 6);

  // scene name centred
  g.font = "700 18px Georgia, 'Times New Roman', serif";
  g.textAlign = 'center';
  g.fillStyle = 'rgba(20,14,10,.85)';
  g.fillText(s.sceneKey ? sceneLabel(s) : '', W / 2, 30);

  scorePill(g, 16, 16, s.dogs.cheddar.score, DOGS.cheddar.dry.body[1], 'CHEDDAR', 'left', s.playerId === 'cheddar');
  scorePill(g, W - 16, 16, s.dogs.cocoa.score, DOGS.cocoa.dry.body[1], 'COCOA', 'right', s.playerId === 'cocoa');
}

function sceneLabel(s: GameState): string {
  const left = Math.max(0, Math.ceil(s.timeLeft));
  return `${nameFor(s.sceneKey)}  ·  ${left}s`;
}

function nameFor(key: string): string {
  if (key === 'pool') return 'The Pool';
  if (key === 'house') return 'The House';
  return 'The Backyard';
}

function scorePill(
  g: G,
  x: number,
  y: number,
  score: number,
  swatch: string,
  name: string,
  align: 'left' | 'right',
  isPlayer: boolean,
): void {
  const w = 116;
  const h = 34;
  const px = align === 'left' ? x : x - w;
  g.save();
  g.fillStyle = 'rgba(255,250,242,.92)';
  rounded(g, px, y, w, h, 10);
  g.fill();
  if (isPlayer) {
    g.strokeStyle = '#f4d3a4';
    g.lineWidth = 2.5;
    rounded(g, px, y, w, h, 10);
    g.stroke();
  }
  // swatch
  g.fillStyle = swatch;
  g.beginPath();
  g.arc(px + 18, y + h / 2, 10, 0, 7);
  g.fill();
  // name + score
  g.fillStyle = '#3a2c20';
  g.textAlign = 'left';
  g.font = '800 11px -apple-system, sans-serif';
  g.fillText(name, px + 34, y + 14);
  g.font = '800 18px -apple-system, sans-serif';
  g.fillText(String(score), px + 34, y + 29);
  g.restore();
}
