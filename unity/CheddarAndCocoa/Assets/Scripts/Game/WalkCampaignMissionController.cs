using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned social-manipulation co-op puzzle. The dogs con the human into a walk by sending
    /// ONE clear message built from BOTH of them at once - Cocoa's door-stare AND Cheddar presenting the
    /// leash - and holding it. Covering only one (or wandering off) confuses the human; too many misreads
    /// end the run.
    /// </summary>
    public sealed class WalkCampaignMissionController : IMissionController
    {
        private const float StationRange = 3.5f;
        private const float ComprehendNeeded = 2.5f; // both dogs hold the combo this long -> walk earned.
        private const float ConfusionMax = 3f;       // incomplete combo this long -> the human misreads.
        private const int MaxMisreads = 3;
        private const float HumanReactionSeconds = 0.65f;
        private const SocialStimulus RequiredMessage = SocialStimulus.DoorStare | SocialStimulus.PresentLeash;

        private static readonly Color HumanConfusedColor = new(0.9f, 0.8f, 0.5f);
        private static readonly Color HumanGettingItColor = new(0.5f, 0.85f, 0.55f);
        private static readonly Color HumanMisreadColor = new(0.95f, 0.6f, 0.25f);
        private static readonly Color HumanSuccessColor = new(0.45f, 0.9f, 0.6f);
        private static readonly Color HumanFailColor = new(0.9f, 0.16f, 0.1f);

        private readonly CoopSocialManipulationPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _human;
        private GameObject _leash;
        private MissionActorFeedback _humanFeedback;
        private MissionPropArtAttachment _humanArt;
        private MissionPropArtAttachment _leashArt;
        private TextMesh _humanLabel;
        private TextMesh _leashLabel;
        private Vector2 _doorZone;
        private Vector2 _leashZone;
        private int _misreadsSeen;
        private bool _gettingItScored;
        private bool _failed;
        private float _humanReactionUntil;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.WalkCampaign;
        public bool IsComplete => _puzzle.Solved;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "Too many mixed signals - the human gave up and brought the wrong thing one time too many."
            : null;
        public CoopSocialManipulationPuzzle Puzzle => _puzzle;
        public Vector2 DoorZone => _doorZone;
        public Vector2 LeashZone => _leashZone;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildWalkCampaignSummary(_puzzle);

        public string ObjectiveLabel => _puzzle.ExactMatch
            ? $"Hold it together! Cocoa stares the door, Cheddar holds the leash - the human's {Mathf.RoundToInt(_puzzle.Comprehension / ComprehendNeeded * 100)}% sold (confused {_puzzle.Misreads}/{MaxMisreads})"
            : $"Send ONE message: Cocoa stare at the door AND Cheddar present the leash at once (confused {_puzzle.Misreads}/{MaxMisreads})";

        public void Initialize(MissionContext context)
        {
            _context = context;
            BuildScene();
            Cleanup();
        }

        public void StartMission()
        {
            _puzzle.Configure(RequiredMessage, ComprehendNeeded, ConfusionMax);
            _misreadsSeen = 0;
            _gettingItScored = false;
            _failed = false;
            _humanReactionUntil = 0f;
            _doorZone = new Vector2(_context.Bounds.center.x - 6f, _context.Bounds.center.y - 6f);
            _leashZone = new Vector2(_context.Bounds.center.x + 11f, _context.Bounds.center.y + 3f);
            SetSceneActive(true);
            UpdateLabels();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null) return;

            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            // The message is built from positions: Cocoa stares down the door, Cheddar presents the leash.
            // Neither stimulus alone reads, so both dogs must hold their stations at the same time.
            SocialStimulus active = SocialStimulus.None;
            if (Vector2.Distance(_context.Dogs[cocoa].transform.position, _doorZone) <= StationRange)
                active |= SocialStimulus.DoorStare;
            if (Vector2.Distance(_context.Dogs[cheddar].transform.position, _leashZone) <= StationRange)
                active |= SocialStimulus.PresentLeash;
            _puzzle.SetActiveSet(active);
            _puzzle.Advance(deltaTime);

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
            hideDistance = StationRange;
            if (_context.IndexOfDog(DogId.Cocoa) == dogIndex)
            {
                target = _human != null ? _human.transform : null;
                copy = "STARE AT THE DOOR";
            }
            else
            {
                target = _leash != null ? _leash.transform : null;
                copy = "PRESENT THE LEASH";
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("walk_campaign", score, timeRemaining, _puzzle.Solved ? 1 : 0, 1, _puzzle.Misreads,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: hold a stimulus combo (door-stare / present-leash) for <paramref name="seconds"/>.</summary>
        public void ForceWalkCampaign(float seconds, bool doorStare, bool presentLeash)
        {
            SocialStimulus active = SocialStimulus.None;
            if (doorStare) active |= SocialStimulus.DoorStare;
            if (presentLeash) active |= SocialStimulus.PresentLeash;
            _puzzle.SetActiveSet(active);
            _puzzle.Advance(seconds);
            HandleProgress();
            UpdateLabels();
        }

        private void HandleProgress()
        {
            // First moment the combo clicks: reward reading the room together.
            if (_puzzle.ExactMatch && !_gettingItScored && !_puzzle.Solved)
            {
                _gettingItScored = true;
                _context.AddScore(ScoreEventCatalog.HumanGettingIt.Points, ScoreEventCatalog.HumanGettingIt.Label);
                _context.SetFeedback(GameManager.FeedbackKind.Intro);
                _context.SetCue("The human's getting it - hold the door-stare and the leash together!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "GETTING IT!");
                _context.LogEvent("WalkGettingIt", "combo");
                SetHumanState("HUMAN GETTING IT - HOLD THE MESSAGE!", HumanGettingItColor, 0.1f,
                    new Color(0.78f, 1f, 0.78f, 1f));
            }

            if (_puzzle.Misreads > _misreadsSeen)
            {
                _misreadsSeen = _puzzle.Misreads;
                _gettingItScored = false; // earn the "getting it" pop again on the next clean combo
                _context.AddScore(ScoreEventCatalog.HumanMisread.Points, ScoreEventCatalog.HumanMisread.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                _context.SetCue($"Mixed signals! The human brought the wrong thing. ({_puzzle.Misreads}/{MaxMisreads})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CONFUSED!");
                if (_human != null) _context.SpawnWorldPop(_human.transform.position, "WRONG THING!", new Color(0.95f, 0.6f, 0.25f));
                _context.LogEvent("WalkMisread", $"{_puzzle.Misreads}/{MaxMisreads}");
                if (_puzzle.Misreads >= MaxMisreads)
                {
                    _failed = true;
                    SetHumanState("HUMAN GAVE UP - MIXED SIGNALS!", HumanFailColor, 0.16f,
                        new Color(1f, 0.58f, 0.52f, 1f));
                }
                else
                {
                    _humanReactionUntil = _context.Now() + HumanReactionSeconds;
                    SetHumanState("HUMAN MISREAD - WRONG THING!", HumanMisreadColor, 0.13f,
                        new Color(1f, 0.84f, 0.52f, 1f));
                }
            }

            if (_puzzle.Solved)
            {
                _context.AddScore(ScoreEventCatalog.WalkConned.Points, ScoreEventCatalog.WalkConned.Label);
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "WALKIES!");
                if (_human != null) _context.SpawnWorldPop(_human.transform.position, "WALKIES!", new Color(0.5f, 0.9f, 0.55f));
                _context.LogEvent("WalkConned", "solved");
                SetHumanState("HUMAN GRABBED THE LEASH - WALKIES!", HumanSuccessColor, 0.14f,
                    new Color(0.75f, 1f, 0.78f, 1f));
            }
        }

        private void BuildScene()
        {
            _human = NewMarker("WalkCampaignHuman", new Color(0.9f, 0.8f, 0.5f), "HUMAN - CONVINCE THEM TO WALK YOU!", new Vector3(1.8f, 3.4f, 1f), out _humanLabel);
            _leash = NewMarker("WalkCampaignLeash", new Color(0.6f, 0.8f, 1f), "LEASH - CHEDDAR PRESENT IT!", Vector3.one * 1.2f, out _leashLabel);
            _humanArt = MissionPropArt.AttachObject(_human, FinalGameplayArt.MissionWalkHuman, 0.013f, 18, true);
            _leashArt = MissionPropArt.AttachObject(_leash, FinalGameplayArt.MissionWalkLeash, 0.012f, 18, true);
            _humanFeedback = _human.AddComponent<MissionActorFeedback>();
            _humanFeedback.Init(_human.GetComponent<SpriteRenderer>(), "HUMAN CONFUSED - SEND ONE MESSAGE!", 0.03f, Vector3.forward * 10f);
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
            if (_human != null) { _human.transform.position = _doorZone; _human.SetActive(active); }
            if (_leash != null) { _leash.transform.position = _leashZone; _leash.SetActive(active); }
        }

        private void UpdateLabels()
        {
            if (_human != null)
            {
                _human.transform.position = _doorZone;
                if (_puzzle.Solved)
                    SetHumanState("HUMAN GRABBED THE LEASH - WALKIES!", HumanSuccessColor, 0.14f,
                        new Color(0.75f, 1f, 0.78f, 1f));
                else if (_failed)
                    SetHumanState("HUMAN GAVE UP - MIXED SIGNALS!", HumanFailColor, 0.16f,
                        new Color(1f, 0.58f, 0.52f, 1f));
                else if (_context.Now() >= _humanReactionUntil)
                    SetHumanState(
                        _puzzle.ExactMatch
                            ? $"HUMAN GETTING IT - HOLD IT! ({Mathf.RoundToInt(_puzzle.Comprehension / ComprehendNeeded * 100f)}%)"
                            : $"HUMAN CONFUSED - SEND ONE MESSAGE! (misreads {_puzzle.Misreads}/{MaxMisreads})",
                        _puzzle.ExactMatch ? HumanGettingItColor : HumanConfusedColor,
                        _puzzle.ExactMatch ? 0.09f : 0.03f,
                        _puzzle.ExactMatch ? new Color(0.78f, 1f, 0.78f, 1f) : Color.white);
            }
            if (_leash != null)
            {
                _leash.transform.position = _leashZone;
                if (_leashLabel != null)
                    _leashLabel.text = (_puzzle.Active & SocialStimulus.PresentLeash) != 0 ? "LEASH PRESENTED!" : "LEASH - CHEDDAR PRESENT IT!";
                if (_leashArt != null)
                {
                    bool presented = (_puzzle.Active & SocialStimulus.PresentLeash) != 0;
                    _leashArt.SetTint(presented ? new Color(1f, 0.96f, 0.72f, 1f) : Color.white);
                    if (presented) _leashArt.Pulse(0.12f, 0.04f);
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
