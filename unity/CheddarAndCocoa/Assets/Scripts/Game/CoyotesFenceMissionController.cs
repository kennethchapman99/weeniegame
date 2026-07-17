using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Coyotes at the Fence: the coyote prowls toward fence weak spots, one dog
    /// bark-pins it while the partner fills dirt, and a united bark blocks the final push. Uses the
    /// shared predator actor as the coyote and the shared squirrel actor as the active weak-spot
    /// marker, both through the narrow context.
    /// </summary>
    public sealed class CoyotesFenceMissionController :
        IMissionController, IMissionInteractionController, IMissionUnitedBarkListener,
        IMissionSuccessPresentationController
    {
        private const int GapCount = 4;
        private const int RequiredRepairs = 3;
        private const int MaxBreaches = 3;
        private const float BarkPressureSeconds = 2.25f;
        private const float SuccessHoldSeconds = 1.15f;

        private readonly PatrolDefenseMissionState _state = new PatrolDefenseMissionState();
        private MissionContext _context;
        private Vector2[] _gaps;
        private GameObject[] _gapMarkers;
        private Vector2 _activeGapPosition;
        private bool _pressureHeld;
        private float _pressureUntil;
        private float _successHoldRemaining;

        public PatrolDefenseMissionState State => _state;
        public Vector2[] Gaps => (Vector2[])_gaps.Clone();
        public bool PressureHeld => _pressureHeld;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.CoyotesFence;
        public bool IsComplete => _state.FinalPressureComplete && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _state.FinalPressureComplete && _successHoldRemaining > 0f;
        public bool IsFailed => _state.TooManyBreaches(MaxBreaches);
        public string FailReason => IsFailed ? "The coyote breached the fence one too many times while the dogs got separated." : null;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildPatrolSummary(_state);
        public Vector2 EntryTarget => _activeGapPosition;

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome) return "Coyote retreating! Cheddar and Cocoa hold the repaired fence.";
                if (_state.ReadyForFinalPressure(RequiredRepairs)) return "Block the final coyote push - both dogs bark together";
                if (_state.FakeSnackActive) return "Ignore the fake snack lure - hold the fence";
                if (_pressureHeld) return "Cocoa has the coyote pinned - Cheddar fill the weak spot NOW";
                return $"Cocoa: BARK-pin coyote. Cheddar: fill dirt. Gap {_state.ActiveGapIndex + 1}, repairs {_state.GapsRepaired}/{RequiredRepairs}, breaches {_state.Breaches}/{MaxBreaches}";
            }
        }

        /// <summary>Fence-gap geometry derived from the arena bounds, usable without an active controller.</summary>
        public static Vector2[] ComputeFenceGaps(Rect bounds)
        {
            Vector2 P(float x, float y) => new Vector2(
                bounds.center.x + x * bounds.width * 0.5f,
                bounds.center.y + y * bounds.height * 0.5f);
            return new[] { P(-0.94f, 0.38f), P(-0.94f, -0.38f), P(0.94f, 0.38f), P(0.94f, -0.38f) };
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _gaps = ComputeFenceGaps(context.Bounds);
            BuildGapMarkers();
            Cleanup();
        }

        public void StartMission()
        {
            _state.Reset();
            _state.SelectGap(0);
            _pressureHeld = false;
            _pressureUntil = 0f;
            _successHoldRemaining = 0f;
            _activeGapPosition = _gaps[0];
            SetGapMarkersActive(true);
            // A previous attempt can leave gaps showing stale Breached/Repaired art; every sibling
            // multi-marker mission (Leash Walk's checkpoints, Bone Relay's mounds) resets its marker
            // art on StartMission, so a fresh Coyotes Fence attempt shouldn't start with gaps that
            // look already resolved from the last run.
            for (int i = 0; i < _gapMarkers.Length; i++) SetGapArt(i, FinalGameplayArt.CoyotesFenceGapOpen);
            UpdateGapSignals();
        }

        public void Tick(float deltaTime, float now)
        {
            var predator = _context.PredatorObject;
            if (predator == null) return;
            if (_state.FinalPressureComplete)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            if (_pressureHeld && now >= _pressureUntil)
                ExpireBarkPressure();

            Vector2 target = _gaps[_state.ActiveGapIndex % _gaps.Length];
            predator.transform.position = Vector3.MoveTowards(
                predator.transform.position, target, deltaTime * (_context.SquirrelMoveSpeed * 0.7f));
            if (Vector2.Distance(predator.transform.position, target) < 0.5f)
                EvaluateReach();
        }

        public bool HandleBark(int dogIndex)
        {
            return RegisterBarkPressure(dogIndex, force: false);
        }

        public void OnUnitedBark()
        {
            if (_state.ReadyForFinalPressure(RequiredRepairs)) CompleteFinalPressure();
        }

        public bool HandleInteract(int dogIndex)
        {
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length) return false;
            if (!_context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)) return false;
            return TryRepair(identity.Id);
        }

        public void Cleanup() => SetGapMarkersActive(false);

        public void StageDogsForEntry()
        {
            // The shared round staging deactivates the predator/squirrel for this definition before
            // this hook runs, so the coyote and weak-spot staging must happen here.
            var predator = _context.PredatorObject;
            if (predator != null)
            {
                predator.SetActive(true);
                predator.transform.position = new Vector2(0f, _context.Bounds.yMax + 2f);
                _context.SetActorState(predator, "COYOTE AT THE FENCE - BARK PRESSURE!", new Color(0.55f, 0.32f, 0.12f), 0.28f);
            }
            if (_context.SquirrelObject != null)
            {
                _context.SquirrelObject.SetActive(true);
                _context.SquirrelObject.transform.position = _activeGapPosition;
                _context.SetActorState(_context.SquirrelObject, "WEAK SPOT - FILL DIRT (NEEDS PARTNER BARK)", new Color(0.62f, 0.45f, 0.2f), 0.1f);
            }

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

            if (_state.ReadyForFinalPressure(RequiredRepairs))
            {
                target = _context.Dogs[dogIndex == 0 ? 1 : 0].transform;
                copy = "UNITED BARK";
                hideDistance = 1.6f;
            }
            else
            {
                bool cocoa = _context.IndexOfDog(DogId.Cocoa) == dogIndex;
                target = cocoa
                    ? _context.PredatorObject != null ? _context.PredatorObject.transform : null
                    : _context.SquirrelObject != null ? _context.SquirrelObject.transform : null;
                copy = cocoa ? "BARK-PIN COYOTE" : _pressureHeld ? "INTERACT: FILL DIRT" : "WAIT FOR COCOA PIN";
                hideDistance = cocoa ? _context.SingleBarkSquirrelRange : 2f;
            }

            return target != null;
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
        public void ForceBreach() => RegisterBreach();

        /// <summary>Test hook: start the fake snack lure beat.</summary>
        public void ForceFakeSnack() => TriggerFakeSnack();

        /// <summary>Test hook: block the final push when it is ready.</summary>
        public void ForceFinalBlock() => OnUnitedBark();
        public void ForcePressureTimeout()
        {
            if (_pressureHeld) ExpireBarkPressure();
        }
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        /// <summary>Test hook: resolve the coyote reaching the active gap at current pressure.</summary>
        public void ForceProwlReach() => EvaluateReach();

        /// <summary>Test hook: the resource path currently attached to gap marker <paramref name="index"/>.</summary>
        public string GapResourcePathAt(int index)
        {
            if (_gapMarkers == null || index < 0 || index >= _gapMarkers.Length || _gapMarkers[index] == null) return string.Empty;
            var attachment = _gapMarkers[index].GetComponent<MissionPropArtAttachment>();
            return attachment != null ? attachment.ResourcePath : string.Empty;
        }

        // The coyote prowls toward the active weak spot. If the dogs are holding bark pressure when
        // it arrives, it is driven off; otherwise it breaches the gap.
        private void EvaluateReach()
        {
            if (_state.FinalPressureComplete) return;

            var predator = _context.PredatorObject;
            if (_pressureHeld)
            {
                _pressureHeld = false;
                _pressureUntil = 0f;
                _context.SetCue("The coyote lunged at the weak spot but the bark pressure drove it back!");
                _context.SetActorState(predator, "COYOTE DRIVEN BACK!", new Color(0.7f, 0.42f, 0.16f), 0.24f);
                if (predator != null)
                    _context.SpawnWorldPop(predator.transform.position, "DRIVEN BACK!", new Color(1f, 0.85f, 0.3f));
                _context.LogEvent("CoyoteDrivenBack", "bark pressure drove the coyote back");
                if (predator != null) predator.transform.position = new Vector2(0f, _context.Bounds.yMax + 2f);
                UpdateGapSignals();
                _context.LogObjectiveChanged();
                return;
            }

            RegisterBreach();
            if (predator != null) predator.transform.position = new Vector2(0f, _context.Bounds.yMax + 2f);
        }

        private bool RegisterBarkPressure(int dogIndex, bool force)
        {
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length ||
                _state.FinalPressureComplete || _state.ReadyForFinalPressure(RequiredRepairs)) return false;
            DogId dogId = DogIdAt(dogIndex);
            if (dogId != DogId.Cocoa)
            {
                _context.MarkFailedInteraction(dogId, "Cocoa holds territory; Cheddar fills the dirt gap");
                _context.SetCue("Cheddar's bark is enthusiastic but Cocoa must pin the coyote while he fills dirt.");
                return false;
            }
            if (!force && Vector2.Distance(_context.Dogs[dogIndex].transform.position,
                    _context.PredatorObject.transform.position) > _context.SingleBarkSquirrelRange)
            {
                _context.MarkFailedInteraction(dogId, "get closer to the coyote before bark-pinning it");
                _context.SetCue("Cocoa needs to close the gap before her bark can pin the coyote.");
                return false;
            }
            if (_pressureHeld) return false;

            _state.AddBarkPressure();
            _pressureHeld = true;
            _pressureUntil = _context.Now() + BarkPressureSeconds;
            _context.CreditDog(dogIndex);
            SetActiveGapArt(FinalGameplayArt.CoyotesFenceGapPinned);
            _context.AddScore(ScoreEventCatalog.FenceHeld.Points, ScoreEventCatalog.FenceHeld.Label);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelScared);
            _context.SetCue($"{DogName(dogIndex)} bark-pinned the coyote at the fence - partner can fill dirt now!");
            _context.SetActorState(_context.PredatorObject, "COYOTE BLOCKED - PARTNER FILLS DIRT!", new Color(0.7f, 0.42f, 0.16f), 0.26f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "COYOTE BLOCKED");
            if (_context.PredatorObject != null)
                _context.SpawnWorldPop(_context.PredatorObject.transform.position, "BLOCKED!", new Color(1f, 0.85f, 0.3f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.RequestRumble("coyote_block", 0.12f, 0.24f, 0.12f);
            _context.LogEvent("CoyoteBlocked", $"pressures {_state.BarkPressures}");

            if (_state.FakeSnackActive)
            {
                _state.ResolveFakeSnack();
                SetMissionProp(_context.PredatorObject, FinalGameplayArt.CoyotesFenceGapPinned, 0.013f, 31);
                _context.SetCue("The fake snack lure fizzled - the dogs held the fence instead of taking the bait!");
                _context.LogEvent("CoyoteFakeSnackResolved", "lure resolved by bark pressure");
            }

            UpdateGapSignals();
            _context.LogObjectiveChanged();
            return true;
        }

        private bool TryRepair(DogId dogId, bool force = false)
        {
            int dogIndex = _context.IndexOfDog(dogId);
            if (dogIndex < 0) return false;
            if (dogId != DogId.Cheddar)
            {
                _context.MarkFailedInteraction(dogId, "Cheddar digs and fills; Cocoa keeps the coyote pinned");
                _context.SetCue("Cocoa cannot leave the pin - Cheddar must Interact at the weak spot.");
                return false;
            }
            if (_state.FinalPressureComplete)
            {
                _context.MarkFailedInteraction(dogId, "yard already defended");
                return false;
            }
            if (!_pressureHeld)
            {
                _context.MarkFailedInteraction(dogId, "Cocoa must bark-hold the coyote before filling dirt");
                return false;
            }
            if (!force && Vector2.Distance(_context.Dogs[dogIndex].transform.position, _activeGapPosition) > 2f)
            {
                _context.MarkFailedInteraction(dogId, "too far from the fence weak spot");
                return false;
            }

            _state.AddRepair();
            _context.CreditDog(dogIndex);
            _pressureHeld = false;
            _pressureUntil = 0f;
            int repairedGap = _state.ActiveGapIndex;
            Vector2 repairedPosition = _activeGapPosition;
            SetGapArt(repairedGap, FinalGameplayArt.CoyotesFenceGapRepaired);
            _state.SelectGap((_state.ActiveGapIndex + 1) % GapCount);
            _activeGapPosition = _gaps[_state.ActiveGapIndex % _gaps.Length];
            SetActiveGapArt(FinalGameplayArt.CoyotesFenceGapOpen);
            if (_context.SquirrelObject != null) _context.SquirrelObject.transform.position = _activeGapPosition;
            _context.AddScore(ScoreEventCatalog.DirtFilled.Points, ScoreEventCatalog.DirtFilled.Label);
            _context.SetFeedback(GameManager.FeedbackKind.PartnerRescue);
            _context.SetCue($"{DogName(dogIndex)} filled the weak spot ({_state.GapsRepaired}/{RequiredRepairs}). Patrol the next gap!");
            _context.SetActorState(_context.SquirrelObject, $"WEAK SPOT FILLED {_state.GapsRepaired}/{RequiredRepairs}", new Color(0.45f, 1f, 0.55f), 0.18f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.DirtFilled.Label);
            _context.SpawnWorldPop(repairedPosition, "DIRT FILLED!", new Color(0.55f, 1f, 0.45f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("coyote_repair", 0.2f, 0.4f, 0.14f);
            _context.LogEvent("CoyoteRepair", $"repairs {_state.GapsRepaired}/{RequiredRepairs}");

            if (_state.GapsRepaired == RequiredRepairs - 1 && !_state.FakeSnackActive)
                TriggerFakeSnack();

            if (_state.ReadyForFinalPressure(RequiredRepairs))
            {
                _context.SetActorState(_context.PredatorObject, "COYOTE GOING FOR THE FINAL PUSH - UNITED BARK!", new Color(0.85f, 0.3f, 0.12f), 0.34f);
                _context.SetCue("Fence is mostly patched! Get both dogs together and bark down the final coyote push.");
                _context.LogEvent("CoyoteFinalPressureReady", "final push ready");
            }

            UpdateGapSignals();
            _context.LogObjectiveChanged();
            return true;
        }

        private void RegisterBreach()
        {
            _state.AddBreach();
            _pressureHeld = false;
            _pressureUntil = 0f;
            int breachedGap = _state.ActiveGapIndex;
            SetGapArt(breachedGap, FinalGameplayArt.CoyotesFenceGapBreached);
            _state.SelectGap((_state.ActiveGapIndex + 1) % GapCount);
            _activeGapPosition = _gaps[_state.ActiveGapIndex % _gaps.Length];
            SetActiveGapArt(FinalGameplayArt.CoyotesFenceGapOpen);
            if (_context.SquirrelObject != null) _context.SquirrelObject.transform.position = _activeGapPosition;
            _context.AddScore(ScoreEventCatalog.FakeOut.Points, "COYOTE BREACH");
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"The coyote slipped through a weak spot! Breach {_state.Breaches}/{MaxBreaches}.");
            _context.SetActorState(_context.PredatorObject, $"COYOTE BREACH {_state.Breaches}/{MaxBreaches}!", new Color(0.85f, 0.12f, 0.12f), 0.4f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "COYOTE BREACH!");
            if (_context.PredatorObject != null)
                _context.SpawnWorldPop(_context.PredatorObject.transform.position, "BREACH!", new Color(1f, 0.3f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("coyote_breach", 0.2f, 0.42f, 0.16f);
            _context.LogEvent("CoyoteBreach", $"breaches {_state.Breaches}/{MaxBreaches}");
            UpdateGapSignals();
            if (!_state.TooManyBreaches(MaxBreaches)) _context.LogObjectiveChanged();
        }

        private void TriggerFakeSnack()
        {
            if (_state.FakeSnackActive) return;

            _state.StartFakeSnack();
            var predator = _context.PredatorObject;
            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            bool cheddarCloser = cheddar >= 0 && cocoa >= 0 && predator != null &&
                Vector2.Distance(_context.Dogs[cheddar].transform.position, predator.transform.position) <=
                Vector2.Distance(_context.Dogs[cocoa].transform.position, predator.transform.position);
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStealing);
            _context.SetCue(cheddarCloser
                ? "Fake snack lure! Cheddar is RABIDLY tempted - someone bark him back to the fence!"
                : "Fake snack lure! Don't take the bait - keep barking the coyote off the fence.");
            _context.SetActorState(predator, cheddarCloser ? "FAKE SNACK BAIT - CHEDDAR, NO!" : "FAKE SNACK BAIT - IGNORE IT!", new Color(0.9f, 0.6f, 0.15f), 0.32f);
            SetMissionProp(predator, FinalGameplayArt.CoyotesFenceFakeSnack, 0.013f, 31);
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "FAKE SNACK BAIT!");
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStealMiss);
            _context.RequestRumble("coyote_fake_snack", 0.14f, 0.3f, 0.12f);
            _context.LogEvent("CoyoteFakeSnack", "fake snack lure started");
            _context.LogObjectiveChanged();
        }

        private void CompleteFinalPressure()
        {
            if (!_state.ReadyForFinalPressure(RequiredRepairs)) return;

            _state.CompleteFinalPressure();
            _successHoldRemaining = SuccessHoldSeconds;
            for (int i = 0; i < _context.Dogs.Length; i++)
                if (_context.Dogs[i] != null) _context.CreditDog(i);
            _context.AddScore(ScoreEventCatalog.YardDefended.Points, ScoreEventCatalog.YardDefended.Label);
            _context.SetFeedback(GameManager.FeedbackKind.UnitedBark);
            _context.SetCue("United bark slammed the final coyote push - the yard is defended!");
            _context.SetActorState(_context.PredatorObject, "COYOTE RETREATS - YARD DEFENDED!", Color.gray, 0.1f);
            if (_context.PredatorObject != null)
                _context.PredatorObject.transform.position = new Vector2(0f, _context.Bounds.yMax + 2f);
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, ScoreEventCatalog.YardDefended.Label);
            _context.SpawnWorldPop((Vector2)_context.Dogs[0].transform.position + Vector2.up, "YARD DEFENDED!", new Color(1f, 0.95f, 0.3f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("coyote_yard_defended", 0.34f, 0.62f, 0.2f);
            _context.LogEvent("CoyoteYardDefended", "final push blocked");
            UpdateGapSignals();
        }

        private void ExpireBarkPressure()
        {
            _pressureHeld = false;
            _pressureUntil = 0f;
            SetActiveGapArt(FinalGameplayArt.CoyotesFenceGapOpen);
            _context.SetCue("Cocoa's bark opening closed - repin the coyote before Cheddar fills dirt.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "PIN LOST!");
            _context.LogEvent("CoyotePinExpired", "Cheddar missed Cocoa's bark opening");
            UpdateGapSignals();
            _context.LogObjectiveChanged();
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

        // The coyote's target weak spot carries the distance signal: a warning skin while the
        // coyote is loose (breach threat), flipping to a command once it is bark-pinned (partner:
        // fill dirt now). The final united-bark push moves the objective off the fence, so every
        // gap goes quiet along with complete/fail.
        private void UpdateGapSignals()
        {
            if (_gapMarkers == null || _gapMarkers.Length == 0) return;
            bool live = !_state.FinalPressureComplete && !IsFailed &&
                !_state.ReadyForFinalPressure(RequiredRepairs);
            int active = _state.ActiveGapIndex % _gapMarkers.Length;
            for (int i = 0; i < _gapMarkers.Length; i++)
                ActorSignalBadge.SetStationSignal(_gapMarkers[i], live && i == active, warning: !_pressureHeld);
        }

        private void SetGapMarkersActive(bool active)
        {
            if (_gapMarkers == null) return;
            foreach (var marker in _gapMarkers)
                if (marker != null) marker.SetActive(active);
        }

        private void SetActiveGapArt(string resourcePath) => SetGapArt(_state.ActiveGapIndex, resourcePath);

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

        private string DogName(int dogIndex)
        {
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length || _context.Dogs[dogIndex] == null)
                return "A dog";
            return _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id.ToString()
                : _context.Dogs[dogIndex].name;
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
