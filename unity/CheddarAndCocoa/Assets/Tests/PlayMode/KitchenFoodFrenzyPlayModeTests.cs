using System.Collections;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using CheddarAndCocoa.CameraRig;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CheddarAndCocoa.Tests
{
    public sealed class KitchenFoodFrenzyPlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator KitchenFrenzy_IsSelectableAndBuildsReadableRoleStations()
        {
            yield return LoadKitchen();

            Assert.AreEqual(GameManager.MissionVariant.KitchenFoodFrenzy, _game.ActiveMissionVariant);
            Assert.AreEqual("Kitchen Falling Food Frenzy", _game.ActiveMissionName);
            Assert.IsInstanceOf<KitchenFoodFrenzyMissionController>(_game.ActiveMissionController);
            Assert.AreEqual("kitchen_food_frenzy", _game.RuntimeSnapshot.MissionId);
            Assert.IsNotNull(GameObject.Find("KitchenCounterRoute"));
            Assert.IsNotNull(GameObject.Find("KitchenSafeBowl"));
            Assert.IsNotNull(_game.KitchenTelegraphObject);
            Assert.IsNotNull(_game.KitchenLandingWarningObject);
            Assert.IsFalse(_game.SquirrelObject.activeSelf);
            Assert.IsFalse(_game.PredatorObject.activeSelf);
            Assert.IsFalse(_game.RopeObject.activeSelf);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cheddar"));
            Assert.That(_game.ObjectiveLabel, Does.Contain("COUNTER"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("BARK-KNOCK FOOD"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("GUARD THE BOWL"));

            var cameraRig = Object.FindFirstObjectByType<SharedCameraController>();
            Assert.IsNotNull(cameraRig);
            float kitchenFrame = cameraRig.RequiredOrthoSizeForTargets(
                _game.KitchenCounterPosition, _game.KitchenSafeZonePosition, 16f / 9f);
            Assert.LessOrEqual(kitchenFrame, 12f, "Kitchen stations should stay readable in one couch-co-op frame.");
        }

        [UnityTest]
        public IEnumerator KitchenFrenzy_BarkKnockHasReadablePreDropTelegraph()
        {
            yield return LoadKitchen();

            _game.ForceKitchenTelegraph(DogId.Cheddar, KitchenFoodFrenzyMissionState.FoodKind.Bad);
            Assert.IsTrue(_game.KitchenState.TelegraphActive);
            Assert.IsFalse(_game.KitchenState.DropActive);
            Assert.IsTrue(_game.KitchenTelegraphObject.activeSelf);
            Assert.IsTrue(_game.KitchenLandingWarningObject.activeSelf);
            Assert.IsFalse(_game.KitchenFoodObject.activeSelf);
            Assert.That(_game.ObjectiveLabel, Does.Contain("DROP TELEGRAPHED"));
            Assert.IsTrue(LogContains("KitchenTelegraph"));
            AssertKitchenCueSprites("kitchen_telegraph_purple", "kitchen_landing_purple");

            _game.ForceKitchenReleaseTelegraph();
            Assert.IsFalse(_game.KitchenState.TelegraphActive);
            Assert.IsTrue(_game.KitchenState.DropActive);
            Assert.IsTrue(_game.KitchenFoodObject.activeSelf);
            Assert.IsFalse(_game.KitchenTelegraphObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator KitchenFrenzy_RoleFailuresFoodTypesAndClearPathAreDeterministic()
        {
            yield return LoadKitchen();

            _game.ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind.Good);
            Assert.IsTrue(_game.KitchenState.DropActive);
            Assert.IsTrue(_game.KitchenFoodObject.activeSelf);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa"));

            _game.ForceKitchenCatch(DogId.Cheddar, true);
            Assert.AreEqual(1, _game.KitchenState.RoleFumbles);
            Assert.IsTrue(_game.KitchenState.DropActive, "Wrong catcher must leave the drop recoverable.");
            _game.ForceKitchenCatch(DogId.Cocoa, true);
            Assert.AreEqual(1, _game.KitchenState.GoodCatches);
            Assert.AreEqual(1, _game.KitchenState.Combo);

            _game.ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind.Bad);
            _game.ForceKitchenCatch(DogId.Cocoa, true);
            Assert.AreEqual(1, _game.KitchenState.GrossFumbles);
            Assert.AreEqual(0, _game.KitchenState.Combo);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _game.ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind.Bad);
            _game.ForceKitchenLetFall();
            Assert.IsTrue(LogContains("KitchenDodge"));

            while (!_game.KitchenState.Complete)
            {
                var kind = _game.KitchenState.FinaleActive
                    ? _game.KitchenState.ExpectedFinaleKind
                    : KitchenFoodFrenzyMissionState.FoodKind.Good;
                _game.ForceKitchenDrop(kind);
                if (kind == KitchenFoodFrenzyMissionState.FoodKind.Bad) _game.ForceKitchenLetFall();
                else _game.ForceKitchenCatch(DogId.Cocoa, true);
            }
            yield return null;

            Assert.AreEqual(KitchenFoodFrenzyMissionState.RequiredCatches, _game.KitchenState.GoodCatches);
            Assert.AreEqual(KitchenFoodFrenzyMissionState.FinaleSuccessesRequired, _game.KitchenState.FinaleSuccesses);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, _game.CurrentFlow);
            Assert.That(_game.MissionBanner, Does.Contain("KITCHEN CLEARED"));
            Assert.IsTrue(LogContains("KitchenCatch"));
            Assert.IsTrue(LogContains("KitchenFinaleStarted"));
        }

        [UnityTest]
        public IEnumerator KitchenFrenzy_ReplayResetsFoodAndComboState()
        {
            yield return LoadKitchen();
            _game.ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind.Good);
            _game.ForceKitchenCatch(DogId.Cocoa, true);
            Assert.AreEqual(1, _game.KitchenState.GoodCatches);

            _game.Restart();
            yield return null;

            Assert.AreEqual(0, _game.KitchenState.GoodCatches);
            Assert.AreEqual(0, _game.KitchenState.TotalFumbles);
            Assert.AreEqual(0, _game.KitchenState.Combo);
            Assert.AreEqual(0, _game.KitchenState.FinaleSuccesses);
            Assert.IsFalse(_game.KitchenState.TelegraphActive);
            Assert.IsFalse(_game.KitchenState.DropActive);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator KitchenFrenzy_SwitchingMissionCleansOwnedActorsAndController()
        {
            yield return LoadKitchen();
            _game.ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind.Good);
            Assert.IsTrue(_game.KitchenFoodObject.activeSelf);

            GameObject counter = GameObject.Find("KitchenCounterRoute");
            GameObject bowl = GameObject.Find("KitchenSafeBowl");
            GameObject food = _game.KitchenFoodObject;
            Assert.IsNotNull(counter);
            Assert.IsNotNull(bowl);

            _game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            Assert.IsNotNull(_game.ActiveMissionController);
            Assert.IsInstanceOf<BackyardRescueMissionController>(_game.ActiveMissionController);
            Assert.IsFalse(counter.activeSelf);
            Assert.IsFalse(bowl.activeSelf);
            Assert.IsFalse(food.activeSelf);
        }

        private IEnumerator LoadKitchen()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
            _game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;
        }

        private bool LogContains(string text)
        {
            foreach (string entry in _game.PlaytestEvents)
                if (entry.Contains(text)) return true;
            return false;
        }

        private void AssertKitchenCueSprites(string telegraphName, string landingName)
        {
            var telegraphArt = _game.KitchenTelegraphObject.GetComponent<MissionPropArtAttachment>();
            var landingArt = _game.KitchenLandingWarningObject.GetComponent<MissionPropArtAttachment>();
            Assert.IsNotNull(telegraphArt);
            Assert.IsNotNull(landingArt);
            Assert.IsTrue(telegraphArt.HasRuntimeSprite);
            Assert.IsTrue(landingArt.HasRuntimeSprite);
            Assert.AreEqual(telegraphName, telegraphArt.RuntimeSpriteName);
            Assert.AreEqual(landingName, landingArt.RuntimeSpriteName);
        }
    }
}
