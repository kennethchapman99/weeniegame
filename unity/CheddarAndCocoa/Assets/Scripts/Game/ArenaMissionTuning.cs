namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Single code-side tuning source for the ArenaScene playable slice. Keep this plain data until
    /// there is enough designer workflow to justify ScriptableObject assets.
    /// </summary>
    public sealed class ArenaMissionTuning
    {
        public float IntroPromptSeconds = 5f;

        public int UnitedBarkScore = 100;
        public int PredatorDefendedScore = 300;
        public int RescueScore = 250;
        public int TugScore = 200;
        public int ClearScore = 500;
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

        // Sized for the large backyard (48x28): the shared camera frames both dogs close when they
        // regroup and pulls way back as they split across the yard, while clamping to the walls.
        public float CameraInitialOrthoSize = 9f;
        public float CameraMinOrthoSize = 6.8f;
        public float CameraMaxOrthoSize = 13f;
        public float CameraHorizontalMargin = 4.0f;
        public float CameraVerticalMargin = 3.0f;
        public float CameraFollowLerp = 9f;
        public float CameraZoomLerp = 7f;

        public float SquirrelRangeIndicatorRadius => SingleBarkSquirrelRange;
        public float RescueRangeIndicatorRadius => RescueBarkRange;
        public float TugRangeIndicatorRadius => TugTogetherDistance;

        // Spawn counts scaled up for the large 48x28 yard so the field reads as busy and worth
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
