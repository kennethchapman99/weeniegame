namespace CheddarAndCocoa.Game
{
    public static class ProductionMissionFactory
    {
        public static ProductionMissionSpec GetById(string missionId)
        {
            switch (missionId)
            {
                case "squirrel_conspiracy":
                    return ProductionMissionCatalog.SquirrelConspiracy;
                case "eagle_shadow_panic":
                    return ProductionMissionCatalog.EagleShadowPanic;
                case "coyotes_fence":
                    return ProductionMissionCatalog.CoyotesFence;
                default:
                    return ProductionMissionCatalog.SquirrelConspiracy;
            }
        }
    }
}
