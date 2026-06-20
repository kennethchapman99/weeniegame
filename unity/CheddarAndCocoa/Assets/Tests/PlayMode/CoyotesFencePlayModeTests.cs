using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class CoyotesFencePlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator CoyotesFence_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(17, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.CoyotesFence)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Coyotes at the Fence should be reachable from mission select.");
            Assert.AreEqual("Coyotes at the Fence", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator CoyotesFence_ClearPath_BarkPinFillThenBlockFinalPush()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.CoyotesFence, game.ActiveMissionVariant);
            Assert.AreEqual("Coyotes at the Fence", game.ActiveMissionName);
            Assert.AreEqual("coyotes_fence", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Patrol fence gap"));

            // Repair without partner bark pressure should be rejected (no progress).
            game.ForceCoyoteRepair(DogId.Cheddar);
            yield return null;
            Assert.AreEqual(0, game.CoyotesFenceState.GapsRepaired);

            // Bark pressure then fill, three times, to reach the final-push phase.
            for (int i = 0; i < 3; i++)
            {
                game.ForceCoyoteBarkPressure(DogId.Cocoa);
                game.ForceCoyoteRepair(DogId.Cheddar);
                yield return null;
            }

            Assert.AreEqual(3, game.CoyotesFenceState.GapsRepaired);
            Assert.IsTrue(game.CoyotesFenceState.ReadyForFinalPressure(3));
            Assert.That(game.ObjectiveLabel, Does.Contain("final coyote push"));

            game.ForceCoyoteFinalBlock();
            yield return null;

            Assert.IsTrue(game.CoyotesFenceState.FinalPressureComplete);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, game.CurrentFlow);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Fence Guardians"));
        }

        [UnityTest]
        public IEnumerator CoyotesFence_FakeSnackLure_FiresDeterministically()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            Assert.IsFalse(game.CoyotesFenceState.FakeSnackActive);
            game.ForceCoyoteFakeSnack();
            yield return null;

            Assert.IsTrue(game.CoyotesFenceState.FakeSnackActive);
            Assert.That(game.ObjectiveLabel, Does.Contain("fake snack lure"));

            // Barking the coyote resolves the lure instead of taking the bait.
            game.ForceCoyoteBarkPressure(DogId.Cocoa);
            yield return null;
            Assert.IsFalse(game.CoyotesFenceState.FakeSnackActive);
        }

        [UnityTest]
        public IEnumerator CoyotesFence_FailPath_BreachesEndMission()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            game.ForceCoyoteBreach();
            game.ForceCoyoteBreach();
            game.ForceCoyoteBreach();
            yield return null;

            Assert.AreEqual(3, game.CoyotesFenceState.Breaches);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Needs More Patrols"));
            Assert.That(game.EndReasonLabel, Does.Contain("breached"));
        }

        [UnityTest]
        public IEnumerator CoyotesFence_Replay_ResetsPatrolRuntimeState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;
            game.ForceCoyoteBarkPressure(DogId.Cocoa);
            game.ForceCoyoteRepair(DogId.Cheddar);
            game.ForceCoyoteBreach();
            yield return null;

            Assert.Greater(game.CoyotesFenceState.GapsRepaired + game.CoyotesFenceState.Breaches, 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.CoyotesFence, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.CoyotesFenceState.GapsRepaired);
            Assert.AreEqual(0, game.CoyotesFenceState.Breaches);
            Assert.AreEqual(0, game.CoyotesFenceState.BarkPressures);
            Assert.IsFalse(game.CoyotesFenceState.FakeSnackActive);
            Assert.IsFalse(game.CoyotesFenceState.FinalPressureComplete);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator CoyotesFence_ProwlReach_BarkPressureDrivesOffElseBreaches()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            Assert.Greater(game.FenceGaps.Length, 0);

            // Holding bark pressure when the coyote reaches the gap drives it off (no breach).
            game.ForceCoyoteBarkPressure(DogId.Cocoa);
            game.ForceCoyoteProwlReach();
            yield return null;
            Assert.AreEqual(0, game.CoyotesFenceState.Breaches);

            // Reaching an unguarded gap (pressure already spent) breaches the fence.
            game.ForceCoyoteProwlReach();
            yield return null;
            Assert.AreEqual(1, game.CoyotesFenceState.Breaches);
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
