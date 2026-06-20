using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ScentSearchPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator ScentSearch_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(15, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.ScentSearch)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Scent Search should be reachable from mission select.");
            Assert.AreEqual("Scent Search", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator ScentSearch_ClearPath_SniffAndDigUpEveryBone()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            Assert.AreEqual("scent_search", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Sniff"));
            int required = game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(required, 0);

            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 30)
            {
                game.ForceScentSniff(DogId.Cheddar);
                game.ForceScentDigCorrect(DogId.Cheddar);
                yield return null;
            }

            Assert.AreEqual(required, game.ScentSearchState.Found);
            Assert.AreEqual(0, game.ScentSearchState.WastedDigs);
            Assert.Greater(game.ScentSearchState.Sniffs, 0);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Master Sniffers"));
        }

        [UnityTest]
        public IEnumerator ScentSearch_FailPath_TooManyColdDigsEndMission()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            for (int i = 0; i < 4; i++)
            {
                game.ForceScentDigWrong(DogId.Cheddar);
                yield return null;
            }

            Assert.AreEqual(4, game.ScentSearchState.WastedDigs);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Dug Up The Whole Yard"));
        }

        [UnityTest]
        public IEnumerator ScentSearch_Replay_ResetsScentRuntimeState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;
            game.ForceScentSniff(DogId.Cheddar);
            game.ForceScentDigCorrect(DogId.Cheddar);
            yield return null;

            Assert.AreEqual(1, game.ScentSearchState.Found);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.ScentSearch, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.ScentSearchState.Found);
            Assert.AreEqual(0, game.ScentSearchState.WastedDigs);
            Assert.AreEqual(0, game.ScentSearchState.Sniffs);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator ScentSearch_DiggingUpABone_ShowsAProudPose()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            game.ForceScentDigCorrect(DogId.Cheddar);
            yield return null;

            Assert.AreEqual(1, game.ScentSearchState.Found);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, game.DogFeedback[0].CurrentPose,
                "Digging up a bone should show a proud pose.");
            Assert.That(game.DogFeedback[0].AuthoredPoseSpriteName, Does.Contain("cheddar_proud_e_"));
            Assert.AreEqual("Proud", game.DogFeedback[0].MotionClipLabel);
        }

        [UnityTest]
        public IEnumerator ScentSearch_ColdDig_ShowsWorriedWarningFeedback()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            _game.ForceScentDigWrong(DogId.Cheddar);
            yield return null;

            Assert.AreEqual(1, _game.ScentSearchState.WastedDigs);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, _game.DogFeedback[0].CurrentPose);
            Assert.That(_game.DogFeedback[0].AuthoredPoseSpriteName, Does.Contain("cheddar_sad_e_"));
            Assert.AreEqual("Sad", _game.DogFeedback[0].MotionClipLabel);
            Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, _game.LastAudioCueRequested);
            Assert.AreEqual("cold_dig", _game.LastRumbleRequested);
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
