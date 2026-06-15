/**
 * systems/mission.ts — the co-op mission framework (M12).
 *
 * The structural shift from competitive rounds (timer → next scene) to cooperative missions
 * (objectives → success/fail → retry). Objective completion and the combined score mutate
 * through the single points here — `completeObjective` / `addCombined` — the same discipline
 * `addScore` enforces for the versus rounds (BUILD-PLAN M12 watch-out: no scattered flags).
 *
 * A mission is a `SceneDef` (visuals + per-step mechanics) whose `enter()` calls `startMission`
 * to install objectives + interdependence entities; `sceneManager` calls `tickMission` after the
 * scene update to advance timers and resolve the outcome.
 */

import type { GameState, MissionState, Objective, ObjectiveKind } from '../state/gameState.js';
import { MISSION } from '../config/balance.js';

/** Build a fresh objective (count/progress at 0, not yet done). */
export function objective(
  kind: ObjectiveKind,
  label: string,
  reward: number,
  target = 1,
): Objective {
  return { kind, label, done: false, progress: 0, target, count: 0, reward };
}

/** Install a mission's state (objectives + entities) on entry. Entities default to none. */
export function startMission(
  s: GameState,
  m: Pick<MissionState, 'key' | 'title' | 'objectives'> & Partial<MissionState>,
): void {
  s.mission = {
    key: m.key,
    title: m.title,
    objectives: m.objectives,
    status: 'active',
    combinedScore: 0,
    stars: 0,
    elapsed: 0,
    timeLimit: m.timeLimit ?? MISSION.defaultTime,
    starTime: m.starTime ?? [30, 60],
    pads: m.pads ?? [],
    gate: m.gate ?? null,
    goal: m.goal ?? null,
    data: m.data ?? {},
  };
}

/** The single combined-score mutation point. */
export function addCombined(s: GameState, n: number): void {
  if (s.mission) s.mission.combinedScore += n;
}

/** Complete objective `i` exactly once, awarding its reward to the combined score. */
export function completeObjective(s: GameState, i: number): void {
  const m = s.mission;
  if (!m) return;
  const o = m.objectives[i];
  if (!o || o.done) return;
  o.done = true;
  o.progress = 1;
  addCombined(s, o.reward);
}

/** Set an objective's progress (0..1) for the HUD; flips `done` (once) when it reaches 1. */
export function setProgress(s: GameState, i: number, progress: number): void {
  const m = s.mission;
  if (!m) return;
  const o = m.objectives[i];
  if (!o || o.done) return;
  o.progress = Math.max(0, Math.min(1, progress));
  if (o.progress >= 1) completeObjective(s, i);
}

/**
 * Advance the active mission and resolve it. Success when every objective is done (a finishing
 * time bonus + 1–3★ by completion time); failure when the time limit elapses first.
 */
export function tickMission(s: GameState, dt: number): void {
  const m = s.mission;
  if (!m || m.status !== 'active') return;
  m.elapsed += dt;

  if (m.objectives.every((o) => o.done)) {
    m.status = 'success';
    const left = Math.max(0, m.timeLimit - m.elapsed);
    addCombined(s, Math.floor(left) * MISSION.timeBonus);
    m.stars = m.elapsed < m.starTime[0] ? 3 : m.elapsed < m.starTime[1] ? 2 : 1;
    return;
  }

  if (m.elapsed >= m.timeLimit) m.status = 'fail';
}
