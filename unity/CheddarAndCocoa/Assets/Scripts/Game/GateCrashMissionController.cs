using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned hold-and-release co-op puzzle. Cocoa braces the heavy gate open while Cheddar
    /// squeezes through to the toy; if Cocoa lets go mid-squeeze the gate snaps shut, and too many
    /// snaps end the run.
    /// </summary>
    public sealed class GateCrashMissionController : IMissionController
    {
        private const float HoldRange = 4f;
        private const float CrossRange = 4f;
        private const float CrossNeeded = 0.8f;
        private const float HoldWindow = 30f; // generous; snaps come from releasing, not timeout.
        private const int MaxSnaps = 4;

        private static readonly Color GateIdleColor = new(0.7f, 0.5f, 0.2f);
        private static readonly Color GateHeldColor = new(0.4f, 0.8f, 0.5f);

        private readonly CoopHoldReleasePuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _gate;
        private GameObject _toy;
        private MissionPropArtAttachment _gateArt;
        private MissionPropArtAttachment _toyArt;
        private TextMesh _gateLabel;
        private TextMesh _toyLabel;
        private Vector2 _holdZone;
        private Vector2 _crossZone;
        private int _snapsSeen;
        private bool _failed;
        private float _gateSnapReactionUntil;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.GateCrash;
        public bool IsComplete => _puzzle.Solved;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "The gate snapped shut too many times before Cheddar could squeeze through."
            : null;
        public CoopHoldReleasePuzzle Puzzle => _puzzle;
        public Vector2 HoldZone => _holdZone;
        public Vector2 CrossZone => _crossZone;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildGateCrashSummary(_puzzle);

        public string ObjectiveLabel
        {
            get
            {
                int pct = Mathf.RoundToInt(_puzzle.CrossRatio * 100f);
                return _puzzle.Held
                    ? $"Cheddar: squeeze through while Cocoa holds ({pct}%, snaps {_puzzle.Snaps}/{MaxSnaps})"
                    : $"Cocoa: hold the gate open at the marker (squeeze {pct}%, snaps {_puzzle.Snaps}/{MaxSnaps})";
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
            _puzzle.Configure(CrossNeeded, HoldWindow);
            _snapsSeen = 0;
            _failed = false;
            _gateSnapReactionUntil = 0f;
            _holdZone = new Vector2(_context.Bounds.center.x - 10f, _context.Bounds.center.y);
            _crossZone = new Vector2(_context.Bounds.center.x + 10f, _context.Bounds.center.y);
            SetSceneActive(true);
            MissionPropArt.SetSprite(_gateArt, FinalGameplayArt.GateCrashGateClosed);
            MissionPropArt.SetSprite(_toyArt, FinalGameplayArt.GateCrashToyWaiting);
            UpdateLabels();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null) return;

            int anchor = _context.IndexOfDog(DogId.Cocoa);
            int crosser = _context.IndexOfDog(DogId.Cheddar);
            if (anchor < 0 || crosser < 0) return;

            bool held = Vector2.Distance(_context.Dogs[anchor].transform.position, _holdZone) <= HoldRange;
            _puzzle.SetHeld(held);
            if (Vector2.Distance(_context.Dogs[crosser].transform.position, _crossZone) <= CrossRange)
                _puzzle.Advance(deltaTime);

            HandleSnaps();
            if (_failed) return;
            UpdateLabels();
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
            hideDistance = HoldRange;
            if (_context.IndexOfDog(DogId.Cocoa) == dogIndex)
            {
                target = _gate != null ? _gate.transform : null;
                copy = "HOLD GATE";
            }
            else
            {
                target = _toy != null ? _toy.transform : null;
                copy = "SQUEEZE THROUGH";
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("gate_crash", score, timeRemaining, _puzzle.Solved ? 1 : 0, 1, _puzzle.Snaps,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: Cocoa engages or releases the gate brace (releasing mid-squeeze snaps).</summary>
        public void ForceGateHold(bool held)
        {
            _puzzle.SetHeld(held);
            HandleSnaps();
            UpdateLabels();
        }

        /// <summary>Test hook: advance the squeeze by <paramref name="seconds"/> with the gate held.</summary>
        public void ForceGateCross(float seconds)
        {
            _puzzle.Advance(seconds);
            HandleSnaps();
            UpdateLabels();
        }

        private void HandleSnaps()
        {
            if (_puzzle.Snaps <= _snapsSeen) return;

            _snapsSeen = _puzzle.Snaps;
            _gateSnapReactionUntil = _context.Now() + 0.55f;
            MissionPropArt.SetSprite(_gateArt, FinalGameplayArt.GateCrashGateSnap);
            _context.AddScore(ScoreEventCatalog.FakeOut.Points, "GATE SNAP");
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"The gate snapped shut! ({_puzzle.Snaps}/{MaxSnaps}) Cocoa has to brace it.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "GATE SNAP!");
            _context.SpawnWorldPop(_crossZone, "SNAP!", new Color(1f, 0.35f, 0.2f));
            _context.LogEvent("GateSnap", $"{_puzzle.Snaps}/{MaxSnaps}");
            if (_puzzle.Snaps >= MaxSnaps) _failed = true;
        }

        private void BuildScene()
        {
            _gate = NewMarker("GateCrashGate", GateIdleColor, "COCOA: HOLD THE GATE!", new Vector3(1.4f, 4f, 1f), out _gateLabel);
            _toy = NewMarker("GateCrashToy", new Color(0.6f, 0.8f, 1f), "TOY - CHEDDAR SQUEEZE THROUGH!", Vector3.one * 1.2f, out _toyLabel);
            _gateArt = MissionPropArt.AttachObject(_gate, FinalGameplayArt.GateCrashGateClosed, 0.013f, 18, true);
            _toyArt = MissionPropArt.AttachObject(_toy, FinalGameplayArt.GateCrashToyWaiting, 0.012f, 18, true);
        }

        private GameObject NewMarker(string name, Color color, string label, Vector3 scale, out TextMesh worldLabel)
        {
            var marker = new GameObject(name);
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = _context.RangeSprite ?? _context.ActorSprite;
            renderer.color = color;
            renderer.sortingOrder = 3;
            marker.transform.localScale = scale;
            worldLabel = _context.AddWorldLabel(marker, label, Vector3.up * 0.55f, 12, Color.white);
            marker.SetActive(false);
            return marker;
        }

        private void SetSceneActive(bool active)
        {
            if (_gate != null) { _gate.transform.position = _holdZone; _gate.SetActive(active); }
            if (_toy != null) { _toy.transform.position = _crossZone; _toy.SetActive(active); }
        }

        private void UpdateLabels()
        {
            if (_gate != null)
            {
                _gate.transform.position = _holdZone;
                var sr = _gate.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = _puzzle.Held ? GateHeldColor : GateIdleColor;
                if (_context.Now() < _gateSnapReactionUntil)
                    MissionPropArt.SetSprite(_gateArt, FinalGameplayArt.GateCrashGateSnap);
                else
                    MissionPropArt.SetSprite(_gateArt, _puzzle.Held ? FinalGameplayArt.GateCrashGateHeld : FinalGameplayArt.GateCrashGateClosed);
                if (_gateLabel != null) _gateLabel.text = _puzzle.Held ? "GATE HELD - SQUEEZE THROUGH!" : "COCOA: HOLD THE GATE!";
            }
            if (_toy != null)
            {
                _toy.transform.position = _crossZone;
                MissionPropArt.SetSprite(_toyArt, _puzzle.Solved ? FinalGameplayArt.GateCrashToyClaimed : FinalGameplayArt.GateCrashToyWaiting);
                if (_toyLabel != null) _toyLabel.text = $"TOY - SQUEEZE {Mathf.RoundToInt(_puzzle.CrossRatio * 100f)}%";
            }
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin) => new(
            Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
            Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
    }
}
