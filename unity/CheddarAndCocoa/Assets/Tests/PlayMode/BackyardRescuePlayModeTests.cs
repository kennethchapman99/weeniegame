using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class BackyardRescuePlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator RunsThroughDedicatedController()
        {
            yield return Load();
            Assert.IsNotNull(_game.ActiveMissionController);
            Assert.IsInstanceOf<BackyardRescueMissionController>(_game.ActiveMissionController);
            Assert.AreEqual(GameManager.MissionVariant.BackyardRescue, _game.ActiveMissionController.Variant);
        }

        [UnityTest]
        public IEnumerator SquirrelTrap_TwoPassRoleReversal_ScoresAndCompletes()
        {
            yield return Load();

            Assert.AreEqual(DogId.Cheddar, _game.BackyardTrapState.PressureDog);
            Assert.AreEqual(DogId.Cocoa, _game.BackyardTrapState.GapDog);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cheddar"));
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa"));

            // Wrong pressure dog: fumble
            _game.ForceBackyardTrapRedirect(DogId.Cocoa, true);
            Assert.AreEqual(1, _game.BackyardTrapState.Fumbles);
            Assert.IsFalse(_game.BackyardTrapState.WeenieDropped);

            // Correct pressure dog, gap open: fumble
            _game.ForceBackyardTrapRedirect(DogId.Cheddar, false);
            Assert.AreEqual(2, _game.BackyardTrapState.Fumbles);
            Assert.IsFalse(_game.BackyardTrapState.WeenieDropped);

            // Correct pressure + gap held: redirect succeeds
            int scoreBefore = _game.Score;
            _game.ForceBackyardTrapRedirect(DogId.Cheddar, true);
            yield return null;
            Assert.Greater(_game.Score, scoreBefore);
            Assert.IsTrue(_game.BackyardTrapState.WeenieDropped);
            Assert.IsNotNull(_game.BackyardDroppedWeenie);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa: recover"));

            // Wrong dog recovery: fumble (not complete)
            _game.ForceBackyardTrapRecovery(DogId.Cheddar);
            Assert.AreEqual(3, _game.BackyardTrapState.Fumbles);
            Assert.IsTrue(_game.BackyardTrapState.WeenieDropped);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            // Correct dog recovery: pass 1 complete, roles reverse
            _game.ForceBackyardTrapRecovery(DogId.Cocoa);
            yield return null;
            Assert.AreEqual(1, _game.BackyardTrapState.Recoveries);
            Assert.AreEqual(DogId.Cocoa, _game.BackyardTrapState.PressureDog);
            Assert.AreEqual(DogId.Cheddar, _game.BackyardTrapState.GapDog);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa pressures"));
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cheddar holds"));

            // Pass 2: Cocoa pressures, Cheddar holds gap
            _game.ForceBackyardTrapRedirect(DogId.Cocoa, true);
            _game.ForceBackyardTrapRecovery(DogId.Cheddar);
            yield return null;
            Assert.IsTrue(_game.BackyardTrapState.Complete);
            Assert.AreEqual(2, _game.BackyardTrapState.Redirects);
            Assert.AreEqual(2, _game.BackyardTrapState.Recoveries);
            Assert.IsTrue(LogContains("SquirrelTrapRedirect"));
            Assert.IsTrue(LogContains("SquirrelTrapRecovery"));
        }

        [UnityTest]
        public IEnumerator SquirrelTrap_UsesReadableBackyardStateArt()
        {
            yield return Load();

            AssertMissionArt("BackyardSquirrelTrapEscapeGap", FinalGameplayArt.BackyardTrapGapOpen);

            _game.ForceBackyardTrapRedirect(DogId.Cocoa, true);
            yield return null;
            AssertMissionArt("BackyardSquirrelTrapEscapeGap", FinalGameplayArt.BackyardTrapGapFakeRoute);

            _cocoa.transform.position = _game.BackyardTrapGapPosition;
            yield return new WaitForSeconds(1.25f);
            AssertMissionArt("BackyardSquirrelTrapEscapeGap", FinalGameplayArt.BackyardTrapGapHeld);

            _game.ForceBackyardTrapRedirect(DogId.Cheddar, true);
            yield return null;
            Assert.IsNotNull(_game.BackyardDroppedWeenie);
            AssertMissionArt(_game.BackyardDroppedWeenie.gameObject, FinalGameplayArt.BackyardWeenieDropped);
        }

        [UnityTest]
        public IEnumerator SquirrelStealing_ScoresPenaltyAndTracksStolen()
        {
            yield return Load();
            Assert.AreEqual(0, _game.StolenFood);

            var treat = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None)[0];
            _game.SquirrelObject.transform.position = treat.transform.position + Vector3.right;
            _game.ForceSquirrelStealAttempt();
            Assert.That(_game.ObjectiveLabel, Does.Contain("Bark to scare squirrel"));
            Assert.AreEqual(GameManager.FeedbackKind.SquirrelStealing, _game.LastFeedback);

            _game.SquirrelObject.transform.position = treat.transform.position;
            float guard = 0f;
            while (_game.StolenFood == 0 && guard < 4f) { guard += Time.deltaTime; yield return null; }
            Assert.GreaterOrEqual(_game.StolenFood, 1);
            Assert.Less(_game.Score, 0);
            Assert.That(_game.LastScoreEventLabel, Does.Contain("SQUIRREL GOT ONE"));
        }

        [UnityTest]
        public IEnumerator Replay_ResetsAllState()
        {
            yield return Load();

            _game.ForceBackyardTrapRedirect(DogId.Cheddar, true);
            _game.ForceBackyardTrapRecovery(DogId.Cocoa);
            Assert.AreEqual(1, _game.BackyardTrapState.Recoveries);

            _game.Restart();
            yield return null;

            var ctrl = _game.BackyardRescueController;
            Assert.IsNotNull(ctrl);
            Assert.AreEqual(0, ctrl.TrapState.Recoveries);
            Assert.AreEqual(0, ctrl.TrapState.Redirects);
            Assert.AreEqual(0, ctrl.TrapState.Fumbles);
            Assert.IsFalse(ctrl.TrapState.WeenieDropped);
            Assert.IsNull(ctrl.DroppedWeenie);
            Assert.AreEqual(0, ctrl.Collected);
            Assert.AreEqual(DogId.Cheddar, ctrl.TrapState.PressureDog);
        }

        [UnityTest]
        public IEnumerator ClearPath_FoodTrapTugPredator_AllRequired()
        {
            yield return Load();

            // Complete the trap (both passes)
            _game.ForceBackyardTrapRedirect(DogId.Cheddar, true);
            _game.ForceBackyardTrapRecovery(DogId.Cocoa);
            _game.ForceBackyardTrapRedirect(DogId.Cocoa, true);
            _game.ForceBackyardTrapRecovery(DogId.Cheddar);
            Assert.IsTrue(_game.BackyardTrapState.Complete);

            // Resolve predator + tug
            _cheddar.transform.position = _cocoa.transform.position + Vector3.right;
            _game.ForcePredatorWarning();
            _cheddar.Bark(); _cocoa.Bark();
            Assert.IsTrue(_game.PredatorResolved);

            _game.RopeObject.transform.position = Vector3.zero;
            _cheddar.transform.position = Vector3.zero;
            _cocoa.transform.position = Vector3.right * 0.5f;
            float tugGuard = 0f;
            while (!_game.TugComplete && tugGuard < 4f) { tugGuard += Time.deltaTime; yield return null; }
            Assert.IsTrue(_game.TugComplete);

            // Collect remaining food to clear
            float guard = 0f;
            while (_game.BreakfastRecovered < _game.BreakfastGoal && guard < 5f)
            {
                var treats = Object.FindObjectsByType<Treat>(FindObjectsSortMode.None);
                if (treats.Length > 0) treats[0].CollectBy(_cheddar);
                guard += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(GameManager.State.LevelClear, _game.Phase);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.AreEqual("backyard_rescue", _game.RuntimeSnapshot.MissionId);
        }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
            _game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) _cheddar = id.GetComponent<DogController>();
                else if (id.Id == DogId.Cocoa) _cocoa = id.GetComponent<DogController>();
            }
        }

        private bool LogContains(string text)
        {
            foreach (string entry in _game.PlaytestEvents)
                if (entry.Contains(text)) return true;
            return false;
        }

        private static void AssertMissionArt(string objectName, string expectedResourcePath)
        {
            var go = GameObject.Find(objectName);
            Assert.IsNotNull(go, $"Missing object {objectName}.");
            AssertMissionArt(go, expectedResourcePath);
        }

        private static void AssertMissionArt(GameObject go, string expectedResourcePath)
        {
            var art = go.GetComponent<MissionPropArtAttachment>();
            Assert.IsNotNull(art, $"Missing MissionPropArtAttachment on {go.name}.");
            Assert.AreEqual(expectedResourcePath, art.ResourcePath);
            Assert.IsTrue(art.HasRuntimeSprite, $"Expected runtime sprite for {expectedResourcePath}.");
        }
    }
}
