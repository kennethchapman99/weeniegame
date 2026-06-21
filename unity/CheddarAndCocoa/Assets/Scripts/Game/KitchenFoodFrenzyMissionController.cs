using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Complete runtime ownership for Kitchen Falling Food Frenzy.</summary>
    public sealed class KitchenFoodFrenzyMissionController : IMissionController
    {
        private const float CounterRadius = 3.6f;
        private const float SafeZoneRadius = 3.0f;
        private const float CatchRadius = 1.5f;
        private const float FallSpeed = 4.8f;
        private const float FinaleFallSpeed = 6.2f;
        private const float TelegraphSeconds = 1.25f;
        private const float FinaleTelegraphSeconds = 0.65f;

        private static readonly Color GoodColor = new(1f, 0.85f, 0.4f);
        private static readonly Color BadColor = new(0.7f, 0.4f, 0.85f);

        private readonly KitchenFoodFrenzyMissionState _state = new();
        private MissionContext _context;
        private GameObject _counterMarker;
        private GameObject _safeZoneMarker;
        private GameObject _foodObject;
        private GameObject _telegraphMarker;
        private GameObject _landingWarning;
        private Vector2 _counterPosition;
        private Vector2 _safeZonePosition;
        private float _floorY;
        private float _dropX;
        private float _telegraphUntil;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.KitchenFoodFrenzy;
        public bool IsComplete => _state.Complete;
        public KitchenFoodFrenzyMissionState State => _state;
        public Vector2 CounterPosition => _counterPosition;
        public Vector2 SafeZonePosition => _safeZonePosition;
        public GameObject FoodObject => _foodObject;
        public GameObject TelegraphObject => _telegraphMarker;
        public GameObject LandingWarningObject => _landingWarning;
        public Vector2 EntryTarget => (_counterPosition + _safeZonePosition) * 0.5f;
        public string OutcomeSummary => null;

        public string ObjectiveLabel
        {
            get
            {
                if (_state.Complete) return "Kitchen cleared - replay Kitchen Falling Food Frenzy";
                string step = _state.DropActive
                    ? "Cocoa: catch GOLD in the SAFE BOWL; clear PURPLE and let it splat"
                    : _state.TelegraphActive
                        ? "DROP TELEGRAPHED: Cocoa read the landing circle; Cheddar reset at counter"
                        : "Cheddar: bark at the COUNTER to knock the next food loose";
                string finale = _state.FinaleActive
                    ? $" - DINNER RUSH {_state.FinaleSuccesses}/{KitchenFoodFrenzyMissionState.FinaleSuccessesRequired}"
                    : string.Empty;
                return $"{step} - caught {_state.GoodCatches}/{KitchenFoodFrenzyMissionState.RequiredCatches}, combo x{_state.Combo}{finale}";
            }
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            BuildScene();
            Cleanup();
        }

        public void StartMission()
        {
            _state.Reset();
            _telegraphUntil = 0f;
            _counterPosition = new Vector2(_context.Bounds.center.x, _context.Bounds.center.y + 8f);
            _safeZonePosition = new Vector2(_context.Bounds.center.x, _context.Bounds.center.y - 5f);
            _floorY = _context.Bounds.center.y - 7f;
            SetSceneActive(true);
        }

        public void Tick(float deltaTime, float now)
        {
            if (_state.Complete)
            {
                UpdateMarkers(now);
                return;
            }

            if (_state.TelegraphActive)
            {
                if (now >= _telegraphUntil) ReleaseTelegraph();
            }
            else if (_state.DropActive && _foodObject != null)
            {
                Vector3 position = _foodObject.transform.position;
                position.y -= (_state.FinaleActive ? FinaleFallSpeed : FallSpeed) * deltaTime;
                _foodObject.transform.position = position;

                int sweeper = _context.IndexOfDog(_state.SweeperDog);
                if (sweeper >= 0 && Vector2.Distance(_context.Dogs[sweeper].transform.position, position) <= CatchRadius)
                {
                    bool safe = Vector2.Distance(_context.Dogs[sweeper].transform.position, _safeZonePosition) <= SafeZoneRadius;
                    ResolveCatch(_state.SweeperDog, safe);
                }
                else if (position.y <= _floorY)
                {
                    ResolveLetFall();
                }
            }

            UpdateMarkers(now);
        }

        public bool HandleBark(int dogIndex)
        {
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length) return false;
            DogId dogId = _context.Dogs[dogIndex].GetComponent<DogIdentity>().Id;
            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _counterPosition) > CounterRadius)
            {
                _context.SetCue(dogId == _state.ScoutDog
                    ? "Cheddar must reach the COUNTER route before barking food loose."
                    : "Cocoa guards the bowl; Cheddar owns the counter bark.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss,
                    dogId == _state.ScoutDog ? "GET TO THE COUNTER" : "CHEDDAR BARKS IT LOOSE");
                return false;
            }

            var kind = _state.FinaleActive
                ? _state.ExpectedFinaleKind
                : (_context.Random().NextDouble() < 0.7
                    ? KitchenFoodFrenzyMissionState.FoodKind.Good
                    : KitchenFoodFrenzyMissionState.FoodKind.Bad);
            return ArmTelegraph(dogId, kind);
        }

        public void Cleanup() => SetSceneActive(false);

        public void StageDogsForEntry()
        {
            int scout = _context.IndexOfDog(_state.ScoutDog);
            int sweeper = _context.IndexOfDog(_state.SweeperDog);
            if (scout >= 0) _context.Dogs[scout].transform.position = _counterPosition + new Vector2(-5f, -3f);
            if (sweeper >= 0) _context.Dogs[sweeper].transform.position = _safeZonePosition + new Vector2(5f, 0f);
            foreach (var dog in _context.Dogs)
                if (dog != null && dog.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 1.4f;
            if (_state.Complete) return false;

            if (_state.DropActive || _state.TelegraphActive)
            {
                bool sweeper = _context.IndexOfDog(_state.SweeperDog) == dogIndex;
                target = sweeper
                    ? (_state.DropActive ? _foodObject?.transform : _landingWarning?.transform)
                    : _counterMarker?.transform;
                copy = sweeper
                    ? (_state.PendingKind == KitchenFoodFrenzyMissionState.FoodKind.Bad ? "DODGE PURPLE" : "CATCH GOLD IN BOWL")
                    : "RESET AT COUNTER";
                hideDistance = sweeper ? CatchRadius : CounterRadius;
            }
            else
            {
                bool scout = _context.IndexOfDog(_state.ScoutDog) == dogIndex;
                target = scout ? _counterMarker?.transform : _safeZoneMarker?.transform;
                copy = scout ? "BARK-KNOCK FOOD" : "GUARD THE BOWL";
                hideDistance = scout ? CounterRadius : SafeZoneRadius;
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("kitchen_food_frenzy", score, timeRemaining, _state.GoodCatches,
                KitchenFoodFrenzyMissionState.RequiredCatches, _state.TotalFumbles,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceDrop(KitchenFoodFrenzyMissionState.FoodKind kind)
        {
            if (ArmTelegraph(_state.ScoutDog, kind)) ReleaseTelegraph();
        }

        public void ForceTelegraph(DogId dog, KitchenFoodFrenzyMissionState.FoodKind kind) => ArmTelegraph(dog, kind);
        public void ForceReleaseTelegraph() => ReleaseTelegraph();
        public void ForceCatch(DogId dog, bool intoSafeZone) => ResolveCatch(dog, intoSafeZone);
        public void ForceLetFall() => ResolveLetFall();

        private void BuildScene()
        {
            _counterMarker = NewMarker("KitchenCounterRoute", _context.RangeSprite ?? _context.ActorSprite,
                new Color(0.82f, 0.6f, 0.35f, 0.4f), 1,
                new Vector3(CounterRadius * 2.4f, 1.1f, 1f), "COUNTER - CHEDDAR KNOCKS FOOD LOOSE", 0.42f, 13);
            _safeZoneMarker = NewMarker("KitchenSafeBowl", _context.RangeSprite ?? _context.ActorSprite,
                new Color(0.35f, 1f, 0.55f, 0.3f), 1, Vector3.one * (SafeZoneRadius * 2.2f),
                "SAFE BOWL - COCOA CATCHES HERE", 0.42f, 13);
            _foodObject = NewMarker("KitchenFallingFood", _context.ActorSprite, GoodColor, 4, Vector3.one * 0.7f,
                "FALLING FOOD", 0.34f, 12);
            _telegraphMarker = NewMarker("KitchenPreDropTelegraph", _context.RangeSprite ?? _context.ActorSprite,
                GoodColor, 3, Vector3.one * 1.5f, "BARK KNOCK - WATCH THE LANE", 0.55f, 13);
            _landingWarning = NewMarker("KitchenLandingWarning", _context.RangeSprite ?? _context.ActorSprite,
                new Color(1f, 0.85f, 0.35f, 0.45f), 2, Vector3.one * (CatchRadius * 2.4f),
                "LANDING HERE", 0.45f, 12);
        }

        private GameObject NewMarker(string name, Sprite sprite, Color color, int sortingOrder, Vector3 scale,
            string label, float labelOffset, int labelSize)
        {
            var marker = new GameObject(name);
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            marker.transform.localScale = scale;
            _context.AddWorldLabel(marker, label, Vector3.up * labelOffset, labelSize, Color.white);
            marker.SetActive(false);
            return marker;
        }

        private void SetSceneActive(bool active)
        {
            if (_counterMarker != null)
            {
                _counterMarker.transform.position = _counterPosition;
                _counterMarker.SetActive(active);
            }
            if (_safeZoneMarker != null)
            {
                _safeZoneMarker.transform.position = _safeZonePosition;
                _safeZoneMarker.SetActive(active);
            }
            if (!active) HideFood();
        }

        private void UpdateMarkers(float now)
        {
            if (_safeZoneMarker == null) return;
            var renderer = _safeZoneMarker.GetComponent<SpriteRenderer>();
            int sweeper = _context.IndexOfDog(_state.SweeperDog);
            bool held = sweeper >= 0 && Vector2.Distance(_context.Dogs[sweeper].transform.position, _safeZonePosition) <= SafeZoneRadius;
            if (renderer != null)
                renderer.color = held ? new Color(0.35f, 1f, 0.55f, 0.5f) : new Color(0.35f, 1f, 0.55f, 0.3f);

            if (_telegraphMarker != null && _telegraphMarker.activeSelf)
                _telegraphMarker.transform.localScale = Vector3.one * (1.35f + Mathf.PingPong(now * (_state.FinaleActive ? 3.8f : 2.4f), 0.45f));
            if (_landingWarning != null && _landingWarning.activeSelf)
                _landingWarning.transform.localScale = Vector3.one * (2.5f + Mathf.PingPong(now * 2.8f, 0.8f));
        }

        private bool ArmTelegraph(DogId dog, KitchenFoodFrenzyMissionState.FoodKind kind)
        {
            var result = _state.ArmTelegraph(dog, kind);
            if (result == KitchenFoodFrenzyMissionState.TelegraphResult.WrongScout)
            {
                _context.MarkFailedInteraction(dog, "only Cheddar can bark food loose from the counter");
                _context.SetCue("Cocoa called it out, but Cheddar must make the counter knock.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CHEDDAR BARKS THE COUNTER");
                _context.LogEvent("KitchenWrongScout", "Cocoa tried counter bark");
                return false;
            }
            if (result != KitchenFoodFrenzyMissionState.TelegraphResult.Armed) return false;

            float span = CounterRadius * 1.7f;
            _dropX = Mathf.Clamp(_counterPosition.x + (float)(_context.Random().NextDouble() * 2.0 - 1.0) * span,
                _context.Bounds.xMin + 1.2f, _context.Bounds.xMax - 1.2f);
            _telegraphUntil = _context.Now() + (_state.FinaleActive ? FinaleTelegraphSeconds : TelegraphSeconds);
            Color color = kind == KitchenFoodFrenzyMissionState.FoodKind.Bad ? BadColor : GoodColor;
            _telegraphMarker.transform.position = new Vector2(_dropX, _counterPosition.y);
            _telegraphMarker.GetComponent<SpriteRenderer>().color = color;
            _telegraphMarker.SetActive(true);
            _landingWarning.transform.position = FloorPosition();
            _landingWarning.GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, 0.48f);
            _landingWarning.SetActive(true);

            bool bad = kind == KitchenFoodFrenzyMissionState.FoodKind.Bad;
            _context.SetCue(bad
                ? "Cheddar bark-knocked an ONION loose - Cocoa, clear the landing circle!"
                : "Cheddar bark-knocked dinner loose - Cocoa, line up the gold landing circle!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, bad ? "PURPLE WARNING - DODGE!" : "GOLD WARNING - CATCH!");
            _context.SpawnWorldPop(new Vector2(_dropX, _counterPosition.y), bad ? "ONION INCOMING!" : "FOOD INCOMING!", color);
            _context.RequestRumble("kitchen_telegraph", 0.08f, 0.16f, 0.1f);
            _context.LogEvent("KitchenTelegraph", bad ? "bad" : "good");
            _context.LogObjectiveChanged();
            return true;
        }

        private void ReleaseTelegraph()
        {
            if (_state.ReleaseTelegraph() != KitchenFoodFrenzyMissionState.ReleaseResult.Dropped) return;
            var kind = _state.ActiveKind;
            _foodObject.transform.position = new Vector2(_dropX, _counterPosition.y);
            _foodObject.GetComponent<SpriteRenderer>().color = kind == KitchenFoodFrenzyMissionState.FoodKind.Bad ? BadColor : GoodColor;
            _foodObject.SetActive(true);
            _telegraphMarker.SetActive(false);
            _context.LogEvent("KitchenDrop", kind == KitchenFoodFrenzyMissionState.FoodKind.Bad ? "bad" : "good");
            _context.LogObjectiveChanged();
        }

        private void ResolveCatch(DogId dog, bool intoSafeZone)
        {
            bool wasFinale = _state.FinaleActive;
            var result = _state.Catch(dog, intoSafeZone);
            int dogIndex = _context.IndexOfDog(dog);
            switch (result)
            {
                case KitchenFoodFrenzyMissionState.CatchResult.Caught:
                    int combo = _state.Combo;
                    _context.AddScore(70 + 25 * Mathf.Max(0, combo - 1), combo > 1 ? $"FOOD COMBO x{combo}" : "FOOD CAUGHT");
                    _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                    _context.SetCue($"Cocoa floored it into the bowl! Combo x{combo}.");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, combo > 1 ? $"YUM! COMBO x{combo}" : "YUM! SAFE BOWL");
                    _context.SpawnWorldPop(_safeZonePosition, combo > 1 ? $"YUM! x{combo}" : "YUM!", new Color(0.5f, 1f, 0.65f));
                    _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
                    _context.RequestRumble("kitchen_catch", 0.18f, 0.32f, 0.12f);
                    if (dogIndex >= 0 && _context.DogFeedback[dogIndex] != null) _context.DogFeedback[dogIndex].ShowProudBrief();
                    HideFood();
                    _context.LogEvent("KitchenCatch", $"{_state.GoodCatches}/{KitchenFoodFrenzyMissionState.RequiredCatches}");
                    if (!wasFinale && _state.FinaleActive)
                    {
                        _context.AddScore(100, "DINNER RUSH STARTED");
                        _context.SetCue("DINNER RUSH! Three fast calls: catch gold, dodge purple, catch gold.");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "DINNER RUSH - 3 CALLS!");
                        _context.SpawnWorldPop(_context.Bounds.center, "DINNER RUSH!", new Color(1f, 0.72f, 0.25f));
                        _context.RequestRumble("dinner_rush", 0.28f, 0.5f, 0.2f);
                        _context.LogEvent("KitchenFinaleStarted", "GOOD / BAD / GOOD");
                    }
                    break;
                case KitchenFoodFrenzyMissionState.CatchResult.GrossOut:
                    _context.MarkFailedInteraction(dog, "ate the onion");
                    _context.SetCue("BLECH! Cocoa ate the onion - that one was a DODGE.");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "BLECH! ONION");
                    _context.SpawnWorldPop(_safeZonePosition, "BLECH! ONION!", BadColor);
                    _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
                    _context.RequestRumble("kitchen_gross", 0.3f, 0.58f, 0.18f);
                    if (dogIndex >= 0 && _context.DogFeedback[dogIndex] != null) _context.DogFeedback[dogIndex].ShowPanic();
                    HideFood();
                    _context.LogEvent("KitchenGrossOut", "onion eaten");
                    break;
                case KitchenFoodFrenzyMissionState.CatchResult.UnsafeLanding:
                    _context.SetCue("SPLAT! Caught it off the bowl - steer good food into the SAFE BOWL.");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SPLAT! MISSED BOWL");
                    _context.SpawnWorldPop(_foodObject != null ? (Vector2)_foodObject.transform.position : _safeZonePosition,
                        "SPLAT! NOT IN BOWL", new Color(1f, 0.7f, 0.3f));
                    _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
                    _context.RequestRumble("kitchen_splat", 0.2f, 0.42f, 0.14f);
                    if (dogIndex >= 0 && _context.DogFeedback[dogIndex] != null) _context.DogFeedback[dogIndex].ShowPanic();
                    HideFood();
                    _context.LogEvent("KitchenUnsafe", "off bowl");
                    break;
                case KitchenFoodFrenzyMissionState.CatchResult.WrongCatcher:
                    _context.MarkFailedInteraction(dog, "Cheddar can't floor the catch - that is Cocoa's job");
                    _context.SetCue("Cheddar, you knock it loose - let COCOA floor the catch!");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "WRONG DOG - COCOA FLOORS IT");
                    _context.SpawnWorldPop(_foodObject != null ? (Vector2)_foodObject.transform.position : _counterPosition,
                        "COCOA CATCHES!", new Color(1f, 0.6f, 0.3f));
                    _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
                    _context.LogEvent("KitchenWrongCatcher", "scout tried floor catch");
                    break;
                default:
                    return;
            }
            UpdateMarkers(_context.Now());
            _context.LogObjectiveChanged();
        }

        private void ResolveLetFall()
        {
            var result = _state.LetFall();
            int sweeper = _context.IndexOfDog(_state.SweeperDog);
            if (result == KitchenFoodFrenzyMissionState.FallResult.DodgedBad)
            {
                _context.AddScore(20, "ONION DODGED");
                _context.SetCue("Good dodge! The onion splatted harmlessly.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.ScoreDelta, "DODGED THE ONION");
                _context.SpawnWorldPop(FloorPosition(), "DODGED!", new Color(0.5f, 1f, 0.65f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
                _context.RequestRumble("kitchen_dodge", 0.12f, 0.24f, 0.1f);
                if (sweeper >= 0 && _context.DogFeedback[sweeper] != null) _context.DogFeedback[sweeper].ShowProudBrief();
                _context.LogEvent("KitchenDodge", "bad food avoided");
            }
            else if (result == KitchenFoodFrenzyMissionState.FallResult.MissedGood)
            {
                _context.SetCue("Missed! Good food hit the floor - combo reset.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "MISS! FOOD SPLAT");
                _context.SpawnWorldPop(FloorPosition(), "MISS! SPLAT", new Color(1f, 0.6f, 0.3f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
                _context.RequestRumble("kitchen_miss", 0.25f, 0.48f, 0.16f);
                if (sweeper >= 0 && _context.DogFeedback[sweeper] != null) _context.DogFeedback[sweeper].ShowPanic();
                _context.LogEvent("KitchenMiss", "good food dropped");
            }
            HideFood();
            UpdateMarkers(_context.Now());
            _context.LogObjectiveChanged();
        }

        private Vector2 FloorPosition() => new(_dropX, _floorY);

        private void HideFood()
        {
            if (_foodObject != null) _foodObject.SetActive(false);
            if (_telegraphMarker != null) _telegraphMarker.SetActive(false);
            if (_landingWarning != null) _landingWarning.SetActive(false);
        }
    }
}
