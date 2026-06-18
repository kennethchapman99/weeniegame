using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class MissionStatePlayModeTests
    {
        [Test]
        public void HerdingMissionState_TracksRouteControlStashAndReset()
        {
            var state = new HerdingMissionState();
            state.AdvanceRoute(3);
            state.AddHerd();
            state.AddCutoff();
            state.AddFakeOut();
            state.AddTaunt();

            Assert.AreEqual(1, state.RouteIndex);
            Assert.AreEqual(2, state.ControlCount);
            Assert.IsTrue(state.ReadyForStash(2));
            Assert.IsTrue(state.TooManyTaunts(1));

            state.FindStash();
            Assert.IsTrue(state.StashRevealed);
            Assert.IsTrue(state.StashFound);

            state.Reset();
            Assert.AreEqual(0, state.RouteIndex);
            Assert.AreEqual(0, state.ControlCount);
            Assert.IsFalse(state.StashFound);
        }

        [Test]
        public void ThreatSweepMissionState_TracksHideRescueUnitedFrontAndReset()
        {
            var state = new ThreatSweepMissionState();
            state.AdvanceSweep(2);
            state.AddSafeHide();
            state.AddSafeHide();
            state.AddExposure();

            Assert.AreEqual(1, state.SweepIndex);
            Assert.IsTrue(state.ReadyForRescue(2));
            Assert.IsTrue(state.TooManyExposures(1));

            state.StartRescue();
            state.CompleteRescue();
            Assert.IsTrue(state.ReadyForUnitedFront);

            state.CompleteUnitedFront();
            Assert.IsFalse(state.ReadyForUnitedFront);

            state.Reset();
            Assert.AreEqual(0, state.SafeHides);
            Assert.IsFalse(state.RescueComplete);
        }

        [Test]
        public void PatrolDefenseMissionState_TracksRepairsPressureFakeSnackAndReset()
        {
            var state = new PatrolDefenseMissionState();
            state.SelectGap(2);
            state.AddRepair();
            state.AddRepair();
            state.AddBarkPressure();
            state.StartFakeSnack();

            Assert.AreEqual(2, state.ActiveGapIndex);
            Assert.AreEqual(2, state.GapsRepaired);
            Assert.AreEqual(1, state.BarkPressures);
            Assert.IsTrue(state.FakeSnackActive);
            Assert.IsTrue(state.ReadyForFinalPressure(2));

            state.ResolveFakeSnack();
            state.CompleteFinalPressure();
            Assert.IsFalse(state.FakeSnackActive);
            Assert.IsFalse(state.ReadyForFinalPressure(2));

            state.AddBreach();
            Assert.IsTrue(state.TooManyBreaches(1));

            state.Reset();
            Assert.AreEqual(0, state.GapsRepaired);
            Assert.IsFalse(state.FinalPressureComplete);
        }
    }
}
