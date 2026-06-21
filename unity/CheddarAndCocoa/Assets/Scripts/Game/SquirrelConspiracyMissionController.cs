using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class SquirrelConspiracyMissionController : IMissionController, IMissionInteractionController
    {
        private const int RequiredControls = 4;
        private const int MaxTaunts = 3;
        private const float CutoffRadius = 3f;
        private const float StashInteractRange = 2f;
        private const float RouteResetSeconds = 5.5f;

        private readonly HerdingMissionState _state = new();
        private MissionContext _context;
        private Vector2[] _route;
        private Vector2[] _cutoffZones;
        private GameObject[] _cutoffMarkers;
        private Vector2 _stashPosition;
        private float _routeTimer;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.SquirrelConspiracy;
        public bool IsComplete => _state.StashFound;
        public bool IsFailed => _state.TooManyTaunts(MaxTaunts);
        public string FailReason => IsFailed
            ? "The squirrel taunted the dogs into a full backyard misinformation spiral."
            : null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildSquirrelSummary(_state);
        public string ObjectiveLabel => _state.StashRevealed
            ? "Sniff the revealed stash and interact"
            : $"Herd squirrel route {_state.RouteIndex + 1}/{RequiredControls}: controls {_state.ControlCount}/{RequiredControls}, taunts {_state.Taunts}/{MaxTaunts}";
        public Vector2 EntryTarget => _route != null && _route.Length > 0 ? _route[0] : Vector2.zero;
        public HerdingMissionState State => _state;
        public Vector2[] RouteNodes => _route != null ? (Vector2[])_route.Clone() : System.Array.Empty<Vector2>();
        public Vector2[] CutoffZones => _cutoffZones != null ? (Vector2[])_cutoffZones.Clone() : System.Array.Empty<Vector2>();
        public Vector2 ActiveCutoffZone => _cutoffZones[Mathf.Clamp(_state.RouteIndex, 0, _cutoffZones.Length - 1)];

        public static Vector2[] ComputeRoute(Rect bounds)
        {
            Vector2 P(float x, float y) => new(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            return new[] { P(-0.78f, 0.68f), P(0f, -0.66f), P(0.78f, 0.68f), P(0.64f, -0.62f) };
        }

        public static Vector2[] ComputeCutoffZones(Rect bounds)
        {
            Vector2 P(float x, float y) => new(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            return new[] { P(-0.36f, -0.12f), P(0.36f, -0.12f), P(0.72f, 0.08f), P(-0.12f, 0.12f) };
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _route = ComputeRoute(context.Bounds);
            _cutoffZones = ComputeCutoffZones(context.Bounds);
            _stashPosition = new Vector2(context.Bounds.xMax - 1.7f, context.Bounds.yMin + 1.7f);
            BuildCutoffMarkers();
        }

        public void StartMission()
        {
            _state.Reset();
            _routeTimer = RouteResetSeconds;
            _context.SquirrelObject.transform.position = _route[0];
            _context.SquirrelObject.SetActive(true);
            _context.SetActorState(_context.SquirrelObject, "SQUIRREL CONSPIRACY ROUTE 1", new Color(0.55f, 0.32f, 0.12f), 0.06f);
            UpdateCutoffMarkers();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_context.SquirrelObject == null) return;
            _routeTimer -= deltaTime;
            Vector2 target = _state.StashRevealed ? _stashPosition : _route[_state.RouteIndex];
            _context.SquirrelObject.transform.position = Vector3.MoveTowards(
                _context.SquirrelObject.transform.position,
                target,
                deltaTime * (_context.SquirrelMoveSpeed * 0.65f));
            if (!_state.StashRevealed && _routeTimer <= 0f) RegisterTaunt();
        }

        public bool HandleBark(int dogIndex)
        {
            if (dogIndex < 0 || dogIndex >= _context.Dogs.Length || _state.StashFound) return false;
            float distance = Vector2.Distance(_context.Dogs[dogIndex].transform.position, _context.SquirrelObject.transform.position);
            if (distance > _context.SingleBarkSquirrelRange)
            {
                _state.AddFakeOut();
                _context.AddScore(ScoreEventCatalog.FakeOut.Points, ScoreEventCatalog.FakeOut.Label);
                _context.SetCue("The squirrel sold a fake-out and the dogs barked at absolutely nothing.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "FAKE OUT!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position, "FAKE OUT!", new Color(1f, 0.42f, 0.24f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
                _context.LogEvent("SquirrelFakeOut", "The dogs barked at absolutely nothing.");
                return false;
            }

            bool cutoff = IsPartnerHoldingCutoff(dogIndex);
            var scoreEvent = cutoff ? ScoreEventCatalog.Cutoff : ScoreEventCatalog.GoodHerd;
            if (cutoff) _state.AddCutoff(); else _state.AddHerd();
            _state.AdvanceRoute(_route.Length);
            UpdateCutoffMarkers();
            _routeTimer = RouteResetSeconds;
            _context.CreditDog(dogIndex);
            _context.AddScore(scoreEvent.Points, scoreEvent.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue(cutoff ? "Perfect cutoff! The squirrel route is collapsing." : "Good herd! The squirrel conspiracy is losing ground.");
            _context.SetActorState(_context.SquirrelObject, $"ROUTE {_state.RouteIndex + 1} / CONTROLS {_state.ControlCount}/{RequiredControls}", new Color(0.85f, 0.55f, 0.12f), cutoff ? 0.28f : 0.16f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, scoreEvent.Label);
            _context.SpawnWorldPop(_context.SquirrelObject.transform.position, cutoff ? "CUTOFF!" : "HERD!", new Color(1f, 0.9f, 0.25f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.LogEvent(cutoff ? "SquirrelCutoff" : "SquirrelHerd", $"controls {_state.ControlCount}/{RequiredControls}");

            if (_state.ReadyForStash(RequiredControls))
            {
                _state.RevealStash();
                HideCutoffMarkers();
                _context.AddScore(ScoreEventCatalog.DoubleBarkBlock.Points, ScoreEventCatalog.DoubleBarkBlock.Label);
                _context.SquirrelObject.transform.position = _stashPosition + Vector2.left * 1.2f;
                _context.SetActorState(_context.SquirrelObject, "STASH REVEALED - SNIFF + INTERACT!", new Color(1f, 0.72f, 0.18f), 0.34f);
                _context.SetCue("The squirrel stash is exposed! Get a dog to the stash and interact.");
                _context.LogEvent("SquirrelStashRevealed", "The squirrel stash is exposed.");
            }

            _context.LogObjectiveChanged();
            return true;
        }

        public bool HandleInteract(int dogIndex) => TryFindStash(dogIndex, false);

        public void Cleanup()
        {
            HideCutoffMarkers();
            if (_context?.SquirrelObject != null) _context.SquirrelObject.SetActive(false);
        }

        public void StageDogsForEntry()
        {
            if (_context.Dogs == null || _context.Dogs.Length < 2) return;
            Vector2 inward = (_context.Bounds.center - EntryTarget).normalized;
            if (inward.sqrMagnitude < 0.01f) inward = Vector2.down;
            Vector2 center = EntryTarget + inward * 7f;
            Vector2 side = new Vector2(-inward.y, inward.x) * 1.5f;
            _context.Dogs[0].transform.position = center - side;
            _context.Dogs[1].transform.position = center + side;
            foreach (var dog in _context.Dogs)
                if (dog != null && dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            hideDistance = 1.4f;
            if (_state.StashRevealed)
            {
                target = _context.SquirrelObject.transform;
                copy = "CRACK STASH";
                return true;
            }

            int herder = ClosestDogIndex(_context.SquirrelObject.transform.position);
            if (dogIndex == herder)
            {
                target = _context.SquirrelObject.transform;
                copy = "BARK HERD";
            }
            else
            {
                target = _cutoffMarkers[Mathf.Clamp(_state.RouteIndex, 0, _cutoffMarkers.Length - 1)].transform;
                copy = "HOLD CUTOFF";
                hideDistance = CutoffRadius;
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("squirrel_conspiracy", score, timeRemaining, _state.ControlCount + (_state.StashFound ? 1 : 0),
                RequiredControls + 1, _state.FakeOuts + _state.Taunts,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        public void ForceHerd(DogId dogId) => HandleBark(_context.IndexOfDog(dogId));
        public void ForceTaunt() => RegisterTaunt();
        public void ForceFindStash(DogId dogId) => TryFindStash(_context.IndexOfDog(dogId), true);

        private bool TryFindStash(int dogIndex, bool force)
        {
            if (dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;
            DogId dogId = _context.Dogs[dogIndex].GetComponent<DogIdentity>().Id;
            if (!_state.StashRevealed)
            {
                _context.MarkFailedInteraction(dogId, "stash is not revealed yet");
                return true;
            }
            if (!force && Vector2.Distance(_context.Dogs[dogIndex].transform.position, _stashPosition) > StashInteractRange)
            {
                _context.MarkFailedInteraction(dogId, "too far from squirrel stash");
                return true;
            }

            _state.FindStash();
            _context.CreditDog(dogIndex);
            _context.AddScore(ScoreEventCatalog.StashFound.Points, ScoreEventCatalog.StashFound.Label);
            _context.AddScore(ScoreEventCatalog.ConspiracyCracked.Points, ScoreEventCatalog.ConspiracyCracked.Label);
            _context.SetCue($"{_context.Dogs[dogIndex].name} found the stash. The conspiracy is cracked!");
            _context.SetActorState(_context.SquirrelObject, "CONSPIRACY CRACKED!", new Color(0.3f, 1f, 0.35f), 0.12f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "STASH FOUND!");
            _context.SpawnWorldPop(_stashPosition, "STASH FOUND!", new Color(0.5f, 1f, 0.45f));
            _context.LogEvent("SquirrelStashFound", "The conspiracy is cracked.");
            return true;
        }

        private void RegisterTaunt()
        {
            if (_state.StashRevealed || IsFailed) return;
            _state.AddTaunt();
            _context.AddScore(ScoreEventCatalog.FakeOut.Points, "SQUIRREL TAUNT");
            _state.AdvanceRoute(_route.Length);
            UpdateCutoffMarkers();
            _routeTimer = RouteResetSeconds;
            _context.SquirrelObject.transform.position = _route[_state.RouteIndex];
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"The squirrel taunted the dogs ({_state.Taunts}/{MaxTaunts}). Cut it off before yard gossip wins.");
            _context.SetActorState(_context.SquirrelObject, $"TAUNT {_state.Taunts}/{MaxTaunts} - CUT OFF!", Color.gray, 0.3f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SQUIRREL TAUNT!");
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            _context.LogEvent("SquirrelTaunt", $"taunts {_state.Taunts}/{MaxTaunts}");
            _context.LogObjectiveChanged();
        }

        private void BuildCutoffMarkers()
        {
            _cutoffMarkers = new GameObject[_cutoffZones.Length];
            for (int i = 0; i < _cutoffZones.Length; i++)
            {
                var marker = new GameObject($"SquirrelCutoff_{i}");
                marker.transform.position = _cutoffZones[i];
                marker.transform.localScale = Vector3.one * (CutoffRadius * 1.25f);
                var renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = _context.RangeSprite != null ? _context.RangeSprite : _context.ActorSprite;
                renderer.color = new Color(1f, 0.72f, 0.18f, 0.38f);
                renderer.sortingOrder = 1;
                _context.AddWorldLabel(marker, "HOLD CUTOFF", Vector3.up * 0.38f, 14, Color.white);
                marker.SetActive(false);
                _cutoffMarkers[i] = marker;
            }
        }

        private void UpdateCutoffMarkers()
        {
            for (int i = 0; i < _cutoffMarkers.Length; i++)
                _cutoffMarkers[i].SetActive(!_state.StashRevealed && i == _state.RouteIndex);
        }

        private void HideCutoffMarkers()
        {
            if (_cutoffMarkers == null) return;
            foreach (var marker in _cutoffMarkers) if (marker != null) marker.SetActive(false);
        }

        private bool IsPartnerHoldingCutoff(int barkingDogIndex)
        {
            for (int i = 0; i < _context.Dogs.Length; i++)
                if (i != barkingDogIndex && _context.Dogs[i] != null &&
                    Vector2.Distance(_context.Dogs[i].transform.position, ActiveCutoffZone) <= CutoffRadius)
                    return true;
            return false;
        }

        private int ClosestDogIndex(Vector2 position)
        {
            int best = 0;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < _context.Dogs.Length; i++)
            {
                if (_context.Dogs[i] == null) continue;
                float distance = Vector2.Distance(_context.Dogs[i].transform.position, position);
                if (distance < bestDistance) { best = i; bestDistance = distance; }
            }
            return best;
        }
    }
}
