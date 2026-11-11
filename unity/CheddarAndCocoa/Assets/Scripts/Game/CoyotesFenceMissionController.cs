using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Coyotes at the Fence, rebuilt as a co-op "Pack Defense"
    /// (<see cref="CoopPackDefensePuzzle"/>). The coyote prowls the outside of the fence and digs
    /// under a weak spot against a visible dig meter. Cocoa pins it off the hole (bark in range);
    /// only while it is pinned can Cheddar kick the dirt back in. The coward avoids a hole Cocoa is
    /// already guarding and feints from wave two, a scout coyote that only Cheddar can chase starts
    /// a second dig to split the pair, and before the last repair the coyote tosses bait that drags
    /// Cheddar off until Cocoa barks "LEAVE IT!". Patch three weak spots, then huddle and bark as one
    /// to stop the timed final push. Weak-spot placement is seeded per attempt.
    /// Uses the shared predator actor as the coyote and the shared squirrel actor as the active
    /// weak-spot marker, both through the narrow context.
    /// </summary>
    public sealed class CoyotesFenceMissionController :
        IMissionController, IMissionInteractionController, IMissionUnitedBarkListener,
        IMissionSuccessPresentationController, IMissionPressureHud, IMissionCoachHint
    {
        private const int GapCount = 4;
        private const int RequiredRepairs = 3;
        private const int MaxBreaches = 3;
        private const float SuccessHoldSeconds = 1.15f;
        private const int LeaveItScore = 60;
        private const int ScoutChaseScore = 50;
        private static readonly float[] DigSecondsPerWave = { 8f, 6.5f, 5f };

        private readonly PatrolDefenseMissionState _state = new PatrolDefenseMissionState();
        private readonly CoopPackDefensePuzzle _pack = new CoopPackDefensePuzzle();
        private readonly List<CoopPackDefensePuzzle.PackEvent> _events = new List<CoopPackDefensePuzzle.PackEvent>();
        private MissionContext _context;
        private Vector2[] _gaps;
        private GameObject[] _gapMarkers;
        private GameObject _scout;
        private GameObject _bait;
        private float _successHoldRemaining;

        public PatrolDefenseMissionState State => _state;
        public CoopPackDefensePuzzle Pack => _pack;
        public Vector2[] Gaps => (Vector2[])_gaps.Clone();
        public bool PressureHeld => _pack.PinHeld;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.CoyotesFence;
        public bool IsComplete => _state.FinalPressureComplete && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _state.FinalPressureComplete && _successHoldRemaining > 0f;
        public bool IsFailed => _state.TooManyBreaches(MaxBreaches);
        public string FailReason => IsFailed ? "The coyote pack breached the fence one too many times while the dogs got separated." : null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildPatrolSummary(_state);
        public Vector2 EntryTarget => _gaps != null && _gaps.Length > 0 ? _gaps[0] : Vector2.zero;

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome) return "Coyote pack retreating! Cheddar and Cocoa hold the repaired fence.";
                if (_state.ReadyForFinalPressure(RequiredRepairs))
                    return _pack.FinalPushActive
                        ? "Block the final coyote push - huddle up and BARK TOGETHER before it breaks through"
                        : "The pack is regrouping for another final coyote push - huddle up!";
                if (_state.FakeSnackActive)
                    return _pack.CheddarLured
                        ? "Fake snack lure has Cheddar! Cocoa, bark LEAVE IT next to him"
                        : "Ignore the fake snack lure - Cheddar stay away from it, Cocoa pin the coyote";
                if (_pack.PinHeld) return "Cocoa has the coyote pinned - Cheddar fill the weak spot NOW";
                if (_pack.ScoutActive && _pack.ScoutDigging)
                    return $"SCOUT digging at another weak spot - Cheddar chase it off! Repairs {_state.GapsRepaired}/{RequiredRepairs}, breaches {_state.Breaches}/{MaxBreaches}";
                return $"Cocoa: BARK-pin coyote. Cheddar: fill dirt. Gap {_state.ActiveGapIndex + 1}, repairs {_state.GapsRepaired}/{RequiredRepairs}, breaches {_state.Breaches}/{MaxBreaches}";
            }
        }

        // ---- HUD: dig / pin / final-push meter ----

        public bool PressureVisible => !_state.FinalPressureComplete && !IsFailed &&
            (_pack.FinalPushActive || _pack.PinHeld || _pack.Coyote == CoopPackDefensePuzzle.CoyoteState.Digging || _pack.Dig > 0f);
        public string PressureLabel => _pack.FinalPushActive ? "FINAL PUSH" : _pack.PinHeld ? "PINNED" : "COYOTE DIGGING";
        public float PressureNormalized
        {
            get
            {
                if (_pack.FinalPushActive) return Mathf.Clamp01(1f - _pack.FinalPushRemaining / Mathf.Max(0.01f, _pack.FinalPushSeconds));
                if (_pack.PinHeld) return Mathf.Clamp01(_pack.PinRemaining / 2.6f);
                return Mathf.Clamp01(_pack.Dig);
            }
        }
        public Color PressureColor => _pack.PinHeld ? new Color(0.45f, 0.95f, 0.5f)
            : _pack.FinalPushActive ? new Color(1f, 0.55f, 0.15f) : new Color(0.85f, 0.4f, 0.15f);

        /// <summary>Default fence-gap geometry (seed 0) derived from the arena bounds, usable without an active controller.</summary>
        public static Vector2[] ComputeFenceGaps(Rect bounds)
        {
            CoopPackDefensePuzzle.ComputeGaps(bounds, BackyardPoolZone.WaterRect, GapCount, 0, out var gaps, out _);
            return gaps;
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _pack.Configure(context.Bounds, BackyardPoolZone.WaterRect, GapCount, RequiredRepairs, DigSecondsPerWave);
            _pack.ConfigureFeel(pinRange: context.SingleBarkSquirrelRange);
            _pack.Reset(0);
            _gaps = _pack.Gaps;
            BuildGapMarkers();
            BuildScoutAndBait();
            Cleanup();
        }

        public void StartMission()
        {
            _state.Reset();
            _pack.Reset(_context.Random().Next());
            _events.Clear();
            _gaps = _pack.Gaps;
            _state.SelectGap(0);
            _successHoldRemaining = 0f;
            for (int i = 0; i < _gapMarkers.Length; i++)
                if (_gapMarkers[i] != null) _gapMarkers[i].transform.position = _gaps[i];
            SetGapMarkersActive(true);
            // A previous attempt can leave gaps showing stale Breached/Repaired art; reset every
            // marker so a fresh attempt doesn't open looking already resolved.
            for (int i = 0; i < _gapMarkers.Length; i++) SetGapArt(i, FinalGameplayArt.CoyotesFenceGapOpen);
            if (_scout != null) _scout.SetActive(false);
            if (_bait != null) _bait.SetActive(false);
            ResetSpeedPenalties();
            SyncActors();
            UpdateGapSignals();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_context.PredatorObject == null) return;
            if (_state.FinalPressureComplete)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            if (IsFailed) return;

            Vector2 cheddar = DogPosition(DogId.Cheddar);
            Vector2 cocoa = DogPosition(DogId.Cocoa);
            _pack.Advance(deltaTime, cheddar, cocoa);
            ApplyLure(deltaTime);
            ProcessEvents();
            SyncActors();
            UpdateGapSignals();
        }

        public bool HandleBark(int dogIndex) => RegisterBarkPressure(dogIndex, force: false);

        public void OnUnitedBark()
        {
            if (!_state.ReadyForFinalPressure(RequiredRepairs)) return;
            if (_pack.UnitedBark()) ProcessEvents();
        }

        public bool HandleInteract(int dogIndex)
        {
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length) return false;
            if (!_context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)) return false;
            return TryRepair(identity.Id);
        }

        public void Cleanup()
        {
            SetGapMarkersActive(false);
            if (_scout != null) _scout.SetActive(false);
            if (_bait != null) _bait.SetActive(false);
            ResetSpeedPenalties();
        }

        public void StageDogsForEntry()
        {
            // The shared round staging deactivates the predator/squirrel for this definition before
            // this hook runs, so the coyote and weak-spot staging must happen here.
            var predator = _context.PredatorObject;
            if (predator != null)
            {
                predator.SetActive(true);
                predator.transform.position = _pack.CoyotePosition;
                _context.SetActorState(predator, "COYOTE PROWLING THE FENCE - WATCH WHERE IT DIGS!", new Color(0.55f, 0.32f, 0.12f), 0.28f);
            }
            if (_context.SquirrelObject != null)
            {
                _context.SquirrelObject.SetActive(true);
                _context.SquirrelObject.transform.position = _pack.ActiveGapPosition;
                _context.SetActorState(_context.SquirrelObject, "WEAK SPOT - FILL DIRT (NEEDS COCOA'S PIN)", new Color(0.62f, 0.45f, 0.2f), 0.1f);
            }

            Vector2 entry = EntryTarget;
            Vector2 inward = (_context.Bounds.center - entry).normalized;
            if (inward.sqrMagnitude < 0.01f) inward = Vector2.down;
            // Far enough in that Cocoa doesn't scare the coyote off the first hole before it commits.
            Vector2 center = entry + inward * 9f;
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

            if (_state.ReadyForFinalPressure(RequiredRepairs))
            {
                target = _context.Dogs[dogIndex == 0 ? 1 : 0].transform;
                copy = "UNITED BARK";
                hideDistance = 1.6f;
                return target != null;
            }

            bool cocoa = _context.IndexOfDog(DogId.Cocoa) == dogIndex;
            if (cocoa)
            {
                if (_pack.CheddarLured)
                {
                    int cheddarIndex = _context.IndexOfDog(DogId.Cheddar);
                    target = cheddarIndex >= 0 ? _context.Dogs[cheddarIndex].transform : null;
                    copy = "BARK: LEAVE IT!";
                    hideDistance = _pack.LeaveItRange;
                }
                else
                {
                    target = _context.PredatorObject != null ? _context.PredatorObject.transform : null;
                    copy = _pack.Coyote == CoopPackDefensePuzzle.CoyoteState.Prowling ? "INTERCEPT OR LURK" : "BARK-PIN COYOTE";
                    hideDistance = _context.SingleBarkSquirrelRange;
                }
                return target != null;
            }

            if (_pack.ScoutActive && _pack.ScoutDigging && !_pack.PinHeld && _scout != null)
            {
                target = _scout.transform;
                copy = "CHASE THE SCOUT!";
                hideDistance = _pack.ScoutRange;
                return true;
            }
            target = _context.SquirrelObject != null ? _context.SquirrelObject.transform : null;
            copy = _pack.PinHeld ? "INTERACT: FILL DIRT" : "WAIT FOR COCOA PIN";
            hideDistance = 2f;
            return target != null;
        }

        public GameManager.TutorialActionStep? CoachActionFor(int dogIndex)
        {
            if (_state.FinalPressureComplete || IsFailed || _context?.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length)
                return null;
            if (_state.ReadyForFinalPressure(RequiredRepairs)) return GameManager.TutorialActionStep.Bark;
            DogId dog = DogIdAt(dogIndex);
            Vector2 at = _context.Dogs[dogIndex].transform.position;
            if (dog == DogId.Cocoa)
            {
                if (_pack.CheddarLured && Vector2.Distance(at, DogPosition(DogId.Cheddar)) <= _pack.LeaveItRange)
                    return GameManager.TutorialActionStep.Bark;
                if (!_pack.PinHeld && Vector2.Distance(at, _pack.CoyotePosition) <= _pack.PinRange)
                    return GameManager.TutorialActionStep.Bark;
                return null;
            }
            if (_pack.PinHeld && Vector2.Distance(at, _pack.ActiveGapPosition) <= _pack.FillRange)
                return GameManager.TutorialActionStep.Interact;
            if (_pack.ScoutActive && Vector2.Distance(at, _pack.ScoutPosition) <= _pack.ScoutRange)
                return GameManager.TutorialActionStep.Bark;
            return null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome)
        {
            int progress = _state.GapsRepaired + (_state.FinalPressureComplete ? 1 : 0);
            return new MissionRuntimeSnapshot("coyotes_fence", score, timeRemaining, progress,
                RequiredRepairs + 1, _state.Breaches,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);
        }

        /// <summary>Test hook: register bark pressure from the given dog.</summary>
        public void ForceBarkPressure(DogId dogId) => RegisterBarkPressure(_context.IndexOfDog(dogId), force: true);

        /// <summary>Test hook: fill the active weak spot, skipping the distance check.</summary>
        public void ForceRepair(DogId dogId) => TryRepair(dogId, force: true);

        /// <summary>Test hook: register a fence breach (three end the mission).</summary>
        public void ForceBreach()
        {
            _pack.ForceBreach();
            ProcessEvents();
            SyncActors();
            UpdateGapSignals();
        }

        /// <summary>Test hook: start the fake snack lure beat.</summary>
        public void ForceFakeSnack()
        {
            _pack.ForceLure();
            ProcessEvents();
            SyncActors();
        }

        /// <summary>Test hook: start the scout's dig now.</summary>
        public void ForceScout()
        {
            _pack.ForceScout();
            ProcessEvents();
            SyncActors();
        }

        /// <summary>Test hook: block the final push when it is ready.</summary>
        public void ForceFinalBlock() => OnUnitedBark();

        public void ForcePressureTimeout()
        {
            _pack.ForcePinTimeout();
            ProcessEvents();
            UpdateGapSignals();
        }

        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        /// <summary>Test hook: resolve the coyote's dig completing at the active gap at current pressure.</summary>
        public void ForceProwlReach()
        {
            _pack.ForceReach();
            ProcessEvents();
            SyncActors();
            UpdateGapSignals();
        }

        /// <summary>Test hook: the resource path currently attached to gap marker <paramref name="index"/>.</summary>
        public string GapResourcePathAt(int index)
        {
            if (_gapMarkers == null || index < 0 || index >= _gapMarkers.Length || _gapMarkers[index] == null) return string.Empty;
            var attachment = _gapMarkers[index].GetComponent<MissionPropArtAttachment>();
            return attachment != null ? attachment.ResourcePath : string.Empty;
        }

        // ------------------------------------------------------------------ input

        private bool RegisterBarkPressure(int dogIndex, bool force)
        {
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length ||
                _state.FinalPressureComplete || _state.ReadyForFinalPressure(RequiredRepairs)) return false;
            DogId dogId = DogIdAt(dogIndex);

            if (force)
            {
                if (dogId != DogId.Cocoa) return false;
                bool pinned = _pack.ForcePin();
                ProcessEvents();
                SyncActors();
                UpdateGapSignals();
                return pinned;
            }

            Vector2 at = _context.Dogs[dogIndex].transform.position;
            var result = _pack.Bark(dogId, at, DogPosition(DogId.Cheddar));
            switch (result)
            {
                case CoopPackDefensePuzzle.BarkResult.WrongDog:
                    _context.MarkFailedInteraction(dogId, "Cocoa holds territory; Cheddar fills the dirt gap");
                    _context.SetCue("Cheddar's bark is enthusiastic, but the big coyote only respects Cocoa. Cheddar: fill the dirt (or chase the scout).");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "COCOA: PIN THE COYOTE!");
                    _context.SpawnWorldPop(at + Vector2.up, "COCOA'S JOB", new Color(1f, 0.72f, 0.25f));
                    return true; // a coach beat fired - don't let the generic solo-bark juice clobber it
                case CoopPackDefensePuzzle.BarkResult.OutOfRange when dogId == DogId.Cocoa:
                    _context.MarkFailedInteraction(dogId, "get closer to the coyote before bark-pinning it");
                    _context.SetCue("Cocoa needs to close the gap before her bark can pin the coyote.");
                    return false;
                case CoopPackDefensePuzzle.BarkResult.Nothing:
                case CoopPackDefensePuzzle.BarkResult.AlreadyPinned:
                case CoopPackDefensePuzzle.BarkResult.NotActive:
                case CoopPackDefensePuzzle.BarkResult.OutOfRange:
                    return false;
            }
            ProcessEvents();
            SyncActors();
            UpdateGapSignals();
            return true;
        }

        private bool TryRepair(DogId dogId, bool force = false)
        {
            int dogIndex = _context.IndexOfDog(dogId);
            if (dogIndex < 0) return false;
            if (_state.FinalPressureComplete)
            {
                _context.MarkFailedInteraction(dogId, "yard already defended");
                return false;
            }
            var result = _pack.Fill(dogId, _context.Dogs[dogIndex].transform.position, ignoreDistance: force);
            switch (result)
            {
                case CoopPackDefensePuzzle.FillResult.WrongDog:
                    _context.MarkFailedInteraction(dogId, "Cheddar digs and fills; Cocoa keeps the coyote pinned");
                    _context.SetCue("Cocoa cannot leave the pin - Cheddar must Interact at the weak spot.");
                    _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CHEDDAR: FILL THE GAP!");
                    _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "CHEDDAR'S JOB", new Color(1f, 0.72f, 0.25f));
                    return false;
                case CoopPackDefensePuzzle.FillResult.NotPinned:
                    _context.MarkFailedInteraction(dogId, "Cocoa must bark-pin the coyote before filling dirt");
                    return false;
                case CoopPackDefensePuzzle.FillResult.TooFar:
                    _context.MarkFailedInteraction(dogId, "too far from the fence weak spot");
                    return false;
                case CoopPackDefensePuzzle.FillResult.NotActive:
                    _context.MarkFailedInteraction(dogId, "the fence is patched - huddle for the final push");
                    return false;
            }
            ProcessEvents();
            SyncActors();
            UpdateGapSignals();
            return true;
        }

        // ------------------------------------------------------------------ events

        private void ProcessEvents()
        {
            _events.Clear();
            _pack.DrainEvents(_events);
            foreach (var e in _events)
            {
                switch (e.Kind)
                {
                    case CoopPackDefensePuzzle.EventKind.DigStarted:
                        _context.SetCue("The coyote started DIGGING under the fence! Cocoa: pin it. Cheddar: be at the hole.");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "DIGGING!", new Color(1f, 0.55f, 0.2f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                        SetGapArt(e.Gap, FinalGameplayArt.CoyotesFenceDigging);
                        break;
                    case CoopPackDefensePuzzle.EventKind.SpotGuarded:
                        _context.SetCue("The coyote saw Cocoa guarding that hole and slunk off to another weak spot! Hang back and pounce when it digs.");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "GUARDED!", new Color(0.7f, 0.9f, 1f));
                        break;
                    case CoopPackDefensePuzzle.EventKind.Feint:
                        _context.SetCue("FEINT! The coyote faked one way and cut back to a different weak spot!");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "FEINT!", new Color(1f, 0.8f, 0.3f));
                        break;
                    case CoopPackDefensePuzzle.EventKind.Pinned:
                        OnPinned(e);
                        break;
                    case CoopPackDefensePuzzle.EventKind.PinExpired:
                        SetGapArt(e.Gap, _pack.Dig > 0f ? FinalGameplayArt.CoyotesFenceDigging : FinalGameplayArt.CoyotesFenceGapOpen);
                        _context.SetCue("Cocoa's pin wore off - the coyote is back at the hole. Repin it before Cheddar fills!");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "PIN LOST!");
                        _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
                        _context.LogEvent("CoyotePinExpired", "Cheddar missed Cocoa's bark opening");
                        _context.LogObjectiveChanged();
                        break;
                    case CoopPackDefensePuzzle.EventKind.Repaired:
                        OnRepaired(e);
                        break;
                    case CoopPackDefensePuzzle.EventKind.DrivenBack:
                        SetGapArt(e.Gap, FinalGameplayArt.CoyotesFenceGapOpen);
                        _context.SetCue("The coyote lunged at the weak spot but the bark pressure drove it back!");
                        _context.SetActorState(_context.PredatorObject, "COYOTE DRIVEN BACK!", new Color(0.7f, 0.42f, 0.16f), 0.24f);
                        _context.SpawnWorldPop(e.Position, "DRIVEN BACK!", new Color(1f, 0.85f, 0.3f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                        _context.LogEvent("CoyoteDrivenBack", "bark pressure drove the coyote back");
                        _context.LogObjectiveChanged();
                        break;
                    case CoopPackDefensePuzzle.EventKind.Breached:
                        RegisterBreach(e.Gap, e.Position, $"The coyote dug through a weak spot!");
                        break;
                    case CoopPackDefensePuzzle.EventKind.ScoutArrived:
                        _context.SetCue("A SCOUT coyote is sniffing at another weak spot! Only Cheddar's yappy charge scares it - Cheddar, go bark at it!");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "SCOUT!", new Color(1f, 0.7f, 0.3f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                        _context.SignalRoleHandoff(DogId.Cocoa, DogId.Cheddar);
                        break;
                    case CoopPackDefensePuzzle.EventKind.ScoutChased:
                        _context.CreditDog(IndexOf(DogId.Cheddar));
                        _context.AddScore(ScoutChaseScore, "SCOUT CHASED");
                        _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
                        _context.SetCue("Cheddar charged the scout and it bolted! Back to Cocoa!");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "SCOUT CHASED");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "YIP YIP YIP!", new Color(1f, 0.92f, 0.4f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
                        break;
                    case CoopPackDefensePuzzle.EventKind.ScoutBreached:
                        RegisterBreach(e.Gap, e.Position, "The scout dug through while nobody chased it!");
                        break;
                    case CoopPackDefensePuzzle.EventKind.LureStarted:
                        OnLureStarted(e);
                        break;
                    case CoopPackDefensePuzzle.EventKind.CheddarLured:
                        _context.SetFeedback(GameManager.FeedbackKind.SquirrelStealing);
                        _context.SetCue("Cheddar smelled the bait and he's GONE - dragging himself to it! Cocoa, bark LEAVE IT right next to him!");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CHEDDAR, NO!");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "SNACK?!", new Color(1f, 0.6f, 0.3f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
                        _context.SignalRoleHandoff(DogId.Cheddar, DogId.Cocoa);
                        _context.LogObjectiveChanged();
                        break;
                    case CoopPackDefensePuzzle.EventKind.LeaveIt:
                        _context.CreditDog(IndexOf(DogId.Cocoa));
                        _context.AddScore(LeaveItScore, "LEAVE IT!");
                        _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
                        _context.SetCue("Cocoa: \"LEAVE IT.\" Cheddar snaps out of it. Good boy... ish.");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "LEAVE IT!");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "LEAVE IT!", new Color(0.55f, 1f, 0.55f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                        break;
                    case CoopPackDefensePuzzle.EventKind.BaitEaten:
                        _state.ResolveFakeSnack();
                        _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                        _context.SetCue("Cheddar ATE THE BAIT. The coyote used the distraction to dig way ahead!");
                        _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "BAIT EATEN!");
                        _context.SpawnWorldPop(e.Position + Vector2.up, "*GULP*", new Color(1f, 0.45f, 0.25f));
                        _context.RequestAudioCue(ArenaFeedbackCatalog.EatingGulp);
                        _context.LogEvent("CoyoteBaitEaten", "Cheddar took the bait");
                        _context.LogObjectiveChanged();
                        break;
                    case CoopPackDefensePuzzle.EventKind.LureResolved:
                        _state.ResolveFakeSnack();
                        _context.LogEvent("CoyoteFakeSnackResolved", "lure resolved");
                        _context.LogObjectiveChanged();
                        break;
                    case CoopPackDefensePuzzle.EventKind.FinalPushStarted:
                        _context.SetActorState(_context.PredatorObject, "FINAL PUSH - HUDDLE AND BARK TOGETHER!", new Color(0.85f, 0.3f, 0.12f), 0.34f);
                        _context.SetCue("Fence is patched! The pack is charging - huddle up and BARK TOGETHER before it breaks through!");
                        _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                        _context.LogEvent("CoyoteFinalPressureReady", "final push ready");
                        _context.LogObjectiveChanged();
                        break;
                    case CoopPackDefensePuzzle.EventKind.FinalPushBroke:
                        RegisterBreach(-1, e.Position, "The pack's final push broke through - they'll come again!");
                        break;
                    case CoopPackDefensePuzzle.EventKind.FinalPushBlocked:
                        CompleteFinalPressure();
                        break;
                }
            }
            _events.Clear();
            _state.SelectGap(_pack.ActiveGap);
        }

        private void OnPinned(CoopPackDefensePuzzle.PackEvent e)
        {
            _state.AddBarkPressure();
            int cocoa = IndexOf(DogId.Cocoa);
            if (cocoa >= 0) _context.CreditDog(cocoa);
            SetGapArt(e.Gap, FinalGameplayArt.CoyotesFenceGapPinned);
            _context.AddScore(ScoreEventCatalog.FenceHeld.Points, ScoreEventCatalog.FenceHeld.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue("Cocoa bark-pinned the coyote at the fence - Cheddar, fill the dirt NOW!");
            _context.SetActorState(_context.PredatorObject, "COYOTE PINNED - CHEDDAR FILLS DIRT!", new Color(0.7f, 0.42f, 0.16f), 0.26f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "COYOTE BLOCKED");
            _context.SpawnWorldPop(e.Position, "PINNED!", new Color(1f, 0.85f, 0.3f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.RequestRumble("coyote_block", 0.12f, 0.24f, 0.12f);
            _context.LogEvent("CoyoteBlocked", $"pressures {_state.BarkPressures}");
            _context.LogObjectiveChanged();
        }

        private void OnRepaired(CoopPackDefensePuzzle.PackEvent e)
        {
            _state.AddRepair();
            int cheddar = IndexOf(DogId.Cheddar);
            if (cheddar >= 0) _context.CreditDog(cheddar);
            SetGapArt(e.Gap, FinalGameplayArt.CoyotesFenceGapRepaired);
            _context.AddScore(ScoreEventCatalog.DirtFilled.Points, ScoreEventCatalog.DirtFilled.Label);
            _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
            _context.SetCue(_state.GapsRepaired < RequiredRepairs
                ? $"Cheddar kicked the dirt back in ({_state.GapsRepaired}/{RequiredRepairs})! The coyote slinks off to another weak spot - faster this time."
                : "Last weak spot patched!");
            _context.SetActorState(_context.SquirrelObject, $"WEAK SPOT FILLED {_state.GapsRepaired}/{RequiredRepairs}", new Color(0.45f, 1f, 0.55f), 0.18f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.DirtFilled.Label);
            _context.SpawnWorldPop(e.Position, "DIRT FILLED!", new Color(0.55f, 1f, 0.45f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("coyote_repair", 0.2f, 0.4f, 0.14f);
            _context.LogEvent("CoyoteRepair", $"repairs {_state.GapsRepaired}/{RequiredRepairs}");
            _context.LogObjectiveChanged();
        }

        private void OnLureStarted(CoopPackDefensePuzzle.PackEvent e)
        {
            _state.StartFakeSnack();
            bool cheddarCloser = Vector2.Distance(DogPosition(DogId.Cheddar), e.Position) <=
                Vector2.Distance(DogPosition(DogId.Cocoa), e.Position);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStealing);
            _context.SetCue(cheddarCloser
                ? "Fake snack lure! The coyote tossed bait over the fence - Cheddar, DON'T go near it!"
                : "Fake snack lure! Keep Cheddar away from that bait and keep the coyote pinned.");
            _context.SetActorState(_context.PredatorObject, "FAKE SNACK BAIT - CHEDDAR, NO!", new Color(0.9f, 0.6f, 0.15f), 0.32f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "FAKE SNACK BAIT!");
            _context.SpawnWorldPop(e.Position + Vector2.up, "BAIT!", new Color(1f, 0.6f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            _context.RequestRumble("coyote_fake_snack", 0.14f, 0.3f, 0.12f);
            _context.LogEvent("CoyoteFakeSnack", "fake snack lure started");
            _context.LogObjectiveChanged();
        }

        private void RegisterBreach(int gap, Vector2 at, string cue)
        {
            _state.AddBreach();
            if (gap >= 0) SetGapArt(gap, FinalGameplayArt.CoyotesFenceGapBreached);
            _context.AddScore(ScoreEventCatalog.FakeOut.Points, "COYOTE BREACH");
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"{cue} Breach {_state.Breaches}/{MaxBreaches}.");
            _context.SetActorState(_context.PredatorObject, $"COYOTE BREACH {_state.Breaches}/{MaxBreaches}!", new Color(0.85f, 0.12f, 0.12f), 0.4f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "COYOTE BREACH!");
            _context.SpawnWorldPop(at, "BREACH!", new Color(1f, 0.3f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("coyote_breach", 0.2f, 0.42f, 0.16f);
            _context.RequestShake(0.2f);
            _context.LogEvent("CoyoteBreach", $"breaches {_state.Breaches}/{MaxBreaches}");
            if (!_state.TooManyBreaches(MaxBreaches)) _context.LogObjectiveChanged();
        }

        private void CompleteFinalPressure()
        {
            if (!_state.ReadyForFinalPressure(RequiredRepairs)) return;

            _state.CompleteFinalPressure();
            _successHoldRemaining = SuccessHoldSeconds;
            ResetSpeedPenalties();
            for (int i = 0; i < _context.Dogs.Length; i++)
                if (_context.Dogs[i] != null) _context.CreditDog(i);
            _context.AddScore(ScoreEventCatalog.YardDefended.Points, ScoreEventCatalog.YardDefended.Label);
            _context.SetFeedback(GameManager.FeedbackKind.UnitedBark);
            _context.SetCue("United bark slammed the final push - the whole pack turns tail. The yard is defended!");
            _context.SetActorState(_context.PredatorObject, "COYOTES RETREAT - YARD DEFENDED!", Color.gray, 0.1f);
            if (_context.PredatorObject != null)
                _context.PredatorObject.transform.position = new Vector2(0f, _context.Bounds.yMax + 2f);
            if (_scout != null) _scout.SetActive(false);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.YardDefended.Label);
            _context.SpawnWorldPop((Vector2)_context.Dogs[0].transform.position + Vector2.up, "YARD DEFENDED!", new Color(1f, 0.95f, 0.3f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("coyote_yard_defended", 0.34f, 0.62f, 0.2f);
            _context.LogEvent("CoyoteYardDefended", "final push blocked");
            UpdateGapSignals();
        }

        // ------------------------------------------------------------------ world sync

        private void ApplyLure(float deltaTime)
        {
            int cheddar = IndexOf(DogId.Cheddar);
            if (cheddar < 0) return;
            _context.Dogs[cheddar].SetSpeedPenalty(_pack.SpeedMultiplierFor(DogId.Cheddar));
            Vector2 drift = _pack.LureDrift(_context.Dogs[cheddar].transform.position, deltaTime);
            if (drift.sqrMagnitude > 0f)
                _context.Dogs[cheddar].transform.position += (Vector3)drift;
        }

        private void SyncActors()
        {
            if (_state.FinalPressureComplete) return;
            if (_context.PredatorObject != null && !IsFailed)
                _context.PredatorObject.transform.position = _pack.CoyotePosition;
            if (_context.SquirrelObject != null)
                _context.SquirrelObject.transform.position = _pack.ActiveGapPosition;
            if (_scout != null)
            {
                bool show = _pack.ScoutActive && !_state.ReadyForFinalPressure(RequiredRepairs);
                if (_scout.activeSelf != show) _scout.SetActive(show);
                if (show)
                {
                    _scout.transform.position = _pack.ScoutPosition;
                    ActorSignalBadge.SetStationSignal(_scout, _pack.ScoutDigging, warning: true);
                }
            }
            if (_bait != null)
            {
                if (_bait.activeSelf != _pack.LureActive) _bait.SetActive(_pack.LureActive);
                if (_pack.LureActive) _bait.transform.position = _pack.BaitPosition;
            }
        }

        private void BuildGapMarkers()
        {
            _gapMarkers = new GameObject[_gaps.Length];
            for (int i = 0; i < _gaps.Length; i++)
            {
                var go = new GameObject($"FenceGap_{i}");
                go.transform.position = _gaps[i];
                go.transform.localScale = new Vector3(1.2f, 2.4f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _context.ActorSprite;
                sr.color = new Color(0.5f, 0.36f, 0.18f, 0.6f);
                sr.sortingOrder = 1;
                _context.AddWorldLabel(go, "WEAK SPOT", Vector3.up * 1.4f, 13, Color.white);
                MissionPropArt.AttachObject(go, FinalGameplayArt.CoyotesFenceGapOpen, 0.012f, 18, true);
                go.SetActive(false);
                _gapMarkers[i] = go;
            }
        }

        private void BuildScoutAndBait()
        {
            _scout = new GameObject("CoyoteScout");
            _scout.transform.localScale = Vector3.one * 1.3f;
            var scoutRenderer = _scout.AddComponent<SpriteRenderer>();
            scoutRenderer.sprite = _context.ActorSprite;
            scoutRenderer.color = new Color(0.75f, 0.5f, 0.25f, 0.5f);
            scoutRenderer.sortingOrder = 8;
            _context.AddWorldLabel(_scout, "SCOUT - CHEDDAR BARK!", Vector3.up * 1.1f, 12, Color.white);
            MissionPropArt.AttachObject(_scout, FinalGameplayArt.CoyotesFenceScout, 0.009f, 24, true);
            _scout.SetActive(false);

            _bait = new GameObject("CoyoteBait");
            _bait.transform.localScale = Vector3.one * 1.1f;
            var baitRenderer = _bait.AddComponent<SpriteRenderer>();
            baitRenderer.sprite = _context.ActorSprite;
            baitRenderer.color = new Color(0.95f, 0.6f, 0.25f, 0.45f);
            baitRenderer.sortingOrder = 6;
            _context.AddWorldLabel(_bait, "BAIT - LEAVE IT!", Vector3.up * 0.9f, 12, Color.white);
            MissionPropArt.AttachObject(_bait, FinalGameplayArt.CoyotesFenceFakeSnack, 0.008f, 20, true);
            _bait.SetActive(false);
        }

        // The coyote's target weak spot carries the distance signal: a warning skin while the
        // coyote is loose (breach threat), flipping to a command once it is bark-pinned (partner:
        // fill dirt now). The final united-bark push moves the objective off the fence, so every
        // gap goes quiet along with complete/fail.
        private void UpdateGapSignals()
        {
            if (_gapMarkers == null || _gapMarkers.Length == 0) return;
            bool live = !_state.FinalPressureComplete && !IsFailed &&
                !_state.ReadyForFinalPressure(RequiredRepairs);
            int active = _pack.ActiveGap % _gapMarkers.Length;
            for (int i = 0; i < _gapMarkers.Length; i++)
                ActorSignalBadge.SetStationSignal(_gapMarkers[i], live && i == active, warning: !_pack.PinHeld);
        }

        private void SetGapMarkersActive(bool active)
        {
            if (_gapMarkers == null) return;
            foreach (var marker in _gapMarkers)
                if (marker != null) marker.SetActive(active);
        }

        private void SetGapArt(int gapIndex, string resourcePath)
        {
            if (_gapMarkers == null || _gapMarkers.Length == 0) return;
            int index = Mathf.Clamp(gapIndex, 0, _gapMarkers.Length - 1);
            SetMissionProp(_gapMarkers[index], resourcePath, 0.012f, 18);
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

        private void ResetSpeedPenalties()
        {
            if (_context?.Dogs == null) return;
            foreach (var dog in _context.Dogs)
                if (dog != null) dog.SetSpeedPenalty(1f);
        }

        private int IndexOf(DogId dog) => _context.IndexOfDog(dog);

        private Vector2 DogPosition(DogId dog)
        {
            int index = IndexOf(dog);
            return index >= 0 && _context.Dogs[index] != null ? (Vector2)_context.Dogs[index].transform.position : Vector2.zero;
        }

        private DogId DogIdAt(int dogIndex) => _context.Dogs != null && dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
            _context.Dogs[dogIndex] != null && _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id
                : DogId.Cheddar;

        private Vector2 ClampInsideBounds(Vector2 point, float margin)
        {
            return new Vector2(
                Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
                Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
        }
    }
}
