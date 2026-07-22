namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Physical scale contract for authored outdoor levels. A runtime dog is roughly two world
    /// units long, so this yard keeps the dogs below two percent of its width instead of making
    /// them read like giant props in a single-screen arena.
    /// </summary>
    public static class ArenaWorldScale
    {
        public const float BackyardWidth = 120f;
        public const float BackyardHeight = 68f;
        public const float ApproximateDogLength = 2f;
        public const float MaximumDogToYardWidthRatio = 0.02f;
    }

    /// <summary>
    /// Single code-side tuning source for the ArenaScene playable slice. Keep this plain data until
    /// there is enough designer workflow to justify ScriptableObject assets.
    /// </summary>
    public sealed class ArenaMissionTuning
    {
        /// <summary>
        /// CF1.1: no longer the briefing card's own display timer - the card now waits for a
        /// deliberate bark/interact ("accept") instead of expiring on a clock. Still folds into the
        /// pre-accept LeadInRemaining/LeadInActive reporting value and how long the (currently
        /// unrendered) MissionBanner intro-prompt text lingers after GO.
        /// </summary>
        public float IntroPromptSeconds = 5f;

        /// <summary>
        /// Open-yard discovery beat that runs after the briefing card is explicitly accepted
        /// (CF1.1) and before the round clock and threats start. Any bark/interact/grab during THIS
        /// beat still skips straight to GO, same as before.
        /// </summary>
        public float LeadInSniffSeconds = 2.5f;

        public int UnitedBarkScore = 100;
        public int PredatorDefendedScore = 300;
        public int RescueScore = 250;
        public int TugScore = 200;
        public int ClearScore = 500;
        public int FlawlessBonus = 200;
        public int TimeBonusMultiplier = 5;
        public int PredatorFailurePenalty = 150;
        public int PancakeSquirrelPenalty = 80;
        public int GameOverPenalty = 100;

        public float UnitedBarkWindow = 0.8f;
        public float UnitedBarkRange = 3f;
        public float UnitedBarkCooldown = 1.2f;
        public float SingleBarkSquirrelRange = 4f;
        public float SingleBarkScareSeconds = 1.5f;
        public float UnitedBarkScareSeconds = 3.5f;
        public float RescueBarkRange = 2f;

        public float FirstSquirrelBaseDelay = 9f;
        public float FirstSquirrelTroubleDelay = 7f;
        public float SquirrelBaseDelay = 3.4f;
        public float SquirrelTroubleDelay = 2.2f;
        public float SquirrelMoveSpeed = 1.9f;

        public float PredatorWarningAt = 25f;
        public float PredatorWarningSeconds = 5f;

        public float TugTogetherDistance = 1.6f;
        public float TugInteractDistance = 1.8f;
        public float TugChargePerSecond = 0.5f;
        public float TugInteractProgress = 0.2f;

        // The 120x68 yard has two useful camera modes: a close scrolling exploration frame when
        // the dogs regroup, and a strategic full-yard frame when couch co-op players split up.
        public float CameraInitialOrthoSize = 8f;
        public float CameraMinOrthoSize = 7.5f;
        public float CameraMaxOrthoSize = 34f;
        public float CameraHorizontalMargin = 5.0f;
        public float CameraVerticalMargin = 4.0f;
        public float CameraFollowLerp = 9f;
        public float CameraZoomLerp = 7f;

        // Normal handling remains unchanged around objectives; distant targets get a modest
        // top-speed lift so crossing the 120-unit yard does not become dead travel time.
        public float TravelAssistEngageDistance = 28f;
        public float TravelAssistReleaseDistance = 20f;
        public float TravelAssistSpeedMultiplier = 1.55f;

        public float SquirrelRangeIndicatorRadius => SingleBarkSquirrelRange;
        public float RescueRangeIndicatorRadius => RescueBarkRange;
        public float TugRangeIndicatorRadius => TugTogetherDistance;

        // Spawn counts scaled for the outdoor yard so the field reads as busy and worth
        // traversing rather than a few specks in a big empty box.
        public MissionBalance BackyardRescue = new MissionBalance
        {
            RoundSeconds = 90f,
            SpawnedItemCount = 10,
            ItemGoal = 6,
            ItemScore = 50,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1500,
            HeroScore = 1050,
            SurvivorScore = 350
        };

        public MissionBalance SnackHeist = new MissionBalance
        {
            RoundSeconds = 80f,
            SpawnedItemCount = 7,
            ItemGoal = 4,
            ItemScore = 60,
            MaxStolenFood = 2,
            SquirrelPenalty = 90,
            SquirrelScareScore = 35,
            PawfectScore = 950,
            HeroScore = 700,
            SurvivorScore = 250
        };

        public MissionBalance SockPanic = new MissionBalance
        {
            RoundSeconds = 70f,
            SpawnedItemCount = 9,
            ItemGoal = 5,
            ItemScore = 40,
            MaxStolenFood = 0,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 800,
            HeroScore = 600,
            SurvivorScore = 200
        };

        public MissionBalance SquirrelConspiracy = new MissionBalance
        {
            RoundSeconds = 75f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 75,
            SquirrelScareScore = 75,
            PawfectScore = 1500,
            HeroScore = 1050,
            SurvivorScore = 350
        };

        public MissionBalance EagleShadowPanic = new MissionBalance
        {
            RoundSeconds = 70f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 75,
            SquirrelScareScore = 75,
            PawfectScore = 1500,
            HeroScore = 1050,
            SurvivorScore = 350
        };

        public MissionBalance CoyotesFence = new MissionBalance
        {
            RoundSeconds = 80f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 75,
            SquirrelScareScore = 75,
            PawfectScore = 1600,
            HeroScore = 1100,
            SurvivorScore = 350
        };

        public MissionBalance WeenieRoundup = new MissionBalance
        {
            RoundSeconds = 85f,
            SpawnedItemCount = 0,
            ItemGoal = 5,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1500,
            HeroScore = 1050,
            SurvivorScore = 350
        };

        public MissionBalance ScentSearch = new MissionBalance
        {
            RoundSeconds = 80f,
            SpawnedItemCount = 0,
            ItemGoal = 3,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1400,
            HeroScore = 950,
            SurvivorScore = 300
        };

        public MissionBalance ThunderstormComfort = new MissionBalance
        {
            RoundSeconds = 75f,
            SpawnedItemCount = 0,
            ItemGoal = 4,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1300,
            HeroScore = 900,
            SurvivorScore = 300
        };

        public MissionBalance MarkTheYard = new MissionBalance
        {
            RoundSeconds = 80f,
            SpawnedItemCount = 0,
            ItemGoal = 5,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1400,
            HeroScore = 950,
            SurvivorScore = 300
        };

        public MissionBalance LeashWalk = new MissionBalance
        {
            RoundSeconds = 80f,
            SpawnedItemCount = 0,
            ItemGoal = 4,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1300,
            HeroScore = 900,
            SurvivorScore = 300
        };

        // Ride pacing: 7 road events at ~5-7.5s each is ~45s of ride; 90s leaves honest time
        // bonus on a clean run. Pawfect assumes near-clean events plus most brake braces banked.
        public MissionBalance CarRide = new MissionBalance
        {
            RoundSeconds = 90f,
            SpawnedItemCount = 0,
            ItemGoal = 7,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1600,
            HeroScore = 1100,
            SurvivorScore = 300
        };

        // No positive mission-specific scoring (only the -75 snap penalty), so the whole ceiling is
        // the shared clear package: Clear 500 + Flawless 200 + 5/s time bonus. Pawfect 900 = a
        // flawless clear with ~40s of the 70s round left; the old 1200 exceeded even an instant
        // flawless clear (1050) and was unreachable.
        public MissionBalance GateCrash = new MissionBalance
        {
            RoundSeconds = 70f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 900,
            HeroScore = 650,
            SurvivorScore = 300
        };

        // Same shape as GateCrash: no positive mission-specific scoring (only the -75 spotted
        // penalty), so the clear package is the whole ceiling - the old 1200 was unreachable.
        public MissionBalance TableStealth = new MissionBalance
        {
            RoundSeconds = 70f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 900,
            HeroScore = 650,
            SurvivorScore = 300
        };

        // 3 stash raids at StashFound (300) each = 900 core, same per-hit weight as
        // SquirrelConspiracy's stash payoff - mirrors CoyotesFence's tier.
        public MissionBalance SquirrelSwitcheroo = new MissionBalance
        {
            RoundSeconds = 75f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1600,
            HeroScore = 1100,
            SurvivorScore = 350
        };

        // One HumanGettingIt (120) + one WalkConned (500) core payoff - mirrors ScentSearch/MarkTheYard's tier.
        public MissionBalance WalkCampaign = new MissionBalance
        {
            RoundSeconds = 70f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1400,
            HeroScore = 950,
            SurvivorScore = 300
        };

        // 3 BoneFound (175) hits = 525 core - same sniff-and-find family as ScentSearch, same tier.
        public MissionBalance BoneRelay = new MissionBalance
        {
            RoundSeconds = 80f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1400,
            HeroScore = 950,
            SurvivorScore = 300
        };

        // 4 ContraptionStep (90) hand-offs = 360 core + the 700 clear package. Pawfect 1200 = a
        // flawless chain finished with ~28s of the 75s round left; 1300 would have required
        // finishing all four stations in under ~27s.
        public MissionBalance GreatEscape = new MissionBalance
        {
            RoundSeconds = 75f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1200,
            HeroScore = 900,
            SurvivorScore = 300
        };

        // 3 tight-window ContraptionStep (90) junctions = 270 core + the 700 clear package.
        // Pawfect 1100 = a flawless cascade finished with ~26s of the 70s round left; 1200 would
        // have required all three timing windows inside ~24s.
        public MissionBalance ChaosMachine = new MissionBalance
        {
            RoundSeconds = 70f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1100,
            HeroScore = 800,
            SurvivorScore = 300
        };

        // 5 catches at WeenieDelivered (150) each = 750 core - mirrors CoyotesFence/Backyard's tier.
        public MissionBalance BlanketCatch = new MissionBalance
        {
            RoundSeconds = 75f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1600,
            HeroScore = 1100,
            SurvivorScore = 350
        };

        // 4 chick cycles at ChickNabbed (25) + ChickGulped (150) plus the NestFeastComplete 500
        // = 1200 guaranteed core; parent repels (125, dive-dependent) stack on top. Pawfect 1800
        // sits under the 2170 flawless-clear ceiling (1200 + 700 clear package + 270 time bonus)
        // but demands clean, quick play; Hero 1300 needs the full feast banked.
        public MissionBalance BabyBirdBedlam = new MissionBalance
        {
            RoundSeconds = 90f,
            SpawnedItemCount = 0,
            ItemGoal = 4,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1800,
            HeroScore = 1300,
            SurvivorScore = 350
        };

        // Combo-scored dinner rush (5 catches, escalating +25/combo) already lands in this range;
        // kept close to its former Backyard-inherited numbers, now explicit instead of accidental.
        public MissionBalance KitchenFoodFrenzy = new MissionBalance
        {
            RoundSeconds = 90f,
            SpawnedItemCount = 0,
            ItemGoal = 0,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 50,
            SquirrelScareScore = 25,
            PawfectScore = 1600,
            HeroScore = 1100,
            SurvivorScore = 350
        };

        // Single-grab heist (ItemGoal=1): the puzzle's own score events (lure/bail/skunk/de-skunk/
        // haul) carry most of the run, with BirdSecured (500) as the guaranteed clear anchor - the
        // ceiling on a flawless zero-spray run is 500 (BirdSecured) + 500 (ClearScore) + 200
        // (FlawlessBonus) + 300 (60% of 100s round * 5 time-bonus mult) = 1500. Pawfect sits under
        // that with margin; Hero still clears after at least one detour to the laundry pile.
        public MissionBalance SkunkBlastMayhem = new MissionBalance
        {
            RoundSeconds = 100f,
            SpawnedItemCount = 0,
            ItemGoal = 1,
            ItemScore = 0,
            MaxStolenFood = 3,
            SquirrelPenalty = 60,
            SquirrelScareScore = 60,
            PawfectScore = 1400,
            HeroScore = 950,
            SurvivorScore = 300
        };

        public static ArenaMissionTuning CreateDefault() => new ArenaMissionTuning();

        public MissionBalance BalanceFor(GameManager.MissionVariant variant)
        {
            return variant switch
            {
                GameManager.MissionVariant.SnackHeist => SnackHeist,
                GameManager.MissionVariant.SockPanic => SockPanic,
                GameManager.MissionVariant.SquirrelConspiracy => SquirrelConspiracy,
                GameManager.MissionVariant.EagleShadowPanic => EagleShadowPanic,
                GameManager.MissionVariant.CoyotesFence => CoyotesFence,
                GameManager.MissionVariant.WeenieRoundup => WeenieRoundup,
                GameManager.MissionVariant.ScentSearch => ScentSearch,
                GameManager.MissionVariant.ThunderstormComfort => ThunderstormComfort,
                GameManager.MissionVariant.MarkTheYard => MarkTheYard,
                GameManager.MissionVariant.LeashWalk => LeashWalk,
                GameManager.MissionVariant.CarRide => CarRide,
                GameManager.MissionVariant.GateCrash => GateCrash,
                GameManager.MissionVariant.TableStealth => TableStealth,
                GameManager.MissionVariant.SquirrelSwitcheroo => SquirrelSwitcheroo,
                GameManager.MissionVariant.WalkCampaign => WalkCampaign,
                GameManager.MissionVariant.BoneRelay => BoneRelay,
                GameManager.MissionVariant.GreatEscape => GreatEscape,
                GameManager.MissionVariant.ChaosMachine => ChaosMachine,
                GameManager.MissionVariant.BlanketCatch => BlanketCatch,
                GameManager.MissionVariant.KitchenFoodFrenzy => KitchenFoodFrenzy,
                GameManager.MissionVariant.BabyBirdBedlam => BabyBirdBedlam,
                GameManager.MissionVariant.SkunkBlastMayhem => SkunkBlastMayhem,
                _ => BackyardRescue
            };
        }
    }

    public sealed class MissionBalance
    {
        public float RoundSeconds;
        public int SpawnedItemCount;
        public int ItemGoal;
        public int ItemScore;
        public int MaxStolenFood;
        public int SquirrelPenalty;
        public int SquirrelScareScore;
        public int PawfectScore;
        public int HeroScore;
        public int SurvivorScore;
    }
}
