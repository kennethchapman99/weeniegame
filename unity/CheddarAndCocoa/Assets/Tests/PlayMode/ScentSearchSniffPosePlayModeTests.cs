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
    /// A2.3 (code-side prep only - see the class-level note on DogReadabilityFeedback.Pose.Sniff for
    /// why no authored art exists yet): Scent Search's Sniff() verb now drives a real
    /// DogReadabilityFeedback.Pose.Sniff read on both dogs' tracking beats, distinct from the actual
    /// discovery celebration (ShowProudBrief), so the wiring is ready the moment real art lands.
    /// </summary>
    public sealed class ScentSearchSniffPosePlayModeTests
    {
        [UnityTest]
        public IEnumerator CheddarWildSniff_PlaysTheSniffPose()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;
            var cheddar = FindDog(DogId.Cheddar);
            var cheddarFeedback = cheddar.GetComponent<DogReadabilityFeedback>();

            game.ForceScentSniff(DogId.Cheddar);

            Assert.AreEqual(DogReadabilityFeedback.Pose.Sniff, cheddarFeedback.CurrentPose);
            Assert.IsTrue(cheddarFeedback.UsesAuthoredPoseArt,
                "With no Sniff art yet, this must still render the graceful idle-image fallback, not go blank.");
        }

        [UnityTest]
        public IEnumerator CocoaTrackingSniff_PlaysTheSniffPose_ButAFreshHotCallPlaysProudInstead()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;
            var cocoa = FindDog(DogId.Cocoa);
            var cocoaFeedback = cocoa.GetComponent<DogReadabilityFeedback>();
            var controller = (ScentSearchMissionController)game.ActiveMissionController;

            // Cold/warm tracking sniff, away from the buried spot: the ongoing sniff read.
            cocoa.transform.position = game.ArenaBounds.center + Vector2.left * 15f;
            game.ForceScentSniff(DogId.Cocoa);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sniff, cocoaFeedback.CurrentPose);

            // The exact hot patch: the real discovery beat takes precedence over the ongoing sniff.
            int buried = controller.BuriedSpotIndex;
            cocoa.transform.position = controller.DigSpots[buried];
            game.ForceScentSniff(DogId.Cocoa);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, cocoaFeedback.CurrentPose,
                "Finding the exact hot patch is a bigger beat than the ongoing sniff read.");
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
