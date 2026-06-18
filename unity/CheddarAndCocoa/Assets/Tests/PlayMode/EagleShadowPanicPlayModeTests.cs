using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class EagleShadowPanicPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator EagleShadowPanic_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(12, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.EagleShadowPanic)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Eagle Shadow Panic should be reachable from mission select.");
            Assert.AreEqual("Eagle Shadow Panic", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_ClearPath_HideRescueUnitedFront()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.EagleShadowPanic, game.ActiveMissionVariant);
            Assert.AreEqual("Eagle Shadow Panic", game.ActiveMissionName);
            Assert.AreEqual("eagle_shadow_panic", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Hide from the eagle shadow"));

            int scoreBeforeHides = game.Score;
            game.ForceEagleShadowSafeHide();
            game.ForceEagleShadowSafeHide();
            yield return null;

            Assert.AreEqual(2, game.EagleShadowPanicState.SafeHides);
            Assert.AreEqual(0, game.EagleShadowPanicState.Exposures);
            Assert.IsTrue(game.EagleShadowPanicState.RescueObjectiveActive);
            Assert.Greater(game.Score, scoreBeforeHides, "Safe hides should award score, not penalize.");
            Assert.That(game.ObjectiveLabel, Does.Contain("rescue the toy"));

            game.ForceEagleShadowRescue(DogId.Cocoa);
            yield return null;

            Assert.IsTrue(game.EagleShadowPanicState.RescueComplete);
            Assert.That(game.ObjectiveLabel, Does.Contain("United-front"));

            game.ForceEagleShadowUnitedFront();
            yield return null;

            Assert.IsTrue(game.EagleShadowPanicState.UnitedFrontComplete);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, game.CurrentFlow);
            Assert.IsTrue(game.RuntimeSnapshot.IsComplete);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Backyard Defenders"));
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_FailPath_ExposuresEndMission()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;

            game.ForceEagleShadowExposure();
            game.ForceEagleShadowExposure();
            game.ForceEagleShadowExposure();
            yield return null;

            Assert.AreEqual(3, game.EagleShadowPanicState.Exposures);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Shadow Trouble"));
            Assert.That(game.EndReasonLabel, Does.Contain("open"));
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_Replay_ResetsThreatSweepRuntimeState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;
            game.ForceEagleShadowSafeHide();
            game.ForceEagleShadowExposure();
            yield return null;

            Assert.Greater(game.EagleShadowPanicState.SafeHides + game.EagleShadowPanicState.Exposures, 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.EagleShadowPanic, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.EagleShadowPanicState.SafeHides);
            Assert.AreEqual(0, game.EagleShadowPanicState.Exposures);
            Assert.IsFalse(game.EagleShadowPanicState.RescueObjectiveActive);
            Assert.IsFalse(game.EagleShadowPanicState.RescueComplete);
            Assert.IsFalse(game.EagleShadowPanicState.UnitedFrontComplete);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator EagleShadowPanic_SweepGeometry_CoverHidesAndOpenGroundExposes()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.EagleShadowPanic);
            yield return null;

            var zones = game.EagleCoverZones;
            Assert.Greater(zones.Length, 0);

            // Both dogs tucked into a cover zone with the shadow passing over it: a clean safe hide.
            var cover = zones[0];
            _cheddar.transform.position = new Vector3(cover.x, cover.y, 0f);
            _cocoa.transform.position = new Vector3(cover.x, cover.y, 0f);
            game.PredatorObject.transform.position = new Vector3(cover.x, game.PredatorObject.transform.position.y, 0f);
            game.ForceEagleShadowSweepPass();
            yield return null;

            Assert.AreEqual(1, game.EagleShadowPanicState.SafeHides);
            Assert.AreEqual(0, game.EagleShadowPanicState.Exposures);

            // Caught in the open under the shadow column: an exposure.
            _cheddar.transform.position = new Vector3(0f, 0f, 0f);
            _cocoa.transform.position = new Vector3(0f, 0f, 0f);
            game.PredatorObject.transform.position = new Vector3(0f, game.PredatorObject.transform.position.y, 0f);
            game.ForceEagleShadowSweepPass();
            yield return null;

            Assert.AreEqual(1, game.EagleShadowPanicState.Exposures);
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
