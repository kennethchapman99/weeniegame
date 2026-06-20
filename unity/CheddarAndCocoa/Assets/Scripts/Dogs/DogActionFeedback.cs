using System;
using UnityEngine;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>Dog-local actions that receive anticipation, impact, sustain, and recovery juice.</summary>
    public enum DogFeedbackAction
    {
        None,
        Bark,
        Tug,
        Carry,
        Rescue,
        Zoomies
    }

    public enum DogFeedbackPhase
    {
        Idle,
        Anticipation,
        Impact,
        Sustain,
        Recovery
    }

    /// <summary>Deterministic timing and identity style for one dog action.</summary>
    public readonly struct DogActionFeedbackStyle
    {
        public DogActionFeedbackStyle(float anticipation, float impact, float recovery,
            Vector2 anticipationScale, Vector2 impactScale, float kickDegrees,
            int particleCount, float trailInterval, Color primary, Color secondary,
            string signature)
        {
            Anticipation = anticipation;
            Impact = impact;
            Recovery = recovery;
            AnticipationScale = anticipationScale;
            ImpactScale = impactScale;
            KickDegrees = kickDegrees;
            ParticleCount = particleCount;
            TrailInterval = trailInterval;
            Primary = primary;
            Secondary = secondary;
            Signature = signature;
        }

        public float Anticipation { get; }
        public float Impact { get; }
        public float Recovery { get; }
        public Vector2 AnticipationScale { get; }
        public Vector2 ImpactScale { get; }
        public float KickDegrees { get; }
        public int ParticleCount { get; }
        public float TrailInterval { get; }
        public Color Primary { get; }
        public Color Secondary { get; }
        public string Signature { get; }
    }

    /// <summary>
    /// Central dog-feedback tuning. Cheddar snaps quickly and scatters warm sparks; Cocoa has a
    /// heavier wind-up, wider impact, and controlled teal bursts.
    /// </summary>
    public static class DogActionFeedbackProfile
    {
        public static DogActionFeedbackStyle For(DogId dog, DogFeedbackAction action)
        {
            bool cheddar = dog == DogId.Cheddar;
            Color primary = cheddar ? new Color(1f, 0.48f, 0.08f, 1f) : new Color(0.08f, 0.88f, 0.82f, 1f);
            Color secondary = cheddar ? new Color(1f, 0.9f, 0.22f, 1f) : new Color(0.45f, 1f, 0.72f, 1f);

            switch (action)
            {
                case DogFeedbackAction.Bark:
                    return new DogActionFeedbackStyle(cheddar ? 0.045f : 0.075f, 0.07f, cheddar ? 0.16f : 0.22f,
                        cheddar ? new Vector2(1.16f, 0.78f) : new Vector2(1.2f, 0.75f),
                        cheddar ? new Vector2(0.86f, 1.34f) : new Vector2(1.28f, 1.08f),
                        cheddar ? 9f : 3f, cheddar ? 7 : 5, 0f, primary, secondary,
                        cheddar ? "CHEDDAR BARK POP" : "COCOA COMMAND WAVE");
                case DogFeedbackAction.Tug:
                    return new DogActionFeedbackStyle(cheddar ? 0.08f : 0.13f, 0.09f, 0.2f,
                        cheddar ? new Vector2(1.2f, 0.82f) : new Vector2(1.3f, 0.72f),
                        cheddar ? new Vector2(0.9f, 1.17f) : new Vector2(1.34f, 0.82f),
                        cheddar ? 7f : 2f, cheddar ? 5 : 7, cheddar ? 0.12f : 0.18f, primary, secondary,
                        cheddar ? "CHEDDAR TUG SCRABBLE" : "COCOA ANCHOR DUST");
                case DogFeedbackAction.Carry:
                    return new DogActionFeedbackStyle(cheddar ? 0.07f : 0.11f, 0.08f, 0.24f,
                        cheddar ? new Vector2(1.12f, 0.84f) : new Vector2(1.16f, 0.82f),
                        cheddar ? new Vector2(0.92f, 1.2f) : new Vector2(1.15f, 0.96f),
                        cheddar ? 5f : 1.5f, cheddar ? 6 : 4, cheddar ? 0.16f : 0.22f, primary, secondary,
                        cheddar ? "CHEDDAR LOOT CRUMBS" : "COCOA CARRY GLINT");
                case DogFeedbackAction.Rescue:
                    return new DogActionFeedbackStyle(cheddar ? 0.09f : 0.12f, 0.12f, cheddar ? 0.3f : 0.36f,
                        new Vector2(1.18f, 0.78f),
                        cheddar ? new Vector2(0.84f, 1.42f) : new Vector2(1.24f, 1.18f),
                        cheddar ? 12f : 4f, cheddar ? 12 : 9, 0f, primary, secondary,
                        cheddar ? "CHEDDAR RESCUE CONFETTI" : "COCOA HERO HALO");
                case DogFeedbackAction.Zoomies:
                    return new DogActionFeedbackStyle(cheddar ? 0.035f : 0.06f, 0.06f, 0.28f,
                        cheddar ? new Vector2(1.2f, 0.8f) : new Vector2(1.16f, 0.84f),
                        cheddar ? new Vector2(0.82f, 1.3f) : new Vector2(1.16f, 1.04f),
                        cheddar ? 11f : 3f, cheddar ? 9 : 6, cheddar ? 0.055f : 0.085f, primary, secondary,
                        cheddar ? "CHEDDAR ZOOMIE COMETS" : "COCOA TURBO RIBBONS");
                default:
                    return new DogActionFeedbackStyle(0f, 0f, 0f, Vector2.one, Vector2.one,
                        0f, 0, 0f, primary, secondary, "IDLE");
            }
        }
    }

    /// <summary>
    /// Reusable dog-local feedback sequencer. It never modifies the dog root: callers apply its
    /// sample to a render child, while particles and trails are detached world-space objects.
    /// </summary>
    [RequireComponent(typeof(DogIdentity))]
    public sealed class DogActionFeedback : MonoBehaviour
    {
        private DogIdentity _identity;
        private Transform _visualRoot;
        private Sprite _particleSprite;
        private DogActionFeedbackStyle _style;
        private float _phaseElapsed;
        private float _actionElapsed;
        private float _trailElapsed;
        private bool _transient;
        private bool _tug;
        private bool _carry;
        private bool _zoomies;

        /// <summary>Dog-local timing hook for feedback that must land on the visual action phases.</summary>
        public event Action<DogFeedbackAction, DogFeedbackPhase> PhaseChanged;

        public DogFeedbackAction CurrentAction { get; private set; }
        public DogFeedbackPhase CurrentPhase { get; private set; } = DogFeedbackPhase.Idle;
        public Vector2 VisualScale { get; private set; } = Vector2.one;
        public Vector2 VisualOffset { get; private set; }
        public float VisualRotationDegrees { get; private set; }
        public string CurrentSignature => CurrentAction == DogFeedbackAction.None ? string.Empty : _style.Signature;
        public string LastParticleSignature { get; private set; } = string.Empty;
        public int TotalParticlesEmitted { get; private set; }
        public int TotalTrailsEmitted { get; private set; }
        public Transform VisualRoot => _visualRoot;
        public bool IsNeutral => CurrentPhase == DogFeedbackPhase.Idle && VisualScale == Vector2.one &&
                                 VisualOffset == Vector2.zero && Mathf.Approximately(VisualRotationDegrees, 0f);

        public void Initialize(Transform visualRoot, Sprite particleSprite)
        {
            _identity = GetComponent<DogIdentity>();
            _visualRoot = visualRoot;
            _particleSprite = particleSprite;
        }

        public void Trigger(DogFeedbackAction action)
        {
            if (action != DogFeedbackAction.Bark && action != DogFeedbackAction.Rescue) return;
            Begin(action, true);
        }

        public void SetSustained(DogFeedbackAction action, bool active)
        {
            switch (action)
            {
                case DogFeedbackAction.Tug: _tug = active; break;
                case DogFeedbackAction.Carry: _carry = active; break;
                case DogFeedbackAction.Zoomies: _zoomies = active; break;
                default: return;
            }

            if (_transient) return;
            DogFeedbackAction desired = DesiredSustainedAction();
            if (desired == CurrentAction) return;
            if (CurrentPhase == DogFeedbackPhase.Idle) Begin(desired, false);
            else if (CurrentPhase == DogFeedbackPhase.Recovery) return;
            else BeginRecovery();
        }

        public void Tick(float dt)
        {
            if (_identity == null) _identity = GetComponent<DogIdentity>();
            dt = Mathf.Max(0f, dt);
            _phaseElapsed += dt;
            _actionElapsed += dt;

            AdvancePhase();
            UpdateSample();
        }

        public void TrackMotion(Vector2 velocity, float dt)
        {
            if (velocity.sqrMagnitude < 0.05f || CurrentAction == DogFeedbackAction.None) return;
            float interval = _style.TrailInterval;
            if (_zoomies) interval = DogActionFeedbackProfile.For(_identity.Id, DogFeedbackAction.Zoomies).TrailInterval;
            if (interval <= 0f) return;

            _trailElapsed += Mathf.Max(0f, dt);
            while (_trailElapsed >= interval)
            {
                _trailElapsed -= interval;
                SpawnTrail(velocity.normalized);
            }
        }

        private void Begin(DogFeedbackAction action, bool transient)
        {
            if (_identity == null) _identity = GetComponent<DogIdentity>();
            if (action == DogFeedbackAction.None)
            {
                ResetNeutral();
                return;
            }

            CurrentAction = action;
            _style = DogActionFeedbackProfile.For(_identity.Id, action);
            _phaseElapsed = 0f;
            _actionElapsed = 0f;
            _transient = transient;
            SetPhase(DogFeedbackPhase.Anticipation);
        }

        private void AdvancePhase()
        {
            bool advanced;
            do
            {
                advanced = false;
                float duration = PhaseDuration();
                if (duration > 0f && _phaseElapsed < duration) continue;
                if (duration > 0f) _phaseElapsed -= duration;

                switch (CurrentPhase)
                {
                    case DogFeedbackPhase.Anticipation:
                        SetPhase(DogFeedbackPhase.Impact);
                        EmitImpactParticles();
                        advanced = true;
                        break;
                    case DogFeedbackPhase.Impact:
                        if (!_transient && IsSustained(CurrentAction)) SetPhase(DogFeedbackPhase.Sustain);
                        else SetPhase(DogFeedbackPhase.Recovery);
                        advanced = true;
                        break;
                    case DogFeedbackPhase.Sustain:
                        if (!IsSustained(CurrentAction))
                        {
                            _phaseElapsed = 0f;
                            SetPhase(DogFeedbackPhase.Recovery);
                        }
                        break;
                    case DogFeedbackPhase.Recovery:
                        DogFeedbackAction next = DesiredSustainedAction();
                        if (_transient && next != DogFeedbackAction.None)
                            Begin(next, false);
                        else if (!_transient && next != DogFeedbackAction.None && next != CurrentAction)
                            Begin(next, false);
                        else
                            ResetNeutral();
                        advanced = true;
                        break;
                }
            } while (advanced && CurrentPhase != DogFeedbackPhase.Idle && CurrentPhase != DogFeedbackPhase.Sustain);
        }

        private float PhaseDuration()
        {
            switch (CurrentPhase)
            {
                case DogFeedbackPhase.Anticipation: return _style.Anticipation;
                case DogFeedbackPhase.Impact: return _style.Impact;
                case DogFeedbackPhase.Recovery: return _style.Recovery;
                default: return 0f;
            }
        }

        private void UpdateSample()
        {
            float progress = Mathf.Clamp01(_phaseElapsed / Mathf.Max(0.0001f, PhaseDuration()));
            switch (CurrentPhase)
            {
                case DogFeedbackPhase.Anticipation:
                    VisualScale = Vector2.Lerp(Vector2.one, _style.AnticipationScale, EaseOut(progress));
                    VisualOffset = Vector2.left * (0.09f * progress);
                    VisualRotationDegrees = -_style.KickDegrees * 0.35f * progress;
                    break;
                case DogFeedbackPhase.Impact:
                    VisualScale = Vector2.Lerp(_style.AnticipationScale, _style.ImpactScale, EaseOut(progress));
                    VisualOffset = Vector2.up * (0.11f * (1f - progress));
                    VisualRotationDegrees = _style.KickDegrees * (1f - progress);
                    break;
                case DogFeedbackPhase.Sustain:
                    float wave = Mathf.Sin(_actionElapsed * (CurrentAction == DogFeedbackAction.Tug ? 18f : 10f));
                    VisualScale = Vector2.Lerp(Vector2.one, _style.ImpactScale, 0.12f + Mathf.Abs(wave) * 0.08f);
                    VisualOffset = Vector2.up * Mathf.Abs(wave) * 0.018f;
                    VisualRotationDegrees = wave * _style.KickDegrees * 0.18f;
                    break;
                case DogFeedbackPhase.Recovery:
                    VisualScale = Vector2.Lerp(_style.ImpactScale, Vector2.one, EaseOut(progress));
                    VisualOffset = Vector2.Lerp(Vector2.up * 0.04f, Vector2.zero, progress);
                    VisualRotationDegrees = Mathf.Lerp(_style.KickDegrees * 0.2f, 0f, progress);
                    break;
                default:
                    VisualScale = Vector2.one;
                    VisualOffset = Vector2.zero;
                    VisualRotationDegrees = 0f;
                    break;
            }
        }

        private void BeginRecovery()
        {
            _phaseElapsed = 0f;
            SetPhase(DogFeedbackPhase.Recovery);
        }

        private void SetPhase(DogFeedbackPhase phase)
        {
            CurrentPhase = phase;
            PhaseChanged?.Invoke(CurrentAction, phase);
        }

        private void ResetNeutral()
        {
            CurrentAction = DogFeedbackAction.None;
            CurrentPhase = DogFeedbackPhase.Idle;
            _phaseElapsed = 0f;
            _actionElapsed = 0f;
            _transient = false;
            UpdateSample();
        }

        private DogFeedbackAction DesiredSustainedAction()
        {
            if (_tug) return DogFeedbackAction.Tug;
            if (_carry) return DogFeedbackAction.Carry;
            if (_zoomies) return DogFeedbackAction.Zoomies;
            return DogFeedbackAction.None;
        }

        private bool IsSustained(DogFeedbackAction action) =>
            action == DogFeedbackAction.Tug && _tug ||
            action == DogFeedbackAction.Carry && _carry ||
            action == DogFeedbackAction.Zoomies && _zoomies;

        private void EmitImpactParticles()
        {
            LastParticleSignature = _style.Signature;
            for (int i = 0; i < _style.ParticleCount; i++) SpawnParticle(i, _style.ParticleCount);
            TotalParticlesEmitted += _style.ParticleCount;
        }

        private void SpawnParticle(int index, int count)
        {
            var particle = new GameObject($"{_identity.Id}_{CurrentAction}_Particle_{index}");
            particle.transform.position = transform.position + Vector3.back * 0.03f;
            float angle = (Mathf.PI * 2f * index / Mathf.Max(1, count)) +
                          (_identity.Id == DogId.Cheddar ? index * 0.17f : 0f);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float speed = _identity.Id == DogId.Cheddar ? 2.4f + (index % 3) * 0.35f : 1.65f + (index % 2) * 0.2f;
            float size = _identity.Id == DogId.Cheddar ? 0.07f + (index % 2) * 0.025f : 0.095f;
            particle.transform.localScale = new Vector3(size, size * (_identity.Id == DogId.Cheddar ? 0.65f : 1f), 1f);
            var renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = _particleSprite;
            renderer.sortingOrder = 42;
            renderer.color = index % 2 == 0 ? _style.Primary : _style.Secondary;
            particle.AddComponent<DogActionParticle>().Launch(renderer, direction * speed,
                _identity.Id == DogId.Cheddar ? 0.34f : 0.46f);
        }

        private void SpawnTrail(Vector2 direction)
        {
            var trail = new GameObject($"{_identity.Id}_{CurrentAction}_Trail");
            trail.transform.position = transform.position - (Vector3)(direction * 0.42f) + Vector3.back * 0.04f;
            trail.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            trail.transform.localScale = _identity.Id == DogId.Cheddar
                ? new Vector3(0.22f, 0.045f, 1f)
                : new Vector3(0.3f, 0.075f, 1f);
            var renderer = trail.AddComponent<SpriteRenderer>();
            renderer.sprite = _particleSprite;
            renderer.sortingOrder = 5;
            Color color = _style.Primary;
            color.a = _identity.Id == DogId.Cheddar ? 0.42f : 0.3f;
            renderer.color = color;
            trail.AddComponent<DogActionTrail>().Begin(renderer, _identity.Id == DogId.Cheddar ? 0.22f : 0.34f);
            TotalTrailsEmitted++;
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }

    internal sealed class DogActionParticle : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Vector2 _velocity;
        private float _life;
        private float _age;

        public void Launch(SpriteRenderer renderer, Vector2 velocity, float life)
        {
            _renderer = renderer;
            _velocity = velocity;
            _life = life;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            _velocity *= Mathf.Pow(0.08f, dt);
            transform.position += (Vector3)(_velocity * dt);
            transform.Rotate(0f, 0f, 220f * dt);
            if (_renderer != null)
            {
                Color color = _renderer.color;
                color.a = Mathf.Clamp01(1f - _age / _life);
                _renderer.color = color;
            }
            if (_age >= _life) Destroy(gameObject);
        }
    }

    internal sealed class DogActionTrail : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private float _life;
        private float _age;

        public void Begin(SpriteRenderer renderer, float life)
        {
            _renderer = renderer;
            _life = life;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.localScale += new Vector3(Time.deltaTime * 0.18f, 0f, 0f);
            if (_renderer != null)
            {
                Color color = _renderer.color;
                color.a *= Mathf.Clamp01(1f - _age / _life);
                _renderer.color = color;
            }
            if (_age >= _life) Destroy(gameObject);
        }
    }
}
