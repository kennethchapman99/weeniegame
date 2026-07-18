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
    /// Wiring contract for the shared stall-escalation ladder (see MissionGuidanceEscalation): the
    /// GameManager-owned service ticks only during active play, freezes across the lead-in/briefing,
    /// pause, and held-success windows, and resets on any progress signal or mission start/replay.
    /// Tier rendering (G1.2) is out of scope here - GameManager.GuidanceTier/GuidanceStallSeconds are
    /// the only observable surface this task adds.
    /// </summary>
    public sealed class GuidanceEscalationPlayModeTests
    {
        [UnityTest]
        public IEnumerator MissionStart_InitializesGuidanceAtTierZero()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            Assert.AreEqual(0, game.GuidanceTier);
            Assert.Less(game.GuidanceStallSeconds, 0.1f, "One Update frame after start may tick a hair above zero, but must stay well under the Tier 1 threshold.");
        }

        [UnityTest]
        public IEnumerator ForcedStall_AdvancesTier_AtForcedTimestamps()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            game.ForceGuidanceStall(12f);
            Assert.AreEqual(1, game.GuidanceTier);

            game.ForceGuidanceStall(13f); // total 25s
            Assert.AreEqual(2, game.GuidanceTier);

            game.ForceGuidanceStall(20f); // total 45s
            Assert.AreEqual(3, game.GuidanceTier);
        }

        [UnityTest]
        public IEnumerator ScoreEvent_ResetsStallAndTier()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            game.ForceGuidanceStall(20f);
            Assert.AreEqual(1, game.GuidanceTier);

            var treat = Object.FindFirstObjectByType<Treat>();
            Assert.IsNotNull(treat);
            treat.CollectBy(cheddar);

            Assert.Greater(game.LastScoreDelta, 0, "The collect must actually bank score for this to be a score-event test.");
            Assert.AreEqual(0, game.GuidanceTier, "A score event must drop the ladder back to Tier 0.");
            Assert.AreEqual(0f, game.GuidanceStallSeconds);
        }

        [UnityTest]
        public IEnumerator ObjectiveChangeWithoutScore_ResetsStallAndTier()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            game.ForceGuidanceStall(20f);
            Assert.AreEqual(1, game.GuidanceTier);

            string objectiveBefore = game.ObjectiveLabel;
            game.ForcePredatorWarning();
            yield return null;

            Assert.AreNotEqual(objectiveBefore, game.ObjectiveLabel, "This test needs an objective-copy change that carries no score delta.");
            Assert.AreEqual(0, game.LastScoreDelta, "The predator-warning transition alone must not bank score.");
            Assert.AreEqual(0, game.GuidanceTier, "An objective-copy change alone must drop the ladder back to Tier 0.");
        }

        [UnityTest]
        public IEnumerator Pause_DoesNotAccumulateStallTime()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            game.ForceGuidanceStall(10f);
            float stallBeforePause = game.GuidanceStallSeconds;

            game.TogglePause();
            Assert.IsTrue(game.IsPaused);
            for (int i = 0; i < 10; i++) yield return null;

            Assert.AreEqual(stallBeforePause, game.GuidanceStallSeconds, "Paused frames must not advance the stall clock.");
            Assert.AreEqual(0, game.GuidanceTier);

            game.TogglePause();
            Assert.IsFalse(game.IsPaused);
        }

        [UnityTest]
        public IEnumerator LeadIn_DoesNotAccumulateStallTime()
        {
            GameManager.LeadInSecondsOverride = 30f;
            try
            {
                yield return LoadArena();
                var game = Object.FindFirstObjectByType<GameManager>();
                game.StartMission(GameManager.MissionVariant.BackyardRescue);
                yield return null;
                Assert.IsTrue(game.LeadInActive, "This test needs the lead-in freeze actually active to prove anything.");

                float stallAtLeadInStart = game.GuidanceStallSeconds;
                for (int i = 0; i < 10; i++) yield return null;

                Assert.IsTrue(game.LeadInActive, "The lead-in must still be running for this assertion to be meaningful.");
                Assert.AreEqual(stallAtLeadInStart, game.GuidanceStallSeconds, "The sniff-around lead-in must not advance the stall clock.");
            }
            finally
            {
                GameManager.LeadInSecondsOverride = 0f;
            }
        }

        [UnityTest]
        public IEnumerator HeldSuccessPayoff_DoesNotAccumulateStallTime()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            // SnackHeist requires one Cocoa guard-bark (at the squirrel) before further collects
            // count; mirrors ArenaGameLoopPlayModeTests.SnackHeist_Initializes_Scores_Clears_Fails_AndReplays.
            Object.FindFirstObjectByType<Treat>().CollectBy(cheddar);
            cocoa.transform.position = game.SquirrelObject.transform.position;
            Assert.IsTrue(game.SnackHeistController.HandleBark(1));

            while (game.BreakfastRecovered < game.BreakfastGoal)
            {
                var treat = Object.FindFirstObjectByType<Treat>();
                Assert.IsNotNull(treat);
                treat.CollectBy(cheddar);
                yield return null;
            }

            Assert.IsTrue(game.SnackHeistController.IsPresentingSuccessfulOutcome,
                "This test needs the held-success payoff actually active to prove anything.");
            float stallAtHoldStart = game.GuidanceStallSeconds;
            for (int i = 0; i < 10; i++) yield return null;

            Assert.IsTrue(game.SnackHeistController.IsPresentingSuccessfulOutcome,
                "The held payoff must still be running for this assertion to be meaningful.");
            Assert.AreEqual(stallAtHoldStart, game.GuidanceStallSeconds, "A held success payoff must not advance the stall clock.");
        }

        [UnityTest]
        public IEnumerator Replay_ResetsGuidanceToTierZero()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            game.ForceGuidanceStall(45f);
            Assert.AreEqual(3, game.GuidanceTier);

            game.Restart();
            yield return null;

            Assert.AreEqual(0, game.GuidanceTier);
            Assert.Less(game.GuidanceStallSeconds, 0.1f, "One Update frame after restart may tick a hair above zero, but must stay well under the Tier 1 threshold.");
        }

        [UnityTest]
        public IEnumerator MissionDefinitions_DefaultToFullTierCapAndDefaultTimings()
        {
            var tuning = ArenaMissionTuning.CreateDefault();
            foreach (var variant in MissionControllerRegistry.RegisteredVariants)
            {
                Assert.IsTrue(MissionControllerRegistry.TryBuildDefinition(variant, tuning, out var definition));
                Assert.AreEqual(MissionGuidanceEscalation.MaxTier, definition.GuidanceTierCap, $"{variant} should default to the full ladder.");
                Assert.AreEqual(MissionGuidanceEscalation.DefaultTier1Seconds, definition.GuidanceTier1Seconds, $"{variant} should default to the standard Tier 1 timing.");
                Assert.AreEqual(MissionGuidanceEscalation.DefaultTier2Seconds, definition.GuidanceTier2Seconds, $"{variant} should default to the standard Tier 2 timing.");
                Assert.AreEqual(MissionGuidanceEscalation.DefaultTier3Seconds, definition.GuidanceTier3Seconds, $"{variant} should default to the standard Tier 3 timing.");
            }

            yield break;
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
