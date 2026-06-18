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
        public enum MissionVariant { BackyardRescue, SnackHeist, SockPanic, SquirrelConspiracy }
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
            MissionVariant.SquirrelConspiracy
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
        public MissionRuntimeSnapshot RuntimeSnapshot => BuildRuntimeSnapshot();
        public int SelectedMissionIndex => _selectedMissionIndex;
        public MissionVariant SelectedMissionVariant => MissionOrder[Mathf.Clamp(_selectedMissionIndex, 0, MissionOrder.Length - 1)];
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
        public MissionVariant ActiveMissionVariant => _mission != null ? _mission.Variant : startingMission;
        public string ActiveMissionName => _mission != null ? _mission.Name : "Backyard Rescue";
        public string MissionItemPlural => _mission != null ? _mission.ItemRootName : "Breakfast/Weenies";
        public string MissionIntroPrompt => _mission != null ? _mission.IntroPrompt : "Cheddar + Cocoa must protect the weenies together.";
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
        public int SessionUniqueMissionsCompleted { get; private set; }
        public bool SessionSummaryReady => SessionUniqueMissionsCompleted >= 3;
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
        public FeedbackKind LastFeedback { get; private set; } = FeedbackKind.Intro;
        public JuiceFeedbackKind LastJuiceFeedback { get; private set; } = JuiceFeedbackKind.None;
        public string LastJuiceLabel { get; private set; } = string.Empty;
        public GameObject SquirrelObject { get; private set; }
        public GameObject PredatorObject { get; private set; }
        public GameObject RopeObject { get; private set; }
        public DogReadabilityFeedback[] DogFeedback { get; private set; }
        public ObjectiveArrowFeedback[] ObjectiveArrows { get; private set; }
        public InteractionRangeIndicator[] InteractionRangeIndicators { get; private set; }

        private DogController[] _dogs;
        private GamepadPlayerInput[] _inputs;
        private Vector2[] _dogStarts;
        private Sprite _sprite;
        private Sprite _rangeSprite;
        private Rect _bounds;
        private System.Random _rng;
        private Transform _treatRoot;
        private AudioSource _audio;
        private readonly Dictionary<string, AudioClip> _audioClips = new();
        private readonly Dictionary<string, AudioCueSlot> _audioSlots = ArenaFeedbackCatalog.BuildLookup();
        private readonly List<string> _audioCueRequests = new();
        private readonly List<string> _rumbleRequests = new();
        private MissionDefinition _mission;
        private readonly HerdingMissionState _herdingState = new HerdingMissionState();
        private readonly Vector2[] _squirrelRoute = { new(-5.8f, 2.8f), new(0f, -2.6f), new(5.8f, 2.8f), new(4.6f, -2.4f) };
        private Vector2 _stashPosition;

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
        private readonly int[] _sessionFailuresByMission = new int[MissionOrder.Length];
        private bool _roundResultRecorded;
        private string _lastLoggedObjective = string.Empty;

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
            _mission = BuildMissionDefinition(startingMission, _tuning);
            _selectedMissionIndex = IndexOfMission(startingMission);
            _treatRoot = new GameObject(_mission.ItemRootName).transform;
            BuildAudio();
            BuildMissionObjects();
            HideInteractionRanges();
            ShowMissionSelect();
        }

        public void OnTreatCollected(Treat treat, DogController dog)
        {
            if (!MissionActive() || treat == null) return;

            AddScore(_mission.ItemScore, _mission.CollectedScoreLabel);
            BreakfastRecovered++;
            LastCue = $"{DogName(dog)} recovered {_mission.ItemCollectCueNoun}!";
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
            StartMission(_mission.Variant);
        }

        public void StartMission(MissionVariant variant)
        {
            SelectMission(variant);
            _mission = BuildMissionDefinition(variant, _tuning);
            BeginRound();
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

        public void StartSelectedMission() => StartMission(SelectedMissionVariant);

        public void ReturnToMissionSelect()
        {
            RequestAudioCue(ArenaFeedbackCatalog.UiReplayNextSelect);
            ShowMissionSelect();
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

        public void ShowSessionSummary()
        {
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

            _rng = new System.Random(MissionSeedGenerator.StableSeed(_mission.Variant.ToString(), SessionMissionsPlayed, _selectedMissionIndex));
            ActiveModifier = (RoundModifier)_rng.Next(0, 3);
            _herdingState.Reset();
            _stashPosition = new Vector2(_bounds.xMax - 1.7f, _bounds.yMin + 1.7f);
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
                if (DogFeedback[i] != null) DogFeedback[i].ClearMissionPose();
                _dogs[i].transform.position = _dogStarts[i];
                if (_dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
                if (i < _inputs.Length && _inputs[i] != null) _inputs[i].enabled = true;
                if (ObjectiveArrows[i] != null) ObjectiveArrows[i].Hide();
            }

            ClearTreats();
            for (int i = 0; i < treatCount; i++) SpawnTreat();

            PlaceObject(SquirrelObject, _mission.Variant == MissionVariant.SquirrelConspiracy ? _squirrelRoute[0] : new Vector2(_bounds.xMax - 2f, _bounds.yMax - 2f));
            PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
            PlaceObject(RopeObject, Vector2.zero);
            SquirrelObject.SetActive(_mission.UsesSquirrel);
            PredatorObject.SetActive(_mission.RequiresPredator);
            RopeObject.SetActive(_mission.RequiresTug);
            if (_mission.UsesSquirrel) SetActorState(SquirrelObject, _mission.Variant == MissionVariant.SquirrelConspiracy ? "SQUIRREL CONSPIRACY ROUTE 1" : "Squirrel: WAITING", new Color(0.55f, 0.32f, 0.12f), 0.06f);
            if (_mission.RequiresPredator) SetActorState(PredatorObject, "Predator: OFFSCREEN", Color.gray, 0.04f);
            if (_mission.RequiresTug) SetActorState(RopeObject, "Rope/Tug - BOTH DOGS", new Color(0.95f, 0.7f, 0.15f), 0.08f);
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
            if (_mission.Variant == MissionVariant.SquirrelConspiracy) TickSquirrelConspiracy();
            else TickSquirrel();
            TickPredator();
            TickTugProximity();
            CheckClear();
            UpdateObjectiveArrows();
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
                LogPlaytestEvent("SquirrelFakeOut", LastCue);
                return false;
            }

            bool cutoff = _dogs.Length > 1 && Vector2.Distance(_dogs[0].transform.position, _dogs[1].transform.position) >= 2.25f;
            var scoreEvent = cutoff ? ScoreEventCatalog.Cutoff : ScoreEventCatalog.GoodHerd;
            if (cutoff) _herdingState.AddCutoff();
            else _herdingState.AddHerd();
            _herdingState.AdvanceRoute(_squirrelRoute.Length);
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
                AddScore(ScoreEventCatalog.DoubleBarkBlock.Points, ScoreEventCatalog.DoubleBarkBlock.Label);
                PlaceObject(SquirrelObject, _stashPosition + Vector2.left * 1.2f);
                SetActorState(SquirrelObject, "STASH REVEALED - SNIFF + INTERACT!", new Color(1f, 0.72f, 0.18f), 0.34f);
                LastCue = "The squirrel stash is exposed! Get a dog to the stash and interact.";
                LogPlaytestEvent("SquirrelStashRevealed", LastCue);
            }

            LogObjectiveIfChanged();
            return true;
        }

        private void RegisterSquirrelTaunt()
        {
            _herdingState.AddTaunt();
            AddScore(ScoreEventCatalog.FakeOut.Points, "SQUIRREL TAUNT");
            _herdingState.AdvanceRoute(_squirrelRoute.Length);
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

        private MissionRuntimeSnapshot BuildRuntimeSnapshot()
        {
            string missionId = _mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy ? "squirrel_conspiracy" : ActiveMissionVariant.ToString();
            int progress = _mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy ? _herdingState.ControlCount + (_herdingState.StashFound ? 1 : 0) : BreakfastRecovered;
            int goal = _mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy ? 5 : BreakfastGoal;
            int mistakes = _mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy ? _herdingState.FakeOuts + _herdingState.Taunts : StolenFood + FailedInteractions;
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

            if (DogFeedback[0] != null) DogFeedback[0].ShowTug();
            if (DogFeedback[1] != null) DogFeedback[1].ShowTug();
            TugProgress = Mathf.Min(1f, TugProgress + Time.deltaTime * _tuning.TugChargePerSecond);
            LastFeedback = FeedbackKind.TugTogether;
            LastCue = "Both dogs are tugging - tiny sausage teamwork!";
            SetActorState(RopeObject, $"BOTH DOGS TUGGING {Mathf.RoundToInt(TugProgress * 100f)}%", new Color(1f, 0.78f, 0.22f), 0.22f);
            if (TugProgress >= 1f) CompleteTug();
        }

        private void OnDogInteracted(DogId dogId)
        {
            if (!MissionActive()) return;

            if (_mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy)
            {
                TryFindConspiracyStash(dogId);
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

            if (DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ShowTug();
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
            if (_mission.Variant == MissionVariant.SquirrelConspiracy)
            {
                if (_herdingState.StashFound) EndRound(true);
                return;
            }

            bool hasItems = BreakfastRecovered >= _mission.ItemGoal;
            bool hasPredator = !_mission.RequiresPredator || PredatorResolved;
            bool hasTug = !_mission.RequiresTug || TugComplete;
            if (hasItems && hasPredator && hasTug) EndRound(true);
        }

        private void EndRound(bool clear)
        {
            Phase = clear ? State.LevelClear : State.GameOver;
            CurrentFlow = FlowState.EndScreen;
            Outcome = clear ? MissionOutcome.Clear : MissionOutcome.Failed;
            if (clear)
            {
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
            RecordSessionResult();
            LogPlaytestEvent(clear ? "MissionClear" : "MissionFail", EndSummaryLabel);
            LogObjectiveIfChanged();
        }


        private string BuildOutcomeSummaryLabel()
        {
            string funny = _mission != null && _mission.Variant == MissionVariant.SquirrelConspiracy
                ? MissionOutcomeSummaryBuilder.BuildSquirrelSummary(_herdingState)
                : Outcome.ToString();
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
            if (_mission.Variant == MissionVariant.SquirrelConspiracy)
            {
                if (_herdingState.StashRevealed) return "Sniff the revealed stash and interact";
                return $"Herd squirrel route {_herdingState.RouteIndex + 1}/4: controls {_herdingState.ControlCount}/4, taunts {_herdingState.Taunts}/3";
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

            if (_mission.Variant == MissionVariant.SquirrelConspiracy && _herdingState.TooManyTaunts(3)) return "The squirrel taunted the dogs into a full backyard misinformation spiral.";
            if (_mission.UsesSquirrel && StolenFood >= maxStolenFood) return _mission.StolenFailReason;
            if (TimeRemaining <= 0f) return _mission.TimeFailReason;
            if (_mission.RequiresPredator && PredatorFailed) return _mission.PredatorFailReason;
            return _mission.GenericFailReason;
        }

        private void SetJuice(JuiceFeedbackKind kind, string label)
        {
            LastJuiceFeedback = kind;
            LastJuiceLabel = label;
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

            if (kb != null && (kb.f1Key.wasPressedThisFrame || kb.backquoteKey.wasPressedThisFrame))
            {
                TogglePlaytestOverlay();
            }
            if (kb != null && kb.f2Key.wasPressedThisFrame) SetAudioEnabled(!AudioEnabled);
            if (kb != null && kb.f3Key.wasPressedThisFrame) SetRumbleEnabled(!RumbleEnabled);

            if (MissionSelectVisible)
            {
                bool previous = false;
                bool next = false;
                bool start = false;
                if (kb != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame) { StartMission(MissionVariant.BackyardRescue); return; }
                    if (kb.digit2Key.wasPressedThisFrame) { StartMission(MissionVariant.SnackHeist); return; }
                    if (kb.digit3Key.wasPressedThisFrame) { StartMission(MissionVariant.SockPanic); return; }
                    if (kb.digit4Key.wasPressedThisFrame) { StartMission(MissionVariant.SquirrelConspiracy); return; }
                    previous |= kb.upArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
                    next |= kb.downArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame;
                    start |= kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                }
                if (pad != null)
                {
                    previous |= pad.dpad.up.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame;
                    next |= pad.dpad.down.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame;
                    start |= pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame;
                }

                if (previous) SelectPreviousMission();
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
                bool back = false;
                if (kb != null) back |= kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame;
                if (pad != null) back |= pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame;
                if (back) ReturnToMissionSelect();
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
            int missionIndex = IndexOfMission(_mission.Variant);
            if (missionIndex >= 0) _sessionCompletedMissions[missionIndex] = true;
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
            return $"Failures: Backyard {_sessionFailuresByMission[0]} / Snack {_sessionFailuresByMission[1]} / Sock {_sessionFailuresByMission[2]} / Squirrel {_sessionFailuresByMission[3]}";
        }

        private void UpdateSessionSummaryLabel()
        {
            SessionSummaryLabel = $"Session Summary: {SessionMissionsPlayed} missions played, {SessionTotalScore} total score, {SessionStarsEarned} stars, {SessionUniqueMissionsCompleted}/{MissionOrder.Length} missions finished.";
            SessionRanksEarnedLabel = _sessionRanks.Count == 0 ? "Ranks: none yet." : $"Ranks: {string.Join(" | ", _sessionRanks)}";
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
            if (_inputs == null) return;
            for (int i = 0; i < _inputs.Length; i++)
            {
                if (_inputs[i] != null) _inputs[i].enabled = false;
            }
        }

        private void SetMissionObjectsActive(bool active)
        {
            if (SquirrelObject != null) SquirrelObject.SetActive(active && _mission != null && _mission.UsesSquirrel);
            if (PredatorObject != null) PredatorObject.SetActive(active && _mission != null && _mission.RequiresPredator);
            if (RopeObject != null) RopeObject.SetActive(active && _mission != null && _mission.RequiresTug);
        }

        public static MissionDefinition BuildMissionDefinition(MissionVariant variant) =>
            BuildMissionDefinition(variant, ArenaMissionTuning.CreateDefault());

        private static MissionDefinition BuildMissionDefinition(MissionVariant variant, ArenaMissionTuning tuning)
        {
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
                case MissionVariant.SockPanic:
                    return new MissionDefinition
                    {
                        Variant = MissionVariant.SockPanic,
                        Name = "Sock Panic",
                        IntroPrompt = "Cheddar + Cocoa must rescue the scattered socks before laundry order returns.",
                        ReadyScoreLabel = "READY TO PANIC ABOUT SOCKS",
                        ItemRootName = "Scattered Socks",
                        ItemObjectName = "Panic Sock",
                        ItemWorldLabel = "Sock!",
                        ItemArrowLabel = "SOCK",
                        ItemCollectCueNoun = "a dramatic sock",
                        CollectObjectiveFormat = "Return socks {0}/{1}",
                        CollectedScoreLabel = "SOCK RESCUED",
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
        private Vector3 _rotationPerSecond;

        public string Label => _label != null ? _label.text : string.Empty;

        public void Init(SpriteRenderer renderer, string label, float pulseAmount, Vector3 rotationPerSecond)
        {
            _renderer = renderer;
            _label = GetComponentInChildren<TextMesh>();
            _baseScale = transform.localScale;
            _pulseAmount = pulseAmount;
            _rotationPerSecond = rotationPerSecond;
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
            if (_rotationPerSecond != Vector3.zero) transform.Rotate(_rotationPerSecond * Time.deltaTime);
            if (_label != null) _label.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>Short-lived world text for score, rescue, tug, and miss moments.</summary>
    public sealed class MissionWorldPop : MonoBehaviour
    {
        private TextMesh _label;
        private float _t;

        public string Label => _label != null ? _label.text : string.Empty;

        public void Begin(TextMesh label) => _label = label;

        private void Update()
        {
            _t += Time.deltaTime;
            var art = ArenaArtCatalog.WorldPop;
            transform.position += Vector3.up * (Time.deltaTime * art.RiseSpeed);
            float scale = 1f + Mathf.Sin(Mathf.Clamp01(_t / art.LifeSeconds) * Mathf.PI) * art.PopScaleAmount;
            transform.localScale = Vector3.one * scale;

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
