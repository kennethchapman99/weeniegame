using System.Collections.Generic;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Resolves a <see cref="ProductionMissionSpec"/> from its stable runtime id. The lookup is built
    /// directly from <see cref="ProductionMissionCatalog.All"/> so every authored mission is reachable
    /// by id and the table can never drift behind the catalog.
    /// </summary>
    public static class ProductionMissionFactory
    {
        private static readonly Dictionary<string, ProductionMissionSpec> ById = BuildLookup();

        private static Dictionary<string, ProductionMissionSpec> BuildLookup()
        {
            var lookup = new Dictionary<string, ProductionMissionSpec>(ProductionMissionCatalog.All.Length);
            foreach (var spec in ProductionMissionCatalog.All)
            {
                lookup[spec.Id] = spec;
            }

            return lookup;
        }

        /// <summary>
        /// Returns the spec for <paramref name="missionId"/>, or the Squirrel Conspiracy default when
        /// the id is unknown so callers always get a playable backyard mission.
        /// </summary>
        public static ProductionMissionSpec GetById(string missionId)
        {
            return missionId != null && ById.TryGetValue(missionId, out var spec)
                ? spec
                : ProductionMissionCatalog.SquirrelConspiracy;
        }

        /// <summary>
        /// Resolves <paramref name="missionId"/> without falling back to a default; returns false when
        /// the id is not a known catalog mission.
        /// </summary>
        public static bool TryGetById(string missionId, out ProductionMissionSpec spec)
        {
            if (missionId != null)
            {
                return ById.TryGetValue(missionId, out spec);
            }

            spec = default;
            return false;
        }
    }
}
