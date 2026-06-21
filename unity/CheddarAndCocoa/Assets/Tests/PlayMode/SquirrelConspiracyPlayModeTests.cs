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
        public IEnumerator SquirrelConspiracy_AppearsInSelectWithDeterministicRouteAndCutoffs()
        {
            yield return LoadArena();

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
                found |= _game.MissionVariantAt(i) == GameManager.MissionVariant.SquirrelConspiracy;

            Assert.IsTrue(found);
            Assert.AreEqual(4, _game.SquirrelRouteNodes.Length);
            Assert.AreEqual(_game.SquirrelRouteNodes.Length, _game.SquirrelCutoffZones.Length);
            Assert.AreNotEqual(_game.SquirrelRouteNodes[0], _game.SquirrelCutoffZones[0]);
            Assert.IsTrue(_game.DemoReadiness.Ready);
            Assert.That(_game.DemoReadinessLabel, Does.Contain("READY"));
        }

        [UnityTest]
        public IEnumerator SquirrelConspiracy_HerdCutoffAndEarlyBarkHaveDistinctResults()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.SquirrelConspiracy);
            yield return null;

            Assert.IsInstanceOf<SquirrelConspiracyMissionController>(_game.ActiveMissionController);
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("BARK HERD"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("HOLD CUTOFF"));

            _cheddar.transform.position = _game.SquirrelObject.transform.position + Vector3.right * 20f;
            _game.ForceSquirrelConspiracyHerd(DogId.Cheddar);
            Assert.AreEqual(1, _game.SquirrelConspiracyState.FakeOuts);
            Assert.AreEqual(ScoreEventCatalog.FakeOut.Points, _game.Score);
            Assert.That(_game.LastScoreEventLabel, Does.Contain(ScoreEventCatalog.FakeOut.Label));

            _cheddar.transform.position = _game.SquirrelObject.transform.position;
            _cocoa.transform.position = _game.SquirrelObject.transform.position;
            _game.ForceSquirrelConspiracyHerd(DogId.Cheddar);
            Assert.AreEqual(1, _game.SquirrelConspiracyState.Herds);
            Assert.That(_game.LastScoreEventLabel, Does.Contain(ScoreEventCatalog.GoodHerd.Label));

            _cheddar.transform.position = _game.SquirrelObject.transform.position;
            _cocoa.transform.position = _game.ActiveSquirrelCutoffZone;
            _game.ForceSquirrelConspiracyHerd(DogId.Cheddar);
            Assert.AreEqual(1, _game.SquirrelConspiracyState.Cutoffs);
            Assert.That(_game.LastScoreEventLabel, Does.Contain(ScoreEventCatalog.Cutoff.Label));
        }

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
                cheddar.transform.position = game.SquirrelObject.transform.position;
                cocoa.transform.position = game.ActiveSquirrelCutoffZone;
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
            int originalSeed = game.CurrentMissionSeed;
            var originalModifier = game.ActiveModifier;
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
            Assert.AreEqual(game.SquirrelCutoffZones[0], game.ActiveSquirrelCutoffZone);
            Assert.IsNotNull(GameObject.Find("SquirrelCutoff_0"), "Replay should reactivate the first cutoff zone.");
            Assert.AreEqual(originalSeed, game.CurrentMissionSeed, "Replay must preserve the deterministic mission seed.");
            Assert.AreEqual(originalModifier, game.ActiveModifier, "Replay should reproduce the same seeded modifier.");
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
