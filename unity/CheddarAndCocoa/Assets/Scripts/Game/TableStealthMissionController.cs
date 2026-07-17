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
    public sealed class TableStealthMissionController : IMissionController, IMissionInteractionController,
        IMissionPressureHud, IMissionSuccessPresentationController
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
        private const float SuccessHoldSeconds = 1.15f;

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
        private bool _creditedSolve;
        private bool _failed;
        private bool _flopEngaged;
        private bool _burpWindowForCocoa;
        private float _humanReactionUntil;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.TableStealth;
        public bool IsComplete => _puzzle.Solved && _successHoldRemaining <= 0f;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "Cheddar kept sneaking while the human was watching - they got caught at the table too many times."
            : null;
        public CoopHumanDistractionPuzzle Puzzle => _puzzle;
        public Vector2 HumanZone => _humanZone;
        public Vector2 StealZone => _stealZone;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildTableStealthSummary(_puzzle);
        public string PressureLabel => "STEAK SNEAK";
        public bool PressureVisible => !_puzzle.Solved;
        public float PressureNormalized => _puzzle.SneakRatio;
        public Color PressureColor => Color.Lerp(new Color(0.92f, 0.68f, 0.24f), new Color(0.35f, 1f, 0.52f), _puzzle.SneakRatio);
        public bool IsPresentingSuccessfulOutcome => _puzzle.Solved && _successHoldRemaining > 0f;
        public float SuccessHoldRemaining => _successHoldRemaining;
        public bool FlopEngaged => _flopEngaged;
        public bool BurpWindowForCocoa => _burpWindowForCocoa;

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "Steak secured! One dog sold the distraction and the other stole dinner!";
                if (_burpWindowForCocoa && _puzzle.HumanDistracted)
                    return $"Cocoa: sneak the steak while the human reacts to Cheddar's burp (spotted {_puzzle.Exposures}/{MaxExposures})";
                return _puzzle.HumanDistracted
                    ? $"Cheddar: keep sneaking while Cocoa holds their gaze (spotted {_puzzle.Exposures}/{MaxExposures})"
                    : $"Distract the human: Cocoa Interact-flops, or Cheddar barks a burp, so the partner can sneak (spotted {_puzzle.Exposures}/{MaxExposures})";
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
            _creditedSolve = false;
            _failed = false;
            _flopEngaged = false;
            _burpWindowForCocoa = false;
            _humanReactionUntil = 0f;
            _successHoldRemaining = 0f;
            _humanZone = new Vector2(_context.Bounds.center.x - 10f, _context.Bounds.center.y);
            _stealZone = new Vector2(_context.Bounds.center.x + 10f, _context.Bounds.center.y);
            SetSceneActive(true);
            MissionPropArt.SetSprite(_humanArt, FinalGameplayArt.TableStealthHumanWatching);
            MissionPropArt.SetSprite(_steakArt, FinalGameplayArt.TableStealthSteakAvailable);
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

            int distractor = _context.IndexOfDog(DogId.Cocoa);
            int sneaker = _context.IndexOfDog(DogId.Cheddar);
            if (distractor < 0 || sneaker < 0) return;

            bool cocoaAtHuman = Vector2.Distance(_context.Dogs[distractor].transform.position, _humanZone) <= DistractRange;
            if (_flopEngaged && !cocoaAtHuman)
            {
                _flopEngaged = false;
                _puzzle.SetBellyFlop(false);
                _context.SetCue("Cocoa got up - the human is turning back to the steak. Interact by the human to flop again.");
                _context.LogEvent("TableFlopReleased", "Cocoa left the human");
            }
            else
                _puzzle.SetBellyFlop(_flopEngaged && cocoaAtHuman);

            bool partnerAtSteak = _burpWindowForCocoa
                ? Vector2.Distance(_context.Dogs[distractor].transform.position, _stealZone) <= SneakRange
                : Vector2.Distance(_context.Dogs[sneaker].transform.position, _stealZone) <= SneakRange;
            // Give Cocoa's committed flop its brief attention-ramp without immediately counting
            // Cheddar as spotted; an unprepared approach still registers normally.
            bool sneaking = partnerAtSteak && (_puzzle.HumanDistracted || !_puzzle.BellyFlopped);
            _puzzle.Advance(deltaTime, sneaking);
            if (_burpWindowForCocoa && !_puzzle.HumanDistracted)
                _burpWindowForCocoa = false;

            HandleExposures();
            if (_failed) return;
            UpdateLabels();
        }

        public bool HandleBark(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length)
                return false;

            if (DogIdAt(dogIndex) != DogId.Cheddar)
            {
                _context.SetCue("Cocoa's steady bark won't sell this one - she can Interact-flop, or Cheddar can bark a burp by the human.");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "CHEDDAR BURPS", new Color(1f, 0.75f, 0.3f));
                return true;
            }

            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _humanZone) > DistractRange)
            {
                _context.SetCue("Cheddar needs to bark right by the human so the burp cloud gets their attention.");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "BURP BY HUMAN", new Color(1f, 0.75f, 0.3f));
                return true;
            }

            if (!_puzzle.BurpReady)
            {
                _puzzle.Burp();
                _context.SetCue("Cheddar is out of burp - wait for the human to settle, then bark again.");
                _context.SpawnWorldPop(_humanZone, "tiny burp...", new Color(0.82f, 0.78f, 0.5f));
                return true;
            }

            _flopEngaged = false;
            _puzzle.SetBellyFlop(false);
            _puzzle.Burp();
            _burpWindowForCocoa = true;
            _context.SetCue("Cheddar's burp got the human! Cocoa, sneak the steak now!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "BURP CLOUD!");
            _context.SpawnWorldPop(_humanZone, "BRAAAP!", new Color(0.72f, 0.88f, 0.35f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.RequestRumble("table_burp", 0.1f, 0.24f, 0.1f);
            _context.LogEvent("TableBurp", "Cheddar opened Cocoa's sneak window");
            UpdateLabels();
            return true;
        }

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length)
                return false;

            if (DogIdAt(dogIndex) != DogId.Cocoa)
            {
                _context.SetCue("Cheddar is the noisy burper here - Cocoa is the one who can Interact-flop for belly rubs.");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "COCOA FLOPS", new Color(0.35f, 0.9f, 0.8f));
                return true;
            }

            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _humanZone) > DistractRange)
            {
                _context.SetCue("Cocoa needs to be beside the human before she can Interact-flop for a belly rub.");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "FLOP BY HUMAN", new Color(0.35f, 0.9f, 0.8f));
                return true;
            }

            _burpWindowForCocoa = false;
            _flopEngaged = true;
            _puzzle.SetBellyFlop(true);
            _context.SetCue("Cocoa flopped for belly rubs! Cheddar, sneak the steak while she stays planted.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "BELLY-RUB DECOY!");
            _context.SpawnWorldPop(_humanZone, "BELLY UP!", new Color(0.35f, 1f, 0.78f));
            _context.RequestRumble("table_flop", 0.08f, 0.2f, 0.08f);
            _context.LogEvent("TableFlop", "Cocoa opened Cheddar's sneak window");
            UpdateLabels();
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
            hideDistance = DistractRange;
            if (_context.IndexOfDog(DogId.Cocoa) == dogIndex)
            {
                target = _burpWindowForCocoa && _puzzle.HumanDistracted && _steak != null ? _steak.transform : _human != null ? _human.transform : null;
                copy = _burpWindowForCocoa && _puzzle.HumanDistracted ? "SNEAK THE STEAK" : _flopEngaged ? "STAY FLOPPED" : "INTERACT TO FLOP";
            }
            else
            {
                target = _puzzle.BellyFlopped && _steak != null ? _steak.transform : _human != null ? _human.transform : null;
                copy = _puzzle.BellyFlopped ? "SNEAK THE STEAK" : "BARK A BURP";
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("table_stealth", score, timeRemaining, _puzzle.Solved ? 1 : 0, 1, _puzzle.Exposures,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: Cocoa commits to / releases the belly-flop distraction (the sustain hold).</summary>
        public void ForceTableFlop(bool flopped)
        {
            _flopEngaged = flopped;
            if (flopped) _burpWindowForCocoa = false;
            _puzzle.SetBellyFlop(flopped);
            UpdateLabels();
        }

        /// <summary>Test hook: Cheddar fires a burp-cloud distraction (the burst spike).</summary>
        public void ForceTableBurp()
        {
            bool ready = _puzzle.BurpReady;
            _puzzle.Burp();
            if (ready)
            {
                _flopEngaged = false;
                _puzzle.SetBellyFlop(false);
                _burpWindowForCocoa = true;
            }
            UpdateLabels();
        }

        /// <summary>Test hook: advance the sneak by <paramref name="seconds"/> with the partner in the steak lane.</summary>
        public void ForceTableSneak(float seconds)
        {
            _puzzle.Advance(seconds, true);
            _puzzle.Advance(0.0001f, false); // reset the exposure edge so repeated forced sneaks each register
            HandleExposures();
            UpdateLabels();
        }

        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        private void HandleExposures()
        {
            if (_puzzle.Solved && !_creditedSolve)
            {
                _creditedSolve = true;
                _successHoldRemaining = SuccessHoldSeconds;
                int distractor = _context.IndexOfDog(DogId.Cocoa);
                int sneaker = _context.IndexOfDog(DogId.Cheddar);
                if (distractor >= 0) _context.CreditDog(distractor);
                if (sneaker >= 0) _context.CreditDog(sneaker);
                _context.SetCue("Steak secured! One dog sold the distraction and the other stole dinner!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "STEAK SECURED!");
                _context.SpawnWorldPop(_stealZone, "STEAK SECURED!", new Color(1f, 0.86f, 0.3f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
                _context.RequestRumble("table_steak_secured", 0.28f, 0.52f, 0.2f);
                _context.LogEvent("TableStealthPayoff", "Steak secured; holding live-world success beat");
            }

            if (_puzzle.Exposures <= _exposuresSeen) return;

            _exposuresSeen = _puzzle.Exposures;
            _context.AddScore(ScoreEventCatalog.FakeOut.Points, "SPOTTED");
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"The human glanced over! ({_puzzle.Exposures}/{MaxExposures}) Keep them distracted before sneaking.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SPOTTED!");
            _context.SpawnWorldPop(_stealZone, "SPOTTED!", new Color(1f, 0.35f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("table_spotted", 0.16f, 0.34f, 0.12f);
            _context.LogEvent("TableSpotted", $"{_puzzle.Exposures}/{MaxExposures}");
            if (_puzzle.Exposures >= MaxExposures)
            {
                _failed = true;
                SetHumanState("HUMAN CAUGHT THE TABLE HEIST!", HumanFailColor, 0.16f,
                    new Color(1f, 0.62f, 0.55f, 1f), FinalGameplayArt.TableStealthHumanCaught);
            }
            else
            {
                _humanReactionUntil = _context.Now() + HumanReactionSeconds;
                SetHumanState("HUMAN SPOTTED CHEDDAR!", HumanSpottedColor, 0.14f,
                    new Color(1f, 0.78f, 0.42f, 1f), FinalGameplayArt.TableStealthHumanSpotted);
            }
        }

        private void BuildScene()
        {
            // Identity-only close-range text: the human/steak badge alternation, sprite swaps, and
            // the HUD objective line carry the flop/sneak instruction and the sneak percentage.
            _human = NewMarker("TableStealthHuman", HumanIdleColor, "HUMAN", new Vector3(1.6f, 4f, 1f), out _humanLabel);
            _steak = NewMarker("TableStealthSteak", new Color(0.6f, 0.8f, 1f), "STEAK", Vector3.one * 1.2f, out _steakLabel);
            _humanArt = MissionPropArt.AttachObject(_human, FinalGameplayArt.TableStealthHumanWatching, 0.013f, 18, true);
            _steakArt = MissionPropArt.AttachObject(_steak, FinalGameplayArt.TableStealthSteakAvailable, 0.012f, 18, true);
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
            // Distance signal alternates with the distraction rhythm: human until Cocoa opens a
            // window, steak while the sneak window is live.
            bool window = _puzzle.HumanDistracted || _puzzle.BellyFlopped;
            ActorSignalBadge.SetStationSignal(_human, !_puzzle.Solved && !_failed && !window);
            ActorSignalBadge.SetStationSignal(_steak, !_puzzle.Solved && !_failed && window);
            if (_human != null)
            {
                _human.transform.position = _humanZone;
                if (_puzzle.Solved)
                    SetHumanState("HUMAN DISTRACTED - STEAK GONE!", HumanSuccessColor, 0.12f,
                        new Color(0.8f, 1f, 0.78f, 1f), FinalGameplayArt.TableStealthHumanDistracted);
                else if (_failed)
                    SetHumanState("HUMAN CAUGHT THE TABLE HEIST!", HumanFailColor, 0.16f,
                        new Color(1f, 0.62f, 0.55f, 1f), FinalGameplayArt.TableStealthHumanCaught);
                else if (_context.Now() >= _humanReactionUntil)
                {
                    bool watchingCocoa = _puzzle.BellyFlopped || _puzzle.HumanDistracted;
                    SetHumanState(
                        watchingCocoa ? "HUMAN WATCHING COCOA" : "HUMAN WATCHING TABLE",
                        watchingCocoa ? HumanDistractedColor : HumanIdleColor,
                        watchingCocoa ? 0.08f : 0.03f,
                        watchingCocoa ? new Color(0.78f, 1f, 0.78f, 1f) : Color.white,
                        watchingCocoa ? FinalGameplayArt.TableStealthHumanDistracted : FinalGameplayArt.TableStealthHumanWatching);
                }
            }
            if (_steak != null)
            {
                _steak.transform.position = _stealZone;
                if (_steakLabel != null)
                    _steakLabel.text = _puzzle.Solved ? "STEAK GONE!" : "STEAK";
                if (_steakArt != null)
                {
                    MissionPropArt.SetSprite(_steakArt, _puzzle.Solved
                        ? FinalGameplayArt.TableStealthSteakGone
                        : _puzzle.HumanDistracted || _puzzle.BellyFlopped
                            ? FinalGameplayArt.TableStealthSteakSneakProgress
                            : FinalGameplayArt.TableStealthSteakAvailable);
                    _steakArt.SetTint(_puzzle.Solved
                        ? new Color(1f, 1f, 1f, 0.45f)
                        : _puzzle.HumanDistracted ? new Color(1f, 0.95f, 0.72f, 1f) : Color.white);
                    if (_puzzle.HumanDistracted && !_puzzle.Solved) _steakArt.Pulse(0.12f, 0.04f);
                }
            }
        }

        private void SetHumanState(string label, Color fallbackColor, float pulseAmount, Color artTint, string spritePath)
        {
            if (_humanFeedback != null) _humanFeedback.SetState(label, fallbackColor, pulseAmount);
            else if (_humanLabel != null) _humanLabel.text = label;
            if (_humanArt != null)
            {
                MissionPropArt.SetSprite(_humanArt, spritePath);
                _humanArt.SetTint(artTint);
                _humanArt.Pulse(0.18f, pulseAmount);
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
