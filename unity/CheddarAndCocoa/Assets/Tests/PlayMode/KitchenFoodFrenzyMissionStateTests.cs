using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using NUnit.Framework;

namespace CheddarAndCocoa.Tests.PlayMode
{
    public sealed class KitchenFoodFrenzyMissionStateTests
    {
        private static KitchenFoodFrenzyMissionState NewState() => new KitchenFoodFrenzyMissionState();

        [Test]
        public void CounterFloorDependency_OnlyScoutDropsAndOnlySweeperCatchesIntoSafeZone()
        {
            var state = NewState();

            // The floor sweeper cannot reach the counter to start a drop.
            Assert.AreEqual(KitchenFoodFrenzyMissionState.DropResult.WrongScout,
                state.Trigger(state.SweeperDog, KitchenFoodFrenzyMissionState.FoodKind.Good));
            Assert.AreEqual(1, state.RoleFumbles);
            Assert.IsFalse(state.DropActive);

            // The scout knocks good food loose.
            Assert.AreEqual(KitchenFoodFrenzyMissionState.DropResult.Dropped,
                state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Good));
            Assert.IsTrue(state.DropActive);

            // A second knock while a drop is live does nothing.
            Assert.AreEqual(KitchenFoodFrenzyMissionState.DropResult.AlreadyFalling,
                state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Good));

            // The scout diving for the floor catch is a role fumble, but the drop stays live to recover.
            Assert.AreEqual(KitchenFoodFrenzyMissionState.CatchResult.WrongCatcher,
                state.Catch(state.ScoutDog, true));
            Assert.AreEqual(2, state.RoleFumbles);
            Assert.IsTrue(state.DropActive, "A role fumble must leave the drop recoverable.");

            // The sweeper steers it into the safe zone for the score.
            Assert.AreEqual(KitchenFoodFrenzyMissionState.CatchResult.Caught,
                state.Catch(state.SweeperDog, true));
            Assert.AreEqual(1, state.GoodCatches);
            Assert.AreEqual(1, state.Combo);
            Assert.IsFalse(state.DropActive);
        }

        [Test]
        public void GoodCatchOutsideSafeZone_IsRecoverableSplatNotAScore()
        {
            var state = NewState();
            state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Good);

            Assert.AreEqual(KitchenFoodFrenzyMissionState.CatchResult.UnsafeLanding,
                state.Catch(state.SweeperDog, false));
            Assert.AreEqual(0, state.GoodCatches);
            Assert.AreEqual(1, state.Misses);
            Assert.AreEqual(0, state.Combo);
            Assert.IsFalse(state.DropActive);
        }

        [Test]
        public void BadFood_GrossOutWhenEaten_CorrectWhenDodged()
        {
            var state = NewState();

            // Catching bad food is a gross-out that breaks the combo.
            state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Bad);
            Assert.AreEqual(KitchenFoodFrenzyMissionState.CatchResult.GrossOut,
                state.Catch(state.SweeperDog, true));
            Assert.AreEqual(1, state.GrossFumbles);
            Assert.AreEqual(0, state.GoodCatches);

            // Letting bad food splat on the floor is the correct dodge.
            state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Bad);
            Assert.AreEqual(KitchenFoodFrenzyMissionState.FallResult.DodgedBad, state.LetFall());
            Assert.AreEqual(1, state.GrossFumbles, "Dodging bad food adds no fumble.");
            Assert.IsFalse(state.DropActive);
        }

        [Test]
        public void GoodFoodHittingFloor_IsAMissThatBreaksCombo()
        {
            var state = NewState();

            state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Good);
            Assert.AreEqual(KitchenFoodFrenzyMissionState.CatchResult.Caught, state.Catch(state.SweeperDog, true));
            Assert.AreEqual(1, state.Combo);

            state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Good);
            Assert.AreEqual(KitchenFoodFrenzyMissionState.FallResult.MissedGood, state.LetFall());
            Assert.AreEqual(1, state.Misses);
            Assert.AreEqual(0, state.Combo, "A missed good drop resets the combo.");
        }

        [Test]
        public void CleanCatches_BuildComboAndCompleteTheCourse()
        {
            var state = NewState();

            for (int i = 0; i < KitchenFoodFrenzyMissionState.RequiredCatches; i++)
            {
                Assert.IsFalse(state.Complete);
                state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Good);
                Assert.AreEqual(KitchenFoodFrenzyMissionState.CatchResult.Caught, state.Catch(state.SweeperDog, true));
            }

            Assert.IsTrue(state.Complete);
            Assert.AreEqual(KitchenFoodFrenzyMissionState.RequiredCatches, state.GoodCatches);
            Assert.AreEqual(KitchenFoodFrenzyMissionState.RequiredCatches, state.BestCombo);

            // Completed course rejects further triggers/catches.
            Assert.AreEqual(KitchenFoodFrenzyMissionState.DropResult.Complete,
                state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Good));
        }

        [Test]
        public void Reset_ClearsAllRoundState()
        {
            var state = NewState();
            state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Good);
            state.Catch(state.SweeperDog, true);
            state.Trigger(state.ScoutDog, KitchenFoodFrenzyMissionState.FoodKind.Bad);
            state.Catch(state.SweeperDog, true);

            state.Reset();

            Assert.AreEqual(0, state.GoodCatches);
            Assert.AreEqual(0, state.GrossFumbles);
            Assert.AreEqual(0, state.Misses);
            Assert.AreEqual(0, state.RoleFumbles);
            Assert.AreEqual(0, state.Combo);
            Assert.AreEqual(0, state.BestCombo);
            Assert.IsFalse(state.DropActive);
            Assert.IsFalse(state.Complete);
        }
    }
}
