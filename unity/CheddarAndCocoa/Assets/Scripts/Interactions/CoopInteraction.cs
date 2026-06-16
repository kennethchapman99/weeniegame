using System.Collections.Generic;
using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Interactions
{
    /// <summary>
    /// Base class for the "needs both dogs" interdependence gates — the It-Takes-Two DNA
    /// (docs/COOP-VISION.md). A gate tracks which dogs are participating and fires
    /// <see cref="OnGateOpen"/> exactly once when its condition is met.
    ///
    /// PROTOTYPE MAP: src/systems/gates.ts. Port the concrete gates as subclasses:
    ///   - PressurePadGate   : both pads held (one dog can't cover two)  -> updatePads/allPadsPressed
    ///   - BoostJumpGate     : one dog braces, the other is catapulted    -> canBoost/boostLaunch
    ///   - DistractGrabGate  : one dog taunts a threat while the other grabs -> isDistracted
    ///   - BothOnSpotsGate   : both dogs occupy goal zones                 -> bothOnSpots
    /// Tuning lives in balance.ts GATES (padR, goalR, boostReach, distractR, …) — port to fields.
    ///
    /// Keep these headless-testable: the TS sims drive both dogs and assert the gate opens. Mirror
    /// that with Unity EditMode/PlayMode tests against the same constants.
    /// </summary>
    public abstract class CoopInteraction : MonoBehaviour
    {
        [SerializeField] protected bool requiresBothDogs = true;

        private readonly HashSet<DogId> _participants = new();
        private bool _opened;

        /// <summary>Raised once when the gate's condition is satisfied.</summary>
        public event System.Action OnGateOpen;

        public bool IsOpen => _opened;

        protected void SetParticipating(DogId dog, bool participating)
        {
            if (participating) _participants.Add(dog);
            else _participants.Remove(dog);
            Evaluate();
        }

        /// <summary>Override to define the open condition. Base = both dogs participating.</summary>
        protected virtual bool ConditionMet()
        {
            return !requiresBothDogs ? _participants.Count > 0 : _participants.Count >= 2;
        }

        private void Evaluate()
        {
            if (_opened || !ConditionMet()) return;
            _opened = true;
            OnGateOpen?.Invoke();
        }

        /// <summary>Reset on scene/checkpoint re-entry (the prototype's per-scene-reset rule).</summary>
        public virtual void ResetGate()
        {
            _participants.Clear();
            _opened = false;
        }
    }
}
