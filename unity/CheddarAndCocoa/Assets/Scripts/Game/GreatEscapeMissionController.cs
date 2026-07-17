using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned sequence-chain co-op puzzle. The dogs run an ordered contraption chain with
    /// alternating owners (Cocoa, Cheddar, Cocoa, Cheddar) so neither can rush it alone. Wrong dog /
    /// wrong order is a harmless fumble; dawdling eases the chain back a step; too many botches fail
    /// the breakout.
    /// </summary>
    public sealed class GreatEscapeMissionController : IMissionController, IMissionInteractionController,
        IMissionSuccessPresentationController
    {
        private const float StationRange = 3f;
        private const float SettleTime = 7f; // dawdle this long and the contraption eases back a step.
        private const int MaxWasted = 6;     // fumbles + settles before the breakout falls apart.
        private const float SuccessHoldSeconds = 1.15f;

        private static readonly ChainActor[] Owners = { ChainActor.Cocoa, ChainActor.Cheddar, ChainActor.Cocoa, ChainActor.Cheddar };
        private static readonly Vector2[] Spots = { new(-13f, 7f), new(-5f, -7f), new(6f, 7f), new(14f, -6f) };
        private static readonly string[] Actions = { "PAW THE LATCH", "SHOULDER THE GATE", "DRAG THE COOLER", "SQUEEZE THROUGH" };

        private readonly CoopSequenceChainPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject[] _stations;
        private TextMesh[] _stationLabels;
        private MissionPropArtAttachment[] _stationArt;
        private string[] _stationOverrideArt;
        private float[] _stationOverrideUntil;
        private int _stepSeen;
        private int _creditedStepsMask;
        private int _fumblesSeen;
        private int _settlesSeen;
        private bool _failed;
        private bool _completionPresented;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.GreatEscape;
        public bool IsComplete => _puzzle.Solved && _successHoldRemaining <= 0f;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "They botched the contraption too many times - the gate never opened and the breakout fizzled."
            : null;
        public CoopSequenceChainPuzzle Puzzle => _puzzle;
        public int StationCount => Spots.Length;
        public Vector2 StationSpot(int index) => index >= 0 && index < Spots.Length ? Spots[index] : Vector2.zero;
        public ChainActor StationOwner(int index) => index >= 0 && index < Owners.Length ? Owners[index] : ChainActor.Either;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildGreatEscapeSummary(_puzzle);
        public bool IsPresentingSuccessfulOutcome => _puzzle.Solved && _successHoldRemaining > 0f;
        public float SuccessHoldRemaining => _successHoldRemaining;

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "BREAKOUT! Cocoa built the opening and Cheddar burst through the final gap!";
                int wasted = _puzzle.Fumbles + _puzzle.Settles;
                int active = Mathf.Clamp(_puzzle.Step, 0, Actions.Length - 1);
                string who = _puzzle.NextOwner == ChainActor.Cheddar ? "Cheddar" : "Cocoa";
                return $"{who}: {Actions[active].ToLowerInvariant()} - it's your turn in the chain (step {_puzzle.Step}/{_puzzle.StepCount}, botched {wasted}/{MaxWasted})";
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
            _puzzle.Configure(Owners, SettleTime);
            _stepSeen = 0;
            _creditedStepsMask = 0;
            _fumblesSeen = 0;
            _settlesSeen = 0;
            _failed = false;
            _completionPresented = false;
            _successHoldRemaining = 0f;
            ClearStationOverrides();
            SetSceneActive(true);
            UpdateVisuals();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_failed || _stations == null || _context.Dogs == null) return;

            if (_puzzle.Solved)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                UpdateVisuals();
                return;
            }

            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            _puzzle.Advance(deltaTime);

            HandleProgress();
            if (_failed) return;
            UpdateVisuals();
        }

        public bool HandleBark(int dogIndex) => false;

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length)
                return false;

            int active = Mathf.Clamp(_puzzle.Step, 0, Spots.Length - 1);
            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, Spots[active]) > StationRange)
            {
                string who = _puzzle.NextOwner == ChainActor.Cheddar ? "Cheddar" : "Cocoa";
                _context.SetCue($"{who} must reach the glowing station and Interact for the next contraption step.");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "INTERACT AT GLOW", new Color(1f, 0.72f, 0.3f));
                return true;
            }

            ChainActor actor = DogIdAt(dogIndex) == DogId.Cheddar ? ChainActor.Cheddar : ChainActor.Cocoa;
            _puzzle.TryStep(actor);
            HandleProgress();
            if (!_failed) UpdateVisuals();
            return true;
        }

        public void Cleanup() => SetSceneActive(false);

        public void StageDogsForEntry()
        {
            Vector2 entry = EntryTarget;
            Vector2 inward = _context.Bounds.center - entry;
            inward = inward.sqrMagnitude < 0.01f ? Vector2.down : inward.normalized;
            Vector2 center = entry + inward * 7f;
            Vector2 side = new Vector2(-inward.y, inward.x) * 1.5f;

            for (int i = 0; i < _context.Dogs.Length; i++)
            {
                Vector2 offset = i % 2 == 0 ? -side : side;
                Vector2 position = ClampInsideBounds(center + offset, 1.5f);
                _context.Dogs[i].transform.position = position;
                if (_context.Dogs[i].TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            int active = Mathf.Clamp(_puzzle.Step, 0, Spots.Length - 1);
            target = _stations != null && _stations[active] != null ? _stations[active].transform : null;
            ChainActor owner = _puzzle.NextOwner;
            bool isOwner = (owner == ChainActor.Cheddar && _context.IndexOfDog(DogId.Cheddar) == dogIndex)
                || (owner == ChainActor.Cocoa && _context.IndexOfDog(DogId.Cocoa) == dogIndex);
            copy = isOwner ? "INTERACT: YOUR STEP" : "LET PARTNER INTERACT";
            hideDistance = StationRange;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("great_escape", score, timeRemaining, _puzzle.Step, _puzzle.StepCount, _puzzle.Fumbles + _puzzle.Settles,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: a dog attempts the next contraption step.</summary>
        public void ForceEscapeStep(ChainActor actor)
        {
            _puzzle.TryStep(actor);
            HandleProgress();
            if (!_failed) UpdateVisuals();
        }

        /// <summary>Test hook: let the contraption sit idle for <paramref name="seconds"/> (dawdle regression).</summary>
        public void ForceEscapeIdle(float seconds)
        {
            _puzzle.Advance(seconds);
            HandleProgress();
            if (!_failed) UpdateVisuals();
        }

        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        private void HandleProgress()
        {
            if (_puzzle.Step > _stepSeen)
            {
                int doneStep = Mathf.Clamp(_puzzle.Step - 1, 0, Spots.Length - 1);
                ChainActor completedBy = Owners[doneStep];
                int stepBit = 1 << doneStep;
                if ((_creditedStepsMask & stepBit) == 0)
                {
                    _creditedStepsMask |= stepBit;
                    int actorIndex = _context.IndexOfDog(completedBy == ChainActor.Cheddar ? DogId.Cheddar : DogId.Cocoa);
                    if (actorIndex >= 0) _context.CreditDog(actorIndex);
                    _context.AddScore(ScoreEventCatalog.ContraptionStep.Points, ScoreEventCatalog.ContraptionStep.Label);
                }
                _stepSeen = _puzzle.Step;
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"{Actions[doneStep]} - CLUNK! The contraption advanced. ({_puzzle.Step}/{_puzzle.StepCount})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "CLUNK!");
                _context.SpawnWorldPop(Spots[doneStep], "CLUNK!", new Color(0.6f, 0.85f, 0.95f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
                _context.RequestRumble("escape_step", 0.1f, 0.24f, 0.1f);
                _context.LogEvent("EscapeStep", $"{_puzzle.Step}/{_puzzle.StepCount}");
            }

            bool wasted = false;
            if (_puzzle.Fumbles > _fumblesSeen)
            {
                _fumblesSeen = _puzzle.Fumbles;
                wasted = true;
                ShowStationReaction(Mathf.Clamp(_puzzle.Step, 0, Spots.Length - 1), FinalGameplayArt.GreatEscapeStationFumble, 0.55f);
                string who = _puzzle.NextOwner == ChainActor.Cheddar ? "Cheddar" : "Cocoa";
                _context.SetCue($"Wrong dog - CLANK! Nothing budged. {who} must Interact at the glowing station.");
                _context.SpawnWorldPop(Spots[Mathf.Clamp(_puzzle.Step, 0, Spots.Length - 1)], "WRONG PAWS!", new Color(1f, 0.5f, 0.25f));
            }
            if (_puzzle.Settles > _settlesSeen)
            {
                _settlesSeen = _puzzle.Settles;
                wasted = true;
                _stepSeen = _puzzle.Step;
                ShowStationReaction(Mathf.Clamp(_puzzle.Step, 0, Spots.Length - 1), FinalGameplayArt.GreatEscapeStationSettle, 0.7f);
                _context.SetCue("Too slow - the contraption eased back a step. Keep pace!");
                _context.SpawnWorldPop(Spots[Mathf.Clamp(_puzzle.Step, 0, Spots.Length - 1)], "SLID BACK!", new Color(1f, 0.62f, 0.28f));
            }
            if (wasted)
            {
                _context.AddScore(ScoreEventCatalog.ContraptionFumble.Points, ScoreEventCatalog.ContraptionFumble.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CLANK!");
                _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                _context.RequestRumble("escape_clank", 0.16f, 0.34f, 0.12f);
                int totalWasted = _puzzle.Fumbles + _puzzle.Settles;
                _context.LogEvent("EscapeWaste", $"{totalWasted}/{MaxWasted}");
                if (totalWasted >= MaxWasted) _failed = true;
            }

            if (_puzzle.Solved && !_completionPresented)
            {
                _completionPresented = true;
                _successHoldRemaining = SuccessHoldSeconds;
                _context.SetCue("BREAKOUT! Cocoa built the opening and Cheddar burst through the final gap!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "GREAT ESCAPE!");
                _context.SpawnWorldPop(Spots[Spots.Length - 1], "FREE DOGS!", new Color(1f, 0.86f, 0.3f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
                _context.RequestRumble("great_escape_payoff", 0.3f, 0.55f, 0.2f);
                _context.LogEvent("GreatEscapePayoff", "Breakout complete; holding live-world success beat");
            }
        }

        private void BuildScene()
        {
            _stations = new GameObject[Spots.Length];
            _stationLabels = new TextMesh[Spots.Length];
            _stationArt = new MissionPropArtAttachment[Spots.Length];
            _stationOverrideArt = new string[Spots.Length];
            _stationOverrideUntil = new float[Spots.Length];
            for (int i = 0; i < Spots.Length; i++)
            {
                var go = new GameObject($"EscapeStation_{i}");
                go.transform.position = Spots[i];
                go.transform.localScale = new Vector3(1.7f, 1.3f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _context.ActorSprite;
                sr.color = new Color(0.3f, 0.3f, 0.34f);
                sr.sortingOrder = 3;
                _stationLabels[i] = _context.AddWorldLabel(go, $"{i + 1}.", Vector3.up * 1.3f, 12, Color.white);
                _stationArt[i] = MissionPropArt.AttachObject(go, FinalGameplayArt.GreatEscapeStationWaiting, 0.012f, 18, true);
                go.SetActive(false);
                _stations[i] = go;
            }
        }

        private void SetSceneActive(bool active)
        {
            if (_stations == null) return;
            for (int i = 0; i < _stations.Length; i++)
                if (_stations[i] != null)
                {
                    _stations[i].transform.position = Spots[i];
                    _stations[i].SetActive(active);
                }
        }

        private void UpdateVisuals()
        {
            if (_stations == null) return;
            int active = Mathf.Clamp(_puzzle.Step, 0, _stations.Length - 1);
            for (int i = 0; i < _stations.Length; i++)
            {
                if (_stations[i] == null) continue;
                bool isActive = i == active && !_puzzle.Solved;
                bool done = i < _puzzle.Step;
                ChainActor owner = Owners[i];
                Color ownerTint = owner == ChainActor.Cheddar ? new Color(0.95f, 0.72f, 0.3f) : new Color(0.55f, 0.78f, 1f);
                Color shown = done ? new Color(0.3f, 0.55f, 0.32f) : (isActive ? ownerTint : new Color(0.3f, 0.3f, 0.34f));
                if (_stations[i].TryGetComponent<SpriteRenderer>(out var sr)) sr.color = shown;
                ActorSignalBadge.SetStationSignal(_stations[i], isActive);
                string spritePath = SelectStationSprite(i, isActive, done, owner);
                MissionPropArt.SetSprite(_stationArt != null && i < _stationArt.Length ? _stationArt[i] : null, spritePath);
                if (_stationLabels != null && _stationLabels[i] != null)
                {
                    string who = owner == ChainActor.Cheddar ? "CHEDDAR" : "COCOA";
                    _stationLabels[i].text = done ? "DONE" : $"{i + 1}. {who}: {Actions[i]}";
                }
            }
        }

        private string SelectStationSprite(int index, bool isActive, bool done, ChainActor owner)
        {
            if (_stationOverrideArt != null && index < _stationOverrideArt.Length
                && !string.IsNullOrEmpty(_stationOverrideArt[index])
                && _context.Now() < _stationOverrideUntil[index])
                return _stationOverrideArt[index];
            if (done) return FinalGameplayArt.GreatEscapeStationCompleted;
            if (!isActive) return FinalGameplayArt.GreatEscapeStationWaiting;
            return owner == ChainActor.Cocoa
                ? FinalGameplayArt.GreatEscapeStationCocoaActive
                : FinalGameplayArt.GreatEscapeStationCheddarActive;
        }

        private void ShowStationReaction(int index, string resourcePath, float seconds)
        {
            if (_stationOverrideArt == null || index < 0 || index >= _stationOverrideArt.Length) return;
            _stationOverrideArt[index] = resourcePath;
            _stationOverrideUntil[index] = _context.Now() + seconds;
        }

        private void ClearStationOverrides()
        {
            if (_stationOverrideArt == null) return;
            for (int i = 0; i < _stationOverrideArt.Length; i++)
            {
                _stationOverrideArt[i] = null;
                _stationOverrideUntil[i] = 0f;
            }
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin) => new(
            Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
            Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));

        private DogId DogIdAt(int dogIndex)
        {
            if (_context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length || _context.Dogs[dogIndex] == null)
                return DogId.Cheddar;
            var identity = _context.Dogs[dogIndex].GetComponent<DogIdentity>();
            return identity != null ? identity.Id : DogId.Cheddar;
        }
    }
}
