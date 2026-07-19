using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Baby Bird Bedlam: chicks tumble out of the big oak nest and the dogs' prey
    /// drive takes over. Cheddar grabs each landed chick and shake-shake-shakes it down in one
    /// cartoon gulp; while his mouth is full the furious parent birds dive-bomb him, and only
    /// Cocoa's bark can drive a dive off before it pecks. Wires the Feast-and-Fend co-op puzzle
    /// into the mission flow, reusing the shared predator actor's transform and collision ownership
    /// while replacing its generic eagle presentation with mission-specific mother-bird art.
    /// </summary>
    public sealed class BabyBirdBedlamMissionController : IMissionController, IMissionInteractionController,
        IMissionSuccessPresentationController
    {
        private const int ChicksNeeded = 4;
        private const int ShakesNeeded = 3;
        private const int MaxPecks = 3;
        private const float DiveWindowSeconds = 1.7f;
        private const float GrabRange = 2.2f;
        private const float BarkRepelRange = 4.5f;
        private const float SpawnY = 11f;
        private const float GroundY = -5.5f;
        private const float PerchY = 8.5f;
        private const float FallSpeed = 6f;
        private const float FirstDropDelay = 1.6f;
        private const float RespawnDelay = 2.2f;
        private const float FirstDiveDelay = 2.1f;
        private const float DiveInterval = 3.4f;
        private const float UnclaimedAirliftSeconds = 8f;
        private const float SuccessHoldSeconds = 1.15f;
        private const float ReactionHoldSeconds = 0.72f;

        private readonly CoopFeastGuardPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _chickObj;
        private TextMesh _chickLabel;
        private SpriteRenderer _chickFallbackRenderer;
        private MissionPropArtAttachment _chickArt;
        private string _currentChickArtPath;
        private GameObject _nestObj;
        private MissionPropArtAttachment _nestArt;
        private MissionPropArtAttachment _parentArt;
        private SpriteRenderer _parentFallbackRenderer;
        private bool _parentFallbackWasEnabled;
        private string _currentParentArtPath;
        private bool _chickFalling;
        private float _chickX;
        private float _chickY;
        private float _nextDropAt;
        private float _airliftAt;
        private float _nextDiveAt;
        private bool _failed;
        private float _successHoldRemaining;
        private float _chickReactionUntil;
        private string _chickReactionArtPath;
        private float _parentReactionUntil;

        public CoopFeastGuardPuzzle Puzzle => _puzzle;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.BabyBirdBedlam;
        public bool IsComplete => _puzzle.Solved && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _puzzle.Solved && _successHoldRemaining > 0f;
        public float SuccessHoldRemaining => _successHoldRemaining;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "Three pecks landed - the furious parent birds dive-bombed the dogs clean out of the yard."
            : null;
        public Vector2 EntryTarget => new(0f, GroundY);
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildFeastGuardSummary(_puzzle);

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "Feast complete! Cheddar got the gulps - Cocoa ruled the sky!";
                string tally = $"(chicks {_puzzle.ChicksEaten}/{ChicksNeeded}, pecks {_puzzle.Pecks}/{MaxPecks})";
                if (_puzzle.DiveActive)
                    return $"PARENT DIVING! Cocoa bark it off - Cheddar keep shaking {tally}";
                if (_puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Held)
                    return $"Cheddar shake it down (Tug x{_puzzle.Shakes}/{ShakesNeeded}); Cocoa watch the sky {tally}";
                if (_puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Grounded)
                    return $"Chick down! Cheddar grab it before the parents airlift it back {tally}";
                return $"Eat the fallen chicks before the parents strike back {tally}";
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
            _puzzle.Configure(ChicksNeeded, ShakesNeeded, MaxPecks, DiveWindowSeconds);
            _failed = false;
            _successHoldRemaining = 0f;
            _chickFalling = false;
            _nextDropAt = _context.Now() + FirstDropDelay;
            _airliftAt = 0f;
            _nextDiveAt = 0f;
            _chickReactionUntil = 0f;
            _chickReactionArtPath = string.Empty;
            _parentReactionUntil = 0f;
            _nestObj?.SetActive(true);
            _chickObj?.SetActive(false);
            StageParentAtPerch("PARENT BIRDS CIRCLING THE NEST");
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Solved)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            if (_failed) return;

            if (_parentReactionUntil > 0f && now >= _parentReactionUntil)
            {
                _parentReactionUntil = 0f;
                StageParentAtPerch("PARENT BIRDS CIRCLING THE NEST");
            }

            if (_chickFalling)
            {
                _chickY -= FallSpeed * deltaTime;
                if (_chickY <= GroundY)
                {
                    _chickY = GroundY;
                    _chickFalling = false;
                    if (_puzzle.ChickLanded())
                    {
                        _airliftAt = now + UnclaimedAirliftSeconds;
                        _context.SetCue("A chick tumbled out of the nest! Cheddar, grab it!");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CHICK DOWN!");
                        _context.SpawnWorldPop(new Vector2(_chickX, GroundY), "PLOP!", new Color(1f, 0.9f, 0.4f));
                        _context.LogEvent("BedlamChickLanded", $"x={_chickX:0.0}");
                        _context.LogObjectiveChanged();
                    }
                }
                UpdateChickVisuals();
                return;
            }

            switch (_puzzle.Chick)
            {
                case CoopFeastGuardPuzzle.ChickState.Grounded:
                    if (now >= _airliftAt) HandleAirlift();
                    break;
                case CoopFeastGuardPuzzle.ChickState.Held:
                    TickHeldChick(deltaTime, now);
                    break;
                default:
                    if (now >= _nextDropAt) BeginChickFall();
                    break;
            }
            UpdateChickVisuals();
        }

        public bool HandleBark(int dogIndex)
        {
            if (_puzzle.Solved || _failed) return false;
            if (!_puzzle.DiveActive) return false;
            if (dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;
            if (_context.IndexOfDog(DogId.Cocoa) != dogIndex)
            {
                _context.SetCue("Cheddar cannot bark with a chick in his mouth - Cocoa guards the sky!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "COCOA: BARK THE DIVE!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position, "MOUTH FULL!", new Color(1f, 0.72f, 0.25f));
                _context.MarkFailedInteraction(DogId.Cheddar, "Cheddar tried to bark with a chick in his mouth");
                return true;
            }
            var parent = _context.PredatorObject;
            if (parent == null) return false;
            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, parent.transform.position) > BarkRepelRange)
            {
                _context.SetCue("Cocoa's bark is too far away - get under the diving parent!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "GET UNDER THE DIVE!");
                _context.MarkFailedInteraction(DogId.Cocoa, "Cocoa barked outside the parent-repel range");
                return true;
            }
            HandleRepel(dogIndex);
            return true;
        }

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Solved || _failed) return false;
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length) return false;
            if (!_context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)) return false;

            if (identity.Id != DogId.Cheddar)
            {
                _context.SetCue("Cocoa leaves the chick for Cheddar and keeps watch on the parent birds.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CHEDDAR FEASTS - COCOA GUARDS!");
                if (_chickObj != null)
                    _context.SpawnWorldPop(_chickObj.transform.position, "NOT THE GUARD'S SNACK", new Color(0.55f, 0.82f, 1f));
                _context.MarkFailedInteraction(identity.Id, "Cocoa guards the sky - bark at diving parents instead");
                return true;
            }

            if (_puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Grounded)
            {
                if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, new Vector2(_chickX, GroundY)) > GrabRange)
                {
                    _context.MarkFailedInteraction(identity.Id, "get closer to the chick to grab it");
                    return true;
                }
                HandleGrab(dogIndex);
                return true;
            }

            if (_puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Held)
            {
                HandleShake(dogIndex);
                return true;
            }

            _context.MarkFailedInteraction(identity.Id, "no chick on the ground yet - watch the nest");
            return true;
        }

        public void Cleanup()
        {
            _nestObj?.SetActive(false);
            _chickObj?.SetActive(false);
            _chickReactionUntil = 0f;
            _parentReactionUntil = 0f;
            if (_parentArt != null) _parentArt.ClearOverride();
            if (_parentFallbackRenderer != null) _parentFallbackRenderer.enabled = _parentFallbackWasEnabled;
            _parentArt = null;
            _parentFallbackRenderer = null;
            _currentParentArtPath = string.Empty;
        }

        public void StageDogsForEntry()
        {
            // The shared staging parks the predator for RequiresPredator=false missions, so the
            // parent bird must be re-activated here, same as the eagle-shadow sweep.
            StageParentAtPerch("PARENT BIRDS CIRCLING THE NEST");
            if (_context.SquirrelObject != null) _context.SquirrelObject.SetActive(false);

            int a = _context.IndexOfDog(DogId.Cheddar);
            int b = _context.IndexOfDog(DogId.Cocoa);
            if (a >= 0) PlaceDog(a, new Vector2(-2f, GroundY));
            if (b >= 0) PlaceDog(b, new Vector2(2f, GroundY + 2f));
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 1.4f;

            bool isCheddar = _context.IndexOfDog(DogId.Cheddar) == dogIndex;
            if (isCheddar)
            {
                if (_puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Grounded && _chickObj != null && _chickObj.activeSelf)
                {
                    target = _chickObj.transform;
                    copy = "GRAB THE CHICK";
                    hideDistance = GrabRange;
                }
                else if (_chickFalling && _chickObj != null)
                {
                    target = _chickObj.transform;
                    copy = "CHICK INCOMING";
                    hideDistance = GrabRange;
                }
                else if (_puzzle.Chick != CoopFeastGuardPuzzle.ChickState.Held && _nestObj != null)
                {
                    // Between chicks the nest is the next thing worth staring at.
                    target = _nestObj.transform;
                    copy = "WATCH THE NEST";
                    hideDistance = 2f;
                }
                // While holding he IS the objective (shake in place) - no arrow needed.
            }
            else
            {
                if (_puzzle.DiveActive && _context.PredatorObject != null)
                {
                    target = _context.PredatorObject.transform;
                    copy = "BARK IT OFF!";
                    hideDistance = BarkRepelRange;
                }
                else
                {
                    int a = _context.IndexOfDog(DogId.Cheddar);
                    if (a >= 0)
                    {
                        target = _context.Dogs[a].transform;
                        copy = _puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Held ? "GUARD THE FEAST" : "GUARD THE SKY";
                        hideDistance = 3f;
                    }
                }
            }

            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("baby_bird_bedlam", score, timeRemaining, _puzzle.ChicksEaten, ChicksNeeded, _puzzle.Mistakes,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: land a chick at <paramref name="x"/> immediately.</summary>
        public void ForceChickLand(float x)
        {
            _chickFalling = false;
            _chickReactionUntil = 0f;
            _chickX = x;
            _chickY = GroundY;
            if (_puzzle.ChickLanded())
            {
                _airliftAt = _context.Now() + UnclaimedAirliftSeconds;
                UpdateChickVisuals();
            }
        }

        /// <summary>Test hook: begin a chick fall at a deterministic horizontal position.</summary>
        public void ForceChickFall(float x)
        {
            _chickX = x;
            _chickY = SpawnY;
            _chickFalling = true;
            _chickReactionUntil = 0f;
            _chickObj?.SetActive(true);
            UpdateChickVisuals();
        }

        /// <summary>Test hook: Cheddar grabs the grounded chick, skipping the range check.</summary>
        public void ForceChickGrab()
        {
            int dogIndex = _context.IndexOfDog(DogId.Cheddar);
            if (dogIndex >= 0 && _puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Grounded) HandleGrab(dogIndex);
        }

        /// <summary>Test hook: one shake of the held chick (the last one gulps it).</summary>
        public void ForceChickShake()
        {
            int dogIndex = _context.IndexOfDog(DogId.Cheddar);
            if (dogIndex >= 0 && _puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Held) HandleShake(dogIndex);
        }

        /// <summary>Test hook: a parent bird commits to a dive right now.</summary>
        public void ForceParentDive()
        {
            if (_puzzle.StartDive()) AnnounceDive();
        }

        /// <summary>Test hook: Cocoa repels the active dive, skipping the range check.</summary>
        public void ForceParentRepel()
        {
            int dogIndex = _context.IndexOfDog(DogId.Cocoa);
            if (dogIndex >= 0 && _puzzle.DiveActive) HandleRepel(dogIndex);
        }

        /// <summary>Test hook: run the dive clock; an expired window lands the peck.</summary>
        public void ForceDiveAdvance(float seconds)
        {
            int pecksBefore = _puzzle.Pecks;
            _puzzle.Advance(seconds);
            if (_puzzle.Pecks > pecksBefore) HandlePeck();
        }

        /// <summary>Test hook: the parents airlift the unclaimed grounded chick back to the nest.</summary>
        public void ForceChickAirlift()
        {
            if (_puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Grounded) HandleAirlift();
        }

        /// <summary>Deterministic seam for advancing past the live feast payoff.</summary>
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        private void TickHeldChick(float deltaTime, float now)
        {
            int held = _context.IndexOfDog(DogId.Cheddar);
            if (held >= 0)
            {
                Vector2 snout = (Vector2)_context.Dogs[held].transform.position + new Vector2(0.9f, 0.2f);
                _chickX = snout.x;
                _chickY = snout.y;
            }

            if (!_puzzle.DiveActive)
            {
                if (_nextDiveAt <= 0f) _nextDiveAt = now + FirstDiveDelay;
                if (now >= _nextDiveAt && _puzzle.StartDive()) AnnounceDive();
                return;
            }

            // Swoop the parent bird from the perch toward the busy eater as the window closes.
            var parent = _context.PredatorObject;
            if (parent != null && held >= 0)
            {
                Vector2 dogPos = _context.Dogs[held].transform.position;
                float progress = 1f - Mathf.Clamp01(_puzzle.DiveTimeLeft / DiveWindowSeconds);
                Vector2 perch = new Vector2(dogPos.x, PerchY);
                parent.transform.position = Vector2.Lerp(perch, dogPos + Vector2.up * 0.8f, progress);
                SetParentArt(progress < 0.2f
                    ? FinalGameplayArt.BabyBirdMotherWarning
                    : FinalGameplayArt.BabyBirdMotherAttacking);
            }

            int pecksBefore = _puzzle.Pecks;
            _puzzle.Advance(deltaTime);
            if (_puzzle.Pecks > pecksBefore) HandlePeck();
        }

        private void BeginChickFall()
        {
            var rng = _context.Random();
            _chickX = Mathf.Lerp(_context.Bounds.xMin + 3f, _context.Bounds.xMax - 3f, (float)rng.NextDouble());
            _chickY = SpawnY;
            _chickFalling = true;
            _chickReactionUntil = 0f;
            SetChickArt(FinalGameplayArt.BabyBirdChickFalling);
            if (_chickObj != null)
            {
                _chickObj.SetActive(true);
                _chickObj.transform.position = new Vector3(_chickX, _chickY, 0f);
            }
            _context.SetCue("Incoming! Another chick is tumbling out of the nest.");
            _context.LogEvent("BedlamChickDrop", $"x={_chickX:0.0}");
        }

        private void HandleGrab(int dogIndex)
        {
            if (!_puzzle.Grab()) return;
            _context.AddScore(ScoreEventCatalog.ChickNabbed.Points, ScoreEventCatalog.ChickNabbed.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SoloBark);
            _context.SetCue($"Cheddar nabbed the chick! Shake it (Tug x{ShakesNeeded}) - Cocoa, watch for diving parents!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "NABBED!");
            _context.SpawnWorldPop(new Vector2(_chickX, GroundY), "NABBED!", new Color(1f, 0.9f, 0.4f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            _context.RequestRumble("bedlam_grab", 0.12f, 0.25f, 0.1f);
            _context.LogEvent("BedlamChickGrabbed", $"chick {_puzzle.ChicksEaten + 1}/{ChicksNeeded}");
            _nextDiveAt = _context.Now() + FirstDiveDelay;
            SetChickArt(FinalGameplayArt.BabyBirdChickGrabbed, true);
            _context.LogObjectiveChanged();
        }

        private void HandleShake(int dogIndex)
        {
            int eatenBefore = _puzzle.ChicksEaten;
            bool diveWasActive = _puzzle.DiveActive;
            if (!_puzzle.Shake()) return;

            if (_puzzle.ChicksEaten > eatenBefore)
            {
                _context.CreditDog(dogIndex);
                _context.AddScore(ScoreEventCatalog.ChickGulped.Points, ScoreEventCatalog.ChickGulped.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                _context.SetCue(diveWasActive
                    ? $"GULP - swallowed it mid-dive! The parent pulled up with nothing to save. ({_puzzle.ChicksEaten}/{ChicksNeeded})"
                    : $"GULP! Down the hatch, feathers and all. ({_puzzle.ChicksEaten}/{ChicksNeeded})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "GULP!");
                _context.SpawnWorldPop(new Vector2(_chickX, _chickY), "GULP!", new Color(0.5f, 0.95f, 0.55f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.EatingGulp);
                _context.RequestRumble("bedlam_gulp", 0.25f, 0.5f, 0.18f);
                _context.LogEvent("BedlamChickGulped", $"{_puzzle.ChicksEaten}/{ChicksNeeded}");
                ShowChickReaction(FinalGameplayArt.BabyBirdChickGulped);
                _nextDropAt = _context.Now() + RespawnDelay;
                _nextDiveAt = 0f;
                if (diveWasActive) StageParentAtPerch("PARENT BIRD PULLED UP - TOO SLOW!");
                if (_puzzle.Solved) CompleteFeast();
                else _context.LogObjectiveChanged();
                return;
            }

            _context.SetFeedback(GameManager.FeedbackKind.SoloBark);
            _context.SetCue($"Shake shake shake! ({_puzzle.Shakes}/{ShakesNeeded})");
            _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "SHAKE!");
            _context.SpawnWorldPop(new Vector2(_chickX, _chickY), "SHAKE!", new Color(1f, 0.8f, 0.3f));
            _context.RequestRumble("bedlam_shake", 0.15f, 0.3f, 0.08f);
            _context.LogEvent("BedlamShake", $"{_puzzle.Shakes}/{ShakesNeeded}");
            SetChickArt(FinalGameplayArt.BabyBirdChickShaking, true);
        }

        private void AnnounceDive()
        {
            var parent = _context.PredatorObject;
            if (parent != null)
            {
                _context.SetActorState(parent, "PARENT BIRD DIVE - SNATCH INBOUND!", new Color(0.85f, 0.2f, 0.15f), 0.4f);
                _context.SpawnWorldPop(parent.transform.position, "DIVING!", new Color(1f, 0.3f, 0.2f));
                SetParentArt(FinalGameplayArt.BabyBirdMotherWarning, true);
            }
            _context.SetFeedback(GameManager.FeedbackKind.PredatorAttack);
            _context.SetCue("A parent bird is DIVING at Cheddar - Cocoa, get under it and BARK!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "DIVE!");
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("bedlam_dive", 0.2f, 0.4f, 0.15f);
            _context.LogEvent("BedlamParentDive", $"pecks {_puzzle.Pecks}/{MaxPecks}");
            _context.LogObjectiveChanged();
        }

        private void HandleRepel(int dogIndex)
        {
            if (!_puzzle.Repel()) return;
            _context.CreditDog(dogIndex);
            _context.AddScore(ScoreEventCatalog.ParentRepelled.Points, ScoreEventCatalog.ParentRepelled.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue("Cocoa's bark drove the parent bird off! Keep shaking, Cheddar!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "REPELLED!");
            var parent = _context.PredatorObject;
            if (parent != null)
                _context.SpawnWorldPop(parent.transform.position, "REPELLED!", new Color(0.5f, 0.95f, 0.55f));
            ShowParentReaction(FinalGameplayArt.BabyBirdMotherRepelled,
                "PARENT DRIVEN OFF - REGROUPING", new Vector2(0f, PerchY));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("bedlam_repel", 0.2f, 0.45f, 0.15f);
            _context.LogEvent("BedlamParentRepelled", $"repels {_puzzle.Repels}");
            _nextDiveAt = _context.Now() + DiveInterval;
            _context.LogObjectiveChanged();
        }

        private void HandlePeck()
        {
            _context.AddScore(ScoreEventCatalog.ParentPeck.Points, ScoreEventCatalog.ParentPeck.Label);
            _context.SetFeedback(GameManager.FeedbackKind.PredatorAttack);
            _context.SetCue($"PECKED! Cheddar dropped the chick and it fluttered home. ({_puzzle.Pecks}/{MaxPecks})");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "PECKED!");
            _context.SpawnWorldPop(new Vector2(_chickX, _chickY), "PECKED!", new Color(1f, 0.3f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            _context.RequestRumble("bedlam_peck", 0.3f, 0.55f, 0.2f);
            _context.RequestShake(0.18f);
            _context.LogEvent("BedlamPeck", $"{_puzzle.Pecks}/{MaxPecks}");
            ShowChickReaction(FinalGameplayArt.BabyBirdChickPecked);
            _nextDropAt = _context.Now() + RespawnDelay;
            _nextDiveAt = 0f;
            ShowParentReaction(FinalGameplayArt.BabyBirdMotherAttacking,
                "ANGRY MOTHER LANDS THE PECK!", new Vector2(_chickX, _chickY + 1.2f));
            if (_puzzle.Overrun) _failed = true;
            else _context.LogObjectiveChanged();
        }

        private void HandleAirlift()
        {
            if (!_puzzle.AirliftUnclaimed()) return;
            _context.AddScore(ScoreEventCatalog.ChickAirlifted.Points, ScoreEventCatalog.ChickAirlifted.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue("Too slow - a parent airlifted the chick back to the nest!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "AIRLIFTED!");
            _context.SpawnWorldPop(new Vector2(_chickX, GroundY), "AIRLIFTED!", new Color(0.85f, 0.6f, 0.3f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelEscapeLaugh);
            _context.LogEvent("BedlamAirlift", $"airlifts {_puzzle.Airlifts}");
            _chickObj?.SetActive(false);
            ShowParentReaction(FinalGameplayArt.BabyBirdMotherRescue,
                "ANGRY MOTHER RESCUES HER CHICK", new Vector2(_chickX, GroundY + 2f));
            _nextDropAt = _context.Now() + RespawnDelay;
            _context.LogObjectiveChanged();
        }

        private void CompleteFeast()
        {
            _successHoldRemaining = SuccessHoldSeconds;
            _context.AddScore(ScoreEventCatalog.NestFeastComplete.Points, ScoreEventCatalog.NestFeastComplete.Label);
            _context.SetFeedback(GameManager.FeedbackKind.LevelClear);
            _context.SetCue("Feast complete! The dogs waddle off, full of chicks; the parents file a formal complaint.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "FEAST COMPLETE!");
            _context.SpawnWorldPop(Vector2.up * (GroundY + 1.2f), "FULL BELLIES!", new Color(1f, 0.86f, 0.28f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
            _context.RequestRumble("bedlam_feast", 0.3f, 0.55f, 0.2f);
            var parent = _context.PredatorObject;
            if (parent != null)
            {
                _context.SetActorState(parent, "PARENT BIRDS GIVE UP - NEST DEFEATED", Color.gray, 0.1f);
                parent.transform.position = new Vector2(0f, PerchY);
                _parentReactionUntil = 0f;
                SetParentArt(FinalGameplayArt.BabyBirdMotherDefeated, true);
            }
            _context.LogEvent("BedlamFeastComplete", $"{_puzzle.ChicksEaten}/{ChicksNeeded}");
            _context.LogEvent("BedlamPayoff", "Full-bellies waddle; holding live-world success beat");
        }

        private void StageParentAtPerch(string stateLabel)
        {
            var parent = _context.PredatorObject;
            if (parent == null) return;
            parent.SetActive(true);
            parent.transform.position = new Vector2(0f, PerchY);
            _context.SetActorState(parent, stateLabel, new Color(0.35f, 0.3f, 0.45f), 0.28f);
            SetParentArt(FinalGameplayArt.BabyBirdMotherCircling);
        }

        private void UpdateChickVisuals()
        {
            if (_chickObj == null) return;
            bool reactionVisible = _chickReactionUntil > _context.Now();
            bool visible = reactionVisible || _chickFalling || _puzzle.Chick != CoopFeastGuardPuzzle.ChickState.None;
            if (_chickObj.activeSelf != visible) _chickObj.SetActive(visible);
            if (!visible) return;
            _chickObj.transform.position = new Vector3(_chickX, _chickY, 0f);
            if (reactionVisible)
            {
                SetChickArt(_chickReactionArtPath);
            }
            else if (_chickFalling)
            {
                SetChickArt(FinalGameplayArt.BabyBirdChickFalling);
            }
            else if (_puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Grounded)
            {
                SetChickArt(FinalGameplayArt.BabyBirdChickGrounded);
            }
            else if (_puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Held)
            {
                SetChickArt(_puzzle.Shakes > 0
                    ? FinalGameplayArt.BabyBirdChickShaking
                    : FinalGameplayArt.BabyBirdChickGrabbed);
            }
            if (_chickLabel != null)
            {
                _chickLabel.text = _chickFalling ? "CHICK!"
                    : _puzzle.Chick == CoopFeastGuardPuzzle.ChickState.Held ? $"SHAKE {_puzzle.Shakes}/{ShakesNeeded}"
                    : "GRAB IT!";
            }
        }

        private void BuildScene()
        {
            _nestObj = new GameObject("BedlamNest");
            _nestObj.transform.position = new Vector3(0f, PerchY + 1.2f, 0f);
            _nestObj.transform.localScale = new Vector3(3.2f, 1.4f, 1f);
            var nsr = _nestObj.AddComponent<SpriteRenderer>();
            if (_context.ActorSprite != null) nsr.sprite = _context.ActorSprite;
            nsr.color = new Color(0.5f, 0.35f, 0.18f);
            nsr.sortingOrder = 2;
            _context.AddWorldLabel(_nestObj, "THE NEST", Vector3.up * 1.1f, 12, Color.white);
            // Reuse the authored shade-tree silhouette as the oak beneath the controller-owned
            // nest. It makes the first objective readable before the first chick drops without
            // adding collision or a new gameplay target.
            _nestArt = MissionPropArt.Attach(_nestObj, FinalGameplayArt.EnvironmentShadeTree,
                Vector3.one * 0.018f, 4, Color.white, new Vector3(0f, -4.6f, 0.08f), false);
            _nestArt?.SetProximityAffordance(false);
            _nestObj.SetActive(false);

            _chickObj = new GameObject("BedlamChick");
            _chickObj.transform.position = new Vector3(0f, SpawnY, 0f);
            _chickObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            _chickFallbackRenderer = _chickObj.AddComponent<SpriteRenderer>();
            if (_context.ActorSprite != null) _chickFallbackRenderer.sprite = _context.ActorSprite;
            _chickFallbackRenderer.color = new Color(1f, 0.9f, 0.35f);
            _chickFallbackRenderer.sortingOrder = 6;
            _chickArt = MissionPropArt.Attach(_chickObj, FinalGameplayArt.BabyBirdChickFalling,
                Vector3.one * 0.72f, 8, Color.white, Vector3.zero, false);
            if (_chickArt != null)
            {
                _chickArt.SetProximityAffordance(false);
                _chickFallbackRenderer.enabled = false;
                _currentChickArtPath = FinalGameplayArt.BabyBirdChickFalling;
            }
            _chickLabel = _context.AddWorldLabel(_chickObj, "CHICK!", Vector3.up * 1.1f, 11, Color.white);
            _chickObj.SetActive(false);
        }

        private void ShowChickReaction(string resourcePath)
        {
            _chickReactionArtPath = resourcePath;
            _chickReactionUntil = _context.Now() + ReactionHoldSeconds;
            _chickObj?.SetActive(true);
            SetChickArt(resourcePath, true);
        }

        private void SetChickArt(string resourcePath, bool pulse = false)
        {
            if (_chickArt == null || string.IsNullOrEmpty(resourcePath)) return;
            if (_currentChickArtPath != resourcePath && _chickArt.SetResource(resourcePath))
                _currentChickArtPath = resourcePath;
            if (pulse) _chickArt.Pulse(0.22f, 0.09f);
        }

        private void ShowParentReaction(string resourcePath, string stateLabel, Vector2 position)
        {
            var parent = _context.PredatorObject;
            if (parent == null) return;
            parent.SetActive(true);
            parent.transform.position = position;
            _context.SetActorState(parent, stateLabel, Color.white, 0.32f);
            SetParentArt(resourcePath, true);
            _parentReactionUntil = _context.Now() + ReactionHoldSeconds;
        }

        private void SetParentArt(string resourcePath, bool pulse = false)
        {
            var parent = _context.PredatorObject;
            if (parent == null || string.IsNullOrEmpty(resourcePath)) return;
            // GameManager clears shared-actor overlays after controller StartMission and before
            // StageDogsForEntry. Treat that cleared attachment as uninitialized so Bedlam can
            // reapply its mother sprite without adding a mission-specific manager branch.
            if (_parentArt != null && string.IsNullOrEmpty(_parentArt.ResourcePath))
            {
                _parentArt = null;
                _parentFallbackRenderer = null;
                _currentParentArtPath = string.Empty;
            }
            if (_parentArt == null)
            {
                _parentFallbackRenderer = MissionPropArt.FindFallbackRenderer(parent);
                if (_parentFallbackRenderer != null)
                {
                    _parentFallbackWasEnabled = _parentFallbackRenderer.enabled;
                    _parentFallbackRenderer.enabled = false;
                }
                _parentArt = MissionPropArt.Attach(parent, resourcePath, Vector3.one * 0.74f,
                    32, Color.white, Vector3.zero, false);
                _parentArt?.SetProximityAffordance(false);
                if (_parentArt != null) _currentParentArtPath = resourcePath;
                else if (_parentFallbackRenderer != null) _parentFallbackRenderer.enabled = _parentFallbackWasEnabled;
            }
            else if (_currentParentArtPath != resourcePath && _parentArt.SetResource(resourcePath))
            {
                _currentParentArtPath = resourcePath;
            }
            if (pulse) _parentArt?.Pulse(0.24f, 0.1f);
        }

        private void PlaceDog(int index, Vector2 point)
        {
            const float margin = 1.5f;
            _context.Dogs[index].transform.position = new Vector2(
                Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
                Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
            if (_context.Dogs[index].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }
    }
}
