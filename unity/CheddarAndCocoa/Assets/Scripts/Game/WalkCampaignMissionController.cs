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
    public sealed class WalkCampaignMissionController : IMissionController, IMissionInteractionController,
        IMissionPressureHud, IMissionSuccessPresentationController
    {
        private const float StationRange = 3.5f;
        private const float ComprehendNeeded = 2.5f; // both dogs hold the combo this long -> walk earned.
        private const float ConfusionMax = 3f;       // incomplete combo this long -> the human misreads.
        private const int MaxMisreads = 3;
        private const float HumanReactionSeconds = 0.65f;
        private const float SuccessHoldSeconds = 1.15f;
        private const SocialStimulus RequiredMessage = SocialStimulus.DoorStare | SocialStimulus.PresentLeash;
        // CF2.4: misread comedy-gag tuning (see TriggerMisreadGag). MisreadGagSeconds is how long a
        // recoverable misread's offer takes to ease in; the two distances are how far the human
        // leans toward the dogs for a normal vs. the mission-ending (escalated) misread.
        private const float MisreadGagSeconds = 0.4f;
        private const float MisreadOfferDistance = 1.4f;
        private const float MisreadEscalatedOfferDistance = 2.2f;

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
        private Vector2 _doorZone;
        private Vector2 _leashZone;
        private int _misreadsSeen;
        private bool _misreadEscalated;
        private float _misreadGagT;
        private bool _gettingItScored;
        private bool _creditedSolve;
        private bool _failed;
        private bool _doorStareEngaged;
        private bool _leashPresented;
        private float _humanReactionUntil;
        private float _successHoldRemaining;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.WalkCampaign;
        public bool IsComplete => _puzzle.Solved && _successHoldRemaining <= 0f;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "Too many mixed signals - the human gave up and brought the wrong thing one time too many."
            : null;
        public CoopSocialManipulationPuzzle Puzzle => _puzzle;
        public Vector2 DoorZone => _doorZone;
        public Vector2 LeashZone => _leashZone;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildWalkCampaignSummary(_puzzle);
        public string PressureLabel => "HUMAN GETS IT";
        public bool PressureVisible => !_puzzle.Solved;
        public float PressureNormalized => Mathf.Clamp01(_puzzle.Comprehension / ComprehendNeeded);
        public Color PressureColor => Color.Lerp(HumanConfusedColor, HumanGettingItColor, PressureNormalized);
        public bool IsPresentingSuccessfulOutcome => _puzzle.Solved && _successHoldRemaining > 0f;
        public float SuccessHoldRemaining => _successHoldRemaining;
        public bool DoorStareEngaged => _doorStareEngaged;
        public bool LeashPresented => _leashPresented;
        // CF2.4: read-only test hooks for the misread comedy payoff (see TriggerMisreadGag). Purely
        // observational - nothing here feeds back into puzzle/mechanics state.
        public float MisreadGagProgress => _misreadGagT;
        public bool MisreadEscalated => _misreadEscalated;

        public string ObjectiveLabel => IsPresentingSuccessfulOutcome
            ? "WALKIES! Cocoa held the stare and Cheddar made the leash impossible to ignore!"
            : _puzzle.ExactMatch
                ? $"Hold it together! Cocoa stays staring; Cheddar keeps presenting the leash (misreads {_puzzle.Misreads}/{MaxMisreads})"
                : $"Send ONE message: Cocoa Interact-stares at the door AND Cheddar Interact-presents the leash (confused {_puzzle.Misreads}/{MaxMisreads})";

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
            _misreadEscalated = false;
            _misreadGagT = 0f;
            _gettingItScored = false;
            _creditedSolve = false;
            _failed = false;
            _doorStareEngaged = false;
            _leashPresented = false;
            _humanReactionUntil = 0f;
            _successHoldRemaining = 0f;
            _doorZone = new Vector2(_context.Bounds.center.x - 6f, _context.Bounds.center.y - 6f);
            _leashZone = new Vector2(_context.Bounds.center.x + 11f, _context.Bounds.center.y + 3f);
            SetSceneActive(true);
            MissionPropArt.SetSprite(_humanArt, FinalGameplayArt.WalkCampaignHumanConfused);
            MissionPropArt.SetSprite(_leashArt, FinalGameplayArt.WalkCampaignLeashWaiting);
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

            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            if (cheddar < 0 || cocoa < 0) return;

            bool cocoaAtDoor = Vector2.Distance(_context.Dogs[cocoa].transform.position, _doorZone) <= StationRange;
            bool cheddarAtLeash = Vector2.Distance(_context.Dogs[cheddar].transform.position, _leashZone) <= StationRange;
            if (_doorStareEngaged && !cocoaAtDoor)
            {
                _doorStareEngaged = false;
                _context.SetCue("Cocoa broke the door-stare - return and Interact to send that half again.");
                _context.LogEvent("WalkDoorStareReleased", "Cocoa left station");
            }
            if (_leashPresented && !cheddarAtLeash)
            {
                _leashPresented = false;
                _context.SetCue("Cheddar dropped the leash presentation - return and Interact to send that half again.");
                _context.LogEvent("WalkLeashReleased", "Cheddar left station");
            }

            // Both signals are deliberate poses and both must stay held at once.
            SocialStimulus active = SocialStimulus.None;
            if (_doorStareEngaged && cocoaAtDoor)
                active |= SocialStimulus.DoorStare;
            if (_leashPresented && cheddarAtLeash)
                active |= SocialStimulus.PresentLeash;
            _puzzle.SetActiveSet(active);
            _puzzle.Advance(deltaTime);
            // CF2.4: ease the misread-gag offer in over MisreadGagSeconds while a recoverable
            // (non-escalated) misread's reaction window is live; the escalated/mission-ending
            // misread snaps straight to 1 in TriggerMisreadGag instead (see its XML doc). Gated on
            // the reaction-window deadline (not just "T < 1") so a freshly-reset T of 0 after the
            // window closes does not spuriously restart the ease.
            if (!_misreadEscalated && _context.Now() < _humanReactionUntil && _misreadGagT < 1f)
                _misreadGagT = Mathf.Clamp01(_misreadGagT + deltaTime / MisreadGagSeconds);

            HandleProgress();
            if (_failed) return;
            UpdateLabels();
        }

        public bool HandleBark(int dogIndex) => false;

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length)
                return false;

            DogId dog = DogIdAt(dogIndex);
            if (dog == DogId.Cocoa)
            {
                if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _doorZone) > StationRange)
                {
                    _context.SetCue("Cocoa must reach the door before she can Interact into the serious stare.");
                    _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "INTERACT AT DOOR", new Color(0.35f, 0.9f, 0.8f));
                    return true;
                }
                _doorStareEngaged = true;
                _context.SetCue("Cocoa has the human pinned with the door-stare! Cheddar, present the leash.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "THE STARE!");
                _context.SpawnWorldPop(_doorZone, "WALK. NOW.", new Color(0.35f, 0.9f, 0.8f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                _context.RequestRumble("walk_door_stare", 0.08f, 0.18f, 0.08f);
                _context.LogEvent("WalkDoorStare", "Cocoa engaged");
            }
            else
            {
                if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _leashZone) > StationRange)
                {
                    _context.SetCue("Cheddar must reach the leash before he can Interact to shove it into view.");
                    _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "INTERACT AT LEASH", new Color(1f, 0.72f, 0.3f));
                    return true;
                }
                _leashPresented = true;
                _context.SetCue("Cheddar is presenting the leash with zero subtlety! Cocoa, hold the door-stare.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "LEASH DELIVERY!");
                _context.SpawnWorldPop(_leashZone, "THIS LEASH!", new Color(1f, 0.72f, 0.3f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
                _context.RequestRumble("walk_leash_present", 0.08f, 0.18f, 0.08f);
                _context.LogEvent("WalkLeashPresented", "Cheddar engaged");
            }

            UpdateActiveSetFromEngagement();
            HandleProgress();
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
            hideDistance = StationRange;
            if (_context.IndexOfDog(DogId.Cocoa) == dogIndex)
            {
                target = _human != null ? _human.transform : null;
                copy = _doorStareEngaged ? "HOLD THE STARE" : "INTERACT TO STARE";
            }
            else
            {
                target = _leash != null ? _leash.transform : null;
                copy = _leashPresented ? "KEEP PRESENTING" : "INTERACT WITH LEASH";
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

        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

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
                _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
                _context.RequestRumble("walk_getting_it", 0.1f, 0.22f, 0.1f);
                _context.LogEvent("WalkGettingIt", "combo");
                SetHumanState("HUMAN GETTING IT!", HumanGettingItColor, 0.1f,
                    new Color(0.78f, 1f, 0.78f, 1f), FinalGameplayArt.WalkCampaignHumanGettingIt);
            }

            if (_puzzle.Misreads > _misreadsSeen)
            {
                _misreadsSeen = _puzzle.Misreads;
                _gettingItScored = false; // earn the "getting it" pop again on the next clean combo
                _context.AddScore(ScoreEventCatalog.HumanMisread.Points, ScoreEventCatalog.HumanMisread.Label);
                _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
                string wrongThing = WrongThingForMisread(_puzzle.Misreads);
                _context.SetCue($"Mixed signals! The human brought {wrongThing}. ({_puzzle.Misreads}/{MaxMisreads})");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "CONFUSED!");
                if (_human != null) _context.SpawnWorldPop(_human.transform.position, wrongThing.ToUpperInvariant(), new Color(0.95f, 0.6f, 0.25f));
                // CF2.4: comedic payoff, presentation-only. The catalog's WalkCampaign HowToPlay step
                // promises this makes the human "fetch a funny wrong item," but pre-fix the branch only
                // swapped a label/sprite and fired the same ThreatWarning cue every other mission's
                // generic warning-miss uses - no different from a beep. TriggerMisreadGag fires BEFORE
                // ThreatWarning below so ThreatWarning stays the LAST cue requested and
                // LastAudioCueRequested is unchanged for the existing pinned test.
                bool escalated = _puzzle.Misreads >= MaxMisreads;
                TriggerMisreadGag(escalated);
                _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
                _context.RequestRumble("walk_misread", 0.16f, 0.34f, 0.12f);
                _context.LogEvent("WalkMisread", $"{_puzzle.Misreads}/{MaxMisreads}");
                if (escalated)
                {
                    _failed = true;
                    SetHumanState("HUMAN GAVE UP - MIXED SIGNALS!", HumanFailColor, 0.16f,
                        new Color(1f, 0.58f, 0.52f, 1f), FinalGameplayArt.WalkCampaignHumanGaveUp);
                }
                else
                {
                    _humanReactionUntil = _context.Now() + HumanReactionSeconds;
                    SetHumanState("HUMAN MISREAD - WRONG THING!", HumanMisreadColor, 0.13f,
                        new Color(1f, 0.84f, 0.52f, 1f), FinalGameplayArt.WalkCampaignHumanMisread);
                }
            }

            if (_puzzle.Solved && !_creditedSolve)
            {
                _creditedSolve = true;
                _successHoldRemaining = SuccessHoldSeconds;
                _context.AddScore(ScoreEventCatalog.WalkConned.Points, ScoreEventCatalog.WalkConned.Label);
                int cheddar = _context.IndexOfDog(DogId.Cheddar);
                int cocoa = _context.IndexOfDog(DogId.Cocoa);
                if (cheddar >= 0) _context.CreditDog(cheddar);
                if (cocoa >= 0) _context.CreditDog(cocoa);
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "WALKIES!");
                if (_human != null) _context.SpawnWorldPop(_human.transform.position, "WALKIES!", new Color(0.5f, 0.9f, 0.55f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
                _context.RequestRumble("walkies_payoff", 0.3f, 0.55f, 0.2f);
                _context.LogEvent("WalkConned", "solved");
                SetHumanState("HUMAN GRABBED THE LEASH - WALKIES!", HumanSuccessColor, 0.14f,
                    new Color(0.75f, 1f, 0.78f, 1f), FinalGameplayArt.WalkCampaignHumanWalkies);
                MissionPropArt.SetSprite(_leashArt, FinalGameplayArt.WalkCampaignLeashGrabbed);
                foreach (var feedback in _context.DogFeedback)
                    if (feedback != null) feedback.ShowProudBrief();
            }
        }

        private void BuildScene()
        {
            // Identity/state-only close-range text: the per-half badge signals, confused/getting-it
            // and waiting/presented sprites, and the HUD objective line carry the combo instruction.
            _human = NewMarker("WalkCampaignHuman", new Color(0.9f, 0.8f, 0.5f), "HUMAN", new Vector3(1.8f, 3.4f, 1f), out _humanLabel);
            _leash = NewMarker("WalkCampaignLeash", new Color(0.6f, 0.8f, 1f), "LEASH", Vector3.one * 1.2f, out _);
            _humanArt = MissionPropArt.AttachObject(_human, FinalGameplayArt.WalkCampaignHumanConfused, 0.013f, 18, true);
            _leashArt = MissionPropArt.AttachObject(_leash, FinalGameplayArt.WalkCampaignLeashWaiting, 0.012f, 18, true);
            _humanFeedback = _human.AddComponent<MissionActorFeedback>();
            _humanFeedback.Init(_human.GetComponent<SpriteRenderer>(), "HUMAN CONFUSED", 0.03f, Vector3.forward * 10f);
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
            // Distance signal: each half of the exact-combo message signals until its dog is
            // actually sending it (held is resolved, like the Pee Break beat stations).
            bool live = !_puzzle.Solved && !_failed;
            ActorSignalBadge.SetStationSignal(_human, live && (_puzzle.Active & SocialStimulus.DoorStare) == 0);
            ActorSignalBadge.SetStationSignal(_leash, live && (_puzzle.Active & SocialStimulus.PresentLeash) == 0);
            if (_human != null)
            {
                _human.transform.position = _doorZone + MisreadGagOffset();
                if (_puzzle.Solved)
                    SetHumanState("HUMAN GRABBED THE LEASH - WALKIES!", HumanSuccessColor, 0.14f,
                        new Color(0.75f, 1f, 0.78f, 1f), FinalGameplayArt.WalkCampaignHumanWalkies);
                else if (_failed)
                    SetHumanState("HUMAN GAVE UP - MIXED SIGNALS!", HumanFailColor, 0.16f,
                        new Color(1f, 0.58f, 0.52f, 1f), FinalGameplayArt.WalkCampaignHumanGaveUp);
                else if (_context.Now() >= _humanReactionUntil)
                {
                    // CF2.4: the reaction window closed - snap the offer back to rest before
                    // reverting to the steady CONFUSED/GETTING IT pose below.
                    _misreadGagT = 0f;
                    SetHumanState(
                        _puzzle.ExactMatch ? "HUMAN GETTING IT!" : "HUMAN CONFUSED",
                        _puzzle.ExactMatch ? HumanGettingItColor : HumanConfusedColor,
                        _puzzle.ExactMatch ? 0.09f : 0.03f,
                        _puzzle.ExactMatch ? new Color(0.78f, 1f, 0.78f, 1f) : Color.white,
                        _puzzle.ExactMatch ? FinalGameplayArt.WalkCampaignHumanGettingIt : FinalGameplayArt.WalkCampaignHumanConfused);
                }
            }
            if (_leash != null)
            {
                _leash.transform.position = _leashZone;
                if (_leashArt != null)
                {
                    bool presented = (_puzzle.Active & SocialStimulus.PresentLeash) != 0;
                    MissionPropArt.SetSprite(_leashArt, _puzzle.Solved
                        ? FinalGameplayArt.WalkCampaignLeashGrabbed
                        : presented ? FinalGameplayArt.WalkCampaignLeashPresented : FinalGameplayArt.WalkCampaignLeashWaiting);
                    _leashArt.SetTint(presented ? new Color(1f, 0.96f, 0.72f, 1f) : Color.white);
                    if (presented) _leashArt.Pulse(0.12f, 0.04f);
                }
            }
        }

        /// <summary>
        /// CF2.4 (finding: WalkCampaign's HowToPlay step promises the misread "makes the human fetch
        /// a funny wrong item," but the branch above only swapped a label/sprite and fired the same
        /// ThreatWarning cue every other mission's generic warning-miss uses - no different from a
        /// beep). Presentation-only reaction layered on the misread branch in HandleProgress() -
        /// Misreads/Comprehension/Confusion/_failed are already fully computed by the time this runs
        /// and are never touched here. The human leans toward wherever the dogs actually are (holding
        /// out the wrong item) and both dogs turn to react, reusing the existing guidance-nudge read
        /// (no new art). A distinct SquirrelStunned cue fires here, before the generic ThreatWarning
        /// the caller still fires right after, so LastAudioCueRequested stays unchanged for the
        /// existing pinned test. The escalated (mission-ending, third) misread snaps straight to the
        /// full offer instead of easing in over MisreadGagSeconds like a recoverable one does -
        /// Tick() stops calling UpdateLabels() the instant _failed is set this same frame (see
        /// Tick()'s "if (_failed) return;"), so there are no more frames left to ease through; this
        /// reads as a held freeze-frame at the game-over beat instead.
        /// </summary>
        private void TriggerMisreadGag(bool escalated)
        {
            _misreadEscalated = escalated;
            _misreadGagT = escalated ? 1f : 0f;
            _context.RequestAudioCue(ArenaFeedbackCatalog.SquirrelStunned);
            if (escalated && _human != null)
                _human.transform.position = _doorZone + MisreadGagOffset();

            Vector2 humanPosition = _human != null ? (Vector2)_human.transform.position : _doorZone;
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowGuidanceNudge(humanPosition - (Vector2)feedback.transform.position);
        }

        /// <summary>How far (0..1, via _misreadGagT) and which direction the human currently leans
        /// toward the dogs while holding out the misread's wrong item. At rest (no active misread
        /// reaction) this is Vector2.zero, so it composes onto _doorZone as a no-op.</summary>
        private Vector2 MisreadGagOffset()
        {
            if (_misreadGagT <= 0f) return Vector2.zero;
            float distance = _misreadEscalated ? MisreadEscalatedOfferDistance : MisreadOfferDistance;
            return MisreadDirectionTowardDogs() * distance * _misreadGagT;
        }

        private Vector2 MisreadDirectionTowardDogs()
        {
            if (_context.Dogs == null || _context.Dogs.Length == 0) return Vector2.zero;
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (var dog in _context.Dogs)
            {
                if (dog == null) continue;
                sum += (Vector2)dog.transform.position;
                count++;
            }
            if (count == 0) return Vector2.zero;
            Vector2 toward = sum / count - _doorZone;
            return toward.sqrMagnitude < 0.0001f ? Vector2.zero : toward.normalized;
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

        private void UpdateActiveSetFromEngagement()
        {
            SocialStimulus active = SocialStimulus.None;
            if (_doorStareEngaged) active |= SocialStimulus.DoorStare;
            if (_leashPresented) active |= SocialStimulus.PresentLeash;
            _puzzle.SetActiveSet(active);
        }

        private static string WrongThingForMisread(int misread) => misread switch
        {
            1 => "the food bowl",
            2 => "a bath towel",
            _ => "the vacuum"
        };

        private DogId DogIdAt(int dogIndex)
        {
            if (_context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length || _context.Dogs[dogIndex] == null)
                return DogId.Cheddar;
            var identity = _context.Dogs[dogIndex].GetComponent<DogIdentity>();
            return identity != null ? identity.Id : DogId.Cheddar;
        }
    }
}
