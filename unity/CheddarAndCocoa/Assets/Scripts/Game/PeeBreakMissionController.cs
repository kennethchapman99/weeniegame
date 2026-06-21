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
        private GameObject _teenager;
        private GameObject _phone;
        private GameObject _bladderMeter;
        private TextMesh _teenagerLabel;
        private TextMesh _phoneLabel;
        private TextMesh _bladderLabel;
        private Vector2 _doorPosition;
        private Vector2 _leashPosition;
        private Vector2 _hallwayPosition;
        private Vector2 _chargerPosition;
        private float[] _lastDoorBarks = { float.NegativeInfinity, float.NegativeInfinity };
        private float _barkSignalUntil = float.NegativeInfinity;
        private int _beatIndex;
        private int _beatMisreadsSeen;

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

        public string ObjectiveLabel
        {
            get
            {
                string meters = $"BLADDER {Bladder * 100f:0}% / PHONE {PhoneBattery * 100f:0}% / misreads {Misreads}";
                return CurrentBeat switch
                {
                    Beat.DoorStare => $"1/4 Cocoa: hold the DOOR STARE - {meters}",
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
            _hallwayPosition = new Vector2(_context.Bounds.center.x - 3f, _context.Bounds.center.y);
            _chargerPosition = new Vector2(_context.Bounds.center.x + 2f, _context.Bounds.center.y + 1.5f);
            _beatIndex = 0;
            Misreads = 0;
            Bladder = 0.12f;
            PhoneBattery = 1f;
            DoorOpen = false;
            _lastDoorBarks[0] = _lastDoorBarks[1] = float.NegativeInfinity;
            _barkSignalUntil = float.NegativeInfinity;
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
            if (cheddar >= 0) _context.Dogs[cheddar].transform.position = _doorPosition + new Vector2(-7f, -3f);
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
                    target = _door.transform;
                    copy = cheddar ? "WATCH COCOA" : "HOLD DOOR STARE";
                    break;
                case Beat.LeashMessage:
                case Beat.UnitedBark:
                    target = cheddar ? _leash.transform : _door.transform;
                    copy = cheddar ? "PRESENT LEASH" : "HOLD DOOR STARE";
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
            if (_beatIndex == 3 ? BothDoorBarksRecent(now) : now <= _barkSignalUntil)
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
                _context.SetCue($"Teenager: not now... {wrongThing} Reset the message.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, $"MISREAD: {wrongThing}");
                _context.SpawnWorldPop(_teenager.transform.position, wrongThing, new Color(1f, 0.55f, 0.35f));
                _context.RequestAudioCue(ArenaFeedbackCatalog.ScorePenalty);
                _context.LogEvent("PeeBreakMisread", wrongThing);
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
        }

        private void BuildScene()
        {
            _door = NewMarker("PeeBreakDoor", new Color(1f, 0.82f, 0.3f), "DOOR - COCOA STARES", new Vector3(2.4f, 4f, 1f));
            _leash = NewMarker("PeeBreakLeash", new Color(0.3f, 0.9f, 1f), "LEASH - CHEDDAR PRESENTS", Vector3.one * 1.4f);
            _hallway = NewMarker("PeeBreakHallwayBlock", new Color(1f, 0.58f, 0.25f), "HALLWAY - CHEDDAR BLOCKS", Vector3.one * 2.6f);
            _charger = NewMarker("PeeBreakCharger", new Color(0.75f, 0.45f, 1f), "CHARGER - COCOA UNPLUGS", Vector3.one * 1.5f);
            _teenager = NewMarker("PeeBreakTeenager", new Color(0.65f, 0.72f, 0.9f), "TEENAGER ?", new Vector3(2f, 2.6f, 1f), out _teenagerLabel);
            _phone = NewMarker("PeeBreakPhone", new Color(0.4f, 0.9f, 1f), "PHONE 100%", Vector3.one * 0.7f, out _phoneLabel);
            _bladderMeter = NewMarker("PeeBreakBladderMeter", new Color(0.4f, 0.8f, 1f), "BLADDER 12%", new Vector3(0.5f, 0.35f, 1f), out _bladderLabel);
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

        private void SetSceneActive(bool active)
        {
            foreach (var marker in new[] { _door, _leash, _hallway, _charger, _teenager, _phone, _bladderMeter })
                if (marker != null) marker.SetActive(active);
            if (active) UpdateScene();
        }

        private void UpdateScene()
        {
            if (_door == null) return;
            _door.transform.position = _doorPosition;
            _leash.transform.position = _leashPosition;
            _hallway.transform.position = _hallwayPosition;
            _charger.transform.position = _chargerPosition;
            _teenager.transform.position = _context.Bounds.center + new Vector2(2f, 5f);
            _phone.transform.position = (Vector2)_teenager.transform.position + new Vector2(0.8f, -0.2f);
            _bladderMeter.transform.position = _doorPosition + new Vector2(0f, -3.2f);

            _door.transform.localScale = DoorOpen ? new Vector3(0.35f, 4f, 1f) : new Vector3(2.4f, 4f, 1f);
            _phone.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.2f, 0.15f, 0.25f), new Color(0.4f, 0.9f, 1f), PhoneBattery);
            _bladderMeter.transform.localScale = new Vector3(Mathf.Lerp(0.5f, 4f, Bladder), 0.35f, 1f);
            _bladderMeter.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.4f, 0.8f, 1f), new Color(1f, 0.35f, 0.2f), Bladder);
            if (_phoneLabel != null) _phoneLabel.text = $"PHONE {PhoneBattery * 100f:0}%";
            if (_bladderLabel != null) _bladderLabel.text = $"BLADDER {Bladder * 100f:0}%";
            if (_teenagerLabel != null)
                _teenagerLabel.text = CurrentBeat switch
                {
                    Beat.DoorStare => "TEENAGER: SCROLLING",
                    Beat.LeashMessage => "TEENAGER: LOOKING UP?",
                    Beat.ChargerGambit => "TEENAGER: PHONE FADING",
                    Beat.UnitedBark => "TEENAGER: ALMOST GETS IT",
                    _ => "TEENAGER: OH! OUTSIDE!"
                };
            _hallway.SetActive(!DoorOpen && _beatIndex == 2);
            _charger.SetActive(!DoorOpen && _beatIndex == 2);
            _leash.SetActive(!DoorOpen && (_beatIndex == 1 || _beatIndex == 3));
        }
    }
}
