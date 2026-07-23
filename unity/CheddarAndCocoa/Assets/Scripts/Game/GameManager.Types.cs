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
        public enum MissionVariant { BackyardRescue, SnackHeist, SockPanic, SquirrelConspiracy, EagleShadowPanic, CoyotesFence, WeenieRoundup, ScentSearch, ThunderstormComfort, MarkTheYard, LeashWalk, CarRide, GateCrash, TableStealth, SquirrelSwitcheroo, WalkCampaign, BoneRelay, GreatEscape, ChaosMachine, BlanketCatch, KitchenFoodFrenzy, OperationPeeBreak, BabyBirdBedlam, SkunkBlastMayhem, TickInvasion }
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
            WrestleFlip,
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
            public string RoleHint;
            public string MechanicTag;
            public string SceneCue;
            public string ReusablePresentation;
            public ReadabilityRequirement RequiredReadability;

            // Guidance escalation ladder overrides (see MissionGuidanceEscalation). Default: every
            // mission gets the full 0-3 tier ladder at the default 12s/25s/45s timings; a mission may
            // override the cap and timings for a timing-critical window without any code branching.
            public int GuidanceTierCap = MissionGuidanceEscalation.MaxTier;
            public float GuidanceTier1Seconds = MissionGuidanceEscalation.DefaultTier1Seconds;
            public float GuidanceTier2Seconds = MissionGuidanceEscalation.DefaultTier2Seconds;
            public float GuidanceTier3Seconds = MissionGuidanceEscalation.DefaultTier3Seconds;
            /// <summary>
            /// Role-turn beacon (G1.3): show it at Tier 0 too, not just Tier 1+. For hard-handoff
            /// puzzles where "whose turn" is core to solving the step, not just a stall rescue.
            /// </summary>
            public bool GuidanceBeaconAlwaysOn = false;

            public string PresentationLine =>
                string.IsNullOrEmpty(MechanicTag) && string.IsNullOrEmpty(SceneCue)
                    ? string.Empty
                    : $"{MechanicTag} | {SceneCue}";
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
