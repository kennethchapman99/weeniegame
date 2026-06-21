using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>External definitions for controller-owned missions.</summary>
    public static class MissionCatalog
    {
        public static bool TryBuild(
            GameManager.MissionVariant variant,
            ArenaMissionTuning tuning,
            out GameManager.MissionDefinition definition)
        {
            if (variant != GameManager.MissionVariant.KitchenFoodFrenzy)
            {
                definition = null;
                return false;
            }

            var balance = tuning.BalanceFor(variant);
            definition = new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Kitchen Falling Food Frenzy",
                IntroPrompt = "Cheddar barks at the counter to knock food loose; Cocoa reads the warning circle, catches gold in the bowl, and dodges purple. Finish the three-call dinner rush!",
                ReadyScoreLabel = "READY FOR FALLING FOOD",
                ItemRootName = "Kitchen Scraps",
                ItemObjectName = "Falling Food",
                ItemWorldLabel = "Food!",
                ItemArrowLabel = "FOOD",
                ItemCollectCueNoun = "a falling snack",
                CollectObjectiveFormat = "Catch food {0}/{1}",
                CollectedScoreLabel = "FOOD CAUGHT",
                ItemScore = balance.ItemScore,
                SpawnedItemCount = 0,
                ItemGoal = 0,
                RoundSeconds = balance.RoundSeconds,
                PawfectScore = balance.PawfectScore,
                HeroScore = balance.HeroScore,
                SurvivorScore = balance.SurvivorScore,
                UsesSquirrel = false,
                RequiresPredator = false,
                RequiresTug = false,
                MaxStolenFood = balance.MaxStolenFood,
                SquirrelPenalty = balance.SquirrelPenalty,
                SquirrelScareScore = balance.SquirrelScareScore,
                SquirrelObjectiveText = "No squirrel - catch the falling food",
                SquirrelStealingCue = "No squirrel in the Kitchen.",
                SquirrelStoleCue = "No squirrel in the Kitchen.",
                SquirrelStealScoreLabel = "KITCHEN MESS",
                SquirrelScareScoreLabel = "COUNTER BARK",
                SquirrelStealingActorLabel = "SQUIRREL OFF DUTY",
                SquirrelDroppedActorLabel = "SQUIRREL OFF DUTY",
                SquirrelStoleActorLabel = "SQUIRREL OFF DUTY",
                SquirrelMissPopLabel = "MISS! -FOOD",
                SquirrelStealJuiceLabel = "MISS! FOOD SPLAT",
                SquirrelScareJuiceLabel = "COUNTER POP!",
                TugObjectiveText = "Catch the falling food",
                WaitingObjectiveText = "Cheddar, bark at the counter to knock the next food loose",
                ClearObjectiveText = "Kitchen cleared - replay Kitchen Falling Food Frenzy",
                ClearBannerPrefix = "KITCHEN CLEARED!",
                ClearScoreLabel = "KITCHEN FRENZY CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay the Kitchen",
                FailObjectiveText = "Mission failed - replay Kitchen Falling Food Frenzy",
                GenericFailReason = "The good food kept hitting the floor.",
                TimeFailReason = "Dinner ended before enough food was floored into the bowl.",
                StolenFailReason = "No squirrel here, just a sticky kitchen floor.",
                PredatorFailReason = "No predator here, just a busy stove.",
                PawfectClearReason = "A flawless counter-and-floor relay - chef's kiss.",
                HeroClearReason = "Most of the good food made it into the bowl.",
                BasicClearReason = "The bowl got filled, even if the floor took a few casualties.",
                ItemColor = new Color(1f, 0.85f, 0.4f),
                ItemAccentColor = new Color(1f, 0.7f, 0.3f),
                ItemSecondaryColor = new Color(0.7f, 0.4f, 0.85f),
                ItemPopColor = new Color(0.5f, 1f, 0.65f)
            };
            return true;
        }
    }
}
