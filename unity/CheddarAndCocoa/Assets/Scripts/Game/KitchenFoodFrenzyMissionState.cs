namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic core state for the Kitchen Falling Food Frenzy mission (Game Design Bible #8).
    /// An arcade collection beat with danger filtering and a built-in co-op split: the scout dog
    /// (Cheddar) tips food off the counter with <see cref="TriggerDrop"/>, then the sweeper dog
    /// (Cocoa) decides what to do with the in-flight item — <see cref="Catch"/> the good stuff,
    /// <see cref="LetFall"/> the dangerous stuff, or have the scout <see cref="Nudge"/> a bad item
    /// into the safe landing zone (the trash) for a teamwork save.
    ///
    /// Pure logic (no UnityEngine) so PlayMode/EditMode tests can drive the full catch/dodge/combo/
    /// fail loop without real-time falling physics. A single item is in flight at a time to keep the
    /// scout/sweeper hand-off readable and the simulation deterministic.
    /// </summary>
    public sealed class KitchenFoodFrenzyMissionState
    {
        /// <summary>What kind of thing just got tipped off the counter.</summary>
        public enum Food
        {
            /// <summary>Pancake, chicken, cheese, weenies, mystery delicious thing — catch it.</summary>
            Good,
            /// <summary>Onion, broccoli, medicine, gross vegetable — let it hit the floor or nudge it away.</summary>
            Bad,
            /// <summary>Spicy food / hot pan drip — burns on contact; must be dodged or nudged.</summary>
            Hot
        }

        /// <summary>Outcome of a sweeper/scout action on the in-flight item.</summary>
        public enum Resolution
        {
            /// <summary>Nothing was in flight, or the round was already over.</summary>
            None,
            /// <summary>Caught good food. Score and combo go up.</summary>
            Yum,
            /// <summary>Caught a gross/bad item. Upset tummy — a strike.</summary>
            Yuck,
            /// <summary>Caught hot food. Burned tongue — a strike.</summary>
            Burn,
            /// <summary>Scout nudged a dangerous item into the safe zone. Teamwork save.</summary>
            Saved,
            /// <summary>Good food was let go (or knocked away) and hit the floor uneaten.</summary>
            Missed,
            /// <summary>A dangerous item was correctly let go and landed harmlessly.</summary>
            FloorSafe
        }

        public int TargetScore { get; private set; } = 30;
        public int MaxStrikes { get; private set; } = 3;
        public int BaseValue { get; private set; } = 5;
        public int ComboStep { get; private set; } = 2;
        public int SaveBonus { get; private set; } = 2;

        /// <summary>The item currently falling toward the floor, if any.</summary>
        public Food? InFlight { get; private set; }

        public int Score { get; private set; }
        /// <summary>Consecutive good catches without a miss/strike in between.</summary>
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int GoodCaught { get; private set; }
        /// <summary>Bad tummies plus burns — the fail counter.</summary>
        public int Strikes { get; private set; }
        /// <summary>Dangerous items the scout nudged into the safe zone for the team.</summary>
        public int Saves { get; private set; }
        /// <summary>Good food lost to the floor.</summary>
        public int Misses { get; private set; }

        public bool Cleared => Score >= TargetScore;
        public bool Failed => Strikes >= MaxStrikes;
        public bool RoundOver => Cleared || Failed;

        /// <summary>
        /// Configures and resets the round. Negative/zero inputs are clamped to sane minimums so a
        /// misconfigured mission spec can't make the round unwinnable or crash the combo math.
        /// </summary>
        public void Configure(int targetScore, int maxStrikes, int baseValue, int comboStep, int saveBonus)
        {
            TargetScore = targetScore < 1 ? 1 : targetScore;
            MaxStrikes = maxStrikes < 1 ? 1 : maxStrikes;
            BaseValue = baseValue < 1 ? 1 : baseValue;
            ComboStep = comboStep < 0 ? 0 : comboStep;
            SaveBonus = saveBonus < 0 ? 0 : saveBonus;
            ClearRound();
        }

        /// <summary>
        /// Scout tips an item off the counter. Fails if the round is over or an item is already in
        /// flight — one item at a time keeps the hand-off readable and forces the dogs to talk.
        /// </summary>
        public bool TriggerDrop(Food food)
        {
            if (RoundOver || InFlight.HasValue) return false;
            InFlight = food;
            return true;
        }

        /// <summary>Sweeper catches the in-flight item.</summary>
        public Resolution Catch()
        {
            if (RoundOver || !InFlight.HasValue) return None();

            Food food = InFlight.Value;
            InFlight = null;

            switch (food)
            {
                case Food.Good:
                    GoodCaught++;
                    Combo++;
                    if (Combo > BestCombo) BestCombo = Combo;
                    Score += BaseValue + (Combo - 1) * ComboStep;
                    return Resolution.Yum;
                case Food.Hot:
                    Strikes++;
                    Combo = 0;
                    return Resolution.Burn;
                default: // Food.Bad
                    Strikes++;
                    Combo = 0;
                    return Resolution.Yuck;
            }
        }

        /// <summary>
        /// Sweeper lets the in-flight item drop. Correct for dangerous food (lands harmlessly), but
        /// letting good food hit the floor is a miss that breaks the combo.
        /// </summary>
        public Resolution LetFall()
        {
            if (RoundOver || !InFlight.HasValue) return None();

            Food food = InFlight.Value;
            InFlight = null;

            if (food == Food.Good)
            {
                Misses++;
                Combo = 0;
                return Resolution.Missed;
            }

            // Bad/Hot fell safely — no strike, combo streak survives.
            return Resolution.FloorSafe;
        }

        /// <summary>
        /// Scout nudges the in-flight item into the safe landing zone (the trash). A teamwork save
        /// for dangerous food; nudging good food away from the sweeper just wastes it (funny failure).
        /// </summary>
        public Resolution Nudge()
        {
            if (RoundOver || !InFlight.HasValue) return None();

            Food food = InFlight.Value;
            InFlight = null;

            if (food == Food.Good)
            {
                // Knocked the good food into the trash — combo-breaking miss.
                Misses++;
                Combo = 0;
                return Resolution.Missed;
            }

            Saves++;
            Score += SaveBonus;
            return Resolution.Saved;
        }

        public void Reset() => ClearRound();

        private Resolution None() => Resolution.None;

        private void ClearRound()
        {
            InFlight = null;
            Score = 0;
            Combo = 0;
            BestCombo = 0;
            GoodCaught = 0;
            Strikes = 0;
            Saves = 0;
            Misses = 0;
        }
    }
}
