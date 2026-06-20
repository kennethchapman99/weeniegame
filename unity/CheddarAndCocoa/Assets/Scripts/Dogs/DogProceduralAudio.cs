using System.Collections.Generic;
using UnityEngine;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>Identity-specific procedural cue settings for one action phase.</summary>
    public readonly struct DogProceduralCueStyle
    {
        public DogProceduralCueStyle(float frequency, float duration, float volume, float cooldown,
            float harmonic, float noise, string signature)
        {
            Frequency = frequency;
            Duration = duration;
            Volume = volume;
            Cooldown = cooldown;
            Harmonic = harmonic;
            Noise = noise;
            Signature = signature;
        }

        public float Frequency { get; }
        public float Duration { get; }
        public float Volume { get; }
        public float Cooldown { get; }
        public float Harmonic { get; }
        public float Noise { get; }
        public string Signature { get; }
    }

    /// <summary>Pure tuning lookup shared by runtime synthesis and deterministic tests.</summary>
    public static class DogProceduralAudioProfile
    {
        public static DogProceduralCueStyle For(DogId dog, DogFeedbackAction action, DogFeedbackPhase phase)
        {
            bool cheddar = dog == DogId.Cheddar;
            int actionIndex = Mathf.Max(0, (int)action - 1);
            float actionBase = (cheddar ? 330f : 205f) + actionIndex * (cheddar ? 47f : 31f);
            float identityHarmonic = cheddar ? 0.42f : 0.68f;
            string dogName = cheddar ? "CHEDDAR" : "COCOA";
            string actionName = action.ToString().ToUpperInvariant();

            switch (phase)
            {
                case DogFeedbackPhase.Anticipation:
                    return new DogProceduralCueStyle(actionBase * (cheddar ? 1.18f : 0.82f),
                        cheddar ? 0.055f : 0.085f, 0.26f, 0.09f, identityHarmonic, 0.05f,
                        $"{dogName} {actionName} WIND-UP");
                case DogFeedbackPhase.Impact:
                    return new DogProceduralCueStyle(actionBase * (action == DogFeedbackAction.Bark ? 0.72f : 1.35f),
                        action == DogFeedbackAction.Rescue ? 0.2f : 0.13f, action == DogFeedbackAction.Bark ? 0.72f : 0.5f,
                        action == DogFeedbackAction.Bark ? 0.18f : 0.12f, identityHarmonic,
                        action == DogFeedbackAction.Bark ? (cheddar ? 0.32f : 0.18f) : 0.1f,
                        $"{dogName} {actionName} HIT");
                case DogFeedbackPhase.Sustain:
                    return new DogProceduralCueStyle(actionBase * (cheddar ? 1.48f : 1.05f),
                        cheddar ? 0.11f : 0.16f, 0.3f, 0.28f, identityHarmonic, 0.04f,
                        $"{dogName} {actionName} LOOP");
                case DogFeedbackPhase.Recovery:
                    return new DogProceduralCueStyle(actionBase * (cheddar ? 0.88f : 0.64f),
                        cheddar ? 0.075f : 0.12f, 0.22f, 0.1f, identityHarmonic, 0.03f,
                        $"{dogName} {actionName} SETTLE");
                default:
                    return new DogProceduralCueStyle(0f, 0f, 0f, 0f, 0f, 0f, "SILENT");
            }
        }
    }

    /// <summary>
    /// Reusable dog-local synthesizer synchronized to <see cref="DogActionFeedback"/>. A fixed
    /// AudioSource pool, per-cue cooldowns, and one sustain voice prevent repeated action state from
    /// turning into audio spam. It does not depend on mission state or the arena audio system.
    /// </summary>
    [RequireComponent(typeof(DogIdentity))]
    [RequireComponent(typeof(DogActionFeedback))]
    public sealed class DogProceduralAudio : MonoBehaviour
    {
        public const int VoiceLimit = 3;
        private const int SampleRate = 22050;

        private sealed class Voice
        {
            public AudioSource Source;
            public float BusyUntil;
            public bool Sustaining;
            public DogFeedbackAction Action;
        }

        private readonly List<Voice> _voices = new List<Voice>(VoiceLimit);
        private readonly Dictionary<int, float> _nextAllowedAt = new Dictionary<int, float>();
        private readonly Dictionary<int, AudioClip> _clips = new Dictionary<int, AudioClip>();
        private DogActionFeedback _feedback;
        private DogIdentity _identity;

        public int TotalCuesPlayed { get; private set; }
        public int TotalCuesRejected { get; private set; }
        public int PeakVoiceCount { get; private set; }
        public string LastCueSignature { get; private set; } = string.Empty;
        public DogFeedbackAction LastCueAction { get; private set; }
        public DogFeedbackPhase LastCuePhase { get; private set; } = DogFeedbackPhase.Idle;

        public void Initialize(DogActionFeedback feedback = null)
        {
            if (_feedback != null) _feedback.PhaseChanged -= OnPhaseChanged;
            _identity = GetComponent<DogIdentity>();
            _feedback = feedback != null ? feedback : GetComponent<DogActionFeedback>();
            EnsureVoices();
            if (_feedback != null) _feedback.PhaseChanged += OnPhaseChanged;
        }

        /// <summary>Deterministic entry point used by the phase hook and focused PlayMode tests.</summary>
        public bool TryPlayPhaseCue(DogFeedbackAction action, DogFeedbackPhase phase, float now)
        {
            if (_identity == null) _identity = GetComponent<DogIdentity>();
            if (action == DogFeedbackAction.None || phase == DogFeedbackPhase.Idle) return false;
            EnsureVoices();

            if (phase == DogFeedbackPhase.Recovery) StopSustain(action);

            int key = CueKey(action, phase);
            if (_nextAllowedAt.TryGetValue(key, out float nextAllowed) && now < nextAllowed)
            {
                TotalCuesRejected++;
                return false;
            }

            Voice voice = FindFreeVoice(now);
            if (voice == null)
            {
                TotalCuesRejected++;
                return false;
            }

            DogProceduralCueStyle style = DogProceduralAudioProfile.For(_identity.Id, action, phase);
            AudioClip clip = GetOrCreateClip(action, phase, style);
            bool sustain = phase == DogFeedbackPhase.Sustain;
            voice.Source.Stop();
            voice.Source.clip = clip;
            voice.Source.volume = style.Volume;
            voice.Source.pitch = 1f;
            voice.Source.loop = sustain;
            voice.Source.Play();
            voice.BusyUntil = sustain ? float.PositiveInfinity : now + style.Duration;
            voice.Sustaining = sustain;
            voice.Action = action;
            _nextAllowedAt[key] = now + style.Cooldown;

            TotalCuesPlayed++;
            LastCueAction = action;
            LastCuePhase = phase;
            LastCueSignature = style.Signature;
            PeakVoiceCount = Mathf.Max(PeakVoiceCount, ActiveVoiceCount(now));
            return true;
        }

        public int ActiveVoiceCount(float now)
        {
            int count = 0;
            foreach (Voice voice in _voices)
            {
                if (voice.Sustaining || voice.BusyUntil > now) count++;
            }
            return count;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_feedback != null) _feedback.PhaseChanged -= OnPhaseChanged;
            foreach (AudioClip clip in _clips.Values)
            {
                if (clip != null) Destroy(clip);
            }
        }

        private void OnPhaseChanged(DogFeedbackAction action, DogFeedbackPhase phase)
        {
            TryPlayPhaseCue(action, phase, Time.unscaledTime);
        }

        private void EnsureVoices()
        {
            while (_voices.Count < VoiceLimit)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1.5f;
                source.maxDistance = 14f;
                source.dopplerLevel = 0f;
                _voices.Add(new Voice { Source = source });
            }
        }

        private Voice FindFreeVoice(float now)
        {
            foreach (Voice voice in _voices)
            {
                if (!voice.Sustaining && voice.BusyUntil <= now) return voice;
            }
            return null;
        }

        private void StopSustain(DogFeedbackAction action)
        {
            foreach (Voice voice in _voices)
            {
                if (!voice.Sustaining || voice.Action != action) continue;
                voice.Source.Stop();
                voice.Source.loop = false;
                voice.Sustaining = false;
                voice.BusyUntil = 0f;
            }
        }

        private AudioClip GetOrCreateClip(DogFeedbackAction action, DogFeedbackPhase phase,
            DogProceduralCueStyle style)
        {
            int key = CueKey(action, phase);
            if (_clips.TryGetValue(key, out AudioClip existing) && existing != null) return existing;

            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(style.Duration * SampleRate));
            var samples = new float[sampleCount];
            int seed = 97 + (int)_identity.Id * 131 + (int)action * 31 + (int)phase * 17;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)Mathf.Max(1, sampleCount - 1);
                float envelope = Mathf.Sin(progress * Mathf.PI);
                float sweep = phase == DogFeedbackPhase.Anticipation ? 0.78f + progress * 0.55f :
                    phase == DogFeedbackPhase.Recovery ? 1.18f - progress * 0.48f : 1f;
                float fundamental = Mathf.Sin(Mathf.PI * 2f * style.Frequency * sweep * t);
                float harmonic = Mathf.Sin(Mathf.PI * 2f * style.Frequency * 2f * t) * style.Harmonic;
                seed = unchecked(seed * 1103515245 + 12345);
                float noise = (((seed >> 16) & 0x7fff) / 16383.5f - 1f) * style.Noise;
                samples[i] = Mathf.Clamp((fundamental + harmonic + noise) * envelope * 0.48f, -1f, 1f);
            }

            string clipName = $"{_identity.Id}_{action}_{phase}_Procedural";
            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            _clips[key] = clip;
            return clip;
        }

        private static int CueKey(DogFeedbackAction action, DogFeedbackPhase phase) =>
            (int)action * 16 + (int)phase;
    }
}
