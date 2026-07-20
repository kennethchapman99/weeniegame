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
    /// Covers the first-level (Backyard Rescue) progressive button tutorial drawn by
    /// <see cref="ArenaHud"/>. Every action must be demonstrated independently by both players;
    /// later verbs do not clear while an earlier prompt is active, and pause-menu skip/replay seams
    /// preserve a quick path for returning couch players.
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

        [Test]
        public void ProductionHud_KeepsObjectiveAndPlayerIdentityReadableWithoutCoveringThePlayfield()
        {
            var layout = ArenaHud.BuildGameplayHudLayout(1920f, 1080f);

            Assert.LessOrEqual(layout.TopBar.height, 108f,
                "Normal play should use one compact objective card, not the old multi-band debug dashboard.");
            Assert.Less(layout.TopBar.yMax, 140f);
            Assert.GreaterOrEqual(ArenaHud.GameplayObjectiveFontSize, 26);
            Assert.GreaterOrEqual(ArenaHud.GameplayIdentityFontSize, 22);
            Assert.GreaterOrEqual(ArenaHud.GameplayStatusFontSize, 18);
            Rect pressure = ArenaHud.BuildPressureMeterRect(layout.TopBar);
            Assert.GreaterOrEqual(pressure.xMin, layout.TopBar.xMin);
            Assert.LessOrEqual(pressure.xMax, layout.TopBar.xMax);
            Assert.GreaterOrEqual(pressure.yMin, layout.TopBar.yMin);
            Assert.LessOrEqual(pressure.yMax, layout.TopBar.yMax,
                "A mission pressure meter should remain inside the compact top HUD, not become another screen band.");
            Assert.LessOrEqual(layout.CheddarChip.yMax, 1080f);
            Assert.LessOrEqual(layout.CocoaChip.yMax, 1080f);
            Assert.Less(layout.CheddarChip.xMax, layout.CocoaChip.xMin,
                "P1 and P2 identity chips should anchor separate couch-player corners.");
            Assert.GreaterOrEqual(layout.CheddarChip.height, ArenaHud.GameplayIdentityFontSize * 2.8f,
                "Two-line player/source chips need enough height to avoid clipping either line.");
            Assert.AreEqual("P1  CHEDDAR\nCONNECT PAD",
                ArenaHud.BuildPlayerIdentityChipLabel("P1  CHEDDAR", "CONNECT PAD"));
            foreach (string line in ArenaHud.BuildPlayerIdentityChipLabel("P2  COCOA", "PAD READY").Split('\n'))
                Assert.LessOrEqual(line.Length, 12,
                    "Player and source stay on separate short lines even on a narrow couch window.");
            Assert.That(ArenaHud.PlayerIdentityLabel, Does.Contain("P1 CHEDDAR"));
            Assert.That(ArenaHud.PlayerIdentityLabel, Does.Contain("P2 COCOA"));
        }

        /// <summary>
        /// CF1.2: a mission can expose both a pressure meter (IMissionPressureHud) and a
        /// beat-progress meter (IMissionBeatProgressHud) at once - Operation Pee Break's
        /// always-on bladder pressure plus its beat-progress readout, which used to be
        /// world-anchored on the Teenager and scrolled off-screen when the dogs moved to the
        /// bottom of the room. Both compact meters must fit inside the SAME top bar height this
        /// class already pins above, never grow it, never overlap each other, and stay ordered
        /// pressure-then-progress top-to-bottom.
        /// </summary>
        [Test]
        public void StackedTopBarMeters_FitInsideTheUnchangedTopBarAndDoNotOverlap()
        {
            var layout = ArenaHud.BuildGameplayHudLayout(1920f, 1080f);

            Rect soloProgress = ArenaHud.BuildBeatProgressMeterRect(layout.TopBar);
            Assert.GreaterOrEqual(soloProgress.xMin, layout.TopBar.xMin);
            Assert.LessOrEqual(soloProgress.xMax, layout.TopBar.xMax);
            Assert.GreaterOrEqual(soloProgress.yMin, layout.TopBar.yMin);
            Assert.LessOrEqual(soloProgress.yMax, layout.TopBar.yMax,
                "A lone beat-progress meter should remain inside the compact top HUD, exactly like the pressure meter.");

            Rect pressureSlot = ArenaHud.BuildStackedPressureMeterRect(layout.TopBar);
            Rect progressSlot = ArenaHud.BuildStackedProgressMeterRect(layout.TopBar);
            Assert.GreaterOrEqual(pressureSlot.xMin, layout.TopBar.xMin);
            Assert.LessOrEqual(progressSlot.xMax, layout.TopBar.xMax);
            Assert.GreaterOrEqual(pressureSlot.yMin, layout.TopBar.yMin,
                "CF1.2 must not grow the top bar upward to fit two meters.");
            Assert.LessOrEqual(progressSlot.yMax, layout.TopBar.yMax,
                "CF1.2 must not grow the top bar downward to fit two meters - both stay inside the height already pinned above.");
            Assert.LessOrEqual(pressureSlot.yMax, progressSlot.yMin,
                "The pressure meter must stack strictly above the progress meter with no vertical overlap.");
            Assert.AreEqual(pressureSlot.x, progressSlot.x, 0.01f,
                "Stacked meters should share the same left edge so the pair reads as one dashboard readout.");
            Assert.AreEqual(pressureSlot.width, progressSlot.width, 0.01f);
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
            Assert.AreEqual(GameManager.TutorialActionStep.Bark, rig.Game.CurrentTutorialAction);
            Assert.IsFalse(rig.Game.TutorialActionDone(DogId.Cheddar, GameManager.TutorialActionStep.Bark));
            Assert.IsFalse(rig.Game.TutorialActionDone(DogId.Cocoa, GameManager.TutorialActionStep.Bark));
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
        public IEnumerator OnePlayer_CannotClearTheirPartnersPrompt_AndLaterActionsWaitTheirTurn()
        {
            var rig = new Rig();
            yield return BootBackyardRescue(rig);

            // Random later buttons do not let one player mash through the complete legend.
            rig.Cheddar.Interact();
            rig.Cheddar.Jump();
            rig.Cheddar.Wrestle();
            Assert.IsFalse(rig.Game.TutorialInteractDone);
            Assert.IsFalse(rig.Game.TutorialJumpDone);
            Assert.IsFalse(rig.Game.TutorialWrestleDone);

            rig.Cheddar.Bark();
            yield return null;

            Assert.IsTrue(rig.Game.TutorialActionDone(DogId.Cheddar, GameManager.TutorialActionStep.Bark));
            Assert.IsFalse(rig.Game.TutorialActionDone(DogId.Cocoa, GameManager.TutorialActionStep.Bark));
            Assert.IsFalse(rig.Game.TutorialBarkDone,
                "The aggregate Bark step must wait until Cocoa has also found her button.");
            Assert.AreEqual(GameManager.TutorialActionStep.Bark, rig.Game.CurrentTutorialAction);
            Assert.IsTrue(rig.Game.ShowActionTutorial);
        }

        [UnityTest]
        public IEnumerator Actions_UnlockProgressively_AndRequireBothDogs()
        {
            var rig = new Rig();
            yield return BootBackyardRescue(rig);

            rig.Cheddar.Bark();
            rig.Cocoa.Bark();
            Assert.IsTrue(rig.Game.TutorialBarkDone);
            Assert.AreEqual(GameManager.TutorialActionStep.Interact, rig.Game.CurrentTutorialAction);

            rig.Cheddar.Interact();
            rig.Cocoa.Interact();
            Assert.IsTrue(rig.Game.TutorialInteractDone);
            Assert.AreEqual(GameManager.TutorialActionStep.Jump, rig.Game.CurrentTutorialAction);
            Assert.AreEqual(0, rig.Game.FailedInteractions,
                "Players obeying the Interact lesson must not receive an error cue or miss penalty.");

            rig.Cheddar.Jump();
            rig.Cocoa.Jump();
            Assert.IsTrue(rig.Game.TutorialJumpDone);
            Assert.AreEqual(GameManager.TutorialActionStep.Wrestle, rig.Game.CurrentTutorialAction);

            // The dogs spawn far apart, so both attempts whiff. Button discovery still counts; the
            // tutorial is teaching input ownership, not demanding a successful wrestle outcome.
            rig.Cheddar.Wrestle();
            rig.Cocoa.Wrestle();
            yield return null;

            Assert.IsTrue(rig.Game.TutorialWrestleDone);
            Assert.AreEqual(GameManager.TutorialActionStep.Complete, rig.Game.CurrentTutorialAction);
            Assert.IsFalse(rig.Game.ShowActionTutorial,
                "The tutorial should retire only after both players try every progressively disclosed action.");
        }

        [UnityTest]
        public IEnumerator Tutorial_CanBeSkippedAndReplayedWithoutRestartingTheMission()
        {
            var rig = new Rig();
            yield return BootBackyardRescue(rig);

            rig.Game.SkipActionTutorial();
            Assert.IsFalse(rig.Game.ShowActionTutorial);
            Assert.AreEqual(GameManager.TutorialActionStep.Complete, rig.Game.CurrentTutorialAction);

            rig.Game.ReplayActionTutorial();
            Assert.IsTrue(rig.Game.ShowActionTutorial);
            Assert.AreEqual(GameManager.TutorialActionStep.Bark, rig.Game.CurrentTutorialAction);
            Assert.IsFalse(rig.Game.TutorialActionDone(DogId.Cheddar, GameManager.TutorialActionStep.Bark));
            Assert.IsFalse(rig.Game.TutorialActionDone(DogId.Cocoa, GameManager.TutorialActionStep.Bark));
        }

        [UnityTest]
        public IEnumerator CameraShakeComfortSetting_GatesFutureShakeRequests()
        {
            var rig = new Rig();
            yield return BootBackyardRescue(rig);

            rig.Game.SetCameraShakeEnabled(false);
            Assert.IsFalse(rig.Game.CameraShakeEnabled);
            rig.Game.ForcePredatorAttack();
            Assert.AreEqual(0, rig.Game.ShakeRequestCount,
                "Disabling shake from pause should suppress the next threat camera kick.");
            Assert.AreEqual(0f, rig.Game.LastShakeMagnitude);

            rig.Game.StartMission(GameManager.MissionVariant.BackyardRescue);
            rig.Game.SetCameraShakeEnabled(true);
            rig.Game.ForcePredatorAttack();
            Assert.AreEqual(1, rig.Game.ShakeRequestCount);
            Assert.Greater(rig.Game.LastShakeMagnitude, 0f);
            var cameraRig = Camera.main.GetComponent<CheddarAndCocoa.CameraRig.SharedCameraController>();
            Assert.Greater(cameraRig.PendingShakeMagnitude, 0f);

            rig.Game.SetCameraShakeEnabled(false);
            Assert.AreEqual(0f, cameraRig.PendingShakeMagnitude,
                "Turning shake off must cancel motion already queued before the pause setting changed.");
        }

        [UnityTest]
        public IEnumerator Restarting_TheMission_RearmsTheTutorial()
        {
            var rig = new Rig();
            yield return BootBackyardRescue(rig);
            rig.Game.SkipActionTutorial();
            Assert.IsFalse(rig.Game.ShowActionTutorial);

            rig.Game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            Assert.IsTrue(rig.Game.ShowActionTutorial, "Replaying Backyard Rescue should show the legend again.");
            Assert.IsFalse(rig.Game.TutorialBarkDone);
            Assert.IsFalse(rig.Game.TutorialActionDone(DogId.Cheddar, GameManager.TutorialActionStep.Bark));
            Assert.IsFalse(rig.Game.TutorialActionDone(DogId.Cocoa, GameManager.TutorialActionStep.Bark));
        }
    }
}
