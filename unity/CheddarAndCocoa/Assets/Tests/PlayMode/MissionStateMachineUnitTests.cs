using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Fast, scene-free guards on the per-mission state machines' boundary behavior, so the
    /// counters can't underflow/overflow or report clear/fail at the wrong thresholds.
    /// </summary>
    public sealed class MissionStateMachineUnitTests
    {
        [Test]
        public void CarryRoundup_PickupDeliverDropAccounting()
        {
            var s = new CarryRoundupMissionState();
            s.Configure(3);
            Assert.AreEqual(3, s.Loose);

            Assert.IsTrue(s.TryPickup());
            Assert.AreEqual(2, s.Loose);
            s.Deliver();
            Assert.AreEqual(1, s.Delivered);

            // A dropped weenie returns to the yard and counts as a fumble.
            s.Drop();
            Assert.AreEqual(3, s.Loose);
            Assert.AreEqual(1, s.Drops);

            // Cannot pick up past what's loose.
            Assert.IsTrue(s.TryPickup());
            Assert.IsTrue(s.TryPickup());
            Assert.IsTrue(s.TryPickup());
            Assert.IsFalse(s.TryPickup(), "No pickup when nothing is loose.");
            Assert.AreEqual(0, s.Loose);

            s.Reset();
            Assert.AreEqual(0, s.Loose);
            Assert.AreEqual(0, s.Delivered);
            Assert.AreEqual(0, s.Drops);
        }

        [Test]
        public void ScentSearch_FindAndWastedThresholds()
        {
            var s = new ScentSearchMissionState();
            s.Reset();
            Assert.IsFalse(s.ReadyToClear(3));
            s.AddFind();
            s.AddFind();
            Assert.IsFalse(s.ReadyToClear(3));
            s.AddFind();
            Assert.IsTrue(s.ReadyToClear(3));

            Assert.IsFalse(s.TooManyWastedDigs(4));
            for (int i = 0; i < 4; i++) s.AddWastedDig();
            Assert.IsTrue(s.TooManyWastedDigs(4));
        }

        [Test]
        public void Territory_ClaimUnclaimClampsAndReportsAllClaimed()
        {
            var s = new TerritoryMissionState();
            s.Configure(2);
            Assert.IsFalse(s.AllClaimed);

            s.Unclaim(); // nothing claimed yet -> no underflow, no reclaim
            Assert.AreEqual(0, s.Claimed);
            Assert.AreEqual(0, s.Reclaims);

            s.Claim();
            s.Claim();
            s.Claim(); // cannot exceed zone count
            Assert.AreEqual(2, s.Claimed);
            Assert.IsTrue(s.AllClaimed);

            s.Unclaim();
            Assert.AreEqual(1, s.Claimed);
            Assert.AreEqual(1, s.Reclaims);
            Assert.IsFalse(s.AllClaimed);
        }

        [Test]
        public void Thunderstorm_SurvivesUpToRequiredClaps()
        {
            var s = new ThunderstormMissionState();
            s.Configure(3);
            Assert.IsFalse(s.ReadyToClear());
            s.SurviveClap();
            s.SurviveClap();
            Assert.IsFalse(s.ReadyToClear());
            s.SurviveClap();
            Assert.IsTrue(s.ReadyToClear());

            s.Reset();
            Assert.AreEqual(0, s.ClapsSurvived);
            Assert.IsFalse(s.ReadyToClear());
        }
    }
}
