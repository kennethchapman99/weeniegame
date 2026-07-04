using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Predator animation fidelity pass: the coyote and squirrel stop sliding around as static
    /// cutouts between frame swaps (gait, lunge, back-pedal, hop, snatch, tremble), the eagle's
    /// lift is synced to its wing frames, and all threat strips crossfade between frames instead
    /// of snapping. Pose math is pure; the crossfade wiring runs in the real ArenaScene.
    /// </summary>
    public sealed class ThreatMotionFidelityPlayModeTests
    {
        private static readonly (ThreatMotionArt.Actor actor, ThreatMotionArt.Clip clip)[] AnimatedPairs =
        {
            (ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Idle),
            (ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Run),
            (ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Steal),
            (ThreatMotionArt.Actor.Squirrel, ThreatMotionArt.Clip.Scared),
            (ThreatMotionArt.Actor.Eagle, ThreatMotionArt.Clip.Sweep),
            (ThreatMotionArt.Actor.Eagle, ThreatMotionArt.Clip.Attack),
            (ThreatMotionArt.Actor.Coyote, ThreatMotionArt.Clip.Patrol),
            (ThreatMotionArt.Actor.Coyote, ThreatMotionArt.Clip.Threaten),
            (ThreatMotionArt.Actor.Coyote, ThreatMotionArt.Clip.Retreat),
        };

        // ---- Pure pose math ----

        [Test]
        public void CoyotePoses_GaitLungeAndRetreatReadInFacingDirection()
        {
            var gaitTop = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Coyote,
                ThreatMotionArt.Clip.Patrol, 0.125f, mirrored: false);
            Assert.Greater(gaitTop.Offset.y, 0.02f,
                "Patrol must bounce on its footfalls instead of sliding flat.");
            var gaitContact = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Coyote,
                ThreatMotionArt.Clip.Patrol, 0f, mirrored: false);
            Assert.AreEqual(0f, gaitContact.Offset.y, 0.001f,
                "Footfall contact keeps the coyote grounded at the cycle start.");

            var lungeEast = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Coyote,
                ThreatMotionArt.Clip.Threaten, 0.25f, mirrored: false);
            var lungeWest = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Coyote,
                ThreatMotionArt.Clip.Threaten, 0.25f, mirrored: true);
            Assert.Greater(lungeEast.Offset.x, 0.05f, "The threaten snap must push toward facing.");
            Assert.Less(lungeWest.Offset.x, -0.05f, "A mirrored coyote must lunge the other way.");
            Assert.Less(lungeEast.RotationDegrees, -2f,
                "The east-facing lunge leans the nose forward (clockwise).");
            Assert.Greater(lungeEast.Scale.x, 1.01f, "The lunge stretches along the ground.");

            var coil = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Coyote,
                ThreatMotionArt.Clip.Threaten, 0.75f, mirrored: false);
            Assert.Less(coil.Offset.x, 0f, "Between lunges the coyote coils back for the next snap.");
            Assert.Less(coil.Offset.y, 0f, "The coil crouches down.");

            var retreatEast = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Coyote,
                ThreatMotionArt.Clip.Retreat, 0.25f, mirrored: false);
            var retreatWest = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Coyote,
                ThreatMotionArt.Clip.Retreat, 0.25f, mirrored: true);
            Assert.Greater(retreatEast.RotationDegrees, 3f,
                "Driven back, the east-facing coyote leans away from the dogs.");
            Assert.Less(retreatWest.RotationDegrees, -3f, "The mirrored retreat leans the other way.");
            Assert.Less(retreatEast.Offset.x, 0f, "Retreat shifts the weight onto the haunches.");
        }

        [Test]
        public void SquirrelPoses_HopSnatchAndTrembleAnimate()
        {
            var hopAir = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Squirrel,
                ThreatMotionArt.Clip.Run, 0.125f, mirrored: false);
            Assert.Greater(hopAir.Offset.y, 0.04f, "The run is a bounding hop, not a flat slide.");
            Assert.Less(hopAir.RotationDegrees, -2f, "Airborne, the squirrel leans into its travel.");
            var hopGround = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Squirrel,
                ThreatMotionArt.Clip.Run, 0.5f, mirrored: false);
            Assert.AreEqual(0f, hopGround.Offset.y, 0.001f, "Each hop lands back on the ground.");

            var grabDown = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Squirrel,
                ThreatMotionArt.Clip.Steal, 0.125f, mirrored: false);
            Assert.Less(grabDown.Offset.y, -0.01f, "Stealing ducks the head down toward the loot.");
            Assert.AreEqual(1f, grabDown.Scale.x, 0.001f,
                "The steal pose stays uniform so the silhouette never reads deformed mid-heist.");

            var trembleLeft = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Squirrel,
                ThreatMotionArt.Clip.Scared, 1f / 24f, mirrored: false);
            var trembleRight = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Squirrel,
                ThreatMotionArt.Clip.Scared, 3f / 24f, mirrored: false);
            Assert.Greater(trembleLeft.Offset.x, 0.005f, "The scared shiver swings side to side.");
            Assert.Less(trembleRight.Offset.x, -0.005f, "The shiver crosses back within the cycle.");
            Assert.Less(trembleLeft.Offset.y, -0.02f, "Scared cowers low the whole time.");
        }

        [Test]
        public void EaglePoses_WingSyncedLiftAndVolumePreservingDive()
        {
            var upstroke = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Eagle,
                ThreatMotionArt.Clip.Sweep, 0.25f, mirrored: false);
            var downstroke = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Eagle,
                ThreatMotionArt.Clip.Sweep, 0.75f, mirrored: false);
            Assert.AreEqual(0.07f, upstroke.Offset.y, 0.005f,
                "The sweep lift peaks with the flap cycle instead of on its own clock.");
            Assert.AreEqual(-0.07f, downstroke.Offset.y, 0.005f,
                "The lift falls through the second half of the flap cycle.");
            Assert.AreEqual(upstroke.Scale.x, upstroke.Scale.y, 0.001f,
                "Sweep scale stays uniform; depth breathing is layered on by the animator.");

            var strike = ThreatMotionPose.Evaluate(ThreatMotionArt.Actor.Eagle,
                ThreatMotionArt.Clip.Attack, 0.25f, mirrored: false);
            Assert.Less(strike.Offset.y, -0.05f, "The talon strike drops toward the target.");
            Assert.Greater(strike.Scale.y, 1.02f, "The dive stretches the body vertically.");
            Assert.AreEqual(1f, strike.Scale.x * strike.Scale.y, 0.001f,
                "Dive stretch preserves volume so the eagle never reads squashed.");
        }

        [Test]
        public void AllPoses_StayBoundedAndVolumePreserving()
        {
            foreach (var (actor, clip) in AnimatedPairs)
            {
                for (int step = 0; step < 32; step++)
                {
                    float phase = step / 32f;
                    foreach (bool mirrored in new[] { false, true })
                    {
                        var pose = ThreatMotionPose.Evaluate(actor, clip, phase, mirrored);
                        Assert.LessOrEqual(pose.Offset.magnitude, ThreatMotionPose.MaxOffset,
                            $"{actor}/{clip} offset must stay near the actor at phase {phase}.");
                        Assert.LessOrEqual(Mathf.Abs(pose.RotationDegrees),
                            ThreatMotionPose.MaxRotationDegrees,
                            $"{actor}/{clip} rotation must stay a lean, not a spin.");
                        Assert.LessOrEqual(Mathf.Max(pose.Scale.x, pose.Scale.y),
                            ThreatMotionPose.MaxStretch,
                            $"{actor}/{clip} stretch must stay subtle at phase {phase}.");
                        Assert.AreEqual(1f, pose.Scale.x * pose.Scale.y, 0.01f,
                            $"{actor}/{clip} stretch must preserve volume (no half-height skew bug).");
                    }
                }
            }
        }

        [Test]
        public void CrossfadeAlpha_EasesInAndStaysClamped()
        {
            Assert.AreEqual(0f, ThreatReadabilityAnimator.CrossfadeAlpha(0f), 0.001f);
            Assert.AreEqual(1f, ThreatReadabilityAnimator.CrossfadeAlpha(1f), 0.001f);
            Assert.AreEqual(0.25f, ThreatReadabilityAnimator.CrossfadeAlpha(0.5f), 0.001f,
                "Ease-in keeps each authored frame crisp for most of its slot.");
            Assert.AreEqual(0f, ThreatReadabilityAnimator.CrossfadeAlpha(-1f), 0.001f);
            Assert.AreEqual(1f, ThreatReadabilityAnimator.CrossfadeAlpha(2f), 0.001f);
        }

        [Test]
        public void FractionalFrameMath_MatchesTheIntegerFrameContract()
        {
            foreach (var (actor, clip) in AnimatedPairs)
            {
                int count = ThreatMotionArt.FrameCount(actor, clip);
                for (float elapsed = 0f; elapsed < 2f; elapsed += 0.07f)
                {
                    float fractional = ThreatMotionArt.FractionalFrameAtTime(actor, clip, elapsed);
                    Assert.AreEqual(Mathf.FloorToInt(fractional) % count,
                        ThreatMotionArt.FrameAtTime(actor, clip, elapsed),
                        $"{actor}/{clip} fractional frame must floor to the displayed frame.");
                    float sub = ThreatMotionArt.SubFrameFractionAtTime(actor, clip, elapsed);
                    Assert.That(sub, Is.InRange(0f, 1f));
                    float cycle = ThreatMotionArt.CyclePhaseAtTime(actor, clip, elapsed);
                    Assert.That(cycle, Is.InRange(0f, 1f));
                }
            }
        }

        // ---- Scene wiring ----

        [UnityTest]
        public IEnumerator ThreatActors_CrossfadeFramesAndCarryProceduralPose()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var predator = game.PredatorObject;
            var motion = predator.GetComponent<ThreatReadabilityAnimator>();
            Assert.IsNotNull(motion);
            motion.SetLabelState("SHADOW! HUDDLE + DOUBLE BARK!");
            yield return null;
            Assert.IsTrue(motion.UsesAuthoredMotion);

            var authored = predator.transform.Find("ThreatAuthoredMotion");
            Assert.IsNotNull(authored);
            var blend = authored.Find("ThreatMotionBlend");
            Assert.IsNotNull(blend, "The crossfade renderer must ride inside the authored child.");
            var blendRenderer = blend.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(blendRenderer);
            Assert.Greater(blendRenderer.sortingOrder,
                authored.GetComponent<SpriteRenderer>().sortingOrder,
                "The incoming frame fades in above the current frame.");

            // Same-frame consistency: the blend must always hold the NEXT strip frame while the
            // authored renderer holds the current one, and its alpha must stay a valid fade.
            bool sawBlendVisible = false;
            bool sawPoseOffset = false;
            float deadline = Time.time + 1.2f;
            Vector3 restingPosition = new Vector3(0f, -0.08f, -0.18f);
            while (Time.time < deadline)
            {
                yield return null;
                if (!motion.UsesAuthoredMotion) continue;
                Assert.That(motion.BlendAlpha, Is.InRange(0f, 1f));
                if (motion.BlendSpriteName.Length > 0 && motion.CurrentClipLabel == "Sweep")
                {
                    sawBlendVisible = true;
                    int count = ThreatMotionArt.FrameCount(ThreatMotionArt.Actor.Eagle,
                        ThreatMotionArt.Clip.Sweep);
                    int next = (motion.CurrentFrameIndex + 1) % count;
                    Assert.AreEqual($"eagle_sweep_e_{next:00}", motion.BlendSpriteName,
                        "The crossfade must blend toward the immediate next wing frame.");
                }
                if ((authored.localPosition - restingPosition).magnitude > 0.01f)
                    sawPoseOffset = true;
            }
            Assert.IsTrue(sawBlendVisible, "The crossfade should become visible within a second.");
            Assert.IsTrue(sawPoseOffset, "The wing-synced lift should move the authored child.");

            // Rebind the same animator as a coyote and confirm the pose layer follows the actor.
            motion.SetLabelState("COYOTE FENCE PRESSURE - BARK!");
            yield return null;
            Assert.AreEqual("Coyote", motion.CurrentActorLabel);
            Assert.AreEqual("Threaten", motion.CurrentClipLabel);

            bool sawLungeShift = false;
            deadline = Time.time + 1.2f;
            while (Time.time < deadline)
            {
                // The mission tick may push its own eagle label back onto the shared actor; this
                // test only pins that the threaten pose physically moves the body, so keep the
                // coyote clip bound while polling.
                motion.SetLabelState("COYOTE FENCE PRESSURE - BARK!");
                yield return null;
                if (Mathf.Abs(authored.localPosition.x - restingPosition.x) > 0.02f)
                {
                    sawLungeShift = true;
                    break;
                }
            }
            Assert.IsTrue(sawLungeShift,
                "The threaten clip should lunge the coyote body instead of sliding a static cutout.");
        }
    }
}
