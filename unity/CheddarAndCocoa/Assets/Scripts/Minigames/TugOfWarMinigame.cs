using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Minigames
{
    /// <summary>
    /// Two-dog tug-of-war. Both dogs lock onto a rope (MovementMode.Tug) and mash to pull the
    /// rope position toward their side; first past the win threshold wins, or it stalemates.
    ///
    /// PROTOTYPE MAP: src/systems/tug.ts + balance.ts TUG —
    ///   grabRange 40, winAt 0.98, stalemate 14s, aiMash {cocoa 2.6, cheddar 2.3},
    ///   ropeSpawnChance 0.30. Audio: continuous growl bed (~0.5–0.85s cadence), yip on win.
    ///   Scoring: solo grab +2, win +3 (SCORE.ropeSolo / ropeWin).
    ///
    /// In couch co-op each human mashes their own side (no auto-mash); in solo the AI partner
    /// mashes at its dog's aiMash rate. This can be competitive (versus) OR a cooperative beat
    /// (e.g. tug a gate open together) depending on the mission.
    /// </summary>
    public sealed class TugOfWarMinigame : MonoBehaviour
    {
        [SerializeField] private float winAt = 0.98f;     // rope position threshold (|p| >= winAt)
        [SerializeField] private float stalemateTime = 14f;
        [SerializeField] private float pullPerMash = 0.04f;

        // Rope position in [-1 (Cocoa side) .. +1 (Cheddar side)]; 0 = center.
        public float Rope { get; private set; }
        public bool Active { get; private set; }
        private float _timer;

        public event System.Action<DogId> OnWin;
        public event System.Action OnStalemate;

        /// <summary>Begin a tug between the two dogs (both enter MovementMode.Tug elsewhere).</summary>
        public void Begin()
        {
            Active = true;
            Rope = 0f;
            _timer = 0f;
        }

        /// <summary>A pull input from one dog this frame. Cheddar pulls +, Cocoa pulls -.</summary>
        public void Mash(DogId dog, float strength = 1f)
        {
            if (!Active) return;
            Rope += (dog == DogId.Cheddar ? 1f : -1f) * pullPerMash * strength;
            Rope = Mathf.Clamp(Rope, -1f, 1f);
        }

        private void Update()
        {
            if (!Active) return;
            _timer += Time.deltaTime;

            if (Rope >= winAt) { Resolve(DogId.Cheddar); return; }
            if (Rope <= -winAt) { Resolve(DogId.Cocoa); return; }
            if (_timer >= stalemateTime)
            {
                Active = false;
                OnStalemate?.Invoke(); // rope flies away, no score (prototype)
            }
        }

        private void Resolve(DogId winner)
        {
            Active = false;
            OnWin?.Invoke(winner);
        }
    }
}
