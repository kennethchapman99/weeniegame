using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned human-distraction co-op puzzle. Cocoa flops belly-up to hold the human's
    /// gaze (a sustained hold, with Cheddar's burp as a burst spike) while Cheddar sneaks the dropped
    /// steak; sneaking while the human is watching gets the pair spotted, and too many exposures end
    /// the run.
    /// </summary>
    public sealed class TableStealthMissionController : IMissionController
    {
        private const float DistractRange = 4f;
        private const float SneakRange = 4f;
        private const float SneakNeeded = 1.5f;
        private const float AttentionThreshold = 0.3f;
        private const float AttentionDecay = 0.25f;
        private const float BurpSpike = 0.8f;
        private const float BurpCooldown = 1.5f;
        private const float FlopRise = 3f;
        private const float FlopStamina = 8f;
        private const int MaxExposures = 4;
        private const float HumanReactionSeconds = 0.55f;

        private static readonly Color HumanIdleColor = new(0.7f, 0.5f, 0.2f);
        private static readonly Color HumanDistractedColor = new(0.4f, 0.8f, 0.5f);
        private static readonly Color HumanSpottedColor = new(1f, 0.36f, 0.18f);
        private static readonly Color HumanSuccessColor = new(0.45f, 0.9f, 0.55f);
        private static readonly Color HumanFailColor = new(0.9f, 0.12f, 0.08f);

        private readonly CoopHumanDistractionPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _human;
        private GameObject _steak;
        private MissionActorFeedback _humanFeedback;
        private MissionPropArtAttachment _humanArt;
        private MissionPropArtAttachment _steakArt;
        private TextMesh _humanLabel;
        private TextMesh _steakLabel;
        private Vector2 _humanZone;
        private Vector2 _stealZone;
        private int _exposuresSeen;
        private bool _failed;
        private float _humanReactionUntil;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.TableStealth;
        public bool IsComplete => _puzzle.Solved;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "Cheddar kept sneaking while the human was watching - they got caught at the table too many times."
            : null;
        public CoopHumanDistractionPuzzle Puzzle => _puzzle;
        public Vector2 HumanZone => _humanZone;
        public Vector2 StealZone => _stealZone;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildTableStealthSummary(_puzzle);

        public string ObjectiveLabel
        {
            get
            {
                int pct = Mathf.RoundToInt(_puzzle.SneakRatio * 100f);
                return _puzzle.HumanDistracted
                    ? $"Cheddar: sneak the steak while the human is distracted ({pct}%, spotted {_puzzle.Exposures}/{MaxExposures})"
                    : $"Cocoa: flop belly-up by the human to hold their gaze (sneak {pct}%, spotted {_puzzle.Exposures}/{MaxExposures})";
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
            _puzzle.Configure(SneakNeeded, AttentionThreshold, AttentionDecay,
                BurpSpike, BurpCooldown, FlopRise, FlopStamina);
            _exposuresSeen = 0;
            _failed = false;
            _humanReactionUntil = 0f;
            _humanZone = new Vector2(_context.Bounds.center.x - 10f, _context.Bounds.center.y);
            _stealZone = new Vector2(_context.Bounds.center.x + 10f, _context.Bounds.center.y);
            SetSceneActive(true);
            UpdateLabels();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null) return;

            int distractor = _context.IndexOfDog(DogId.Cocoa);
            int sneaker = _context.IndexOfDog(DogId.Cheddar);
            if (distractor < 0 || sneaker < 0) return;

            bool flopping = Vector2.Distance(_context.Dogs[distractor].transform.position, _humanZone) <= DistractRange;
            _puzzle.SetBellyFlop(flopping);
            bool sneaking = Vector2.Distance(_context.Dogs[sneaker].transform.position, _stealZone) <= SneakRange;
            _puzzle.Advance(deltaTime, sneaking);

            HandleExposures();
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
            hideDistance = DistractRange;
            if (_context.IndexOfDog(DogId.Cocoa) == dogIndex)
            {
                target = _human != null ? _human.transform : null;
                copy = "FLOP TO DISTRACT";
            }
            else
            {
                target = _steak != null ? _steak.transform : null;
                copy = "SNEAK THE STEAK";
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("table_stealth", score, timeRemaining, _puzzle.Solved ? 1 : 0, 1, _puzzle.Exposures,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: Cocoa commits to / releases the belly-flop distraction (the sustain hold).</summary>
        public void ForceTableFlop(bool flopped)
        {
            _puzzle.SetBellyFlop(flopped);
            UpdateLabels();
        }

        /// <summary>Test hook: Cheddar fires a burp-cloud distraction (the burst spike).</summary>
        public void ForceTableBurp() => _puzzle.Burp();

        /// <summary>Test hook: advance the sneak by <paramref name="seconds"/> with the partner in the steak lane.</summary>
        public void ForceTableSneak(float seconds)
        {
            _puzzle.Advance(seconds, true);
            _puzzle.Advance(0.0001f, false); // reset the exposure edge so repeated forced sneaks each register
            HandleExposures();
            UpdateLabels();
        }

        private void HandleExposures()
        {
            if (_puzzle.Exposures <= _exposuresSeen) return;

            _exposuresSeen = _puzzle.Exposures;
            _context.AddScore(ScoreEventCatalog.FakeOut.Points, "SPOTTED");
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"The human glanced over! ({_puzzle.Exposures}/{MaxExposures}) Keep them distracted before sneaking.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SPOTTED!");
            _context.SpawnWorldPop(_stealZone, "SPOTTED!", new Color(1f, 0.35f, 0.2f));
            _context.LogEvent("TableSpotted", $"{_puzzle.Exposures}/{MaxExposures}");
            if (_puzzle.Exposures >= MaxExposures)
            {
                _failed = true;
                SetHumanState("HUMAN CAUGHT THE TABLE HEIST!", HumanFailColor, 0.16f,
                    new Color(1f, 0.62f, 0.55f, 1f));
            }
            else
            {
                _humanReactionUntil = _context.Now() + HumanReactionSeconds;
                SetHumanState("HUMAN SPOTTED CHEDDAR!", HumanSpottedColor, 0.14f,
                    new Color(1f, 0.78f, 0.42f, 1f));
            }
        }

        private void BuildScene()
        {
            _human = NewMarker("TableStealthHuman", HumanIdleColor, "COCOA: FLOP TO DISTRACT THE HUMAN!", new Vector3(1.6f, 4f, 1f), out _humanLabel);
            _steak = NewMarker("TableStealthSteak", new Color(0.6f, 0.8f, 1f), "STEAK - CHEDDAR SNEAK IT!", Vector3.one * 1.2f, out _steakLabel);
            _humanArt = MissionPropArt.AttachObject(_human, FinalGameplayArt.MissionTableHuman, 0.013f, 18, true);
            _steakArt = MissionPropArt.AttachObject(_steak, FinalGameplayArt.MissionSteakPlate, 0.012f, 18, true);
            _humanFeedback = _human.AddComponent<MissionActorFeedback>();
            _humanFeedback.Init(_human.GetComponent<SpriteRenderer>(), "HUMAN WATCHING TABLE", 0.03f, Vector3.forward * 12f);
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
            if (_human != null) { _human.transform.position = _humanZone; _human.SetActive(active); }
            if (_steak != null) { _steak.transform.position = _stealZone; _steak.SetActive(active); }
        }

        private void UpdateLabels()
        {
            if (_human != null)
            {
                _human.transform.position = _humanZone;
                if (_puzzle.Solved)
                    SetHumanState("HUMAN DISTRACTED - STEAK GONE!", HumanSuccessColor, 0.12f,
                        new Color(0.8f, 1f, 0.78f, 1f));
                else if (_failed)
                    SetHumanState("HUMAN CAUGHT THE TABLE HEIST!", HumanFailColor, 0.16f,
                        new Color(1f, 0.62f, 0.55f, 1f));
                else if (_context.Now() >= _humanReactionUntil)
                {
                    bool watchingCocoa = _puzzle.BellyFlopped || _puzzle.HumanDistracted;
                    SetHumanState(
                        watchingCocoa ? "HUMAN WATCHING COCOA - STEAK OPEN!" : "HUMAN WATCHING TABLE",
                        watchingCocoa ? HumanDistractedColor : HumanIdleColor,
                        watchingCocoa ? 0.08f : 0.03f,
                        watchingCocoa ? new Color(0.78f, 1f, 0.78f, 1f) : Color.white);
                }
            }
            if (_steak != null)
            {
                _steak.transform.position = _stealZone;
                if (_steakLabel != null)
                    _steakLabel.text = _puzzle.Solved
                        ? "STEAK GONE!"
                        : $"STEAK - SNEAK {Mathf.RoundToInt(_puzzle.SneakRatio * 100f)}%";
                if (_steakArt != null)
                {
                    _steakArt.SetTint(_puzzle.Solved
                        ? new Color(1f, 1f, 1f, 0.45f)
                        : _puzzle.HumanDistracted ? new Color(1f, 0.95f, 0.72f, 1f) : Color.white);
                    if (_puzzle.HumanDistracted && !_puzzle.Solved) _steakArt.Pulse(0.12f, 0.04f);
                }
            }
        }

        private void SetHumanState(string label, Color fallbackColor, float pulseAmount, Color artTint)
        {
            if (_humanFeedback != null) _humanFeedback.SetState(label, fallbackColor, pulseAmount);
            else if (_humanLabel != null) _humanLabel.text = label;
            if (_humanArt != null)
            {
                _humanArt.SetTint(artTint);
                _humanArt.Pulse(0.18f, pulseAmount);
            }
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin) => new(
            Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
            Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
    }
}
