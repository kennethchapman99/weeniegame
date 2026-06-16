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
            Assert.AreEqual(GameManager.FeedbackKind.Intro, game.LastFeedback);
            Assert.IsNotNull(game.SquirrelObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.PredatorObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.RopeObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.GetComponent<AudioSource>());
            Assert.IsNotNull(Camera.main.GetComponent<AudioListener>());

            var cheddarFeedback = cheddar.GetComponent<DogReadabilityFeedback>();
            var cocoaFeedback = cocoa.GetComponent<DogReadabilityFeedback>();
            Assert.IsNotNull(cheddarFeedback);
            Assert.IsNotNull(cocoaFeedback);
            Assert.That(cheddarFeedback.IdentityLabel, Does.Contain("CHEDDAR CHAOS PUP"));
            Assert.That(cocoaFeedback.IdentityLabel, Does.Contain("COCOA SPOT QUEEN"));
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

            var treats = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
            Assert.Greater(treats.Length, 0);
            int scoreBefore = game.Score;
            treats[0].CollectBy(cheddar);
            Assert.Greater(game.Score, scoreBefore);
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

            // Squirrel pressure: it can steal if ignored after being placed on food.
            game.Restart();
            yield return null;
            var target = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
            game.SquirrelObject.transform.position = target.transform.position;
            float guard = 0f;
            while (game.StolenFood == 0 && guard < 4f) { guard += Time.deltaTime; yield return null; }
            Assert.GreaterOrEqual(game.StolenFood, 1, "Squirrel should eventually steal breakfast.");
            Assert.AreEqual(GameManager.FeedbackKind.SquirrelStoleFood, game.LastFeedback);
            Assert.That(game.SquirrelObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("GOT A WEENIE"));

            // Bark near the squirrel should scare it and reward a small score bump.
            target = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
            game.SquirrelObject.transform.position = target.transform.position;
            cheddar.transform.position = game.SquirrelObject.transform.position;
            scoreBefore = game.Score;
            cheddar.Bark();
            Assert.Greater(game.Score, scoreBefore, "Barking near squirrel should affect game state.");
            Assert.That(game.LastCue, Does.Contain("squirrel").IgnoreCase);
            Assert.AreEqual(GameManager.FeedbackKind.SquirrelScared, game.LastFeedback);
            Assert.That(game.SquirrelObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("DROPPED"));

            // Predator warning/attack can be resolved by united bark.
            cocoa.transform.position = cheddar.transform.position + Vector3.right * 5f;
            game.ForcePredatorWarning();
            yield return null;
            Assert.AreEqual(GameManager.State.PredatorWarning, game.Phase);
            Assert.AreEqual(GameManager.FeedbackKind.PredatorHuddle, game.LastFeedback);
            Assert.That(game.PredatorObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("HUDDLE"));
            Assert.That(game.ObjectiveArrows[0].Label, Does.Contain("HUDDLE + BARK"));
            cocoa.transform.position = cheddar.transform.position + Vector3.right;
            cheddar.Bark(); cocoa.Bark();
            Assert.IsTrue(game.PredatorResolved);
            Assert.That(game.LastCue, Does.Contain("predator").IgnoreCase);
            Assert.AreEqual(GameManager.FeedbackKind.UnitedBark, game.LastFeedback);

            // Failed predator attack stuns/grabs, then the partner rescues by coming close and barking.
            game.Restart();
            game.ForcePredatorAttack();
            yield return null;
            Assert.AreEqual(GameManager.State.PredatorAttack, game.Phase);
            Assert.IsTrue(game.AnyDogGrabbed);
            Assert.AreEqual(GameManager.FeedbackKind.PredatorAttack, game.LastFeedback);
            Assert.IsTrue(cheddar.Mode == MovementMode.Stunned || cocoa.Mode == MovementMode.Stunned);
            Assert.IsTrue(cheddarFeedback.CurrentPose == DogReadabilityFeedback.Pose.Stunned ||
                          cocoaFeedback.CurrentPose == DogReadabilityFeedback.Pose.Stunned);
            Assert.IsTrue(game.ObjectiveArrows[0].Label.Contains("BARK RESCUE") ||
                          game.ObjectiveArrows[1].Label.Contains("BARK RESCUE"));
            cheddar.transform.position = cocoa.transform.position;
            cheddar.Bark(); cocoa.Bark();
            yield return null;
            Assert.IsFalse(game.AnyDogGrabbed, "Partner bark should rescue grabbed dog.");
            Assert.AreEqual(GameManager.FeedbackKind.PartnerRescue, game.LastFeedback);
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
            Assert.AreEqual(GameManager.FeedbackKind.TugTogether, game.LastFeedback);
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
            Assert.AreEqual(GameManager.FeedbackKind.LevelClear, game.LastFeedback);
            Assert.That(game.MissionBanner, Does.Contain("BACKYARD SAVED"));
            yield return null;
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, cheddarFeedback.CurrentPose);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, cocoaFeedback.CurrentPose);

            game.Restart();
            game.ForceGameOver();
            Assert.IsTrue(game.IsGameOver);
            Assert.AreEqual(GameManager.FeedbackKind.GameOver, game.LastFeedback);
            Assert.That(game.MissionBanner, Does.Contain("MISSION FAILED"));
            yield return null;
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, cheddarFeedback.CurrentPose);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, cocoaFeedback.CurrentPose);
            game.Restart();
            Assert.AreEqual(GameManager.State.Playing, game.Phase);
            Assert.AreEqual(0, game.Score);
        }
    }
}
