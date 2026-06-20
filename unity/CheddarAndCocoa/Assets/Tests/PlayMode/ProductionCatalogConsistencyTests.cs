using System.Collections.Generic;
using System.Reflection;
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
        private static readonly ProductionMissionSpec[] Specs = ProductionMissionCatalog.All;

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
            Assert.AreEqual("car_ride", ProductionMissionCatalog.CarRide.Id);
        }

        [Test]
        public void AllArrayCoversEveryDeclaredCatalogSpec()
        {
            // Reflectively gather every spec field on the catalog so a newly authored mission that is
            // forgotten in ProductionMissionCatalog.All trips this guard instead of silently dropping
            // out of factory id lookups.
            var declared = new List<string>();
            foreach (var field in typeof(ProductionMissionCatalog).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(ProductionMissionSpec))
                {
                    declared.Add(((ProductionMissionSpec)field.GetValue(null)).Id);
                }
            }

            var inAll = new HashSet<string>();
            foreach (var spec in ProductionMissionCatalog.All)
            {
                inAll.Add(spec.Id);
            }

            Assert.AreEqual(declared.Count, ProductionMissionCatalog.All.Length,
                "ProductionMissionCatalog.All must list every declared mission spec exactly once.");

            foreach (var id in declared)
            {
                Assert.IsTrue(inAll.Contains(id), $"Mission {id} is declared but missing from ProductionMissionCatalog.All.");
            }
        }
    }
}
