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
    public sealed partial class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Core actions taught by the first-play Backyard Rescue onboarding. <see cref="Complete"/>
        /// is a terminal display state, not an action a dog can perform.
        /// </summary>
        public enum TutorialActionStep
        {
            Bark,
            Interact,
            Jump,
            Wrestle,
            Complete
        }

        // Nested enums and the MissionDefinition data type live in GameManager.Types.cs (same
        // partial class) to keep this controller file focused on runtime behavior.

        private static readonly MissionVariant[] MissionOrder =
        {
            // Showcase order follows the strongest current couch-ready slices. Keep the first five
            // intentional: they receive the most cold-start traffic and are the owner's quality bar.
            MissionVariant.OperationPeeBreak,
            MissionVariant.KitchenFoodFrenzy,
            MissionVariant.CarRide,
            MissionVariant.BabyBirdBedlam,
            MissionVariant.GateCrash,
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
            MissionVariant.TableStealth,
            MissionVariant.SquirrelSwitcheroo,
            MissionVariant.WalkCampaign,
            MissionVariant.BoneRelay,
            MissionVariant.GreatEscape,
            MissionVariant.ChaosMachine,
            MissionVariant.BlanketCatch
        };

        [Header("Mission selection")]
        [SerializeField] private MissionVariant startingMission = MissionVariant.OperationPeeBreak;

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

        // Mission select renders as a paged picture-tile grid (row-major, 4x3 per page). The grid
        // shape lives here, not in the view, so directional navigation and the UGUI screen can
        // never disagree about which tile is next.
        public const int MissionSelectGridColumns = 4;
        public const int MissionSelectGridRowsPerPage = 3;
        public const int MissionSelectTilesPerPage = MissionSelectGridColumns * MissionSelectGridRowsPerPage;
        public int MissionSelectPageCount =>
            (MissionOrder.Length + MissionSelectTilesPerPage - 1) / MissionSelectTilesPerPage;
        public int SelectedMissionPage => _selectedMissionIndex / MissionSelectTilesPerPage;
        public SnackHeistMissionController SnackHeistController => _activeMissionController as SnackHeistMissionController;
        public SquirrelConspiracyMissionController SquirrelConspiracyController => _activeMissionController as SquirrelConspiracyMissionController;
        public HerdingMissionState SquirrelConspiracyState => SquirrelConspiracyController?.State ?? _emptyHerdingState;
        public Vector2[] SquirrelRouteNodes => SquirrelConspiracyMissionController.ComputeRoute(_bounds);
        public Vector2[] SquirrelCutoffZones => SquirrelConspiracyMissionController.ComputeCutoffZones(_bounds);
        public Vector2 ActiveSquirrelCutoffZone => SquirrelConspiracyController?.ActiveCutoffZone ?? SquirrelCutoffZones[0];
        public ThreatSweepMissionState EagleShadowPanicState => EagleShadowController?.SweepState ?? _emptyThreatSweepState;
        public CoopRescueTimingPuzzle EagleRescuePuzzle => EagleShadowController?.RescuePuzzle ?? _emptyEagleRescuePuzzle;
        public Vector2 EagleSnatchPosition => EagleShadowController?.SnatchPosition ?? default;
        public PatrolDefenseMissionState CoyotesFenceState => CoyotesFenceController?.State ?? _emptyPatrolState;
        public Vector2[] EagleCoverZones => EagleShadowController?.CoverZones ?? EagleShadowPanicMissionController.ComputeCoverZones(_bounds);
        public string EagleCoverArtResourcePath(int index) => EagleShadowController?.CoverResourcePathAt(index) ?? string.Empty;
        public Vector2[] FenceGaps => CoyotesFenceController?.Gaps ?? CoyotesFenceMissionController.ComputeFenceGaps(_bounds);
        public string CoyoteGapArtResourcePath(int index) => CoyotesFenceController?.GapResourcePathAt(index) ?? string.Empty;
        public WeenieRoundupMissionController WeenieRoundupController => _activeMissionController as WeenieRoundupMissionController;
        public CarryRoundupMissionState WeenieRoundupState => WeenieRoundupController?.State ?? _emptyCarryState;
        public Vector2 BowlPosition => WeenieRoundupController?.BowlPosition ?? _bounds.center;
        public ScentSearchMissionController ScentSearchController => _activeMissionController as ScentSearchMissionController;
        public ScentSearchMissionState ScentSearchState => ScentSearchController?.State ?? _emptyScentState;
        public Vector2[] DigSpots => ScentSearchMissionController.ComputeDigSpots(_bounds);
        public PanicMeter Panic => _panic;
        /// <summary>Current stall-escalation tier (0 Discovery - 3 Rescue).</summary>
        public int GuidanceTier => _guidance.Tier;
        public float GuidanceStallSeconds => _guidance.StallSeconds;
        /// <summary>
        /// The dog whose objective target this frame is unambiguous (the other dog has no target of
        /// its own). Null whenever both dogs have a target - most missions hand a target to both dogs
        /// at once (with different copy telling one to stand down), which this deliberately does not
        /// try to disambiguate by parsing copy text; see G1.2 in docs/AGENT-WORK-QUEUE-PRELAUNCH.md.
        /// </summary>
        public int? GuidanceOwningDogIndex => _guidanceOwningDogIndex;
        /// <summary>Tier 2+: the non-owning dog's index, to pulse their HUD identity chip. Null unless GuidanceOwningDogIndex is known.</summary>
        public int? GuidancePartnerDogIndex => GuidanceTier >= 2 && _guidanceOwningDogIndex.HasValue
            ? (_guidanceOwningDogIndex.Value == 0 ? 1 : 0)
            : (int?)null;
        /// <summary>Tier 3: flash the HUD objective line.</summary>
        public bool GuidanceRescueActive => GuidanceTier >= 3;
        /// <summary>Tier 3: the owning dog's name to prefix onto the flashed objective line, or empty if unknown.</summary>
        public string GuidanceRescueDogName => _guidanceOwningDogIndex.HasValue && _dogs != null &&
            _guidanceOwningDogIndex.Value < _dogs.Length
                ? DogName(_dogs[_guidanceOwningDogIndex.Value])
                : string.Empty;
        /// <summary>Role-turn beacon (G1.3): whether the paw badge is currently showing over the objective target.</summary>
        public bool GuidanceBeaconVisible => _roleTurnBeacon != null && _roleTurnBeacon.IsShowing;
        public Color GuidanceBeaconTint => _roleTurnBeacon != null ? _roleTurnBeacon.CurrentTint : Color.white;
        /// <summary>Handoff flip flourish (G1.4): a one-shot flash on both HUD identity chips when the active role passes between dogs.</summary>
        public bool HandoffChipFlashVisible => Time.time < _handoffFlashUntil;
        public DogId? LastHandoffFromDog { get; private set; }
        public DogId? LastHandoffToDog { get; private set; }
        public int HandoffSignalCount { get; private set; }
        public MarkTheYardMissionController MarkTheYardController => _activeMissionController as MarkTheYardMissionController;
        public TerritoryMissionState MarkTheYardState => MarkTheYardController?.State ?? _emptyTerritoryState;
        public Vector2[] TerritoryZones => MarkTheYardMissionController.ComputeZones(_bounds);
        public Vector2[] LeashCheckpoints => LeashWalkMissionController.ComputeCheckpoints(_bounds);
        public CarRideMissionController CarRideController => _activeMissionController as CarRideMissionController;
        public CarRideMissionState CarRideState => CarRideController?.State ?? _emptyCarState;
        public SockPanicMissionController SockPanicController => _activeMissionController as SockPanicMissionController;
        public SockBasketMissionState SockPanicState => SockPanicController?.State ?? _emptySockBasketState;
        public BackyardRescueMissionController BackyardRescueController => _activeMissionController as BackyardRescueMissionController;
        public BackyardSquirrelTrapState BackyardTrapState => BackyardRescueController?.TrapState;
        public Vector2 BackyardTrapGapPosition => BackyardRescueController?.GapPosition ?? Vector2.zero;
        public Treat BackyardDroppedWeenie => BackyardRescueController?.DroppedWeenie;
        public KitchenFoodFrenzyMissionState KitchenState => KitchenController?.State;
        public Vector2 KitchenCounterPosition => KitchenController?.CounterPosition ?? Vector2.zero;
        public Vector2 KitchenSafeZonePosition => KitchenController?.SafeZonePosition ?? Vector2.zero;
        public GameObject KitchenFoodObject => KitchenController?.FoodObject;
        public GameObject KitchenTelegraphObject => KitchenController?.TelegraphObject;
        public GameObject KitchenLandingWarningObject => KitchenController?.LandingWarningObject;
        public PeeBreakMissionController PeeBreakController => _activeMissionController as PeeBreakMissionController;
        public IMissionController ActiveMissionController => _activeMissionController;
        public GameObject LaundryBasketObject => SockPanicController?.BasketObject;
        public Treat ExposedSock => SockPanicController?.ExposedSock;
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
        public BoneRelayMissionController BoneRelayController => _activeMissionController as BoneRelayMissionController;
        public CoopScentRelayPuzzle BoneRelayPuzzle => BoneRelayController?.Puzzle ?? _emptyBoneRelayPuzzle;
        public int BoneMoundCount => BoneRelayController?.MoundCount ?? 0;
        public Vector2 BoneScentZone => BoneRelayController?.ScentZone ?? Vector2.zero;
        public Vector2 BoneMoundSpot(int index) => BoneRelayController?.MoundSpot(index) ?? Vector2.zero;
        public GreatEscapeMissionController GreatEscapeController => _activeMissionController as GreatEscapeMissionController;
        public CoopSequenceChainPuzzle GreatEscapePuzzle => GreatEscapeController?.Puzzle ?? _emptyEscapePuzzle;
        public int EscapeStationCount => GreatEscapeController?.StationCount ?? 0;
        public Vector2 EscapeStationSpot(int index) => GreatEscapeController?.StationSpot(index) ?? Vector2.zero;
        public ChainActor EscapeStationOwner(int index) => GreatEscapeController?.StationOwner(index) ?? ChainActor.Either;
        public ChaosMachineMissionController ChaosMachineController => _activeMissionController as ChaosMachineMissionController;
        public CoopChaosMachinePuzzle ChaosMachinePuzzle => ChaosMachineController?.Puzzle ?? _emptyChaosJunctionPuzzle;
        public int ChaosJunctionCount => ChaosMachineController?.JunctionCount ?? 0;
        public Vector2 ChaosLeverZone => ChaosMachineController?.LeverZone ?? Vector2.zero;
        public Vector2 ChaosJunctionSpot(int index) => ChaosMachineController?.JunctionSpot(index) ?? Vector2.zero;
        public ChainActor ChaosJunctionOwner(int index) => ChaosMachineController?.JunctionOwner(index) ?? ChainActor.Either;
        public BlanketCatchMissionController BlanketCatchController => _activeMissionController as BlanketCatchMissionController;
        public CoopStretchSpanPuzzle BlanketPuzzle => BlanketCatchController?.Puzzle ?? _emptyBlanketPuzzle;
        public float BlanketCatchY => BlanketCatchController?.CatchY ?? -6f;
        public BabyBirdBedlamMissionController BabyBirdBedlamController => _activeMissionController as BabyBirdBedlamMissionController;
        public CoopFeastGuardPuzzle FeastGuardPuzzle => BabyBirdBedlamController?.Puzzle ?? _emptyFeastGuardPuzzle;
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
        public string SelectedMissionPresentationLine => BuildMissionDefinition(SelectedMissionVariant, _tuning).PresentationLine;
        public string SelectedMissionRoleHint => BuildMissionDefinition(SelectedMissionVariant, _tuning).RoleHint;
        public string SelectedMissionReadinessLabel => MissionReadinessLabelFor(SelectedMissionVariant);
        public string SelectedMissionChallengeLabel => MissionChallengeLabelFor(SelectedMissionVariant);
        public MissionVariant CouchTestFocusVariant => MissionVariant.OperationPeeBreak;
        public string CouchTestFocusName => BuildMissionDefinition(CouchTestFocusVariant, _tuning).Name;
        public string CouchTestFocusLabel => $"COUCH TEST FOCUS: {CouchTestFocusName} - press F5 / P / Y to highlight";
        public string FamilyShowcaseShortcutLabel => "Showcase jumps: F7 Backyard | F6 Kitchen | F8 Weenies | F9 Walkies | F5 Pee";
        public State Phase { get; private set; } = State.Intro;
        public bool IsGameOver => Phase == State.GameOver;
        public bool IsLevelClear => Phase == State.LevelClear;
        public int UnitedBarks { get; private set; }
        private int _breakfastRecovered;
        public int BreakfastRecovered => SnackHeistController?.Recovered ?? BackyardRescueController?.Collected ?? _breakfastRecovered;
        public int BreakfastGoal => _mission != null ? _mission.ItemGoal : recoveryGoal;
        private int _stolenFood;
        public int StolenFood => SnackHeistController?.Stolen ?? BackyardRescueController?.Stolen ?? _stolenFood;
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
        public string MissionPresentationLine => _mission != null ? _mission.PresentationLine : string.Empty;
        public string MissionRoleHint => _mission != null ? _mission.RoleHint : string.Empty;
        public string MissionReusablePresentation => _mission != null ? _mission.ReusablePresentation : string.Empty;
        public string ActiveMissionReadinessLabel => _mission != null ? MissionReadinessLabelFor(_mission.Variant) : SelectedMissionReadinessLabel;
        public bool MissionOpeningPresentationVisible => MissionActive() &&
            _activeMissionController is IMissionOpeningPresentationController opening && opening.IsPresentingOpening;
        public bool MissionBriefingVisible => MissionActive() && Time.time < _introPromptUntil;
        public string MissionBanner { get; private set; } = string.Empty;
        public string EndRank { get; private set; } = "Needs More Bark";
        public string EndHeadlineLabel => !EndScreenVisible ? string.Empty : IsLevelClear ? "MISSION COMPLETE" : "MISSION FAILED";
        public string EndScoreLabel => !EndScreenVisible ? string.Empty : $"Score {Score}  |  Stars {StarRating}/3";
        public string EndBestScoreLabel => !EndScreenVisible ? string.Empty : $"Best {BestScoreForMission(ActiveMissionVariant)}";
        public string EndSummaryLabel { get; private set; } = string.Empty;
        public string EndReasonLabel { get; private set; } = string.Empty;
        public string EndChallengeLabel
        {
            get
            {
                if (!EndScreenVisible) return string.Empty;
                if (LastRoundFlawless)
                {
                    string rivalry = FlawlessRivalryLabel;
                    return string.IsNullOrEmpty(rivalry) ? "Challenge beaten: FLAWLESS clear" : $"Challenge beaten: {rivalry}";
                }
                return $"Replay target: {MissionChallengeLabelFor(ActiveMissionVariant).Replace("Challenge: ", string.Empty)}";
            }
        }
        public bool ReplayPromptVisible => IsGameOver || IsLevelClear;
        public string ReplayPromptLabel => ReplayPromptVisible ? (_mission != null ? _mission.ReplayPrompt : "Press R / Enter / Start to replay the weenie rescue") : string.Empty;
        public bool EndReplayAvailable => EndScreenVisible;
        public bool EndNextMissionAvailable => EndScreenVisible;
        public bool EndMissionSelectAvailable => EndScreenVisible;
        public string EndReplayActionLabel => EndReplayAvailable ? "Replay" : string.Empty;
        public string EndNextActionLabel => EndNextMissionAvailable ? "Next Mission" : string.Empty;
        public string EndMissionSelectActionLabel => EndMissionSelectAvailable ? "Mission Select" : string.Empty;
        public int SessionMissionsPlayed { get; private set; }
        public int SessionTotalScore { get; private set; }
        public int SessionStarsEarned { get; private set; }
        public int SessionFlawlessClears { get; private set; }
        public int SessionUniqueMissionsCompleted { get; private set; }
        public int SessionUniqueMissionsCleared { get; private set; }
        public bool SessionAllMissionsCompleted => SessionUniqueMissionsCleared >= MissionOrder.Length;
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
        public int ColdReadQuestionCount { get; private set; }
        public int MissionReplayCount { get; private set; }

        // The first-play tutorial is deliberately per dog: one player cannot dismiss the other
        // player's prompt. Only the currently displayed action records progress, keeping the lesson
        // progressive instead of rewarding random button-mashing through all four verbs at once.
        private const int TutorialActionCount = (int)TutorialActionStep.Complete;
        private bool[,] _tutorialActionDone;

        public bool ActionTutorialAvailable =>
            ActiveMissionVariant == MissionVariant.BackyardRescue && MissionActive();
        public TutorialActionStep CurrentTutorialAction
        {
            get
            {
                if (!TutorialActionDoneForAll(TutorialActionStep.Bark)) return TutorialActionStep.Bark;
                if (!TutorialActionDoneForAll(TutorialActionStep.Interact)) return TutorialActionStep.Interact;
                if (!TutorialActionDoneForAll(TutorialActionStep.Jump)) return TutorialActionStep.Jump;
                if (!TutorialActionDoneForAll(TutorialActionStep.Wrestle)) return TutorialActionStep.Wrestle;
                return TutorialActionStep.Complete;
            }
        }

        // Compatibility/readability properties now mean both players completed that action.
        public bool TutorialBarkDone => TutorialActionDoneForAll(TutorialActionStep.Bark);
        public bool TutorialInteractDone => TutorialActionDoneForAll(TutorialActionStep.Interact);
        public bool TutorialJumpDone => TutorialActionDoneForAll(TutorialActionStep.Jump);
        public bool TutorialWrestleDone => TutorialActionDoneForAll(TutorialActionStep.Wrestle);
        public bool ShowActionTutorial =>
            ActionTutorialAvailable && CurrentTutorialAction != TutorialActionStep.Complete;

        public bool TutorialActionDone(DogId dogId, TutorialActionStep action)
        {
            if (action == TutorialActionStep.Complete)
                return CurrentTutorialAction == TutorialActionStep.Complete;
            int dogIndex = _dogs != null ? IndexOfDog(dogId) : -1;
            return dogIndex >= 0 && _tutorialActionDone != null &&
                dogIndex < _tutorialActionDone.GetLength(0) && _tutorialActionDone[dogIndex, (int)action];
        }

        public string PlayerControlSourceLabel(DogId dogId)
        {
            int dogIndex = _dogs != null ? IndexOfDog(dogId) : -1;
            if (dogIndex < 0 || _inputs == null || dogIndex >= _inputs.Length || _inputs[dogIndex] == null)
                return "CONNECT PAD";
            if (_inputs[dogIndex].HasBoundGamepad) return "PAD READY";
            if (_inputs[dogIndex].HasDisconnectedGamepad) return "PAD LOST";
            return _inputs[dogIndex].AssignedKeyboardScheme != GamepadPlayerInput.KeyboardScheme.None
                ? "KEYS"
                : "CONNECT PAD";
        }

        /// <summary>Pause-menu escape hatch for returning players.</summary>
        public void SkipActionTutorial()
        {
            if (!ActionTutorialAvailable) return;
            EnsureActionTutorialProgress();
            for (int dog = 0; dog < _tutorialActionDone.GetLength(0); dog++)
                for (int action = 0; action < TutorialActionCount; action++)
                    _tutorialActionDone[dog, action] = true;
            LogPlaytestEvent("Tutorial", "skipped");
        }

        /// <summary>Pause-menu replay for couch testers who want the prompts back.</summary>
        public void ReplayActionTutorial()
        {
            if (!ActionTutorialAvailable) return;
            ResetActionTutorialProgress();
            LogPlaytestEvent("Tutorial", "replayed from Bark");
        }
        public float MissionDurationSeconds => CurrentFlow == FlowState.MissionSelect ? 0f : Mathf.Clamp(roundDuration - TimeRemaining, 0f, roundDuration);

        /// <summary>Test/dev seam: fixed lead-in length in seconds; null uses briefing + sniff tuning.</summary>
        public static float? LeadInSecondsOverride;

        /// <summary>True while the round is inside the frozen sniff-around discovery window.</summary>
        public bool LeadInActive => _leadInRemaining > 0f && Phase == State.Playing;
        public float LeadInRemaining => Mathf.Max(0f, _leadInRemaining);
        public string LeadInCountdownLabel => LeadInActive
            ? $"SNIFF AROUND! GO IN {Mathf.CeilToInt(_leadInRemaining)} - BARK TO GO NOW"
            : string.Empty;

        /// <summary>
        /// Mission-facing clock: Time.time minus every second spent frozen in a lead-in, so
        /// controller schedules anchored at StartMission hold still until the round actually GOes.
        /// </summary>
        public float MissionNow => Time.time - _missionClockOffset;
        public string FailPressureLabel => BuildFailPressureLabel();
        public string DogPositionsLabel => BuildDogPositionsLabel();
        public string PlaytestCountersLabel => $"Barks {BarksUsed} / missed interacts {FailedInteractions} / objective shifts {ObjectiveChangeCount} / cold-read ? {ColdReadQuestionCount} / duration {MissionDurationSeconds:0.0}s / replays {MissionReplayCount}";
        public string PlaytestHotkeysLabel => "F1 overlay / F2 audio / F3 rumble / F4 mark cold-read question";
        public string MissionFailureSummaryLabel => BuildMissionFailureSummaryLabel();
        public bool AudioEnabled { get; private set; } = true;
        public bool RumbleEnabled { get; private set; } = true;
        public bool CameraShakeEnabled { get; private set; } = true;
        public IReadOnlyList<string> AudioCueRequests => _audioCueRequests;
        public IReadOnlyList<string> RumbleRequests => _rumbleRequests;
        public string LastAudioCueRequested { get; private set; } = string.Empty;
        public string LastAudioClipPlayed { get; private set; } = string.Empty;
        public string LastRumbleRequested { get; private set; } = string.Empty;
        public float LastShakeMagnitude { get; private set; }
        public int ShakeRequestCount { get; private set; }
        public int AudioCueRequestCount => _audioCueRequests.Count;
        public int RumbleRequestCount => _rumbleRequests.Count;
        public int ActiveRumblePadCount => _activeRumbleDeviceIds.Count;
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
        private readonly Dictionary<string, AudioClip[]> _audioClipBanks = new();
        private readonly Dictionary<string, int> _audioBankIndices = new();
        private readonly Dictionary<string, AudioCueSlot> _audioSlots = ArenaFeedbackCatalog.BuildLookup();
        private readonly List<string> _audioCueRequests = new();
        private readonly List<string> _rumbleRequests = new();
        private readonly HashSet<int> _activeRumbleDeviceIds = new();
        private readonly HashSet<int> _rumbleDispatchDeviceIds = new();
        private CheddarAndCocoa.CameraRig.SharedCameraController _camera;
        private MissionDefinition _mission;
        private GameObject _bunnyCameoObject;
        private readonly HerdingMissionState _emptyHerdingState = new HerdingMissionState();
        private readonly Dictionary<MissionVariant, IMissionController> _missionControllers = new();
        private IMissionController _activeMissionController;
        private KitchenFoodFrenzyMissionController KitchenController =>
            _activeMissionController as KitchenFoodFrenzyMissionController;
        private EagleShadowPanicMissionController EagleShadowController =>
            _activeMissionController as EagleShadowPanicMissionController;
        private CoyotesFenceMissionController CoyotesFenceController =>
            _activeMissionController as CoyotesFenceMissionController;
        private readonly ThreatSweepMissionState _emptyThreatSweepState = new ThreatSweepMissionState();
        private readonly PatrolDefenseMissionState _emptyPatrolState = new PatrolDefenseMissionState();
        private readonly CoopRescueTimingPuzzle _emptyEagleRescuePuzzle = new CoopRescueTimingPuzzle();
        private readonly CarryRoundupMissionState _emptyCarryState = new CarryRoundupMissionState();
        private readonly ScentSearchMissionState _emptyScentState = new ScentSearchMissionState();
        private PanicMeter _panic;
        private RoleTurnBeacon _roleTurnBeacon;
        private readonly MissionGuidanceEscalation _guidance = new MissionGuidanceEscalation();
        private int? _guidanceOwningDogIndex;
        private int _guidanceLastTier;
        private float _guidanceNudgeAt;
        private readonly TextMesh[] _guidanceWidenedLabels = new TextMesh[2];
        private float _handoffFlashUntil;
        private int[] _dogContribution;
        private readonly CarRideMissionState _emptyCarState = new CarRideMissionState();
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
        private readonly CoopSequenceChainPuzzle _emptyEscapePuzzle = new CoopSequenceChainPuzzle();
        private readonly CoopChaosMachinePuzzle _emptyChaosJunctionPuzzle = new CoopChaosMachinePuzzle();
        private readonly CoopStretchSpanPuzzle _emptyBlanketPuzzle = new CoopStretchSpanPuzzle();
        private readonly CoopFeastGuardPuzzle _emptyFeastGuardPuzzle = new CoopFeastGuardPuzzle();
        private readonly CoopScentRelayPuzzle _emptyBoneRelayPuzzle = new CoopScentRelayPuzzle();
        // Mark the Yard now lives in MarkTheYardMissionController; this empty state backs the
        // compatibility accessor when the mission is not the active controller.
        private readonly TerritoryMissionState _emptyTerritoryState = new TerritoryMissionState();
        private readonly SockBasketMissionState _emptySockBasketState = new SockBasketMissionState();

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
        private float _leadInRemaining;
        private bool _openingPresentationWasActive;
        private float _missionClockOffset;
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
            _rng = new System.Random(seed);
            _dogStarts = new Vector2[dogs.Length];
            _lastBarks = new float[dogs.Length];
            _dogContribution = new int[dogs.Length];
            _tutorialActionDone = new bool[dogs.Length, TutorialActionCount];
            DogFeedback = new DogReadabilityFeedback[dogs.Length];
            ObjectiveArrows = new ObjectiveArrowFeedback[dogs.Length];
            InteractionRangeIndicators = new InteractionRangeIndicator[dogs.Length + 3];

            for (int i = 0; i < dogs.Length; i++)
            {
                _dogStarts[i] = dogs[i].transform.position;
                dogs[i].OnBark += OnDogBarked;
                dogs[i].OnInteract += OnDogInteracted;
                dogs[i].OnWrestle += OnDogWrestled;
                dogs[i].OnJump += OnDogJumped;
                dogs[i].TryGetComponent(out DogReadabilityFeedback dogFeedback);
                dogs[i].TryGetComponent(out ObjectiveArrowFeedback objectiveArrow);
                DogFeedback[i] = dogFeedback;
                ObjectiveArrows[i] = objectiveArrow;
                InteractionRangeIndicators[i] = dogs[i].gameObject.AddComponent<InteractionRangeIndicator>();
                InteractionRangeIndicators[i].Init(_rangeSprite, new Color(0.6f, 1f, 0.75f, 0.42f), "RESCUE BARK");
            }
            WorldLabelVisibility.SetPromptTargets(_dogs);
            WorldLabelVisibility.SetDebugVisible(PlaytestOverlayVisible);
            ObjectiveArrowFeedback.SetDebugTextVisible(PlaytestOverlayVisible);
            InteractionRangeIndicator.SetDebugTextVisible(PlaytestOverlayVisible);
            DogReadabilityFeedback.SetDebugIdentityLabelsVisible(PlaytestOverlayVisible);
            MissionPropArtAttachment.SetAffordanceTargets(_dogs);
            MissionPropArtAttachment.SetDebugGeometryVisible(PlaytestOverlayVisible);

            _playtestLog.Clear();
            _panic = gameObject.AddComponent<PanicMeter>();
            _roleTurnBeacon = gameObject.AddComponent<RoleTurnBeacon>();
            _roleTurnBeacon.Init();
            _mission = BuildMissionDefinition(startingMission, _tuning);
            _selectedMissionIndex = IndexOfMission(startingMission);
            _treatRoot = new GameObject(_mission.ItemRootName).transform;
            BuildAudio();
            BuildMissionObjects();
            HideInteractionRanges();
            ShowMissionSelect();
        }

        /// <summary>Wires the shared couch camera so mission clear/fail can add a cosmetic screen shake.</summary>
        public void SetSharedCamera(CheddarAndCocoa.CameraRig.SharedCameraController camera) => _camera = camera;

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
            squirrelObject: SquirrelObject,
            predatorObject: PredatorObject,
            squirrelMoveSpeed: _tuning.SquirrelMoveSpeed,
            singleBarkSquirrelRange: _tuning.SingleBarkSquirrelRange,
            singleBarkScareSeconds: _tuning.SingleBarkScareSeconds,
            firstSquirrelBaseDelay: _tuning.FirstSquirrelBaseDelay,
            firstSquirrelTroubleDelay: _tuning.FirstSquirrelTroubleDelay,
            squirrelBaseDelay: _tuning.SquirrelBaseDelay,
            squirrelTroubleDelay: _tuning.SquirrelTroubleDelay,
            itemScore: _mission.ItemScore,
            maxStolenFood: _mission.MaxStolenFood,
            squirrelPenalty: _mission.SquirrelPenalty,
            squirrelScareScore: _mission.SquirrelScareScore,
            pancakeSquirrelPenalty: _tuning.PancakeSquirrelPenalty,
            panicMeter: _panic,
            random: () => _rng,
            now: () => MissionNow,
            activeModifier: () => ActiveModifier,
            debugPresentationEnabled: () => PlaytestOverlayVisible,
            audioEnabled: () => AudioEnabled,
            activeTreats: () => _treats,
            isPredatorResolved: () => PredatorResolved,
            isTugComplete: () => TugComplete,
            tugProgress: () => TugProgress,
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
            addWorldLabel: AddWorldLabel,
            objectiveGoal: _mission.ItemGoal,
            createActor: kind => MakeActor(ArenaArtCatalog.Actor(kind)),
            acquireHiddenTreat: FindFirstHiddenTreat,
            recoverCollectible: RecoverControllerCollectible,
            replaceCollectible: ReplaceControllerCollectible,
            setActorState: SetActorState,
            pulse: Pulse,
            requestShake: RequestShake,
            signalRoleHandoff: SignalRoleHandoff);

        public void OnTreatCollected(Treat treat, DogController dog)
        {
            if (!MissionActive() || treat == null) return;

            // Scooping the first collectible mid-sniff counts as starting to play: end the freeze
            // so the discovery window can't be farmed, then bank the grab normally.
            if (_leadInRemaining > 0f) EndLeadIn($"{DogName(dog)} grabbed the first collectible");

            int collectorIndex = dog != null && dog.TryGetComponent<DogIdentity>(out var collectorIdentity)
                ? IndexOfDog(collectorIdentity.Id)
                : -1;
            if (_activeMissionController is IMissionTreatCollector controllerCollector &&
                controllerCollector.HandleTreatCollected(treat, collectorIndex))
            {
                CheckClear();
                return;
            }

            AddScore(_mission.ItemScore, _mission.CollectedScoreLabel);
            _breakfastRecovered++;
            LastCue = $"{DogName(dog)} recovered {_mission.ItemCollectCueNoun}!";
            Pulse(dog != null ? dog.gameObject : null, 1.2f);
            SetJuice(JuiceFeedbackKind.ScoreDelta, LastScoreEventLabel);
            SpawnWorldPop(dog != null ? dog.transform.position : treat.transform.position, LastScoreEventLabel, _mission.ItemPopColor);
            RequestAudioCue(ArenaFeedbackCatalog.EatingGulp);
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
            RequestAudioCue(ArenaFeedbackCatalog.UiButtonConfirm);
            _reuseMissionSeedOnNextBegin = true;
            StartMission(_mission.Variant);
        }

        public void StartMission(MissionVariant variant)
        {
            bool startedFromMenu = MissionSelectVisible;
            SetPaused(false);
            SelectMission(variant);
            if (startedFromMenu)
            {
                RequestAudioCue(ArenaFeedbackCatalog.UiButtonConfirm);
                RequestAudioCue(ArenaFeedbackCatalog.UiMenuClose);
            }
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
                RequestAudioCue(ArenaFeedbackCatalog.UiMenuFocus);
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

        public void SelectCouchTestFocusMission() => SelectMission(CouchTestFocusVariant);
        public void SelectKitchenShowcaseMission() => SelectMission(MissionVariant.KitchenFoodFrenzy);
        public void SelectBackyardShowcaseMission() => SelectMission(MissionVariant.BackyardRescue);
        public void SelectWeenieShowcaseMission() => SelectMission(MissionVariant.WeenieRoundup);
        public void SelectWalkiesShowcaseMission() => SelectMission(MissionVariant.LeashWalk);

        private void SelectMissionGridStep(int columnDelta, int rowDelta)
        {
            int count = MissionOrder.Length;
            if (columnDelta != 0)
            {
                // Horizontal steps walk the row-major tile order linearly, so pushing right past a
                // row (or page) edge lands on the next tile the couch sees, wrapping at the ends.
                SelectMission(MissionOrder[(_selectedMissionIndex + columnDelta + count) % count]);
                return;
            }

            int pageStart = SelectedMissionPage * MissionSelectTilesPerPage;
            int pageSize = Mathf.Min(MissionSelectTilesPerPage, count - pageStart);
            int rowsThisPage = Mathf.CeilToInt(pageSize / (float)MissionSelectGridColumns);
            int local = _selectedMissionIndex - pageStart;
            int row = local / MissionSelectGridColumns;
            int column = local % MissionSelectGridColumns;
            int targetRow = (row + rowDelta + rowsThisPage) % rowsThisPage;
            int targetIndex = pageStart + targetRow * MissionSelectGridColumns + column;

            // Keep grid navigation safe if the final row is not full.
            if (targetIndex >= count)
                targetIndex = count - 1;

            SelectMission(MissionOrder[targetIndex]);
        }

        public void StartSelectedMission() => StartMission(SelectedMissionVariant);

        public void ReturnToMissionSelect()
        {
            SetPaused(false);
            RequestAudioCue(ArenaFeedbackCatalog.UiMenuOpen);
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
            SessionUniqueMissionsCleared = 0;
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
            RequestAudioCue(ArenaFeedbackCatalog.UiButtonConfirm);
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
            RequestAudioCue(ArenaFeedbackCatalog.UiButtonConfirm);
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
            RequestAudioCue(ArenaFeedbackCatalog.UiMenuOpen);
            LogPlaytestEvent("SessionSummary", SessionSummaryLabel);
            LogObjectiveIfChanged();
        }

        public void TogglePlaytestOverlay() => SetPlaytestOverlayVisible(!PlaytestOverlayVisible);

        public void SetPlaytestOverlayVisible(bool visible)
        {
            if (PlaytestOverlayVisible == visible) return;
            PlaytestOverlayVisible = visible;
            WorldLabelVisibility.SetDebugVisible(visible);
            ObjectiveArrowFeedback.SetDebugTextVisible(visible);
            InteractionRangeIndicator.SetDebugTextVisible(visible);
            DogReadabilityFeedback.SetDebugIdentityLabelsVisible(visible);
            MissionPropArtAttachment.SetDebugGeometryVisible(visible);
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

        public void SetCameraShakeEnabled(bool enabled)
        {
            CameraShakeEnabled = enabled;
            if (!enabled)
            {
                LastShakeMagnitude = 0f;
                _camera?.ClearShake();
            }
            LogPlaytestEvent("CameraShake", enabled ? "enabled" : "disabled");
        }

        public void RecordColdReadQuestion(string note = "what do I do?")
        {
            if (!MissionActive()) return;
            if (!PlaytestOverlayVisible) SetPlaytestOverlayVisible(true);
            ColdReadQuestionCount++;
            string guidance = string.IsNullOrEmpty(TeamGuidanceLabel) ? "no team guidance" : TeamGuidanceLabel;
            LogPlaytestEvent("ColdReadQuestion",
                $"{ActiveMissionVariant} / {Phase} / {ObjectiveLabel} / {guidance} / {DogPositionsLabel} / {note}");
        }

        public void ClearFeedbackRequests()
        {
            _audioCueRequests.Clear();
            _rumbleRequests.Clear();
            LastAudioCueRequested = string.Empty;
            LastAudioClipPlayed = string.Empty;
            LastRumbleRequested = string.Empty;
        }

        public int AuthoredAudioClipCount(string cueName) =>
            !string.IsNullOrEmpty(cueName) && _audioClipBanks.TryGetValue(cueName, out var bank) ? bank.Length : 0;

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
            return $"{FormatMissionDuration(mission.RoundSeconds)} • {goal}";
        }

        private static string FormatMissionDuration(float seconds)
        {
            if (seconds >= 120f && Mathf.Approximately(seconds % 60f, 0f))
                return $"{Mathf.RoundToInt(seconds / 60f)}m";
            return $"{seconds:0}s";
        }

        public static string MissionChallengeLabelFor(MissionVariant variant)
        {
            return variant switch
            {
                MissionVariant.OperationPeeBreak => "Challenge: Pawfect signal - 0 misreads",
                MissionVariant.BackyardRescue => "Challenge: all weenies, no steals",
                MissionVariant.KitchenFoodFrenzy => "Challenge: clean Dinner Rush",
                MissionVariant.SquirrelConspiracy => "Challenge: crack the case with no fake-outs",
                MissionVariant.EagleShadowPanic => "Challenge: no dog grabbed by the shadow",
                MissionVariant.CoyotesFence => "Challenge: perfect fence defense",
                MissionVariant.SnackHeist => "Challenge: stash it all, zero squirrel steals",
                MissionVariant.SockPanic => "Challenge: 5-for-5 socks, no missed dives",
                MissionVariant.WeenieRoundup => "Challenge: deliver all 5, no fumbles",
                MissionVariant.ScentSearch => "Challenge: 3 bones, zero wrong digs",
                MissionVariant.ThunderstormComfort => "Challenge: weather all 5 claps, calm the whole time",
                MissionVariant.MarkTheYard => "Challenge: claim all 5 zones, no reclaims",
                MissionVariant.LeashWalk => "Challenge: every checkpoint, zero leash snaps",
                MissionVariant.CarRide => "Challenge: ride all 7 road events, zero tumbles",
                MissionVariant.GateCrash => "Challenge: squeeze through without a single snap",
                MissionVariant.TableStealth => "Challenge: sneak the steak, never spotted",
                MissionVariant.SquirrelSwitcheroo => "Challenge: raid the stash, zero backfires",
                MissionVariant.WalkCampaign => "Challenge: sell it first try, zero misreads",
                MissionVariant.BoneRelay => "Challenge: 3 bones, no wasted digs",
                MissionVariant.GreatEscape => "Challenge: break out without a single fumble",
                MissionVariant.ChaosMachine => "Challenge: run the whole cascade, zero misfires",
                MissionVariant.BlanketCatch => "Challenge: 5 catches, never rip the blanket",
                MissionVariant.BabyBirdBedlam => "Challenge: eat all 4 chicks, zero pecks",
                _ => "Challenge: clear clean for FLAWLESS"
            };
        }

        public string MissionReadinessLabelFor(MissionVariant variant)
        {
            var definition = BuildMissionDefinition(variant, _tuning);
            var result = ReadabilityValidator.ValidateMissionDefinition(definition);
            return result.Passed
                ? $"Readability gate: READY - {definition.MechanicTag}"
                : $"Readability gate: BLOCKED - missing {result.Missing}";
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
            if (MissionActive() && SnackHeistController != null)
            {
                SnackHeistController.ForceStealAttempt();
                CheckClear();
                return;
            }
            if (MissionActive() && BackyardRescueController != null)
            {
                BackyardRescueController.ForceStealAttempt();
                CheckClear();
                UpdateInteractionRanges();
                return;
            }
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

        public void ForceCollectTreat()
        {
            if (!MissionActive()) return;
            SnackHeistController?.ForceCollectTreat();
            CheckClear();
        }

        public void ForceStealAttempt()
        {
            if (!MissionActive()) return;
            SnackHeistController?.ForceSteal();
            CheckClear();
        }

        public void ForceBackyardTrapRedirect(DogId pressureDog, bool gapHeld = true)
        {
            if (!MissionActive() || BackyardRescueController == null) return;
            BackyardRescueController.ForceRedirect(pressureDog, gapHeld);
            CheckClear();
            UpdateObjectiveArrows();
            UpdateInteractionRanges();
        }

        public void ForceBackyardTrapRecovery(DogId dogId)
        {
            if (!MissionActive() || BackyardRescueController == null) return;
            BackyardRescueController.ForceWeenieRecovery(dogId);
            CheckClear();
            UpdateObjectiveArrows();
            UpdateInteractionRanges();
        }

        public void ForceGameOver() => EndRound(false);


        public void ForceSquirrelConspiracyHerd(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) SquirrelConspiracyController?.ForceHerd(dogId);
            CheckClear();
        }

        public void ForceSquirrelConspiracyTaunt()
        {
            if (MissionActive()) SquirrelConspiracyController?.ForceTaunt();
            CheckClear();
        }

        public void ForceSquirrelConspiracyFindStash(DogId dogId = DogId.Cocoa)
        {
            if (MissionActive()) SquirrelConspiracyController?.ForceFindStash(dogId);
            CheckClear();
        }

        public void ForceEagleShadowSafeHide()
        {
            if (MissionActive()) EagleShadowController?.ForceSafeHide();
        }

        public void ForceEagleShadowExposure()
        {
            if (!MissionActive()) return;
            EagleShadowController?.ForceExposure();
            CheckClear();
        }

        /// <summary>Test/convenience hook: run the full wiggle+pull rescue to free the snatched dog.</summary>
        public void ForceEagleShadowRescue(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) EagleShadowController?.ForceRescue();
        }

        public void ForceEagleShadowUnitedFront()
        {
            if (!MissionActive()) return;
            EagleShadowController?.ForceUnitedFront();
            CheckClear();
        }

        public void ForceEagleShadowSweepPass()
        {
            if (!MissionActive()) return;
            EagleShadowController?.ForceSweepPass();
            CheckClear();
        }

        public void ForceCoyoteBarkPressure(DogId dogId = DogId.Cocoa)
        {
            if (MissionActive()) CoyotesFenceController?.ForceBarkPressure(dogId);
        }

        public void ForceCoyoteRepair(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) CoyotesFenceController?.ForceRepair(dogId);
        }

        public void ForceCoyoteBreach()
        {
            if (!MissionActive()) return;
            CoyotesFenceController?.ForceBreach();
            CheckClear();
        }

        public void ForceCoyoteFakeSnack()
        {
            if (MissionActive()) CoyotesFenceController?.ForceFakeSnack();
        }

        public void ForceCoyoteFinalBlock()
        {
            if (!MissionActive()) return;
            CoyotesFenceController?.ForceFinalBlock();
            CheckClear();
        }

        public void ForceCoyoteProwlReach()
        {
            if (!MissionActive()) return;
            CoyotesFenceController?.ForceProwlReach();
            CheckClear();
        }

        public void ForceSockBasketTip(DogId dogId = DogId.Cocoa)
        {
            if (MissionActive()) SockPanicController?.ForceTip(dogId);
        }

        public void ForceSockBasketTimeout()
        {
            if (MissionActive()) SockPanicController?.ForceTimeout();
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
            _breakfastRecovered = 0;
            _stolenFood = 0;
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
            ResetActionTutorialProgress();
            _guidance.Configure(_mission.GuidanceTierCap, _mission.GuidanceTier1Seconds,
                _mission.GuidanceTier2Seconds, _mission.GuidanceTier3Seconds);
            _guidance.Reset();
            ResetGuidancePresentation();

            if (!_reuseMissionSeedOnNextBegin)
                // Mission order is presentation, not tuning. Use the enum's stable identity so a
                // quality-driven selector reorder cannot silently change deterministic modifiers.
                _missionSeed = MissionSeedGenerator.StableSeed(
                    _mission.Variant.ToString(), SessionMissionsPlayed, (int)_mission.Variant);
            _reuseMissionSeedOnNextBegin = false;
            _rng = new System.Random(_missionSeed);
            ActiveModifier = (RoundModifier)_rng.Next(0, 3);
            ActivateMissionController(_mission.Variant);
            if (LeadInSecondsOverride.HasValue && LeadInSecondsOverride.Value <= 0f &&
                _activeMissionController is IMissionOpeningPresentationController testOpening)
                testOpening.SkipOpeningPresentation();
            _openingPresentationWasActive = MissionOpeningPresentationVisible;
            if (_dogContribution != null) System.Array.Clear(_dogContribution, 0, _dogContribution.Length);
            if (_panic != null) _panic.ResetMeter();
            _nextUnitedBarkAt = 0f;
            _teamBarkFeedbackUntil = 0f;
            _scorePopUntil = 0f;
            _handoffFlashUntil = 0f;
            LastHandoffFromDog = null;
            LastHandoffToDog = null;
            HandoffSignalCount = 0;
            _squirrelScaredUntil = 0f;
            _nextSquirrelScareScoreAt = 0f;
            _squirrelTarget = null;
            _squirrelHasStarted = false;
            _introPromptUntil = Time.time + _tuning.IntroPromptSeconds;
            _leadInRemaining = Mathf.Max(0f, LeadInSecondsOverride ?? (_tuning.IntroPromptSeconds + _tuning.LeadInSniffSeconds));
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
                _dogs[i].ResetMissionOverlays();
                if (DogFeedback[i] != null) DogFeedback[i].SetCarrying(false);
                if (DogFeedback[i] != null) DogFeedback[i].ClearMissionPose();
                _dogs[i].transform.position = _dogStarts[i];
                if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
                if (i < _inputs.Length && _inputs[i] != null) _inputs[i].enabled = true;
                if (ObjectiveArrows[i] != null) ObjectiveArrows[i].Hide();
            }

            ClearTreats();
            for (int i = 0; i < treatCount; i++) SpawnTreat();

            // Squirrel and Predator are shared actors reused across many missions. A mission that
            // promotes a mission-prop overlay onto one of them (e.g. Eagle Shadow Panic's talon grip,
            // Coyotes Fence's pinned-gap post) leaves that sprite - and the dimmed fallback body under
            // it - in place until something else overwrites it. Clear both every mission start so a
            // fresh mission never inherits a stale overlay from whatever last used the actor.
            ClearSharedActorOverride(SquirrelObject);
            ClearSharedActorOverride(PredatorObject);

            // Park the waiting squirrel on a visible perch just inside the close camera instead of the
            // far yard corner, so players actually see the threat the HUD/arrows reference. It is still
            // far enough from the dog spawns (+/-10,0) to not be in instant bark range.
            PlaceObject(SquirrelObject, _activeMissionController is SquirrelConspiracyMissionController
                ? SquirrelConspiracyController.EntryTarget
                : new Vector2(11f, 7f));
            PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
            PlaceObject(RopeObject, Vector2.zero);
            PlaceObject(_bunnyCameoObject, new Vector2(_bounds.xMin + 1.4f, _bounds.yMin + 1.0f));
            SquirrelObject.SetActive(_mission.UsesSquirrel);
            PredatorObject.SetActive(_mission.RequiresPredator);
            RopeObject.SetActive(_mission.RequiresTug);
            if (_bunnyCameoObject != null) _bunnyCameoObject.SetActive(true);
            RequestAudioCue(ArenaFeedbackCatalog.BunnyHop);
            if (_mission.UsesSquirrel) SetActorState(SquirrelObject, _activeMissionController is SquirrelConspiracyMissionController ? "SQUIRREL CONSPIRACY ROUTE 1" : "Squirrel: WAITING", new Color(0.55f, 0.32f, 0.12f), 0.06f);
            if (_mission.RequiresPredator) SetActorState(PredatorObject, "Predator: OFFSCREEN", Color.gray, 0.04f);
            if (_mission.RequiresTug) SetActorState(RopeObject, "Rope/Tug - BOTH DOGS", new Color(0.95f, 0.7f, 0.15f), 0.08f);
            StageDogsForMissionEntry();
            UpdateObjectiveArrows();
            _lastLoggedObjective = string.Empty;
            ColdReadQuestionCount = 0;
            LogPlaytestEvent("MissionStarted", $"{_mission.Name} / {ActiveModifierLabel} / {roundDuration:0}s");
            LogObjectiveIfChanged();
        }

        private void Update()
        {
            TickFlowInput();
            if (!MissionActive()) return;

            TickMissionSelectionKeys();
            if (!MissionActive()) return;

            if (_activeMissionController is IMissionOpeningPresentationController openingPresentation)
            {
                openingPresentation.TickOpeningPresentation(Time.unscaledDeltaTime);
                if (openingPresentation.IsPresentingOpening)
                {
                    _openingPresentationWasActive = true;
                    _missionClockOffset += Time.deltaTime;
                    SetOpeningPresentationDogLock(true);
                    MissionBanner = string.Empty;
                    return;
                }

                if (_openingPresentationWasActive)
                {
                    _openingPresentationWasActive = false;
                    SetOpeningPresentationDogLock(false);
                    _introPromptUntil = Time.time + _tuning.IntroPromptSeconds;
                    _leadInRemaining = Mathf.Max(0f, LeadInSecondsOverride ??
                        (_tuning.IntroPromptSeconds + _tuning.LeadInSniffSeconds));
                    MissionBanner = MissionIntroPrompt;
                    LogPlaytestEvent("OpeningPresentation", "explainer complete; controls card shown");
                }
            }

            MissionBanner = Time.time < _introPromptUntil ? MissionIntroPrompt : string.Empty;

            // Sniff-around lead-in: the yard is visible and the dogs can roam, but the round
            // clock, threats, and controller schedules hold still until the discovery beat ends.
            if (_leadInRemaining > 0f)
            {
                TickLeadIn();
                return;
            }

            // Resolve the players' current-frame actions before timeout. A co-op objective earned
            // on the final visible fraction of a second must win that race, and an optional
            // controller-owned success presentation can then hold the live payoff without the
            // shared clock converting it into a failure.
            TickModifier();
            if (_activeMissionController != null) _activeMissionController.Tick(Time.deltaTime, MissionNow);
            else TickSquirrel();
            TickPredator();
            TickTugProximity();
            CheckClear();
            if (!MissionActive()) return;

            bool presentingEarnedSuccess = _activeMissionController is IMissionSuccessPresentationController successPresentation
                && successPresentation.IsPresentingSuccessfulOutcome;
            if (!presentingEarnedSuccess) _guidance.Tick(Time.deltaTime);
            if (!presentingEarnedSuccess) TimeRemaining -= Time.deltaTime;
            if (!presentingEarnedSuccess && TimeRemaining <= 0f)
            {
                EndRound(false);
                return;
            }

            UpdateObjectiveArrows();
            UpdateGuidancePresentation();
            UpdateTravelAssists();
            UpdateInteractionRanges();
            LogObjectiveIfChanged();
        }

        private void TickLeadIn()
        {
            _missionClockOffset += Time.deltaTime;
            _leadInRemaining -= Time.deltaTime;
            if (_leadInRemaining <= 0f)
            {
                EndLeadIn("sniff timer");
                return;
            }

            // Discovery aids stay live so the look-around actually teaches the level.
            UpdateObjectiveArrows();
            UpdateTravelAssists();
            UpdateInteractionRanges();
            LogObjectiveIfChanged();
        }

        private void SetOpeningPresentationDogLock(bool locked)
        {
            if (_dogs == null) return;
            foreach (var dog in _dogs)
            {
                if (dog == null) continue;
                dog.SetMode(locked ? MovementMode.Transit : MovementMode.Free);
                if (dog.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
        }

        public void SkipMissionOpeningPresentation()
        {
            if (_activeMissionController is not IMissionOpeningPresentationController opening ||
                !opening.IsPresentingOpening) return;
            opening.SkipOpeningPresentation();
            LogPlaytestEvent("OpeningPresentation", "skipped by player");
        }

        private void EndLeadIn(string reason)
        {
            _leadInRemaining = 0f;
            // An early skip can land while the briefing card is still up; drop card and banner with it.
            _introPromptUntil = Mathf.Min(_introPromptUntil, Time.time);
            _nextZoomiesPulseAt = Time.time + 6f;
            LastCue = $"GO! {MissionIntroPrompt}";
            LastFeedback = FeedbackKind.Intro;
            SetJuice(JuiceFeedbackKind.SuccessPop, "GO!");
            RequestAudioCue(ArenaFeedbackCatalog.ScoreGain);
            RequestRumble("lead_in_go", 0.14f, 0.3f, 0.14f);
            foreach (var dog in _dogs) SpawnWorldPop(dog.transform.position, "GO!", new Color(1f, 0.86f, 0.32f));
            LogPlaytestEvent("LeadIn", $"GO ({reason})");
            LogObjectiveIfChanged();
        }

        private void TickModifier()
        {
            if (ActiveModifier != RoundModifier.ZoomiesSurge || Time.time < _nextZoomiesPulseAt) return;

            foreach (var dog in _dogs) dog.TriggerZoomies();
            LastCue = "Zoomies surge! Hold the line!";
            _nextZoomiesPulseAt = Time.time + 10f;
            RequestAudioCue(ArenaFeedbackCatalog.AccelerationSkid);
            LogPlaytestEvent("Modifier", LastCue);
        }

        private void TickSquirrel()
        {
            if (!_mission.UsesSquirrel) return;
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
            RequestAudioCue(ArenaFeedbackCatalog.SquirrelChatter);
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

            _stolenFood++;
            AddScore(-(ActiveModifier == RoundModifier.PancakePanic ? _tuning.PancakeSquirrelPenalty : _mission.SquirrelPenalty), _mission.SquirrelStealScoreLabel);
            _squirrelTarget = null;
            _squirrelTimer = SquirrelDelay();
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = _mission.SquirrelStoleCue;
            SetJuice(JuiceFeedbackKind.WarningMiss, _mission.SquirrelStealJuiceLabel);
            SetActorState(SquirrelObject, _mission.SquirrelStoleActorLabel, Color.gray, 0.22f);
            RequestAudioCue(ArenaFeedbackCatalog.SquirrelChatter);
            RequestAudioCue(ArenaFeedbackCatalog.SquirrelEscapeLaugh);
            SpawnWorldPop(SquirrelObject.transform.position, _mission.SquirrelMissPopLabel, new Color(1f, 0.35f, 0.2f));
            RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            RequestRumble("squirrel_penalty", 0.18f, 0.38f, 0.16f);
            LogPlaytestEvent("SquirrelStole", $"{StolenFood}/{maxStolenFood}");

            if (StolenFood >= maxStolenFood) EndRound(false);
        }


        public void ForceWeeniePickup(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) WeenieRoundupController?.ForcePickup(dogId);
        }

        public void ForceWeenieDeliver(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) WeenieRoundupController?.ForceDeliver(dogId);
            CheckClear();
        }

        public void ForceWeenieDrop(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) WeenieRoundupController?.ForceDrop(dogId);
        }

        // --- Scent Search (sniff + dig) ---

        public void ForceScentSniff(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) ScentSearchController?.ForceSniff(dogId);
        }

        public void ForceScentDigCorrect(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) ScentSearchController?.ForceDigCorrect(dogId);
            CheckClear();
        }

        public void ForceScentDigWrong(DogId dogId = DogId.Cheddar)
        {
            if (MissionActive()) ScentSearchController?.ForceDigWrong(dogId);
            CheckClear();
        }



        // --- Thunderstorm Comfort now lives in ThunderstormComfortMissionController. ---

        public ThunderstormComfortMissionController ThunderstormController =>
            _activeMissionController as ThunderstormComfortMissionController;

        public ThunderstormMissionState ThunderstormState =>
            ThunderstormController?.StormState ?? _emptyStormState;

        private readonly ThunderstormMissionState _emptyStormState = new ThunderstormMissionState();

        public void ForceThunderclap()
        {
            if (MissionActive()) ThunderstormController?.ForceThunderclap();
            CheckClear();
        }

        public void ForceComfortStep(float seconds)
        {
            if (MissionActive()) ThunderstormController?.ForceComfortStep(seconds);
            CheckClear();
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

        // --- Walkies on the Leash now lives in LeashWalkMissionController. ---

        public LeashWalkMissionController LeashWalkController =>
            _activeMissionController as LeashWalkMissionController;

        public LeashWalkMissionState LeashWalkState =>
            LeashWalkController?.State ?? _emptyLeashState;

        private readonly LeashWalkMissionState _emptyLeashState = new LeashWalkMissionState();

        public void ForceReachCheckpoint()
        {
            if (MissionActive()) LeashWalkController?.ForceReachCheckpoint();
            CheckClear();
        }

        public void ForceLeashSnap()
        {
            if (MissionActive()) LeashWalkController?.ForceLeashSnap();
            CheckClear();
        }

        // --- Car Ride Chaos (backseat road events) ---

        /// <summary>Test hook: bank one clean road event (turn or brake ridden out).</summary>
        public void ForceCarEventSurvived()
        {
            if (MissionActive()) CarRideController?.ForceEventSurvived();
            CheckClear();
        }

        /// <summary>Test hook: one dog tumbles (bonk/squish/fling equivalent).</summary>
        public void ForceCarTumble(int dogIndex = 0)
        {
            if (MissionActive()) CarRideController?.ForceTumble(dogIndex);
            CheckClear();
        }

        /// <summary>Test hook: the snatched dog wiggles to crack the grip open.</summary>
        public void ForceEagleShadowWiggle()
        {
            if (MissionActive()) EagleShadowController?.ForceWiggle();
        }

        /// <summary>Test hook: the free dog pulls; only counts while the wiggle window is open.</summary>
        public void ForceEagleShadowPull()
        {
            if (!MissionActive()) return;
            EagleShadowController?.ForcePull();
            if (Phase != State.GameOver) CheckClear();
        }

        /// <summary>Test hook: let the cracked grip re-tighten (the wiggle window closes).</summary>
        public void ForceEagleRescueAdvance(float seconds)
        {
            if (MissionActive()) EagleShadowController?.ForceRescueAdvance(seconds);
        }

        private MissionRuntimeSnapshot BuildRuntimeSnapshot()
        {
            if (_activeMissionController != null)
                return _activeMissionController.CreateSnapshot(Score, TimeRemaining, Outcome);

            string missionId;
            int progress;
            int goal;
            int mistakes;
            missionId = ActiveMissionVariant.ToString();
            progress = BreakfastRecovered;
            goal = BreakfastGoal;
            mistakes = StolenFood + FailedInteractions;
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
            RequestShake(0.2f);
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
            SetActorState(RopeObject, "BOTH DOGS TUGGING - KEEP PULLING!", new Color(1f, 0.78f, 0.22f), 0.22f);
            if (TugProgress >= 1f) CompleteTug();
        }

        private Treat FindFirstHiddenTreat()
        {
            foreach (var treat in _treats)
                if (treat != null && !treat.gameObject.activeSelf) return treat;
            return null;
        }

        // Several controllers (SockPanic, SnackHeist, BackyardRescue's trap recovery) set a distinct
        // reaction sprite on the treat itself (Decoy/Saved/Stashed) right before calling one of these -
        // an immediate Destroy() would remove it from the scene before Unity ever rendered a frame with
        // that sprite showing. Object.Destroy(obj, seconds) keeps it fully visible for a beat first.
        // Disable the collider immediately so a dog re-entering the trigger during that window can't
        // double-collect an already-resolved treat.
        private const float CollectedTreatLingerSeconds = 0.5f;

        private void RecoverControllerCollectible(Treat treat)
        {
            if (treat == null) return;
            _breakfastRecovered++;
            _treats.Remove(treat);
            if (treat.TryGetComponent<Collider2D>(out var collider)) collider.enabled = false;
            Destroy(treat.gameObject, CollectedTreatLingerSeconds);
            SpawnTreat();
        }

        private void ReplaceControllerCollectible(Treat treat)
        {
            if (treat == null) return;
            _treats.Remove(treat);
            if (treat.TryGetComponent<Collider2D>(out var collider)) collider.enabled = false;
            Destroy(treat.gameObject, CollectedTreatLingerSeconds);
            SpawnTreat();
        }

        private void OnDogInteracted(DogId dogId)
        {
            if (!MissionActive()) return;
            if (MissionOpeningPresentationVisible)
            {
                SkipMissionOpeningPresentation();
                return;
            }
            bool tutorialDiscovery = TryRecordTutorialAction(dogId, TutorialActionStep.Interact);

            // A deliberate interact during the sniff-around freeze means "we're ready" — start the
            // round without charging a missed-interaction against the players.
            if (_leadInRemaining > 0f)
            {
                EndLeadIn($"{dogId} interacted");
                return;
            }

            if (_activeMissionController is IMissionInteractionController interactionController &&
                interactionController.HandleInteract(IndexOfDog(dogId)))
            {
                return;
            }

            if (_mission == null || !_mission.RequiresTug)
            {
                // A first-play discovery press is successful tutorial input even when there is no
                // nearby gameplay object. Do not answer the exact button we asked for with the
                // disabled/error cue; contextual targets teach interaction precision later.
                if (tutorialDiscovery) return;
                MarkFailedInteraction(dogId, "no interact target in this mission");
                return;
            }

            if (TugComplete)
            {
                if (tutorialDiscovery) return;
                MarkFailedInteraction(dogId, "tug already complete");
                return;
            }

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;
            if (Vector2.Distance(_dogs[dogIndex].transform.position, RopeObject.transform.position) > _tuning.TugInteractDistance)
            {
                if (tutorialDiscovery) return;
                MarkFailedInteraction(dogId, "too far from rope");
                return;
            }

            if (DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ShowTug((Vector2)(RopeObject.transform.position - _dogs[dogIndex].transform.position));
            TugProgress = Mathf.Min(1f, TugProgress + _tuning.TugInteractProgress);
            LastFeedback = FeedbackKind.TugNeedsPartner;
            LastCue = $"{DogName(_dogs[dogIndex])} has the rope - partner pile on!";
            SetActorState(RopeObject, "ROPE MOVING - NEED PARTNER DOG", new Color(1f, 0.78f, 0.22f), 0.2f);
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
            RequestAudioCue(ArenaFeedbackCatalog.ToySqueak);
            RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            RequestRumble("tug_success", 0.32f, 0.58f, 0.2f);
            LogPlaytestEvent("TugComplete", "Rope objective complete");
            CheckClear();
        }

        private void OnDogBarked(DogId dogId)
        {
            if (!MissionActive()) return;
            if (MissionOpeningPresentationVisible)
            {
                SkipMissionOpeningPresentation();
                return;
            }
            TryRecordTutorialAction(dogId, TutorialActionStep.Bark);

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;

            // "Bark when ready": during the sniff-around freeze a bark just starts the round.
            if (_leadInRemaining > 0f)
            {
                BarksUsed++;
                _lastBarks[dogIndex] = Time.time;
                RequestAudioCue(ArenaFeedbackCatalog.Bark);
                RequestRumble("bark", 0.08f, 0.18f, 0.08f);
                LogPlaytestEvent("Bark", DogName(_dogs[dogIndex]));
                EndLeadIn($"{DogName(_dogs[dogIndex])} barked ready");
                return;
            }

            BarksUsed++;
            CreditDog(dogIndex);
            _lastBarks[dogIndex] = Time.time;
            var dog = _dogs[dogIndex];
            bool barkDidSomething = false;
            RequestAudioCue(ArenaFeedbackCatalog.Bark);
            RequestRumble("bark", 0.08f, 0.18f, 0.08f);
            LogPlaytestEvent("Bark", DogName(dog));

            if (_activeMissionController != null)
            {
                barkDidSomething = _activeMissionController.HandleBark(dogIndex);
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
            ScareSquirrel(_tuning.UnitedBarkScareSeconds, "United bark shook the whole yard!", false);
            LogPlaytestEvent("UnitedBark", $"{UnitedBarks} total");

            if (Phase == State.PredatorWarning || Phase == State.PredatorAttack) ResolvePredator();
            if (_activeMissionController is IMissionUnitedBarkListener unitedBarkListener)
            {
                unitedBarkListener.OnUnitedBark();
                CheckClear();
            }
        }

        /// <summary>Resolves a wrestle attempt against the attacker's sibling: range/immunity gates
        /// (mirrors the prototype's canWrestle/doWrestle in src/systems/wrestle.ts), then an
        /// asymmetric reversal-odds roll (Cocoa 0.78 / Cheddar 0.70 attacker win chance) that stuns
        /// and knocks back the loser, and a dust burst on both dogs at the point of impact. Still no
        /// spot/couch steal-on-win - that needs a cuddle-spot system this arena doesn't have, tracked
        /// as the one remaining follow-up in docs/ARENA-PLAYABLE.md.</summary>
        private void OnDogWrestled(DogId dogId)
        {
            if (!MissionActive()) return;
            TryRecordTutorialAction(dogId, TutorialActionStep.Wrestle);

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0 || _dogs.Length < 2) return;
            int partnerIndex = dogIndex == 0 ? 1 : 0;

            var attacker = _dogs[dogIndex];
            var defender = _dogs[partnerIndex];
            if (attacker == null || defender == null) return;

            // Defender mid-tug/swim/stunned/transit: nothing to wrestle right now, same as the
            // prototype's canWrestle gate on both sides.
            if (defender.Busy)
            {
                attacker.ApplyWrestleCooldown(defender.WrestleWhiffCooldownSeconds);
                return;
            }

            if (defender.Immune)
            {
                attacker.ApplyWrestleCooldown(attacker.WrestleImmuneBlockedCooldownSeconds);
                LastCue = $"{DogName(defender)} is too cozy to flip!";
                LogPlaytestEvent("WrestleBlocked", LastCue);
                return;
            }

            if (Vector2.Distance(attacker.transform.position, defender.transform.position) > attacker.WrestleRange)
            {
                // Just out of range: lunge toward the sibling instead of a silent no-op, matching the
                // prototype's near-miss nudge - a whiff should still read as a real attempt.
                attacker.ApplyWrestleLunge(defender.transform.position - attacker.transform.position);
                attacker.ApplyWrestleCooldown(attacker.WrestleWhiffCooldownSeconds);
                return;
            }

            attacker.ApplyWrestleCooldown(attacker.WrestleCooldownSeconds);
            bool attackerWins = _rng.NextDouble() < attacker.WrestleWinChance;
            var winner = attackerWins ? attacker : defender;
            var loser = attackerWins ? defender : attacker;

            Vector2 knockDir = (Vector2)loser.transform.position - (Vector2)winner.transform.position;
            if (knockDir.sqrMagnitude < 0.0001f) knockDir = Vector2.right;
            knockDir.Normalize();
            loser.ApplyWrestleStun(loser.WrestleLoserStunSeconds, knockDir * loser.WrestleKnockbackSpeed);
            winner.DampVelocity(winner.WrestleWinnerDamp);

            if (DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ActionFeedback?.Trigger(DogFeedbackAction.Wrestle);
            if (DogFeedback[partnerIndex] != null) DogFeedback[partnerIndex].ActionFeedback?.Trigger(DogFeedbackAction.Wrestle);

            LastFeedback = FeedbackKind.WrestleFlip;
            LastCue = attackerWins
                ? $"{DogName(winner)} flipped {DogName(loser)}!"
                : $"{DogName(loser)} tried to flip {DogName(winner)} - REVERSAL!";
            SetJuice(JuiceFeedbackKind.WarningMiss, attackerWins ? $"{DogName(winner).ToUpperInvariant()} FLIPPED THEM!" : "REVERSAL!");
            SpawnWorldPop((winner.transform.position + loser.transform.position) * 0.5f,
                attackerWins ? "FLIP!" : "REVERSAL!", new Color(1f, 0.65f, 0.35f));
            RequestAudioCue(ArenaFeedbackCatalog.SquirrelStunned);
            RequestRumble("wrestle", 0.22f, 0.4f, 0.16f);
            LogPlaytestEvent("Wrestle", LastCue);
        }

        /// <summary>Jump has no gameplay resolution here (DogController.Jump already ran the arc
        /// hop) - this only records the currently taught action for that specific player.</summary>
        private void OnDogJumped(DogId dogId)
        {
            if (!MissionActive()) return;
            TryRecordTutorialAction(dogId, TutorialActionStep.Jump);
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
            if (awardScore) RequestAudioCue(ArenaFeedbackCatalog.SquirrelStunned);
            LogPlaytestEvent(awardScore ? "SquirrelScared" : "SquirrelUnitedScare", LastCue);
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
            RequestAudioCue(ArenaFeedbackCatalog.ToySqueak);
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
            bool hasItems = BreakfastRecovered >= _mission.ItemGoal;
            bool hasPredator = !_mission.RequiresPredator || PredatorResolved;
            bool hasTug = !_mission.RequiresTug || TugComplete;
            if (hasItems && hasPredator && hasTug) EndRound(true);
        }

        private void EndRound(bool clear)
        {
            _leadInRemaining = 0f;
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
                RequestAudioCue(ArenaFeedbackCatalog.StarAppear);
                RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
                RequestRumble("mission_win", 0.42f, 0.68f, 0.24f);
                // A flawless clear is the best outcome on offer; let the camera celebrate harder than
                // a scrappy win, same asymmetry as the fail shake being bigger than a clean clear.
                RequestShake(LastRoundFlawless ? 0.28f : 0.18f);
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
                RequestShake(0.32f);
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
            _activeMissionController?.Cleanup();
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
            else
                funny = Outcome.ToString();
            return $"{funny}: {Score} - {EndRank}";
        }

        private bool MissionActive() => Phase == State.Playing || Phase == State.PredatorWarning || Phase == State.PredatorAttack;

        private void AddScore(int delta, string reason)
        {
            _guidance.NotifyProgress();
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
            _guidance.NotifyProgress();
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
            RequestAudioCue(ArenaFeedbackCatalog.UiButtonDisabled);
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
            if (kb != null && kb.f4Key.wasPressedThisFrame) RecordColdReadQuestion();

            if (MissionSelectVisible)
            {
                bool up = false;
                bool down = false;
                bool left = false;
                bool right = false;
                bool next = false;
                bool focus = false;
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
                    focus |= kb.f5Key.wasPressedThisFrame || kb.pKey.wasPressedThisFrame;
                    if (kb.f6Key.wasPressedThisFrame || kb.kKey.wasPressedThisFrame)
                    {
                        SelectKitchenShowcaseMission();
                        return;
                    }
                    if (kb.f7Key.wasPressedThisFrame || kb.bKey.wasPressedThisFrame)
                    {
                        SelectBackyardShowcaseMission();
                        return;
                    }
                    if (kb.f8Key.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                    {
                        SelectWeenieShowcaseMission();
                        return;
                    }
                    if (kb.f9Key.wasPressedThisFrame || kb.lKey.wasPressedThisFrame)
                    {
                        SelectWalkiesShowcaseMission();
                        return;
                    }
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
                    focus |= pad.buttonNorth.wasPressedThisFrame;
                    start |= pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame;
                }

                if (up) SelectMissionAbove();
                else if (down) SelectMissionBelow();
                else if (left) SelectMissionLeft();
                else if (right) SelectMissionRight();
                else if (next) SelectNextMission();
                else if (focus) SelectCouchTestFocusMission();
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
            _leadInRemaining = 0f;
            CurrentFlow = FlowState.MissionSelect;
            Phase = State.Intro;
            Outcome = MissionOutcome.InProgress;
            TimeRemaining = 0f;
            Score = 0;
            LastScoreDelta = 0;
            UnitedBarks = 0;
            _breakfastRecovered = 0;
            _stolenFood = 0;
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
            ResetActionTutorialProgress();
            _guidance.Reset();
            ResetGuidancePresentation();
            _scorePopUntil = 0f;
            _handoffFlashUntil = 0f;
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
            SessionUniqueMissionsCleared = CountClearedMissions();
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
            SessionSummaryLabel = $"{lead} {SessionMissionsPlayed} missions played, {SessionTotalScore} score, {SessionStarsEarned} stars, {SessionFlawlessClears} flawless, {SessionUniqueMissionsCleared}/{MissionOrder.Length} finished.";
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

        private int CountClearedMissions()
        {
            int count = 0;
            for (int i = 0; i < _sessionClearedMissions.Length; i++)
            {
                if (_sessionClearedMissions[i]) count++;
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
            if (!active) _activeMissionController?.Cleanup();
        }

        public static MissionDefinition BuildMissionDefinition(MissionVariant variant) =>
            BuildMissionDefinition(variant, ArenaMissionTuning.CreateDefault());

        private static MissionDefinition BuildMissionDefinition(MissionVariant variant, ArenaMissionTuning tuning)
        {
            if (MissionCatalog.TryBuild(variant, tuning, out var registeredDefinition))
                return registeredDefinition;

            throw new System.InvalidOperationException($"No MissionDefinition registered for {variant}. Add it to MissionCatalog.");
        }

        private void EnsureActionTutorialProgress()
        {
            int dogCount = _dogs != null ? _dogs.Length : 0;
            if (_tutorialActionDone == null || _tutorialActionDone.GetLength(0) != dogCount)
                _tutorialActionDone = new bool[dogCount, TutorialActionCount];
        }

        private void ResetActionTutorialProgress()
        {
            EnsureActionTutorialProgress();
            if (_tutorialActionDone.Length > 0)
                System.Array.Clear(_tutorialActionDone, 0, _tutorialActionDone.Length);
        }

        private bool TutorialActionDoneForAll(TutorialActionStep action)
        {
            if (action == TutorialActionStep.Complete) return CurrentTutorialAction == TutorialActionStep.Complete;
            if (_dogs == null || _dogs.Length == 0 || _tutorialActionDone == null ||
                _tutorialActionDone.GetLength(0) != _dogs.Length) return false;

            int actionIndex = (int)action;
            for (int dog = 0; dog < _dogs.Length; dog++)
                if (!_tutorialActionDone[dog, actionIndex]) return false;
            return true;
        }

        private bool TryRecordTutorialAction(DogId dogId, TutorialActionStep action)
        {
            if (!ActionTutorialAvailable || CurrentTutorialAction != action) return false;
            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return false;

            EnsureActionTutorialProgress();
            int actionIndex = (int)action;
            if (_tutorialActionDone[dogIndex, actionIndex]) return false;

            _tutorialActionDone[dogIndex, actionIndex] = true;
            string detail = TutorialActionDoneForAll(action)
                ? $"both players completed {action}"
                : $"{dogId} completed {action}; waiting for partner";
            LogPlaytestEvent("Tutorial", detail);
            return true;
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

        private const float GuidanceWideLabelRange = 1000f;
        private const float GuidanceNudgeIntervalSeconds = 1.2f;

        /// <summary>
        /// Renders the guidance-escalation ladder (see MissionGuidanceEscalation): Tier 1 brightens
        /// the current objective arrow/breadcrumbs, periodically pulses the objective prop, and turns
        /// the acting dog's head toward it; Tier 2 lifts the proximity gate on that objective's world
        /// label; Tier 3 flags the HUD objective line (rendered in ArenaHud) and fires one placeholder
        /// audio cue on the tier-up edge. Tier 0 leaves every one of these untouched/reverted.
        /// </summary>
        private void UpdateGuidancePresentation()
        {
            if (ObjectiveArrows == null || _dogs == null) return;

            bool has0 = TryGetObjectiveTarget(0, out var target0, out _, out _);
            bool has1 = TryGetObjectiveTarget(1, out var target1, out _, out _);
            _guidanceOwningDogIndex = ResolveGuidanceOwningDogIndex(has0, has1);

            int tier = GuidanceTier;
            bool emphasize = tier >= 1;
            for (int i = 0; i < ObjectiveArrows.Length; i++)
                ObjectiveArrows[i]?.SetEmphasis(emphasize);

            if (tier >= 1 && Time.time >= _guidanceNudgeAt)
            {
                _guidanceNudgeAt = Time.time + GuidanceNudgeIntervalSeconds;
                if (has0) NudgeTowardGuidanceTarget(0, target0);
                if (has1) NudgeTowardGuidanceTarget(1, target1);
                if (has0 && target0 != null) target0.GetComponent<MissionPropArtAttachment>()?.Pulse(0.3f, 0.12f);
                if (has1 && target1 != null && target1 != target0)
                    target1.GetComponent<MissionPropArtAttachment>()?.Pulse(0.3f, 0.12f);
            }

            UpdateGuidanceLabelGate(0, tier >= 2 && has0 ? target0 : null);
            UpdateGuidanceLabelGate(1, tier >= 2 && has1 ? target1 : null);
            UpdateRoleTurnBeacon(tier, has0, target0, has1, target1);

            if (tier >= 3 && _guidanceLastTier < 3) RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _guidanceLastTier = tier;
        }

        /// <summary>
        /// Unambiguous only when exactly one dog has an objective target this frame. Most missions
        /// hand both dogs a target at once (with different copy telling one to stand down), which
        /// this deliberately does not try to disambiguate by parsing copy text - see the caveat on
        /// GuidanceOwningDogIndex. Pure/static so it's directly testable without a scene.
        /// </summary>
        public static int? ComputeGuidanceOwningDogIndex(bool dog0HasTarget, bool dog1HasTarget) =>
            dog0HasTarget && !dog1HasTarget ? 0 : dog1HasTarget && !dog0HasTarget ? (int?)1 : null;

        /// <summary>
        /// Prefers a controller's explicit <see cref="IMissionRoleOwner"/> signal (hard-handoff
        /// puzzles that already track a single current actor internally) over the generic
        /// presence/absence heuristic, which most missions fall through to.
        /// </summary>
        private int? ResolveGuidanceOwningDogIndex(bool has0, bool has1)
        {
            if (_activeMissionController is IMissionRoleOwner roleOwner && roleOwner.RoleOwnerDog.HasValue)
            {
                int index = IndexOfDog(roleOwner.RoleOwnerDog.Value);
                if (index >= 0) return index;
            }

            return ComputeGuidanceOwningDogIndex(has0, has1);
        }

        private static readonly Color GuidanceBeaconCheddarColor = new Color(1f, 0.55f, 0.1f);
        private static readonly Color GuidanceBeaconCocoaColor = new Color(0.42f, 0.27f, 0.14f);

        private void UpdateRoleTurnBeacon(int tier, bool has0, Transform target0, bool has1, Transform target1)
        {
            if (_roleTurnBeacon == null) return;

            bool eligible = _guidanceOwningDogIndex.HasValue &&
                (tier >= 1 || (_mission != null && _mission.GuidanceBeaconAlwaysOn));
            if (!eligible)
            {
                _roleTurnBeacon.Hide();
                return;
            }

            int owner = _guidanceOwningDogIndex.Value;
            if (owner < 0 || owner >= _dogs.Length || _dogs[owner] == null)
            {
                _roleTurnBeacon.Hide();
                return;
            }

            Transform beaconTarget = owner == 0 ? (has0 ? target0 : null) : (has1 ? target1 : null);
            if (beaconTarget == null)
            {
                _roleTurnBeacon.Hide();
                return;
            }

            bool isCheddar = _dogs[owner].TryGetComponent<DogIdentity>(out var identity) && identity.Id == DogId.Cheddar;
            _roleTurnBeacon.Show(beaconTarget, isCheddar ? GuidanceBeaconCheddarColor : GuidanceBeaconCocoaColor);
        }

        private void NudgeTowardGuidanceTarget(int dogIndex, Transform target)
        {
            if (target == null || dogIndex < 0 || dogIndex >= _dogs.Length) return;
            var feedback = DogFeedback != null && dogIndex < DogFeedback.Length ? DogFeedback[dogIndex] : null;
            if (feedback == null || _dogs[dogIndex] == null) return;
            feedback.ShowGuidanceNudge(target.position - _dogs[dogIndex].transform.position);
        }

        private void UpdateGuidanceLabelGate(int dogIndex, Transform target)
        {
            TextMesh desired = target != null ? target.GetComponentInChildren<TextMesh>() : null;
            TextMesh current = _guidanceWidenedLabels[dogIndex];
            if (current == desired) return;

            if (current != null) WorldLabelVisibility.Attach(current, WorldLabelVisibility.DefaultPromptRange);
            if (desired != null) WorldLabelVisibility.Attach(desired, GuidanceWideLabelRange);
            _guidanceWidenedLabels[dogIndex] = desired;
        }

        private void ResetGuidancePresentation()
        {
            _guidanceOwningDogIndex = null;
            _guidanceLastTier = 0;
            _guidanceNudgeAt = 0f;
            for (int i = 0; i < _guidanceWidenedLabels.Length; i++)
            {
                if (_guidanceWidenedLabels[i] != null)
                    WorldLabelVisibility.Attach(_guidanceWidenedLabels[i], WorldLabelVisibility.DefaultPromptRange);
                _guidanceWidenedLabels[i] = null;
            }
            _roleTurnBeacon?.Hide();
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

            return false;
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
            var nearestTreat = FindNearestTreat(_bounds.center);
            return nearestTreat != null ? (Vector2)nearestTreat.transform.position : _bounds.center;
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

            bool squirrelStealing = BackyardRescueController?.IsSquirrelStealing == true || _squirrelTarget != null;
            if (_mission.UsesSquirrel && squirrelStealing && SquirrelObject != null)
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
            AttachCollectiblePropArt(go);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var treat = go.AddComponent<Treat>();
            treat.Bind(this);
            _treats.Add(treat);

            AddWorldLabel(go, _mission.ItemWorldLabel, Vector3.up * 2.2f, 16, Color.white);
            if (_activeMissionController is IMissionTreatCollector { SpawnTreatsHidden: true }) go.SetActive(false);
        }

        private void BuildCollectibleArt(GameObject go, CollectibleVisualSlot art)
        {
            foreach (var part in art.Parts)
            {
                AddActorPart(go, part, _sprite,
                    part.ResolveColor(_mission.ItemColor, _mission.ItemAccentColor, _mission.ItemSecondaryColor));
            }
        }

        private void AttachCollectiblePropArt(GameObject go)
        {
            string path = _mission.Variant switch
            {
                MissionVariant.SnackHeist => FinalGameplayArt.MissionSnackPlate,
                MissionVariant.SockPanic => FinalGameplayArt.MissionSockBundle,
                MissionVariant.BlanketCatch => FinalGameplayArt.MissionFallingSnack,
                _ => null
            };
            if (!string.IsNullOrEmpty(path))
                MissionPropArt.AttachObject(go, path, 0.013f, 18, true);
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
            _bunnyCameoObject = MakeDraftBunnyCameo();
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

            // The placeholder rig keeps its authored non-uniform BodyScale, but on a child object.
            // Putting BodyScale on the root squashed everything parented to the actor afterwards
            // (authored motion frames, mission prop art, labels, range rings) into skewed sprites.
            var body = new GameObject(ArenaArtCatalog.PlaceholderBodyName);
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = art.BodyScale;

            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = art.RootColor;
            sr.sortingOrder = 6;

            BuildActorArt(body, art);
            var threatAnimator = AddThreatAnimator(go, art);
            AddWorldLabel(go, art.Label, art.LabelOffset, 24, Color.white);
            go.AddComponent<MissionActorFeedback>().Init(sr, art.Label, art.PulseAmount, art.RotationPerSecond);
            threatAnimator?.SetLabelState(art.Label);
            var range = go.AddComponent<InteractionRangeIndicator>();
            range.Init(_rangeSprite, new Color(1f, 1f, 1f, 0.35f), "RANGE");
            return go;
        }

        private static ThreatReadabilityAnimator AddThreatAnimator(GameObject go, ActorVisualSlot art)
        {
            ThreatMotionArt.Actor defaultActor = art.ObjectName == "Squirrel"
                ? ThreatMotionArt.Actor.Squirrel
                : art.ObjectName == "Predator Warning"
                    ? ThreatMotionArt.Actor.Eagle
                : ThreatMotionArt.Actor.Unknown;
            bool supportsThreatMotion = defaultActor != ThreatMotionArt.Actor.Unknown;
            if (!supportsThreatMotion) return null;

            var fallbackRenderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            var animator = go.AddComponent<ThreatReadabilityAnimator>();
            // RootScale is the authored-motion size knob now that the root itself stays unscaled.
            animator.Init(defaultActor, fallbackRenderers, art.RootScale);
            return animator;
        }

        private void BuildActorArt(GameObject body, ActorVisualSlot art)
        {
            foreach (var part in art.Parts)
            {
                AddActorPart(body, part, _sprite, part.Color);
            }
            AddDraftActorBadges(body, art);
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
            WorldLabelSkin.Attach(label, scorePop: false);
            WorldLabelVisibility.Attach(label);
            return label;
        }

        private static void SetActorState(GameObject go, string label, Color color, float pulse)
        {
            if (go == null) return;
            if (go.TryGetComponent<MissionActorFeedback>(out var feedback)) feedback.SetState(label, color, pulse);
            if (go.TryGetComponent<ThreatReadabilityAnimator>(out var animator)) animator.SetLabelState(label);
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
            WorldLabelSkin.Attach(label, scorePop: true);
            go.AddComponent<MissionWorldPop>().Begin(label);
        }

        private void PlaceObject(GameObject go, Vector2 position)
        {
            if (go != null) go.transform.position = position;
        }

        private static void ClearSharedActorOverride(GameObject actor)
        {
            if (actor != null && actor.TryGetComponent<MissionPropArtAttachment>(out var attachment))
                attachment.ClearOverride();
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
            {
                AudioClip[] bank = LoadAuthoredArenaSfxBank(cue);
                if (bank.Length == 0) bank = new[] { MakeGeneratedArenaSfxClip(cue) };
                _audioClipBanks[cue.Name] = bank;
                _audioClips[cue.Name] = bank[0];
            }

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

        private static AudioClip[] LoadAuthoredArenaSfxBank(AudioCueSlot cue)
        {
            var paths = AuthoredAudioCatalog.CueBankFor(cue.Name);
            var clips = new List<AudioClip>(paths.Count);
            foreach (string path in paths)
            {
                AudioClip clip = Resources.Load<AudioClip>(path);
                if (clip != null) clips.Add(clip);
            }

            return clips.ToArray();
        }

        private static AudioClip MakeGeneratedArenaSfxClip(AudioCueSlot cue)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * cue.Seconds);
            var samples = new float[sampleCount];
            uint noise = 2166136261u ^ (uint)(cue.Name.Length * 16777619);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float progress = i / Mathf.Max(1f, sampleCount - 1f);
                float envelope = AttackReleaseEnvelope(progress, 0.08f, 1.8f);
                float hz = Mathf.Max(40f, cue.Frequency * (1f + cue.Sweep * progress));
                float white = NextNoise(ref noise) * cue.Noise;
                float wave = SfxWave(cue.Kind, hz, t, progress, white);
                samples[i] = Mathf.Clamp(wave * envelope * cue.Volume, -1f, 1f);
            }

            var clip = AudioClip.Create(cue.Name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float AttackReleaseEnvelope(float progress, float attack, float releasePower)
        {
            float a = Mathf.Clamp01(progress / Mathf.Max(0.001f, attack));
            float r = Mathf.Pow(1f - progress, releasePower);
            return Mathf.Clamp01(a * r);
        }

        private static float NextNoise(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return ((state & 0xffffu) / 32767.5f) - 1f;
        }

        private static float SfxWave(ArenaFeedbackCatalog.GeneratedSfxKind kind, float hz, float t, float progress, float noise)
        {
            float main = Mathf.Sin(2f * Mathf.PI * hz * t);
            float harmonic = Mathf.Sin(2f * Mathf.PI * hz * 2.01f * t) * 0.32f;
            float sub = Mathf.Sin(2f * Mathf.PI * hz * 0.5f * t) * 0.28f;
            float chirp = Mathf.Sin(2f * Mathf.PI * hz * (1f + progress * 2.2f) * t);

            switch (kind)
            {
                case ArenaFeedbackCatalog.GeneratedSfxKind.DogBark:
                    return Mathf.Sign(main + harmonic * 0.7f) * (0.62f + 0.25f * Mathf.Sin(progress * Mathf.PI * 3f)) + noise * 0.38f;
                case ArenaFeedbackCatalog.GeneratedSfxKind.TeamSuccess:
                    return main * 0.45f + Mathf.Sin(2f * Mathf.PI * hz * 1.5f * t) * 0.3f + chirp * 0.18f;
                case ArenaFeedbackCatalog.GeneratedSfxKind.CrunchCollect:
                    return main * 0.18f + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * hz * 3f * t)) * 0.22f + noise * 0.6f;
                case ArenaFeedbackCatalog.GeneratedSfxKind.SquirrelAlarm:
                    return chirp * 0.45f + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * hz * 5f * t)) * 0.24f + noise * 0.5f;
                case ArenaFeedbackCatalog.GeneratedSfxKind.ScoreSparkle:
                    return chirp * 0.42f + Mathf.Sin(2f * Mathf.PI * hz * 2.5f * t) * 0.3f + noise * 0.12f;
                case ArenaFeedbackCatalog.GeneratedSfxKind.PenaltyThunk:
                    return sub * (1f - progress) + main * 0.16f + noise * 0.26f;
                case ArenaFeedbackCatalog.GeneratedSfxKind.VictoryFanfare:
                    return main * 0.3f + Mathf.Sin(2f * Mathf.PI * hz * 1.25f * t) * 0.28f
                        + Mathf.Sin(2f * Mathf.PI * hz * 1.5f * t) * 0.22f + chirp * 0.16f;
                case ArenaFeedbackCatalog.GeneratedSfxKind.FailureSigh:
                    return sub * 0.44f + main * 0.18f + noise * 0.28f;
                case ArenaFeedbackCatalog.GeneratedSfxKind.UiBlip:
                    return main * 0.55f + chirp * 0.2f;
                case ArenaFeedbackCatalog.GeneratedSfxKind.ThreatRattle:
                    return sub * 0.35f + Mathf.Sign(main) * 0.24f + noise * 0.72f;
                default:
                    return main + noise;
            }
        }

        private void RequestAudioCue(string cueName)
        {
            if (!AudioEnabled || string.IsNullOrEmpty(cueName)) return;
            if (!_audioSlots.ContainsKey(cueName)) return;

            RequestSemanticCompanionCue(cueName);
            LastAudioCueRequested = cueName;
            _audioCueRequests.Add(cueName);
            AudioClip clip = NextAudioClip(cueName);
            LastAudioClipPlayed = clip != null ? clip.name : string.Empty;
            if (_audio != null && clip != null)
                _audio.PlayOneShot(clip);
        }

        private void RequestSemanticCompanionCue(string cueName)
        {
            string[] companions = cueName switch
            {
                ArenaFeedbackCatalog.SnackSockCollect => new[] { ArenaFeedbackCatalog.EatingGulp },
                ArenaFeedbackCatalog.TugRescueSuccess => new[] { ArenaFeedbackCatalog.ToySqueak },
                ArenaFeedbackCatalog.SquirrelStealMiss => new[]
                {
                    ArenaFeedbackCatalog.SquirrelChatter,
                    ArenaFeedbackCatalog.SquirrelEscapeLaugh
                },
                ArenaFeedbackCatalog.MissionWin => new[] { ArenaFeedbackCatalog.StarAppear },
                _ => null
            };
            if (companions == null) return;
            foreach (string companion in companions)
            {
                if (string.IsNullOrEmpty(companion) || _audioCueRequests.Contains(companion)) continue;
                RequestAudioCue(companion);
            }
        }

        private AudioClip NextAudioClip(string cueName)
        {
            if (!_audioClipBanks.TryGetValue(cueName, out var bank) || bank == null || bank.Length == 0)
                return _audioClips.TryGetValue(cueName, out var fallback) ? fallback : null;

            _audioBankIndices.TryGetValue(cueName, out int index);
            AudioClip clip = bank[Mathf.Abs(index) % bank.Length];
            _audioBankIndices[cueName] = index + 1;
            return clip;
        }

        private void RequestRumble(string requestName, float lowFrequency, float highFrequency, float seconds)
        {
            if (!RumbleEnabled || string.IsNullOrEmpty(requestName)) return;

            LastRumbleRequested = requestName;
            _rumbleRequests.Add(requestName);
            CancelInvoke(nameof(StopRumble));
            StopRumble();
            ForEachPlayerGamepad(pad =>
            {
                pad.SetMotorSpeeds(lowFrequency, highFrequency);
                _activeRumbleDeviceIds.Add(pad.deviceId);
            });
            if (_activeRumbleDeviceIds.Count > 0)
                Invoke(nameof(StopRumble), Mathf.Max(0.01f, seconds));
        }

        /// <summary>Cosmetic camera kick, mirroring RequestRumble's controller kick - purely additive,
        /// no effect if no camera is wired.</summary>
        private void RequestShake(float magnitude)
        {
            if (!CameraShakeEnabled || magnitude <= 0f) return;
            LastShakeMagnitude = magnitude;
            ShakeRequestCount++;
            _camera?.AddShake(magnitude);
        }

        /// <summary>
        /// Handoff flip flourish (G1.4): a baton-swoosh visual between the two dogs, a brief pulse on
        /// both HUD identity chips, and one placeholder audio cue (a dedicated cue is S5.1). Mission
        /// controllers call this at their own existing role-flip moments - this method owns none of
        /// that timing, only the shared presentation.
        /// </summary>
        private void SignalRoleHandoff(DogId fromDog, DogId toDog)
        {
            if (_dogs == null) return;
            int fromIndex = IndexOfDog(fromDog);
            int toIndex = IndexOfDog(toDog);
            if (fromIndex < 0 || toIndex < 0 || _dogs[fromIndex] == null || _dogs[toIndex] == null) return;

            Sprite swooshSprite = FinalGameplayArt.Load(FinalGameplayArt.DogFxChaosSpark);
            DogHandoffSwoosh.Spawn(swooshSprite, _dogs[fromIndex].transform.position, _dogs[toIndex].transform.position);
            _handoffFlashUntil = Time.time + 0.6f;
            RequestAudioCue(ArenaFeedbackCatalog.UiReplayNextSelect);
            LastHandoffFromDog = fromDog;
            LastHandoffToDog = toDog;
            HandoffSignalCount++;
        }

        private void StopRumble()
        {
            ForEachPlayerGamepad(pad => pad.SetMotorSpeeds(0f, 0f));
            _activeRumbleDeviceIds.Clear();
        }

        private void ForEachPlayerGamepad(Action<Gamepad> apply)
        {
            _rumbleDispatchDeviceIds.Clear();
            if (_inputs != null)
            {
                foreach (var input in _inputs)
                {
                    if (input == null || !input.HasBoundGamepad) continue;
                    int deviceId = input.BoundGamepadDeviceId;
                    if (!_rumbleDispatchDeviceIds.Add(deviceId)) continue;
                    if (InputSystem.GetDeviceById(deviceId) is Gamepad pad && pad.added) apply(pad);
                }
            }

            // Keyboard-only rigs and early startup can have no explicit player binding. Preserve
            // the old current-pad fallback in that case, but never let it replace a bound P1/P2 pad.
            if (_rumbleDispatchDeviceIds.Count == 0 && Gamepad.current != null && Gamepad.current.added)
            {
                _rumbleDispatchDeviceIds.Add(Gamepad.current.deviceId);
                apply(Gamepad.current);
            }
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
                dog.OnWrestle -= OnDogWrestled;
                dog.OnJump -= OnDogJumped;
            }
        }
    }
}
