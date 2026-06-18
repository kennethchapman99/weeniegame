using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ProductionSystemsPlayModeTests
    {
        [Test]
        public void MissionProductionCatalog_DefinesBackyardExpansionMissions()
        {
            Assert.AreEqual("squirrel_conspiracy", ProductionMissionCatalog.SquirrelConspiracy.Id);
            Assert.AreEqual(ProductionMechanicModule.Herding, ProductionMissionCatalog.SquirrelConspiracy.PrimaryModule);
            Assert.AreEqual(ProductionMissionPack.Backyard, ProductionMissionCatalog.SquirrelConspiracy.Pack);
            Assert.That(ProductionMissionCatalog.SquirrelConspiracy.Objective, Does.Contain("stash"));

            Assert.AreEqual(ProductionMechanicModule.ThreatSweep, ProductionMissionCatalog.EagleShadowPanic.PrimaryModule);
            Assert.AreEqual(ProductionMechanicModule.PatrolDefense, ProductionMissionCatalog.CoyotesFence.PrimaryModule);
        }

        [Test]
        public void MissionRankCalculator_MapsScoreToRankAndStars()
        {
            var pawfect = MissionRankCalculator.Calculate(1600, true, 1500, 1000, 350);
            Assert.AreEqual(MissionRankCalculator.PawfectRank, pawfect.Rank);
            Assert.AreEqual(3, pawfect.Stars);

            var hero = MissionRankCalculator.Calculate(1200, true, 1500, 1000, 350);
            Assert.AreEqual(MissionRankCalculator.HeroRank, hero.Rank);
            Assert.AreEqual(2, hero.Stars);

            var survivorFail = MissionRankCalculator.Calculate(500, false, 1500, 1000, 350);
            Assert.AreEqual(MissionRankCalculator.SurvivorRank, survivorFail.Rank);
            Assert.AreEqual(0, survivorFail.Stars);

            var lowClear = MissionRankCalculator.Calculate(100, true, 1500, 1000, 350);
            Assert.AreEqual(MissionRankCalculator.LowRank, lowClear.Rank);
            Assert.AreEqual(1, lowClear.Stars);
        }

        [Test]
        public void ScoreEventCatalog_FavorsTeamworkEvents()
        {
            Assert.AreEqual("CUTOFF", ScoreEventCatalog.Cutoff.Label);
            Assert.Greater(ScoreEventCatalog.Cutoff.Points, ScoreEventCatalog.GoodHerd.Points);
            Assert.IsTrue(ScoreEventCatalog.Cutoff.IsTeamwork);
            Assert.Less(ScoreEventCatalog.FakeOut.Points, 0);
            Assert.IsTrue(ScoreEventCatalog.ConspiracyCracked.IsTeamwork);
        }

        [Test]
        public void ChallengeObjectiveCatalog_DefinesReplayGoals()
        {
            Assert.AreEqual(ChallengeObjectiveKind.NoFakeOuts, ChallengeObjectiveCatalog.SquirrelNoFakeOuts.Kind);
            Assert.AreEqual(1500, ChallengeObjectiveCatalog.SquirrelScore1500.TargetValue);
            Assert.That(ChallengeObjectiveCatalog.EagleNoGrab.Label, Does.Contain("grabbed"));
            Assert.AreEqual(ChallengeObjectiveKind.PerfectCutoffs, ChallengeObjectiveCatalog.CoyotePerfectFence.Kind);
        }
    }
}
