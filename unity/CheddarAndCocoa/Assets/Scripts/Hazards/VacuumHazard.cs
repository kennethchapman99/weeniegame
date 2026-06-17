using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Interactions;

namespace CheddarAndCocoa.Hazards
{
    /// <summary>
    /// "The Cleaning Ladies Are Here" vacuum — a patrolling threat for the distract/carry mission.
    /// It sweeps back and forth hoovering the floor. While a pup that ISN'T carrying the toy gets in
    /// its face (within <see cref="distractR"/>) it fixates and creeps after that decoy; otherwise it
    /// patrols. If the toy-carrier strays within <see cref="catchR"/> of an UN-distracted vacuum, the
    /// toy is "put away" — dropped, and the carrier is briefly stunned.
    ///
    /// Co-op heart (distract / objective split): the toy can only cross the room while a teammate
    /// keeps the vacuum lured — one pup can't both decoy and carry. PROTOTYPE MAP: the frozen TS
    /// "cleaning" mission + systems/gates.ts `isDistracted`. Additive scaffolding — wire it from a
    /// bootstrap (docs/UNITY-MISSIONS-PORT.md); it does not touch the verified arena.
    /// </summary>
    public sealed class VacuumHazard : Hazard
    {
        [Header("Patrol + fixation tuning (world units / seconds)")]
        [SerializeField] private float patrolMinX = -6f;
        [SerializeField] private float patrolMaxX = 6f;
        [SerializeField] private float patrolSpeed = 3f;   // u/s while sweeping
        [SerializeField] private float creepSpeed = 1f;    // u/s while fixated on a decoy
        [SerializeField] private float distractR = 1.9f;   // a non-carrier pup within this lures it
        [SerializeField] private float catchR = 1.1f;      // catches an un-distracted carrier within this
        [SerializeField] private float dropStun = 0.8f;    // seconds the caught carrier is stunned

        private DogIdentity[] _dogs;
        private CarriedItem _toy;
        private int _dir = 1;

        private DogController _stunnedDog;
        private float _stunTimer;

        /// <summary>True while a decoy is keeping the vacuum busy (renderers show the alarmed face).</summary>
        public bool Distracted { get; private set; }

        public void Configure(DogIdentity[] dogs, CarriedItem toy)
        {
            _dogs = dogs;
            _toy = toy;
            Phase = HazardPhase.Active; // the vacuum is always live; all logic runs in TickActive
        }

        protected override void TickIdle() { }
        protected override void TickTelegraph() { }

        protected override void TickActive()
        {
            if (_dogs == null) return;
            float dt = Time.deltaTime;
            TickStunRecovery(dt);

            DogId? carrier = _toy != null ? _toy.Carrier : null;
            DogIdentity decoy = NearestDecoy(carrier);
            Distracted = decoy != null;

            if (Distracted)
            {
                // fixate: creep slowly toward the decoy (so it stays off the carrier)
                float dirX = Mathf.Sign(decoy.transform.position.x - transform.position.x);
                transform.position += new Vector3(dirX * creepSpeed * dt, 0f, 0f);
            }
            else
            {
                // patrol sweep, bouncing at the ends of the range
                transform.position += new Vector3(_dir * patrolSpeed * dt, 0f, 0f);
                if (transform.position.x <= patrolMinX) { SetX(patrolMinX); _dir = 1; }
                else if (transform.position.x >= patrolMaxX) { SetX(patrolMaxX); _dir = -1; }

                // catch the carrier out in the open
                if (carrier.HasValue && _toy != null)
                {
                    var holder = DogWithId(carrier.Value);
                    if (holder != null &&
                        Vector2.Distance(holder.transform.position, transform.position) <= catchR)
                    {
                        PutAwayToy(holder);
                    }
                }
            }
        }

        private void PutAwayToy(DogIdentity holder)
        {
            _toy.PutAway(holder.transform.position);
            var dc = holder.GetComponent<DogController>();
            if (dc != null)
            {
                dc.SetMode(MovementMode.Stunned);
                _stunnedDog = dc;
                _stunTimer = dropStun;
            }
            ReportHit(holder.Id);
        }

        private void TickStunRecovery(float dt)
        {
            if (_stunnedDog == null) return;
            _stunTimer -= dt;
            if (_stunTimer <= 0f)
            {
                _stunnedDog.SetMode(MovementMode.Free);
                _stunnedDog = null;
            }
        }

        private DogIdentity NearestDecoy(DogId? carrier)
        {
            DogIdentity best = null;
            float bestD = distractR;
            foreach (var d in _dogs)
            {
                if (d == null || (carrier.HasValue && d.Id == carrier.Value)) continue;
                float dist = Vector2.Distance(d.transform.position, transform.position);
                if (dist <= bestD) { bestD = dist; best = d; }
            }
            return best;
        }

        private DogIdentity DogWithId(DogId id)
        {
            foreach (var d in _dogs)
                if (d != null && d.Id == id) return d;
            return null;
        }

        private void SetX(float x)
        {
            var p = transform.position;
            p.x = x;
            transform.position = p;
        }
    }
}
