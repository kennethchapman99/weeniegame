using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Eagle Shadow Panic: hide from the sweeping shadow in cover zones, survive
    /// the talon snatch/rescue timing beat, then drive the eagle off with a united-front bark.
    /// Uses the shared predator actor as the sweeping shadow and the shared squirrel actor as the
    /// talon-grip rescue marker, both through the narrow context.
    /// </summary>
    public sealed class EagleShadowPanicMissionController :
        IMissionController, IMissionInteractionController, IMissionUnitedBarkListener,
        IMissionSuccessPresentationController
    {
        private const int SweepCount = 4;
        private const int RequiredHides = 2;
        private const int MaxExposures = 3;
        private const int RescuePullsNeeded = 3;
        private const float RescueWindowSeconds = 1.2f; // a wiggle cracks the grip open for this long
        private const float RescueRange = 3.5f;         // the free dog must be this close to pull
        private const float CoverRadius = 3f;
        // Y the eagle shadow sweeps along: inside the dogs' play band (cover zones sit in the lower
        // and upper thirds) so the sweep visibly crosses over the dogs instead of the far top fence.
        private const float SweepHeight = 0.5f;
        private const float SuccessHoldSeconds = 1.15f;

        private readonly ThreatSweepMissionState _state = new ThreatSweepMissionState();
        // Rescue phase (Rescue-Timing co-op puzzle): after the hides, the eagle SNATCHES Cheddar
        // into its talons. The held dog (Cheddar) wiggles (Tug/Rescue button) to crack the grip and
        // open a brief window; the free dog (Cocoa) pulls in that window to yank him down. Pulling
        // with no window open is a mistimed miss (recoverable).
        private readonly CoopRescueTimingPuzzle _rescue = new CoopRescueTimingPuzzle();
        private MissionContext _context;
        private Vector2[] _coverZones;
        private GameObject[] _coverMarkers;
        private Vector2 _snatchPosition;
        private int _pullsSeen;
        private int _missesSeen;
        private int _sweepDir = 1;
        private float _successHoldRemaining;

        public ThreatSweepMissionState SweepState => _state;
        public CoopRescueTimingPuzzle RescuePuzzle => _rescue;
        public Vector2[] CoverZones => (Vector2[])_coverZones.Clone();
        public Vector2 SnatchPosition => _snatchPosition;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.EagleShadowPanic;
        public bool IsComplete => _state.UnitedFrontComplete && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _state.UnitedFrontComplete && _successHoldRemaining > 0f;
        public bool IsFailed => _state.TooManyExposures(MaxExposures);
        public string FailReason => IsFailed ? "The eagle shadow caught the dogs in the open one too many times." : null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildThreatSweepSummary(_state);
        public Vector2 EntryTarget => _coverZones[0];

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome) return "Eagle retreating! Cheddar and Cocoa hold the yard together.";
                if (_state.RescueComplete) return "United-front bark circle: huddle close and bark together";
                if (_state.RescueObjectiveActive) return $"Eagle snatched Cheddar! Cheddar wiggle (Tug/Rescue), Cocoa pull in the window (pulls {_rescue.Pulls}/{_rescue.PullsNeeded})";
                return $"Hide from the eagle shadow: safe hides {_state.SafeHides}/{RequiredHides}, exposures {_state.Exposures}/{MaxExposures}";
            }
        }

        /// <summary>Cover-zone geometry derived from the arena bounds, usable without an active controller.</summary>
        public static Vector2[] ComputeCoverZones(Rect bounds)
        {
            Vector2 P(float x, float y) => new Vector2(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            return new[] { P(-0.7f, -0.64f), P(0.7f, -0.64f), P(0f, 0.68f) };
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _coverZones = ComputeCoverZones(context.Bounds);
            BuildCoverMarkers();
            Cleanup();
        }

        public void StartMission()
        {
            _state.Reset();
            _rescue.Reset();
            _pullsSeen = 0;
            _missesSeen = 0;
            _sweepDir = 1;
            _successHoldRemaining = 0f;
            // Keep the snatch/rescue point inside the play band so the rescue beat is on-screen.
            _snatchPosition = new Vector2(0f, 6f);
            SetCoverMarkersActive(true);
            // A previous attempt can leave every cover zone showing the last sweep's Safe/Spotted
            // art; reset to Safe so a fresh attempt doesn't open already looking exposed.
            SetCoverArt(FinalGameplayArt.EagleShadowCoverSafe);
            UpdateCoverSignals();
        }

        public void Tick(float deltaTime, float now)
        {
            UpdateCoverSignals();
            var predator = _context.PredatorObject;
            if (predator == null) return;
            if (_state.UnitedFrontComplete)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            // Rescue phase: the eagle has snatched Cheddar - drive the wiggle/pull timing instead of sweeping.
            if (_state.RescueObjectiveActive && !_state.RescueComplete) { TickRescue(deltaTime); return; }
            if (_state.RescueComplete) return; // freed; united-front phase, dogs roam

            var pos = predator.transform.position;
            float limit = _context.Bounds.xMax - 1.5f;
            pos.x += _sweepDir * deltaTime * (_context.SquirrelMoveSpeed * 1.4f);
            if (pos.x >= limit) { pos.x = limit; _sweepDir = -1; predator.transform.position = pos; EvaluateSweep(); return; }
            if (pos.x <= -limit) { pos.x = -limit; _sweepDir = 1; predator.transform.position = pos; EvaluateSweep(); return; }
            predator.transform.position = pos;
        }

        public bool HandleBark(int dogIndex) => false;

        public void OnUnitedBark()
        {
            if (_state.ReadyForUnitedFront) CompleteUnitedFront();
        }

        public bool HandleInteract(int dogIndex)
        {
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length) return false;
            if (!_context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)) return false;
            TryCompleteRescue(identity.Id);
            return true;
        }

        public void Cleanup() => SetCoverMarkersActive(false);

        public void StageDogsForEntry()
        {
            // The shared round staging deactivates and parks the predator for RequiresPredator=false
            // missions before this hook runs, so the sweep staging must happen here.
            var predator = _context.PredatorObject;
            if (predator != null)
            {
                predator.SetActive(true);
                // Sweep across the dogs' play band (around the cover zones) instead of along the far
                // top fence, so the shadow is actually seen passing overhead. Exposure stays x-column based.
                predator.transform.position = new Vector2(-(_context.Bounds.xMax - 1.5f), SweepHeight);
                _context.SetActorState(predator, "EAGLE SHADOW SWEEP - HIDE IN COVER!", new Color(0.16f, 0.16f, 0.2f), 0.3f);
            }
            // The talon-grip indicator (reuses the squirrel actor) only appears once a dog is snatched.
            if (_context.SquirrelObject != null) _context.SquirrelObject.SetActive(false);

            Vector2 entry = EntryTarget;
            Vector2 inward = (_context.Bounds.center - entry).normalized;
            if (inward.sqrMagnitude < 0.01f) inward = Vector2.down;
            Vector2 center = entry + inward * 7f;
            Vector2 side = new Vector2(-inward.y, inward.x) * 1.5f;
            for (int i = 0; i < _context.Dogs.Length; i++)
            {
                Vector2 offset = i % 2 == 0 ? -side : side;
                _context.Dogs[i].transform.position = ClampInsideBounds(center + offset, 1.5f);
                if (_context.Dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            }
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 1.4f;

            if (_state.RescueObjectiveActive && !_state.RescueComplete)
            {
                // Cheddar is the one snatched (wiggle in place); Cocoa is pointed at the talons to pull.
                if (_context.IndexOfDog(DogId.Cheddar) == dogIndex)
                {
                    target = null;
                    copy = "WIGGLE!";
                }
                else
                {
                    target = _context.SquirrelObject != null ? _context.SquirrelObject.transform : null;
                    copy = "PULL HIM FREE";
                    hideDistance = RescueRange;
                }
            }
            else if (_state.RescueComplete)
            {
                target = _context.Dogs[dogIndex == 0 ? 1 : 0].transform;
                copy = "HUDDLE + BARK";
                hideDistance = 1.6f;
            }
            else
            {
                target = FindNearestActiveCover(_context.Dogs[dogIndex].transform.position);
                copy = "HIDE HERE";
            }

            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome)
        {
            int progress = _state.SafeHides + (_state.RescueComplete ? 1 : 0) + (_state.UnitedFrontComplete ? 1 : 0);
            return new MissionRuntimeSnapshot("eagle_shadow_panic", score, timeRemaining, progress,
                RequiredHides + 2, _state.Exposures,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);
        }

        /// <summary>Test hook: register a clean safe hide (may open the snatch/rescue beat).</summary>
        public void ForceSafeHide() => RegisterSafeHide();

        /// <summary>Test hook: register an exposure (three end the mission).</summary>
        public void ForceExposure() => RegisterExposure();

        /// <summary>Test hook: run wiggle+pull cycles until the snatched dog is freed.</summary>
        public void ForceRescue()
        {
            if (!_state.RescueObjectiveActive || _state.RescueComplete) return;
            int guard = 0;
            while (!_rescue.Freed && guard++ < 50)
            {
                _rescue.Wiggle();   // crack the grip open
                _rescue.Pull();     // and pull within the window
                HandleRescueProgress();
            }
        }

        /// <summary>Test hook: complete the united-front bark circle when it is ready.</summary>
        public void ForceUnitedFront() => OnUnitedBark();
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        /// <summary>Test hook: evaluate one shadow sweep pass at the current positions.</summary>
        public void ForceSweepPass() => EvaluateSweep();

        /// <summary>Test hook: the resource path currently attached to cover marker <paramref name="index"/>.</summary>
        public string CoverResourcePathAt(int index)
        {
            if (_coverMarkers == null || index < 0 || index >= _coverMarkers.Length || _coverMarkers[index] == null) return string.Empty;
            var attachment = _coverMarkers[index].GetComponent<MissionPropArtAttachment>();
            return attachment != null ? attachment.ResourcePath : string.Empty;
        }

        /// <summary>Test hook: the snatched dog wiggles to crack the grip open.</summary>
        public void ForceWiggle()
        {
            _rescue.Wiggle();
            UpdateRescueVisuals();
        }

        /// <summary>Test hook: the free dog pulls; only counts while the wiggle window is open.</summary>
        public void ForcePull()
        {
            _rescue.Pull();
            HandleRescueProgress();
        }

        /// <summary>Test hook: let the cracked grip re-tighten (the wiggle window closes).</summary>
        public void ForceRescueAdvance(float seconds)
        {
            _rescue.Advance(seconds);
            UpdateRescueVisuals();
        }

        // One completed pass crosses the whole play band. Both dogs must actually be tucked into
        // cover when it resolves; checking only the eagle's endpoint column lets open-ground dogs
        // earn free hides after the shadow has already passed them.
        private void EvaluateSweep()
        {
            if (_state.RescueObjectiveActive || _state.RescueComplete) return;

            bool exposed = false;
            if (_context.Dogs != null)
            {
                foreach (var dog in _context.Dogs)
                {
                    bool inCover = NearestCoverDistance(dog.transform.position) < CoverRadius;
                    if (!inCover) { exposed = true; break; }
                }
            }

            if (exposed) RegisterExposure();
            else RegisterSafeHide();
        }

        private void RegisterSafeHide()
        {
            if (_state.RescueComplete) return;

            _state.AddSafeHide();
            _state.AdvanceSweep(SweepCount);
            for (int i = 0; i < _context.Dogs.Length; i++)
                if (_context.Dogs[i] != null) _context.CreditDog(i);
            _context.AddScore(ScoreEventCatalog.SafeHide.Points, ScoreEventCatalog.SafeHide.Label);
            _context.SetFeedback(GameManager.FeedbackKind.PredatorHuddle);
            _context.SetCue("Safe in cover! The eagle shadow swept past.");
            SetCoverArt(FinalGameplayArt.EagleShadowCoverSafe);
            _context.SetActorState(_context.PredatorObject, $"SHADOW SWEEP {_state.SweepIndex + 1} - HIDES {_state.SafeHides}/{RequiredHides}", new Color(0.16f, 0.16f, 0.2f), 0.28f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.SafeHide.Label);
            if (_context.PredatorObject != null)
                _context.SpawnWorldPop(_context.PredatorObject.transform.position, "SAFE HIDE!", new Color(0.55f, 0.85f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("eagle_safe_hide", 0.1f, 0.2f, 0.1f);
            _context.LogEvent("EagleSafeHide", $"hides {_state.SafeHides}/{RequiredHides}");

            if (_state.ReadyForRescue(RequiredHides))
            {
                StartSnatchRescue();
            }

            UpdateCoverSignals();
            _context.LogObjectiveChanged();
        }

        private void RegisterExposure()
        {
            _state.AddExposure();
            _state.AdvanceSweep(SweepCount);
            _context.AddScore(ScoreEventCatalog.FakeOut.Points, "EAGLE SPOOK");
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"Caught in the open! The eagle shadow spotted a dog ({_state.Exposures}/{MaxExposures}).");
            SetCoverArt(FinalGameplayArt.EagleShadowCoverSpotted);
            _context.SetActorState(_context.PredatorObject, $"SPOTTED! EXPOSURE {_state.Exposures}/{MaxExposures}", new Color(0.85f, 0.12f, 0.12f), 0.4f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "EAGLE SPOOK!");
            if (_context.PredatorObject != null)
                _context.SpawnWorldPop(_context.PredatorObject.transform.position, "SPOTTED!", new Color(1f, 0.3f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("eagle_exposure", 0.2f, 0.42f, 0.16f);
            _context.LogEvent("EagleExposure", $"exposures {_state.Exposures}/{MaxExposures}");
            if (!_state.TooManyExposures(MaxExposures)) _context.LogObjectiveChanged();
        }

        private void StartSnatchRescue()
        {
            _state.StartRescue();
            _rescue.Configure(RescuePullsNeeded, RescueWindowSeconds);
            _pullsSeen = 0;
            _missesSeen = 0;
            // The eagle swoops to the snatch point with Cheddar in its talons.
            if (_context.PredatorObject != null)
                _context.PredatorObject.transform.position = _snatchPosition + Vector2.up * 1.2f;
            if (_context.SquirrelObject != null)
            {
                _context.SquirrelObject.SetActive(true);
                _context.SquirrelObject.transform.position = _snatchPosition;
            }
            _context.AddScore(150, "SHADOW DISTRACTED");
            _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
            _context.SetCue("The eagle SNATCHED Cheddar! Cheddar: wiggle (Tug/Rescue) to crack the grip. Cocoa: get close and pull him free in the window!");
            UpdateRescueVisuals();
            _context.LogEvent("EagleSnatch", "eagle snatched Cheddar");
        }

        private void TickRescue(float deltaTime)
        {
            int held = _context.IndexOfDog(DogId.Cheddar);
            // Pin the snatched dog in the talons; the eagle hovers just above.
            if (held >= 0)
            {
                _context.Dogs[held].transform.position = _snatchPosition;
                if (_context.Dogs[held].TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
            if (_context.PredatorObject != null)
                _context.PredatorObject.transform.position = _snatchPosition + Vector2.up * 1.2f;

            _rescue.Advance(deltaTime); // the cracked grip re-tightens as the window closes
            UpdateRescueVisuals();
        }

        private void UpdateRescueVisuals()
        {
            // Both rescue phases demand action before the grip re-tightens, so they pulse in the
            // urgency channel (0.26+): the closed grip raises a warning badge for Cheddar's wiggle,
            // the cracked window a command badge for Cocoa's pull.
            if (_context.SquirrelObject != null)
                _context.SetActorState(_context.SquirrelObject,
                    _rescue.WindowOpen ? "GRIP CRACKED - COCOA PULL NOW!" : "TALON GRIP - CHEDDAR WIGGLE!",
                    _rescue.WindowOpen ? new Color(0.45f, 1f, 0.55f) : new Color(0.85f, 0.5f, 0.5f), 0.3f);
            SetMissionProp(_context.SquirrelObject,
                _rescue.WindowOpen ? FinalGameplayArt.EagleShadowTalonGripOpen : FinalGameplayArt.EagleShadowTalonGripClosed,
                0.013f, 31);
        }

        private void HandleRescueProgress()
        {
            if (_rescue.Pulls > _pullsSeen)
            {
                _pullsSeen = _rescue.Pulls;
                int cocoa = _context.IndexOfDog(DogId.Cocoa);
                if (cocoa >= 0) _context.CreditDog(cocoa);
                _context.AddScore(ScoreEventCatalog.SafeHide.Points, "GOOD PULL");
                _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
                _context.SetCue($"Heave! Cocoa cracked him loose a bit more. ({_rescue.Pulls}/{_rescue.PullsNeeded})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "HEAVE!");
                _context.SpawnWorldPop(_snatchPosition, "HEAVE!", new Color(0.5f, 0.95f, 0.55f));
                _context.LogEvent("EagleRescuePull", $"{_rescue.Pulls}/{_rescue.PullsNeeded}");
            }

            if (_rescue.MissedPulls > _missesSeen)
            {
                _missesSeen = _rescue.MissedPulls;
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                _context.SetCue("Mistimed pull - wait for Cheddar's wiggle to crack the grip first!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "MISTIMED!");
                _context.SpawnWorldPop(_snatchPosition, "TOO SOON!", new Color(1f, 0.72f, 0.25f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
                _context.LogEvent("EagleRescueMiss", $"{_rescue.MissedPulls}");
            }

            if (_rescue.Freed && !_state.RescueComplete) CompleteSnatchRescue();
        }

        private void CompleteSnatchRescue()
        {
            _state.CompleteRescue();
            int held = _context.IndexOfDog(DogId.Cheddar);
            if (held >= 0) _context.CreditDog(held);
            _context.AddScore(ScoreEventCatalog.ToyRescued.Points, "PARTNER RESCUED");
            _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
            _context.SetCue("Cocoa yanked Cheddar free of the talons! Now form the united-front bark circle.");
            if (_context.PredatorObject != null)
                _context.PredatorObject.transform.position = new Vector2(
                    0f, Mathf.Min(_context.Bounds.yMax - 2f, _snatchPosition.y + 6f));
            if (_context.SquirrelObject != null)
                _context.SetActorState(_context.SquirrelObject, "CHEDDAR'S FREE! HUDDLE FOR THE UNITED FRONT!", new Color(0.45f, 1f, 0.65f), 0.12f);
            SetMissionProp(_context.SquirrelObject, FinalGameplayArt.EagleShadowTalonGripFreed, 0.013f, 31);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "RESCUED!");
            _context.SpawnWorldPop(_snatchPosition, "RESCUED!", new Color(0.5f, 1f, 0.45f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("eagle_partner_rescue", 0.32f, 0.6f, 0.2f);
            _context.LogEvent("EaglePartnerRescued", "Cheddar freed from the talons");
            _context.LogObjectiveChanged();
        }

        // Rescue interact: the held dog (Cheddar) wiggles to crack the talon grip; the free dog (Cocoa)
        // pulls in that window. Both come through the Tug/Rescue button via the shared interact path.
        private void TryCompleteRescue(DogId dogId)
        {
            int dogIndex = _context.IndexOfDog(dogId);
            if (dogIndex < 0) return;
            if (!_state.RescueObjectiveActive)
            {
                _context.MarkFailedInteraction(dogId, "rescue is not open yet - keep hiding from the shadow");
                return;
            }
            if (_state.RescueComplete)
            {
                _context.MarkFailedInteraction(dogId, "Cheddar's already free");
                return;
            }

            if (dogId == DogId.Cheddar)
            {
                // The snatched dog struggles, cracking the grip open for a moment.
                _rescue.Wiggle();
                _context.CreditDog(dogIndex);
                _context.SetFeedback(GameManager.FeedbackKind.SoloBark);
                _context.SetCue("Cheddar wiggles - the grip cracks open! Cocoa, pull NOW!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "WIGGLE!");
                UpdateRescueVisuals();
                return;
            }

            // Cocoa (the free dog) pulls - only lands while she's close enough to the talons.
            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _snatchPosition) > RescueRange)
            {
                _context.MarkFailedInteraction(dogId, "get closer to the talons to pull Cheddar free");
                return;
            }
            _rescue.Pull();
            HandleRescueProgress();
        }

        private void CompleteUnitedFront()
        {
            if (!_state.ReadyForUnitedFront) return;

            _state.CompleteUnitedFront();
            _successHoldRemaining = SuccessHoldSeconds;
            for (int i = 0; i < _context.Dogs.Length; i++)
                if (_context.Dogs[i] != null) _context.CreditDog(i);
            _context.AddScore(ScoreEventCatalog.UnitedFront.Points, ScoreEventCatalog.UnitedFront.Label);
            _context.AddScore(500, "SHADOW PANIC CLEAR");
            _context.SetFeedback(GameManager.FeedbackKind.UnitedBark);
            _context.SetCue("United-front bark circle! The eagle gave up and the yard is safe.");
            _context.SetActorState(_context.PredatorObject, "UNITED FRONT - EAGLE RETREATS!", Color.gray, 0.1f);
            if (_context.PredatorObject != null)
                _context.PredatorObject.transform.position = new Vector2(0f, _context.Bounds.yMax + 2f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.UnitedFront.Label);
            _context.SpawnWorldPop((Vector2)_context.Dogs[0].transform.position + Vector2.up, "UNITED FRONT!", new Color(1f, 0.95f, 0.3f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("eagle_united_front", 0.34f, 0.62f, 0.2f);
            _context.LogEvent("EagleUnitedFront", "united front complete");
        }

        private void BuildCoverMarkers()
        {
            _coverMarkers = new GameObject[_coverZones.Length];
            for (int i = 0; i < _coverZones.Length; i++)
            {
                var go = new GameObject($"EagleCover_{i}");
                go.transform.position = _coverZones[i];
                go.transform.localScale = Vector3.one * (CoverRadius * 1.4f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _context.ActorSprite;
                sr.color = new Color(0.3f, 0.7f, 0.4f, 0.45f);
                sr.sortingOrder = 1;
                _context.AddWorldLabel(go, "HIDE HERE", Vector3.up * 0.9f, 14, Color.white);
                MissionPropArt.AttachObject(go, FinalGameplayArt.EagleShadowCoverSafe, 0.012f, 18, true);
                go.SetActive(false);
                _coverMarkers[i] = go;
            }
        }

        // During the hide phase every open cover pad signals at distance (the eagle actor carries
        // the threat warning; the pads are go-here commands). A cover with a dog already tucked
        // inside is resolved and drops its badge; the snatch/rescue and united-front beats move
        // the urgency to the talon actor, so all covers go quiet.
        private void UpdateCoverSignals()
        {
            if (_coverMarkers == null) return;
            bool hidePhase = !_state.RescueObjectiveActive && !_state.RescueComplete && !IsFailed;
            for (int i = 0; i < _coverMarkers.Length; i++)
            {
                bool occupied = false;
                if (hidePhase && _context.Dogs != null)
                {
                    foreach (var dog in _context.Dogs)
                    {
                        if (dog == null) continue;
                        if (Vector2.Distance(dog.transform.position, _coverZones[i]) < CoverRadius)
                        {
                            occupied = true;
                            break;
                        }
                    }
                }
                ActorSignalBadge.SetStationSignal(_coverMarkers[i], hidePhase && !occupied);
            }
        }

        private void SetCoverMarkersActive(bool active)
        {
            if (_coverMarkers == null) return;
            foreach (var marker in _coverMarkers)
                if (marker != null) marker.SetActive(active);
        }

        private void SetCoverArt(string resourcePath)
        {
            if (_coverMarkers == null) return;
            foreach (var marker in _coverMarkers)
                SetMissionProp(marker, resourcePath, 0.012f, 18);
        }

        private static void SetMissionProp(GameObject go, string resourcePath, float scale, int sortingOrder)
        {
            if (go == null || string.IsNullOrEmpty(resourcePath)) return;
            var attachment = go.GetComponent<MissionPropArtAttachment>();
            if (attachment != null && attachment.HasRuntimeSprite)
            {
                MissionPropArt.SetSprite(attachment, resourcePath);
                return;
            }
            MissionPropArt.AttachObject(go, resourcePath, scale, sortingOrder, true);
        }

        private float NearestCoverDistance(Vector2 position)
        {
            float best = float.PositiveInfinity;
            foreach (var zone in _coverZones)
                best = Mathf.Min(best, Vector2.Distance(position, zone));
            return best;
        }

        private Transform FindNearestActiveCover(Vector2 position)
        {
            Transform nearest = null;
            float nearestDistance = float.PositiveInfinity;
            if (_coverMarkers == null) return null;
            foreach (var marker in _coverMarkers)
            {
                if (marker == null || !marker.activeSelf) continue;
                float distance = Vector2.Distance(position, marker.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = marker.transform;
                }
            }
            return nearest;
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin)
        {
            return new Vector2(
                Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
                Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
        }
    }
}
