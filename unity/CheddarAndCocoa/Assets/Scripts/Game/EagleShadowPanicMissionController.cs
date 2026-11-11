using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Eagle Shadow Panic, rebuilt as a co-op "Shadow Run":
    /// <list type="number">
    /// <item><b>Shadow Run</b> (<see cref="CoopShadowRunPuzzle"/>): the eagle's shadow sweeps the yard
    /// back and forth as a moving column, resting at each edge. The humans left the dogs' toys out on
    /// the lawn; the pair dash out between passes and stash every toy in a bush. Shadow over a dog in
    /// the open = live GRAB: the grabbed dog wiggles, the partner runs over and pulls in the window,
    /// or the dog is CARRIED OFF (a strike). A dog tucked in a bush can bark to make the eagle
    /// hesitate (Cocoa holds it twice as long); barking in the open hurries the shadow instead. The
    /// rope toy needs both dogs on it at once. Two runs, the second faster and with the rope.</item>
    /// <item><b>Snatch</b>: furious at losing every toy, the eagle dives and snatches Cheddar - the
    /// same wiggle/pull rescue, three pulls.</item>
    /// <item><b>United front</b>: huddle and bark together to drive the eagle off for good.</item>
    /// </list>
    /// Uses the shared predator actor as the eagle riding the shadow and the shared squirrel actor as
    /// the talon-grip rescue marker, both through the narrow context.
    /// </summary>
    public sealed class EagleShadowPanicMissionController :
        IMissionController, IMissionInteractionController, IMissionUnitedBarkListener,
        IMissionSuccessPresentationController, IMissionPressureHud, IMissionBeatProgressHud, IMissionCoachHint
    {
        // Two toy runs, mirrored by ThreatSweepMissionState.SafeHides (one "safe hide" per cleared run).
        private const int RequiredHides = 2;
        private const int MaxExposures = 3;
        private const int RescuePullsNeeded = 3;
        private const float RescueWindowSeconds = 1.2f; // a wiggle cracks the grip open for this long
        private const float RescueRange = 3.5f;         // the free dog must be this close to pull
        private const float CoverRadius = 3f;
        // Y the eagle actor rides at while its shadow sweeps the yard (the shadow column itself spans
        // the full yard height).
        private const float SweepHeight = 0.5f;
        private const float ShadowHalfWidth = 3.5f;
        private const float SuccessHoldSeconds = 1.15f;
        private const float HideWarningSeconds = 2.5f;
        private const int ToyStashScore = 60;
        private const int RunClearBonus = 120;
        private const int LiveRescueScore = 100;
        private const int DecoyScore = 25;
        private static readonly int[] ToysPerRun = { 2, 3 };
        private static readonly bool[] RopeInRun = { false, true };
        private static readonly float[] SweepSpeedPerRun = { 13f, 15f };
        private static readonly string[] SmallToyArt =
        {
            FinalGameplayArt.EagleShadowToyBall,
            FinalGameplayArt.EagleShadowToyDuck,
            FinalGameplayArt.EagleShadowToyBone
        };
        private static readonly Color ShadowTint = new Color(0.07f, 0.07f, 0.13f, 0.32f);
        private static readonly Color AlertTint = new Color(0.35f, 0.05f, 0.08f, 0.38f);

        private readonly ThreatSweepMissionState _state = new ThreatSweepMissionState();
        private readonly CoopShadowRunPuzzle _run = new CoopShadowRunPuzzle();
        // Finale (Rescue-Timing co-op puzzle): after the runs, the eagle SNATCHES Cheddar into its
        // talons. Cheddar wiggles (Tug/Rescue) to crack the grip and open a brief window; Cocoa pulls
        // in that window. Pulling with no window open is a mistimed miss (recoverable).
        private readonly CoopRescueTimingPuzzle _rescue = new CoopRescueTimingPuzzle();
        private readonly List<CoopShadowRunPuzzle.RunEvent> _events = new List<CoopShadowRunPuzzle.RunEvent>();
        private MissionContext _context;
        private Vector2[] _coverZones;
        private GameObject[] _coverMarkers;
        private GameObject _shadowColumn;
        private SpriteRenderer _shadowRenderer;
        private readonly List<GameObject> _toyMarkers = new List<GameObject>();
        private int _toyMarkersRun = -1;
        private Vector2 _snatchPosition;
        private Vector2 _grabPosition;
        private int _pullsSeen;
        private int _missesSeen;
        private int _liveMissesSeen;
        private float _successHoldRemaining;

        public ThreatSweepMissionState SweepState => _state;
        public CoopRescueTimingPuzzle RescuePuzzle => _rescue;
        public CoopShadowRunPuzzle ShadowRun => _run;
        public Vector2[] CoverZones => (Vector2[])_coverZones.Clone();
        public Vector2 SnatchPosition => _snatchPosition;
        public Vector2 LiveGrabPosition => _grabPosition;
        public bool InShadowRun => !_state.RescueObjectiveActive && !_state.RescueComplete && !_run.Complete;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.EagleShadowPanic;
        public bool IsComplete => _state.UnitedFrontComplete && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _state.UnitedFrontComplete && _successHoldRemaining > 0f;
        public bool IsFailed => _state.TooManyExposures(MaxExposures);
        public string FailReason => IsFailed ? "The eagle carried a dog off from the open lawn one too many times." : null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildThreatSweepSummary(_state);
        public Vector2 EntryTarget => _coverZones[2];

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome) return "Eagle retreating! Cheddar and Cocoa hold the yard together.";
                if (_state.RescueComplete) return "United-front bark circle: huddle close and bark together";
                if (_state.RescueObjectiveActive) return $"Eagle snatched Cheddar! Cheddar wiggle (Tug/Rescue), Cocoa pull in the window (pulls {_rescue.Pulls}/{_rescue.PullsNeeded})";
                if (_run.GrabActive)
                    return $"{DogName(_run.GrabbedDog.Value)} GRABBED! {DogName(_run.GrabbedDog.Value)} wiggle, {DogName(Partner(_run.GrabbedDog.Value))} pull - before the carry-off (strikes {_state.Exposures}/{MaxExposures})";
                return $"Hide from the eagle shadow and save the toys: run {Mathf.Min(_run.RunIndex + 1, RequiredHides)}/{RequiredHides}, toys {_run.ToysStashedThisRun}/{_run.ToysNeededThisRun}, strikes {_state.Exposures}/{MaxExposures}";
            }
        }

        // ---- HUD: live danger meter + toys-saved progress ----

        public bool PressureVisible => InShadowRun && (_run.GrabActive || AnyDogExposedToIncomingShadow());
        public string PressureLabel => _run.GrabActive ? "CARRY-OFF" : "SHADOW";
        public float PressureNormalized
        {
            get
            {
                if (_run.GrabActive) return _run.CarryOffNormalized;
                float soonest = SoonestShadowOnExposedDog();
                if (float.IsInfinity(soonest)) return 0f;
                return Mathf.Clamp01(1f - soonest / 6f);
            }
        }
        public Color PressureColor => _run.GrabActive ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.45f, 0.42f, 0.7f);

        public bool ProgressVisible => InShadowRun;
        public string ProgressLabel => $"TOYS SAVED {_run.TotalToysStashed}/{TotalToys}";
        public float ProgressNormalized => TotalToys > 0 ? Mathf.Clamp01(_run.TotalToysStashed / (float)TotalToys) : 0f;
        public Color ProgressColor => new Color(0.55f, 0.9f, 0.55f);

        private static int TotalToys
        {
            get
            {
                int total = 0;
                foreach (int n in ToysPerRun) total += n;
                return total;
            }
        }

        /// <summary>Cover-zone geometry derived from the arena bounds, usable without an active controller.</summary>
        public static Vector2[] ComputeCoverZones(Rect bounds)
        {
            Vector2 P(float x, float y) => new Vector2(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            // Three corner bushes plus two mid-lawn stepping-stone bushes, so a dash across the yard
            // can be broken into hops between passes. Every bush sits on dry land (the left one
            // tucks below the pool's deep end).
            return new[] { P(-0.7f, -0.64f), P(0.7f, -0.64f), P(0f, 0.68f), P(-0.5f, -0.15f), P(0.36f, 0.1f) };
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _coverZones = ComputeCoverZones(context.Bounds);
            Rect b = context.Bounds;
            _run.Configure(b, b.xMin + 1.5f, b.xMax - 1.5f, _coverZones, CoverRadius,
                ToysPerRun, RopeInRun, SweepSpeedPerRun);
            _run.ConfigureFeel(shadowHalfWidth: ShadowHalfWidth, rescueRange: RescueRange,
                grabWindowSeconds: RescueWindowSeconds);
            _run.ConfigureKeepOut(BackyardPoolZone.WaterRect);
            BuildCoverMarkers();
            BuildShadowColumn();
            Cleanup();
        }

        public void StartMission()
        {
            _state.Reset();
            _rescue.Reset();
            _run.Reset(_context.Random().Next());
            _events.Clear();
            _pullsSeen = 0;
            _missesSeen = 0;
            _liveMissesSeen = 0;
            _successHoldRemaining = 0f;
            _grabPosition = default;
            // Keep the snatch/rescue point inside the play band so the rescue beat is on-screen.
            _snatchPosition = new Vector2(0f, 6f);
            SetCoverMarkersActive(true);
            if (_shadowColumn != null) _shadowColumn.SetActive(true);
            // A previous attempt can leave every cover zone showing the last sweep's Safe/Spotted
            // art; reset to Safe so a fresh attempt doesn't open already looking exposed.
            SetCoverArt(FinalGameplayArt.EagleShadowCoverSafe);
            RebuildToyMarkers();
            ResetSpeedPenalties();
            UpdateCoverSignals();
            UpdateShadowVisuals();
        }

        public void Tick(float deltaTime, float now)
        {
            UpdateCoverSignals();
            var predator = _context.PredatorObject;
            if (_state.UnitedFrontComplete)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            // Finale: the eagle has snatched Cheddar - drive the wiggle/pull timing.
            if (_state.RescueObjectiveActive && !_state.RescueComplete) { TickRescue(deltaTime); return; }
            if (_state.RescueComplete || predator == null) return; // freed; united-front phase, dogs roam
            if (IsFailed) return;

            _run.Advance(deltaTime, DogPosition(DogId.Cheddar), DogPosition(DogId.Cocoa));
            ProcessRunEvents();
            if (!InShadowRun) return;

            if (_run.GrabActive) PinGrabbedDog();
            ApplySpeedPenalties();
            UpdateShadowVisuals();
            UpdateToyMarkers();
        }

        public bool HandleBark(int dogIndex)
        {
            if (!InShadowRun || !TryDogId(dogIndex, out var dog)) return false;
            var result = _run.Bark(dog, DogPosition(dog));
            ProcessRunEvents();
            return result == CoopShadowRunPuzzle.BarkResult.Decoy || result == CoopShadowRunPuzzle.BarkResult.Alerted;
        }

        public void OnUnitedBark()
        {
            if (_state.ReadyForUnitedFront) CompleteUnitedFront();
        }

        public bool HandleInteract(int dogIndex)
        {
            if (!TryDogId(dogIndex, out var id)) return false;
            if (InShadowRun && _run.GrabActive)
            {
                HandleLiveGrabInteract(id);
                return true;
            }
            if (InShadowRun)
            {
                _context.MarkFailedInteraction(id, "no button needed - just run over a toy to grab it, then into a bush");
                return true;
            }
            TryCompleteRescue(id);
            return true;
        }

        public void Cleanup()
        {
            SetCoverMarkersActive(false);
            if (_shadowColumn != null) _shadowColumn.SetActive(false);
            foreach (var marker in _toyMarkers)
                if (marker != null) Object.Destroy(marker);
            _toyMarkers.Clear();
            _toyMarkersRun = -1;
            ResetSpeedPenalties();
        }

        public void StageDogsForEntry()
        {
            // The shared round staging deactivates and parks the predator for RequiresPredator=false
            // missions before this hook runs, so the sweep staging must happen here.
            var predator = _context.PredatorObject;
            if (predator != null)
            {
                predator.SetActive(true);
                predator.transform.position = new Vector2(-(_context.Bounds.xMax - 1.5f), SweepHeight);
                _context.SetActorState(predator, "EAGLE CIRCLING - SAVE THE TOYS, HIDE IN BUSHES!", new Color(0.16f, 0.16f, 0.2f), 0.3f);
            }
            // The talon-grip indicator (reuses the squirrel actor) only appears once a dog is grabbed.
            if (_context.SquirrelObject != null) _context.SquirrelObject.SetActive(false);

            // Both dogs start tucked into the top bush, side by side.
            Vector2 entry = EntryTarget;
            for (int i = 0; i < _context.Dogs.Length; i++)
            {
                Vector2 offset = new Vector2(i % 2 == 0 ? -1f : 1f, 0f);
                _context.Dogs[i].transform.position = ClampInsideBounds(entry + offset, 1.5f);
                if (_context.Dogs[i].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
            }
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 1.4f;
            if (!TryDogId(dogIndex, out var dog)) return false;

            if (_state.RescueObjectiveActive && !_state.RescueComplete)
            {
                // Cheddar is the one snatched (wiggle in place); Cocoa is pointed at the talons to pull.
                if (dog == DogId.Cheddar)
                {
                    copy = "WIGGLE!";
                }
                else
                {
                    target = _context.SquirrelObject != null ? _context.SquirrelObject.transform : null;
                    copy = "PULL HIM FREE";
                    hideDistance = RescueRange;
                }
                return target != null;
            }
            if (_state.RescueComplete)
            {
                target = _context.Dogs[dogIndex == 0 ? 1 : 0].transform;
                copy = "HUDDLE + BARK";
                hideDistance = 1.6f;
                return target != null;
            }
            if (!InShadowRun) return false;

            if (_run.GrabActive)
            {
                if (_run.GrabbedDog == dog)
                {
                    copy = "WIGGLE!";
                    return false;
                }
                target = _context.SquirrelObject != null ? _context.SquirrelObject.transform : null;
                copy = "PULL!";
                hideDistance = RescueRange;
                return target != null;
            }

            Vector2 at = DogPosition(dog);
            bool exposed = !_run.IsInCover(at);
            bool carrying = _run.CarriedToy(dog) >= 0 || (_run.RopeLifted && _run.HoldsRope(dog));
            if (carrying)
            {
                target = FindNearestActiveCover(at);
                copy = "STASH IT";
                return target != null;
            }
            if (exposed && _run.SecondsUntilShadowAt(at.x, HideWarningSeconds) < HideWarningSeconds)
            {
                target = FindNearestActiveCover(at);
                copy = "HIDE!";
                return target != null;
            }
            int toy = BestToyFor(dog, at);
            if (toy >= 0 && toy < _toyMarkers.Count && _toyMarkers[toy] != null)
            {
                target = _toyMarkers[toy].transform;
                bool partnerOnRope = _run.ToyIsRope(toy) && _run.HoldsRope(Partner(dog));
                copy = partnerOnRope ? "GRAB THE OTHER END" : _run.ToyIsRope(toy) ? "ROPE: BOTH DOGS" : "FETCH";
                return true;
            }
            return false;
        }

        public GameManager.TutorialActionStep? CoachActionFor(int dogIndex)
        {
            if (!TryDogId(dogIndex, out var dog)) return null;
            if (_state.RescueObjectiveActive && !_state.RescueComplete) return GameManager.TutorialActionStep.Interact;
            if (_state.RescueComplete && !_state.UnitedFrontComplete) return GameManager.TutorialActionStep.Bark;
            if (!InShadowRun) return null;
            if (_run.GrabActive) return GameManager.TutorialActionStep.Interact;
            // A dog safe in a bush while the partner is about to be caught in the open: bark the decoy.
            DogId partner = Partner(dog);
            Vector2 partnerAt = DogPosition(partner);
            if (_run.IsInCover(DogPosition(dog)) && !_run.IsInCover(partnerAt) && _run.DecoyCooldownRemaining <= 0f &&
                _run.SecondsUntilShadowAt(partnerAt.x, HideWarningSeconds) < HideWarningSeconds)
                return GameManager.TutorialActionStep.Bark;
            return null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome)
        {
            int progress = _state.SafeHides + (_state.RescueComplete ? 1 : 0) + (_state.UnitedFrontComplete ? 1 : 0);
            return new MissionRuntimeSnapshot("eagle_shadow_panic", score, timeRemaining, progress,
                RequiredHides + 2, _state.Exposures,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);
        }

        /// <summary>Test hook: clear the current toy run outright (two clears open the snatch/rescue beat).</summary>
        public void ForceSafeHide()
        {
            if (!InShadowRun) return;
            _run.ForceClearRun();
            ProcessRunEvents();
        }

        /// <summary>Test hook: register a strike (three end the mission).</summary>
        public void ForceExposure() => RegisterExposure(_context.PredatorObject != null
            ? (Vector2)_context.PredatorObject.transform.position : Vector2.zero, "SPOTTED!");

        /// <summary>Test hook: run wiggle+pull cycles until the snatched (or live-grabbed) dog is freed.</summary>
        public void ForceRescue()
        {
            if (InShadowRun && _run.GrabActive)
            {
                DogId held = _run.GrabbedDog.Value;
                int guard = 0;
                while (_run.GrabActive && guard++ < 50)
                {
                    _run.Wiggle(held);
                    _run.Pull(Partner(held), _grabPosition, _grabPosition);
                }
                ProcessRunEvents();
                return;
            }
            if (!_state.RescueObjectiveActive || _state.RescueComplete) return;
            int g = 0;
            while (!_rescue.Freed && g++ < 50)
            {
                _rescue.Wiggle();   // crack the grip open
                _rescue.Pull();     // and pull within the window
                HandleRescueProgress();
            }
        }

        /// <summary>Test hook: complete the united-front bark circle when it is ready.</summary>
        public void ForceUnitedFront() => OnUnitedBark();
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        /// <summary>
        /// Test hook: drag the shadow straight over the dogs' current positions. A dog in the open is
        /// grabbed; dogs in cover are passed over.
        /// </summary>
        public void ForceSweepPass()
        {
            if (!InShadowRun || _run.GrabActive) return;
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            {
                Vector2 at = DogPosition(dog);
                if (_run.IsInCover(at)) continue;
                _run.ForceGrab(dog, at);
                break;
            }
            ProcessRunEvents();
        }

        /// <summary>Test hook: let the live grab run out and carry the dog off.</summary>
        public void ForceCarryOff()
        {
            if (!InShadowRun || !_run.GrabActive) return;
            _run.ForceCarryOff(_grabPosition);
            ProcessRunEvents();
        }

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
            if (InShadowRun && _run.GrabActive) { _run.Wiggle(_run.GrabbedDog.Value); UpdateGrabVisuals(); return; }
            _rescue.Wiggle();
            UpdateRescueVisuals();
        }

        /// <summary>Test hook: the free dog pulls; only counts while the wiggle window is open.</summary>
        public void ForcePull()
        {
            if (InShadowRun && _run.GrabActive)
            {
                _run.Pull(Partner(_run.GrabbedDog.Value), _grabPosition, _grabPosition);
                ProcessRunEvents();
                return;
            }
            _rescue.Pull();
            HandleRescueProgress();
        }

        /// <summary>Test hook: let the cracked grip re-tighten (the wiggle window closes).</summary>
        public void ForceRescueAdvance(float seconds)
        {
            _rescue.Advance(seconds);
            UpdateRescueVisuals();
        }

        // ---------------------------------------------------------------- shadow-run events

        private void ProcessRunEvents()
        {
            _events.Clear();
            _run.DrainEvents(_events);
            foreach (var e in _events)
            {
                switch (e.Kind)
                {
                    case CoopShadowRunPuzzle.RunEventKind.ToyPickedUp:
                        _context.CreditDog(IndexOf(e.Dog));
                        _context.SetCue($"{DogName(e.Dog)} grabbed a toy! Get it into a bush before the shadow comes back.");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "GOT IT!", new Color(1f, 0.9f, 0.4f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.ToySqueak);
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.ToyStashed:
                        _context.CreditDog(IndexOf(e.Dog));
                        _context.AddScore(ToyStashScore, "TOY SAVED");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "TOY SAVED");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "SAVED!", new Color(0.55f, 1f, 0.55f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
                        _context.LogEvent("EagleToyStashed", $"run {e.RunIndex + 1} toys {_run.ToysStashedThisRun}/{_run.ToysNeededThisRun}");
                        _context.LogObjectiveChanged();
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.ToyDropped:
                        _context.SetCue("The toy hit the grass! Someone grab it again.");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "DROPPED!", new Color(1f, 0.7f, 0.3f));
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.RopeNeedsPartner:
                        _context.SetFeedback(GameManager.FeedbackKind.TugNeedsPartner);
                        _context.SetCue($"The rope toy is too big for one dog! {DogName(Partner(e.Dog))}, grab the other end!");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "NEED A PARTNER!", new Color(1f, 0.85f, 0.35f));
                        _context.SignalRoleHandoff(e.Dog, Partner(e.Dog));
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.RopeLifted:
                        _context.SetFeedback(GameManager.FeedbackKind.TugTogether);
                        _context.SetCue("Both ends! Stay close and haul the rope toy into a bush together.");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "TEAM CARRY!", new Color(0.6f, 1f, 0.6f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.ToySqueak);
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.DogGrabbed:
                        OnLiveGrab(e);
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.GrabRescued:
                        OnLiveRescue(e);
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.CarriedOff:
                        OnCarriedOff(e);
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.CleanPass:
                        _context.SpawnWorldPop(new Vector2(e.Position.x, SweepHeight + 2f), "SHADOW PASSED", new Color(0.7f, 0.8f, 1f));
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.RunCleared:
                        RegisterSafeHide();
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.DecoyStarted:
                        _context.CreditDog(IndexOf(e.Dog));
                        _context.AddScore(DecoyScore, "DECOY BARK");
                        _context.SetFeedback(GameManager.FeedbackKind.SoloBark);
                        _context.SetCue(e.Dog == DogId.Cocoa
                            ? "Cocoa barks from the bushes - the eagle stops dead to stare her down! GO GO GO!"
                            : "Cheddar yips from the bushes - the eagle hesitates for a second!");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "DECOY!");
                        if (_context.PredatorObject != null)
                            _context.SpawnWorldPop((Vector2)_context.PredatorObject.transform.position + Vector2.up, "HUH?!", new Color(1f, 0.95f, 0.4f));
                        break;
                    case CoopShadowRunPuzzle.RunEventKind.EagleAlerted:
                        _context.SetFeedback(GameManager.FeedbackKind.PredatorAttack);
                        _context.SetCue($"{DogName(e.Dog)} barked out in the OPEN - the eagle heard that and speeds up! Bark from a bush instead.");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "IT HEARD YOU!");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "UH OH", new Color(1f, 0.35f, 0.25f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                        break;
                }
            }
            _events.Clear();
            if (InShadowRun && _toyMarkersRun != _run.RunIndex) RebuildToyMarkers();
        }

        private void OnLiveGrab(CoopShadowRunPuzzle.RunEvent e)
        {
            _grabPosition = e.Position;
            _liveMissesSeen = _run.GrabRescue.MissedPulls;
            if (_context.PredatorObject != null)
                _context.PredatorObject.transform.position = _grabPosition + Vector2.up * 1.2f;
            if (_context.SquirrelObject != null)
            {
                _context.SquirrelObject.SetActive(true);
                _context.SquirrelObject.transform.position = _grabPosition;
            }
            SetCoverArt(FinalGameplayArt.EagleShadowCoverSpotted);
            _context.SetFeedback(GameManager.FeedbackKind.PredatorAttack);
            _context.SetCue($"The eagle GRABBED {DogName(e.Dog)}! {DogName(e.Dog)}: wiggle (Tug/Rescue). {DogName(Partner(e.Dog))}: run over and pull in the window!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "GRABBED!");
            _context.SpawnWorldPop(_grabPosition + Vector2.up * 2f, "GRABBED!", new Color(1f, 0.3f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("eagle_live_grab", 0.3f, 0.55f, 0.2f);
            _context.RequestShake(0.25f);
            _context.LogEvent("EagleLiveGrab", DogName(e.Dog));
            UpdateGrabVisuals();
            _context.LogObjectiveChanged();
        }

        private void OnLiveRescue(CoopShadowRunPuzzle.RunEvent e)
        {
            _context.CreditDog(IndexOf(Partner(e.Dog)));
            _context.AddScore(LiveRescueScore, "PARTNER RESCUED");
            _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
            _context.SetCue($"{DogName(Partner(e.Dog))} yanked {DogName(e.Dog)} out of the talons! Back to the bushes!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "RESCUED!");
            _context.SpawnWorldPop(_grabPosition + Vector2.up * 2f, "RESCUED!", new Color(0.5f, 1f, 0.45f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("eagle_live_rescue", 0.25f, 0.5f, 0.15f);
            _context.LogEvent("EagleLiveRescue", DogName(e.Dog));
            EndGrabVisuals();
            _context.LogObjectiveChanged();
        }

        private void OnCarriedOff(CoopShadowRunPuzzle.RunEvent e)
        {
            // The eagle flaps off with the dog, gets bored, and drops it in a bush. Embarrassing.
            int index = IndexOf(e.Dog);
            if (index >= 0)
            {
                _context.Dogs[index].transform.position = e.Position;
                if (_context.Dogs[index].TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
            EndGrabVisuals();
            RegisterExposure(e.Position, "CARRIED OFF!");
            _context.SetCue($"Too slow - the eagle carried {DogName(e.Dog)} off and dumped them in a bush! Strike {_state.Exposures}/{MaxExposures}.");
        }

        private void HandleLiveGrabInteract(DogId id)
        {
            DogId held = _run.GrabbedDog.Value;
            if (id == held)
            {
                _run.Wiggle(id);
                _context.CreditDog(IndexOf(id));
                _context.SetFeedback(GameManager.FeedbackKind.SoloBark);
                _context.SetCue($"{DogName(id)} wiggles - the grip cracks open! {DogName(Partner(id))}, pull NOW!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "WIGGLE!");
                UpdateGrabVisuals();
                return;
            }

            var result = _run.Pull(id, DogPosition(id), _grabPosition);
            switch (result)
            {
                case CoopShadowRunPuzzle.PullResult.TooFar:
                    _context.MarkFailedInteraction(id, $"get closer to the talons to pull {DogName(held)} free");
                    break;
                case CoopShadowRunPuzzle.PullResult.Mistimed:
                    _context.SetCue($"Mistimed pull - wait for {DogName(held)}'s wiggle to crack the grip first!");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "MISTIMED!");
                    _context.SpawnWorldPop(_grabPosition, "TOO SOON!", new Color(1f, 0.72f, 0.25f));
                    _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
                    break;
                case CoopShadowRunPuzzle.PullResult.Landed:
                    _context.CreditDog(IndexOf(id));
                    _context.SetCue($"Heave! One more good pull frees {DogName(held)}!");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "HEAVE!");
                    _context.SpawnWorldPop(_grabPosition, "HEAVE!", new Color(0.5f, 0.95f, 0.55f));
                    break;
            }
            ProcessRunEvents();
            UpdateGrabVisuals();
        }

        private void PinGrabbedDog()
        {
            int held = IndexOf(_run.GrabbedDog.Value);
            if (held >= 0)
            {
                // The talons lift the dog a little higher as the carry-off timer runs down.
                float lift = Mathf.Lerp(0f, 1.6f, _run.CarryOffNormalized);
                _context.Dogs[held].transform.position = _grabPosition + Vector2.up * lift;
                if (_context.Dogs[held].TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
            if (_context.PredatorObject != null)
                _context.PredatorObject.transform.position = _grabPosition + Vector2.up * (1.2f + 1.6f * _run.CarryOffNormalized);
            if (_context.SquirrelObject != null)
                _context.SquirrelObject.transform.position = _grabPosition + Vector2.up * 1.6f * _run.CarryOffNormalized;
            UpdateGrabVisuals();
        }

        private void UpdateGrabVisuals()
        {
            if (!_run.GrabActive) return;
            var grip = _run.GrabRescue;
            DogId held = _run.GrabbedDog.Value;
            if (_context.SquirrelObject != null)
                _context.SetActorState(_context.SquirrelObject,
                    grip.WindowOpen ? $"GRIP CRACKED - {DogName(Partner(held)).ToUpperInvariant()} PULL NOW!" : $"TALON GRIP - {DogName(held).ToUpperInvariant()} WIGGLE!",
                    grip.WindowOpen ? new Color(0.45f, 1f, 0.55f) : new Color(0.85f, 0.5f, 0.5f), 0.3f);
            SetMissionProp(_context.SquirrelObject,
                grip.WindowOpen ? FinalGameplayArt.EagleShadowTalonGripOpen : FinalGameplayArt.EagleShadowTalonGripClosed,
                0.013f, 31);
            if (grip.MissedPulls > _liveMissesSeen) _liveMissesSeen = grip.MissedPulls;
        }

        private void EndGrabVisuals()
        {
            if (_context.SquirrelObject != null) _context.SquirrelObject.SetActive(false);
            SetCoverArt(FinalGameplayArt.EagleShadowCoverSafe);
            if (_context.PredatorObject != null)
                _context.SetActorState(_context.PredatorObject, "EAGLE CIRCLING - SAVE THE TOYS!", new Color(0.16f, 0.16f, 0.2f), 0.28f);
        }

        // ---------------------------------------------------------------- run progression

        private void RegisterSafeHide()
        {
            if (_state.RescueComplete) return;

            _state.AddSafeHide();
            _state.AdvanceSweep(RequiredHides);
            for (int i = 0; i < _context.Dogs.Length; i++)
                if (_context.Dogs[i] != null) _context.CreditDog(i);
            _context.AddScore(ScoreEventCatalog.SafeHide.Points, ScoreEventCatalog.SafeHide.Label);
            _context.AddScore(RunClearBonus, "TOY RUN CLEARED");
            _context.SetFeedback(GameManager.FeedbackKind.PredatorHuddle);
            _context.SetCue(_state.SafeHides < RequiredHides
                ? "Every toy saved! The eagle screeches - here comes a faster pass, and the big rope toy is out there."
                : "All the toys are safe... and the eagle is FURIOUS.");
            SetCoverArt(FinalGameplayArt.EagleShadowCoverSafe);
            if (_context.PredatorObject != null)
                _context.SetActorState(_context.PredatorObject, $"TOY RUN {_state.SafeHides}/{RequiredHides} SAVED", new Color(0.16f, 0.16f, 0.2f), 0.28f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "TOY RUN CLEARED");
            if (_context.PredatorObject != null)
                _context.SpawnWorldPop(_context.PredatorObject.transform.position, "RUN CLEARED!", new Color(0.55f, 0.85f, 1f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("eagle_safe_hide", 0.1f, 0.2f, 0.1f);
            _context.LogEvent("EagleSafeHide", $"runs {_state.SafeHides}/{RequiredHides}");

            if (_state.ReadyForRescue(RequiredHides))
            {
                if (_run.GrabActive) _run.ForceClearRun();
                StartSnatchRescue();
            }

            UpdateCoverSignals();
            _context.LogObjectiveChanged();
        }

        private void RegisterExposure(Vector2 at, string popLabel)
        {
            _state.AddExposure();
            _context.AddScore(ScoreEventCatalog.FakeOut.Points, "EAGLE SPOOK");
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"Caught in the open! The eagle got a dog ({_state.Exposures}/{MaxExposures}).");
            SetCoverArt(FinalGameplayArt.EagleShadowCoverSpotted);
            _context.SetActorState(_context.PredatorObject, $"STRIKE {_state.Exposures}/{MaxExposures}", new Color(0.85f, 0.12f, 0.12f), 0.4f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "EAGLE SPOOK!");
            _context.SpawnWorldPop(at, popLabel, new Color(1f, 0.3f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("eagle_exposure", 0.2f, 0.42f, 0.16f);
            _context.LogEvent("EagleExposure", $"exposures {_state.Exposures}/{MaxExposures}");
            if (!_state.TooManyExposures(MaxExposures)) _context.LogObjectiveChanged();
        }

        // ---------------------------------------------------------------- finale

        private void StartSnatchRescue()
        {
            _state.StartRescue();
            _rescue.Configure(RescuePullsNeeded, RescueWindowSeconds);
            _pullsSeen = 0;
            _missesSeen = 0;
            ResetSpeedPenalties();
            foreach (var marker in _toyMarkers)
                if (marker != null) marker.SetActive(false);
            if (_shadowColumn != null) _shadowColumn.SetActive(false);
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
            _context.SetCue("The furious eagle SNATCHED Cheddar! Cheddar: wiggle (Tug/Rescue) to crack the grip. Cocoa: get close and pull him free in the window!");
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

        // Finale interact: the held dog (Cheddar) wiggles to crack the talon grip; the free dog (Cocoa)
        // pulls in that window. Both come through the Tug/Rescue button via the shared interact path.
        private void TryCompleteRescue(DogId dogId)
        {
            int dogIndex = _context.IndexOfDog(dogId);
            if (dogIndex < 0) return;
            if (!_state.RescueObjectiveActive)
            {
                _context.MarkFailedInteraction(dogId, "rescue is not open yet - keep saving the toys");
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

        // ---------------------------------------------------------------- visuals

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

        // The shadow itself: a soft dark band the full height of the yard, so "am I under it?" is
        // readable at a glance from anywhere on screen.
        private void BuildShadowColumn()
        {
            _shadowColumn = new GameObject("EagleShadowColumn");
            _shadowRenderer = _shadowColumn.AddComponent<SpriteRenderer>();
            var authored = Resources.Load<Sprite>(FinalGameplayArt.EagleShadowColumn);
            _shadowRenderer.sprite = authored != null ? authored : _context.ActorSprite;
            _shadowRenderer.color = ShadowTint;
            _shadowRenderer.sortingOrder = 3;
            Vector2 size = _shadowRenderer.sprite != null ? (Vector2)_shadowRenderer.sprite.bounds.size : Vector2.one;
            _shadowColumn.transform.localScale = new Vector3(
                ShadowHalfWidth * 2f / Mathf.Max(0.01f, size.x),
                _context.Bounds.height / Mathf.Max(0.01f, size.y), 1f);
            _shadowColumn.transform.position = new Vector2(_context.Bounds.xMin, _context.Bounds.center.y);
            _shadowColumn.SetActive(false);
        }

        private void UpdateShadowVisuals()
        {
            float x = _run.ShadowX;
            if (_shadowColumn != null)
            {
                _shadowColumn.transform.position = new Vector3(x, _context.Bounds.center.y, 0f);
                // While the eagle banks at an edge the shadow fades: that is the "go now" read.
                float alpha = _run.ShadowBanking ? 0.35f : 1f;
                Color tint = _run.EagleAlerted ? AlertTint : ShadowTint;
                tint.a *= alpha;
                if (_shadowRenderer != null) _shadowRenderer.color = tint;
            }
            if (_run.GrabActive || _context.PredatorObject == null) return;
            _context.PredatorObject.transform.position = new Vector2(x, SweepHeight);
            if (_run.DecoyActive)
                _context.SetActorState(_context.PredatorObject, "EAGLE DISTRACTED - GO!", new Color(1f, 0.85f, 0.3f), 0.2f);
            else if (_run.ShadowBanking)
                _context.SetActorState(_context.PredatorObject, "EAGLE TURNING - DASH NOW!", new Color(0.45f, 0.9f, 0.5f), 0.18f);
            else
                _context.SetActorState(_context.PredatorObject, _run.EagleAlerted ? "EAGLE ALERTED - FASTER!" : "SHADOW SWEEPING - HIDE!",
                    _run.EagleAlerted ? new Color(0.85f, 0.15f, 0.15f) : new Color(0.16f, 0.16f, 0.2f), 0.3f);
        }

        private void RebuildToyMarkers()
        {
            foreach (var marker in _toyMarkers)
                if (marker != null) Object.Destroy(marker);
            _toyMarkers.Clear();
            _toyMarkersRun = _run.RunIndex;
            if (_run.Complete) return;
            for (int i = 0; i < _run.ToyCount; i++)
            {
                bool rope = _run.ToyIsRope(i);
                var go = new GameObject($"ShadowRunToy_{i}");
                go.transform.position = _run.ToyPosition(i);
                go.transform.localScale = Vector3.one * (rope ? 1.6f : 1.1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _context.ActorSprite;
                sr.color = rope ? new Color(0.95f, 0.75f, 0.35f, 0.5f) : new Color(1f, 0.95f, 0.55f, 0.5f);
                sr.sortingOrder = 6;
                _context.AddWorldLabel(go, rope ? "ROPE - 2 DOGS" : "TOY", Vector3.up * 0.9f, 13, Color.white);
                string art = rope ? FinalGameplayArt.RopeTug : SmallToyArt[((i + _run.Seed) & int.MaxValue) % SmallToyArt.Length];
                MissionPropArt.AttachObject(go, art, rope ? 0.009f : 0.006f, 20, true);
                _toyMarkers.Add(go);
            }
        }

        private void UpdateToyMarkers()
        {
            for (int i = 0; i < _toyMarkers.Count && i < _run.ToyCount; i++)
            {
                var marker = _toyMarkers[i];
                if (marker == null) continue;
                var state = _run.ToyStateAt(i);
                if (state == CoopShadowRunPuzzle.ToyState.Stashed)
                {
                    if (marker.activeSelf) marker.SetActive(false);
                    continue;
                }
                Vector2 at = _run.ToyPosition(i);
                // Carried toys ride in the dog's mouth, just above it.
                marker.transform.position = state == CoopShadowRunPuzzle.ToyState.Carried ? at + Vector2.up * 0.7f : at;
                ActorSignalBadge.SetStationSignal(marker, state == CoopShadowRunPuzzle.ToyState.OnLawn);
            }
        }

        // During the run every open cover pad signals at distance (the eagle actor carries the threat
        // warning; the pads are go-here commands). A cover with a dog already tucked inside is
        // resolved and drops its badge; the snatch/rescue and united-front beats move the urgency to
        // the talon actor, so all covers go quiet.
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

        // ---------------------------------------------------------------- helpers

        private void ApplySpeedPenalties()
        {
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            {
                int index = IndexOf(dog);
                if (index >= 0) _context.Dogs[index].SetSpeedPenalty(_run.SpeedMultiplierFor(dog));
            }
        }

        private void ResetSpeedPenalties()
        {
            if (_context?.Dogs == null) return;
            foreach (var dog in _context.Dogs)
                if (dog != null) dog.SetSpeedPenalty(1f);
        }

        private int BestToyFor(DogId dog, Vector2 at)
        {
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            bool partnerOnRope = _run.HoldsRope(Partner(dog));
            for (int i = 0; i < _run.ToyCount; i++)
            {
                if (_run.ToyStateAt(i) != CoopShadowRunPuzzle.ToyState.OnLawn) continue;
                float d = Vector2.Distance(at, _run.ToyPosition(i));
                // The partner waiting on the rope trumps everything else.
                if (partnerOnRope && _run.ToyIsRope(i)) return i;
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            return best;
        }

        private bool AnyDogExposedToIncomingShadow() => !float.IsInfinity(SoonestShadowOnExposedDog());

        private float SoonestShadowOnExposedDog()
        {
            float soonest = float.PositiveInfinity;
            foreach (DogId dog in new[] { DogId.Cheddar, DogId.Cocoa })
            {
                Vector2 at = DogPosition(dog);
                if (_run.IsInCover(at)) continue;
                soonest = Mathf.Min(soonest, _run.SecondsUntilShadowAt(at.x, 6f));
            }
            return soonest;
        }

        private bool TryDogId(int dogIndex, out DogId id)
        {
            id = DogId.Cheddar;
            if (_context?.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length || _context.Dogs[dogIndex] == null) return false;
            if (!_context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)) return false;
            id = identity.Id;
            return true;
        }

        private int IndexOf(DogId dog) => _context.IndexOfDog(dog);

        private Vector2 DogPosition(DogId dog)
        {
            int index = IndexOf(dog);
            return index >= 0 && _context.Dogs[index] != null ? (Vector2)_context.Dogs[index].transform.position : _coverZones[0];
        }

        private static DogId Partner(DogId dog) => dog == DogId.Cheddar ? DogId.Cocoa : DogId.Cheddar;
        private static string DogName(DogId dog) => dog == DogId.Cheddar ? "Cheddar" : "Cocoa";

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
