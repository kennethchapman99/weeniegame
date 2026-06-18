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

        private Vector3 _baseScale = Vector3.one;
        private float _barkPop; // cosmetic squash-stretch timer after a bark

        // Overlays (do not change Mode):
        public bool Zoomies { get; private set; }
        public bool TravelAssist { get; private set; }
        public float TravelAssistMultiplier { get; private set; } = 1f;
        private float _zoomiesUntil;
        public bool Immune { get; private set; }   // belly-rub power-up — blocks wrestle/predator
        private float _wetTimer;                    // dryT: slick render + AI avoids floaties
        private float _jumpT;                       // 0..jumpDuration; height = sin(pi * t/dur)

        public float MaxSpeedUnitsPerSecond => CurrentSpeed();
        public float AccelerationUnitsPerSecond => tuning != null ? tuning.acceleration : 0f;
        public float DecelerationUnitsPerSecond => tuning != null ? tuning.deceleration : 0f;
        public float TurnResponsivenessUnitsPerSecond => tuning != null ? tuning.turnResponsiveness : 0f;
        public float StopSpeed => tuning != null ? tuning.stopSpeed : 0f;
        public Vector2 CurrentVelocity => _body != null ? _body.linearVelocity : Vector2.zero;

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

            if (Busy)
            {
                // Stunned/swimming/etc. still need their own per-mode update; stubbed for now.
                // TODO: port per-mode handling (swim toward nearest deck, shake timer, transit lerp).
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

            // TODO: jump arc (B): _jumpT ramps over tuning.jumpDuration; expose Height for the
            // renderer + predator dodge check (height > 0.3 at the strike).
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

        // Cosmetic only: decay the bark squash-stretch. Logic stays in Tick (logic/render split).
        private void Update()
        {
            if (_barkPop > 0f)
            {
                _barkPop = Mathf.Max(0f, _barkPop - Time.deltaTime);
                float s = 1f + _barkPop * 1.2f; // brief puff-up
                transform.localScale = new Vector3(_baseScale.x * s, _baseScale.y * s, _baseScale.z);
            }
        }

        private float CurrentSpeed()
        {
            // Convert prototype per-frame target (already ratio-correct) to units/sec.
            float baseUnitsPerSec = tuning.baseSpeed / pixelsPerUnit * 60f;
            float speed = Zoomies ? baseUnitsPerSec * tuning.zoomiesMultiplier : baseUnitsPerSec;
            return TravelAssist ? speed * TravelAssistMultiplier : speed;
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
