using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Game-feel coverage for the Backyard Rescue tug beat: the live rope pull (not a forced pose) must
    /// drive both dogs into the Tug clip, gate on the partner, read as two dogs pulling from opposite
    /// sides, and keep Cheddar/Cocoa visually distinct.
    /// </summary>
    public sealed class TugFeelPlayModeTests
    {
        [Test]
        public void TugCadence_DiffersBetweenCheddarAndCocoa()
        {
            // Cheddar tugs faster (9 fps) than Cocoa (7 fps); on the 3-frame loop they land on different
            // frames at the same instant, so the team tug never looks like one synced animation.
            int cheddar = CharacterMotionArt.FrameAtTime(DogId.Cheddar, CharacterMotionArt.Clip.Tug, 0.25f);
            int cocoa = CharacterMotionArt.FrameAtTime(DogId.Cocoa, CharacterMotionArt.Clip.Tug, 0.25f);
            Assert.AreNotEqual(cheddar, cocoa, "Cheddar and Cocoa should tug at distinct cadence to preserve identity.");
        }

        [UnityTest]
        public IEnumerator TeamTug_BothDogsTugAndFaceTheRopeFromOppositeSides()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return new WaitForSeconds(0.2f);

            var cheddar = GameObject.Find("Cheddar");
            var cocoa = GameObject.Find("Cocoa");
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);
            cheddar.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            cocoa.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            var cheddarFeedback = cheddar.GetComponent<DogReadabilityFeedback>();
            var cocoaFeedback = cocoa.GetComponent<DogReadabilityFeedback>();

            game.RopeObject.transform.position = Vector3.zero;

            // Partner-gating: a lone dog at the rope must not charge the tug; the rope asks for the partner.
            PlaceStill(cheddar, new Vector3(-1f, 0f, 0f));
            PlaceStill(cocoa, new Vector3(6f, 0f, 0f));
            yield return null;
            yield return null;
            float lonelyProgress = game.TugProgress;
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(lonelyProgress, game.TugProgress, 0.0001f,
                "A single dog at the rope must not charge the tug alone.");
            Assert.That(game.RopeObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("NEEDS BOTH DOGS"));

            // Both dogs flank the rope and commit: both lock into the live Tug clip (not a one-off forced pose).
            for (int i = 0; i < 5; i++)
            {
                PlaceStill(cheddar, new Vector3(-1f, 0f, 0f));
                PlaceStill(cocoa, new Vector3(1f, 0f, 0f));
                yield return null;
            }

            Assert.AreEqual(DogReadabilityFeedback.Pose.Tug, cheddarFeedback.CurrentPose);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Tug, cocoaFeedback.CurrentPose);
            Assert.AreEqual("Tug", cheddarFeedback.MotionClipLabel);
            Assert.AreEqual("Tug", cocoaFeedback.MotionClipLabel);
            Assert.That(cheddarFeedback.AuthoredPoseSpriteName, Does.Contain("_tug_e_"));
            Assert.That(cocoaFeedback.AuthoredPoseSpriteName, Does.Contain("_tug_e_"));

            // ...and they face the rope from opposite sides: left dog unmirrored, right dog mirrored.
            var cheddarPose = cheddar.transform.Find("CheddarAuthoredPose").GetComponent<SpriteRenderer>();
            var cocoaPose = cocoa.transform.Find("CocoaAuthoredPose").GetComponent<SpriteRenderer>();
            Assert.IsFalse(cheddarPose.flipX, "Cheddar (left of rope) should face right, into the rope.");
            Assert.IsTrue(cocoaPose.flipX, "Cocoa (right of rope) should face left, into the rope.");
            Assert.IsFalse(game.TugComplete, "Opposing-facing check must run mid-tug, before completion.");
        }

        private static void PlaceStill(GameObject dog, Vector3 position)
        {
            dog.transform.position = position;
            if (dog.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = Vector2.zero;
        }
    }
}
