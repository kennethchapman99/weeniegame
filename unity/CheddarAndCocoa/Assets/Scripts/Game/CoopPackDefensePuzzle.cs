using System;
using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for Coyotes at the Fence's "Pack Defense": a coyote prowls the
    /// outside of the fence to a weak spot and starts DIGGING under it. The dig is a visible meter;
    /// if it fills, the coyote breaches. The dogs keep their distinct jobs:
    /// <list type="bullet">
    /// <item><b>Cocoa pins.</b> Her bark (in range) pins the coyote and shoves it off the hole for a
    /// few seconds. The dig freezes but is not undone.</item>
    /// <item><b>Coyotes are cowards.</b> If Cocoa is already standing guard at the weak spot when
    /// the coyote arrives, it slinks off to a different one ("GUARDED!") - so she has to hang back
    /// and pounce once it commits to digging. Cheddar doesn't scare it at all. From the second wave
    /// the coyote also FEINTS: it heads for one weak spot, then cuts back to the real one.</item>
    /// <item><b>Cheddar fills.</b> Only while the coyote is pinned, Cheddar Interacts at the weak spot
    /// to kick the dirt back in - a repair. The coyote slinks off to the next weak spot, digging
    /// faster each wave.</item>
    /// <item><b>The scout.</b> From the second wave a scrawny scout coyote starts a slow dig at a
    /// different weak spot. Cocoa will not leave the big one for it; only Cheddar's yappy charge (bark
    /// in range) chases the scout off. So the pair has to split up and come back together.</item>
    /// <item><b>The bait.</b> Before the last repair the coyote tosses a snack over the fence. If
    /// Cheddar wanders near it he is LURED: slowed and dragged toward the snack. Cocoa barks near him
    /// ("LEAVE IT!") to snap him out, or pins the coyote to call its bluff. If Cheddar eats it the
    /// coyote gets a free head start on its dig.</item>
    /// <item><b>The final push.</b> Fence patched, the coyote and its scout charge together: the dogs
    /// have a few seconds to huddle and bark as one. Too slow and the push breaches, then comes again.</item>
    /// </list>
    /// Weak-spot placement along the fence is seeded, so every attempt walks a different route.
    /// Pure logic: the mission feeds dog positions and time and presents the queued events; tests
    /// drive all of it without an engine.
    /// </summary>
    public sealed class CoopPackDefensePuzzle
    {
        public enum EventKind
        {
            /// <summary>The coyote reached the active weak spot and started digging.</summary>
            DigStarted,
            /// <summary>Cocoa pinned the coyote off the hole.</summary>
            Pinned,
            /// <summary>Cocoa was standing guard at the weak spot, so the coyote picked another one.</summary>
            SpotGuarded,
            /// <summary>The coyote faked toward one weak spot and cut back to the real one.</summary>
            Feint,
            /// <summary>The pin ran out before Cheddar filled the hole; the coyote is back on it.</summary>
            PinExpired,
            /// <summary>Cheddar filled the active weak spot.</summary>
            Repaired,
            /// <summary>A dig completed while the coyote was pinned - it gave up on that hole for now.</summary>
            DrivenBack,
            /// <summary>The main coyote dug through.</summary>
            Breached,
            /// <summary>The scout started digging at another weak spot.</summary>
            ScoutArrived,
            /// <summary>Cheddar chased the scout off.</summary>
            ScoutChased,
            /// <summary>The scout dug through.</summary>
            ScoutBreached,
            /// <summary>The coyote tossed bait into the yard.</summary>
            LureStarted,
            /// <summary>Cheddar got close to the bait and is being dragged to it.</summary>
            CheddarLured,
            /// <summary>Cocoa barked Cheddar out of the lure.</summary>
            LeaveIt,
            /// <summary>Cheddar ate the bait; the coyote's dig jumped ahead.</summary>
            BaitEaten,
            /// <summary>The lure ended without Cheddar eating it (Cocoa pinned the coyote or barked him off).</summary>
            LureResolved,
            /// <summary>The fence is patched; the final charge is on.</summary>
            FinalPushStarted,
            /// <summary>The final push ran out the clock and breached; it will come again.</summary>
            FinalPushBroke,
            /// <summary>United bark blocked the final push. Mission won.</summary>
            FinalPushBlocked
        }

        public readonly struct PackEvent
        {
            public readonly EventKind Kind;
            public readonly DogId Dog;
            public readonly int Gap;
            public readonly Vector2 Position;

            public PackEvent(EventKind kind, DogId dog, int gap, Vector2 position)
            {
                Kind = kind;
                Dog = dog;
                Gap = gap;
                Position = position;
            }
        }

        public enum CoyoteState { Prowling, Digging, Pinned, Charging, Gone }

        public enum BarkResult { Nothing, Pinned, AlreadyPinned, OutOfRange, WrongDog, ScoutChased, LeaveIt, NotActive }

        public enum FillResult { Filled, NotPinned, TooFar, WrongDog, NotActive }

        // ---- tuning ----
        private Rect _yard = new Rect(-60f, -34f, 120f, 68f);
        private Rect _keepOut;
        private int _gapCount = 4;
        private int _requiredRepairs = 3;
        private float[] _digSecondsPerWave = { 8f, 6.5f, 5f };
        private float _prowlSpeed = 9f;
        private float _pinSeconds = 2.6f;
        private float _pinShove = 3f;
        private float _pinRange = 4f;
        private float _fillRange = 2.4f;
        private float _scoutDigSeconds = 11f;
        private float _scoutRange = 4.5f;
        private float _scoutRespawnSeconds = 6f;
        private float _lureRadius = 7f;
        private float _lurePullSpeed = 2.6f;
        private float _lureSpeedMultiplier = 0.45f;
        private float _leaveItRange = 6f;
        private float _baitEatenDigBonus = 0.35f;
        private float _finalPushSeconds = 8f;
        private float _finalPushRetrySeconds = 2f;
        private float _guardRadius = 7f;
        private float _feintChance = 0.6f;

        // ---- state ----
        private readonly List<PackEvent> _events = new List<PackEvent>();
        private System.Random _random = new System.Random(0);
        private Vector2[] _gaps = new Vector2[0];
        private Vector2[] _gapNormals = new Vector2[0];
        private float _scoutCooldown;
        private float _finalRetryRemaining;
        private int _feintGap = -1;

        public int ActiveGap { get; private set; }
        public int Repairs { get; private set; }
        public int Breaches { get; private set; }
        public int Pins { get; private set; }
        public int Seed { get; private set; }
        public CoyoteState Coyote { get; private set; }
        public Vector2 CoyotePosition { get; private set; }
        public float Dig { get; private set; }
        public float PinRemaining { get; private set; }
        public bool PinHeld => Coyote == CoyoteState.Pinned;
        public int Guarded { get; private set; }
        public int Feints { get; private set; }
        public float GuardRadius => _guardRadius;
        /// <summary>The weak spot the coyote is actually walking toward right now (differs from
        /// <see cref="ActiveGap"/> during a feint).</summary>
        public int ProwlTarget => _feintGap >= 0 ? _feintGap : ActiveGap;

        public bool ScoutActive { get; private set; }
        public bool ScoutDigging { get; private set; }
        public int ScoutGap { get; private set; } = -1;
        public Vector2 ScoutPosition { get; private set; }
        public float ScoutDig { get; private set; }
        public int ScoutsChased { get; private set; }

        public bool LureActive { get; private set; }
        public Vector2 BaitPosition { get; private set; }
        public bool CheddarLured { get; private set; }

        public bool FinalPushActive { get; private set; }
        public float FinalPushRemaining { get; private set; }
        public float FinalPushSeconds => _finalPushSeconds;
        public bool Complete { get; private set; }
        public bool ReadyForFinalPush => Repairs >= _requiredRepairs && !Complete;

        public int GapCount => _gaps.Length;
        public Vector2 GapPosition(int index) => _gaps[index];
        public Vector2[] Gaps => (Vector2[])_gaps.Clone();
        public float PinRange => _pinRange;
        public float FillRange => _fillRange;
        public float ScoutRange => _scoutRange;
        public float LeaveItRange => _leaveItRange;
        public float LureRadius => _lureRadius;
        public float DigSecondsThisWave => _digSecondsPerWave[Mathf.Clamp(Repairs, 0, _digSecondsPerWave.Length - 1)];
        public Vector2 ActiveGapPosition => _gaps.Length > 0 ? _gaps[ActiveGap % _gaps.Length] : Vector2.zero;

        public void Configure(Rect yard, Rect keepOut, int gapCount, int requiredRepairs, float[] digSecondsPerWave)
        {
            if (gapCount < 2) throw new ArgumentException("Need at least two weak spots.", nameof(gapCount));
            if (digSecondsPerWave == null || digSecondsPerWave.Length == 0) throw new ArgumentException("Need dig pacing.", nameof(digSecondsPerWave));
            _yard = yard;
            _keepOut = keepOut;
            _gapCount = gapCount;
            _requiredRepairs = Mathf.Max(1, requiredRepairs);
            _digSecondsPerWave = (float[])digSecondsPerWave.Clone();
        }

        /// <summary>Optional feel tuning; arguments left at their defaults keep the current values.</summary>
        public void ConfigureFeel(float prowlSpeed = -1f, float pinSeconds = -1f, float pinRange = -1f, float fillRange = -1f,
            float scoutDigSeconds = -1f, float scoutRange = -1f, float lureRadius = -1f, float lurePullSpeed = -1f,
            float leaveItRange = -1f, float finalPushSeconds = -1f, float guardRadius = -1f, float feintChance = -1f)
        {
            if (guardRadius > 0f) _guardRadius = guardRadius;
            if (feintChance >= 0f) _feintChance = feintChance;
            if (prowlSpeed > 0f) _prowlSpeed = prowlSpeed;
            if (pinSeconds > 0f) _pinSeconds = pinSeconds;
            if (pinRange > 0f) _pinRange = pinRange;
            if (fillRange > 0f) _fillRange = fillRange;
            if (scoutDigSeconds > 0f) _scoutDigSeconds = scoutDigSeconds;
            if (scoutRange > 0f) _scoutRange = scoutRange;
            if (lureRadius > 0f) _lureRadius = lureRadius;
            if (lurePullSpeed > 0f) _lurePullSpeed = lurePullSpeed;
            if (leaveItRange > 0f) _leaveItRange = leaveItRange;
            if (finalPushSeconds > 0f) _finalPushSeconds = finalPushSeconds;
        }

        public void Reset(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
            _events.Clear();
            ComputeGaps(_yard, _keepOut, _gapCount, seed, out _gaps, out _gapNormals);
            ActiveGap = 0;
            Repairs = 0;
            Breaches = 0;
            Pins = 0;
            Dig = 0f;
            PinRemaining = 0f;
            ScoutActive = false;
            ScoutDigging = false;
            ScoutGap = -1;
            ScoutDig = 0f;
            ScoutsChased = 0;
            _scoutCooldown = 0f;
            LureActive = false;
            CheddarLured = false;
            FinalPushActive = false;
            FinalPushRemaining = 0f;
            _finalRetryRemaining = 0f;
            Complete = false;
            Guarded = 0;
            Feints = 0;
            _feintGap = -1;
            // The coyote starts outside the fence a little way down from the first weak spot.
            CoyotePosition = DigSpot(0) + Tangent(0) * 14f;
            Coyote = CoyoteState.Prowling;
        }

        public void DrainEvents(List<PackEvent> into)
        {
            if (into != null) into.AddRange(_events);
            _events.Clear();
        }

        /// <summary>The fence weak spot index's outside dig position (where the coyote stands).</summary>
        public Vector2 DigSpot(int gap) => _gaps[gap] + _gapNormals[gap] * 1.5f;

        /// <summary>The movement multiplier the mission should apply to Cheddar while the bait has him.</summary>
        public float SpeedMultiplierFor(DogId dog) => dog == DogId.Cheddar && CheddarLured ? _lureSpeedMultiplier : 1f;

        /// <summary>
        /// The pull the bait exerts on Cheddar this step (a world-space displacement to add), or zero.
        /// </summary>
        public Vector2 LureDrift(Vector2 cheddarPosition, float deltaTime)
        {
            if (!CheddarLured) return Vector2.zero;
            Vector2 toBait = BaitPosition - cheddarPosition;
            float distance = toBait.magnitude;
            if (distance < 0.01f) return Vector2.zero;
            return toBait / distance * Mathf.Min(distance, _lurePullSpeed * deltaTime);
        }

        public void Advance(float deltaTime, Vector2 cheddarPosition, Vector2 cocoaPosition)
        {
            if (Complete || deltaTime <= 0f) return;
            if (FinalPushActive || ReadyForFinalPush) { AdvanceFinalPush(deltaTime, cheddarPosition, cocoaPosition); return; }

            AdvanceCoyote(deltaTime, cocoaPosition);
            AdvanceScout(deltaTime);
            AdvanceLure(cheddarPosition);
        }

        /// <summary>A dog barks at <paramref name="position"/>. Cocoa pins/leaves-it; Cheddar chases the scout.</summary>
        public BarkResult Bark(DogId dog, Vector2 position, Vector2 cheddarPosition)
        {
            if (Complete || ReadyForFinalPush) return BarkResult.NotActive;
            if (dog == DogId.Cocoa)
            {
                bool nearCoyote = Vector2.Distance(position, CoyotePosition) <= _pinRange;
                // A digging coyote is pinned off its hole; a prowling one can be intercepted on the
                // way (pinned where it stands) if Cocoa reads where it's going.
                if (nearCoyote && (Coyote == CoyoteState.Digging || Coyote == CoyoteState.Prowling))
                {
                    Pin(DogId.Cocoa);
                    return BarkResult.Pinned;
                }
                if (nearCoyote && Coyote == CoyoteState.Pinned) return BarkResult.AlreadyPinned;
                if (CheddarLured && Vector2.Distance(position, cheddarPosition) <= _leaveItRange)
                {
                    CheddarLured = false;
                    LureActive = false;
                    _events.Add(new PackEvent(EventKind.LeaveIt, DogId.Cocoa, -1, cheddarPosition));
                    _events.Add(new PackEvent(EventKind.LureResolved, DogId.Cocoa, -1, BaitPosition));
                    return BarkResult.LeaveIt;
                }
                return BarkResult.OutOfRange;
            }

            if (ScoutActive && Vector2.Distance(position, ScoutPosition) <= _scoutRange)
            {
                ChaseScout();
                return BarkResult.ScoutChased;
            }
            if (Vector2.Distance(position, CoyotePosition) <= _pinRange) return BarkResult.WrongDog;
            return BarkResult.Nothing;
        }

        /// <summary>Test/hook path: Cocoa pins the coyote regardless of distance.</summary>
        public bool ForcePin()
        {
            if (Complete || ReadyForFinalPush || Coyote == CoyoteState.Pinned) return false;
            Pin(DogId.Cocoa);
            return true;
        }

        public FillResult Fill(DogId dog, Vector2 position, bool ignoreDistance = false)
        {
            if (Complete || ReadyForFinalPush) return FillResult.NotActive;
            if (dog != DogId.Cheddar) return FillResult.WrongDog;
            if (Coyote != CoyoteState.Pinned) return FillResult.NotPinned;
            if (!ignoreDistance && Vector2.Distance(position, ActiveGapPosition) > _fillRange) return FillResult.TooFar;

            int repaired = ActiveGap;
            Repairs++;
            PinRemaining = 0f;
            Dig = 0f;
            _events.Add(new PackEvent(EventKind.Repaired, DogId.Cheddar, repaired, _gaps[repaired]));
            AdvanceToNextGap();
            if (Repairs == 1 && !ScoutActive) _scoutCooldown = 2.5f; // the scout shows up for wave two
            if (Repairs == _requiredRepairs - 1 && !LureActive) StartLure();
            if (ReadyForFinalPush) StartFinalPush();
            return FillResult.Filled;
        }

        /// <summary>
        /// Resolve the current dig as if it just completed: a pinned coyote is driven back off this
        /// hole, an unpinned one breaches.
        /// </summary>
        public void ForceReach()
        {
            if (Complete || ReadyForFinalPush) return;
            if (Coyote == CoyoteState.Pinned)
            {
                PinRemaining = 0f;
                Dig = 0f;
                Coyote = CoyoteState.Prowling;
                CoyotePosition = DigSpot(ActiveGap) + _gapNormals[ActiveGap] * 6f;
                _events.Add(new PackEvent(EventKind.DrivenBack, DogId.Cocoa, ActiveGap, _gaps[ActiveGap]));
                return;
            }
            Breach();
        }

        /// <summary>Test/hook path: the coyote breaches the active weak spot now.</summary>
        public void ForceBreach() => Breach();

        /// <summary>Test/hook path: the pin runs out now.</summary>
        public void ForcePinTimeout()
        {
            if (Coyote != CoyoteState.Pinned) return;
            PinRemaining = 0f;
            ReleasePin();
        }

        /// <summary>Test/hook path: start the bait beat now.</summary>
        public void ForceLure() { if (!LureActive && !Complete) StartLure(); }

        /// <summary>Test/hook path: the scout starts digging now.</summary>
        public void ForceScout() { if (!ScoutActive && !Complete && !ReadyForFinalPush) SpawnScout(); }

        /// <summary>
        /// The pair barked together while huddled (the orchestrator's united-bark detection). Blocks
        /// the final push when it is on.
        /// </summary>
        public bool UnitedBark()
        {
            if (!ReadyForFinalPush || Complete) return false;
            if (!FinalPushActive) StartFinalPush();
            Complete = true;
            FinalPushActive = false;
            Coyote = CoyoteState.Gone;
            ScoutActive = false;
            _events.Add(new PackEvent(EventKind.FinalPushBlocked, DogId.Cocoa, -1, CoyotePosition));
            return true;
        }

        // ------------------------------------------------------------------ internals

        private void Pin(DogId dog)
        {
            Pins++;
            Coyote = CoyoteState.Pinned;
            _feintGap = -1;
            PinRemaining = _pinSeconds;
            // Shoved back out past the fence: off the hole if it was digging, or just held where it
            // was intercepted along the way.
            bool atHole = Vector2.Distance(CoyotePosition, DigSpot(ActiveGap)) < 1f;
            CoyotePosition = atHole
                ? DigSpot(ActiveGap) + _gapNormals[ActiveGap] * _pinShove
                : CoyotePosition + _gapNormals[ActiveGap] * (_pinShove * 0.5f);
            _events.Add(new PackEvent(EventKind.Pinned, dog, ActiveGap, CoyotePosition));
            if (LureActive)
            {
                LureActive = false;
                CheddarLured = false;
                _events.Add(new PackEvent(EventKind.LureResolved, dog, -1, BaitPosition));
            }
        }

        private void ReleasePin()
        {
            Coyote = CoyoteState.Prowling;
            _events.Add(new PackEvent(EventKind.PinExpired, DogId.Cocoa, ActiveGap, CoyotePosition));
        }

        private void AdvanceCoyote(float deltaTime, Vector2 cocoaPosition)
        {
            switch (Coyote)
            {
                case CoyoteState.Pinned:
                    PinRemaining = Mathf.Max(0f, PinRemaining - deltaTime);
                    if (PinRemaining <= 0f) ReleasePin();
                    break;
                case CoyoteState.Prowling:
                {
                    Vector2 spot = DigSpot(ProwlTarget);
                    CoyotePosition = Vector2.MoveTowards(CoyotePosition, spot, _prowlSpeed * deltaTime);
                    if (_feintGap >= 0)
                    {
                        // Most of the way to the fake: cut back toward the real weak spot.
                        if (Vector2.Distance(CoyotePosition, spot) < 6f)
                        {
                            _feintGap = -1;
                            Feints++;
                            _events.Add(new PackEvent(EventKind.Feint, DogId.Cocoa, ActiveGap, CoyotePosition));
                        }
                        break;
                    }
                    if (Vector2.Distance(CoyotePosition, spot) >= 0.05f) break;
                    if (Vector2.Distance(cocoaPosition, _gaps[ActiveGap]) <= _guardRadius && Dig <= 0f)
                    {
                        // Cocoa's already standing guard here - not worth it. Try a different hole.
                        Guarded++;
                        int guarded = ActiveGap;
                        ActiveGap = NextUnguardedGap(cocoaPosition);
                        if (ScoutActive && ScoutGap == ActiveGap) MoveScoutOffActiveGap();
                        _events.Add(new PackEvent(EventKind.SpotGuarded, DogId.Cocoa, guarded, _gaps[guarded]));
                        break;
                    }
                    Coyote = CoyoteState.Digging;
                    _events.Add(new PackEvent(EventKind.DigStarted, DogId.Cocoa, ActiveGap, _gaps[ActiveGap]));
                    break;
                }
                case CoyoteState.Digging:
                    Dig = Mathf.Min(1f, Dig + deltaTime / DigSecondsThisWave);
                    if (Dig >= 1f) Breach();
                    break;
            }
        }

        private int NextUnguardedGap(Vector2 cocoaPosition)
        {
            for (int step = 1; step < _gaps.Length; step++)
            {
                int candidate = (ActiveGap + step) % _gaps.Length;
                if (Vector2.Distance(cocoaPosition, _gaps[candidate]) > _guardRadius) return candidate;
            }
            return (ActiveGap + 1) % _gaps.Length;
        }

        private void Breach()
        {
            int breached = ActiveGap;
            Breaches++;
            Dig = 0f;
            PinRemaining = 0f;
            _events.Add(new PackEvent(EventKind.Breached, DogId.Cocoa, breached, _gaps[breached]));
            AdvanceToNextGap();
        }

        private void AdvanceToNextGap()
        {
            ActiveGap = (ActiveGap + 1) % _gaps.Length;
            if (ScoutActive && ScoutGap == ActiveGap) MoveScoutOffActiveGap();
            Coyote = CoyoteState.Prowling;
            _feintGap = -1;
            // From wave two the coyote sometimes heads for a different weak spot first.
            if (Repairs + Breaches >= 1 && _random.NextDouble() < _feintChance)
            {
                int fake = (ActiveGap + 1 + _random.Next(_gaps.Length - 1)) % _gaps.Length;
                if (fake != ActiveGap) _feintGap = fake;
            }
        }

        private void AdvanceScout(float deltaTime)
        {
            if (!ScoutActive)
            {
                if (Repairs < 1) return; // the scout only joins from wave two
                _scoutCooldown -= deltaTime;
                if (_scoutCooldown <= 0f) SpawnScout();
                return;
            }
            Vector2 spot = DigSpot(ScoutGap);
            if (!ScoutDigging)
            {
                ScoutPosition = Vector2.MoveTowards(ScoutPosition, spot, _prowlSpeed * 0.8f * deltaTime);
                if (Vector2.Distance(ScoutPosition, spot) < 0.05f) ScoutDigging = true;
                return;
            }
            ScoutDig = Mathf.Min(1f, ScoutDig + deltaTime / _scoutDigSeconds);
            if (ScoutDig < 1f) return;
            Breaches++;
            _events.Add(new PackEvent(EventKind.ScoutBreached, DogId.Cheddar, ScoutGap, _gaps[ScoutGap]));
            ScoutActive = false;
            ScoutDigging = false;
            ScoutDig = 0f;
            _scoutCooldown = _scoutRespawnSeconds;
        }

        private void SpawnScout()
        {
            ScoutActive = true;
            ScoutDigging = false;
            ScoutDig = 0f;
            // The scout picks a weak spot that is NOT the one the big coyote is on, and prefers one
            // far from it - the whole point is to pull the pair apart.
            int best = -1;
            float bestDistance = -1f;
            for (int i = 0; i < _gaps.Length; i++)
            {
                if (i == ActiveGap) continue;
                float d = Vector2.Distance(_gaps[i], _gaps[ActiveGap]) + (float)_random.NextDouble() * 12f;
                if (d > bestDistance) { bestDistance = d; best = i; }
            }
            ScoutGap = Mathf.Max(0, best);
            ScoutPosition = DigSpot(ScoutGap) + Tangent(ScoutGap) * 10f;
            _events.Add(new PackEvent(EventKind.ScoutArrived, DogId.Cheddar, ScoutGap, _gaps[ScoutGap]));
        }

        private void MoveScoutOffActiveGap()
        {
            for (int step = 1; step < _gaps.Length; step++)
            {
                int candidate = (ActiveGap + step) % _gaps.Length;
                if (candidate == ActiveGap) continue;
                ScoutGap = candidate;
                ScoutDigging = false;
                return;
            }
        }

        private void ChaseScout()
        {
            ScoutsChased++;
            _events.Add(new PackEvent(EventKind.ScoutChased, DogId.Cheddar, ScoutGap, ScoutPosition));
            ScoutActive = false;
            ScoutDigging = false;
            ScoutDig = 0f;
            _scoutCooldown = _scoutRespawnSeconds;
        }

        private void StartLure()
        {
            LureActive = true;
            CheddarLured = false;
            // The bait lands on the lawn between the active weak spot and the middle of the yard.
            Vector2 gap = ActiveGapPosition;
            Vector2 inward = (_yard.center - gap).normalized;
            float reach = 9f + (float)_random.NextDouble() * 5f;
            BaitPosition = ClampToYard(gap + inward * reach);
            _events.Add(new PackEvent(EventKind.LureStarted, DogId.Cheddar, ActiveGap, BaitPosition));
        }

        private void AdvanceLure(Vector2 cheddarPosition)
        {
            if (!LureActive) return;
            float distance = Vector2.Distance(cheddarPosition, BaitPosition);
            if (!CheddarLured && distance <= _lureRadius)
            {
                CheddarLured = true;
                _events.Add(new PackEvent(EventKind.CheddarLured, DogId.Cheddar, -1, cheddarPosition));
            }
            if (CheddarLured && distance <= 0.8f)
            {
                CheddarLured = false;
                LureActive = false;
                Dig = Mathf.Min(0.95f, Dig + _baitEatenDigBonus);
                _events.Add(new PackEvent(EventKind.BaitEaten, DogId.Cheddar, ActiveGap, BaitPosition));
            }
        }

        private void StartFinalPush()
        {
            FinalPushActive = true;
            FinalPushRemaining = _finalPushSeconds;
            _finalRetryRemaining = 0f;
            Coyote = CoyoteState.Charging;
            LureActive = false;
            CheddarLured = false;
            _events.Add(new PackEvent(EventKind.FinalPushStarted, DogId.Cocoa, -1, CoyotePosition));
        }

        private void AdvanceFinalPush(float deltaTime, Vector2 cheddarPosition, Vector2 cocoaPosition)
        {
            if (!FinalPushActive)
            {
                _finalRetryRemaining -= deltaTime;
                if (_finalRetryRemaining <= 0f) StartFinalPush();
                return;
            }
            // The pack charges at the pair.
            Vector2 middle = (cheddarPosition + cocoaPosition) * 0.5f;
            CoyotePosition = Vector2.MoveTowards(CoyotePosition, middle, _prowlSpeed * 0.35f * deltaTime);
            FinalPushRemaining = Mathf.Max(0f, FinalPushRemaining - deltaTime);
            if (FinalPushRemaining > 0f) return;
            Breaches++;
            FinalPushActive = false;
            _finalRetryRemaining = _finalPushRetrySeconds;
            CoyotePosition = DigSpot(ActiveGap) + _gapNormals[ActiveGap] * 6f;
            _events.Add(new PackEvent(EventKind.FinalPushBroke, DogId.Cocoa, -1, middle));
        }

        private Vector2 Tangent(int gap) => new Vector2(-_gapNormals[gap].y, _gapNormals[gap].x);

        private Vector2 ClampToYard(Vector2 p)
        {
            const float margin = 4f;
            return new Vector2(
                Mathf.Clamp(p.x, _yard.xMin + margin, _yard.xMax - margin),
                Mathf.Clamp(p.y, _yard.yMin + margin, _yard.yMax - margin));
        }

        /// <summary>
        /// Seeded weak-spot layout: <paramref name="count"/> spots walked along the left fence, the back
        /// fence and the right fence (the house side has no fence to dig under), a fair run apart, never
        /// at a corner and never against <paramref name="keepOut"/>. Normals point OUT of the yard.
        /// </summary>
        public static void ComputeGaps(Rect yard, Rect keepOut, int count, int seed, out Vector2[] gaps, out Vector2[] normals)
        {
            var random = new System.Random(seed ^ 0x5eed);
            float insetX = yard.width * 0.03f;
            float insetY = yard.height * 0.045f;
            float left = yard.xMin + insetX, right = yard.xMax - insetX;
            float top = yard.yMax - insetY, bottom = yard.yMin + yard.height * 0.18f;
            float sideLength = top - bottom;
            float topLength = right - left;
            float total = sideLength * 2f + topLength;
            const float cornerClear = 7f;

            Vector2 PointAt(float s, out Vector2 normal)
            {
                if (s < sideLength) { normal = Vector2.left; return new Vector2(left, bottom + s); }
                s -= sideLength;
                if (s < topLength) { normal = Vector2.up; return new Vector2(left + s, top); }
                s -= topLength;
                normal = Vector2.right;
                return new Vector2(right, top - s);
            }

            bool Usable(float s)
            {
                if (Mathf.Abs(s - sideLength) < cornerClear || Mathf.Abs(s - (sideLength + topLength)) < cornerClear) return false;
                Vector2 p = PointAt(s, out _);
                if (keepOut.width > 0f && keepOut.height > 0f &&
                    p.x > keepOut.xMin - 3f && p.x < keepOut.xMax + 3f && p.y > keepOut.yMin - 3f && p.y < keepOut.yMax + 3f)
                    return false;
                return true;
            }

            float span = Mathf.Lerp(38f, 48f, (float)random.NextDouble());
            float maxStart = Mathf.Max(0f, total - span * (count - 1) - 4f);
            float start = 4f + (float)random.NextDouble() * Mathf.Max(0f, maxStart - 4f);
            bool reverse = random.Next(2) == 1;
            gaps = new Vector2[count];
            normals = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float s = start + i * span;
                if (reverse) s = total - s;
                // Slide off corners / the keep-out region along the fence, in walking direction.
                float step = reverse ? -1f : 1f;
                for (int nudge = 0; nudge < 40 && !Usable(s); nudge++) s += step;
                s = Mathf.Clamp(s, 0f, total - 0.01f);
                gaps[i] = PointAt(s, out normals[i]);
            }
        }
    }
}
