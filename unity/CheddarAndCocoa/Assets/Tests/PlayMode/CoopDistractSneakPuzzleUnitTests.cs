using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the Distract-and-Sneak primitive: progress needs both dogs cooperating,
    /// checkpoints bank progress, and both over- and under-distracting get the sneaker spotted (only
    /// back to the last checkpoint).
    /// </summary>
    public sealed class CoopDistractSneakPuzzleUnitTests
    {
        private static CoopDistractSneakPuzzle Make(int segments = 3, float segmentTime = 0.5f)
        {
            var p = new CoopDistractSneakPuzzle();
            p.Configure(segments, segmentTime);
            return p;
        }

        [Test]
        public void NoProgressWithoutBothDistractingAndSneaking()
        {
            var p = Make();
            p.Advance(0.5f, distracting: false, sneaking: true);
            Assert.AreEqual(0, p.Segment);
            Assert.AreEqual(0f, p.SegmentProgress);

            p.Advance(0.5f, distracting: true, sneaking: false);
            Assert.AreEqual(0f, p.SegmentProgress, "Distractor alone doesn't move the sneaker.");
        }

        [Test]
        public void DistractAndSneakInBursts_BanksCheckpointsToSolve()
        {
            var p = Make(segments: 3, segmentTime: 0.5f);
            for (int i = 0; i < 3 && !p.Solved; i++)
            {
                p.Advance(0.5f, distracting: true, sneaking: true); // bank one checkpoint
                if (!p.Solved) p.Advance(0.6f, distracting: false, sneaking: false); // rest sheds annoyance (safe at checkpoint)
            }

            Assert.IsTrue(p.Solved);
            Assert.AreEqual(0, p.Spotted, "A clean burst-and-rest rhythm is never spotted.");
        }

        [Test]
        public void RestingAtACheckpointIsSafeEvenIfWatchfulnessMaxes()
        {
            var p = Make(segments: 3, segmentTime: 0.5f);
            p.Advance(0.5f, true, true); // bank checkpoint 1 -> SegmentProgress 0 (safe)
            Assert.AreEqual(1, p.Segment);

            // Stop distracting for a long time: watchfulness maxes, but the sneaker is at a checkpoint.
            p.Advance(2f, false, false);
            Assert.IsTrue(p.Watchfulness >= 1f);
            Assert.AreEqual(0, p.Spotted, "Being at a checkpoint is safe even when the enemy fully looks back.");
            Assert.AreEqual(1, p.Segment);
        }

        [Test]
        public void OverDistracting_BuildsAnnoyanceAndSpotsTheExposedSneaker()
        {
            var p = Make(segments: 3, segmentTime: 2f); // long segment so it stays exposed
            p.Advance(0.5f, true, true); // progress 0.5 (exposed), annoyance 0.5
            Assert.Greater(p.SegmentProgress, 0f);
            Assert.AreEqual(0, p.Spotted);

            p.Advance(0.5f, true, true); // annoyance hits 1.0 while exposed -> spotted
            Assert.AreEqual(1, p.Spotted);
            Assert.AreEqual(0f, p.SegmentProgress, "Spotted knocks back to the last checkpoint.");
            Assert.AreEqual(0, p.Segment);
        }

        [Test]
        public void UnderDistracting_LetsWatchfulnessSpotTheExposedSneaker()
        {
            var p = Make(segments: 3, segmentTime: 2f);
            p.Advance(0.5f, true, true);   // progress 0.5 (exposed)
            p.Advance(0.5f, false, false); // watchfulness 0.5
            Assert.AreEqual(0, p.Spotted);
            p.Advance(0.6f, false, false); // watchfulness > 1 while exposed -> spotted
            Assert.AreEqual(1, p.Spotted);
            Assert.AreEqual(0f, p.SegmentProgress);
        }

        [Test]
        public void SpotKnocksBackOnlyToLastCheckpoint_NotToStart()
        {
            var p = Make(segments: 3, segmentTime: 0.5f);
            p.Advance(0.5f, true, true); // bank checkpoint 1
            Assert.AreEqual(1, p.Segment);

            // Start segment 2, then get spotted by over-distraction.
            var q = p; // same instance
            q.Advance(0.4f, true, true);  // segment-2 progress (exposed), annoyance up
            q.Advance(1.0f, true, true);  // annoyance maxes while exposed -> spotted
            Assert.AreEqual(1, q.Spotted);
            Assert.AreEqual(1, q.Segment, "Still keeps the first banked checkpoint after a spot.");
        }
    }
}
