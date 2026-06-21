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

        public enum TelegraphResult { Armed, WrongScout, Busy, Complete }
        public enum ReleaseResult { Dropped, NothingArmed, AlreadyFalling, Complete }
        public enum DropResult { Dropped, WrongScout, AlreadyFalling, Complete }
        public enum CatchResult { Caught, GrossOut, UnsafeLanding, WrongCatcher, NothingFalling, Complete }
        public enum FallResult { DodgedBad, MissedGood, NothingFalling }

        public const int WarmupCatches = 3;
        public const int FinaleSuccessesRequired = 3;
        // The three-beat finale is GOOD / BAD / GOOD, so a clean clear contains five catches.
        public const int RequiredCatches = WarmupCatches + 2;

        public DogId ScoutDog => DogId.Cheddar;
        public DogId SweeperDog => DogId.Cocoa;

        public int GoodCatches { get; private set; }
        public int GrossFumbles { get; private set; }
        public int Misses { get; private set; }
        public int RoleFumbles { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int FinaleSuccesses { get; private set; }

        public bool TelegraphActive { get; private set; }
        public FoodKind PendingKind { get; private set; }
        public bool DropActive { get; private set; }
        public FoodKind ActiveKind { get; private set; }

        public bool FinaleActive => GoodCatches >= WarmupCatches && !Complete;
        public FoodKind ExpectedFinaleKind => FinaleSuccesses == 1 ? FoodKind.Bad : FoodKind.Good;
        public bool Complete => GoodCatches >= WarmupCatches && FinaleSuccesses >= FinaleSuccessesRequired;
        public int TotalFumbles => GrossFumbles + Misses + RoleFumbles;

        /// <summary>Cheddar's bark warns Cocoa which lane and food type will drop next.</summary>
        public TelegraphResult ArmTelegraph(DogId dog, FoodKind kind)
        {
            if (Complete) return TelegraphResult.Complete;
            if (TelegraphActive || DropActive) return TelegraphResult.Busy;
            if (dog != ScoutDog)
            {
                RoleFumbles++;
                return TelegraphResult.WrongScout;
            }

            TelegraphActive = true;
            PendingKind = FinaleActive ? ExpectedFinaleKind : kind;
            return TelegraphResult.Armed;
        }

        public ReleaseResult ReleaseTelegraph()
        {
            if (Complete) return ReleaseResult.Complete;
            if (DropActive) return ReleaseResult.AlreadyFalling;
            if (!TelegraphActive) return ReleaseResult.NothingArmed;

            TelegraphActive = false;
            DropActive = true;
            ActiveKind = PendingKind;
            return ReleaseResult.Dropped;
        }

        /// <summary>The scout (Cheddar) knocks a piece of food loose from the counter route.</summary>
        public DropResult Trigger(DogId dog, FoodKind kind)
        {
            TelegraphResult armed = ArmTelegraph(dog, kind);
            if (armed == TelegraphResult.Complete) return DropResult.Complete;
            if (armed == TelegraphResult.Busy) return DropResult.AlreadyFalling;
            if (armed == TelegraphResult.WrongScout)
            {
                return DropResult.WrongScout;
            }
            ReleaseTelegraph();
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

            bool wasFinale = FinaleActive;
            GoodCatches++;
            Combo++;
            if (Combo > BestCombo) BestCombo = Combo;
            if (wasFinale) FinaleSuccesses++;
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
                if (FinaleActive) FinaleSuccesses++;
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
            FinaleSuccesses = 0;
            TelegraphActive = false;
            PendingKind = FoodKind.Good;
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
