using System;
using System.Collections.Generic;

namespace CheddarAndCocoa.Game
{
    /// <summary>External controller factories. Unmigrated missions intentionally have no entry yet.</summary>
    public static class MissionControllerRegistry
    {
        private static readonly IReadOnlyDictionary<GameManager.MissionVariant, Func<IMissionController>> Factories =
            new Dictionary<GameManager.MissionVariant, Func<IMissionController>>
            {
                [GameManager.MissionVariant.KitchenFoodFrenzy] = () => new KitchenFoodFrenzyMissionController(),
                [GameManager.MissionVariant.OperationPeeBreak] = () => new PeeBreakMissionController()
            };

        public static bool TryCreate(GameManager.MissionVariant variant, out IMissionController controller)
        {
            if (Factories.TryGetValue(variant, out var factory))
            {
                controller = factory();
                return true;
            }

            controller = null;
            return false;
        }
    }
}
