using System.Collections.Generic;
using UnityEngine;
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
        public enum RoundModifier { SquirrelTrouble, ZoomiesSurge, PancakePanic }
        public enum MissionOutcome { InProgress, Clear, Failed }
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

        public enum JuiceFeedbackKind
        {
            None,
            BarkBurst,
            SuccessPop,
            WarningMiss,
            ScoreDelta
        }

        private const int ItemScore = 50;
        private const int SquirrelScareScore = 25;
        private const int UnitedBarkScore = 100;
        private const int PredatorDefendedScore = 300;
        private const int RescueScore = 250;
        private const int TugScore = 200;
        private const int ClearScore = 500;
        private const int TimeBonusMultiplier = 5;
        private const int PredatorFailurePenalty = 150;
        private const int SquirrelPenalty = 50;
        private const int PancakePenalty = 80;
        private const int GameOverPenalty = 100;

        [Header("Mission pacing")]
        [SerializeField] private float roundDuration = 75f;
        [SerializeField] private int treatCount = 5;
        [SerializeField] private int recoveryGoal = 6;
        [SerializeField] private int maxStolenFood = 3;
        [SerializeField] private float introPromptSeconds = 5f;

        [Header("Bark tuning")]
        [SerializeField] private float unitedBarkWindow = 0.8f;
        [SerializeField] private float unitedBarkRange = 3f;
        [SerializeField] private float unitedBarkCooldown = 1.2f;
        [SerializeField] private float singleBarkSquirrelRange = 4f;
        [SerializeField] private float singleBarkScareSeconds = 1.5f;
        [SerializeField] private float unitedBarkScareSeconds = 3.5f;

        [Header("Hazard pacing")]
        [SerializeField] private float squirrelBaseDelay = 2.6f;
        [SerializeField] private float squirrelTroubleDelay = 1.4f;
        [SerializeField] private float firstSquirrelBaseDelay = 7.5f;
        [SerializeField] private float firstSquirrelTroubleDelay = 6.2f;
        [SerializeField] private float squirrelMoveSpeed = 2.2f;
        [SerializeField] private float predatorWarningAt = 18f;
        [SerializeField] private float predatorWarningSeconds = 4f;
        [SerializeField] private float tugChargePerSecond = 0.38f;

        public int Score { get; private set; }
        public int LastScoreDelta { get; private set; }
        public float TimeRemaining { get; private set; }
        public float RoundDuration => roundDuration;
        public State Phase { get; private set; } = State.Intro;
        public bool IsGameOver => Phase == State.GameOver;
        public bool IsLevelClear => Phase == State.LevelClear;
        public int UnitedBarks { get; private set; }
        public int BreakfastRecovered { get; private set; }
        public int BreakfastGoal => recoveryGoal;
        public int StolenFood { get; private set; }
        public int MaxStolenFood => maxStolenFood;
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
        public string MissionIntroPrompt => "Cheddar + Cocoa must protect the weenies together.";
        public string MissionBanner { get; private set; } = string.Empty;
        public string EndRank { get; private set; } = "Needs More Bark";
        public string EndSummaryLabel { get; private set; } = string.Empty;
        public string EndReasonLabel { get; private set; } = string.Empty;
        public bool ReplayPromptVisible => IsGameOver || IsLevelClear;
        public string ReplayPromptLabel => ReplayPromptVisible ? "Press R / Enter / Start to replay the weenie rescue" : string.Empty;
        public FeedbackKind LastFeedback { get; private set; } = FeedbackKind.Intro;
        public JuiceFeedbackKind LastJuiceFeedback { get; private set; } = JuiceFeedbackKind.None;
        public string LastJuiceLabel { get; private set; } = string.Empty;
        public GameObject SquirrelObject { get; private set; }
        public GameObject PredatorObject { get; private set; }
        public GameObject RopeObject { get; private set; }
        public DogReadabilityFeedback[] DogFeedback { get; private set; }
        public ObjectiveArrowFeedback[] ObjectiveArrows { get; private set; }

        private DogController[] _dogs;
        private GamepadPlayerInput[] _inputs;
        private Vector2[] _dogStarts;
        private Sprite _sprite;
        private Rect _bounds;
        private System.Random _rng;
        private Transform _treatRoot;
        private AudioSource _audio;
        private AudioClip _barkCue;
        private AudioClip _dangerCue;
        private AudioClip _successCue;

        private readonly List<Treat> _treats = new();
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

        public void Init(DogController[] dogs, GamepadPlayerInput[] inputs, Sprite treatSprite, Rect bounds, int seed)
        {
            _dogs = dogs;
            _inputs = inputs;
            _sprite = treatSprite;
            _bounds = bounds;
            _rng = new System.Random(seed);
            _dogStarts = new Vector2[dogs.Length];
            _lastBarks = new float[dogs.Length];
            DogFeedback = new DogReadabilityFeedback[dogs.Length];
            ObjectiveArrows = new ObjectiveArrowFeedback[dogs.Length];

            for (int i = 0; i < dogs.Length; i++)
            {
                _dogStarts[i] = dogs[i].transform.position;
                dogs[i].OnBark += OnDogBarked;
                dogs[i].OnInteract += OnDogInteracted;
                dogs[i].TryGetComponent(out DogReadabilityFeedback dogFeedback);
                dogs[i].TryGetComponent(out ObjectiveArrowFeedback objectiveArrow);
                DogFeedback[i] = dogFeedback;
                ObjectiveArrows[i] = objectiveArrow;
            }

            _treatRoot = new GameObject("Breakfast/Weenies").transform;
            BuildAudio();
            BuildMissionObjects();
            BeginRound();
        }

        public void OnTreatCollected(Treat treat, DogController dog)
        {
            if (!MissionActive() || treat == null) return;

            AddScore(ItemScore, "WEENIE SAVED");
            BreakfastRecovered++;
            LastCue = $"{DogName(dog)} recovered breakfast!";
            Pulse(dog != null ? dog.gameObject : null, 1.2f);
            SetJuice(JuiceFeedbackKind.ScoreDelta, LastScoreEventLabel);
            SpawnWorldPop(dog != null ? dog.transform.position : treat.transform.position, LastScoreEventLabel, new Color(1f, 0.9f, 0.25f));
            PlayCue(_successCue);

            _treats.Remove(treat);
            Destroy(treat.gameObject);
            SpawnTreat();
            CheckClear();
        }

        public void Restart() => BeginRound();

        public void SetRoundDuration(float seconds)
        {
            roundDuration = Mathf.Max(0.01f, seconds);
            if (MissionActive()) TimeRemaining = Mathf.Min(TimeRemaining, roundDuration);
        }

        public void ForcePredatorWarning()
        {
            if (MissionActive()) StartPredatorWarning();
        }

        public void ForcePredatorAttack()
        {
            if (MissionActive() || Phase == State.PredatorWarning) StartPredatorAttack();
        }

        public void ForceSquirrelStealAttempt()
        {
            if (!MissionActive() || _treats.Count == 0) return;

            StartSquirrelSteal(FindNearestTreat(SquirrelObject.transform.position) ?? _treats[0]);
        }

        public void ForceGameOver() => EndRound(false);

        private void BeginRound()
        {
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
            LastScoreEventLabel = "0 READY TO PROTECT WEENIES";
            MissionBanner = MissionIntroPrompt;
            EndRank = "Needs More Bark";
            EndSummaryLabel = string.Empty;
            EndReasonLabel = string.Empty;
            LastFeedback = FeedbackKind.Intro;
            LastJuiceFeedback = JuiceFeedbackKind.None;
            LastJuiceLabel = string.Empty;
            LastScorePopLabel = string.Empty;

            ActiveModifier = (RoundModifier)_rng.Next(0, 3);
            _nextUnitedBarkAt = 0f;
            _teamBarkFeedbackUntil = 0f;
            _scorePopUntil = 0f;
            _squirrelScaredUntil = 0f;
            _nextSquirrelScareScoreAt = 0f;
            _squirrelTarget = null;
            _squirrelHasStarted = false;
            _introPromptUntil = Time.time + introPromptSeconds;
            _squirrelTimer = SquirrelDelay();
            _predatorTimer = predatorWarningAt;
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

            PlaceObject(SquirrelObject, new Vector2(_bounds.xMax - 2f, _bounds.yMax - 2f));
            PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
            PlaceObject(RopeObject, Vector2.zero);
            SetActorState(SquirrelObject, "Squirrel: WAITING", new Color(0.55f, 0.32f, 0.12f), 0.06f);
            SetActorState(PredatorObject, "Predator: OFFSCREEN", Color.gray, 0.04f);
            SetActorState(RopeObject, "Rope/Tug - BOTH DOGS", new Color(0.95f, 0.7f, 0.15f), 0.08f);
        }

        private void Update()
        {
            if (!MissionActive()) return;

            MissionBanner = Time.time < _introPromptUntil ? MissionIntroPrompt : string.Empty;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                EndRound(false);
                return;
            }

            TickModifier();
            TickSquirrel();
            TickPredator();
            TickTugProximity();
            CheckClear();
            UpdateObjectiveArrows();
        }

        private void TickModifier()
        {
            if (ActiveModifier != RoundModifier.ZoomiesSurge || Time.time < _nextZoomiesPulseAt) return;

            foreach (var dog in _dogs) dog.TriggerZoomies();
            LastCue = "Zoomies surge! Hold the line!";
            _nextZoomiesPulseAt = Time.time + 10f;
            PlayCue(_barkCue);
        }

        private void TickSquirrel()
        {
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
                Time.deltaTime * squirrelMoveSpeed);

            if (Vector2.Distance(SquirrelObject.transform.position, _squirrelTarget.transform.position) < 0.25f)
                SquirrelStealsTarget();
        }

        private void StartSquirrelSteal(Treat target)
        {
            if (target == null) return;

            _squirrelTarget = target;
            _squirrelHasStarted = true;
            LastFeedback = FeedbackKind.SquirrelStealing;
            LastCue = "Squirrel is tiptoeing off with a weenie - bark now!";
            SetJuice(JuiceFeedbackKind.WarningMiss, "BARK TO SCARE SQUIRREL");
            SetActorState(SquirrelObject, "SQUIRREL STEALING - BARK!", new Color(0.7f, 0.35f, 0.08f), 0.32f);
            PlayCue(_dangerCue);
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
            AddScore(-(ActiveModifier == RoundModifier.PancakePanic ? PancakePenalty : SquirrelPenalty), "SQUIRREL GOT ONE");
            _squirrelTarget = null;
            _squirrelTimer = SquirrelDelay();
            LastFeedback = FeedbackKind.SquirrelStoleFood;
            LastCue = "Squirrel got a weenie and is being rude about it!";
            SetJuice(JuiceFeedbackKind.WarningMiss, "MISS! SQUIRREL STOLE A WEENIE");
            SetActorState(SquirrelObject, "SQUIRREL GOT A WEENIE!", Color.gray, 0.22f);
            SpawnWorldPop(SquirrelObject.transform.position, "MISS! -WEENIE", new Color(1f, 0.35f, 0.2f));
            PlayCue(_dangerCue);

            if (StolenFood >= maxStolenFood) EndRound(false);
        }

        private void TickPredator()
        {
            if (PredatorResolved || PredatorFailed) return;

            _predatorTimer -= Time.deltaTime;
            if (_predatorTimer <= predatorWarningSeconds && Phase == State.Playing) StartPredatorWarning();
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
            SetActorState(PredatorObject, "HUDDLE + BARK!", new Color(1f, 0.08f, 0.08f), 0.42f);
            PlayCue(_dangerCue);
        }

        private void StartPredatorAttack()
        {
            _nextUnitedBarkAt = 0f;
            Phase = State.PredatorAttack;
            if (_predatorTarget < 0) _predatorTarget = 0;

            PredatorObject.name = "Predator Attack";
            PlaceObject(PredatorObject, _dogs[_predatorTarget].transform.position);
            SetActorState(PredatorObject, "PREDATOR ATTACK!", new Color(0.8f, 0f, 0f), 0.45f);

            _grabbedDog = _predatorTarget;
            _dogs[_grabbedDog].SetMode(MovementMode.Stunned);
            PredatorFailed = true;
            AddScore(-PredatorFailurePenalty, "PREDATOR HIT");
            LastFeedback = FeedbackKind.PredatorAttack;
            LastCue = $"{DogName(_dogs[_grabbedDog])} got yoinked! Partner bark rescue!";
            SetJuice(JuiceFeedbackKind.WarningMiss, $"RESCUE {DogName(_dogs[_grabbedDog]).ToUpperInvariant()}!");
            SpawnWorldPop(_dogs[_grabbedDog].transform.position, "YOINKED!", new Color(1f, 0.2f, 0.2f));
            PlayCue(_dangerCue);
        }

        private void ResolvePredator()
        {
            PredatorResolved = true;
            PredatorFailed = false;
            Phase = State.Playing;
            AddScore(PredatorDefendedScore, "PREDATOR YEETED");
            LastFeedback = FeedbackKind.UnitedBark;
            LastCue = "DOUBLE WOOF drove the predator away!";
            PredatorObject.name = "Predator Driven Away";
            PlaceObject(PredatorObject, new Vector2(0f, _bounds.yMax + 2f));
            SetActorState(PredatorObject, "PREDATOR YEETED", Color.gray, 0.08f);
            SetJuice(JuiceFeedbackKind.SuccessPop, "PREDATOR YEETED!");
            SpawnWorldPop(_dogs[0].transform.position + Vector3.up, "DOUBLE WOOF!", new Color(1f, 0.95f, 0.25f));
            PlayCue(_successCue);
            CheckClear();
        }

        private void TickTugProximity()
        {
            if (TugComplete || _dogs.Length < 2) return;

            bool cheddarNear = Vector2.Distance(_dogs[0].transform.position, RopeObject.transform.position) < 1.6f;
            bool cocoaNear = Vector2.Distance(_dogs[1].transform.position, RopeObject.transform.position) < 1.6f;
            if (!cheddarNear || !cocoaNear)
            {
                if (cheddarNear != cocoaNear)
                {
                    LastFeedback = FeedbackKind.TugNeedsPartner;
                    LastCue = "Rope wiggles: both dogs have to commit together!";
                    string waitingFor = cheddarNear ? "WAITING FOR COCOA" : "WAITING FOR CHEDDAR";
                    SetActorState(RopeObject, $"Rope/Tug - {waitingFor}", new Color(1f, 0.8f, 0.28f), 0.2f);
                }
                return;
            }

            if (DogFeedback[0] != null) DogFeedback[0].ShowTug();
            if (DogFeedback[1] != null) DogFeedback[1].ShowTug();
            TugProgress = Mathf.Min(1f, TugProgress + Time.deltaTime * tugChargePerSecond);
            LastFeedback = FeedbackKind.TugTogether;
            LastCue = "Both dogs are tugging - tiny sausage teamwork!";
            SetActorState(RopeObject, $"BOTH TUGGING {Mathf.RoundToInt(TugProgress * 100f)}%", new Color(1f, 0.78f, 0.22f), 0.22f);
            if (TugProgress >= 1f) CompleteTug();
        }

        private void OnDogInteracted(DogId dogId)
        {
            if (!MissionActive() || TugComplete) return;

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;
            if (Vector2.Distance(_dogs[dogIndex].transform.position, RopeObject.transform.position) > 1.8f) return;

            if (DogFeedback[dogIndex] != null) DogFeedback[dogIndex].ShowTug();
            TugProgress = Mathf.Min(1f, TugProgress + 0.2f);
            LastFeedback = FeedbackKind.TugNeedsPartner;
            LastCue = $"{DogName(_dogs[dogIndex])} has the rope - partner pile on!";
            SetActorState(RopeObject, $"Rope/Tug {Mathf.RoundToInt(TugProgress * 100f)}% - NEED PARTNER", new Color(1f, 0.78f, 0.22f), 0.2f);
            PlayCue(_barkCue);
            if (TugProgress >= 1f) CompleteTug();
        }

        private void CompleteTug()
        {
            TugComplete = true;
            AddScore(TugScore, "TUG COMPLETE");
            LastFeedback = FeedbackKind.TugTogether;
            LastCue = "Rope tug complete - dramatic victory chomps!";
            RopeObject.name = "Rope/Tug Complete";
            SetActorState(RopeObject, "ROPE COMPLETE!", new Color(0.3f, 1f, 0.3f), 0.08f);
            SetJuice(JuiceFeedbackKind.SuccessPop, "TUG POP! ROPE COMPLETE");
            SpawnWorldPop(RopeObject.transform.position, "TUG POP!", new Color(0.45f, 1f, 0.35f));
            PlayCue(_successCue);
            CheckClear();
        }

        private void OnDogBarked(DogId dogId)
        {
            if (!MissionActive()) return;

            int dogIndex = IndexOfDog(dogId);
            if (dogIndex < 0) return;

            _lastBarks[dogIndex] = Time.time;
            var dog = _dogs[dogIndex];
            bool barkDidSomething = false;

            if (Vector2.Distance(dog.transform.position, SquirrelObject.transform.position) < singleBarkSquirrelRange)
            {
                ScareSquirrel(singleBarkScareSeconds, $"{DogName(dog)} scared the squirrel!", true);
                barkDidSomething = true;
            }

            if (_grabbedDog >= 0 && dogIndex != _grabbedDog &&
                Vector2.Distance(dog.transform.position, _dogs[_grabbedDog].transform.position) < 2f)
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
            AddScore(UnitedBarkScore, "UNITED BARK");
            _nextUnitedBarkAt = Time.time + unitedBarkCooldown;
            _teamBarkFeedbackUntil = Time.time + 0.35f;
            LastFeedback = FeedbackKind.UnitedBark;
            ScareSquirrel(unitedBarkScareSeconds, "United bark shook the whole yard!", false);

            if (Phase == State.PredatorWarning || Phase == State.PredatorAttack) ResolvePredator();
        }

        private void ScareSquirrel(float seconds, string cue, bool awardScore)
        {
            _squirrelTarget = null;
            _squirrelScaredUntil = Mathf.Max(_squirrelScaredUntil, Time.time + seconds);
            _squirrelTimer = SquirrelDelay();
            if (awardScore && Time.time >= _nextSquirrelScareScoreAt)
            {
                AddScore(SquirrelScareScore, "SQUIRREL SCARED");
                _nextSquirrelScareScoreAt = Time.time + 1f;
            }
            LastFeedback = awardScore ? FeedbackKind.SquirrelScared : FeedbackKind.UnitedBark;
            LastCue = awardScore ? $"{cue} It dropped the snack plan!" : "DOUBLE WOOF made the squirrel reconsider its life.";
            SetActorState(SquirrelObject, awardScore ? "SQUIRREL DROPPED IT!" : "SQUIRREL HID FROM DOUBLE WOOF", new Color(0.85f, 0.85f, 0.85f), 0.08f);
            SetJuice(awardScore ? JuiceFeedbackKind.SuccessPop : JuiceFeedbackKind.BarkBurst,
                awardScore ? "SQUIRREL DROP POP!" : "DOUBLE WOOF BURST");
            SpawnWorldPop(SquirrelObject.transform.position, awardScore ? "DROP!" : "DOUBLE WOOF!", new Color(0.9f, 0.95f, 1f));
            PlayCue(_barkCue);
        }

        private void RescueGrabbedDog(DogController rescuer)
        {
            if (_grabbedDog < 0) return;

            int rescuedDog = _grabbedDog;
            _dogs[_grabbedDog].SetMode(MovementMode.Free);
            _grabbedDog = -1;
            Phase = State.Playing;
            AddScore(RescueScore, "PARTNER RESCUE");
            LastFeedback = FeedbackKind.PartnerRescue;
            LastCue = $"{DogName(rescuer)} bark-rescued their sibling - heroic nonsense!";
            if (DogFeedback[rescuedDog] != null) DogFeedback[rescuedDog].ShowRescued();
            int rescuerIndex = IndexOfDog(rescuer.GetComponent<DogIdentity>().Id);
            if (rescuerIndex >= 0 && DogFeedback[rescuerIndex] != null) DogFeedback[rescuerIndex].ShowProudBrief();
            SetJuice(JuiceFeedbackKind.SuccessPop, "RESCUE POP!");
            SpawnWorldPop(_dogs[rescuedDog].transform.position, "RESCUED!", new Color(0.45f, 1f, 0.65f));
            PlayCue(_successCue);
        }

        private void CheckClear()
        {
            if (Phase == State.LevelClear || Phase == State.GameOver) return;
            if (BreakfastRecovered >= recoveryGoal && PredatorResolved && TugComplete) EndRound(true);
        }

        private void EndRound(bool clear)
        {
            Phase = clear ? State.LevelClear : State.GameOver;
            Outcome = clear ? MissionOutcome.Clear : MissionOutcome.Failed;
            if (clear)
            {
                AddScore(ClearScore + Mathf.CeilToInt(TimeRemaining) * TimeBonusMultiplier, "LEVEL CLEAR");
                EndRank = RankForScore(Score, true);
                StarRating = Score >= 1500 ? 3 : Score >= 1000 ? 2 : 1;
                LastFeedback = FeedbackKind.LevelClear;
                LastCue = $"BACKYARD SAVED! {EndRank}. Score {Score}";
                MissionBanner = $"BACKYARD SAVED! {EndRank}";
                EndReasonLabel = EndReasonFor(clear);
                SetJuice(JuiceFeedbackKind.SuccessPop, "BACKYARD SAVED POP!");
                PlayCue(_successCue);
            }
            else
            {
                AddScore(-GameOverPenalty, "GAME OVER");
                EndRank = RankForScore(Score, false);
                StarRating = 0;
                LastFeedback = FeedbackKind.GameOver;
                LastCue = $"MISSION FAILED: {EndRank}. Score {Score}";
                MissionBanner = $"MISSION FAILED! {EndRank}";
                EndReasonLabel = EndReasonFor(clear);
                SetJuice(JuiceFeedbackKind.WarningMiss, "SAD FLOP REPLAY!");
                PlayCue(_dangerCue);
            }
            EndSummaryLabel = $"{Outcome}: {Score} - {EndRank}";

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
        }

        private string BuildObjectiveLabel()
        {
            if (IsLevelClear) return "Backyard saved - replay the weenie rescue";
            if (IsGameOver) return "Mission failed - replay the weenie rescue";
            if (_dogs == null || _dogs.Length == 0) return "Protect the weenies";

            if (Phase == State.PredatorAttack && _grabbedDog >= 0)
                return $"Rescue {DogName(_dogs[_grabbedDog])}";
            if (Phase == State.PredatorWarning)
                return "Huddle + bark at the shadow";
            if (_squirrelTarget != null)
                return "Bark to scare squirrel";
            if (!TugComplete && BreakfastRecovered >= Mathf.Max(2, recoveryGoal / 2))
                return "Both dogs tug the rope";
            if (BreakfastRecovered < recoveryGoal)
                return $"Save weenies {BreakfastRecovered}/{recoveryGoal}";
            if (!TugComplete)
                return "Both dogs tug the rope";
            if (!PredatorResolved)
                return "Clear the yard together";

            return "Clear the yard";
        }

        private string EndReasonFor(bool clear)
        {
            if (clear)
            {
                if (EndRank == "Pawfect Yard") return "Tiny legends protected every snack with style.";
                if (EndRank == "Backyard Heroes") return "The yard survived with respectable barking.";
                return "The weenies made it, even if dignity did not.";
            }

            if (StolenFood >= maxStolenFood) return "The squirrel union escaped with too many weenies.";
            if (TimeRemaining <= 0f) return "The clock won while the dogs had opinions.";
            if (PredatorFailed) return "The shadow caused a dramatic rescue backlog.";
            return "Needs more bark before the next weenie rescue.";
        }

        private void SetJuice(JuiceFeedbackKind kind, string label)
        {
            LastJuiceFeedback = kind;
            LastJuiceLabel = label;
        }

        private static string RankForScore(int score, bool clear)
        {
            if (clear && score >= 1500) return "Pawfect Yard";
            if (clear && score >= 1000) return "Backyard Heroes";
            if (score >= 350) return "Snack Survivors";
            return "Needs More Bark";
        }

        private float SquirrelDelay()
        {
            if (!_squirrelHasStarted)
                return ActiveModifier == RoundModifier.SquirrelTrouble ? firstSquirrelTroubleDelay : firstSquirrelBaseDelay;
            return ActiveModifier == RoundModifier.SquirrelTrouble ? squirrelTroubleDelay : squirrelBaseDelay;
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
                if (Time.time - _lastBarks[i] > unitedBarkWindow) return false;
            }
            return true;
        }

        private bool DogsAreHuddled()
        {
            Vector2 first = _dogs[0].transform.position;
            for (int i = 1; i < _dogs.Length; i++)
            {
                if (Vector2.Distance(first, _dogs[i].transform.position) > unitedBarkRange) return false;
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

            if (_squirrelTarget != null)
            {
                target = SquirrelObject.transform;
                copy = "BARK SQUIRREL";
                hideDistance = 2.2f;
                return true;
            }

            if (!TugComplete && BreakfastRecovered >= Mathf.Max(2, recoveryGoal / 2))
            {
                target = RopeObject.transform;
                copy = "BOTH TUG";
                hideDistance = 1.7f;
                return true;
            }

            var nearestTreat = FindNearestTreat(_dogs[dogIndex].transform.position);
            if (nearestTreat == null) return false;

            target = nearestTreat.transform;
            copy = "WEENIE";
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

        private void SpawnTreat()
        {
            const float margin = 1.2f;
            float x = Mathf.Lerp(_bounds.xMin + margin, _bounds.xMax - margin, (float)_rng.NextDouble());
            float y = Mathf.Lerp(_bounds.yMin + margin, _bounds.yMax - margin, (float)_rng.NextDouble());

            var go = new GameObject("Breakfast/Weenie");
            go.transform.SetParent(_treatRoot);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(0.72f, 0.32f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.9f, 0.28f, 0.18f);
            sr.sortingOrder = 5;
            AddActorPart(go, "WeenieBunLeft", _sprite, new Color(0.98f, 0.76f, 0.4f),
                new Vector3(-0.5f, 0f, -0.03f), new Vector3(0.2f, 0.9f, 1f), 6);
            AddActorPart(go, "WeenieBunRight", _sprite, new Color(0.98f, 0.76f, 0.4f),
                new Vector3(0.5f, 0f, -0.03f), new Vector3(0.2f, 0.9f, 1f), 6);
            AddActorPart(go, "WeenieMustard", _sprite, new Color(1f, 0.9f, 0.12f),
                new Vector3(0f, 0.18f, -0.04f), new Vector3(0.8f, 0.18f, 1f), 7);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var treat = go.AddComponent<Treat>();
            treat.Bind(this);
            _treats.Add(treat);

            AddWorldLabel(go, "Weenie", Vector3.up * 2.2f, 16, Color.white);
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
            SquirrelObject = MakeActor("Squirrel", new Color(0.55f, 0.32f, 0.12f), 0.7f, "Squirrel", 0.18f);
            PredatorObject = MakeActor("Predator Warning", new Color(0.7f, 0.05f, 0.08f), 1.1f, "Predator Warning", 0.25f);
            RopeObject = MakeActor("Rope/Tug", new Color(0.95f, 0.7f, 0.15f), 0.9f, "Rope/Tug", 0.1f);
        }

        private GameObject MakeActor(string name, Color color, float scale, string label, float pulse)
        {
            var go = new GameObject(name);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = color;
            sr.sortingOrder = 6;

            BuildActorArt(go, name, sr);
            AddWorldLabel(go, label, ActorLabelOffset(name), 24, Color.white);
            go.AddComponent<MissionActorFeedback>().Init(sr, label, pulse);
            return go;
        }

        private static Vector3 ActorLabelOffset(string name)
        {
            if (name.Contains("Squirrel") || name.Contains("Predator") || name.Contains("Rope"))
                return Vector3.up * 1.8f;
            return Vector3.up * 0.85f;
        }

        private void BuildActorArt(GameObject go, string name, SpriteRenderer root)
        {
            if (name.Contains("Squirrel"))
            {
                root.transform.localScale = new Vector3(0.78f, 0.48f, 1f);
                AddActorPart(go, "SquirrelFlagTail", _sprite, new Color(0.72f, 0.42f, 0.12f),
                    new Vector3(-0.55f, 0.22f, -0.02f), new Vector3(0.32f, 0.9f, 1f), 7);
                AddActorPart(go, "SquirrelPointNose", _sprite, new Color(0.36f, 0.18f, 0.07f),
                    new Vector3(0.52f, 0.04f, -0.03f), new Vector3(0.28f, 0.22f, 1f), 8);
                AddActorPart(go, "SquirrelBeadyEye", _sprite, Color.black,
                    new Vector3(0.28f, 0.2f, -0.04f), new Vector3(0.09f, 0.09f, 1f), 9);
                return;
            }

            if (name.Contains("Predator"))
            {
                root.transform.localScale = new Vector3(1.25f, 0.48f, 1f);
                AddActorPart(go, "PredatorWingLeft", _sprite, new Color(0.18f, 0.03f, 0.04f),
                    new Vector3(-0.62f, 0.04f, -0.02f), new Vector3(0.55f, 0.28f, 1f), 7);
                AddActorPart(go, "PredatorWingRight", _sprite, new Color(0.18f, 0.03f, 0.04f),
                    new Vector3(0.62f, 0.04f, -0.02f), new Vector3(0.55f, 0.28f, 1f), 7);
                AddActorPart(go, "PredatorWarningEyeA", _sprite, new Color(1f, 0.1f, 0.06f),
                    new Vector3(-0.16f, 0.16f, -0.04f), new Vector3(0.12f, 0.12f, 1f), 9);
                AddActorPart(go, "PredatorWarningEyeB", _sprite, new Color(1f, 0.1f, 0.06f),
                    new Vector3(0.16f, 0.16f, -0.04f), new Vector3(0.12f, 0.12f, 1f), 9);
                return;
            }

            if (name.Contains("Rope"))
            {
                root.transform.localScale = new Vector3(1.45f, 0.24f, 1f);
                AddActorPart(go, "RopeStripeA", _sprite, new Color(0.55f, 0.27f, 0.08f),
                    new Vector3(-0.36f, 0f, -0.02f), new Vector3(0.16f, 1.1f, 1f), 7);
                AddActorPart(go, "RopeStripeB", _sprite, new Color(0.55f, 0.27f, 0.08f),
                    new Vector3(0.36f, 0f, -0.02f), new Vector3(0.16f, 1.1f, 1f), 7);
                AddActorPart(go, "RopeEndLeft", _sprite, new Color(1f, 0.82f, 0.3f),
                    new Vector3(-0.76f, 0f, -0.03f), new Vector3(0.16f, 1.5f, 1f), 8);
                AddActorPart(go, "RopeEndRight", _sprite, new Color(1f, 0.82f, 0.3f),
                    new Vector3(0.76f, 0f, -0.03f), new Vector3(0.16f, 1.5f, 1f), 8);
            }
        }

        private static SpriteRenderer AddActorPart(GameObject parent, string name, Sprite sprite, Color color,
            Vector3 localPosition, Vector3 localScale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
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
            var go = new GameObject($"MissionPop_{text.Replace(" ", "_").Replace("!", string.Empty).Replace("+", "PLUS").Replace("-", "MINUS")}");
            go.transform.position = position + Vector3.up * 0.95f;
            var label = go.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 28;
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
            _audio.volume = 0.18f;
            _barkCue = MakeTone("MissionBarkCue", 520f, 0.08f);
            _dangerCue = MakeTone("MissionDangerCue", 180f, 0.16f);
            _successCue = MakeTone("MissionSuccessCue", 740f, 0.12f);
        }

        private AudioClip MakeTone(string name, float frequency, float seconds)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(sampleRate * seconds);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (i / (float)sampleCount);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.25f;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PlayCue(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
        }

        private void OnDestroy()
        {
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

        public string Label => _label != null ? _label.text : string.Empty;

        public void Init(SpriteRenderer renderer, string label, float pulseAmount)
        {
            _renderer = renderer;
            _label = GetComponentInChildren<TextMesh>();
            _baseScale = transform.localScale;
            _pulseAmount = pulseAmount;
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
            if (name.Contains("Squirrel")) transform.Rotate(0f, 0f, 80f * Time.deltaTime);
            if (name.Contains("Rope")) transform.Rotate(0f, 0f, 45f * Time.deltaTime);
            if (_label != null) _label.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>Short-lived world text for score, rescue, tug, and miss moments.</summary>
    public sealed class MissionWorldPop : MonoBehaviour
    {
        private const float Life = 1.05f;
        private TextMesh _label;
        private float _t;

        public string Label => _label != null ? _label.text : string.Empty;

        public void Begin(TextMesh label) => _label = label;

        private void Update()
        {
            _t += Time.deltaTime;
            transform.position += Vector3.up * (Time.deltaTime * 0.35f);
            float scale = 1f + Mathf.Sin(Mathf.Clamp01(_t / Life) * Mathf.PI) * 0.28f;
            transform.localScale = Vector3.one * scale;

            if (_label != null)
            {
                _label.transform.rotation = Quaternion.identity;
                Color c = _label.color;
                c.a = Mathf.Lerp(1f, 0f, _t / Life);
                _label.color = c;
            }

            if (_t >= Life) Destroy(gameObject);
        }
    }
}
