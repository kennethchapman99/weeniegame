using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Input;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Mission controller for the ArenaScene Backyard Mission vertical slice. It intentionally keeps
    /// the prototype self-contained: generated placeholder actors, deterministic pacing hooks for
    /// tests, and simple co-op rules that make bark/tug/rescue gameplay-relevant.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public enum State { Intro, Playing, PredatorWarning, PredatorAttack, LevelClear, GameOver }
        public enum FlowState { MissionSelect, Playing, EndScreen, SessionSummary }
        public enum RoundModifier { SquirrelTrouble, ZoomiesSurge, PancakePanic }
        public enum MissionOutcome { InProgress, Clear, Failed }
        public enum MissionVariant { BackyardRescue, SnackHeist, SockPanic, SquirrelConspiracy, EagleShadowPanic, CoyotesFence, WeenieRoundup, ScentSearch, ThunderstormComfort, MarkTheYard, LeashWalk, CarRide, GateCrash, TableStealth, SquirrelSwitcheroo, WalkCampaign, BoneRelay, GreatEscape, ChaosMachine, BlanketCatch, KitchenFoodFrenzy, OperationPeeBreak }
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

        private static readonly MissionVariant[] MissionOrder =
        {
            MissionVariant.BackyardRescue,
            MissionVariant.SnackHeist,
            MissionVariant.SockPanic,
            MissionVariant.SquirrelConspiracy,
            MissionVariant.EagleShadowPanic,
            MissionVariant.CoyotesFence,
            MissionVariant.WeenieRoundup,
            MissionVariant.ScentSearch,
            MissionVariant.ThunderstormComfort,
            MissionVariant.MarkTheYard,
            MissionVariant.LeashWalk,
            MissionVariant.CarRide,
            MissionVariant.GateCrash,
            MissionVariant.TableStealth,
            MissionVariant.SquirrelSwitcheroo,
            MissionVariant.WalkCampaign,
            MissionVariant.BoneRelay,
            MissionVariant.GreatEscape,
            MissionVariant.ChaosMachine,
            MissionVariant.BlanketCatch,
            MissionVariant.KitchenFoodFrenzy,
            MissionVariant.OperationPeeBreak
        };

        [Header("Mission selection")]
        [SerializeField] private MissionVariant startingMission = MissionVariant.BackyardRescue;

        private readonly ArenaMissionTuning _tuning = ArenaMissionTuning.CreateDefault();
        private readonly PlaytestEventLog _playtestLog = new PlaytestEventLog();
        private float roundDuration = 90f;
        private int treatCount = 5;
        private int recoveryGoal = 6;
        private int maxStolenFood = 3;

        public int Score { get; private set; }
        public int LastScoreDelta { get; private set; }
        public float TimeRemaining { get; private set; }
        public float RoundDuration => roundDuration;
        public FlowState CurrentFlow { get; private set; } = FlowState.MissionSelect;
        public bool MissionSelectVisible => CurrentFlow == FlowState.MissionSelect;
        public bool EndScreenVisible => CurrentFlow == FlowState.EndScreen;
        public bool SessionSummaryVisible => CurrentFlow == FlowState.SessionSummary;
        public int MissionSelectOptionCount => MissionOrder.Length;
        public HerdingMissionState SquirrelConspiracyState => _herdingState;
        public Vector2[] SquirrelRouteNodes => (Vector2[])_squirrelRoute.Clone();
        public Vector2[] SquirrelCutoffZones => (Vector2[])_squirrelCutoffZones.Clone();
        public Vector2 ActiveSquirrelCutoffZone => _squirrelCutoffZones[Mathf.Clamp(_herdingState.RouteIndex, 0, _squirrelCutoffZones.Length - 1)];
        public ThreatSweepMissionState EagleShadowPanicState => _threatSweepState;
        public CoopRescueTimingPuzzle EagleRescuePuzzle => _eagleRescue;
        public Vector2 EagleSnatchPosition => _eagleSnatchPosition;
        public PatrolDefenseMissionState CoyotesFenceState => _patrolState;
        public Vector2[] EagleCoverZones => (Vector2[])_eagleCoverZones.Clone();
        public Vector2[] FenceGaps => (Vector2[])_fenceGaps.Clone();
        public CarryRoundupMissionState WeenieRoundupState => _carryState;
        public Vector2 BowlPosition => _bowlPosition;
        public ScentSearchMissionState ScentSearchState => _scentState;
        public Vector2[] DigSpots => (Vector2[])_digSpots.Clone();
        public ThunderstormMissionState ThunderstormState => _stormState;
        public PanicMeter Panic => _panic;
        public MarkTheYardMissionController MarkTheYardController => _activeMissionController as MarkTheYardMissionController;
        public TerritoryMissionState MarkTheYardState => MarkTheYardController?.State ?? _emptyTerritoryState;
        public Vector2[] TerritoryZones => MarkTheYardMissionController.ComputeZones(_bounds);
        public LeashWalkMissionState LeashWalkState => _leashState;
        public Vector2[] LeashCheckpoints => (Vector2[])_leashCheckpoints.Clone();
        public CarBalanceMissionState CarRideState => _carState;
        public float CarBalance => _carBalance;
        public SockBasketMissionState SockPanicState => _sockBasketState;
        public BackyardSquirrelTrapState BackyardTrapState => _backyardTrapState;
        public Vector2 BackyardTrapGapPosition => _backyardTrapGapPosition;
        public Treat BackyardDroppedWeenie => _backyardDroppedWeenie;
        public KitchenFoodFrenzyMissionState KitchenState => KitchenController?.State;
        public Vector2 KitchenCounterPosition => KitchenController?.CounterPosition ?? Vector2.zero;
        public Vector2 KitchenSafeZonePosition => KitchenController?.SafeZonePosition ?? Vector2.zero;
        public GameObject KitchenFoodObject => KitchenController?.FoodObject;
        public GameObject KitchenTelegraphObject => KitchenController?.TelegraphObject;
        public GameObject KitchenLandingWarningObject => KitchenController?.LandingWarningObject;
        public PeeBreakMissionController PeeBreakController => _activeMissionController as PeeBreakMissionController;
        public IMissionController ActiveMissionController => _activeMissionController;
        public GameObject LaundryBasketObject => _laundryBasketObject;
        public Treat ExposedSock => _exposedSock;
        public GateCrashMissionController GateCrashController => _activeMissionController as GateCrashMissionController;
        public CoopHoldReleasePuzzle GateCrashPuzzle => GateCrashController?.Puzzle ?? _emptyGatePuzzle;
        public Vector2 GateHoldZone => GateCrashController?.HoldZone ?? _bounds.center;
        public Vector2 GateCrossZone => GateCrashController?.CrossZone ?? _bounds.center;
        public TableStealthMissionController TableStealthController => _activeMissionController as TableStealthMissionController;
        public CoopHumanDistractionPuzzle TableStealthPuzzle => TableStealthController?.Puzzle ?? _emptyTablePuzzle;
        public Vector2 TableHumanZone => TableStealthController?.HumanZone ?? _bounds.center;
        public Vector2 TableStealZone => TableStealthController?.StealZone ?? _bounds.center;
        public SquirrelSwitcherooMissionController SquirrelSwitcherooController => _activeMissionController as SquirrelSwitcherooMissionController;
        public CoopBaitSwitchPuzzle SwitcherooPuzzle => SquirrelSwitcherooController?.Puzzle ?? _emptySwitcherooPuzzle;
        public Vector2 SwitcherooDecoyZone => SquirrelSwitcherooController?.DecoyZone ?? _bounds.center;
        public Vector2 SwitcherooStashZone => SquirrelSwitcherooController?.StashZone ?? _bounds.center;
        public WalkCampaignMissionController WalkCampaignController => _activeMissionController as WalkCampaignMissionController;
        public CoopSocialManipulationPuzzle WalkCampaignPuzzle => WalkCampaignController?.Puzzle ?? _emptyWalkPuzzle;
        public Vector2 WalkDoorZone => WalkCampaignController?.DoorZone ?? _bounds.center;
        public Vector2 WalkLeashZone => WalkCampaignController?.LeashZone ?? _bounds.center;
        public CoopScentRelayPuzzle BoneRelayPuzzle => _boneRelay;
        public int BoneMoundCount => _boneMoundSpots.Length;
        public Vector2 BoneScentZone => _boneScentZone;
        public Vector2 BoneMoundSpot(int index) => index >= 0 && index < _boneMoundSpots.Length ? _boneMoundSpots[index] : Vector2.zero;
        public GreatEscapeMissionController GreatEscapeController => _activeMissionController as GreatEscapeMissionController;
        public CoopSequenceChainPuzzle GreatEscapePuzzle => GreatEscapeController?.Puzzle ?? _emptyEscapePuzzle;
        public int EscapeStationCount => GreatEscapeController?.StationCount ?? 0;
        public Vector2 EscapeStationSpot(int index) => GreatEscapeController?.StationSpot(index) ?? Vector2.zero;
        public ChainActor EscapeStationOwner(int index) => GreatEscapeController?.StationOwner(index) ?? ChainActor.Either;
        public CoopChaosMachinePuzzle ChaosMachinePuzzle => _chaos;
        public int ChaosJunctionCount => _chaosJunctionSpots.Length;
        public Vector2 ChaosLeverZone => _chaosLeverZone;
        public Vector2 ChaosJunctionSpot(int index) => index >= 0 && index < _chaosJunctionSpots.Length ? _chaosJunctionSpots[index] : Vector2.zero;
        public ChainActor ChaosJunctionOwner(int index) => index >= 0 && index < _chaosOwners.Length ? _chaosOwners[index] : ChainActor.Either;
        public CoopStretchSpanPuzzle BlanketPuzzle => _blanket;
        public Vector2 BlanketItemPosition => new(_blanketItemX, _blanketItemY);
        public float BlanketCatchY => BlanketCatchLineY;
        public MissionRuntimeSnapshot RuntimeSnapshot => BuildRuntimeSnapshot();
        public int CurrentMissionSeed => _missionSeed;
        public DemoReadinessResult DemoReadiness => DemoReadinessGate.Evaluate(DemoReadinessGate.RequiredForBackyardDemo);
        public string DemoReadinessLabel => DemoReadiness.Ready
            ? "Demo gate: READY (select/clear/fail/replay/controller/readability)"
            : $"Demo gate: BLOCKED - missing {DemoReadiness.Missing}";
        public int SelectedMissionIndex => _selectedMissionIndex;
        public MissionVariant SelectedMissionVariant => MissionOrder[Mathf.Clamp(_selectedMissionIndex, 0, MissionOrder.Length - 1)];
        public MissionVariant MissionVariantAt(int index) => MissionOrder[Mathf.Clamp(index, 0, MissionOrder.Length - 1)];
        public string SelectedMissionName => BuildMissionDefinition(SelectedMissionVariant, _tuning).Name;
        public string SelectedMissionBriefing => BuildMissionDefinition(SelectedMissionVariant, _tuning).IntroPrompt;
        public State Phase { get; private set; } = State.Intro;
        public bool IsGameOver => Phase == State.GameOver;
        public bool IsLevelClear => Phase == State.LevelClear;
        public int UnitedBarks { get; private set; }
        public int BreakfastRecovered { get; private set; }
        public int BreakfastGoal => _mission != null ? _mission.ItemGoal : recoveryGoal;
        public int StolenFood { get; private set; }
        public int MaxStolenFood => _mission != null ? _mission.MaxStolenFood : maxStolenFood;
        public bool PredatorResolved { get; private set; }
        public bool PredatorFailed { get; private set; }
        public bool AnyDogGrabbed => _grabbedDog >= 0;
        public float TugProgress { get; private set; }
        public bool TugComplete { get; private set; }
        public int StarRating { get; private set; }
        public MissionOutcome Outcome { get; private set; } = MissionOutcome.InProgress;
        public RoundModifier ActiveModifier { get; private set; }
        public string ActiveModifierLabel => ActiveModifier switch
        {
            RoundModifier.SquirrelTrouble => "Squirrel Trouble",
            RoundModifier.ZoomiesSurge => "Zoomies Surge",
            _ => "Pancake Panic"
        };
        public string LastCue { get; private set; } = "Ready";
        public string LastScoreEventLabel { get; private set; } = "Score ready";
        public string LastScorePopLabel { get; private set; } = string.Empty;
        public bool ScorePopVisible => Time.time < _scorePopUntil;
        public string ObjectiveLabel => BuildObjectiveLabel();
        public string TeamGuidanceLabel
        {
            get
            {
                if (!MissionActive() || ObjectiveArrows == null || _dogs == null) return string.Empty;
                var guidance = new List<string>(ObjectiveArrows.Length);
                for (int i = 0; i < ObjectiveArrows.Length && i < _dogs.Length; i++)
                {
                    string route = ObjectiveArrows[i] != null ? ObjectiveArrows[i].GuidanceLabel : string.Empty;
                    if (string.IsNullOrEmpty(route)) continue;
                    string travel = _dogs[i] != null && _dogs[i].TravelAssist ? " [TRAIL SPRINT]" : string.Empty;
                    guidance.Add($"{DogName(_dogs[i])}: {route}{travel}");
                }
                return guidance.Count == 0 ? string.Empty : string.Join("  •  ", guidance);
            }
        }
        public MissionVariant ActiveMissionVariant => _mission != null ? _mission.Variant : startingMission;
        public string ActiveMissionName => _mission != null ? _mission.Name : "Backyard Rescue";
        public string MissionItemPlural => _mission != null ? _mission.ItemRootName : "Breakfast/Weenies";
        public string MissionIntroPrompt => _mission != null ? _mission.IntroPrompt : "Cheddar + Cocoa must protect the weenies together.";
        public bool MissionBriefingVisible => MissionActive() && Time.time < _introPromptUntil;
        public string MissionBanner { get; private set; } = string.Empty;
        public string EndRank { get; private set; } = "Needs More Bark";
        public string EndSummaryLabel { get; private set; } = string.Empty;
        public string EndReasonLabel { get; private set; } = string.Empty;
        public bool ReplayPromptVisible => IsGameOver || IsLevelClear;
        public string ReplayPromptLabel => ReplayPromptVisible ? (_mission != null ? _mission.ReplayPrompt : "Press R / Enter / Start to replay the weenie rescue") : string.Empty;
        public bool EndReplayAvailable => EndScreenVisible;
        public bool EndNextMissionAvailable => EndScreenVisible;
        public bool EndMissionSelectAvailable => EndScreenVisible;
        public string EndReplayActionLabel => EndReplayAvailable ? "Replay" : string.Empty;
        public string EndNextActionLabel => EndNextMissionAvailable ? (SessionSummaryReady ? "Session Summary" : "Next Mission") : string.Empty;
        public string EndMissionSelectActionLabel => EndMissionSelectAvailable ? "Mission Select" : string.Empty;
        public int SessionMissionsPlayed { get; private set; }
        public int SessionTotalScore { get; private set; }
        public int SessionStarsEarned { get; private set; }
        public int SessionFlawlessClears { get; private set; }
        public int SessionUniqueMissionsCompleted { get; private set; }
        public bool SessionAllMissionsCompleted => SessionUniqueMissionsCompleted >= MissionOrder.Length;
        public bool SessionSummaryReady => SessionUniqueMissionsCompleted >= 3 &&
            SessionUniqueMissionsCompleted / 3 > _lastSummaryMilestoneShown;
        public string SessionContinueActionLabel => SessionAllMissionsCompleted ? "Victory Lap" : "Continue Session";
        public string SessionSummaryLabel { get; private set; } = "Session Summary: no missions played yet.";
        public string SessionRanksEarnedLabel { get; private set; } = "Ranks: none yet.";
        public ArenaMissionTuning Tuning => _tuning;
        public PlaytestEventLog PlaytestLog => _playtestLog;
        public IReadOnlyList<string> PlaytestEvents => _playtestLog.Entries;
        public string LastPlaytestEvent => _playtestLog.LastEvent;
        public bool PlaytestOverlayVisible { get; private set; }
        public bool PlaytestModeEnabled => PlaytestOverlayVisible;
        public int BarksUsed { get; private set; }
        public int FailedInteractions { get; private set; }
        public int ObjectiveChangeCount { get; private set; }
        public int MissionReplayCount { get; private set; }
        public float MissionDurationSeconds => CurrentFlow == FlowState.MissionSelect ? 0f : Mathf.Clamp(roundDuration - TimeRemaining, 0f, roundDuration);
        public string FailPressureLabel => BuildFailPressureLabel();
        public string DogPositionsLabel => BuildDogPositionsLabel();
        public string PlaytestCountersLabel => $"Barks {BarksUsed} / missed interacts {FailedInteractions} / objective shifts {ObjectiveChangeCount} / duration {MissionDurationSeconds:0.0}s / replays {MissionReplayCount}";
        public string MissionFailureSummaryLabel => BuildMissionFailureSummaryLabel();
        public bool AudioEnabled { get; private set; } = true;
        public bool RumbleEnabled { get; private set; } = true;
        public IReadOnlyList<string> AudioCueRequests => _audioCueRequests;
        public IReadOnlyList<string> RumbleRequests => _rumbleRequests;
        public string LastAudioCueRequested { get; private set; } = string.Empty;
        public string LastRumbleRequested { get; private set; } = string.Empty;
        public int AudioCueRequestCount => _audioCueRequests.Count;
        public int RumbleRequestCount => _rumbleRequests.Count;
        public bool MusicLoopReady => _music != null && _music.clip != null && _music.loop;
        public bool MusicMuted => _music == null || _music.mute;
        public bool IsPaused { get; private set; }
        public bool QuitRequested { get; private set; }
        public FeedbackKind LastFeedback { get; private set; } = FeedbackKind.Intro;
        public JuiceFeedbackKind LastJuiceFeedback { get; private set; } = JuiceFeedbackKind.None;
        public string LastJuiceLabel { get; private set; } = string.Empty;
        public int JuiceFeedbackSequence { get; private set; }
        public event Action<JuiceFeedbackKind, string> OnJuiceFeedback;
        public GameObject SquirrelObject { get; private set; }
        public GameObject PredatorObject { get; private set; }
        public GameObject RopeObject { get; private set; }
        public DogReadabilityFeedback[] DogFeedback { get; private set; }
        public ObjectiveArrowFeedback[] ObjectiveArrows { get; private set; }
        public InteractionRangeIndicator[] InteractionRangeIndicators { get; private set; }
        public Vector2 MissionEntryTarget => _missionEntryTarget;
        public float MaximumMissionEntryDistance => 12f;
        public Rect ArenaBounds => _bounds;

        private DogController[] _dogs;
        private GamepadPlayerInput[] _inputs;
        private Vector2[] _dogStarts;
        private Sprite _sprite;
        private Sprite _rangeSprite;
        private Rect _bounds;
        private System.Random _rng;
        private int _missionSeed;
        private bool _reuseMissionSeedOnNextBegin;
        private Transform _treatRoot;
        private AudioSource _audio;
        private AudioSource _music;
        private readonly Dictionary<string, AudioClip> _audioClips = new();
        private readonly Dictionary<string, AudioCueSlot> _audioSlots = ArenaFeedbackCatalog.BuildLookup();
        private readonly List<string> _audioCueRequests = new();
        private readonly List<string> _rumbleRequests = new();
        private MissionDefinition _mission;
        private GameObject _bunnyCameoObject;
        private readonly HerdingMissionState _herdingState = new HerdingMissionState();
        private readonly BackyardSquirrelTrapState _backyardTrapState = new BackyardSquirrelTrapState();
        private Vector2 _backyardTrapGapPosition;
        private GameObject _backyardTrapGapMarker;
        private Treat _backyardDroppedWeenie;
        private const float BackyardTrapGapRadius = 3.2f;
        private readonly Dictionary<MissionVariant, IMissionController> _missionControllers = new();
        private IMissionController _activeMissionController;
        private KitchenFoodFrenzyMissionController KitchenController =>
            _activeMissionController as KitchenFoodFrenzyMissionController;
        private readonly ThreatSweepMissionState _threatSweepState = new ThreatSweepMissionState();
        private readonly PatrolDefenseMissionState _patrolState = new PatrolDefenseMissionState();
        private readonly Vector2[] _squirrelRoute = { new(-15f, 9f), new(0f, -9f), new(15f, 9f), new(12f, -8f) };
        private readonly Vector2[] _squirrelCutoffZones = { new(-8f, -3f), new(8f, -3f), new(16f, 2f), new(-3f, 3f) };
        private GameObject[] _squirrelCutoffMarkers;
        private const float SquirrelCutoffRadius = 3f;
        private const int ShadowSweepCount = 4;
        private const int EagleRequiredHides = 2;
        private const int EagleMaxExposures = 3;
        // Eagle Shadow Panic rescue phase (Rescue-Timing co-op puzzle): after the hides, the eagle SNATCHES
        // Cheddar into its talons. The held dog (Cheddar) wiggles (Tug/Rescue button) to crack the grip and
        // open a brief window; the free dog (Cocoa) pulls in that window to yank him down. Pulling with no
        // window open is a mistimed miss (recoverable). Enough well-timed pulls free him -> united front.
        private readonly CoopRescueTimingPuzzle _eagleRescue = new CoopRescueTimingPuzzle();
        private Vector2 _eagleSnatchPosition;
        private int _eagleRescuePullsSeen;
        private int _eagleRescueMissesSeen;
        private const int EagleRescuePulls = 3;
        private const float EagleRescueWindow = 1.2f; // a wiggle cracks the grip open for this long
        private const float EagleRescueRange = 3.5f;  // the free dog must be this close to pull
        private const int FenceGapCount = 4;
        private const int CoyoteRequiredRepairs = 3;
        private const int CoyoteMaxBreaches = 3;
        private const float EagleCoverRadius = 3f;
        private const float EagleShadowWidth = 3.5f;
        // Y the eagle shadow sweeps along: inside the dogs' play band (cover zones sit at y -8..+9)
        // so the sweep visibly crosses over the dogs instead of flying along the far top fence.
        private const float EagleSweepHeight = 0.5f;
        private Vector2 _stashPosition;
        private Vector2 _fenceGapPosition;
        private bool _coyotePressureHeld;
        private readonly Vector2[] _eagleCoverZones = { new(-14f, -8f), new(14f, -8f), new(0f, 9f) };
        private GameObject[] _eagleCoverMarkers;
        private int _eagleSweepDir = 1;
        private readonly Vector2[] _fenceGaps = { new(-22f, 6f), new(-22f, -6f), new(22f, 6f), new(22f, -6f) };
        private GameObject[] _coyoteGapMarkers;
        private readonly CarryRoundupMissionState _carryState = new CarryRoundupMissionState();
        private readonly Vector2[] _weenieSpots = { new(-18f, 10f), new(18f, 11f), new(-20f, -10f), new(6f, -11f), new(20f, -3f) };
        private GameObject[] _weenieMarkers;
        private GameObject[] _carriedMarkers;
        private GameObject _bowlObject;
        private bool[] _dogCarrying;
        private Vector2 _bowlPosition;
        private const int WeenieRequiredDeliveries = 5;
        private const float WeeniePickupRange = 1.6f;
        private const float BowlDeliverRange = 2.2f;
        private readonly ScentSearchMissionState _scentState = new ScentSearchMissionState();
        private readonly Vector2[] _digSpots = { new(-19f, 9f), new(17f, 10f), new(-18f, -9f), new(8f, -10f), new(20f, 1f), new(-3f, 6f) };
        private GameObject[] _digMarkers;
        private int _buriedSpot = -1;
        private const int ScentRequiredFinds = 3;
        private const int ScentMaxWastedDigs = 4;
        private const float DigRange = 2f;
        private readonly ThunderstormMissionState _stormState = new ThunderstormMissionState();
        private PanicMeter _panic;
        private float _nextThunderAt;
        private const int StormRequiredClaps = 5;
        private const float StormClapInterval = 5.5f;
        private int[] _dogContribution;
        private readonly LeashWalkMissionState _leashState = new LeashWalkMissionState();
        private readonly Vector2[] _leashCheckpoints = { new(-18f, -10f), new(18f, -10f), new(18f, 10f), new(-18f, 10f) };
        private GameObject[] _leashCheckpointMarkers;
        private const float CheckpointRange = 2.6f;
        private const float MaxLeash = 7f;
        private const int MaxLeashSnaps = 4;
        private float _nextLeashSnapAt;
        private readonly CarBalanceMissionState _carState = new CarBalanceMissionState();
        private float _carBalance;       // -1 (tipping left) .. +1 (tipping right)
        private int _carLurchDir = 1;
        private float _nextLurchAt;
        private const int CarRequiredLurches = 6;
        private const int CarMaxSpills = 4;
        private const float CarLurchInterval = 4f;
        // Gate Crash (Hold-and-Release co-op puzzle): Cocoa anchors the gate, Cheddar squeezes through.
        // Gate Crash now lives in GateCrashMissionController; this empty puzzle backs the compatibility
        // accessor when the mission is not the active controller.
        private readonly CoopHoldReleasePuzzle _emptyGatePuzzle = new CoopHoldReleasePuzzle();
        // Table Stealth (Human-Distraction co-op puzzle): Cocoa flops belly-up to hold the human's gaze
        // (sustain) while Cheddar sneaks the dropped steak from under the table; sneaking while the human
        // is looking gets the pair spotted (a recoverable exposure, not a silent punish).
        // Table Stealth now lives in TableStealthMissionController; this empty puzzle backs the
        // compatibility accessor when the mission is not the active controller.
        private readonly CoopHumanDistractionPuzzle _emptyTablePuzzle = new CoopHumanDistractionPuzzle();
        // Squirrel Switcheroo (Bait-and-Switch co-op puzzle): Cheddar feints at a decoy nut pile to lure
        // the squirrel off the buried stash; only while the squirrel is COMMITTED to chasing the decoy
        // can Cocoa raid the real stash. Over-feint and the squirrel wises up (or Cheddar chases his own
        // decoy) - the window snaps shut (a recoverable backfire, not a silent punish).
        // The Ol' Switcheroo now lives in SquirrelSwitcherooMissionController; this empty puzzle backs
        // the compatibility accessor when the mission is not the active controller.
        private readonly CoopBaitSwitchPuzzle _emptySwitcherooPuzzle = new CoopBaitSwitchPuzzle();
        // Walk Campaign (Social-Manipulation co-op puzzle): the dogs con the human into a walk by sending
        // ONE clear message built from BOTH dogs at once - Cocoa's dignified door-stare AND Cheddar
        // presenting the leash. Cover only one station (or neither) and the human gets confused and
        // brings the wrong thing (a recoverable misread); confuse them too many times and the walk is off.
        // The Walk Campaign now lives in WalkCampaignMissionController; this empty puzzle backs the
        // compatibility accessor when the mission is not the active controller.
        private readonly CoopSocialManipulationPuzzle _emptyWalkPuzzle = new CoopSocialManipulationPuzzle();
        // The Bone Detail (Scent-Relay split-information co-op puzzle): several look-alike dirt mounds, only
        // one hiding the real bone. Cocoa (the reader) noses the scent post to call which mound is real;
        // Cheddar (the digger) is the only one who can dig, but can't tell them apart on his own - so he
        // must wait for her call. Digging blind, or digging a decoy mound, just wastes a dig (recoverable);
        // waste too many and the team gives up. Each find re-buries the bone elsewhere, forcing a re-relay.
        private readonly CoopScentRelayPuzzle _boneRelay = new CoopScentRelayPuzzle();
        private readonly Vector2[] _boneMoundSpots = { new(-12f, -6f), new(12f, -6f), new(-12f, 6f), new(12f, 6f) };
        private Vector2 _boneScentZone = new(0f, 9f);
        private GameObject[] _boneMounds;
        private TextMesh[] _boneMoundLabels;
        private int _boneDiggerInside = -1;
        private int _boneFindsSeen;
        private int _boneBlindSeen;
        private int _boneWrongSeen;
        private static readonly Color BoneMoundIdleColor = new(0.42f, 0.3f, 0.16f);
        private static readonly Color BoneMoundCallColor = new(0.5f, 0.9f, 0.55f);
        private const float BoneScentRange = 3.5f;
        private const float BoneDigRange = 3f;
        private const int BoneFindsNeeded = 3;
        private const int BoneMaxWasted = 5; // blind digs + wrong-mound digs before the team gives up
        private readonly CoopSequenceChainPuzzle _emptyEscapePuzzle = new CoopSequenceChainPuzzle();
        // The Rube Goldberg (Chaos-Machine co-op puzzle): pre-position, pull the lever, and the cascade
        // runs ITSELF through junctions - but each junction has a brief assist window where its owner dog
        // must be in position or the machine misfires and stalls visibly at that exact junction. A
        // re-pull resumes from the stall. Both dogs must trust the timing and cover their junctions.
        private readonly CoopChaosMachinePuzzle _chaos = new CoopChaosMachinePuzzle();
        private readonly ChainActor[] _chaosOwners = { ChainActor.Cocoa, ChainActor.Cheddar, ChainActor.Cocoa };
        private readonly Vector2[] _chaosJunctionSpots = { new(-4f, 7f), new(6f, -7f), new(14f, 7f) };
        private readonly string[] _chaosJunctionActions = { "TOWEL DROP", "BASKET TIP", "TOY LAUNCH" };
        private Vector2 _chaosLeverZone = new(-14f, -7f);
        private GameObject[] _chaosJunctions;
        private TextMesh[] _chaosJunctionLabels;
        private int _chaosStageSeen;
        private int _chaosStallsSeen;
        private const float ChaosLeverRange = 3f;
        private const float ChaosJunctionRange = 3f;
        private const float ChaosWindowPerStage = 3f; // assist window at each junction
        private const int ChaosMaxStalls = 4;
        // The Blanket Catch (Stretch-Span co-op puzzle): both dogs grip a blanket stretched between them
        // to catch food falling from the counter. The span is only usable in a taut SEPARATION band -
        // too close and it sags (slack), too far and it rips - and its MIDPOINT must be under the falling
        // item. Both dogs must coordinate spacing AND position. Over-stretching rips the blanket; too many
        // rips and it's done. Catch enough to win.
        private readonly CoopStretchSpanPuzzle _blanket = new CoopStretchSpanPuzzle();
        private GameObject _blanketObject;       // the stretched blanket sprite (midpoint, scaled by span)
        private TextMesh _blanketLabel;
        private GameObject _blanketFallingItem;  // the current falling snack
        private float _blanketItemX;
        private float _blanketItemY;
        private int _blanketCaughtSeen;
        private int _blanketMissedSeen;
        private int _blanketRipsSeen;
        private const float BlanketMinSeparation = 4f;
        private const float BlanketMaxSeparation = 11f;
        private const float BlanketCatchTolerance = 2.5f;
        private const int BlanketCatchesNeeded = 5;
        private const int BlanketMaxRips = 3;
        private const float BlanketCatchLineY = -6f;  // the height the dogs hold the blanket at
        private const float BlanketSpawnY = 11f;      // food falls from up here
        private const float BlanketFallSpeed = 7f;
        // Mark the Yard now lives in MarkTheYardMissionController; this empty state backs the
        // compatibility accessor when the mission is not the active controller.
        private readonly TerritoryMissionState _emptyTerritoryState = new TerritoryMissionState();
        private readonly SockBasketMissionState _sockBasketState = new SockBasketMissionState();
        private GameObject _laundryBasketObject;
        private Treat _exposedSock;
        private float _sockOpeningUntil;
        private const float SockBasketInteractRange = 2.6f;
        private const float SockOpeningSeconds = 6f;

        private readonly List<Treat> _treats = new();
        private readonly List<string> _sessionRanks = new();
        private float[] _lastBarks;
        private float _nextUnitedBarkAt;
        private float _squirrelTimer;
        private float _squirrelScaredUntil;
        private float _nextSquirrelScareScoreAt;
        private Treat _squirrelTarget;
        private bool _squirrelHasStarted;
        private float _introPromptUntil;
        private float _scorePopUntil;
        private float _teamBarkFeedbackUntil;
        private float _predatorTimer;
        private int _predatorTarget = -1;
        private int _grabbedDog = -1;
        private float _nextZoomiesPulseAt;
        private int _selectedMissionIndex;
        private readonly bool[] _sessionCompletedMissions = new bool[MissionOrder.Length];
        private readonly bool[] _sessionClearedMissions = new bool[MissionOrder.Length];
        private readonly bool[] _sessionFlawlessMissions = new bool[MissionOrder.Length];
        private readonly int[] _sessionFailuresByMission = new int[MissionOrder.Length];
        private readonly int[] _sessionBestByMission = new int[MissionOrder.Length];
        private bool _roundResultRecorded;
        private string _lastLoggedObjective = string.Empty;
        private Vector2 _missionEntryTarget;
        private int _lastSummaryMilestoneShown;

        public void Init(DogController[] dogs, GamepadPlayerInput[] inputs, Sprite treatSprite, Sprite rangeSprite, Rect bounds, int seed)
        {
            _dogs = dogs;
            _inputs = inputs;
            _sprite = treatSprite;
            _rangeSprite = rangeSprite;
            _bounds = bounds;
            ConfigureSpatialLayout();
            _rng = new System.Random(seed);
            _dogStarts = new Vector2[dogs.Length];
            _lastBarks = new float[dogs.Length];
            _dogContribution = new int[dogs.Length];
            DogFeedback = new DogReadabilityFeedback[dogs.Length];
            ObjectiveArrows = new ObjectiveArrowFeedback[dogs.Length];
            InteractionRangeIndicators = new InteractionRangeIndicator[dogs.Length + 3];

            for (int i = 0; i < dogs.Length; i++)
            {
                _dogStarts[i] = dogs[i].transform.position;
                dogs[i].OnBark += OnDogBarked;
                dogs[i].OnInteract += OnDogInteracted;
                dogs[i].TryGetComponent(out DogReadabilityFeedback dogFeedback);
                dogs[i].TryGetComponent(out ObjectiveArrowFeedback objectiveArrow);
                DogFeedback[i] = dogFeedback;
                ObjectiveArrows[i] = objectiveArrow;
                InteractionRangeIndicators[i] = dogs[i].gameObject.AddComponent<InteractionRangeIndicator>();
                InteractionRangeIndicators[i].Init(_rangeSprite, new Color(0.6f, 1f, 0.75f, 0.42f), "RESCUE BARK");
            }

            _playtestLog.Clear();
            _panic = gameObject.AddComponent<PanicMeter>();
            _mission = BuildMissionDefinition(startingMission, _tuning);
            _selectedMissionIndex = IndexOfMission(startingMission);
            _treatRoot = new GameObject(_mission.ItemRootName).transform;
            BuildAudio();
            BuildMissionObjects();
            HideInteractionRanges();
            ShowMissionSelect();
        }

        private void ActivateMissionController(MissionVariant variant)
        {
            _activeMissionController?.Cleanup();
            _activeMissionController = null;

            if (!_missionControllers.TryGetValue(variant, out var controller))
            {
                if (!MissionControllerRegistry.TryCreate(variant, out controller)) return;
                controller.Initialize(CreateMissionContext());
                _missionControllers.Add(variant, controller);
            }

            _activeMissionController = controller;
            _activeMissionController.StartMission();
        }

        private MissionContext CreateMissionContext() => new MissionContext(
            dogs: _dogs,
            dogFeedback: DogFeedback,
            bounds: _bounds,
            actorSprite: _sprite,
            rangeSprite: _rangeSprite,
            random: () => _rng,
            now: () => Time.time,
            addScore: AddScore,
            creditDog: CreditDog,
            setCue: cue => LastCue = cue,
            setFeedback: feedback => LastFeedback = feedback,
            setJuice: SetJuice,
            spawnWorldPop: (position, text, color) => SpawnWorldPop(position, text, color),
            requestAudioCue: RequestAudioCue,
            requestRumble: RequestRumble,
            logEvent: LogPlaytestEvent,
            logObjectiveChanged: LogObjectiveIfChanged,
            markFailedInteraction: MarkFailedInteraction,
            addWorldLabel: AddWorldLabel);

        private void ConfigureSpatialLayout()
        {
            Vector2 P(float x, float y) => new Vector2(
                _bounds.center.x + x * _bounds.width * 0.5f,
                _bounds.center.y + y * _bounds.height * 0.5f);

            Vector2[] squirrel = { P(-0.78f, 0.68f), P(0f, -0.66f), P(0.78f, 0.68f), P(0.64f, -0.62f) };
            Vector2[] cutoffs = { P(-0.36f, -0.12f), P(0.36f, -0.12f), P(0.72f, 0.08f), P(-0.12f, 0.12f) };
            Vector2[] cover = { P(-0.7f, -0.64f), P(0.7f, -0.64f), P(0f, 0.68f) };
            Vector2[] gaps = { P(-0.94f, 0.38f), P(-0.94f, -0.38f), P(0.94f, 0.38f), P(0.94f, -0.38f) };
            Vector2[] weenies = { P(-0.72f, 0.62f), P(0.7f, 0.68f), P(-0.82f, -0.6f), P(0.22f, -0.7f), P(0.82f, -0.18f) };
            Vector2[] digs = { P(-0.78f, 0.58f), P(0.68f, 0.64f), P(-0.74f, -0.58f), P(0.32f, -0.68f), P(0.82f, 0.08f), P(-0.18f, 0.36f) };
            Vector2[] leash = { P(-0.72f, -0.62f), P(0.72f, -0.62f), P(0.72f, 0.62f), P(-0.72f, 0.62f) };
            _backyardTrapGapPosition = P(0.72f, 0.08f);

            squirrel.CopyTo(_squirrelRoute, 0);
            cutoffs.CopyTo(_squirrelCutoffZones, 0);
            cover.CopyTo(_eagleCoverZones, 0);
            gaps.CopyTo(_fenceGaps, 0);
            weenies.CopyTo(_weenieSpots, 0);
            digs.CopyTo(_digSpots, 0);
            leash.CopyTo(_leashCheckpoints, 0);
        }

        public void OnTreatCollected(Treat treat, DogController dog)
        {
            if (!MissionActive() || treat == null) return;

            bool trapRecovery = false;
            if (_mission.Variant == MissionVariant.BackyardRescue && treat == _backyardDroppedWeenie)
            {
                DogId dogId = dog != null && dog.TryGetComponent<DogIdentity>(out var identity)
                    ? identity.Id
                    : DogId.Cheddar;
                var recovery = _backyardTrapState.TryRecover(dogId);
                if (recovery != BackyardSquirrelTrapState.RecoveryResult.Success)
                {
                    RegisterBackyardTrapRecoveryFumble(dogId);
                    return;
                }

                trapRecovery = true;
                _backyardDroppedWeenie = null;
                LastCue = $"{dogId} recovered the trap weenie - roles reverse!";
                SpawnWorldPop(treat.transform.position,
                    _backyardTrapState.Complete ? "TRAP COMPLETE!" : "SWAP ROLES!",
                    new Color(0.45f, 1f, 0.65f));
                LogPlaytestEvent("SquirrelTrapRecovery", $"{dogId} recovered {_backyardTrapState.Recoveries}/{BackyardSquirrelTrapState.RequiredRecoveries}");
                UpdateBackyardTrapGapMarker();
            }

            bool partnerSockDive = false;
            if (_mission.Variant == MissionVariant.SockPanic)
            {
                int dogIndex = dog != null ? IndexOfDog(dog.GetComponent<DogIdentity>().Id) : -1;
                var result = _sockBasketState.TryCollect(dogIndex);
                if (result == SockBasketMissionState.CollectResult.BasketClosed)
                {
                    MarkFailedInteraction(dog != null ? dog.GetComponent<DogIdentity>().Id : DogId.Cheddar, "tip the laundry basket first");
                    return;
                }
                if (result == SockBasketMissionState.CollectResult.SameDogDecoy)
                {
                    RegisterSockFumble("DECOY! The basket-tipper needs their partner to dive.");
                    return;
                }
                partnerSockDive = true;
                _exposedSock = null;
                CloseSockBasketVisuals();
            }

            AddScore(
                partnerSockDive ? ScoreEventCatalog.SockDive.Points : _mission.ItemScore,
                partnerSockDive ? ScoreEventCatalog.SockDive.Label : trapRecovery ? "TRAP WEENIE RECOVERED" : _mission.CollectedScoreLabel);
            BreakfastRecovered++;
            LastCue = trapRecovery
                ? $"{DogName(dog)} recovered the dropped weenie" + (_backyardTrapState.Complete ? " - squirrel trap complete!" : " - swap roles!")
                : $"{DogName(dog)} recovered {_mission.ItemCollectCueNoun}!";
            Pulse(dog != null ? dog.gameObject : null, 1.2f);
            SetJuice(JuiceFeedbackKind.ScoreDelta, LastScoreEventLabel);
            SpawnWorldPop(dog != null ? dog.transform.position : treat.transform.position, LastScoreEventLabel, _mission.ItemPopColor);
            RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);

            _treats.Remove(treat);
            Destroy(treat.gameObject);
            SpawnTreat();
            LogPlaytestEvent("Collection", $"{DogName(dog)} collected {_mission.ItemCollectCueNoun} {BreakfastRecovered}/{recoveryGoal}");
            CheckClear();
            LogObjectiveIfChanged();
        }

        public void Restart()
        {
            if (_mission == null)
            {
                StartSelectedMission();
                return;
            }

            MissionReplayCount++;
            LogPlaytestEvent("Replay", _mission.Name);
            RequestAudioCue(ArenaFeedbackCatalog.UiReplayNextSelect);
            _reuseMissionSeedOnNextBegin = true;
            StartMission(_mission.Variant);
        }

        public void StartMission(MissionVariant variant)
        {
            SetPaused(false);
            SelectMission(variant);
            _mission = BuildMissionDefinition(variant, _tuning);
            BeginRound();
        }

        public void TogglePause()
        {
            if (IsPaused) SetPaused(false);
            else if (MissionActive()) SetPaused(true);
        }

        public void RequestQuit()
        {
            QuitRequested = true;
            LogPlaytestEvent("Quit", "requested");
#if !UNITY_EDITOR
            Application.Quit();
#endif
        }

        public void SelectMission(MissionVariant variant)
        {
            int index = IndexOfMission(variant);
            if (index < 0) return;

            _selectedMissionIndex = index;
            _mission = BuildMissionDefinition(variant, _tuning);
            if (MissionSelectVisible)
            {
                LastCue = $"{_mission.Name}: {_mission.IntroPrompt}";
                MissionBanner = "Mission Select";
                RequestAudioCue(ArenaFeedbackCatalog.UiReplayNextSelect);
                LogPlaytestEvent("MissionSelected", _mission.Name);
                LogObjectiveIfChanged();
            }
        }

        public void SelectPreviousMission()
        {
            _selectedMissionIndex = (_selectedMissionIndex + MissionOrder.Length - 1) % MissionOrder.Length;
            SelectMission(SelectedMissionVariant);
        }

        public void SelectNextMission()
        {
            _selectedMissionIndex = (_selectedMissionIndex + 1) % MissionOrder.Length;
            SelectMission(SelectedMissionVariant);
        }

        public void SelectMissionAbove() => SelectMissionGridStep(0, -1);
        public void SelectMissionBelow() => SelectMissionGridStep(0, 1);
        public void SelectMissionLeft() => SelectMissionGridStep(-1, 0);
        public void SelectMissionRight() => SelectMissionGridStep(1, 0);

        private void SelectMissionGridStep(int columnDelta, int rowDelta)
        {
            const int columns = 2;
            int rows = Mathf.CeilToInt(MissionOrder.Length / (float)columns);
            int currentColumn = _selectedMissionIndex / rows;
            int currentRow = _selectedMissionIndex % rows;
            int targetColumn = (currentColumn + columnDelta + columns) % columns;
            int targetRow = (currentRow + rowDelta + rows) % rows;
            int targetIndex = targetColumn * rows + targetRow;

            // Keep grid navigation safe if the final column is not full.
            if (targetIndex >= MissionOrder.Length)
                targetIndex = MissionOrder.Length - 1;

            SelectMission(MissionOrder[targetIndex]);
        }

        public void StartSelectedMission() => StartMission(SelectedMissionVariant);

        public void ReturnToMissionSelect()
        {
            SetPaused(false);
            RequestAudioCue(ArenaFeedbackCatalog.UiReplayNextSelect);
            ShowMissionSelect();
        }

        /// <summary>Clear all session-accumulated stats for a fresh couch sitting.</summary>
        public void ResetSession()
        {
            SessionMissionsPlayed = 0;
            SessionTotalScore = 0;
            SessionStarsEarned = 0;
            SessionFlawlessClears = 0;
            SessionUniqueMissionsCompleted = 0;
            _lastSummaryMilestoneShown = 0;
            _sessionRanks.Clear();
            System.Array.Clear(_sessionCompletedMissions, 0, _sessionCompletedMissions.Length);
            System.Array.Clear(_sessionClearedMissions, 0, _sessionClearedMissions.Length);
            System.Array.Clear(_sessionFlawlessMissions, 0, _sessionFlawlessMissions.Length);
            System.Array.Clear(_sessionFailuresByMission, 0, _sessionFailuresByMission.Length);
            System.Array.Clear(_sessionBestByMission, 0, _sessionBestByMission.Length);
            SessionSummaryLabel = "Session Summary: no missions played yet.";
            SessionRanksEarnedLabel = "Ranks: none yet.";
            LogPlaytestEvent("SessionReset", "fresh session");
        }

        public void ChooseNextMission()
        {
            RequestAudioCue(ArenaFeedbackCatalog.UiReplayNextSelect);
            if (SessionSummaryReady)
            {
                LogPlaytestEvent("Next", "Session Summary");
                ShowSessionSummary();
                return;
            }

            int current = _mission != null ? IndexOfMission(_mission.Variant) : _selectedMissionIndex;
            int next = NextUnfinishedMissionIndex(current);
            LogPlaytestEvent("Next", MissionOrder[next].ToString());
            StartMission(MissionOrder[next]);
        }

        public void ContinueSession()
        {
            RequestAudioCue(ArenaFeedbackCatalog.UiReplayNextSelect);
            int current = _mission != null ? IndexOfMission(_mission.Variant) : _selectedMissionIndex;
            int next = NextUnfinishedMissionIndex(current);
            LogPlaytestEvent("ContinueSession", MissionOrder[next].ToString());
            StartMission(MissionOrder[next]);
        }

        public void ShowSessionSummary()
        {
            _lastSummaryMilestoneShown = Mathf.Max(_lastSummaryMilestoneShown, SessionUniqueMissionsCompleted / 3);
            CurrentFlow = FlowState.SessionSummary;
            Phase = State.Intro;
            Outcome = MissionOutcome.InProgress;
            MissionBanner = "Session Summary";
            LastCue = SessionSummaryLabel;
            LastFeedback = FeedbackKind.Intro;
            DisableDogInputs();
            HideObjectiveArrows();
            HideInteractionRanges();
            SetMissionObjectsActive(false);
            LogPlaytestEvent("SessionSummary", SessionSummaryLabel);
            LogObjectiveIfChanged();
        }

        public void TogglePlaytestOverlay() => SetPlaytestOverlayVisible(!PlaytestOverlayVisible);

        public void SetPlaytestOverlayVisible(bool visible)
        {
            if (PlaytestOverlayVisible == visible) return;
            PlaytestOverlayVisible = visible;
            LogPlaytestEvent("Overlay", visible ? "shown" : "hidden");
        }

        public void SetAudioEnabled(bool enabled)
        {
            AudioEnabled = enabled;
            if (_audio != null) _audio.mute = !enabled;
            if (_music != null) _music.mute = !enabled;
            LogPlaytestEvent("Audio", enabled ? "enabled" : "disabled");
        }

        public void SetRumbleEnabled(bool enabled)
        {
            RumbleEnabled = enabled;
            if (!enabled) StopRumble();
            LogPlaytestEvent("Rumble", enabled ? "enabled" : "disabled");
        }

        public void ClearFeedbackRequests()
        {
            _audioCueRequests.Clear();
            _rumbleRequests.Clear();
            LastAudioCueRequested = string.Empty;
            LastRumbleRequested = string.Empty;
        }

        public void SetRoundDuration(float seconds)
        {
            roundDuration = Mathf.Max(0.01f, seconds);
            if (MissionActive()) TimeRemaining = Mathf.Min(TimeRemaining, roundDuration);
        }

        public bool LastRoundWasBest { get; private set; }
        public bool LastRoundFlawless { get; private set; }

        public string MvpLabel
        {
            get
            {
                if (_dogContribution == null || _dogs == null || _dogs.Length == 0) return "MVP: --";
                int best = -1, bestIdx = -1; bool tie = false;
                for (int i = 0; i < _dogContribution.Length; i++)
                {
                    if (_dogContribution[i] > best) { best = _dogContribution[i]; bestIdx = i; tie = false; }
                    else if (_dogContribution[i] == best) tie = true;
                }
                if (best <= 0) return "MVP: awaiting dog heroics";
                if (tie) return "MVP: Nose-to-nose draw - chaos meets queen";
                string dog = DogName(_dogs[bestIdx]);
                return dog == "Cheddar"
                    ? $"MVP: Cheddar - Chaos Crown ({best} big plays)"
                    : $"MVP: Cocoa - Queen of the Yard ({best} clutch plays)";
            }
        }

        public string FlawlessRivalryLabel
        {
            get
            {
                if (!LastRoundFlawless || _dogContribution == null || _dogContribution.Length < 2) return string.Empty;
                if (_dogContribution[0] == _dogContribution[1]) return "FLAWLESS PACK: chaos + calm";
                int winner = _dogContribution[0] > _dogContribution[1] ? 0 : 1;
                return DogName(_dogs[winner]) == "Cheddar"
                    ? "FLAWLESS: Cheddar caused exactly the right chaos"
                    : "FLAWLESS: Cocoa upheld the royal standard";
            }
        }

        public int BestScoreForMission(MissionVariant variant)
        {
            int index = IndexOfMission(variant);
            return index >= 0 && index < _sessionBestByMission.Length ? _sessionBestByMission[index] : 0;
        }

        public string MissionSelectStatusFor(MissionVariant variant)
        {
            int index = IndexOfMission(variant);
            if (index < 0 || !_sessionCompletedMissions[index]) return "NEW";

            string result = _sessionFlawlessMissions[index]
                ? "FLAWLESS"
                : _sessionClearedMissions[index] ? "CLEARED" : "RETRY";
            return $"{result} • BEST {_sessionBestByMission[index]}";
        }

        public string MissionSelectDetailsFor(MissionVariant variant)
        {
            var mission = BuildMissionDefinition(variant, _tuning);
            string goal = mission.ItemGoal > 0 ? $"{mission.ItemGoal} {mission.ItemRootName}" : "team objective";
            return $"{mission.RoundSeconds:0}s • {goal}";
        }

        public int FailuresForMission(MissionVariant variant)
        {
            int index = IndexOfMission(variant);
            return index >= 0 && index < _sessionFailuresByMission.Length ? _sessionFailuresByMission[index] : 0;
        }

        public void ForcePredatorWarning()
        {
            if (MissionActive() && _mission.RequiresPredator) StartPredatorWarning();
        }

        public void ForcePredatorAttack()
        {
            if (_mission.RequiresPredator && (MissionActive() || Phase == State.PredatorWarning)) StartPredatorAttack();
        }

        public void ForceSquirrelStealAttempt()
        {
            if (!_mission.UsesSquirrel || !MissionActive() || _treats.Count == 0) return;

            var nearby = FindTreatNear(SquirrelObject.transform.position, 0.05f);
            if (nearby != null)
            {
                _squirrelTarget = nearby;
                SquirrelStealsTarget();
                UpdateInteractionRanges();
                return;
            }

            var target = FindNearestTreat(SquirrelObject.transform.position) ?? _treats[0];
            StartSquirrelSteal(target);
            UpdateInteractionRanges();
        }

        public void ForceBackyardTrapRedirect(DogId pressureDog, bool gapHeld = true)
        {
            if (!MissionActive() || _mission.Variant != MissionVariant.BackyardRescue || _backyardTrapState.Complete) return;
            if (_squirrelTarget == null && !_backyardTrapState.WeenieDropped && _treats.Count > 0)
                StartSquirrelSteal(_treats[0]);
            ResolveBackyardTrapPressure(pressureDog, gapHeld);
            UpdateObjectiveArrows();
            UpdateInteractionRanges();
        }

        public void ForceBackyardTrapRecovery(DogId dogId)
        {
            if (!MissionActive() || _mission.Variant != MissionVariant.BackyardRescue || _backyardDroppedWeenie == null) return;
            int dogIndex = IndexOfDog(dogId);
            if (dogIndex >= 0) _backyardDroppedWeenie.CollectBy(_dogs[dogIndex]);
        }

        public void ForceGameOver() => EndRound(false);


        public void ForceSquirrelConspiracyHerd(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy)
                TryProgressSquirrelConspiracyBark(IndexOfDog(dogId));
        }

        public void ForceSquirrelConspiracyTaunt()
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.SquirrelConspiracy) return;
            RegisterSquirrelTaunt();
        }

        public void ForceSquirrelConspiracyFindStash(DogId dogId = DogId.Cocoa)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy)
                TryFindConspiracyStash(dogId, true);
        }

        public void ForceEagleShadowSafeHide()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.EagleShadowPanic)
                RegisterEagleShadowSafeHide();
        }

        public void ForceEagleShadowExposure()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.EagleShadowPanic)
                RegisterEagleShadowExposure();
        }

        /// <summary>Test/convenience hook: run the full wiggle+pull rescue to free the snatched dog.</summary>
        public void ForceEagleShadowRescue(DogId dogId = DogId.Cheddar)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.EagleShadowPanic) return;
            if (!_threatSweepState.RescueObjectiveActive || _threatSweepState.RescueComplete) return;
            int guard = 0;
            while (!_eagleRescue.Freed && guard++ < 50)
            {
                _eagleRescue.Wiggle();   // crack the grip open
                _eagleRescue.Pull();     // and pull within the window
                HandleEagleRescueProgress();
            }
        }

        public void ForceEagleShadowUnitedFront()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.EagleShadowPanic)
                CompleteEagleShadowUnitedFront();
        }

        public void ForceEagleShadowSweepPass()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.EagleShadowPanic)
                EvaluateEagleShadowSweep();
        }

        public void ForceCoyoteBarkPressure(DogId dogId = DogId.Cocoa)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.CoyotesFence)
                RegisterCoyoteBarkPressure(IndexOfDog(dogId));
        }

        public void ForceCoyoteRepair(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.CoyotesFence)
                TryCoyoteRepair(dogId, true);
        }

        public void ForceCoyoteBreach()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.CoyotesFence)
                RegisterCoyoteBreach();
        }

        public void ForceCoyoteFakeSnack()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.CoyotesFence)
                TriggerCoyoteFakeSnack();
        }

        public void ForceCoyoteFinalBlock()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.CoyotesFence)
                CompleteCoyoteFinalPressure();
        }

        public void ForceCoyoteProwlReach()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.CoyotesFence)
                EvaluatePatrolReach();
        }

        public void ForceSockBasketTip(DogId dogId = DogId.Cocoa)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.SockPanic)
                TryTipSockBasket(dogId, true);
        }

        public void ForceSockBasketTimeout()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.SockPanic &&
                _sockBasketState.ExpireOpening())
                RegisterSockFumble("FUMBLE! The basket flopped shut on the runaway sock.");
        }

        private void BeginRound()
        {
            if (_mission == null) _mission = BuildMissionDefinition(startingMission, _tuning);
            CurrentFlow = FlowState.Playing;
            roundDuration = _mission.RoundSeconds;
            treatCount = _mission.SpawnedItemCount;
            recoveryGoal = _mission.ItemGoal;
            maxStolenFood = _mission.MaxStolenFood;
            if (_treatRoot != null) _treatRoot.name = _mission.ItemRootName;

            Score = 0;
            LastScoreDelta = 0;
            UnitedBarks = 0;
            BreakfastRecovered = 0;
            StolenFood = 0;
            PredatorResolved = false;
            PredatorFailed = false;
            TugProgress = 0f;
            TugComplete = false;
            StarRating = 0;
            Outcome = MissionOutcome.InProgress;
            TimeRemaining = roundDuration;
            Phase = State.Playing;
            LastCue = MissionIntroPrompt;
            LastScoreEventLabel = $"0 {_mission.ReadyScoreLabel}";
            MissionBanner = MissionIntroPrompt;
            EndRank = "Needs More Bark";
            EndSummaryLabel = string.Empty;
            EndReasonLabel = string.Empty;
            LastFeedback = FeedbackKind.Intro;
            LastJuiceFeedback = JuiceFeedbackKind.None;
            LastJuiceLabel = string.Empty;
            LastScorePopLabel = string.Empty;
            _roundResultRecorded = false;
            BarksUsed = 0;
            FailedInteractions = 0;
            ObjectiveChangeCount = 0;

            if (!_reuseMissionSeedOnNextBegin)
                _missionSeed = MissionSeedGenerator.StableSeed(_mission.Variant.ToString(), SessionMissionsPlayed, _selectedMissionIndex);
            _reuseMissionSeedOnNextBegin = false;
            _rng = new System.Random(_missionSeed);
            ActiveModifier = (RoundModifier)_rng.Next(0, 3);
            ActivateMissionController(_mission.Variant);
            _herdingState.Reset();
            _backyardTrapState.Reset();
            _backyardDroppedWeenie = null;
            _threatSweepState.Reset();
            _patrolState.Reset();
            _coyotePressureHeld = false;
            _stashPosition = new Vector2(_bounds.xMax - 1.7f, _bounds.yMin + 1.7f);
            _fenceGapPosition = _fenceGaps[0];
            _carryState.Reset();
            _scentState.Reset();
            _stormState.Reset();
            _leashState.Reset();
            _carState.Reset();
            _sockBasketState.Reset();
            _exposedSock = null;
            _sockOpeningUntil = 0f;
            _carBalance = 0f;
            _carLurchDir = 1;
            if (_dogContribution != null) System.Array.Clear(_dogContribution, 0, _dogContribution.Length);
            if (_panic != null) _panic.ResetMeter();
            _bowlPosition = new Vector2(_bounds.xMax - 4f, _bounds.yMin + 3f);
            if (_dogCarrying != null)
                for (int i = 0; i < _dogCarrying.Length; i++) _dogCarrying[i] = false;
            _nextUnitedBarkAt = 0f;
            _teamBarkFeedbackUntil = 0f;
            _scorePopUntil = 0f;
            _squirrelScaredUntil = 0f;
            _nextSquirrelScareScoreAt = 0f;
            _squirrelTarget = null;
            _squirrelHasStarted = false;
            _introPromptUntil = Time.time + _tuning.IntroPromptSeconds;
            _squirrelTimer = SquirrelDelay();
            _predatorTimer = _mission.RequiresPredator ? _tuning.PredatorWarningAt : float.PositiveInfinity;
            _predatorTarget = -1;
            _grabbedDog = -1;
            _nextZoomiesPulseAt = Time.time + 6f;

            for (int i = 0; i < _dogs.Length; i++)
            {
                _lastBarks[i] = float.NegativeInfinity;
                _dogs[i].SetMode(MovementMode.Free);
                _dogs[i].SetTravelAssist(false);
                if (DogFeedback[i] != null) DogFeedback[i].SetCarrying(false);
                if (DogFeedback[i] != null) DogFeedback[i].ClearMissionPose();
                _dogs[i].transform.position = _dogStarts[i];
                if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
                if (i < _inputs.Length && _inputs[i] != null) _inputs[i].enabled = true;
                if (ObjectiveArrows[i] != null) ObjectiveArrows[i].Hide();
            }

            ClearTreats();
            for (int i = 0; i < treatCount; i++) SpawnTreat();

            // Park the waiting squirrel on a visible perch just inside the close camera instead of the
            // far yard corner, so players actually see the threat the HUD/arrows reference. It is still
            // far enough from the dog spawns (+/-10,0) to not be in instant bark range.
            PlaceObject(SquirrelObject, _mission.Variant == MissionVariant.SquirrelConspiracy ? _squirrelRoute[0] : new Vector2(11f, 7f));
            PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
            PlaceObject(RopeObject, Vector2.zero);
            PlaceObject(_bunnyCameoObject, new Vector2(_bounds.xMin + 1.4f, _bounds.yMin + 1.0f));
            SquirrelObject.SetActive(_mission.UsesSquirrel);
            PredatorObject.SetActive(_mission.RequiresPredator);
            RopeObject.SetActive(_mission.RequiresTug);
            if (_bunnyCameoObject != null) _bunnyCameoObject.SetActive(true);
            if (_mission.UsesSquirrel) SetActorState(SquirrelObject, _mission.Variant == MissionVariant.SquirrelConspiracy ? "SQUIRREL CONSPIRACY ROUTE 1" : "Squirrel: WAITING", new Color(0.55f, 0.32f, 0.12f), 0.06f);
            UpdateBackyardTrapGapMarker();
            if (_mission.Variant == MissionVariant.SquirrelConspiracy) UpdateSquirrelCutoffMarkers();
            else SetSquirrelCutoffMarkersActive(false);
            if (_mission.RequiresPredator) SetActorState(PredatorObject, "Predator: OFFSCREEN", Color.gray, 0.04f);
            if (_mission.RequiresTug) SetActorState(RopeObject, "Rope/Tug - BOTH DOGS", new Color(0.95f, 0.7f, 0.15f), 0.08f);
            if (_mission.Variant == MissionVariant.EagleShadowPanic)
            {
                PredatorObject.SetActive(true);
                // Sweep across the dogs' play band (around the cover zones) instead of along the far top
                // fence, so the shadow is actually seen passing overhead. Exposure stays x-column based.
                PlaceObject(PredatorObject, new Vector2(-(_bounds.xMax - 1.5f), EagleSweepHeight));
                SetActorState(PredatorObject, "EAGLE SHADOW SWEEP - HIDE IN COVER!", new Color(0.16f, 0.16f, 0.2f), 0.3f);
                // The talon-grip indicator (reuses the squirrel actor) only appears once a dog is snatched.
                // Keep the snatch/rescue point inside the play band so the rescue beat is on-screen.
                _eagleSnatchPosition = new Vector2(0f, 6f);
                if (SquirrelObject != null) SquirrelObject.SetActive(false);
                _eagleRescue.Reset();
                _eagleRescuePullsSeen = 0;
                _eagleRescueMissesSeen = 0;
                _eagleSweepDir = 1;
                SetEagleCoverMarkersActive(true);
            }
            else
            {
                SetEagleCoverMarkersActive(false);
            }
            if (_mission.Variant == MissionVariant.CoyotesFence)
            {
                PredatorObject.SetActive(true);
                SquirrelObject.SetActive(true);
                _patrolState.SelectGap(0);
                _fenceGapPosition = _fenceGaps[0];
                PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
                PlaceObject(SquirrelObject, _fenceGapPosition);
                SetActorState(PredatorObject, "COYOTE AT THE FENCE - BARK PRESSURE!", new Color(0.55f, 0.32f, 0.12f), 0.28f);
                SetActorState(SquirrelObject, "WEAK SPOT - FILL DIRT (NEEDS PARTNER BARK)", new Color(0.62f, 0.45f, 0.2f), 0.1f);
                SetCoyoteGapMarkersActive(true);
            }
            else
            {
                SetCoyoteGapMarkersActive(false);
            }
            if (_mission.Variant == MissionVariant.WeenieRoundup)
            {
                _carryState.Configure(WeenieRequiredDeliveries);
                if (_bowlObject != null) { _bowlObject.SetActive(true); PlaceObject(_bowlObject, _bowlPosition); SetActorState(_bowlObject, $"HOME BOWL 0/{WeenieRequiredDeliveries}", new Color(0.85f, 0.85f, 0.9f), 0.1f); }
                if (_weenieMarkers != null)
                    for (int i = 0; i < _weenieMarkers.Length; i++)
                        if (_weenieMarkers[i] != null) { _weenieMarkers[i].transform.position = _weenieSpots[i]; _weenieMarkers[i].SetActive(true); }
                if (_carriedMarkers != null)
                    foreach (var marker in _carriedMarkers) if (marker != null) marker.SetActive(false);
            }
            else
            {
                SetWeenieRoundupActive(false);
            }
            if (_mission.Variant == MissionVariant.ScentSearch)
            {
                _scentState.Reset();
                if (_digMarkers != null)
                    for (int i = 0; i < _digMarkers.Length; i++)
                        if (_digMarkers[i] != null) { _digMarkers[i].transform.position = _digSpots[i]; _digMarkers[i].SetActive(true); }
                ChooseBuriedSpot();
            }
            else
            {
                SetScentSearchActive(false);
            }
            if (_mission.Variant == MissionVariant.ThunderstormComfort)
            {
                _stormState.Configure(StormRequiredClaps);
                if (_panic != null) _panic.ResetMeter();
                _nextThunderAt = Time.time + StormClapInterval;
                if (PredatorObject != null)
                {
                    PredatorObject.SetActive(true);
                    PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax - 1.5f));
                    SetActorState(PredatorObject, "STORM CLOUD - HUDDLE TO STAY CALM!", new Color(0.32f, 0.34f, 0.45f), 0.18f);
                }
            }
            if (_mission.Variant == MissionVariant.LeashWalk)
            {
                _leashState.Configure(_leashCheckpoints.Length);
                _nextLeashSnapAt = 0f;
                // Start the tethered dogs side by side (default spawns are far enough apart to snap
                // the leash instantly), then it's on the players to stay close as they walk.
                if (_dogs != null && _dogs.Length >= 2)
                {
                    _dogs[0].transform.position = new Vector2(-1.5f, 0f);
                    _dogs[1].transform.position = new Vector2(1.5f, 0f);
                    foreach (var dog in _dogs)
                        if (dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
                }
                SetLeashCheckpointMarkersActive(true);
            }
            else
            {
                SetLeashCheckpointMarkersActive(false);
            }
            if (_mission.Variant == MissionVariant.CarRide)
            {
                _carState.Configure(CarRequiredLurches);
                _carBalance = 0f;
                _carLurchDir = 1;
                _nextLurchAt = Time.time + CarLurchInterval;
                if (PredatorObject != null)
                {
                    PredatorObject.SetActive(true);
                    PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax - 1.5f));
                    SetActorState(PredatorObject, "CAR - LEAN TO KEEP IT LEVEL!", new Color(0.5f, 0.4f, 0.3f), 0.12f);
                }
            }
            if (_laundryBasketObject != null)
            {
                bool sockPanic = _mission.Variant == MissionVariant.SockPanic;
                _laundryBasketObject.SetActive(sockPanic);
                if (sockPanic)
                {
                    PlaceObject(_laundryBasketObject, _bounds.center);
                    SetActorState(_laundryBasketObject, "LAUNDRY BASKET - ONE DOG TIP, PARTNER DIVE!", new Color(0.78f, 0.56f, 0.3f), 0.08f);
                }
            }
            if (_mission.Variant == MissionVariant.BoneRelay)
            {
                _boneRelay.Configure(_boneMoundSpots.Length, BoneFindsNeeded, _missionSeed);
                _boneDiggerInside = -1;
                _boneFindsSeen = 0;
                _boneBlindSeen = 0;
                _boneWrongSeen = 0;
                SetBoneRelayActive(true);
                // Reuse the squirrel actor as the scent post Cocoa noses to call the real mound.
                if (SquirrelObject != null)
                {
                    SquirrelObject.SetActive(true);
                    PlaceObject(SquirrelObject, _boneScentZone);
                    SetActorState(SquirrelObject, "SCENT POST - COCOA SNIFF HERE", new Color(0.7f, 0.6f, 0.95f), 0.12f);
                }
                UpdateBoneMoundVisuals();
            }
            else
            {
                SetBoneRelayActive(false);
            }
            if (_mission.Variant == MissionVariant.ChaosMachine)
            {
                _chaos.Configure(_chaosJunctionSpots.Length, ChaosWindowPerStage);
                _chaosStageSeen = 0;
                _chaosStallsSeen = 0;
                SetChaosJunctionsActive(true);
                // Reuse the predator actor as the lever the dogs pull to start the cascade.
                if (PredatorObject != null)
                {
                    PredatorObject.SetActive(true);
                    PlaceObject(PredatorObject, _chaosLeverZone);
                    SetActorState(PredatorObject, "LEVER - PULL TO START THE CASCADE", new Color(0.85f, 0.55f, 0.3f), 0.14f);
                }
                UpdateChaosVisuals();
            }
            else
            {
                SetChaosJunctionsActive(false);
            }
            if (_mission.Variant == MissionVariant.BlanketCatch)
            {
                _blanket.Configure(BlanketMinSeparation, BlanketMaxSeparation, BlanketCatchTolerance, BlanketCatchesNeeded, BlanketMaxRips);
                _blanketCaughtSeen = 0;
                _blanketMissedSeen = 0;
                _blanketRipsSeen = 0;
                if (_blanketObject != null) _blanketObject.SetActive(true);
                SpawnBlanketItem();
                UpdateBlanketVisuals();
            }
            else
            {
                if (_blanketObject != null) _blanketObject.SetActive(false);
                if (_blanketFallingItem != null) _blanketFallingItem.SetActive(false);
            }
            StageDogsForMissionEntry();
            UpdateObjectiveArrows();
            _lastLoggedObjective = string.Empty;
            LogPlaytestEvent("MissionStarted", $"{_mission.Name} / {ActiveModifierLabel} / {roundDuration:0}s");
            LogObjectiveIfChanged();
        }

        private void Update()
        {
            TickFlowInput();
            if (!MissionActive()) return;

            TickMissionSelectionKeys();
            if (!MissionActive()) return;

            MissionBanner = Time.time < _introPromptUntil ? MissionIntroPrompt : string.Empty;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                EndRound(false);
                return;
            }

            TickModifier();
            if (_mission.Variant == MissionVariant.SockPanic) TickSockBasket();
            else if (_mission.Variant == MissionVariant.SquirrelConspiracy) TickSquirrelConspiracy();
            else if (_mission.Variant == MissionVariant.EagleShadowPanic) TickThreatSweep();
            else if (_mission.Variant == MissionVariant.CoyotesFence) TickPatrolDefense();
            else if (_mission.Variant == MissionVariant.WeenieRoundup) TickCarryRoundup();
            else if (_mission.Variant == MissionVariant.ScentSearch) { }
            else if (_mission.Variant == MissionVariant.ThunderstormComfort) TickThunderstorm();
            else if (_mission.Variant == MissionVariant.LeashWalk) TickLeashWalk();
            else if (_mission.Variant == MissionVariant.CarRide) TickCarRide();
            else if (_mission.Variant == MissionVariant.BoneRelay) TickBoneRelay();
            else if (_mission.Variant == MissionVariant.ChaosMachine) TickChaosMachine();
            else if (_mission.Variant == MissionVariant.BlanketCatch) TickBlanketCatch();
            else if (_activeMissionController != null) _activeMissionController.Tick(Time.deltaTime, Time.time);
            else TickSquirrel();
            TickPredator();
            TickTugProximity();
            CheckClear();
            UpdateBackyardTrapGapMarker();
            UpdateObjectiveArrows();
            UpdateTravelAssists();
            UpdateInteractionRanges();
            LogObjectiveIfChanged();
        }

        private void TickModifier()
        {
            if (ActiveModifier != RoundModifier.ZoomiesSurge || Time.time < _nextZoomiesPulseAt) return;

            foreach (var dog in _dogs) dog.TriggerZoomies();
            LastCue = "Zoomies surge! Hold the line!";
            _nextZoomiesPulseAt = Time.time + 10f;
            RequestAudioCue(ArenaFeedbackCatalog.Bark);
            LogPlaytestEvent("Modifier", LastCue);
        }

        private void TickSquirrel()
        {
            if (!_mission.UsesSquirrel) return;
            if (_mission.Variant == MissionVariant.BackyardRescue && _backyardTrapState.WeenieDropped) return;
            if (Time.time < _squirrelScaredUntil) return;

            var nearbySnack = FindTreatNear(SquirrelObject.transform.position, 0.3f);
            if (nearbySnack != null)
            {
                _squirrelTarget = nearbySnack;
                SquirrelStealsTarget();
                return;
            }

            if (_squirrelTarget == null)
            {
                _squirrelTimer -= Time.deltaTime;
                if (_squirrelTimer <= 0f && _treats.Count > 0)
                {
                    StartSquirrelSteal(_treats[_rng.Next(_treats.Count)]);
                }
                return;
            }

            SquirrelObject.transform.position = Vector3.MoveTowards(
                SquirrelObject.transform.position,
                _squirrelTarget.transform.position,
                Time.deltaTime * _tuning.SquirrelMoveSpeed);

            if (Vector2.Distance(SquirrelObject.transform.position, _squirrelTarget.transform.position) < 0.25f)
                SquirrelStealsTarget();
        }

        private void StartSquirrelSteal(Treat target)
        {
            if (target == null) return;

            _squirrelTarget = target;
            _squirrelHasStarted = true;
            LastFeedback = FeedbackKind.SquirrelStealing;
            LastCue = _mission.SquirrelStealingCue;
            SetJuice(JuiceFeedbackKind.WarningMiss, _mission.SquirrelObjectiveText.ToUpperInvariant());
            SetActorState(SquirrelObject, _mission.SquirrelStealingActorLabel, new Color(0.7f, 0.35f, 0.08f), 0.32f);
            RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            RequestRumble("squirrel_warning", 0.12f, 0.24f, 0.12f);
            LogPlaytestEvent("SquirrelPressure", _mission.SquirrelStealingActorLabel);
            LogObjectiveIfChanged();
        }

        private void SquirrelStealsTarget()
        {
            if (_squirrelTarget != null)
            {
                _treats.Remove(_squirrelTarget);
                Destroy(_squirrelTarget.gameObject);
                SpawnTreat();
            }

            StolenFood++;
            AddScore(-(ActiveModifier == RoundModifier.PancakePanic ? _tuning.PancakeSquirrelPenalty : _mission.SquirrelPenalty), _mission.SquirrelStealScoreLabel);
            _squirrelTarget = null;
            _squirrelTimer = SquirrelDelay();
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = _mission.SquirrelStoleCue;
            SetJuice(JuiceFeedbackKind.WarningMiss, _mission.SquirrelStealJuiceLabel);
            SetActorState(SquirrelObject, _mission.SquirrelStoleActorLabel, Color.gray, 0.22f);
            SpawnWorldPop(SquirrelObject.transform.position, _mission.SquirrelMissPopLabel, new Color(1f, 0.35f, 0.2f));
            RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            RequestRumble("squirrel_penalty", 0.18f, 0.38f, 0.16f);
            LogPlaytestEvent("SquirrelStole", $"{StolenFood}/{maxStolenFood}");

            if (StolenFood >= maxStolenFood) EndRound(false);
        }


        private void TickSquirrelConspiracy()
        {
            if (!_mission.UsesSquirrel || SquirrelObject == null) return;

            _squirrelTimer -= Time.deltaTime;
            Vector2 target = _herdingState.StashRevealed ? _stashPosition : _squirrelRoute[_herdingState.RouteIndex];
            SquirrelObject.transform.position = Vector3.MoveTowards(
                SquirrelObject.transform.position,
                target,
                Time.deltaTime * (_tuning.SquirrelMoveSpeed * 0.65f));

            if (!_herdingState.StashRevealed && _squirrelTimer <= 0f)
                RegisterSquirrelTaunt();
        }

        private bool TryProgressSquirrelConspiracyBark(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _dogs.Length || _herdingState.StashFound) return false;

            float distance = Vector2.Distance(_dogs[dogIndex].transform.position, SquirrelObject.transform.position);
            if (distance > _tuning.SingleBarkSquirrelRange)
            {
                _herdingState.AddFakeOut();
                AddScore(ScoreEventCatalog.FakeOut.Points, ScoreEventCatalog.FakeOut.Label);
                LastCue = "The squirrel sold a fake-out and the dogs barked at absolutely nothing.";
                SetJuice(JuiceFeedbackKind.WarningMiss, "FAKE OUT!");
                SpawnWorldPop(_dogs[dogIndex].transform.position, "FAKE OUT!", new Color(1f, 0.42f, 0.24f));
                RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
                LogPlaytestEvent("SquirrelFakeOut", LastCue);
                return false;
            }

            bool cutoff = IsPartnerHoldingSquirrelCutoff(dogIndex);
            var scoreEvent = cutoff ? ScoreEventCatalog.Cutoff : ScoreEventCatalog.GoodHerd;
            if (cutoff) _herdingState.AddCutoff();
            else _herdingState.AddHerd();
            _herdingState.AdvanceRoute(_squirrelRoute.Length);
            UpdateSquirrelCutoffMarkers();
            _squirrelTimer = 5.5f;
            AddScore(scoreEvent.Points, scoreEvent.Label);
            LastFeedback = FeedbackKind.SquirrelScared;
            LastCue = cutoff ? "Perfect cutoff! The squirrel route is collapsing." : "Good herd! The squirrel conspiracy is losing ground.";
            SetActorState(SquirrelObject, $"ROUTE {_herdingState.RouteIndex + 1} / CONTROLS {_herdingState.ControlCount}/4", new Color(0.85f, 0.55f, 0.12f), cutoff ? 0.28f : 0.16f);
            SetJuice(JuiceFeedbackKind.SuccessPop, scoreEvent.Label);
            SpawnWorldPop(SquirrelObject.transform.position, cutoff ? "CUTOFF!" : "HERD!", new Color(1f, 0.9f, 0.25f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            LogPlaytestEvent(cutoff ? "SquirrelCutoff" : "SquirrelHerd", $"controls {_herdingState.ControlCount}/4");

            if (_herdingState.ReadyForStash(4))
            {
                _herdingState.RevealStash();
                SetSquirrelCutoffMarkersActive(false);
                AddScore(ScoreEventCatalog.DoubleBarkBlock.Points, ScoreEventCatalog.DoubleBarkBlock.Label);
                PlaceObject(SquirrelObject, _stashPosition + Vector2.left * 1.2f);
                SetActorState(SquirrelObject, "STASH REVEALED - SNIFF + INTERACT!", new Color(1f, 0.72f, 0.18f), 0.34f);
                LastCue = "The squirrel stash is exposed! Get a dog to the stash and interact.";
                LogPlaytestEvent("SquirrelStashRevealed", LastCue);
            }

            LogObjectiveIfChanged();
            return true;
        }

        private bool IsPartnerHoldingSquirrelCutoff(int barkingDogIndex)
        {
            Vector2 zone = ActiveSquirrelCutoffZone;
            for (int i = 0; i < _dogs.Length; i++)
            {
                if (i == barkingDogIndex || _dogs[i] == null) continue;
                if (Vector2.Distance(_dogs[i].transform.position, zone) <= SquirrelCutoffRadius) return true;
            }
            return false;
        }

        private void RegisterSquirrelTaunt()
        {
            _herdingState.AddTaunt();
            AddScore(ScoreEventCatalog.FakeOut.Points, "SQUIRREL TAUNT");
            _herdingState.AdvanceRoute(_squirrelRoute.Length);
            UpdateSquirrelCutoffMarkers();
            _squirrelTimer = 5.5f;
            PlaceObject(SquirrelObject, _squirrelRoute[_herdingState.RouteIndex]);
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = $"The squirrel taunted the dogs ({_herdingState.Taunts}/3). Cut it off before yard gossip wins.";
            SetActorState(SquirrelObject, $"TAUNT {_herdingState.Taunts}/3 - CUT OFF!", Color.gray, 0.3f);
            SetJuice(JuiceFeedbackKind.WarningMiss, "SQUIRREL TAUNT!");
            RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            LogPlaytestEvent("SquirrelTaunt", LastCue);
            if (_herdingState.TooManyTaunts(3)) EndRound(false);
            else LogObjectiveIfChanged();
        }

        private void TryFindConspiracyStash(DogId dogId, bool force = false)
        {
            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;
            if (!_herdingState.StashRevealed)
            {
                MarkFailedInteraction(dogId, "stash is not revealed yet");
                return;
            }

            if (!force && Vector2.Distance(_dogs[dogIndex].transform.position, _stashPosition) > 2f)
            {
                MarkFailedInteraction(dogId, "too far from squirrel stash");
                return;
            }

            _herdingState.FindStash();
            AddScore(ScoreEventCatalog.StashFound.Points, ScoreEventCatalog.StashFound.Label);
            AddScore(ScoreEventCatalog.ConspiracyCracked.Points, ScoreEventCatalog.ConspiracyCracked.Label);
            LastCue = $"{DogName(_dogs[dogIndex])} found the stash. The conspiracy is cracked!";
            SetActorState(SquirrelObject, "CONSPIRACY CRACKED!", new Color(0.3f, 1f, 0.35f), 0.12f);
            SetJuice(JuiceFeedbackKind.SuccessPop, "STASH FOUND!");
            SpawnWorldPop(_stashPosition, "STASH FOUND!", new Color(0.5f, 1f, 0.45f));
            LogPlaytestEvent("SquirrelStashFound", LastCue);
            CheckClear();
        }

        private void BuildEagleCoverMarkers()
        {
            _eagleCoverMarkers = new GameObject[_eagleCoverZones.Length];
            for (int i = 0; i < _eagleCoverZones.Length; i++)
            {
                var go = new GameObject($"EagleCover_{i}");
                go.transform.position = _eagleCoverZones[i];
                go.transform.localScale = Vector3.one * (EagleCoverRadius * 1.4f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = new Color(0.3f, 0.7f, 0.4f, 0.45f);
                sr.sortingOrder = 1;
                AddWorldLabel(go, "HIDE HERE", Vector3.up * 0.9f, 14, Color.white);
                go.SetActive(false);
                _eagleCoverMarkers[i] = go;
            }
        }

        private void BuildBackyardTrapGapMarker()
        {
            _backyardTrapGapMarker = new GameObject("BackyardSquirrelTrapEscapeGap");
            _backyardTrapGapMarker.transform.position = _backyardTrapGapPosition;
            _backyardTrapGapMarker.transform.localScale = Vector3.one * (BackyardTrapGapRadius * 1.25f);
            var sr = _backyardTrapGapMarker.AddComponent<SpriteRenderer>();
            sr.sprite = _rangeSprite != null ? _rangeSprite : _sprite;
            sr.color = new Color(0.35f, 0.8f, 1f, 0.34f);
            sr.sortingOrder = 1;
            AddWorldLabel(_backyardTrapGapMarker, "ESCAPE GAP - HOLD HERE", Vector3.up * 0.38f, 14, Color.white);
            _backyardTrapGapMarker.SetActive(false);
        }

        private void UpdateBackyardTrapGapMarker()
        {
            if (_backyardTrapGapMarker == null) return;
            bool active = _mission != null && _mission.Variant == MissionVariant.BackyardRescue &&
                          MissionActive() && !_backyardTrapState.Complete;
            _backyardTrapGapMarker.transform.position = _backyardTrapGapPosition;
            _backyardTrapGapMarker.SetActive(active);
            var sr = _backyardTrapGapMarker.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = IsBackyardGapHeld()
                    ? new Color(0.35f, 1f, 0.5f, 0.48f)
                    : new Color(0.35f, 0.8f, 1f, 0.34f);
        }

        private void BuildSquirrelCutoffMarkers()
        {
            _squirrelCutoffMarkers = new GameObject[_squirrelCutoffZones.Length];
            for (int i = 0; i < _squirrelCutoffZones.Length; i++)
            {
                var go = new GameObject($"SquirrelCutoff_{i}");
                go.transform.position = _squirrelCutoffZones[i];
                go.transform.localScale = Vector3.one * (SquirrelCutoffRadius * 1.25f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _rangeSprite != null ? _rangeSprite : _sprite;
                sr.color = new Color(1f, 0.72f, 0.18f, 0.38f);
                sr.sortingOrder = 1;
                AddWorldLabel(go, "HOLD CUTOFF", Vector3.up * 0.38f, 14, Color.white);
                go.SetActive(false);
                _squirrelCutoffMarkers[i] = go;
            }
        }

        private void UpdateSquirrelCutoffMarkers()
        {
            if (_squirrelCutoffMarkers == null) return;
            for (int i = 0; i < _squirrelCutoffMarkers.Length; i++)
            {
                if (_squirrelCutoffMarkers[i] == null) continue;
                _squirrelCutoffMarkers[i].transform.position = _squirrelCutoffZones[i];
                _squirrelCutoffMarkers[i].SetActive(_mission != null &&
                    _mission.Variant == MissionVariant.SquirrelConspiracy &&
                    MissionActive() && !_herdingState.StashRevealed && i == _herdingState.RouteIndex);
            }
        }

        private void SetSquirrelCutoffMarkersActive(bool active)
        {
            if (!active)
            {
                if (_squirrelCutoffMarkers != null)
                    foreach (var marker in _squirrelCutoffMarkers)
                        if (marker != null) marker.SetActive(false);
                return;
            }
            UpdateSquirrelCutoffMarkers();
        }

        private void SetEagleCoverMarkersActive(bool active)
        {
            if (_eagleCoverMarkers == null) return;
            foreach (var marker in _eagleCoverMarkers)
                if (marker != null) marker.SetActive(active);
        }

        private void BuildCoyoteGapMarkers()
        {
            _coyoteGapMarkers = new GameObject[_fenceGaps.Length];
            for (int i = 0; i < _fenceGaps.Length; i++)
            {
                var go = new GameObject($"FenceGap_{i}");
                go.transform.position = _fenceGaps[i];
                go.transform.localScale = new Vector3(1.2f, 2.4f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = new Color(0.5f, 0.36f, 0.18f, 0.6f);
                sr.sortingOrder = 1;
                AddWorldLabel(go, "WEAK SPOT", Vector3.up * 1.4f, 13, Color.white);
                go.SetActive(false);
                _coyoteGapMarkers[i] = go;
            }
        }

        private void SetCoyoteGapMarkersActive(bool active)
        {
            if (_coyoteGapMarkers == null) return;
            foreach (var marker in _coyoteGapMarkers)
                if (marker != null) marker.SetActive(active);
        }

        // The coyote prowls toward the active weak spot. If the dogs are holding bark pressure when
        // it arrives, it is driven off; otherwise it breaches the gap.
        private void EvaluatePatrolReach()
        {
            if (_patrolState.FinalPressureComplete) return;

            if (_coyotePressureHeld)
            {
                _coyotePressureHeld = false;
                LastCue = "The coyote lunged at the weak spot but the bark pressure drove it back!";
                SetActorState(PredatorObject, "COYOTE DRIVEN BACK!", new Color(0.7f, 0.42f, 0.16f), 0.24f);
                SpawnWorldPop(PredatorObject.transform.position, "DRIVEN BACK!", new Color(1f, 0.85f, 0.3f));
                LogPlaytestEvent("CoyoteDrivenBack", LastCue);
                PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
                LogObjectiveIfChanged();
                return;
            }

            RegisterCoyoteBreach();
            PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
        }

        private void TickPatrolDefense()
        {
            if (PredatorObject == null || _patrolState.FinalPressureComplete) return;

            Vector2 target = _fenceGaps[_patrolState.ActiveGapIndex % _fenceGaps.Length];
            PredatorObject.transform.position = Vector3.MoveTowards(
                PredatorObject.transform.position, target, Time.deltaTime * (_tuning.SquirrelMoveSpeed * 0.7f));
            if (Vector2.Distance(PredatorObject.transform.position, target) < 0.5f)
                EvaluatePatrolReach();
        }

        // --- Weenie Roundup (carry to bowl) ---

        private void BuildWeenieRoundupObjects()
        {
            _weenieMarkers = new GameObject[_weenieSpots.Length];
            for (int i = 0; i < _weenieSpots.Length; i++)
            {
                var go = new GameObject($"LooseWeenie_{i}");
                go.transform.position = _weenieSpots[i];
                go.transform.localScale = new Vector3(1.1f, 0.5f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = new Color(0.78f, 0.34f, 0.24f);
                sr.sortingOrder = 4;
                AddWorldLabel(go, "WEENIE", Vector3.up * 1.2f, 13, Color.white);
                go.SetActive(false);
                _weenieMarkers[i] = go;
            }

            int dogCount = _dogs != null ? _dogs.Length : 2;
            _carriedMarkers = new GameObject[dogCount];
            _dogCarrying = new bool[dogCount];
            for (int i = 0; i < dogCount; i++)
            {
                var go = new GameObject($"CarriedWeenie_{i}");
                go.transform.localScale = new Vector3(0.9f, 0.4f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = new Color(0.85f, 0.4f, 0.28f);
                sr.sortingOrder = 11;
                go.SetActive(false);
                _carriedMarkers[i] = go;
            }

            _bowlObject = new GameObject("HomeBowl");
            _bowlObject.transform.localScale = new Vector3(3f, 2f, 1f);
            var bowlSr = _bowlObject.AddComponent<SpriteRenderer>();
            bowlSr.sprite = _sprite;
            bowlSr.color = new Color(0.85f, 0.85f, 0.9f);
            bowlSr.sortingOrder = 2;
            AddWorldLabel(_bowlObject, "HOME BOWL", Vector3.up * 1.4f, 16, Color.white);
            _bowlObject.SetActive(false);
        }

        private void SetWeenieRoundupActive(bool active)
        {
            if (_bowlObject != null) _bowlObject.SetActive(active);
            if (_weenieMarkers != null)
                for (int i = 0; i < _weenieMarkers.Length; i++)
                    if (_weenieMarkers[i] != null) _weenieMarkers[i].SetActive(active);
            if (_carriedMarkers != null)
                foreach (var marker in _carriedMarkers)
                    if (marker != null) marker.SetActive(false);
        }

        private int NearestActiveWeenie(Vector2 position)
        {
            int best = -1;
            float bestDist = float.PositiveInfinity;
            if (_weenieMarkers == null) return best;
            for (int i = 0; i < _weenieMarkers.Length; i++)
            {
                if (_weenieMarkers[i] == null || !_weenieMarkers[i].activeSelf) continue;
                float d = Vector2.Distance(position, _weenieMarkers[i].transform.position);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        private int FirstActiveWeenie()
        {
            if (_weenieMarkers == null) return -1;
            for (int i = 0; i < _weenieMarkers.Length; i++)
                if (_weenieMarkers[i] != null && _weenieMarkers[i].activeSelf) return i;
            return -1;
        }

        private void PickupWeenie(int dogIndex, int markerIndex)
        {
            if (dogIndex < 0 || dogIndex >= _dogCarrying.Length || _dogCarrying[dogIndex]) return;
            if (markerIndex < 0 || markerIndex >= _weenieMarkers.Length || !_weenieMarkers[markerIndex].activeSelf) return;
            if (!_carryState.TryPickup()) return;

            _weenieMarkers[markerIndex].SetActive(false);
            _dogCarrying[dogIndex] = true;
            if (dogIndex < DogFeedback.Length && DogFeedback[dogIndex] != null) DogFeedback[dogIndex].SetCarrying(true);
            CreditDog(dogIndex);
            if (_carriedMarkers[dogIndex] != null) _carriedMarkers[dogIndex].SetActive(true);
            AddScore(ScoreEventCatalog.WeeniePickup.Points, ScoreEventCatalog.WeeniePickup.Label);
            LastFeedback = FeedbackKind.PartnerRescue;
            LastCue = $"{DogName(_dogs[dogIndex])} grabbed a weenie - carry it to the bowl!";
            SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.WeeniePickup.Label);
            if (dogIndex < DogFeedback.Length && DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ShowProudBrief();
            SpawnWorldPop(_dogs[dogIndex].transform.position, "WEENIE GRABBED!", new Color(1f, 0.78f, 0.3f));
            RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            RequestRumble("weenie_pickup", 0.12f, 0.22f, 0.1f);
            LogPlaytestEvent("WeeniePickup", $"loose {_carryState.Loose}");
            LogObjectiveIfChanged();
        }

        private void DeliverWeenie(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _dogCarrying.Length || !_dogCarrying[dogIndex]) return;

            _dogCarrying[dogIndex] = false;
            if (dogIndex < DogFeedback.Length && DogFeedback[dogIndex] != null) DogFeedback[dogIndex].SetCarrying(false);
            if (_carriedMarkers[dogIndex] != null) _carriedMarkers[dogIndex].SetActive(false);
            _carryState.Deliver();
            CreditDog(dogIndex);
            if (dogIndex < DogFeedback.Length && DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ShowProudBrief();
            AddScore(ScoreEventCatalog.WeenieDelivered.Points, ScoreEventCatalog.WeenieDelivered.Label);
            LastFeedback = FeedbackKind.LevelClear;
            LastCue = $"{DogName(_dogs[dogIndex])} delivered a weenie to the bowl! ({_carryState.Delivered}/{WeenieRequiredDeliveries})";
            SetActorState(_bowlObject, $"HOME BOWL {_carryState.Delivered}/{WeenieRequiredDeliveries}", new Color(0.6f, 1f, 0.7f), 0.2f);
            SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.WeenieDelivered.Label);
            SpawnWorldPop(_bowlPosition, "DELIVERED!", new Color(0.55f, 1f, 0.45f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("weenie_delivered", 0.24f, 0.45f, 0.16f);
            LogPlaytestEvent("WeenieDelivered", $"{_carryState.Delivered}/{WeenieRequiredDeliveries}");

            if (_carryState.ReadyToClear(WeenieRequiredDeliveries))
            {
                AddScore(ScoreEventCatalog.RoundupComplete.Points, ScoreEventCatalog.RoundupComplete.Label);
                CheckClear();
            }
            else
            {
                LogObjectiveIfChanged();
            }
        }

        private void DropWeenie(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _dogCarrying.Length || !_dogCarrying[dogIndex]) return;

            _dogCarrying[dogIndex] = false;
            if (dogIndex < DogFeedback.Length && DogFeedback[dogIndex] != null) DogFeedback[dogIndex].SetCarrying(false);
            if (_carriedMarkers[dogIndex] != null) _carriedMarkers[dogIndex].SetActive(false);
            _carryState.Drop();
            if (dogIndex < DogFeedback.Length && DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ShowPanic();

            // The fumbled weenie bounces a couple units away (outside pickup range) so the dog has to
            // chase it down again rather than instantly re-grabbing it at its feet.
            Vector2 dropAt = (Vector2)_dogs[dogIndex].transform.position + new Vector2(WeeniePickupRange + 1f, 0f);
            dropAt.x = Mathf.Clamp(dropAt.x, _bounds.xMin + 1f, _bounds.xMax - 1f);
            for (int i = 0; i < _weenieMarkers.Length; i++)
            {
                if (_weenieMarkers[i] != null && !_weenieMarkers[i].activeSelf)
                {
                    _weenieMarkers[i].transform.position = dropAt;
                    _weenieMarkers[i].SetActive(true);
                    break;
                }
            }

            AddScore(ScoreEventCatalog.WeenieDropped.Points, ScoreEventCatalog.WeenieDropped.Label);
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = $"{DogName(_dogs[dogIndex])} fumbled the weenie - go grab it again!";
            SetJuice(JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.WeenieDropped.Label);
            SpawnWorldPop(dropAt, "FUMBLE!", new Color(1f, 0.4f, 0.22f));
            RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            RequestRumble("weenie_fumble", 0.18f, 0.34f, 0.14f);
            LogPlaytestEvent("WeenieDropped", $"drops {_carryState.Drops}");
            LogObjectiveIfChanged();
        }

        private void TickCarryRoundup()
        {
            if (_dogs == null || _dogCarrying == null) return;

            for (int i = 0; i < _dogs.Length; i++)
            {
                if (_dogCarrying[i])
                {
                    if (_carriedMarkers[i] != null)
                        _carriedMarkers[i].transform.position = _dogs[i].transform.position + Vector3.up * 0.6f;
                    if (Vector2.Distance(_dogs[i].transform.position, _bowlPosition) <= BowlDeliverRange)
                        DeliverWeenie(i);
                }
                else
                {
                    int marker = NearestActiveWeenie(_dogs[i].transform.position);
                    if (marker >= 0 && Vector2.Distance(_dogs[i].transform.position, _weenieMarkers[marker].transform.position) <= WeeniePickupRange)
                        PickupWeenie(i, marker);
                }
            }
        }

        public void ForceWeeniePickup(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.WeenieRoundup)
                PickupWeenie(IndexOfDog(dogId), FirstActiveWeenie());
        }

        public void ForceWeenieDeliver(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.WeenieRoundup)
                DeliverWeenie(IndexOfDog(dogId));
        }

        public void ForceWeenieDrop(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.WeenieRoundup)
                DropWeenie(IndexOfDog(dogId));
        }

        // --- Scent Search (sniff + dig) ---

        private void BuildScentSearchMarkers()
        {
            _digMarkers = new GameObject[_digSpots.Length];
            for (int i = 0; i < _digSpots.Length; i++)
            {
                var go = new GameObject($"DigSpot_{i}");
                go.transform.position = _digSpots[i];
                go.transform.localScale = new Vector3(1.6f, 1.0f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = new Color(0.42f, 0.3f, 0.16f);
                sr.sortingOrder = 3;
                AddWorldLabel(go, "DIG?", Vector3.up * 1.1f, 13, Color.white);
                go.SetActive(false);
                _digMarkers[i] = go;
            }
        }

        private void SetScentSearchActive(bool active)
        {
            if (_digMarkers == null) return;
            foreach (var marker in _digMarkers)
                if (marker != null) marker.SetActive(active);
        }

        private void BuildBoneRelayMarkers()
        {
            _boneMounds = new GameObject[_boneMoundSpots.Length];
            _boneMoundLabels = new TextMesh[_boneMoundSpots.Length];
            for (int i = 0; i < _boneMoundSpots.Length; i++)
            {
                var go = new GameObject($"BoneMound_{i}");
                go.transform.position = _boneMoundSpots[i];
                go.transform.localScale = new Vector3(1.8f, 1.1f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = BoneMoundIdleColor;
                sr.sortingOrder = 3;
                _boneMoundLabels[i] = AddWorldLabel(go, "DIG?", Vector3.up * 1.2f, 13, Color.white);
                go.SetActive(false);
                _boneMounds[i] = go;
            }
        }

        private void SetBoneRelayActive(bool active)
        {
            if (_boneMounds == null) return;
            foreach (var marker in _boneMounds)
                if (marker != null) marker.SetActive(active);
        }


        private void BuildChaosJunctions()
        {
            _chaosJunctions = new GameObject[_chaosJunctionSpots.Length];
            _chaosJunctionLabels = new TextMesh[_chaosJunctionSpots.Length];
            for (int i = 0; i < _chaosJunctionSpots.Length; i++)
            {
                var go = new GameObject($"ChaosJunction_{i}");
                go.transform.position = _chaosJunctionSpots[i];
                go.transform.localScale = new Vector3(1.7f, 1.3f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = new Color(0.3f, 0.3f, 0.34f);
                sr.sortingOrder = 3;
                _chaosJunctionLabels[i] = AddWorldLabel(go, $"{i + 1}.", Vector3.up * 1.3f, 12, Color.white);
                go.SetActive(false);
                _chaosJunctions[i] = go;
            }
        }

        private void SetChaosJunctionsActive(bool active)
        {
            if (_chaosJunctions == null) return;
            foreach (var marker in _chaosJunctions)
                if (marker != null) marker.SetActive(active);
        }

        private void BuildBlanketObjects()
        {
            _blanketObject = new GameObject("CatchBlanket");
            _blanketObject.transform.position = new Vector3(0f, BlanketCatchLineY, 0f);
            _blanketObject.transform.localScale = new Vector3(BlanketMinSeparation, 0.5f, 1f);
            var bsr = _blanketObject.AddComponent<SpriteRenderer>();
            bsr.sprite = _sprite;
            bsr.color = new Color(0.45f, 0.85f, 0.55f);
            bsr.sortingOrder = 3;
            _blanketLabel = AddWorldLabel(_blanketObject, "BLANKET", Vector3.up * 1.6f, 12, Color.white);
            _blanketObject.SetActive(false);

            _blanketFallingItem = new GameObject("FallingSnack");
            _blanketFallingItem.transform.position = new Vector3(0f, BlanketSpawnY, 0f);
            _blanketFallingItem.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var isr = _blanketFallingItem.AddComponent<SpriteRenderer>();
            isr.sprite = _sprite;
            isr.color = new Color(0.95f, 0.8f, 0.4f);
            isr.sortingOrder = 4;
            AddWorldLabel(_blanketFallingItem, "SNACK", Vector3.up * 1.1f, 11, Color.white);
            _blanketFallingItem.SetActive(false);
        }

        private void ChooseBuriedSpot()
        {
            // Pick a random still-active dig spot to bury the next bone under.
            var active = new System.Collections.Generic.List<int>();
            for (int i = 0; i < _digMarkers.Length; i++)
                if (_digMarkers[i] != null && _digMarkers[i].activeSelf) active.Add(i);
            _buriedSpot = active.Count == 0 ? -1 : active[_rng.Next(active.Count)];
        }

        private int NearestActiveDigSpot(Vector2 position)
        {
            int best = -1;
            float bestDist = float.PositiveInfinity;
            for (int i = 0; i < _digMarkers.Length; i++)
            {
                if (_digMarkers[i] == null || !_digMarkers[i].activeSelf) continue;
                float d = Vector2.Distance(position, _digMarkers[i].transform.position);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        private void DoScentSniff(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _dogs.Length || _buriedSpot < 0) return;

            _scentState.AddSniff();
            float dist = Vector2.Distance(_dogs[dogIndex].transform.position, _digSpots[_buriedSpot]);
            string heat = dist < 5f ? "RED HOT" : dist < 11f ? "WARM" : "COLD";
            if (dist < 5f) AddScore(ScoreEventCatalog.ScentSniff.Points, ScoreEventCatalog.ScentSniff.Label);
            LastFeedback = FeedbackKind.SquirrelScared;
            LastCue = $"{DogName(_dogs[dogIndex])} sniffs... the bone scent is {heat}!";
            SetJuice(dist < 5f ? JuiceFeedbackKind.SuccessPop : JuiceFeedbackKind.BarkBurst, $"SCENT: {heat}");
            SpawnWorldPop(_dogs[dogIndex].transform.position + Vector3.up, heat, dist < 5f ? new Color(1f, 0.45f, 0.2f) : new Color(0.6f, 0.75f, 1f));
            RequestAudioCue(ArenaFeedbackCatalog.Bark);
            LogPlaytestEvent("ScentSniff", heat);
            LogObjectiveIfChanged();
        }

        private void DigAtSpot(int dogIndex, int spotIndex, bool force)
        {
            if (dogIndex < 0 || dogIndex >= _dogs.Length) return;
            if (spotIndex < 0 || spotIndex >= _digMarkers.Length || !_digMarkers[spotIndex].activeSelf)
            {
                MarkFailedInteraction(_dogs[dogIndex].GetComponent<DogIdentity>().Id, "nothing to dig here");
                return;
            }
            if (!force && Vector2.Distance(_dogs[dogIndex].transform.position, _digSpots[spotIndex]) > DigRange)
            {
                MarkFailedInteraction(_dogs[dogIndex].GetComponent<DogIdentity>().Id, "too far from the dig spot");
                return;
            }

            if (spotIndex == _buriedSpot)
            {
                _scentState.AddFind();
                _digMarkers[spotIndex].SetActive(false);
                CreditDog(dogIndex);
                if (dogIndex < DogFeedback.Length && DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ShowProudBrief();
                AddScore(ScoreEventCatalog.BoneFound.Points, ScoreEventCatalog.BoneFound.Label);
                LastFeedback = FeedbackKind.PartnerRescue;
                LastCue = $"{DogName(_dogs[dogIndex])} dug up a buried bone! ({_scentState.Found}/{ScentRequiredFinds})";
                SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.BoneFound.Label);
                SpawnWorldPop(_digSpots[spotIndex], "BONE!", new Color(0.95f, 0.9f, 0.7f));
                RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                RequestRumble("bone_found", 0.26f, 0.5f, 0.16f);
                LogPlaytestEvent("BoneFound", $"{_scentState.Found}/{ScentRequiredFinds}");
                if (_scentState.ReadyToClear(ScentRequiredFinds))
                {
                    AddScore(ScoreEventCatalog.ScentSearchComplete.Points, ScoreEventCatalog.ScentSearchComplete.Label);
                    CheckClear();
                    return;
                }
                ChooseBuriedSpot();
            }
            else
            {
                _scentState.AddWastedDig();
                AddScore(ScoreEventCatalog.ColdDig.Points, ScoreEventCatalog.ColdDig.Label);
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                LastCue = $"{DogName(_dogs[dogIndex])} dug a cold hole - nothing here ({_scentState.WastedDigs}/{ScentMaxWastedDigs}).";
                SetJuice(JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.ColdDig.Label);
                if (dogIndex < DogFeedback.Length && DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ShowPanic();
                SpawnWorldPop(_digSpots[spotIndex], "COLD!", new Color(0.6f, 0.75f, 1f));
                RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                RequestRumble("cold_dig", 0.14f, 0.28f, 0.12f);
                LogPlaytestEvent("ColdDig", $"{_scentState.WastedDigs}/{ScentMaxWastedDigs}");
                if (_scentState.TooManyWastedDigs(ScentMaxWastedDigs)) { EndRound(false); return; }
            }
            LogObjectiveIfChanged();
        }

        public void ForceScentSniff(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.ScentSearch)
                DoScentSniff(IndexOfDog(dogId));
        }

        public void ForceScentDigCorrect(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.ScentSearch)
                DigAtSpot(IndexOfDog(dogId), _buriedSpot, true);
        }

        public void ForceScentDigWrong(DogId dogId = DogId.Cheddar)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.ScentSearch) return;
            for (int i = 0; i < _digMarkers.Length; i++)
                if (_digMarkers[i] != null && _digMarkers[i].activeSelf && i != _buriedSpot)
                {
                    DigAtSpot(IndexOfDog(dogId), i, true);
                    return;
                }
        }

        // --- Thunderstorm Comfort (panic co-regulation) ---

        private void TickThunderstorm()
        {
            if (_panic == null || _dogs == null || _dogs.Length < 2) return;

            _panic.Step(_dogs[0].transform.position, _dogs[1].transform.position, Time.deltaTime);
            if (Vector2.Distance(_dogs[0].transform.position, _dogs[1].transform.position) <= _panic.CuddleRadius)
                for (int i = 0; i < DogFeedback.Length; i++)
                    if (DogFeedback[i] != null) DogFeedback[i].ShowComfort();
            if (_panic.Maxed != null) { EndRound(false); return; }

            if (Time.time >= _nextThunderAt)
            {
                _nextThunderAt = Time.time + StormClapInterval;
                ThunderClap();
            }
        }

        private void ThunderClap()
        {
            if (_panic == null) return;

            // Cheddar (chaos puppy) spooks harder than Cocoa (veteran queen).
            _panic.AddSpike(DogId.Cheddar, 0.26f);
            _panic.AddSpike(DogId.Cocoa, 0.16f);
            for (int i = 0; i < DogFeedback.Length; i++)
                if (DogFeedback[i] != null) DogFeedback[i].ShowPanic(); // both pups flinch at the clap
            RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            RequestRumble("thunderclap", 0.3f, 0.55f, 0.2f);
            SpawnWorldPop(new Vector2(0f, _bounds.yMax - 2f), "BOOM!", new Color(0.8f, 0.85f, 1f));
            if (PredatorObject != null) SetActorState(PredatorObject, "THUNDERCLAP - HUDDLE!", new Color(0.3f, 0.32f, 0.42f), 0.4f);

            if (_panic.Maxed != null) { EndRound(false); return; }

            _stormState.SurviveClap();
            AddScore(ScoreEventCatalog.StormWeathered.Points, ScoreEventCatalog.StormWeathered.Label);
            LastFeedback = FeedbackKind.PredatorHuddle;
            LastCue = $"Thunderclap weathered! ({_stormState.ClapsSurvived}/{_stormState.RequiredClaps}) Keep huddling.";
            SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.StormWeathered.Label);
            LogPlaytestEvent("Thunderclap", $"{_stormState.ClapsSurvived}/{_stormState.RequiredClaps}");
            CheckClear();
        }

        public void ForceThunderclap()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.ThunderstormComfort)
                ThunderClap();
        }

        public void ForceComfortStep(float seconds)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.ThunderstormComfort) return;
            if (_panic == null || _dogs == null || _dogs.Length < 2) return;

            float before = Mathf.Max(_panic.CheddarPanic, _panic.CocoaPanic);
            _panic.Step(_dogs[0].transform.position, _dogs[1].transform.position, seconds);
            float after = Mathf.Max(_panic.CheddarPanic, _panic.CocoaPanic);
            if (after < before) AddScore(ScoreEventCatalog.StormComfort.Points, ScoreEventCatalog.StormComfort.Label);
            if (_panic.Maxed != null) EndRound(false);
        }

        // --- Mark the Yard now lives in MarkTheYardMissionController; the hooks below forward to it. ---

        /// <summary>Compatibility hook forwarded to the active Mark the Yard controller.</summary>
        public void ForceClaimZone(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) MarkTheYardController?.ForceClaimZone(dogId);
            CheckClear();
        }

        /// <summary>Compatibility hook forwarded to the active Mark the Yard controller.</summary>
        public void ForceSquirrelReclaim()
        {
            if (MissionActive()) MarkTheYardController?.ForceSquirrelReclaim();
        }

        // --- Walkies on the Leash (tethered checkpoint walk) ---

        private void BuildLeashCheckpointMarkers()
        {
            _leashCheckpointMarkers = new GameObject[_leashCheckpoints.Length];
            for (int i = 0; i < _leashCheckpoints.Length; i++)
            {
                var go = new GameObject($"LeashCheckpoint_{i}");
                go.transform.position = _leashCheckpoints[i];
                go.transform.localScale = Vector3.one * (CheckpointRange * 1.4f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _sprite;
                sr.color = new Color(0.45f, 0.6f, 0.85f, 0.4f);
                sr.sortingOrder = 1;
                AddWorldLabel(go, "CHECKPOINT", Vector3.up * 0.9f, 13, Color.white);
                go.SetActive(false);
                _leashCheckpointMarkers[i] = go;
            }
        }

        private void SetLeashCheckpointMarkersActive(bool active)
        {
            if (_leashCheckpointMarkers == null) return;
            for (int i = 0; i < _leashCheckpointMarkers.Length; i++)
                if (_leashCheckpointMarkers[i] != null)
                    _leashCheckpointMarkers[i].SetActive(active && i >= _leashState.CheckpointIndex);
        }

        private void RegisterCheckpointReached()
        {
            if (_leashState.ReadyToClear()) return;
            int idx = _leashState.CheckpointIndex;
            _leashState.ReachCheckpoint();
            if (idx >= 0 && idx < _leashCheckpointMarkers.Length && _leashCheckpointMarkers[idx] != null)
                _leashCheckpointMarkers[idx].SetActive(false);
            AddScore(ScoreEventCatalog.CheckpointReached.Points, ScoreEventCatalog.CheckpointReached.Label);
            LastFeedback = FeedbackKind.UnitedBark;
            LastCue = $"Checkpoint reached together! ({_leashState.Reached}/{_leashState.RequiredCheckpoints}) Stay close.";
            SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.CheckpointReached.Label);
            SpawnWorldPop(_leashCheckpoints[idx], "CHECKPOINT!", new Color(0.6f, 0.8f, 1f));
            if (DogFeedback != null)
                foreach (var feedback in DogFeedback)
                    if (feedback != null) feedback.ShowProudBrief();
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("checkpoint", 0.2f, 0.4f, 0.14f);
            LogPlaytestEvent("Checkpoint", $"{_leashState.Reached}/{_leashState.RequiredCheckpoints}");
            if (_leashState.ReadyToClear()) { AddScore(ScoreEventCatalog.WalkComplete.Points, ScoreEventCatalog.WalkComplete.Label); CheckClear(); }
            else LogObjectiveIfChanged();
        }

        private void RegisterLeashSnap()
        {
            _leashState.Snap();
            AddScore(ScoreEventCatalog.LeashSnap.Points, ScoreEventCatalog.LeashSnap.Label);
            LastFeedback = FeedbackKind.TugNeedsPartner;
            LastCue = $"The leash snapped taut - too far apart! ({_leashState.Snaps}/{MaxLeashSnaps})";
            SetJuice(JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.LeashSnap.Label);
            Vector3 midpoint = _dogs != null && _dogs.Length >= 2
                ? (_dogs[0].transform.position + _dogs[1].transform.position) * 0.5f
                : Vector3.zero;
            SpawnWorldPop(midpoint, "LEASH SNAP!", new Color(1f, 0.42f, 0.24f));
            if (DogFeedback != null)
                foreach (var feedback in DogFeedback)
                    if (feedback != null) feedback.ShowPanic();
            RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            RequestRumble("leash_snap", 0.18f, 0.4f, 0.14f);
            LogPlaytestEvent("LeashSnap", $"{_leashState.Snaps}/{MaxLeashSnaps}");
            if (_leashState.TooManySnaps(MaxLeashSnaps)) EndRound(false);
            else LogObjectiveIfChanged();
        }

        private void TickLeashWalk()
        {
            if (_dogs == null || _dogs.Length < 2 || _leashState.ReadyToClear()) return;

            // Leash snaps if the dogs drift too far apart (rate-limited so one stretch isn't a barrage).
            if (Vector2.Distance(_dogs[0].transform.position, _dogs[1].transform.position) > MaxLeash && Time.time >= _nextLeashSnapAt)
            {
                _nextLeashSnapAt = Time.time + 1.5f;
                RegisterLeashSnap();
                if (_leashState.TooManySnaps(MaxLeashSnaps)) return;
            }

            // Both dogs must be on the current checkpoint together to bank it.
            int idx = _leashState.CheckpointIndex;
            if (idx >= 0 && idx < _leashCheckpoints.Length)
            {
                bool a = Vector2.Distance(_dogs[0].transform.position, _leashCheckpoints[idx]) <= CheckpointRange;
                bool b = Vector2.Distance(_dogs[1].transform.position, _leashCheckpoints[idx]) <= CheckpointRange;
                if (a && b) RegisterCheckpointReached();
            }
        }

        public void ForceReachCheckpoint()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.LeashWalk)
                RegisterCheckpointReached();
        }

        public void ForceLeashSnap()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.LeashWalk)
                RegisterLeashSnap();
        }

        // --- Car Ride Balance (vehicle lean) ---

        private void ApplyCarLurch()
        {
            if (_carState.ReadyToClear()) return;

            _carBalance += _carLurchDir * 0.4f;
            _carLurchDir = -_carLurchDir;
            if (Mathf.Abs(_carBalance) >= 1f) { RegisterCarSpill(); return; }

            _carState.SurviveLurch();
            AddScore(ScoreEventCatalog.LurchSteadied.Points, ScoreEventCatalog.LurchSteadied.Label);
            LastFeedback = FeedbackKind.UnitedBark;
            LastCue = $"Steadied the lurch! ({_carState.LurchesSurvived}/{_carState.RequiredLurches}) Lean to balance.";
            SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.LurchSteadied.Label);
            Vector3 midpoint = _dogs != null && _dogs.Length >= 2
                ? (_dogs[0].transform.position + _dogs[1].transform.position) * 0.5f
                : (Vector3)_bounds.center;
            SpawnWorldPop(midpoint, "STEADIED!", new Color(0.55f, 1f, 0.7f));
            if (DogFeedback != null)
                foreach (var feedback in DogFeedback)
                    if (feedback != null) feedback.ShowProudBrief();
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("car_lurch", 0.18f, 0.36f, 0.12f);
            LogPlaytestEvent("CarSteadied", $"{_carState.LurchesSurvived}/{_carState.RequiredLurches}");
            if (_carState.ReadyToClear()) { AddScore(ScoreEventCatalog.RideComplete.Points, ScoreEventCatalog.RideComplete.Label); CheckClear(); }
            else LogObjectiveIfChanged();
        }

        private void RegisterCarSpill()
        {
            _carState.Spill();
            _carBalance = 0f;
            AddScore(ScoreEventCatalog.CarSpill.Points, ScoreEventCatalog.CarSpill.Label);
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = $"The car tipped and everyone spilled! ({_carState.Spills}/{CarMaxSpills}) Lean the other way next time.";
            SetJuice(JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.CarSpill.Label);
            Vector3 midpoint = _dogs != null && _dogs.Length >= 2
                ? (_dogs[0].transform.position + _dogs[1].transform.position) * 0.5f
                : (Vector3)_bounds.center;
            SpawnWorldPop(midpoint, "CAR SPILL!", new Color(1f, 0.38f, 0.22f));
            if (DogFeedback != null)
                foreach (var feedback in DogFeedback)
                    if (feedback != null) feedback.ShowPanic();
            RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            RequestRumble("car_spill", 0.22f, 0.45f, 0.16f);
            LogPlaytestEvent("CarSpill", $"{_carState.Spills}/{CarMaxSpills}");
            if (_carState.TooManySpills(CarMaxSpills)) EndRound(false);
            else LogObjectiveIfChanged();
        }

        private void TickCarRide()
        {
            if (_carState.ReadyToClear()) return;

            // The dogs' average side (x) leans the car; to stay level they must sit opposite the tilt.
            float avgX = 0f;
            if (_dogs != null && _dogs.Length > 0)
            {
                foreach (var d in _dogs) avgX += d.transform.position.x;
                avgX /= _dogs.Length;
            }
            float lean = Mathf.Clamp(avgX / 8f, -1f, 1f);
            _carBalance = Mathf.Clamp(_carBalance + (lean * 0.6f + _carLurchDir * 0.04f) * Time.deltaTime, -1.4f, 1.4f);
            if (PredatorObject != null)
                SetActorState(PredatorObject, $"CAR TILT {(_carBalance >= 0 ? "RIGHT" : "LEFT")} {Mathf.RoundToInt(Mathf.Abs(_carBalance) * 100f)}% - LEAN!", new Color(0.5f, 0.4f, 0.3f), 0.12f);

            if (Mathf.Abs(_carBalance) >= 1f) { RegisterCarSpill(); return; }
            if (Time.time >= _nextLurchAt) { _nextLurchAt = Time.time + CarLurchInterval; ApplyCarLurch(); }
        }

        public void ForceCarLurch()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.CarRide)
                ApplyCarLurch();
        }

        public void ForceCarSpill()
        {
            if (MissionActive() && _mission != null && _mission.Variant == MissionVariant.CarRide)
                RegisterCarSpill();
        }

        // --- Gate Crash (Hold-and-Release co-op puzzle) ---

        /// <summary>Compatibility hook forwarded to the active Gate Crash controller.</summary>
        public void ForceGateHold(bool held = true)
        {
            if (MissionActive()) GateCrashController?.ForceGateHold(held);
            CheckClear();
        }

        /// <summary>Compatibility hook forwarded to the active Gate Crash controller.</summary>
        public void ForceGateCross(float seconds)
        {
            if (MissionActive()) GateCrashController?.ForceGateCross(seconds);
            CheckClear();
        }

        /// <summary>Compatibility hook forwarded to the active Table Stealth controller.</summary>
        public void ForceTableFlop(bool flopped = true)
        {
            if (MissionActive()) TableStealthController?.ForceTableFlop(flopped);
        }

        /// <summary>Compatibility hook forwarded to the active Table Stealth controller.</summary>
        public void ForceTableBurp()
        {
            if (MissionActive()) TableStealthController?.ForceTableBurp();
        }

        /// <summary>Compatibility hook forwarded to the active Table Stealth controller.</summary>
        public void ForceTableSneak(float seconds)
        {
            if (MissionActive()) TableStealthController?.ForceTableSneak(seconds);
            CheckClear();
        }

        /// <summary>Compatibility hook forwarded to the active Switcheroo controller.</summary>
        public void ForceSwitcherooBait(float seconds, bool baiting = true)
        {
            if (MissionActive()) SquirrelSwitcherooController?.ForceSwitcherooBait(seconds, baiting);
            CheckClear();
        }

        /// <summary>Compatibility hook forwarded to the active Switcheroo controller.</summary>
        public void ForceSwitcherooStrike()
        {
            if (MissionActive()) SquirrelSwitcherooController?.ForceSwitcherooStrike();
            CheckClear();
        }

        /// <summary>Compatibility hook forwarded to the active Walk Campaign controller.</summary>
        public void ForceWalkCampaign(float seconds, bool doorStare, bool presentLeash)
        {
            if (MissionActive()) WalkCampaignController?.ForceWalkCampaign(seconds, doorStare, presentLeash);
            CheckClear();
        }

        private void TickBoneRelay()
        {
            if (_boneRelay.Solved || _boneMounds == null) return;

            int reader = IndexOfDog(DogId.Cocoa);
            int digger = IndexOfDog(DogId.Cheddar);
            if (reader < 0 || digger < 0) return;

            // Cocoa reads the scent post: while she's nosing it she can call which mound holds the bone.
            if (Vector2.Distance(_dogs[reader].transform.position, _boneScentZone) <= BoneScentRange)
                _boneRelay.Reveal();

            // Cheddar digs a mound on entry (one dig per approach, so he must leave and re-enter to dig again).
            int inside = -1;
            for (int i = 0; i < _boneMounds.Length; i++)
            {
                if (_boneMounds[i] == null || !_boneMounds[i].activeSelf) continue;
                if (Vector2.Distance(_dogs[digger].transform.position, _boneMounds[i].transform.position) <= BoneDigRange)
                { inside = i; break; }
            }
            if (inside >= 0 && inside != _boneDiggerInside) _boneRelay.ActOn(inside);
            _boneDiggerInside = inside;

            HandleBoneRelayProgress();
            if (Phase == State.GameOver) return;
            UpdateBoneMoundVisuals();
        }

        private void HandleBoneRelayProgress()
        {
            int digger = IndexOfDog(DogId.Cheddar);
            Vector2 digPos = digger >= 0 ? (Vector2)_dogs[digger].transform.position : _boneScentZone;

            if (_boneRelay.Finds > _boneFindsSeen)
            {
                _boneFindsSeen = _boneRelay.Finds;
                AddScore(ScoreEventCatalog.BoneFound.Points, ScoreEventCatalog.BoneFound.Label);
                LastFeedback = FeedbackKind.SquirrelScared;
                LastCue = $"Cocoa called it, Cheddar dug it up! ({_boneRelay.Finds}/{BoneFindsNeeded})";
                SetJuice(JuiceFeedbackKind.SuccessPop, "BONE!");
                SpawnWorldPop(digPos, "BONE!", new Color(0.5f, 0.9f, 0.55f));
                LogPlaytestEvent("BoneFound", $"{_boneRelay.Finds}/{BoneFindsNeeded}");
            }

            bool wasted = false;
            if (_boneRelay.BlindActs > _boneBlindSeen)
            {
                _boneBlindSeen = _boneRelay.BlindActs;
                wasted = true;
                LastCue = "Cheddar dug blind - wait for Cocoa's call!";
            }
            if (_boneRelay.WrongDigs > _boneWrongSeen)
            {
                _boneWrongSeen = _boneRelay.WrongDigs;
                wasted = true;
                LastCue = "Wrong mound - that one's a decoy.";
            }
            if (wasted)
            {
                AddScore(ScoreEventCatalog.ColdDig.Points, ScoreEventCatalog.ColdDig.Label);
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                SetJuice(JuiceFeedbackKind.WarningMiss, "NOPE!");
                SpawnWorldPop(digPos, "NOPE!", new Color(0.85f, 0.5f, 0.3f));
                int totalWasted = _boneRelay.BlindActs + _boneRelay.WrongDigs;
                LogPlaytestEvent("BoneWaste", $"{totalWasted}/{BoneMaxWasted}");
                if (totalWasted >= BoneMaxWasted) EndRound(false);
            }
        }

        private void UpdateBoneMoundVisuals()
        {
            if (_boneMounds == null) return;
            int call = _boneRelay.RevealedTarget;
            for (int i = 0; i < _boneMounds.Length; i++)
            {
                if (_boneMounds[i] == null) continue;
                bool isCall = i == call;
                if (_boneMounds[i].TryGetComponent<SpriteRenderer>(out var sr))
                    sr.color = isCall ? BoneMoundCallColor : BoneMoundIdleColor;
                if (_boneMoundLabels != null && _boneMoundLabels[i] != null)
                    _boneMoundLabels[i].text = isCall ? "DIG HERE!" : "DIG?";
            }
            if (SquirrelObject != null)
                SetActorState(SquirrelObject, _boneRelay.Known ? "SCENT POST - SHE'S CALLING IT!" : "SCENT POST - COCOA SNIFF HERE",
                    _boneRelay.Known ? BoneMoundCallColor : new Color(0.7f, 0.6f, 0.95f), 0.12f);
        }

        /// <summary>Test hook: Cocoa noses the scent post and calls the real mound.</summary>
        public void ForceBoneReveal()
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.BoneRelay) return;
            _boneRelay.Reveal();
            UpdateBoneMoundVisuals();
        }

        /// <summary>Test hook: Cheddar digs mound <paramref name="target"/>; finds only the called mound.</summary>
        public void ForceBoneDig(int target)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.BoneRelay) return;
            _boneRelay.ActOn(target);
            HandleBoneRelayProgress();
            if (Phase != State.GameOver) { UpdateBoneMoundVisuals(); CheckClear(); }
        }

        public void ForceEscapeStep(ChainActor actor)
        {
            if (MissionActive()) GreatEscapeController?.ForceEscapeStep(actor);
            CheckClear();
        }

        public void ForceEscapeIdle(float seconds)
        {
            if (MissionActive()) GreatEscapeController?.ForceEscapeIdle(seconds);
            CheckClear();
        }

        private void TickChaosMachine()
        {
            if (_chaos.Solved || _chaosJunctions == null) return;

            int cheddar = IndexOfDog(DogId.Cheddar);
            int cocoa = IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            if (!_chaos.Running)
            {
                // Either dog at the lever (re)pulls it - starting fresh or resuming from a stall.
                bool atLever = Vector2.Distance(_dogs[cheddar].transform.position, _chaosLeverZone) <= ChaosLeverRange
                    || Vector2.Distance(_dogs[cocoa].transform.position, _chaosLeverZone) <= ChaosLeverRange;
                if (atLever) _chaos.Trigger();
            }

            if (_chaos.Running)
            {
                int stage = Mathf.Clamp(_chaos.Stage, 0, _chaosJunctionSpots.Length - 1);
                ChainActor owner = _chaosOwners[stage];
                int ownerIdx = owner == ChainActor.Cheddar ? cheddar : cocoa;
                bool assisting = Vector2.Distance(_dogs[ownerIdx].transform.position, _chaosJunctionSpots[stage]) <= ChaosJunctionRange;
                _chaos.Advance(Time.deltaTime, assisting);
            }

            HandleChaosProgress();
            if (Phase == State.GameOver) return;
            UpdateChaosVisuals();
        }

        private void HandleChaosProgress()
        {
            if (_chaos.Stage > _chaosStageSeen)
            {
                _chaosStageSeen = _chaos.Stage;
                AddScore(ScoreEventCatalog.ContraptionStep.Points, "CASCADE ROLLED");
                LastFeedback = FeedbackKind.SquirrelScared;
                LastCue = $"Whirr-clunk! The cascade rolled through a junction. ({_chaos.Stage}/{_chaos.StageCount})";
                SetJuice(JuiceFeedbackKind.SuccessPop, "WHIRR!");
                int doneStage = Mathf.Clamp(_chaos.Stage - 1, 0, _chaosJunctionSpots.Length - 1);
                SpawnWorldPop(_chaosJunctionSpots[doneStage], "WHIRR!", new Color(0.6f, 0.85f, 0.95f));
                LogPlaytestEvent("ChaosStage", $"{_chaos.Stage}/{_chaos.StageCount}");
            }

            if (_chaos.Stalls > _chaosStallsSeen)
            {
                _chaosStallsSeen = _chaos.Stalls;
                AddScore(ScoreEventCatalog.ContraptionFumble.Points, "MISFIRE");
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                int jam = Mathf.Clamp(_chaos.StalledStage, 0, _chaosJunctionSpots.Length - 1);
                LastCue = $"Misfire! The machine jammed at the {_chaosJunctionActions[jam].ToLowerInvariant()} - re-pull the lever. ({_chaos.Stalls}/{ChaosMaxStalls})";
                SetJuice(JuiceFeedbackKind.WarningMiss, "MISFIRE!");
                SpawnWorldPop(_chaosJunctionSpots[jam], "STUCK!", new Color(1f, 0.4f, 0.25f));
                LogPlaytestEvent("ChaosStall", $"{_chaos.Stalls}/{ChaosMaxStalls}");
                if (_chaos.Stalls >= ChaosMaxStalls) EndRound(false);
            }
        }

        private void UpdateChaosVisuals()
        {
            if (_chaosJunctions == null) return;
            int stage = Mathf.Clamp(_chaos.Stage, 0, _chaosJunctions.Length - 1);
            for (int i = 0; i < _chaosJunctions.Length; i++)
            {
                if (_chaosJunctions[i] == null) continue;
                bool fired = i < _chaos.Stage;
                bool active = i == stage && !_chaos.Solved;
                bool stalledHere = _chaos.StalledStage == i;
                ChainActor owner = _chaosOwners[i];
                Color ownerTint = owner == ChainActor.Cheddar ? new Color(0.95f, 0.72f, 0.3f) : new Color(0.55f, 0.78f, 1f);
                Color shown = fired ? new Color(0.3f, 0.55f, 0.32f)
                    : stalledHere ? new Color(1f, 0.4f, 0.25f)
                    : active && _chaos.Running ? ownerTint
                    : new Color(0.3f, 0.3f, 0.34f);
                if (_chaosJunctions[i].TryGetComponent<SpriteRenderer>(out var sr)) sr.color = shown;
                if (_chaosJunctionLabels != null && _chaosJunctionLabels[i] != null)
                {
                    string who = owner == ChainActor.Cheddar ? "CHEDDAR" : "COCOA";
                    _chaosJunctionLabels[i].text = fired ? "FIRED" : $"{who}: {_chaosJunctionActions[i]}";
                }
            }
            if (PredatorObject != null)
                SetActorState(PredatorObject, _chaos.Running ? "CASCADE RUNNING - COVER YOUR JUNCTIONS!" : "LEVER - PULL TO START THE CASCADE",
                    _chaos.Running ? new Color(0.5f, 0.85f, 0.55f) : new Color(0.85f, 0.55f, 0.3f), 0.14f);
        }

        /// <summary>Test hook: a dog pulls the lever to start (or resume) the cascade.</summary>
        public void ForceChaosTrigger()
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.ChaosMachine) return;
            _chaos.Trigger();
            UpdateChaosVisuals();
        }

        /// <summary>Test hook: advance the live cascade, with the current junction's helper in position or not.</summary>
        public void ForceChaosAdvance(float seconds, bool assisting)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.ChaosMachine) return;
            _chaos.Advance(seconds, assisting);
            HandleChaosProgress();
            if (Phase != State.GameOver) { UpdateChaosVisuals(); CheckClear(); }
        }

        private void TickBlanketCatch()
        {
            if (_blanket.Solved) return;

            int a = IndexOfDog(DogId.Cheddar);
            int b = IndexOfDog(DogId.Cocoa);
            if (a < 0 || b < 0) return;

            // The blanket is defined by BOTH dogs: separation (taut band) and midpoint (catch alignment).
            Vector2 pa = _dogs[a].transform.position;
            Vector2 pb = _dogs[b].transform.position;
            _blanket.UpdateSpan(Vector2.Distance(pa, pb), (pa.x + pb.x) * 0.5f);
            HandleBlanketRips();

            // The current snack falls; when it reaches the blanket line, the span tries to catch it.
            _blanketItemY -= BlanketFallSpeed * Time.deltaTime;
            if (_blanketItemY <= BlanketCatchLineY)
            {
                _blanket.TryCatch(_blanketItemX);
                HandleBlanketProgress();
                if (Phase == State.GameOver) return;
                if (!_blanket.Solved) SpawnBlanketItem();
            }
            UpdateBlanketVisuals();
        }

        private void SpawnBlanketItem()
        {
            _blanketItemX = Mathf.Lerp(_bounds.xMin + 3f, _bounds.xMax - 3f, (float)_rng.NextDouble());
            _blanketItemY = BlanketSpawnY;
            if (_blanketFallingItem != null)
            {
                _blanketFallingItem.SetActive(true);
                _blanketFallingItem.transform.position = new Vector3(_blanketItemX, _blanketItemY, 0f);
            }
        }

        private void HandleBlanketRips()
        {
            if (_blanket.Rips > _blanketRipsSeen)
            {
                _blanketRipsSeen = _blanket.Rips;
                AddScore(ScoreEventCatalog.WeenieDropped.Points, "BLANKET RIP");
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                LastCue = $"Over-stretched! The blanket ripped. ({_blanket.Rips}/{BlanketMaxRips})";
                SetJuice(JuiceFeedbackKind.WarningMiss, "RIP!");
                LogPlaytestEvent("BlanketRip", $"{_blanket.Rips}/{BlanketMaxRips}");
                if (_blanket.TooManyRips) EndRound(false);
            }
        }

        private void HandleBlanketProgress()
        {
            if (_blanket.Caught > _blanketCaughtSeen)
            {
                _blanketCaughtSeen = _blanket.Caught;
                AddScore(ScoreEventCatalog.WeenieDelivered.Points, "SNACK CAUGHT");
                LastFeedback = FeedbackKind.SquirrelScared;
                LastCue = $"Nice catch! The blanket snagged the snack. ({_blanket.Caught}/{BlanketCatchesNeeded})";
                SetJuice(JuiceFeedbackKind.SuccessPop, "CAUGHT!");
                SpawnWorldPop(new Vector2(_blanketItemX, BlanketCatchLineY), "CAUGHT!", new Color(0.5f, 0.95f, 0.55f));
                LogPlaytestEvent("BlanketCatch", $"{_blanket.Caught}/{BlanketCatchesNeeded}");
            }

            if (_blanket.Missed > _blanketMissedSeen)
            {
                _blanketMissedSeen = _blanket.Missed;
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                LastCue = _blanket.Slack ? "Too close - the blanket sagged and the snack bounced off."
                    : _blanket.Overstretched ? "Blanket's torn - close the gap to fix the span."
                    : "Missed - get the middle of the blanket under the snack.";
                SetJuice(JuiceFeedbackKind.WarningMiss, "MISSED!");
                SpawnWorldPop(new Vector2(_blanketItemX, BlanketCatchLineY), "SPLAT!", new Color(0.85f, 0.5f, 0.3f));
                LogPlaytestEvent("BlanketMiss", $"{_blanket.Missed}");
            }
        }

        private void UpdateBlanketVisuals()
        {
            if (_blanketFallingItem != null)
                _blanketFallingItem.transform.position = new Vector3(_blanketItemX, _blanketItemY, 0f);
            if (_blanketObject == null) return;
            Color tint = _blanket.Taut ? new Color(0.45f, 0.85f, 0.55f)
                : _blanket.Overstretched ? new Color(0.9f, 0.35f, 0.25f)
                : new Color(0.85f, 0.8f, 0.35f); // slack
            _blanketObject.transform.position = new Vector3(_blanket.MidpointX, BlanketCatchLineY, 0f);
            float width = Mathf.Clamp(_blanket.Separation, 1f, BlanketMaxSeparation + 2f);
            _blanketObject.transform.localScale = new Vector3(width, 0.5f, 1f);
            if (_blanketObject.TryGetComponent<SpriteRenderer>(out var sr)) sr.color = tint;
            if (_blanketLabel != null)
            {
                _blanketLabel.text = _blanket.Taut ? $"BLANKET TAUT - CATCH! ({_blanket.Caught}/{BlanketCatchesNeeded})"
                    : _blanket.Overstretched ? "TOO FAR - RIPPING!" : "TOO CLOSE - SAGGING";
                // Keep the label upright and a readable size despite the blanket's horizontal stretch.
                _blanketLabel.transform.localScale = new Vector3(0.08f / Mathf.Max(width, 0.01f), 0.16f, 1f);
            }
        }

        /// <summary>Test hook: set the blanket span directly (separation + midpoint), as if from dog positions.</summary>
        public void ForceBlanketSpan(float separation, float midpointX)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.BlanketCatch) return;
            _blanket.UpdateSpan(separation, midpointX);
            HandleBlanketRips();
            if (Phase != State.GameOver) UpdateBlanketVisuals();
        }

        /// <summary>Test hook: a snack reaches the blanket line at <paramref name="itemX"/>; the span tries to catch it.</summary>
        public void ForceBlanketCatch(float itemX)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.BlanketCatch) return;
            _blanketItemX = itemX;
            _blanket.TryCatch(itemX);
            HandleBlanketProgress();
            if (Phase != State.GameOver) { UpdateBlanketVisuals(); CheckClear(); }
        }

        private float NearestEagleCoverDistance(Vector2 position)
        {
            float best = float.PositiveInfinity;
            foreach (var zone in _eagleCoverZones)
                best = Mathf.Min(best, Vector2.Distance(position, zone));
            return best;
        }

        // One sweep pass of the eagle shadow: any dog caught in the shadow column and not tucked
        // into a cover zone is exposed; otherwise the dogs successfully hid.
        private void EvaluateEagleShadowSweep()
        {
            if (_threatSweepState.RescueObjectiveActive || _threatSweepState.RescueComplete) return;

            bool exposed = false;
            if (_dogs != null && PredatorObject != null)
            {
                float shadowX = PredatorObject.transform.position.x;
                foreach (var dog in _dogs)
                {
                    bool underShadow = Mathf.Abs(dog.transform.position.x - shadowX) < EagleShadowWidth;
                    bool inCover = NearestEagleCoverDistance(dog.transform.position) < EagleCoverRadius;
                    if (underShadow && !inCover) { exposed = true; break; }
                }
            }

            if (exposed) RegisterEagleShadowExposure();
            else RegisterEagleShadowSafeHide();
        }

        private void TickThreatSweep()
        {
            if (PredatorObject == null) return;
            // Rescue phase: the eagle has snatched Cheddar - drive the wiggle/pull timing instead of sweeping.
            if (_threatSweepState.RescueObjectiveActive && !_threatSweepState.RescueComplete) { TickEagleRescue(); return; }
            if (_threatSweepState.RescueComplete) return; // freed; united-front phase, dogs roam

            var pos = PredatorObject.transform.position;
            float limit = _bounds.xMax - 1.5f;
            pos.x += _eagleSweepDir * Time.deltaTime * (_tuning.SquirrelMoveSpeed * 1.4f);
            if (pos.x >= limit) { pos.x = limit; _eagleSweepDir = -1; PredatorObject.transform.position = pos; EvaluateEagleShadowSweep(); return; }
            if (pos.x <= -limit) { pos.x = -limit; _eagleSweepDir = 1; PredatorObject.transform.position = pos; EvaluateEagleShadowSweep(); return; }
            PredatorObject.transform.position = pos;
        }

        private void StartEagleSnatchRescue()
        {
            _threatSweepState.StartRescue();
            _eagleRescue.Configure(EagleRescuePulls, EagleRescueWindow);
            _eagleRescuePullsSeen = 0;
            _eagleRescueMissesSeen = 0;
            // The eagle swoops to the snatch point with Cheddar in its talons.
            if (PredatorObject != null) PlaceObject(PredatorObject, _eagleSnatchPosition + Vector2.up * 1.2f);
            if (SquirrelObject != null)
            {
                SquirrelObject.SetActive(true);
                PlaceObject(SquirrelObject, _eagleSnatchPosition);
            }
            AddScore(150, "SHADOW DISTRACTED");
            LastFeedback = FeedbackKind.PartnerRescue;
            LastCue = "The eagle SNATCHED Cheddar! Cheddar: wiggle (Tug/Rescue) to crack the grip. Cocoa: get close and pull him free in the window!";
            UpdateEagleRescueVisuals();
            LogPlaytestEvent("EagleSnatch", LastCue);
        }

        private void TickEagleRescue()
        {
            int held = IndexOfDog(DogId.Cheddar);
            // Pin the snatched dog in the talons; the eagle hovers just above.
            if (held >= 0)
            {
                _dogs[held].transform.position = _eagleSnatchPosition;
                var body = _dogs[held].GetComponent<Rigidbody2D>();
                if (body != null) body.linearVelocity = Vector2.zero;
            }
            if (PredatorObject != null) PlaceObject(PredatorObject, _eagleSnatchPosition + Vector2.up * 1.2f);

            _eagleRescue.Advance(Time.deltaTime); // the cracked grip re-tightens as the window closes
            UpdateEagleRescueVisuals();
        }

        private void UpdateEagleRescueVisuals()
        {
            if (SquirrelObject != null)
                SetActorState(SquirrelObject,
                    _eagleRescue.WindowOpen ? "GRIP CRACKED - COCOA PULL NOW!" : "TALON GRIP - CHEDDAR WIGGLE!",
                    _eagleRescue.WindowOpen ? new Color(0.45f, 1f, 0.55f) : new Color(0.85f, 0.5f, 0.5f), 0.16f);
        }

        private void HandleEagleRescueProgress()
        {
            if (_eagleRescue.Pulls > _eagleRescuePullsSeen)
            {
                _eagleRescuePullsSeen = _eagleRescue.Pulls;
                AddScore(ScoreEventCatalog.SafeHide.Points, "GOOD PULL");
                LastFeedback = FeedbackKind.PartnerRescue;
                LastCue = $"Heave! Cocoa cracked him loose a bit more. ({_eagleRescue.Pulls}/{_eagleRescue.PullsNeeded})";
                SetJuice(JuiceFeedbackKind.SuccessPop, "HEAVE!");
                SpawnWorldPop(_eagleSnatchPosition, "HEAVE!", new Color(0.5f, 0.95f, 0.55f));
                LogPlaytestEvent("EagleRescuePull", $"{_eagleRescue.Pulls}/{_eagleRescue.PullsNeeded}");
            }

            if (_eagleRescue.MissedPulls > _eagleRescueMissesSeen)
            {
                _eagleRescueMissesSeen = _eagleRescue.MissedPulls;
                LastFeedback = FeedbackKind.SquirrelStoleFood;
                LastCue = "Mistimed pull - wait for Cheddar's wiggle to crack the grip first!";
                SetJuice(JuiceFeedbackKind.WarningMiss, "MISTIMED!");
                LogPlaytestEvent("EagleRescueMiss", $"{_eagleRescue.MissedPulls}");
            }

            if (_eagleRescue.Freed && !_threatSweepState.RescueComplete) CompleteEagleSnatchRescue();
        }

        private void CompleteEagleSnatchRescue()
        {
            _threatSweepState.CompleteRescue();
            int held = IndexOfDog(DogId.Cheddar);
            if (held >= 0) CreditDog(held);
            AddScore(ScoreEventCatalog.ToyRescued.Points, "PARTNER RESCUED");
            LastFeedback = FeedbackKind.PartnerRescue;
            LastCue = "Cocoa yanked Cheddar free of the talons! Now form the united-front bark circle.";
            if (PredatorObject != null) PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
            if (SquirrelObject != null) { SetActorState(SquirrelObject, "CHEDDAR'S FREE! HUDDLE FOR THE UNITED FRONT!", new Color(0.45f, 1f, 0.65f), 0.12f); }
            SetJuice(JuiceFeedbackKind.SuccessPop, "RESCUED!");
            SpawnWorldPop(_eagleSnatchPosition, "RESCUED!", new Color(0.5f, 1f, 0.45f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("eagle_partner_rescue", 0.32f, 0.6f, 0.2f);
            LogPlaytestEvent("EaglePartnerRescued", LastCue);
            LogObjectiveIfChanged();
        }

        private void RegisterEagleShadowSafeHide()
        {
            if (_threatSweepState.RescueComplete) return;

            _threatSweepState.AddSafeHide();
            _threatSweepState.AdvanceSweep(ShadowSweepCount);
            AddScore(ScoreEventCatalog.SafeHide.Points, ScoreEventCatalog.SafeHide.Label);
            LastFeedback = FeedbackKind.PredatorHuddle;
            LastCue = "Safe in cover! The eagle shadow swept past.";
            SetActorState(PredatorObject, $"SHADOW SWEEP {_threatSweepState.SweepIndex + 1} - HIDES {_threatSweepState.SafeHides}/{EagleRequiredHides}", new Color(0.16f, 0.16f, 0.2f), 0.28f);
            SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.SafeHide.Label);
            SpawnWorldPop(PredatorObject.transform.position, "SAFE HIDE!", new Color(0.55f, 0.85f, 1f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("eagle_safe_hide", 0.1f, 0.2f, 0.1f);
            LogPlaytestEvent("EagleSafeHide", $"hides {_threatSweepState.SafeHides}/{EagleRequiredHides}");

            if (_threatSweepState.ReadyForRescue(EagleRequiredHides))
            {
                StartEagleSnatchRescue();
            }

            LogObjectiveIfChanged();
        }

        private void RegisterEagleShadowExposure()
        {
            _threatSweepState.AddExposure();
            _threatSweepState.AdvanceSweep(ShadowSweepCount);
            AddScore(ScoreEventCatalog.FakeOut.Points, "EAGLE SPOOK");
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = $"Caught in the open! The eagle shadow spotted a dog ({_threatSweepState.Exposures}/{EagleMaxExposures}).";
            SetActorState(PredatorObject, $"SPOTTED! EXPOSURE {_threatSweepState.Exposures}/{EagleMaxExposures}", new Color(0.85f, 0.12f, 0.12f), 0.4f);
            SetJuice(JuiceFeedbackKind.WarningMiss, "EAGLE SPOOK!");
            SpawnWorldPop(PredatorObject.transform.position, "SPOTTED!", new Color(1f, 0.3f, 0.2f));
            RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            RequestRumble("eagle_exposure", 0.2f, 0.42f, 0.16f);
            LogPlaytestEvent("EagleExposure", LastCue);
            if (_threatSweepState.TooManyExposures(EagleMaxExposures)) EndRound(false);
            else LogObjectiveIfChanged();
        }

        // Rescue interact: the held dog (Cheddar) wiggles to crack the talon grip; the free dog (Cocoa)
        // pulls in that window. Both come through the Tug/Rescue button via the shared interact path.
        private void TryCompleteEagleShadowRescue(DogId dogId, bool force = false)
        {
            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;
            if (!_threatSweepState.RescueObjectiveActive)
            {
                MarkFailedInteraction(dogId, "rescue is not open yet - keep hiding from the shadow");
                return;
            }
            if (_threatSweepState.RescueComplete)
            {
                MarkFailedInteraction(dogId, "Cheddar's already free");
                return;
            }

            if (dogId == DogId.Cheddar)
            {
                // The snatched dog struggles, cracking the grip open for a moment.
                _eagleRescue.Wiggle();
                LastFeedback = FeedbackKind.SoloBark;
                LastCue = "Cheddar wiggles - the grip cracks open! Cocoa, pull NOW!";
                SetJuice(JuiceFeedbackKind.BarkBurst, "WIGGLE!");
                UpdateEagleRescueVisuals();
                return;
            }

            // Cocoa (the free dog) pulls - only lands while she's close enough to the talons.
            if (!force && Vector2.Distance(_dogs[dogIndex].transform.position, _eagleSnatchPosition) > EagleRescueRange)
            {
                MarkFailedInteraction(dogId, "get closer to the talons to pull Cheddar free");
                return;
            }
            _eagleRescue.Pull();
            HandleEagleRescueProgress();
        }

        /// <summary>Test hook: the snatched dog wiggles to crack the grip open.</summary>
        public void ForceEagleShadowWiggle()
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.EagleShadowPanic) return;
            _eagleRescue.Wiggle();
            UpdateEagleRescueVisuals();
        }

        /// <summary>Test hook: the free dog pulls; only counts while the wiggle window is open.</summary>
        public void ForceEagleShadowPull()
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.EagleShadowPanic) return;
            _eagleRescue.Pull();
            HandleEagleRescueProgress();
            if (Phase != State.GameOver) CheckClear();
        }

        /// <summary>Test hook: let the cracked grip re-tighten (the wiggle window closes).</summary>
        public void ForceEagleRescueAdvance(float seconds)
        {
            if (!MissionActive() || _mission == null || _mission.Variant != MissionVariant.EagleShadowPanic) return;
            _eagleRescue.Advance(seconds);
            UpdateEagleRescueVisuals();
        }

        private void CompleteEagleShadowUnitedFront()
        {
            if (!_threatSweepState.ReadyForUnitedFront) return;

            _threatSweepState.CompleteUnitedFront();
            AddScore(ScoreEventCatalog.UnitedFront.Points, ScoreEventCatalog.UnitedFront.Label);
            AddScore(500, "SHADOW PANIC CLEAR");
            LastFeedback = FeedbackKind.UnitedBark;
            LastCue = "United-front bark circle! The eagle gave up and the yard is safe.";
            SetActorState(PredatorObject, "UNITED FRONT - EAGLE RETREATS!", Color.gray, 0.1f);
            SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.UnitedFront.Label);
            SpawnWorldPop(_dogs[0].transform.position + Vector3.up, "UNITED FRONT!", new Color(1f, 0.95f, 0.3f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("eagle_united_front", 0.34f, 0.62f, 0.2f);
            LogPlaytestEvent("EagleUnitedFront", LastCue);
            CheckClear();
        }

        private void RegisterCoyoteBarkPressure(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _dogs.Length || _patrolState.FinalPressureComplete) return;

            _patrolState.AddBarkPressure();
            _coyotePressureHeld = true;
            AddScore(ScoreEventCatalog.FenceHeld.Points, ScoreEventCatalog.FenceHeld.Label);
            LastFeedback = FeedbackKind.SquirrelScared;
            LastCue = $"{DogName(_dogs[dogIndex])} bark-pinned the coyote at the fence - partner can fill dirt now!";
            SetActorState(PredatorObject, "COYOTE BLOCKED - PARTNER FILLS DIRT!", new Color(0.7f, 0.42f, 0.16f), 0.26f);
            SetJuice(JuiceFeedbackKind.SuccessPop, "COYOTE BLOCKED");
            SpawnWorldPop(PredatorObject.transform.position, "BLOCKED!", new Color(1f, 0.85f, 0.3f));
            RequestAudioCue(ArenaFeedbackCatalog.Bark);
            RequestRumble("coyote_block", 0.12f, 0.24f, 0.12f);
            LogPlaytestEvent("CoyoteBlocked", $"pressures {_patrolState.BarkPressures}");

            if (_patrolState.FakeSnackActive)
            {
                _patrolState.ResolveFakeSnack();
                LastCue = "The fake snack lure fizzled - the dogs held the fence instead of taking the bait!";
                LogPlaytestEvent("CoyoteFakeSnackResolved", LastCue);
            }

            LogObjectiveIfChanged();
        }

        private void TryCoyoteRepair(DogId dogId, bool force = false)
        {
            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;
            if (_patrolState.FinalPressureComplete)
            {
                MarkFailedInteraction(dogId, "yard already defended");
                return;
            }
            if (!_coyotePressureHeld)
            {
                MarkFailedInteraction(dogId, "partner must bark-hold the coyote before filling dirt");
                return;
            }
            if (!force && Vector2.Distance(_dogs[dogIndex].transform.position, _fenceGapPosition) > 2f)
            {
                MarkFailedInteraction(dogId, "too far from the fence weak spot");
                return;
            }

            _patrolState.AddRepair();
            CreditDog(dogIndex);
            _coyotePressureHeld = false;
            _patrolState.SelectGap((_patrolState.ActiveGapIndex + 1) % FenceGapCount);
            _fenceGapPosition = _fenceGaps[_patrolState.ActiveGapIndex % _fenceGaps.Length];
            PlaceObject(SquirrelObject, _fenceGapPosition);
            AddScore(ScoreEventCatalog.DirtFilled.Points, ScoreEventCatalog.DirtFilled.Label);
            LastFeedback = FeedbackKind.PartnerRescue;
            LastCue = $"{DogName(_dogs[dogIndex])} filled the weak spot ({_patrolState.GapsRepaired}/{CoyoteRequiredRepairs}). Patrol the next gap!";
            SetActorState(SquirrelObject, $"WEAK SPOT FILLED {_patrolState.GapsRepaired}/{CoyoteRequiredRepairs}", new Color(0.45f, 1f, 0.55f), 0.18f);
            SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.DirtFilled.Label);
            SpawnWorldPop(_fenceGapPosition, "DIRT FILLED!", new Color(0.55f, 1f, 0.45f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("coyote_repair", 0.2f, 0.4f, 0.14f);
            LogPlaytestEvent("CoyoteRepair", $"repairs {_patrolState.GapsRepaired}/{CoyoteRequiredRepairs}");

            if (_patrolState.ReadyForFinalPressure(CoyoteRequiredRepairs))
            {
                SetActorState(PredatorObject, "COYOTE GOING FOR THE FINAL PUSH - UNITED BARK!", new Color(0.85f, 0.3f, 0.12f), 0.34f);
                LastCue = "Fence is mostly patched! Get both dogs together and bark down the final coyote push.";
                LogPlaytestEvent("CoyoteFinalPressureReady", LastCue);
            }

            LogObjectiveIfChanged();
        }

        private void RegisterCoyoteBreach()
        {
            _patrolState.AddBreach();
            _coyotePressureHeld = false;
            _patrolState.SelectGap((_patrolState.ActiveGapIndex + 1) % FenceGapCount);
            AddScore(ScoreEventCatalog.FakeOut.Points, "COYOTE BREACH");
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = $"The coyote slipped through a weak spot! Breach {_patrolState.Breaches}/{CoyoteMaxBreaches}.";
            SetActorState(PredatorObject, $"COYOTE BREACH {_patrolState.Breaches}/{CoyoteMaxBreaches}!", new Color(0.85f, 0.12f, 0.12f), 0.4f);
            SetJuice(JuiceFeedbackKind.WarningMiss, "COYOTE BREACH!");
            SpawnWorldPop(PredatorObject.transform.position, "BREACH!", new Color(1f, 0.3f, 0.2f));
            RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            RequestRumble("coyote_breach", 0.2f, 0.42f, 0.16f);
            LogPlaytestEvent("CoyoteBreach", LastCue);
            if (_patrolState.TooManyBreaches(CoyoteMaxBreaches)) EndRound(false);
            else LogObjectiveIfChanged();
        }

        private void TriggerCoyoteFakeSnack()
        {
            if (_patrolState.FakeSnackActive) return;

            _patrolState.StartFakeSnack();
            bool cheddarCloser = _dogs.Length > 1 &&
                Vector2.Distance(_dogs[0].transform.position, PredatorObject.transform.position) <=
                Vector2.Distance(_dogs[1].transform.position, PredatorObject.transform.position);
            LastFeedback = FeedbackKind.SquirrelStealing;
            LastCue = cheddarCloser
                ? "Fake snack lure! Cheddar is RABIDLY tempted - someone bark him back to the fence!"
                : "Fake snack lure! Don't take the bait - keep barking the coyote off the fence.";
            SetActorState(PredatorObject, cheddarCloser ? "FAKE SNACK BAIT - CHEDDAR, NO!" : "FAKE SNACK BAIT - IGNORE IT!", new Color(0.9f, 0.6f, 0.15f), 0.32f);
            SetJuice(JuiceFeedbackKind.WarningMiss, "FAKE SNACK BAIT!");
            RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            RequestRumble("coyote_fake_snack", 0.14f, 0.3f, 0.12f);
            LogPlaytestEvent("CoyoteFakeSnack", LastCue);
            LogObjectiveIfChanged();
        }

        private void CompleteCoyoteFinalPressure()
        {
            if (!_patrolState.ReadyForFinalPressure(CoyoteRequiredRepairs)) return;

            _patrolState.CompleteFinalPressure();
            AddScore(ScoreEventCatalog.YardDefended.Points, ScoreEventCatalog.YardDefended.Label);
            LastFeedback = FeedbackKind.UnitedBark;
            LastCue = "United bark slammed the final coyote push - the yard is defended!";
            SetActorState(PredatorObject, "COYOTE RETREATS - YARD DEFENDED!", Color.gray, 0.1f);
            SetJuice(JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.YardDefended.Label);
            SpawnWorldPop(_dogs[0].transform.position + Vector3.up, "YARD DEFENDED!", new Color(1f, 0.95f, 0.3f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("coyote_yard_defended", 0.34f, 0.62f, 0.2f);
            LogPlaytestEvent("CoyoteYardDefended", LastCue);
            CheckClear();
        }

        private MissionRuntimeSnapshot BuildRuntimeSnapshot()
        {
            if (_activeMissionController != null)
                return _activeMissionController.CreateSnapshot(Score, TimeRemaining, Outcome);

            string missionId;
            int progress;
            int goal;
            int mistakes;
            if (_mission != null && _mission.Variant == MissionVariant.BackyardRescue)
            {
                missionId = "backyard_rescue";
                progress = BreakfastRecovered + _backyardTrapState.Recoveries;
                goal = BreakfastGoal + BackyardSquirrelTrapState.RequiredRecoveries;
                mistakes = StolenFood + _backyardTrapState.Fumbles;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy)
            {
                missionId = "squirrel_conspiracy";
                progress = _herdingState.ControlCount + (_herdingState.StashFound ? 1 : 0);
                goal = 5;
                mistakes = _herdingState.FakeOuts + _herdingState.Taunts;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.EagleShadowPanic)
            {
                missionId = "eagle_shadow_panic";
                progress = _threatSweepState.SafeHides + (_threatSweepState.RescueComplete ? 1 : 0) + (_threatSweepState.UnitedFrontComplete ? 1 : 0);
                goal = EagleRequiredHides + 2;
                mistakes = _threatSweepState.Exposures;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.CoyotesFence)
            {
                missionId = "coyotes_fence";
                progress = _patrolState.GapsRepaired + (_patrolState.FinalPressureComplete ? 1 : 0);
                goal = CoyoteRequiredRepairs + 1;
                mistakes = _patrolState.Breaches;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.WeenieRoundup)
            {
                missionId = "weenie_roundup";
                progress = _carryState.Delivered;
                goal = WeenieRequiredDeliveries;
                mistakes = _carryState.Drops;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.ScentSearch)
            {
                missionId = "scent_search";
                progress = _scentState.Found;
                goal = ScentRequiredFinds;
                mistakes = _scentState.WastedDigs;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.ThunderstormComfort)
            {
                missionId = "thunderstorm_comfort";
                progress = _stormState.ClapsSurvived;
                goal = _stormState.RequiredClaps;
                mistakes = 0;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.LeashWalk)
            {
                missionId = "leash_walk";
                progress = _leashState.Reached;
                goal = _leashState.RequiredCheckpoints;
                mistakes = _leashState.Snaps;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.CarRide)
            {
                missionId = "car_ride";
                progress = _carState.LurchesSurvived;
                goal = _carState.RequiredLurches;
                mistakes = _carState.Spills;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.BoneRelay)
            {
                missionId = "bone_relay";
                progress = _boneRelay.Finds;
                goal = BoneFindsNeeded;
                mistakes = _boneRelay.BlindActs + _boneRelay.WrongDigs;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.ChaosMachine)
            {
                missionId = "chaos_machine";
                progress = _chaos.Stage;
                goal = _chaos.StageCount;
                mistakes = _chaos.Stalls;
            }
            else if (_mission != null && _mission.Variant == MissionVariant.BlanketCatch)
            {
                missionId = "blanket_catch";
                progress = _blanket.Caught;
                goal = BlanketCatchesNeeded;
                mistakes = _blanket.Rips + _blanket.Missed;
            }
            else
            {
                missionId = ActiveMissionVariant.ToString();
                progress = BreakfastRecovered;
                goal = BreakfastGoal;
                mistakes = StolenFood + FailedInteractions;
            }
            return new MissionRuntimeSnapshot(missionId, Score, TimeRemaining, progress, goal, mistakes, Outcome == MissionOutcome.Clear, Outcome == MissionOutcome.Failed);
        }

        private void TickPredator()
        {
            if (!_mission.RequiresPredator) return;
            if (PredatorResolved || PredatorFailed) return;

            _predatorTimer -= Time.deltaTime;
            if (_predatorTimer <= _tuning.PredatorWarningSeconds && Phase == State.Playing) StartPredatorWarning();
            if (_predatorTimer <= 0f && Phase == State.PredatorWarning) StartPredatorAttack();
        }

        private void StartPredatorWarning()
        {
            _nextUnitedBarkAt = 0f;
            Phase = State.PredatorWarning;
            _predatorTarget = _rng.Next(_dogs.Length);
            LastFeedback = FeedbackKind.PredatorHuddle;
            LastCue = $"Shadow over {DogName(_dogs[_predatorTarget])}! Huddle together and bark!";
            PredatorObject.name = "Predator Warning";
            PlaceObject(PredatorObject, (Vector2)_dogs[_predatorTarget].transform.position + Vector2.up * 2f);
            SetActorState(PredatorObject, "SHADOW! HUDDLE + DOUBLE BARK!", new Color(1f, 0.08f, 0.08f), 0.42f);
            SetJuice(JuiceFeedbackKind.WarningMiss, "SHADOW WARNING!");
            RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            RequestRumble("predator_warning", 0.16f, 0.3f, 0.14f);
            LogPlaytestEvent("PredatorWarning", LastCue);
            LogObjectiveIfChanged();
        }

        private void StartPredatorAttack()
        {
            _nextUnitedBarkAt = 0f;
            Phase = State.PredatorAttack;
            if (_predatorTarget < 0) _predatorTarget = 0;

            PredatorObject.name = "Predator Attack";
            PlaceObject(PredatorObject, _dogs[_predatorTarget].transform.position);
            SetActorState(PredatorObject, $"YOINKED {DogName(_dogs[_predatorTarget]).ToUpperInvariant()} - PARTNER BARK!", new Color(0.8f, 0f, 0f), 0.45f);

            _grabbedDog = _predatorTarget;
            _dogs[_grabbedDog].SetMode(MovementMode.Stunned);
            PredatorFailed = true;
            AddScore(-_tuning.PredatorFailurePenalty, "PREDATOR HIT");
            LastFeedback = FeedbackKind.PredatorAttack;
            LastCue = $"{DogName(_dogs[_grabbedDog])} got yoinked! Partner bark rescue!";
            SetJuice(JuiceFeedbackKind.WarningMiss, $"RESCUE {DogName(_dogs[_grabbedDog]).ToUpperInvariant()}!");
            SpawnWorldPop(_dogs[_grabbedDog].transform.position, "YOINKED!", new Color(1f, 0.2f, 0.2f));
            RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            RequestRumble("predator_penalty", 0.24f, 0.45f, 0.18f);
            LogPlaytestEvent("PredatorAttack", LastCue);
            LogObjectiveIfChanged();
        }

        private void ResolvePredator()
        {
            PredatorResolved = true;
            PredatorFailed = false;
            Phase = State.Playing;
            AddScore(_tuning.PredatorDefendedScore, "PREDATOR YEETED");
            LastFeedback = FeedbackKind.UnitedBark;
            LastCue = "DOUBLE WOOF drove the predator away!";
            PredatorObject.name = "Predator Driven Away";
            PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
            SetActorState(PredatorObject, "DOUBLE WOOF YEETED SHADOW", Color.gray, 0.08f);
            SetJuice(JuiceFeedbackKind.SuccessPop, "PREDATOR YEETED!");
            SpawnWorldPop(_dogs[0].transform.position + Vector3.up, "DOUBLE WOOF!", new Color(1f, 0.95f, 0.25f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("team_success", 0.32f, 0.55f, 0.18f);
            LogPlaytestEvent("PredatorDefended", LastCue);
            CheckClear();
        }

        private void TickTugProximity()
        {
            if (!_mission.RequiresTug) return;
            if (TugComplete || _dogs.Length < 2) return;

            bool cheddarNear = Vector2.Distance(_dogs[0].transform.position, RopeObject.transform.position) < _tuning.TugTogetherDistance;
            bool cocoaNear = Vector2.Distance(_dogs[1].transform.position, RopeObject.transform.position) < _tuning.TugTogetherDistance;
            if (!cheddarNear || !cocoaNear)
            {
                if (cheddarNear != cocoaNear)
                {
                    LastFeedback = FeedbackKind.TugNeedsPartner;
                    LastCue = "Rope wiggles: both dogs have to commit together!";
                    string waitingFor = cheddarNear ? "WAITING FOR COCOA" : "WAITING FOR CHEDDAR";
                    SetActorState(RopeObject, $"ROPE NEEDS BOTH DOGS - {waitingFor}", new Color(1f, 0.8f, 0.28f), 0.2f);
                    LogObjectiveIfChanged();
                }
                return;
            }

            // Face each dog into the rope so the team tug reads as two dogs pulling from opposite sides.
            if (DogFeedback[0] != null) DogFeedback[0].ShowTug((Vector2)(RopeObject.transform.position - _dogs[0].transform.position));
            if (DogFeedback[1] != null) DogFeedback[1].ShowTug((Vector2)(RopeObject.transform.position - _dogs[1].transform.position));
            TugProgress = Mathf.Min(1f, TugProgress + Time.deltaTime * _tuning.TugChargePerSecond);
            LastFeedback = FeedbackKind.TugTogether;
            LastCue = "Both dogs are tugging - tiny sausage teamwork!";
            SetActorState(RopeObject, $"BOTH DOGS TUGGING {Mathf.RoundToInt(TugProgress * 100f)}%", new Color(1f, 0.78f, 0.22f), 0.22f);
            if (TugProgress >= 1f) CompleteTug();
        }

        private void TickSockBasket()
        {
            if (!_sockBasketState.BasketOpen || Time.time < _sockOpeningUntil) return;
            _sockBasketState.ExpireOpening();
            RegisterSockFumble("FUMBLE! The basket flopped shut on the runaway sock.");
        }

        private void TryTipSockBasket(DogId dogId, bool force = false)
        {
            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0 || _laundryBasketObject == null) return;
            if (!force && Vector2.Distance(_dogs[dogIndex].transform.position, _laundryBasketObject.transform.position) > SockBasketInteractRange)
            {
                MarkFailedInteraction(dogId, "too far from laundry basket");
                return;
            }
            if (!_sockBasketState.TryOpen(dogIndex))
            {
                MarkFailedInteraction(dogId, "basket already held open");
                return;
            }

            _exposedSock = FindFirstHiddenSock();
            if (_exposedSock == null)
            {
                _sockBasketState.ExpireOpening();
                return;
            }

            _exposedSock.transform.position = _laundryBasketObject.transform.position + new Vector3(2f, 0f, 0f);
            _exposedSock.gameObject.SetActive(true);
            _sockOpeningUntil = Time.time + SockOpeningSeconds;
            AddScore(ScoreEventCatalog.BasketTipped.Points, ScoreEventCatalog.BasketTipped.Label);
            LastCue = $"{DogName(_dogs[dogIndex])} tipped the basket - partner dive for the sock!";
            SetActorState(_laundryBasketObject, "BASKET HELD OPEN - PARTNER DIVE NOW!", new Color(0.96f, 0.72f, 0.32f), 0.22f);
            SpawnWorldPop(_laundryBasketObject.transform.position, "TIP! PARTNER DIVE!", new Color(0.62f, 0.9f, 1f));
            RequestAudioCue(ArenaFeedbackCatalog.Bark);
            LogPlaytestEvent("SockBasket", LastCue);
            LogObjectiveIfChanged();
        }

        private Treat FindFirstHiddenSock()
        {
            foreach (var treat in _treats)
                if (treat != null && !treat.gameObject.activeSelf) return treat;
            return null;
        }

        private void RegisterSockFumble(string cue)
        {
            if (_exposedSock != null) _exposedSock.gameObject.SetActive(false);
            _exposedSock = null;
            AddScore(ScoreEventCatalog.SockDecoy.Points, ScoreEventCatalog.SockDecoy.Label);
            LastCue = cue;
            CloseSockBasketVisuals();
            SpawnWorldPop(_laundryBasketObject != null ? _laundryBasketObject.transform.position : Vector3.zero, "DECOY FUMBLE!", new Color(1f, 0.45f, 0.25f));
            RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
            LogPlaytestEvent("SockFumble", cue);
            LogObjectiveIfChanged();
        }

        private void CloseSockBasketVisuals()
        {
            _sockOpeningUntil = 0f;
            if (_laundryBasketObject != null)
                SetActorState(_laundryBasketObject, "LAUNDRY BASKET - TIP AGAIN!", new Color(0.78f, 0.56f, 0.3f), 0.08f);
        }

        private void OnDogInteracted(DogId dogId)
        {
            if (!MissionActive()) return;

            if (_mission != null && _mission.Variant == MissionVariant.SockPanic)
            {
                TryTipSockBasket(dogId);
                return;
            }

            if (_mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy)
            {
                TryFindConspiracyStash(dogId);
                return;
            }

            if (_mission != null && _mission.Variant == MissionVariant.EagleShadowPanic)
            {
                TryCompleteEagleShadowRescue(dogId);
                return;
            }

            if (_mission != null && _mission.Variant == MissionVariant.CoyotesFence)
            {
                TryCoyoteRepair(dogId);
                return;
            }

            if (_mission != null && _mission.Variant == MissionVariant.ScentSearch)
            {
                int dig = IndexOfDog(dogId);
                if (dig >= 0) DigAtSpot(dig, NearestActiveDigSpot(_dogs[dig].transform.position), false);
                return;
            }

            if (_mission == null || !_mission.RequiresTug)
            {
                MarkFailedInteraction(dogId, "no interact target in this mission");
                return;
            }

            if (TugComplete)
            {
                MarkFailedInteraction(dogId, "tug already complete");
                return;
            }

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;
            if (Vector2.Distance(_dogs[dogIndex].transform.position, RopeObject.transform.position) > _tuning.TugInteractDistance)
            {
                MarkFailedInteraction(dogId, "too far from rope");
                return;
            }

            if (DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ShowTug((Vector2)(RopeObject.transform.position - _dogs[dogIndex].transform.position));
            TugProgress = Mathf.Min(1f, TugProgress + _tuning.TugInteractProgress);
            LastFeedback = FeedbackKind.TugNeedsPartner;
            LastCue = $"{DogName(_dogs[dogIndex])} has the rope - partner pile on!";
            SetActorState(RopeObject, $"ROPE {Mathf.RoundToInt(TugProgress * 100f)}% - NEED PARTNER DOG", new Color(1f, 0.78f, 0.22f), 0.2f);
            RequestAudioCue(ArenaFeedbackCatalog.Bark);
            LogPlaytestEvent("Tug", LastCue);
            if (TugProgress >= 1f) CompleteTug();
        }

        private void CompleteTug()
        {
            TugComplete = true;
            AddScore(_tuning.TugScore, "TUG COMPLETE");
            LastFeedback = FeedbackKind.TugTogether;
            LastCue = "Rope tug complete - dramatic victory chomps!";
            RopeObject.name = "Rope/Tug Complete";
            SetActorState(RopeObject, "ROPE COMPLETE! TEAM CHOMP!", new Color(0.3f, 1f, 0.3f), 0.08f);
            SetJuice(JuiceFeedbackKind.SuccessPop, "TUG POP! ROPE COMPLETE");
            SpawnWorldPop(RopeObject.transform.position, "TUG POP!", new Color(0.45f, 1f, 0.35f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("tug_success", 0.32f, 0.58f, 0.2f);
            LogPlaytestEvent("TugComplete", "Rope objective complete");
            CheckClear();
        }

        private void OnDogBarked(DogId dogId)
        {
            if (!MissionActive()) return;

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;

            BarksUsed++;
            CreditDog(dogIndex);
            _lastBarks[dogIndex] = Time.time;
            var dog = _dogs[dogIndex];
            bool barkDidSomething = false;
            RequestAudioCue(ArenaFeedbackCatalog.Bark);
            RequestRumble("bark", 0.08f, 0.18f, 0.08f);
            LogPlaytestEvent("Bark", DogName(dog));

            if (_mission.Variant == MissionVariant.SquirrelConspiracy)
            {
                barkDidSomething = TryProgressSquirrelConspiracyBark(dogIndex);
            }
            else if (_mission.Variant == MissionVariant.BackyardRescue &&
                     !_backyardTrapState.Complete && _squirrelTarget != null &&
                     Vector2.Distance(dog.transform.position, SquirrelObject.transform.position) < _tuning.SingleBarkSquirrelRange)
            {
                barkDidSomething = ResolveBackyardTrapPressure(dogId, IsBackyardGapHeld());
            }
            else if (_mission.Variant == MissionVariant.CoyotesFence)
            {
                RegisterCoyoteBarkPressure(dogIndex);
                barkDidSomething = true;
            }
            else if (_mission.Variant == MissionVariant.ScentSearch)
            {
                DoScentSniff(dogIndex);
                barkDidSomething = true;
            }
            else if (_activeMissionController != null)
            {
                _activeMissionController.HandleBark(dogIndex);
                barkDidSomething = true;
            }
            else if (_mission.UsesSquirrel && Vector2.Distance(dog.transform.position, SquirrelObject.transform.position) < _tuning.SingleBarkSquirrelRange)
            {
                ScareSquirrel(_tuning.SingleBarkScareSeconds, $"{DogName(dog)} scared the squirrel!", true);
                barkDidSomething = true;
            }

            if (_grabbedDog >= 0 && dogIndex != _grabbedDog &&
                Vector2.Distance(dog.transform.position, _dogs[_grabbedDog].transform.position) < _tuning.RescueBarkRange)
            {
                RescueGrabbedDog(dog);
                return;
            }

            if (Time.time < _nextUnitedBarkAt || !AllDogsBarkedRecently() || !DogsAreHuddled())
            {
                if (!barkDidSomething && Time.time >= _teamBarkFeedbackUntil)
                {
                    LastFeedback = FeedbackKind.SoloBark;
                    LastCue = $"{DogName(dog)} solo WOOF: emotionally powerful, mechanically suspicious.";
                    SetJuice(JuiceFeedbackKind.BarkBurst, $"{DogName(dog).ToUpperInvariant()} BARK BURST");
                }
                return;
            }

            UnitedBarks++;
            AddScore(_tuning.UnitedBarkScore, "UNITED BARK");
            _nextUnitedBarkAt = Time.time + _tuning.UnitedBarkCooldown;
            _teamBarkFeedbackUntil = Time.time + 0.35f;
            LastFeedback = FeedbackKind.UnitedBark;
            if (_mission.Variant != MissionVariant.BackyardRescue || _backyardTrapState.Complete ||
                (_squirrelTarget == null && !_backyardTrapState.WeenieDropped))
                ScareSquirrel(_tuning.UnitedBarkScareSeconds, "United bark shook the whole yard!", false);
            LogPlaytestEvent("UnitedBark", $"{UnitedBarks} total");

            if (Phase == State.PredatorWarning || Phase == State.PredatorAttack) ResolvePredator();
            if (_mission.Variant == MissionVariant.EagleShadowPanic && _threatSweepState.ReadyForUnitedFront) CompleteEagleShadowUnitedFront();
            if (_mission.Variant == MissionVariant.CoyotesFence && _patrolState.ReadyForFinalPressure(CoyoteRequiredRepairs)) CompleteCoyoteFinalPressure();
        }

        private void ScareSquirrel(float seconds, string cue, bool awardScore)
        {
            _squirrelTarget = null;
            _squirrelScaredUntil = Mathf.Max(_squirrelScaredUntil, Time.time + seconds);
            _squirrelTimer = SquirrelDelay();
            if (awardScore && Time.time >= _nextSquirrelScareScoreAt)
            {
                AddScore(_mission.SquirrelScareScore, _mission.SquirrelScareScoreLabel);
                _nextSquirrelScareScoreAt = Time.time + 1f;
            }
            LastFeedback = awardScore ? FeedbackKind.SquirrelScared : FeedbackKind.UnitedBark;
            LastCue = awardScore ? $"{cue} It dropped the snack plan!" : "DOUBLE WOOF made the squirrel reconsider its life.";
            SetActorState(SquirrelObject, awardScore ? _mission.SquirrelDroppedActorLabel : "SQUIRREL HID FROM DOUBLE WOOF", new Color(0.85f, 0.85f, 0.85f), 0.08f);
            SetJuice(awardScore ? JuiceFeedbackKind.SuccessPop : JuiceFeedbackKind.BarkBurst,
                awardScore ? _mission.SquirrelScareJuiceLabel : "DOUBLE WOOF BURST");
            SpawnWorldPop(SquirrelObject.transform.position, awardScore ? "DROP!" : "DOUBLE WOOF!", new Color(0.9f, 0.95f, 1f));
            if (awardScore) RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            LogPlaytestEvent(awardScore ? "SquirrelScared" : "SquirrelUnitedScare", LastCue);
            LogObjectiveIfChanged();
        }

        private bool IsBackyardGapHeld()
        {
            int gapDog = IndexOfDog(_backyardTrapState.GapDog);
            return gapDog >= 0 && Vector2.Distance(_dogs[gapDog].transform.position, _backyardTrapGapPosition) <= BackyardTrapGapRadius;
        }

        private bool ResolveBackyardTrapPressure(DogId dogId, bool gapHeld)
        {
            if (_squirrelTarget == null) return false;

            var result = _backyardTrapState.TryRedirect(dogId, gapHeld);
            if (result == BackyardSquirrelTrapState.RedirectResult.Success)
            {
                _backyardDroppedWeenie = _squirrelTarget;
                _squirrelTarget = null;
                _backyardDroppedWeenie.transform.position = _backyardTrapGapPosition + Vector2.left * 3f;
                PlaceObject(SquirrelObject, _backyardTrapGapPosition + Vector2.right * 4f);
                _squirrelTimer = SquirrelDelay();
                AddScore(_mission.SquirrelScareScore, "SQUIRREL REDIRECTED");
                LastFeedback = FeedbackKind.SquirrelScared;
                LastCue = $"{dogId} pressured the squirrel into the blocked route - {_backyardTrapState.RecoveryDog} recover the dropped weenie!";
                SetActorState(SquirrelObject, "TRAPPED! PARTNER RECOVER THE DROP", new Color(0.85f, 0.85f, 0.85f), 0.12f);
                SetJuice(JuiceFeedbackKind.SuccessPop, "SQUIRREL REDIRECT! PARTNER RECOVER");
                SpawnWorldPop(_backyardDroppedWeenie.transform.position, "DROP! PARTNER ONLY!", new Color(0.9f, 0.95f, 1f));
                RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                LogPlaytestEvent("SquirrelTrapRedirect", LastCue);
                UpdateBackyardTrapGapMarker();
                LogObjectiveIfChanged();
                return true;
            }

            if (result == BackyardSquirrelTrapState.RedirectResult.WrongPressureDog)
                RegisterBackyardTrapFumble($"WRONG WOOF! {_backyardTrapState.PressureDog} must pressure this pass.");
            else if (result == BackyardSquirrelTrapState.RedirectResult.GapOpen)
                RegisterBackyardTrapFumble($"FAKE ROUTE! {_backyardTrapState.GapDog} must hold the escape gap first.");
            return result != BackyardSquirrelTrapState.RedirectResult.Complete;
        }

        private void RegisterBackyardTrapFumble(string cue)
        {
            LastCue = cue + " The squirrel loops back for another try.";
            SetJuice(JuiceFeedbackKind.WarningMiss, "SQUIRREL JUKE!");
            SpawnWorldPop(SquirrelObject.transform.position, "NYEH-HEH! WRONG WAY!", new Color(1f, 0.45f, 0.25f));
            RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
            RequestRumble("squirrel_penalty", 0.12f, 0.25f, 0.12f);
            LogPlaytestEvent("SquirrelTrapFumble", LastCue);
            LogObjectiveIfChanged();
        }

        private void RegisterBackyardTrapRecoveryFumble(DogId dogId)
        {
            MarkFailedInteraction(dogId, $"only {_backyardTrapState.RecoveryDog} can recover the trap weenie");
            LastCue = $"HOT-POTATO FUMBLE! {dogId} caused the drop, so {_backyardTrapState.RecoveryDog} must recover it.";
            if (_backyardDroppedWeenie != null)
            {
                Vector2 bounce = dogId == DogId.Cheddar ? new Vector2(-2.5f, 1.5f) : new Vector2(2.5f, -1.5f);
                _backyardDroppedWeenie.transform.position = ClampInsideBounds((Vector2)_backyardDroppedWeenie.transform.position + bounce, 1.2f);
                SpawnWorldPop(_backyardDroppedWeenie.transform.position, "HOT POTATO! PARTNER ONLY!", new Color(1f, 0.45f, 0.25f));
            }
            SetJuice(JuiceFeedbackKind.WarningMiss, "PARTNER RECOVERY FUMBLE");
            RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
            LogPlaytestEvent("SquirrelTrapFumble", LastCue);
            LogObjectiveIfChanged();
        }

        private void RescueGrabbedDog(DogController rescuer)
        {
            if (_grabbedDog < 0) return;

            int rescuedDog = _grabbedDog;
            _dogs[_grabbedDog].SetMode(MovementMode.Free);
            _grabbedDog = -1;
            Phase = State.Playing;
            AddScore(_tuning.RescueScore, "PARTNER RESCUE");
            LastFeedback = FeedbackKind.PartnerRescue;
            LastCue = $"{DogName(rescuer)} bark-rescued their sibling - heroic nonsense!";
            if (DogFeedback[rescuedDog] != null) DogFeedback[rescuedDog].ShowRescued();
            int rescuerIndex = IndexOfDog(rescuer.GetComponent<DogIdentity>().Id);
            if (rescuerIndex >= 0 && DogFeedback[rescuerIndex] != null) DogFeedback[rescuerIndex].ShowProudBrief();
            SetJuice(JuiceFeedbackKind.SuccessPop, "RESCUE POP!");
            SpawnWorldPop(_dogs[rescuedDog].transform.position, "RESCUED!", new Color(0.45f, 1f, 0.65f));
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("rescue_success", 0.34f, 0.62f, 0.2f);
            LogPlaytestEvent("Rescue", LastCue);
            LogObjectiveIfChanged();
        }

        private void CheckClear()
        {
            if (Phase == State.LevelClear || Phase == State.GameOver) return;
            if (_activeMissionController != null)
            {
                if (_activeMissionController.IsComplete) EndRound(true);
                else if (_activeMissionController.IsFailed) EndRound(false);
                return;
            }
            if (_mission.Variant == MissionVariant.SquirrelConspiracy)
            {
                if (_herdingState.StashFound) EndRound(true);
                return;
            }
            if (_mission.Variant == MissionVariant.EagleShadowPanic)
            {
                if (_threatSweepState.UnitedFrontComplete) EndRound(true);
                return;
            }
            if (_mission.Variant == MissionVariant.CoyotesFence)
            {
                if (_patrolState.FinalPressureComplete) EndRound(true);
                return;
            }
            if (_mission.Variant == MissionVariant.WeenieRoundup)
            {
                if (_carryState.ReadyToClear(WeenieRequiredDeliveries)) EndRound(true);
                return;
            }
            if (_mission.Variant == MissionVariant.ScentSearch)
            {
                if (_scentState.ReadyToClear(ScentRequiredFinds)) EndRound(true);
                return;
            }
            if (_mission.Variant == MissionVariant.ThunderstormComfort)
            {
                if (_stormState.ReadyToClear())
                {
                    AddScore(ScoreEventCatalog.StormCleared.Points, ScoreEventCatalog.StormCleared.Label);
                    EndRound(true);
                }
                return;
            }
            if (_mission.Variant == MissionVariant.LeashWalk)
            {
                if (_leashState.ReadyToClear()) EndRound(true);
                return;
            }
            if (_mission.Variant == MissionVariant.CarRide)
            {
                if (_carState.ReadyToClear()) EndRound(true);
                return;
            }
            if (_mission.Variant == MissionVariant.BoneRelay)
            {
                if (_boneRelay.Solved) EndRound(true);
                return;
            }
            if (_mission.Variant == MissionVariant.ChaosMachine)
            {
                if (_chaos.Solved) EndRound(true);
                return;
            }
            if (_mission.Variant == MissionVariant.BlanketCatch)
            {
                if (_blanket.Solved) EndRound(true);
                return;
            }

            bool hasItems = BreakfastRecovered >= _mission.ItemGoal;
            bool hasPredator = !_mission.RequiresPredator || PredatorResolved;
            bool hasTug = !_mission.RequiresTug || TugComplete;
            bool hasBackyardTrap = _mission.Variant != MissionVariant.BackyardRescue || _backyardTrapState.Complete;
            if (hasItems && hasPredator && hasTug && hasBackyardTrap) EndRound(true);
        }

        private void EndRound(bool clear)
        {
            Phase = clear ? State.LevelClear : State.GameOver;
            CurrentFlow = FlowState.EndScreen;
            Outcome = clear ? MissionOutcome.Clear : MissionOutcome.Failed;
            if (clear)
            {
                LastRoundFlawless = BuildRuntimeSnapshot().Mistakes == 0;
                if (LastRoundFlawless) AddScore(_tuning.FlawlessBonus, "FLAWLESS");
                AddScore(_tuning.ClearScore + Mathf.CeilToInt(TimeRemaining) * _tuning.TimeBonusMultiplier, _mission.ClearScoreLabel);
                var rank = MissionRankCalculator.Calculate(Score, true, _mission.PawfectScore, _mission.HeroScore, _mission.SurvivorScore);
                EndRank = rank.Rank;
                StarRating = rank.Stars;
                LastFeedback = FeedbackKind.LevelClear;
                LastCue = $"{_mission.ClearBannerPrefix} {EndRank}. Score {Score}";
                MissionBanner = $"{_mission.ClearBannerPrefix} {EndRank}";
                EndReasonLabel = EndReasonFor(clear);
                SetJuice(JuiceFeedbackKind.SuccessPop, $"{_mission.ClearBannerPrefix} POP!");
                RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
                RequestRumble("mission_win", 0.42f, 0.68f, 0.24f);
            }
            else
            {
                LastRoundFlawless = false;
                AddScore(-_tuning.GameOverPenalty, "GAME OVER");
                EndRank = RankForScore(Score, false, _mission);
                StarRating = 0;
                LastFeedback = FeedbackKind.GameOver;
                LastCue = $"MISSION FAILED: {EndRank}. Score {Score}";
                MissionBanner = $"MISSION FAILED! {EndRank}";
                EndReasonLabel = EndReasonFor(clear);
                SetJuice(JuiceFeedbackKind.WarningMiss, "SAD FLOP REPLAY!");
                RequestAudioCue(ArenaFeedbackCatalog.MissionFail);
                RequestRumble("mission_fail", 0.24f, 0.5f, 0.24f);
            }
            EndSummaryLabel = BuildOutcomeSummaryLabel();

            foreach (var dog in _dogs)
            {
                dog.SetMode(MovementMode.Free);
                if (dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            }

            for (int i = 0; i < DogFeedback.Length; i++)
            {
                if (DogFeedback[i] == null) continue;
                if (clear) DogFeedback[i].ShowProud();
                else DogFeedback[i].ShowSad();
            }

            for (int i = 0; i < _inputs.Length; i++)
            {
                if (_inputs[i] != null) _inputs[i].enabled = false;
            }

            HideObjectiveArrows();
            HideInteractionRanges();
            if (_backyardTrapGapMarker != null) _backyardTrapGapMarker.SetActive(false);
            _activeMissionController?.Cleanup();
            SetSquirrelCutoffMarkersActive(false);
            RecordSessionResult();
            LogPlaytestEvent(clear ? "MissionClear" : "MissionFail", EndSummaryLabel);
            LogObjectiveIfChanged();
        }

        /// <summary>Compatibility hook forwarded to the active Kitchen controller.</summary>
        public void ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind kind)
        {
            if (MissionActive()) KitchenController?.ForceDrop(kind);
        }

        /// <summary>Compatibility hook forwarded to the active Kitchen controller.</summary>
        public void ForceKitchenTelegraph(DogId dog, KitchenFoodFrenzyMissionState.FoodKind kind)
        {
            if (MissionActive()) KitchenController?.ForceTelegraph(dog, kind);
        }

        /// <summary>Compatibility hook forwarded to the active Kitchen controller.</summary>
        public void ForceKitchenReleaseTelegraph()
        {
            if (MissionActive()) KitchenController?.ForceReleaseTelegraph();
        }

        /// <summary>Compatibility hook forwarded to the active Kitchen controller.</summary>
        public void ForceKitchenCatch(DogId dog, bool intoSafeZone)
        {
            if (MissionActive()) KitchenController?.ForceCatch(dog, intoSafeZone);
        }

        /// <summary>Compatibility hook forwarded to the active Kitchen controller.</summary>
        public void ForceKitchenLetFall()
        {
            if (MissionActive()) KitchenController?.ForceLetFall();
        }

        /// <summary>Deterministic hook for the controller-owned Pee Break state machine.</summary>
        public void ForcePeeBreakAdvance(SocialStimulus active, float deltaTime)
        {
            if (MissionActive()) PeeBreakController?.ForceAdvance(active, deltaTime);
            CheckClear();
        }


        private string BuildOutcomeSummaryLabel()
        {
            string funny;
            if (_activeMissionController != null)
                funny = _activeMissionController.OutcomeSummary ?? Outcome.ToString();
            else if (_mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy)
                funny = MissionOutcomeSummaryBuilder.BuildSquirrelSummary(_herdingState);
            else if (_mission != null && _mission.Variant == MissionVariant.EagleShadowPanic)
                funny = MissionOutcomeSummaryBuilder.BuildThreatSweepSummary(_threatSweepState);
            else if (_mission != null && _mission.Variant == MissionVariant.CoyotesFence)
                funny = MissionOutcomeSummaryBuilder.BuildPatrolSummary(_patrolState);
            else if (_mission != null && _mission.Variant == MissionVariant.WeenieRoundup)
                funny = MissionOutcomeSummaryBuilder.BuildCarrySummary(_carryState, WeenieRequiredDeliveries);
            else if (_mission != null && _mission.Variant == MissionVariant.ScentSearch)
                funny = MissionOutcomeSummaryBuilder.BuildScentSummary(_scentState, ScentRequiredFinds);
            else if (_mission != null && _mission.Variant == MissionVariant.ThunderstormComfort)
                funny = MissionOutcomeSummaryBuilder.BuildThunderstormSummary(_stormState);
            else if (_mission != null && _mission.Variant == MissionVariant.LeashWalk)
                funny = MissionOutcomeSummaryBuilder.BuildLeashSummary(_leashState);
            else if (_mission != null && _mission.Variant == MissionVariant.CarRide)
                funny = MissionOutcomeSummaryBuilder.BuildCarBalanceSummary(_carState);
            else if (_mission != null && _mission.Variant == MissionVariant.BoneRelay)
                funny = MissionOutcomeSummaryBuilder.BuildBoneRelaySummary(_boneRelay);
            else if (_mission != null && _mission.Variant == MissionVariant.ChaosMachine)
                funny = MissionOutcomeSummaryBuilder.BuildChaosMachineSummary(_chaos);
            else if (_mission != null && _mission.Variant == MissionVariant.BlanketCatch)
                funny = MissionOutcomeSummaryBuilder.BuildBlanketCatchSummary(_blanket);
            else
                funny = Outcome.ToString();
            return $"{funny}: {Score} - {EndRank}";
        }

        private bool MissionActive() => Phase == State.Playing || Phase == State.PredatorWarning || Phase == State.PredatorAttack;

        private void AddScore(int delta, string reason)
        {
            Score += delta;
            LastScoreDelta = delta;
            string sign = delta >= 0 ? "+" : "-";
            LastScoreEventLabel = $"{sign}{Mathf.Abs(delta)} {reason}";
            LastScorePopLabel = LastScoreEventLabel;
            _scorePopUntil = Time.time + 1.4f;
            RequestAudioCue(delta >= 0 ? ArenaFeedbackCatalog.ScoreGain : ArenaFeedbackCatalog.ScorePenalty);
            LogPlaytestEvent("ScoreDelta", LastScoreEventLabel);
        }

        private string BuildObjectiveLabel()
        {
            if (_mission == null) return "Protect the weenies";
            if (MissionSelectVisible) return "Choose a mission";
            if (SessionSummaryVisible) return "Session Summary";
            if (IsLevelClear) return _mission.ClearObjectiveText;
            if (IsGameOver) return _mission.FailObjectiveText;
            if (_dogs == null || _dogs.Length == 0) return _mission.IntroPrompt;

            if (Phase == State.PredatorAttack && _grabbedDog >= 0)
                return $"Rescue {DogName(_dogs[_grabbedDog])}";
            if (Phase == State.PredatorWarning)
                return "Huddle + bark at the shadow";
            if (_activeMissionController != null) return _activeMissionController.ObjectiveLabel;
            if (_mission.Variant == MissionVariant.BackyardRescue && !_backyardTrapState.Complete)
            {
                if (_backyardTrapState.WeenieDropped)
                    return $"{_backyardTrapState.RecoveryDog}: recover the dropped weenie (partner only) - trap {_backyardTrapState.Recoveries}/{BackyardSquirrelTrapState.RequiredRecoveries}";
                if (_squirrelTarget != null)
                    return $"Bark to scare squirrel: {_backyardTrapState.PressureDog} pressure / {_backyardTrapState.GapDog} HOLD ESCAPE GAP - trap {_backyardTrapState.Recoveries}/{BackyardSquirrelTrapState.RequiredRecoveries}";
                if (_mission.RequiresTug && !TugComplete && BreakfastRecovered >= Mathf.Max(2, recoveryGoal / 2))
                    return _mission.TugObjectiveText;
                return $"Save weenies; next trap: {_backyardTrapState.PressureDog} pressures, {_backyardTrapState.GapDog} holds gap ({_backyardTrapState.Recoveries}/{BackyardSquirrelTrapState.RequiredRecoveries})";
            }
            if (_mission.Variant == MissionVariant.SockPanic)
            {
                if (_sockBasketState.BasketOpen)
                    return $"Basket open! Partner dive for the sock ({BreakfastRecovered}/{recoveryGoal} returned)";
                return $"Tip the laundry basket, then partner-dive: socks {BreakfastRecovered}/{recoveryGoal}, fumbles {_sockBasketState.Fumbles}";
            }
            if (_mission.Variant == MissionVariant.SquirrelConspiracy)
            {
                if (_herdingState.StashRevealed) return "Sniff the revealed stash and interact";
                return $"Herd squirrel route {_herdingState.RouteIndex + 1}/4: controls {_herdingState.ControlCount}/4, taunts {_herdingState.Taunts}/3";
            }
            if (_mission.Variant == MissionVariant.EagleShadowPanic)
            {
                if (_threatSweepState.RescueComplete) return "United-front bark circle: huddle close and bark together";
                if (_threatSweepState.RescueObjectiveActive) return $"Eagle snatched Cheddar! Cheddar wiggle (Tug/Rescue), Cocoa pull in the window (pulls {_eagleRescue.Pulls}/{_eagleRescue.PullsNeeded})";
                return $"Hide from the eagle shadow: safe hides {_threatSweepState.SafeHides}/{EagleRequiredHides}, exposures {_threatSweepState.Exposures}/{EagleMaxExposures}";
            }
            if (_mission.Variant == MissionVariant.CoyotesFence)
            {
                if (_patrolState.ReadyForFinalPressure(CoyoteRequiredRepairs)) return "Block the final coyote push - both dogs bark together";
                if (_patrolState.FakeSnackActive) return "Ignore the fake snack lure - hold the fence";
                if (_coyotePressureHeld) return "Coyote pinned - partner fill the weak spot now";
                return $"Patrol fence gap {_patrolState.ActiveGapIndex + 1}: repairs {_patrolState.GapsRepaired}/{CoyoteRequiredRepairs}, breaches {_patrolState.Breaches}/{CoyoteMaxBreaches}";
            }
            if (_mission.Variant == MissionVariant.WeenieRoundup)
            {
                bool anyCarrying = _dogCarrying != null && System.Array.Exists(_dogCarrying, c => c);
                if (anyCarrying) return $"Carry the weenie to the HOME BOWL ({_carryState.Delivered}/{WeenieRequiredDeliveries} delivered)";
                return $"Round up the scattered weenies: {_carryState.Delivered}/{WeenieRequiredDeliveries} delivered, {_carryState.Loose} loose";
            }
            if (_mission.Variant == MissionVariant.ScentSearch)
            {
                return $"Sniff (bark) for HOT/COLD, then dig (interact): bones {_scentState.Found}/{ScentRequiredFinds}, cold digs {_scentState.WastedDigs}/{ScentMaxWastedDigs}";
            }
            if (_mission.Variant == MissionVariant.ThunderstormComfort)
            {
                int panicPct = _panic != null ? Mathf.RoundToInt(Mathf.Max(_panic.CheddarPanic, _panic.CocoaPanic) * 100f) : 0;
                return $"Huddle to stay calm and ride out the storm: claps {_stormState.ClapsSurvived}/{_stormState.RequiredClaps}, panic {panicPct}%";
            }
            if (_mission.Variant == MissionVariant.LeashWalk)
            {
                return $"Walk the leash together to checkpoint {Mathf.Min(_leashState.Reached + 1, _leashState.RequiredCheckpoints)}/{_leashState.RequiredCheckpoints} - stay close (snaps {_leashState.Snaps}/{MaxLeashSnaps})";
            }
            if (_mission.Variant == MissionVariant.CarRide)
            {
                string tilt = Mathf.Abs(_carBalance) < 0.15f ? "LEVEL" : (_carBalance > 0 ? "tipping RIGHT" : "tipping LEFT");
                return $"Lean to keep the car level ({tilt}): steadied {_carState.LurchesSurvived}/{_carState.RequiredLurches}, spills {_carState.Spills}/{CarMaxSpills}";
            }
            if (_mission.Variant == MissionVariant.BoneRelay)
            {
                int wasted = _boneRelay.BlindActs + _boneRelay.WrongDigs;
                if (!_boneRelay.Known) return $"Cocoa: sniff the scent post to call the real mound - Cheddar, wait for it! (bones {_boneRelay.Finds}/{BoneFindsNeeded}, wasted {wasted}/{BoneMaxWasted})";
                return $"Cheddar: dig the glowing mound Cocoa called! (bones {_boneRelay.Finds}/{BoneFindsNeeded}, wasted {wasted}/{BoneMaxWasted})";
            }
            if (_mission.Variant == MissionVariant.ChaosMachine)
            {
                if (!_chaos.Running) return $"Pre-position at your junctions, then pull the lever to start the cascade (junctions {_chaos.Stage}/{_chaos.StageCount}, misfires {_chaos.Stalls}/{ChaosMaxStalls})";
                int stage = Mathf.Clamp(_chaos.Stage, 0, _chaosJunctionActions.Length - 1);
                string who = _chaosOwners[stage] == ChainActor.Cheddar ? "Cheddar" : "Cocoa";
                return $"{who}: be at the {_chaosJunctionActions[stage].ToLowerInvariant()} junction NOW - the cascade's rolling! (junctions {_chaos.Stage}/{_chaos.StageCount}, misfires {_chaos.Stalls}/{ChaosMaxStalls})";
            }
            if (_mission.Variant == MissionVariant.BlanketCatch)
            {
                string span = _blanket.Taut ? "taut - slide the middle under the snack" : _blanket.Overstretched ? "too far apart - close up before it rips!" : "too close - spread out to pull it taut";
                return $"Hold the blanket {span} (caught {_blanket.Caught}/{BlanketCatchesNeeded}, rips {_blanket.Rips}/{BlanketMaxRips})";
            }
            if (_squirrelTarget != null)
                return _mission.SquirrelObjectiveText;
            if (_mission.RequiresTug && !TugComplete && BreakfastRecovered >= Mathf.Max(2, recoveryGoal / 2))
                return _mission.TugObjectiveText;
            if (BreakfastRecovered < recoveryGoal)
                return string.Format(_mission.CollectObjectiveFormat, BreakfastRecovered, recoveryGoal);
            if (_mission.RequiresTug && !TugComplete)
                return _mission.TugObjectiveText;
            if (!PredatorResolved)
                return _mission.WaitingObjectiveText;

            return _mission.WaitingObjectiveText;
        }

        private string EndReasonFor(bool clear)
        {
            if (clear)
            {
                if (EndRank == "Pawfect Yard") return _mission.PawfectClearReason;
                if (EndRank == "Backyard Heroes") return _mission.HeroClearReason;
                return _mission.BasicClearReason;
            }

            if (_activeMissionController != null && !string.IsNullOrEmpty(_activeMissionController.FailReason))
                return _activeMissionController.FailReason;

            if (_mission.Variant == MissionVariant.SquirrelConspiracy && _herdingState.TooManyTaunts(3)) return "The squirrel taunted the dogs into a full backyard misinformation spiral.";
            if (_mission.Variant == MissionVariant.EagleShadowPanic && _threatSweepState.TooManyExposures(EagleMaxExposures)) return "The eagle shadow caught the dogs in the open one too many times.";
            if (_mission.Variant == MissionVariant.CoyotesFence && _patrolState.TooManyBreaches(CoyoteMaxBreaches)) return "The coyote breached the fence one too many times while the dogs got separated.";
            if (_mission.Variant == MissionVariant.ScentSearch && _scentState.TooManyWastedDigs(ScentMaxWastedDigs)) return "The dogs dug up half the yard chasing cold scents and ran out of patience.";
            if (_mission.Variant == MissionVariant.LeashWalk && _leashState.TooManySnaps(MaxLeashSnaps)) return "The leash snapped taut too many times - the walk fell apart.";
            if (_mission.Variant == MissionVariant.CarRide && _carState.TooManySpills(CarMaxSpills)) return "The car tipped over too many times on the way home.";
            if (_mission.Variant == MissionVariant.BoneRelay && _boneRelay.BlindActs + _boneRelay.WrongDigs >= BoneMaxWasted) return "The dogs dug up half the yard guessing instead of waiting for Cocoa's call.";
            if (_mission.Variant == MissionVariant.ChaosMachine && _chaos.Stalls >= ChaosMaxStalls) return "The machine misfired too many times - the cascade never made it to the end.";
            if (_mission.Variant == MissionVariant.BlanketCatch && _blanket.TooManyRips) return "The blanket tore to shreds - too many over-stretches and there was nothing left to catch with.";
            if (_mission.Variant == MissionVariant.ThunderstormComfort && _panic != null && _panic.Maxed != null) return $"{_panic.Maxed} panicked at the thunder and bolted before the storm passed.";
            if (_mission.UsesSquirrel && StolenFood >= maxStolenFood) return _mission.StolenFailReason;
            if (TimeRemaining <= 0f) return _mission.TimeFailReason;
            if (_mission.RequiresPredator && PredatorFailed) return _mission.PredatorFailReason;
            return _mission.GenericFailReason;
        }

        private void SetJuice(JuiceFeedbackKind kind, string label)
        {
            LastJuiceFeedback = kind;
            LastJuiceLabel = label;
            JuiceFeedbackSequence++;
            OnJuiceFeedback?.Invoke(kind, label);
        }

        private void LogObjectiveIfChanged()
        {
            string objective = ObjectiveLabel;
            if (objective == _lastLoggedObjective) return;

            _lastLoggedObjective = objective;
            ObjectiveChangeCount++;
            LogPlaytestEvent("ObjectiveChanged", objective);
        }

        private void LogPlaytestEvent(string kind, string detail)
        {
            _playtestLog.Add(kind, detail);
        }

        private void CreditDog(int dogIndex)
        {
            if (_dogContribution != null && dogIndex >= 0 && dogIndex < _dogContribution.Length) _dogContribution[dogIndex]++;
        }

        private void MarkFailedInteraction(DogId dogId, string reason)
        {
            FailedInteractions++;
            LogPlaytestEvent("InteractionMiss", $"{dogId}: {reason}");
        }

        private static string RankForScore(int score, bool clear, MissionDefinition mission)
        {
            return MissionRankCalculator.Calculate(score, clear, mission.PawfectScore, mission.HeroScore, mission.SurvivorScore).Rank;
        }

        private float SquirrelDelay()
        {
            if (!_squirrelHasStarted)
                return ActiveModifier == RoundModifier.SquirrelTrouble ? _tuning.FirstSquirrelTroubleDelay : _tuning.FirstSquirrelBaseDelay;
            return ActiveModifier == RoundModifier.SquirrelTrouble ? _tuning.SquirrelTroubleDelay : _tuning.SquirrelBaseDelay;
        }

        private void TickFlowInput()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;

            bool pausePressed = (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                                (pad != null && pad.startButton.wasPressedThisFrame);
            if (IsPaused)
            {
                if (pausePressed) SetPaused(false);
                return;
            }
            if (MissionActive() && pausePressed)
            {
                SetPaused(true);
                return;
            }

            if (kb != null && (kb.f1Key.wasPressedThisFrame || kb.backquoteKey.wasPressedThisFrame))
            {
                TogglePlaytestOverlay();
            }
            if (kb != null && kb.f2Key.wasPressedThisFrame) SetAudioEnabled(!AudioEnabled);
            if (kb != null && kb.f3Key.wasPressedThisFrame) SetRumbleEnabled(!RumbleEnabled);

            if (MissionSelectVisible)
            {
                bool up = false;
                bool down = false;
                bool left = false;
                bool right = false;
                bool next = false;
                bool start = false;
                if (kb != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame) { StartMission(MissionVariant.BackyardRescue); return; }
                    if (kb.digit2Key.wasPressedThisFrame) { StartMission(MissionVariant.SnackHeist); return; }
                    if (kb.digit3Key.wasPressedThisFrame) { StartMission(MissionVariant.SockPanic); return; }
                    if (kb.digit4Key.wasPressedThisFrame) { StartMission(MissionVariant.SquirrelConspiracy); return; }
                    if (kb.digit5Key.wasPressedThisFrame) { StartMission(MissionVariant.EagleShadowPanic); return; }
                    if (kb.digit6Key.wasPressedThisFrame) { StartMission(MissionVariant.CoyotesFence); return; }
                    if (kb.digit7Key.wasPressedThisFrame) { StartMission(MissionVariant.WeenieRoundup); return; }
                    if (kb.digit8Key.wasPressedThisFrame) { StartMission(MissionVariant.ScentSearch); return; }
                    if (kb.digit9Key.wasPressedThisFrame) { StartMission(MissionVariant.ThunderstormComfort); return; }
                    if (kb.digit0Key.wasPressedThisFrame) { StartMission(MissionVariant.MarkTheYard); return; }
                    up |= kb.upArrowKey.wasPressedThisFrame;
                    down |= kb.downArrowKey.wasPressedThisFrame;
                    left |= kb.leftArrowKey.wasPressedThisFrame;
                    right |= kb.rightArrowKey.wasPressedThisFrame;
                    next |= kb.tabKey.wasPressedThisFrame;
                    start |= kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                }
                if (pad != null)
                {
                    up |= pad.dpad.up.wasPressedThisFrame;
                    down |= pad.dpad.down.wasPressedThisFrame;
                    left |= pad.dpad.left.wasPressedThisFrame;
                    right |= pad.dpad.right.wasPressedThisFrame;
                    start |= pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame;
                }

                if (up) SelectMissionAbove();
                else if (down) SelectMissionBelow();
                else if (left) SelectMissionLeft();
                else if (right) SelectMissionRight();
                else if (next) SelectNextMission();
                else if (start) StartSelectedMission();
                return;
            }

            if (EndScreenVisible)
            {
                bool replay = false;
                bool next = false;
                bool missionSelect = false;
                if (kb != null)
                {
                    replay |= kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                    next |= kb.nKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
                    missionSelect |= kb.mKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame;
                }
                if (pad != null)
                {
                    replay |= pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame;
                    next |= pad.rightShoulder.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame;
                    missionSelect |= pad.buttonEast.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame;
                }

                if (missionSelect) ReturnToMissionSelect();
                else if (next) ChooseNextMission();
                else if (replay) Restart();
                return;
            }

            if (SessionSummaryVisible)
            {
                bool continueSession = false;
                bool back = false;
                if (kb != null)
                {
                    continueSession |= kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.nKey.wasPressedThisFrame;
                    back |= kb.mKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame;
                }
                if (pad != null)
                {
                    continueSession |= pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame;
                    back |= pad.buttonEast.wasPressedThisFrame;
                }
                if (back) ReturnToMissionSelect();
                else if (continueSession) ContinueSession();
            }
        }

        private void TickMissionSelectionKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) StartMission(MissionVariant.BackyardRescue);
            else if (kb.digit2Key.wasPressedThisFrame) StartMission(MissionVariant.SnackHeist);
            else if (kb.digit3Key.wasPressedThisFrame) StartMission(MissionVariant.SockPanic);
            else if (kb.digit4Key.wasPressedThisFrame) StartMission(MissionVariant.SquirrelConspiracy);
            else if (kb.digit5Key.wasPressedThisFrame) StartMission(MissionVariant.EagleShadowPanic);
            else if (kb.digit6Key.wasPressedThisFrame) StartMission(MissionVariant.CoyotesFence);
            else if (kb.digit7Key.wasPressedThisFrame) StartMission(MissionVariant.WeenieRoundup);
            else if (kb.digit8Key.wasPressedThisFrame) StartMission(MissionVariant.ScentSearch);
            else if (kb.digit9Key.wasPressedThisFrame) StartMission(MissionVariant.ThunderstormComfort);
            else if (kb.digit0Key.wasPressedThisFrame) StartMission(MissionVariant.MarkTheYard);
        }

        private void ShowMissionSelect()
        {
            CurrentFlow = FlowState.MissionSelect;
            Phase = State.Intro;
            Outcome = MissionOutcome.InProgress;
            TimeRemaining = 0f;
            Score = 0;
            LastScoreDelta = 0;
            UnitedBarks = 0;
            BreakfastRecovered = 0;
            StolenFood = 0;
            PredatorResolved = false;
            PredatorFailed = false;
            TugProgress = 0f;
            TugComplete = false;
            StarRating = 0;
            EndRank = "Needs More Bark";
            EndSummaryLabel = string.Empty;
            EndReasonLabel = string.Empty;
            LastScoreEventLabel = "0 READY FOR DOG BUSINESS";
            LastScorePopLabel = string.Empty;
            LastCue = $"{SelectedMissionName}: {SelectedMissionBriefing}";
            MissionBanner = "Mission Select";
            LastFeedback = FeedbackKind.Intro;
            LastJuiceFeedback = JuiceFeedbackKind.None;
            LastJuiceLabel = string.Empty;
            BarksUsed = 0;
            FailedInteractions = 0;
            ObjectiveChangeCount = 0;
            _scorePopUntil = 0f;
            _squirrelTarget = null;
            _grabbedDog = -1;
            ClearTreats();
            DisableDogInputs();
            HideObjectiveArrows();
            HideInteractionRanges();
            SetMissionObjectsActive(false);

            if (_dogs != null)
            {
                for (int i = 0; i < _dogs.Length; i++)
                {
                    _dogs[i].SetMode(MovementMode.Free);
                    _dogs[i].transform.position = _dogStarts[i];
                    if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
                    if (DogFeedback != null && i < DogFeedback.Length && DogFeedback[i] != null) DogFeedback[i].ClearMissionPose();
                }
            }

            _lastLoggedObjective = string.Empty;
            LogPlaytestEvent("MissionSelect", SelectedMissionName);
            LogObjectiveIfChanged();
        }

        private void RecordSessionResult()
        {
            if (_roundResultRecorded) return;
            _roundResultRecorded = true;

            SessionMissionsPlayed++;
            SessionTotalScore += Score;
            SessionStarsEarned += StarRating;
            if (LastRoundFlawless) SessionFlawlessClears++;
            int missionIndex = IndexOfMission(_mission.Variant);
            LastRoundWasBest = false;
            if (missionIndex >= 0)
            {
                bool playedBefore = _sessionCompletedMissions[missionIndex];
                _sessionCompletedMissions[missionIndex] = true;
                if (Outcome == MissionOutcome.Clear) _sessionClearedMissions[missionIndex] = true;
                if (LastRoundFlawless) _sessionFlawlessMissions[missionIndex] = true;
                if (Score > _sessionBestByMission[missionIndex])
                {
                    LastRoundWasBest = playedBefore; // only a "new best" if there was a prior run to beat
                    _sessionBestByMission[missionIndex] = Score;
                }
            }
            if (Outcome == MissionOutcome.Failed && missionIndex >= 0) _sessionFailuresByMission[missionIndex]++;
            SessionUniqueMissionsCompleted = CountCompletedMissions();
            _sessionRanks.Add($"{_mission.Name}: {EndRank}");
            UpdateSessionSummaryLabel();
        }

        private string BuildFailPressureLabel()
        {
            if (_mission == null || CurrentFlow == FlowState.MissionSelect) return "Fail pressure: no active mission";

            string squirrel = _mission.UsesSquirrel ? $"squirrel {StolenFood}/{maxStolenFood}" : "squirrel off";
            string predator = _mission.RequiresPredator
                ? (PredatorResolved ? "predator resolved" : PredatorFailed ? "predator failed/rescue path" : $"predator {Phase}")
                : "predator off";
            string tug = _mission.RequiresTug ? $"tug {Mathf.RoundToInt(TugProgress * 100f)}%" : "tug off";
            return $"Fail pressure: {Mathf.CeilToInt(Mathf.Max(0f, TimeRemaining))}s / {squirrel} / {predator} / {tug}";
        }

        private string BuildDogPositionsLabel()
        {
            if (_dogs == null || _dogs.Length == 0) return "Dogs: not spawned";

            var parts = new List<string>(_dogs.Length);
            foreach (var dog in _dogs)
            {
                if (dog == null) continue;
                Vector3 p = dog.transform.position;
                parts.Add($"{DogName(dog)} ({p.x:0.0},{p.y:0.0})");
            }

            return parts.Count == 0 ? "Dogs: not spawned" : $"Dogs: {string.Join(" | ", parts)}";
        }

        private string BuildMissionFailureSummaryLabel()
        {
            var parts = new List<string>(MissionOrder.Length);
            for (int i = 0; i < MissionOrder.Length; i++)
                if (_sessionFailuresByMission[i] > 0)
                    parts.Add($"{BuildMissionDefinition(MissionOrder[i], _tuning).Name} {_sessionFailuresByMission[i]}");
            return parts.Count == 0 ? "Failures: none yet" : $"Failures: {string.Join(" / ", parts)}";
        }

        private void UpdateSessionSummaryLabel()
        {
            string lead = SessionAllMissionsCompleted
                ? "Backyard legends! Cheddar + Cocoa finished every mission."
                : "Session Summary:";
            SessionSummaryLabel = $"{lead} {SessionMissionsPlayed} missions played, {SessionTotalScore} score, {SessionStarsEarned} stars, {SessionFlawlessClears} flawless, {SessionUniqueMissionsCompleted}/{MissionOrder.Length} finished.";
            if (_sessionRanks.Count == 0)
            {
                SessionRanksEarnedLabel = "Recent ranks: none yet.";
                return;
            }

            const int visibleRanks = 3;
            int first = Mathf.Max(0, _sessionRanks.Count - visibleRanks);
            var recent = new List<string>(visibleRanks);
            for (int i = first; i < _sessionRanks.Count; i++) recent.Add(_sessionRanks[i]);
            int earlier = _sessionRanks.Count - recent.Count;
            string earlierLabel = earlier > 0 ? $" (+{earlier} earlier)" : string.Empty;
            SessionRanksEarnedLabel = $"Recent ranks{earlierLabel}: {string.Join(" | ", recent)}";
        }

        private int CountCompletedMissions()
        {
            int count = 0;
            for (int i = 0; i < _sessionCompletedMissions.Length; i++)
            {
                if (_sessionCompletedMissions[i]) count++;
            }
            return count;
        }

        private int NextUnfinishedMissionIndex(int current)
        {
            if (current < 0) current = _selectedMissionIndex;
            for (int offset = 1; offset <= MissionOrder.Length; offset++)
            {
                int candidate = (current + offset) % MissionOrder.Length;
                if (!_sessionCompletedMissions[candidate]) return candidate;
            }
            return (current + 1) % MissionOrder.Length;
        }

        private static int IndexOfMission(MissionVariant variant)
        {
            for (int i = 0; i < MissionOrder.Length; i++)
            {
                if (MissionOrder[i] == variant) return i;
            }
            return 0;
        }

        private void DisableDogInputs()
        {
            if (_dogs != null)
                foreach (var dog in _dogs)
                    if (dog != null) dog.SetTravelAssist(false);
            if (_inputs == null) return;
            for (int i = 0; i < _inputs.Length; i++)
            {
                if (_inputs[i] != null) _inputs[i].enabled = false;
            }
        }

        private void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (paused)
            {
                DisableDogInputs();
                StopRumble();
            }
            else if (MissionActive() && _inputs != null)
            {
                foreach (var input in _inputs)
                    if (input != null) input.enabled = true;
            }
            LogPlaytestEvent("Pause", paused ? "paused" : "resumed");
        }

        private void SetMissionObjectsActive(bool active)
        {
            if (SquirrelObject != null) SquirrelObject.SetActive(active && _mission != null && _mission.UsesSquirrel);
            if (PredatorObject != null) PredatorObject.SetActive(active && _mission != null && _mission.RequiresPredator);
            if (RopeObject != null) RopeObject.SetActive(active && _mission != null && _mission.RequiresTug);
            if (_bunnyCameoObject != null) _bunnyCameoObject.SetActive(active);
            if (!active && _backyardTrapGapMarker != null) _backyardTrapGapMarker.SetActive(false);
            if (!active) _activeMissionController?.Cleanup();
            if (!active || _mission == null || _mission.Variant != MissionVariant.EagleShadowPanic) SetEagleCoverMarkersActive(false);
            if (!active || _mission == null || _mission.Variant != MissionVariant.SquirrelConspiracy) SetSquirrelCutoffMarkersActive(false);
            if (!active || _mission == null || _mission.Variant != MissionVariant.CoyotesFence) SetCoyoteGapMarkersActive(false);
            if (!active || _mission == null || _mission.Variant != MissionVariant.WeenieRoundup) SetWeenieRoundupActive(false);
            if (!active || _mission == null || _mission.Variant != MissionVariant.ScentSearch) SetScentSearchActive(false);
            if (!active || _mission == null || _mission.Variant != MissionVariant.LeashWalk) SetLeashCheckpointMarkersActive(false);
        }

        public static MissionDefinition BuildMissionDefinition(MissionVariant variant) =>
            BuildMissionDefinition(variant, ArenaMissionTuning.CreateDefault());

        private static MissionDefinition BuildMissionDefinition(MissionVariant variant, ArenaMissionTuning tuning)
        {
            if (MissionCatalog.TryBuild(variant, tuning, out var registeredDefinition))
                return registeredDefinition;

            var balance = tuning.BalanceFor(variant);
            switch (variant)
            {
                case MissionVariant.SnackHeist:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.SnackHeist,
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
                case MissionVariant.SquirrelConspiracy:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.SquirrelConspiracy,
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
                case MissionVariant.EagleShadowPanic:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.EagleShadowPanic,
                        Name = "Eagle Shadow Panic",
                        IntroPrompt = "Cheddar + Cocoa must hide from the sweeping eagle shadow. Hide twice and the eagle swoops and SNATCHES Cheddar - he wiggles (Tug/Rescue) to crack the talon grip while Cocoa pulls him free in the window. Then form a united-front bark circle to drive the eagle off.",
                        ReadyScoreLabel = "READY TO DODGE THE SHADOW",
                        ItemRootName = "Shadow Cover",
                        ItemObjectName = "Cover Spot",
                        ItemWorldLabel = "Hide!",
                        ItemArrowLabel = "HIDE",
                        ItemCollectCueNoun = "a safe hide",
                        CollectObjectiveFormat = "Survive shadow sweep {0}/{1}",
                        CollectedScoreLabel = "SAFE HIDE",
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
                        SquirrelObjectiveText = "Hide from the eagle shadow",
                        SquirrelStealingCue = "No squirrel here - the eagle shadow is the threat.",
                        SquirrelStoleCue = "No squirrel here - watch the sky.",
                        SquirrelStealScoreLabel = "EAGLE SPOOK",
                        SquirrelScareScoreLabel = "SHADOW DISTRACTED",
                        SquirrelStealingActorLabel = "EAGLE SHADOW SWEEP",
                        SquirrelDroppedActorLabel = "SHADOW PASSED",
                        SquirrelStoleActorLabel = "SHADOW SPOTTED A DOG",
                        SquirrelMissPopLabel = "SPOTTED!",
                        SquirrelStealJuiceLabel = "EAGLE SPOOK!",
                        SquirrelScareJuiceLabel = "SHADOW DISTRACTED!",
                        TugObjectiveText = "Rescue the stranded toy",
                        WaitingObjectiveText = "Hide in cover and wait out the shadow",
                        ClearObjectiveText = "Yard defended - replay Eagle Shadow Panic",
                        ClearBannerPrefix = "EAGLE DRIVEN OFF!",
                        ClearScoreLabel = "SHADOW PANIC CLEAR",
                        ReplayPrompt = "Press R / Enter / Start to replay Eagle Shadow Panic",
                        FailObjectiveText = "Mission failed - replay Eagle Shadow Panic",
                        GenericFailReason = "Needs tighter hide-and-bark timing before the next flyover.",
                        TimeFailReason = "The eagle circled until the clock ran out.",
                        StolenFailReason = "The eagle shadow kept catching dogs in the open.",
                        PredatorFailReason = "The eagle shadow caught a dog in the open.",
                        PawfectClearReason = "Tiny defenders dodged every shadow and barked the eagle out of the sky.",
                        HeroClearReason = "The toy was rescued and the united front held strong.",
                        BasicClearReason = "The eagle gave up, even if a few sweeps got close.",
                        ItemColor = new Color(0.4f, 0.46f, 0.6f),
                        ItemAccentColor = new Color(0.7f, 0.82f, 1f),
                        ItemSecondaryColor = new Color(0.14f, 0.16f, 0.22f),
                        ItemPopColor = new Color(0.7f, 0.85f, 1f)
                    };
                case MissionVariant.CoyotesFence:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.CoyotesFence,
                        Name = "Coyotes at the Fence",
                        IntroPrompt = "Cheddar + Cocoa must patrol the fence gaps, bark-pin the coyote, fill the weak spots together, and block the final push.",
                        ReadyScoreLabel = "READY TO HOLD THE FENCE",
                        ItemRootName = "Fence Weak Spots",
                        ItemObjectName = "Weak Spot",
                        ItemWorldLabel = "Gap!",
                        ItemArrowLabel = "GAP",
                        ItemCollectCueNoun = "a filled weak spot",
                        CollectObjectiveFormat = "Fill weak spots {0}/{1}",
                        CollectedScoreLabel = "DIRT FILLED",
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
                        SquirrelObjectiveText = "Bark-pin the coyote at the fence",
                        SquirrelStealingCue = "No squirrel here - the coyote is testing the fence.",
                        SquirrelStoleCue = "No squirrel here - watch the gaps.",
                        SquirrelStealScoreLabel = "COYOTE BREACH",
                        SquirrelScareScoreLabel = "FENCE HELD",
                        SquirrelStealingActorLabel = "COYOTE AT THE FENCE",
                        SquirrelDroppedActorLabel = "COYOTE BLOCKED",
                        SquirrelStoleActorLabel = "COYOTE BREACH",
                        SquirrelMissPopLabel = "BREACH!",
                        SquirrelStealJuiceLabel = "COYOTE BREACH!",
                        SquirrelScareJuiceLabel = "FENCE HELD!",
                        TugObjectiveText = "Fill the fence weak spot",
                        WaitingObjectiveText = "Patrol the fence gaps together",
                        ClearObjectiveText = "Yard defended - replay Coyotes at the Fence",
                        ClearBannerPrefix = "YARD DEFENDED!",
                        ClearScoreLabel = "COYOTE PATROL CLEAR",
                        ReplayPrompt = "Press R / Enter / Start to replay Coyotes at the Fence",
                        FailObjectiveText = "Mission failed - replay Coyotes at the Fence",
                        GenericFailReason = "Needs tighter patrol splits before the next coyote shift.",
                        TimeFailReason = "The coyote outlasted the patrol until the clock ran out.",
                        StolenFailReason = "The coyote breached the fence too many times.",
                        PredatorFailReason = "The coyote isolated a dog at the fence.",
                        PawfectClearReason = "Tiny patrol legends held every gap and barked the coyote into retirement.",
                        HeroClearReason = "The fence held and the final push was blocked clean.",
                        BasicClearReason = "The yard survived, even if a few gaps got scary.",
                        ItemColor = new Color(0.55f, 0.42f, 0.2f),
                        ItemAccentColor = new Color(0.85f, 0.7f, 0.35f),
                        ItemSecondaryColor = new Color(0.2f, 0.14f, 0.06f),
                        ItemPopColor = new Color(0.95f, 0.8f, 0.35f)
                    };
                case MissionVariant.WeenieRoundup:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.WeenieRoundup,
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
                case MissionVariant.ScentSearch:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.ScentSearch,
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
                case MissionVariant.ThunderstormComfort:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.ThunderstormComfort,
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
                case MissionVariant.LeashWalk:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.LeashWalk,
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
                case MissionVariant.CarRide:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.CarRide,
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
                case MissionVariant.BoneRelay:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.BoneRelay,
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
                case MissionVariant.ChaosMachine:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.ChaosMachine,
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
                case MissionVariant.BlanketCatch:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.BlanketCatch,
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
                case MissionVariant.SockPanic:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.SockPanic,
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
                default:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.BackyardRescue,
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
        }

        private int IndexOfDog(DogId dogId)
        {
            for (int i = 0; i < _dogs.Length; i++)
            {
                if (_dogs[i] != null && _dogs[i].GetComponent<DogIdentity>().Id == dogId) return i;
            }
            return -1;
        }

        private bool AllDogsBarkedRecently()
        {
            for (int i = 0; i < _lastBarks.Length; i++)
            {
                if (Time.time - _lastBarks[i] > _tuning.UnitedBarkWindow) return false;
            }
            return true;
        }

        private bool DogsAreHuddled()
        {
            Vector2 first = _dogs[0].transform.position;
            for (int i = 1; i < _dogs.Length; i++)
            {
                if (Vector2.Distance(first, _dogs[i].transform.position) > _tuning.UnitedBarkRange) return false;
            }
            return true;
        }

        private Treat FindTreatNear(Vector2 position, float range)
        {
            foreach (var treat in _treats)
            {
                if (treat != null && Vector2.Distance(position, treat.transform.position) <= range) return treat;
            }
            return null;
        }

        private Treat FindNearestTreat(Vector2 position)
        {
            Treat nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (var treat in _treats)
            {
                if (treat == null) continue;
                float distance = Vector2.Distance(position, treat.transform.position);
                if (distance >= nearestDistance) continue;
                nearest = treat;
                nearestDistance = distance;
            }
            return nearest;
        }

        private void UpdateObjectiveArrows()
        {
            if (ObjectiveArrows == null) return;

            for (int i = 0; i < ObjectiveArrows.Length; i++)
            {
                var arrow = ObjectiveArrows[i];
                if (arrow == null) continue;
                if (TryGetObjectiveTarget(i, out var target, out var copy, out var hideDistance))
                    arrow.PointAt(target, copy, hideDistance);
                else
                    arrow.Hide();
            }
        }

        private void UpdateTravelAssists()
        {
            if (_dogs == null) return;
            for (int i = 0; i < _dogs.Length; i++)
            {
                var dog = _dogs[i];
                if (dog == null) continue;
                bool active = false;
                if (TryGetObjectiveTarget(i, out var target, out _, out _) && target != null)
                {
                    float distance = Vector2.Distance(dog.transform.position, target.position);
                    float threshold = dog.TravelAssist
                        ? _tuning.TravelAssistReleaseDistance
                        : _tuning.TravelAssistEngageDistance;
                    active = distance > threshold;
                }
                dog.SetTravelAssist(active, _tuning.TravelAssistSpeedMultiplier);
            }
        }

        private bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 0.9f;

            if (!MissionActive() || dogIndex < 0 || dogIndex >= _dogs.Length) return false;

            if (Phase == State.PredatorAttack && _grabbedDog >= 0)
            {
                if (dogIndex == _grabbedDog)
                {
                    int partner = _grabbedDog == 0 ? 1 : 0;
                    target = _dogs[partner].transform;
                    copy = "PARTNER BARK";
                    hideDistance = 0.4f;
                    return true;
                }

                target = _dogs[_grabbedDog].transform;
                copy = "BARK RESCUE";
                hideDistance = 1.8f;
                return true;
            }

            if (Phase == State.PredatorWarning)
            {
                int partner = dogIndex == 0 ? 1 : 0;
                target = _dogs[partner].transform;
                copy = "HUDDLE + BARK";
                hideDistance = 1.6f;
                return true;
            }

            if (TryGetProductionMissionTarget(dogIndex, out target, out copy, out hideDistance))
                return true;

            if (_mission.UsesSquirrel && _squirrelTarget != null)
            {
                target = SquirrelObject.transform;
                copy = "BARK SQUIRREL";
                hideDistance = 2.2f;
                return true;
            }

            if (_mission.RequiresTug && !TugComplete && BreakfastRecovered >= Mathf.Max(2, recoveryGoal / 2))
            {
                target = RopeObject.transform;
                copy = "BOTH TUG";
                hideDistance = 1.7f;
                return true;
            }

            var nearestTreat = FindNearestTreat(_dogs[dogIndex].transform.position);
            if (nearestTreat == null) return false;

            target = nearestTreat.transform;
            copy = _mission.ItemArrowLabel;
            hideDistance = 1.2f;
            return true;
        }

        private bool TryGetProductionMissionTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 1.4f;

            if (_activeMissionController != null)
                return _activeMissionController.TryGetObjectiveTarget(dogIndex, out target, out copy, out hideDistance);

            switch (_mission.Variant)
            {
                case MissionVariant.BackyardRescue:
                    if (_backyardTrapState.Complete) return false;
                    if (_backyardTrapState.WeenieDropped)
                    {
                        target = _backyardDroppedWeenie != null ? _backyardDroppedWeenie.transform : null;
                        copy = IndexOfDog(_backyardTrapState.RecoveryDog) == dogIndex ? "RECOVER DROP" : "PARTNER ONLY";
                        hideDistance = 1.2f;
                    }
                    else if (_squirrelTarget != null)
                    {
                        bool pressureDog = IndexOfDog(_backyardTrapState.PressureDog) == dogIndex;
                        target = pressureDog
                            ? (SquirrelObject != null ? SquirrelObject.transform : null)
                            : (_backyardTrapGapMarker != null ? _backyardTrapGapMarker.transform : null);
                        copy = pressureDog ? "BARK PRESSURE" : "HOLD ESCAPE GAP";
                        hideDistance = pressureDog ? 2.2f : BackyardTrapGapRadius;
                    }
                    else return false;
                    break;
                case MissionVariant.SockPanic:
                    if (_sockBasketState.BasketOpen)
                    {
                        if (dogIndex == _sockBasketState.OpenerDogIndex)
                        {
                            target = _laundryBasketObject != null ? _laundryBasketObject.transform : null;
                            copy = "HOLD BASKET";
                            hideDistance = SockBasketInteractRange;
                        }
                        else
                        {
                            target = _exposedSock != null ? _exposedSock.transform : null;
                            copy = "DIVE FOR SOCK";
                            hideDistance = 1.2f;
                        }
                    }
                    else
                    {
                        target = _laundryBasketObject != null ? _laundryBasketObject.transform : null;
                        copy = "TIP BASKET";
                        hideDistance = SockBasketInteractRange;
                    }
                    break;
                case MissionVariant.SquirrelConspiracy:
                    if (_herdingState.StashRevealed)
                    {
                        target = SquirrelObject != null ? SquirrelObject.transform : null;
                        copy = "CRACK STASH";
                    }
                    else
                    {
                        int herder = ClosestDogIndex(SquirrelObject != null ? (Vector2)SquirrelObject.transform.position : Vector2.zero);
                        if (dogIndex == herder)
                        {
                            target = SquirrelObject != null ? SquirrelObject.transform : null;
                            copy = "BARK HERD";
                        }
                        else
                        {
                            int markerCount = _squirrelCutoffMarkers != null ? _squirrelCutoffMarkers.Length : 0;
                            int route = markerCount > 0 ? Mathf.Clamp(_herdingState.RouteIndex, 0, markerCount - 1) : 0;
                            target = markerCount > 0 && _squirrelCutoffMarkers[route] != null ? _squirrelCutoffMarkers[route].transform : null;
                            copy = "HOLD CUTOFF";
                            hideDistance = SquirrelCutoffRadius;
                        }
                    }
                    break;
                case MissionVariant.EagleShadowPanic:
                    if (_threatSweepState.RescueObjectiveActive && !_threatSweepState.RescueComplete)
                    {
                        // Cheddar is the one snatched (wiggle in place); Cocoa is pointed at the talons to pull.
                        if (IndexOfDog(DogId.Cheddar) == dogIndex)
                        {
                            target = null;
                            copy = "WIGGLE!";
                        }
                        else
                        {
                            target = SquirrelObject != null ? SquirrelObject.transform : null;
                            copy = "PULL HIM FREE";
                            hideDistance = EagleRescueRange;
                        }
                    }
                    else if (_threatSweepState.RescueComplete)
                    {
                        target = _dogs[dogIndex == 0 ? 1 : 0].transform;
                        copy = "HUDDLE + BARK";
                        hideDistance = 1.6f;
                    }
                    else
                    {
                        target = FindNearestActiveMarker(_eagleCoverMarkers, _dogs[dogIndex].transform.position);
                        copy = "HIDE HERE";
                    }
                    break;
                case MissionVariant.CoyotesFence:
                    if (_patrolState.ReadyForFinalPressure(CoyoteRequiredRepairs))
                    {
                        target = _dogs[dogIndex == 0 ? 1 : 0].transform;
                        copy = "UNITED BARK";
                        hideDistance = 1.6f;
                    }
                    else
                    {
                        target = SquirrelObject != null ? SquirrelObject.transform : null;
                        copy = _coyotePressureHeld ? "FILL DIRT" : "BARK COYOTE";
                    }
                    break;
                case MissionVariant.WeenieRoundup:
                    if (_dogCarrying != null && dogIndex < _dogCarrying.Length && _dogCarrying[dogIndex])
                    {
                        target = _bowlObject != null ? _bowlObject.transform : null;
                        copy = "HOME BOWL";
                    }
                    else
                    {
                        target = FindNearestActiveMarker(_weenieMarkers, _dogs[dogIndex].transform.position);
                        copy = "PICK UP WEENIE";
                    }
                    break;
                case MissionVariant.ScentSearch:
                    target = FindNearestActiveMarker(_digMarkers, _dogs[dogIndex].transform.position);
                    copy = "SNIFF PATCH";
                    break;
                case MissionVariant.ThunderstormComfort:
                    target = _dogs[dogIndex == 0 ? 1 : 0].transform;
                    copy = "COMFORT PARTNER";
                    hideDistance = 1.8f;
                    break;
                case MissionVariant.LeashWalk:
                    int checkpoint = _leashState.CheckpointIndex;
                    if (_leashCheckpointMarkers != null && checkpoint >= 0 && checkpoint < _leashCheckpointMarkers.Length && _leashCheckpointMarkers[checkpoint] != null)
                        target = _leashCheckpointMarkers[checkpoint].transform;
                    copy = "WALK TOGETHER";
                    hideDistance = CheckpointRange;
                    break;
                case MissionVariant.BoneRelay:
                    if (IndexOfDog(DogId.Cocoa) == dogIndex)
                    {
                        target = SquirrelObject != null ? SquirrelObject.transform : null; // the scent post Cocoa reads
                        copy = "SNIFF SCENT";
                        hideDistance = BoneScentRange;
                    }
                    else
                    {
                        int call = _boneRelay.RevealedTarget;
                        if (call >= 0 && _boneMounds != null && call < _boneMounds.Length && _boneMounds[call] != null)
                            target = _boneMounds[call].transform; // the mound Cocoa called
                        else
                            target = FindNearestActiveMarker(_boneMounds, _dogs[dogIndex].transform.position);
                        copy = call >= 0 ? "DIG THE CALL" : "WAIT FOR CALL";
                        hideDistance = BoneDigRange;
                    }
                    break;
                case MissionVariant.ChaosMachine:
                    if (!_chaos.Running)
                    {
                        target = PredatorObject != null ? PredatorObject.transform : null; // the lever
                        copy = "PULL LEVER";
                        hideDistance = ChaosLeverRange;
                    }
                    else
                    {
                        int stage = Mathf.Clamp(_chaos.Stage, 0, _chaosJunctionSpots.Length - 1);
                        target = _chaosJunctions != null && _chaosJunctions[stage] != null ? _chaosJunctions[stage].transform : null;
                        ChainActor owner = _chaosOwners[stage];
                        bool isOwner = (owner == ChainActor.Cheddar && IndexOfDog(DogId.Cheddar) == dogIndex)
                            || (owner == ChainActor.Cocoa && IndexOfDog(DogId.Cocoa) == dogIndex);
                        copy = isOwner ? "COVER JUNCTION" : "NEXT JUNCTION";
                        hideDistance = ChaosJunctionRange;
                    }
                    break;
                case MissionVariant.BlanketCatch:
                    target = _blanketFallingItem != null && _blanketFallingItem.activeSelf ? _blanketFallingItem.transform : null;
                    copy = "GET UNDER SNACK";
                    hideDistance = BlanketCatchTolerance;
                    break;
                case MissionVariant.CarRide:
                default:
                    return false;
            }

            return target != null;
        }

        private static Transform FindNearestActiveMarker(GameObject[] markers, Vector2 position)
        {
            Transform nearest = null;
            float nearestDistance = float.PositiveInfinity;
            if (markers == null) return null;
            foreach (var marker in markers)
            {
                if (marker == null || !marker.activeSelf) continue;
                float distance = Vector2.Distance(position, marker.transform.position);
                if (distance >= nearestDistance) continue;
                nearest = marker.transform;
                nearestDistance = distance;
            }
            return nearest;
        }

        private int ClosestDogIndex(Vector2 position)
        {
            int best = 0;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < _dogs.Length; i++)
            {
                if (_dogs[i] == null) continue;
                float distance = Vector2.Distance(_dogs[i].transform.position, position);
                if (distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }

        private void StageDogsForMissionEntry()
        {
            if (_activeMissionController != null)
            {
                _missionEntryTarget = _activeMissionController.EntryTarget;
                _activeMissionController.StageDogsForEntry();
                return;
            }

            _missionEntryTarget = ResolveMissionEntryTarget();
            Vector2 inward = (_bounds.center - _missionEntryTarget).normalized;
            if (inward.sqrMagnitude < 0.01f) inward = Vector2.down;
            Vector2 center = _missionEntryTarget + inward * 7f;
            Vector2 side = new Vector2(-inward.y, inward.x) * 1.5f;

            for (int i = 0; i < _dogs.Length; i++)
            {
                Vector2 offset = i % 2 == 0 ? -side : side;
                Vector2 position = ClampInsideBounds(center + offset, 1.5f);
                _dogs[i].transform.position = position;
                if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            }
        }

        private Vector2 ResolveMissionEntryTarget()
        {
            switch (_mission.Variant)
            {
                case MissionVariant.SquirrelConspiracy: return _squirrelRoute[0];
                case MissionVariant.SockPanic: return _laundryBasketObject != null ? (Vector2)_laundryBasketObject.transform.position : _bounds.center;
                case MissionVariant.EagleShadowPanic: return _eagleCoverZones[0];
                case MissionVariant.CoyotesFence: return _fenceGapPosition;
                case MissionVariant.WeenieRoundup: return _weenieSpots[0];
                case MissionVariant.ScentSearch: return _digSpots[0];
                case MissionVariant.ThunderstormComfort:
                case MissionVariant.CarRide: return _bounds.center;
                case MissionVariant.LeashWalk: return _leashCheckpoints[0];
                case MissionVariant.BlanketCatch: return new Vector2(0f, BlanketCatchLineY);
                default:
                    var nearestTreat = FindNearestTreat(_bounds.center);
                    return nearestTreat != null ? (Vector2)nearestTreat.transform.position : _bounds.center;
            }
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin)
        {
            return new Vector2(
                Mathf.Clamp(point.x, _bounds.xMin + margin, _bounds.xMax - margin),
                Mathf.Clamp(point.y, _bounds.yMin + margin, _bounds.yMax - margin));
        }

        private void HideObjectiveArrows()
        {
            if (ObjectiveArrows == null) return;
            foreach (var arrow in ObjectiveArrows)
            {
                if (arrow != null) arrow.Hide();
            }
        }

        private void UpdateInteractionRanges()
        {
            if (InteractionRangeIndicators == null) return;

            HideInteractionRanges();

            if (!MissionActive()) return;

            if (_mission.UsesSquirrel && _squirrelTarget != null && SquirrelObject != null)
            {
                var squirrelRange = SquirrelObject.GetComponent<InteractionRangeIndicator>();
                if (squirrelRange != null)
                    squirrelRange.Show(_tuning.SquirrelRangeIndicatorRadius, "BARK RANGE", new Color(1f, 0.92f, 0.35f, 0.36f));
            }

            if (_mission.RequiresTug && !TugComplete && RopeObject != null &&
                BreakfastRecovered >= Mathf.Max(2, recoveryGoal / 2))
            {
                var tugRange = RopeObject.GetComponent<InteractionRangeIndicator>();
                if (tugRange != null)
                    tugRange.Show(_tuning.TugRangeIndicatorRadius, "BOTH DOGS", new Color(1f, 0.82f, 0.22f, 0.34f));
            }

            if (_grabbedDog >= 0 && _grabbedDog < _dogs.Length)
            {
                var rescueRange = InteractionRangeIndicators[_grabbedDog];
                if (rescueRange != null)
                    rescueRange.Show(_tuning.RescueRangeIndicatorRadius, "RESCUE BARK", new Color(0.55f, 1f, 0.7f, 0.42f));
            }
        }

        private void HideInteractionRanges()
        {
            if (InteractionRangeIndicators == null) return;
            foreach (var indicator in InteractionRangeIndicators)
            {
                if (indicator != null) indicator.Hide();
            }
        }

        private void SpawnTreat()
        {
            const float margin = 1.2f;
            float x = Mathf.Lerp(_bounds.xMin + margin, _bounds.xMax - margin, (float)_rng.NextDouble());
            float y = Mathf.Lerp(_bounds.yMin + margin, _bounds.yMax - margin, (float)_rng.NextDouble());

            var go = new GameObject(_mission.ItemObjectName);
            go.transform.SetParent(_treatRoot);
            go.transform.position = new Vector3(x, y, 0f);
            var art = ArenaArtCatalog.Collectible(_mission.Variant);
            go.transform.localScale = art.RootScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = _mission.ItemColor;
            sr.sortingOrder = 5;
            BuildCollectibleArt(go, art);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var treat = go.AddComponent<Treat>();
            treat.Bind(this);
            _treats.Add(treat);

            AddWorldLabel(go, _mission.ItemWorldLabel, Vector3.up * 2.2f, 16, Color.white);
            if (_mission.Variant == MissionVariant.SockPanic) go.SetActive(false);
        }

        private void BuildCollectibleArt(GameObject go, CollectibleVisualSlot art)
        {
            foreach (var part in art.Parts)
            {
                AddActorPart(go, part, _sprite,
                    part.ResolveColor(_mission.ItemColor, _mission.ItemAccentColor, _mission.ItemSecondaryColor));
            }
        }

        private void ClearTreats()
        {
            foreach (var treat in _treats)
            {
                if (treat != null) Destroy(treat.gameObject);
            }
            _treats.Clear();
        }

        private void BuildMissionObjects()
        {
            SquirrelObject = MakeActor(ArenaArtCatalog.Actor(ArenaArtCatalog.ActorKind.Squirrel));
            PredatorObject = MakeActor(ArenaArtCatalog.Actor(ArenaArtCatalog.ActorKind.Predator));
            RopeObject = MakeActor(ArenaArtCatalog.Actor(ArenaArtCatalog.ActorKind.Rope));
            _laundryBasketObject = MakeActor(ArenaArtCatalog.Actor(ArenaArtCatalog.ActorKind.LaundryBasket));
            _laundryBasketObject.SetActive(false);
            _bunnyCameoObject = MakeDraftBunnyCameo();
            BuildBackyardTrapGapMarker();
            BuildEagleCoverMarkers();
            BuildSquirrelCutoffMarkers();
            BuildCoyoteGapMarkers();
            BuildWeenieRoundupObjects();
            BuildScentSearchMarkers();
            BuildBoneRelayMarkers();
            BuildChaosJunctions();
            BuildBlanketObjects();
            BuildLeashCheckpointMarkers();
            if (InteractionRangeIndicators != null)
            {
                int offset = _dogs != null ? _dogs.Length : 0;
                if (offset < InteractionRangeIndicators.Length) InteractionRangeIndicators[offset] = SquirrelObject.GetComponent<InteractionRangeIndicator>();
                if (offset + 1 < InteractionRangeIndicators.Length) InteractionRangeIndicators[offset + 1] = PredatorObject.GetComponent<InteractionRangeIndicator>();
                if (offset + 2 < InteractionRangeIndicators.Length) InteractionRangeIndicators[offset + 2] = RopeObject.GetComponent<InteractionRangeIndicator>();
            }
        }

        private GameObject MakeActor(ActorVisualSlot art)
        {
            var go = new GameObject(art.ObjectName);
            go.transform.localScale = Vector3.one * art.RootScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = art.RootColor;
            sr.sortingOrder = 6;

            BuildActorArt(go, art, sr);
            AddWorldLabel(go, art.Label, art.LabelOffset, 24, Color.white);
            go.AddComponent<MissionActorFeedback>().Init(sr, art.Label, art.PulseAmount, art.RotationPerSecond);
            var range = go.AddComponent<InteractionRangeIndicator>();
            range.Init(_rangeSprite, new Color(1f, 1f, 1f, 0.35f), "RANGE");
            return go;
        }

        private void BuildActorArt(GameObject go, ActorVisualSlot art, SpriteRenderer root)
        {
            root.transform.localScale = art.BodyScale;
            foreach (var part in art.Parts)
            {
                AddActorPart(go, part, _sprite, part.Color);
            }
            AddDraftActorBadges(go, art);
        }

        private void AddDraftActorBadges(GameObject go, ActorVisualSlot art)
        {
            for (int i = 0; i < art.DraftSprites.Length; i++)
            {
                var id = art.DraftSprites[i];
                string name = id switch
                {
                    ArenaDraftArt.SpriteId.SquirrelCharacter => ArenaDraftArt.SquirrelBadgeName,
                    ArenaDraftArt.SpriteId.EagleReference => ArenaDraftArt.EagleBadgeName,
                    ArenaDraftArt.SpriteId.CoyoteReference => ArenaDraftArt.CoyoteBadgeName,
                    ArenaDraftArt.SpriteId.BackyardProps => ArenaDraftArt.BackyardPropsBadgeName,
                    _ => $"DraftArtBadge_{id}"
                };
                Vector3 position = id switch
                {
                    ArenaDraftArt.SpriteId.EagleReference => new Vector3(-0.82f, 0.12f, 0.05f),
                    ArenaDraftArt.SpriteId.CoyoteReference => new Vector3(0.82f, 0.12f, 0.05f),
                    ArenaDraftArt.SpriteId.BackyardProps => new Vector3(0f, -0.02f, 0.05f),
                    _ => new Vector3(0f, 0f, 0.05f)
                };
                Vector3 scale = id == ArenaDraftArt.SpriteId.BackyardProps
                    ? new Vector3(0.055f, 0.055f, 1f)
                    : new Vector3(0.09f, 0.09f, 1f);
                ArenaDraftArt.AddSpriteBadge(go.transform, name, id, position, scale, 4,
                    new Color(1f, 1f, 1f, 0.72f));
            }
        }

        private GameObject MakeDraftBunnyCameo()
        {
            var go = new GameObject(ArenaDraftArt.BunnyCameoName);
            go.transform.localScale = Vector3.one * 0.65f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.78f, 0.54f, 0.32f, 0.48f);
            sr.sortingOrder = 2;
            sr.transform.localScale = new Vector3(0.52f, 0.32f, 1f);

            AddActorPart(go, new PartSlot("BunnyCameoEarA", new Color(0.9f, 0.7f, 0.45f, 0.6f), new Vector3(-0.16f, 0.26f, -0.02f), new Vector3(0.13f, 0.42f, 1f), 3), _sprite, new Color(0.9f, 0.7f, 0.45f, 0.6f));
            AddActorPart(go, new PartSlot("BunnyCameoEarB", new Color(0.9f, 0.7f, 0.45f, 0.6f), new Vector3(0.1f, 0.3f, -0.02f), new Vector3(0.12f, 0.46f, 1f), 3), _sprite, new Color(0.9f, 0.7f, 0.45f, 0.6f));
            ArenaDraftArt.AddSpriteBadge(go.transform, "DraftBunnyReferenceBadge",
                ArenaDraftArt.SpriteId.BunnyReference, new Vector3(0f, 0.05f, 0.04f),
                new Vector3(0.075f, 0.075f, 1f), 3, new Color(1f, 1f, 1f, 0.7f));
            return go;
        }

        private static SpriteRenderer AddActorPart(GameObject parent, PartSlot part, Sprite sprite, Color color)
        {
            var go = new GameObject(part.Name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = part.LocalPosition;
            go.transform.localScale = part.LocalScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = part.SortingOrder;
            return sr;
        }

        private static TextMesh AddWorldLabel(GameObject parent, string text, Vector3 offset, int size, Color color)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(parent.transform);
            labelGo.transform.localPosition = offset;
            labelGo.transform.localRotation = Quaternion.identity;
            labelGo.transform.localScale = Vector3.one * 0.08f;

            var label = labelGo.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            return label;
        }

        private static void SetActorState(GameObject go, string label, Color color, float pulse)
        {
            if (go == null) return;
            if (go.TryGetComponent<MissionActorFeedback>(out var feedback)) feedback.SetState(label, color, pulse);
        }

        private static void Pulse(GameObject go, float amount)
        {
            if (go != null && go.TryGetComponent<MissionActorFeedback>(out var feedback)) feedback.Pulse(amount);
        }

        private void SpawnWorldPop(Vector3 position, string text, Color color)
        {
            var art = ArenaArtCatalog.WorldPop;
            var go = new GameObject($"{art.NamePrefix}_{text.Replace(" ", "_").Replace("!", string.Empty).Replace("+", "PLUS").Replace("-", "MINUS")}");
            go.transform.position = position + art.SpawnOffset;
            var label = go.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = art.FontSize;
            label.characterSize = 0.08f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            go.AddComponent<MissionWorldPop>().Begin(label);
        }

        private void PlaceObject(GameObject go, Vector2 position)
        {
            if (go != null) go.transform.position = position;
        }

        private string DogName(DogController dog)
        {
            if (dog == null) return "A dog";
            var id = dog.GetComponent<DogIdentity>();
            return id != null ? id.Id.ToString() : dog.name;
        }

        private void BuildAudio()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.volume = 1f;

            foreach (var cue in ArenaFeedbackCatalog.RequiredAudioCues)
                _audioClips[cue.Name] = MakePlaceholderClip(cue);

            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = 0.32f;
            _music.clip = MakeBackyardMusicLoop();
            _music.Play();
        }

        private static AudioClip MakeBackyardMusicLoop()
        {
            const int sampleRate = 22050;
            const float secondsPerBeat = 0.6f;
            const int beats = 16;
            int sampleCount = Mathf.CeilToInt(sampleRate * secondsPerBeat * beats);
            var samples = new float[sampleCount];
            int[] melodyMidi = { 72, 76, 79, 76, 74, 77, 81, 77, 72, 76, 79, 83, 81, 79, 76, 74 };
            int[] bassMidi = { 48, 48, 53, 53, 45, 45, 55, 55 };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                int beat = Mathf.Min(beats - 1, Mathf.FloorToInt(t / secondsPerBeat));
                float beatT = (t - beat * secondsPerBeat) / secondsPerBeat;
                float melodyHz = 440f * Mathf.Pow(2f, (melodyMidi[beat] - 69) / 12f);
                float bassHz = 440f * Mathf.Pow(2f, (bassMidi[beat / 2] - 69) / 12f);
                float pluck = Mathf.Sin(2f * Mathf.PI * melodyHz * t) * Mathf.Exp(-3.8f * beatT);
                float bass = Mathf.Sin(2f * Mathf.PI * bassHz * t) * (0.55f + 0.45f * Mathf.Cos(Mathf.PI * beatT));
                float tailFade = i > sampleCount - sampleRate / 20
                    ? (sampleCount - i) / (sampleRate / 20f)
                    : 1f;
                samples[i] = (pluck * 0.09f + bass * 0.045f) * tailFade;
            }

            var clip = AudioClip.Create(ArenaFeedbackCatalog.BackyardMusicLoop, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip MakePlaceholderClip(AudioCueSlot cue)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * cue.Seconds);
            var samples = new float[sampleCount];
            uint noise = 2166136261u;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (i / (float)sampleCount);
                float wave;
                if (cue.Wave == ArenaFeedbackCatalog.PlaceholderWave.Noise)
                {
                    noise ^= (uint)(i + cue.Name.Length * 31);
                    noise *= 16777619u;
                    wave = ((noise & 1023u) / 511.5f) - 1f;
                }
                else
                {
                    wave = Mathf.Sin(2f * Mathf.PI * cue.Frequency * t);
                }
                samples[i] = wave * envelope * cue.Volume;
            }

            var clip = AudioClip.Create(cue.Name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void RequestAudioCue(string cueName)
        {
            if (!AudioEnabled || string.IsNullOrEmpty(cueName)) return;
            if (!_audioSlots.ContainsKey(cueName)) return;

            LastAudioCueRequested = cueName;
            _audioCueRequests.Add(cueName);
            if (_audio != null && _audioClips.TryGetValue(cueName, out var clip) && clip != null)
                _audio.PlayOneShot(clip);
        }

        private void RequestRumble(string requestName, float lowFrequency, float highFrequency, float seconds)
        {
            if (!RumbleEnabled || string.IsNullOrEmpty(requestName)) return;

            LastRumbleRequested = requestName;
            _rumbleRequests.Add(requestName);

            var pad = Gamepad.current;
            if (pad == null) return;

            pad.SetMotorSpeeds(lowFrequency, highFrequency);
            CancelInvoke(nameof(StopRumble));
            Invoke(nameof(StopRumble), Mathf.Max(0.01f, seconds));
        }

        private void StopRumble()
        {
            var pad = Gamepad.current;
            if (pad != null) pad.SetMotorSpeeds(0f, 0f);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            StopRumble();
            if (_dogs == null) return;
            foreach (var dog in _dogs)
            {
                if (dog == null) continue;
                dog.OnBark -= OnDogBarked;
                dog.OnInteract -= OnDogInteracted;
            }
        }
    }

    /// <summary>Lightweight placeholder polish: readable world label + idle pulse/rotation.</summary>
    public sealed class MissionActorFeedback : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private TextMesh _label;
        private Vector3 _baseScale;
        private float _pulseAmount;
        private Quaternion _baseRotation;
        private float _swaySpeed;
        private float _swayAmplitudeDegrees;
        private float _swayPhase;

        public string Label => _label != null ? _label.text : string.Empty;

        public void Init(SpriteRenderer renderer, string label, float pulseAmount, Vector3 rotationPerSecond)
        {
            _renderer = renderer;
            _label = GetComponentInChildren<TextMesh>();
            _baseScale = transform.localScale;
            _pulseAmount = pulseAmount;
            _baseRotation = transform.localRotation;

            // The old behavior spun props continuously (squirrel 80deg/s, rope 45deg/s), which made
            // them read as unidentifiable rotating blobs. Reinterpret that authored "spin" intent as a
            // small, bounded life-wobble around the resting pose so the silhouette stays recognizable.
            float spin = rotationPerSecond.magnitude;
            if (spin > 0.01f)
            {
                _swaySpeed = Mathf.Clamp(spin * 0.03f, 1.2f, 3f);
                _swayAmplitudeDegrees = Mathf.Min(7f, spin * 0.12f);
                // Deterministic per-object phase so wobbles desync without runtime RNG.
                _swayPhase = (GetInstanceID() & 1023) / 1023f * Mathf.PI * 2f;
            }

            SetState(label, renderer != null ? renderer.color : Color.white, pulseAmount);
        }

        public void SetState(string label, Color color, float pulseAmount)
        {
            if (_label != null) _label.text = label;
            if (_renderer != null) _renderer.color = color;
            _pulseAmount = pulseAmount;
        }

        public void Pulse(float amount)
        {
            _pulseAmount = Mathf.Max(_pulseAmount, amount);
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * _pulseAmount;
            transform.localScale = _baseScale * pulse;
            if (_swayAmplitudeDegrees > 0f)
            {
                float angle = Mathf.Sin(Time.time * _swaySpeed + _swayPhase) * _swayAmplitudeDegrees;
                transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
            }
            if (_label != null) _label.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>Short-lived world text for score, rescue, tug, and miss moments.</summary>
    public sealed class MissionWorldPop : MonoBehaviour
    {
        private TextMesh _label;
        private float _t;

        public string Label => _label != null ? _label.text : string.Empty;
        public float StrategicScale { get; private set; } = 1f;

        public void Begin(TextMesh label) => _label = label;

        private void Update()
        {
            _t += Time.deltaTime;
            var art = ArenaArtCatalog.WorldPop;
            transform.position += Vector3.up * (Time.deltaTime * art.RiseSpeed);
            float popScale = 1f + Mathf.Sin(Mathf.Clamp01(_t / art.LifeSeconds) * Mathf.PI) * art.PopScaleAmount;
            StrategicScale = Camera.main != null
                ? Mathf.Clamp(Camera.main.orthographicSize / 7.5f, 1f, 3.2f)
                : 1f;
            transform.localScale = Vector3.one * (popScale * StrategicScale);

            if (_label != null)
            {
                _label.transform.rotation = Quaternion.identity;
                Color c = _label.color;
                c.a = Mathf.Lerp(1f, 0f, _t / art.LifeSeconds);
                _label.color = c;
            }

            if (_t >= art.LifeSeconds) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Generated ring + label used only when a bark, tug, or rescue range is actionable.
    /// </summary>
    public sealed class InteractionRangeIndicator : MonoBehaviour
    {
        private GameObject _root;
        private GameObject _labelRoot;
        private SpriteRenderer _ring;
        private TextMesh _label;
        private Color _baseColor = Color.white;
        private float _radius = 1f;

        public bool IsVisible => _root != null && _root.activeSelf;
        public float Radius => _radius;
        public string Label => IsVisible && _label != null ? _label.text : string.Empty;

        public void Init(Sprite ringSprite, Color color, string label)
        {
            _baseColor = color;

            _root = new GameObject("InteractionRangeIndicator");
            _root.transform.SetParent(transform);
            _root.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            _root.transform.localRotation = Quaternion.identity;

            _ring = _root.AddComponent<SpriteRenderer>();
            _ring.sprite = ringSprite;
            _ring.sortingOrder = 3;

            _labelRoot = new GameObject("InteractionRangeLabel");
            _labelRoot.transform.SetParent(transform);
            _labelRoot.transform.localPosition = new Vector3(0f, 1.45f, -0.02f);
            _labelRoot.transform.localScale = Vector3.one * 0.08f;
            _label = _labelRoot.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 18;

            Show(1f, label, color);
            Hide();
        }

        public void Show(float radius, string label, Color color)
        {
            if (_root == null) return;

            _radius = Mathf.Max(0.1f, radius);
            _baseColor = color;
            _root.SetActive(true);
            if (_labelRoot != null) _labelRoot.SetActive(true);
            _root.transform.localScale = Vector3.one * (_radius * 2f);
            if (_labelRoot != null) _labelRoot.transform.localPosition = new Vector3(0f, _radius + 0.45f, -0.02f);

            if (_ring != null) _ring.color = _baseColor;
            if (_label != null)
            {
                _label.text = label;
                Color labelColor = color;
                labelColor.a = 1f;
                _label.color = labelColor;
            }
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
            if (_labelRoot != null) _labelRoot.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!IsVisible) return;

            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.035f;
            _root.transform.localScale = Vector3.one * (_radius * 2f * pulse);
            if (_labelRoot != null) _labelRoot.transform.localPosition = new Vector3(0f, _radius + 0.45f, -0.02f);
            if (_label != null) _label.transform.rotation = Quaternion.identity;
        }
    }
}
