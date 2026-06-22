using UnityEngine;

namespace CheddarAndCocoa.Game
{
    // Nested type declarations for GameManager, split out of the main controller file. These remain
    // nested in the same partial class, so every reference (GameManager.State, GameManager.MissionVariant,
    // GameManager.MissionDefinition, ...) resolves exactly as before.
    public sealed partial class GameManager : MonoBehaviour
    {
        public enum State { Intro, Playing, PredatorWarning, PredatorAttack, LevelClear, GameOver }
        public enum FlowState { MissionSelect, Playing, EndScreen, SessionSummary }
        public enum RoundModifier { SquirrelTrouble, ZoomiesSurge, PancakePanic }
        public enum MissionOutcome { InProgress, Clear, Failed }
        public enum MissionVariant { BackyardRescue, SnackHeist, SockPanic, SquirrelConspiracy, EagleShadowPanic, CoyotesFence, WeenieRoundup, ScentSearch, ThunderstormComfort, MarkTheYard, LeashWalk, CarRide, GateCrash, TableStealth, SquirrelSwitcheroo, WalkCampaign, BoneRelay, GreatEscape, ChaosMachine, BlanketCatch, KitchenFoodFrenzy }
        public enum FeedbackKind
        {
            Intro,
            SoloBark,
            UnitedBark,
            SquirrelStealing,
            SquirrelScared,
            SquirrelStoleFood,
            PredatorHuddle,
            PredatorAttack,
            PartnerRescue,
            TugNeedsPartner,
            TugTogether,
            LevelClear,
            GameOver
        }

        [System.Serializable]
        public sealed class MissionDefinition
        {
            public MissionVariant Variant;
            public string Name;
            public string IntroPrompt;
            public string ReadyScoreLabel;
            public string ItemRootName;
            public string ItemObjectName;
            public string ItemWorldLabel;
            public string ItemArrowLabel;
            public string ItemCollectCueNoun;
            public string CollectObjectiveFormat;
            public string CollectedScoreLabel;
            public int ItemScore;
            public int SpawnedItemCount;
            public int ItemGoal;
            public float RoundSeconds;
            public int PawfectScore;
            public int HeroScore;
            public int SurvivorScore;
            public bool UsesSquirrel;
            public bool RequiresPredator;
            public bool RequiresTug;
            public int MaxStolenFood;
            public int SquirrelPenalty;
            public int SquirrelScareScore;
            public string SquirrelObjectiveText;
            public string SquirrelStealingCue;
            public string SquirrelStoleCue;
            public string SquirrelStealScoreLabel;
            public string SquirrelScareScoreLabel;
            public string SquirrelStealingActorLabel;
            public string SquirrelDroppedActorLabel;
            public string SquirrelStoleActorLabel;
            public string SquirrelMissPopLabel;
            public string SquirrelStealJuiceLabel;
            public string SquirrelScareJuiceLabel;
            public string TugObjectiveText;
            public string WaitingObjectiveText;
            public string ClearObjectiveText;
            public string ClearBannerPrefix;
            public string ClearScoreLabel;
            public string ReplayPrompt;
            public string FailObjectiveText;
            public string GenericFailReason;
            public string TimeFailReason;
            public string StolenFailReason;
            public string PredatorFailReason;
            public string PawfectClearReason;
            public string HeroClearReason;
            public string BasicClearReason;
            public Color ItemColor;
            public Color ItemAccentColor;
            public Color ItemSecondaryColor;
            public Color ItemPopColor;
        }

        public enum JuiceFeedbackKind
        {
            None,
            BarkBurst,
            SuccessPop,
            WarningMiss,
            ScoreDelta
        }
    }
}
