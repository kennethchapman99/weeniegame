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
            Assert.IsNotNull(game.SquirrelObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.PredatorObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.RopeObject.GetComponent<MissionActorFeedback>());
            Assert.IsNotNull(game.GetComponent<AudioSource>());

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

            // Squirrel pressure: it can steal if ignored after being placed on food.
            var target = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
            game.SquirrelObject.transform.position = target.transform.position;
            float guard = 0f;
            while (game.StolenFood == 0 && guard < 4f) { guard += Time.deltaTime; yield return null; }
            Assert.GreaterOrEqual(game.StolenFood, 1, "Squirrel should eventually steal breakfast.");

            // Bark near the squirrel should scare it and reward a small score bump.
            target = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
            game.SquirrelObject.transform.position = target.transform.position;
            cheddar.transform.position = game.SquirrelObject.transform.position;
            scoreBefore = game.Score;
            cheddar.Bark();
            Assert.Greater(game.Score, scoreBefore, "Barking near squirrel should affect game state.");
            Assert.That(game.LastCue, Does.Contain("squirrel").IgnoreCase);

            // Predator warning/attack can be resolved by united bark.
            game.ForcePredatorWarning();
            Assert.AreEqual(GameManager.State.PredatorWarning, game.Phase);
            cocoa.transform.position = cheddar.transform.position + Vector3.right;
            cheddar.Bark(); cocoa.Bark();
            Assert.IsTrue(game.PredatorResolved);
            Assert.That(game.LastCue, Does.Contain("predator").IgnoreCase);

            // Failed predator attack stuns/grabs, then the partner rescues by coming close and barking.
            game.Restart();
            game.ForcePredatorAttack();
            Assert.AreEqual(GameManager.State.PredatorAttack, game.Phase);
            Assert.IsTrue(game.AnyDogGrabbed);
            Assert.IsTrue(cheddar.Mode == MovementMode.Stunned || cocoa.Mode == MovementMode.Stunned);
            cheddar.transform.position = cocoa.transform.position;
            cheddar.Bark(); cocoa.Bark();
            Assert.IsFalse(game.AnyDogGrabbed, "Partner bark should rescue grabbed dog.");

            // Tug completes when both dogs coordinate on the rope.
            game.RopeObject.transform.position = Vector3.zero;
            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = Vector3.right * 0.5f;
            guard = 0f;
            while (!game.TugComplete && guard < 4f) { guard += Time.deltaTime; yield return null; }
            Assert.IsTrue(game.TugComplete);
            Assert.That(game.RopeObject.GetComponent<MissionActorFeedback>().Label, Does.Contain("COMPLETE"));

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

            game.Restart();
            game.ForceGameOver();
            Assert.IsTrue(game.IsGameOver);
            game.Restart();
            Assert.AreEqual(GameManager.State.Playing, game.Phase);
            Assert.AreEqual(0, game.Score);
        }
    }
}
