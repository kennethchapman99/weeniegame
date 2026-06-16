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

        [SerializeField] private float pixelsPerUnit = 100f; // world-unit conversion for prototype speeds
        [SerializeField] private DogTuning tuning;

        private Rigidbody2D _body;
        private DogIdentity _identity;

        public MovementMode Mode { get; private set; } = MovementMode.Free;
        public bool Busy => Mode != MovementMode.Free; // prototype's busy() — gates wrestle/tug/interact

        // Overlays (do not change Mode):
        public bool Zoomies { get; private set; }
        private float _zoomiesUntil;
        public bool Immune { get; private set; }   // belly-rub power-up — blocks wrestle/predator
        private float _wetTimer;                    // dryT: slick render + AI avoids floaties
        private float _jumpT;                       // 0..jumpDuration; height = sin(pi * t/dur)

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _identity = GetComponent<DogIdentity>();
            if (tuning == null) tuning = _identity.Tuning;
            _body.gravityScale = 0f; // top-down / 2.5D orthographic; no falling
        }

        /// <summary>Call once per FixedUpdate from the input source with the latest intent.</summary>
        public void Tick(in MoveIntent intent, float dt)
        {
            UpdateOverlays(dt);

            if (Busy)
            {
                // Stunned/swimming/etc. still need their own per-mode update; stubbed for now.
                // TODO: port per-mode handling (swim toward nearest deck, shake timer, transit lerp).
                return;
            }

            float speed = CurrentSpeed();
            Vector2 desired = intent.move;

            // TODO: arrival easing (prototype anti-jitter): scale speed to 0 within arriveRadius of
            // the target and hard-stop inside it. With analog sticks the stick magnitude already
            // provides the ramp; arrival easing matters for tap-to-move / AI waypoints.
            _body.linearVelocity = desired * speed;

            // TODO: jump arc (B): _jumpT ramps over tuning.jumpDuration; expose Height for the
            // renderer + predator dodge check (height > 0.3 at the strike).
        }

        private float CurrentSpeed()
        {
            // Convert prototype per-frame target (already ratio-correct) to units/sec.
            float baseUnitsPerSec = tuning.baseSpeed / pixelsPerUnit * 60f;
            return Zoomies ? baseUnitsPerSec * tuning.zoomiesMultiplier : baseUnitsPerSec;
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

        public void SetMode(MovementMode mode) => Mode = mode; // single mutation point for mode changes
    }
}
