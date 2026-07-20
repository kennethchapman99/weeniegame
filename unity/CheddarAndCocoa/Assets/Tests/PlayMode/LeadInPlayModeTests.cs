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
    /// Contract for the post-StartMission freeze: the yard is visible and the dogs can roam, but
    /// the round clock, threats, and controller schedules hold still across two phases. Phase 1
    /// (CF1.1) is the briefing card itself: it never expires on a timer, only a deliberate
    /// bark/interact/grab ("accept") dismisses it. Phase 2 is the existing timed sniff-around
    /// discovery beat that then runs exactly as before, endable early by any deliberate dog verb.
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
        public IEnumerator Briefing_StaysVisiblePastTheOldFiveSecondTimer_WithNoInput()
        {
            // CF1.1 (#4/#5): the couch-test finding was "the control card dismisses itself after a
            // period of time - I need time to look at it and accept it." Prove the card survives
            // comfortably past the pre-fix ArenaMissionTuning.IntroPromptSeconds (5f) with zero
            // player input, and that nothing about the game is ticking underneath it meanwhile.
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(game.MissionBriefingVisible);

            float frozenTimeRemaining = game.TimeRemaining;
            float frozenMissionNow = game.MissionNow;

            yield return new WaitForSeconds(5.5f);

            Assert.IsTrue(game.MissionBriefingVisible,
                "The card must wait for an explicit bark/interact/grab and never auto-dismiss on a timer.");
            Assert.IsTrue(game.LeadInActive);
            Assert.AreEqual(frozenTimeRemaining, game.TimeRemaining, "The round clock must not advance while the card is up.");
            Assert.Less(Mathf.Abs(game.MissionNow - frozenMissionNow), 0.01f, "The mission clock must plateau while the card is up.");
        }

        [UnityTest]
        public IEnumerator Briefing_RealBarkDispatch_AcceptsAndDismissesTheCard()
        {
            // Routed through the real DogController.Bark() -> GameManager.OnDogBarked dispatch, not
            // a Force*/controller-only bypass (known-trap #2 in AGENT-WORK-QUEUE-COUCHFIX.md).
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(game.MissionBriefingVisible);

            cheddar.Bark();

            Assert.IsFalse(game.MissionBriefingVisible, "A real bark dispatch must accept and dismiss the briefing card.");
            Assert.IsTrue(game.LeadInActive,
                "Accepting the card must hand off into the still-frozen sniff beat, not skip straight to live play.");
            Assert.AreEqual(1, game.BarksUsed);
            Assert.IsTrue(LogContains(game, "MissionBriefing: accepted"));
        }

        [UnityTest]
        public IEnumerator LeadIn_BarkAcceptsBriefing_ThenSecondBarkStartsTheRoundEarly()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(game.LeadInActive);
            Assert.IsTrue(game.MissionBriefingVisible);

            // CF1.1: the first bark only accepts the card - it must not skip straight to live play.
            float roundClockAtFirstBark = game.TimeRemaining;
            cheddar.Bark();

            Assert.IsFalse(game.MissionBriefingVisible, "A ready-bark should drop the briefing card.");
            Assert.IsTrue(game.LeadInActive, "Accepting the card hands off into the still-frozen sniff beat, not live play.");
            Assert.AreEqual(1, game.BarksUsed);
            Assert.IsTrue(LogContains(game, "MissionBriefing: accepted"));

            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(roundClockAtFirstBark, game.TimeRemaining, "The round clock must still be frozen during the post-accept sniff beat.");

            // A second bark during the sniff beat still ends the freeze early, same as pre-CF1.1.
            cheddar.Bark();

            Assert.IsFalse(game.LeadInActive);
            Assert.AreEqual(2, game.BarksUsed);
            Assert.IsTrue(LogContains(game, "LeadIn: GO"));
            Assert.That(game.LastCue, Does.StartWith("GO!"));

            for (int i = 0; i < 30; i++) yield return null;
            Assert.Less(game.TimeRemaining, roundClockAtFirstBark, "The round clock must run once the sniff beat is skipped.");
        }

        [UnityTest]
        public IEnumerator LeadIn_FirstGrabAcceptsBriefing_AndStillScores()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(game.LeadInActive);
            Assert.IsTrue(game.MissionBriefingVisible);

            var treats = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
            Assert.Greater(treats.Length, 0);
            treats[0].CollectBy(cheddar);

            // CF1.1: scooping the first collectible mid-briefing accepts the card - it must not
            // also skip past the post-accept sniff beat.
            Assert.IsFalse(game.MissionBriefingVisible, "Scooping a collectible counts as accepting the briefing.");
            Assert.IsTrue(game.LeadInActive, "Accepting hands off into the still-frozen sniff beat, not live play.");
            Assert.Greater(game.Score, 0, "The accepting grab must still bank normally.");
            Assert.IsTrue(LogContains(game, "MissionBriefing: accepted"));
        }

        [UnityTest]
        public IEnumerator LeadIn_InteractAcceptsBriefing_WithoutAMissPenalty()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cocoa);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(game.LeadInActive);
            Assert.IsTrue(game.MissionBriefingVisible);

            cocoa.Interact();

            Assert.IsFalse(game.MissionBriefingVisible, "A ready-interact should drop the briefing card.");
            Assert.IsTrue(game.LeadInActive, "Accepting hands off into the still-frozen sniff beat, not live play.");
            Assert.AreEqual(0, game.FailedInteractions, "The ready-interact must not count as a missed interaction.");
            Assert.IsTrue(LogContains(game, "MissionBriefing: accepted"));
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
            // CF1.1: a zero-second override now also auto-accepts the card so the ~650 legacy
            // deterministic tests using this seam reach live play immediately, same as they always
            // have - mirroring how this same override already force-skips the opening presentation.
            Assert.IsFalse(game.MissionBriefingVisible, "The zero-second override also auto-accepts the briefing card.");
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
