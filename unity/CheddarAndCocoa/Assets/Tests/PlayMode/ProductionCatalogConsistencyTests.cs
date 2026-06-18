using System.Collections.Generic;
using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Fast, scene-free guards on the production mission catalog so the documented mission ids stay
    /// unique and keep matching the runtime snapshot ids the missions actually emit.
    /// </summary>
    public sealed class ProductionCatalogConsistencyTests
    {
        private static readonly ProductionMissionSpec[] Specs =
        {
            ProductionMissionCatalog.SquirrelConspiracy,
            ProductionMissionCatalog.EagleShadowPanic,
            ProductionMissionCatalog.WeenieRoundup,
            ProductionMissionCatalog.ScentSearch,
            ProductionMissionCatalog.ThunderstormComfort,
            ProductionMissionCatalog.MarkTheYard,
            ProductionMissionCatalog.LeashWalk,
            ProductionMissionCatalog.CoyotesFence,
        };

        [Test]
        public void CatalogIdsAndTitlesAreUniqueAndNonEmpty()
        {
            var ids = new HashSet<string>();
            var titles = new HashSet<string>();
            foreach (var spec in Specs)
            {
                Assert.IsFalse(string.IsNullOrEmpty(spec.Id), "Mission id should not be empty.");
                Assert.IsFalse(string.IsNullOrEmpty(spec.Title), $"Mission {spec.Id} title should not be empty.");
                Assert.IsTrue(ids.Add(spec.Id), $"Duplicate mission id: {spec.Id}");
                Assert.IsTrue(titles.Add(spec.Title), $"Duplicate mission title: {spec.Title}");
                Assert.IsFalse(string.IsNullOrEmpty(spec.Objective), $"Mission {spec.Id} needs objective copy.");
                Assert.IsFalse(string.IsNullOrEmpty(spec.ClearCondition), $"Mission {spec.Id} needs a clear condition.");
                Assert.IsFalse(string.IsNullOrEmpty(spec.FailCondition), $"Mission {spec.Id} needs a fail condition.");
            }
        }

        [Test]
        public void ProductionCatalogIdsMatchDocumentedRuntimeIds()
        {
            // These are the ids the missions emit via MissionRuntimeSnapshot; keep them in lockstep
            // with the catalog so analytics/tests can trust either source.
            Assert.AreEqual("squirrel_conspiracy", ProductionMissionCatalog.SquirrelConspiracy.Id);
            Assert.AreEqual("eagle_shadow_panic", ProductionMissionCatalog.EagleShadowPanic.Id);
            Assert.AreEqual("coyotes_fence", ProductionMissionCatalog.CoyotesFence.Id);
            Assert.AreEqual("weenie_roundup", ProductionMissionCatalog.WeenieRoundup.Id);
            Assert.AreEqual("scent_search", ProductionMissionCatalog.ScentSearch.Id);
            Assert.AreEqual("thunderstorm_comfort", ProductionMissionCatalog.ThunderstormComfort.Id);
            Assert.AreEqual("mark_the_yard", ProductionMissionCatalog.MarkTheYard.Id);
            Assert.AreEqual("leash_walk", ProductionMissionCatalog.LeashWalk.Id);
        }
    }
}
