using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Bootstrap;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Covers <see cref="GameManager.ShowActionTutorial"/> and its four latching flags — the
    /// first-level (Backyard Rescue) on-screen button-prompt legend that <see cref="ArenaHud"/>
    /// draws. Asserts it only appears for Backyard Rescue, that each action button flips its own
    /// flag regardless of whether the action resolves into anything (a whiffed wrestle still counts
    /// as "the player found the button"), and that the legend retires once all four are learned.
    /// </summary>
    public sealed class ActionTutorialPlayModeTests
    {
        private sealed class Rig
        {
            public GameManager Game;
            public DogController Cheddar;
            public DogController Cocoa;
        }

        // Mirrors the proven ArenaBootstrap boot sequence from
        // ControllerCoopPlayModeTests.KeyboardFallback_ProvidesInteractForBothDogs: two frames for
        // Start() to build floor/camera/dogs/GameManager, then a frame for StartMission to settle,
        // before any component is queried. Skipping those yields is what made the first version of
        // this file crash (GameManager not built yet) and corrupt the shared test scene for the
        // fixtures that ran after it.
        private static IEnumerator BootBackyardRescue(Rig rig)
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
            yield return null;

            new GameObject("Boot").AddComponent<ArenaBootstrap>();
            yield return null;
            yield return null;

            rig.Game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(rig.Game, "ArenaBootstrap did not build a GameManager.");
            rig.Game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) rig.Cheddar = id.GetComponent<DogController>();
                else if (id.Id == DogId.Cocoa) rig.Cocoa = id.GetComponent<DogController>();
            }
            Assert.IsNotNull(rig.Cheddar);
            Assert.IsNotNull(rig.Cocoa);
        }

        [UnityTest]
        public IEnumerator BackyardRescue_StartsWithTutorialVisibleAndNothingLearnedYet()
        {
            var rig = new Rig();
            yield return BootBackyardRescue(rig);

            Assert.IsTrue(rig.Game.ShowActionTutorial, "Tutorial legend should show at the start of Backyard Rescue.");
            Assert.IsFalse(rig.Game.TutorialBarkDone);
            Assert.IsFalse(rig.Game.TutorialInteractDone);
            Assert.IsFalse(rig.Game.TutorialJumpDone);
            Assert.IsFalse(rig.Game.TutorialWrestleDone);
        }

        [UnityTest]
        public IEnumerator OtherMissions_NeverShowTheBackyardRescueTutorial()
        {
            var rig = new Rig();
            yield return BootBackyardRescue(rig);

            rig.Game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            Assert.IsFalse(rig.Game.ShowActionTutorial, "Only Backyard Rescue is the first-level tutorial mission.");
        }

        [UnityTest]
        public IEnumerator EachActionButton_LatchesItsOwnFlag_EvenWhenItWhiffs()
        {
            var rig = new Rig();
            yield return BootBackyardRescue(rig);

            // Cheddar/Cocoa spawn ~20 units apart (ArenaBootstrap), well outside wrestle range, so
            // this wrestle attempt whiffs — the flag should still flip because it tracks "found the
            // button", not "won the exchange".
            rig.Cheddar.Bark();
            rig.Cheddar.Interact();
            rig.Cheddar.Jump();
            rig.Cheddar.Wrestle();
            yield return null;

            Assert.IsTrue(rig.Game.TutorialBarkDone, "Bark should latch the tutorial flag.");
            Assert.IsTrue(rig.Game.TutorialInteractDone, "Interact should latch the tutorial flag.");
            Assert.IsTrue(rig.Game.TutorialJumpDone, "Jump should latch the tutorial flag.");
            Assert.IsTrue(rig.Game.TutorialWrestleDone, "A whiffed wrestle attempt should still latch the tutorial flag.");
            Assert.IsFalse(rig.Game.ShowActionTutorial, "Legend should retire once all four actions are demonstrated.");
        }

        [UnityTest]
        public IEnumerator Restarting_TheMission_RearmsTheTutorial()
        {
            var rig = new Rig();
            yield return BootBackyardRescue(rig);
            rig.Cheddar.Bark();
            rig.Cheddar.Interact();
            rig.Cheddar.Jump();
            rig.Cheddar.Wrestle();
            yield return null;
            Assert.IsFalse(rig.Game.ShowActionTutorial);

            rig.Game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            Assert.IsTrue(rig.Game.ShowActionTutorial, "Replaying Backyard Rescue should show the legend again.");
            Assert.IsFalse(rig.Game.TutorialBarkDone);
        }
    }
}
