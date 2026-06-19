using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the scent-relay split-information beat: the digger can't act blind,
    /// only the reader's revealed target finds, decoys are harmless, each find needs a fresh relay,
    /// and the seeded target sequence is reproducible.
    /// </summary>
    public sealed class CoopScentRelayPuzzleUnitTests
    {
        private static CoopScentRelayPuzzle Make(int targets = 4, int finds = 3, int seed = 12345)
        {
            var p = new CoopScentRelayPuzzle();
            p.Configure(targets, finds, seed);
            return p;
        }

        [Test]
        public void DiggingBeforeAReveal_IsABlindGuessThatFails()
        {
            var p = Make();
            Assert.AreEqual(-1, p.RevealedTarget, "Nothing is signaled before a reveal.");
            p.ActOn(0);
            Assert.AreEqual(0, p.Finds);
            Assert.AreEqual(1, p.BlindActs);
        }

        [Test]
        public void RevealThenDigTheRealTarget_Finds()
        {
            var p = Make();
            p.Reveal();
            int real = p.RevealedTarget;
            Assert.That(real, Is.InRange(0, p.TargetCount - 1));
            p.ActOn(real);
            Assert.AreEqual(1, p.Finds);
            Assert.AreEqual(0, p.WrongDigs);
            Assert.IsFalse(p.Known, "A find re-buries the next item, so the team must relay again.");
        }

        [Test]
        public void DiggingTheWrongTargetIsAHarmlessDecoy_TheReaderStillKnows()
        {
            var p = Make(targets: 4);
            p.Reveal();
            int real = p.RevealedTarget;
            int wrong = (real + 1) % p.TargetCount;
            p.ActOn(wrong);
            Assert.AreEqual(0, p.Finds);
            Assert.AreEqual(1, p.WrongDigs);
            Assert.IsTrue(p.Known, "A decoy dig doesn't lose the reveal.");
            p.ActOn(p.RevealedTarget);
            Assert.AreEqual(1, p.Finds);
        }

        [Test]
        public void EachFindNeedsAFreshReveal_RelayToSolve()
        {
            var p = Make(finds: 3);
            int guard = 0;
            while (!p.Solved && guard++ < 20)
            {
                p.Reveal();
                p.ActOn(p.RevealedTarget);
            }
            Assert.IsTrue(p.Solved);
            Assert.AreEqual(3, p.Finds);
            Assert.AreEqual(0, p.BlindActs);
            Assert.AreEqual(0, p.WrongDigs);

            // Acting after the last find without a reveal would be blind, but Solved short-circuits it.
            p.ActOn(0);
            Assert.AreEqual(0, p.BlindActs);
        }

        [Test]
        public void ActingBetweenFindsWithoutReRevealing_IsBlind()
        {
            var p = Make(finds: 3);
            p.Reveal();
            p.ActOn(p.RevealedTarget); // find 1
            p.ActOn(p.CorrectTarget);  // no fresh reveal -> blind
            Assert.AreEqual(1, p.Finds);
            Assert.AreEqual(1, p.BlindActs);
        }

        [Test]
        public void SameSeed_ProducesTheSameTargetSequence()
        {
            var a = Make(seed: 999);
            var b = Make(seed: 999);
            for (int i = 0; i < 3; i++)
            {
                a.Reveal();
                b.Reveal();
                Assert.AreEqual(a.RevealedTarget, b.RevealedTarget, "Seeded relay is reproducible.");
                a.ActOn(a.RevealedTarget);
                b.ActOn(b.RevealedTarget);
            }
        }

        [Test]
        public void ResetClearsEverything()
        {
            var p = Make();
            p.Reveal();
            p.ActOn((p.RevealedTarget + 1) % p.TargetCount); // wrong
            p.ActOn(p.RevealedTarget);                       // find
            p.Reset();
            Assert.AreEqual(0, p.Finds);
            Assert.AreEqual(0, p.WrongDigs);
            Assert.AreEqual(0, p.BlindActs);
            Assert.IsFalse(p.Known);
            Assert.IsFalse(p.Solved);
        }
    }
}
