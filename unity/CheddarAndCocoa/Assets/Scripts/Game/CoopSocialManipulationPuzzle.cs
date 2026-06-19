using System;

namespace CheddarAndCocoa.Game
{
    /// <summary>Distinct dog-applied social stimuli a human can read (combine to send a message).</summary>
    [Flags]
    public enum SocialStimulus
    {
        None = 0,
        DoorStare = 1,
        PresentLeash = 2,
        BarkRhythm = 4,
        NudgeShoe = 8,
        BlockHallway = 16,
        UnplugCharger = 32,
    }

    /// <summary>
    /// Reusable co-op puzzle primitive for the Social manipulation beat (doctrine #9): a human is a
    /// puzzle system, not just a hazard. The dogs combine distinct stimuli (door-stare, present leash,
    /// bark rhythm, nudge shoe, block hallway…) to send ONE clear message, e.g. "take us for a walk".
    /// The lock is combinatorial and split across both dogs:
    ///
    ///   - the human only "gets it" while the active stimuli are EXACTLY the required combination
    ///     (<see cref="ExactMatch"/>), and that combo is drawn from both dogs so neither can do it
    ///     alone;
    ///   - a wrong or incomplete combo builds <see cref="Confusion"/> (faster with an off-message
    ///     stimulus), and if it maxes the human MISREADS and brings the wrong thing — a funny,
    ///     recoverable failure (<see cref="Misreads"/>), not a silent penalty.
    ///
    /// Pure logic: the mission toggles stimuli (from inputs/positions) and advances time; tests drive
    /// it deterministically.
    /// </summary>
    public sealed class CoopSocialManipulationPuzzle
    {
        private SocialStimulus _required = SocialStimulus.None;
        private float _comprehendNeeded = 2f;
        private float _confusionMax = 3f;

        public SocialStimulus Active { get; private set; }
        public float Comprehension { get; private set; }
        public float Confusion { get; private set; }
        public int Misreads { get; private set; }

        public bool Solved => _required != SocialStimulus.None && Comprehension >= _comprehendNeeded;
        public bool ExactMatch => Active == _required && _required != SocialStimulus.None;
        public bool HasOffMessageStimulus => (Active & ~_required) != 0;
        public SocialStimulus Required => _required;

        public void Configure(SocialStimulus required, float comprehendNeeded, float confusionMax)
        {
            _required = required;
            _comprehendNeeded = comprehendNeeded <= 0f ? 2f : comprehendNeeded;
            _confusionMax = confusionMax <= 0f ? 3f : confusionMax;
            Reset();
        }

        /// <summary>Toggle a single stimulus on/off (a dog starts/stops doing it).</summary>
        public void SetStimulus(SocialStimulus stimulus, bool on)
        {
            Active = on ? (Active | stimulus) : (Active & ~stimulus);
        }

        /// <summary>Replace the whole active set (used by the position driver each step).</summary>
        public void SetActiveSet(SocialStimulus active) => Active = active;

        public void Advance(float dt)
        {
            if (Solved || dt <= 0f) return;

            if (ExactMatch)
            {
                Comprehension += dt;
                Confusion = Math.Max(0f, Confusion - dt);
                if (Comprehension >= _comprehendNeeded) Comprehension = _comprehendNeeded;
            }
            else
            {
                Confusion += dt * (HasOffMessageStimulus ? 1.5f : 1f);
                Comprehension = Math.Max(0f, Comprehension - dt * 0.5f);
                if (Confusion >= _confusionMax)
                {
                    Misreads++;        // human brings the wrong thing
                    Confusion = 0f;
                    Comprehension = 0f;
                }
            }
        }

        public void Reset()
        {
            Active = SocialStimulus.None;
            Comprehension = 0f;
            Confusion = 0f;
            Misreads = 0;
        }
    }
}
