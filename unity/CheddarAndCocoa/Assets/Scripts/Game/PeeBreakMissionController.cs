using System.IO;
using CheddarAndCocoa.Dogs;
using UnityEngine;
using UnityEngine.Video;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned four-beat deep slice in which the dogs combine exact, role-locked signals
    /// to convince the Teenager to open the door. Misreads reset the current attempt, never the run.
    /// </summary>
    public sealed class PeeBreakMissionController : IMissionController, IMissionSuccessPresentationController,
        IMissionInteractionController, IMissionPressureHud, IMissionOpeningPresentationController
    {
        public enum Beat
        {
            DoorStare,
            LeashMessage,
            ChargerGambit,
            UnitedBark,
            Complete
        }

        public enum TeenPresentationState
        {
            DistractedIdle,
            AnnoyedReacting,
            DistractedAgain,
            StandingSuccess,
            ImpatientFail
        }

        private const float StationRange = 2.25f;
        private const float UnitedBarkWindow = 0.8f;
        private const float PromptRange = StationRange + 0.35f;
        private const float DoorOpenPayoffSeconds = 1.15f;
        private const float ToyInteractRange = 2.8f;
        private const float ToyKickSpeed = 7.5f;
        public const float OpeningExplainerDurationSeconds = 10.042f;
        public const string OpeningComprehensionCorrectionLabel = "TEENAGER COMPREHENSION";
        private const float OpeningExplainerSafetyTimeoutSeconds = 18f;
        private const string OpeningExplainerRelativePath = "OperationPeeBreak/operation_pee_break_intro.mp4";

        private static readonly SocialStimulus[] RequiredByBeat =
        {
            SocialStimulus.DoorStare,
            SocialStimulus.DoorStare | SocialStimulus.PresentLeash,
            SocialStimulus.UnplugCharger | SocialStimulus.BlockHallway,
            SocialStimulus.DoorStare | SocialStimulus.PresentLeash | SocialStimulus.BarkRhythm
        };

        private static readonly float[] ComprehensionByBeat = { 0.65f, 2f, 2.5f, 2.25f };
        private static readonly float[] ConfusionByBeat = { 5f, 3f, 2.5f, 3f };

        private readonly CoopSocialManipulationPuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _door;
        private GameObject _leash;
        private GameObject _hallway;
        private GameObject _charger;
        private GameObject _cheddarCoach;
        private GameObject _teenager;
        private GameObject _phone;
        private GameObject _bladderMeter;
        private GameObject _misreadProp;
        private GameObject _misreadAccent;
        private GameObject _roomFloor;
        private GameObject _roomWall;
        private GameObject _roomWoodFloor;
        private GameObject _roomBaseboard;
        private GameObject _livingRoomArt;
        private GameObject _successRoomArt;
        private GameObject _roomWindowArt;
        private GameObject _roomWindowTop;
        private GameObject _roomWindowBottom;
        private GameObject _roomWindowLeft;
        private GameObject _roomWindowRight;
        private GameObject _couchBack;
        private GameObject _couchSeat;
        private GameObject _sideTable;
        private GameObject _phoneGlow;
        private GameObject _chargerCord;
        private GameObject _doorFrame;
        private GameObject _closedDoorSlab;
        private GameObject _openSunbeam;
        private GameObject _leashHook;
        private GameObject _hallwayRug;
        private GameObject _couchBlanketSlump;
        private GameObject _chewToyUnderTable;
        private GameObject _straySockA;
        private GameObject _straySockB;
        private GameObject _phoneNotificationPing;
        private GameObject _doorMat;
        private GameObject _shoeLeft;
        private GameObject _shoeRight;
        private GameObject _cheddarUrgencyCue;
        private GameObject _cocoaUrgencyCue;
        private GameObject _teenagerHead;
        private GameObject _teenagerHoodie;
        private GameObject _teenagerThumbs;
        private GameObject _teenagerFootWiggle;
        private GameObject _teenagerPhoneBeam;
        private GameObject _teenagerDoorBeam;
        private GameObject _teenagerQuestionBubble;
        private GameObject _teenagerOhBubble;
        private GameObject _phoneBatteryFill;
        private GameObject _phoneChargeBolt;
        private GameObject _phoneDeadSlash;
        private GameObject _chargerPluggedEnd;
        private GameObject _chargerUnpluggedEnd;
        private GameObject _doorOutdoorView;
        private GameObject _doorOpenPanel;
        private GameObject _outdoorGrassPatch;
        private GameObject _outdoorFireHydrant;
        private GameObject _reliefSparkleA;
        private GameObject _reliefSparkleB;
        private GameObject _reliefSparkleC;
        private GameObject _leashPresentedTrail;
        private GameObject _bladderWarningFill;
        private GameObject _bladderUrgencyTick;
        private GameObject _couchArt;
        private GameObject _teenagerArt;
        private GameObject _phoneArt;
        private GameObject _openDoorArt;
        private GameObject _leashArt;
        private GameObject _hydrantArt;
        private GameObject _bladderArt;
        private GameObject _misreadTennisBallArt;
        private GameObject _playBall;
        private GameObject _playBallArt;
        private GameObject _squeakyToy;
        private GameObject _squeakyToyHandle;
        private GameObject _comprehensionTrack;
        private GameObject _comprehensionFill;
        private GameObject _confusionFill;
        private GameObject _introVideoObject;
        private VideoPlayer _introVideoPlayer;
        private readonly GameObject[] _beatPips = new GameObject[4];
        private TextMesh _doorLabel;
        private TextMesh _leashLabel;
        private TextMesh _hallwayLabel;
        private TextMesh _chargerLabel;
        private TextMesh _cheddarCoachLabel;
        private TextMesh _teenagerLabel;
        private TextMesh _phoneLabel;
        private TextMesh _bladderLabel;
        private TextMesh _misreadLabel;
        private Vector2 _doorPosition;
        private Vector2 _leashPosition;
        private Vector2 _hallwayPosition;
        private Vector2 _chargerPosition;
        private Vector2 _cheddarCoachPosition;
        private float[] _lastDoorBarks = { float.NegativeInfinity, float.NegativeInfinity };
        private float _barkSignalUntil = float.NegativeInfinity;
        private float _unitedBarkSignalUntil = float.NegativeInfinity;
        private float _successHoldRemaining;
        private float _signalReactionUntil;
        private Vector2 _playBallVelocity;
        private Vector2 _squeakyToyVelocity;
        private SocialStimulus _lastActiveSet;
        private int _beatIndex;
        private int _beatMisreadsSeen;
        private string _latestMisreadThing = string.Empty;
        private float _openingExplainerElapsed;

        public TeenPresentationState TeenState { get; private set; }
        public GameManager.MissionVariant Variant => GameManager.MissionVariant.OperationPeeBreak;
        public Beat CurrentBeat => (Beat)Mathf.Clamp(_beatIndex, 0, 4);
        public int CompletedBeats => Mathf.Clamp(_beatIndex, 0, 4);
        public int Misreads { get; private set; }
        public float Bladder { get; private set; }
        public float PhoneBattery { get; private set; }
        public bool DoorOpen { get; private set; }
        public int GeneratedPeeBreakPropSpriteCount { get; private set; }
        public bool HasGeneratedCartoonProps => GeneratedPeeBreakPropSpriteCount >= 8;
        public float SuccessHoldRemaining => _successHoldRemaining;
        public bool IsPresentingSuccessfulOutcome => DoorOpen && _successHoldRemaining > 0f;
        public bool IsPresentingOpening { get; private set; }
        public string OpeningOverlayLabel => _introVideoPlayer != null && _introVideoPlayer.isPlaying &&
            NeedsComprehensionCorrection(_introVideoPlayer.time)
                ? OpeningComprehensionCorrectionLabel
                : string.Empty;
        public bool OpeningExplainerAvailable { get; private set; }
        public string OpeningExplainerPath => Path.Combine(Application.streamingAssetsPath, OpeningExplainerRelativePath);
        public string PressureLabel => "BLADDER EMERGENCY";
        public bool PressureVisible => true;
        public float PressureNormalized => Bladder;
        public Color PressureColor => Color.Lerp(new Color(0.3f, 0.78f, 1f), new Color(1f, 0.24f, 0.12f), Bladder);
        public int ToyKickCount { get; private set; }
        public int SignalReactionCount { get; private set; }
        public Vector2 PlayBallPosition => _playBall != null ? _playBall.transform.position : Vector2.zero;
        public Vector2 SqueakyToyPosition => _squeakyToy != null ? _squeakyToy.transform.position : Vector2.zero;
        public bool IsComplete => DoorOpen && _successHoldRemaining <= 0f;
        public CoopSocialManipulationPuzzle Puzzle => _puzzle;
        public SocialStimulus Required => _beatIndex < RequiredByBeat.Length ? RequiredByBeat[_beatIndex] : SocialStimulus.None;
        public Vector2 DoorPosition => _doorPosition;
        public Vector2 LeashPosition => _leashPosition;
        public Vector2 HallwayPosition => _hallwayPosition;
        public Vector2 ChargerPosition => _chargerPosition;
        public Vector2 EntryTarget => _doorPosition;
        public string OutcomeSummary => DoorOpen
            ? Misreads == 0 ? "Pee Break Pawfect"
            : Misreads == 1 ? "Outside, Eventually"
            : "Many Wrong Ideas Later"
            : Misreads > 0 ? "Still Misunderstood" : "Still Holding It";
        public bool IsFailed => false;
        public string FailReason => null;

        public string ObjectiveLabel
        {
            get
            {
                string recovery = Misreads > 0 ? $" / MISREADS {Misreads}" : string.Empty;
                return CurrentBeat switch
                {
                    Beat.DoorStare => $"1/4 Cocoa holds DOOR STARE; Cheddar watches / no bark{recovery}",
                    Beat.LeashMessage => $"2/4 Cocoa STARE + Cheddar PRESENT LEASH{recovery}",
                    Beat.ChargerGambit => $"3/4 Cheddar BLOCK HALLWAY + Cocoa UNPLUG CHARGER{recovery}",
                    Beat.UnitedBark => $"4/4 Hold STARE + LEASH, then BOTH BARK by the door{recovery}",
                    _ => "DOOR OPEN - OUTSIDE!"
                };
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
            // The generated room plates author the entry into the center-right wall. Keep the
            // gameplay station on that exact architectural anchor instead of floating a door prop
            // over the middle of the rug.
            _doorPosition = new Vector2(_context.Bounds.center.x + 13.6f, _context.Bounds.center.y + 8.2f);
            _leashPosition = _doorPosition + new Vector2(-3f, -1f);
            _cheddarCoachPosition = _doorPosition + new Vector2(-6f, -3f);
            _hallwayPosition = new Vector2(_context.Bounds.center.x - 3f, _context.Bounds.center.y);
            _chargerPosition = new Vector2(_context.Bounds.center.x + 2f, _context.Bounds.center.y + 1.5f);
            _beatIndex = 0;
            Misreads = 0;
            Bladder = 0.12f;
            PhoneBattery = 1f;
            DoorOpen = false;
            _successHoldRemaining = 0f;
            _signalReactionUntil = float.NegativeInfinity;
            _lastActiveSet = SocialStimulus.None;
            _playBallVelocity = Vector2.zero;
            _squeakyToyVelocity = Vector2.zero;
            ToyKickCount = 0;
            SignalReactionCount = 0;
            TeenState = TeenPresentationState.DistractedIdle;
            _latestMisreadThing = string.Empty;
            _lastDoorBarks[0] = _lastDoorBarks[1] = float.NegativeInfinity;
            _barkSignalUntil = float.NegativeInfinity;
            _unitedBarkSignalUntil = float.NegativeInfinity;
            ConfigureBeat();
            SetSceneActive(true);
            _playBall.transform.position = _context.Bounds.center + new Vector2(-8f, -4.5f);
            _squeakyToy.transform.position = _context.Bounds.center + new Vector2(5.5f, -5.2f);
            UpdateScene();
            StartOpeningPresentation();
        }

        public void TickOpeningPresentation(float unscaledDeltaTime)
        {
            if (!IsPresentingOpening) return;
            _openingExplainerElapsed += Mathf.Max(0f, unscaledDeltaTime);
            if (_introVideoPlayer != null && _introVideoPlayer.isPrepared && _introVideoPlayer.audioTrackCount > 0)
                _introVideoPlayer.SetDirectAudioMute(0, !_context.AudioEnabled());
            if (_openingExplainerElapsed >= OpeningExplainerSafetyTimeoutSeconds)
                FinishOpeningPresentation("safety timeout");
        }

        public void SkipOpeningPresentation() => FinishOpeningPresentation("player skip");

        public static bool NeedsComprehensionCorrection(double playbackSeconds) =>
            playbackSeconds >= 4.6d && playbackSeconds < 5.95d ||
            playbackSeconds >= 7.3d && playbackSeconds < 8.9d;

        public void Tick(float deltaTime, float now)
        {
            if (deltaTime <= 0f) return;
            if (DoorOpen)
            {
                AdvanceSuccessHold(deltaTime);
                return;
            }
            AdvanceToys(deltaTime);
            AdvanceSimulation(BuildActiveSet(now), deltaTime);
        }

        public bool HandleInteract(int dogIndex)
        {
            if (DoorOpen || dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length ||
                _context.Dogs[dogIndex] == null) return false;

            Vector2 dogPosition = _context.Dogs[dogIndex].transform.position;
            float ballDistance = Vector2.Distance(dogPosition, _playBall.transform.position);
            float squeakyDistance = Vector2.Distance(dogPosition, _squeakyToy.transform.position);
            if (Mathf.Min(ballDistance, squeakyDistance) > ToyInteractRange) return false;

            bool ball = ballDistance <= squeakyDistance;
            GameObject toy = ball ? _playBall : _squeakyToy;
            Vector2 direction = (Vector2)toy.transform.position - dogPosition;
            if (direction.sqrMagnitude < 0.01f) direction = dogIndex == 0 ? Vector2.right : Vector2.left;
            direction.Normalize();
            Vector2 kick = (direction + new Vector2(-direction.y, direction.x) * 0.18f).normalized * ToyKickSpeed;
            if (ball) _playBallVelocity = kick;
            else _squeakyToyVelocity = kick * 0.78f;

            ToyKickCount++;
            string toyName = ball ? "TENNIS BALL" : "SQUEAKY";
            _context.Pulse(toy, 0.28f);
            _context.SpawnWorldPop(toy.transform.position, ball ? "BOOP!" : "SQUEAK!", new Color(1f, 0.85f, 0.25f));
            _context.RequestAudioCue(ball ? ArenaFeedbackCatalog.UiMenuFocus : ArenaFeedbackCatalog.BunnyHop);
            _context.LogEvent("PeeBreakToy", $"dog {dogIndex} batted {toyName}");
            return true;
        }

        public bool HandleBark(int dogIndex)
        {
            if (DoorOpen || dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length) return false;
            float now = _context.Now();
            _barkSignalUntil = now + UnitedBarkWindow;
            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, _doorPosition) <= StationRange + 1f)
                _lastDoorBarks[dogIndex] = now;

            if (_beatIndex == 3 && BothDoorBarksRecent(now))
            {
                _unitedBarkSignalUntil = now + ComprehensionByBeat[3] + 0.1f;
                _context.SetFeedback(GameManager.FeedbackKind.UnitedBark);
                _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "UNITED BARK!");
                _context.RequestRumble("pee_break_united_bark", 0.35f, 0.65f, 0.22f);
                _context.SpawnWorldPop(_doorPosition, "WOOF + WOOF!", new Color(1f, 0.92f, 0.35f));
            }
            return true;
        }

        public void Cleanup()
        {
            FinishOpeningPresentation("mission cleanup");
            SetSceneActive(false);
        }

        public void StageDogsForEntry()
        {
            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            if (cheddar >= 0) _context.Dogs[cheddar].transform.position = _cheddarCoachPosition + new Vector2(-3f, -1f);
            if (cocoa >= 0) _context.Dogs[cocoa].transform.position = _doorPosition + new Vector2(-4f, 1f);
            foreach (var dog in _context.Dogs)
                if (dog != null && dog.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = StationRange;
            bool cheddar = dogIndex == _context.IndexOfDog(DogId.Cheddar);
            switch (CurrentBeat)
            {
                case Beat.DoorStare:
                    target = cheddar ? _cheddarCoach.transform : _door.transform;
                    copy = cheddar ? "WATCH COCOA / NO BARK" : "HOLD DOOR STARE";
                    break;
                case Beat.LeashMessage:
                    target = cheddar ? _leash.transform : _door.transform;
                    copy = cheddar ? "PRESENT LEASH" : "HOLD DOOR STARE";
                    break;
                case Beat.UnitedBark:
                    target = cheddar ? _leash.transform : _door.transform;
                    copy = cheddar ? "LEASH + BARK!" : "STARE + BARK!";
                    break;
                case Beat.ChargerGambit:
                    target = cheddar ? _hallway.transform : _charger.transform;
                    copy = cheddar ? "BLOCK HALLWAY" : "UNPLUG CHARGER";
                    break;
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("operation_pee_break", score, timeRemaining, CompletedBeats, 4, Misreads,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Advances the same puzzle path used by live position/input driving.</summary>
        public void ForceAdvance(SocialStimulus active, float deltaTime)
        {
            if (deltaTime <= 0f) return;
            if (DoorOpen)
            {
                AdvanceSuccessHold(deltaTime);
                return;
            }
            AdvanceSimulation(active, deltaTime);
        }

        private void AdvanceSuccessHold(float deltaTime)
        {
            _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
            UpdateScene();
        }

        private void AdvanceSimulation(SocialStimulus active, float deltaTime)
        {
            Bladder = Mathf.Clamp01(Bladder + deltaTime * (_beatIndex >= 3 ? 0.018f : 0.009f));
            if (_beatIndex == 2 && (active & SocialStimulus.UnplugCharger) != 0)
                PhoneBattery = Mathf.Clamp01(PhoneBattery - deltaTime * 0.18f);
            AdvancePuzzle(active, deltaTime);
        }

        private void AdvanceToys(float deltaTime)
        {
            AdvanceToy(_playBall, ref _playBallVelocity, deltaTime, 230f);
            AdvanceToy(_squeakyToy, ref _squeakyToyVelocity, deltaTime, -145f);
        }

        private void AdvanceToy(GameObject toy, ref Vector2 velocity, float deltaTime, float spinSpeed)
        {
            if (toy == null || velocity.sqrMagnitude <= 0.001f) return;
            Vector2 position = (Vector2)toy.transform.position + velocity * deltaTime;
            float margin = 2f;
            position.x = Mathf.Clamp(position.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin);
            position.y = Mathf.Clamp(position.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin);
            toy.transform.position = new Vector3(position.x, position.y, toy.transform.position.z);
            toy.transform.Rotate(0f, 0f, spinSpeed * deltaTime * Mathf.Clamp01(velocity.magnitude / ToyKickSpeed));
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, deltaTime * 4.4f);
        }

        private SocialStimulus BuildActiveSet(float now)
        {
            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            bool CheddarAt(Vector2 p) => cheddar >= 0 && Vector2.Distance(_context.Dogs[cheddar].transform.position, p) <= StationRange;
            bool CocoaAt(Vector2 p) => cocoa >= 0 && Vector2.Distance(_context.Dogs[cocoa].transform.position, p) <= StationRange;

            SocialStimulus active = SocialStimulus.None;
            if (CocoaAt(_doorPosition)) active |= SocialStimulus.DoorStare;
            if (CheddarAt(_leashPosition)) active |= SocialStimulus.PresentLeash;
            if (CheddarAt(_hallwayPosition)) active |= SocialStimulus.BlockHallway;
            if (CocoaAt(_chargerPosition)) active |= SocialStimulus.UnplugCharger;
            if (_beatIndex == 3 ? now <= _unitedBarkSignalUntil : now <= _barkSignalUntil)
                active |= SocialStimulus.BarkRhythm;
            return active;
        }

        private bool BothDoorBarksRecent(float now) =>
            now - _lastDoorBarks[0] <= UnitedBarkWindow && now - _lastDoorBarks[1] <= UnitedBarkWindow;

        private void AdvancePuzzle(SocialStimulus active, float deltaTime)
        {
            ReactToNewSignals(active);
            _puzzle.SetActiveSet(active);
            _puzzle.Advance(deltaTime);
            if (_puzzle.Misreads > _beatMisreadsSeen)
            {
                int added = _puzzle.Misreads - _beatMisreadsSeen;
                Misreads += added;
                _beatMisreadsSeen = _puzzle.Misreads;
                string wrongThing = Misreads % 3 == 1 ? "TENNIS BALL?" : Misreads % 3 == 2 ? "BLANKET?" : "DINNER?";
                _context.SetCue($"Teenager misunderstood: {wrongThing} Funny, but not outside. Reset the marked jobs.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, $"MISREAD: {wrongThing}");
                _context.SpawnWorldPop(_teenager.transform.position, wrongThing, new Color(1f, 0.55f, 0.35f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
                _context.LogEvent("PeeBreakMisread", wrongThing);
                ShowMisreadProp(wrongThing);
            }

            if (_puzzle.Solved) AdvanceBeat();
            UpdateScene();
        }

        private void ReactToNewSignals(SocialStimulus active)
        {
            SocialStimulus newlyCorrect = (active & Required) & ~_lastActiveSet;
            _lastActiveSet = active;
            if (newlyCorrect == SocialStimulus.None) return;

            _signalReactionUntil = _context.Now() + 0.65f;
            SignalReactionCount++;
            PulseStimulus(newlyCorrect, SocialStimulus.DoorStare, _door);
            PulseStimulus(newlyCorrect, SocialStimulus.PresentLeash, _leashArt ?? _leash);
            PulseStimulus(newlyCorrect, SocialStimulus.BlockHallway, _hallwayRug ?? _hallway);
            PulseStimulus(newlyCorrect, SocialStimulus.UnplugCharger, _phoneArt ?? _charger);
            PulseStimulus(newlyCorrect, SocialStimulus.BarkRhythm, _teenagerArt ?? _teenager);
            _context.Pulse(_teenagerArt ?? _teenager, 0.2f);
            _context.RequestAudioCue(ArenaFeedbackCatalog.UiMenuFocus);
        }

        private void PulseStimulus(SocialStimulus active, SocialStimulus stimulus, GameObject target)
        {
            if ((active & stimulus) != 0 && target != null) _context.Pulse(target, 0.24f);
        }

        private void AdvanceBeat()
        {
            bool completedChargerGambit = _beatIndex == 2;
            CreditBeatRoles(_beatIndex);
            _beatIndex++;
            _context.AddScore(150, "TEENAGER COMPREHENSION");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, _beatIndex >= 4 ? "OH! YOU NEED TO GO GO!" : "TEENAGER LOOKS UP!");
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.LogEvent("PeeBreakBeat", $"completed {_beatIndex}/4");
            _context.LogObjectiveChanged();
            _context.Pulse(_teenagerArt ?? _teenager, 0.42f);
            _signalReactionUntil = _context.Now() + 0.9f;

            if (_beatIndex >= 4)
            {
                DoorOpen = true;
                _successHoldRemaining = DoorOpenPayoffSeconds;
                Bladder = 0f;
                StageDogsForDoorOpenPayoff();
                _context.SetCue("The Teenager finally gets it. Door open. OUTSIDE! Relief zoomies!");
                _context.SetFeedback(GameManager.FeedbackKind.LevelClear);
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "DOOR OPEN - RELIEF ZOOMIES!");
                _context.SpawnWorldPop(_doorPosition, "RELIEF ZOOMIES!", new Color(1f, 0.95f, 0.55f));
                _context.RequestRumble("pee_break_door_open", 0.45f, 0.75f, 0.3f);
                _context.LogEvent("PeeBreakDoorOpen", "united bark climax complete");
                return;
            }

            if (completedChargerGambit) PhoneBattery = 0f;

            ConfigureBeat();
        }

        private void StageDogsForDoorOpenPayoff()
        {
            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            PlaceDog(cheddar, _doorPosition + new Vector2(-7f, -4.8f));
            PlaceDog(cocoa, _doorPosition + new Vector2(-3.5f, -4.8f));
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();

            void PlaceDog(int index, Vector2 position)
            {
                if (index < 0 || _context.Dogs == null || index >= _context.Dogs.Length || _context.Dogs[index] == null) return;
                var dog = _context.Dogs[index];
                dog.transform.position = new Vector3(position.x, position.y, dog.transform.position.z);
                if (dog.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
        }

        private void CreditBeatRoles(int completedBeatIndex)
        {
            int cheddar = _context.IndexOfDog(DogId.Cheddar);
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            switch ((Beat)completedBeatIndex)
            {
                case Beat.DoorStare:
                    if (cocoa >= 0) _context.CreditDog(cocoa);
                    break;
                case Beat.LeashMessage:
                case Beat.ChargerGambit:
                case Beat.UnitedBark:
                    if (cheddar >= 0) _context.CreditDog(cheddar);
                    if (cocoa >= 0) _context.CreditDog(cocoa);
                    break;
            }
        }

        private void ConfigureBeat()
        {
            _puzzle.Configure(RequiredByBeat[_beatIndex], ComprehensionByBeat[_beatIndex], ConfusionByBeat[_beatIndex]);
            _beatMisreadsSeen = 0;
            _latestMisreadThing = string.Empty;
            _lastActiveSet = SocialStimulus.None;
            _barkSignalUntil = float.NegativeInfinity;
            _unitedBarkSignalUntil = float.NegativeInfinity;
            if (_misreadProp != null) _misreadProp.SetActive(false);
            if (_misreadAccent != null) _misreadAccent.SetActive(false);
        }

        private void BuildScene()
        {
            // The mission is an interior, so its foundation covers the complete arena bounds. This
            // prevents the backyard plate from leaking around the edge when the shared camera eases,
            // zooms, or frames the two dogs at opposite stations.
            _roomFloor = NewScenery("PeeBreakRoomFloor", new Color(0.34f, 0.25f, 0.19f),
                new Vector3(_context.Bounds.width + 6f, _context.Bounds.height + 6f, 1f), 0);
            _roomWall = NewScenery("PeeBreakRoomWall", new Color(0.58f, 0.46f, 0.34f), new Vector3(42f, 17f, 1f), -4);
            _roomWoodFloor = NewScenery("PeeBreakRoomWoodFloor", new Color(0.27f, 0.17f, 0.11f), new Vector3(42f, 9f, 1f), -3);
            _roomBaseboard = NewScenery("PeeBreakRoomBaseboard", new Color(0.78f, 0.63f, 0.43f), new Vector3(42f, 0.34f, 1f), -2);
            // The solid room foundation masks camera overscan outside the authored 16:9 plate.
            // The plates sit one layer above it and all mission props/dogs remain higher still.
            _livingRoomArt = NewGeneratedScenery("PeeBreakGeneratedLivingRoomArt", FinalGameplayArt.PeeBreakLivingRoomPlate, 1);
            _successRoomArt = NewGeneratedScenery("PeeBreakGeneratedLivingRoomSuccessArt", FinalGameplayArt.PeeBreakLivingRoomSuccessPlate, 1);
            _roomWindowArt = NewGeneratedScenery("PeeBreakRoomWindowArt", FinalGameplayArt.EnvironmentBackyardPlate, -1);
            _roomWindowTop = NewScenery("PeeBreakRoomWindowTop", new Color(0.22f, 0.12f, 0.07f), new Vector3(6.4f, 0.28f, 1f), 0);
            _roomWindowBottom = NewScenery("PeeBreakRoomWindowBottom", new Color(0.22f, 0.12f, 0.07f), new Vector3(6.4f, 0.28f, 1f), 0);
            _roomWindowLeft = NewScenery("PeeBreakRoomWindowLeft", new Color(0.22f, 0.12f, 0.07f), new Vector3(0.28f, 3.9f, 1f), 0);
            _roomWindowRight = NewScenery("PeeBreakRoomWindowRight", new Color(0.22f, 0.12f, 0.07f), new Vector3(0.28f, 3.9f, 1f), 0);
            _couchBack = NewScenery("PeeBreakCouchBack", new Color(0.22f, 0.36f, 0.55f), new Vector3(9.4f, 1.45f, 1f), 0);
            _couchSeat = NewScenery("PeeBreakCouchSeat", new Color(0.29f, 0.45f, 0.66f), new Vector3(8.8f, 2.75f, 1f), 0);
            _sideTable = NewScenery("PeeBreakSideTable", new Color(0.38f, 0.22f, 0.12f), new Vector3(1.4f, 1.2f, 1f), 0);
            _phoneGlow = NewSignalScenery("PeeBreakPhoneGlow", new Color(0.2f, 0.9f, 1f, 0.32f), new Vector3(2.2f, 2.2f, 1f), 4);
            _chargerCord = NewScenery("PeeBreakChargerCord", new Color(0.06f, 0.06f, 0.08f), new Vector3(5f, 0.16f, 1f), 1);
            _doorFrame = NewScenery("PeeBreakDoorFrame", new Color(0.38f, 0.18f, 0.08f), new Vector3(3.2f, 4.7f, 1f), 0);
            _closedDoorSlab = NewScenery("PeeBreakClosedDoorSlab", new Color(0.52f, 0.25f, 0.1f), new Vector3(2.55f, 4.35f, 1f), 1);
            _openSunbeam = NewSignalScenery("PeeBreakOpenSunbeam", new Color(1f, 0.9f, 0.35f, 0.55f), new Vector3(5.5f, 3.4f, 1f), 1);
            _leashHook = NewScenery("PeeBreakLeashHook", new Color(0.78f, 0.78f, 0.7f), new Vector3(0.7f, 0.7f, 1f), 1);
            _hallwayRug = NewSignalScenery("PeeBreakHallwayRug", new Color(0.92f, 0.42f, 0.18f, 0.26f), new Vector3(4.2f, 2.1f, 1f), -1);
            _door = NewMarker("PeeBreakDoor", new Color(1f, 0.82f, 0.3f), "DOOR - COCOA STARES", new Vector3(2.4f, 4f, 1f), out _doorLabel);
            _leash = NewMarker("PeeBreakLeash", new Color(0.3f, 0.9f, 1f), "LEASH - CHEDDAR PRESENTS", Vector3.one * 1.4f, out _leashLabel);
            _hallway = NewMarker("PeeBreakHallwayBlock", new Color(1f, 0.58f, 0.25f), "HALLWAY - CHEDDAR BLOCKS", Vector3.one * 2.6f, out _hallwayLabel);
            _charger = NewMarker("PeeBreakCharger", new Color(0.75f, 0.45f, 1f), "CHARGER - COCOA UNPLUGS", Vector3.one * 1.5f, out _chargerLabel);
            _cheddarCoach = NewMarker("PeeBreakCheddarCoach", new Color(0.55f, 0.78f, 1f), "CHEDDAR WATCH PAD\nNO BARK YET", new Vector3(1.7f, 1.1f, 1f), out _cheddarCoachLabel);
            // Keep the Teenager root at unit scale. The prior 3.1 x 4.7 marker scale also magnified
            // every child progress bar and silhouette block, producing the giant rectangles that
            // obscured the finished character art at 1080p.
            _teenager = NewMarker("PeeBreakTeenager", new Color(0.65f, 0.72f, 0.9f), "TEENAGER ?", Vector3.one, out _teenagerLabel);
            _phone = NewMarker("PeeBreakPhone", new Color(0.4f, 0.9f, 1f), "PHONE CHARGING", Vector3.one, out _phoneLabel);
            _bladderMeter = NewMarker("PeeBreakBladderMeter", new Color(0.4f, 0.8f, 1f), "BLADDER EMERGENCY", new Vector3(0.5f, 0.35f, 1f), out _bladderLabel);
            _misreadProp = NewMarker("PeeBreakMisreadProp", new Color(1f, 0.48f, 0.2f), "MISREAD", Vector3.one * 1.1f, out _misreadLabel);
            _misreadAccent = NewChildMarker(_misreadProp, "PeeBreakMisreadAccent", Color.white, new Vector3(0.16f, 1.4f, 1f), new Vector3(0f, 0f, -0.05f), 4);
            _cheddarUrgencyCue = NewScenery("PeeBreakCheddarUrgencyCue", new Color(1f, 0.9f, 0.22f, 0.82f), new Vector3(0.28f, 0.78f, 1f), 21);
            _cocoaUrgencyCue = NewScenery("PeeBreakCocoaUrgencyCue", new Color(0.55f, 0.95f, 1f, 0.82f), new Vector3(0.28f, 0.78f, 1f), 21);
            BuildRecognizableRoomDetails();
            BuildGeneratedPropArt();
            BuildOpeningExplainer();
            ApplyLivingRoomFallbackVisibility();
        }

        private void BuildOpeningExplainer()
        {
            _introVideoObject = new GameObject("PeeBreakOpeningExplainerVideo");
            _introVideoObject.SetActive(false);
            _introVideoPlayer = _introVideoObject.AddComponent<VideoPlayer>();
            _introVideoPlayer.playOnAwake = false;
            _introVideoPlayer.isLooping = false;
            _introVideoPlayer.waitForFirstFrame = true;
            _introVideoPlayer.skipOnDrop = true;
            _introVideoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            _introVideoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            _introVideoPlayer.targetCameraAlpha = 1f;
            _introVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            _introVideoPlayer.prepareCompleted += OnOpeningExplainerPrepared;
            _introVideoPlayer.loopPointReached += OnOpeningExplainerFinished;
            _introVideoPlayer.errorReceived += OnOpeningExplainerError;
        }

        private void StartOpeningPresentation()
        {
            OpeningExplainerAvailable = File.Exists(OpeningExplainerPath);
            _openingExplainerElapsed = 0f;
            IsPresentingOpening = OpeningExplainerAvailable && _introVideoPlayer != null;
            if (!IsPresentingOpening)
            {
                _context.LogEvent("PeeBreakIntro", $"explainer missing: {OpeningExplainerPath}");
                return;
            }

            _introVideoObject.SetActive(true);
            _introVideoPlayer.Stop();
            _introVideoPlayer.targetCamera = Camera.main;
            _introVideoPlayer.url = OpeningExplainerPath;
            _introVideoPlayer.Prepare();
            _context.LogEvent("PeeBreakIntro", "opening explainer preparing");
        }

        private void OnOpeningExplainerPrepared(VideoPlayer source)
        {
            if (!IsPresentingOpening || source == null) return;
            if (source.audioTrackCount > 0) source.SetDirectAudioMute(0, !_context.AudioEnabled());
            source.Play();
            _context.LogEvent("PeeBreakIntro", "opening explainer playing");
        }

        private void OnOpeningExplainerFinished(VideoPlayer source) =>
            FinishOpeningPresentation("video complete");

        private void OnOpeningExplainerError(VideoPlayer source, string message)
        {
            _context.LogEvent("PeeBreakIntro", $"video error: {message}");
            FinishOpeningPresentation("video error");
        }

        private void FinishOpeningPresentation(string reason)
        {
            bool wasPresenting = IsPresentingOpening;
            IsPresentingOpening = false;
            if (_introVideoPlayer != null) _introVideoPlayer.Stop();
            if (_introVideoObject != null) _introVideoObject.SetActive(false);
            if (wasPresenting && _context != null) _context.LogEvent("PeeBreakIntro", reason);
        }

        private void ApplyLivingRoomFallbackVisibility()
        {
            if (_livingRoomArt == null) return;
            SetRendererEnabled(_roomWall, false);
            SetRendererEnabled(_roomWoodFloor, false);
            SetRendererEnabled(_roomBaseboard, false);
            SetRendererEnabled(_roomWindowArt, false);
            SetRendererEnabled(_roomWindowTop, false);
            SetRendererEnabled(_roomWindowBottom, false);
            SetRendererEnabled(_roomWindowLeft, false);
            SetRendererEnabled(_roomWindowRight, false);
            SetRendererEnabled(_sideTable, false);
            SetRendererEnabled(_doorFrame, false);
            SetRendererEnabled(_closedDoorSlab, false);
            HideNamedChildren(_sideTable, "TableLeg", "TableTopLip", "WaterCup", "ChewToyUnderTable");
            HideNamedChildren(_door, "DoorPanel", "DoorKnob", "DoorMat", "Shoe", "DoorOutdoorView",
                "DoorOpenPanel", "OutdoorGrassPatch", "OutdoorFireHydrant");
        }

        private void BuildRecognizableRoomDetails()
        {
            NewChildScenery(_couchSeat, "PeeBreakCouchLeftArm", new Color(0.16f, 0.27f, 0.42f), new Vector3(0.16f, 1.28f, 1f), new Vector3(-0.56f, 0f, 0f), 1);
            NewChildScenery(_couchSeat, "PeeBreakCouchRightArm", new Color(0.16f, 0.27f, 0.42f), new Vector3(0.16f, 1.28f, 1f), new Vector3(0.56f, 0f, 0f), 1);
            NewChildScenery(_couchSeat, "PeeBreakCouchCushionLine", new Color(0.12f, 0.2f, 0.32f, 0.86f), new Vector3(0.06f, 0.92f, 1f), Vector3.zero, 1);
            NewChildScenery(_couchSeat, "PeeBreakCouchSeatFrontLip", new Color(0.12f, 0.21f, 0.34f, 0.82f), new Vector3(1.04f, 0.08f, 1f), new Vector3(0f, -0.46f, -0.01f), 2);
            NewChildScenery(_couchSeat, "PeeBreakCouchPillowA", new Color(0.88f, 0.72f, 0.42f), new Vector3(0.18f, 0.32f, 1f), new Vector3(-0.25f, 0.22f, -0.02f), 3);
            NewChildScenery(_couchSeat, "PeeBreakCouchPillowB", new Color(0.63f, 0.22f, 0.32f), new Vector3(0.2f, 0.3f, 1f), new Vector3(0.25f, 0.18f, -0.02f), 3);
            _couchBlanketSlump = NewChildScenery(_couchSeat, "PeeBreakCouchBlanketSlump", new Color(0.72f, 0.82f, 0.95f, 0.86f), new Vector3(0.28f, 0.34f, 1f), new Vector3(0.08f, -0.18f, -0.03f), 4);
            _straySockA = NewChildScenery(_couchSeat, "PeeBreakStraySockA", new Color(0.95f, 0.95f, 0.86f, 0.9f), new Vector3(0.1f, 0.22f, 1f), new Vector3(-0.42f, -0.58f, -0.04f), 4);
            _straySockB = NewChildScenery(_couchSeat, "PeeBreakStraySockB", new Color(0.22f, 0.28f, 0.38f, 0.88f), new Vector3(0.08f, 0.2f, 1f), new Vector3(-0.32f, -0.63f, -0.05f), 4);
            NewChildScenery(_sideTable, "PeeBreakTableLeg", new Color(0.2f, 0.1f, 0.05f), new Vector3(0.18f, 1.55f, 1f), new Vector3(0f, -0.58f, 0f), 1);
            NewChildScenery(_sideTable, "PeeBreakTableTopLip", new Color(0.55f, 0.32f, 0.16f), new Vector3(1.1f, 0.16f, 1f), new Vector3(0f, 0.48f, -0.01f), 2);
            NewChildScenery(_sideTable, "PeeBreakWaterCup", new Color(0.62f, 0.86f, 1f, 0.72f), new Vector3(0.22f, 0.34f, 1f), new Vector3(-0.28f, 0.7f, -0.02f), 3);
            _chewToyUnderTable = NewChildScenery(_sideTable, "PeeBreakChewToyUnderTable", new Color(0.95f, 0.56f, 0.18f, 0.92f), new Vector3(0.22f, 0.12f, 1f), new Vector3(0.34f, -0.72f, -0.03f), 4);

            _teenagerHead = NewChildScenery(_teenager, "PeeBreakTeenagerHead", new Color(0.95f, 0.72f, 0.52f), new Vector3(0.5f, 0.42f, 1f), new Vector3(0f, 0.42f, -0.01f), 4);
            NewChildScenery(_teenager, "PeeBreakTeenagerHair", new Color(0.12f, 0.08f, 0.05f), new Vector3(0.5f, 0.12f, 1f), new Vector3(0f, 0.58f, -0.02f), 5);
            NewChildScenery(_teenager, "PeeBreakTeenagerLegs", new Color(0.16f, 0.18f, 0.26f), new Vector3(0.95f, 0.24f, 1f), new Vector3(0f, -0.38f, -0.01f), 4);
            _teenagerHoodie = NewChildScenery(_teenager, "PeeBreakTeenagerHoodie", new Color(0.32f, 0.38f, 0.62f), new Vector3(0.82f, 0.82f, 1f), new Vector3(0f, -0.02f, 0.01f), 3);
            _teenagerThumbs = NewChildScenery(_teenager, "PeeBreakTeenagerThumbs", new Color(0.95f, 0.72f, 0.52f), new Vector3(0.42f, 0.12f, 1f), new Vector3(0.28f, 0.03f, -0.02f), 5);
            _teenagerFootWiggle = NewChildScenery(_teenager, "PeeBreakTeenagerFootWiggle", new Color(0.09f, 0.1f, 0.14f), new Vector3(0.32f, 0.1f, 1f), new Vector3(0.44f, -0.48f, -0.02f), 5);
            NewChildScenery(_teenager, "PeeBreakTeenagerAirPod", new Color(0.94f, 0.94f, 0.88f), new Vector3(0.1f, 0.18f, 1f), new Vector3(0.3f, 0.45f, -0.03f), 6);
            _teenagerPhoneBeam = NewChildScenery(_teenager, "PeeBreakTeenagerPhoneAttentionBeam", new Color(0.2f, 0.9f, 1f, 0.28f), new Vector3(2.3f, 0.08f, 1f), new Vector3(1.25f, -0.15f, -0.04f), 2);
            _teenagerDoorBeam = NewChildScenery(_teenager, "PeeBreakTeenagerDoorAttentionBeam", new Color(1f, 0.92f, 0.35f, 0.32f), new Vector3(3.3f, 0.08f, 1f), new Vector3(1.65f, -0.35f, -0.04f), 2);
            _teenagerQuestionBubble = NewChildSignal(_teenager, "PeeBreakTeenagerQuestionBubble", new Color(1f, 1f, 1f, 0.92f), new Vector3(0.82f, 0.82f, 1f), new Vector3(-2.3f, 2.55f, -0.05f), 17);
            _teenagerOhBubble = NewChildSignal(_teenager, "PeeBreakTeenagerOhBubble", new Color(1f, 0.92f, 0.36f, 0.96f), new Vector3(1.15f, 1.15f, 1f), new Vector3(-2.2f, 2.7f, -0.05f), 17);
            AddBubbleText(_teenagerQuestionBubble, "?", 52, new Color(0.12f, 0.16f, 0.2f), 18);
            AddBubbleText(_teenagerOhBubble, "OH!", 34, new Color(0.18f, 0.12f, 0.04f), 18);
            _comprehensionTrack = NewChildScenery(_teenager, "PeeBreakTeenagerComprehensionTrack", new Color(0.02f, 0.04f, 0.05f, 0.76f), new Vector3(4.2f, 0.24f, 1f), new Vector3(0f, 3.1f, -0.04f), 17);
            _comprehensionFill = NewChildScenery(_comprehensionTrack, "PeeBreakTeenagerComprehensionFill", new Color(0.3f, 1f, 0.55f, 0.94f), new Vector3(0.04f, 0.64f, 1f), Vector3.zero, 19);
            _confusionFill = NewChildScenery(_comprehensionTrack, "PeeBreakTeenagerConfusionFill", new Color(1f, 0.38f, 0.12f, 0.88f), new Vector3(0.04f, 0.24f, 1f), new Vector3(0f, -0.72f, -0.01f), 19);
            for (int i = 0; i < _beatPips.Length; i++)
            {
                _beatPips[i] = NewChildScenery(_teenager, $"PeeBreakBeatPip{i + 1}", new Color(0.16f, 0.2f, 0.22f, 0.86f),
                    new Vector3(0.24f, 0.24f, 1f), new Vector3(-1.15f + i * 0.76f, 3.55f, -0.05f), 18);
            }

            NewChildScenery(_phone, "PeeBreakPhoneScreen", new Color(0.02f, 0.04f, 0.08f), new Vector3(0.52f, 0.68f, 1f), Vector3.zero, 5);
            NewChildScenery(_phone, "PeeBreakPhoneReflection", new Color(0.7f, 1f, 1f, 0.62f), new Vector3(0.1f, 0.54f, 1f), new Vector3(-0.12f, 0f, -0.01f), 6);
            NewChildScenery(_phone, "PeeBreakPhoneBatteryShell", new Color(0.88f, 0.96f, 1f), new Vector3(0.42f, 0.08f, 1f), new Vector3(0f, -0.22f, -0.02f), 7);
            _phoneBatteryFill = NewChildScenery(_phone, "PeeBreakPhoneBatteryFill", new Color(0.2f, 1f, 0.55f), new Vector3(0.38f, 0.05f, 1f), new Vector3(0f, -0.22f, -0.03f), 8);
            _phoneChargeBolt = NewChildScenery(_phone, "PeeBreakPhoneChargeBolt", new Color(1f, 0.92f, 0.2f), new Vector3(0.08f, 0.32f, 1f), new Vector3(0.18f, 0.04f, -0.03f), 8);
            _phoneNotificationPing = NewChildSignal(_phone, "PeeBreakPhoneNotificationPing", new Color(1f, 0.95f, 0.28f, 0.88f), new Vector3(0.26f, 0.26f, 1f), new Vector3(-0.42f, 0.48f, -0.04f), 9);
            _phoneDeadSlash = NewChildScenery(_phone, "PeeBreakPhoneDeadSlash", new Color(1f, 0.2f, 0.12f), new Vector3(0.08f, 0.78f, 1f), Vector3.zero, 9);
            _phoneDeadSlash.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);

            NewChildScenery(_door, "PeeBreakDoorPanelTop", new Color(0.72f, 0.42f, 0.16f), new Vector3(0.62f, 0.2f, 1f), new Vector3(0f, 0.22f, -0.01f), 4);
            NewChildScenery(_door, "PeeBreakDoorPanelBottom", new Color(0.72f, 0.42f, 0.16f), new Vector3(0.62f, 0.2f, 1f), new Vector3(0f, -0.24f, -0.01f), 4);
            NewChildScenery(_door, "PeeBreakDoorKnob", new Color(1f, 0.96f, 0.55f), new Vector3(0.12f, 0.08f, 1f), new Vector3(0.34f, 0f, -0.02f), 5);
            _doorMat = NewChildScenery(_door, "PeeBreakDoorMat", new Color(0.2f, 0.32f, 0.18f), new Vector3(1.02f, 0.18f, 1f), new Vector3(-0.02f, -0.62f, -0.05f), 2);
            _shoeLeft = NewChildScenery(_door, "PeeBreakShoeLeft", new Color(0.08f, 0.07f, 0.06f), new Vector3(0.22f, 0.42f, 1f), new Vector3(-0.58f, -0.58f, -0.06f), 6);
            _shoeRight = NewChildScenery(_door, "PeeBreakShoeRight", new Color(0.1f, 0.08f, 0.06f), new Vector3(0.22f, 0.42f, 1f), new Vector3(-0.32f, -0.62f, -0.06f), 6);
            _shoeLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            _shoeRight.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            _doorOutdoorView = NewChildScenery(_door, "PeeBreakDoorOutdoorView", new Color(0.42f, 0.82f, 0.34f), new Vector3(0.72f, 0.82f, 1f), new Vector3(0.34f, 0f, 0.03f), 1);
            _doorOpenPanel = NewChildScenery(_door, "PeeBreakDoorOpenPanel", new Color(0.55f, 0.28f, 0.1f), new Vector3(0.26f, 0.96f, 1f), new Vector3(-0.42f, 0f, -0.02f), 5);
            _outdoorGrassPatch = NewChildScenery(_door, "PeeBreakOutdoorGrassPatch", new Color(0.18f, 0.62f, 0.22f), new Vector3(0.92f, 0.16f, 1f), new Vector3(0.42f, -0.34f, -0.04f), 6);
            _outdoorFireHydrant = NewChildScenery(_door, "PeeBreakOutdoorFireHydrant", new Color(0.92f, 0.16f, 0.12f), new Vector3(0.2f, 0.42f, 1f), new Vector3(0.62f, -0.06f, -0.05f), 7);
            NewChildScenery(_outdoorFireHydrant, "PeeBreakOutdoorHydrantCap", new Color(1f, 0.82f, 0.22f), new Vector3(1.25f, 0.2f, 1f), new Vector3(0f, 0.55f, -0.01f), 8);
            NewChildScenery(_outdoorFireHydrant, "PeeBreakOutdoorHydrantSidePeg", new Color(0.72f, 0.08f, 0.08f), new Vector3(1.6f, 0.18f, 1f), new Vector3(0f, 0.08f, -0.01f), 8);
            _reliefSparkleA = NewChildScenery(_door, "PeeBreakReliefSparkleA", new Color(1f, 0.95f, 0.35f), new Vector3(0.12f, 0.42f, 1f), new Vector3(0.18f, 0.5f, -0.06f), 8);
            _reliefSparkleB = NewChildScenery(_door, "PeeBreakReliefSparkleB", new Color(0.72f, 1f, 0.52f), new Vector3(0.1f, 0.34f, 1f), new Vector3(0.72f, 0.38f, -0.06f), 8);
            _reliefSparkleC = NewChildScenery(_door, "PeeBreakReliefSparkleC", new Color(0.55f, 0.95f, 1f), new Vector3(0.08f, 0.3f, 1f), new Vector3(0.52f, -0.46f, -0.06f), 8);

            NewChildScenery(_leash, "PeeBreakLeashStrap", new Color(0.02f, 0.16f, 0.23f), new Vector3(0.18f, 1.36f, 1f), Vector3.zero, 4);
            NewChildScenery(_leash, "PeeBreakLeashClip", new Color(0.86f, 0.86f, 0.74f), new Vector3(0.24f, 0.2f, 1f), new Vector3(0f, -0.52f, -0.01f), 5);
            NewChildScenery(_leash, "PeeBreakLeashHandleLoop", new Color(0.04f, 0.34f, 0.46f), new Vector3(0.46f, 0.34f, 1f), new Vector3(0f, 0.52f, -0.01f), 5);
            _leashPresentedTrail = NewChildScenery(_leash, "PeeBreakLeashPresentedTrail", new Color(0.2f, 0.95f, 1f, 0.34f), new Vector3(1.35f, 0.08f, 1f), new Vector3(0.55f, -0.06f, -0.02f), 3);
            NewChildScenery(_leashHook, "PeeBreakHookPeg", new Color(0.45f, 0.45f, 0.4f), new Vector3(0.26f, 0.1f, 1f), Vector3.zero, 2);
            NewChildScenery(_leashHook, "PeeBreakHangingLeashLoop", new Color(0.04f, 0.3f, 0.42f), new Vector3(0.44f, 0.48f, 1f), new Vector3(0f, -0.45f, -0.01f), 2);
            NewChildScenery(_leashHook, "PeeBreakHangingLeashTail", new Color(0.02f, 0.18f, 0.26f), new Vector3(0.12f, 1.05f, 1f), new Vector3(0.18f, -0.8f, -0.02f), 3);

            NewChildScenery(_charger, "PeeBreakOutletPlate", new Color(0.93f, 0.88f, 0.75f), new Vector3(0.46f, 0.34f, 1f), Vector3.zero, 4);
            NewChildScenery(_charger, "PeeBreakOutletSlots", new Color(0.16f, 0.12f, 0.18f), new Vector3(0.08f, 0.24f, 1f), new Vector3(0.08f, 0f, -0.01f), 5);
            NewChildScenery(_chargerCord, "PeeBreakCordPlug", new Color(0.04f, 0.04f, 0.06f), new Vector3(0.16f, 2.4f, 1f), new Vector3(0.5f, 0f, -0.01f), 2);
            _chargerPluggedEnd = NewChildScenery(_charger, "PeeBreakChargerPluggedEnd", new Color(0.04f, 0.04f, 0.06f), new Vector3(0.3f, 0.2f, 1f), new Vector3(-0.34f, 0f, -0.02f), 6);
            _chargerUnpluggedEnd = NewChildScenery(_charger, "PeeBreakChargerUnpluggedEnd", new Color(0.04f, 0.04f, 0.06f), new Vector3(0.36f, 0.18f, 1f), new Vector3(-0.68f, -0.42f, -0.02f), 6);

            NewChildScenery(_hallwayRug, "PeeBreakHallwayWallLeft", new Color(0.24f, 0.16f, 0.13f, 0.9f), new Vector3(0.08f, 1.1f, 1f), new Vector3(-0.48f, 0f, -0.01f), 0);
            NewChildScenery(_hallwayRug, "PeeBreakHallwayWallRight", new Color(0.24f, 0.16f, 0.13f, 0.9f), new Vector3(0.08f, 1.1f, 1f), new Vector3(0.48f, 0f, -0.01f), 0);

            _bladderWarningFill = NewChildScenery(_bladderMeter, "PeeBreakBladderWarningFill", new Color(1f, 0.35f, 0.2f, 0.62f), new Vector3(0.9f, 0.22f, 1f), Vector3.zero, 4);
            _bladderUrgencyTick = NewChildScenery(_bladderMeter, "PeeBreakBladderUrgencyTick", new Color(1f, 0.95f, 0.2f), new Vector3(0.08f, 0.7f, 1f), new Vector3(0.52f, 0f, -0.02f), 5);
        }

        private void BuildGeneratedPropArt()
        {
            GeneratedPeeBreakPropSpriteCount = 0;
            _couchArt = NewGeneratedProp("PeeBreakGeneratedCouchArt", FinalGameplayArt.PeeBreakCouch, 11);
            _teenagerArt = NewGeneratedProp("PeeBreakGeneratedTeenagerArt", FinalGameplayArt.PeeBreakTeenager, 15);
            // The dynamic battery, notification, and dead-state details sit at sorting orders 7-9;
            // the generated phone belongs just behind them so those state changes remain legible.
            _phoneArt = NewGeneratedProp("PeeBreakGeneratedPhoneChargerArt", FinalGameplayArt.PeeBreakPhoneCharger, 6);
            _openDoorArt = NewGeneratedProp("PeeBreakGeneratedOpenDoorArt", FinalGameplayArt.PeeBreakOpenDoor, 13);
            _leashArt = NewGeneratedProp("PeeBreakGeneratedLeashArt", FinalGameplayArt.PeeBreakLeash, 15);
            _hydrantArt = NewGeneratedProp("PeeBreakGeneratedHydrantReliefArt", FinalGameplayArt.PeeBreakHydrantRelief, 15);
            _bladderArt = NewGeneratedProp("PeeBreakGeneratedBladderMeterArt", FinalGameplayArt.PeeBreakBladderMeter, 15);
            _misreadTennisBallArt = NewGeneratedProp("PeeBreakGeneratedMisreadTennisBallArt", FinalGameplayArt.PeeBreakMisreadTennisBall, 16);
            _playBall = NewScenery("PeeBreakPlayBall", new Color(0.55f, 1f, 0.28f), Vector3.one * 0.72f, 16);
            _playBallArt = NewGeneratedProp("PeeBreakPlayBallArt", FinalGameplayArt.PeeBreakMisreadTennisBall, 17);
            if (_playBallArt != null) SetRendererEnabled(_playBall, false);
            _squeakyToy = NewScenery("PeeBreakSqueakyToy", new Color(1f, 0.65f, 0.16f), new Vector3(0.95f, 0.46f, 1f), 16);
            _squeakyToyHandle = NewChildScenery(_squeakyToy, "PeeBreakSqueakyToyHandle", new Color(0.8f, 0.18f, 0.16f),
                new Vector3(0.34f, 1.5f, 1f), new Vector3(-0.52f, 0f, -0.02f), 17);
            HideGeneratedBackedBlocks();
        }

        private void HideGeneratedBackedBlocks()
        {
            // Generated art should be the production read, not a translucent square sitting on top
            // of another translucent square. Keep placeholder objects alive for deterministic
            // lifecycle/debug tests, but do not render their block-built silhouettes when the final
            // sprite loaded successfully.
            if (_couchArt != null)
            {
                SetRendererEnabled(_couchBack, false);
                SetRendererEnabled(_couchSeat, false);
            }
            if (_teenagerArt != null) SetRendererEnabled(_teenager, false);
            if (_phoneArt != null)
            {
                SetRendererEnabled(_phone, false);
                SetRendererEnabled(_chargerCord, false);
            }
            HideNamedChildren(_couchSeat, "CouchLeftArm", "CouchRightArm", "CouchCushionLine",
                "CouchSeatFrontLip", "CouchPillowA", "CouchPillowB", "CouchBlanketSlump",
                "StraySockA", "StraySockB");
            HideNamedChildren(_teenager, "TeenagerHead", "TeenagerHair", "TeenagerLegs",
                "TeenagerHoodie", "TeenagerThumbs", "TeenagerFootWiggle", "TeenagerAirPod");
            HideNamedChildren(_phone, "PhoneScreen", "PhoneReflection");
            HideNamedChildren(_chargerCord, "CordPlug");
            if (_leashArt != null)
            {
                HideNamedChildren(_leash, "LeashStrap", "LeashClip", "LeashHandleLoop");
                SetRendererEnabled(_leashHook, false);
                HideNamedChildren(_leashHook, "HookPeg", "HangingLeashLoop", "HangingLeashTail");
            }
        }

        private GameObject NewScenery(string name, Color color, Vector3 scale, int sortingOrder)
        {
            var marker = new GameObject(name);
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = _context.ActorSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            marker.transform.localScale = scale;
            marker.SetActive(false);
            return marker;
        }

        private GameObject NewSignalScenery(string name, Color color, Vector3 scale, int sortingOrder)
        {
            var marker = NewScenery(name, color, scale, sortingOrder);
            marker.GetComponent<SpriteRenderer>().sprite = _context.RangeSprite ?? _context.ActorSprite;
            return marker;
        }

        private GameObject NewGeneratedScenery(string name, string resourcePath, int sortingOrder)
        {
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return null;

            var scenery = new GameObject(name);
            var renderer = scenery.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            scenery.SetActive(false);
            return scenery;
        }

        private GameObject NewGeneratedProp(string name, string resourcePath, int sortingOrder)
        {
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return null;

            var marker = new GameObject(name);
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            marker.SetActive(false);
            GeneratedPeeBreakPropSpriteCount++;
            return marker;
        }

        private static void HideNamedChildren(GameObject parent, params string[] fragments)
        {
            if (parent == null) return;
            foreach (Transform child in parent.transform)
            {
                foreach (string fragment in fragments)
                {
                    if (child.name.Contains(fragment))
                    {
                        SetRendererEnabled(child.gameObject, false);
                        break;
                    }
                }
            }
        }

        private static void SetRendererEnabled(GameObject marker, bool enabled)
        {
            if (marker == null || !marker.TryGetComponent<SpriteRenderer>(out var renderer)) return;
            renderer.enabled = enabled;
        }

        private GameObject NewChildScenery(GameObject parent, string name, Color color, Vector3 scale, Vector3 localPosition, int sortingOrder)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent.transform);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = scale;
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = _context.ActorSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return marker;
        }

        private GameObject NewChildSignal(GameObject parent, string name, Color color, Vector3 scale, Vector3 localPosition, int sortingOrder)
        {
            var marker = NewChildScenery(parent, name, color, scale, localPosition, sortingOrder);
            marker.GetComponent<SpriteRenderer>().sprite = _context.RangeSprite ?? _context.ActorSprite;
            return marker;
        }

        private static void AddBubbleText(GameObject bubble, string copy, int fontSize, Color color, int sortingOrder)
        {
            if (bubble == null) return;
            var textObject = new GameObject("IconText");
            textObject.transform.SetParent(bubble.transform);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            textObject.transform.localScale = Vector3.one * 0.1f;
            var text = textObject.AddComponent<TextMesh>();
            text.text = copy;
            text.fontSize = fontSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            if (text.TryGetComponent<MeshRenderer>(out var renderer)) renderer.sortingOrder = sortingOrder;
        }

        private GameObject NewMarker(string name, Color color, string label, Vector3 scale)
        {
            return NewMarker(name, color, label, scale, out _);
        }

        private GameObject NewMarker(string name, Color color, string label, Vector3 scale, out TextMesh worldLabel)
        {
            var marker = new GameObject(name);
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = _context.RangeSprite ?? _context.ActorSprite;
            renderer.color = color;
            renderer.sortingOrder = 3;
            marker.transform.localScale = scale;
            worldLabel = _context.AddWorldLabel(marker, label, Vector3.up * 0.68f, 9, Color.white);
            marker.SetActive(false);
            return marker;
        }

        private GameObject NewChildMarker(GameObject parent, string name, Color color, Vector3 scale, Vector3 localPosition, int sortingOrder)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent.transform);
            marker.transform.localPosition = localPosition;
            marker.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            marker.transform.localScale = scale;
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = _context.RangeSprite ?? _context.ActorSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            marker.SetActive(false);
            return marker;
        }

        private void SetSceneActive(bool active)
        {
            foreach (var marker in new[]
            {
                _roomFloor, _roomWall, _roomWoodFloor, _roomBaseboard, _livingRoomArt, _successRoomArt, _roomWindowArt,
                _roomWindowTop, _roomWindowBottom, _roomWindowLeft, _roomWindowRight,
                _couchBack, _couchSeat, _sideTable, _phoneGlow, _chargerCord, _doorFrame, _closedDoorSlab, _openSunbeam,
                _leashHook, _hallwayRug, _door, _leash, _hallway, _charger, _cheddarCoach, _teenager, _phone,
                _bladderMeter, _misreadProp, _misreadAccent, _cheddarUrgencyCue, _cocoaUrgencyCue,
                _couchArt, _teenagerArt, _phoneArt, _openDoorArt,
                _leashArt, _hydrantArt, _bladderArt, _misreadTennisBallArt,
                _playBall, _playBallArt, _squeakyToy
            })
                if (marker != null) marker.SetActive(active);
            if (active && _misreadProp != null) _misreadProp.SetActive(false);
            if (active && _misreadAccent != null) _misreadAccent.SetActive(false);
            if (active && _misreadTennisBallArt != null) _misreadTennisBallArt.SetActive(false);
            if (active && _openSunbeam != null) _openSunbeam.SetActive(false);
            if (active && _hydrantArt != null) _hydrantArt.SetActive(false);
            if (active && _hallwayRug != null) _hallwayRug.SetActive(false);
            if (active) UpdateScene();
        }

        private void UpdateScene()
        {
            if (_door == null) return;
            Vector2 roomCenter = _context.Bounds.center + new Vector2(4f, 3.6f);
            Vector2 windowCenter = _context.Bounds.center + new Vector2(-5.5f, 8f);
            _roomFloor.transform.position = _context.Bounds.center;
            bool hasSuccessPlate = _successRoomArt != null;
            PlaceGeneratedArt(_livingRoomArt, roomCenter, 5.7f, !DoorOpen || !hasSuccessPlate);
            PlaceGeneratedArt(_successRoomArt, roomCenter, 5.7f, DoorOpen);
            _roomWall.transform.position = roomCenter + new Vector2(0f, 2.3f);
            _roomWoodFloor.transform.position = roomCenter + new Vector2(0f, -6.1f);
            _roomBaseboard.transform.position = roomCenter + new Vector2(0f, -1.6f);
            PlaceGeneratedArt(_roomWindowArt, windowCenter, 0.9f);
            SetGeneratedArtTint(_roomWindowArt, new Color(0.82f, 0.86f, 0.78f, 0.86f));
            _roomWindowTop.transform.position = windowCenter + new Vector2(0f, 1.95f);
            _roomWindowBottom.transform.position = windowCenter + new Vector2(0f, -1.95f);
            _roomWindowLeft.transform.position = windowCenter + new Vector2(-3.2f, 0f);
            _roomWindowRight.transform.position = windowCenter + new Vector2(3.2f, 0f);
            _couchBack.transform.position = _context.Bounds.center + new Vector2(-1.5f, 6.1f);
            _couchSeat.transform.position = _context.Bounds.center + new Vector2(-1.5f, 4.9f);
            _sideTable.transform.position = _context.Bounds.center + new Vector2(3f, 4.45f);
            _doorFrame.transform.position = _doorPosition;
            _closedDoorSlab.transform.position = _doorPosition;
            _openSunbeam.transform.position = _doorPosition + new Vector2(1.8f, -1.2f);
            _leashHook.transform.position = _leashPosition + new Vector2(-0.45f, 1.05f);
            _hallwayRug.transform.position = _hallwayPosition;
            _door.transform.position = _doorPosition;
            _leash.transform.position = _leashPosition;
            _cheddarCoach.transform.position = _cheddarCoachPosition;
            _hallway.transform.position = _hallwayPosition;
            _charger.transform.position = _chargerPosition;
            bool cocoaAtDoor = DogAt(DogId.Cocoa, _doorPosition);
            bool cheddarAtLeash = DogAt(DogId.Cheddar, _leashPosition);
            bool cheddarAtCoach = DogAt(DogId.Cheddar, _cheddarCoachPosition);
            bool cheddarAtHallway = DogAt(DogId.Cheddar, _hallwayPosition);
            bool cocoaAtCharger = DogAt(DogId.Cocoa, _chargerPosition);
            UpdateTeenPresentationState(cocoaAtDoor, cheddarAtLeash, cheddarAtHallway, cocoaAtCharger);
            // Distance signal: each beat's required stations raise a command badge until their dog
            // is actually holding the spot (held is the resolved state, like the escape gap).
            ActorSignalBadge.SetStationSignal(_door,
                !DoorOpen && (_beatIndex == 0 || _beatIndex == 1 || _beatIndex == 3) && !cocoaAtDoor);
            ActorSignalBadge.SetStationSignal(_cheddarCoach, !DoorOpen && _beatIndex == 0 && !cheddarAtCoach);
            ActorSignalBadge.SetStationSignal(_leash,
                !DoorOpen && (_beatIndex == 1 || _beatIndex == 3) && !cheddarAtLeash);
            ActorSignalBadge.SetStationSignal(_hallway, !DoorOpen && _beatIndex == 2 && !cheddarAtHallway);
            ActorSignalBadge.SetStationSignal(_charger, !DoorOpen && _beatIndex == 2 && !cocoaAtCharger);
            _teenager.transform.position = TeenState == TeenPresentationState.StandingSuccess
                ? _context.Bounds.center + new Vector2(6.3f, 4.8f)
                : _context.Bounds.center + new Vector2(0.25f, 4.9f);
            _phone.transform.position = (Vector2)_teenager.transform.position + new Vector2(2.2f, -0.45f);
            _phoneGlow.transform.position = _phone.transform.position;
            _chargerCord.transform.position = ((_phone.transform.position + _charger.transform.position) * 0.5f);
            Vector2 cordDelta = (Vector2)(_charger.transform.position - _phone.transform.position);
            _chargerCord.transform.localScale = new Vector3(Mathf.Max(0.5f, cordDelta.magnitude), 0.16f, 1f);
            _chargerCord.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(cordDelta.y, cordDelta.x) * Mathf.Rad2Deg);
            _bladderMeter.transform.position = _doorPosition + new Vector2(0f, -3.2f);
            _misreadProp.transform.position = (Vector2)_teenager.transform.position + new Vector2(2f, -1.3f);

            _door.transform.localScale = DoorOpen ? new Vector3(0.35f, 4f, 1f) : new Vector3(2.4f, 4f, 1f);
            float pulse = 1f + Mathf.Sin(Time.time * 7.5f) * 0.04f;
            // The distracted Teenager sprite already includes its beanbag. A second full couch
            // behind it read as duplicate furniture, so keep the standalone couch as fallback-only.
            PlaceGeneratedArt(_couchArt, _couchSeat.transform.position + new Vector3(-0.2f, 0.35f, -0.25f), 4.65f,
                _teenagerArt == null && !DoorOpen);
            PlaceGeneratedArt(_teenagerArt, _teenager.transform.position + new Vector3(0f, TeenState == TeenPresentationState.StandingSuccess ? 0.24f : -0.08f, -0.25f),
                3.2f,
                !DoorOpen,
                TeenState == TeenPresentationState.AnnoyedReacting ? Mathf.Sin(Time.time * 12f) * 2.5f : 0f);
            bool chargerBeat = CurrentBeat == Beat.ChargerGambit;
            Vector3 phoneArtPosition = chargerBeat
                ? ((Vector2)_phone.transform.position + _chargerPosition) * 0.5f
                : _phone.transform.position + new Vector3(0.2f, 0f, -0.25f);
            float phoneArtScale = chargerBeat ? 2.05f + Mathf.Sin(Time.time * 6f) * 0.06f : 1.35f;
            PlaceGeneratedArt(_phoneArt, phoneArtPosition + new Vector3(0f, 0f, -0.25f), phoneArtScale,
                !DoorOpen && chargerBeat && PhoneBattery > 0.02f, 0f);
            SetGeneratedArtTint(_phoneArt, chargerBeat ? Color.white : new Color(0.86f, 0.94f, 1f, 0.92f));
            // The success plate contains the open architectural doorway and standing Teenager.
            // Retain the isolated door sprite only as fallback if that full-state plate is absent.
            PlaceGeneratedArt(_openDoorArt, _doorFrame.transform.position + new Vector3(0.25f, 0f, -0.25f), 2.75f,
                DoorOpen && !hasSuccessPlate);
            bool leashRelevant = _beatIndex == 1 || _beatIndex == 3;
            float leashScale = leashRelevant ? 1.04f + (!cheddarAtLeash ? Mathf.Sin(Time.time * 7f) * 0.05f : 0f) : 0.76f;
            PlaceGeneratedArt(_leashArt, _leash.transform.position + new Vector3(0.05f, 0f, -0.25f), leashScale,
                !DoorOpen && (_beatIndex == 1 || _beatIndex == 3 || _beatIndex == 0));
            SetGeneratedArtTint(_leashArt, leashRelevant ? Color.white : new Color(0.68f, 0.76f, 0.78f, 0.62f));
            PlaceGeneratedArt(_hydrantArt, _doorPosition + new Vector2(2.25f, -0.75f), 1.4f, DoorOpen,
                Mathf.Sin(Time.time * 6f) * 3f);
            PlaceGeneratedArt(_bladderArt, _bladderMeter.transform.position + new Vector3(0f, 0.25f, -0.25f),
                Mathf.Lerp(0.34f, 0.66f, Bladder), !DoorOpen, Mathf.Sin(Time.time * 8f) * Mathf.Lerp(0f, 7f, Bladder));
            PlaceGeneratedArt(_misreadTennisBallArt, _misreadProp.transform.position + new Vector3(0f, 0.25f, -0.25f),
                0.55f, _misreadProp.activeSelf && _latestMisreadThing == "TENNIS BALL?", Mathf.Sin(Time.time * 9f) * 8f);
            PlaceGeneratedArt(_playBallArt, _playBall.transform.position + new Vector3(0f, 0.08f, -0.25f),
                0.46f, !DoorOpen, _playBall.transform.eulerAngles.z);
            if (_squeakyToyHandle != null)
                _squeakyToyHandle.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 5.4f) * 8f);
            if (_context.Now() <= _signalReactionUntil && _teenagerArt != null)
            {
                float reaction = 1f + Mathf.Abs(Mathf.Sin(Time.time * 15f)) * 0.06f;
                _teenagerArt.transform.localScale = Vector3.one * (3.2f * reaction);
            }
            if (_teenagerThumbs != null)
            {
                bool phoneInHand = TeenState != TeenPresentationState.StandingSuccess;
                _teenagerThumbs.SetActive(phoneInHand);
                if (phoneInHand)
                    _teenagerThumbs.transform.localPosition = new Vector3(0.28f + Mathf.Sin(Time.time * 16f) * 0.04f, 0.03f, -0.02f);
            }
            if (_teenagerHead != null)
            {
                float glance = TeenState == TeenPresentationState.AnnoyedReacting || TeenState == TeenPresentationState.StandingSuccess ? -0.08f : 0f;
                _teenagerHead.transform.localPosition = new Vector3(glance + Mathf.Sin(Time.time * 2.2f) * 0.018f,
                    0.42f + Mathf.Sin(Time.time * 1.7f) * 0.016f, -0.01f);
            }
            if (_teenagerHoodie != null)
            {
                float standStretch = TeenState == TeenPresentationState.StandingSuccess ? 1.26f : 1f;
                _teenagerHoodie.transform.localScale = new Vector3(0.82f, (0.82f + Mathf.Sin(Time.time * 1.7f) * 0.018f) * standStretch, 1f);
            }
            if (_teenagerFootWiggle != null)
                _teenagerFootWiggle.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 10.5f) * 14f);
            if (_couchBlanketSlump != null)
                _couchBlanketSlump.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.35f) * 3f);
            if (_straySockA != null)
                _straySockA.transform.localRotation = Quaternion.Euler(0f, 0f, -16f + Mathf.Sin(Time.time * 1.8f) * 2f);
            if (_straySockB != null)
                _straySockB.transform.localRotation = Quaternion.Euler(0f, 0f, 14f + Mathf.Sin(Time.time * 1.5f) * 2f);
            if (_chewToyUnderTable != null)
                _chewToyUnderTable.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.4f) * 5f);
            if (_phoneNotificationPing != null)
            {
                bool phoneDistracting = !DoorOpen && PhoneBattery > 0.08f && CurrentBeat != Beat.ChargerGambit;
                _phoneNotificationPing.SetActive(phoneDistracting);
                if (phoneDistracting)
                {
                    float ping = 1f + Mathf.Abs(Mathf.Sin(Time.time * 5.8f)) * 0.55f;
                    _phoneNotificationPing.transform.localScale = new Vector3(0.26f, 0.26f, 1f) * ping;
                }
            }
            if (_phoneGlow != null)
            {
                _phoneGlow.SetActive(!DoorOpen && PhoneBattery > 0.02f);
                _phoneGlow.transform.localScale = new Vector3(2.2f, 2.2f, 1f) * Mathf.Lerp(0.92f, pulse, PhoneBattery);
            }
            UpdateTeenagerProgressRead();
            SetMarkerColor(_door, cocoaAtDoor ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.82f, 0.3f));
            SetMarkerColor(_leash, cheddarAtLeash ? new Color(0.5f, 1f, 0.5f) : new Color(0.3f, 0.9f, 1f));
            SetMarkerColor(_cheddarCoach, cheddarAtCoach ? new Color(0.5f, 1f, 0.5f) : new Color(0.55f, 0.78f, 1f));
            SetMarkerColor(_hallway, cheddarAtHallway ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.58f, 0.25f));
            SetMarkerColor(_charger, cocoaAtCharger ? new Color(0.5f, 1f, 0.5f) : new Color(0.75f, 0.45f, 1f));
            _phone.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.2f, 0.15f, 0.25f), new Color(0.4f, 0.9f, 1f), PhoneBattery);
            _phoneGlow.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.08f, 0.08f, 0.12f, 0.16f), new Color(0.2f, 0.9f, 1f, 0.5f), PhoneBattery);
            if (_phoneBatteryFill != null)
            {
                _phoneBatteryFill.transform.localScale = new Vector3(Mathf.Lerp(0.04f, 0.38f, PhoneBattery), 0.05f, 1f);
                _phoneBatteryFill.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(1f, 0.16f, 0.08f), new Color(0.2f, 1f, 0.55f), PhoneBattery);
            }
            if (_phoneChargeBolt != null) _phoneChargeBolt.SetActive(PhoneBattery > 0.08f && CurrentBeat != Beat.UnitedBark);
            if (_phoneDeadSlash != null) _phoneDeadSlash.SetActive(PhoneBattery <= 0.08f);
            _bladderMeter.transform.localScale = new Vector3(Mathf.Lerp(0.5f, 4f, Bladder), 0.35f, 1f);
            _bladderMeter.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.4f, 0.8f, 1f), new Color(1f, 0.35f, 0.2f), Bladder);
            if (_bladderWarningFill != null)
            {
                _bladderWarningFill.SetActive(Bladder >= 0.45f);
                _bladderWarningFill.transform.localScale = new Vector3(Mathf.Lerp(0.15f, 0.92f, Bladder), 0.22f, 1f);
            }
            if (_bladderUrgencyTick != null)
            {
                _bladderUrgencyTick.SetActive(Bladder >= 0.72f);
                _bladderUrgencyTick.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 12f) * 12f);
            }
            UpdateDogUrgencyCue(DogId.Cheddar, _cheddarUrgencyCue, 0f);
            UpdateDogUrgencyCue(DogId.Cocoa, _cocoaUrgencyCue, 1.3f);
            if (_doorLabel != null)
                _doorLabel.text = DoorOpen ? "DOOR OPEN"
                    : CurrentBeat == Beat.ChargerGambit ? "DOOR WAITING"
                    : CurrentBeat == Beat.UnitedBark && cocoaAtDoor && !cheddarAtLeash ? "COCOA STARE LOCKED\nNEEDS CHEDDAR LEASH"
                    : CurrentBeat == Beat.UnitedBark && cocoaAtDoor && cheddarAtLeash ? "COCOA STARE LOCKED\nBARK TOGETHER"
                    : CurrentBeat == Beat.LeashMessage && cocoaAtDoor && !cheddarAtLeash ? "COCOA STARE LOCKED\nNEEDS CHEDDAR LEASH"
                    : cocoaAtDoor ? "COCOA STARE LOCKED" : "COCOA STAND HERE\nDOOR STARE";
            if (_leashLabel != null)
                _leashLabel.text = CurrentBeat == Beat.LeashMessage && cheddarAtLeash && !cocoaAtDoor ? "CHEDDAR LEASH READY\nNEEDS COCOA STARE"
                    : CurrentBeat == Beat.UnitedBark && cheddarAtLeash && !cocoaAtDoor ? "CHEDDAR LEASH READY\nNEEDS COCOA STARE"
                    : CurrentBeat == Beat.UnitedBark && cheddarAtLeash && cocoaAtDoor ? "CHEDDAR LEASH READY\nBARK TOGETHER"
                    : cheddarAtLeash ? "CHEDDAR LEASH READY" : CurrentBeat == Beat.UnitedBark ? "CHEDDAR HOLD LEASH\nTHEN BOTH BARK" : "CHEDDAR STAND HERE\nPRESENT LEASH";
            if (_cheddarCoachLabel != null)
                _cheddarCoachLabel.text = cheddarAtCoach ? "CHEDDAR WATCHING\nNO BARK YET" : "CHEDDAR WATCH PAD\nNO BARK YET";
            if (_hallwayLabel != null)
                _hallwayLabel.text = cheddarAtHallway && !cocoaAtCharger ? "CHEDDAR BLOCK LOCKED\nNEEDS COCOA CHARGER"
                    : cheddarAtHallway ? "CHEDDAR BLOCK LOCKED" : "CHEDDAR STAND HERE\nBLOCK HALLWAY";
            if (_chargerLabel != null)
                _chargerLabel.text = cocoaAtCharger && !cheddarAtHallway ? "COCOA UNPLUGGING\nNEEDS CHEDDAR BLOCK"
                    : cocoaAtCharger ? "COCOA UNPLUGGING" : "COCOA STAND HERE\nUNPLUG CHARGER";
            if (_phoneLabel != null)
                _phoneLabel.text = PhoneBattery <= 0.08f ? "PHONE DEAD"
                    : PhoneBattery <= 0.35f ? "PHONE LOW"
                    : PhoneBattery < 0.995f ? "PHONE DRAINING"
                    : "PHONE CHARGING";
            if (_bladderLabel != null) _bladderLabel.text = "BLADDER EMERGENCY";
            if (_teenagerLabel != null)
                _teenagerLabel.text = !string.IsNullOrEmpty(_latestMisreadThing) ? $"TEENAGER: {_latestMisreadThing}"
                    : CurrentBeat == Beat.LeashMessage && cocoaAtDoor && !cheddarAtLeash ? "TEENAGER: NEEDS LEASH TOO"
                    : CurrentBeat == Beat.LeashMessage && cheddarAtLeash && !cocoaAtDoor ? "TEENAGER: NEEDS STARE TOO"
                    : CurrentBeat == Beat.LeashMessage && cocoaAtDoor && cheddarAtLeash ? "TEENAGER: GETTING IT!"
                    : CurrentBeat == Beat.ChargerGambit && cheddarAtHallway && !cocoaAtCharger ? "TEENAGER: NEEDS CHARGER TOO"
                    : CurrentBeat == Beat.ChargerGambit && cocoaAtCharger && !cheddarAtHallway ? "TEENAGER: NEEDS HALLWAY BLOCK"
                    : CurrentBeat == Beat.ChargerGambit && cheddarAtHallway && cocoaAtCharger ? "TEENAGER: PHONE FADING!"
                    : CurrentBeat == Beat.UnitedBark && cocoaAtDoor && !cheddarAtLeash ? "TEENAGER: NEEDS LEASH + BARK"
                    : CurrentBeat == Beat.UnitedBark && cheddarAtLeash && !cocoaAtDoor ? "TEENAGER: NEEDS STARE + BARK"
                    : CurrentBeat == Beat.UnitedBark && cocoaAtDoor && cheddarAtLeash ? "TEENAGER: BARK TOGETHER!"
                    : CurrentBeat switch
                {
                    Beat.DoorStare => "TEENAGER: SCROLLING",
                    Beat.LeashMessage => "TEENAGER: LOOKING UP?",
                    Beat.ChargerGambit => "TEENAGER: PHONE FADING",
                    Beat.UnitedBark => "TEENAGER: ALMOST GETS IT",
                    _ => "TEENAGER: OH! OUTSIDE!"
                };
            _cheddarCoach.SetActive(!DoorOpen && _beatIndex == 0);
            _hallway.SetActive(!DoorOpen && _beatIndex == 2);
            _charger.SetActive(!DoorOpen && _beatIndex == 2);
            _chargerCord.SetActive(!DoorOpen && _beatIndex == 2);
            _hallwayRug.SetActive(!DoorOpen && _beatIndex == 2);
            _closedDoorSlab.SetActive(!DoorOpen);
            _doorFrame.SetActive(!DoorOpen);
            _openSunbeam.SetActive(DoorOpen);
            _leash.SetActive(!DoorOpen && (_beatIndex == 1 || _beatIndex == 3));
            if (_teenagerPhoneBeam != null) _teenagerPhoneBeam.SetActive(!DoorOpen && PhoneBattery > 0.08f && CurrentBeat != Beat.ChargerGambit);
            if (_teenagerDoorBeam != null) _teenagerDoorBeam.SetActive(!DoorOpen && (cocoaAtDoor || cheddarAtLeash || CurrentBeat == Beat.UnitedBark || PhoneBattery <= 0.08f));
            if (_teenagerQuestionBubble != null) _teenagerQuestionBubble.SetActive(!DoorOpen && CurrentBeat != Beat.UnitedBark && PhoneBattery > 0.08f);
            if (_teenagerOhBubble != null) _teenagerOhBubble.SetActive(!DoorOpen && (CurrentBeat == Beat.UnitedBark || PhoneBattery <= 0.08f));
            if (_chargerPluggedEnd != null) _chargerPluggedEnd.SetActive(!DoorOpen && CurrentBeat == Beat.ChargerGambit && PhoneBattery > 0.08f);
            if (_chargerUnpluggedEnd != null) _chargerUnpluggedEnd.SetActive(CurrentBeat == Beat.UnitedBark || PhoneBattery <= 0.08f);
            if (_doorOutdoorView != null) _doorOutdoorView.SetActive(DoorOpen);
            if (_doorOpenPanel != null) _doorOpenPanel.SetActive(DoorOpen);
            if (_outdoorGrassPatch != null) _outdoorGrassPatch.SetActive(DoorOpen);
            if (_outdoorFireHydrant != null) _outdoorFireHydrant.SetActive(DoorOpen);
            AnimateReliefSparkle(_reliefSparkleA, DoorOpen, 0f);
            AnimateReliefSparkle(_reliefSparkleB, DoorOpen, 0.7f);
            AnimateReliefSparkle(_reliefSparkleC, DoorOpen, 1.4f);
            if (_leashPresentedTrail != null) _leashPresentedTrail.SetActive(!DoorOpen && (_beatIndex == 1 || _beatIndex == 3) && cheddarAtLeash);
            ApplyPromptVisibility();
        }

        private void ApplyPromptVisibility()
        {
            bool debug = _context.DebugPresentationEnabled();
            bool doorRelevant = !DoorOpen && CurrentBeat != Beat.ChargerGambit;
            SetMarkerPrompt(_door, _doorLabel, doorRelevant && (debug || AnyDogNear(_doorPosition)));
            SetMarkerPrompt(_leash, _leashLabel, !DoorOpen && (_beatIndex == 1 || _beatIndex == 3) && (debug || AnyDogNear(_leashPosition)));
            SetMarkerPrompt(_cheddarCoach, _cheddarCoachLabel, !DoorOpen && _beatIndex == 0 && (debug || AnyDogNear(_cheddarCoachPosition)));
            SetMarkerPrompt(_hallway, _hallwayLabel, !DoorOpen && _beatIndex == 2 && (debug || AnyDogNear(_hallwayPosition)));
            SetMarkerPrompt(_charger, _chargerLabel, !DoorOpen && _beatIndex == 2 && (debug || AnyDogNear(_chargerPosition)));
            SetMarkerPrompt(_phone, _phoneLabel, debug || AnyDogNear(_phone.transform.position));
            SetMarkerPrompt(_bladderMeter, _bladderLabel, debug);
            SetMarkerPrompt(_teenager, _teenagerLabel, debug);
            SetMarkerPrompt(_misreadProp, _misreadLabel, _misreadProp != null && _misreadProp.activeSelf && (debug || AnyDogNear(_misreadProp.transform.position)));
        }

        private void SetMarkerPrompt(GameObject marker, TextMesh label, bool visible)
        {
            if (marker != null && marker.TryGetComponent<SpriteRenderer>(out var renderer))
                renderer.enabled = visible && _context.DebugPresentationEnabled();
            if (label != null) label.gameObject.SetActive(visible);
        }

        private static void AnimateReliefSparkle(GameObject sparkle, bool active, float phase)
        {
            if (sparkle == null) return;
            sparkle.SetActive(active);
            if (!active) return;
            float wag = Mathf.Sin(Time.time * 9f + phase);
            sparkle.transform.localRotation = Quaternion.Euler(0f, 0f, wag * 22f);
            float scale = 0.92f + wag * 0.08f;
            sparkle.transform.localScale = new Vector3(sparkle.transform.localScale.x, Mathf.Max(0.08f, scale * 0.34f), 1f);
        }

        private void UpdateTeenagerProgressRead()
        {
            float comprehensionNeeded = _beatIndex < ComprehensionByBeat.Length ? ComprehensionByBeat[_beatIndex] : 1f;
            float confusionMax = _beatIndex < ConfusionByBeat.Length ? ConfusionByBeat[_beatIndex] : 1f;
            float comprehension = DoorOpen ? 1f : Mathf.Clamp01(_puzzle.Comprehension / comprehensionNeeded);
            float confusion = DoorOpen ? 0f : Mathf.Clamp01(_puzzle.Confusion / confusionMax);

            if (_comprehensionTrack != null)
                _comprehensionTrack.SetActive(!DoorOpen);
            if (_comprehensionFill != null)
            {
                _comprehensionFill.transform.localScale = new Vector3(Mathf.Lerp(0.04f, 0.98f, comprehension), 0.64f, 1f);
                _comprehensionFill.GetComponent<SpriteRenderer>().color =
                    Color.Lerp(new Color(0.3f, 0.7f, 1f, 0.8f), new Color(0.45f, 1f, 0.35f, 0.96f), comprehension);
            }
            if (_confusionFill != null)
            {
                _confusionFill.SetActive(!DoorOpen && confusion > 0.02f);
                _confusionFill.transform.localScale = new Vector3(Mathf.Lerp(0.04f, 0.98f, confusion), 0.24f, 1f);
            }

            for (int i = 0; i < _beatPips.Length; i++)
            {
                var pip = _beatPips[i];
                if (pip == null) continue;
                bool completed = _beatIndex > i || DoorOpen;
                bool current = _beatIndex == i && !DoorOpen;
                pip.SetActive(!DoorOpen);
                pip.transform.localScale = Vector3.one * (current ? 0.3f + Mathf.Sin(Time.time * 8f) * 0.025f : 0.22f);
                SetMarkerColor(pip, completed ? new Color(0.55f, 1f, 0.35f, 0.96f)
                    : current ? new Color(1f, 0.88f, 0.28f, 0.96f)
                    : new Color(0.16f, 0.2f, 0.22f, 0.86f));
            }
        }

        private void UpdateTeenPresentationState(bool cocoaAtDoor, bool cheddarAtLeash, bool cheddarAtHallway, bool cocoaAtCharger)
        {
            if (DoorOpen)
            {
                TeenState = TeenPresentationState.StandingSuccess;
                return;
            }

            bool partialMessage = CurrentBeat switch
            {
                Beat.LeashMessage => cocoaAtDoor ^ cheddarAtLeash,
                Beat.ChargerGambit => cheddarAtHallway ^ cocoaAtCharger,
                Beat.UnitedBark => cocoaAtDoor ^ cheddarAtLeash,
                _ => false
            };

            if (!string.IsNullOrEmpty(_latestMisreadThing) || _puzzle.Confusion > 0.05f || partialMessage)
                TeenState = TeenPresentationState.AnnoyedReacting;
            else if (_beatIndex > 0 && _puzzle.Comprehension <= 0.02f && PhoneBattery > 0.08f)
                TeenState = TeenPresentationState.DistractedAgain;
            else
                TeenState = TeenPresentationState.DistractedIdle;
        }

        private void UpdateDogUrgencyCue(DogId dogId, GameObject cue, float phase)
        {
            if (cue == null) return;
            int index = _context.IndexOfDog(dogId);
            bool active = !DoorOpen && Bladder >= 0.42f && index >= 0 && index < _context.Dogs.Length && _context.Dogs[index] != null;
            cue.SetActive(active);
            if (!active) return;

            var dog = _context.Dogs[index];
            cue.transform.position = dog.transform.position + new Vector3(0f, 1.15f + Mathf.Sin(Time.time * 9f + phase) * 0.12f, -0.2f);
            float scale = Mathf.Lerp(0.42f, 0.88f, Bladder);
            cue.transform.localScale = new Vector3(0.22f, 0.66f, 1f) * scale;
            cue.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 13f + phase) * 18f);
            if (Bladder >= 0.72f && _context.DogFeedback != null && index < _context.DogFeedback.Length)
                _context.DogFeedback[index]?.ShowPanic();
        }

        private bool AnyDogNear(Vector2 position)
        {
            if (_context.Dogs == null) return false;
            foreach (var dog in _context.Dogs)
                if (dog != null && Vector2.Distance(dog.transform.position, position) <= PromptRange)
                    return true;
            return false;
        }

        private bool AnyDogNear(Vector3 position) => AnyDogNear((Vector2)position);

        private bool DogAt(DogId dogId, Vector2 position)
        {
            int dog = _context.IndexOfDog(dogId);
            return dog >= 0 && Vector2.Distance(_context.Dogs[dog].transform.position, position) <= StationRange;
        }

        private static void SetMarkerColor(GameObject marker, Color color)
        {
            if (marker != null && marker.TryGetComponent<SpriteRenderer>(out var renderer))
                renderer.color = color;
        }

        private static void PlaceGeneratedArt(GameObject art, Vector3 position, float scale, bool active = true, float zRotation = 0f)
        {
            if (art == null) return;
            art.SetActive(active);
            if (!active) return;
            art.transform.position = position;
            art.transform.localScale = Vector3.one * scale;
            art.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
        }

        private static void SetGeneratedArtTint(GameObject art, Color color)
        {
            if (art != null && art.TryGetComponent<SpriteRenderer>(out var renderer)) renderer.color = color;
        }

        private void ShowMisreadProp(string wrongThing)
        {
            _latestMisreadThing = wrongThing;
            if (_misreadProp == null) return;
            bool tennisBall = wrongThing == "TENNIS BALL?";
            _misreadProp.SetActive(true);
            _misreadProp.transform.localScale = tennisBall ? Vector3.one * 1.35f : Vector3.one * 1.1f;
            SetMarkerColor(_misreadProp, tennisBall ? new Color(0.55f, 1f, 0.28f) : new Color(1f, 0.48f, 0.2f));
            if (_misreadAccent != null) _misreadAccent.SetActive(tennisBall);
            if (_misreadLabel != null)
                _misreadLabel.text = $"{wrongThing}\nWRONG IDEA\nTRY DOG JOBS";
        }
    }
}
