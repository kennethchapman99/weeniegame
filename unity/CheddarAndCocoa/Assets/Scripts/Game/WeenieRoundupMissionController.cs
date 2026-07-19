using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class WeenieRoundupMissionController : IMissionController, IMissionSuccessPresentationController
    {
        private const int RequiredDeliveries = 5;
        private const int JumboMarkerIndex = RequiredDeliveries - 1;
        private const float PickupRange = 1.6f;
        private const float BowlRange = 2.2f;
        private const float JumboSteadyRange = 3.4f;
        private const float JumboSeparationGraceSeconds = 0.8f;
        private const float SuccessHoldSeconds = 1.15f;

        private readonly CarryRoundupMissionState _state = new();
        private MissionContext _context;
        private Vector2[] _spots;
        private GameObject[] _looseMarkers;
        private GameObject[] _carriedMarkers;
        private bool[] _dogCarrying;
        private GameObject _bowl;
        private Vector2 _bowlPosition;
        private bool _jumboInTransit;
        private float _jumboSeparatedFor;
        private bool _jumboRoleCueShown;
        private bool _jumboSteadyCueShown;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.WeenieRoundup;
        public bool IsComplete => _state.ReadyToClear(RequiredDeliveries) && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _state.ReadyToClear(RequiredDeliveries) && _successHoldRemaining > 0f;
        public bool IsFailed => false;
        public string FailReason => null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildCarrySummary(_state, RequiredDeliveries);
        public Vector2 EntryTarget => _spots != null && _spots.Length > 0 ? _spots[0] : Vector2.zero;
        public CarryRoundupMissionState State => _state;
        public Vector2 BowlPosition => _bowlPosition;
        public string ObjectiveLabel => IsPresentingSuccessfulOutcome
            ? "Bowl full! Cocoa steadied the jumbo and Cheddar brought it home."
            : _jumboInTransit
                ? $"JUMBO HAUL: Cocoa stay beside Cheddar all the way to the HOME BOWL ({_state.Delivered}/{RequiredDeliveries})"
            : IsAnyDogCarrying()
            ? $"Carry the weenie to the HOME BOWL ({_state.Delivered}/{RequiredDeliveries} delivered)"
            : _state.Delivered >= RequiredDeliveries - 1
                ? "FINAL JUMBO: Cocoa steady it, then Cheddar grab it and haul together!"
                : $"Round up the scattered weenies: {_state.Delivered}/{RequiredDeliveries} delivered, {_state.Loose} loose";

        public static Vector2[] ComputeSpots(Rect bounds)
        {
            Vector2 P(float x, float y) => new(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            return new[] { P(-0.72f, 0.62f), P(0.7f, 0.68f), P(-0.82f, -0.6f), P(0.22f, -0.7f), P(0.82f, -0.18f) };
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _spots = ComputeSpots(_context.Bounds);
            BuildActors();
        }

        public void StartMission()
        {
            _state.Configure(RequiredDeliveries);
            _bowlPosition = new Vector2(_context.Bounds.xMax - 4f, _context.Bounds.yMin + 3f);
            _bowl.transform.position = _bowlPosition;
            _bowl.SetActive(true);
            SetProp(_bowl, FinalGameplayArt.WeenieRoundupBowlEmpty);
            _context.SetActorState(_bowl, $"HOME BOWL 0/{RequiredDeliveries}", new Color(0.85f, 0.85f, 0.9f), 0.1f);
            for (int i = 0; i < _looseMarkers.Length; i++)
            {
                _looseMarkers[i].transform.position = _spots[i];
                SetProp(_looseMarkers[i], FinalGameplayArt.WeenieRoundupLoose);
                _looseMarkers[i].SetActive(true);
                if (i == JumboMarkerIndex)
                    _context.SetActorState(_looseMarkers[i], "JUMBO WEENIE - DELIVER 4 SMALL ONES FIRST", new Color(0.8f, 0.58f, 0.3f), 0.08f);
            }
            for (int i = 0; i < _dogCarrying.Length; i++)
            {
                _dogCarrying[i] = false;
                _carriedMarkers[i].transform.localScale = new Vector3(0.9f, 0.4f, 1f);
                _carriedMarkers[i].SetActive(false);
                if (i < _context.DogFeedback.Length && _context.DogFeedback[i] != null)
                    _context.DogFeedback[i].SetCarrying(false);
            }
            _jumboInTransit = false;
            _jumboSeparatedFor = 0f;
            _jumboRoleCueShown = false;
            _jumboSteadyCueShown = false;
            _successHoldRemaining = 0f;
        }

        public void Tick(float deltaTime, float now)
        {
            if (_state.ReadyToClear(RequiredDeliveries))
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }

            if (_jumboInTransit)
            {
                int cheddar = _context.IndexOfDog(DogId.Cheddar);
                int cocoa = _context.IndexOfDog(DogId.Cocoa);
                if (cheddar >= 0 && cocoa >= 0)
                {
                    bool together = Vector2.Distance(_context.Dogs[cheddar].transform.position,
                        _context.Dogs[cocoa].transform.position) <= JumboSteadyRange;
                    _jumboSeparatedFor = together ? 0f : _jumboSeparatedFor + deltaTime;
                    if (_jumboSeparatedFor >= JumboSeparationGraceSeconds)
                    {
                        Drop(cheddar);
                        return;
                    }
                }
            }

            for (int i = 0; i < _context.Dogs.Length; i++)
            {
                if (_dogCarrying[i])
                {
                    _carriedMarkers[i].transform.position = _context.Dogs[i].transform.position + Vector3.up * 0.6f;
                    if (Vector2.Distance(_context.Dogs[i].transform.position, _bowlPosition) <= BowlRange)
                        Deliver(i, false);
                }
                else
                {
                    int marker = NearestLoose(_context.Dogs[i].transform.position);
                    if (marker >= 0 && Vector2.Distance(_context.Dogs[i].transform.position, _looseMarkers[marker].transform.position) <= PickupRange)
                        Pickup(i, marker, false);
                }
            }
        }

        public bool HandleBark(int dogIndex) => false;

        public void Cleanup()
        {
            if (_bowl != null) _bowl.SetActive(false);
            if (_looseMarkers != null)
                foreach (var marker in _looseMarkers) if (marker != null) marker.SetActive(false);
            if (_carriedMarkers != null)
                for (int i = 0; i < _carriedMarkers.Length; i++)
                {
                    if (_carriedMarkers[i] != null) _carriedMarkers[i].SetActive(false);
                    if (i < _context.DogFeedback.Length && _context.DogFeedback[i] != null)
                        _context.DogFeedback[i].SetCarrying(false);
                }
        }

        public void StageDogsForEntry()
        {
            if (_context.Dogs == null || _context.Dogs.Length < 2) return;
            Vector2 inward = _context.Bounds.center - EntryTarget;
            inward = inward.sqrMagnitude < 0.01f ? Vector2.down : inward.normalized;
            Vector2 center = EntryTarget + inward * 7f;
            Vector2 side = new Vector2(-inward.y, inward.x) * 1.5f;
            _context.Dogs[0].transform.position = center - side;
            _context.Dogs[1].transform.position = center + side;
            foreach (var dog in _context.Dogs)
                if (dog != null && dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            if (dogIndex >= 0 && dogIndex < _dogCarrying.Length && _dogCarrying[dogIndex])
            {
                target = _bowl.transform;
                copy = "HOME BOWL";
            }
            else if (_jumboInTransit && _context.IndexOfDog(DogId.Cocoa) == dogIndex)
            {
                int cheddar = _context.IndexOfDog(DogId.Cheddar);
                target = cheddar >= 0 ? _context.Dogs[cheddar].transform : _bowl.transform;
                copy = "STEADY CHEDDAR";
            }
            else
            {
                int nearest = dogIndex >= 0 && dogIndex < _context.Dogs.Length
                    ? NearestLoose(_context.Dogs[dogIndex].transform.position)
                    : -1;
                target = nearest >= 0 ? _looseMarkers[nearest].transform : null;
                copy = nearest == JumboMarkerIndex
                    ? (_context.IndexOfDog(DogId.Cocoa) == dogIndex ? "STEADY JUMBO" : "GRAB JUMBO")
                    : "PICK UP WEENIE";
            }
            hideDistance = 1.4f;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("weenie_roundup", score, timeRemaining, _state.Delivered, RequiredDeliveries, _state.Drops,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForcePickup(DogId dogId) => Pickup(_context.IndexOfDog(dogId), FirstLoose(), true);
        public void ForceDeliver(DogId dogId) => Deliver(_context.IndexOfDog(dogId), true);
        public void ForceDrop(DogId dogId) => Drop(_context.IndexOfDog(dogId));
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        private void Pickup(int dogIndex, int markerIndex, bool force)
        {
            if (dogIndex < 0 || dogIndex >= _dogCarrying.Length || _dogCarrying[dogIndex]) return;
            if (markerIndex < 0 || markerIndex >= _looseMarkers.Length || !_looseMarkers[markerIndex].activeSelf) return;
            bool isJumbo = markerIndex == JumboMarkerIndex;
            if (isJumbo)
            {
                if (_state.Delivered < RequiredDeliveries - 1) return;
                int cheddar = _context.IndexOfDog(DogId.Cheddar);
                int cocoa = _context.IndexOfDog(DogId.Cocoa);
                if (dogIndex != cheddar)
                {
                    if (!force && !_jumboRoleCueShown)
                    {
                        _jumboRoleCueShown = true;
                        _context.MarkFailedInteraction(DogId.Cocoa, "Cocoa steadies the jumbo; Cheddar grabs and carries it");
                        _context.SetCue("Cocoa has the jumbo steady - Cheddar, come grab it!");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CHEDDAR: GRAB IT!");
                        _context.SpawnWorldPop(_looseMarkers[markerIndex].transform.position + Vector3.up, "CHEDDAR'S JOB", new Color(1f, 0.72f, 0.25f));
                    }
                    return;
                }
                bool cocoaSteady = cocoa >= 0 && Vector2.Distance(_context.Dogs[cocoa].transform.position,
                    _looseMarkers[markerIndex].transform.position) <= JumboSteadyRange;
                if (!force && !cocoaSteady)
                {
                    if (!_jumboSteadyCueShown)
                    {
                        _jumboSteadyCueShown = true;
                        _context.MarkFailedInteraction(DogId.Cheddar, "Cocoa must stand beside the jumbo before Cheddar grabs it");
                        _context.SetCue("That jumbo is too wobbly for Cheddar alone - Cocoa, get beside it and steady!");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "NEED COCOA STEADY!");
                        _context.SpawnWorldPop(_looseMarkers[markerIndex].transform.position + Vector3.up, "TOO WOBBLY!", new Color(1f, 0.72f, 0.25f));
                    }
                    return;
                }
            }
            if (!_state.TryPickup()) return;
            _looseMarkers[markerIndex].SetActive(false);
            _dogCarrying[dogIndex] = true;
            _jumboInTransit = isJumbo;
            _jumboSeparatedFor = 0f;
            _jumboRoleCueShown = false;
            _jumboSteadyCueShown = false;
            SetProp(_carriedMarkers[dogIndex], FinalGameplayArt.WeenieRoundupCarried);
            _carriedMarkers[dogIndex].transform.localScale = isJumbo
                ? new Vector3(1.35f, 0.6f, 1f)
                : new Vector3(0.9f, 0.4f, 1f);
            _carriedMarkers[dogIndex].SetActive(true);
            if (_context.DogFeedback[dogIndex] != null)
            {
                _context.DogFeedback[dogIndex].SetCarrying(true);
                _context.DogFeedback[dogIndex].ShowProudBrief();
            }
            _context.CreditDog(dogIndex);
            if (isJumbo)
            {
                int cocoa = _context.IndexOfDog(DogId.Cocoa);
                if (cocoa >= 0)
                {
                    _context.CreditDog(cocoa);
                    if (_context.DogFeedback[cocoa] != null) _context.DogFeedback[cocoa].ShowProudBrief();
                }
                _context.SignalRoleHandoff(DogId.Cocoa, DogId.Cheddar);
            }
            _context.AddScore(ScoreEventCatalog.WeeniePickup.Points, ScoreEventCatalog.WeeniePickup.Label);
            _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
            _context.SetCue(isJumbo
                ? "Cocoa steadied the jumbo - Cheddar grabbed it! Stay together to the bowl!"
                : $"{DogName(dogIndex)} grabbed a weenie - carry it to the bowl!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.WeeniePickup.Label);
            _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position,
                isJumbo ? "TEAM JUMBO!" : "WEENIE GRABBED!", new Color(1f, 0.78f, 0.3f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            _context.RequestRumble("weenie_pickup", 0.12f, 0.22f, 0.1f);
            _context.LogEvent("WeeniePickup", $"loose {_state.Loose}");
            _context.LogObjectiveChanged();
            UpdateBowlState();
        }

        private void Deliver(int dogIndex, bool force)
        {
            if (dogIndex < 0 || dogIndex >= _dogCarrying.Length || !_dogCarrying[dogIndex]) return;
            if (_jumboInTransit && !force)
            {
                int cocoa = _context.IndexOfDog(DogId.Cocoa);
                bool cocoaAtBowl = cocoa >= 0
                    && Vector2.Distance(_context.Dogs[cocoa].transform.position, _bowlPosition) <= BowlRange
                    && Vector2.Distance(_context.Dogs[cocoa].transform.position,
                        _context.Dogs[dogIndex].transform.position) <= JumboSteadyRange;
                if (!cocoaAtBowl) return;
            }
            bool deliveredJumbo = _jumboInTransit;
            _dogCarrying[dogIndex] = false;
            _jumboInTransit = false;
            _jumboSeparatedFor = 0f;
            _carriedMarkers[dogIndex].SetActive(false);
            if (_context.DogFeedback[dogIndex] != null)
            {
                _context.DogFeedback[dogIndex].SetCarrying(false);
                _context.DogFeedback[dogIndex].ShowProudBrief();
            }
            _state.Deliver();
            if (_state.Delivered == RequiredDeliveries - 1)
            {
                _context.SetActorState(_looseMarkers[JumboMarkerIndex],
                    "JUMBO READY - COCOA STEADY + CHEDDAR GRAB", new Color(1f, 0.72f, 0.28f), 0.24f);
                _context.SpawnWorldPop(_looseMarkers[JumboMarkerIndex].transform.position,
                    "JUMBO READY!", new Color(1f, 0.72f, 0.28f));
            }
            SetProp(_bowl, _state.Delivered >= RequiredDeliveries
                ? FinalGameplayArt.WeenieRoundupBowlFull
                : FinalGameplayArt.WeenieRoundupBowlProgress);
            _context.CreditDog(dogIndex);
            _context.AddScore(ScoreEventCatalog.WeenieDelivered.Points, ScoreEventCatalog.WeenieDelivered.Label);
            _context.SetFeedback(GameManager.FeedbackKind.LevelClear);
            _context.SetCue(_state.Delivered == RequiredDeliveries - 1
                ? "Four little weenies are home - now team up for the JUMBO!"
                : deliveredJumbo
                    ? "Cocoa steadied it, Cheddar hauled it - the JUMBO is home!"
                    : $"{DogName(dogIndex)} delivered a weenie to the bowl! ({_state.Delivered}/{RequiredDeliveries})");
            UpdateBowlState();
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.WeenieDelivered.Label);
            _context.SpawnWorldPop(_bowlPosition, deliveredJumbo ? "JUMBO HOME!" : "DELIVERED!", new Color(0.55f, 1f, 0.45f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("weenie_delivered", 0.24f, 0.45f, 0.16f);
            _context.LogEvent("WeenieDelivered", $"{_state.Delivered}/{RequiredDeliveries}");
            if (_state.ReadyToClear(RequiredDeliveries))
            {
                _successHoldRemaining = SuccessHoldSeconds;
                _context.AddScore(ScoreEventCatalog.RoundupComplete.Points, ScoreEventCatalog.RoundupComplete.Label);
                _context.SetActorState(_bowl, "HOME BOWL FULL - TEAM JUMBO!", new Color(0.55f, 1f, 0.45f), 0.35f);
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "BOWL FULL!");
                _context.SpawnWorldPop(_bowlPosition + Vector2.up, "BOWL FULL!", new Color(1f, 0.9f, 0.35f));
                // The delivering dog already got ShowProudBrief() above; the finale is a two-dog
                // beat (Cocoa steadied the jumbo too), so re-affirm/extend the pose on both.
                foreach (var feedback in _context.DogFeedback)
                    if (feedback != null) feedback.ShowProudBrief();
                _context.RequestRumble("roundup_complete", 0.38f, 0.62f, 0.24f);
            }
            else _context.LogObjectiveChanged();
        }

        private void Drop(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _dogCarrying.Length || !_dogCarrying[dogIndex]) return;
            _dogCarrying[dogIndex] = false;
            bool droppedJumbo = _jumboInTransit;
            _jumboInTransit = false;
            _jumboSeparatedFor = 0f;
            _carriedMarkers[dogIndex].SetActive(false);
            if (_context.DogFeedback[dogIndex] != null)
            {
                _context.DogFeedback[dogIndex].SetCarrying(false);
                _context.DogFeedback[dogIndex].ShowPanic();
            }
            _state.Drop();
            Vector2 dropAt = (Vector2)_context.Dogs[dogIndex].transform.position + new Vector2(PickupRange + 1f, 0f);
            dropAt.x = Mathf.Clamp(dropAt.x, _context.Bounds.xMin + 1f, _context.Bounds.xMax - 1f);
            int marker = droppedJumbo ? JumboMarkerIndex : FirstInactiveMarker();
            if (marker >= 0)
            {
                _looseMarkers[marker].transform.position = dropAt;
                SetProp(_looseMarkers[marker], FinalGameplayArt.WeenieRoundupDropped);
                _looseMarkers[marker].SetActive(true);
            }
            _context.AddScore(ScoreEventCatalog.WeenieDropped.Points, ScoreEventCatalog.WeenieDropped.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue(droppedJumbo
                ? "The jumbo wobbled loose - Cocoa stay beside Cheddar and try the team haul again!"
                : $"{DogName(dogIndex)} fumbled the weenie - go grab it again!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, ScoreEventCatalog.WeenieDropped.Label);
            _context.SpawnWorldPop(dropAt, "FUMBLE!", new Color(1f, 0.4f, 0.22f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("weenie_fumble", 0.18f, 0.34f, 0.14f);
            _context.LogEvent("WeenieDropped", $"drops {_state.Drops}");
            _context.LogObjectiveChanged();
            UpdateBowlState();
        }

        private void BuildActors()
        {
            _looseMarkers = new GameObject[_spots.Length];
            for (int i = 0; i < _spots.Length; i++)
                _looseMarkers[i] = MakeMarker($"LooseWeenie_{i}",
                    i == JumboMarkerIndex ? new Vector3(1.65f, 0.75f, 1f) : new Vector3(1.1f, 0.5f, 1f),
                    i == JumboMarkerIndex ? new Color(1f, 0.62f, 0.2f) : new Color(0.78f, 0.34f, 0.24f),
                    4, i == JumboMarkerIndex ? "JUMBO WEENIE" : "WEENIE");
            _carriedMarkers = new GameObject[_context.Dogs.Length];
            _dogCarrying = new bool[_context.Dogs.Length];
            for (int i = 0; i < _carriedMarkers.Length; i++) _carriedMarkers[i] = MakeMarker($"CarriedWeenie_{i}", new Vector3(0.9f, 0.4f, 1f), new Color(0.85f, 0.4f, 0.28f), 11, null);
            _bowl = MakeMarker("HomeBowl", new Vector3(3f, 2f, 1f), new Color(0.85f, 0.85f, 0.9f), 2, "HOME BOWL");
        }

        private GameObject MakeMarker(string name, Vector3 scale, Color color, int order, string label)
        {
            var go = new GameObject(name);
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _context.ActorSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (label != null) _context.AddWorldLabel(go, label, Vector3.up * (name == "HomeBowl" ? 1.4f : 1.2f), name == "HomeBowl" ? 16 : 13, Color.white);
            go.AddComponent<MissionActorFeedback>().Init(renderer, label ?? name, 0.1f, Vector3.zero);
            string propPath = name == "HomeBowl"
                ? FinalGameplayArt.WeenieRoundupBowlEmpty
                : name.StartsWith("CarriedWeenie")
                    ? FinalGameplayArt.WeenieRoundupCarried
                    : FinalGameplayArt.WeenieRoundupLoose;
            MissionPropArt.AttachObject(go, propPath, name == "HomeBowl" ? 0.013f : 0.011f, order + 14, name != "HomeBowl");
            go.SetActive(false);
            return go;
        }

        private static void SetProp(GameObject go, string resourcePath)
        {
            if (go == null) return;
            var attachment = go.GetComponent<MissionPropArtAttachment>();
            if (attachment != null) MissionPropArt.SetSprite(attachment, resourcePath);
        }

        private int NearestLoose(Vector2 position)
        {
            int best = -1; float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < _looseMarkers.Length; i++)
            {
                if (!_looseMarkers[i].activeSelf) continue;
                if (i == JumboMarkerIndex && _state.Delivered < RequiredDeliveries - 1) continue;
                float distance = Vector2.Distance(position, _looseMarkers[i].transform.position);
                if (distance < bestDistance) { best = i; bestDistance = distance; }
            }
            return best;
        }

        private int FirstLoose() { for (int i = 0; i < _looseMarkers.Length; i++) if (_looseMarkers[i].activeSelf) return i; return -1; }
        private int FirstInactiveMarker() { for (int i = 0; i < _looseMarkers.Length; i++) if (!_looseMarkers[i].activeSelf) return i; return -1; }
        private bool IsAnyDogCarrying() { foreach (bool carrying in _dogCarrying) if (carrying) return true; return false; }
        private string DogName(int dogIndex) => _context.Dogs[dogIndex] != null ? _context.Dogs[dogIndex].name : "Dog";

        /// <summary>
        /// The bowl rides the actor pulse channel: while a weenie is in transit the delivery is
        /// the act-now objective, so the NOW! state raises the distance badge over home.
        /// </summary>
        private void UpdateBowlState()
        {
            if (_bowl == null) return;
            if (!IsComplete && IsAnyDogCarrying())
                _context.SetActorState(_bowl, $"HOME BOWL {_state.Delivered}/{RequiredDeliveries} - BRING IT NOW!", new Color(1f, 0.85f, 0.35f), 0.3f);
            else if (_state.Delivered > 0)
                _context.SetActorState(_bowl, $"HOME BOWL {_state.Delivered}/{RequiredDeliveries}", new Color(0.6f, 1f, 0.7f), 0.2f);
            else
                _context.SetActorState(_bowl, $"HOME BOWL 0/{RequiredDeliveries}", new Color(0.85f, 0.85f, 0.9f), 0.1f);
        }
    }
}
