using System.Collections;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// The always-on couch Button Guide coach: a persistent, glanceable per-player "which button
    /// does what, and when" legend. These lock the two honest-signal contracts the coach draws from -
    /// the session-scoped enable toggle/visibility gate, and Kitchen's <see cref="IMissionCoachHint"/>
    /// only lighting the scout's BARK button during the real "bark the next drop loose" wait window
    /// (never on the sweeper's positional catch, never once a drop is telegraphed/falling).
    /// </summary>
    public sealed class ButtonCoachPlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator ButtonCoach_DefaultsOnInLivePlayAndTogglesOffThroughTheSetting()
        {
            yield return LoadKitchen();

            Assert.IsTrue(_game.ButtonCoachEnabled, "The couch coach should default ON for first-time players.");
            Assert.IsTrue(_game.ButtonCoachVisible, "It should draw during live play by default.");

            _game.SetButtonCoachEnabled(false);
            Assert.IsFalse(_game.ButtonCoachEnabled);
            Assert.IsFalse(_game.ButtonCoachVisible, "Turning the setting off must hide the coach.");

            _game.SetButtonCoachEnabled(true);
            Assert.IsTrue(_game.ButtonCoachVisible, "Turning it back on must show the coach again.");
        }

        [UnityTest]
        public IEnumerator ButtonCoach_IsNotVisibleOutsideLivePlay()
        {
            yield return LoadKitchen();
            Assert.IsTrue(_game.ButtonCoachVisible);

            _game.ReturnToMissionSelect();
            yield return null;
            Assert.IsFalse(_game.ButtonCoachVisible,
                "The coach is a live-play overlay - it must not draw over the mission-select screen.");
        }

        [UnityTest]
        public IEnumerator Kitchen_CoachHint_LightsScoutBarkOnlyDuringTheWaitWindow()
        {
            yield return LoadKitchen();

            DogId scout = _game.KitchenState.ScoutDog;   // Cheddar - owns the counter bark
            DogId sweeper = _game.KitchenState.SweeperDog; // Cocoa - catches by position, no button

            // Waiting for the next counter-knock: the scout's BARK button should be lit, the
            // sweeper's should not (her job is to stand under the drop, which is movement, not a face
            // button - the coach must never fake a highlight there).
            Assert.IsFalse(_game.KitchenState.DropActive);
            Assert.IsFalse(_game.KitchenState.TelegraphActive);
            Assert.AreEqual(GameManager.TutorialActionStep.Bark, _game.CoachActionFor(scout),
                "Scout's BARK should be lit during the wait window.");
            Assert.IsNull(_game.CoachActionFor(sweeper),
                "Sweeper catches by position - the coach must not highlight a button for her.");

            // Drop telegraphed: nobody should be barking, so the light goes dark.
            _game.ForceKitchenTelegraph(scout, KitchenFoodFrenzyMissionState.FoodKind.Good);
            Assert.IsTrue(_game.KitchenState.TelegraphActive);
            Assert.IsNull(_game.CoachActionFor(scout),
                "Once a drop is telegraphed the scout should stop barking, so the light must go dark.");

            // Drop live: still no bark prompt for either dog.
            _game.ForceKitchenReleaseTelegraph();
            Assert.IsTrue(_game.KitchenState.DropActive);
            Assert.IsNull(_game.CoachActionFor(scout));
            Assert.IsNull(_game.CoachActionFor(sweeper));
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
    }
}
