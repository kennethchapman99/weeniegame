using NUnit.Framework;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Deterministic guards for the Burr Maze stealth beat: facing-direction + angle + distance
    /// vision-cone geometry, notice-dwell accrual/reset (hiding hard-resets it, leaving the cone soft-
    /// resets it), detection as a position-only reset (never a fail flag), asymmetric burr accrual and
    /// the caked speed/notice penalty, the partner-held burr-pick, bark-lure NOT resetting the
    /// barker's own dwell, and the independent twist patrol.
    /// </summary>
    public sealed class CoopBurrMazePuzzleUnitTests
    {
        private static CoopBurrMazePuzzle Make(
            float cheddarBrambleRate = 0.16f, float cocoaBrambleRate = 0.07f,
            float burrThreshold = 0.55f, float burrSpeedMultiplier = 0.6f, float burrNoticeMultiplier = 1.6f,
            float cheddarNoticeRate = 1.25f, float cocoaNoticeRate = 1f,
            float noticeDwellThreshold = 1.3f,
            float cheddarWarningFraction = 0.72f, float cocoaWarningFraction = 0.42f,
            float lureDurationSeconds = 3f,
            float burrPickRatePerSecond = 0.28f,
            float twistTriggerSeconds = 20f,
            int checkpointCount = 4)
        {
            var p = new CoopBurrMazePuzzle();
            p.Configure(cheddarBrambleRate, cocoaBrambleRate, burrThreshold, burrSpeedMultiplier, burrNoticeMultiplier,
                cheddarNoticeRate, cocoaNoticeRate, noticeDwellThreshold, cheddarWarningFraction, cocoaWarningFraction,
                lureDurationSeconds, burrPickRatePerSecond, twistTriggerSeconds, checkpointCount);
            return p;
        }

        private static CoopBurrMazePuzzle.DogExposure Clear() => new(false, false, false, false);
        private static CoopBurrMazePuzzle.DogExposure InPrimaryCone() => new(false, true, false, false);
        private static CoopBurrMazePuzzle.DogExposure Hidden() => new(false, false, false, true);
        private static CoopBurrMazePuzzle.DogExposure InBramble() => new(true, false, false, false);
        private static CoopBurrMazePuzzle.DogExposure InTwistCone() => new(false, false, true, false);

        [Test]
        public void StartState_NoBurrsNoDetectionAtFirstCheckpointPrimaryOnly()
        {
            var p = Make();
            Assert.AreEqual(0f, p.CheddarBurr);
            Assert.AreEqual(0f, p.CocoaBurr);
            Assert.AreEqual(0, p.Detections);
            Assert.AreEqual(0, p.CheckpointIndex);
            Assert.IsFalse(p.Cleared);
            Assert.IsFalse(p.TwistPatrolActive);
            Assert.IsFalse(p.LureActive);
            Assert.IsNull(p.PickTarget);
            Assert.AreEqual(0f, p.ElapsedSeconds);
        }

        [Test]
        public void IsInCone_InsideAngleAndDistance_IsTrue()
        {
            Assert.IsTrue(CoopBurrMazePuzzle.IsInCone(
                new UnityEngine.Vector2(0f, 0f), UnityEngine.Vector2.right,
                new UnityEngine.Vector2(3f, 0.5f), 80f, 5f));
        }

        [Test]
        public void IsInCone_OutsideMaxDistance_IsFalse()
        {
            Assert.IsFalse(CoopBurrMazePuzzle.IsInCone(
                new UnityEngine.Vector2(0f, 0f), UnityEngine.Vector2.right,
                new UnityEngine.Vector2(10f, 0f), 80f, 5f));
        }

        [Test]
        public void IsInCone_OutsideAngle_IsFalse()
        {
            // Directly behind the patrol - well outside an 80-degree cone.
            Assert.IsFalse(CoopBurrMazePuzzle.IsInCone(
                new UnityEngine.Vector2(0f, 0f), UnityEngine.Vector2.right,
                new UnityEngine.Vector2(-3f, 0f), 80f, 5f));
        }

        [Test]
        public void ConeNotice_InCone_AccumulatesDwellTowardDetection()
        {
            var p = Make(cheddarNoticeRate: 1f, noticeDwellThreshold: 1.0f);
            p.Advance(0.9f, InPrimaryCone(), Clear());
            Assert.AreEqual(0, p.Detections, "Not yet at the threshold.");
            p.Advance(0.2f, InPrimaryCone(), Clear());
            Assert.AreEqual(1, p.Detections, "Crossing the dwell threshold should trigger a detection.");
        }

        [Test]
        public void ConeNotice_LeavingConeResetsDwell()
        {
            var p = Make(cheddarNoticeRate: 1f, noticeDwellThreshold: 1.0f, cheddarWarningFraction: 0.5f);
            p.Advance(0.8f, InPrimaryCone(), Clear());
            Assert.IsTrue(p.IsAboutToBeNoticed(DogId.Cheddar));

            p.Advance(0.05f, Clear(), Clear()); // leaves the cone
            Assert.IsFalse(p.IsAboutToBeNoticed(DogId.Cheddar), "Leaving the cone should reset the dwell.");

            p.Advance(0.8f, InPrimaryCone(), Clear());
            Assert.AreEqual(0, p.Detections, "A reset dwell needs the full threshold again, not a top-up.");
        }

        [Test]
        public void ConeNotice_NeverInConeNeverAccumulates()
        {
            var p = Make(noticeDwellThreshold: 1.0f);
            for (int i = 0; i < 20; i++) p.Advance(1f, Clear(), Clear());
            Assert.AreEqual(0, p.Detections);
            Assert.IsFalse(p.IsAboutToBeNoticed(DogId.Cheddar));
        }

        [Test]
        public void HidingSpot_ResetsInProgressDwellEvenMidAccumulation()
        {
            var p = Make(cheddarNoticeRate: 1f, noticeDwellThreshold: 1.0f, cheddarWarningFraction: 0.5f);
            p.Advance(0.6f, InPrimaryCone(), Clear());
            Assert.IsTrue(p.IsAboutToBeNoticed(DogId.Cheddar), "Mid-accumulation, past the warning fraction.");

            p.Advance(0.01f, Hidden(), Clear()); // ducks into a bush
            Assert.IsFalse(p.IsAboutToBeNoticed(DogId.Cheddar), "Hiding must hard-reset the dwell to zero.");
        }

        [Test]
        public void Detection_CrossingThresholdResetsBothDogsDwell_NotJustTheDetectedOne()
        {
            // Cocoa's slower notice rate keeps her own dwell short of full detection (0.65 of a 1.0
            // threshold) but comfortably past her own warning fraction, so if the reset only applied
            // to Cheddar (the dog who actually tripped it), her dwell would still read "about to be
            // noticed" afterward.
            var p = Make(cheddarNoticeRate: 1f, cocoaNoticeRate: 0.5f, noticeDwellThreshold: 1.0f, cocoaWarningFraction: 0.15f);
            var cocoaPartial = new CoopBurrMazePuzzle.DogExposure(false, true, false, false);
            p.Advance(0.4f, InPrimaryCone(), cocoaPartial);
            Assert.IsTrue(p.IsAboutToBeNoticed(DogId.Cocoa));

            p.Advance(0.9f, InPrimaryCone(), cocoaPartial); // Cheddar alone crosses the threshold and triggers detection
            Assert.AreEqual(1, p.Detections);
            Assert.IsFalse(p.IsAboutToBeNoticed(DogId.Cocoa), "A detection resets BOTH dogs' dwell, not only the one who tripped it.");
        }

        [Test]
        public void Detection_DoesNotAffectCheckpointProgressOrClearedState()
        {
            var p = Make(cheddarNoticeRate: 1f, noticeDwellThreshold: 1.0f);
            p.AdvanceCheckpoint();
            Assert.AreEqual(1, p.CheckpointIndex);

            p.Advance(1.1f, InPrimaryCone(), Clear());
            Assert.AreEqual(1, p.Detections);
            Assert.AreEqual(1, p.CheckpointIndex, "A detection costs position, never banked checkpoint progress.");
            Assert.IsFalse(p.Cleared, "A detection is a stealth-retry loop, never a fail/clear flag.");
        }

        [Test]
        public void BurrAccrual_InBramble_IncreasesTheCorrectDogsMeter()
        {
            var p = Make(cheddarBrambleRate: 0.1f, cocoaBrambleRate: 0.1f);
            p.Advance(2f, InBramble(), Clear());
            Assert.Greater(p.CheddarBurr, 0f);
            Assert.AreEqual(0f, p.CocoaBurr, "Cocoa was never exposed to bramble this tick.");
        }

        [Test]
        public void BurrAccrual_CheddarRateIsMeasurablyHigherThanCocoaOverSameExposure()
        {
            var p = Make(cheddarBrambleRate: 0.16f, cocoaBrambleRate: 0.07f);
            p.Advance(2f, InBramble(), InBramble());
            Assert.Greater(p.CheddarBurr, p.CocoaBurr,
                "Cheddar's reckless bramble charges should cake him faster than Cocoa over identical exposure.");
            Assert.Greater(p.CheddarBurr, p.CocoaBurr * 1.5f, "The gap should be substantial, not a rounding difference.");
        }

        [Test]
        public void BurrPenalty_AboveThreshold_SlowsMovementMultiplier()
        {
            var p = Make(burrThreshold: 0.5f, burrSpeedMultiplier: 0.6f, cheddarBrambleRate: 1f);
            Assert.AreEqual(1f, p.SpeedMultiplierFor(DogId.Cheddar));
            p.Advance(1f, InBramble(), Clear());
            Assert.IsTrue(p.IsBurrCaked(DogId.Cheddar));
            Assert.AreEqual(0.6f, p.SpeedMultiplierFor(DogId.Cheddar), 0.001f, "A caked dog must actually read as slower, not just a bigger meter number.");
        }

        [Test]
        public void BurrPenalty_AboveThreshold_NoticeDwellAccumulatesFaster()
        {
            // Detection timing proof: with an identical threshold and base notice rate, a burr-caked
            // dog should cross the detection threshold in fewer seconds of cone exposure than a clean
            // dog - a real widened-notice effect, not just a bigger meter number.
            var caked = Make(burrThreshold: 0.2f, burrNoticeMultiplier: 2f, cheddarBrambleRate: 1f, cheddarNoticeRate: 1f, noticeDwellThreshold: 3f);
            caked.Advance(1f, InBramble(), Clear()); // cakes Cheddar past the low threshold first
            Assert.IsTrue(caked.IsBurrCaked(DogId.Cheddar));

            var clean = Make(burrThreshold: 0.99f, cheddarNoticeRate: 1f, noticeDwellThreshold: 3f);

            for (int i = 0; i < 60 && caked.Detections == 0; i++) caked.Advance(0.1f, InPrimaryCone(), Clear());
            for (int i = 0; i < 60 && clean.Detections == 0; i++) clean.Advance(0.1f, InPrimaryCone(), Clear());

            Assert.AreEqual(1, caked.Detections);
            Assert.AreEqual(1, clean.Detections);
            Assert.Less(caked.ElapsedSeconds, clean.ElapsedSeconds,
                "A burr-caked dog's widened notice-dwell should reach detection sooner than a clean dog under identical exposure.");
        }

        [Test]
        public void BurrPick_ReducesTargetMeterGraduallyWhileSet()
        {
            var p = Make(cheddarBrambleRate: 1f, burrPickRatePerSecond: 0.25f);
            p.Advance(1f, InBramble(), Clear());
            float before = p.CheddarBurr;
            Assert.Greater(before, 0f);

            p.SetPicking(DogId.Cheddar);
            p.Advance(1f, Clear(), Clear());
            Assert.Less(p.CheddarBurr, before, "An active pick should reduce the target's burr meter over time.");
        }

        [Test]
        public void BurrPick_ClearsItselfOnceTargetReachesZero()
        {
            var p = Make(cheddarBrambleRate: 1f, burrPickRatePerSecond: 2f);
            p.Advance(0.3f, InBramble(), Clear());
            p.SetPicking(DogId.Cheddar);
            p.Advance(1f, Clear(), Clear());
            Assert.AreEqual(0f, p.CheddarBurr);
            Assert.IsNull(p.PickTarget, "The pick should auto-clear once the target is fully clean.");
        }

        [Test]
        public void BurrPick_ADetectionInterruptsAnInProgressPick()
        {
            var p = Make(cheddarBrambleRate: 1f, noticeDwellThreshold: 1f, cheddarNoticeRate: 1f);
            p.Advance(0.5f, InBramble(), Clear());
            p.SetPicking(DogId.Cheddar);
            Assert.IsNotNull(p.PickTarget);

            p.ForceDetection();
            Assert.IsNull(p.PickTarget, "A patrol detection should interrupt an in-progress burr-pick.");
        }

        [Test]
        public void BarkLure_ActivatesForConfiguredDuration()
        {
            var p = Make(lureDurationSeconds: 2f);
            p.SetLure(DogId.Cheddar);
            Assert.IsTrue(p.LureActive);
            Assert.AreEqual(DogId.Cheddar, p.LureDog);

            p.Advance(1.9f, Clear(), Clear());
            Assert.IsTrue(p.LureActive, "The lure should still be active just under its duration.");
            p.Advance(0.2f, Clear(), Clear());
            Assert.IsFalse(p.LureActive, "The lure should expire once its duration elapses.");
        }

        [Test]
        public void BarkLure_DoesNotResetTheBarkersOwnNoticeDwell()
        {
            var p = Make(cheddarNoticeRate: 1f, noticeDwellThreshold: 1f, lureDurationSeconds: 5f);
            p.Advance(0.5f, InPrimaryCone(), Clear()); // Cheddar accumulates dwell first
            p.SetLure(DogId.Cheddar); // Cheddar barks - breaks his own cover, but is not a hiding action
            p.Advance(0.5f, InPrimaryCone(), Clear()); // still in the cone, dwell keeps climbing
            Assert.AreEqual(1, p.Detections,
                "Barking must not reset the barker's own dwell - it should still reach detection on schedule.");
        }

        [Test]
        public void TwistPatrol_ActivatesAtTheConfiguredTriggerTime()
        {
            var p = Make(twistTriggerSeconds: 5f);
            p.Advance(4.9f, Clear(), Clear());
            Assert.IsFalse(p.TwistPatrolActive);
            p.Advance(0.2f, Clear(), Clear());
            Assert.IsTrue(p.TwistPatrolActive);
        }

        [Test]
        public void TwistPatrol_ExposureIsIgnoredBeforeActivation()
        {
            var p = Make(twistTriggerSeconds: 100f, cheddarNoticeRate: 1f, noticeDwellThreshold: 1f);
            for (int i = 0; i < 20; i++) p.Advance(0.1f, InTwistCone(), Clear());
            Assert.AreEqual(0, p.Detections, "The twist patrol's cone shouldn't count before it activates.");
        }

        [Test]
        public void TwistPatrol_DetectsIndependentlyOfThePrimaryPatrol()
        {
            var p = Make(twistTriggerSeconds: 0f, cheddarNoticeRate: 1f, noticeDwellThreshold: 1f);
            p.ForceTwistPatrolActive();
            // Cheddar is never in the primary cone here - only the twist patrol can catch him.
            p.Advance(1.1f, InTwistCone(), Clear());
            Assert.AreEqual(1, p.Detections, "The twist patrol should be able to independently detect a dog the primary patrol wasn't watching.");
        }

        [Test]
        public void ClearPath_ReachingTheFinalCheckpointClearsTheMission()
        {
            var p = Make(checkpointCount: 4);
            Assert.IsTrue(p.AdvanceCheckpoint());
            Assert.IsFalse(p.Cleared);
            Assert.IsTrue(p.AdvanceCheckpoint());
            Assert.IsFalse(p.Cleared);
            Assert.IsTrue(p.AdvanceCheckpoint());
            Assert.IsTrue(p.Cleared, "Reaching the last of 4 checkpoints (3 advances) should clear the mission.");
            Assert.IsFalse(p.AdvanceCheckpoint(), "No further advances once cleared.");
        }

        [Test]
        public void Reset_ClearsEverythingBackToStartState()
        {
            var p = Make();
            p.Advance(2f, InBramble(), InBramble());
            p.AdvanceCheckpoint();
            p.SetLure(DogId.Cheddar);
            p.SetPicking(DogId.Cocoa);
            p.ForceDetection();
            p.ForceTwistPatrolActive();

            p.Reset();
            Assert.AreEqual(0f, p.CheddarBurr);
            Assert.AreEqual(0f, p.CocoaBurr);
            Assert.AreEqual(0, p.Detections);
            Assert.AreEqual(0, p.CheckpointIndex);
            Assert.IsFalse(p.Cleared);
            Assert.IsFalse(p.TwistPatrolActive);
            Assert.IsFalse(p.LureActive);
            Assert.IsNull(p.LureDog);
            Assert.IsNull(p.PickTarget);
            Assert.AreEqual(0f, p.ElapsedSeconds);
        }
    }
}
