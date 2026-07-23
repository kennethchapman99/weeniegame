using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Controller-owned Burr Maze: a hedge-and-bramble maze at the back of the yard, patrolled by the
    /// neighbor's territorial cat. Every dog instinct says "chase the cat" - this mission is about NOT
    /// doing that: a facing-direction vision cone sweeps a patrol route, and lingering in it too long
    /// (a brief dwell, not instant) gets both dogs swept back to the last safe checkpoint - a
    /// stealth-retry loop, not a hard fail, and the third distinct failure pattern in this roster
    /// (alongside Skunk Blast Mayhem's fail-forward de-skunk detour and Tick Invasion's hard fail).
    /// Cheddar's reckless bramble charges cake him in burrs fast (slower + louder once caked - a real
    /// speed penalty and a wider effective notice-dwell, never a fail state on its own); Cocoa reads
    /// the patrol earliest (an earlier "about to be spotted" telegraph), accrues burrs slower, and
    /// moves more quietly. Ducking into a bush hard-resets a dog's in-progress notice-dwell; barking
    /// while NOT hidden lures the cat's attention for a few seconds at the cost of breaking the
    /// barker's own cover. Partway through the run a second, faster kitten patrol activates on the
    /// maze's short route, forcing the pair to route around whichever patrol currently owns the
    /// "safe" path. Wires the Burr Maze co-op puzzle into the mission flow, reusing the shared
    /// predator actor as the primary cat patrol (Baby Bird Bedlam/Skunk Blast Mayhem's precedent for
    /// repurposing it) and a second generated actor for the kitten twist.
    /// </summary>
    public sealed class BurrMazeMissionController : IMissionController, IMissionInteractionController,
        IMissionPressureHud, IMissionSuccessPresentationController
    {
        private const float CheddarBrambleRate = 0.16f;
        private const float CocoaBrambleRate = 0.07f;
        private const float BurrThreshold = 0.55f;
        private const float BurrSpeedMultiplier = 0.6f;
        private const float BurrNoticeMultiplier = 1.6f;
        private const float CheddarNoticeRate = 1.25f;    // louder baseline - notices faster
        private const float CocoaNoticeRate = 1f;
        private const float NoticeDwellThreshold = 1.3f;
        private const float CheddarWarningFraction = 0.72f; // later, shorter warning window
        private const float CocoaWarningFraction = 0.42f;   // earlier veteran-read warning window
        private const float LureDurationSeconds = 3f;
        private const float BurrPickRatePerSecond = 0.28f;
        private const float TwistTriggerSeconds = 20f;
        private const int CheckpointCount = 4;

        private const float PrimaryPatrolHalfWidth = 9f;
        private const float PrimaryPatrolSpeed = 2.6f;
        private const float PrimaryConeAngle = 80f;
        private const float PrimaryConeDistance = 7.5f;

        private const float TwistPatrolHalfHeight = 7f;
        private const float TwistPatrolSpeed = 4.2f; // faster than the primary patrol
        private const float TwistConeAngle = 70f;
        private const float TwistConeDistance = 6f;

        // Cheddar is mechanically "louder" - the cat's effective cone reach against him is wider;
        // Cocoa is mechanically quieter - it is shorter against her.
        private const float CheddarDetectionRadiusMultiplier = 1.25f;
        private const float CocoaDetectionRadiusMultiplier = 0.85f;

        private const float BrambleRadius = 2.4f;
        private const float HideRadius = 1.9f;
        private const float CheckpointRadius = 2.4f;
        private const float PickRange = 2.2f;
        private const float StationaryVelocitySqr = 0.05f;
        private const float SuccessHoldSeconds = 1.15f;

        private static readonly Vector2[] CheckpointOffsets =
        {
            new Vector2(-14f, 0f),
            new Vector2(-5f, 6f),
            new Vector2(5f, -6f),
            new Vector2(14f, 0f)
        };
        private static readonly Vector2[] BrambleOffsets = { new Vector2(-2f, 3f), new Vector2(2f, -3f) };
        private static readonly Vector2[] HideOffsets = { new Vector2(-9f, 3f), new Vector2(9f, -3f), new Vector2(0f, 4.5f) };

        private static readonly Color SafeColor = new(0.42f, 0.85f, 0.5f);
        private static readonly Color WarningColor = new(1f, 0.78f, 0.25f);
        private static readonly Color DetectedColor = new(1f, 0.32f, 0.2f);
        private static readonly Color CakedColor = new(0.6f, 0.42f, 0.24f);
        private static readonly Color LureColor = new(0.95f, 0.55f, 0.85f);
        private static readonly Color TwistColor = new(0.85f, 0.35f, 0.65f);

        private readonly CoopBurrMazePuzzle _puzzle = new();
        private MissionContext _context;

        private GameObject _catObj;    // primary patrol - the shared predator actor, repurposed
        private GameObject _kittenObj; // twist patrol - a second generated predator-styled actor
        private GameObject[] _checkpointObjs;
        private GameObject[] _brambleObjs;
        private GameObject[] _bushObjs;
        private TextMesh _cheddarBurrLabel;
        private TextMesh _cocoaBurrLabel;

        private Vector2 _primaryPos;
        private Vector2 _primaryFacing = Vector2.right;
        private int _primaryDir = 1;
        private Vector2 _twistPos;
        private Vector2 _twistFacing = Vector2.up;
        private int _twistDir = 1;

        private bool _pickEngaged;
        private DogId _pickPicker;
        private bool _clearAnnounced;
        private float _successHoldRemaining;
        private bool _lureActivePrev;
        private bool _cheddarWarnedPrev;
        private bool _cocoaWarnedPrev;
        private bool _cheddarCakedPrev;
        private bool _cocoaCakedPrev;

        public CoopBurrMazePuzzle Puzzle => _puzzle;
        public GameObject CatObject => _catObj;
        public GameObject KittenObject => _kittenObj;

        public GameManager.MissionVariant Variant => GameManager.MissionVariant.BurrMaze;
        public bool IsComplete => _puzzle.Cleared && _successHoldRemaining <= 0f;
        public bool IsPresentingSuccessfulOutcome => _puzzle.Cleared && _successHoldRemaining > 0f;
        // Detection is a stealth-retry loop (position reset, no meter penalty), not a fail condition -
        // the only way this mission ends badly is the shared round timeout.
        public bool IsFailed => false;
        public string FailReason => null;
        public Vector2 EntryTarget => _context != null ? CheckpointPosition(0) : Vector2.zero;
        public string OutcomeSummary => _puzzle.Cleared
            ? (_puzzle.Mistakes == 0 ? "Never Spotted" : "Burr-Caked But Through")
            : "Still Casing The Maze";

        public bool PressureVisible => !_puzzle.Cleared;
        public string PressureLabel
        {
            get
            {
                if (_puzzle.IsAboutToBeNoticed(DogId.Cheddar) || _puzzle.IsAboutToBeNoticed(DogId.Cocoa))
                {
                    DogId warned = _puzzle.IsAboutToBeNoticed(DogId.Cocoa) ? DogId.Cocoa : DogId.Cheddar;
                    return $"{Name(warned)} ABOUT TO BE SPOTTED!";
                }
                DogId worse = _puzzle.BurrOf(DogId.Cheddar) >= _puzzle.BurrOf(DogId.Cocoa) ? DogId.Cheddar : DogId.Cocoa;
                int pct = Mathf.RoundToInt(_puzzle.BurrOf(worse) * 100f);
                return _puzzle.IsBurrCaked(worse) ? $"{Name(worse)} BURRS {pct}% - CAKED!" : $"{Name(worse)} BURRS {pct}%";
            }
        }
        public float PressureNormalized => Mathf.Clamp01(Mathf.Max(_puzzle.BurrOf(DogId.Cheddar), _puzzle.BurrOf(DogId.Cocoa)));
        public Color PressureColor => (_puzzle.IsAboutToBeNoticed(DogId.Cheddar) || _puzzle.IsAboutToBeNoticed(DogId.Cocoa))
            ? WarningColor
            : Color.Lerp(SafeColor, CakedColor, PressureNormalized);

        public string ObjectiveLabel
        {
            get
            {
                if (IsPresentingSuccessfulOutcome)
                    return "Clear! Both dogs slipped past the cat and out the far hedge.";
                if (_puzzle.IsAboutToBeNoticed(DogId.Cheddar)) return "Cheddar is about to be spotted - duck into a bush now!";
                if (_puzzle.IsAboutToBeNoticed(DogId.Cocoa)) return "Cocoa is about to be spotted - duck into a bush now!";
                if (_puzzle.TwistPatrolActive)
                    return $"A second, faster patrol is loose too - reach checkpoint {_puzzle.CheckpointIndex + 1} of {CheckpointOffsets.Length}.";
                return $"Sneak past the cat's patrol to checkpoint {_puzzle.CheckpointIndex + 1} of {CheckpointOffsets.Length}.";
            }
        }

        public void Initialize(MissionContext context)
        {
            _context = context;
            _catObj = context.PredatorObject;
            BuildProps();
            Cleanup();
        }

        public void StartMission()
        {
            _puzzle.Configure(
                CheddarBrambleRate, CocoaBrambleRate,
                BurrThreshold, BurrSpeedMultiplier, BurrNoticeMultiplier,
                CheddarNoticeRate, CocoaNoticeRate,
                NoticeDwellThreshold,
                CheddarWarningFraction, CocoaWarningFraction,
                LureDurationSeconds,
                BurrPickRatePerSecond,
                TwistTriggerSeconds,
                CheckpointCount);

            _pickEngaged = false;
            _clearAnnounced = false;
            _successHoldRemaining = 0f;
            _lureActivePrev = false;
            _cheddarWarnedPrev = false;
            _cocoaWarnedPrev = false;
            _cheddarCakedPrev = false;
            _cocoaCakedPrev = false;

            Vector2 center = _context.Bounds.center;
            _primaryDir = 1;
            _primaryPos = center + new Vector2(-PrimaryPatrolHalfWidth, 0f);
            _primaryFacing = Vector2.right;
            _twistDir = 1;
            _twistPos = center + new Vector2(0f, -TwistPatrolHalfHeight);
            _twistFacing = Vector2.up;

            ActivateCat();
            if (_kittenObj != null) _kittenObj.SetActive(false);
            PositionProps();
            SetGroupActive(_checkpointObjs, true);
            SetGroupActive(_brambleObjs, true);
            SetGroupActive(_bushObjs, true);

            UpdateBurrLabels();
        }

        public void Tick(float deltaTime, float now)
        {
            if (_puzzle.Cleared)
            {
                _successHoldRemaining = Mathf.Max(0f, _successHoldRemaining - deltaTime);
                return;
            }
            if (_context.Dogs == null) return;

            int cheddarIdx = _context.IndexOfDog(DogId.Cheddar);
            int cocoaIdx = _context.IndexOfDog(DogId.Cocoa);
            if (cheddarIdx < 0 || cocoaIdx < 0) return;

            Vector2 cheddarPos = _context.Dogs[cheddarIdx].transform.position;
            Vector2 cocoaPos = _context.Dogs[cocoaIdx].transform.position;

            UpdatePatrols(deltaTime, cheddarPos, cocoaPos);

            bool cheddarHidden = IsHidden(cheddarPos);
            bool cocoaHidden = IsHidden(cocoaPos);
            bool cheddarBramble = IsInBramble(cheddarPos);
            bool cocoaBramble = IsInBramble(cocoaPos);

            bool cheddarPrimaryCone = CoopBurrMazePuzzle.IsInCone(_primaryPos, _primaryFacing, cheddarPos,
                PrimaryConeAngle, PrimaryConeDistance * CheddarDetectionRadiusMultiplier);
            bool cocoaPrimaryCone = CoopBurrMazePuzzle.IsInCone(_primaryPos, _primaryFacing, cocoaPos,
                PrimaryConeAngle, PrimaryConeDistance * CocoaDetectionRadiusMultiplier);
            bool cheddarTwistCone = _puzzle.TwistPatrolActive && CoopBurrMazePuzzle.IsInCone(_twistPos, _twistFacing, cheddarPos,
                TwistConeAngle, TwistConeDistance * CheddarDetectionRadiusMultiplier);
            bool cocoaTwistCone = _puzzle.TwistPatrolActive && CoopBurrMazePuzzle.IsInCone(_twistPos, _twistFacing, cocoaPos,
                TwistConeAngle, TwistConeDistance * CocoaDetectionRadiusMultiplier);

            UpdatePickEngagement(cheddarIdx, cocoaIdx, cheddarPos, cocoaPos, cheddarHidden, cocoaHidden);

            int detectionsBefore = _puzzle.Detections;
            int checkpointBefore = _puzzle.CheckpointIndex;
            bool twistWasActive = _puzzle.TwistPatrolActive;
            DogId? pickTargetBefore = _puzzle.PickTarget;

            _puzzle.Advance(deltaTime,
                new CoopBurrMazePuzzle.DogExposure(cheddarBramble, cheddarPrimaryCone, cheddarTwistCone, cheddarHidden),
                new CoopBurrMazePuzzle.DogExposure(cocoaBramble, cocoaPrimaryCone, cocoaTwistCone, cocoaHidden));

            if (!twistWasActive && _puzzle.TwistPatrolActive) AnnounceTwistPatrol();

            ApplySpeedPenalties();
            UpdateCakedAnnouncements();
            UpdateWarningAnnouncements();

            if (pickTargetBefore.HasValue && !_puzzle.PickTarget.HasValue && _puzzle.BurrOf(pickTargetBefore.Value) <= 0f)
                AnnouncePickComplete(pickTargetBefore.Value);

            if (_puzzle.Detections > detectionsBefore)
            {
                HandleDetection(cheddarIdx, cocoaIdx);
            }
            else
            {
                TryAdvanceCheckpoint(cheddarIdx, cocoaIdx);
                if (_puzzle.CheckpointIndex > checkpointBefore) AnnounceCheckpoint();
            }

            UpdateBurrLabels();
            UpdateLurePresentation();

            if (_puzzle.Cleared && !_clearAnnounced) AnnounceClear();
        }

        public bool HandleBark(int dogIndex)
        {
            if (_puzzle.Cleared || !TryResolveDog(dogIndex, out DogId dog)) return false;
            Vector2 pos = _context.Dogs[dogIndex].transform.position;
            if (IsHidden(pos))
            {
                _context.SetCue($"{Name(dog)} needs to be out of cover to bark loud enough to lure the cat.");
                return true;
            }

            _puzzle.SetLure(dog);
            _context.SetCue($"{Name(dog)} barks loud - the cat's attention swings this way! {Name(Other(dog))}, move now.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.BarkBurst, "LURED!");
            PopAtDog(dog, "LURE!", LureColor);
            _context.RequestAudioCue(ArenaFeedbackCatalog.Bark);
            _context.LogEvent("BurrMazeLure", Name(dog));
            _context.LogObjectiveChanged();
            return true;
        }

        public bool HandleInteract(int dogIndex)
        {
            if (_puzzle.Cleared || !TryResolveDog(dogIndex, out DogId picker)) return false;
            DogId target = Other(picker);

            if (_puzzle.BurrOf(target) <= 0f)
            {
                _context.SetCue($"{Name(target)} has no burrs to pick right now.");
                return true;
            }

            Vector2 pickerPos = _context.Dogs[dogIndex].transform.position;
            int targetIdx = _context.IndexOfDog(target);
            Vector2 targetPos = targetIdx >= 0 ? _context.Dogs[targetIdx].transform.position : pickerPos;
            if (Vector2.Distance(pickerPos, targetPos) > PickRange)
            {
                _context.MarkFailedInteraction(picker, "get close to your burr-caked partner to start picking");
                _context.SetCue($"{Name(picker)} is too far from {Name(target)} to pick burrs.");
                return true;
            }

            if (_pickEngaged)
            {
                _context.SetCue($"{Name(picker)} is already picking {Name(target)} clean - stay put and out of sight!");
                return true;
            }

            _pickEngaged = true;
            _pickPicker = picker;
            _puzzle.SetPicking(target);
            _context.SetCue($"{Name(picker)} starts picking burrs off {Name(target)} - hold still, both of you, and stay out of the cat's line of sight!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "PICKING BURRS...");
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            _context.LogEvent("BurrMazePickStart", $"{Name(picker)}->{Name(target)}");
            _context.LogObjectiveChanged();
            return true;
        }

        public void Cleanup()
        {
            _pickEngaged = false;
            _clearAnnounced = false;
            _successHoldRemaining = 0f;
            _lureActivePrev = false;
            _cheddarWarnedPrev = false;
            _cocoaWarnedPrev = false;
            _cheddarCakedPrev = false;
            _cocoaCakedPrev = false;
            if (_kittenObj != null) _kittenObj.SetActive(false);
            SetGroupActive(_checkpointObjs, false);
            SetGroupActive(_brambleObjs, false);
            SetGroupActive(_bushObjs, false);
        }

        public void StageDogsForEntry()
        {
            // The shared staging parks the predator for RequiresPredator=false missions (this one),
            // so the cat must be re-activated here, same precedent as Baby Bird Bedlam's parent bird
            // and Skunk Blast Mayhem's skunk.
            ActivateCat();
            Vector2 start = CheckpointPosition(0);
            int cheddarIdx = _context.IndexOfDog(DogId.Cheddar);
            int cocoaIdx = _context.IndexOfDog(DogId.Cocoa);
            if (cheddarIdx >= 0) PlaceDog(cheddarIdx, start + new Vector2(0f, 1.2f));
            if (cocoaIdx >= 0) PlaceDog(cocoaIdx, start + new Vector2(0f, -1.2f));
        }

        public bool TryGetObjectiveTarget(int dogIndex, out Transform target, out string copy, out float hideDistance)
        {
            target = null;
            copy = string.Empty;
            hideDistance = 1.6f;
            if (_puzzle.Cleared || _context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;

            DogId me = DogIdAt(dogIndex);
            Vector2 myPos = _context.Dogs[dogIndex].transform.position;

            if (_puzzle.IsAboutToBeNoticed(me) && !IsHidden(myPos))
            {
                var bush = NearestBush(myPos);
                if (bush != null)
                {
                    target = bush.transform;
                    copy = "ABOUT TO BE SPOTTED - HIDE!";
                    hideDistance = HideRadius;
                    return true;
                }
            }

            int nextIndex = Mathf.Min(_puzzle.CheckpointIndex + 1, CheckpointOffsets.Length - 1);
            var marker = _checkpointObjs != null && nextIndex < _checkpointObjs.Length ? _checkpointObjs[nextIndex] : null;
            target = marker != null ? marker.transform : null;
            copy = nextIndex == CheckpointOffsets.Length - 1 ? "REACH THE MAZE END" : "NEXT CHECKPOINT";
            hideDistance = CheckpointRadius;
            return target != null;
        }

        public MissionRuntimeSnapshot CreateSnapshot(int score, float timeRemaining, GameManager.MissionOutcome outcome) =>
            new("burr_maze", score, timeRemaining, _puzzle.CheckpointIndex, CheckpointOffsets.Length - 1, _puzzle.Mistakes,
                outcome == GameManager.MissionOutcome.Clear, outcome == GameManager.MissionOutcome.Failed);

        /// <summary>Deterministic seam for advancing past the live clear payoff.</summary>
        public void ForceFinishSuccessPresentation() => _successHoldRemaining = 0f;

        /// <summary>Test seam: resolve GameManager's dog-index space for a given dog identity.</summary>
        public int DogIndexOf(DogId dogId) => _context.IndexOfDog(dogId);

        /// <summary>Test seam: the checkpoint world position for a given index.</summary>
        public Vector2 CheckpointPosition(int index) => _context.Bounds.center + CheckpointOffsets[Mathf.Clamp(index, 0, CheckpointOffsets.Length - 1)];

        /// <summary>Test seam: the bramble-patch world position for a given index.</summary>
        public Vector2 BramblePosition(int index) => _context.Bounds.center + BrambleOffsets[Mathf.Clamp(index, 0, BrambleOffsets.Length - 1)];

        /// <summary>Test seam: the hiding-bush world position for a given index.</summary>
        public Vector2 HidePosition(int index) => _context.Bounds.center + HideOffsets[Mathf.Clamp(index, 0, HideOffsets.Length - 1)];

        private void UpdatePatrols(float dt, Vector2 cheddarPos, Vector2 cocoaPos)
        {
            Vector2 center = _context.Bounds.center;

            if (_puzzle.LureActive && _puzzle.LureDog.HasValue)
            {
                Vector2 target = _puzzle.LureDog.Value == DogId.Cheddar ? cheddarPos : cocoaPos;
                Vector2 toTarget = target - _primaryPos;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    _primaryFacing = toTarget.normalized;
                    _primaryPos += _primaryFacing * PrimaryPatrolSpeed * dt;
                }
            }
            else
            {
                _primaryPos.x += _primaryDir * PrimaryPatrolSpeed * dt;
                float leftEdge = center.x - PrimaryPatrolHalfWidth;
                float rightEdge = center.x + PrimaryPatrolHalfWidth;
                if (_primaryPos.x >= rightEdge) { _primaryPos.x = rightEdge; _primaryDir = -1; }
                else if (_primaryPos.x <= leftEdge) { _primaryPos.x = leftEdge; _primaryDir = 1; }
                _primaryPos.y = center.y;
                _primaryFacing = new Vector2(_primaryDir, 0f);
            }
            if (_catObj != null) _catObj.transform.position = _primaryPos;

            if (!_puzzle.TwistPatrolActive) return;
            if (_kittenObj != null && !_kittenObj.activeSelf) _kittenObj.SetActive(true);

            _twistPos.y += _twistDir * TwistPatrolSpeed * dt;
            float bottomEdge = center.y - TwistPatrolHalfHeight;
            float topEdge = center.y + TwistPatrolHalfHeight;
            if (_twistPos.y >= topEdge) { _twistPos.y = topEdge; _twistDir = -1; }
            else if (_twistPos.y <= bottomEdge) { _twistPos.y = bottomEdge; _twistDir = 1; }
            _twistPos.x = center.x;
            _twistFacing = new Vector2(0f, _twistDir);
            if (_kittenObj != null) _kittenObj.transform.position = _twistPos;
        }

        private void UpdatePickEngagement(int cheddarIdx, int cocoaIdx, Vector2 cheddarPos, Vector2 cocoaPos, bool cheddarHidden, bool cocoaHidden)
        {
            if (!_pickEngaged) { _puzzle.SetPicking(null); return; }

            DogId target = Other(_pickPicker);
            Vector2 pickerPos = _pickPicker == DogId.Cheddar ? cheddarPos : cocoaPos;
            Vector2 targetPos = target == DogId.Cheddar ? cheddarPos : cocoaPos;
            bool pickerHidden = _pickPicker == DogId.Cheddar ? cheddarHidden : cocoaHidden;
            bool targetHidden = target == DogId.Cheddar ? cheddarHidden : cocoaHidden;
            bool inRange = Vector2.Distance(pickerPos, targetPos) <= PickRange;
            bool pickerStationary = IsStationary(_pickPicker == DogId.Cheddar ? cheddarIdx : cocoaIdx);
            bool targetStationary = IsStationary(target == DogId.Cheddar ? cheddarIdx : cocoaIdx);
            bool valid = inRange && !pickerHidden && !targetHidden && pickerStationary && targetStationary && _puzzle.BurrOf(target) > 0f;

            if (!valid)
            {
                _pickEngaged = false;
                _puzzle.SetPicking(null);
                _context.SetCue("The burr-pick broke off - stay close, hold still, and stay out of sight for it to finish.");
                return;
            }
            _puzzle.SetPicking(target);
        }

        private bool IsStationary(int dogIndex)
        {
            if (dogIndex < 0 || _context.Dogs == null || dogIndex >= _context.Dogs.Length || _context.Dogs[dogIndex] == null) return true;
            return !_context.Dogs[dogIndex].TryGetComponent<Rigidbody2D>(out var body) || body.linearVelocity.sqrMagnitude <= StationaryVelocitySqr;
        }

        private bool IsHidden(Vector2 pos)
        {
            for (int i = 0; i < HideOffsets.Length; i++)
                if (Vector2.Distance(pos, _context.Bounds.center + HideOffsets[i]) <= HideRadius) return true;
            return false;
        }

        private bool IsInBramble(Vector2 pos)
        {
            for (int i = 0; i < BrambleOffsets.Length; i++)
                if (Vector2.Distance(pos, _context.Bounds.center + BrambleOffsets[i]) <= BrambleRadius) return true;
            return false;
        }

        private GameObject NearestBush(Vector2 pos)
        {
            GameObject best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < HideOffsets.Length; i++)
            {
                float d = Vector2.Distance(pos, _context.Bounds.center + HideOffsets[i]);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = _bushObjs != null && i < _bushObjs.Length ? _bushObjs[i] : null;
                }
            }
            return best;
        }

        private void TryAdvanceCheckpoint(int cheddarIdx, int cocoaIdx)
        {
            if (_puzzle.CheckpointIndex >= CheckpointOffsets.Length - 1) return;
            Vector2 next = CheckpointPosition(_puzzle.CheckpointIndex + 1);
            bool cheddarThere = cheddarIdx >= 0 && Vector2.Distance(_context.Dogs[cheddarIdx].transform.position, next) <= CheckpointRadius;
            bool cocoaThere = cocoaIdx >= 0 && Vector2.Distance(_context.Dogs[cocoaIdx].transform.position, next) <= CheckpointRadius;
            if (cheddarThere && cocoaThere) _puzzle.AdvanceCheckpoint();
        }

        private void HandleDetection(int cheddarIdx, int cocoaIdx)
        {
            Vector2 safe = CheckpointPosition(_puzzle.CheckpointIndex);
            if (cheddarIdx >= 0) PlaceDog(cheddarIdx, safe + new Vector2(0f, 1f));
            if (cocoaIdx >= 0) PlaceDog(cocoaIdx, safe + new Vector2(0f, -1f));
            _pickEngaged = false;

            _context.SetCue("SPOTTED! Both dogs scramble back to the last safe hedge corner - no burrs lost, just ground.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SPOTTED!");
            _context.SpawnWorldPop(safe, "SPOTTED!", DetectedColor);
            _context.SetFeedback(GameManager.FeedbackKind.PredatorAttack);
            ShowSadFor(DogId.Cheddar);
            ShowSadFor(DogId.Cocoa);
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.RequestShake(0.2f);
            _context.LogEvent("BurrMazeDetected", $"checkpoint={_puzzle.CheckpointIndex}");
            _context.LogObjectiveChanged();
        }

        private void AnnounceCheckpoint()
        {
            _context.AddScore(ScoreEventCatalog.BurrMazeCheckpoint.Points, ScoreEventCatalog.BurrMazeCheckpoint.Label);
            _context.SetCue($"Checkpoint reached! ({_puzzle.CheckpointIndex}/{CheckpointOffsets.Length - 1})");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "CHECKPOINT!");
            _context.SpawnWorldPop(CheckpointPosition(_puzzle.CheckpointIndex), "CHECKPOINT!", SafeColor);
            _context.RequestAudioCue(ArenaFeedbackCatalog.SnackSockCollect);
            _context.LogEvent("BurrMazeCheckpoint", $"{_puzzle.CheckpointIndex}/{CheckpointOffsets.Length - 1}");
            _context.LogObjectiveChanged();
        }

        private void AnnounceTwistPatrol()
        {
            if (_kittenObj != null)
            {
                _kittenObj.SetActive(true);
                _kittenObj.transform.position = _twistPos;
                _context.SetActorState(_kittenObj, "KITTEN PATROL - FASTER, SHORTER ROUTE", TwistColor, 0.3f);
            }
            _context.SetCue("A second, faster patrol - the cat's kitten - is now sweeping the short route!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, "SECOND PATROL!");
            if (_kittenObj != null) _context.SpawnWorldPop(_kittenObj.transform.position, "KITTEN ON PATROL!", TwistColor);
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.LogEvent("BurrMazeTwistPatrol", "activated");
        }

        private void AnnouncePickComplete(DogId target)
        {
            _context.AddScore(ScoreEventCatalog.BurrPickedClean.Points, ScoreEventCatalog.BurrPickedClean.Label);
            _pickEngaged = false;
            _context.SetCue($"{Name(target)} is picked clean of burrs!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "BURRS CLEAN!");
            PopAtDog(target, "CLEAN!", SafeColor);
            _context.RequestAudioCue(ArenaFeedbackCatalog.TugRescueSuccess);
            _context.LogEvent("BurrMazePickComplete", Name(target));
            _context.LogObjectiveChanged();
        }

        private void ApplySpeedPenalties()
        {
            int cheddarIdx = _context.IndexOfDog(DogId.Cheddar);
            int cocoaIdx = _context.IndexOfDog(DogId.Cocoa);
            if (cheddarIdx >= 0) _context.Dogs[cheddarIdx].SetSpeedPenalty(_puzzle.SpeedMultiplierFor(DogId.Cheddar));
            if (cocoaIdx >= 0) _context.Dogs[cocoaIdx].SetSpeedPenalty(_puzzle.SpeedMultiplierFor(DogId.Cocoa));
        }

        private void UpdateCakedAnnouncements()
        {
            bool cheddarCaked = _puzzle.IsBurrCaked(DogId.Cheddar);
            bool cocoaCaked = _puzzle.IsBurrCaked(DogId.Cocoa);
            if (cheddarCaked && !_cheddarCakedPrev) AnnounceCaked(DogId.Cheddar);
            if (cocoaCaked && !_cocoaCakedPrev) AnnounceCaked(DogId.Cocoa);
            _cheddarCakedPrev = cheddarCaked;
            _cocoaCakedPrev = cocoaCaked;
        }

        private void AnnounceCaked(DogId dog)
        {
            _context.SetCue($"{Name(dog)} is burr-caked - slower now, and rustling loud enough to widen the cat's notice. Partner: pick them clean!");
            _context.SetJuice(GameManager.JuiceFeedbackKind.WarningMiss, $"{Name(dog).ToUpperInvariant()} CAKED!");
            PopAtDog(dog, "CAKED!", CakedColor);
            _context.RequestAudioCue(ArenaFeedbackCatalog.ThreatWarning);
            _context.LogEvent("BurrMazeCaked", Name(dog));
            _context.LogObjectiveChanged();
        }

        private void UpdateWarningAnnouncements()
        {
            bool cheddarWarned = _puzzle.IsAboutToBeNoticed(DogId.Cheddar);
            bool cocoaWarned = _puzzle.IsAboutToBeNoticed(DogId.Cocoa);
            if (cheddarWarned && !_cheddarWarnedPrev) AnnounceWarning(DogId.Cheddar);
            if (cocoaWarned && !_cocoaWarnedPrev) AnnounceWarning(DogId.Cocoa);
            _cheddarWarnedPrev = cheddarWarned;
            _cocoaWarnedPrev = cocoaWarned;
        }

        private void AnnounceWarning(DogId dog)
        {
            _context.SetCue($"{Name(dog)} is about to be spotted - duck into a bush now!");
            PopAtDog(dog, "ABOUT TO BE SPOTTED!", WarningColor);
            _context.LogObjectiveChanged();
        }

        private void UpdateLurePresentation()
        {
            if (_puzzle.LureActive && !_lureActivePrev && _catObj != null)
                _context.SetActorState(_catObj, $"CAT CHASING {Name(_puzzle.LureDog ?? DogId.Cheddar).ToUpperInvariant()}!", LureColor, 0.3f);
            else if (!_puzzle.LureActive && _lureActivePrev && _catObj != null)
                _context.SetActorState(_catObj, "CAT PATROL - STAY OUT OF THE CONE", new Color(0.35f, 0.3f, 0.32f), 0.06f);
            _lureActivePrev = _puzzle.LureActive;
        }

        private void AnnounceClear()
        {
            _clearAnnounced = true;
            _successHoldRemaining = SuccessHoldSeconds;
            _context.AddScore(ScoreEventCatalog.BurrMazeCleared.Points, ScoreEventCatalog.BurrMazeCleared.Label);
            if (_context.Dogs != null)
                for (int i = 0; i < _context.Dogs.Length; i++) _context.CreditDog(i);
            _context.SetFeedback(GameManager.FeedbackKind.LevelClear);
            _context.SetCue("Clear! Both dogs slip past the cat and its kitten and out the far hedge with the prize.");
            _context.SetJuice(GameManager.JuiceFeedbackKind.SuccessPop, "MAZE CLEARED!");
            _context.SpawnWorldPop(CheckpointPosition(CheckpointOffsets.Length - 1), "MAZE CLEARED!", SafeColor);
            foreach (var feedback in _context.DogFeedback)
                if (feedback != null) feedback.ShowProudBrief();
            _context.RequestAudioCue(ArenaFeedbackCatalog.MissionWin);
            _context.RequestRumble("burr_maze_clear", 0.28f, 0.5f, 0.2f);
            _context.LogEvent("BurrMazeCleared", $"detections={_puzzle.Mistakes}");
        }

        private void UpdateBurrLabels()
        {
            if (_cheddarBurrLabel != null)
            {
                _cheddarBurrLabel.text = $"BURRS {Pct(_puzzle.BurrOf(DogId.Cheddar))}%";
                _cheddarBurrLabel.color = LabelColorFor(DogId.Cheddar);
            }
            if (_cocoaBurrLabel != null)
            {
                _cocoaBurrLabel.text = $"BURRS {Pct(_puzzle.BurrOf(DogId.Cocoa))}%";
                _cocoaBurrLabel.color = LabelColorFor(DogId.Cocoa);
            }
        }

        private Color LabelColorFor(DogId dog) =>
            _puzzle.IsBurrCaked(dog) ? CakedColor : Color.Lerp(SafeColor, CakedColor, _puzzle.BurrOf(dog));

        private void ActivateCat()
        {
            if (_catObj == null) return;
            _catObj.SetActive(true);
            _catObj.transform.position = _primaryPos;
            _context.SetActorState(_catObj, "CAT PATROL - STAY OUT OF THE CONE", new Color(0.35f, 0.3f, 0.32f), 0.06f);
        }

        private void BuildProps()
        {
            _kittenObj = _context.CreateActor(ArenaArtCatalog.ActorKind.Predator);
            if (_kittenObj != null)
            {
                _kittenObj.name = "BurrMazeKitten";
                _kittenObj.transform.localScale *= 0.55f; // smaller silhouette reads as "kitten"
                if (_kittenObj.TryGetComponent<SpriteRenderer>(out var kittenRenderer)) kittenRenderer.color = TwistColor;
                _context.AddWorldLabel(_kittenObj, "KITTEN", Vector3.up * 1f, 10, Color.white);
                _kittenObj.SetActive(false);
            }

            _checkpointObjs = new GameObject[CheckpointOffsets.Length];
            for (int i = 0; i < CheckpointOffsets.Length; i++)
            {
                var marker = new GameObject($"BurrMazeCheckpoint_{i}");
                var renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = _context.RangeSprite != null ? _context.RangeSprite : _context.ActorSprite;
                renderer.color = new Color(SafeColor.r, SafeColor.g, SafeColor.b, 0.45f);
                renderer.sortingOrder = 2;
                bool isEnd = i == CheckpointOffsets.Length - 1;
                marker.transform.localScale = Vector3.one * (isEnd ? 2.4f : 1.6f);
                string label = i == 0 ? "START" : isEnd ? "MAZE END" : $"CHECKPOINT {i}";
                _context.AddWorldLabel(marker, label, Vector3.up * 1.1f, 11, Color.white);
                marker.SetActive(false);
                _checkpointObjs[i] = marker;
            }

            _brambleObjs = new GameObject[BrambleOffsets.Length];
            for (int i = 0; i < BrambleOffsets.Length; i++)
            {
                var patch = new GameObject($"BurrMazeBramble_{i}");
                var renderer = patch.AddComponent<SpriteRenderer>();
                renderer.sprite = _context.RangeSprite != null ? _context.RangeSprite : _context.ActorSprite;
                renderer.color = new Color(0.42f, 0.3f, 0.12f, 0.35f);
                renderer.sortingOrder = 1;
                patch.transform.localScale = Vector3.one * (BrambleRadius * 0.9f);
                // Reuses the generic grass-patch prop art (first use in the roster) tinted into a
                // bramble read rather than inventing a new art surface, per the gameplay-first
                // greybox pivot.
                MissionPropArt.AttachObject(patch, FinalGameplayArt.Grass, 0.02f, 6, false);
                _context.AddWorldLabel(patch, "BRAMBLE", Vector3.up * 1f, 10, new Color(0.85f, 0.7f, 0.4f));
                patch.SetActive(false);
                _brambleObjs[i] = patch;
            }

            _bushObjs = new GameObject[HideOffsets.Length];
            for (int i = 0; i < HideOffsets.Length; i++)
            {
                var bush = new GameObject($"BurrMazeBush_{i}");
                var renderer = bush.AddComponent<SpriteRenderer>();
                renderer.sprite = _context.RangeSprite != null ? _context.RangeSprite : _context.ActorSprite;
                renderer.color = new Color(0.2f, 0.45f, 0.2f, 0.4f);
                renderer.sortingOrder = 5;
                bush.transform.localScale = Vector3.one * (HideRadius * 1.6f);
                // Reuses the existing bush prop art - the same "cover" fiction Backyard Rescue's
                // environment pass already uses it for.
                MissionPropArt.AttachObject(bush, FinalGameplayArt.Bush, 0.02f, 12, true);
                _context.AddWorldLabel(bush, "HIDE HERE", Vector3.up * 1.1f, 10, Color.white);
                bush.SetActive(false);
                _bushObjs[i] = bush;
            }

            if (_context.Dogs != null)
            {
                int cheddarIdx = _context.IndexOfDog(DogId.Cheddar);
                int cocoaIdx = _context.IndexOfDog(DogId.Cocoa);
                if (cheddarIdx >= 0) _cheddarBurrLabel = _context.AddWorldLabel(_context.Dogs[cheddarIdx].gameObject, "BURRS 0%", new Vector3(0f, -1.05f, -0.1f), 11, Color.white);
                if (cocoaIdx >= 0) _cocoaBurrLabel = _context.AddWorldLabel(_context.Dogs[cocoaIdx].gameObject, "BURRS 0%", new Vector3(0f, -1.05f, -0.1f), 11, Color.white);
            }
        }

        private void PositionProps()
        {
            for (int i = 0; i < _checkpointObjs.Length; i++)
                if (_checkpointObjs[i] != null) _checkpointObjs[i].transform.position = CheckpointPosition(i);
            for (int i = 0; i < _brambleObjs.Length; i++)
                if (_brambleObjs[i] != null) _brambleObjs[i].transform.position = BramblePosition(i);
            for (int i = 0; i < _bushObjs.Length; i++)
                if (_bushObjs[i] != null) _bushObjs[i].transform.position = HidePosition(i);
        }

        private static void SetGroupActive(GameObject[] group, bool active)
        {
            if (group == null) return;
            foreach (var go in group) if (go != null) go.SetActive(active);
        }

        private void PlaceDog(int index, Vector2 point)
        {
            const float margin = 1.5f;
            _context.Dogs[index].transform.position = new Vector2(
                Mathf.Clamp(point.x, _context.Bounds.xMin + margin, _context.Bounds.xMax - margin),
                Mathf.Clamp(point.y, _context.Bounds.yMin + margin, _context.Bounds.yMax - margin));
            if (_context.Dogs[index].TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }

        private void PopAtDog(DogId dog, string text, Color color)
        {
            int idx = _context.IndexOfDog(dog);
            if (idx >= 0) _context.SpawnWorldPop(_context.Dogs[idx].transform.position, text, color);
        }

        private void ShowSadFor(DogId dog)
        {
            int idx = _context.IndexOfDog(dog);
            if (idx >= 0 && idx < _context.DogFeedback.Length) _context.DogFeedback[idx]?.ShowPanic();
        }

        private bool TryResolveDog(int dogIndex, out DogId dog)
        {
            dog = DogId.Cheddar;
            if (_context.Dogs == null || dogIndex < 0 || dogIndex >= _context.Dogs.Length) return false;
            if (!_context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)) return false;
            dog = identity.Id;
            return true;
        }

        private DogId DogIdAt(int dogIndex) =>
            _context.Dogs != null && dogIndex >= 0 && dogIndex < _context.Dogs.Length &&
            _context.Dogs[dogIndex] != null && _context.Dogs[dogIndex].TryGetComponent<DogIdentity>(out var identity)
                ? identity.Id : DogId.Cheddar;

        private static DogId Other(DogId dog) => dog == DogId.Cheddar ? DogId.Cocoa : DogId.Cheddar;
        private static string Name(DogId dog) => dog == DogId.Cheddar ? "Cheddar" : "Cocoa";
        private static int Pct(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
    }
}
