using System;
using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for Eagle Shadow Panic's "Shadow Run" hide phase: an eagle
    /// shadow sweeps the whole yard as a moving column, and the dogs must dash out of cover between
    /// passes to rescue toys left on the lawn and stash them back in cover.
    ///
    /// The rules a couch pair has to learn are deliberately few:
    /// <list type="bullet">
    /// <item>Shadow over you in the open = GRABBED. Cover is the only safe place.</item>
    /// <item>Carry toys to any cover to stash them. Stash every toy to clear the run.</item>
    /// <item>A grabbed dog cannot free itself: it wiggles to crack the grip, the partner runs over and
    /// pulls inside that window (the same split-timing lock as <see cref="CoopRescueTimingPuzzle"/>,
    /// which this class reuses). Too slow and the dog is CARRIED OFF - a strike.</item>
    /// <item>A dog tucked in cover can bark to make the eagle hesitate, buying the runner time. A dog
    /// barking in the open just gives its position away and hurries the shadow.</item>
    /// <item>The rope toy is too big for one dog: both must take an end and stay together.</item>
    /// </list>
    ///
    /// Cheddar and Cocoa are mechanically asymmetric rather than reskinned: Cheddar carries at full
    /// speed (chaos-puppy runner), Cocoa's bark holds the eagle's attention much longer (queen's
    /// authority, the natural decoy). Either dog can do either job, so a pair can swap roles on a
    /// replay and get a different game.
    ///
    /// Replayability comes from a seed: toy placement and each pass's speed wobble are derived from it,
    /// so a rematch plays differently while tests stay deterministic.
    ///
    /// Pure logic: the mission feeds real dog positions and time, and presents the queued
    /// <see cref="RunEvent"/>s; tests drive all of it without an engine.
    /// </summary>
    public sealed class CoopShadowRunPuzzle
    {
        public enum RunEventKind
        {
            /// <summary>A dog picked up a small toy.</summary>
            ToyPickedUp,
            /// <summary>A toy reached cover and is safe.</summary>
            ToyStashed,
            /// <summary>A carried toy was dropped back on the lawn (grab, or the rope pair split).</summary>
            ToyDropped,
            /// <summary>One dog is on the rope toy and needs the partner to take the other end.</summary>
            RopeNeedsPartner,
            /// <summary>Both dogs have the rope toy and are carrying it together.</summary>
            RopeLifted,
            /// <summary>The shadow caught a dog in the open and the eagle grabbed it.</summary>
            DogGrabbed,
            /// <summary>The partner pulled the grabbed dog free in time.</summary>
            GrabRescued,
            /// <summary>Rescue came too late: the dog was carried off and dropped back in cover. A strike.</summary>
            CarriedOff,
            /// <summary>The shadow finished a pass without grabbing anyone.</summary>
            CleanPass,
            /// <summary>Every toy of the current run is stashed.</summary>
            RunCleared,
            /// <summary>A bark from cover made the eagle hesitate.</summary>
            DecoyStarted,
            /// <summary>A bark from the open alerted the eagle and sped the shadow up.</summary>
            EagleAlerted
        }

        public readonly struct RunEvent
        {
            public readonly RunEventKind Kind;
            public readonly DogId Dog;
            public readonly int ToyIndex;
            public readonly Vector2 Position;
            public readonly int RunIndex;

            public RunEvent(RunEventKind kind, DogId dog, int toyIndex, Vector2 position, int runIndex)
            {
                Kind = kind;
                Dog = dog;
                ToyIndex = toyIndex;
                Position = position;
                RunIndex = runIndex;
            }
        }

        public enum ToyState { OnLawn, Carried, Stashed }

        public enum BarkResult { NoEffect, Decoy, OnCooldown, Alerted }

        public enum PullResult { NoGrab, WrongDog, TooFar, Mistimed, Landed, Freed }

        private sealed class Toy
        {
            public Vector2 Position;
            public bool IsRope;
            public ToyState State;
            public bool CheddarHolds;
            public bool CocoaHolds;
        }

        // ---- tuning (see Configure) ----
        private float _sweepMinX = -58.5f;
        private float _sweepMaxX = 58.5f;
        private Rect _yard = new Rect(-60f, -34f, 120f, 68f);
        private Vector2[] _covers = new Vector2[0];
        private float _coverRadius = 3f;
        private int[] _toysPerRun = { 2, 3 };
        private bool[] _ropeInRun = { false, true };
        private float[] _sweepSpeedPerRun = { 11f, 13.5f };
        private float _shadowHalfWidth = 3.5f;
        private float _edgePauseSeconds = 2f;
        private float _spotDwellSeconds = 0.2f;
        private float _pickupRadius = 1.7f;
        private float _ropeGripRadius = 2.4f;
        private float _ropeMaxSeparation = 5.5f;
        private float _cocoaDecoySeconds = 2.4f;
        private float _cheddarDecoySeconds = 1.2f;
        private float _decoyCooldownSeconds = 5f;
        private float _alertSeconds = 2f;
        private float _alertSpeedMultiplier = 1.5f;
        private int _grabPullsNeeded = 2;
        private float _grabWindowSeconds = 1.2f;
        private float _carryOffSeconds = 9f;
        private float _rescueRange = 3.5f;
        private float _cocoaCarrySpeed = 0.9f;
        private float _ropeCarrySpeed = 0.8f;

        // ---- state ----
        private readonly List<Toy> _toys = new List<Toy>();
        private readonly List<RunEvent> _events = new List<RunEvent>();
        private readonly CoopRescueTimingPuzzle _grabRescue = new CoopRescueTimingPuzzle();
        private System.Random _random = new System.Random(0);
        private int _seed;
        private float _cheddarDwell;
        private float _cocoaDwell;
        private float _edgePauseRemaining;
        private float _stallRemaining;
        private float _alertRemaining;
        private float _decoyCooldownRemaining;
        private float _carryOffRemaining;
        private float _passSpeedWobble = 1f;
        private bool _grabThisPass;
        private bool _ropePartnerCueSent;
        private DogId? _grabbedDog;
        private int _lastAnchor = -1;
        private Rect _keepOut;

        /// <summary>Index of the run in progress (0-based); equals <see cref="RunCount"/> once complete.</summary>
        public int RunIndex { get; private set; }
        public int RunCount => _toysPerRun.Length;
        public bool Complete => RunIndex >= RunCount;
        public int ToysStashedThisRun { get; private set; }
        public int ToysNeededThisRun => Complete ? 0 : _toysPerRun[RunIndex];
        public int TotalToysStashed { get; private set; }
        public int TotalGrabs { get; private set; }
        public int TotalCarryOffs { get; private set; }
        public int CleanPasses { get; private set; }
        public int PassesThisRun { get; private set; }
        public float ElapsedSeconds { get; private set; }

        /// <summary>World X of the centre of the shadow column.</summary>
        public float ShadowX { get; private set; }
        /// <summary>+1 sweeping toward max X, -1 toward min X.</summary>
        public int ShadowDirection { get; private set; } = 1;
        public float ShadowHalfWidth => _shadowHalfWidth;
        /// <summary>True while the eagle is banking off-yard between passes; nobody can be spotted.</summary>
        public bool ShadowBanking => _edgePauseRemaining > 0f;
        public bool DecoyActive => _stallRemaining > 0f;
        public bool EagleAlerted => _alertRemaining > 0f;
        public float DecoyCooldownRemaining => _decoyCooldownRemaining;

        public bool GrabActive => _grabbedDog.HasValue;
        public DogId? GrabbedDog => _grabbedDog;
        public float CarryOffRemaining => _carryOffRemaining;
        public float CarryOffSeconds => _carryOffSeconds;
        /// <summary>0 = just grabbed, 1 = about to be carried off.</summary>
        public float CarryOffNormalized => GrabActive ? Mathf.Clamp01(1f - _carryOffRemaining / _carryOffSeconds) : 0f;
        public CoopRescueTimingPuzzle GrabRescue => _grabRescue;
        public float RescueRange => _rescueRange;
        public float CoverRadius => _coverRadius;

        public int ToyCount => _toys.Count;
        public Vector2 ToyPosition(int index) => _toys[index].Position;
        public ToyState ToyStateAt(int index) => _toys[index].State;
        public bool ToyIsRope(int index) => _toys[index].IsRope;
        public int Seed => _seed;

        /// <summary>Current shadow speed in units/second, including this pass's wobble and any alert.</summary>
        public float ShadowSpeed
        {
            get
            {
                if (Complete) return 0f;
                float speed = _sweepSpeedPerRun[RunIndex] * _passSpeedWobble;
                return EagleAlerted ? speed * _alertSpeedMultiplier : speed;
            }
        }

        /// <summary>
        /// Geometry and pacing. <paramref name="yard"/> is the playable rect; the shadow column sweeps
        /// between <paramref name="sweepMinX"/> and <paramref name="sweepMaxX"/>. The per-run arrays must
        /// be the same length (one entry per run).
        /// </summary>
        public void Configure(Rect yard, float sweepMinX, float sweepMaxX, Vector2[] covers, float coverRadius,
            int[] toysPerRun, bool[] ropeInRun, float[] sweepSpeedPerRun)
        {
            if (covers == null || covers.Length == 0) throw new ArgumentException("At least one cover zone is required.", nameof(covers));
            if (toysPerRun == null || ropeInRun == null || sweepSpeedPerRun == null ||
                toysPerRun.Length == 0 || toysPerRun.Length != ropeInRun.Length || toysPerRun.Length != sweepSpeedPerRun.Length)
                throw new ArgumentException("Per-run arrays must be non-empty and the same length.");
            _yard = yard;
            _sweepMinX = Mathf.Min(sweepMinX, sweepMaxX);
            _sweepMaxX = Mathf.Max(sweepMinX, sweepMaxX);
            _covers = (Vector2[])covers.Clone();
            _coverRadius = Mathf.Max(0.5f, coverRadius);
            _toysPerRun = (int[])toysPerRun.Clone();
            _ropeInRun = (bool[])ropeInRun.Clone();
            _sweepSpeedPerRun = (float[])sweepSpeedPerRun.Clone();
        }

        /// <summary>
        /// A region toys must never spawn in or be dropped into (the backyard pool: a toy floating in
        /// the deep end is not a fair ask). Pass a zero-size rect to clear it.
        /// </summary>
        public void ConfigureKeepOut(Rect keepOut) => _keepOut = keepOut;

        /// <summary>Optional feel tuning; every argument keeps its current value when left at its default.</summary>
        public void ConfigureFeel(float shadowHalfWidth = -1f, float edgePauseSeconds = -1f, float spotDwellSeconds = -1f,
            float cocoaDecoySeconds = -1f, float cheddarDecoySeconds = -1f, float decoyCooldownSeconds = -1f,
            int grabPullsNeeded = -1, float grabWindowSeconds = -1f, float carryOffSeconds = -1f, float rescueRange = -1f)
        {
            if (shadowHalfWidth > 0f) _shadowHalfWidth = shadowHalfWidth;
            if (edgePauseSeconds >= 0f) _edgePauseSeconds = edgePauseSeconds;
            if (spotDwellSeconds >= 0f) _spotDwellSeconds = spotDwellSeconds;
            if (cocoaDecoySeconds > 0f) _cocoaDecoySeconds = cocoaDecoySeconds;
            if (cheddarDecoySeconds > 0f) _cheddarDecoySeconds = cheddarDecoySeconds;
            if (decoyCooldownSeconds >= 0f) _decoyCooldownSeconds = decoyCooldownSeconds;
            if (grabPullsNeeded > 0) _grabPullsNeeded = grabPullsNeeded;
            if (grabWindowSeconds > 0f) _grabWindowSeconds = grabWindowSeconds;
            if (carryOffSeconds > 0f) _carryOffSeconds = carryOffSeconds;
            if (rescueRange > 0f) _rescueRange = rescueRange;
        }

        /// <summary>Start a fresh attempt. The same seed always produces the same toy layout and pass wobble.</summary>
        public void Reset(int seed)
        {
            _seed = seed;
            _random = new System.Random(seed);
            _toys.Clear();
            _events.Clear();
            _grabRescue.Reset();
            RunIndex = 0;
            ToysStashedThisRun = 0;
            TotalToysStashed = 0;
            TotalGrabs = 0;
            TotalCarryOffs = 0;
            CleanPasses = 0;
            PassesThisRun = 0;
            ElapsedSeconds = 0f;
            ShadowX = _sweepMinX;
            ShadowDirection = 1;
            _cheddarDwell = 0f;
            _cocoaDwell = 0f;
            // The eagle banks in before the first pass so the dogs get a beat to read the yard.
            _edgePauseRemaining = _edgePauseSeconds;
            _stallRemaining = 0f;
            _alertRemaining = 0f;
            _decoyCooldownRemaining = 0f;
            _carryOffRemaining = 0f;
            _passSpeedWobble = 1f;
            _grabThisPass = false;
            _ropePartnerCueSent = false;
            _grabbedDog = null;
            _lastAnchor = -1;
            if (_covers.Length > 0) SpawnRunToys();
        }

        /// <summary>Hands the queued events to the mission and clears the queue.</summary>
        public void DrainEvents(List<RunEvent> into)
        {
            if (into != null) into.AddRange(_events);
            _events.Clear();
        }

        public bool IsInCover(Vector2 position) => NearestCoverDistance(position) < _coverRadius;

        public float NearestCoverDistance(Vector2 position)
        {
            float best = float.PositiveInfinity;
            for (int i = 0; i < _covers.Length; i++)
                best = Mathf.Min(best, Vector2.Distance(position, _covers[i]));
            return best;
        }

        public Vector2 NearestCover(Vector2 position)
        {
            int best = 0;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < _covers.Length; i++)
            {
                float distance = Vector2.Distance(position, _covers[i]);
                if (distance < bestDistance) { bestDistance = distance; best = i; }
            }
            return _covers.Length > 0 ? _covers[best] : position;
        }

        /// <summary>True while the shadow column overlaps this X (and the eagle is actually over the yard).</summary>
        public bool ShadowCovers(float x) => !ShadowBanking && !Complete && Mathf.Abs(x - ShadowX) <= _shadowHalfWidth;

        /// <summary>
        /// Where the shadow column will be <paramref name="secondsAhead"/> from now if nobody barks,
        /// accounting for any current stall/bank and the turn-around pause at each edge. Ignores the
        /// small random speed wobble rolled at each turn, so treat it as a readable estimate.
        /// </summary>
        public float PredictShadowX(float secondsAhead)
        {
            if (Complete) return ShadowX;
            float x = ShadowX;
            float dir = ShadowDirection;
            float speed = ShadowSpeed;
            float hold = _stallRemaining + _edgePauseRemaining;
            float t = Mathf.Max(0f, secondsAhead);
            for (int guard = 0; guard < 16 && t > 0f; guard++)
            {
                float wait = Mathf.Min(hold, t);
                t -= wait;
                hold = 0f;
                if (t <= 0f || speed <= 0f) break;
                float edge = dir > 0f ? _sweepMaxX : _sweepMinX;
                float toEdge = Mathf.Abs(edge - x) / speed;
                if (t < toEdge) { x += dir * speed * t; break; }
                x = edge;
                t -= toEdge;
                dir = -dir;
                hold = _edgePauseSeconds;
            }
            return x;
        }

        /// <summary>
        /// Seconds until the shadow column next overlaps <paramref name="x"/> (0 if it already does),
        /// searched up to <paramref name="horizon"/>. Drives the "SAFE / WAIT" read on the HUD.
        /// </summary>
        public float SecondsUntilShadowAt(float x, float horizon = 12f)
        {
            if (Complete) return float.PositiveInfinity;
            if (ShadowCovers(x)) return 0f;
            for (float t = 0.1f; t <= horizon; t += 0.1f)
                if (Mathf.Abs(PredictShadowX(t) - x) <= _shadowHalfWidth && !PredictBanking(t)) return t;
            return float.PositiveInfinity;
        }

        // True while the shadow sits parked at an edge at time t (an approximation via position).
        private bool PredictBanking(float t)
        {
            float x = PredictShadowX(t);
            return Mathf.Approximately(x, _sweepMinX) || Mathf.Approximately(x, _sweepMaxX);
        }

        /// <summary>0..1 fill of the "about to be spotted" dwell for a dog standing in the shadow.</summary>
        public float SpotDwellNormalized(DogId dog)
        {
            if (_spotDwellSeconds <= 0f) return 0f;
            return Mathf.Clamp01((dog == DogId.Cheddar ? _cheddarDwell : _cocoaDwell) / _spotDwellSeconds);
        }

        /// <summary>Index of the small toy this dog is carrying, or -1.</summary>
        public int CarriedToy(DogId dog)
        {
            for (int i = 0; i < _toys.Count; i++)
            {
                var toy = _toys[i];
                if (toy.State != ToyState.Carried || toy.IsRope) continue;
                if (dog == DogId.Cheddar ? toy.CheddarHolds : toy.CocoaHolds) return i;
            }
            return -1;
        }

        /// <summary>True when this dog has an end of the rope toy (whether or not the partner has the other).</summary>
        public bool HoldsRope(DogId dog)
        {
            for (int i = 0; i < _toys.Count; i++)
            {
                var toy = _toys[i];
                if (!toy.IsRope || toy.State == ToyState.Stashed) continue;
                if (dog == DogId.Cheddar ? toy.CheddarHolds : toy.CocoaHolds) return true;
            }
            return false;
        }

        /// <summary>True while both dogs are carrying the rope toy together.</summary>
        public bool RopeLifted
        {
            get
            {
                for (int i = 0; i < _toys.Count; i++)
                    if (_toys[i].IsRope && _toys[i].State == ToyState.Carried) return true;
                return false;
            }
        }

        /// <summary>
        /// Movement multiplier the mission should apply while a dog hauls a toy. Cheddar carries a
        /// small toy at full speed; Cocoa is a touch slower; the shared rope slows both.
        /// </summary>
        public float SpeedMultiplierFor(DogId dog)
        {
            if (RopeLifted) return _ropeCarrySpeed;
            if (CarriedToy(dog) >= 0) return dog == DogId.Cocoa ? _cocoaCarrySpeed : 1f;
            return 1f;
        }

        /// <summary>Advance the shadow, spotting, toy carrying, and any live grab by <paramref name="deltaTime"/>.</summary>
        public void Advance(float deltaTime, Vector2 cheddarPosition, Vector2 cocoaPosition)
        {
            if (Complete || deltaTime <= 0f) return;
            ElapsedSeconds += deltaTime;
            if (_decoyCooldownRemaining > 0f) _decoyCooldownRemaining = Mathf.Max(0f, _decoyCooldownRemaining - deltaTime);
            if (_alertRemaining > 0f) _alertRemaining = Mathf.Max(0f, _alertRemaining - deltaTime);

            if (GrabActive)
            {
                // The eagle is busy with its catch: the sweep holds until the grab resolves.
                AdvanceGrab(deltaTime, cheddarPosition, cocoaPosition);
            }
            else
            {
                AdvanceShadow(deltaTime);
                AdvanceSpotting(deltaTime, cheddarPosition, cocoaPosition);
            }

            if (!Complete) AdvanceToys(cheddarPosition, cocoaPosition);
        }

        /// <summary>
        /// A dog barks. From cover it is a decoy that stalls the shadow (Cocoa's lasts about twice as
        /// long as Cheddar's); from the open it alerts the eagle and briefly speeds the shadow up.
        /// </summary>
        public BarkResult Bark(DogId dog, Vector2 position)
        {
            if (Complete || GrabActive) return BarkResult.NoEffect;
            if (!IsInCover(position))
            {
                _alertRemaining = _alertSeconds;
                _events.Add(new RunEvent(RunEventKind.EagleAlerted, dog, -1, position, RunIndex));
                return BarkResult.Alerted;
            }
            if (_decoyCooldownRemaining > 0f) return BarkResult.OnCooldown;
            _stallRemaining = dog == DogId.Cocoa ? _cocoaDecoySeconds : _cheddarDecoySeconds;
            _decoyCooldownRemaining = _decoyCooldownSeconds;
            _events.Add(new RunEvent(RunEventKind.DecoyStarted, dog, -1, position, RunIndex));
            return BarkResult.Decoy;
        }

        /// <summary>The grabbed dog struggles, cracking the grip open for the partner. Returns false for anyone else.</summary>
        public bool Wiggle(DogId dog)
        {
            if (!GrabActive || _grabbedDog.Value != dog) return false;
            _grabRescue.Wiggle();
            return true;
        }

        /// <summary>The free dog pulls. Only lands in range and inside the wiggle window.</summary>
        public PullResult Pull(DogId dog, Vector2 pullerPosition, Vector2 grabbedPosition)
        {
            if (!GrabActive) return PullResult.NoGrab;
            if (_grabbedDog.Value == dog) return PullResult.WrongDog;
            if (Vector2.Distance(pullerPosition, grabbedPosition) > _rescueRange) return PullResult.TooFar;
            int before = _grabRescue.Pulls;
            _grabRescue.Pull();
            if (_grabRescue.Pulls == before) return PullResult.Mistimed;
            if (!_grabRescue.Freed) return PullResult.Landed;
            ResolveGrab(rescued: true, grabbedPosition);
            return PullResult.Freed;
        }

        /// <summary>Test/hook path: stash every remaining toy of the current run and clear it.</summary>
        public void ForceClearRun()
        {
            if (Complete) return;
            if (GrabActive) ResolveGrab(rescued: true, Vector2.zero);
            for (int i = 0; i < _toys.Count; i++)
            {
                var toy = _toys[i];
                if (toy.State == ToyState.Stashed) continue;
                toy.State = ToyState.Stashed;
                toy.CheddarHolds = false;
                toy.CocoaHolds = false;
                ToysStashedThisRun++;
                TotalToysStashed++;
            }
            ClearRun();
        }

        /// <summary>Test/hook path: the eagle grabs this dog right now, wherever it is.</summary>
        public void ForceGrab(DogId dog, Vector2 position) => StartGrab(dog, position);

        /// <summary>Test/hook path: run the carry-off timer out on the current grab.</summary>
        public void ForceCarryOff(Vector2 grabbedPosition)
        {
            if (GrabActive) ResolveGrab(rescued: false, grabbedPosition);
        }

        private void AdvanceShadow(float deltaTime)
        {
            // A stall or bank eats only its own share of the step; any leftover time keeps the shadow
            // moving, so the result doesn't depend on frame size.
            if (_stallRemaining > 0f)
            {
                float used = Mathf.Min(_stallRemaining, deltaTime);
                _stallRemaining -= used;
                deltaTime -= used;
                if (deltaTime <= 0f) return;
            }
            if (_edgePauseRemaining > 0f)
            {
                float used = Mathf.Min(_edgePauseRemaining, deltaTime);
                _edgePauseRemaining -= used;
                deltaTime -= used;
                if (deltaTime <= 0f) return;
            }

            ShadowX += ShadowDirection * ShadowSpeed * deltaTime;
            bool reachedEdge = ShadowDirection > 0 ? ShadowX >= _sweepMaxX : ShadowX <= _sweepMinX;
            if (!reachedEdge) return;

            ShadowX = ShadowDirection > 0 ? _sweepMaxX : _sweepMinX;
            ShadowDirection = -ShadowDirection;
            _edgePauseRemaining = _edgePauseSeconds;
            PassesThisRun++;
            if (!_grabThisPass)
            {
                CleanPasses++;
                _events.Add(new RunEvent(RunEventKind.CleanPass, DogId.Cheddar, -1, new Vector2(ShadowX, 0f), RunIndex));
            }
            _grabThisPass = false;
            // Each pass leans a little faster or slower so the rhythm can't be learned by rote, and
            // the eagle gets steadily more impatient the longer a run drags on.
            float impatience = 1f + Mathf.Min(0.24f, 0.04f * PassesThisRun);
            _passSpeedWobble = impatience * (0.92f + 0.16f * (float)_random.NextDouble());
        }

        private void AdvanceSpotting(float deltaTime, Vector2 cheddarPosition, Vector2 cocoaPosition)
        {
            _cheddarDwell = NextDwell(_cheddarDwell, deltaTime, cheddarPosition);
            _cocoaDwell = NextDwell(_cocoaDwell, deltaTime, cocoaPosition);
            // Cheddar first on a dead tie: the chaos puppy is always the one caught mid-zoom.
            if (_cheddarDwell >= _spotDwellSeconds && ShadowCovers(cheddarPosition.x) && !IsInCover(cheddarPosition))
                StartGrab(DogId.Cheddar, cheddarPosition);
            else if (_cocoaDwell >= _spotDwellSeconds && ShadowCovers(cocoaPosition.x) && !IsInCover(cocoaPosition))
                StartGrab(DogId.Cocoa, cocoaPosition);
        }

        private float NextDwell(float dwell, float deltaTime, Vector2 position)
        {
            bool exposed = ShadowCovers(position.x) && !IsInCover(position);
            return exposed ? dwell + deltaTime : 0f;
        }

        private void StartGrab(DogId dog, Vector2 position)
        {
            if (Complete || GrabActive) return;
            _grabbedDog = dog;
            _grabThisPass = true;
            _carryOffRemaining = _carryOffSeconds;
            _grabRescue.Configure(_grabPullsNeeded, _grabWindowSeconds);
            _cheddarDwell = 0f;
            _cocoaDwell = 0f;
            _stallRemaining = 0f;
            TotalGrabs++;
            DropToysHeldBy(dog, position);
            _events.Add(new RunEvent(RunEventKind.DogGrabbed, dog, -1, position, RunIndex));
        }

        private void AdvanceGrab(float deltaTime, Vector2 cheddarPosition, Vector2 cocoaPosition)
        {
            _grabRescue.Advance(deltaTime);
            _carryOffRemaining = Mathf.Max(0f, _carryOffRemaining - deltaTime);
            if (_carryOffRemaining > 0f) return;
            Vector2 grabbedPosition = _grabbedDog.Value == DogId.Cheddar ? cheddarPosition : cocoaPosition;
            ResolveGrab(rescued: false, grabbedPosition);
        }

        private void ResolveGrab(bool rescued, Vector2 grabbedPosition)
        {
            if (!GrabActive) return;
            DogId dog = _grabbedDog.Value;
            _grabbedDog = null;
            _carryOffRemaining = 0f;
            if (rescued)
            {
                _events.Add(new RunEvent(RunEventKind.GrabRescued, dog, -1, grabbedPosition, RunIndex));
            }
            else
            {
                TotalCarryOffs++;
                // The eagle loses interest and drops its catch into the nearest cover.
                _events.Add(new RunEvent(RunEventKind.CarriedOff, dog, -1, NearestCover(grabbedPosition), RunIndex));
            }
            // Give the pair a breath before the sweep resumes.
            _edgePauseRemaining = Mathf.Max(_edgePauseRemaining, _edgePauseSeconds);
        }

        private void AdvanceToys(Vector2 cheddarPosition, Vector2 cocoaPosition)
        {
            bool cheddarFree = _grabbedDog != DogId.Cheddar;
            bool cocoaFree = _grabbedDog != DogId.Cocoa;

            for (int i = 0; i < _toys.Count; i++)
            {
                var toy = _toys[i];
                if (toy.State == ToyState.Stashed) continue;
                if (toy.IsRope) AdvanceRope(i, toy, cheddarPosition, cocoaPosition, cheddarFree, cocoaFree);
                else AdvanceSmallToy(i, toy, cheddarPosition, cocoaPosition, cheddarFree, cocoaFree);
                if (Complete) return;
            }
        }

        private void AdvanceSmallToy(int index, Toy toy, Vector2 cheddarPosition, Vector2 cocoaPosition,
            bool cheddarFree, bool cocoaFree)
        {
            if (toy.State == ToyState.OnLawn)
            {
                // Cheddar wins a simultaneous grab: he was always going to get there first.
                if (cheddarFree && !IsCarryingAnything(DogId.Cheddar) &&
                    Vector2.Distance(cheddarPosition, toy.Position) <= _pickupRadius)
                    PickUp(index, toy, DogId.Cheddar, cheddarPosition);
                else if (cocoaFree && !IsCarryingAnything(DogId.Cocoa) &&
                    Vector2.Distance(cocoaPosition, toy.Position) <= _pickupRadius)
                    PickUp(index, toy, DogId.Cocoa, cocoaPosition);
                return;
            }

            DogId carrier = toy.CheddarHolds ? DogId.Cheddar : DogId.Cocoa;
            toy.Position = carrier == DogId.Cheddar ? cheddarPosition : cocoaPosition;
            if (IsInCover(toy.Position)) Stash(index, toy, carrier);
        }

        private void AdvanceRope(int index, Toy toy, Vector2 cheddarPosition, Vector2 cocoaPosition,
            bool cheddarFree, bool cocoaFree)
        {
            if (toy.State == ToyState.OnLawn)
            {
                bool cheddarOn = cheddarFree && CarriedToy(DogId.Cheddar) < 0 &&
                    Vector2.Distance(cheddarPosition, toy.Position) <= _ropeGripRadius;
                bool cocoaOn = cocoaFree && CarriedToy(DogId.Cocoa) < 0 &&
                    Vector2.Distance(cocoaPosition, toy.Position) <= _ropeGripRadius;
                toy.CheddarHolds = cheddarOn;
                toy.CocoaHolds = cocoaOn;
                if (cheddarOn && cocoaOn)
                {
                    toy.State = ToyState.Carried;
                    _ropePartnerCueSent = false;
                    _events.Add(new RunEvent(RunEventKind.RopeLifted, DogId.Cheddar, index, toy.Position, RunIndex));
                }
                else if (cheddarOn || cocoaOn)
                {
                    if (!_ropePartnerCueSent)
                    {
                        _ropePartnerCueSent = true;
                        _events.Add(new RunEvent(RunEventKind.RopeNeedsPartner, cheddarOn ? DogId.Cheddar : DogId.Cocoa,
                            index, toy.Position, RunIndex));
                    }
                }
                else
                {
                    _ropePartnerCueSent = false;
                }
                return;
            }

            // Carried by both: the rope rides between them and snaps free if they drift apart.
            if (Vector2.Distance(cheddarPosition, cocoaPosition) > _ropeMaxSeparation)
            {
                Drop(index, toy, DogId.Cheddar);
                return;
            }
            toy.Position = (cheddarPosition + cocoaPosition) * 0.5f;
            if (IsInCover(toy.Position)) Stash(index, toy, DogId.Cheddar);
        }

        private bool IsCarryingAnything(DogId dog) => CarriedToy(dog) >= 0 || (RopeLifted && HoldsRope(dog));

        private void PickUp(int index, Toy toy, DogId dog, Vector2 position)
        {
            toy.State = ToyState.Carried;
            toy.CheddarHolds = dog == DogId.Cheddar;
            toy.CocoaHolds = dog == DogId.Cocoa;
            toy.Position = position;
            _events.Add(new RunEvent(RunEventKind.ToyPickedUp, dog, index, position, RunIndex));
        }

        private void Drop(int index, Toy toy, DogId dog)
        {
            toy.State = ToyState.OnLawn;
            toy.CheddarHolds = false;
            toy.CocoaHolds = false;
            toy.Position = ClampToLawn(toy.Position);
            _ropePartnerCueSent = false;
            _events.Add(new RunEvent(RunEventKind.ToyDropped, dog, index, toy.Position, RunIndex));
        }

        private void DropToysHeldBy(DogId dog, Vector2 position)
        {
            for (int i = 0; i < _toys.Count; i++)
            {
                var toy = _toys[i];
                if (toy.State != ToyState.Carried) continue;
                bool holds = dog == DogId.Cheddar ? toy.CheddarHolds : toy.CocoaHolds;
                if (!holds) continue;
                if (!toy.IsRope) toy.Position = position;
                Drop(i, toy, dog);
            }
        }

        private void Stash(int index, Toy toy, DogId dog)
        {
            toy.State = ToyState.Stashed;
            toy.CheddarHolds = false;
            toy.CocoaHolds = false;
            ToysStashedThisRun++;
            TotalToysStashed++;
            _events.Add(new RunEvent(RunEventKind.ToyStashed, dog, index, toy.Position, RunIndex));
            if (ToysStashedThisRun >= _toysPerRun[RunIndex]) ClearRun();
        }

        private void ClearRun()
        {
            int cleared = RunIndex;
            RunIndex++;
            ToysStashedThisRun = 0;
            PassesThisRun = 0;
            _passSpeedWobble = 1f;
            _ropePartnerCueSent = false;
            _events.Add(new RunEvent(RunEventKind.RunCleared, DogId.Cheddar, -1, Vector2.zero, cleared));
            if (!Complete) SpawnRunToys();
        }

        // Each run picks a different "home" bush and scatters its toys around that bush and its
        // nearest neighbour: a fair dash out onto the lawn, toward the middle of the yard, never inside
        // a cover radius and never stacked. Localising a run keeps it about timing, not marathons,
        // while the seed moves the action around the yard from attempt to attempt.
        private void SpawnRunToys()
        {
            _toys.Clear();
            int count = _toysPerRun[RunIndex];
            bool rope = _ropeInRun[RunIndex];
            Vector2 centre = _yard.center;
            int anchor = _random.Next(0, _covers.Length);
            if (anchor == _lastAnchor && _covers.Length > 1) anchor = (anchor + 1) % _covers.Length;
            _lastAnchor = anchor;
            int neighbour = anchor;
            float neighbourDistance = float.PositiveInfinity;
            for (int c = 0; c < _covers.Length; c++)
            {
                if (c == anchor) continue;
                float d = Vector2.Distance(_covers[c], _covers[anchor]);
                if (d < neighbourDistance) { neighbourDistance = d; neighbour = c; }
            }
            for (int i = 0; i < count; i++)
            {
                bool isRope = rope && i == count - 1;
                Vector2 position = Vector2.zero;
                for (int attempt = 0; attempt < 24; attempt++)
                {
                    Vector2 cover = _covers[i % 2 == 0 ? anchor : neighbour];
                    Vector2 inward = (centre - cover).normalized;
                    if (inward.sqrMagnitude < 0.01f) inward = Vector2.up;
                    float swing = Mathf.Lerp(-70f, 70f, (float)_random.NextDouble()) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(
                        inward.x * Mathf.Cos(swing) - inward.y * Mathf.Sin(swing),
                        inward.x * Mathf.Sin(swing) + inward.y * Mathf.Cos(swing));
                    float minReach = isRope ? 11f : 9f;
                    float maxReach = isRope ? 14f : 13f;
                    float reach = Mathf.Lerp(minReach, maxReach, (float)_random.NextDouble()) + RunIndex;
                    position = ClampToLawn(cover + direction * reach);
                    if (NearestCoverDistance(position) < _coverRadius + 5f) continue;
                    if (InKeepOut(position)) continue;
                    bool crowded = false;
                    for (int j = 0; j < _toys.Count; j++)
                        if (Vector2.Distance(position, _toys[j].Position) < 6f) { crowded = true; break; }
                    if (!crowded) break;
                }
                _toys.Add(new Toy { Position = position, IsRope = isRope, State = ToyState.OnLawn });
            }
        }

        private Vector2 ClampToLawn(Vector2 position)
        {
            const float margin = 3f;
            var clamped = new Vector2(
                Mathf.Clamp(position.x, _yard.xMin + margin, _yard.xMax - margin),
                Mathf.Clamp(position.y, _yard.yMin + margin, _yard.yMax - margin));
            if (!InKeepOut(clamped)) return clamped;
            // Nudge a dropped toy out to the nearest dry edge of the keep-out region.
            const float lip = 2.5f;
            float left = clamped.x - _keepOut.xMin, right = _keepOut.xMax - clamped.x;
            float down = clamped.y - _keepOut.yMin, up = _keepOut.yMax - clamped.y;
            float min = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
            if (min == left) clamped.x = _keepOut.xMin - lip;
            else if (min == right) clamped.x = _keepOut.xMax + lip;
            else if (min == down) clamped.y = _keepOut.yMin - lip;
            else clamped.y = _keepOut.yMax + lip;
            return clamped;
        }

        private bool InKeepOut(Vector2 position)
        {
            if (_keepOut.width <= 0f || _keepOut.height <= 0f) return false;
            return position.x > _keepOut.xMin - 1.5f && position.x < _keepOut.xMax + 1.5f &&
                position.y > _keepOut.yMin - 1.5f && position.y < _keepOut.yMax + 1.5f;
        }
    }
}
