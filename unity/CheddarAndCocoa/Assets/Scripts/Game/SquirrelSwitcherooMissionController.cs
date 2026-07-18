using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned bait-and-switch co-op puzzle. Cheddar feints at a decoy to commit the squirrel
    /// to a chase; only while it is committed can Cocoa raid the real stash. Over-feinting wises the
    /// squirrel up, and too many backfires end the run.
    /// </summary>
    public sealed class SquirrelSwitcherooMissionController : IMissionController, IMissionInteractionController,
        IMissionSuccessPresentationController
    {
        private const float BaitRange = 4f;
        private const float StashRange = 4f;
        private const float CommitThreshold = 0.6f;
        private const float CommitRate = 1f;   // baiter in range commits the squirrel in ~0.6s.
        private const float DecayRate = 0.5f;  // easing off keeps the window open briefly.
        private const float OverbaitTolerance = 0.6f; // holding the pin this long wises the squirrel up.
        private const int HitsNeeded = 3;
        private const int MaxBackfires = 4;
        private const float SuccessHoldSeconds = 1.15f;

        private static readonly Color GuardingColor = new(0.7f, 0.5f, 0.2f);
        private static readonly Color ChasingColor = new(0.4f, 0.8f, 0.5f);

        private readonly CoopBaitSwitchPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _decoy;
        private GameObject _stash;
        private MissionPropArtAttachment _decoyArt;
        private MissionPropArtAttachment _stashArt;
        private Vector2 _decoyZone;
        private Vector2 _stashZone;
        private int _backfiresSeen;
        private int _hitsSeen;
        private int _whiffsSeen;
        private bool _struckThisWindow;
        private bool _failed;
        private bool _baitEngaged;
        private float _decoyReactionUntil;
        private float _stashReactionUntil;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.SquirrelSwitcheroo;
        public bool IsComplete => _puzzle.Solved && _successHoldRemaining <= 0f;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "Cheddar over-baited and the squirrel wised up too many times - it never left the stash."
            : null;
        public CoopBaitSwitchPuzzle Puzzle => _puzzle;
        public Vector2 DecoyZone => _decoyZone;
        public Vector2 StashZone => _stashZone;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildSwitcherooSummary(_puzzle);
        public bool IsPresentingSuccessfulOutcome => _puzzle.Solved && _successHoldRemaining > 0f;
        public float SuccessHoldRemaining => _successHoldRemaining;
        public bool BaitEngaged => _baitEngaged;

        public string ObjectiveLabel => IsPresentingSuccessfulOutcome
            ? "Stash cracked! Cheddar sold the fake and Cocoa stole the real prize!"
            : _puzzle.Committed
                ? $"Cocoa: Interact at the stash now; Cheddar, ease off the decoy! (raids {_puzzle.Hits}/{HitsNeeded}, backfires {_puzzle.Backfires}/{MaxBackfires})"
                : $"Cheddar: Bark by the decoy, then feather away to pull the squirrel off the stash (raids {_puzzle.Hits}/{HitsNeeded}, backfires {_puzzle.Backfires}/{MaxBackfires})";

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
            _whiffsSeen = 0;
            _struckThisWindow = false;
            _failed = false;
            _baitEngaged = false;
            _decoyReactionUntil = 0f;
            _stashReactionUntil = 0f;
            _successHoldRemaining = 0f;
            _decoyZone = new Vector2(_context.Bounds.center.x - 10f, _context.Bounds.center.y);
            _stashZone = new Vector2(_context.Bounds.center.x + 10f, _context.Bounds.center.y);
            SetSceneActive(true);
            MissionPropArt.SetSprite(_decoyArt, FinalGameplayArt.SwitcherooDecoyGuarded);
            MissionPropArt.SetSprite(_stashArt, FinalGameplayArt.SwitcherooStashGuarded);
            UpdateLabels();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_failed || _context.Dogs == null) return;

            if (_puzzle.Solved)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                UpdateLabels();
                return;
            }

            int baiter = _context.IndexOfDog(DogId.Cheddar);
            int striker = _context.IndexOfDog(DogId.Cocoa);
            if (baiter < 0 || striker < 0) return;

            bool cheddarAtDecoy = Vector2.Distance(_context.Dogs[baiter].transform.position, _decoyZone) <= BaitRange;
            if (_baitEngaged && !cheddarAtDecoy)
            {
                _baitEngaged = false;
                _context.SetCue("Cheddar peeled away from the decoy - Cocoa, read the commitment window!");
                _context.LogEvent("SwitcherooBaitReleased", "Cheddar feathered away");
            }

            // Cheddar must deliberately bark to begin the feint; commitment decays once he eases off.
            bool baiting = _baitEngaged && cheddarAtDecoy;
            _puzzle.Advance(deltaTime, baiting);

            // Cocoa's raid is a deliberate Interact, exactly once per committed window.
            if (!_puzzle.Committed) _struckThisWindow = false;

            HandleProgress();
            if (_failed) return;
            UpdateLabels();
        }

        public bool HandleBark(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length)
                return false;

            if (DogIdAt(dogIndex) != DogId.Cheddar)
            {
                _context.SetCue("Cocoa is the stash raider - Cheddar has to Bark-taunt the squirrel at the decoy.");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "CHEDDAR BAITS", new Color(1f, 0.72f, 0.3f));
                _context.MarkFailedInteraction(DogId.Cocoa, "Cocoa tried to bait - that's Cheddar's move");
                return true;
            }

            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _decoyZone) > BaitRange)
            {
                _context.SetCue("Cheddar needs to be beside the decoy before his Bark-taunt will fool the squirrel.");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "BARK BY DECOY", new Color(1f, 0.72f, 0.3f));
                return true;
            }

            if (_baitEngaged)
            {
                _context.SetCue("The squirrel is already taking the bait - Cheddar, peel away before it wises up!");
                return true;
            }

            _baitEngaged = true;
            _context.SetCue("Cheddar sold the fake! Hold just long enough, then peel away while Cocoa raids.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "HEY, SQUIRREL!");
            _context.SpawnWorldPop(_decoyZone, "COME GET IT!", new Color(1f, 0.74f, 0.24f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.RequestRumble("switcheroo_bait", 0.08f, 0.2f, 0.08f);
            _context.LogEvent("SwitcherooBaitEngaged", "Cheddar barked at decoy");
            UpdateLabels();
            return true;
        }

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length)
                return false;

            if (DogIdAt(dogIndex) != DogId.Cocoa)
            {
                _context.SetCue("Cheddar keeps the squirrel busy; Cocoa is the one who Interacts to raid the stash.");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "COCOA RAIDS", new Color(0.35f, 0.9f, 0.8f));
                _context.MarkFailedInteraction(DogId.Cheddar, "Cheddar tried to raid - that's Cocoa's move");
                return true;
            }

            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _stashZone) > StashRange)
            {
                _context.SetCue("Cocoa needs to reach the real stash before she can Interact to raid it.");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "INTERACT AT STASH", new Color(0.35f, 0.9f, 0.8f));
                return true;
            }

            if (_struckThisWindow)
            {
                _context.SetCue("That opening is spent - Cheddar must reset and bait the squirrel again.");
                return true;
            }

            bool committed = _puzzle.Committed;
            _puzzle.Strike();
            if (committed)
            {
                _struckThisWindow = true;
                _baitEngaged = false;
            }
            HandleProgress();
            if (!_failed) UpdateLabels();
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
            hideDistance = BaitRange;
            if (_context.IndexOfDog(DogId.Cheddar) == dogIndex)
            {
                target = _decoy != null ? _decoy.transform : null;
                copy = _baitEngaged ? "FEATHER AWAY" : "BARK THE DECOY";
            }
            else
            {
                target = _stash != null ? _stash.transform : null;
                copy = _puzzle.Committed ? "INTERACT TO RAID" : "WAIT FOR CHASE";
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
            if (!_failed) UpdateLabels();
        }

        /// <summary>Test hook: Cocoa raids the stash; lands only while the squirrel is committed to the decoy.</summary>
        public void ForceSwitcherooStrike()
        {
            _puzzle.Strike();
            HandleProgress();
        }

        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        private void HandleProgress()
        {
            if (_puzzle.Hits > _hitsSeen)
            {
                _hitsSeen = _puzzle.Hits;
                _baitEngaged = false;
                _context.AddScore(ScoreEventCatalog.StashFound.Points, "STASH RAIDED");
                int baiter = _context.IndexOfDog(DogId.Cheddar);
                int striker = _context.IndexOfDog(DogId.Cocoa);
                if (baiter >= 0) _context.CreditDog(baiter);
                if (striker >= 0) _context.CreditDog(striker);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue($"Cocoa snatched from the stash while the squirrel chased the decoy! ({_puzzle.Hits}/{HitsNeeded})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SWITCHEROO!");
                _context.SpawnWorldPop(_stashZone, "SWITCHEROO!", new Color(0.45f, 0.9f, 0.55f));
                _context.LogEvent("SwitcherooRaid", $"{_puzzle.Hits}/{HitsNeeded}");
                _stashReactionUntil = _context.Now() + 0.55f;
                MissionPropArt.SetSprite(_stashArt, FinalGameplayArt.SwitcherooStashRaided);
                _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
                _context.RequestRumble("switcheroo_raid", 0.12f, 0.28f, 0.1f);
                if (_puzzle.Solved)
                {
                    _successHoldRemaining = SuccessHoldSeconds;
                    _context.SetCue("Stash cracked! Cheddar sold the fake and Cocoa stole the real prize!");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "STASH CRACKED!");
                    _context.SpawnWorldPop(_stashZone, "STASH CRACKED!", new Color(1f, 0.86f, 0.3f));
                    _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
                    _context.RequestRumble("switcheroo_payoff", 0.28f, 0.52f, 0.2f);
                    _context.LogEvent("SwitcherooPayoff", "Stash cracked; holding live-world success beat");
                }
            }

            if (_puzzle.Whiffs > _whiffsSeen)
            {
                _whiffsSeen = _puzzle.Whiffs;
                _context.SetCue("BONK! The squirrel was still guarding. Cheddar must Bark-bait it into a chase first.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "GUARDED! BONK!");
                _context.SpawnWorldPop(_stashZone, "BONK!", new Color(1f, 0.5f, 0.25f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
                _context.RequestRumble("switcheroo_whiff", 0.14f, 0.3f, 0.12f);
                _context.LogEvent("SwitcherooWhiff", _puzzle.Whiffs.ToString());
                _stashReactionUntil = _context.Now() + 0.55f;
            }

            if (_puzzle.Backfires > _backfiresSeen)
            {
                _backfiresSeen = _puzzle.Backfires;
                _context.AddScore(ScoreEventCatalog.FakeOut.Points, "BAIT BACKFIRE");
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                _context.SetCue($"Over-baited! The squirrel wised up and bolted back to the stash. ({_puzzle.Backfires}/{MaxBackfires})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "BACKFIRE!");
                _context.SpawnWorldPop(_decoyZone, "WISED UP!", new Color(1f, 0.35f, 0.2f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                _context.RequestRumble("switcheroo_backfire", 0.18f, 0.38f, 0.14f);
                _context.LogEvent("SwitcherooBackfire", $"{_puzzle.Backfires}/{MaxBackfires}");
                _decoyReactionUntil = _context.Now() + 0.55f;
                MissionPropArt.SetSprite(_decoyArt, FinalGameplayArt.SwitcherooDecoyBackfire);
                _baitEngaged = false;
                if (_puzzle.Backfires >= MaxBackfires) _failed = true;
            }
        }

        private void BuildScene()
        {
            // Identity-only close-range text: the badge alternation, guarded/chased/raided sprites,
            // and the HUD objective line carry the feint/raid instruction and counters.
            _decoy = NewMarker("SwitcherooDecoy", GuardingColor, "DECOY", new Vector3(1.6f, 3f, 1f), out _);
            _stash = NewMarker("SwitcherooStash", new Color(0.6f, 0.8f, 1f), "STASH", Vector3.one * 1.2f, out _);
            _decoyArt = MissionPropArt.AttachObject(_decoy, FinalGameplayArt.SwitcherooDecoyGuarded, 0.012f, 18, true);
            _stashArt = MissionPropArt.AttachObject(_stash, FinalGameplayArt.SwitcherooStashGuarded, 0.012f, 18, true);
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
            // Distance signal alternates with the bait rhythm: decoy until the squirrel commits
            // to the feint, stash while the raid window is open.
            ActorSignalBadge.SetStationSignal(_decoy, !_puzzle.Solved && !_failed && !_puzzle.Committed);
            ActorSignalBadge.SetStationSignal(_stash, !_puzzle.Solved && !_failed && _puzzle.Committed);
            if (_decoy != null)
            {
                _decoy.transform.position = _decoyZone;
                var sr = _decoy.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = _puzzle.Committed ? ChasingColor : GuardingColor;
                MissionPropArt.SetSprite(_decoyArt, _puzzle.Solved
                    ? FinalGameplayArt.SwitcherooDecoyChased
                    : _context.Now() < _decoyReactionUntil
                    ? FinalGameplayArt.SwitcherooDecoyBackfire
                    : _puzzle.Committed
                        ? FinalGameplayArt.SwitcherooDecoyChased
                        : FinalGameplayArt.SwitcherooDecoyGuarded);
            }
            if (_stash != null)
            {
                _stash.transform.position = _stashZone;
                MissionPropArt.SetSprite(_stashArt, _puzzle.Solved
                    ? FinalGameplayArt.SwitcherooStashRaided
                    : _context.Now() < _stashReactionUntil
                    ? FinalGameplayArt.SwitcherooStashRaided
                    : _puzzle.Committed
                        ? FinalGameplayArt.SwitcherooStashOpen
                        : FinalGameplayArt.SwitcherooStashGuarded);
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
