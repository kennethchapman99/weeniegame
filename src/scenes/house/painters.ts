/**
 * scenes/house/painters.ts — the ART layer for the house: one painter per room, registered by
 * room id. Pure drawing; reads only door anchors from the map data for the doorway glows. Ported
 * from the prototype's bgFoyer/bgFamily/bgRec (+ hardwood/stoneFireplace/doorGlow/stairVisual);
 * `rec` is re-skinned a touch brighter as the Solarium. Swap any of these to re-skin a room
 * without touching its collision/gameplay data (see docs/ROOM-SCHEMA.md).
 */

import type { Rng } from '../../core/rng.js';
import type { DoorDef } from '../mapdef.js';
import { WORLD } from '../../config/balance.js';
import { rounded } from '../../core/math.js';
import { HOUSE_MAP } from './rooms.js';

type G = CanvasRenderingContext2D;
export type RoomPainter = (g: G, rng: Rng) => void;

const W = WORLD.w;
const H = WORLD.h;

function hardwood(g: G, y0: number, c1: string, c2: string, rng: Rng): void {
  const fg = g.createLinearGradient(0, y0, 0, H);
  fg.addColorStop(0, c1);
  fg.addColorStop(1, c2);
  g.fillStyle = fg;
  g.fillRect(0, y0, W, H - y0);
  g.strokeStyle = 'rgba(80,50,25,.28)';
  g.lineWidth = 2;
  for (let y = y0 + 36; y < H; y += 46) {
    g.beginPath();
    g.moveTo(0, y);
    g.lineTo(W, y);
    g.stroke();
  }
  for (let i = 0; i < 18; i++) {
    const x = rng.range(0, W);
    const y = y0 + 36 + ((rng.next() * 7) | 0) * 46;
    g.beginPath();
    g.moveTo(x, y);
    g.lineTo(x, Math.min(H, y + 46));
    g.stroke();
  }
}

function stoneFireplace(g: G, x: number, y: number, w: number, h: number, rng: Rng): void {
  g.fillStyle = '#8d7a64';
  rounded(g, x, y - 90, w, h + 90, 8);
  g.fill();
  const cols = ['#a4907a', '#7d6a55', '#b3a08a', '#907d67', '#6e5d4a'];
  let sy = y - 84;
  while (sy < y + h - 14) {
    let sx = x + 6;
    while (sx < x + w - 8) {
      const sw = rng.range(22, 46);
      const shh = rng.range(16, 26);
      g.fillStyle = cols[(rng.next() * cols.length) | 0]!;
      rounded(g, sx, sy, Math.min(sw, x + w - 8 - sx), shh, 6);
      g.fill();
      sx += sw + 4;
    }
    sy += 24;
  }
  g.fillStyle = '#3d2c1c';
  rounded(g, x + 6, y + h - 78, w - 12, 10, 3);
  g.fill();
  g.fillStyle = '#1c1410';
  rounded(g, x + 18, y + h - 62, w - 36, 52, 6);
  g.fill();
  const fire = g.createRadialGradient(x + w / 2, y + h - 26, 4, x + w / 2, y + h - 26, 34);
  fire.addColorStop(0, '#ffd98c');
  fire.addColorStop(0.5, '#e8853a');
  fire.addColorStop(1, 'rgba(120,40,10,0)');
  g.fillStyle = fire;
  g.beginPath();
  g.arc(x + w / 2, y + h - 26, 34, 0, 7);
  g.fill();
}

function doorGlow(g: G, door: DoorDef): void {
  g.save();
  g.translate(door.cx, door.cy);
  g.scale(1, 0.55);
  const dg = g.createRadialGradient(0, 0, 6, 0, 0, 54);
  dg.addColorStop(0, 'rgba(255,232,170,.45)');
  dg.addColorStop(1, 'rgba(255,232,170,0)');
  g.fillStyle = dg;
  g.beginPath();
  g.arc(0, 0, 54, 0, 7);
  g.fill();
  g.restore();
  g.fillStyle = 'rgba(60,40,20,.55)';
  g.font = '800 12px -apple-system, sans-serif';
  g.textAlign = 'center';
  g.fillText('→ ' + door.label, door.cx, door.cy - 46);
}

function stairVisual(g: G, x: number, y: number): void {
  g.fillStyle = '#caa86f';
  for (let i = 0; i < 6; i++) {
    rounded(g, x + i * 4, y + i * 22, 118 - i * 8, 18, 4);
    g.fill();
  }
  g.fillStyle = '#8a5f33';
  for (let i = 0; i < 6; i++) g.fillRect(x + i * 4, y + i * 22 + 16, 118 - i * 8, 4);
  g.strokeStyle = '#3a2a1a';
  g.lineWidth = 4;
  g.lineCap = 'round';
  g.beginPath();
  g.moveTo(x + 116, y - 6);
  g.quadraticCurveTo(x + 96, y + 60, x + 76, y + 128);
  g.stroke();
  g.strokeStyle = '#2c2018';
  g.lineWidth = 2;
  for (let i = 0; i < 5; i++) {
    g.beginPath();
    g.moveTo(x + 112 - i * 8, y + 8 + i * 24);
    g.lineTo(x + 112 - i * 8, y + 26 + i * 24);
    g.stroke();
  }
}

