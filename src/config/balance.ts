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
  // NOTE: aiFactor is INERT. The prototype scales the AI's pre-normalised intent vector by
  // 0.88, but moveDog normalises that vector, cancelling the factor — so the AI actually runs
  // at full player speed. Matching the prototype's observed behaviour (owner decision); kept
  // here as the documented hook if a real difficulty slider is wanted later. See MECHANICS.md.
  aiFactor: 0.88,
  aiArriveRadius: 8,     // AI arrival-easing radius (prototype uses 8, vs 10 for touch)
  arriveRadius: 10,      // px within which speed eases to 0
  arriveFalloff: 60,     // px over which arrive ramps 0->1 beyond arriveRadius (prototype's /60)
  faceFlipThreshold: 0.8,// |vx| needed to flip facing
  steerLerp: 0.18,       // velocity approach rate (land)
  steerLerpZoom: 0.24,   // velocity approach rate during zoomies
  idleDecay: 0.78,       // velocity decay when no input
  idleSnap: 0.15         // |v| below which velocity hard-stops to 0
} as const;

export const INPUT = {
  // Gamepad left-stick deadzone: deflection below this reads as no input; above it,
  // the remaining 0..1 range maps to `arrive` (analog speed), mirroring touch's arrive ramp.
  gamepadDeadzone: 0.25,
} as const;

// Co-op "needs both dogs" gate primitives + missions (M12).
export const GATES = {
  padR: 38, // pressure-pad press radius
  goalR: 52, // goal-zone occupancy radius
  gateOpenRate: 3, // gate open-animation speed (prog/sec once latched)
  boostPadR: 34, // booster must be within this of the boost pad
  boostReach: 70, // jumper must be within this of the pad to be launched
  boostJumpMult: 1.7, // boosted arc duration vs a solo jump
  boostLaunchSpeed: 50, // catapult impulse off the booster (decays ~0.78/frame → ~225px of travel)
  distractR: 120, // a threat is distracted while a dog is within this
} as const;

export const MISSION = {
  defaultTime: 90, // generous fail timer for a v1 mission
  timeBonus: 2, // combined-score points per whole second left at success
} as const;

// "Stay Together" survive mission — a hawk dives at whichever pup strays from its sibling.
export const HAWK = {
  huddleR: 116, // pups within this of each other are safe — the hawk won't commit
  circleR: 150, // orbit radius above the yard
  circleSpeed: 1.0, // orbit angular speed (rad/s)
  diveSpeed: 8.5, // dive velocity toward the lone pup (px/frame)
  diveEvery: [2.2, 3.6] as [number, number], // seconds between dive attempts
  grabR: 32, // a dive connecting within this of the target grabs it (→ fail)
  retreat: 1.1, // seconds spent retreating after a dive before re-circling
} as const;

// "Kitchen Counter Caper" co-op mission — asymmetric abilities (Cheddar knocks, both eat).
export const KITCHEN = {
  leapRange: 76, // how close Cheddar must be to a counter item to knock it off
  knockCD: 0.5, // min seconds between knock-offs (one per chair-leap)
  eatR: 40, // proximity to gobble a floor item
  foodScore: 4, // combined-score per item eaten
  floorTtl: 6, // seconds a fallen item survives before it's swept up
  target: 5, // items to eat to win
} as const;

// "Sneak the Snack" distract+grab mission tuning.
export const SNEAK = {
  grabR: 42, // how close a dog must be to a treat to grab it
  settleSpeed: 2, // the cat only lunges a dog SETTLED on a treat (slower than this) — not one running past
  lungeStun: 0.7, // stun on the grabber when the guard catches it undistracted
  lungeKnockback: 7, // knockback speed away from the guard
  lungeCD: 0.7, // min seconds between guard lunges
  treatScore: 4, // combined-score per treat
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
  cooldown: 2.6, whiffCooldown: 0.5, loserStun: 1.35, range: 95,
  immuneBlockedCD: 0.6, // attacker cooldown when the target is belly-rub immune
  lungeSpeed: 8,        // lunge velocity when target is just out of range
  knockback: 8.2,       // loser launch speed
  winnerDamp: 0.2,      // winner's velocity damping on a flip
  stealReach: 30,       // extra px beyond spot/couch radius for a steal-on-win
  aiRange: 82,          // AI engages a wrestle within this distance
  aiRandomRate: 0.25,   // AI's per-second chance to start trouble when nothing else
  stunSkidDecay: 0.9    // velocity decay per step while stunned
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

/**
 * Yard features modelled on the real backyard (Burnet St). The magnolia is the squirrels'
 * hideout: a squirrel scampers to the trunk and vanishes up into the canopy if no dog tags it.
 */
export const YARD = {
  magnolia: { x: 726, y: 250, canopyR: 64, trunkBaseY: 326 },
  squirrelClimb: 0.85, // seconds to scurry up the trunk and disappear
} as const;

export const PREDATOR = {
  firstSpawn: [11, 17] as const,
  respawn: [13, 20] as const,
  minTimeLeft: 9,
  dodgeJumpHeight: 0.3, // jumpHeight above this at the strike = dodged
  coyote: {
    warn: 1.4, enterSpeed: 1.6, chargeSpeed: 5.6, missAfter: 2.4,
    grabRange: 40, grabStun: 2.2, dragTime: 2.0, dragSpeed: 1.2, dragStun: 1.5,
    dropStun: 0.5, fleeSpeed: 7,
  },
  eagle: {
    warn: 1.6, diveSpeed: 11, diveWindow: 2.6, grabRange: 44, grabStun: 3,
    carryTime: 2.6, carryStun: 2, liftSpeed: 22, driftSpeed: 18, dropStun: 0.6,
    climbSpeed: 6, orbitRX: 120, orbitRY: 40, orbitSpeed: 2.2,
  },
  unitedFrontRange: 86, scareRange: 150,
  rescueRange: 70,
  rescueStun: 0.3, // brief settle after a rescue
} as const;

export const EVENTS = {
  schedule: [6, 10] as const,
  bellyImmunity: 3,
  squirrelChance: 0.4, // of the event roll (yard); else treat (<0.72) or belly
  treatChance: 0.72,
  squirrelReward: 3, treatReward: 2,
} as const;
