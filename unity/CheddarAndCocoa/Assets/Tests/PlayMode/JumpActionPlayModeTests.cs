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
    /// Coverage for the jump verb: couch feedback was "there's only a bark action" because jump (B /
    /// Left-Shift / Right-Ctrl) and interact were wired to input but never did anything visible.
    /// Jump is now a real arc hop with per-dog feedback, gated the same way wrestle/tug/interact are
    /// (rooted while Busy).
    /// </summary>
    public sealed class JumpActionPlayModeTests
    {
        [UnityTest]
        public IEnumerator Jump_RampsHeightThenReturnsToZero_AndShowsTheJumpPose()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var cheddarGo = GameObject.Find("Cheddar");
            Assert.IsNotNull(cheddarGo);
            cheddarGo.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            var dog = cheddarGo.GetComponent<DogController>();
            var feedback = cheddarGo.GetComponent<DogReadabilityFeedback>();

            Assert.IsFalse(dog.IsJumping);
            Assert.AreEqual(0f, dog.JumpHeight01);

            // GamepadPlayerInput is disabled above so nothing but this test drives Tick() - the
            // decay/height ramp in UpdateOverlays only runs from inside Tick(), so the test has to
            // pump it manually each frame instead of relying on a real input source.
            dog.Jump();
            Assert.IsTrue(dog.IsJumping, "Jump() should immediately start the arc.");

            dog.Tick(default, Time.deltaTime);
            yield return null;
            Assert.AreEqual(DogReadabilityFeedback.Pose.Jump, feedback.CurrentPose,
                "The jump pose should be visible while airborne, not just logged.");
            Assert.Greater(dog.JumpHeight01, 0f, "Height should ramp up mid-arc.");

            float guard = 0f;
            while (dog.IsJumping && guard < 2f)
            {
                dog.Tick(default, Time.deltaTime);
                guard += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(dog.IsJumping, "The jump must land within its authored duration.");
            Assert.AreEqual(0f, dog.JumpHeight01, "Height must return to the ground on landing.");
        }

        [UnityTest]
        public IEnumerator Jump_IsBlockedWhileBusy()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var cheddarGo = GameObject.Find("Cheddar");
            cheddarGo.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            var dog = cheddarGo.GetComponent<DogController>();

            dog.SetMode(MovementMode.Stunned);
            dog.Jump();
            Assert.IsFalse(dog.IsJumping, "A rooted dog (stunned/tug/shaking/transit) must not hop.");
        }

        [UnityTest]
        public IEnumerator Jump_IsConsumedFromMoveIntent()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            var cheddarGo = GameObject.Find("Cheddar");
            cheddarGo.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            var dog = cheddarGo.GetComponent<DogController>();

            dog.Tick(new DogController.MoveIntent { jump = true }, Time.deltaTime);
            Assert.IsTrue(dog.IsJumping, "Tick() must resolve intent.jump into a real hop, not silently drop it.");
        }
    }
}
