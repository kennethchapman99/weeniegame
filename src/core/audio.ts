/**
 * core/audio.ts — tiny Web Audio synth, no files (CLAUDE.md zero-network rule). Ported from
 * the prototype's tone()/sndGrowl/sndBark/sndYip/sndScreech/sndSplashy.
 *
 * Systems never touch this directly — they push SoundId values into GameState.sounds (keeping
 * the sim deterministic + headless-safe); the host (main.ts) drains that queue and calls play().
 * Must init on a user gesture (autoplay policy): call resume() on first tap. Fails silently if
 * AudioContext is unavailable (the test harness stubs it away).
 */

export type SoundId = 'growl' | 'bark' | 'yip' | 'screech' | 'splash';

type Win = typeof globalThis & {
  AudioContext?: typeof AudioContext;
  webkitAudioContext?: typeof AudioContext;
};

export class AudioBus {
  private ac: AudioContext | null = null;
  private ok = true;
  private muted = false;

  constructor() {
    // restore the mute preference (localStorage is blocked in some sandboxes — fail soft)
    try {
      this.muted = globalThis.localStorage?.getItem('cc-muted') === '1';
    } catch {
      /* ignore */
    }
  }

  /** Whether sound is currently muted. */
  get isMuted(): boolean {
    return this.muted;
  }

  /** Flip mute and persist the choice. Returns the new state. */
  toggleMuted(): boolean {
    this.muted = !this.muted;
    try {
      globalThis.localStorage?.setItem('cc-muted', this.muted ? '1' : '0');
    } catch {
      /* ignore */
    }
    return this.muted;
  }

  /** Resume/create the context — call from a user gesture. */
  resume(): void {
    this.ctx();
  }

  play(id: SoundId): void {
    if (this.muted) return;
    switch (id) {
      case 'growl':
        this.growl();
        break;
      case 'bark':
        this.bark();
        break;
      case 'yip':
        this.tone('square', 640, 820, 0.09, 0.07, 1400);
        break;
      case 'screech':
        this.tone('sawtooth', 1500, 640, 0.45, 0.05, 2400);
        break;
      case 'splash':
        this.tone('triangle', 300, 90, 0.25, 0.07, 500);
        break;
    }
  }

  private ctx(): AudioContext | null {
    if (!this.ok) return null;
    try {
      if (!this.ac) {
        const w = globalThis as Win;
        const Ctor = w.AudioContext ?? w.webkitAudioContext;
        if (!Ctor) {
          this.ok = false;
          return null;
        }
        this.ac = new Ctor();
      }
      if (this.ac.state === 'suspended') void this.ac.resume();
      return this.ac;
    } catch {
      this.ok = false;
      return null;
    }
  }

  private tone(
    type: OscillatorType,
    f0: number,
    f1: number,
    dur: number,
    vol: number,
    filtF = 1200,
  ): void {
    const a = this.ctx();
    if (!a) return;
    try {
      const o = a.createOscillator();
      const g = a.createGain();
      const f = a.createBiquadFilter();
      o.type = type;
      o.frequency.setValueAtTime(f0, a.currentTime);
      o.frequency.exponentialRampToValueAtTime(Math.max(30, f1), a.currentTime + dur);
      f.type = 'lowpass';
      f.frequency.value = filtF;
      g.gain.setValueAtTime(0.0001, a.currentTime);
      g.gain.exponentialRampToValueAtTime(vol, a.currentTime + 0.03);
      g.gain.exponentialRampToValueAtTime(0.0001, a.currentTime + dur);
      o.connect(f);
      f.connect(g);
      g.connect(a.destination);
      o.start();
      o.stop(a.currentTime + dur + 0.05);
    } catch {
      /* fail silently */
    }
  }

  private growl(): void {
    const a = this.ctx();
    if (!a) return;
    try {
      const o = a.createOscillator();
      const g = a.createGain();
      const f = a.createBiquadFilter();
      const l = a.createOscillator();
      const lg = a.createGain();
      o.type = 'sawtooth';
      // Cosmetic audio pitch jitter; audio is host-layer and never runs in the deterministic
      // headless sim, so it needn't be seeded (the lone sanctioned Math.random in src).
      // eslint-disable-next-line no-restricted-properties
      o.frequency.value = 78 + Math.random() * 22;
      l.type = 'sine';
      l.frequency.value = 19;
      lg.gain.value = 26;
      l.connect(lg);
      lg.connect(o.frequency);
      f.type = 'lowpass';
      f.frequency.value = 280;
      g.gain.setValueAtTime(0.0001, a.currentTime);
      g.gain.exponentialRampToValueAtTime(0.09, a.currentTime + 0.05);
      g.gain.exponentialRampToValueAtTime(0.0001, a.currentTime + 0.55);
      o.connect(f);
      f.connect(g);
      g.connect(a.destination);
      o.start();
      l.start();
      o.stop(a.currentTime + 0.6);
      l.stop(a.currentTime + 0.6);
    } catch {
      /* fail silently */
    }
  }

  private bark(): void {
    this.tone('square', 420, 300, 0.07, 0.08, 900);
    setTimeout(() => this.tone('square', 380, 260, 0.08, 0.08, 900), 110);
  }
}
