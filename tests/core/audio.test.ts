/**
 * AudioBus mute toggle. The synth itself needs Web Audio (stubbed away headless), but the mute
 * state is plain logic and persistence is fail-soft when localStorage is unavailable.
 */
import { describe, it, expect } from 'vitest';
import { AudioBus } from '../../src/core/audio.js';

describe('AudioBus mute', () => {
  it('defaults to unmuted and toggles', () => {
    const bus = new AudioBus();
    expect(bus.isMuted).toBe(false);
    expect(bus.toggleMuted()).toBe(true);
    expect(bus.isMuted).toBe(true);
    expect(bus.toggleMuted()).toBe(false);
    expect(bus.isMuted).toBe(false);
  });

  it('play() is a no-op when muted (and never throws without Web Audio)', () => {
    const bus = new AudioBus();
    bus.toggleMuted();
    expect(() => bus.play('bark')).not.toThrow();
  });
});
