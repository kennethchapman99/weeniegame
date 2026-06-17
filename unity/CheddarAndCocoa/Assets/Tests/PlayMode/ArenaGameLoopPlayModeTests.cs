using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ArenaGameLoopPlayModeTests
    {
        [UnityTest]
        public IEnumerator BackyardMission_Objectives_Hazards_Tug_Clear_AndRestart()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            DogController cheddar = null, cocoa = null;
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                var dc = id.GetComponent<DogController>();
                if (id.Id == DogId.Cheddar) cheddar = dc;
                else if (id.Id == DogId.Cocoa) cocoa = dc;
            }
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            Assert.AreEqual(GameManager.State.Playing, game.Phase);
            Assert.IsNotEmpty(game.ActiveModifierLabel);
            Assert.IsNotNull(game.SquirrelObject);
            Assert.IsNotNull(game.PredatorObject);
            Assert.IsNotNull(game.RopeObject);
            Assert.IsNotEmpty(game.LastCue);
            Assert.That(game.MissionIntroPrompt, Is.EqualTo("Cheddar + Cocoa must protect the weenies together."));
            Assert.That(game.MissionBanner, Does.Contain("protect the weenies"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Save weenies"));
            Assert.AreEqual(GameManager.FeedbackKind.Intro, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.None, game.LastJuiceFeedback);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.LastScoreDelta);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.That(game.LastScoreEventLabel, Does.Contain("READY"));
            Assert.IsFalse(game.ReplayPromptVisible);
            Assert.IsEmpty(game.EndSummaryLabel);
            Assert.IsNotNull(game.SquirrelObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.PredatorObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.RopeObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.SquirrelObject.transform.Find("SquirrelFlagTail"));
            Assert.IsNotNull(game.PredatorObject.transform.Find("PredatorWarningEyeA"));
            Assert.IsNotNull(game.RopeObject.transform.Find("RopeStripeA"));
            Assert.IsNotNull(game.GetComponent<AudioSource>());
            Assert.IsNotNull(Camera.main.GetComponent<AudioListener>());

            var cheddarFeedback = cheddar.GetComponent<DogReadabilityFeedback>();
            var cocoaFeedback = cocoa.GetComponent<DogReadabilityFeedback>();
            Assert.IsNotNull(cheddarFeedback);
            Assert.IsNotNull(cocoaFeedback);
            Assert.That(cheddarFeedback.IdentityLabel, Does.Contain("CHEDDAR CHAOS PUP"));
            Assert.That(cocoaFeedback.IdentityLabel, Does.Contain("COCOA SPOT QUEEN"));
            Assert.That(cheddarFeedback.ArtDirectionSignature, Does.Contain("golden-chaos"));
            Assert.That(cocoaFeedback.ArtDirectionSignature, Does.Contain("chocolate-spot"));
            Assert.IsNotNull(cheddar.transform.Find("LongDogSnout"));
            Assert.IsNotNull(cheddar.transform.Find("CheddarRedCollar"));
            Assert.IsNotNull(cheddar.transform.Find("CheddarIntentArrow"));
            Assert.IsNotNull(cocoa.transform.Find("LongDogSnout"));
            Assert.IsNotNull(cocoa.transform.Find("CocoaTealCollar"));
            Assert.IsNotNull(cocoa.transform.Find("CocoaIntentArrow"));
            Assert.IsNotNull(cocoa.transform.Find("CocoaQueenSpotA"));
            Assert.IsNotNull(game.ObjectiveArrows);
            Assert.AreEqual(2, game.ObjectiveArrows.Length);
            Assert.IsNotNull(game.ObjectiveArrows[0]);
            Assert.IsNotNull(game.ObjectiveArrows[1]);
            Assert.That(game.SquirrelObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("WAITING"));
            float introGuard = 0f;
            while (introGuard < 3f)
            {
                introGuard += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(0, game.StolenFood, "First squirrel steal should wait long enough for players to orient.");

            cheddar.Bark();
            yield return null;
            Assert.AreEqual(DogReadabilityFeedback.Pose.Bark, cheddarFeedback.CurrentPose);
            Assert.That(cheddarFeedback.IdentityLabel, Does.Contain("WOOF!"));
            Assert.AreEqual(GameManager.FeedbackKind.SoloBark, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.BarkBurst, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("BARK BURST"));
            Assert.IsNotNull(GameObject.Find("BarkBurst"));

            var rb = cheddar.GetComponent<Rigidbody2D>();
            cheddar.GetComponent<CheddarAndCocoa.Input.GamepadPlayerInput>().enabled = false;
            rb.linearVelocity = Vector2.left;
            float intentGuard = 0f;
            while (cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Bark && intentGuard < 0.6f)
            {
                intentGuard += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(DogReadabilityFeedback.Pose.Run, cheddarFeedback.CurrentPose);
            Assert.AreEqual("FacingLeft", cheddarFeedback.FacingIntentLabel);
            rb.linearVelocity = Vector2.zero;
            game.Restart();
            yield return null;

            var treats = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
            Assert.Greater(treats.Length, 0);
            Assert.IsNotNull(treats[0].transform.Find("WeenieMustard"));
            int scoreBefore = game.Score;
            treats[0].CollectBy(cheddar);
            Assert.AreEqual(scoreBefore + 50, game.Score);
            Assert.AreEqual(50, game.LastScoreDelta);
            Assert.AreEqual("+50 WEENIE SAVED", game.LastScoreEventLabel);
            Assert.AreEqual("+50 WEENIE SAVED", game.LastScorePopLabel);
            Assert.IsTrue(game.ScorePopVisible);
            Assert.IsTrue(FindWorldPopContaining("+50"));
            Assert.AreEqual(1, game.BreakfastRecovered);
            yield return null;

            cocoa.transform.position = cheddar.transform.position + Vector3.right * 6f;
            int unitedBefore = game.UnitedBarks;
            cheddar.Bark(); cocoa.Bark();
            Assert.AreEqual(unitedBefore, game.UnitedBarks, "United bark should require close dogs.");

            cocoa.transform.position = cheddar.transform.position + Vector3.right;
            cheddar.Bark(); cocoa.Bark();
            Assert.Greater(game.UnitedBarks, unitedBefore, "Close timed barks should count.");
            Assert.AreEqual(GameManager.FeedbackKind.UnitedBark, game.LastFeedback);
            Assert.AreEqual(100, game.LastScoreDelta);
            Assert.AreEqual("+100 UNITED BARK", game.LastScoreEventLabel);

            // Squirrel pressure: it can steal if ignored after being placed on food.
            game.Restart();
            yield return null;
            Assert.AreEqual(0, game.Score);
            var target = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
            game.SquirrelObject.transform.position = target.transform.position + Vector3.right;
            game.ForceSquirrelStealAttempt();
            Assert.That(game.ObjectiveLabel, Does.Contain("Bark to scare squirrel"));
            Assert.AreEqual(GameManager.FeedbackKind.SquirrelStealing, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.WarningMiss, game.LastJuiceFeedback);
            game.SquirrelObject.transform.position = target.transform.position;
            float guard = 0f;
            while (game.StolenFood == 0 && guard < 4f) { guard += Time.deltaTime; yield return null; }
            Assert.GreaterOrEqual(game.StolenFood, 1, "Squirrel should eventually steal breakfast.");
            Assert.Less(game.Score, 0);
            Assert.Less(game.LastScoreDelta, 0);
            Assert.That(game.LastScoreEventLabel, Does.Contain("SQUIRREL GOT ONE"));
            Assert.That(game.LastScorePopLabel, Does.Contain("SQUIRREL GOT ONE"));
            Assert.AreEqual(GameManager.FeedbackKind.SquirrelStoleFood, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.WarningMiss, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("SQUIRREL STOLE"));
            Assert.That(game.SquirrelObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("GOT A WEENIE"));
            Assert.IsTrue(FindWorldPopContaining("MISS"));

            // Bark near the squirrel should scare it and reward a small score bump.
            target = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
            game.ForceSquirrelStealAttempt();
            game.SquirrelObject.transform.position = target.transform.position;
            cheddar.transform.position = game.SquirrelObject.transform.position;
            scoreBefore = game.Score;
            cheddar.Bark();
            Assert.Greater(game.Score, scoreBefore, "Barking near squirrel should affect game state.");
            Assert.AreEqual(25, game.LastScoreDelta);
            Assert.AreEqual("+25 SQUIRREL SCARED", game.LastScoreEventLabel);
            Assert.That(game.LastCue, Does.Contain("squirrel").IgnoreCase);
            Assert.AreEqual(GameManager.FeedbackKind.SquirrelScared, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.SuccessPop, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("SQUIRREL DROP"));
            Assert.That(game.SquirrelObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("DROPPED"));

            // Predator warning/attack can be resolved by united bark.
            cheddar.transform.position = new Vector3(-4f, 4f, 0f);
            cocoa.transform.position = new Vector3(4f, 4f, 0f);
            game.ForcePredatorWarning();
            yield return null;
            Assert.AreEqual(GameManager.State.PredatorWarning, game.Phase);
            Assert.AreEqual(GameManager.FeedbackKind.PredatorHuddle, game.LastFeedback);
            Assert.That(game.ObjectiveLabel, Does.Contain("Huddle + bark"));
            Assert.That(game.PredatorObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("HUDDLE"));
            Assert.That(game.ObjectiveArrows[0].Label, Does.Contain("HUDDLE + BARK"));
            cocoa.transform.position = cheddar.transform.position + Vector3.right;
            scoreBefore = game.Score;
            cheddar.Bark(); cocoa.Bark();
            Assert.IsTrue(game.PredatorResolved);
            Assert.GreaterOrEqual(game.Score, scoreBefore + 400);
            Assert.AreEqual(300, game.LastScoreDelta);
            Assert.AreEqual("+300 PREDATOR YEETED", game.LastScoreEventLabel);
            Assert.That(game.LastCue, Does.Contain("predator").IgnoreCase);
            Assert.AreEqual(GameManager.FeedbackKind.UnitedBark, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.SuccessPop, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("PREDATOR YEETED"));

            // Failed predator attack stuns/grabs, then the partner rescues by coming close and barking.
            game.Restart();
            game.ForcePredatorAttack();
            yield return null;
            Assert.AreEqual(GameManager.State.PredatorAttack, game.Phase);
            Assert.IsTrue(game.AnyDogGrabbed);
            Assert.AreEqual(GameManager.FeedbackKind.PredatorAttack, game.LastFeedback);
            Assert.That(game.ObjectiveLabel, Does.Contain("Rescue"));
            Assert.AreEqual(-150, game.LastScoreDelta);
            Assert.AreEqual("-150 PREDATOR HIT", game.LastScoreEventLabel);
            Assert.AreEqual("-150 PREDATOR HIT", game.LastScorePopLabel);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.WarningMiss, game.LastJuiceFeedback);
            Assert.IsTrue(cheddar.Mode == MovementMode.Stunned || cocoa.Mode == MovementMode.Stunned);
            Assert.IsTrue(cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Stunned ||
                          cocoaFeedback.CurrentPose == DogReadabilityFeedback.Pose.Stunned);
            Assert.IsTrue(game.ObjectiveArrows[0].Label.Contains("BARK RESCUE") ||
                          game.ObjectiveArrows[1].Label.Contains("BARK RESCUE"));
            cheddar.transform.position = cocoa.transform.position;
            cheddar.Bark(); cocoa.Bark();
            yield return null;
            Assert.IsFalse(game.AnyDogGrabbed, "Partner bark should rescue grabbed dog.");
            Assert.AreEqual(250, game.LastScoreDelta);
            Assert.AreEqual("+250 PARTNER RESCUE", game.LastScoreEventLabel);
            Assert.AreEqual(GameManager.FeedbackKind.PartnerRescue, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.SuccessPop, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("RESCUE POP"));
            Assert.IsTrue(cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Rescued ||
                          cocoaFeedback.CurrentPose == DogReadabilityFeedback.Pose.Rescued ||
                          cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Proud ||
                          cocoaFeedback.CurrentPose == DogReadabilityFeedback.Pose.Proud);

            // Tug completes when both dogs coordinate on the rope.
            game.Restart();
            for (int i = 0; i < 3; i++)
            {
                Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0].CollectBy(cheddar);
                yield return null;
            }
            game.RopeObject.transform.position = Vector3.zero;
            cheddar.transform.position = Vector3.left * 4f;
            cocoa.transform.position = Vector3.right * 4f;
            yield return null;
            Assert.That(game.ObjectiveArrows[0].Label, Does.Contain("BOTH TUG"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Both dogs tug"));
            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = Vector3.right * 4f;
            yield return null;
            Assert.AreEqual(GameManager.FeedbackKind.TugNeedsPartner, game.LastFeedback);
            Assert.That(game.RopeObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("WAITING"));
            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = Vector3.right * 0.5f;
            guard = 0f;
            while (!game.TugComplete && guard < 4f) { guard += Time.deltaTime; yield return null; }
            Assert.IsTrue(game.TugComplete);
            Assert.AreEqual(200, game.LastScoreDelta);
            Assert.AreEqual("+200 TUG COMPLETE", game.LastScoreEventLabel);
            Assert.AreEqual(GameManager.FeedbackKind.TugTogether, game.LastFeedback);
            Assert.AreEqual(GameManager.JuiceFeedbackKind.SuccessPop, game.LastJuiceFeedback);
            Assert.That(game.LastJuiceLabel, Does.Contain("TUG POP"));
            Assert.That(game.RopeObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("COMPLETE"));
            Assert.IsTrue(cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Tug ||
                          cocoaFeedback.CurrentPose == DogReadabilityFeedback.Pose.Tug);

            // Level clear requires food, tug, and predator resolution.
            game.ForcePredatorWarning();
            cheddar.Bark(); cocoa.Bark();
            guard = 0f;
            while (game.BreakfastRecovered < game.BreakfastGoal && guard < 5f)
            {
                var t = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
                t.CollectBy(cheddar);
                guard += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(GameManager.State.LevelClear, game.Phase);
            Assert.GreaterOrEqual(game.StarRating, 1);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.That(game.ReplayPromptLabel, Does.Contain("replay"));
            Assert.AreEqual("Pawfect Yard", game.EndRank);
            Assert.That(game.EndSummaryLabel, Does.Contain("Clear"));
            Assert.That(game.EndSummaryLabel, Does.Contain(game.Score.ToString()));
            Assert.That(game.EndSummaryLabel, Does.Contain(game.EndRank));
            Assert.That(game.EndReasonLabel, Does.Contain("Tiny legends"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Backyard saved"));
            Assert.That(game.LastScoreEventLabel, Does.Contain("LEVEL CLEAR"));
            Assert.AreEqual(GameManager.FeedbackKind.LevelClear, game.LastFeedback);
            Assert.That(game.MissionBanner, Does.Contain("BACKYARD SAVED"));
            yield return null;
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, cheddarFeedback.CurrentPose);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, cocoaFeedback.CurrentPose);

            game.Restart();
            game.ForceGameOver();
            Assert.IsTrue(game.IsGameOver);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(-100, game.LastScoreDelta);
            Assert.AreEqual("-100 GAME OVER", game.LastScoreEventLabel);
            Assert.AreEqual("Needs More Bark", game.EndRank);
            Assert.That(game.EndSummaryLabel, Does.Contain("Failed"));
            Assert.That(game.EndReasonLabel, Does.Contain("Needs more bark"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Mission failed"));
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.That(game.ReplayPromptLabel, Does.Contain("replay"));
            Assert.AreEqual(GameManager.FeedbackKind.GameOver, game.LastFeedback);
            Assert.That(game.MissionBanner, Does.Contain("MISSION FAILED"));
            yield return null;
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, cheddarFeedback.CurrentPose);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, cocoaFeedback.CurrentPose);
            game.Restart();
            Assert.AreEqual(GameManager.State.Playing, game.Phase);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.IsFalse(game.ReplayPromptVisible);
            Assert.IsEmpty(game.EndSummaryLabel);
            Assert.IsEmpty(game.EndReasonLabel);
            Assert.That(game.ObjectiveLabel, Does.Contain("Save weenies"));
        }

        [UnityTest]
        public IEnumerator SnackHeist_Initializes_Scores_Clears_Fails_AndReplays()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cheddar = FindDog(DogId.Cheddar);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.SnackHeist, game.ActiveMissionVariant);
            Assert.AreEqual("Snack Heist", game.ActiveMissionName);
            Assert.That(game.MissionIntroPrompt, Does.Contain("forbidden snack stash"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Stash snacks"));
            Assert.IsTrue(game.SquirrelObject.activeSelf);
            Assert.IsFalse(game.PredatorObject.activeSelf);
            Assert.IsFalse(game.RopeObject.activeSelf);

            var firstSnack = FirstTreat();
            Assert.IsNotNull(firstSnack.transform.Find("SnackCrumbA"));
            firstSnack.CollectBy(cheddar);
            Assert.AreEqual(60, game.LastScoreDelta);
            Assert.AreEqual("+60 SNACK STASHED", game.LastScoreEventLabel);
            Assert.That(game.ObjectiveLabel, Does.Contain("Stash snacks 1/4"));

            while (game.BreakfastRecovered < game.BreakfastGoal)
            {
                FirstTreat().CollectBy(cheddar);
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.That(game.ReplayPromptLabel, Does.Contain("Snack Heist"));
            Assert.That(game.MissionBanner, Does.Contain("SNACK STASH SAVED"));
            Assert.That(game.LastScoreEventLabel, Does.Contain("SNACK HEIST CLEAR"));

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            int stolenBefore = game.StolenFood;
            ForceOneSquirrelSteal(game);
            yield return WaitForStolenFood(game, stolenBefore + 1);
            Assert.That(game.LastScoreEventLabel, Does.Contain("SNACK THIEF"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Stash snacks"));

            ForceOneSquirrelSteal(game);
            yield return WaitForOutcome(game, GameManager.MissionOutcome.Failed);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.That(game.EndReasonLabel, Does.Contain("forbidden snacks"));
            Assert.That(game.ReplayPromptLabel, Does.Contain("Snack Heist"));
        }

        [UnityTest]
        public IEnumerator SockPanic_Initializes_Scores_Clears_Fails_AndReplays()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(game);
            Assert.IsNotNull(cocoa);

            game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.SockPanic, game.ActiveMissionVariant);
            Assert.AreEqual("Sock Panic", game.ActiveMissionName);
            Assert.That(game.MissionIntroPrompt, Does.Contain("scattered socks"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Return socks"));
            Assert.IsFalse(game.SquirrelObject.activeSelf);
            Assert.IsFalse(game.PredatorObject.activeSelf);
            Assert.IsFalse(game.RopeObject.activeSelf);

            var firstSock = FirstTreat();
            Assert.IsNotNull(firstSock.transform.Find("SockStripeA"));
            firstSock.CollectBy(cocoa);
            Assert.AreEqual(40, game.LastScoreDelta);
            Assert.AreEqual("+40 SOCK RESCUED", game.LastScoreEventLabel);
            Assert.That(game.ObjectiveLabel, Does.Contain("Return socks 1/5"));

            while (game.BreakfastRecovered < game.BreakfastGoal)
            {
                FirstTreat().CollectBy(cocoa);
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.That(game.ReplayPromptLabel, Does.Contain("Sock Panic"));
            Assert.That(game.MissionBanner, Does.Contain("SOCKS SORTED"));
            Assert.That(game.LastScoreEventLabel, Does.Contain("SOCK PANIC CLEAR"));

            game.StartMission(GameManager.MissionVariant.SockPanic);
            game.SetRoundDuration(0.02f);
            yield return WaitForOutcome(game, GameManager.MissionOutcome.Failed);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.IsTrue(game.ReplayPromptVisible);
            Assert.That(game.EndReasonLabel, Does.Contain("Laundry order returned"));
            Assert.That(game.ObjectiveLabel, Does.Contain("Sock Panic"));
        }

        private static DogController FindDog(DogId dogId)
        {
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == dogId) return id.GetComponent<DogController>();
            }

            return null;
        }

        private static Treat FirstTreat()
        {
            var treats = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
            Assert.Greater(treats.Length, 0);
            return treats[0];
        }

        private static void ForceOneSquirrelSteal(GameManager game)
        {
            var target = FirstTreat();
            game.SquirrelObject.transform.position = target.transform.position;
            game.ForceSquirrelStealAttempt();
        }

        private static IEnumerator WaitForStolenFood(GameManager game, int target)
        {
            float guard = 0f;
            while (game.StolenFood < target && guard < 2f)
            {
                guard += Time.deltaTime;
                yield return null;
            }

            Assert.GreaterOrEqual(game.StolenFood, target);
        }

        private static IEnumerator WaitForOutcome(GameManager game, GameManager.MissionOutcome outcome)
        {
            float guard = 0f;
            while (game.Outcome != outcome && guard < 2f)
            {
                guard += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(outcome, game.Outcome);
        }

        private static bool FindWorldPopContaining(string text)
        {
            foreach (var pop in Object.FindObjectsByType<MissionWorldPop>(FindObjectsSortMode.None))
            {
                if (pop.Label.Contains(text)) return true;
            }

            return false;
        }
    }
}
