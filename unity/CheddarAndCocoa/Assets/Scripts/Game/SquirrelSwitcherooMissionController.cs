using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned bait-and-switch co-op puzzle. Cheddar feints at a decoy to commit the squirrel
    /// to a chase; only while it is committed can Cocoa raid the real stash. Over-feinting wises the
    /// squirrel up, and too many backfires end the run.
    /// </summary>
    public sealed class SquirrelSwitcherooMissionController : IMissionController
    {
        private const float BaitRange = 4f;
        private const float StashRange = 4f;
        private const float CommitThreshold = 0.6f;
        private const float CommitRate = 1f;   // baiter in range commits the squirrel in ~0.6s.
        private const float DecayRate = 0.5f;  // easing off keeps the window open briefly.
        private const float OverbaitTolerance = 0.6f; // holding the pin this long wises the squirrel up.
        private const int HitsNeeded = 3;
        private const int MaxBackfires = 4;

        private static readonly Color GuardingColor = new(0.7f, 0.5f, 0.2f);
        private static readonly Color ChasingColor = new(0.4f, 0.8f, 0.5f);

        private readonly CoopBaitSwitchPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _decoy;
        private GameObject _stash;
        private TextMesh _decoyLabel;
        private TextMesh _stashLabel;
        private Vector2 _decoyZone;
        private Vector2 _stashZone;
        private int _backfiresSeen;
        private int _hitsSeen;
        private bool _struckThisWindow;
        private bool _failed;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.SquirrelSwitcheroo;
        public bool IsComplete => _puzzle.Solved;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "Cheddar over-baited and the squirrel wised up too many times - it never left the stash."
            : null;
        public CoopBaitSwitchPuzzle Puzzle => _puzzle;
        public Vector2 DecoyZone => _decoyZone;
        public Vector2 StashZone => _stashZone;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildSwitcherooSummary(_puzzle);

        public string ObjectiveLabel => _puzzle.Committed
            ? $"Cocoa: raid the stash now - the squirrel's chasing the decoy! (raids {_puzzle.Hits}/{HitsNeeded}, backfires {_puzzle.Backfires}/{MaxBackfires})"
            : $"Cheddar: feint at the decoy to bait the squirrel off the stash (raids {_puzzle.Hits}/{HitsNeeded}, backfires {_puzzle.Backfires}/{MaxBackfires})";

        public void Initialize(MissionContext context)
        {
            _context = context;
            BuildScene();
            Cleanup();
        }

        public void StartMission()
        {
            _puzzle.Configure(CommitThreshold, CommitRate, DecayRate, OverbaitTolerance, HitsNeeded, MaxBackfires);
            _backfiresSeen = 0;
            _hitsSeen = 0;
            _struckThisWindow = false;
            _failed = false;
            _decoyZone = new Vector2(_context.Bounds.center.x - 10f, _context.Bounds.center.y);
            _stashZone = new Vector2(_context.Bounds.center.x + 10f, _context.Bounds.center.y);
            SetSceneActive(true);
            UpdateLabels();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null) return;

            int baiter = _context.IndexOfDog(DogId.Cheddar);
            int striker = _context.IndexOfDog(DogId.Cocoa);
            if (baiter < 0 || striker < 0) return;

            // Cheddar feints at the decoy to commit the squirrel; commitment decays when he eases off.
            bool baiting = Vector2.Distance(_context.Dogs[baiter].transform.position, _decoyZone) <= BaitRange;
            _puzzle.Advance(deltaTime, baiting);

            // Cocoa raids the stash: exactly one grab per committed window (she must re-bait for the next).
            if (!_puzzle.Committed) _struckThisWindow = false;
            bool atStash = Vector2.Distance(_context.Dogs[striker].transform.position, _stashZone) <= StashRange;
            if (atStash && _puzzle.Committed && !_struckThisWindow)
            {
                _puzzle.Strike();
                _struckThisWindow = true;
            }

            HandleProgress();
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
            hideDistance = BaitRange;
            if (_context.IndexOfDog(DogId.Cheddar) == dogIndex)
            {
                target = _decoy != null ? _decoy.transform : null;
                copy = "FEINT THE DECOY";
            }
            else
            {
                target = _stash != null ? _stash.transform : null;
                copy = "RAID THE STASH";
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("squirrel_switcheroo", score, timeRemaining, _puzzle.Hits, HitsNeeded, _puzzle.Backfires + _puzzle.Whiffs,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: Cheddar feints at the decoy for <paramref name="seconds"/> (or eases off when baiting=false).</summary>
        public void ForceSwitcherooBait(float seconds, bool baiting = true)
        {
            _puzzle.Advance(seconds, baiting);
            HandleProgress();
        }

        /// <summary>Test hook: Cocoa raids the stash; lands only while the squirrel is committed to the decoy.</summary>
        public void ForceSwitcherooStrike()
        {
            _puzzle.Strike();
            HandleProgress();
        }

        private void HandleProgress()
        {
            if (_puzzle.Hits > _hitsSeen)
            {
                _hitsSeen = _puzzle.Hits;
                _context.AddScore(ScoreEventCatalog.StashFound.Points, "STASH RAIDED");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"Cocoa snatched from the stash while the squirrel chased the decoy! ({_puzzle.Hits}/{HitsNeeded})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SWITCHEROO!");
                _context.SpawnWorldPop(_stashZone, "SWITCHEROO!", new Color(0.45f, 0.9f, 0.55f));
                _context.LogEvent("SwitcherooRaid", $"{_puzzle.Hits}/{HitsNeeded}");
            }

            if (_puzzle.Backfires > _backfiresSeen)
            {
                _backfiresSeen = _puzzle.Backfires;
                _context.AddScore(ScoreEventCatalog.FakeOut.Points, "BAIT BACKFIRE");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                _context.SetCue($"Over-baited! The squirrel wised up and bolted back to the stash. ({_puzzle.Backfires}/{MaxBackfires})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "BACKFIRE!");
                _context.SpawnWorldPop(_decoyZone, "WISED UP!", new Color(1f, 0.35f, 0.2f));
                _context.LogEvent("SwitcherooBackfire", $"{_puzzle.Backfires}/{MaxBackfires}");
                if (_puzzle.Backfires >= MaxBackfires) _failed = true;
            }
        }

        private void BuildScene()
        {
            _decoy = NewMarker("SwitcherooDecoy", GuardingColor, "SQUIRREL - CHEDDAR FEINT THE DECOY!", new Vector3(1.6f, 3f, 1f), out _decoyLabel);
            _stash = NewMarker("SwitcherooStash", new Color(0.6f, 0.8f, 1f), "STASH - COCOA RAID IT WHEN HE BITES!", Vector3.one * 1.2f, out _stashLabel);
            MissionPropArt.AttachObject(_decoy, FinalGameplayArt.MissionDecoyToy, 0.012f, 18, true);
            MissionPropArt.AttachObject(_stash, FinalGameplayArt.MissionSquirrelStash, 0.012f, 18, true);
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
            if (_decoy != null) { _decoy.transform.position = _decoyZone; _decoy.SetActive(active); }
            if (_stash != null) { _stash.transform.position = _stashZone; _stash.SetActive(active); }
        }

        private void UpdateLabels()
        {
            if (_decoy != null)
            {
                _decoy.transform.position = _decoyZone;
                var sr = _decoy.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = _puzzle.Committed ? ChasingColor : GuardingColor;
                if (_decoyLabel != null) _decoyLabel.text = _puzzle.Committed ? "SQUIRREL CHASING THE DECOY - RAID NOW!" : "SQUIRREL GUARDING THE STASH - FEINT IT!";
            }
            if (_stash != null)
            {
                _stash.transform.position = _stashZone;
                if (_stashLabel != null) _stashLabel.text = $"STASH - RAIDS {_puzzle.Hits}/{HitsNeeded}";
            }
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin) => new(
            Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
            Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
    }
}
