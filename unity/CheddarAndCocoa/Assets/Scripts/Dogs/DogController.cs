using UnityEngine;
using CheddarAndCocoa.Data;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>
    /// Mutually-exclusive movement states. Lifted from docs/ARCHITECTURE.md's recommended
    /// discriminated <c>MovementMode</c> — replaces the prototype's bag-of-booleans on the dog
    /// (which caused "two states at once" bugs). Overlays (zoomies, jump, wet timer, immunity)
    /// are tracked separately, not as modes.
    /// </summary>
    public enum MovementMode
    {
        Free,     // normal, accepts input
        Stunned,  // wrestle/predator knockback — no input
        Swimming, // fell in the pool — slow, clipped render
        Shaking,  // drying off at the deck edge
        Transit,  // moving through a door / up stairs
        Tug       // locked into a tug-of-war (see TugOfWarMinigame)
    }

    /// <summary>
    /// Drives a single dog's locomotion. Input-source agnostic: a controller
    /// (<see cref="CheddarAndCocoa.Input.GamepadPlayerInput"/>) or an AI brain writes an
    /// <see cref="MoveIntent"/> each frame; this component turns it into motion and owns the
    /// movement mode + overlays. Keep simulation here and rendering in a separate component
    /// (the prototype's logic/render separation rule).
    ///
    /// PROTOTYPE MAP:
    ///   - systems/movement.ts  -> Move()/arrival easing/zoomies/jump arc below
    ///   - state/dog.ts         -> MovementMode + overlays
    ///   - the ⚠️ units bug      -> prototype used pos += v*dt*60; here we use a Rigidbody2D and
    ///                              real units/sec, so DO NOT copy the *60 — copy the speed RATIOS
    ///                              from DogTuning and tune once against the prototype's feel.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(DogIdentity))]
    public sealed class DogController : MonoBehaviour
    {
        /// <summary>What an input source feeds in each frame. World-space, normalized-ish.</summary>
        public struct MoveIntent
        {
            public Vector2 move;      // desired direction; magnitude 0..1 maps to analog speed (arrive ramp)
            public bool wrestle;      // A button — lunge/flip a nearby sibling
            public bool jump;         // B button — arc hop (dodge predators)
            public bool bark;         // X button — united-front scare / signal
            public bool interact;     // Y button — grab/sniff/use a CoopInteraction
        }

        [SerializeField] private float pixelsPerUnit = 50f; // world-unit conversion for prototype speeds
        [SerializeField] private DogTuning tuning;

        private Rigidbody2D _body;
        private DogIdentity _identity;

        public MovementMode Mode { get; private set; } = MovementMode.Free;
        public bool Busy => Mode != MovementMode.Free; // prototype's busy() — gates wrestle/tug/interact

        /// <summary>Fired when this dog barks (debug UI / SFX / united-front defense subscribe).</summary>
        public event System.Action<DogId> OnBark;
        /// <summary>Fired on the grab/interact button (placeholder until interactions are wired).</summary>
        public event System.Action<DogId> OnInteract;
        /// <summary>Fired on the jump button (feedback/audio layers subscribe for the hop pop).</summary>
        public event System.Action<DogId> OnJump;
        /// <summary>Fired on the wrestle button; GameManager resolves the cross-dog range/immunity/
        /// reversal-odds roll (this controller only knows about itself, not its sibling).</summary>
        public event System.Action<DogId> OnWrestle;

        private Vector3 _baseScale = Vector3.one;
        private float _barkPop; // cosmetic squash-stretch timer after a bark

        // Overlays (do not change Mode):
        public bool Zoomies { get; private set; }
        public bool TravelAssist { get; private set; }
        public float TravelAssistMultiplier { get; private set; } = 1f;
        /// <summary>
        /// Generic mission-owned speed debuff (e.g. Burr Maze's burr-caked "rustle" penalty).
        /// Separate from <see cref="TravelAssistMultiplier"/>, which is boost-only (clamped >=1).
        /// </summary>
        public float SpeedPenaltyMultiplier { get; private set; } = 1f;
        private float _zoomiesUntil;
        public bool Immune { get; private set; }   // belly-rub power-up — blocks wrestle/predator
        private float _wetTimer;                    // dryT: slick render + AI avoids floaties
        private float _jumpT;                       // counts down from jumpDuration to 0 while jumping
        public bool IsJumping => _jumpT > 0f;
        public float JumpHeight01 { get; private set; } // 0..1 arc height, sin(pi * elapsed/duration)
        /// <summary>Normalized 0..1 travel through the current hop, used only to synchronize art.</summary>
        public float JumpProgress01 { get; private set; }
        private float _wrestleStunT;                 // counts down from wrestleLoserStun to 0 while flipped
        public bool IsWrestleStunned => _wrestleStunT > 0f;
        private float _wrestleCooldownUntil;          // Time.time the next wrestle attempt is allowed
        public bool WrestleOnCooldown => Time.time < _wrestleCooldownUntil;
        public float WrestleWinChance => tuning != null ? tuning.wrestleWinChance : 0.5f;
        public float WrestleRange => tuning != null ? tuning.wrestleRange : 1.8f;
        public float WrestleLoserStunSeconds => tuning != null ? tuning.wrestleLoserStun : 1.35f;
        public float WrestleCooldownSeconds => tuning != null ? tuning.wrestleCooldown : 2.6f;
        public float WrestleWhiffCooldownSeconds => tuning != null ? tuning.wrestleWhiffCooldown : 0.5f;
        public float WrestleImmuneBlockedCooldownSeconds => tuning != null ? tuning.wrestleImmuneBlockedCooldown : 0.6f;
        public float WrestleKnockbackSpeed => tuning != null ? tuning.wrestleKnockback : 10f;
        public float WrestleWinnerDamp => tuning != null ? tuning.wrestleWinnerDamp : 0.2f;
        public float WrestleLungeSpeed => tuning != null ? tuning.wrestleLungeSpeed : 9.8f;

        // Pool overlays/ratios ported from the frozen TS build (config/balance.ts SPEED + POOL):
        // water 1.6 vs free 4.4, floater 4.9 vs free 4.4. The pool zone component owns splash,
        // shake, and wet-timer orchestration; the controller only owns how each mode steers.
        public const float SwimSpeedRatio = 0.36f;
        public const float FloaterSpeedRatio = 1.11f;
        public bool OnFloater { get; private set; }
        public bool IsWet => _wetTimer > 0f;
        public void SetOnFloater(bool onFloater) => OnFloater = onFloater;
        public void SetWet(float seconds) => _wetTimer = Mathf.Max(_wetTimer, seconds);

        /// <summary>Mission-start/restart hook: overlays are timers, not modes, so SetMode(Free)
        /// alone doesn't clear them — a dog could otherwise carry a wet tint into a new round
        /// (even an indoor one with no pool).</summary>
        public void ResetMissionOverlays()
        {
            Zoomies = false;
            OnFloater = false;
            _wetTimer = 0f;
            _wrestleStunT = 0f;
            _wrestleCooldownUntil = 0f;
            SpeedPenaltyMultiplier = 1f;
        }

        /// <summary>Mission-owned slow debuff. Clamped below 1 so it can never accidentally boost.</summary>
        public void SetSpeedPenalty(float multiplier) => SpeedPenaltyMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);

        public float MaxSpeedUnitsPerSecond => CurrentSpeed();
        public float AccelerationUnitsPerSecond => tuning != null ? tuning.acceleration : 0f;
        public float DecelerationUnitsPerSecond => tuning != null ? tuning.deceleration : 0f;
        public float TurnResponsivenessUnitsPerSecond => tuning != null ? tuning.turnResponsiveness : 0f;
        public float StopSpeed => tuning != null ? tuning.stopSpeed : 0f;
        public Vector2 CurrentVelocity => _body != null ? _body.linearVelocity : Vector2.zero;
        public float BarkVisualPulse => 1f + _barkPop * 1.2f;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _identity = GetComponent<DogIdentity>();
            if (tuning == null) tuning = _identity.Tuning;
            _body.gravityScale = 0f; // top-down / 2.5D orthographic; no falling
            _body.freezeRotation = true;
            _baseScale = transform.localScale;
        }

        /// <summary>Call once per frame from the input source with the latest intent.</summary>
        public void Tick(in MoveIntent intent, float dt)
        {
            UpdateOverlays(dt);

            // Action buttons resolve even while moving (bark/interact are not blocked by Free).
            if (intent.bark) Bark();
            if (intent.interact) Interact();
            if (intent.jump) Jump();
            if (intent.wrestle) Wrestle();

            if (Mode == MovementMode.Swimming)
            {
                // Swimming stays steerable, just slow (prototype SPEED.water). Zoomies fizzle in
                // the water; the pool zone flips the mode back at the deck edge via Shaking.
                Vector2 swimInput = intent.move.sqrMagnitude > 1f ? intent.move.normalized : intent.move;
                float swimSpeed = tuning.baseSpeed / pixelsPerUnit * 60f * SwimSpeedRatio;
                _body.linearVelocity = Vector2.MoveTowards(
                    _body.linearVelocity, swimInput * swimSpeed, tuning.acceleration * 0.6f * dt);
                return;
            }

            if (Busy)
            {
                // Shaking/stunned/transit/tug: rooted. Shake and transit timers are owned by the
                // systems that set those modes (pool zone, doors), not the controller.
                _body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 desiredInput = intent.move.sqrMagnitude > 1f ? intent.move.normalized : intent.move;
            Vector2 desiredVelocity = desiredInput * CurrentSpeed();
            Vector2 currentVelocity = _body.linearVelocity;
            float response = MovementResponse(currentVelocity, desiredVelocity);

            Vector2 nextVelocity = Vector2.MoveTowards(currentVelocity, desiredVelocity, response * dt);
            if (desiredInput.sqrMagnitude <= 0.0001f && nextVelocity.magnitude <= tuning.stopSpeed)
                nextVelocity = Vector2.zero;

            _body.linearVelocity = nextVelocity;
        }

        /// <summary>Bark: the core verb. For the first playable this logs + pops the sprite + fires
        /// an event. Later this drives the united-front predator scare-off (systems/predators.ts).</summary>
        public void Bark()
        {
            _barkPop = 0.18f;
            Debug.Log($"[{_identity.Id}] WOOF!");
            OnBark?.Invoke(_identity.Id);
        }

        /// <summary>Grab/interact placeholder — logs + fires an event. Later: pick up a toy / use a
        /// CoopInteraction / sniff a ScentTrail.</summary>
        public void Interact()
        {
            Debug.Log($"[{_identity.Id}] interact (grab placeholder)");
            OnInteract?.Invoke(_identity.Id);
        }

        /// <summary>Jump: a short arc hop, the default contextual-action fallback when nothing else
        /// is in range. No-ops mid-jump or while Busy (Shaking/Stunned/Tug/Transit are rooted).
        /// Height ramps via <see cref="JumpHeight01"/> in <see cref="UpdateOverlays"/> for the
        /// renderer, and will gate a predator dodge once threats check it (prototype JUMP:
        /// height > 0.3 at the strike).</summary>
        public void Jump()
        {
            if (Busy || IsJumping) return;
            _jumpT = tuning != null ? Mathf.Max(0.0001f, tuning.jumpDuration) : 0.5f;
            Debug.Log($"[{_identity.Id}] HOP!");
            OnJump?.Invoke(_identity.Id);
        }

        /// <summary>Wrestle: lunge for a nearby sibling. This controller only knows about itself, so
        /// it just gates on its own eligibility (not mid-jump-worthy busy state, not on cooldown) and
        /// fires the event; GameManager resolves range/immunity/reversal-odds against the sibling and
        /// calls back into <see cref="ApplyWrestleStun"/>/<see cref="ApplyWrestleCooldown"/>.</summary>
        public void Wrestle()
        {
            if (Busy || WrestleOnCooldown) return;
            Debug.Log($"[{_identity.Id}] WRESTLE!");
            OnWrestle?.Invoke(_identity.Id);
        }

        /// <summary>Called by GameManager after resolving a wrestle attempt (win, lose, whiff, or
        /// blocked) so the next attempt from this dog waits out the right cooldown.</summary>
        public void ApplyWrestleCooldown(float seconds) => _wrestleCooldownUntil = Time.time + Mathf.Max(0f, seconds);

        /// <summary>Called by GameManager on the losing dog: root it in a timed stun (auto-recovers,
        /// unlike the predator-grab stun which needs a rescue bark) and launch the knockback.</summary>
        public void ApplyWrestleStun(float seconds, Vector2 knockbackVelocity)
        {
            Mode = MovementMode.Stunned;
            _wrestleStunT = Mathf.Max(0.0001f, seconds);
            _body.linearVelocity = knockbackVelocity;
        }

        /// <summary>Called by GameManager on the winning dog: a brief velocity damp on the flip,
        /// matching the prototype's winnerDamp.</summary>
        public void DampVelocity(float factor) => _body.linearVelocity *= factor;

        /// <summary>Called by GameManager when a wrestle whiffs just out of range: a one-shot velocity
        /// kick toward the sibling, matching the prototype's near-miss lunge (src/systems/wrestle.ts).
        /// Free mode's own Tick() steers it back toward player input on the next frames via the
        /// existing MoveTowards response, so this only needs to set the initial velocity - no new
        /// mode or override window required.</summary>
        public void ApplyWrestleLunge(Vector2 towardSibling)
        {
            if (towardSibling.sqrMagnitude < 0.0001f) return;
            _body.linearVelocity = towardSibling.normalized * WrestleLungeSpeed;
        }

        // Cosmetic only: decay the bark squash-stretch. Logic stays in Tick (logic/render split).
        private void Update()
        {
            if (_barkPop > 0f)
            {
                _barkPop = Mathf.Max(0f, _barkPop - Time.deltaTime);
                // The arena readability component applies the pulse to its visual child so the
                // physics silhouette stays stable. Keep this root-scale fallback for the minimal
                // controller test scene, which deliberately has no art-feedback component.
                if (!TryGetComponent<DogReadabilityFeedback>(out _))
                {
                    float s = BarkVisualPulse;
                    transform.localScale = new Vector3(_baseScale.x * s, _baseScale.y * s, _baseScale.z);
                }
            }
        }

        private float CurrentSpeed()
        {
            // Convert prototype per-frame target (already ratio-correct) to units/sec.
            float baseUnitsPerSec = tuning.baseSpeed / pixelsPerUnit * 60f;
            float speed = Zoomies ? baseUnitsPerSec * tuning.zoomiesMultiplier : baseUnitsPerSec;
            if (OnFloater) speed *= FloaterSpeedRatio; // prototype: slightly faster scampering on floaties
            if (TravelAssist) speed *= TravelAssistMultiplier;
            return speed * SpeedPenaltyMultiplier;
        }

        private float MovementResponse(Vector2 currentVelocity, Vector2 desiredVelocity)
        {
            if (desiredVelocity.sqrMagnitude <= 0.0001f) return tuning.deceleration;
            if (currentVelocity.sqrMagnitude <= 0.0001f) return tuning.acceleration;

            float alignment = Vector2.Dot(currentVelocity.normalized, desiredVelocity.normalized);
            if (alignment < 0.35f) return tuning.turnResponsiveness;
            return tuning.acceleration;
        }

        private void UpdateOverlays(float dt)
        {
            if (Zoomies && Time.time >= _zoomiesUntil) Zoomies = false;
            if (_wetTimer > 0f) _wetTimer = Mathf.Max(0f, _wetTimer - dt);

            if (_jumpT > 0f)
            {
                float duration = tuning != null ? Mathf.Max(0.0001f, tuning.jumpDuration) : 0.5f;
                _jumpT = Mathf.Max(0f, _jumpT - dt);
                JumpProgress01 = Mathf.Clamp01(1f - _jumpT / duration);
                // Snap the landing frame to exactly 0 rather than Sin(PI), which floats a tiny
                // nonzero value (~-8.7e-8) instead of a clean touchdown.
                JumpHeight01 = _jumpT <= 0f ? 0f : Mathf.Sin(Mathf.PI * (1f - _jumpT / duration));
            }
            else
            {
                JumpHeight01 = 0f;
                JumpProgress01 = 0f;
            }

            if (_wrestleStunT > 0f)
            {
                _wrestleStunT = Mathf.Max(0f, _wrestleStunT - dt);
                // A wrestle stun self-expires (unlike the predator-grab stun, which needs a rescue
                // bark) - only clear the mode if nothing else re-stunned/re-modes the dog meanwhile.
                if (_wrestleStunT <= 0f && Mode == MovementMode.Stunned) Mode = MovementMode.Free;
            }
        }

        /// <summary>Hot-streak turbo. Hook this from the scoring system (prototype: 3 scores / 8s).</summary>
        public void TriggerZoomies()
        {
            Zoomies = true;
            _zoomiesUntil = Time.time + tuning.zoomiesDuration;
        }

        public void SetTravelAssist(bool active, float multiplier = 1f)
        {
            TravelAssist = active;
            TravelAssistMultiplier = active ? Mathf.Max(1f, multiplier) : 1f;
        }

        public void SetMode(MovementMode mode) => Mode = mode; // single mutation point for mode changes
    }
}
