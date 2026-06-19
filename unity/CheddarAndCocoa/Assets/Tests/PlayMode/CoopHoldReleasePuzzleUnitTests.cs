using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic, scene-free guards for the Hold-and-Release co-op puzzle primitive: progress is
    /// gated on the anchor's hold, the hold window is a real time pressure, and letting go mid-cross
    /// snaps it back rather than silently failing.
    /// </summary>
    public sealed class CoopHoldReleasePuzzleUnitTests
    {
        private static CoopHoldReleasePuzzle Make(float cross = 1f, float window = 5f)
        {
            var p = new CoopHoldReleasePuzzle();
            p.Configure(cross, window);
            return p;
        }

        [Test]
        public void CrossingOnlyProgressesWhileTheAnchorHolds()
        {
            var p = Make();
            // Crosser tries to advance with no anchor holding: nothing happens (the co-op lock).
            p.Advance(0.5f);
            Assert.AreEqual(0f, p.CrossProgress);
            Assert.IsFalse(p.Solved);

            p.SetHeld(true);
            p.Advance(0.5f);
            Assert.Greater(p.CrossProgress, 0f, "With the anchor holding, the crosser makes progress.");
        }

        [Test]
        public void CompletingTheCrossWhileHeldSolvesTheBeat()
        {
            var p = Make(cross: 1f, window: 5f);
            p.SetHeld(true);
            p.Advance(0.6f);
            Assert.IsFalse(p.Solved);
            p.Advance(0.6f); // total 1.2 >= cross 1.0
            Assert.IsTrue(p.Solved);
            Assert.AreEqual(0, p.Snaps);
            Assert.AreEqual(1f, p.CrossRatio, 0.0001f);
        }

        [Test]
        public void ReleasingMidCrossSnapsBackAndResetsProgress()
        {
            var p = Make();
            p.SetHeld(true);
            p.Advance(0.5f);
            Assert.Greater(p.CrossProgress, 0f);

            p.SetHeld(false); // anchor lets go mid-cross -> snap
            Assert.AreEqual(1, p.Snaps);
            Assert.AreEqual(0f, p.CrossProgress, "A snap resets the crossing.");
            Assert.IsFalse(p.Held);
            Assert.IsFalse(p.Solved);
        }

        [Test]
        public void ReleasingBeforeAnyProgressDoesNotSnap()
        {
            var p = Make();
            p.SetHeld(true);
            p.SetHeld(false); // grabbed then let go before the crosser moved
            Assert.AreEqual(0, p.Snaps, "Letting go before any cross progress is not a snap.");
        }

        [Test]
        public void HoldWindowRunningOutSnapsBeforeTheCrossFinishes()
        {
            var p = Make(cross: 5f, window: 1f); // window shorter than the cross needs
            p.SetHeld(true);
            p.Advance(1.5f); // window (1.0) drains first
            Assert.AreEqual(1, p.Snaps, "An expired hold window snaps the beat.");
            Assert.IsFalse(p.Solved);
            Assert.AreEqual(0f, p.CrossProgress);
            Assert.IsFalse(p.Held);
        }

        [Test]
        public void ReGrabbingAfterAManualReleaseRechargesTheWindowToFinish()
        {
            // Window (2.0) >= cross (1.0) so the beat IS solvable in one clean hold; the first attempt
            // fails only because the anchor lets go mid-cross.
            var p = Make(cross: 1f, window: 2f);
            p.SetHeld(true);
            p.Advance(0.5f); // progress 0.5, not solved
            Assert.IsFalse(p.Solved);

            p.SetHeld(false); // anchor bails mid-cross -> snap, progress lost
            Assert.AreEqual(1, p.Snaps);
            Assert.AreEqual(0f, p.CrossProgress);

            // Re-grab gives a fresh patience window; a clean hold now finishes.
            p.SetHeld(true);
            p.Advance(0.6f);
            p.Advance(0.6f); // 1.2 >= 1.0
            Assert.IsTrue(p.Solved);
            Assert.AreEqual(1, p.Snaps, "Only the one mid-cross release snapped.");
        }

        [Test]
        public void SolvedBeatIsTerminal()
        {
            var p = Make(cross: 0.5f, window: 5f);
            p.SetHeld(true);
            p.Advance(0.6f);
            Assert.IsTrue(p.Solved);

            p.SetHeld(false);
            p.Advance(1f);
            Assert.IsTrue(p.Solved, "Solved stays solved.");
            Assert.AreEqual(0, p.Snaps, "Releasing after solving is not a snap.");
        }

        [Test]
        public void ResetClearsEverything()
        {
            var p = Make();
            p.SetHeld(true);
            p.Advance(0.4f);
            p.SetHeld(false); // snap
            Assert.AreEqual(1, p.Snaps);

            p.Reset();
            Assert.IsFalse(p.Held);
            Assert.IsFalse(p.Solved);
            Assert.AreEqual(0, p.Snaps);
            Assert.AreEqual(0f, p.CrossProgress);
            Assert.AreEqual(0f, p.HoldRemaining);
        }
    }
}
