using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the stretch-span (long-dog bridge / blanket) beat: catching needs the
    /// span taut AND centered, slack/over-stretched/off-center all miss, and over-stretching rips once
    /// per event.
    /// </summary>
    public sealed class CoopStretchSpanPuzzleUnitTests
    {
        private static CoopStretchSpanPuzzle Make(
            float min = 1.5f, float max = 5f, float tol = 1.5f, int need = 4, int maxRips = 3)
        {
            var p = new CoopStretchSpanPuzzle();
            p.Configure(min, max, tol, need, maxRips);
            return p;
        }

        [Test]
        public void TautAndCentered_Catches()
        {
            var p = Make();
            p.UpdateSpan(separation: 3f, midpointX: 2f); // in band
            Assert.IsTrue(p.Taut);
            p.TryCatch(itemX: 2.5f); // within tolerance of midpoint
            Assert.AreEqual(1, p.Caught);
            Assert.AreEqual(0, p.Missed);
        }

        [Test]
        public void TautButOffCentre_Misses()
        {
            var p = Make();
            p.UpdateSpan(3f, 0f);
            p.TryCatch(itemX: 10f); // far from midpoint
            Assert.AreEqual(0, p.Caught);
            Assert.AreEqual(1, p.Missed);
        }

        [Test]
        public void Slack_CannotCatch()
        {
            var p = Make(min: 1.5f);
            p.UpdateSpan(separation: 0.5f, midpointX: 2f); // too close -> slack
            Assert.IsTrue(p.Slack);
            Assert.IsFalse(p.Taut);
            p.TryCatch(2f);
            Assert.AreEqual(0, p.Caught);
            Assert.AreEqual(1, p.Missed);
        }

        [Test]
        public void OverStretching_RipsOncePerEvent()
        {
            var p = Make(max: 5f);
            p.UpdateSpan(7f, 0f);  // over -> rip 1
            Assert.AreEqual(1, p.Rips);
            p.UpdateSpan(8f, 0f);  // still over -> no new rip
            Assert.AreEqual(1, p.Rips);
            p.UpdateSpan(3f, 0f);  // back in band
            p.UpdateSpan(9f, 0f);  // over again -> rip 2
            Assert.AreEqual(2, p.Rips);
        }

        [Test]
        public void OverStretchedSpan_CannotCatch()
        {
            var p = Make(max: 5f);
            p.UpdateSpan(7f, 2f);
            Assert.IsTrue(p.Overstretched);
            p.TryCatch(2f);
            Assert.AreEqual(0, p.Caught);
            Assert.AreEqual(1, p.Missed);
        }

        [Test]
        public void EnoughTautCentredCatches_Solve()
        {
            var p = Make(need: 3);
            for (int i = 0; i < 3; i++)
            {
                p.UpdateSpan(3f, i); // taut, midpoint at i
                p.TryCatch(i);       // centered
            }
            Assert.IsTrue(p.Solved);
            Assert.AreEqual(0, p.Missed);
            Assert.AreEqual(0, p.Rips);
        }

        [Test]
        public void TooManyRips_IsReported()
        {
            var p = Make(max: 5f, maxRips: 2);
            p.UpdateSpan(7f, 0f); // rip 1
            p.UpdateSpan(3f, 0f);
            Assert.IsFalse(p.TooManyRips);
            p.UpdateSpan(7f, 0f); // rip 2
            Assert.IsTrue(p.TooManyRips);
        }

        [Test]
        public void ResetClearsEverything()
        {
            var p = Make();
            p.UpdateSpan(7f, 1f); // rip
            p.UpdateSpan(3f, 1f);
            p.TryCatch(1f);       // catch
            p.Reset();
            Assert.AreEqual(0, p.Caught);
            Assert.AreEqual(0, p.Missed);
            Assert.AreEqual(0, p.Rips);
            Assert.IsFalse(p.Solved);
        }
    }
}
