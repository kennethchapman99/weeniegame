using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>External definitions for controller-owned missions.</summary>
    public static class MissionCatalog
    {
        public static bool TryBuild(
            GameManager.MissionVariant variant,
            ArenaMissionTuning tuning,
            out GameManager.MissionDefinition definition) =>
            MissionControllerRegistry.TryBuildDefinition(variant, tuning, out definition);

        internal static GameManager.MissionDefinition BuildKitchenDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.KitchenFoodFrenzy;
            var balance = tuning.BalanceFor(variant);
            var definition = new GameManager.MissionDefinition
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
            return definition;
        }

        internal static GameManager.MissionDefinition BuildMarkTheYardDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.MarkTheYard;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Mark the Yard",
                IntroPrompt = "Cheddar + Cocoa must claim every territory zone and hold them all at once - but the squirrel keeps re-marking them, so split up and cover the yard.",
                ReadyScoreLabel = "READY TO MARK TERRITORY",
                ItemRootName = "Territory Zones",
                ItemObjectName = "Zone",
                ItemWorldLabel = "Claim!",
                ItemArrowLabel = "ZONE",
                ItemCollectCueNoun = "a zone",
                CollectObjectiveFormat = "Mark zones {0}/{1}",
                CollectedScoreLabel = "ZONE MARKED",
                ItemScore = balance.ItemScore,
                SpawnedItemCount = balance.SpawnedItemCount,
                ItemGoal = balance.ItemGoal,
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
                SquirrelObjectiveText = "Claim the territory zones",
                SquirrelStealingCue = "The squirrel is re-marking your zones!",
                SquirrelStoleCue = "The squirrel stole a zone back!",
                SquirrelStealScoreLabel = "ZONE STOLEN",
                SquirrelScareScoreLabel = "ZONE MARKED",
                SquirrelStealingActorLabel = "SQUIRREL RE-MARKING",
                SquirrelDroppedActorLabel = "ZONE HELD",
                SquirrelStoleActorLabel = "SQUIRREL STOLE A ZONE",
                SquirrelMissPopLabel = "STOLEN!",
                SquirrelStealJuiceLabel = "ZONE STOLEN!",
                SquirrelScareJuiceLabel = "ZONE MARKED!",
                TugObjectiveText = "Hold the zones together",
                WaitingObjectiveText = "Cover the last zone together",
                ClearObjectiveText = "Yard claimed - replay Mark the Yard",
                ClearBannerPrefix = "YARD CLAIMED!",
                ClearScoreLabel = "YARD MARKED",
                ReplayPrompt = "Press R / Enter / Start to replay Mark the Yard",
                FailObjectiveText = "Mission failed - replay Mark the Yard",
                GenericFailReason = "Needs better yard coverage before the squirrel re-marks everything.",
                TimeFailReason = "The squirrel kept stealing zones until the clock ran out.",
                StolenFailReason = "The squirrel out-marked the dogs across the whole yard.",
                PredatorFailReason = "No predator here, just a territorial squirrel.",
                PawfectClearReason = "Tiny landlords held every corner of the yard at once - total domination.",
                HeroClearReason = "The whole yard got claimed before the squirrel could recover.",
                BasicClearReason = "The yard is theirs, even if the squirrel made them work for it.",
                ItemColor = new Color(0.5f, 0.5f, 0.55f),
                ItemAccentColor = new Color(0.3f, 0.8f, 0.45f),
                ItemSecondaryColor = new Color(0.2f, 0.2f, 0.24f),
                ItemPopColor = new Color(0.45f, 0.9f, 0.55f)
            };
        }

        internal static GameManager.MissionDefinition BuildPeeBreakDefinition(ArenaMissionTuning tuning) =>
            new GameManager.MissionDefinition
            {
                Variant = GameManager.MissionVariant.OperationPeeBreak,
                Name = "Operation Pee Break",
                IntroPrompt = "The Teenager is lost in the phone. Cocoa stares, Cheddar presents the leash, then swap roles for the charger gambit and finish with a united bark!",
                ReadyScoreLabel = "READY TO ASK OUT",
                ItemRootName = "Pee Break Props",
                ItemObjectName = "Pee Break Signal",
                ItemWorldLabel = "Signal",
                ItemArrowLabel = "SIGNAL",
                ItemCollectCueNoun = "signal",
                CollectObjectiveFormat = "Get through beat {0}/{1}",
                CollectedScoreLabel = "TEENAGER COMPREHENSION",
                ItemScore = 0,
                SpawnedItemCount = 0,
                ItemGoal = 4,
                RoundSeconds = 480f,
                PawfectScore = 1800,
                HeroScore = 1250,
                SurvivorScore = 400,
                UsesSquirrel = false,
                RequiresPredator = false,
                RequiresTug = false,
                MaxStolenFood = 0,
                SquirrelPenalty = 0,
                SquirrelScareScore = 0,
                SquirrelObjectiveText = "Convince the Teenager",
                SquirrelStealingCue = "The Teenager is the puzzle.",
                SquirrelStoleCue = "The Teenager misread the dogs.",
                SquirrelStealScoreLabel = "MISREAD",
                SquirrelScareScoreLabel = "CLEAR SIGNAL",
                SquirrelStealingActorLabel = "TEENAGER",
                SquirrelDroppedActorLabel = "WRONG IDEA",
                SquirrelStoleActorLabel = "MISREAD",
                SquirrelMissPopLabel = "NOT NOW",
                SquirrelStealJuiceLabel = "MISREAD!",
                SquirrelScareJuiceLabel = "THEY GET IT!",
                TugObjectiveText = "Send one clear message",
                WaitingObjectiveText = "Cocoa: hold the door stare",
                ClearObjectiveText = "Door open - outside!",
                ClearBannerPrefix = "PEE BREAK!",
                ClearScoreLabel = "DOOR OPEN",
                ReplayPrompt = "Press R / Enter / Start to replay Operation Pee Break",
                FailObjectiveText = "Too late - replay Operation Pee Break",
                GenericFailReason = "The phone won this round.",
                TimeFailReason = "The Teenager stayed glued to the phone too long.",
                StolenFailReason = "The Teenager misunderstood the signal.",
                PredatorFailReason = "The phone reclaimed the Teenager's attention.",
                PawfectClearReason = "One crystal-clear dog message and a glorious open door.",
                HeroClearReason = "A few wrong guesses, then the Teenager finally got it.",
                BasicClearReason = "Outside at last. The carpet survives.",
                ItemColor = new Color(1f, 0.82f, 0.3f),
                ItemAccentColor = new Color(0.35f, 0.9f, 1f),
                ItemSecondaryColor = new Color(0.75f, 0.45f, 1f),
                ItemPopColor = new Color(1f, 0.95f, 0.55f)
            };
    }
}
