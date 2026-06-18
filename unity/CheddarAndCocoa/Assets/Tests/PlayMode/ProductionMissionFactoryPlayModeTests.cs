using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ProductionMissionFactoryPlayModeTests
    {
        [Test]
        public void ProductionMissionFactory_ReturnsExpectedMissionSpecs()
        {
            Assert.AreEqual(
                "The Great Backyard Squirrel Conspiracy",
                ProductionMissionFactory.GetById("squirrel_conspiracy").Title);

            Assert.AreEqual(
                ProductionMechanicModule.ThreatSweep,
                ProductionMissionFactory.GetById("eagle_shadow_panic").PrimaryModule);

            Assert.AreEqual(
                ProductionMechanicModule.PatrolDefense,
                ProductionMissionFactory.GetById("coyotes_fence").PrimaryModule);
        }

        [Test]
        public void SummaryBuilder_ProducesOutcomeLabels()
        {
            var squirrel = new HerdingMissionState();
            squirrel.FindStash();
            Assert.AreEqual("Conspiracy Cracked", MissionOutcomeSummaryBuilder.BuildSquirrelSummary(squirrel));

            var threat = new ThreatSweepMissionState();
            threat.CompleteRescue();
            threat.CompleteUnitedFront();
            Assert.AreEqual("Backyard Defenders", MissionOutcomeSummaryBuilder.BuildThreatSweepSummary(threat));

            var patrol = new PatrolDefenseMissionState();
            patrol.CompleteFinalPressure();
            Assert.AreEqual("Fence Guardians", MissionOutcomeSummaryBuilder.BuildPatrolSummary(patrol));
        }
    }
}
