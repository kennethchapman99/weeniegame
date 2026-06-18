using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ProductionProgressionPlayModeTests
    {
        [Test]
        public void MissionSeedGenerator_IsStableAndBounded()
        {
            int a = MissionSeedGenerator.StableSeed("squirrel_conspiracy", 2, 1);
            int b = MissionSeedGenerator.StableSeed("squirrel_conspiracy", 2, 1);
            int c = MissionSeedGenerator.StableSeed("squirrel_conspiracy", 3, 1);

            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
            Assert.That(MissionSeedGenerator.VariantIndex(a, 4), Is.InRange(0, 3));
            Assert.AreEqual(0, MissionSeedGenerator.VariantIndex(a, 0));
        }

        [Test]
        public void ContentVariantCatalog_DefinesReplayableVariantFamilies()
        {
            Assert.AreEqual(ContentVariantKind.Layout, ContentVariantCatalog.SquirrelRouteShuffle.Kind);
            Assert.AreEqual(ContentVariantKind.Objective, ContentVariantCatalog.ZeroFakeOutChallenge.Kind);
            Assert.AreEqual(ContentVariantKind.Comedy, ContentVariantCatalog.FakeSnackComedy.Kind);
            Assert.AreEqual(ContentVariantKind.Modifier, ContentVariantCatalog.WetFloorsModifier.Kind);
        }

        [Test]
        public void BossAndCollectibleCatalogs_DefineProductionTargets()
        {
            Assert.AreEqual(BossPhaseKind.Teach, BossPhaseCatalog.SquirrelTeach.Kind);
            Assert.AreEqual(BossPhaseKind.Teamwork, BossPhaseCatalog.SquirrelTeamwork.Kind);
            Assert.Greater(BossPhaseCatalog.NailGrinderFinal.RequiredProgress, 0);

            Assert.AreEqual(CollectibleSetKind.SquirrelEvidence, CollectibleSetCatalog.SquirrelEvidence.Kind);
            Assert.Greater(CollectibleSetCatalog.LostToys.RequiredCount, CollectibleSetCatalog.SquirrelEvidence.RequiredCount);
            Assert.That(CollectibleSetCatalog.NeighborhoodSmells.DisplayName, Does.Contain("Smells"));
        }

        [Test]
        public void PlayerStatsAccumulator_TracksLifetimeDogStats()
        {
            var stats = new PlayerStatsAccumulator();
            stats.RecordBark();
            stats.RecordBark(united: true);
            stats.RecordRescue();
            stats.RecordSquirrelChased();
            stats.RecordToyRecovered();
            stats.RecordPoolFall();
            stats.RecordFakeSnackEaten();

            Assert.AreEqual(2, stats.Stats.Barks);
            Assert.AreEqual(1, stats.Stats.UnitedBarks);
            Assert.AreEqual(1, stats.Stats.Rescues);
            Assert.AreEqual(1, stats.Stats.SquirrelsChased);
            Assert.AreEqual(1, stats.Stats.ToysRecovered);
            Assert.AreEqual(1, stats.Stats.PoolFalls);
            Assert.AreEqual(1, stats.Stats.FakeSnacksEaten);

            stats.Reset();
            Assert.AreEqual(0, stats.Stats.Barks);
        }
    }
}
