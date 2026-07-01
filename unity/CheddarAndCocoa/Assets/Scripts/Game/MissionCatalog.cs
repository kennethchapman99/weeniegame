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

        internal static GameManager.MissionDefinition ApplyPresentationMetadata(GameManager.MissionDefinition definition)
        {
            if (definition == null) return null;
            var profile = PresentationProfileFor(definition.Variant);
            definition.RoleHint = profile.RoleHint;
            definition.MechanicTag = profile.MechanicTag;
            definition.SceneCue = profile.SceneCue;
            definition.ReusablePresentation = profile.ReusablePresentation;
            definition.RequiredReadability = ReadabilityRequirement.ObjectiveVisible |
                                             ReadabilityRequirement.ScoreVisible |
                                             ReadabilityRequirement.RoleLabelVisible |
                                             ReadabilityRequirement.ReplayVisible |
                                             ReadabilityRequirement.DogIdentityReadable |
                                             profile.ExtraRequirements;
            return definition;
        }

        private static MissionPresentationProfile PresentationProfileFor(GameManager.MissionVariant variant)
        {
            const string dogs = "Shared Cheddar/Cocoa dog rigs, bark VFX, objective arrows, and reusable pose-state animation.";
            const string threats = "Shared Cheddar/Cocoa rigs plus squirrel/eagle/coyote motion actors, bark VFX, and objective arrows.";

            switch (variant)
            {
                case GameManager.MissionVariant.BackyardRescue:
                    return new MissionPresentationProfile("Cheddar pressures first; Cocoa holds and recovers, then the trap roles flip.", "Rescue + bait-and-switch", "Backyard lawn, fence gap, rope, and predator lanes", threats, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.SnackHeist:
                    return new MissionPresentationProfile("Either dog can stash snacks while the partner guards the squirrel lane.", "Steal + defend", "Snack district with plates, crumbs, and stash cues", threats, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.SockPanic:
                    return new MissionPresentationProfile("One dog tips the basket; the partner dives for the exposed sock.", "Hold-and-release", "Laundry district with basket, sock, and decoy cues", dogs);
                case GameManager.MissionVariant.SquirrelConspiracy:
                    return new MissionPresentationProfile("Nearest dog pressures the squirrel while the partner holds the active cutoff.", "Chase + cutoff", "Fence route, stash reveal, and cutoff markers", threats, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.EagleShadowPanic:
                    return new MissionPresentationProfile("Both dogs hide, then Cocoa rescues Cheddar before the united-bark finish.", "Hide + rescue", "Eagle sweep lane, cover bands, and rescue circle", threats, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.CoyotesFence:
                    return new MissionPresentationProfile("One dog bark-pins the coyote while the partner fills the weak spot.", "Defend + repair", "Fence pressure lane, weak gaps, and fake lure", threats, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.WeenieRoundup:
                    return new MissionPresentationProfile("Each dog carries loose weenies to the shared bowl without fumbling.", "Carry + deliver", "Bowl lane with loose/carry weenie markers", dogs);
                case GameManager.MissionVariant.ScentSearch:
                    return new MissionPresentationProfile("Use bark-sniff heat cues, then commit to the right dig spot.", "Sniff + dig", "Scent patches, mounds, and dig-spot grass", dogs);
                case GameManager.MissionVariant.ThunderstormComfort:
                    return new MissionPresentationProfile("Stay close, comfort panic spikes, and bark together through thunder.", "Comfort + calm", "Storm band, huddle zone, and emotion feedback", dogs, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.MarkTheYard:
                    return new MissionPresentationProfile("Split up to claim zones, then regroup coverage before the squirrel reclaims.", "Territory control", "Central lawn zones with reclaim feedback", threats, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.LeashWalk:
                    return new MissionPresentationProfile("Move as a tethered pair and negotiate each checkpoint without snapping.", "Tethered traversal", "Route stones, leash dashes, and checkpoint markers", dogs, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.CarRide:
                    return new MissionPresentationProfile("Counter-lean together as lurches threaten the back-seat balance.", "Balance + timing", "Car silhouette, lurch cues, and spill meter", dogs, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.GateCrash:
                    return new MissionPresentationProfile("Cocoa braces the gate while Cheddar squeezes through for the toy.", "Hold-and-release", "Gate, toy, brace pad, and squeeze lane", dogs, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.TableStealth:
                    return new MissionPresentationProfile("One dog distracts the human while the partner sneaks the steak.", "Distract-and-sneak", "Table, human attention, steak, and exposure reads", dogs, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.SquirrelSwitcheroo:
                    return new MissionPresentationProfile("One dog sells the decoy while the partner raids the real stash.", "Bait-and-switch", "Decoy, stash, squirrel route, and backfire cues", threats, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.WalkCampaign:
                    return new MissionPresentationProfile("Split the door stare and leash presentation until the human gets it.", "Social manipulation", "Human, door, leash, and comprehension meter", dogs);
                case GameManager.MissionVariant.BoneRelay:
                    return new MissionPresentationProfile("One dog reads the scent relay while the partner digs the matching mound.", "Smell-and-act", "Scent zone, mounds, bone, and relay arrows", dogs);
                case GameManager.MissionVariant.GreatEscape:
                    return new MissionPresentationProfile("Alternate station ownership through the escape contraption chain.", "Sequence chain", "Contraption stations, owner colors, and botch cues", dogs, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.ChaosMachine:
                    return new MissionPresentationProfile("Pull the lever, then split through machine junctions before they reset.", "Chaos machine", "Machine stages, junction pads, and cause/effect pops", dogs, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.BlanketCatch:
                    return new MissionPresentationProfile("Stretch the blanket together and catch the falling prize in the shared span.", "Long-dog geometry", "Blanket span, falling object, and catch lane", dogs);
                case GameManager.MissionVariant.KitchenFoodFrenzy:
                    return new MissionPresentationProfile("Cheddar pops food loose; Cocoa reads the warning circle and catches gold.", "Bark + catch relay", "Counter, bowl, warning circle, and dinner-rush callouts", dogs, ReadabilityRequirement.WarningVisible);
                case GameManager.MissionVariant.OperationPeeBreak:
                    return new MissionPresentationProfile("Cocoa stares, Cheddar carries/blocks, then both unite-bark the door open.", "Social manipulation", "Couch, phone, leash, charger, door, and relief payoff", dogs, ReadabilityRequirement.WarningVisible);
                default:
                    return new MissionPresentationProfile("Cheddar and Cocoa use their shared dog verbs together.", "Co-op dog mission", "Readable greybox arena with objective markers", dogs);
            }
        }

        private readonly struct MissionPresentationProfile
        {
            public readonly string RoleHint;
            public readonly string MechanicTag;
            public readonly string SceneCue;
            public readonly string ReusablePresentation;
            public readonly ReadabilityRequirement ExtraRequirements;

            public MissionPresentationProfile(
                string roleHint,
                string mechanicTag,
                string sceneCue,
                string reusablePresentation,
                ReadabilityRequirement extraRequirements = ReadabilityRequirement.None)
            {
                RoleHint = roleHint;
                MechanicTag = mechanicTag;
                SceneCue = sceneCue;
                ReusablePresentation = reusablePresentation;
                ExtraRequirements = extraRequirements;
            }
        }

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

        internal static GameManager.MissionDefinition BuildGateCrashDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.GateCrash;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Gate Crash",
                IntroPrompt = "The toy rolled under the heavy gate. Cocoa has to brace the gate open while Cheddar squeezes through to grab it - if she lets go mid-squeeze, the gate snaps shut.",
                ReadyScoreLabel = "READY TO CRASH THE GATE",
                ItemRootName = "Gate",
                ItemObjectName = "Gate",
                ItemWorldLabel = "Hold!",
                ItemArrowLabel = "GATE",
                ItemCollectCueNoun = "a squeeze-through",
                CollectObjectiveFormat = "Squeeze through {0}/{1}",
                CollectedScoreLabel = "SQUEEZED THROUGH",
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
                SquirrelObjectiveText = "Hold the gate / squeeze through",
                SquirrelStealingCue = "No squirrel here - mind the gate.",
                SquirrelStoleCue = "No squirrel here - hold it open.",
                SquirrelStealScoreLabel = "GATE SNAP",
                SquirrelScareScoreLabel = "SQUEEZED THROUGH",
                SquirrelStealingActorLabel = "GATE",
                SquirrelDroppedActorLabel = "GATE HELD",
                SquirrelStoleActorLabel = "GATE SNAP",
                SquirrelMissPopLabel = "SNAP!",
                SquirrelStealJuiceLabel = "GATE SNAP!",
                SquirrelScareJuiceLabel = "SQUEEZED THROUGH!",
                TugObjectiveText = "Hold the gate together",
                WaitingObjectiveText = "Squeeze through while the gate is held",
                ClearObjectiveText = "Toy rescued - replay Gate Crash",
                ClearBannerPrefix = "GATE CRASHED!",
                ClearScoreLabel = "GATE CRASH CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay Gate Crash",
                FailObjectiveText = "Mission failed - replay Gate Crash",
                GenericFailReason = "Needs steadier holding before the next squeeze.",
                TimeFailReason = "The squeeze took too long and the toy stayed stuck.",
                StolenFailReason = "The gate snapped shut too many times.",
                PredatorFailReason = "No predator here, just a heavy gate.",
                PawfectClearReason = "Cocoa braced like a champ and Cheddar slipped through in one clean go.",
                HeroClearReason = "The toy came home with only a wobble of the gate.",
                BasicClearReason = "They got the toy, even if the gate slammed a couple of times.",
                ItemColor = new Color(0.55f, 0.45f, 0.3f),
                ItemAccentColor = new Color(0.8f, 0.65f, 0.4f),
                ItemSecondaryColor = new Color(0.26f, 0.2f, 0.12f),
                ItemPopColor = new Color(0.6f, 0.8f, 1f)
            };
        }

        internal static GameManager.MissionDefinition BuildTableStealthDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.TableStealth;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Table Stealth",
                IntroPrompt = "A steak dropped under the dinner table. One dog has to hold the human's attention - Cocoa flops belly-up for a rub, Cheddar burps a cloud - while the other sneaks the steak. Sneak while the human is watching and you get spotted.",
                ReadyScoreLabel = "READY TO RAID THE TABLE",
                ItemRootName = "Steak",
                ItemObjectName = "Steak",
                ItemWorldLabel = "Sneak!",
                ItemArrowLabel = "STEAK",
                ItemCollectCueNoun = "a clean sneak",
                CollectObjectiveFormat = "Sneak the steak {0}/{1}",
                CollectedScoreLabel = "STEAK SNEAKED",
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
                SquirrelObjectiveText = "Distract the human / sneak the steak",
                SquirrelStealingCue = "No squirrel here - mind the human.",
                SquirrelStoleCue = "No squirrel here - keep them distracted.",
                SquirrelStealScoreLabel = "SPOTTED",
                SquirrelScareScoreLabel = "STEAK SNEAKED",
                SquirrelStealingActorLabel = "HUMAN",
                SquirrelDroppedActorLabel = "DISTRACTED",
                SquirrelStoleActorLabel = "SPOTTED",
                SquirrelMissPopLabel = "SPOTTED!",
                SquirrelStealJuiceLabel = "SPOTTED!",
                SquirrelScareJuiceLabel = "STEAK SNEAKED!",
                TugObjectiveText = "Distract and sneak together",
                WaitingObjectiveText = "Sneak the steak while the human is distracted",
                ClearObjectiveText = "Steak rescued - replay Table Stealth",
                ClearBannerPrefix = "STEAK SNEAKED!",
                ClearScoreLabel = "TABLE STEALTH CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay Table Stealth",
                FailObjectiveText = "Mission failed - replay Table Stealth",
                GenericFailReason = "Needs a steadier distraction before the next sneak.",
                TimeFailReason = "The sneak took too long and the steak stayed under the table.",
                StolenFailReason = "The human caught the sneaky pup too many times.",
                PredatorFailReason = "No predator here, just a hungry human.",
                PawfectClearReason = "Cocoa held the human spellbound and Cheddar lifted the steak without a sound.",
                HeroClearReason = "The steak came home with only a nervous glance or two.",
                BasicClearReason = "They got the steak, even if the human nearly caught them.",
                ItemColor = new Color(0.6f, 0.3f, 0.28f),
                ItemAccentColor = new Color(0.85f, 0.5f, 0.4f),
                ItemSecondaryColor = new Color(0.3f, 0.14f, 0.12f),
                ItemPopColor = new Color(0.6f, 0.8f, 1f)
            };
        }

        internal static GameManager.MissionDefinition BuildSquirrelSwitcherooDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.SquirrelSwitcheroo;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "The Ol' Switcheroo",
                IntroPrompt = "The squirrel is guarding its buried stash and won't leave. Cheddar feints at a decoy nut pile to bait it into a chase - and only while the squirrel is committed to the decoy can Cocoa raid the real stash. Over-feint and the squirrel wises up (or Cheddar bolts after his own decoy) and the window snaps shut.",
                ReadyScoreLabel = "READY TO PULL THE SWITCHEROO",
                ItemRootName = "Stash",
                ItemObjectName = "Stash",
                ItemWorldLabel = "Raid!",
                ItemArrowLabel = "STASH",
                ItemCollectCueNoun = "a stash raid",
                CollectObjectiveFormat = "Raid the stash {0}/{1}",
                CollectedScoreLabel = "STASH RAIDED",
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
                SquirrelObjectiveText = "Bait the squirrel / raid the stash",
                SquirrelStealingCue = "The squirrel is guarding the stash - bait it off.",
                SquirrelStoleCue = "The squirrel bolted back - feint the decoy again.",
                SquirrelStealScoreLabel = "BAIT BACKFIRE",
                SquirrelScareScoreLabel = "STASH RAIDED",
                SquirrelStealingActorLabel = "SQUIRREL",
                SquirrelDroppedActorLabel = "CHASING DECOY",
                SquirrelStoleActorLabel = "WISED UP",
                SquirrelMissPopLabel = "WISED UP!",
                SquirrelStealJuiceLabel = "BACKFIRE!",
                SquirrelScareJuiceLabel = "SWITCHEROO!",
                TugObjectiveText = "Bait and raid together",
                WaitingObjectiveText = "Raid the stash while the squirrel chases the decoy",
                ClearObjectiveText = "Stash raided - replay The Ol' Switcheroo",
                ClearBannerPrefix = "SWITCHEROO!",
                ClearScoreLabel = "SWITCHEROO CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay The Ol' Switcheroo",
                FailObjectiveText = "Mission failed - replay The Ol' Switcheroo",
                GenericFailReason = "Needs a cleaner feint before the next raid.",
                TimeFailReason = "The squirrel never fully committed and the stash stayed buried.",
                StolenFailReason = "The squirrel wised up to the bait too many times.",
                PredatorFailReason = "No predator here, just a cagey squirrel.",
                PawfectClearReason = "Cheddar sold the feint and Cocoa cleaned out the stash without a single backfire.",
                HeroClearReason = "The stash came home with only a wised-up glance or two.",
                BasicClearReason = "They got the stash, even if the squirrel caught on a few times.",
                ItemColor = new Color(0.5f, 0.38f, 0.24f),
                ItemAccentColor = new Color(0.8f, 0.62f, 0.34f),
                ItemSecondaryColor = new Color(0.28f, 0.2f, 0.1f),
                ItemPopColor = new Color(0.45f, 0.9f, 0.55f)
            };
        }

        internal static GameManager.MissionDefinition BuildWalkCampaignDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.WalkCampaign;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "The Walk Campaign",
                IntroPrompt = "The human won't take the hint. Send ONE clear message together - Cocoa fixes the door with a dignified stare while Cheddar presents the leash - and hold it until the human gets it. Cover only one (or wander off) and they get confused and bring the wrong thing.",
                ReadyScoreLabel = "READY TO DEMAND A WALK",
                ItemRootName = "Walk",
                ItemObjectName = "Walk",
                ItemWorldLabel = "Walkies!",
                ItemArrowLabel = "HUMAN",
                ItemCollectCueNoun = "the human's attention",
                CollectObjectiveFormat = "Convince the human {0}/{1}",
                CollectedScoreLabel = "WALK CONNED",
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
                SquirrelObjectiveText = "Send the human one clear message",
                SquirrelStealingCue = "The human's not getting it - send one message.",
                SquirrelStoleCue = "Mixed signals - the human brought the wrong thing.",
                SquirrelStealScoreLabel = "HUMAN CONFUSED",
                SquirrelScareScoreLabel = "THEY'RE GETTING IT",
                SquirrelStealingActorLabel = "HUMAN",
                SquirrelDroppedActorLabel = "GETTING IT",
                SquirrelStoleActorLabel = "CONFUSED",
                SquirrelMissPopLabel = "WRONG THING!",
                SquirrelStealJuiceLabel = "CONFUSED!",
                SquirrelScareJuiceLabel = "GETTING IT!",
                TugObjectiveText = "Door-stare and leash together",
                WaitingObjectiveText = "Hold the door-stare and the leash at the same time",
                ClearObjectiveText = "Walk secured - replay The Walk Campaign",
                ClearBannerPrefix = "WALKIES!",
                ClearScoreLabel = "WALK CONNED",
                ReplayPrompt = "Press R / Enter / Start to replay The Walk Campaign",
                FailObjectiveText = "Mission failed - replay The Walk Campaign",
                GenericFailReason = "The human needs a clearer, steadier message.",
                TimeFailReason = "The human never quite got the message in time.",
                StolenFailReason = "Too many mixed signals confused the human.",
                PredatorFailReason = "No predator here, just a clueless human.",
                PawfectClearReason = "Cocoa and Cheddar sent one flawless message and the leash came out on the first ask.",
                HeroClearReason = "The human got it after only a confused glance or two.",
                BasicClearReason = "They got their walk, even if the human fetched the wrong thing a few times first.",
                ItemColor = new Color(0.55f, 0.7f, 0.95f),
                ItemAccentColor = new Color(0.8f, 0.85f, 0.5f),
                ItemSecondaryColor = new Color(0.3f, 0.35f, 0.5f),
                ItemPopColor = new Color(0.5f, 0.9f, 0.55f)
            };
        }

        internal static GameManager.MissionDefinition BuildChaosMachineDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.ChaosMachine;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "The Rube Goldberg",
                IntroPrompt = "The dogs rigged a backyard contraption: towel drop, basket tip, toy launch. Pre-position at your junctions, then pull the lever - the cascade runs ITSELF, but each junction has a split-second window where its owner dog must be in place or the machine misfires and jams there. A re-pull resumes from the jam. Trust the timing and cover your junctions.",
                ReadyScoreLabel = "READY TO RIG THE MACHINE",
                ItemRootName = "Junction",
                ItemObjectName = "Junction",
                ItemWorldLabel = "Whirr!",
                ItemArrowLabel = "JUNCTION",
                ItemCollectCueNoun = "a cascade junction",
                CollectObjectiveFormat = "Run the cascade {0}/{1}",
                CollectedScoreLabel = "CASCADE ROLLED",
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
                SquirrelObjectiveText = "Cover your junctions, pull the lever",
                SquirrelStealingCue = "Pull the lever to start the cascade.",
                SquirrelStoleCue = "Misfire - re-pull from the jam.",
                SquirrelStealScoreLabel = "MISFIRE",
                SquirrelScareScoreLabel = "CASCADE ROLLED",
                SquirrelStealingActorLabel = "MACHINE",
                SquirrelDroppedActorLabel = "WHIRR",
                SquirrelStoleActorLabel = "JAMMED",
                SquirrelMissPopLabel = "STUCK!",
                SquirrelStealJuiceLabel = "MISFIRE!",
                SquirrelScareJuiceLabel = "WHIRR!",
                TugObjectiveText = "Run the machine together",
                WaitingObjectiveText = "Be at your junction when the cascade arrives",
                ClearObjectiveText = "Cascade complete - replay The Rube Goldberg",
                ClearBannerPrefix = "KA-CHUNK!",
                ClearScoreLabel = "RUBE GOLDBERG CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay The Rube Goldberg",
                FailObjectiveText = "Mission failed - replay The Rube Goldberg",
                GenericFailReason = "The cascade needs cleaner timing at the junctions.",
                TimeFailReason = "The cascade never reached the end in time.",
                StolenFailReason = "Too many misfires jammed the machine for good.",
                PredatorFailReason = "No predator here, just a temperamental contraption.",
                PawfectClearReason = "Cheddar and Cocoa nailed every junction window - the cascade ran end to end without a single misfire.",
                HeroClearReason = "The cascade finished after only a misfire or two.",
                BasicClearReason = "They got the cascade through, even after a few jams.",
                ItemColor = new Color(0.62f, 0.64f, 0.7f),
                ItemAccentColor = new Color(0.85f, 0.6f, 0.35f),
                ItemSecondaryColor = new Color(0.32f, 0.34f, 0.4f),
                ItemPopColor = new Color(0.6f, 0.85f, 0.95f)
            };
        }

        internal static GameManager.MissionDefinition BuildGreatEscapeDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.GreatEscape;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "The Great Escape",
                IntroPrompt = "Zoomies time - but the gate's latched. Run the contraption chain in order, taking turns: Cocoa paws the latch, Cheddar shoulders the gate, Cocoa drags the cooler to prop it, Cheddar squeezes through. The active station glows for whoever's turn it is. Wrong dog or wrong order does nothing; dawdle and the contraption eases back a step.",
                ReadyScoreLabel = "READY TO BUST OUT",
                ItemRootName = "Step",
                ItemObjectName = "Step",
                ItemWorldLabel = "Clunk!",
                ItemArrowLabel = "STEP",
                ItemCollectCueNoun = "a contraption step",
                CollectObjectiveFormat = "Work the contraption {0}/{1}",
                CollectedScoreLabel = "CONTRAPTION STEP",
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
                SquirrelObjectiveText = "Take turns down the chain",
                SquirrelStealingCue = "Whose turn is it? Read the glowing station.",
                SquirrelStoleCue = "Botched it - reset to the glowing step.",
                SquirrelStealScoreLabel = "CONTRAPTION FUMBLE",
                SquirrelScareScoreLabel = "CONTRAPTION STEP",
                SquirrelStealingActorLabel = "CONTRAPTION",
                SquirrelDroppedActorLabel = "CLUNK",
                SquirrelStoleActorLabel = "JAMMED",
                SquirrelMissPopLabel = "CLANK!",
                SquirrelStealJuiceLabel = "CLANK!",
                SquirrelScareJuiceLabel = "CLUNK!",
                TugObjectiveText = "Run the chain together",
                WaitingObjectiveText = "Do your step when the station glows for you",
                ClearObjectiveText = "Busted out - replay The Great Escape",
                ClearBannerPrefix = "JAILBREAK!",
                ClearScoreLabel = "GREAT ESCAPE CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay The Great Escape",
                FailObjectiveText = "Mission failed - replay The Great Escape",
                GenericFailReason = "The chain needs cleaner hand-offs before the gate gives.",
                TimeFailReason = "The contraption never made it down the chain in time.",
                StolenFailReason = "Too many botched hand-offs jammed the contraption.",
                PredatorFailReason = "No predator here, just a stubborn latch.",
                PawfectClearReason = "Cheddar and Cocoa ran the whole chain clean, hand-off after hand-off, straight out the gate.",
                HeroClearReason = "They busted out with only a clank or two along the way.",
                BasicClearReason = "They got out, even after the contraption jammed a few times.",
                ItemColor = new Color(0.6f, 0.62f, 0.68f),
                ItemAccentColor = new Color(0.8f, 0.7f, 0.4f),
                ItemSecondaryColor = new Color(0.32f, 0.34f, 0.4f),
                ItemPopColor = new Color(0.6f, 0.85f, 0.95f)
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

        internal static GameManager.MissionDefinition BuildBoneRelayDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.BoneRelay;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "The Bone Detail",
                IntroPrompt = "Four look-alike dirt mounds, only one hiding the real bone. Cocoa's the nose: she sniffs the scent post to call which mound is real. Cheddar's the only one who can dig - but he can't tell them apart, so he has to wait for her call. Dig blind or dig a decoy and it's a wasted dig; waste too many and the team gives up. Each bone re-buries the next somewhere new.",
                ReadyScoreLabel = "READY TO WORK THE SCENT",
                ItemRootName = "Bone",
                ItemObjectName = "Bone",
                ItemWorldLabel = "Bone!",
                ItemArrowLabel = "MOUND",
                ItemCollectCueNoun = "a buried bone",
                CollectObjectiveFormat = "Dig up the bones {0}/{1}",
                CollectedScoreLabel = "BONE DUG UP",
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
                SquirrelObjectiveText = "Read the scent / dig the call",
                SquirrelStealingCue = "Wait for Cocoa to call the mound.",
                SquirrelStoleCue = "Wasted dig - read the call first.",
                SquirrelStealScoreLabel = "COLD DIG",
                SquirrelScareScoreLabel = "BONE DUG UP",
                SquirrelStealingActorLabel = "SCENT POST",
                SquirrelDroppedActorLabel = "CALLING IT",
                SquirrelStoleActorLabel = "COLD",
                SquirrelMissPopLabel = "NOPE!",
                SquirrelStealJuiceLabel = "NOPE!",
                SquirrelScareJuiceLabel = "BONE!",
                TugObjectiveText = "Sniff and dig together",
                WaitingObjectiveText = "Dig the mound Cocoa calls",
                ClearObjectiveText = "Bones recovered - replay The Bone Detail",
                ClearBannerPrefix = "BONES!",
                ClearScoreLabel = "BONE DETAIL CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay The Bone Detail",
                FailObjectiveText = "Mission failed - replay The Bone Detail",
                GenericFailReason = "Too much guessing - read the scent before digging.",
                TimeFailReason = "The bones stayed buried - the relay never clicked in time.",
                StolenFailReason = "Too many blind digs wasted the whole yard.",
                PredatorFailReason = "No predator here, just a lot of dirt.",
                PawfectClearReason = "Cocoa called every mound and Cheddar dug them clean - not a single wasted scoop.",
                HeroClearReason = "The bones came up with only a stray dig or two.",
                BasicClearReason = "They got the bones, even after digging up a good chunk of the yard.",
                ItemColor = new Color(0.5f, 0.38f, 0.22f),
                ItemAccentColor = new Color(0.78f, 0.7f, 0.5f),
                ItemSecondaryColor = new Color(0.3f, 0.22f, 0.12f),
                ItemPopColor = new Color(0.5f, 0.9f, 0.55f)
            };
        }

        internal static GameManager.MissionDefinition BuildBlanketCatchDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.BlanketCatch;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "The Blanket Catch",
                IntroPrompt = "Food's tumbling off the counter! Cheddar and Cocoa each grab a corner of a blanket and stretch it between them - too close and it sags, too far and it RIPS, so find the taut band. Then slide the middle of the blanket under each falling snack to catch it. Rip the blanket too many times and it's done.",
                ReadyScoreLabel = "READY TO CATCH SOME SNACKS",
                ItemRootName = "Snack",
                ItemObjectName = "Snack",
                ItemWorldLabel = "Catch!",
                ItemArrowLabel = "SNACK",
                ItemCollectCueNoun = "a caught snack",
                CollectObjectiveFormat = "Catch the falling snacks {0}/{1}",
                CollectedScoreLabel = "SNACK CAUGHT",
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
                SquirrelObjectiveText = "Stretch the blanket / catch the food",
                SquirrelStealingCue = "Find the taut band and get under the snack.",
                SquirrelStoleCue = "Missed - re-center the blanket.",
                SquirrelStealScoreLabel = "BLANKET RIP",
                SquirrelScareScoreLabel = "SNACK CAUGHT",
                SquirrelStealingActorLabel = "BLANKET",
                SquirrelDroppedActorLabel = "CAUGHT",
                SquirrelStoleActorLabel = "RIPPED",
                SquirrelMissPopLabel = "SPLAT!",
                SquirrelStealJuiceLabel = "RIP!",
                SquirrelScareJuiceLabel = "CAUGHT!",
                TugObjectiveText = "Stretch the blanket together",
                WaitingObjectiveText = "Slide the taut blanket under the next snack",
                ClearObjectiveText = "Snacks caught - replay The Blanket Catch",
                ClearBannerPrefix = "DINNER!",
                ClearScoreLabel = "BLANKET CATCH CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay The Blanket Catch",
                FailObjectiveText = "Mission failed - replay The Blanket Catch",
                GenericFailReason = "The blanket needs a steadier taut span before the next snack.",
                TimeFailReason = "The snacks all hit the floor before the blanket caught enough.",
                StolenFailReason = "The blanket ripped one too many times.",
                PredatorFailReason = "No predator here, just gravity and snacks.",
                PawfectClearReason = "Cheddar and Cocoa held a perfect taut blanket and caught every snack without a single tear.",
                HeroClearReason = "They caught the haul with only a small tear or two.",
                BasicClearReason = "They got dinner, even if the blanket took a beating.",
                ItemColor = new Color(0.95f, 0.8f, 0.4f),
                ItemAccentColor = new Color(0.5f, 0.85f, 0.55f),
                ItemSecondaryColor = new Color(0.4f, 0.3f, 0.15f),
                ItemPopColor = new Color(0.5f, 0.95f, 0.55f)
            };
        }

        internal static GameManager.MissionDefinition BuildLeashWalkDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.LeashWalk;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Walkies on the Leash",
                IntroPrompt = "Cheddar + Cocoa are clipped to one leash. Walk through every checkpoint together without drifting so far apart the leash snaps.",
                ReadyScoreLabel = "READY FOR WALKIES",
                ItemRootName = "Checkpoints",
                ItemObjectName = "Checkpoint",
                ItemWorldLabel = "Walk!",
                ItemArrowLabel = "WALK",
                ItemCollectCueNoun = "a checkpoint",
                CollectObjectiveFormat = "Reach checkpoints {0}/{1}",
                CollectedScoreLabel = "CHECKPOINT",
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
                SquirrelObjectiveText = "Walk the leash together",
                SquirrelStealingCue = "No squirrel here - mind the leash.",
                SquirrelStoleCue = "No squirrel here - stay close.",
                SquirrelStealScoreLabel = "LEASH SNAP",
                SquirrelScareScoreLabel = "CHECKPOINT",
                SquirrelStealingActorLabel = "LEASH",
                SquirrelDroppedActorLabel = "CHECKPOINT",
                SquirrelStoleActorLabel = "LEASH SNAP",
                SquirrelMissPopLabel = "SNAP!",
                SquirrelStealJuiceLabel = "LEASH SNAP!",
                SquirrelScareJuiceLabel = "CHECKPOINT!",
                TugObjectiveText = "Walk to the checkpoint together",
                WaitingObjectiveText = "Walk to the last checkpoint together",
                ClearObjectiveText = "Great walk - replay Walkies on the Leash",
                ClearBannerPrefix = "WALK COMPLETE!",
                ClearScoreLabel = "WALK COMPLETE",
                ReplayPrompt = "Press R / Enter / Start to replay Walkies on the Leash",
                FailObjectiveText = "Mission failed - replay Walkies on the Leash",
                GenericFailReason = "Needs the dogs to walk more in sync next time.",
                TimeFailReason = "The walk dragged on and the checkpoints never lined up.",
                StolenFailReason = "The leash snapped taut too many times.",
                PredatorFailReason = "No predator here, just a tangled leash.",
                PawfectClearReason = "Two pups walked the whole route in perfect lockstep - dream walkies.",
                HeroClearReason = "Every checkpoint reached with the leash mostly slack.",
                BasicClearReason = "They finished the walk, even if the leash got dramatic.",
                ItemColor = new Color(0.45f, 0.6f, 0.85f),
                ItemAccentColor = new Color(0.7f, 0.82f, 1f),
                ItemSecondaryColor = new Color(0.18f, 0.24f, 0.36f),
                ItemPopColor = new Color(0.6f, 0.8f, 1f)
            };
        }

        internal static GameManager.MissionDefinition BuildSockPanicDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.SockPanic;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Sock Panic",
                IntroPrompt = "Tip the laundry basket, then have the partner dive for each exposed sock before laundry order returns.",
                ReadyScoreLabel = "READY TO PANIC ABOUT SOCKS",
                ItemRootName = "Scattered Socks",
                ItemObjectName = "Panic Sock",
                ItemWorldLabel = "Sock!",
                ItemArrowLabel = "SOCK",
                ItemCollectCueNoun = "a dramatic sock",
                CollectObjectiveFormat = "Return socks {0}/{1}",
                CollectedScoreLabel = "PARTNER SOCK DIVE",
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
                SquirrelObjectiveText = "No squirrel - find socks",
                SquirrelStealingCue = "No squirrel in Sock Panic.",
                SquirrelStoleCue = "No squirrel in Sock Panic.",
                SquirrelStealScoreLabel = "SOCK CONFUSION",
                SquirrelScareScoreLabel = "SOCK BARK",
                SquirrelStealingActorLabel = "SQUIRREL OFF DUTY",
                SquirrelDroppedActorLabel = "SQUIRREL OFF DUTY",
                SquirrelStoleActorLabel = "SQUIRREL OFF DUTY",
                SquirrelMissPopLabel = "MISS! -SOCK",
                SquirrelStealJuiceLabel = "MISS! SOCK PANIC",
                SquirrelScareJuiceLabel = "SOCK BARK POP!",
                TugObjectiveText = "Return the socks",
                WaitingObjectiveText = "Find the last runaway sock",
                ClearObjectiveText = "Sock panic solved - replay Sock Panic",
                ClearBannerPrefix = "SOCKS SORTED!",
                ClearScoreLabel = "SOCK PANIC CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay Sock Panic",
                FailObjectiveText = "Mission failed - replay Sock Panic",
                GenericFailReason = "Needs more sock urgency before laundry patrol.",
                TimeFailReason = "Laundry order returned before the final sock was rescued.",
                StolenFailReason = "No squirrel stole socks; the dogs simply lost the plot.",
                PredatorFailReason = "No predator here, just laundry pressure.",
                PawfectClearReason = "Tiny legends restored sock civilization.",
                HeroClearReason = "The laundry pile survived with only mild drama.",
                BasicClearReason = "The socks came home looking emotionally handled.",
                ItemColor = new Color(0.42f, 0.72f, 1f),
                ItemAccentColor = new Color(1f, 0.88f, 0.28f),
                ItemSecondaryColor = new Color(0.12f, 0.42f, 0.72f),
                ItemPopColor = new Color(0.62f, 0.9f, 1f)
            };
        }

        internal static GameManager.MissionDefinition BuildCarRideDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.CarRide;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Car Ride Balance",
                IntroPrompt = "Cheddar + Cocoa are riding in the back of the car. As it lurches around corners, lean to opposite sides to keep it level all the way home.",
                ReadyScoreLabel = "READY FOR THE CAR RIDE",
                ItemRootName = "Lurches",
                ItemObjectName = "Lurch",
                ItemWorldLabel = "Lean!",
                ItemArrowLabel = "LEAN",
                ItemCollectCueNoun = "a steadied lurch",
                CollectObjectiveFormat = "Steady lurches {0}/{1}",
                CollectedScoreLabel = "STEADIED",
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
                SquirrelObjectiveText = "Lean to keep the car level",
                SquirrelStealingCue = "No squirrel here - mind the tilt.",
                SquirrelStoleCue = "No squirrel here - keep it level.",
                SquirrelStealScoreLabel = "SPILL",
                SquirrelScareScoreLabel = "STEADIED",
                SquirrelStealingActorLabel = "CAR TILT",
                SquirrelDroppedActorLabel = "STEADIED",
                SquirrelStoleActorLabel = "SPILL",
                SquirrelMissPopLabel = "SPILL!",
                SquirrelStealJuiceLabel = "SPILL!",
                SquirrelScareJuiceLabel = "STEADIED!",
                TugObjectiveText = "Keep the car level together",
                WaitingObjectiveText = "Hold it level until the ride ends",
                ClearObjectiveText = "Made it home - replay Car Ride Balance",
                ClearBannerPrefix = "MADE IT HOME!",
                ClearScoreLabel = "RIDE COMPLETE",
                ReplayPrompt = "Press R / Enter / Start to replay Car Ride Balance",
                FailObjectiveText = "Mission failed - replay Car Ride Balance",
                GenericFailReason = "Needs smoother counter-leaning before the next drive.",
                TimeFailReason = "The drive dragged on and the car never settled.",
                StolenFailReason = "The car tipped over one too many times.",
                PredatorFailReason = "No predator here, just questionable driving.",
                PawfectClearReason = "Two pups counter-balanced every corner - smoothest ride ever.",
                HeroClearReason = "The car made it home level with only a wobble or two.",
                BasicClearReason = "They got home, even if the snacks slid around a bit.",
                ItemColor = new Color(0.5f, 0.42f, 0.32f),
                ItemAccentColor = new Color(0.8f, 0.7f, 0.5f),
                ItemSecondaryColor = new Color(0.24f, 0.2f, 0.14f),
                ItemPopColor = new Color(0.85f, 0.75f, 0.55f)
            };
        }

        internal static GameManager.MissionDefinition BuildScentSearchDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.ScentSearch;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Scent Search",
                IntroPrompt = "Cheddar + Cocoa must sniff out the buried bones (bark to sniff HOT/COLD) and dig them up (interact) before the yard is a mess.",
                ReadyScoreLabel = "READY TO SNIFF",
                ItemRootName = "Dig Spots",
                ItemObjectName = "Dig Spot",
                ItemWorldLabel = "Dig?",
                ItemArrowLabel = "DIG",
                ItemCollectCueNoun = "a buried bone",
                CollectObjectiveFormat = "Dig up bones {0}/{1}",
                CollectedScoreLabel = "BONE DUG UP",
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
                SquirrelObjectiveText = "Sniff out the buried bones",
                SquirrelStealingCue = "No squirrel here - follow the bone scent.",
                SquirrelStoleCue = "No squirrel here - keep sniffing.",
                SquirrelStealScoreLabel = "COLD DIG",
                SquirrelScareScoreLabel = "HOT SNIFF",
                SquirrelStealingActorLabel = "SCENT TRAIL",
                SquirrelDroppedActorLabel = "BONE DUG UP",
                SquirrelStoleActorLabel = "COLD HOLE",
                SquirrelMissPopLabel = "COLD!",
                SquirrelStealJuiceLabel = "COLD DIG!",
                SquirrelScareJuiceLabel = "HOT SNIFF!",
                TugObjectiveText = "Dig up the bones",
                WaitingObjectiveText = "Sniff out the last bone together",
                ClearObjectiveText = "All bones found - replay Scent Search",
                ClearBannerPrefix = "BONES UNEARTHED!",
                ClearScoreLabel = "SEARCH COMPLETE",
                ReplayPrompt = "Press R / Enter / Start to replay Scent Search",
                FailObjectiveText = "Mission failed - replay Scent Search",
                GenericFailReason = "Needs sharper sniffing before digging next time.",
                TimeFailReason = "The bones stayed buried until the clock ran out.",
                StolenFailReason = "The dogs dug too many cold holes chasing bad scents.",
                PredatorFailReason = "No predator here, just a lot of dirt.",
                PawfectClearReason = "Tiny truffle-hounds nosed out every bone with barely a wasted dig.",
                HeroClearReason = "Every bone got unearthed with respectable sniffing discipline.",
                BasicClearReason = "The bones came up, even if the lawn will need reseeding.",
                ItemColor = new Color(0.42f, 0.3f, 0.16f),
                ItemAccentColor = new Color(0.7f, 0.55f, 0.3f),
                ItemSecondaryColor = new Color(0.2f, 0.14f, 0.07f),
                ItemPopColor = new Color(0.95f, 0.9f, 0.7f)
            };
        }

        internal static GameManager.MissionDefinition BuildWeenieRoundupDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.WeenieRoundup;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Weenie Roundup",
                IntroPrompt = "Cheddar + Cocoa must round up every scattered weenie and carry them back to the home bowl together.",
                ReadyScoreLabel = "READY TO ROUND UP WEENIES",
                ItemRootName = "Scattered Weenies",
                ItemObjectName = "Loose Weenie",
                ItemWorldLabel = "Weenie!",
                ItemArrowLabel = "WEENIE",
                ItemCollectCueNoun = "a weenie",
                CollectObjectiveFormat = "Deliver weenies {0}/{1}",
                CollectedScoreLabel = "WEENIE DELIVERED",
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
                SquirrelObjectiveText = "Carry the weenies home",
                SquirrelStealingCue = "No squirrel here - just a lot of weenies to carry.",
                SquirrelStoleCue = "No squirrel here - mind the bowl.",
                SquirrelStealScoreLabel = "FUMBLED WEENIE",
                SquirrelScareScoreLabel = "WEENIE DELIVERED",
                SquirrelStealingActorLabel = "WEENIE LOOSE",
                SquirrelDroppedActorLabel = "WEENIE DELIVERED",
                SquirrelStoleActorLabel = "WEENIE FUMBLED",
                SquirrelMissPopLabel = "FUMBLE!",
                SquirrelStealJuiceLabel = "FUMBLED WEENIE!",
                SquirrelScareJuiceLabel = "WEENIE DELIVERED!",
                TugObjectiveText = "Carry weenies to the bowl",
                WaitingObjectiveText = "Round up the last weenies together",
                ClearObjectiveText = "Bowl is full - replay Weenie Roundup",
                ClearBannerPrefix = "BOWL FILLED!",
                ClearScoreLabel = "ROUNDUP COMPLETE",
                ReplayPrompt = "Press R / Enter / Start to replay Weenie Roundup",
                FailObjectiveText = "Mission failed - replay Weenie Roundup",
                GenericFailReason = "Needs faster weenie hauling before the next mealtime.",
                TimeFailReason = "The clock ran out before every weenie made it to the bowl.",
                StolenFailReason = "Too many weenies got fumbled in the long carry.",
                PredatorFailReason = "No predator here, just a lot of running.",
                PawfectClearReason = "Tiny haulers filled the bowl with zero fumbles - elite weenie logistics.",
                HeroClearReason = "Every weenie made it home with respectable carrying discipline.",
                BasicClearReason = "The bowl got filled, even if a few weenies hit the dirt first.",
                ItemColor = new Color(0.78f, 0.34f, 0.24f),
                ItemAccentColor = new Color(0.95f, 0.6f, 0.4f),
                ItemSecondaryColor = new Color(0.3f, 0.12f, 0.08f),
                ItemPopColor = new Color(1f, 0.6f, 0.4f)
            };
        }

        internal static GameManager.MissionDefinition BuildThunderstormComfortDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.ThunderstormComfort;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Thunderstorm Comfort",
                IntroPrompt = "A storm is rolling in. Cheddar + Cocoa must huddle close to keep each other calm and ride out every thunderclap together.",
                ReadyScoreLabel = "READY TO RIDE OUT THE STORM",
                ItemRootName = "Storm",
                ItemObjectName = "Thunderclap",
                ItemWorldLabel = "Boom!",
                ItemArrowLabel = "HUDDLE",
                ItemCollectCueNoun = "a weathered clap",
                CollectObjectiveFormat = "Weather claps {0}/{1}",
                CollectedScoreLabel = "CLAP WEATHERED",
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
                SquirrelObjectiveText = "Huddle to stay calm",
                SquirrelStealingCue = "No squirrel here - just thunder.",
                SquirrelStoleCue = "No squirrel here - mind the panic.",
                SquirrelStealScoreLabel = "PANIC SPIKE",
                SquirrelScareScoreLabel = "COMFORT HUDDLE",
                SquirrelStealingActorLabel = "STORM CLOUD",
                SquirrelDroppedActorLabel = "CLAP WEATHERED",
                SquirrelStoleActorLabel = "PANIC!",
                SquirrelMissPopLabel = "BOOM!",
                SquirrelStealJuiceLabel = "PANIC SPIKE!",
                SquirrelScareJuiceLabel = "COMFORT HUDDLE!",
                TugObjectiveText = "Huddle through the storm",
                WaitingObjectiveText = "Stay close until the storm passes",
                ClearObjectiveText = "Storm passed - replay Thunderstorm Comfort",
                ClearBannerPrefix = "STORM WEATHERED!",
                ClearScoreLabel = "STORM PASSED",
                ReplayPrompt = "Press R / Enter / Start to replay Thunderstorm Comfort",
                FailObjectiveText = "Mission failed - replay Thunderstorm Comfort",
                GenericFailReason = "Needs tighter huddling before the next storm front.",
                TimeFailReason = "The storm dragged on and the dogs lost their nerve.",
                StolenFailReason = "The panic spiked past the breaking point.",
                PredatorFailReason = "A dog bolted from the thunder.",
                PawfectClearReason = "Two brave pups out-cuddled the whole storm without a single bolt.",
                HeroClearReason = "The storm passed with the dogs keeping each other steady.",
                BasicClearReason = "They rode it out, even if a few claps got hairy.",
                ItemColor = new Color(0.32f, 0.34f, 0.45f),
                ItemAccentColor = new Color(0.6f, 0.65f, 0.85f),
                ItemSecondaryColor = new Color(0.16f, 0.17f, 0.24f),
                ItemPopColor = new Color(0.8f, 0.85f, 1f)
            };
        }

        internal static GameManager.MissionDefinition BuildBackyardRescueDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.BackyardRescue;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Backyard Rescue",
                IntroPrompt = "Cheddar + Cocoa must protect the weenies together.",
                ReadyScoreLabel = "READY TO PROTECT WEENIES",
                ItemRootName = "Breakfast/Weenies",
                ItemObjectName = "Breakfast/Weenie",
                ItemWorldLabel = "Weenie",
                ItemArrowLabel = "WEENIE",
                ItemCollectCueNoun = "breakfast",
                CollectObjectiveFormat = "Save weenies {0}/{1}",
                CollectedScoreLabel = "WEENIE SAVED",
                ItemScore = balance.ItemScore,
                SpawnedItemCount = balance.SpawnedItemCount,
                ItemGoal = balance.ItemGoal,
                RoundSeconds = balance.RoundSeconds,
                PawfectScore = balance.PawfectScore,
                HeroScore = balance.HeroScore,
                SurvivorScore = balance.SurvivorScore,
                UsesSquirrel = true,
                RequiresPredator = true,
                RequiresTug = true,
                MaxStolenFood = balance.MaxStolenFood,
                SquirrelPenalty = balance.SquirrelPenalty,
                SquirrelScareScore = balance.SquirrelScareScore,
                SquirrelObjectiveText = "Bark to scare squirrel",
                SquirrelStealingCue = "Squirrel is tiptoeing off with a weenie - bark now!",
                SquirrelStoleCue = "Squirrel got a weenie and is being rude about it!",
                SquirrelStealScoreLabel = "SQUIRREL GOT ONE",
                SquirrelScareScoreLabel = "SQUIRREL SCARED",
                SquirrelStealingActorLabel = "SQUIRREL STEALING - BARK!",
                SquirrelDroppedActorLabel = "SQUIRREL DROPPED IT!",
                SquirrelStoleActorLabel = "SQUIRREL GOT A WEENIE!",
                SquirrelMissPopLabel = "MISS! -WEENIE",
                SquirrelStealJuiceLabel = "MISS! SQUIRREL STOLE A WEENIE",
                SquirrelScareJuiceLabel = "SQUIRREL DROP POP!",
                TugObjectiveText = "Both dogs tug the rope",
                WaitingObjectiveText = "Clear the yard together",
                ClearObjectiveText = "Backyard saved - replay the weenie rescue",
                ClearBannerPrefix = "BACKYARD SAVED!",
                ClearScoreLabel = "LEVEL CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay the weenie rescue",
                FailObjectiveText = "Mission failed - replay the weenie rescue",
                GenericFailReason = "Needs more bark before the next weenie rescue.",
                TimeFailReason = "The clock won while the dogs had opinions.",
                StolenFailReason = "The squirrel union escaped with too many weenies.",
                PredatorFailReason = "The shadow caused a dramatic rescue backlog.",
                PawfectClearReason = "Tiny legends protected every snack with style.",
                HeroClearReason = "The yard survived with respectable barking.",
                BasicClearReason = "The weenies made it, even if dignity did not.",
                ItemColor = new Color(0.9f, 0.28f, 0.18f),
                ItemAccentColor = new Color(1f, 0.9f, 0.12f),
                ItemSecondaryColor = new Color(0.98f, 0.76f, 0.4f),
                ItemPopColor = new Color(1f, 0.9f, 0.25f)
            };
        }

        internal static GameManager.MissionDefinition BuildSnackHeistDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.SnackHeist;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "Snack Heist",
                IntroPrompt = "Cheddar + Cocoa must secure the forbidden snack stash before the squirrel union notices.",
                ReadyScoreLabel = "READY TO HEIST SNACKS",
                ItemRootName = "Forbidden Snacks",
                ItemObjectName = "Forbidden Snack",
                ItemWorldLabel = "Snack!",
                ItemArrowLabel = "SNACK",
                ItemCollectCueNoun = "a forbidden snack",
                CollectObjectiveFormat = "Stash snacks {0}/{1}",
                CollectedScoreLabel = "SNACK STASHED",
                ItemScore = balance.ItemScore,
                SpawnedItemCount = balance.SpawnedItemCount,
                ItemGoal = balance.ItemGoal,
                RoundSeconds = balance.RoundSeconds,
                PawfectScore = balance.PawfectScore,
                HeroScore = balance.HeroScore,
                SurvivorScore = balance.SurvivorScore,
                UsesSquirrel = true,
                RequiresPredator = false,
                RequiresTug = false,
                MaxStolenFood = balance.MaxStolenFood,
                SquirrelPenalty = balance.SquirrelPenalty,
                SquirrelScareScore = balance.SquirrelScareScore,
                SquirrelObjectiveText = "Bark-guard the snack thief",
                SquirrelStealingCue = "Squirrel is reaching for the forbidden snack stash - bark guard!",
                SquirrelStoleCue = "Squirrel got a snack and looks professionally smug!",
                SquirrelStealScoreLabel = "SNACK THIEF",
                SquirrelScareScoreLabel = "SNACK GUARD BARK",
                SquirrelStealingActorLabel = "SQUIRREL SNACK HEIST - BARK!",
                SquirrelDroppedActorLabel = "SQUIRREL DROPPED THE SNACK!",
                SquirrelStoleActorLabel = "SQUIRREL STOLE A SNACK!",
                SquirrelMissPopLabel = "MISS! -SNACK",
                SquirrelStealJuiceLabel = "MISS! SQUIRREL STOLE A SNACK",
                SquirrelScareJuiceLabel = "SNACK DROP POP!",
                TugObjectiveText = "Guard the snack stash",
                WaitingObjectiveText = "Guard the stash together",
                ClearObjectiveText = "Snack stash saved - replay Snack Heist",
                ClearBannerPrefix = "SNACK STASH SAVED!",
                ClearScoreLabel = "SNACK HEIST CLEAR",
                ReplayPrompt = "Press R / Enter / Start to replay Snack Heist",
                FailObjectiveText = "Mission failed - replay Snack Heist",
                GenericFailReason = "Needs more bark before the next snack crime.",
                TimeFailReason = "The snack window closed while everyone had opinions.",
                StolenFailReason = "The squirrel union escaped with too many forbidden snacks.",
                PredatorFailReason = "No predator here, just snack-related consequences.",
                PawfectClearReason = "Tiny legends protected the snack stash with suspicious expertise.",
                HeroClearReason = "The stash survived with respectable snack discipline.",
                BasicClearReason = "The snacks made it home, even if crumb law was broken.",
                ItemColor = new Color(0.95f, 0.58f, 0.18f),
                ItemAccentColor = new Color(1f, 0.88f, 0.18f),
                ItemSecondaryColor = new Color(0.38f, 0.18f, 0.08f),
                ItemPopColor = new Color(1f, 0.78f, 0.25f)
            };
        }

        internal static GameManager.MissionDefinition BuildSquirrelConspiracyDefinition(ArenaMissionTuning tuning)
        {
            const GameManager.MissionVariant variant = GameManager.MissionVariant.SquirrelConspiracy;
            var balance = tuning.BalanceFor(variant);
            return new GameManager.MissionDefinition
            {
                Variant = variant,
                Name = "The Great Backyard Squirrel Conspiracy",
                IntroPrompt = "Cheddar + Cocoa must herd the suspicious squirrel, reveal the hidden stash, and crack the backyard conspiracy.",
                ReadyScoreLabel = "READY TO INVESTIGATE SQUIRRELS",
                ItemRootName = "Conspiracy Clues",
                ItemObjectName = "Conspiracy Clue",
                ItemWorldLabel = "Clue!",
                ItemArrowLabel = "CLUE",
                ItemCollectCueNoun = "a clue",
                CollectObjectiveFormat = "Crack squirrel route {0}/{1}",
                CollectedScoreLabel = "CLUE FOUND",
                ItemScore = balance.ItemScore,
                SpawnedItemCount = balance.SpawnedItemCount,
                ItemGoal = balance.ItemGoal,
                RoundSeconds = balance.RoundSeconds,
                PawfectScore = balance.PawfectScore,
                HeroScore = balance.HeroScore,
                SurvivorScore = balance.SurvivorScore,
                UsesSquirrel = true,
                RequiresPredator = false,
                RequiresTug = false,
                MaxStolenFood = balance.MaxStolenFood,
                SquirrelPenalty = balance.SquirrelPenalty,
                SquirrelScareScore = balance.SquirrelScareScore,
                SquirrelObjectiveText = "Herd and cutoff the suspicious squirrel",
                SquirrelStealingCue = "The squirrel is running its conspiracy route - cut it off!",
                SquirrelStoleCue = "The squirrel taunted the yard and moved the stash gossip forward!",
                SquirrelStealScoreLabel = "SQUIRREL TAUNT",
                SquirrelScareScoreLabel = "GOOD HERD",
                SquirrelStealingActorLabel = "SQUIRREL ROUTE - HERD!",
                SquirrelDroppedActorLabel = "SQUIRREL ROUTE BLOCKED!",
                SquirrelStoleActorLabel = "SQUIRREL TAUNTED!",
                SquirrelMissPopLabel = "TAUNT!",
                SquirrelStealJuiceLabel = "MISS! SQUIRREL TAUNT",
                SquirrelScareJuiceLabel = "HERD POP!",
                TugObjectiveText = "Reveal the squirrel stash",
                WaitingObjectiveText = "Track the squirrel route together",
                ClearObjectiveText = "Conspiracy cracked - replay Squirrel Conspiracy",
                ClearBannerPrefix = "CONSPIRACY CRACKED!",
                ClearScoreLabel = "SQUIRREL CASE CLOSED",
                ReplayPrompt = "Press R / Enter / Start to replay Squirrel Conspiracy",
                FailObjectiveText = "Mission failed - replay Squirrel Conspiracy",
                GenericFailReason = "Needs more coordinated backyard detective barking.",
                TimeFailReason = "The squirrel moved the stash before the dogs solved the case.",
                StolenFailReason = "The squirrel taunted the yard into believing fake snack news.",
                PredatorFailReason = "No predator here, just squirrel propaganda.",
                PawfectClearReason = "Tiny detectives cracked the squirrel conspiracy with elite cutoffs.",
                HeroClearReason = "The stash was found before squirrel gossip took over.",
                BasicClearReason = "The conspiracy collapsed under respectable dog pressure.",
                ItemColor = new Color(0.7f, 0.42f, 0.12f),
                ItemAccentColor = new Color(1f, 0.88f, 0.22f),
                ItemSecondaryColor = new Color(0.24f, 0.12f, 0.04f),
                ItemPopColor = new Color(1f, 0.78f, 0.25f)
            };
        }
    }
}
