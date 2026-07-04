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
    /// Contract for the sniff-around lead-in: after StartMission the yard is visible and the dogs
    /// can roam, but the round clock, threats, and controller schedules hold still until the
    /// discovery beat ends — naturally, or early via any deliberate dog verb.
    /// </summary>
    public sealed class LeadInPlayModeTests
    {
        [SetUp]
        public void EnableLongLeadIn() =>
            // Long enough that tiny headless deltaTime can never expire it inside a test.
            GameManager.LeadInSecondsOverride = 30f;

        [TearDown]
        public void RestoreSuiteDefault() =>
            // The assembly-wide fixture runs the rest of the suite with the lead-in disabled.
            GameManager.LeadInSecondsOverride = 0f;

        [Test]
        public void LeadInTuningDefaults_AreShortAndAdditiveToBriefing()
        {
            var tuning = ArenaMissionTuning.CreateDefault();
            Assert.AreEqual(2.5f, tuning.LeadInSniffSeconds);
            // Total freeze = briefing card + open-yard sniff; keep it "just a little" lead-in.
            Assert.LessOrEqual(tuning.IntroPromptSeconds + tuning.LeadInSniffSeconds, 8f);
        }

        [UnityTest]
        public IEnumerator LeadIn_FreezesRoundClockAndMissionClock_UntilSkipped()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            Assert.IsTrue(game.LeadInActive);
            Assert.IsTrue(game.MissionBriefingVisible, "The briefing card should cover the start of the freeze.");
            Assert.That(game.LeadInCountdownLabel, Does.Contain("BARK"));

            float frozenTimeRemaining = game.TimeRemaining;
            float frozenMissionNow = game.MissionNow;
            for (int i = 0; i < 10; i++) yield return null;

            Assert.IsTrue(game.LeadInActive);
            Assert.AreEqual(frozenTimeRemaining, game.TimeRemaining, "The round clock must not run during the lead-in.");
            Assert.Less(Mathf.Abs(game.MissionNow - frozenMissionNow), 0.01f, "The mission clock must plateau during the lead-in.");

            game.ForceGameOver();
            Assert.IsFalse(game.LeadInActive, "Ending the round must always clear the lead-in freeze.");
        }

        [UnityTest]
        public IEnumerator LeadIn_BarkStartsTheRoundEarly_AndDropsTheBriefing()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(game.LeadInActive);

            float roundClockAtBark = game.TimeRemaining;
            cheddar.Bark();

            Assert.IsFalse(game.LeadInActive);
            Assert.IsFalse(game.MissionBriefingVisible, "A ready-bark should drop the briefing card with the freeze.");
            Assert.AreEqual(1, game.BarksUsed);
            Assert.IsTrue(LogContains(game, "LeadIn: GO"));
            Assert.That(game.LastCue, Does.StartWith("GO!"));

            for (int i = 0; i < 30; i++) yield return null;
            Assert.Less(game.TimeRemaining, roundClockAtBark, "The round clock must run once the lead-in is skipped.");
        }

        [UnityTest]
        public IEnumerator LeadIn_FirstGrabStartsTheRound_AndStillScores()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(game.LeadInActive);

            var treats = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
            Assert.Greater(treats.Length, 0);
            treats[0].CollectBy(cheddar);

            Assert.IsFalse(game.LeadInActive, "Scooping a collectible counts as starting to play.");
            Assert.Greater(game.Score, 0, "The lead-in-ending grab must still bank normally.");
            Assert.IsTrue(LogContains(game, "LeadIn: GO"));
        }

        [UnityTest]
        public IEnumerator LeadIn_InteractStartsTheRound_WithoutAMissPenalty()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cocoa);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(game.LeadInActive);

            cocoa.Interact();

            Assert.IsFalse(game.LeadInActive);
            Assert.AreEqual(0, game.FailedInteractions, "The ready-interact must not count as a missed interaction.");
            Assert.IsTrue(LogContains(game, "LeadIn: GO"));
        }

        [UnityTest]
        public IEnumerator LeadIn_OverrideZero_SkipsStraightToAction()
        {
            GameManager.LeadInSecondsOverride = 0f;
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            Assert.IsFalse(game.LeadInActive);
            Assert.IsEmpty(game.LeadInCountdownLabel);
            Assert.Less(Mathf.Abs(game.MissionNow - Time.time), 0.01f, "With no freeze the mission clock tracks real time.");
            Assert.IsTrue(game.MissionBriefingVisible, "The briefing card itself is untouched by the override.");
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

        private static bool LogContains(GameManager game, string text)
        {
            foreach (string entry in game.PlaytestEvents)
            {
                if (entry.Contains(text)) return true;
            }

            return false;
        }
    }
}