function paintFoyer(g: G, rng: Rng): void {
  const wg = g.createLinearGradient(0, 0, 0, 250);
  wg.addColorStop(0, '#efe3c8');
  wg.addColorStop(1, '#e2d2b0');
  g.fillStyle = wg;
  g.fillRect(0, 0, W, 250);
  g.fillStyle = '#fff';
  g.fillRect(0, 0, W, 26);
  // double front doors + sidelights + transom
  g.fillStyle = '#f5efe2';
  rounded(g, 352, 36, 256, 196, 6);
  g.fill();
  g.fillStyle = '#b9c9c2';
  rounded(g, 366, 84, 52, 140, 4);
  g.fill();
  rounded(g, 542, 84, 52, 140, 4);
  g.fill();
  rounded(g, 366, 46, 228, 28, 4);
  g.fill();
  g.fillStyle = '#8fa39b';
  rounded(g, 426, 64, 50, 160, 4);
  g.fill();
  rounded(g, 484, 64, 50, 160, 4);
  g.fill();
  g.fillStyle = '#e8e2d2';
  g.fillRect(478, 64, 4, 160);
  g.fillStyle = '#caa15a';
  g.beginPath();
  g.arc(470, 150, 3.4, 0, 7);
  g.fill();
  g.beginPath();
  g.arc(490, 150, 3.4, 0, 7);
  g.fill();
  // slate landing steps
  g.fillStyle = '#5d6258';
  rounded(g, 372, 228, 216, 22, 4);
  g.fill();
  g.fillStyle = '#6e7468';
  rounded(g, 386, 246, 188, 18, 4);
  g.fill();
  // baseboard + hardwood
  g.fillStyle = '#f4efe3';
  g.fillRect(0, 250, W, 12);
  hardwood(g, 262, '#b07a44', '#8e5e30', rng);
  // round woven rug
  g.save();
  g.translate(480, 430);
  g.scale(1, 0.55);
  g.fillStyle = '#6e5a40';
  g.beginPath();
  g.arc(0, 0, 150, 0, 7);
  g.fill();
  g.fillStyle = '#8a7354';
  g.beginPath();
  g.arc(0, 0, 132, 0, 7);
  g.fill();
  g.strokeStyle = 'rgba(50,38,24,.35)';
  g.lineWidth = 3;
  for (let r = 24; r < 130; r += 18) {
    g.beginPath();
    g.arc(0, 0, r, 0, 7);
    g.stroke();
  }
  g.restore();
  stoneFireplace(g, 780, 228, 140, 128, rng);
  stairVisual(g, 46, 238);
  for (const d of HOUSE_MAP.rooms.foyer!.doors) doorGlow(g, d);
}

function paintFamily(g: G, rng: Rng): void {
  const wg = g.createLinearGradient(0, 0, 0, 250);
  wg.addColorStop(0, '#efe4c6');
  wg.addColorStop(1, '#e4d4ae');
  g.fillStyle = wg;
  g.fillRect(0, 0, W, 250);
  g.fillStyle = '#fff';
  g.fillRect(0, 0, W, 26);
  g.fillStyle = 'rgba(255,255,255,.7)';
  g.fillRect(0, 176, W, 7);
  // gallery frames
  for (const [fx, fy, fw, fh] of [
    [300, 60, 46, 62],
    [356, 76, 46, 46],
    [412, 58, 46, 64],
    [468, 78, 46, 44],
    [524, 62, 46, 60],
  ] as const) {
    g.fillStyle = '#2c241c';
    g.fillRect(fx, fy, fw, fh);
    g.fillStyle = '#ece4d2';
    g.fillRect(fx + 5, fy + 5, fw - 10, fh - 10);
  }
  // window mirror
  g.strokeStyle = '#5a4632';
  g.lineWidth = 5;
  g.strokeRect(640, 52, 150, 108);
  g.fillStyle = '#cfe0e8';
  g.fillRect(645, 57, 140, 98);
  g.strokeStyle = '#5a4632';
  g.lineWidth = 3;
  g.beginPath();
  g.moveTo(715, 57);
  g.lineTo(715, 155);
  g.moveTo(645, 106);
  g.lineTo(785, 106);
  g.stroke();
  // baseboard + hardwood + woven rug
  g.fillStyle = '#f4efe3';
  g.fillRect(0, 250, W, 12);
  hardwood(g, 262, '#ab763f', '#8a5a2c', rng);
  g.fillStyle = '#b9a888';
  rounded(g, 250, 330, 520, 240, 18);
  g.fill();
  g.fillStyle = '#a89878';
  rounded(g, 262, 342, 496, 216, 14);
  g.fill();
  // big beige couch (obstacle)
  g.fillStyle = '#c9b694';
  rounded(g, 330, 228, 330, 84, 16);
  g.fill();
  g.fillStyle = '#d8c7a6';
  for (let i = 0; i < 4; i++) {
    rounded(g, 340 + i * 80, 236, 72, 40, 10);
    g.fill();
  }
  // coffee table
  g.fillStyle = '#6b4a2a';
  rounded(g, 415, 368, 165, 78, 8);
  g.fill();
  g.fillStyle = '#7d5a36';
  rounded(g, 423, 374, 149, 30, 5);
  g.fill();
  // leather chair
  g.fillStyle = '#8a6a4e';
  rounded(g, 760, 402, 140, 96, 20);
  g.fill();
  g.fillStyle = '#a07d5c';
  rounded(g, 772, 412, 116, 46, 14);
  g.fill();
  // ★ THE DOG COUCH (top-left hallway nook)
  g.fillStyle = '#5e4630';
  rounded(g, 118, 236, 140, 86, 18);
  g.fill();
  g.fillStyle = '#7a5c3e';
  rounded(g, 128, 228, 54, 52, 12);
  g.fill();
  rounded(g, 194, 228, 54, 52, 12);
  g.fill();
  g.fillStyle = '#4a3826';
  rounded(g, 118, 300, 140, 24, 10);
  g.fill();
  g.fillStyle = '#c4564a';
  rounded(g, 138, 268, 100, 30, 12);
  g.fill();
  g.fillStyle = 'rgba(60,40,20,.6)';
  g.font = '800 11px -apple-system, sans-serif';
  g.textAlign = 'center';
  g.fillText('THE DOG COUCH', 188, 222);
  for (const d of HOUSE_MAP.rooms.family!.doors) doorGlow(g, d);
}

