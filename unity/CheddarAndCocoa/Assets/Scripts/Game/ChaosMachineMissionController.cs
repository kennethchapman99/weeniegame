using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Rube-Goldberg co-op puzzle. The dogs pre-position at their junctions and pull
    /// the lever; the cascade runs itself — but each junction has a brief assist window where its owner
    /// must be in position or the machine misfires and jams. A re-pull resumes from the jam; too many
    /// misfires fail the mission.
    /// </summary>
    public sealed class ChaosMachineMissionController : IMissionController
    {
        private const float LeverRangeVal = 3f;
        private const float JunctionRange = 3f;
        private const float WindowPerStage = 3f;
        private const int MaxStalls = 4;

        private static readonly ChainActor[] Owners = { ChainActor.Cocoa, ChainActor.Cheddar, ChainActor.Cocoa };
        private static readonly Vector2[] JunctionSpots = { new(-4f, 7f), new(6f, -7f), new(14f, 7f) };
        private static readonly string[] Actions = { "TOWEL DROP", "BASKET TIP", "TOY LAUNCH" };
        private static readonly Vector2 LeverPos = new(-14f, -7f);

        private readonly CoopChaosMachinePuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _lever;
        private TextMesh _leverLabel;
        private GameObject[] _junctions;
        private TextMesh[] _junctionLabels;
        private int _stageSeen;
        private int _stallsSeen;
        private bool _failed;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.ChaosMachine;
        public bool IsComplete => _puzzle.Solved;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "The machine misfired too many times - the cascade never made it to the end."
            : null;
        public CoopChaosMachinePuzzle Puzzle => _puzzle;
        public Vector2 LeverZone => LeverPos;
        public int JunctionCount => JunctionSpots.Length;
        public Vector2 JunctionSpot(int index) => index >= 0 && index < JunctionSpots.Length ? JunctionSpots[index] : Vector2.zero;
        public ChainActor JunctionOwner(int index) => index >= 0 && index < Owners.Length ? Owners[index] : ChainActor.Either;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildChaosMachineSummary(_puzzle);

        public string ObjectiveLabel
        {
            get
            {
                if (!_puzzle.Running)
                    return $"Pre-position at your junctions, then pull the lever to start the cascade (junctions {_puzzle.Stage}/{_puzzle.StageCount}, misfires {_puzzle.Stalls}/{MaxStalls})";
                int stage = Mathf.Clamp(_puzzle.Stage, 0, Actions.Length - 1);
                string who = Owners[stage] == ChainActor.Cheddar ? "Cheddar" : "Cocoa";
                return $"{who}: be at the {Actions[stage].ToLowerInvariant()} junction NOW - the cascade's rolling! (junctions {_puzzle.Stage}/{_puzzle.StageCount}, misfires {_puzzle.Stalls}/{MaxStalls})";
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
            _puzzle.Configure(JunctionSpots.Length, WindowPerStage);
            _stageSeen = 0;
            _stallsSeen = 0;
            _failed = false;
            SetSceneActive(true);
            UpdateVisuals();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null) return;

            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            if (!_puzzle.Running)
            {
                bool atLever = Vector2.Distance(_context.Dogs[cheddar].transform.position, LeverPos) <= LeverRangeVal
                    || Vector2.Distance(_context.Dogs[cocoa].transform.position, LeverPos) <= LeverRangeVal;
                if (atLever) _puzzle.Trigger();
            }

            if (_puzzle.Running)
            {
                int stage = Mathf.Clamp(_puzzle.Stage, 0, JunctionSpots.Length - 1);
                ChainActor owner = Owners[stage];
                int ownerIdx = owner == ChainActor.Cheddar ? cheddar : cocoa;
                bool assisting = Vector2.Distance(_context.Dogs[ownerIdx].transform.position, JunctionSpots[stage]) <= JunctionRange;
                _puzzle.Advance(deltaTime, assisting);
            }

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
                Vector2 pos = ClampInsideBounds(center + offset, 1.5f);
                _context.Dogs[i].transform.position = pos;
                if (_context.Dogs[i].TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            if (!_puzzle.Running)
            {
                target = _lever != null ? _lever.transform : null;
                copy = "PULL LEVER";
                hideDistance = LeverRangeVal;
            }
            else
            {
                int stage = Mathf.Clamp(_puzzle.Stage, 0, JunctionSpots.Length - 1);
                target = _junctions != null && _junctions[stage] != null ? _junctions[stage].transform : null;
                ChainActor owner = Owners[stage];
                bool isOwner = (owner == ChainActor.Cheddar && _context.IndexOfDog(DogId.Cheddar) == dogIndex)
                    || (owner == ChainActor.Cocoa && _context.IndexOfDog(DogId.Cocoa) == dogIndex);
                copy = isOwner ? "COVER JUNCTION" : "NEXT JUNCTION";
                hideDistance = JunctionRange;
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("chaos_machine", score, timeRemaining, _puzzle.Stage, _puzzle.StageCount, _puzzle.Stalls,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: pull (or re-pull) the lever to start/resume the cascade.</summary>
        public void ForceChaosTrigger()
        {
            _puzzle.Trigger();
            UpdateVisuals();
        }

        /// <summary>Test hook: advance the live cascade, with the current junction's owner in position or not.</summary>
        public void ForceChaosAdvance(float seconds, bool assisting)
        {
            _puzzle.Advance(seconds, assisting);
            HandleProgress();
            if (!_failed) UpdateVisuals();
        }

        private void HandleProgress()
        {
            if (_puzzle.Stage > _stageSeen)
            {
                _stageSeen = _puzzle.Stage;
                _context.AddScore(ScoreEventCatalog.ContraptionStep.Points, "CASCADE ROLLED");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"Whirr-clunk! The cascade rolled through a junction. ({_puzzle.Stage}/{_puzzle.StageCount})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "WHIRR!");
                int doneStage = Mathf.Clamp(_puzzle.Stage - 1, 0, JunctionSpots.Length - 1);
                _context.SpawnWorldPop(JunctionSpots[doneStage], "WHIRR!", new Color(0.6f, 0.85f, 0.95f));
                _context.LogEvent("ChaosStage", $"{_puzzle.Stage}/{_puzzle.StageCount}");
            }

            if (_puzzle.Stalls > _stallsSeen)
            {
                _stallsSeen = _puzzle.Stalls;
                _context.AddScore(ScoreEventCatalog.ContraptionFumble.Points, "MISFIRE");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                int jam = Mathf.Clamp(_puzzle.StalledStage, 0, JunctionSpots.Length - 1);
                _context.SetCue($"Misfire! The machine jammed at the {Actions[jam].ToLowerInvariant()} - re-pull the lever. ({_puzzle.Stalls}/{MaxStalls})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "MISFIRE!");
                _context.SpawnWorldPop(JunctionSpots[jam], "STUCK!", new Color(1f, 0.4f, 0.25f));
                _context.LogEvent("ChaosStall", $"{_puzzle.Stalls}/{MaxStalls}");
                if (_puzzle.Stalls >= MaxStalls) _failed = true;
            }
        }

        private void BuildScene()
        {
            _lever = new GameObject("ChaosMachineLever");
            _lever.transform.position = LeverPos;
            _lever.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            var leverSr = _lever.AddComponent<SpriteRenderer>();
            leverSr.sprite = _context.ActorSprite;
            leverSr.color = new Color(0.85f, 0.55f, 0.3f);
            leverSr.sortingOrder = 3;
            _leverLabel = _context.AddWorldLabel(_lever, "LEVER - PULL TO START THE CASCADE", Vector3.up * 1.3f, 11, Color.white);
            _lever.SetActive(false);

            _junctions = new GameObject[JunctionSpots.Length];
            _junctionLabels = new TextMesh[JunctionSpots.Length];
            for (int i = 0; i < JunctionSpots.Length; i++)
            {
                var go = new GameObject($"ChaosJunction_{i}");
                go.transform.position = JunctionSpots[i];
                go.transform.localScale = new Vector3(1.7f, 1.3f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _context.ActorSprite;
                sr.color = new Color(0.3f, 0.3f, 0.34f);
                sr.sortingOrder = 3;
                _junctionLabels[i] = _context.AddWorldLabel(go, $"{i + 1}.", Vector3.up * 1.3f, 12, Color.white);
                go.SetActive(false);
                _junctions[i] = go;
            }
        }

        private void SetSceneActive(bool active)
        {
            if (_lever != null) _lever.SetActive(active);
            if (_junctions == null) return;
            foreach (var j in _junctions)
                if (j != null) j.SetActive(active);
        }

        private void UpdateVisuals()
        {
            if (_lever != null && _lever.TryGetComponent<SpriteRenderer>(out var leverSr))
            {
                leverSr.color = _puzzle.Running ? new Color(0.5f, 0.85f, 0.55f) : new Color(0.85f, 0.55f, 0.3f);
                if (_leverLabel != null)
                    _leverLabel.text = _puzzle.Running ? "CASCADE RUNNING - COVER YOUR JUNCTIONS!" : "LEVER - PULL TO START THE CASCADE";
            }

            if (_junctions == null) return;
            int active = Mathf.Clamp(_puzzle.Stage, 0, _junctions.Length - 1);
            for (int i = 0; i < _junctions.Length; i++)
            {
                if (_junctions[i] == null) continue;
                bool fired = i < _puzzle.Stage;
                bool isActive = i == active && !_puzzle.Solved;
                bool stalledHere = _puzzle.StalledStage == i;
                ChainActor owner = Owners[i];
                Color ownerTint = owner == ChainActor.Cheddar ? new Color(0.95f, 0.72f, 0.3f) : new Color(0.55f, 0.78f, 1f);
                Color shown = fired ? new Color(0.3f, 0.55f, 0.32f)
                    : stalledHere ? new Color(0.9f, 0.35f, 0.2f)
                    : isActive && _puzzle.Running ? ownerTint
                    : new Color(0.3f, 0.3f, 0.34f);
                if (_junctions[i].TryGetComponent<SpriteRenderer>(out var sr)) sr.color = shown;
                if (_junctionLabels != null && _junctionLabels[i] != null)
                {
                    string who = owner == ChainActor.Cheddar ? "CHEDDAR" : "COCOA";
                    _junctionLabels[i].text = fired ? "FIRED" : $"{who}: {Actions[i]}";
                }
            }
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin) => new(
            Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
            Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
    }
}
