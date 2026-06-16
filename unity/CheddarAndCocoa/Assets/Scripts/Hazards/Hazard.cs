using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Hazards
{
    /// <summary>Telegraph → active → recover lifecycle shared by every danger in the game.</summary>
    public enum HazardPhase { Idle, Telegraph, Active, Recover }

    /// <summary>
    /// Base class for hazards: a danger that telegraphs, strikes, and can be dodged or defended.
    /// Concrete hazards subclass this and fill in targeting + the strike resolution.
    ///
    /// PROTOTYPE MAP: src/systems/predators.ts + balance.ts PREDATOR / HAWK, and the kitchen
    /// chair-swat (LEVEL-IDEAS.md). Examples to port as subclasses:
    ///   - CoyoteHazard : enter → warn 1.4s → charge 5.6 → grab/whiff → drag (grab/drag stuns)
    ///   - EagleHazard  : circle → warn 1.6s → dive 11 → carry 2.6s (lifts the dog away)
    ///   - HawkHazard   : "Stay Together" — dives at whichever pup STRAYS from its sibling
    ///   - ChairSwat    : seated human swats Cheddar off the chair (Kitchen, Cheddar-only risk)
    ///
    /// Co-op defenses (the centerpiece): UNITED-FRONT — both dogs within huddle range + free
    /// bark the predator off (balance.ts PREDATOR.unitedFrontRange 86, scareRange 150, HAWK.huddleR
    /// 116). Dodges: jump height > 0.3 at the strike, zoomies, or belly-rub immunity. RESCUE: the
    /// sibling reaches a grabbed dog to free it. Keep the "hilarious bark-off" tone (M7 owner ask).
    /// </summary>
    public abstract class Hazard : MonoBehaviour
    {
        [SerializeField] protected float telegraphTime = 1.4f; // PREDATOR warn windows
        [SerializeField] protected float grabRange = 0.4f;     // world units (port from px / PPU)
        [SerializeField] protected float scoreOnHit = -1f;     // carried-off penalty (min 0 overall)

        public HazardPhase Phase { get; protected set; } = HazardPhase.Idle;
        protected Transform Target;

        /// <summary>Raised when a dog is grabbed/struck (score + VFX + rescue-window hooks).</summary>
        public event System.Action<DogId> OnHit;

        protected virtual void Update()
        {
            switch (Phase)
            {
                case HazardPhase.Idle:      TickIdle(); break;
                case HazardPhase.Telegraph: TickTelegraph(); break;
                case HazardPhase.Active:    TickActive(); break;
                case HazardPhase.Recover:   TickRecover(); break;
            }
        }

        // Subclasses implement the FSM beats:
        protected abstract void TickIdle();      // wait/spawn timer, pick the lone/strayed dog
        protected abstract void TickTelegraph();  // show the reticle/warning; allow huddle to scare off
        protected abstract void TickActive();     // charge/dive; resolve grab vs dodge vs rescue
        protected virtual void TickRecover() { }  // flee/retreat before re-arming

        /// <summary>Call from the strike resolution when a dog is actually caught.</summary>
        protected void ReportHit(DogId dog) => OnHit?.Invoke(dog);

        /// <summary>True if the team is huddled enough to repel this hazard (united front).</summary>
        protected virtual bool ScaredOffByHuddle() => false; // TODO: measure dog separation vs range
    }
}
