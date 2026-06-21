using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ThunderstormComfortPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator ThunderstormComfort_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(22, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.ThunderstormComfort)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Thunderstorm Comfort should be reachable from mission select.");
            Assert.AreEqual("Thunderstorm Comfort", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_ClearPath_HuddleThroughEveryClap()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            Assert.AreEqual("thunderstorm_comfort", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Huddle"));

            // Keep both dogs huddled together so comfort drains the panic each clap adds.
            _cheddar.transform.position = Vector3.zero;
            _cocoa.transform.position = Vector3.zero;

            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 30)
            {
                _cheddar.transform.position = Vector3.zero;
                _cocoa.transform.position = Vector3.zero;
                game.ForceThunderclap();
                game.ForceComfortStep(2f);
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.ThunderstormState.ReadyToClear());
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Weathered The Storm"));
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_FailPath_PanicMaxesWhenApart()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            // Dogs kept far apart: nothing drains panic, so repeated claps max it out and they bolt.
            _cheddar.transform.position = new Vector3(-12f, 0f, 0f);
            _cocoa.transform.position = new Vector3(12f, 0f, 0f);

            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 12)
            {
                game.ForceThunderclap();
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Spooked By Thunder"));
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_Replay_ResetsStormAndPanic()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;
            game.ForceThunderclap();
            yield return null;

            Assert.Greater(game.ThunderstormState.ClapsSurvived + (game.Panic.CheddarPanic > 0f ? 1 : 0), 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.ThunderstormComfort, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.ThunderstormState.ClapsSurvived);
            Assert.Less(game.Panic.CheddarPanic, 0.1f);
            Assert.Less(game.Panic.CocoaPanic, 0.1f);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_Thunderclap_MakesDogsFlinch()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            // Keep the dogs apart so the huddle-comfort pose doesn't override the flinch.
            _cheddar.transform.position = new Vector3(-12f, 0f, 0f);
            _cocoa.transform.position = new Vector3(12f, 0f, 0f);

            game.ForceThunderclap();
            yield return null;

            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, game.DogFeedback[0].CurrentPose,
                "Dogs should visibly flinch (Sad pose) at a thunderclap.");
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
