using System.Collections.Generic;
using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Hazards
{
    /// <summary>
    /// "The Thunderstorm" hazard — telegraphed lightning whose thunderclap spikes the panic of any
    /// pup caught in the open. Cooperates with <see cref="PanicMeter"/>: a strike is foreshadowed by
    /// a flash (telegraph), then the boom adds panic to each pup scaled by how close it is to the
    /// strike — unless the pup is tucked under a shelter, which blunts the spike. Between strikes the
    /// pups must huddle to comfort each other (handled by PanicMeter). Survive the storm to win.
    ///
    /// Co-op heart: neither pup can calm down alone, and a lone pup in the open eats a full spike —
    /// so the team must move and shelter together. PROTOTYPE MAP: the frozen TS "storm" mission +
    /// GAME-DESIGN-BIBLE §16 (Thunderstorm) / §"Panic/calm meter". Additive scaffolding — wire it in
    /// from a bootstrap (see docs/UNITY-MISSIONS-PORT.md); it does not touch the verified arena.
    /// </summary>
    public sealed class ThunderstormHazard : Hazard
    {
        [Header("Strike tuning (world units / seconds)")]
        [SerializeField] private float flashLead = 0.7f;       // telegraph flash before the boom
        [SerializeField] private float recoverSeconds = 0.6f;  // calm after a boom before re-arming
        [SerializeField] private Vector2 boomEvery = new Vector2(2.4f, 4.0f);
        [SerializeField] private float panicSpike = 0.34f;     // spike to an exposed pup at the epicenter
        [SerializeField] private float spikeFalloff = 4.4f;    // ~280px — spike fades to 0 by this distance
        [SerializeField] private float shelterShield = 0.8f;   // fraction of the spike blocked while sheltered
        [SerializeField] private float fieldHalfWidth = 9f;    // strikes land within ±this in x

        private PanicMeter _panic;
        private Transform _cheddar;
        private Transform _cocoa;
        private readonly List<(Transform tf, float radius)> _shelters = new();

        private float _timer;       // counts down to the next strike while Idle
        private float _phaseTimer;  // time spent in the current Telegraph/Recover phase

        /// <summary>Current flash brightness 0..1 (renderers read this for the screen flash).</summary>
        public float Flash { get; private set; }
        /// <summary>World-x of the pending/active strike (renderers draw the bolt here).</summary>
        public float StrikeX { get; private set; }

        public void Configure(PanicMeter panic, Transform cheddar, Transform cocoa)
        {
            _panic = panic;
            _cheddar = cheddar;
            _cocoa = cocoa;
            _timer = Random.Range(boomEvery.x, boomEvery.y);
            Phase = HazardPhase.Idle;
        }

        public void AddShelter(Transform shelter, float radius) => _shelters.Add((shelter, radius));

        protected override void TickIdle()
        {
            if (_panic == null) return;
            Flash = Mathf.MoveTowards(Flash, 0f, Time.deltaTime * 1.6f);
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                StrikeX = Random.Range(-fieldHalfWidth, fieldHalfWidth);
                Flash = 0.5f; // telegraph
                _phaseTimer = 0f;
                Phase = HazardPhase.Telegraph;
            }
        }

        protected override void TickTelegraph()
        {
            _phaseTimer += Time.deltaTime;
            if (_phaseTimer >= flashLead) Phase = HazardPhase.Active;
        }

        protected override void TickActive()
        {
            Flash = 1f; // the clap
            ApplySpike(DogId.Cheddar, _cheddar);
            ApplySpike(DogId.Cocoa, _cocoa);
            _phaseTimer = 0f;
            Phase = HazardPhase.Recover;
        }

        protected override void TickRecover()
        {
            Flash = Mathf.MoveTowards(Flash, 0f, Time.deltaTime * 1.6f);
            _phaseTimer += Time.deltaTime;
            if (_phaseTimer >= recoverSeconds)
            {
                _timer = Random.Range(boomEvery.x, boomEvery.y);
                Phase = HazardPhase.Idle;
            }
        }

        private void ApplySpike(DogId id, Transform dog)
        {
            if (dog == null) return;
            float near = 1f - Mathf.Min(1f, Mathf.Abs(dog.position.x - StrikeX) / spikeFalloff);
            float spike = panicSpike * near;
            if (IsSheltered(dog.position)) spike *= 1f - shelterShield;
            if (spike <= 0f) return;
            _panic.AddSpike(id, spike);
            ReportHit(id); // lets VFX/audio react to a pup getting spooked
        }

        private bool IsSheltered(Vector3 pos)
        {
            foreach (var (tf, radius) in _shelters)
                if (tf != null && Vector2.Distance(pos, tf.position) <= radius) return true;
            return false;
        }
    }
}
