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
    public sealed class GreatEscapeMissionController : IMissionController
    {
        private const float StationRange = 3f;
        private const float SettleTime = 7f; // dawdle this long and the contraption eases back a step.
        private const int MaxWasted = 6;     // fumbles + settles before the breakout falls apart.

        private static readonly ChainActor[] Owners = { ChainActor.Cocoa, ChainActor.Cheddar, ChainActor.Cocoa, ChainActor.Cheddar };
        private static readonly Vector2[] Spots = { new(-13f, 7f), new(-5f, -7f), new(6f, 7f), new(14f, -6f) };
        private static readonly string[] Actions = { "PAW THE LATCH", "SHOULDER THE GATE", "DRAG THE COOLER", "SQUEEZE THROUGH" };

        private readonly CoopSequenceChainPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject[] _stations;
        private TextMesh[] _stationLabels;
        private int _dogInside = -1;
        private int _stepSeen;
        private int _fumblesSeen;
        private int _settlesSeen;
        private bool _failed;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.GreatEscape;
        public bool IsComplete => _puzzle.Solved;
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

        public string ObjectiveLabel
        {
            get
            {
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
            _dogInside = -1;
            _stepSeen = 0;
            _fumblesSeen = 0;
            _settlesSeen = 0;
            _failed = false;
            SetSceneActive(true);
            UpdateVisuals();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved || _failed || _stations == null || _context.Dogs == null) return;

            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            int active = Mathf.Clamp(_puzzle.Step, 0, Spots.Length - 1);
            Vector2 station = Spots[active];
            ChainActor owner = _puzzle.NextOwner;

            bool cheddarThere = Vector2.Distance(_context.Dogs[cheddar].transform.position, station) <= StationRange;
            bool cocoaThere = Vector2.Distance(_context.Dogs[cocoa].transform.position, station) <= StationRange;

            // Prefer the owner if present at the active station; a wrong-dog visit registers as a fumble.
            int insideDog = -1;
            ChainActor insideActor = ChainActor.Either;
            if (owner == ChainActor.Cheddar && cheddarThere) { insideDog = cheddar; insideActor = ChainActor.Cheddar; }
            else if (owner == ChainActor.Cocoa && cocoaThere) { insideDog = cocoa; insideActor = ChainActor.Cocoa; }
            else if (cheddarThere) { insideDog = cheddar; insideActor = ChainActor.Cheddar; }
            else if (cocoaThere) { insideDog = cocoa; insideActor = ChainActor.Cocoa; }

            if (insideDog >= 0 && insideDog != _dogInside) _puzzle.TryStep(insideActor);
            _dogInside = insideDog;

            _puzzle.Advance(deltaTime);

            HandleProgress();
            if (_failed) return;
            UpdateVisuals();
        }

        public bool HandleBark(int dogIndex) => false;

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
            copy = isOwner ? "YOUR STEP" : "LET PARTNER GO";
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

        private void HandleProgress()
        {
            if (_puzzle.Step > _stepSeen)
            {
                _stepSeen = _puzzle.Step;
                _context.AddScore(ScoreEventCatalog.ContraptionStep.Points, ScoreEventCatalog.ContraptionStep.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"Clunk! The contraption advanced. ({_puzzle.Step}/{_puzzle.StepCount})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "CLUNK!");
                int doneStep = Mathf.Clamp(_puzzle.Step - 1, 0, Spots.Length - 1);
                _context.SpawnWorldPop(Spots[doneStep], "CLUNK!", new Color(0.6f, 0.85f, 0.95f));
                _context.LogEvent("EscapeStep", $"{_puzzle.Step}/{_puzzle.StepCount}");
            }

            bool wasted = false;
            if (_puzzle.Fumbles > _fumblesSeen)
            {
                _fumblesSeen = _puzzle.Fumbles;
                wasted = true;
                _context.SetCue("Wrong dog or wrong order - nothing budged.");
            }
            if (_puzzle.Settles > _settlesSeen)
            {
                _settlesSeen = _puzzle.Settles;
                wasted = true;
                _context.SetCue("Too slow - the contraption eased back a step. Keep pace!");
            }
            if (wasted)
            {
                _context.AddScore(ScoreEventCatalog.ContraptionFumble.Points, ScoreEventCatalog.ContraptionFumble.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CLANK!");
                int totalWasted = _puzzle.Fumbles + _puzzle.Settles;
                _context.LogEvent("EscapeWaste", $"{totalWasted}/{MaxWasted}");
                if (totalWasted >= MaxWasted) _failed = true;
            }
        }

        private void BuildScene()
        {
            _stations = new GameObject[Spots.Length];
            _stationLabels = new TextMesh[Spots.Length];
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
                if (_stationLabels != null && _stationLabels[i] != null)
                {
                    string who = owner == ChainActor.Cheddar ? "CHEDDAR" : "COCOA";
                    _stationLabels[i].text = done ? "DONE" : $"{i + 1}. {who}: {Actions[i]}";
                }
            }
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin) => new(
            Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
            Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
    }
}