function paintSolarium(g: G, rng: Rng): void {
  // bright sunroom walls (re-skinned from the prototype's brick rec room)
  const wg = g.createLinearGradient(0, 0, 0, 250);
  wg.addColorStop(0, '#f3ead2');
  wg.addColorStop(1, '#e9dcbc');
  g.fillStyle = wg;
  g.fillRect(0, 0, W, 250);
  g.fillStyle = '#fff';
  g.fillRect(0, 0, W, 22);
  // a wall of glass — tall arched windows letting the sun in
  for (let wx = 60; wx < W - 60; wx += 150) {
    g.fillStyle = '#f6fbff';
    rounded(g, wx, 40, 110, 200, 8);
    g.fill();
    g.fillStyle = '#cfe6f2';
    rounded(g, wx + 10, 66, 40, 150, 6);
    g.fill();
    rounded(g, wx + 60, 66, 40, 150, 6);
    g.fill();
    g.beginPath();
    g.arc(wx + 30, 66, 20, Math.PI, 0);
    g.fill();
    g.beginPath();
    g.arc(wx + 80, 66, 20, Math.PI, 0);
    g.fill();
    g.strokeStyle = '#bcae8e';
    g.lineWidth = 3;
    g.strokeRect(wx, 40, 110, 200);
  }
  // warm sunlit tile floor
  g.fillStyle = '#fff';
  g.fillRect(0, 250, W, 10);
  const cf = g.createLinearGradient(0, 260, 0, H);
  cf.addColorStop(0, '#efe6cf');
  cf.addColorStop(1, '#dccfae');
  g.fillStyle = cf;
  g.fillRect(0, 260, W, H - 260);
  g.fillStyle = 'rgba(255,240,200,.10)';
  for (let i = 0; i < 5; i++) g.fillRect(0, 280 + i * 64, W, 30);
  // sectional + chaise
  g.fillStyle = '#9fb38a';
  rounded(g, 330, 244, 300, 78, 16);
  g.fill();
  g.fillStyle = '#b3c69e';
  for (let i = 0; i < 4; i++) {
    rounded(g, 340 + i * 72, 252, 64, 40, 10);
    g.fill();
  }
  g.fillStyle = '#9fb38a';
  rounded(g, 560, 244, 84, 178, 16);
  g.fill();
  // dark coffee table
  g.fillStyle = '#6b5230';
  rounded(g, 385, 392, 150, 68, 8);
  g.fill();
  g.fillStyle = '#84693f';
  rounded(g, 393, 398, 134, 26, 4);
  g.fill();
  // big round blue ottoman (circle obstacle)
  g.save();
  g.translate(800, 452);
  g.scale(1, 0.7);
  const og = g.createRadialGradient(-12, -14, 8, 0, 0, 58);
  og.addColorStop(0, '#5a7fb4');
  og.addColorStop(1, '#3a567e');
  g.fillStyle = og;
  g.beginPath();
  g.arc(0, 0, 56, 0, 7);
  g.fill();
  g.restore();
  void rng;
  stairVisual(g, 46, 238);
  for (const d of HOUSE_MAP.rooms.solarium!.doors) doorGlow(g, d);
}

export const ROOM_PAINTERS: Record<string, RoomPainter> = {
  foyer: paintFoyer,
  family: paintFamily,
  solarium: paintSolarium,
};
