using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Shared-panic / co-regulation primitive — the emotional heart of the comfort missions
    /// (Thunderstorm) and the reusable basis for the Vet, Nail-Grinder, and Big-Dog ideas.
    ///
    /// Each pup carries its own panic level (0..1). The ONLY way panic comes down is the two pups
    /// huddling within <see cref="cuddleR"/> to comfort each other; a pup left alone slowly works
    /// itself up. A mission watches <see cref="Maxed"/> — if EITHER pup hits 1.0, both bolt and the
    /// mission fails. Hazards (thunder, the grinder, a looming vet) push panic up via
    /// <see cref="AddSpike"/>.
    ///
    /// PROTOTYPE MAP: the frozen TS reference's "The Thunderstorm" mission design + GAME-DESIGN-BIBLE
    /// §"Panic/calm meter". Tuning is embedded here (the TS build is frozen): values originate from
    /// px-space and are converted to world units (~0.0156 u/px on the 20×12u arena / 1280px field).
    /// </summary>
    public sealed class PanicMeter : MonoBehaviour
    {
        [Header("Co-regulation tuning (per-second rates)")]
        [SerializeField] private float cuddleR = 1.9f;       // ~120px — within this the pups comfort each other
        [SerializeField] private float comfortDrain = 0.17f; // panic/sec drained from BOTH while cuddling
        [SerializeField] private float aloneRise = 0.05f;    // panic/sec a lone pup gains

        public float CheddarPanic { get; private set; }
        public float CocoaPanic { get; private set; }

        /// <summary>The pup (if any) that has maxed out — the mission's fail trigger. Null = safe.</summary>
        public DogId? Maxed =>
            CheddarPanic >= 1f ? DogId.Cheddar : CocoaPanic >= 1f ? DogId.Cocoa : (DogId?)null;

        public float CuddleRadius => cuddleR;

        public float PanicOf(DogId id) => id == DogId.Cheddar ? CheddarPanic : CocoaPanic;

        public void ResetMeter()
        {
            CheddarPanic = 0f;
            CocoaPanic = 0f;
        }

        /// <summary>Add a panic spike to one pup (a thunderclap, a grinder pass), clamped to 1.</summary>
        public void AddSpike(DogId id, float amount)
        {
            if (id == DogId.Cheddar) CheddarPanic = Mathf.Clamp01(CheddarPanic + amount);
            else CocoaPanic = Mathf.Clamp01(CocoaPanic + amount);
        }

        /// <summary>
        /// Advance both meters one step given the two pups' positions. Cuddling drains both;
        /// apart, each rises. Pure given inputs (no other state) so it's straightforward to test.
        /// </summary>
        public void Step(Vector2 cheddar, Vector2 cocoa, float dt)
        {
            bool cuddling = Vector2.Distance(cheddar, cocoa) <= cuddleR;
            float delta = cuddling ? -comfortDrain * dt : aloneRise * dt;
            CheddarPanic = Mathf.Clamp01(CheddarPanic + delta);
            CocoaPanic = Mathf.Clamp01(CocoaPanic + delta);
        }
    }
}
