using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class CarRideMissionController : IMissionController
    {
        private const int RequiredLurches = 6;
        private const int MaxSpills = 4;
        private const float LurchInterval = 4f;

        private readonly CarBalanceMissionState _state = new();
        private MissionContext _context;
        private GameObject _car;
        private float _balance;
        private int _lurchDirection;
        private float _nextLurchAt;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.CarRide;
        public bool IsComplete => _state.ReadyToClear();
        public bool IsFailed => _state.TooManySpills(MaxSpills);
        public string FailReason => IsFailed ? "The car tipped over too many times on the way home." : null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildCarBalanceSummary(_state);
        public Vector2 EntryTarget => _context != null ? _context.Bounds.center : Vector2.zero;
        public CarBalanceMissionState State => _state;
        public float Balance => _balance;

        public string ObjectiveLabel
        {
            get
            {
                string tilt = Mathf.Abs(_balance) < 0.15f ? "LEVEL" : (_balance > 0f ? "tipping RIGHT" : "tipping LEFT");
                return $"Lean to keep the car level ({tilt}): steadied {_state.LurchesSurvived}/{_state.RequiredLurches}, spills {_state.Spills}/{MaxSpills}";
            }
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _car = _context.CreateActor(ArenaArtCatalog.ActorKind.Predator);
            _car.name = "Car Ride Balance Vehicle";
            MissionPropArt.AttachObject(_car, FinalGameplayArt.MissionCarBalance, 0.013f, 18, true);
            _car.SetActive(false);
        }

        public void StartMission()
        {
            _state.Configure(RequiredLurches);
            _balance = 0f;
            _lurchDirection = 1;
            _nextLurchAt = _context.Now() + LurchInterval;
            _car.transform.position = new Vector2(0f, _context.Bounds.yMax - 1.5f);
            _car.SetActive(true);
            _context.SetActorState(_car, "CAR - LEAN TO KEEP IT LEVEL!", new Color(0.5f, 0.4f, 0.3f), 0.12f);
        }

        public void Tick(float deltaTime, float now)
        {
            if (IsComplete || IsFailed) return;

            float averageX = 0f;
            if (_context.Dogs != null && _context.Dogs.Length > 0)
            {
                foreach (var dog in _context.Dogs)
                    if (dog != null) averageX += dog.transform.position.x;
                averageX /= _context.Dogs.Length;
            }
            float lean = Mathf.Clamp(averageX / 8f, -1f, 1f);
            _balance = Mathf.Clamp(_balance + (lean * 0.6f + _lurchDirection * 0.04f) * deltaTime, -1.4f, 1.4f);
            _context.SetActorState(_car,
                $"CAR TILT {(_balance >= 0f ? "RIGHT" : "LEFT")} {Mathf.RoundToInt(Mathf.Abs(_balance) * 100f)}% - LEAN!",
                new Color(0.5f, 0.4f, 0.3f), 0.12f);

            if (Mathf.Abs(_balance) >= 1f)
            {
                RegisterSpill();
                return;
            }
            if (now >= _nextLurchAt)
            {
                _nextLurchAt = now + LurchInterval;
                ApplyLurch();
            }
        }

        public bool HandleBark(int dogIndex) => false;

        public void Cleanup()
        {
            if (_car != null) _car.SetActive(false);
        }

        public void StageDogsForEntry()
        {
            if (_context.Dogs == null || _context.Dogs.Length < 2) return;
            Vector2 staging = _context.Bounds.center + Vector2.down * 7f;
            _context.Dogs[0].transform.position = staging + Vector2.left * 1.5f;
            _context.Dogs[1].transform.position = staging + Vector2.right * 1.5f;
            foreach (var dog in _context.Dogs)
                if (dog != null && dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 1.4f;
            return false;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("car_ride", score, timeRemaining, _state.LurchesSurvived, _state.RequiredLurches, _state.Spills,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceLurch() => ApplyLurch();
        public void ForceSpill() => RegisterSpill();

        private void ApplyLurch()
        {
            if (IsComplete || IsFailed) return;
            _balance += _lurchDirection * 0.4f;
            _lurchDirection = -_lurchDirection;
            if (Mathf.Abs(_balance) >= 1f)
            {
                RegisterSpill();
                return;
            }

            _state.SurviveLurch();
            _context.AddScore(ScoreEventCatalog.LurchSteadied.Points, ScoreEventCatalog.LurchSteadied.Label);
            _context.SetFeedback(GameManager.FeedbackKind.UnitedBark);
            _context.SetCue($"Steadied the lurch! ({_state.LurchesSurvived}/{_state.RequiredLurches}) Lean to balance.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.LurchSteadied.Label);
            _context.SpawnWorldPop(DogMidpoint(), "STEADIED!", new Color(0.55f, 1f, 0.7f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("car_lurch", 0.18f, 0.36f, 0.12f);
            _context.LogEvent("CarSteadied", $"{_state.LurchesSurvived}/{_state.RequiredLurches}");
            if (IsComplete)
                _context.AddScore(ScoreEventCatalog.RideComplete.Points, ScoreEventCatalog.RideComplete.Label);
            else
                _context.LogObjectiveChanged();
        }

        private void RegisterSpill()
        {
            if (IsComplete || IsFailed) return;
            _state.Spill();
            _balance = 0f;
            _context.AddScore(ScoreEventCatalog.CarSpill.Points, ScoreEventCatalog.CarSpill.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"The car tipped and everyone spilled! ({_state.Spills}/{MaxSpills}) Lean the other way next time.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.CarSpill.Label);
            _context.SpawnWorldPop(DogMidpoint(), "CAR SPILL!", new Color(1f, 0.38f, 0.22f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowPanic();
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("car_spill", 0.22f, 0.45f, 0.16f);
            _context.LogEvent("CarSpill", $"{_state.Spills}/{MaxSpills}");
            if (!IsFailed) _context.LogObjectiveChanged();
        }

        private Vector2 DogMidpoint() => _context.Dogs != null && _context.Dogs.Length >= 2
            ? (_context.Dogs[0].transform.position + _context.Dogs[1].transform.position) * 0.5f
            : _context.Bounds.center;
    }
}
