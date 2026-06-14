/**
 * render/dog.ts — side-view long-haired mini dachshund renderer.
 * Faithful port of the prototype's `drawDog`. Pure drawing: reads dog state, mutates
 * nothing. Wet/swim/shake/stun branches are wired but only fire once later milestones
 * set those modes/overlays; M1 dogs are always 'free' so they render the dry trot.
 *
 * `t` is elapsed time in milliseconds (animation phase).
 */

import type { Dog } from '../state/dog.js';
import { jumpHeight } from '../state/dog.js';
import { DOGS, type Palette } from '../config/dogs.js';
import { rounded, shade } from '../core/math.js';
import { JUMP } from '../config/balance.js';

type G = CanvasRenderingContext2D;

export function drawDog(g: G, d: Dog, t: number): void {
  const swimming = d.mode === 'swimming';
  const stunned = d.mode === 'stunned';
  const shaking = d.mode === 'shaking';
  const wet = d.dryT > 0 && !swimming;
  const p: Palette = swimming || wet ? DOGS[d.id].wet : DOGS[d.id].dry;
  const dir = d.face;
  const trot = stunned ? 1 : Math.min(1, Math.hypot(d.vx, d.vy) / 3.2);
  const bob = Math.abs(Math.sin(t * 0.02 + d.seed)) * 3 * trot;
  const wag = Math.sin(t * 0.025 + d.seed) * (0.5 + trot * 0.6);

  // zoomies after-image trail
  if (d.trail.length) {
    for (const tr of d.trail) {
      g.save();
      g.globalAlpha = tr.life * 0.22;
      g.fillStyle = d.id === 'cheddar' ? '#e3ab63' : '#5a3621';
      g.beginPath();
      g.ellipse(tr.x, tr.y, 30, 16, 0, 0, 7);
      g.fill();
      g.restore();
    }
  }

  const lift = jumpHeight(d, JUMP.duration) * 26; // hop height
  g.save();
  g.translate(d.x, d.y - bob - lift);

  if (lift > 1) {
    // ground shadow shrinks as you rise
    g.save();
    g.translate(0, 26 + bob + lift);
    g.scale(1 - lift * 0.012, 0.3);
    g.fillStyle = 'rgba(20,14,8,' + (0.3 - lift * 0.006) + ')';
    g.beginPath();
    g.arc(0, 0, 40, 0, 7);
    g.fill();
    g.restore();
  }
  if (!swimming && lift <= 1) {
    g.save();
    g.scale(dir, 1);
    g.translate(0, (stunned ? 20 : 26) + bob);
    g.scale(1, 0.32);
    const sh = g.createRadialGradient(0, 0, 4, 0, 0, 46);
    sh.addColorStop(0, 'rgba(20,14,8,.34)');
    sh.addColorStop(1, 'rgba(20,14,8,0)');
    g.fillStyle = sh;
    g.beginPath();
    g.arc(0, 0, 46, 0, 7);
    g.fill();
    g.restore();
  }

  // BARK! exaggerated lunge: big forward thrusts, full-body recoil jitter, and a squash/stretch
  // puff pulse — deliberately over-the-top (the united-front bark-off should look hilarious).
  if (d.barkT > 0) {
    const bp = Math.min(1, d.barkT / 0.55); // 1 → 0
    const chomp = Math.sin(t * 0.05); // synced with the mouth gape
    g.translate(dir * (4 + chomp * 12) * bp, Math.sin(t * 0.11 + d.seed) * 5 * bp);
    g.rotate(dir * chomp * 0.08 * bp); // nose-up lunge
    const puff = 1 + 0.2 * bp * (0.5 + 0.5 * chomp); // squash-stretch
    g.scale(puff, 1 + 0.16 * bp * (0.5 - 0.5 * chomp));
  }

  if (stunned) {
    g.rotate(Math.sin(t * 0.035 + d.seed) * 0.12);
    g.scale(dir, -1);
    g.translate(0, 6);
  } else if (shaking) {
    g.rotate(Math.sin(t * 0.12 + d.seed) * 0.22);
    g.scale(dir, 1);
  } else if (swimming) {
    g.scale(dir, 1);
    g.beginPath();
    g.rect(-78, -92, 156, 96);
    g.clip();
    g.translate(0, Math.round(18 + Math.sin(t * 0.012 + d.seed) * 2));
    g.rotate(-0.14);
  } else {
    g.scale(dir, 1);
  }

  // tail (feathered)
  g.save();
  g.translate(-40, -8);
  g.rotate(-0.5 + wag * 0.45);
  g.fillStyle = p.body[1];
  g.beginPath();
  g.moveTo(0, 3);
  g.quadraticCurveTo(-18, -6, -30, -20);
  g.quadraticCurveTo(-22, -4, -26, -2);
  g.quadraticCurveTo(-19, 2, -22, 8);
  g.quadraticCurveTo(-12, 7, 0, 7);
  g.closePath();
  g.fill();
  g.restore();

  // far legs
  legPair(g, p, -gaitOf(t, d, trot, stunned), true);

  // long body — capsule with vertical fur gradient
  const bgr = g.createLinearGradient(0, -24, 0, 24);
  bgr.addColorStop(0, p.body[0]);
  bgr.addColorStop(1, p.body[1]);
  g.fillStyle = bgr;
  rounded(g, -44, -20, 86, 42, 21);
  g.fill();
  // belly feathering
  g.fillStyle = p.body[1];
  g.beginPath();
  for (let i = 0; i < 6; i++) {
    const fx = -36 + i * 14;
    g.moveTo(fx, 18);
    g.quadraticCurveTo(fx + 4, 30 + (i % 2) * 3, fx + 9, 19);
  }
  g.fill();
  // back highlight
  g.strokeStyle = 'rgba(255,255,255,.16)';
  g.lineWidth = 5;
  g.lineCap = 'round';
  g.beginPath();
  g.moveTo(-36, -15);
  g.quadraticCurveTo(0, -22, 36, -13);
  g.stroke();

  // near legs
  legPair(g, p, gaitOf(t, d, trot, stunned), false);

  // chest fluff
  g.fillStyle = p.chest;
  g.beginPath();
  g.moveTo(30, -8);
  g.quadraticCurveTo(46, 4, 38, 22);
  g.quadraticCurveTo(33, 28, 28, 20);
  g.quadraticCurveTo(24, 8, 30, -8);
  g.closePath();
  g.fill();

  // head group
  g.save();
  g.translate(44, -22);
  g.rotate(Math.sin(t * 0.004 + d.seed) * 0.04);
  const hg = g.createRadialGradient(6, -8, 2, 2, 0, 26);
  hg.addColorStop(0, p.body[0]);
  hg.addColorStop(1, p.body[1]);
  g.fillStyle = hg;
  g.beginPath();
  g.arc(0, 0, 19, 0, 7);
  g.fill();
  // snout
  g.beginPath();
  g.moveTo(10, -7);
  g.quadraticCurveTo(30, -7, 33, 2);
  g.quadraticCurveTo(33, 8, 24, 9);
  g.quadraticCurveTo(12, 10, 8, 6);
  g.closePath();
  g.fill();
  // nose
  g.fillStyle = p.nose;
  g.beginPath();
  g.arc(31, 0, 4.4, 0, 7);
  g.fill();
  g.fillStyle = 'rgba(255,255,255,.45)';
  g.beginPath();
  g.arc(29.6, -1.6, 1.4, 0, 7);
  g.fill();
  // mouth
  g.strokeStyle = p.nose;
  g.lineWidth = 1.6;
  g.lineCap = 'round';
  g.beginPath();
  g.moveTo(29, 5);
  g.quadraticCurveTo(24, 9, 19, 7);
  g.stroke();
  // eye
  g.fillStyle = '#fff';
  g.beginPath();
  g.arc(8, -6, 5.6, 0, 7);
  g.fill();
  g.fillStyle = p.eye;
  g.beginPath();
  g.arc(9.4, -5.6, 3.9, 0, 7);
  g.fill();
  g.fillStyle = '#000';
  g.beginPath();
  g.arc(10, -5.4, 2, 0, 7);
  g.fill();
  g.fillStyle = 'rgba(255,255,255,.95)';
  g.beginPath();
  g.arc(8.4, -7.2, 1.3, 0, 7);
  g.fill();
  // brow tan dot (cocoa)
  if (d.id === 'cocoa') {
    g.fillStyle = '#8a5b38';
    g.beginPath();
    g.arc(6, -13, 2.6, 0, 7);
    g.fill();
  }
  // long floppy ear
  g.save();
  g.translate(-2, -12);
  g.rotate(Math.sin(t * 0.012 + d.seed) * 0.08 + 0.12);
  const eg = g.createLinearGradient(0, 0, 0, 40);
  eg.addColorStop(0, p.ear[0]);
  eg.addColorStop(1, p.ear[1]);
  g.fillStyle = eg;
  g.beginPath();
  g.moveTo(0, 0);
  g.bezierCurveTo(-16, 4, -19, 26, -12, 40);
  g.bezierCurveTo(-8, 46, 2, 44, 5, 36);
  g.bezierCurveTo(9, 22, 8, 6, 0, 0);
  g.closePath();
  g.fill();
  g.strokeStyle = 'rgba(0,0,0,.14)';
  g.lineWidth = 1.6;
  g.beginPath();
  g.moveTo(-9, 12);
  g.quadraticCurveTo(-5, 16, -8, 22);
  g.stroke();
  g.beginPath();
  g.moveTo(-4, 20);
  g.quadraticCurveTo(0, 25, -3, 31);
  g.stroke();
  g.restore();
  g.restore(); // head

  g.restore(); // dog

  if (wet && !swimming) {
    // slick sheen line along the back while drying
    g.save();
    g.translate(d.x, d.y - bob);
    g.scale(d.face, 1);
    g.strokeStyle = 'rgba(220,240,255,.4)';
    g.lineWidth = 3;
    g.lineCap = 'round';
    g.beginPath();
    g.moveTo(-30, -17);
    g.quadraticCurveTo(0, -23, 30, -15);
    g.stroke();
    g.restore();
  }

  if (d.barkT > 0) drawBarkMouth(g, d, t);
}

