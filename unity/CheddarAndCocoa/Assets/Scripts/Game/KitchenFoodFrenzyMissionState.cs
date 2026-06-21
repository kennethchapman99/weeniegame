using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic state for the Kitchen Falling Food Frenzy co-op beat. The mission is built on a
    /// counter/floor role dependency, not parallel catching: only the <see cref="ScoutDog"/> (Cheddar,
    /// the chaos puppy who can leap the counter) can knock a piece of food loose, and only the
    /// <see cref="SweeperDog"/> (Cocoa, the floor veteran guarding the bowl) can convert a falling drop
    /// into a scored catch by nudging it into the safe landing zone. Good food must be caught, bad food
    /// must be dodged (let it splat), and catching a drop that is not steered into the safe zone is a
    /// funny near-miss rather than a hard fail. A clean catch builds the combo; any fumble resets it.
    /// </summary>
    public sealed class KitchenFoodFrenzyMissionState
    {
        public enum FoodKind { Good, Bad }

        public enum DropResult { Dropped, WrongScout, AlreadyFalling, Complete }
        public enum CatchResult { Caught, GrossOut, UnsafeLanding, WrongCatcher, NothingFalling, Complete }
        public enum FallResult { DodgedBad, MissedGood, NothingFalling }

        public const int RequiredCatches = 4;

        public DogId ScoutDog => DogId.Cheddar;
        public DogId SweeperDog => DogId.Cocoa;

        public int GoodCatches { get; private set; }
        public int GrossFumbles { get; private set; }
        public int Misses { get; private set; }
        public int RoleFumbles { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }

        public bool DropActive { get; private set; }
        public FoodKind ActiveKind { get; private set; }

        public bool Complete => GoodCatches >= RequiredCatches;
        public int TotalFumbles => GrossFumbles + Misses + RoleFumbles;

        /// <summary>The scout (Cheddar) knocks a piece of food loose from the counter route.</summary>
        public DropResult Trigger(DogId dog, FoodKind kind)
        {
            if (Complete) return DropResult.Complete;
            if (DropActive) return DropResult.AlreadyFalling;
            if (dog != ScoutDog)
            {
                RoleFumbles++;
                return DropResult.WrongScout;
            }

            DropActive = true;
            ActiveKind = kind;
            return DropResult.Dropped;
        }

        /// <summary>
        /// The sweeper (Cocoa) tries to catch the falling drop. Only a good drop steered into the safe
        /// landing zone scores; bad food eaten is a gross-out, an unsafe landing splats, and the scout
        /// reaching for the floor catch is a role fumble that leaves the drop live to recover.
        /// </summary>
        public CatchResult Catch(DogId dog, bool intoSafeZone)
        {
            if (Complete) return CatchResult.Complete;
            if (!DropActive) return CatchResult.NothingFalling;

            if (dog != SweeperDog)
            {
                RoleFumbles++;
                ResetCombo();
                return CatchResult.WrongCatcher;
            }

            if (ActiveKind == FoodKind.Bad)
            {
                GrossFumbles++;
                ResetCombo();
                ConsumeDrop();
                return CatchResult.GrossOut;
            }

            if (!intoSafeZone)
            {
                Misses++;
                ResetCombo();
                ConsumeDrop();
                return CatchResult.UnsafeLanding;
            }

            GoodCatches++;
            Combo++;
            if (Combo > BestCombo) BestCombo = Combo;
            ConsumeDrop();
            return CatchResult.Caught;
        }

        /// <summary>The active drop reaches the floor uncaught: dodging bad food is correct, letting
        /// good food splat is a miss that breaks the combo.</summary>
        public FallResult LetFall()
        {
            if (!DropActive) return FallResult.NothingFalling;

            if (ActiveKind == FoodKind.Bad)
            {
                ConsumeDrop();
                return FallResult.DodgedBad;
            }

            Misses++;
            ResetCombo();
            ConsumeDrop();
            return FallResult.MissedGood;
        }

        public void Reset()
        {
            GoodCatches = 0;
            GrossFumbles = 0;
            Misses = 0;
            RoleFumbles = 0;
            Combo = 0;
            BestCombo = 0;
            DropActive = false;
            ActiveKind = FoodKind.Good;
        }

        private void ResetCombo() => Combo = 0;

        private void ConsumeDrop()
        {
            DropActive = false;
            ActiveKind = FoodKind.Good;
        }
    }
}
