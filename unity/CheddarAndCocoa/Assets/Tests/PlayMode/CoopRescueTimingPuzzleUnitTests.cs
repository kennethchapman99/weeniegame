using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the Rescue timing primitive: only the held dog's wiggle opens a
    /// window, only a pull inside that window counts, mistimed pulls miss (recoverably), and enough
    /// well-timed pulls free the dog.
    /// </summary>
    public sealed class CoopRescueTimingPuzzleUnitTests
    {
        private static CoopRescueTimingPuzzle Make(int pulls = 3, float window = 1f)
        {
            var p = new CoopRescueTimingPuzzle();
            p.Configure(pulls, window);
            return p;
        }

        [Test]
        public void PullWithoutAnOpenWindow_Misses()
        {
            var p = Make();
            Assert.IsFalse(p.WindowOpen);
            p.Pull();
            Assert.AreEqual(0, p.Pulls);
            Assert.AreEqual(1, p.MissedPulls);
        }

        [Test]
        public void WiggleThenPull_LandsAGoodRescuePull()
        {
            var p = Make();
            p.Wiggle();
            Assert.IsTrue(p.WindowOpen);
            p.Pull();
            Assert.AreEqual(1, p.Pulls);
            Assert.AreEqual(0, p.MissedPulls);
            Assert.IsFalse(p.WindowOpen, "A good pull spends the window.");
        }

        [Test]
        public void PullAfterTheWindowCloses_Misses()
        {
            var p = Make(window: 1f);
            p.Wiggle();
            p.Advance(1.5f); // window elapses before the pull
            Assert.IsFalse(p.WindowOpen);
            p.Pull();
            Assert.AreEqual(0, p.Pulls);
            Assert.AreEqual(1, p.MissedPulls);
        }

        [Test]
        public void EnoughWellTimedPulls_FreeTheDog()
        {
            var p = Make(pulls: 3, window: 1f);
            for (int i = 0; i < 3; i++)
            {
                p.Wiggle();
                p.Advance(0.3f); // still within the window
                p.Pull();
            }

            Assert.IsTrue(p.Freed);
            Assert.AreEqual(3, p.Pulls);
            Assert.AreEqual(0, p.MissedPulls);
        }

        [Test]
        public void OneWiggleAllowsOnlyOneGoodPull()
        {
            var p = Make(pulls: 3);
            p.Wiggle();
            p.Pull(); // good
            p.Pull(); // window already spent -> miss
            Assert.AreEqual(1, p.Pulls);
            Assert.AreEqual(1, p.MissedPulls);
        }

        [Test]
        public void FreedIsTerminalAndIgnoresFurtherInput()
        {
            var p = Make(pulls: 1);
            p.Wiggle();
            p.Pull();
            Assert.IsTrue(p.Freed);

            p.Wiggle();
            Assert.IsFalse(p.WindowOpen, "Wiggle does nothing once freed.");
            p.Pull();
            Assert.AreEqual(1, p.Pulls);
            Assert.AreEqual(0, p.MissedPulls, "Pulls after freeing are ignored, not misses.");
        }

        [Test]
        public void ResetClearsEverything()
        {
            var p = Make();
            p.Wiggle();
            p.Pull();
            p.Pull(); // a miss
            p.Reset();
            Assert.AreEqual(0, p.Pulls);
            Assert.AreEqual(0, p.MissedPulls);
            Assert.IsFalse(p.WindowOpen);
            Assert.IsFalse(p.Freed);
        }
    }
}
