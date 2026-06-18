using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class MissionRuntimePlayModeTests
    {
        [Test]
        public void MissionRuntimeSnapshot_ReportsProgressAndCompletion()
        {
            var snapshot = new MissionRuntimeSnapshot(
                "squirrel_conspiracy",
                500,
                42f,
                2,
                4,
                1,
                isClear: false,
                isFailed: false);

            Assert.AreEqual(0.5f, snapshot.ProgressRatio);
            Assert.IsFalse(snapshot.IsComplete);

            var clear = new MissionRuntimeSnapshot("squirrel_conspiracy", 1500, 10f, 4, 4, 0, true, false);
            Assert.AreEqual(1f, clear.ProgressRatio);
            Assert.IsTrue(clear.IsComplete);
        }

        [Test]
        public void ChallengeObjectiveEvaluator_ChecksScoreCountersAndTime()
        {
            var clear = new MissionRuntimeSnapshot("squirrel_conspiracy", 1600, 10f, 4, 4, 0, true, false);

            Assert.IsTrue(ChallengeObjectiveEvaluator.ScoreAtLeast(ChallengeObjectiveCatalog.SquirrelScore1500, clear));
            Assert.IsTrue(ChallengeObjectiveEvaluator.CounterAtMost(ChallengeObjectiveCatalog.SquirrelNoFakeOuts, 0));
            Assert.IsFalse(ChallengeObjectiveEvaluator.CounterAtMost(ChallengeObjectiveCatalog.SquirrelNoFakeOuts, 1));

            var speed = new ChallengeObjectiveSpec("speed", "Clear fast", ChallengeObjectiveKind.ClearUnderSeconds, 60);
            Assert.IsTrue(ChallengeObjectiveEvaluator.ClearUnderSeconds(speed, clear, 55f));
            Assert.IsFalse(ChallengeObjectiveEvaluator.ClearUnderSeconds(speed, clear, 65f));
        }
    }
}
