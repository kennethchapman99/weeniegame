using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned hold-and-release co-op puzzle. Cocoa braces the heavy gate open while Cheddar
    /// squeezes through to the toy; if Cocoa lets go mid-squeeze the gate snaps shut, and too many
    /// snaps end the run.
    /// </summary>
    public sealed class GateCrashMissionController : IMissionController, IMissionInteractionController,
        IMissionPressureHud, IMissionSuccessPresentationController
    {
        private const float HoldRange = 4f;
        private const float CrossRange = 4f;
        private const float CrossNeeded = 0.8f;
        private const float HoldWindow = 30f; // generous; snaps come from releasing, not timeout.
        private const int MaxSnaps = 4;
        private const float SuccessHoldSeconds = 1.15f;

        private static readonly Color GateIdleColor = new(0.7f, 0.5f, 0.2f);
        private static readonly Color GateHeldColor = new(0.4f, 0.8f, 0.5f);

        private readonly CoopHoldReleasePuzzle _puzzle = new();
        private MissionContext _context;
        private GameObject _gate;
        private GameObject _gateFloorAnchor;
        private GameObject _toy;
        private MissionPropArtAttachment _gateArt;
        private MissionPropArtAttachment _toyArt;
        private Vector2 _holdZone;
        private Vector2 _crossZone;
        private int _snapsSeen;
        private bool _creditedSolve;
        private bool _failed;
        private bool _anchorEngaged;
        private float _gateSnapReactionUntil;
        private float _successHoldRemaining;

        // "Cheddar believes every closed door is a personal attack" - purely cosmetic, no
        // mechanic effect. See docs/GAME-DESIGN-BIBLE.md's Running gags list.
        private const float DoorOutrageCooldown = 8f;
        private const float DoorOutrageRadius = 2.2f;
        private float _nextDoorOutrageAt;
        public int DoorOutrageCount { get; private set; }

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.GateCrash;
        public bool IsComplete => _puzzle.Solved && _successHoldRemaining <= 0f;
        public bool IsFailed => _failed;
        public string FailReason => _failed
            ? "The gate snapped shut too many times before Cheddar could squeeze through."
            : null;
        public CoopHoldReleasePuzzle Puzzle => _puzzle;
        public Vector2 HoldZone => _holdZone;
        /// <summary>
        /// CF2.6 (roster audit of CF1.6's finding #6): the gate marker (_holdZone) is drawn tall
        /// against the yard's back fence line the same way Pee Break's original door was (scale
        /// (1.4, 4, 1) - see BuildScene()) - Cocoa's required brace spot centered on that point
        /// visually reads as bracing halfway up the gate, and this is a HOLD station (Cocoa must
        /// stay anchored the whole squeeze, not a brief touch), the worst-case shape per the
        /// couch-fix queue's own prioritization. _gateFloorAnchor is an invisible child at local Y
        /// -0.62 (identical ratio to Pee Break's doormat anchor from CF1.6, no new art - just a
        /// bare Transform), so its world position sits ~2.48 units below the gate's own center at
        /// the gate's visual base. This is the anchor for the anchor-engage/hold checks and
        /// Cocoa's guidance target below; the gate ART itself (_gate, _holdZone) is untouched.
        /// </summary>
        public Vector2 GateFloorAnchor => _gateFloorAnchor.transform.position;
        public Vector2 CrossZone => _crossZone;
        public Vector2 EntryTarget => _context.Bounds.center;
        public string OutcomeSummary => MissionOutcomeSummaryBuilder.BuildGateCrashSummary(_puzzle);
        public string PressureLabel => "SQUEEZE THROUGH";
        public bool PressureVisible => !_puzzle.Solved;
        public float PressureNormalized => _puzzle.CrossRatio;
        public Color PressureColor => Color.Lerp(new Color(0.95f, 0.55f, 0.16f), new Color(0.38f, 1f, 0.5f), _puzzle.CrossRatio);
        public float SuccessHoldRemaining => _successHoldRemaining;
        public bool IsPresentingSuccessfulOutcome => _puzzle.Solved && _successHoldRemaining > 0f;
        public bool AnchorEngaged => _anchorEngaged;

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "Toy rescued! Cheddar has it - Cocoa held strong!";
                return _puzzle.Held
                    ? $"Cheddar: squeeze through while Cocoa holds (snaps {_puzzle.Snaps}/{MaxSnaps})"
                    : $"Cocoa: reach the gate and Interact to anchor it (snaps {_puzzle.Snaps}/{MaxSnaps})";
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
            _puzzle.Configure(CrossNeeded, HoldWindow);
            _snapsSeen = 0;
            _creditedSolve = false;
            _failed = false;
            _anchorEngaged = false;
            _gateSnapReactionUntil = 0f;
            _successHoldRemaining = 0f;
            _nextDoorOutrageAt = _context.Now() + DoorOutrageCooldown;
            _holdZone = new Vector2(_context.Bounds.center.x - 10f, _context.Bounds.center.y);
            _crossZone = new Vector2(_context.Bounds.center.x + 10f, _context.Bounds.center.y);
            SetSceneActive(true);
            MissionPropArt.SetSprite(_gateArt, FinalGameplayArt.GateCrashGateClosed);
            MissionPropArt.SetSprite(_toyArt, FinalGameplayArt.GateCrashToyWaiting);
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

            int anchor = _context.IndexOfDog(DogId.Cocoa);
            int crosser = _context.IndexOfDog(DogId.Cheddar);
            if (anchor < 0 || crosser < 0) return;

            // CF2.6: anchor the hold check to the gate's floor position, not the tall gate art's
            // own center - see GateFloorAnchor's XML doc for the full rationale.
            bool anchorInRange = Vector2.Distance(_context.Dogs[anchor].transform.position, GateFloorAnchor) <= HoldRange;
            if (_anchorEngaged && !anchorInRange) _anchorEngaged = false;
            bool held = _anchorEngaged && anchorInRange;
            _puzzle.SetHeld(held);
            if (Vector2.Distance(_context.Dogs[crosser].transform.position, _crossZone) <= CrossRange)
                _puzzle.Advance(deltaTime);

            UpdateGateAnchorPose(held);
            HandleSnaps();
            if (_failed) return;
            UpdateLabels();

            if (now >= _nextDoorOutrageAt)
            {
                _nextDoorOutrageAt = now + DoorOutrageCooldown;
                TrySpawnDoorOutrage();
            }
        }

        public bool HandleBark(int dogIndex) => false;

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Solved || _failed || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;
            DogId dogId = DogIdAt(dogIndex);
            if (dogId != DogId.Cocoa)
            {
                _context.MarkFailedInteraction(dogId, "Cocoa is the steady gate anchor; Cheddar takes the squeeze route");
                _context.SetCue("Cheddar can glare at the gate, but Cocoa must Interact to plant the anchor.");
                _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "COCOA: ANCHOR THE GATE!");
                _context.SpawnWorldPop(_context.Dogs[dogIndex].transform.position + Vector3.up, "COCOA'S JOB", new Color(1f, 0.72f, 0.25f));
                return true;
            }

            // CF2.6: same floor anchor as the Tick() hold check - Cocoa engages from the gate's
            // base, not its tall art center.
            if (Vector2.Distance(_context.Dogs[dogIndex].transform.position, GateFloorAnchor) > HoldRange)
            {
                _context.MarkFailedInteraction(dogId, "get closer to the gate before anchoring it");
                _context.SetCue("Cocoa needs paws on the gate - reach the GATE marker and Interact.");
                return true;
            }

            if (_anchorEngaged)
            {
                _context.SetCue("Cocoa is anchored - stay planted while Cheddar squeezes through!");
                return true;
            }

            _anchorEngaged = true;
            _puzzle.SetHeld(true);
            MissionPropArt.SetSprite(_gateArt, FinalGameplayArt.GateCrashGateHeld);
            if (_context.DogFeedback[dogIndex] != null) _context.DogFeedback[dogIndex].ShowProudBrief();
            _context.SetCue("Cocoa deliberately anchored the gate - Cheddar, squeeze to the toy!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "GATE ANCHORED!");
            _context.SpawnWorldPop(_holdZone, "COCOA ANCHORED!", new Color(0.48f, 1f, 0.68f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.RequestRumble("gate_anchor", 0.12f, 0.28f, 0.1f);
            _context.LogEvent("GateAnchored", $"attempt {_puzzle.Snaps + 1}");
            _context.LogObjectiveChanged();
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
            hideDistance = HoldRange;
            if (_context.IndexOfDog(DogId.Cocoa) == dogIndex)
            {
                // CF2.6: guide Cocoa's arrow/beacon/breadcrumb to the floor anchor, not up the
                // gate art itself (_gateFloorAnchor is a real GameObject/Transform, same idiom as
                // Pee Break's _doorMat).
                target = _gateFloorAnchor != null ? _gateFloorAnchor.transform : null;
                copy = _puzzle.Held ? "STAY ANCHORED" : "INTERACT TO ANCHOR";
            }
            else
            {
                target = _toy != null ? _toy.transform : null;
                copy = "SQUEEZE THROUGH";
            }
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("gate_crash", score, timeRemaining, _puzzle.Solved ? 1 : 0, 1, _puzzle.Snaps,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Test hook: Cocoa engages or releases the gate brace (releasing mid-squeeze snaps).</summary>
        public void ForceGateHold(bool held)
        {
            _anchorEngaged = held;
            _puzzle.SetHeld(held);
            HandleSnaps();
            UpdateLabels();
        }

        /// <summary>Test hook: advance the squeeze by <paramref name="seconds"/> with the gate held.</summary>
        public void ForceGateCross(float seconds)
        {
            _puzzle.Advance(seconds);
            HandleSnaps();
            UpdateLabels();
        }

        /// <summary>Deterministic seam for tests that need to advance past the live-world payoff.</summary>
        public void ForceFinishSuccessPresentation()
        {
            _successHoldRemaining = 0f;
            UpdateLabels();
        }

        /// <summary>Public so tests can trigger the gag directly instead of waiting out the cooldown.</summary>
        public void TrySpawnDoorOutrage()
        {
            if (_puzzle.Solved || _failed || _puzzle.Held || _context.Dogs == null) return;
            if (_context.Now() < _gateSnapReactionUntil) return; // don't step on the real snap reaction

            int crosser = _context.IndexOfDog(DogId.Cheddar);
            if (crosser < 0) return;

            DogController cheddar = _context.Dogs[crosser];
            if (cheddar == null || cheddar.Busy) return;

            Vector2 pos = cheddar.transform.position;
            if (Vector2.Distance(pos, _holdZone) > DoorOutrageRadius) return;
            if (!cheddar.TryGetComponent<Rigidbody2D>(out var body) || body.linearVelocity.sqrMagnitude > 0.05f) return;

            BackyardArtVfxPulse.Spawn(_holdZone, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert,
                new Vector3(0.016f, 0.016f, 1f), 19, new Color(0.95f, 0.35f, 0.3f, 0.4f), 0.9f, -35f);
            _context.SpawnWorldPop(_holdZone + Vector2.up * 0.6f, "HOW DARE YOU", new Color(1f, 0.5f, 0.35f));
            DoorOutrageCount++;
        }

        /// <summary>
        /// CF2.6 (roster audit of CF1.6's finding #6): grounding the anchor-engage check fixes
        /// WHERE Cocoa has to stand, but she still just stood there passively while bracing. Give
        /// her a readable "holding the gate" presentation using an existing pose only - the same
        /// idiom CF1.6 used for the door stare (DogReadabilityFeedback.ShowGuidanceNudge, forces
        /// Idle facing the gate for 0.7s, refreshed every tick while actually held so the window
        /// never lapses mid-hold).
        /// </summary>
        private void UpdateGateAnchorPose(bool held)
        {
            if (!held) return;
            int cocoa = _context.IndexOfDog(DogId.Cocoa);
            if (cocoa < 0 || _context.Dogs == null || cocoa >= _context.Dogs.Length || _context.Dogs[cocoa] == null) return;
            if (_context.DogFeedback == null || cocoa >= _context.DogFeedback.Length || _context.DogFeedback[cocoa] == null) return;
            Vector2 faceGate = GateFloorAnchor - (Vector2)_context.Dogs[cocoa].transform.position;
            _context.DogFeedback[cocoa].ShowGuidanceNudge(faceGate);
        }

        private void HandleSnaps()
        {
            if (_puzzle.Solved && !_creditedSolve)
            {
                _creditedSolve = true;
                _successHoldRemaining = SuccessHoldSeconds;
                int anchor = _context.IndexOfDog(DogId.Cocoa);
                int crosser = _context.IndexOfDog(DogId.Cheddar);
                if (anchor >= 0) _context.CreditDog(anchor);
                if (crosser >= 0) _context.CreditDog(crosser);
                MissionPropArt.SetSprite(_toyArt, FinalGameplayArt.GateCrashToyClaimed);
                _context.SetCue("Cheddar got the toy! Cocoa held the gate open!");
                _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "TOY RESCUED!");
                _context.SpawnWorldPop(_crossZone, "TOY RESCUED!", new Color(1f, 0.9f, 0.3f));
                foreach (var feedback in _context.DogFeedback)
                    if (feedback != null) feedback.ShowProudBrief();
                _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
                _context.RequestRumble("gate_toy_rescued", 0.3f, 0.55f, 0.2f);
                _context.LogEvent("GateCrashPayoff", "Toy rescued; holding live-world success beat");
            }

            if (_puzzle.Snaps <= _snapsSeen) return;

            _snapsSeen = _puzzle.Snaps;
            _anchorEngaged = false;
            _gateSnapReactionUntil = _context.Now() + 0.55f;
            MissionPropArt.SetSprite(_gateArt, FinalGameplayArt.GateCrashGateSnap);
            _context.AddScore(ScoreEventCatalog.FakeOut.Points, "GATE SNAP");
            _context.SetFeedback(GameManager.FeedbackKind.SquirrelStoleFood);
            _context.SetCue($"The gate snapped shut! ({_puzzle.Snaps}/{MaxSnaps}) Cocoa has to brace it.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "GATE SNAP!");
            _context.SpawnWorldPop(_crossZone, "SNAP!", new Color(1f, 0.35f, 0.2f));
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestRumble("gate_snap", 0.18f, 0.38f, 0.14f);
            _context.RequestShake(0.12f);
            _context.LogEvent("GateSnap", $"{_puzzle.Snaps}/{MaxSnaps}");
            if (_puzzle.Snaps >= MaxSnaps) _failed = true;
        }

        private void BuildScene()
        {
            // Identity-only close-range text: the badge alternation, held/snap sprites, and the
            // HUD objective line carry who-does-what-now (couch-test-#4-era signal recipe).
            _gate = NewMarker("GateCrashGate", GateIdleColor, "GATE", new Vector3(1.4f, 4f, 1f), out _);
            // CF2.6: bare Transform, no renderer - no new art, just a floor-level anchor point at
            // local Y -0.62 against the gate's own Y-scale (4), the same ratio Pee Break's doormat
            // uses (CF1.6). See GateFloorAnchor's XML doc above.
            _gateFloorAnchor = new GameObject("GateCrashGateFloorAnchor");
            _gateFloorAnchor.transform.SetParent(_gate.transform, false);
            _gateFloorAnchor.transform.localPosition = new Vector3(0f, -0.62f, 0f);
            _toy = NewMarker("GateCrashToy", new Color(0.6f, 0.8f, 1f), "TOY", Vector3.one * 1.2f, out _);
            _gateArt = MissionPropArt.AttachObject(_gate, FinalGameplayArt.GateCrashGateClosed, 0.013f, 18, true);
            _toyArt = MissionPropArt.AttachObject(_toy, FinalGameplayArt.GateCrashToyWaiting, 0.012f, 18, true);
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
            if (_gate != null) { _gate.transform.position = _holdZone; _gate.SetActive(active); }
            if (_toy != null) { _toy.transform.position = _crossZone; _toy.SetActive(active); }
        }

        private void UpdateLabels()
        {
            // Distance signal alternates with the hold-release rhythm: gate until Cocoa braces
            // it, toy while the squeeze window is open.
            ActorSignalBadge.SetStationSignal(_gate, !_puzzle.Solved && !_failed && !_puzzle.Held);
            ActorSignalBadge.SetStationSignal(_toy, !_puzzle.Solved && !_failed && _puzzle.Held);
            if (_gate != null)
            {
                _gate.transform.position = _holdZone;
                var sr = _gate.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = _puzzle.Held ? GateHeldColor : GateIdleColor;
                if (_context.Now() < _gateSnapReactionUntil)
                    MissionPropArt.SetSprite(_gateArt, FinalGameplayArt.GateCrashGateSnap);
                else
                    MissionPropArt.SetSprite(_gateArt, _puzzle.Held ? FinalGameplayArt.GateCrashGateHeld : FinalGameplayArt.GateCrashGateClosed);
            }
            if (_toy != null)
            {
                _toy.transform.position = _crossZone;
                MissionPropArt.SetSprite(_toyArt, _puzzle.Solved ? FinalGameplayArt.GateCrashToyClaimed : FinalGameplayArt.GateCrashToyWaiting);
            }
        }

        private Vector2 ClampInsideBounds(Vector2 point, float margin) => new(
            Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
            Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));

        private DogId DogIdAt(int dogIndex) => _context.Dogs != null && dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
            _context.Dogs[dogIndex] != null && _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id : DogId.Cheddar;
    }
}
