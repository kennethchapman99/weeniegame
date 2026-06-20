using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the bait-and-switch (readable-deception) beat: a strike lands only while
    /// the enemy is committed to the decoy, under-baiting and over-baiting both deny the strike, pushing
    /// commitment to full backfires once and resets the window, and enough committed hits solve it.
    /// </summary>
    public sealed class CoopBaitSwitchPuzzleUnitTests
    {
        private static CoopBaitSwitchPuzzle Make(
            float threshold = 0.6f, float commitRate = 1f, float decayRate = 1f,
            float overbaitTolerance = 0.5f, int need = 3, int maxBackfires = 3)
        {
            var p = new CoopBaitSwitchPuzzle();
            p.Configure(threshold, commitRate, decayRate, overbaitTolerance, need, maxBackfires);
            return p;
        }

        [Test]
        public void Committed_StrikeLands()
        {
            var p = Make();
            p.Advance(0.7f, baiting: true); // commitment 0.7 -> in band [0.6, 1)
            Assert.IsTrue(p.Committed);
            p.Strike();
            Assert.AreEqual(1, p.Hits);
            Assert.AreEqual(0, p.Whiffs);
        }

        [Test]
        public void UnderBaited_StrikeWhiffs()
        {
            var p = Make();
            p.Advance(0.4f, baiting: true); // 0.4 < threshold -> enemy still guarding
            Assert.IsFalse(p.Committed);
            p.Strike();
            Assert.AreEqual(0, p.Hits);
            Assert.AreEqual(1, p.Whiffs);
        }

        [Test]
        public void OverBaiting_BackfiresOnceAndResetsCommitment()
        {
            var p = Make();
            p.Advance(1f, baiting: true); // commitment reaches 1 -> backfire, snaps to 0
            Assert.AreEqual(1, p.Backfires);
            Assert.AreEqual(0f, p.Commitment, 1e-4);
            Assert.IsFalse(p.Committed);
            Assert.IsFalse(p.Overbaited);
        }

        [Test]
        public void OverBaited_StrikeWhiffs()
        {
            var p = Make();
            p.Advance(0.7f, baiting: true);     // committed
            p.Advance(0.5f, baiting: true);     // pushes to 1 -> backfire -> commitment 0
            Assert.IsFalse(p.Committed);
            p.Strike();                          // window already slammed shut
            Assert.AreEqual(0, p.Hits);
            Assert.AreEqual(1, p.Whiffs);
        }

        [Test]
        public void EasingOff_KeepsWindowOpenThenDecaysBelowThreshold()
        {
            var p = Make(threshold: 0.6f, commitRate: 1f, decayRate: 1f);
            p.Advance(0.7f, baiting: true);  // 0.7 committed
            p.Advance(0.05f, baiting: false); // ease off -> 0.65 still committed
            Assert.IsTrue(p.Committed);
            p.Strike();
            Assert.AreEqual(1, p.Hits);
            p.Advance(0.2f, baiting: false); // decays to ~0.45 -> guarding again
            Assert.IsFalse(p.Committed);
        }

        [Test]
        public void EnoughCommittedHits_Solve()
        {
            var p = Make(need: 3);
            for (int i = 0; i < 3; i++)
            {
                p.Advance(0.7f, baiting: true);  // re-commit the enemy to the decoy
                p.Strike();                       // land inside the window
                p.Advance(2f, baiting: false);    // ease fully off before the next rep (avoid over-bait)
            }
            Assert.IsTrue(p.Solved);
            Assert.AreEqual(3, p.Hits);
        }

        [Test]
        public void FeatheringAtFull_DoesNotBackfireIfEasedWithinTolerance()
        {
            var p = Make(commitRate: 1f, decayRate: 1f, overbaitTolerance: 0.5f);
            p.Advance(0.7f, baiting: true);  // 0.7 committed
            p.Advance(0.4f, baiting: true);  // clamps to full; overbait timer 0.4 < tolerance
            Assert.IsTrue(p.Overbaited);
            Assert.AreEqual(0, p.Backfires);
            p.Advance(0.3f, baiting: false); // eased off in time -> timer resets, still committed
            Assert.AreEqual(0, p.Backfires);
            Assert.IsTrue(p.Committed);
            p.Strike();
            Assert.AreEqual(1, p.Hits);
        }

        [Test]
        public void TooManyBackfires_IsReported()
        {
            var p = Make(maxBackfires: 2);
            p.Advance(1f, baiting: true); // backfire 1
            Assert.IsFalse(p.TooManyBackfires);
            p.Advance(1f, baiting: true); // backfire 2
            Assert.IsTrue(p.TooManyBackfires);
        }

        [Test]
        public void SolvedStrike_IsIgnored()
        {
            var p = Make(need: 1);
            p.Advance(0.7f, baiting: true);
            p.Strike(); // solves
            Assert.IsTrue(p.Solved);
            p.Advance(0.7f, baiting: true);
            p.Strike(); // ignored once solved
            Assert.AreEqual(1, p.Hits);
            Assert.AreEqual(0, p.Whiffs);
        }

        [Test]
        public void ResetClearsEverything()
        {
            var p = Make();
            p.Advance(1f, baiting: true); // backfire
            p.Advance(0.7f, baiting: true);
            p.Strike();                   // hit
            p.Reset();
            Assert.AreEqual(0f, p.Commitment, 1e-4);
            Assert.AreEqual(0, p.Hits);
            Assert.AreEqual(0, p.Whiffs);
            Assert.AreEqual(0, p.Backfires);
            Assert.IsFalse(p.Solved);
        }
    }
}
