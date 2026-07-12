using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the feast-and-fend (baby bird) beat: the prize lifecycle only moves
    /// forward through grab/shake, dives only threaten a held prize, a repel must land inside the
    /// dive window, an expired dive pecks and frees the prize, and the final shake gulps mid-dive.
    /// </summary>
    public sealed class CoopFeastGuardPuzzleUnitTests
    {
        private static CoopFeastGuardPuzzle Make(
            int chicks = 4, int shakes = 3, int maxPecks = 3, float window = 1.6f)
        {
            var p = new CoopFeastGuardPuzzle();
            p.Configure(chicks, shakes, maxPecks, window);
            return p;
        }

        [Test]
        public void GrabShakeGulp_EatsTheChick()
        {
            var p = Make();
            Assert.IsTrue(p.ChickLanded());
            Assert.IsTrue(p.Grab());
            Assert.IsTrue(p.Shake());
            Assert.IsTrue(p.Shake());
            Assert.AreEqual(0, p.ChicksEaten, "Two of three shakes should not gulp yet.");
            Assert.IsTrue(p.Shake());
            Assert.AreEqual(1, p.ChicksEaten);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.None, p.Chick);
        }

        [Test]
        public void LifecycleGates_RejectOutOfOrderMoves()
        {
            var p = Make();
            Assert.IsFalse(p.Grab(), "Nothing to grab before a chick lands.");
            Assert.IsFalse(p.Shake(), "Nothing to shake before a grab.");
            Assert.IsFalse(p.StartDive(), "Parents only dive at a busy eater.");
            Assert.IsTrue(p.ChickLanded());
            Assert.IsFalse(p.ChickLanded(), "Only one live chick at a time.");
            Assert.IsFalse(p.Shake(), "A grounded chick cannot be shaken - grab it first.");
            Assert.IsTrue(p.Grab());
            Assert.IsFalse(p.Grab(), "A held chick cannot be re-grabbed.");
        }

        [Test]
        public void RepelInsideWindow_DrivesTheDiveOff()
        {
            var p = Make();
            p.ChickLanded();
            p.Grab();
            Assert.IsTrue(p.StartDive());
            Assert.IsFalse(p.StartDive(), "Only one dive at a time.");
            p.Advance(0.5f);
            Assert.IsTrue(p.Repel());
            Assert.AreEqual(1, p.Repels);
            Assert.AreEqual(0, p.Pecks);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.Held, p.Chick, "The feast survives a repelled dive.");
            Assert.IsFalse(p.Repel(), "No dive left to repel.");
        }

        [Test]
        public void ExpiredDive_PecksAndFreesTheChick()
        {
            var p = Make(window: 1f);
            p.ChickLanded();
            p.Grab();
            p.Shake();
            p.StartDive();
            p.Advance(1.1f);
            Assert.AreEqual(1, p.Pecks);
            Assert.IsFalse(p.DiveActive);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.None, p.Chick, "The pecked-loose chick escapes.");
            Assert.AreEqual(0, p.Shakes, "Shake progress dies with the escaped chick.");
            Assert.AreEqual(1, p.Mistakes);
        }

        [Test]
        public void FinalShake_GulpsMidDiveAndCancelsIt()
        {
            var p = Make(shakes: 2, window: 5f);
            p.ChickLanded();
            p.Grab();
            p.Shake();
            p.StartDive();
            Assert.IsTrue(p.Shake(), "The gulp shake still lands during a dive.");
            Assert.AreEqual(1, p.ChicksEaten);
            Assert.IsFalse(p.DiveActive, "Nothing left to defend - the dive breaks off.");
            p.Advance(10f);
            Assert.AreEqual(0, p.Pecks, "A cancelled dive can never land.");
        }

        [Test]
        public void ThreePecks_OverrunTheYard()
        {
            var p = Make(window: 1f);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsFalse(p.Overrun);
                p.ChickLanded();
                p.Grab();
                p.StartDive();
                p.Advance(2f);
            }
            Assert.AreEqual(3, p.Pecks);
            Assert.IsTrue(p.Overrun);
            Assert.IsFalse(p.StartDive(), "An overrun feast is over - no more dives.");
        }

        [Test]
        public void UnclaimedChick_AirliftsAsAMistake()
        {
            var p = Make();
            p.ChickLanded();
            Assert.IsTrue(p.AirliftUnclaimed());
            Assert.AreEqual(1, p.Airlifts);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.None, p.Chick);
            Assert.AreEqual(1, p.Mistakes);
            Assert.IsFalse(p.AirliftUnclaimed(), "Nothing left to airlift.");
        }

        [Test]
        public void EatingEnoughChicks_Solves()
        {
            var p = Make(chicks: 2, shakes: 1);
            p.ChickLanded();
            p.Grab();
            p.Shake();
            Assert.IsFalse(p.Solved);
            p.ChickLanded();
            p.Grab();
            p.Shake();
            Assert.IsTrue(p.Solved);
            Assert.IsFalse(p.ChickLanded(), "A solved feast spawns no more chicks.");
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var p = Make(window: 1f);
            p.ChickLanded();
            p.Grab();
            p.StartDive();
            p.Advance(2f); // peck
            p.ChickLanded();
            p.AirliftUnclaimed();
            Assert.Greater(p.Mistakes, 0);

            p.Reset();
            Assert.AreEqual(0, p.ChicksEaten);
            Assert.AreEqual(0, p.Pecks);
            Assert.AreEqual(0, p.Airlifts);
            Assert.AreEqual(0, p.Repels);
            Assert.AreEqual(0, p.Mistakes);
            Assert.IsFalse(p.DiveActive);
            Assert.AreEqual(CoopFeastGuardPuzzle.ChickState.None, p.Chick);
        }
    }
}
