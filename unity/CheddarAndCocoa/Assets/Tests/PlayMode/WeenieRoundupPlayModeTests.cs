using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class WeenieRoundupPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator WeenieRoundup_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(14, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.WeenieRoundup)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Weenie Roundup should be reachable from mission select.");
            Assert.AreEqual("Weenie Roundup", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator WeenieRoundup_ClearPath_CarryEveryWeenieToTheBowl()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return null;

            Assert.AreEqual("weenie_roundup", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Round up"));
            int required = game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(required, 0);
            Assert.AreEqual(required, game.WeenieRoundupState.Loose);

            // Two dogs ferry weenies in parallel until the bowl is full.
            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 50)
            {
                game.ForceWeeniePickup(DogId.Cheddar);
                game.ForceWeenieDeliver(DogId.Cheddar);
                game.ForceWeeniePickup(DogId.Cocoa);
                game.ForceWeenieDeliver(DogId.Cocoa);
                yield return null;
            }

            Assert.AreEqual(required, game.WeenieRoundupState.Delivered);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, game.CurrentFlow);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Weenie Wranglers"));
        }

        [UnityTest]
        public IEnumerator WeenieRoundup_Drop_ReturnsWeenieToTheYard()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return null;

            int looseAtStart = game.WeenieRoundupState.Loose;
            game.ForceWeeniePickup(DogId.Cheddar);
            yield return null;
            Assert.AreEqual(looseAtStart - 1, game.WeenieRoundupState.Loose);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, game.DogFeedback[0].CurrentPose);
            Assert.IsTrue(HasWorldPop("WEENIE GRABBED"));

            game.ForceWeenieDrop(DogId.Cheddar);
            yield return null;
            Assert.AreEqual(looseAtStart, game.WeenieRoundupState.Loose, "A fumbled weenie returns to the yard.");
            Assert.AreEqual(1, game.WeenieRoundupState.Drops);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, game.DogFeedback[0].CurrentPose);
            Assert.IsTrue(HasWorldPop("FUMBLE"));
            Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, game.LastAudioCueRequested);
        }

        [UnityTest]
        public IEnumerator WeenieRoundup_CarryPosePersistsUntilCargoLeavesDog()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return null;

            _game.ForceWeeniePickup(DogId.Cheddar);
            Assert.IsTrue(_game.DogFeedback[0].IsCarrying);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Carry, _game.DogFeedback[0].CurrentPose);
            Assert.That(_game.DogFeedback[0].AuthoredPoseSpriteName, Does.Contain("cheddar_carry_e_"));
            Assert.AreEqual("Carry", _game.DogFeedback[0].MotionClipLabel);

            _game.ForceWeenieDrop(DogId.Cheddar);
            yield return null;
            Assert.IsFalse(_game.DogFeedback[0].IsCarrying);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, _game.DogFeedback[0].CurrentPose);
        }

        [UnityTest]
        public IEnumerator WeenieRoundup_Replay_ResetsCarryRuntimeState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return null;
            game.ForceWeeniePickup(DogId.Cheddar);
            game.ForceWeenieDeliver(DogId.Cheddar);
            yield return null;

            Assert.AreEqual(1, game.WeenieRoundupState.Delivered);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.WeenieRoundup, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.WeenieRoundupState.Delivered);
            Assert.AreEqual(0, game.WeenieRoundupState.Drops);
            Assert.IsFalse(game.DogFeedback[0].IsCarrying);
            Assert.Greater(game.WeenieRoundupState.Loose, 0);
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

        private static bool HasWorldPop(string text)
        {
            foreach (var pop in Object.FindObjectsByType<MissionWorldPop>(FindObjectsSortMode.None))
                if (pop.Label.Contains(text)) return true;
            return false;
        }
    }
}
