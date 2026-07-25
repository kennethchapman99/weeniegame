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
    /// A2.2: pressing Interact visibly does something on the dog itself, not just the prop. The
    /// shared choke point is GameManager.OnDogInteracted - it plays DogReadabilityFeedback's
    /// squash-and-pop read only when the mission controller's HandleInteract both returns true AND
    /// did not also call MarkFailedInteraction (a wrong-role/too-far coach beat), so the two
    /// reactions never double-fire together. Gate Crash is the fixture: Cocoa anchoring the gate is
    /// a clean accepted-Interact case, and Cheddar attempting the same action is a clean
    /// wrong-role-coached rejection.
    /// </summary>
    public sealed class InteractMicroAnimationPlayModeTests
    {
        [UnityTest]
        public IEnumerator AcceptedInteract_PlaysTheSquashRead_OnTheAcceptingDog()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;
            var cocoa = FindDog(DogId.Cocoa);
            var cocoaFeedback = cocoa.GetComponent<DogReadabilityFeedback>();

            Assert.IsFalse(cocoaFeedback.IsShowingInteractAccepted);

            cocoa.transform.position = game.GateHoldZone;
            cocoa.Interact();

            Assert.IsTrue(game.GateCrashController.AnchorEngaged, "Sanity: this must be the genuine acceptance path.");
            Assert.IsTrue(cocoaFeedback.IsShowingInteractAccepted, "A genuinely accepted Interact must play the squash read.");
            Assert.AreEqual(DogFeedbackAction.Interact, cocoaFeedback.ActionFeedback.CurrentAction,
                "A genuinely accepted Interact should also play the fuller particled grab beat.");
        }

        [UnityTest]
        public IEnumerator WrongRoleInteract_DoesNotDoubleFire_WithTheCoachingGag()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;
            var cheddar = FindDog(DogId.Cheddar);
            var cheddarFeedback = cheddar.GetComponent<DogReadabilityFeedback>();

            cheddar.transform.position = game.GateHoldZone;
            cheddar.Interact();

            Assert.IsFalse(game.GateCrashController.AnchorEngaged, "Cheddar cannot anchor the gate - Cocoa's role.");
            Assert.That(game.LastJuiceLabel, Does.Contain("COCOA"), "Sanity: this must be the coached-rejection path.");
            Assert.IsFalse(cheddarFeedback.IsShowingInteractAccepted,
                "A wrong-role coached rejection must not also play the accepted-Interact squash.");
            Assert.AreEqual(DogFeedbackAction.InteractMiss, cheddarFeedback.ActionFeedback.CurrentAction,
                "A coached rejection still needs its own readable miss beat instead of silence.");
        }

        [UnityTest]
        public IEnumerator TooFarInteract_DoesNotDoubleFire_WithTheCoachingGag()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;
            var cocoa = FindDog(DogId.Cocoa);
            var cocoaFeedback = cocoa.GetComponent<DogReadabilityFeedback>();

            cocoa.transform.position = game.GateHoldZone + Vector2.right * 20f; // out of HoldRange
            cocoa.Interact();

            Assert.IsFalse(game.GateCrashController.AnchorEngaged);
            Assert.IsFalse(cocoaFeedback.IsShowingInteractAccepted,
                "A too-far rejection must not also play the accepted-Interact squash.");
            Assert.AreEqual(DogFeedbackAction.InteractMiss, cocoaFeedback.ActionFeedback.CurrentAction,
                "A too-far rejection still needs its own readable miss beat instead of silence.");
        }

        [UnityTest]
        public IEnumerator SquashRead_ClearsItselfAfterTheBeat()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GateCrash);
            yield return null;
            var cocoa = FindDog(DogId.Cocoa);
            var cocoaFeedback = cocoa.GetComponent<DogReadabilityFeedback>();

            cocoa.transform.position = game.GateHoldZone;
            cocoa.Interact();
            Assert.IsTrue(cocoaFeedback.IsShowingInteractAccepted);

            yield return new WaitForSeconds(0.35f); // longer than the 0.22s beat

            Assert.IsFalse(cocoaFeedback.IsShowingInteractAccepted, "The squash read must clear itself after its short beat.");
        }

        private static IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        private static DogController FindDog(DogId dogId)
        {
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == dogId) return id.GetComponent<DogController>();
            }

            return null;
        }
    }
}
