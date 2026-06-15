/**
 * systems/gates.ts — the "needs both dogs" interdependence primitives (M12).
 *
 * These are the reusable co-op mechanics missions compose from. Each is small and DOM-free so
 * it's driven straight from the headless sim (drive both dogs, assert the gate opens). None of
 * them can be satisfied by a single dog — that interdependence is the whole point (It Takes Two).
 *
 *   - pressure pads (`updatePads` + `allPadsPressed`): two+ pads pressed *at the same time*;
 *     one dog can't cover two, so both are required. The momentary form is "bothOnSpots"; hold
 *     a timer on top for "paired pressure pads".
 *   - boost-jump (`canBoost` + `boostLaunch`): one dog braces on a boost pad so the other can
 *     launch off it to a ledge it couldn't reach alone.
 *   - distract+grab (`isDistracted`): one dog draws a threat's attention so the other can grab
 *     the objective unmolested.
 */

import type { Dog } from '../state/dog.js';
import type { Pad } from '../state/gameState.js';
import type { Point } from '../core/math.js';
import { GATES, JUMP } from '../config/balance.js';

/** Mark which pads are pressed this step (nearest dog within the pad radius presses it). */
export function updatePads(pads: Pad[], dogs: Dog[]): void {
  for (const p of pads) {
    p.on = false;
    p.by = null;
    let best = p.r;
    for (const d of dogs) {
      const dd = Math.hypot(d.x - p.x, d.y - p.y);
      if (dd <= best) {
        best = dd;
        p.on = true;
        p.by = d.id;
      }
    }
  }
}

/** True only when EVERY pad is pressed this step — and by distinct dogs (no double-counting). */
export function allPadsPressed(pads: Pad[]): boolean {
  if (pads.length === 0) return false;
  if (!pads.every((p) => p.on && p.by)) return false;
  const pressers = new Set(pads.map((p) => p.by));
  return pressers.size === pads.length; // each pad pressed by a *different* dog
}

/**
 * Boost-jump eligibility: the `booster` is parked on the pad and the `jumper` is close and
 * grounded (not already airborne). Both dogs are needed — the jumper can't boost itself.
 */
export function canBoost(booster: Dog, jumper: Dog, pad: Point): boolean {
  if (booster.mode !== 'free' || jumper.mode !== 'free') return false;
  if (jumper.jumpT > 0) return false;
  const boosterOn = Math.hypot(booster.x - pad.x, booster.y - pad.y) <= GATES.boostPadR;
  const jumperNear = Math.hypot(jumper.x - pad.x, jumper.y - pad.y) <= GATES.boostReach;
  return boosterOn && jumperNear;
}

/** Launch the jumper off the booster: a longer, higher arc than a solo jump (returns true). */
export function boostLaunch(jumper: Dog, toward: Point): boolean {
  jumper.jumpT = JUMP.duration * GATES.boostJumpMult;
  const dx = toward.x - jumper.x;
  const dy = toward.y - jumper.y;
  const m = Math.hypot(dx, dy) || 1;
  jumper.vx = (dx / m) * GATES.boostLaunchSpeed;
  jumper.vy = (dy / m) * GATES.boostLaunchSpeed;
  return true;
}

/** A threat is distracted while any *other* dog is within `r` of it (so a teammate can sneak in). */
export function isDistracted(threat: Point, dogs: Dog[], r = GATES.distractR): boolean {
  return dogs.some((d) => Math.hypot(d.x - threat.x, d.y - threat.y) <= r);
}
