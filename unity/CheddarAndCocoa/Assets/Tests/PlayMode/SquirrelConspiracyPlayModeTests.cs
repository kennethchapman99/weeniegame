using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class SquirrelConspiracyPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;
        [UnityTest]
        public IEnumerator SquirrelConspiracy_ClearPath_RevealsFindsStashAndSummarizesOutcome()
        {
            yield return LoadArena();
            var game = _game;
            var cheddar = _cheddar;
            var cocoa = _cocoa;

            game.StartMission(GameManager.MissionVariant.SquirrelConspiracy);
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.SquirrelConspiracy, game.ActiveMissionVariant);
            Assert.AreEqual("squirrel_conspiracy", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Herd squirrel route"));

            for (int i = 0; i < 4; i++)
            {
                cheddar.transform.position = game.SquirrelObject.transform.position + Vector3.left;
                cocoa.transform.position = game.SquirrelObject.transform.position + Vector3.right * 2.6f;
                game.ForceSquirrelConspiracyHerd(DogId.Cheddar);
                yield return null;
            }

            Assert.IsTrue(game.SquirrelConspiracyState.StashRevealed);
            Assert.AreEqual(4, game.SquirrelConspiracyState.ControlCount);
            Assert.That(game.ObjectiveLabel, Does.Contain("Sniff"));

            game.ForceSquirrelConspiracyFindStash(DogId.Cocoa);
            yield return null;

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, game.CurrentFlow);
            Assert.IsTrue(game.RuntimeSnapshot.IsComplete);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Conspiracy Cracked"));
            Assert.That(game.EndReasonLabel, Does.Contain("squirrel"));
        }

        [UnityTest]
        public IEnumerator SquirrelConspiracy_FailPath_TauntsEndMission()
        {
            yield return LoadArena();
            var game = _game;
            var cheddar = _cheddar;
            var cocoa = _cocoa;

            game.StartMission(GameManager.MissionVariant.SquirrelConspiracy);
            yield return null;

            game.ForceSquirrelConspiracyTaunt();
            game.ForceSquirrelConspiracyTaunt();
            game.ForceSquirrelConspiracyTaunt();
            yield return null;

            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.AreEqual(3, game.SquirrelConspiracyState.Taunts);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Outplayed By A Rodent"));
            Assert.That(game.EndReasonLabel, Does.Contain("taunted"));
        }

        [UnityTest]
        public IEnumerator SquirrelConspiracy_Replay_ResetsHerdingRuntimeState()
        {
            yield return LoadArena();
            var game = _game;
            var cheddar = _cheddar;
            var cocoa = _cocoa;

            game.StartMission(GameManager.MissionVariant.SquirrelConspiracy);
            yield return null;
            game.ForceSquirrelConspiracyHerd(DogId.Cheddar);
            game.ForceSquirrelConspiracyTaunt();
            yield return null;

            Assert.Greater(game.SquirrelConspiracyState.ControlCount + game.SquirrelConspiracyState.Taunts, 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.SquirrelConspiracy, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.SquirrelConspiracyState.ControlCount);
            Assert.AreEqual(0, game.SquirrelConspiracyState.Taunts);
            Assert.IsFalse(game.SquirrelConspiracyState.StashRevealed);
            Assert.IsFalse(game.SquirrelConspiracyState.StashFound);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        private IEnumerator LoadArena()
        {
            _game = null;
            _cheddar = null;
            _cocoa = null;
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            _game = Object.FindFirstObjectByType<GameManager>();
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) _cheddar = id.GetComponent<DogController>();
                if (id.Id == DogId.Cocoa) _cocoa = id.GetComponent<DogController>();
            }

            Assert.IsNotNull(_game);
            Assert.IsNotNull(_cheddar);
            Assert.IsNotNull(_cocoa);
        }
    }
}
