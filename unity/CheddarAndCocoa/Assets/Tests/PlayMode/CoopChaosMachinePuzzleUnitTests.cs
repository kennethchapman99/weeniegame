using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the chaos-machine cascade: it only runs after a trigger, clears a
    /// junction when the helper assists in the window, misfires (visibly) at a junction otherwise, and
    /// resumes from the stall on a re-trigger.
    /// </summary>
    public sealed class CoopChaosMachinePuzzleUnitTests
    {
        private static CoopChaosMachinePuzzle Make(int stages = 4, float window = 1f)
        {
            var p = new CoopChaosMachinePuzzle();
            p.Configure(stages, window);
            return p;
        }

        [Test]
        public void NoAdvanceUntilTriggered()
        {
            var p = Make();
            p.Advance(1f, assisting: true);
            Assert.AreEqual(0, p.Stage);
            Assert.IsFalse(p.Running);
        }

        [Test]
        public void HelpersInPlace_CascadeRunsThroughEveryJunctionToSolve()
        {
            var p = Make(stages: 4, window: 1f);
            p.Trigger();
            int guard = 0;
            while (!p.Solved && guard++ < 20)
                p.Advance(0.5f, assisting: true);

            Assert.IsTrue(p.Solved);
            Assert.AreEqual(0, p.Stalls);
            Assert.IsFalse(p.Running);
        }

        [Test]
        public void MissingHelper_MisfiresAndStallsAtThatStageVisibly()
        {
            var p = Make(stages: 4, window: 1f);
            p.Trigger();
            p.Advance(0.6f, assisting: true);  // clears stage 0 -> now at stage 1
            Assert.AreEqual(1, p.Stage);

            p.Advance(0.6f, assisting: false); // window not yet out
            Assert.IsTrue(p.Running);
            p.Advance(0.6f, assisting: false); // window expires at stage 1 -> stall
            Assert.IsFalse(p.Running);
            Assert.AreEqual(1, p.StalledStage);
            Assert.AreEqual(1, p.Stalls);
            Assert.AreEqual(1, p.Stage, "Stall keeps the cleared stages.");
        }

        [Test]
        public void ReTriggerResumesFromTheStallAndCanFinish()
        {
            var p = Make(stages: 3, window: 1f);
            p.Trigger();
            p.Advance(1.2f, assisting: false); // stall at stage 0
            Assert.AreEqual(0, p.StalledStage);
            Assert.AreEqual(1, p.Stalls);

            p.Trigger(); // dog back in place, pull again
            Assert.IsTrue(p.Running);
            Assert.AreEqual(-1, p.StalledStage);
            int guard = 0;
            while (!p.Solved && guard++ < 20)
                p.Advance(0.5f, assisting: true);
            Assert.IsTrue(p.Solved);
            Assert.AreEqual(1, p.Stalls, "The earlier misfire is still tallied.");
        }

        [Test]
        public void TriggerDoesNothingWhileRunningOrSolved()
        {
            var p = Make(stages: 1, window: 1f);
            p.Trigger();
            p.Advance(0.5f, true); // stage 1 of 1 -> solved
            Assert.IsTrue(p.Solved);
            p.Trigger();
            Assert.IsFalse(p.Running, "Can't re-trigger a finished machine.");
        }

        [Test]
        public void ResetClearsEverything()
        {
            var p = Make();
            p.Trigger();
            p.Advance(1.5f, false); // stall
            p.Reset();
            Assert.AreEqual(0, p.Stage);
            Assert.IsFalse(p.Running);
            Assert.AreEqual(-1, p.StalledStage);
            Assert.AreEqual(0, p.Stalls);
            Assert.IsFalse(p.Solved);
        }
    }
}
