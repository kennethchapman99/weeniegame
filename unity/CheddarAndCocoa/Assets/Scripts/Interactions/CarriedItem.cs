using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Objectives;

namespace CheddarAndCocoa.Interactions
{
    /// <summary>
    /// A pick-up-and-carry object for Escort missions (the favourite toy in "The Cleaning Ladies Are
    /// Here"). A free pup within <see cref="grabR"/> picks it up; it then rides the carrier until the
    /// carrier reaches the safe zone (within <see cref="deliverR"/>), which completes the linked
    /// Escort <see cref="LevelObjective"/>. A hazard can knock it loose with <see cref="PutAway"/>.
    ///
    /// PROTOTYPE MAP: the frozen TS "cleaning" mission's toy/couch carry + Objectives.ObjectiveKind
    /// .Escort. Additive scaffolding — see docs/UNITY-MISSIONS-PORT.md for wiring + the PlayMode test.
    /// </summary>
    public sealed class CarriedItem : MonoBehaviour
    {
        [Header("Carry tuning (world units)")]
        [SerializeField] private float grabR = 0.6f;     // proximity for a free pup to pick it up
        [SerializeField] private float deliverR = 0.9f;  // proximity to the safe zone to complete
        [SerializeField] private float carryHeight = 0.25f; // visual offset above the carrier

        private DogIdentity[] _dogs;
        private Transform _safeZone;
        private LevelObjective _escort;
        private DogIdentity _carrier;

        public bool Safe { get; private set; }
        public DogId? Carrier => _carrier != null ? _carrier.Id : (DogId?)null;

        public void Configure(DogIdentity[] dogs, Transform safeZone, LevelObjective escort)
        {
            _dogs = dogs;
            _safeZone = safeZone;
            _escort = escort;
        }

        public void ResetItem()
        {
            _carrier = null;
            Safe = false;
        }

        /// <summary>Knocked from a pup's mouth back into the open at <paramref name="dropAt"/>.</summary>
        public void PutAway(Vector3 dropAt)
        {
            _carrier = null;
            transform.position = dropAt;
        }

        private void Update()
        {
            if (Safe || _dogs == null) return;

            if (_carrier == null)
            {
                _carrier = FindGrabber();
                return;
            }

            // ride the carrier
            var p = _carrier.transform.position;
            p.y += carryHeight;
            transform.position = p;

            // delivered to the safe zone?
            if (_safeZone != null &&
                Vector2.Distance(_carrier.transform.position, _safeZone.position) <= deliverR)
            {
                Safe = true;
                transform.position = _safeZone.position;
                if (_escort != null) _escort.Complete();
            }
        }

        private DogIdentity FindGrabber()
        {
            foreach (var d in _dogs)
            {
                if (d == null) continue;
                var dc = d.GetComponent<DogController>();
                if (dc != null && dc.Busy) continue; // a stunned/tugging pup can't grab
                if (Vector2.Distance(d.transform.position, transform.position) <= grabR) return d;
            }
            return null;
        }
    }
}
