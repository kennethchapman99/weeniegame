namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the "two dogs, same distraction, different signature move"
    /// beat (a Distract-and-sneak, doctrine #2, built on soft asymmetry): one dog holds a human's
    /// attention while the other sneaks the objective — but the two dogs distract in mechanically
    /// different ways, so picking who distracts changes how the sneak feels:
    ///
    ///   - <see cref="Burp"/> (Cheddar's burp cloud) is a BURST: a big instant attention spike, then a
    ///     cooldown before he can do it again, so attention sawtooths and the sneaker advances only in
    ///     intermittent windows (stop-and-go, timed). Burping on cooldown is a comic
    ///     <see cref="WastedBurps"/> (nothing happens).
    ///   - <see cref="SetBellyFlop"/> (Cocoa rolls over for a belly rub) is a SUSTAIN: while she stays
    ///     flopped the human's attention is steadily held up, giving the sneaker a clean run — but she
    ///     is committed (can't do anything else) and her flop has limited stamina, after which she gets
    ///     up indignantly and the hold drops.
    ///
    /// Either way the human's <see cref="Attention"/> is the same shared meter; the sneaker only makes
    /// progress while the human <see cref="HumanDistracted"/>, and sneaking while undistracted just
    /// stalls and tallies a recoverable <see cref="Exposures"/> (no silent punish). Pure logic; the
    /// mission decides which dog is sneaking via the flag passed to <see cref="Advance"/>.
    ///
    /// Method names lean into the gag for readability but are really the BURST and SUSTAIN distraction
    /// archetypes, so a mission can reskin them (zoomies vs sad-eyes, squeaky toy vs sploot, etc.).
    /// </summary>
    public sealed class CoopHumanDistractionPuzzle
    {
        private float _sneakNeeded = 3f;
        private float _attentionThreshold = 0.5f;
        private float _attentionDecay = 0.5f;   // human loses interest per second
        private float _burpSpike = 0.7f;        // attention added by one burp
        private float _burpCooldown = 2f;       // seconds before Cheddar can burp again
        private float _flopRise = 1.2f;         // attention gained per second while Cocoa is flopped
        private float _flopStaminaMax = 2.5f;   // how long Cocoa can hold the belly-up flop

        public float Attention { get; private set; }
        public float BurpCooldownRemaining { get; private set; }
        public bool BellyFlopped { get; private set; }
        public float FlopStamina { get; private set; }
        public float SneakProgress { get; private set; }

        /// <summary>Burps fired while still on cooldown (eager Cheddar, no effect).</summary>
        public int WastedBurps { get; private set; }

        /// <summary>Times the sneaker started moving while the human was NOT distracted (recoverable).</summary>
        public int Exposures { get; private set; }

        public bool Solved { get; private set; }
        public bool HumanDistracted => Attention >= _attentionThreshold;
        public bool BurpReady => BurpCooldownRemaining <= 0f;
        public float SneakRatio => _sneakNeeded <= 0f ? 1f : (SneakProgress / _sneakNeeded);

        private bool _exposed;

        public void Configure(float sneakNeeded, float attentionThreshold, float attentionDecay,
            float burpSpike, float burpCooldown, float flopRise, float flopStamina)
        {
            _sneakNeeded = sneakNeeded <= 0f ? 3f : sneakNeeded;
            _attentionThreshold = Clamp01(attentionThreshold);
            _attentionDecay = attentionDecay < 0f ? 0f : attentionDecay;
            _burpSpike = burpSpike < 0f ? 0f : burpSpike;
            _burpCooldown = burpCooldown < 0f ? 0f : burpCooldown;
            _flopRise = flopRise < 0f ? 0f : flopRise;
            _flopStaminaMax = flopStamina <= 0f ? 1f : flopStamina;
            Reset();
        }

        /// <summary>Cheddar's burp cloud: a big instant attention spike, then a cooldown.</summary>
        public void Burp()
        {
            if (Solved) return;
            if (BurpCooldownRemaining > 0f) { WastedBurps++; return; }
            Attention = Clamp01(Attention + _burpSpike);
            BurpCooldownRemaining = _burpCooldown;
        }

        /// <summary>Cocoa rolls belly-up for a rub (sustained hold) or gets up. Needs stamina to flop.</summary>
        public void SetBellyFlop(bool flopped)
        {
            if (Solved) return;
            if (flopped) { if (FlopStamina > 0f) BellyFlopped = true; }
            else BellyFlopped = false;
        }

        /// <summary>
        /// Advance the beat. <paramref name="sneaking"/> is whether the non-distracting dog is moving
        /// through the objective lane this step.
        /// </summary>
        public void Advance(float dt, bool sneaking)
        {
            if (Solved || dt <= 0f) return;

            if (BurpCooldownRemaining > 0f)
                BurpCooldownRemaining = System.Math.Max(0f, BurpCooldownRemaining - dt);

            if (BellyFlopped)
            {
                Attention = Clamp01(Attention + _flopRise * dt);
                FlopStamina -= dt;
                if (FlopStamina <= 0f)
                {
                    FlopStamina = 0f;
                    BellyFlopped = false; // Cocoa gives up the flop
                }
            }

            Attention = Clamp01(Attention - _attentionDecay * dt);

            bool distracted = HumanDistracted;
            if (sneaking && distracted)
            {
                SneakProgress += dt;
                if (SneakProgress >= _sneakNeeded)
                {
                    SneakProgress = _sneakNeeded;
                    Solved = true;
                }
            }

            bool nowExposed = sneaking && !distracted && !Solved;
            if (nowExposed && !_exposed) Exposures++;
            _exposed = nowExposed;
        }

        public void Reset()
        {
            Attention = 0f;
            BurpCooldownRemaining = 0f;
            BellyFlopped = false;
            FlopStamina = _flopStaminaMax;
            SneakProgress = 0f;
            WastedBurps = 0;
            Exposures = 0;
            Solved = false;
            _exposed = false;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
