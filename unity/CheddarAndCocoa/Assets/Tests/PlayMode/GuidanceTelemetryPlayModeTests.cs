using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// G1.6: the guidance ladder produces couch-test data. Tier-2/Tier-3 crossings are counted
    /// per mission attempt (edge-triggered, not continuous), logged through the existing playtest
    /// event path, and folded into the session summary once an attempt ends. The F1 debug readout
    /// and the session-summary line are the only places this data renders - never the normal-play HUD.
    /// </summary>
    public sealed class GuidanceTelemetryPlayModeTests
    {
        [UnityTest]
        public IEnumerator ForcedStall_RecordsTier2AndTier3Activations_ThroughThePlaytestEventLog()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            Assert.AreEqual(0, game.GuidanceTier2Activations);
            Assert.AreEqual(0, game.GuidanceTier3Activations);

            game.ForceGuidanceStall(25f); // crosses into Tier 2
            yield return null;

            Assert.AreEqual(1, game.GuidanceTier2Activations);
            Assert.AreEqual(0, game.GuidanceTier3Activations);
            Assert.IsTrue(LogContains(game, "GuidanceTier2:"), "The Tier-2 crossing must be recorded in the playtest event log.");

            game.ForceGuidanceStall(20f); // total 45s, crosses into Tier 3
            yield return null;

            Assert.AreEqual(1, game.GuidanceTier2Activations, "Staying past Tier 2 must not recount it.");
            Assert.AreEqual(1, game.GuidanceTier3Activations);
            Assert.IsTrue(LogContains(game, "GuidanceTier3:"), "The Tier-3 crossing must be recorded in the playtest event log.");
        }

        [UnityTest]
        public IEnumerator RepeatedStalls_EachCrossingCountsSeparately()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            game.ForceGuidanceStall(25f);
            yield return null;
            Assert.AreEqual(1, game.GuidanceTier2Activations);

            // A real progress signal resets the ladder; stalling again crosses Tier 2 a second time.
            ((KitchenFoodFrenzyMissionController)game.ActiveMissionController)
                .ForceDrop(KitchenFoodFrenzyMissionState.FoodKind.Good);
            yield return null;
            yield return null;
            Assert.AreEqual(0, game.GuidanceTier);

            game.ForceGuidanceStall(25f);
            yield return null;
            Assert.AreEqual(2, game.GuidanceTier2Activations, "Each fresh crossing into Tier 2 must count separately.");
        }

        [UnityTest]
        public IEnumerator MissionEnd_FoldsActivationCountsIntoTheSessionSummary()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            Assert.AreEqual("Stalls: none yet.", game.SessionGuidanceActivationsLabel);

            game.ForceGuidanceStall(45f); // Tier 2 then Tier 3
            yield return null;
            game.ForceGameOver();
            yield return null;

            Assert.That(game.SessionGuidanceActivationsLabel, Does.Contain("T2x1"));
            Assert.That(game.SessionGuidanceActivationsLabel, Does.Contain("T3x1"));
        }

        [UnityTest]
        public IEnumerator QuietAttempt_LeavesSessionSummaryUnmentioned()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            game.ForceGameOver();
            yield return null;

            Assert.AreEqual("Stalls: none yet.", game.SessionGuidanceActivationsLabel,
                "An attempt with no stalls must not clutter the session summary.");
        }

        [UnityTest]
        public IEnumerator Replay_ResetsPerAttemptActivationCounts()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            game.ForceGuidanceStall(45f);
            yield return null;
            Assert.AreEqual(1, game.GuidanceTier2Activations);
            Assert.AreEqual(1, game.GuidanceTier3Activations);

            game.Restart();
            yield return null;

            Assert.AreEqual(0, game.GuidanceTier2Activations);
            Assert.AreEqual(0, game.GuidanceTier3Activations);
        }

        [UnityTest]
        public IEnumerator NormalPlayHud_NeverMentionsTierOrStalled()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            game.ForceGuidanceStall(45f);
            yield return null;

            // The normal-play, always-visible HUD strings must stay guidance-ladder-silent; the tier
            // readout only exists in GuidanceDebugLabel (F1 overlay) and SessionGuidanceActivationsLabel
            // (post-session screen), neither of which is part of the production gameplay HUD.
            Assert.That(game.ObjectiveLabel, Does.Not.Contain("Tier"));
            Assert.That(game.ObjectiveLabel, Does.Not.Contain("stalled"));
            Assert.That(game.TeamGuidanceLabel, Does.Not.Contain("Tier"));
            Assert.That(game.TeamGuidanceLabel, Does.Not.Contain("stalled"));

            Assert.That(game.GuidanceDebugLabel, Does.Contain("Tier"));
            Assert.That(game.GuidanceDebugLabel, Does.Contain("stalled"));
        }

        private static bool LogContains(GameManager game, string text)
        {
            foreach (string entry in game.PlaytestEvents)
                if (entry.Contains(text)) return true;
            return false;
        }

        private static IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }
    }
}
