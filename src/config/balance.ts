/**
 * config/balance.ts — single source of truth for all tuning constants.
 * Values lifted from the validated prototype. Do not change without re-running
 * the relevant sim (see docs/TESTING.md). See docs/MECHANICS.md for rationale.
 *
 * This is a STARTER file for milestone M0. Extend it as systems are ported;
 * never inline these numbers in system code.
 */

export const WORLD = { w: 960, h: 600, dprCap: 3 } as const;

export const BOUNDS = { minX: 50, maxX: 960 - 50, minY: 215, maxY: 600 - 45 } as const;

export const SPEED = {
  free: 4.4,
  floater: 4.9,
  water: 1.6,
  zoomMult: 1.85,
  aiFactor: 0.88,
  arriveRadius: 10,      // px within which speed eases to 0
  arriveFalloff: 60,     // px over which arrive ramps 0->1 beyond arriveRadius (prototype's /60)
  faceFlipThreshold: 0.8,// |vx| needed to flip facing
  steerLerp: 0.18,       // velocity approach rate (land)
  steerLerpZoom: 0.24,   // velocity approach rate during zoomies
  idleDecay: 0.78,       // velocity decay when no input
  idleSnap: 0.15         // |v| below which velocity hard-stops to 0
} as const;

export const ROUNDS = [
  { key: 'yard',  name: 'The Backyard', time: 45 },
  { key: 'pool',  name: 'The Pool',     time: 45 },
  { key: 'house', name: 'The House',    time: 75 }
] as const;

export const SCORE = {
  toy: 1, ropeSolo: 2, ropeWin: 3, spot: 3, couch: 5,
  squish: 1, sunbeam: 1, treat: 2, squirrel: 3, carriedPenalty: 1
} as const;

export const SPOT = {
  radius: 52,        // cuddle-spot bed radius
  hold: 3,           // seconds of solo occupancy to score
  stealBumpCD: 1.4,  // attacker cooldown after a steal
  stealRelSpeed: 2.2,// min relative speed for a contact to count as a steal
  dogCollideDist: 54 // dog-vs-dog separation distance
} as const;

export const WRESTLE = {
  winChance: { cocoa: 0.78, cheddar: 0.70 },
  cooldown: 2.6, whiffCooldown: 0.5, loserStun: 1.35, range: 95
} as const;

export const ZOOMIES = { streak: 3, windowMs: 8000, duration: 4 } as const;

export const JUMP = { duration: 0.5, dodgeThreshold: 0.3 } as const;

export const TUG = {
  grabRange: 40, winAt: 0.98, stalemate: 14,
  aiMash: { cocoa: 2.6, cheddar: 2.3 }, ropeSpawnChance: 0.30
} as const;

export const POOL = { shake: 0.9, wetTimer: 4.5 } as const;

export const HOUSE = {
  stairTime: { cheddar: 0.5, cocoa: 1.05 },
  doorTime: 0.35,
  couch: { hold: 4, reward: 5, cooldown: 6 },
  squish: { perRoom: 2, napTime: 1.2, respawn: 6 }
} as const;

export const SUNBEAM = { baskTime: 3.5, relocate: [9, 13] as const } as const;

export const PREDATOR = {
  firstSpawn: [11, 17] as const,
  respawn: [13, 20] as const,
  minTimeLeft: 9,
  coyote: { warn: 1.4, chargeSpeed: 5.6, missAfter: 2.4, dragTime: 2.0 },
  eagle:  { warn: 1.6, diveSpeed: 11, diveWindow: 2.6, carryTime: 2.6 },
  unitedFrontRange: 86, scareRange: 150,
  rescueRange: 70
} as const;

export const EVENTS = { schedule: [6, 10] as const, bellyImmunity: 3 } as const;