/** A chomping open gape + tongue + radiating impact lines at the snout while barking. */
function drawBarkMouth(g: G, d: Dog, t: number): void {
  const bp = Math.min(1, d.barkT / 0.55);
  const open = (0.4 + 0.6 * Math.abs(Math.sin(t * 0.05))) * bp; // rapid chomp, synced to lunge
  g.save();
  g.translate(d.x, d.y - 2);
  g.scale(d.face, 1);
  const hx = 70;
  const hy = -24;
  const gape = 8 + open * 18; // big cartoon gape
  // gaping mouth (dark maw)
  g.fillStyle = '#3a1418';
  g.beginPath();
  g.ellipse(hx, hy, 14, gape, 0, 0, 7);
  g.fill();
  // upper + lower fangs
  g.fillStyle = '#fff';
  for (const sx of [-8, 4]) {
    g.beginPath();
    g.moveTo(hx + sx, hy - gape * 0.6);
    g.lineTo(hx + sx + 3, hy - gape * 0.6 + 6);
    g.lineTo(hx + sx - 3, hy - gape * 0.6 + 6);
    g.closePath();
    g.fill();
    g.beginPath();
    g.moveTo(hx + sx, hy + gape * 0.6);
    g.lineTo(hx + sx + 3, hy + gape * 0.6 - 6);
    g.lineTo(hx + sx - 3, hy + gape * 0.6 - 6);
    g.closePath();
    g.fill();
  }
  // lolling tongue
  g.fillStyle = '#e2768a';
  g.beginPath();
  g.ellipse(hx + 3, hy + gape * 0.45, 6, 4 + open * 6, 0, 0, 7);
  g.fill();
  // radiating spittle/impact lines
  g.strokeStyle = `rgba(255,255,255,${0.6 * bp})`;
  g.lineWidth = 2.5;
  g.lineCap = 'round';
  for (let i = -2; i <= 2; i++) {
    const a = i * 0.34;
    g.beginPath();
    g.moveTo(hx + 14, hy);
    g.lineTo(hx + 14 + Math.cos(a) * (18 + open * 10), hy + Math.sin(a) * (18 + open * 10));
    g.stroke();
  }
  g.restore();
}

function gaitOf(t: number, d: Dog, trot: number, stunned: boolean): number {
  return Math.sin(t * (stunned ? 0.05 : 0.02) + d.seed) * trot;
}

function legPair(g: G, p: Palette, ph: number, far: boolean): void {
  const c = far ? shade(p.body[1], -14) : p.body[1];
  drawLeg(g, -30, ph * 6, c);
  drawLeg(g, 26, -ph * 6, c);
}

function drawLeg(g: G, lx: number, swing: number, c: string): void {
  g.save();
  g.translate(lx, 14);
  g.rotate(swing * 0.045);
  g.fillStyle = c;
  rounded(g, -5.5, 0, 11, 16, 5);
  g.fill();
  g.beginPath();
  g.ellipse(0, 16, 7, 4.6, 0, 0, 7);
  g.fill();
  g.restore();
}
