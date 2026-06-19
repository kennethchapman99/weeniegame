using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the Sequential cause/effect chain: steps are role-gated and ordered,
    /// wrong attempts fumble harmlessly, dawdling eases the chain back, and the alternating owners
    /// force role reversal.
    /// </summary>
    public sealed class CoopSequenceChainPuzzleUnitTests
    {
        // Garden-gate contraption: Cheddar paws the latch, Cocoa shoulders the gate, Cheddar darts through.
        private static CoopSequenceChainPuzzle Gate(float settle = 0f)
        {
            var p = new CoopSequenceChainPuzzle();
            p.Configure(new[] { ChainActor.Cheddar, ChainActor.Cocoa, ChainActor.Cheddar }, settle);
            return p;
        }

        [Test]
        public void CorrectRoleOrder_SolvesTheChainWithNoFumbles()
        {
            var p = Gate();
            Assert.AreEqual(ChainActor.Cheddar, p.NextOwner);
            p.TryStep(ChainActor.Cheddar);
            Assert.AreEqual(ChainActor.Cocoa, p.NextOwner);
            p.TryStep(ChainActor.Cocoa);
            p.TryStep(ChainActor.Cheddar);

            Assert.IsTrue(p.Solved);
            Assert.AreEqual(0, p.Fumbles);
            Assert.AreEqual(3, p.Step);
        }

        [Test]
        public void WrongDog_FumblesWithoutAdvancing()
        {
            var p = Gate();
            p.TryStep(ChainActor.Cocoa); // step 1 belongs to Cheddar
            Assert.AreEqual(0, p.Step);
            Assert.AreEqual(1, p.Fumbles);

            p.TryStep(ChainActor.Cheddar); // correct
            Assert.AreEqual(1, p.Step);
            p.TryStep(ChainActor.Cheddar); // step 2 belongs to Cocoa
            Assert.AreEqual(1, p.Step);
            Assert.AreEqual(2, p.Fumbles);
        }

        [Test]
        public void EitherOwnerStep_AcceptsEitherDog()
        {
            var p = new CoopSequenceChainPuzzle();
            p.Configure(new[] { ChainActor.Either, ChainActor.Either }, 0f);
            p.TryStep(ChainActor.Cocoa);
            p.TryStep(ChainActor.Cheddar);
            Assert.IsTrue(p.Solved);
            Assert.AreEqual(0, p.Fumbles);
        }

        [Test]
        public void DawdlingPastTheSettleWindow_EasesTheChainBack()
        {
            var p = Gate(settle: 1f);
            p.TryStep(ChainActor.Cheddar); // Step 1
            Assert.AreEqual(1, p.Step);

            p.Advance(0.6f); // not yet settled
            Assert.AreEqual(1, p.Step);
            p.Advance(0.6f); // total 1.2 >= settle -> ease back one step
            Assert.AreEqual(0, p.Step);
            Assert.AreEqual(1, p.Settles);
        }

        [Test]
        public void AStepResetsTheIdleTimer_SoKeepingPaceAvoidsSettling()
        {
            var p = Gate(settle: 1f);
            p.TryStep(ChainActor.Cheddar);
            p.Advance(0.8f);
            p.TryStep(ChainActor.Cocoa); // resets idle
            p.Advance(0.8f);             // 0.8 < settle since the last step
            Assert.AreEqual(2, p.Step);
            Assert.AreEqual(0, p.Settles);
        }

        [Test]
        public void SettleNeverDropsBelowZeroAndZeroSettleDisablesRegression()
        {
            var p = Gate(settle: 0f); // disabled
            p.Advance(100f);
            Assert.AreEqual(0, p.Step);
            Assert.AreEqual(0, p.Settles);

            var q = Gate(settle: 0.5f);
            q.Advance(10f); // at step 0, nothing to ease back
            Assert.AreEqual(0, q.Step);
            Assert.AreEqual(0, q.Settles);
        }

        [Test]
        public void SolvedChainIsTerminalAndResetClears()
        {
            var p = Gate(settle: 1f);
            p.TryStep(ChainActor.Cheddar);
            p.TryStep(ChainActor.Cocoa);
            p.TryStep(ChainActor.Cheddar);
            Assert.IsTrue(p.Solved);
            p.Advance(100f);
            p.TryStep(ChainActor.Cocoa);
            Assert.IsTrue(p.Solved, "Solved stays solved.");
            Assert.AreEqual(ChainActor.Either, p.NextOwner);

            p.Reset();
            Assert.AreEqual(0, p.Step);
            Assert.AreEqual(0, p.Fumbles);
            Assert.AreEqual(0, p.Settles);
            Assert.IsFalse(p.Solved);
        }
    }
}
