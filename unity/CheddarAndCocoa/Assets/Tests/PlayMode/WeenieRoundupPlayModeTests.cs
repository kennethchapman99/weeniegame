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

            Assert.AreEqual(24, game.MissionSelectOptionCount);

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
        public IEnumerator WeenieRoundup_ClearPath_EndsWithSteadyJumboTeamHaulAndLivePayoff()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return null;

            Assert.IsInstanceOf<WeenieRoundupMissionController>(game.ActiveMissionController,
                "Weenie Roundup must run entirely through its own IMissionController.");
            Assert.AreEqual("weenie_roundup", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Round up"));
            int required = game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(required, 0);
            Assert.AreEqual(required, game.WeenieRoundupState.Loose);

            // The first four stay the fast, parallel carry loop.
            for (int i = 0; i < required - 1; i++)
            {
                DogId carrier = i % 2 == 0 ? DogId.Cheddar : DogId.Cocoa;
                game.ForceWeeniePickup(carrier);
                game.ForceWeenieDeliver(carrier);
            }
            yield return null;

            Assert.AreEqual(required - 1, game.WeenieRoundupState.Delivered);
            Assert.That(game.ObjectiveLabel, Does.Contain("FINAL JUMBO"));

            Vector2 jumbo = WeenieRoundupMissionController.ComputeSpots(game.ArenaBounds)[required - 1];
            _cheddar.transform.position = jumbo;
            _cocoa.transform.position = jumbo + Vector2.right;
            yield return null;

            Assert.AreEqual(0, game.WeenieRoundupState.Loose,
                "Cheddar should lift the jumbo only once Cocoa is beside it to steady.");
            Assert.That(game.ObjectiveLabel, Does.Contain("JUMBO HAUL"));
            Assert.IsTrue(HasWorldPop("TEAM JUMBO"));

            Vector2 bowl = ((WeenieRoundupMissionController)game.ActiveMissionController).BowlPosition;
            _cheddar.transform.position = bowl;
            _cocoa.transform.position = bowl + Vector2.left;
            yield return null;

            Assert.AreEqual(required, game.WeenieRoundupState.Delivered);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome,
                "The full bowl should remain live briefly before the end screen takes over.");
            Assert.IsTrue(((WeenieRoundupMissionController)game.ActiveMissionController).IsPresentingSuccessfulOutcome);
            Assert.IsTrue(HasWorldPop("BOWL FULL"));
            foreach (var feedback in game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose,
                    "Both dogs (the hauler and the steadying partner) should pose proud during the held payoff.");

            ((WeenieRoundupMissionController)game.ActiveMissionController).ForceFinishSuccessPresentation();
            yield return null;

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, game.CurrentFlow);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Weenie Wranglers"));
        }

        [UnityTest]
        public IEnumerator WeenieRoundup_JumboRejectsSoloGrab_AndSeparationFumblesRecoverably()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return null;

            Vector2 jumbo = WeenieRoundupMissionController.ComputeSpots(_game.ArenaBounds)[4];
            _cheddar.transform.position = jumbo;
            _cocoa.transform.position = jumbo + Vector2.right;
            yield return null;
            Assert.AreEqual(5, _game.WeenieRoundupState.Loose,
                "The jumbo is the climax and must stay locked until all four small carries are banked.");

            _cheddar.transform.position = _game.ArenaBounds.center;
            _cocoa.transform.position = _game.ArenaBounds.center + Vector2.right;
            for (int i = 0; i < 4; i++)
            {
                DogId carrier = i % 2 == 0 ? DogId.Cheddar : DogId.Cocoa;
                _game.ForceWeeniePickup(carrier);
                _game.ForceWeenieDeliver(carrier);
            }
            yield return null;

            _cheddar.transform.position = jumbo;
            _cocoa.transform.position = jumbo + Vector2.left * 8f;
            yield return null;

            Assert.AreEqual(1, _game.WeenieRoundupState.Loose,
                "Cheddar cannot turn the jumbo into another solo fetch errand.");
            Assert.That(_game.LastCue, Does.Contain("Cocoa"));
            Assert.That(_game.LastJuiceLabel, Does.Contain("STEADY"), "The solo-jumbo attempt must produce a visible coach beat.");
            Assert.IsTrue(HasWorldPop("WOBBLY"), "The solo-jumbo attempt must produce a visible world pop.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "The solo-jumbo attempt must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _cocoa.transform.position = jumbo + Vector2.right;
            yield return null;
            Assert.AreEqual(0, _game.WeenieRoundupState.Loose);

            _cocoa.transform.position = jumbo + Vector2.left * 8f;
            yield return new WaitForSeconds(0.95f);

            Assert.AreEqual(1, _game.WeenieRoundupState.Loose,
                "A separated jumbo should bounce back into the yard for an immediate retry.");
            Assert.AreEqual(1, _game.WeenieRoundupState.Drops);
            Assert.That(_game.LastCue, Does.Contain("try the team haul again"));
            Assert.IsTrue(HasWorldPop("FUMBLE"));
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
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
