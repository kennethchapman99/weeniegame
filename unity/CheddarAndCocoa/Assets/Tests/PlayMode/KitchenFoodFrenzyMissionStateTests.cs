using CheddarAndCocoa.Game;
using NUnit.Framework;
using State = CheddarAndCocoa.Game.KitchenFoodFrenzyMissionState;

namespace CheddarAndCocoa.Tests.PlayMode
{
    /// <summary>
    /// Deterministic coverage for the Kitchen Falling Food Frenzy core: scout/sweeper hand-off,
    /// combo scoring, danger filtering, teamwork nudges, and clear/fail conditions.
    /// </summary>
    public sealed class KitchenFoodFrenzyMissionStateTests
    {
        private static State NewRound(int target = 30, int maxStrikes = 3)
        {
            var state = new State();
            state.Configure(target, maxStrikes, baseValue: 5, comboStep: 2, saveBonus: 2);
            return state;
        }

        private static State.Resolution CatchDrop(State state, State.Food food)
        {
            Assert.IsTrue(state.TriggerDrop(food), "Scout should be able to tip an item when nothing is in flight.");
            return state.Catch();
        }

        [Test]
        public void OnlyOneItemInFlightAtATime()
        {
            var state = NewRound();

            Assert.IsTrue(state.TriggerDrop(State.Food.Good));
            Assert.IsFalse(state.TriggerDrop(State.Food.Bad), "Second drop should be refused until the first resolves.");

            Assert.AreEqual(State.Resolution.Yum, state.Catch());
            Assert.IsNull(state.InFlight);
            Assert.IsTrue(state.TriggerDrop(State.Food.Bad), "Drop allowed again once the line is clear.");
        }

        [Test]
        public void ResolvingWithNothingInFlightIsANoOp()
        {
            var state = NewRound();

            Assert.AreEqual(State.Resolution.None, state.Catch());
            Assert.AreEqual(State.Resolution.None, state.LetFall());
            Assert.AreEqual(State.Resolution.None, state.Nudge());
            Assert.AreEqual(0, state.Score);
            Assert.AreEqual(0, state.Strikes);
        }

        [Test]
        public void ComboScalingClearsTheRound()
        {
            var state = NewRound(target: 30);

            // 5, 7, 9, 11 -> 32 total across a 4-catch streak.
            Assert.AreEqual(State.Resolution.Yum, CatchDrop(state, State.Food.Good));
            Assert.AreEqual(5, state.Score);
            Assert.AreEqual(1, state.Combo);

            CatchDrop(state, State.Food.Good);
            Assert.AreEqual(12, state.Score);
            CatchDrop(state, State.Food.Good);
            Assert.AreEqual(21, state.Score);
            CatchDrop(state, State.Food.Good);

            Assert.AreEqual(32, state.Score);
            Assert.AreEqual(4, state.GoodCaught);
            Assert.AreEqual(4, state.BestCombo);
            Assert.IsTrue(state.Cleared);
            Assert.IsTrue(state.RoundOver);
        }

        [Test]
        public void CatchingDangerousFoodStrikesAndBreaksCombo()
        {
            var state = NewRound();

            CatchDrop(state, State.Food.Good);
            CatchDrop(state, State.Food.Good);
            Assert.AreEqual(2, state.Combo);

            Assert.AreEqual(State.Resolution.Yuck, CatchDrop(state, State.Food.Bad));
            Assert.AreEqual(1, state.Strikes);
            Assert.AreEqual(0, state.Combo);

            Assert.AreEqual(State.Resolution.Burn, CatchDrop(state, State.Food.Hot));
            Assert.AreEqual(2, state.Strikes);
            Assert.IsFalse(state.Failed);
            Assert.AreEqual(2, state.BestCombo, "Best combo should remember the earlier streak.");
        }

        [Test]
        public void ThreeBadBitesFailTheRound()
        {
            var state = NewRound();

            CatchDrop(state, State.Food.Bad);
            CatchDrop(state, State.Food.Hot);
            CatchDrop(state, State.Food.Bad);

            Assert.AreEqual(3, state.Strikes);
            Assert.IsTrue(state.Failed);
            Assert.IsTrue(state.RoundOver);
        }

        [Test]
        public void LettingDangerFallIsSafeButLosingGoodFoodIsAMiss()
        {
            var state = NewRound();

            Assert.IsTrue(state.TriggerDrop(State.Food.Hot));
            Assert.AreEqual(State.Resolution.FloorSafe, state.LetFall());
            Assert.AreEqual(0, state.Strikes);

            // Build a streak, then drop a good one on the floor.
            CatchDrop(state, State.Food.Good);
            Assert.AreEqual(1, state.Combo);

            Assert.IsTrue(state.TriggerDrop(State.Food.Good));
            Assert.AreEqual(State.Resolution.Missed, state.LetFall());
            Assert.AreEqual(1, state.Misses);
            Assert.AreEqual(0, state.Combo, "A dropped treat breaks the combo.");
        }

        [Test]
        public void NudgeSavesDangerForTeamworkButWastesGoodFood()
        {
            var state = NewRound();

            Assert.IsTrue(state.TriggerDrop(State.Food.Bad));
            Assert.AreEqual(State.Resolution.Saved, state.Nudge());
            Assert.AreEqual(1, state.Saves);
            Assert.AreEqual(0, state.Strikes);
            Assert.AreEqual(2, state.Score, "Teamwork save grants the save bonus.");

            Assert.IsTrue(state.TriggerDrop(State.Food.Good));
            Assert.AreEqual(State.Resolution.Missed, state.Nudge());
            Assert.AreEqual(1, state.Misses);
        }

        [Test]
        public void ActionsAfterRoundOverAreIgnored()
        {
            var state = NewRound(maxStrikes: 1);

            CatchDrop(state, State.Food.Bad);
            Assert.IsTrue(state.Failed);

            Assert.IsFalse(state.TriggerDrop(State.Food.Good), "No drops once the round is over.");
            Assert.AreEqual(State.Resolution.None, state.Catch());
            Assert.AreEqual(1, state.Strikes);
            Assert.AreEqual(0, state.Score);
        }

        [Test]
        public void ConfigureClampsAndResetClearsRoundState()
        {
            var state = NewRound();
            CatchDrop(state, State.Food.Good);
            CatchDrop(state, State.Food.Bad);
            state.TriggerDrop(State.Food.Good);

            state.Reset();
            Assert.AreEqual(0, state.Score);
            Assert.AreEqual(0, state.Combo);
            Assert.AreEqual(0, state.BestCombo);
            Assert.AreEqual(0, state.GoodCaught);
            Assert.AreEqual(0, state.Strikes);
            Assert.AreEqual(0, state.Saves);
            Assert.AreEqual(0, state.Misses);
            Assert.IsNull(state.InFlight);

            // Degenerate config is clamped so the round stays winnable and crash-free.
            state.Configure(targetScore: 0, maxStrikes: 0, baseValue: 0, comboStep: -3, saveBonus: -1);
            Assert.AreEqual(1, state.TargetScore);
            Assert.AreEqual(1, state.MaxStrikes);
            Assert.AreEqual(1, state.BaseValue);
            Assert.AreEqual(0, state.ComboStep);
            Assert.AreEqual(0, state.SaveBonus);

            Assert.AreEqual(State.Resolution.Yum, CatchDrop(state, State.Food.Good));
            Assert.IsTrue(state.Cleared, "Clamped 1-point target clears on the first catch.");
        }
    }
}
