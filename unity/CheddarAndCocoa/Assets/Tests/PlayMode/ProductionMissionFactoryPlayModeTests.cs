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
        public void ProductionMissionFactory_ResolvesEveryCatalogIdToItself()
        {
            foreach (var spec in ProductionMissionCatalog.All)
            {
                var resolved = ProductionMissionFactory.GetById(spec.Id);
                Assert.AreEqual(spec.Id, resolved.Id, $"Factory should resolve {spec.Id} to its own spec.");
                Assert.AreEqual(spec.Title, resolved.Title, $"Factory returned the wrong title for {spec.Id}.");
            }
        }

        [Test]
        public void ProductionMissionFactory_UnknownIdFallsBackToSquirrelConspiracy()
        {
            Assert.AreEqual(
                ProductionMissionCatalog.SquirrelConspiracy.Id,
                ProductionMissionFactory.GetById("not_a_real_mission").Id);

            Assert.AreEqual(
                ProductionMissionCatalog.SquirrelConspiracy.Id,
                ProductionMissionFactory.GetById(null).Id);
        }

        [Test]
        public void ProductionMissionFactory_TryGetByIdReportsMatchAndMiss()
        {
            Assert.IsTrue(ProductionMissionFactory.TryGetById("weenie_roundup", out var hit));
            Assert.AreEqual("weenie_roundup", hit.Id);

            Assert.IsFalse(ProductionMissionFactory.TryGetById("not_a_real_mission", out _));
            Assert.IsFalse(ProductionMissionFactory.TryGetById(null, out _));
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
