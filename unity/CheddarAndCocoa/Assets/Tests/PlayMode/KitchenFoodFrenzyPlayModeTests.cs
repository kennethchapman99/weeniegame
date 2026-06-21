using System.Collections;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
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
            Assert.AreEqual("kitchen_food_frenzy", _game.RuntimeSnapshot.MissionId);
            Assert.IsNotNull(GameObject.Find("KitchenCounterRoute"));
            Assert.IsNotNull(GameObject.Find("KitchenSafeBowl"));
            Assert.IsFalse(_game.SquirrelObject.activeSelf);
            Assert.IsFalse(_game.PredatorObject.activeSelf);
            Assert.IsFalse(_game.RopeObject.activeSelf);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cheddar"));
            Assert.That(_game.ObjectiveLabel, Does.Contain("COUNTER"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("KNOCK FOOD LOOSE"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("GUARD THE BOWL"));
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
                _game.ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind.Good);
                _game.ForceKitchenCatch(DogId.Cocoa, true);
            }
            yield return null;

            Assert.AreEqual(KitchenFoodFrenzyMissionState.RequiredCatches, _game.KitchenState.GoodCatches);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, _game.CurrentFlow);
            Assert.That(_game.MissionBanner, Does.Contain("KITCHEN CLEARED"));
            Assert.IsTrue(LogContains("KitchenCatch"));
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
            Assert.IsFalse(_game.KitchenState.DropActive);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
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
    }
}
