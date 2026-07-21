using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Car Ride Chaos: Cheddar and Cocoa ride the back bench home while the driver takes turns
    /// and hits the brakes. Turns tilt the whole cabin and slide the dogs plus the loose seat
    /// junk (cooler, toy bin) toward the outside of the turn — fight the slide or brace, and
    /// jump the junk as it sweeps across the bench. A brake telegraph demands a brace (interact)
    /// before the stop or the dog is flung into the front seats. Tumbles add up; too many fails
    /// the ride. A united bark makes the driver ease up on the next road event.
    ///
    /// Cheddar is light chaos-puppy cargo and slides hardest; Cocoa plants like a veteran and
    /// slides least — she holds the line while he does the acrobatics.
    /// </summary>
    public sealed class CarRideMissionController : IMissionController, IMissionInteractionController,
        IMissionUnitedBarkListener, IMissionPressureHud, IMissionSuccessPresentationController
    {
        public enum RoadEventKind { TurnLeft, TurnRight, Brake }
        private enum Phase { Cruise, Telegraph, Turning, BrakeSettle }

        private const int MaxTumbles = 5;
        private const float CruiseSeconds = 2.6f;
        private const float TelegraphSeconds = 1.6f;
        private const float TurnSeconds = 3.2f;
        private const float BrakeSettleSeconds = 0.9f;
        private const float BraceSeconds = 1.8f;
        private const float DogSlideSpeed = 3.4f;
        private const float ObstacleSlideSpeed = 6.8f;
        private const float CheddarSlideMultiplier = 1.25f;
        private const float CocoaSlideMultiplier = 0.85f;
        private const float CabinTiltDegrees = 5.5f;
        private const float EasedIntensity = 0.55f;
        private const float SeatHalfWidth = 16f;
        private const float SeatTopOffset = 2.6f;
        private const float SeatBottomOffset = 5.8f;
        private const float DoorSquishMargin = 0.9f;
        private const float ObstacleBonkRadius = 1.6f;
        private const float PartnerBraceRange = 3.4f;
        private const float SuccessHoldSeconds = 1.15f;

        /// <summary>The whole ride home, in order. Length defines the clear requirement.</summary>
        private static readonly RoadEventKind[] RideScript =
        {
            RoadEventKind.TurnRight, RoadEventKind.Brake, RoadEventKind.TurnLeft,
            RoadEventKind.TurnRight, RoadEventKind.Brake, RoadEventKind.TurnLeft,
            RoadEventKind.Brake
        };

        private readonly CarRideMissionState _state = new();
        private MissionContext _context;
        private GameObject _dashboard;
        private MissionLevelAreaArt _levelAreaArt;
        private readonly GameObject[] _obstacles = new GameObject[2];
        private static readonly string[] ObstacleLabels = { "COOLER", "TOY BIN" };

        private Phase _phase;
        private float _phaseEndsAt;
        private RoadEventKind _currentEvent;
        private int _eventTumbles;
        private bool _driverEased;
        private bool _cheddarTuckedForBrake;
        private float _visualTilt;
        private float _sceneryScroll;
        private float _successHoldRemaining;
        private readonly float[] _bracedUntil = new float[2];
        private readonly bool[] _doorSquished = new bool[2];
        private readonly bool[,] _obstacleBonked = new bool[2, 2];
        private readonly bool[,] _obstacleHopped = new bool[2, 2];

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.CarRide;
        public bool IsComplete => _state.ReadyToClear() && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _state.ReadyToClear() && _successHoldRemaining > 0f;
        public float SuccessHoldRemaining => _successHoldRemaining;
        public bool IsFailed => _state.TooManyTumbles(MaxTumbles);
        public string FailReason => IsFailed ? "The backseat crew got tossed around one tumble too many." : null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildCarRideSummary(_state);
        public Vector2 EntryTarget => _context != null ? _context.Bounds.center : Vector2.zero;
        public CarRideMissionState State => _state;
        public RoadEventKind CurrentRoadEvent => _currentEvent;
        public bool IsTelegraphing => _phase == Phase.Telegraph;
        public bool IsTurning => _phase == Phase.Turning;
        public bool DriverEased => _driverEased;
        public bool CheddarTuckedForBrake => _cheddarTuckedForBrake;
        public bool IsDogBraced(int dogIndex) => BraceActive(dogIndex, _context.Now());
        /// <summary>CF2.3 read-only presentation summary: how far into the scripted ride home we
        /// are, 0 at the start to 1 once every road event is resolved.</summary>
        public float RideProgress => _state.RequiredEvents > 0
            ? Mathf.Clamp01((float)_state.EventsResolved / _state.RequiredEvents) : 0f;
        public string PressureLabel => "SLIDE FORCE";
        public bool PressureVisible => !IsPresentingSuccessfulOutcome;
        public float PressureNormalized => Mathf.Clamp01(Mathf.Abs(_visualTilt) / CabinTiltDegrees);
        public Color PressureColor => Color.Lerp(
            new Color(0.35f, 0.92f, 0.62f),
            new Color(1f, 0.2f, 0.08f),
            PressureNormalized);

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "We're home! Cocoa held the line - Cheddar survived the backseat rodeo!";
                string beat = _phase switch
                {
                    Phase.Telegraph when _currentEvent == RoadEventKind.Brake && !_cheddarTuckedForBrake => "BRAKES AHEAD - Cocoa brace, Cheddar tuck!",
                    Phase.Telegraph when _currentEvent == RoadEventKind.Brake => "BRAKE TEAM READY - hold together!",
                    Phase.Telegraph => "Turn ahead - hold on!",
                    Phase.Turning => "Sliding - jump the junk!",
                    _ => "Watch the driver",
                };
                return $"Ride home ({beat}): road events {_state.EventsResolved}/{_state.RequiredEvents}, tumbles {_state.Tumbles}/{MaxTumbles}";
            }
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _dashboard = _context.CreateActor(ArenaArtCatalog.ActorKind.Predator);
            _dashboard.name = "Car Ride Driver";
            MissionPropArt.AttachObject(_dashboard, FinalGameplayArt.CarDashboardDriver, 0.016f, 18, false);
            _dashboard.SetActive(false);

            string[] obstacleArt = { FinalGameplayArt.SeatCooler, FinalGameplayArt.SeatToyBin };
            string[] obstacleNames = { "SeatObstacle_Cooler", "SeatObstacle_ToyBin" };
            for (int i = 0; i < _obstacles.Length; i++)
            {
                _obstacles[i] = BuildObstacle(obstacleNames[i], obstacleArt[i], ObstacleLabels[i]);
                _obstacles[i].SetActive(false);
            }
        }

        public void StartMission()
        {
            _state.Configure(RideScript.Length);
            _phase = Phase.Cruise;
            _phaseEndsAt = _context.Now() + CruiseSeconds;
            _currentEvent = RideScript[0];
            _eventTumbles = 0;
            _driverEased = false;
            _cheddarTuckedForBrake = false;
            _visualTilt = 0f;
            _sceneryScroll = 0f;
            _successHoldRemaining = 0f;
            for (int i = 0; i < 2; i++)
            {
                _bracedUntil[i] = 0f;
                _doorSquished[i] = false;
            }
            ClearEventLatches();

            Vector2 center = _context.Bounds.center;
            _levelAreaArt = MissionLevelAreaArt.CreateCarRideArea(_context.Bounds);
            _dashboard.transform.position = center + Vector2.up * 8.6f;
            _dashboard.SetActive(true);
            _obstacles[0].transform.position = center + new Vector2(-5.5f, -1.3f);
            _obstacles[1].transform.position = center + new Vector2(5.5f, -1.3f);
            foreach (var obstacle in _obstacles)
                if (obstacle != null) obstacle.SetActive(true);

            SetDriverCalm();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_state.ReadyToClear())
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                UpdatePresentation(deltaTime, 0f);
                return;
            }
            if (_state.ReadyToClear() || IsFailed) return;

            switch (_phase)
            {
                case Phase.Cruise:
                    UpdatePresentation(deltaTime, 0f);
                    if (now >= _phaseEndsAt) BeginTelegraph(now, RideScript[_state.EventsResolved % RideScript.Length]);
                    break;
                case Phase.Telegraph:
                    UpdatePresentation(deltaTime, 0f);
                    if (now < _phaseEndsAt) break;
                    if (_currentEvent == RoadEventKind.Brake)
                    {
                        FireBrake(now);
                    }
                    else
                    {
                        _phase = Phase.Turning;
                        _phaseEndsAt = now + TurnSeconds;
                        _context.SetActorState(_dashboard, "DRIVER: TURNING - HOLD ON!", DriverTint, 0.3f);
                    }
                    break;
                case Phase.Turning:
                    TickTurn(deltaTime, now);
                    break;
                case Phase.BrakeSettle:
                    UpdatePresentation(deltaTime, 0f);
                    if (now >= _phaseEndsAt) ResolveEvent(now);
                    break;
            }

            ClampDogsToSeat();
        }

        public bool HandleBark(int dogIndex) => false;

        /// <summary>Interact = brace: plant claws so turns can't slide you and brakes can't fling
        /// you. It doesn't stop a sliding cooler - jump for that.</summary>
        public bool HandleInteract(int dogIndex)
        {
            if (_state.ReadyToClear() || IsFailed || dogIndex < 0 || dogIndex >= 2) return false;
            if (_phase == Phase.Telegraph && _currentEvent == RoadEventKind.Brake
                && dogIndex >= 0 && dogIndex < _context.Dogs.Length)
                return HandleBrakeBrace(dogIndex);

            SetBrace(dogIndex);
            return true;
        }

        private bool HandleBrakeBrace(int dogIndex)
        {
            DogId dogId = DogIdAt(dogIndex);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            float now = _context.Now();

            if (dogId == DogId.Cocoa)
            {
                SetBrace(dogIndex);
                _context.SetCue("Cocoa planted like an anchor - Cheddar, get beside her and Interact to tuck in!");
                _context.SetActorState(_dashboard, "DRIVER: BRAKES - CHEDDAR TUCK BEHIND COCOA!", DriverTint, 0.3f);
                _context.LogObjectiveChanged();
                return true;
            }

            if (cocoa < 0 || !BraceActive(cocoa, now))
            {
                _context.MarkFailedInteraction(dogId, "Cocoa must plant first so Cheddar has an anchor");
                _context.SetCue("Cheddar cannot brace that chaos-body alone - Cocoa must Interact and plant first!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "COCOA PLANTS FIRST!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "TOO SOON!", new Color(1f, 0.72f, 0.25f));
                return true;
            }

            if (cheddar < 0 || Vector2.Distance(_context.Dogs[cheddar].transform.position,
                    _context.Dogs[cocoa].transform.position) > PartnerBraceRange)
            {
                _context.MarkFailedInteraction(dogId, "get beside planted Cocoa before tucking in");
                _context.SetCue("Cocoa is planted, but Cheddar is too far away - get beside her and Interact!");
                return true;
            }

            SetBrace(dogIndex);
            _cheddarTuckedForBrake = true;
            _context.CreditDog(cocoa);
            _context.CreditDog(cheddar);
            _context.SetCue("Cheddar tucked behind Cocoa's planted stance - hold together for the brake!");
            _context.SetActorState(_dashboard, "DRIVER: BRAKE TEAM READY!", DriverTint, 0.22f);
            _context.SpawnWorldPop(DogMidpoint(), "TUCKED SAFE!", new Color(0.55f, 1f, 0.72f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("car_brace_team", 0.12f, 0.28f, 0.1f);
            _context.LogEvent("CarBrakeTeamReady", _state.EventsResolved.ToString());
            _context.LogObjectiveChanged();
            return true;
        }

        private void SetBrace(int dogIndex)
        {
            float now = _context.Now();
            bool alreadyBraced = BraceActive(dogIndex, now);
            _bracedUntil[dogIndex] = now + BraceSeconds;
            if (!alreadyBraced)
            {
                var dog = _context.Dogs[dogIndex];
                if (dog != null)
                {
                    bool isCocoa = dog.TryGetComponent<DogIdentity>(out var identity) && identity.Id == DogId.Cocoa;
                    _context.SpawnWorldPop(dog.transform.position,
                        isCocoa ? "COCOA PLANTS!" : "CHEDDAR HUNKERS!", new Color(0.65f, 0.85f, 1f));
                    _context.Pulse(dog.gameObject, 0.14f);
                }
                _context.LogEvent("CarBrace", dogIndex.ToString());
            }
        }

        /// <summary>United bark = "hey, easy back there!" The driver eases off for the next event.</summary>
        public void OnUnitedBark()
        {
            if (_state.ReadyToClear() || IsFailed || _driverEased) return;
            if (_phase != Phase.Cruise && _phase != Phase.Telegraph) return;
            _driverEased = true;
            _context.SetCue("The driver hears the barking and eases off the gas!");
            _context.SetActorState(_dashboard, "DRIVER: okay, okay - easing up!", DriverTint, 0.12f);
            _context.LogEvent("CarDriverEased", _state.EventsResolved.ToString());
        }

        public void Cleanup()
        {
            if (_dashboard != null) _dashboard.SetActive(false);
            foreach (var obstacle in _obstacles)
                if (obstacle != null) obstacle.SetActive(false);
            if (_levelAreaArt != null)
            {
                Object.Destroy(_levelAreaArt.gameObject);
                _levelAreaArt = null;
            }
        }

        public void StageDogsForEntry()
        {
            if (_context.Dogs == null || _context.Dogs.Length < 2) return;
            Vector2 bench = _context.Bounds.center + Vector2.down * 1.3f;
            _context.Dogs[0].transform.position = bench + Vector2.left * 2.5f;
            _context.Dogs[1].transform.position = bench + Vector2.right * 2.5f;
            foreach (var dog in _context.Dogs)
                if (dog != null && dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            if (_phase == Phase.Telegraph && _currentEvent == RoadEventKind.Brake
                && dogIndex >= 0 && dogIndex < _context.Dogs.Length)
            {
                bool isCocoa = DogIdAt(dogIndex) == DogId.Cocoa;
                int cocoa = _context.IndexOfDog(DogId.Cocoa);
                target = isCocoa
                    ? _dashboard.transform
                    : cocoa >= 0 ? _context.Dogs[cocoa].transform : _dashboard.transform;
                copy = isCocoa ? "BRACE FIRST" : "TUCK BEHIND COCOA";
                hideDistance = isCocoa ? 1.4f : PartnerBraceRange;
                return target != null;
            }
            target = null;
            copy = string.Empty;
            hideDistance = 1.4f;
            return false;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("car_ride", score, timeRemaining, _state.EventsResolved, _state.RequiredEvents, _state.Tumbles,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        // ---------------------------------------------------------------- test hooks
        /// <summary>Test hook: telegraph a specific road event now (headless time never reaches
        /// the scripted schedule).</summary>
        public void ForceBeginRoadEvent(RoadEventKind kind) => BeginTelegraph(_context.Now(), kind);

        /// <summary>Test hook: resolve the telegraphed/current event now. Brakes evaluate braces
        /// exactly like the live path, so an unbraced forced brake still tumbles both dogs.</summary>
        public void ForceResolveRoadEvent()
        {
            if (_state.ReadyToClear() || IsFailed) return;
            float now = _context.Now();
            if (_phase == Phase.Telegraph && _currentEvent == RoadEventKind.Brake)
            {
                FireBrake(now);
                ResolveEvent(now);
                return;
            }
            if (_phase == Phase.Telegraph) _phase = Phase.Turning;
            ResolveEvent(now);
        }

        /// <summary>Test hook: bank one clean road event (the old ForceLurch equivalent).</summary>
        public void ForceEventSurvived()
        {
            if (_state.ReadyToClear() || IsFailed) return;
            _currentEvent = RoadEventKind.TurnLeft;
            _eventTumbles = 0;
            _phase = Phase.Turning;
            ResolveEvent(_context.Now());
        }

        /// <summary>Test hook: run one turn-slide step at full intensity with a real dt, since
        /// headless frame time is too small to accumulate slide distance.</summary>
        public void ForceTurnSlide(float deltaTime, RoadEventKind kind = RoadEventKind.TurnRight)
        {
            if (_state.ReadyToClear() || IsFailed || kind == RoadEventKind.Brake) return;
            if (_phase != Phase.Turning || _currentEvent != kind)
            {
                _currentEvent = kind;
                _phase = Phase.Turning;
                _phaseEndsAt = _context.Now() + TurnSeconds;
            }
            RunTurnSlide(deltaTime, _driverEased ? EasedIntensity : 1f);
            ClampDogsToSeat();
        }

        public void ForceBrace(int dogIndex) => HandleInteract(dogIndex);
        public void ForceTumble(int dogIndex) => Tumble(dogIndex, "TUMBLE!", Vector2.up * 4f);
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;
        public int DogIndexOf(DogId dogId) => _context.IndexOfDog(dogId);

        // ---------------------------------------------------------------- internals
        private static readonly Color DriverTint = new(0.45f, 0.42f, 0.4f);
        // CF2.3: the dashboard is the one fixed prop for this mission's whole road-script arc (the
        // cabin tilts, obstacles slide, dogs move - the driver doesn't). Warming its calm-phase tint
        // and copy toward "almost home" as EventsResolved climbs gives the WORLD a baseline that
        // tracks overall ride progress, layered under the existing per-event telegraph/turn/brake
        // presentation (untouched). Same pattern CF1.4 used for Pee Break's Teenager.
        private static readonly Color DriverAlmostHomeTint = new(0.55f, 0.72f, 0.5f);

        private void BeginTelegraph(float now, RoadEventKind kind)
        {
            if (_state.ReadyToClear() || IsFailed) return;
            _currentEvent = kind;
            _eventTumbles = 0;
            _phase = Phase.Telegraph;
            _phaseEndsAt = now + TelegraphSeconds;
            ClearEventLatches();
            _cheddarTuckedForBrake = false;
            for (int i = 0; i < 2; i++) _doorSquished[i] = false;

            string warning = kind switch
            {
                RoadEventKind.TurnLeft => "LEFT TURN AHEAD - HOLD ON!",
                RoadEventKind.TurnRight => "RIGHT TURN AHEAD - HOLD ON!",
                _ => "BRAKES AHEAD - BRACE (INTERACT)!",
            };
            _context.SetActorState(_dashboard, $"DRIVER: {warning}", DriverTint, 0.3f);
            _context.SetCue(kind == RoadEventKind.Brake
                ? "Brakes ahead! Cocoa Interact to plant first, then Cheddar tuck beside her!"
                : "Turn ahead! Fight the slide and jump the junk as it sweeps past!");
            _context.RequestRumble("car_telegraph", 0.12f, 0.2f, 0.12f);
            _context.LogEvent("CarTelegraph", kind.ToString());
            _context.LogObjectiveChanged();
        }

        private void TickTurn(float deltaTime, float now)
        {
            float progress = Mathf.Clamp01(1f - (_phaseEndsAt - now) / TurnSeconds);
            float intensity = Mathf.Sin(Mathf.PI * progress) * (_driverEased ? EasedIntensity : 1f);
            RunTurnSlide(deltaTime, intensity);
            if (now >= _phaseEndsAt) ResolveEvent(now);
        }

        /// <summary>One slide step: tilt the cabin, slide unbraced dogs and the seat junk toward
        /// the outside of the turn, then resolve door squishes and junk bonks.</summary>
        private void RunTurnSlide(float deltaTime, float intensity)
        {
            int slideDir = _currentEvent == RoadEventKind.TurnLeft ? 1 : -1;
            UpdatePresentation(deltaTime, slideDir * intensity);

            float now = _context.Now();
            Rect seat = SeatRect();
            for (int i = 0; i < 2; i++)
            {
                var dog = _context.Dogs != null && i < _context.Dogs.Length ? _context.Dogs[i] : null;
                if (dog == null) continue;
                bool braced = BraceActive(i, now);
                if (!braced && !dog.IsJumping)
                {
                    float multiplier = dog.TryGetComponent<DogIdentity>(out var identity) && identity.Id == DogId.Cheddar
                        ? CheddarSlideMultiplier
                        : CocoaSlideMultiplier;
                    dog.transform.position += Vector3.right * (slideDir * DogSlideSpeed * multiplier * intensity * deltaTime);
                }

                // Pinned against the downhill door at real slide force = a squish tumble.
                float doorEdge = slideDir > 0 ? seat.xMax : seat.xMin;
                bool atDoor = Mathf.Abs(dog.transform.position.x - doorEdge) <= DoorSquishMargin;
                if (!braced && !_doorSquished[i] && intensity > 0.45f && atDoor)
                {
                    _doorSquished[i] = true;
                    Tumble(i, "DOOR SQUISH!", new Vector2(-slideDir * 5f, 1.5f));
                }
            }

            for (int o = 0; o < _obstacles.Length; o++)
            {
                var obstacle = _obstacles[o];
                if (obstacle == null) continue;
                Vector3 pos = obstacle.transform.position;
                pos.x = Mathf.Clamp(pos.x + slideDir * ObstacleSlideSpeed * intensity * deltaTime,
                    seat.xMin + 1.1f, seat.xMax - 1.1f);
                obstacle.transform.position = pos;

                if (intensity <= 0.25f) continue;
                for (int i = 0; i < 2; i++)
                {
                    var dog = _context.Dogs != null && i < _context.Dogs.Length ? _context.Dogs[i] : null;
                    if (dog == null) continue;
                    Vector2 delta = dog.transform.position - obstacle.transform.position;
                    if (Mathf.Abs(delta.x) > ObstacleBonkRadius || Mathf.Abs(delta.y) > ObstacleBonkRadius) continue;

                    if (dog.IsJumping)
                    {
                        if (_obstacleHopped[o, i]) continue;
                        _obstacleHopped[o, i] = true;
                        _context.SpawnWorldPop(dog.transform.position + Vector3.up * 0.8f, "CLEAN HOP!",
                            new Color(0.55f, 1f, 0.7f));
                        _context.CreditDog(i);
                        _context.LogEvent("CarCleanHop", $"{ObstacleLabels[o]} dog{i}");
                    }
                    else if (!_obstacleBonked[o, i])
                    {
                        _obstacleBonked[o, i] = true;
                        Tumble(i, $"{ObstacleLabels[o]} BONK!", new Vector2(slideDir * 6f, 1.2f));
                    }
                }
            }
        }

        private void FireBrake(float now)
        {
            _context.SetActorState(_dashboard, "DRIVER: SCREEECH!", DriverTint, 0.3f);
            _context.RequestShake(0.3f);
            _context.RequestRumble("car_brake", 0.3f, 0.55f, 0.2f);
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);

            // Everything loose lurches toward the front seats, dogs included if unbraced.
            Rect seat = SeatRect();
            foreach (var obstacle in _obstacles)
            {
                if (obstacle == null) continue;
                Vector3 pos = obstacle.transform.position;
                pos.y = Mathf.Min(pos.y + 1.6f, seat.yMax - 0.6f);
                obstacle.transform.position = pos;
            }

            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            bool cocoaAnchored = cocoa >= 0 && BraceActive(cocoa, now);
            bool pairStillTogether = cocoa >= 0 && cheddar >= 0
                && Vector2.Distance(_context.Dogs[cocoa].transform.position,
                    _context.Dogs[cheddar].transform.position) <= PartnerBraceRange;

            for (int i = 0; i < 2; i++)
            {
                var dog = _context.Dogs != null && i < _context.Dogs.Length ? _context.Dogs[i] : null;
                if (dog == null) continue;
                DogId dogId = DogIdAt(i);
                bool protectedFromBrake = dogId == DogId.Cocoa
                    ? cocoaAnchored
                    : _cheddarTuckedForBrake && cocoaAnchored && pairStillTogether && BraceActive(i, now);
                if (protectedFromBrake)
                {
                    _context.AddScore(ScoreEventCatalog.BraceHeld.Points, ScoreEventCatalog.BraceHeld.Label);
                    _context.CreditDog(i);
                    _context.SpawnWorldPop(dog.transform.position,
                        dogId == DogId.Cheddar ? "TUCKED SAFE!" : "ANCHORED!", new Color(0.65f, 0.85f, 1f));
                    if (_context.DogFeedback != null && i < _context.DogFeedback.Length && _context.DogFeedback[i] != null)
                        _context.DogFeedback[i].ShowProudBrief();
                }
                else
                {
                    Tumble(i, "FLUNG FORWARD!", new Vector2(Random.Range(-1.2f, 1.2f), 7.5f));
                }
            }

            _phase = Phase.BrakeSettle;
            _phaseEndsAt = now + BrakeSettleSeconds;
        }

        private void ResolveEvent(float now)
        {
            if (_state.ReadyToClear() || IsFailed) return;
            _state.ResolveEvent();
            _driverEased = false;
            _cheddarTuckedForBrake = false;
            ClearEventLatches();

            if (_eventTumbles == 0)
            {
                _context.AddScore(ScoreEventCatalog.RoadEventCleared.Points, ScoreEventCatalog.RoadEventCleared.Label);
                if (_context.Dogs != null)
                    for (int i = 0; i < _context.Dogs.Length; i++)
                        _context.CreditDog(i);
                _context.SetFeedback(GameManager.FeedbackKind.UnitedBark);
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.RoadEventCleared.Label);
                _context.SpawnWorldPop(DogMidpoint(), "SMOOTH!", new Color(0.55f, 1f, 0.7f));
                foreach (var feedback in _context.DogFeedback)
                    if (feedback != null) feedback.ShowProudBrief();
                _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                _context.RequestRumble("car_smooth", 0.18f, 0.36f, 0.12f);
                _context.SetCue($"Rode it out clean! ({_state.EventsResolved}/{_state.RequiredEvents})");
            }
            else
            {
                _context.SetCue($"Rough one - {_eventTumbles} tumble{(_eventTumbles == 1 ? "" : "s")}. ({_state.EventsResolved}/{_state.RequiredEvents})");
            }
            _context.LogEvent("CarRoadEventResolved", $"{_state.EventsResolved}/{_state.RequiredEvents}");

            if (_state.ReadyToClear())
            {
                _successHoldRemaining = SuccessHoldSeconds;
                _context.AddScore(ScoreEventCatalog.RideComplete.Points, ScoreEventCatalog.RideComplete.Label);
                _context.SetActorState(_dashboard, "DRIVER: we're home!", DriverTint, 0.12f);
                _context.SetCue("We're home! Cocoa held the line and Cheddar survived the backseat rodeo!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "WE'RE HOME!");
                _context.SpawnWorldPop(DogMidpoint() + Vector2.up * 1.2f, "WE'RE HOME!", new Color(1f, 0.9f, 0.4f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
                _context.RequestRumble("car_home", 0.3f, 0.55f, 0.2f);
                _context.LogEvent("CarRidePayoff", "Home arrival; holding live-world success beat");
                return;
            }

            _phase = Phase.Cruise;
            _phaseEndsAt = now + CruiseSeconds;
            SetDriverCalm();
            _context.LogObjectiveChanged();
        }

        private void Tumble(int dogIndex, string popText, Vector2 fling)
        {
            if (_state.ReadyToClear() || IsFailed) return;
            _eventTumbles++;
            _state.Tumble();
            _bracedUntil[dogIndex] = 0f;
            _context.AddScore(ScoreEventCatalog.CarTumble.Points, ScoreEventCatalog.CarTumble.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.CarTumble.Label);

            var dog = _context.Dogs != null && dogIndex < _context.Dogs.Length ? _context.Dogs[dogIndex] : null;
            if (dog != null)
            {
                _context.SpawnWorldPop(dog.transform.position, popText, new Color(1f, 0.38f, 0.22f));
                dog.ApplyWrestleStun(0.85f, fling);
            }
            if (_context.DogFeedback != null && dogIndex < _context.DogFeedback.Length && _context.DogFeedback[dogIndex] != null)
                _context.DogFeedback[dogIndex].ShowPanic();

            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("car_tumble", 0.22f, 0.45f, 0.16f);
            _context.RequestShake(0.18f);
            _context.LogEvent("CarTumble", $"{popText} {_state.Tumbles}/{MaxTumbles}");
            if (!IsFailed) _context.LogObjectiveChanged();
        }

        /// <summary>Cosmetic layer: ease the cabin roll toward the current slide force and keep
        /// the windshield scenery rolling past (faster mid-event).</summary>
        private void UpdatePresentation(float deltaTime, float signedIntensity)
        {
            if (_levelAreaArt == null) return;
            _visualTilt = Mathf.Lerp(_visualTilt, -signedIntensity * CabinTiltDegrees, Mathf.Clamp01(6f * deltaTime));
            _levelAreaArt.transform.rotation = Quaternion.Euler(0f, 0f, _visualTilt);

            var scenery = _levelAreaArt.WindshieldScenery;
            if (scenery == null) return;
            float speed = 2.2f + Mathf.Abs(signedIntensity) * 4.5f;
            _sceneryScroll -= speed * deltaTime;
            if (_sceneryScroll <= -MissionLevelAreaArt.SceneryWrapDistance)
                _sceneryScroll += MissionLevelAreaArt.SceneryWrapDistance;
            var local = scenery.localPosition;
            local.x = _sceneryScroll;
            scenery.localPosition = local;
        }

        private void ClampDogsToSeat()
        {
            if (_context.Dogs == null) return;
            Rect seat = SeatRect();
            foreach (var dog in _context.Dogs)
            {
                if (dog == null) continue;
                Vector3 pos = dog.transform.position;
                pos.x = Mathf.Clamp(pos.x, seat.xMin, seat.xMax);
                pos.y = Mathf.Clamp(pos.y, seat.yMin, seat.yMax);
                dog.transform.position = pos;
            }
        }

        private Rect SeatRect()
        {
            Vector2 center = _context.Bounds.center;
            return new Rect(center.x - SeatHalfWidth, center.y - SeatBottomOffset,
                SeatHalfWidth * 2f, SeatBottomOffset + SeatTopOffset);
        }

        private bool BraceActive(int dogIndex, float now)
        {
            if (dogIndex < 0 || dogIndex >= _bracedUntil.Length) return false;
            if (now >= _bracedUntil[dogIndex]) return false;
            var dog = _context.Dogs != null && dogIndex < _context.Dogs.Length ? _context.Dogs[dogIndex] : null;
            return dog == null || !dog.IsJumping; // airborne dogs have nothing planted
        }

        private DogId DogIdAt(int dogIndex) => _context.Dogs != null && dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
            _context.Dogs[dogIndex] != null && _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id : DogId.Cheddar;

        private void SetDriverCalm()
        {
            int remaining = Mathf.Max(0, _state.RequiredEvents - _state.EventsResolved);
            string copy = remaining <= 1
                ? "DRIVER: cruising - almost home!"
                : $"DRIVER: cruising - {remaining} stops from home";
            _context.SetActorState(_dashboard, copy, Color.Lerp(DriverTint, DriverAlmostHomeTint, RideProgress), 0.12f);
        }

        private void ClearEventLatches()
        {
            for (int o = 0; o < 2; o++)
                for (int i = 0; i < 2; i++)
                {
                    _obstacleBonked[o, i] = false;
                    _obstacleHopped[o, i] = false;
                }
        }

        private Vector2 DogMidpoint() => _context.Dogs != null && _context.Dogs.Length >= 2
            ? (Vector2)((_context.Dogs[0].transform.position + _context.Dogs[1].transform.position) * 0.5f)
            : _context.Bounds.center;

        private GameObject BuildObstacle(string name, string resourcePath, string label)
        {
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            renderer.sprite = sprite;
            renderer.sortingOrder = 16;
            if (sprite != null)
            {
                const float worldSize = 2.5f;
                go.transform.localScale = new Vector3(
                    worldSize / Mathf.Max(0.01f, sprite.bounds.size.x),
                    worldSize / Mathf.Max(0.01f, sprite.bounds.size.y), 1f);
            }
            _context.AddWorldLabel(go, label, new Vector3(0f, 1.7f, -0.1f), 34, Color.white);
            return go;
        }
    }
}
