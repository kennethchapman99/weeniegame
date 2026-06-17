using UnityEngine;
using CheddarAndCocoa.Dogs; // DogId

namespace CheddarAndCocoa.Data
{
    /// <summary>
    /// Per-dog tuning, authored as a ScriptableObject asset (one for Cheddar, one for Cocoa).
    /// This is the Unity home for the web prototype's <c>config/balance.ts</c> + <c>config/dogs.ts</c>
    /// values — DO NOT re-derive these numbers. They were balanced by playtesting + the headless
    /// sim in the TypeScript build. See docs/MECHANICS.md and src/config/balance.ts for rationale.
    ///
    /// Asymmetry (docs/COOP-VISION.md, LEVEL-IDEAS.md):
    ///   Cheddar = chaos puppy — faster, zoomies-prone, better squeeze/jump, worse impulse control,
    ///             can chair-leap, can barf.
    ///   Cocoa   = reigning spot queen — stronger/stubborn, better wrestle odds, calmer, holds
    ///             territory, slower on stairs.
    ///
    /// NOTE ON UNITS: the prototype world is 960x600 "logical units" at a fixed 60 Hz; speeds below
    /// are the prototype's per-frame velocity targets. When wiring DogController, convert to
    /// Unity world units/second (decide a pixels-per-unit scale first) — keep the *ratios* intact.
    /// </summary>
    [CreateAssetMenu(fileName = "DogTuning", menuName = "CheddarAndCocoa/Dog Tuning", order = 0)]
    public sealed class DogTuning : ScriptableObject
    {
        [Header("Identity")]
        public DogId dog = DogId.Cheddar;
        [Tooltip("Dry coat tint. Prototype palettes in src/config/dogs.ts (wet variant = each channel shaded -34).")]
        public Color bodyColor = Color.white;
        public Color wetBodyColor = Color.gray;

        [Header("Movement (prototype SPEED — see balance.ts)")]
        [Tooltip("Base land speed. Prototype: 4.4 (per-frame). Scale to units/sec on import.")]
        public float baseSpeed = 4.4f;
        [Tooltip("On a pool floater. Prototype: 4.9.")]
        public float floaterSpeed = 4.9f;
        [Tooltip("Swimming penalty. Prototype: 1.6.")]
        public float swimSpeed = 1.6f;
        [Tooltip("Zoomies turbo multiplier. Prototype: 1.85.")]
        public float zoomiesMultiplier = 1.85f;
        [Tooltip("Arrival-easing radius (anti-jitter). Prototype: 10 (touch) / 8 (AI).")]
        public float arriveRadius = 10f;

        [Header("Movement feel (Unity units/sec tuning)")]
        [Tooltip("Analog stick/input magnitude below this value is ignored.")]
        [Range(0f, 0.9f)] public float inputDeadzone = 0.25f;
        [Tooltip("How quickly the dog reaches max speed from rest.")]
        public float acceleration = 30f;
        [Tooltip("How quickly the dog stops after input is released.")]
        public float deceleration = 36f;
        [Tooltip("Extra response when the dog reverses or cuts sharply.")]
        public float turnResponsiveness = 48f;
        [Tooltip("Velocity below this value snaps to a full stop to avoid tiny drifts.")]
        public float stopSpeed = 0.08f;
        [Tooltip("Minimum velocity that counts as running for lean, bob, paw trails, and labels.")]
        public float runFeedbackSpeed = 0.22f;

        [Header("Zoomies (prototype ZOOMIES)")]
        [Tooltip("Scores within the window to trigger. Prototype: 3.")]
        public int zoomiesStreak = 3;
        [Tooltip("Rolling streak window. Prototype: 8000ms.")]
        public float zoomiesWindow = 8f;
        [Tooltip("Zoomies duration. Prototype: 4s.")]
        public float zoomiesDuration = 4f;

        [Header("Wrestle (prototype WRESTLE.winChance)")]
        [Tooltip("Attacker reversal odds. Prototype: Cocoa 0.78 / Cheddar 0.70.")]
        [Range(0f, 1f)] public float wrestleWinChance = 0.70f;
        [Tooltip("Loser stun seconds. Prototype: 1.35.")]
        public float wrestleLoserStun = 1.35f;
        [Tooltip("Engage range. Prototype: 95.")]
        public float wrestleRange = 95f;

        [Header("Jump (prototype JUMP)")]
        [Tooltip("Arc duration. Prototype: 0.5s. Dodge predators when height>0.3 at the strike.")]
        public float jumpDuration = 0.5f;

        [Header("House traversal (prototype HOUSE.stairTime)")]
        [Tooltip("Stair traversal seconds. Prototype: Cheddar 0.5 / Cocoa 1.05 (Cheddar's real edge).")]
        public float stairTime = 0.5f;

        [Header("Asymmetric kitchen abilities (LEVEL-IDEAS.md / KITCHEN)")]
        [Tooltip("Can leap onto a chair to snatch table food (Cheddar only).")]
        public bool canChairLeap = false;
        [Tooltip("Chance to barf after eating, locking the dog out briefly (Cheddar only). Prototype: tune like wrestle odds.")]
        [Range(0f, 1f)] public float barfChance = 0f;
        [Tooltip("Chew time before a grabbed food item counts. Prototype KITCHEN: Cheddar faster, Cocoa slower.")]
        public float chewTime = 0.4f;
    }
}
