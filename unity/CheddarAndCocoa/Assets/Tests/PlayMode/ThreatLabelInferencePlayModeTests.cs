using NUnit.Framework;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// A2.4: pins ThreatMotionArt.TryInfer against the exact SetActorState label strings the
    /// mission controllers send, not just synthetic keywords. Real acting gaps found by tracing
    /// every coyote/squirrel label through the inference table:
    /// - the fake-snack lure and the final "coyote retreats" defeat state both fell all the way
    ///   through to Patrol (the coyote's calmest read) instead of Threaten/Retreat;
    /// - the squirrel's taunt state reused the cowering Scared clip for what is actually a gleeful
    ///   successful escape;
    /// - BackyardRescue's "SQUIRREL GOT A WEENIE!" theft-success beat fell to Idle instead of the
    ///   grabby Steal clip its sibling "SQUIRREL STOLE A SNACK!" correctly gets.
    /// A fourth candidate turned out NOT to be a bug: the squirrel branch's "must literally say
    /// SQUIRREL or return false" guard looked like a bug (5 of SquirrelConspiracyMissionController's
    /// own 6 labels never say the word, so the guard was silently disabling that mission's squirrel
    /// motion almost entirely) - but FinalArtIntegrationPlayModeTests already pins that exact guard
    /// as intentional, so the shared squirrel actor can be repurposed as a non-squirrel marker
    /// (Coyotes Fence's dirt/weak-spot) without it idle-breathing like a squirrel. Fixed the real
    /// mission bug at its source instead: added "SQUIRREL " to those 5 label strings so they satisfy
    /// the existing (correct) contract, rather than weakening the guard for everyone.
    /// All fixes reuse clips that already have authored art - no new art needed.
    /// </summary>
    public sealed class ThreatLabelInferencePlayModeTests
    {
        [TestCase("COYOTE AT THE FENCE - BARK PRESSURE!", ThreatMotionArt.Clip.Threaten)]
        [TestCase("COYOTE GOING FOR THE FINAL PUSH - UNITED BARK!", ThreatMotionArt.Clip.Threaten)]
        [TestCase("COYOTE BREACH 2/3!", ThreatMotionArt.Clip.Threaten)]
        [TestCase("COYOTE DRIVEN BACK!", ThreatMotionArt.Clip.Retreat)]
        [TestCase("COYOTE BLOCKED - PARTNER FILLS DIRT!", ThreatMotionArt.Clip.Retreat)]
        [TestCase("FAKE SNACK BAIT - CHEDDAR, NO!", ThreatMotionArt.Clip.Threaten)]
        [TestCase("FAKE SNACK BAIT - IGNORE IT!", ThreatMotionArt.Clip.Threaten)]
        [TestCase("COYOTE RETREATS - YARD DEFENDED!", ThreatMotionArt.Clip.Retreat)]
        public void CoyoteLabels_MapToTheReadThatMatchesWhatIsHappening(string label, ThreatMotionArt.Clip expected)
        {
            Assert.IsTrue(ThreatMotionArt.TryInfer(label, ThreatMotionArt.Actor.Coyote, out var actor, out var clip));
            Assert.AreEqual(ThreatMotionArt.Actor.Coyote, actor);
            Assert.AreEqual(expected, clip, $"'{label}' should read as {expected}, not {clip}.");
        }

        [TestCase("SQUIRREL CONSPIRACY ROUTE 1", ThreatMotionArt.Clip.Run)]
        [TestCase("SQUIRREL HERDED - NEEDS COCOA CUTOFF!", ThreatMotionArt.Clip.Run)]
        [TestCase("SQUIRREL ROUTE 2 / CONTROLS 1/4", ThreatMotionArt.Clip.Run)]
        [TestCase("SQUIRREL TAUNT 1/3 - CUT OFF!", ThreatMotionArt.Clip.Run)]
        [TestCase("SQUIRREL STASH REVEALED - SNIFF + INTERACT!", ThreatMotionArt.Clip.Idle)]
        [TestCase("SQUIRREL CONSPIRACY CRACKED!", ThreatMotionArt.Clip.Run)]
        [TestCase("SQUIRREL DROPPED IT!", ThreatMotionArt.Clip.Scared)]
        [TestCase("SQUIRREL GOT A WEENIE!", ThreatMotionArt.Clip.Steal)]
        [TestCase("SQUIRREL STOLE A SNACK!", ThreatMotionArt.Clip.Steal)]
        public void SquirrelLabels_MapToTheReadThatMatchesWhatIsHappening(string label, ThreatMotionArt.Clip expected)
        {
            Assert.IsTrue(ThreatMotionArt.TryInfer(label, ThreatMotionArt.Actor.Squirrel, out var actor, out var clip));
            Assert.AreEqual(ThreatMotionArt.Actor.Squirrel, actor);
            Assert.AreEqual(expected, clip, $"'{label}' should read as {expected}, not {clip}.");
        }

        [TestCase("SKUNK GUARDING THE BIRD - STAY BACK!", ThreatMotionArt.Clip.Patrol)]
        [TestCase("SKUNK FACING CHEDDAR - COCOA, GO!", ThreatMotionArt.Clip.Patrol)]
        [TestCase("TAIL UP - SPRAY INCOMING!", ThreatMotionArt.Clip.Threaten)]
        [TestCase("SKUNK GIVES UP - PRIZE'S GONE!", ThreatMotionArt.Clip.Retreat)]
        public void SkunkLabels_MapToTheReadThatMatchesWhatIsHappening(string label, ThreatMotionArt.Clip expected)
        {
            // Couch test 2026-07-24: Skunk Blast Mayhem's shared PredatorObject had no Skunk actor
            // at all, so every one of these labels fell through TryInfer to the hardcoded Eagle
            // default and rendered a banking eagle instead of a skunk.
            Assert.IsTrue(ThreatMotionArt.TryInfer(label, ThreatMotionArt.Actor.Eagle, out var actor, out var clip));
            Assert.AreEqual(ThreatMotionArt.Actor.Skunk, actor);
            Assert.AreEqual(expected, clip, $"'{label}' should read as {expected}, not {clip}.");
        }

        [Test]
        public void RepurposedSquirrelActorMarkerLabels_StillFallBackInstead_OfIdleBreathingLikeASquirrel()
        {
            // Coyotes Fence reuses the shared squirrel actor as a dirt/weak-spot marker. This must
            // keep returning false (no inference) even after the SQUIRREL-conspiracy fixes above -
            // FinalArtIntegrationPlayModeTests.MissionThreatActors_UseAuthoredMotionAndMarkerFallbacks
            // already pins this exact behavior in the running scene.
            Assert.IsFalse(ThreatMotionArt.TryInfer("WEAK SPOT - FILL DIRT (NEEDS PARTNER BARK)",
                ThreatMotionArt.Actor.Squirrel, out _, out _));
            Assert.IsFalse(ThreatMotionArt.TryInfer("WEAK SPOT FILLED 1/3",
                ThreatMotionArt.Actor.Squirrel, out _, out _));
        }
    }
}
