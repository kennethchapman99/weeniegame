using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the Burr Maze stealth beat: a patrolling cat sweeps a
    /// facing-direction vision cone across a hedge maze, and getting caught in it for too long is a
    /// stealth-retry loop, not a hard fail - both dogs are swept back to the last safe checkpoint
    /// with zero meter penalty (a third distinct failure pattern in this roster, alongside Skunk
    /// Blast Mayhem's fail-forward de-skunk detour and Tick Invasion's hard fail).
    ///
    /// Cheddar and Cocoa are mechanically asymmetric, not just reskinned: Cheddar's reckless bramble
    /// charges pick up burrs much faster (<see cref="CheddarBurr"/> vs <see cref="CocoaBurr"/>) and
    /// his baseline notice-dwell accrues faster too (louder, easier to spot); Cocoa's veteran read
    /// gives her an earlier "about to be noticed" warning window (<see cref="IsAboutToBeNoticed"/>)
    /// and she accrues burrs and notice-dwell more slowly.
    ///
    /// A dog whose burr meter crosses <see cref="BurrThreshold"/> is "caked": their effective
    /// notice-dwell accrual widens (<see cref="Advance"/>) and their movement should be slowed by
    /// <see cref="SpeedMultiplierFor"/> - a real status effect, never a fail condition on its own.
    /// Burrs only come off via a partner-held pick (<see cref="SetPicking"/> + <see cref="Advance"/>,
    /// mirroring <see cref="CoopHoldReleasePuzzle"/>'s toggle-on/auto-release shape): gradual while
    /// held, and the controller drops the hold the instant either dog stops being stationary/exposed
    /// or a patrol detection fires.
    ///
    /// Ducking into a hiding spot hard-resets any in-progress notice-dwell for that dog
    /// (<see cref="Advance"/>'s <c>Hidden</c> input), the core MGS-style tool. Barking while NOT
    /// hidden redirects the patrol's attention for a few seconds (<see cref="SetLure"/>) at the cost
    /// of that dog's own exposure - it deliberately does NOT reset the barking dog's own dwell.
    ///
    /// A second, faster twist patrol activates partway through the run (<see cref="TwistPatrolActive"/>)
    /// and is tracked completely independently of the primary patrol's cone/dwell state, so it can
    /// catch a dog the primary patrol wasn't watching.
    ///
    /// Pure logic: a mission drives real-world cone/bramble/hide checks and time; tests drive all of
    /// it deterministically. <see cref="IsInCone"/> is the reusable facing-direction + angle +
    /// distance geometry check the mission's own patrol actors are checked against each tick.
    /// </summary>
    public sealed class CoopBurrMazePuzzle
    {
        public const float MaxBurr = 1f;

        /// <summary>Per-dog per-tick real-world exposure inputs, computed by the mission from live positions.</summary>
        public readonly struct DogExposure
        {
            public readonly bool InBramble;
            public readonly bool InPrimaryCone;
            public readonly bool InTwistCone;
            public readonly bool Hidden;

            public DogExposure(bool inBramble, bool inPrimaryCone, bool inTwistCone, bool hidden)
            {
                InBramble = inBramble;
                InPrimaryCone = inPrimaryCone;
                InTwistCone = inTwistCone;
                Hidden = hidden;
            }
        }

        private float _cheddarBrambleRate = 0.16f;
        private float _cocoaBrambleRate = 0.07f;
        private float _burrThreshold = 0.55f;
        private float _burrSpeedMultiplier = 0.6f;
        private float _burrNoticeMultiplier = 1.6f;
        private float _cheddarNoticeRate = 1.25f;
        private float _cocoaNoticeRate = 1f;
        private float _noticeDwellThreshold = 1.3f;
        private float _cheddarWarningFraction = 0.72f;
        private float _cocoaWarningFraction = 0.42f;
        private float _lureDurationSeconds = 3f;
        private float _burrPickRatePerSecond = 0.28f;
        private float _twistTriggerSeconds = 20f;
        private int _checkpointCount = 4;

        private float _cheddarPrimaryDwell;
        private float _cocoaPrimaryDwell;
        private float _cheddarTwistDwell;
        private float _cocoaTwistDwell;
        private float _lureUntil;

        public float ElapsedSeconds { get; private set; }
        public float CheddarBurr { get; private set; }
        public float CocoaBurr { get; private set; }
        public bool TwistPatrolActive { get; private set; }
        public int Detections { get; private set; }
        public int CheckpointIndex { get; private set; }
        public bool Cleared { get; private set; }
        public bool LureActive { get; private set; }
        public DogId? LureDog { get; private set; }
        public DogId? PickTarget { get; private set; }

        public int CheckpointCount => _checkpointCount;
        public float BurrThreshold => _burrThreshold;
        public float NoticeDwellThreshold => _noticeDwellThreshold;
        public float TwistTriggerSeconds => _twistTriggerSeconds;
        /// <summary>Mistakes tally for the shared runtime snapshot - each detection counts once.</summary>
        public int Mistakes => Detections;

        public float BurrOf(DogId dog) => dog == DogId.Cheddar ? CheddarBurr : CocoaBurr;
        public bool IsBurrCaked(DogId dog) => BurrOf(dog) >= _burrThreshold;
        public float SpeedMultiplierFor(DogId dog) => IsBurrCaked(dog) ? _burrSpeedMultiplier : 1f;

        public void Configure(
            float cheddarBrambleRate, float cocoaBrambleRate,
            float burrThreshold, float burrSpeedMultiplier, float burrNoticeMultiplier,
            float cheddarNoticeRate, float cocoaNoticeRate,
            float noticeDwellThreshold,
            float cheddarWarningFraction, float cocoaWarningFraction,
            float lureDurationSeconds,
            float burrPickRatePerSecond,
            float twistTriggerSeconds,
            int checkpointCount)
        {
            _cheddarBrambleRate = cheddarBrambleRate <= 0f ? 0.12f : cheddarBrambleRate;
            _cocoaBrambleRate = cocoaBrambleRate <= 0f ? 0.06f : cocoaBrambleRate;
            _burrThreshold = burrThreshold <= 0f ? 0.5f : burrThreshold;
            _burrSpeedMultiplier = burrSpeedMultiplier <= 0f || burrSpeedMultiplier >= 1f ? 0.6f : burrSpeedMultiplier;
            _burrNoticeMultiplier = burrNoticeMultiplier <= 1f ? 1.5f : burrNoticeMultiplier;
            _cheddarNoticeRate = cheddarNoticeRate <= 0f ? 1f : cheddarNoticeRate;
            _cocoaNoticeRate = cocoaNoticeRate <= 0f ? 1f : cocoaNoticeRate;
            _noticeDwellThreshold = noticeDwellThreshold <= 0f ? 1f : noticeDwellThreshold;
            _cheddarWarningFraction = Mathf.Clamp01(cheddarWarningFraction <= 0f ? 0.7f : cheddarWarningFraction);
            _cocoaWarningFraction = Mathf.Clamp01(cocoaWarningFraction <= 0f ? 0.4f : cocoaWarningFraction);
            _lureDurationSeconds = lureDurationSeconds <= 0f ? 2f : lureDurationSeconds;
            _burrPickRatePerSecond = burrPickRatePerSecond <= 0f ? 0.2f : burrPickRatePerSecond;
            _twistTriggerSeconds = twistTriggerSeconds < 0f ? 0f : twistTriggerSeconds;
            _checkpointCount = checkpointCount < 2 ? 2 : checkpointCount;
            Reset();
        }

        /// <summary>
        /// Pure facing-direction + angle + distance vision-cone geometry check, exposed statically so
        /// both the mission (real patrol/dog positions) and tests (synthetic positions) can drive it.
        /// </summary>
        public static bool IsInCone(Vector2 patrolPosition, Vector2 patrolFacing, Vector2 targetPosition,
            float coneAngleDegrees, float maxDistance)
        {
            if (maxDistance <= 0f) return false;
            Vector2 toTarget = targetPosition - patrolPosition;
            float distance = toTarget.magnitude;
            if (distance > maxDistance) return false;
            if (distance < 0.0001f) return true;
            Vector2 facing = patrolFacing.sqrMagnitude > 0.0001f ? patrolFacing.normalized : Vector2.up;
            float angle = Vector2.Angle(facing, toTarget / distance);
            return angle <= coneAngleDegrees * 0.5f;
        }

        /// <summary>Runs burr accrual, notice-dwell accrual/reset, the twist-patrol trigger clock,
        /// the lure timer, and the in-progress burr-pick, for one tick.</summary>
        public void Advance(float dt, DogExposure cheddar, DogExposure cocoa)
        {
            if (Cleared || dt <= 0f) return;

            ElapsedSeconds += dt;
            if (!TwistPatrolActive && ElapsedSeconds >= _twistTriggerSeconds) TwistPatrolActive = true;

            if (cheddar.InBramble) CheddarBurr = Clamp01(CheddarBurr + _cheddarBrambleRate * dt);
            if (cocoa.InBramble) CocoaBurr = Clamp01(CocoaBurr + _cocoaBrambleRate * dt);

            AdvanceDwell(ref _cheddarPrimaryDwell, cheddar.Hidden, cheddar.InPrimaryCone, _cheddarNoticeRate, IsBurrCaked(DogId.Cheddar), dt);
            AdvanceDwell(ref _cocoaPrimaryDwell, cocoa.Hidden, cocoa.InPrimaryCone, _cocoaNoticeRate, IsBurrCaked(DogId.Cocoa), dt);
            AdvanceDwell(ref _cheddarTwistDwell, cheddar.Hidden, TwistPatrolActive && cheddar.InTwistCone, _cheddarNoticeRate, IsBurrCaked(DogId.Cheddar), dt);
            AdvanceDwell(ref _cocoaTwistDwell, cocoa.Hidden, TwistPatrolActive && cocoa.InTwistCone, _cocoaNoticeRate, IsBurrCaked(DogId.Cocoa), dt);

            if (LureActive && ElapsedSeconds >= _lureUntil)
            {
                LureActive = false;
                LureDog = null;
            }

            bool detected = _cheddarPrimaryDwell >= _noticeDwellThreshold || _cheddarTwistDwell >= _noticeDwellThreshold ||
                            _cocoaPrimaryDwell >= _noticeDwellThreshold || _cocoaTwistDwell >= _noticeDwellThreshold;
            if (detected) RegisterDetection();

            if (PickTarget.HasValue)
            {
                DogId target = PickTarget.Value;
                SetBurr(target, BurrOf(target) - _burrPickRatePerSecond * dt);
                if (BurrOf(target) <= 0f) PickTarget = null;
            }
        }

        /// <summary>Barker redirects the patrol's attention for a few seconds. Does NOT reset the
        /// barker's own notice-dwell - barking is not a hiding action.</summary>
        public void SetLure(DogId barker)
        {
            if (Cleared) return;
            LureActive = true;
            LureDog = barker;
            _lureUntil = ElapsedSeconds + _lureDurationSeconds;
        }

        /// <summary>Partner-held burr pick, gradual while set, mirroring <see cref="CoopHoldReleasePuzzle"/>'s
        /// toggle-on/auto-release shape: the mission clears this back to null the instant its own
        /// real-world hold conditions (range/stationary/exposed) break.</summary>
        public void SetPicking(DogId? target) => PickTarget = target;

        /// <summary>Both dogs simultaneously clear the next checkpoint node - progress banks
        /// permanently (unlike a detection, which only costs position, never progress).</summary>
        public bool AdvanceCheckpoint()
        {
            if (Cleared || CheckpointIndex >= _checkpointCount - 1) return false;
            CheckpointIndex++;
            if (CheckpointIndex >= _checkpointCount - 1) Cleared = true;
            return true;
        }

        /// <summary>
        /// A dog's about-to-be-noticed telegraph: past its warning fraction of the detection
        /// threshold but not yet detected. Cocoa's fraction is lower than Cheddar's, giving her a
        /// readably earlier heads-up window per her veteran-read trait.
        /// </summary>
        public bool IsAboutToBeNoticed(DogId dog)
        {
            float warningAt = _noticeDwellThreshold * (dog == DogId.Cheddar ? _cheddarWarningFraction : _cocoaWarningFraction);
            float maxDwell = dog == DogId.Cheddar
                ? Mathf.Max(_cheddarPrimaryDwell, _cheddarTwistDwell)
                : Mathf.Max(_cocoaPrimaryDwell, _cocoaTwistDwell);
            return maxDwell >= warningAt && maxDwell < _noticeDwellThreshold;
        }

        /// <summary>Test/mission hook: force a detection event without grinding dwell timers up.</summary>
        public void ForceDetection() => RegisterDetection();

        /// <summary>Test hook: skip straight to the twist patrol being active.</summary>
        public void ForceTwistPatrolActive() => TwistPatrolActive = true;

        /// <summary>Test hook: jump straight to a checkpoint index instead of grinding real positions.</summary>
        public void ForceCheckpoint(int index)
        {
            if (Cleared) return;
            CheckpointIndex = Mathf.Clamp(index, 0, _checkpointCount - 1);
            if (CheckpointIndex >= _checkpointCount - 1) Cleared = true;
        }

        public void Reset()
        {
            ElapsedSeconds = 0f;
            CheddarBurr = 0f;
            CocoaBurr = 0f;
            _cheddarPrimaryDwell = 0f;
            _cocoaPrimaryDwell = 0f;
            _cheddarTwistDwell = 0f;
            _cocoaTwistDwell = 0f;
            TwistPatrolActive = false;
            Detections = 0;
            CheckpointIndex = 0;
            Cleared = false;
            LureActive = false;
            LureDog = null;
            _lureUntil = 0f;
            PickTarget = null;
        }

        private void RegisterDetection()
        {
            if (Cleared) return;
            Detections++;
            _cheddarPrimaryDwell = 0f;
            _cocoaPrimaryDwell = 0f;
            _cheddarTwistDwell = 0f;
            _cocoaTwistDwell = 0f;
            PickTarget = null; // a detection interrupts any in-progress burr-pick
        }

        private void AdvanceDwell(ref float dwell, bool hidden, bool inCone, float noticeRate, bool caked, float dt)
        {
            if (hidden || !inCone) { dwell = 0f; return; }
            dwell += noticeRate * (caked ? _burrNoticeMultiplier : 1f) * dt;
        }

        private void SetBurr(DogId dog, float value)
        {
            if (dog == DogId.Cheddar) CheddarBurr = Clamp01(value);
            else CocoaBurr = Clamp01(value);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
