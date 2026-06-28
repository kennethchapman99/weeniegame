using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned four-beat deep slice in which the dogs combine exact, role-locked signals
    /// to convince the Teenager to open the door. Misreads reset the current attempt, never the run.
    /// </summary>
    public sealed class PeeBreakMissionController : IMissionController
    {
        public enum Beat
        {
            DoorStare,
            LeashMessage,
            ChargerGambit,
            UnitedBark,
            Complete
        }

        private const float StationRange = 2.25f;
        private const float UnitedBarkWindow = 0.8f;

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
        private GameObject _couchBack;
        private GameObject _couchSeat;
        private GameObject _sideTable;
        private GameObject _phoneGlow;
        private GameObject _chargerCord;
        private GameObject _doorFrame;
        private GameObject _openSunbeam;
        private GameObject _leashHook;
        private GameObject _hallwayRug;
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
        private int _beatIndex;
        private int _beatMisreadsSeen;
        private string _latestMisreadThing = string.Empty;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.OperationPeeBreak;
        public Beat CurrentBeat => (Beat)Mathf.Clamp(_beatIndex, 0, 4);
        public int CompletedBeats => Mathf.Clamp(_beatIndex, 0, 4);
        public int Misreads { get; private set; }
        public float Bladder { get; private set; }
        public float PhoneBattery { get; private set; }
        public bool DoorOpen { get; private set; }
        public bool IsComplete => DoorOpen;
        public CoopSocialManipulationPuzzle Puzzle => _puzzle;
        public SocialStimulus Required => _beatIndex < RequiredByBeat.Length ? RequiredByBeat[_beatIndex] : SocialStimulus.None;
        public Vector2 DoorPosition => _doorPosition;
        public Vector2 LeashPosition => _leashPosition;
        public Vector2 HallwayPosition => _hallwayPosition;
        public Vector2 ChargerPosition => _chargerPosition;
        public Vector2 EntryTarget => _doorPosition;
        public string OutcomeSummary => null;
        public bool IsFailed => false;
        public string FailReason => null;

        public string ObjectiveLabel
        {
            get
            {
                string meters = $"BLADDER {Bladder * 100f:0}% / PHONE {PhoneBattery * 100f:0}% / misreads {Misreads}";
                return CurrentBeat switch
                {
                    Beat.DoorStare => $"1/4 Cocoa holds DOOR STARE; Cheddar watches/no bark - {meters}",
                    Beat.LeashMessage => $"2/4 Cocoa STARE + Cheddar PRESENT LEASH - {meters}",
                    Beat.ChargerGambit => $"3/4 Cheddar BLOCK HALLWAY + Cocoa UNPLUG CHARGER - {meters}",
                    Beat.UnitedBark => $"4/4 Hold STARE + LEASH, then BOTH BARK by the door - {meters}",
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
            _doorPosition = new Vector2(_context.Bounds.center.x + 10f, _context.Bounds.center.y + 5f);
            _leashPosition = _doorPosition + new Vector2(-2.5f, -0.5f);
            _cheddarCoachPosition = _doorPosition + new Vector2(-5f, -2.2f);
            _hallwayPosition = new Vector2(_context.Bounds.center.x - 3f, _context.Bounds.center.y);
            _chargerPosition = new Vector2(_context.Bounds.center.x + 2f, _context.Bounds.center.y + 1.5f);
            _beatIndex = 0;
            Misreads = 0;
            Bladder = 0.12f;
            PhoneBattery = 1f;
            DoorOpen = false;
            _latestMisreadThing = string.Empty;
            _lastDoorBarks[0] = _lastDoorBarks[1] = float.NegativeInfinity;
            _barkSignalUntil = float.NegativeInfinity;
            _unitedBarkSignalUntil = float.NegativeInfinity;
            ConfigureBeat();
            SetSceneActive(true);
            UpdateScene();
        }

        public void Tick(float deltaTime, float now)
        {
            if (DoorOpen || deltaTime <= 0f) return;
            AdvanceSimulation(BuildActiveSet(now), deltaTime);
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

        public void Cleanup() => SetSceneActive(false);

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
            if (DoorOpen || deltaTime <= 0f) return;
            AdvanceSimulation(active, deltaTime);
        }

        private void AdvanceSimulation(SocialStimulus active, float deltaTime)
        {
            Bladder = Mathf.Clamp01(Bladder + deltaTime * (_beatIndex >= 3 ? 0.018f : 0.009f));
            if (_beatIndex == 2 && (active & SocialStimulus.UnplugCharger) != 0)
                PhoneBattery = Mathf.Clamp01(PhoneBattery - deltaTime * 0.18f);
            AdvancePuzzle(active, deltaTime);
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

        private void AdvanceBeat()
        {
            bool completedChargerGambit = _beatIndex == 2;
            _beatIndex++;
            _context.AddScore(150, "TEENAGER COMPREHENSION");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, _beatIndex >= 4 ? "OH! YOU NEED TO GO GO!" : "TEENAGER LOOKS UP!");
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.LogEvent("PeeBreakBeat", $"completed {_beatIndex}/4");
            _context.LogObjectiveChanged();

            if (_beatIndex >= 4)
            {
                DoorOpen = true;
                Bladder = 0f;
                _context.SetCue("The Teenager finally gets it. Door open. OUTSIDE!");
                _context.SetFeedback(GameManager.FeedbackKind.LevelClear);
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "DOOR OPEN - SUNSHINE!");
                _context.SpawnWorldPop(_doorPosition, "OUTSIDE!", new Color(1f, 0.95f, 0.55f));
                _context.RequestRumble("pee_break_door_open", 0.45f, 0.75f, 0.3f);
                _context.LogEvent("PeeBreakDoorOpen", "united bark climax complete");
                return;
            }

            if (completedChargerGambit) PhoneBattery = 0f;

            ConfigureBeat();
        }

        private void ConfigureBeat()
        {
            _puzzle.Configure(RequiredByBeat[_beatIndex], ComprehensionByBeat[_beatIndex], ConfusionByBeat[_beatIndex]);
            _beatMisreadsSeen = 0;
            _latestMisreadThing = string.Empty;
            _barkSignalUntil = float.NegativeInfinity;
            _unitedBarkSignalUntil = float.NegativeInfinity;
            if (_misreadProp != null) _misreadProp.SetActive(false);
            if (_misreadAccent != null) _misreadAccent.SetActive(false);
        }

        private void BuildScene()
        {
            _roomFloor = NewScenery("PeeBreakRoomFloor", new Color(0.36f, 0.3f, 0.24f), new Vector3(25f, 15f, 1f), -2);
            _couchBack = NewScenery("PeeBreakCouchBack", new Color(0.22f, 0.36f, 0.55f), new Vector3(7.5f, 1.2f, 1f), 0);
            _couchSeat = NewScenery("PeeBreakCouchSeat", new Color(0.29f, 0.45f, 0.66f), new Vector3(7f, 2.2f, 1f), 0);
            _sideTable = NewScenery("PeeBreakSideTable", new Color(0.38f, 0.22f, 0.12f), new Vector3(1.4f, 1.2f, 1f), 0);
            _phoneGlow = NewScenery("PeeBreakPhoneGlow", new Color(0.2f, 0.9f, 1f, 0.38f), new Vector3(1.25f, 1.25f, 1f), 1);
            _chargerCord = NewScenery("PeeBreakChargerCord", new Color(0.06f, 0.06f, 0.08f), new Vector3(5f, 0.16f, 1f), 1);
            _doorFrame = NewScenery("PeeBreakDoorFrame", new Color(0.38f, 0.18f, 0.08f), new Vector3(3.2f, 4.7f, 1f), 0);
            _openSunbeam = NewScenery("PeeBreakOpenSunbeam", new Color(1f, 0.9f, 0.35f, 0.62f), new Vector3(5.5f, 3.4f, 1f), 1);
            _leashHook = NewScenery("PeeBreakLeashHook", new Color(0.78f, 0.78f, 0.7f), new Vector3(0.7f, 0.7f, 1f), 1);
            _hallwayRug = NewScenery("PeeBreakHallwayRug", new Color(0.56f, 0.19f, 0.19f), new Vector3(5.8f, 2f, 1f), -1);
            _door = NewMarker("PeeBreakDoor", new Color(1f, 0.82f, 0.3f), "DOOR - COCOA STARES", new Vector3(2.4f, 4f, 1f), out _doorLabel);
            _leash = NewMarker("PeeBreakLeash", new Color(0.3f, 0.9f, 1f), "LEASH - CHEDDAR PRESENTS", Vector3.one * 1.4f, out _leashLabel);
            _hallway = NewMarker("PeeBreakHallwayBlock", new Color(1f, 0.58f, 0.25f), "HALLWAY - CHEDDAR BLOCKS", Vector3.one * 2.6f, out _hallwayLabel);
            _charger = NewMarker("PeeBreakCharger", new Color(0.75f, 0.45f, 1f), "CHARGER - COCOA UNPLUGS", Vector3.one * 1.5f, out _chargerLabel);
            _cheddarCoach = NewMarker("PeeBreakCheddarCoach", new Color(0.55f, 0.78f, 1f), "CHEDDAR WATCH PAD\nNO BARK YET", new Vector3(1.7f, 1.1f, 1f), out _cheddarCoachLabel);
            _teenager = NewMarker("PeeBreakTeenager", new Color(0.65f, 0.72f, 0.9f), "TEENAGER ?", new Vector3(2f, 2.6f, 1f), out _teenagerLabel);
            _phone = NewMarker("PeeBreakPhone", new Color(0.4f, 0.9f, 1f), "PHONE 100%", Vector3.one * 0.7f, out _phoneLabel);
            _bladderMeter = NewMarker("PeeBreakBladderMeter", new Color(0.4f, 0.8f, 1f), "BLADDER 12%", new Vector3(0.5f, 0.35f, 1f), out _bladderLabel);
            _misreadProp = NewMarker("PeeBreakMisreadProp", new Color(1f, 0.48f, 0.2f), "MISREAD", Vector3.one * 1.1f, out _misreadLabel);
            _misreadAccent = NewChildMarker(_misreadProp, "PeeBreakMisreadAccent", Color.white, new Vector3(0.16f, 1.4f, 1f), new Vector3(0f, 0f, -0.05f), 4);
            BuildRecognizableRoomDetails();
        }

        private void BuildRecognizableRoomDetails()
        {
            NewChildScenery(_couchSeat, "PeeBreakCouchLeftArm", new Color(0.16f, 0.27f, 0.42f), new Vector3(0.16f, 1.28f, 1f), new Vector3(-0.56f, 0f, 0f), 1);
            NewChildScenery(_couchSeat, "PeeBreakCouchRightArm", new Color(0.16f, 0.27f, 0.42f), new Vector3(0.16f, 1.28f, 1f), new Vector3(0.56f, 0f, 0f), 1);
            NewChildScenery(_couchSeat, "PeeBreakCouchCushionLine", new Color(0.12f, 0.2f, 0.32f, 0.86f), new Vector3(0.06f, 0.92f, 1f), Vector3.zero, 1);
            NewChildScenery(_sideTable, "PeeBreakTableLeg", new Color(0.2f, 0.1f, 0.05f), new Vector3(0.18f, 1.55f, 1f), new Vector3(0f, -0.58f, 0f), 1);

            NewChildScenery(_teenager, "PeeBreakTeenagerHead", new Color(0.95f, 0.72f, 0.52f), new Vector3(0.42f, 0.34f, 1f), new Vector3(0f, 0.42f, -0.01f), 4);
            NewChildScenery(_teenager, "PeeBreakTeenagerHair", new Color(0.12f, 0.08f, 0.05f), new Vector3(0.42f, 0.1f, 1f), new Vector3(0f, 0.58f, -0.02f), 5);
            NewChildScenery(_teenager, "PeeBreakTeenagerLegs", new Color(0.16f, 0.18f, 0.26f), new Vector3(0.78f, 0.18f, 1f), new Vector3(0f, -0.38f, -0.01f), 4);
            NewChildScenery(_teenager, "PeeBreakTeenagerThumbs", new Color(0.95f, 0.72f, 0.52f), new Vector3(0.32f, 0.1f, 1f), new Vector3(0.25f, 0.03f, -0.02f), 5);

            NewChildScenery(_phone, "PeeBreakPhoneScreen", new Color(0.02f, 0.04f, 0.08f), new Vector3(0.52f, 0.68f, 1f), Vector3.zero, 5);
            NewChildScenery(_phone, "PeeBreakPhoneReflection", new Color(0.7f, 1f, 1f, 0.62f), new Vector3(0.1f, 0.54f, 1f), new Vector3(-0.12f, 0f, -0.01f), 6);

            NewChildScenery(_door, "PeeBreakDoorPanelTop", new Color(0.72f, 0.42f, 0.16f), new Vector3(0.62f, 0.2f, 1f), new Vector3(0f, 0.22f, -0.01f), 4);
            NewChildScenery(_door, "PeeBreakDoorPanelBottom", new Color(0.72f, 0.42f, 0.16f), new Vector3(0.62f, 0.2f, 1f), new Vector3(0f, -0.24f, -0.01f), 4);
            NewChildScenery(_door, "PeeBreakDoorKnob", new Color(1f, 0.96f, 0.55f), new Vector3(0.12f, 0.08f, 1f), new Vector3(0.34f, 0f, -0.02f), 5);

            NewChildScenery(_leash, "PeeBreakLeashStrap", new Color(0.02f, 0.16f, 0.23f), new Vector3(0.18f, 1.36f, 1f), Vector3.zero, 4);
            NewChildScenery(_leash, "PeeBreakLeashClip", new Color(0.86f, 0.86f, 0.74f), new Vector3(0.24f, 0.2f, 1f), new Vector3(0f, -0.52f, -0.01f), 5);
            NewChildScenery(_leashHook, "PeeBreakHookPeg", new Color(0.45f, 0.45f, 0.4f), new Vector3(0.26f, 0.1f, 1f), Vector3.zero, 2);
            NewChildScenery(_leashHook, "PeeBreakHangingLeashLoop", new Color(0.04f, 0.3f, 0.42f), new Vector3(0.44f, 0.48f, 1f), new Vector3(0f, -0.45f, -0.01f), 2);
            NewChildScenery(_leashHook, "PeeBreakHangingLeashTail", new Color(0.02f, 0.18f, 0.26f), new Vector3(0.12f, 1.05f, 1f), new Vector3(0.18f, -0.8f, -0.02f), 3);

            NewChildScenery(_charger, "PeeBreakOutletPlate", new Color(0.93f, 0.88f, 0.75f), new Vector3(0.46f, 0.34f, 1f), Vector3.zero, 4);
            NewChildScenery(_charger, "PeeBreakOutletSlots", new Color(0.16f, 0.12f, 0.18f), new Vector3(0.08f, 0.24f, 1f), new Vector3(0.08f, 0f, -0.01f), 5);
            NewChildScenery(_chargerCord, "PeeBreakCordPlug", new Color(0.04f, 0.04f, 0.06f), new Vector3(0.16f, 2.4f, 1f), new Vector3(0.5f, 0f, -0.01f), 2);
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
            worldLabel = _context.AddWorldLabel(marker, label, Vector3.up * 0.55f, 12, Color.white);
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
                _roomFloor, _couchBack, _couchSeat, _sideTable, _phoneGlow, _chargerCord, _doorFrame, _openSunbeam,
                _leashHook, _hallwayRug, _door, _leash, _hallway, _charger, _cheddarCoach, _teenager, _phone,
                _bladderMeter, _misreadProp, _misreadAccent
            })
                if (marker != null) marker.SetActive(active);
            if (active && _misreadProp != null) _misreadProp.SetActive(false);
            if (active && _misreadAccent != null) _misreadAccent.SetActive(false);
            if (active && _openSunbeam != null) _openSunbeam.SetActive(false);
            if (active) UpdateScene();
        }

        private void UpdateScene()
        {
            if (_door == null) return;
            _roomFloor.transform.position = _context.Bounds.center + new Vector2(2f, 3.6f);
            _couchBack.transform.position = _context.Bounds.center + new Vector2(2f, 6.4f);
            _couchSeat.transform.position = _context.Bounds.center + new Vector2(2f, 5.2f);
            _sideTable.transform.position = _context.Bounds.center + new Vector2(5.3f, 5.1f);
            _doorFrame.transform.position = _doorPosition;
            _openSunbeam.transform.position = _doorPosition + new Vector2(1.8f, -1.2f);
            _leashHook.transform.position = _leashPosition + new Vector2(-0.45f, 1.05f);
            _hallwayRug.transform.position = _hallwayPosition;
            _door.transform.position = _doorPosition;
            _leash.transform.position = _leashPosition;
            _cheddarCoach.transform.position = _cheddarCoachPosition;
            _hallway.transform.position = _hallwayPosition;
            _charger.transform.position = _chargerPosition;
            _teenager.transform.position = _context.Bounds.center + new Vector2(2f, 5f);
            _phone.transform.position = (Vector2)_teenager.transform.position + new Vector2(0.8f, -0.2f);
            _phoneGlow.transform.position = _phone.transform.position;
            _chargerCord.transform.position = ((_phone.transform.position + _charger.transform.position) * 0.5f);
            Vector2 cordDelta = (Vector2)(_charger.transform.position - _phone.transform.position);
            _chargerCord.transform.localScale = new Vector3(Mathf.Max(0.5f, cordDelta.magnitude), 0.16f, 1f);
            _chargerCord.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(cordDelta.y, cordDelta.x) * Mathf.Rad2Deg);
            _bladderMeter.transform.position = _doorPosition + new Vector2(0f, -3.2f);
            _misreadProp.transform.position = (Vector2)_teenager.transform.position + new Vector2(2f, -1.3f);

            bool cocoaAtDoor = DogAt(DogId.Cocoa, _doorPosition);
            bool cheddarAtLeash = DogAt(DogId.Cheddar, _leashPosition);
            bool cheddarAtCoach = DogAt(DogId.Cheddar, _cheddarCoachPosition);
            bool cheddarAtHallway = DogAt(DogId.Cheddar, _hallwayPosition);
            bool cocoaAtCharger = DogAt(DogId.Cocoa, _chargerPosition);
            _door.transform.localScale = DoorOpen ? new Vector3(0.35f, 4f, 1f) : new Vector3(2.4f, 4f, 1f);
            SetMarkerColor(_door, cocoaAtDoor ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.82f, 0.3f));
            SetMarkerColor(_leash, cheddarAtLeash ? new Color(0.5f, 1f, 0.5f) : new Color(0.3f, 0.9f, 1f));
            SetMarkerColor(_cheddarCoach, cheddarAtCoach ? new Color(0.5f, 1f, 0.5f) : new Color(0.55f, 0.78f, 1f));
            SetMarkerColor(_hallway, cheddarAtHallway ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.58f, 0.25f));
            SetMarkerColor(_charger, cocoaAtCharger ? new Color(0.5f, 1f, 0.5f) : new Color(0.75f, 0.45f, 1f));
            _phone.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.2f, 0.15f, 0.25f), new Color(0.4f, 0.9f, 1f), PhoneBattery);
            _phoneGlow.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.08f, 0.08f, 0.12f, 0.16f), new Color(0.2f, 0.9f, 1f, 0.5f), PhoneBattery);
            _bladderMeter.transform.localScale = new Vector3(Mathf.Lerp(0.5f, 4f, Bladder), 0.35f, 1f);
            _bladderMeter.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.4f, 0.8f, 1f), new Color(1f, 0.35f, 0.2f), Bladder);
            if (_doorLabel != null)
                _doorLabel.text = DoorOpen ? "DOOR OPEN"
                    : CurrentBeat == Beat.ChargerGambit ? "DOOR WAITING"
                    : CurrentBeat == Beat.LeashMessage && cocoaAtDoor && !cheddarAtLeash ? "COCOA STARE LOCKED\nNEEDS CHEDDAR LEASH"
                    : cocoaAtDoor ? "COCOA STARE LOCKED" : "COCOA STAND HERE\nDOOR STARE";
            if (_leashLabel != null)
                _leashLabel.text = CurrentBeat == Beat.LeashMessage && cheddarAtLeash && !cocoaAtDoor ? "CHEDDAR LEASH READY\nNEEDS COCOA STARE"
                    : cheddarAtLeash ? "CHEDDAR LEASH READY" : CurrentBeat == Beat.UnitedBark ? "CHEDDAR HOLD LEASH\nTHEN BOTH BARK" : "CHEDDAR STAND HERE\nPRESENT LEASH";
            if (_cheddarCoachLabel != null)
                _cheddarCoachLabel.text = cheddarAtCoach ? "CHEDDAR WATCHING\nNO BARK YET" : "CHEDDAR WATCH PAD\nNO BARK YET";
            if (_hallwayLabel != null)
                _hallwayLabel.text = cheddarAtHallway ? "CHEDDAR BLOCK LOCKED" : "CHEDDAR STAND HERE\nBLOCK HALLWAY";
            if (_chargerLabel != null)
                _chargerLabel.text = cocoaAtCharger ? "COCOA UNPLUGGING" : "COCOA STAND HERE\nUNPLUG CHARGER";
            if (_phoneLabel != null) _phoneLabel.text = $"PHONE {PhoneBattery * 100f:0}%";
            if (_bladderLabel != null) _bladderLabel.text = $"BLADDER {Bladder * 100f:0}%";
            if (_teenagerLabel != null)
                _teenagerLabel.text = !string.IsNullOrEmpty(_latestMisreadThing) ? $"TEENAGER: {_latestMisreadThing}"
                    : CurrentBeat == Beat.LeashMessage && cocoaAtDoor && !cheddarAtLeash ? "TEENAGER: NEEDS LEASH TOO"
                    : CurrentBeat == Beat.LeashMessage && cheddarAtLeash && !cocoaAtDoor ? "TEENAGER: NEEDS STARE TOO"
                    : CurrentBeat == Beat.LeashMessage && cocoaAtDoor && cheddarAtLeash ? "TEENAGER: GETTING IT!"
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
            _openSunbeam.SetActive(DoorOpen);
            _leash.SetActive(!DoorOpen && (_beatIndex == 1 || _beatIndex == 3));
        }

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
