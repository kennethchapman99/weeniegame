using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class LeashWalkPlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator LeashWalk_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(21, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.LeashWalk) { found = true; break; }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Walkies on the Leash should be reachable from mission select.");
            Assert.AreEqual("Walkies on the Leash", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator LeashWalk_ClearPath_ReachEveryCheckpointTogether()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.LeashWalk);
            yield return null;

            Assert.AreEqual("leash_walk", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Walk the leash"));
            int required = game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(required, 0);

            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 30)
            {
                game.ForceReachCheckpoint();
                yield return null;
            }

            Assert.AreEqual(required, game.LeashWalkState.Reached);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Best Walk Ever"));
        }

        [UnityTest]
        public IEnumerator LeashWalk_FailPath_TooManyLeashSnaps()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.LeashWalk);
            yield return null;

            for (int i = 0; i < 4; i++)
            {
                game.ForceLeashSnap();
                yield return null;
            }

            Assert.AreEqual(4, game.LeashWalkState.Snaps);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Tangled Leash"));
        }

        [UnityTest]
        public IEnumerator LeashWalk_Replay_ResetsWalkState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.LeashWalk);
            yield return null;
            game.ForceReachCheckpoint();
            game.ForceLeashSnap();
            yield return null;
            Assert.Greater(game.LeashWalkState.Reached + game.LeashWalkState.Snaps, 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.LeashWalk, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.LeashWalkState.Reached);
            Assert.AreEqual(0, game.LeashWalkState.Snaps);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator LeashWalk_CheckpointAndSnap_HaveReadableDogReactions()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.LeashWalk);
            yield return null;

            _game.ForceReachCheckpoint();
            yield return null;
            Assert.IsTrue(HasWorldPop("CHECKPOINT"));
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose);

            _game.ForceLeashSnap();
            yield return null;
            Assert.IsTrue(HasWorldPop("LEASH SNAP"));
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, feedback.CurrentPose);
            Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, _game.LastAudioCueRequested);
        }

        private static bool HasWorldPop(string text)
        {
            foreach (var pop in Object.FindObjectsByType<MissionWorldPop>(FindObjectsSortMode.None))
                if (pop.Label.Contains(text)) return true;
            return false;
        }

        private IEnumerator LoadArena()
        {
            _game = null;
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
        }
    }
}
