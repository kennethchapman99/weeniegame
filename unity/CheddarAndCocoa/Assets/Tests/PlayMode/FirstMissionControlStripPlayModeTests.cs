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
    /// F4.1: a stranger's first mission of the session — whichever mission that turns out to be —
    /// gets a compact fading control-strip reminder, unless that mission is Backyard Rescue (whose
    /// own full progressive <see cref="ActionTutorialPlayModeTests"/> tutorial covers the same need
    /// in more depth). Exactly one teaching mechanism fires for the session's first mission, and
    /// neither fires again after that, regardless of which missions come next.
    /// </summary>
    public sealed class FirstMissionControlStripPlayModeTests
    {
        private sealed class Rig
        {
            public GameManager Game;
            public DogController Cheddar;
            public DogController Cocoa;
        }

        // Mirrors ActionTutorialPlayModeTests.BootBackyardRescue's proven boot sequence, generalized
        // to start whichever mission the caller wants as the session's first.
        private static IEnumerator Boot(Rig rig, GameManager.MissionVariant firstMission)
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
            yield return null;

            new GameObject("Boot").AddComponent<ArenaBootstrap>();
            yield return null;
            yield return null;

            rig.Game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(rig.Game, "ArenaBootstrap did not build a GameManager.");
            rig.Game.StartMission(firstMission);
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
        public IEnumerator NonTutorialMission_AsFirstOfSession_ShowsTheControlStripNotTheTutorial()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.SnackHeist);

            Assert.IsTrue(rig.Game.FirstMissionControlStripVisible,
                "A stranger's first mission (whatever it is) should show the compact reminder.");
            Assert.AreEqual(1f, rig.Game.FirstMissionControlStripAlpha);
            Assert.IsFalse(rig.Game.ShowActionTutorial, "Only Backyard Rescue owns the full progressive tutorial.");
            Assert.IsFalse(rig.Game.IsFirstMissionVerbUsed(GameManager.TutorialActionStep.Bark));
            Assert.IsFalse(rig.Game.IsFirstMissionVerbUsed(GameManager.TutorialActionStep.Interact));
            Assert.IsFalse(rig.Game.IsFirstMissionVerbUsed(GameManager.TutorialActionStep.Jump));
            Assert.IsFalse(rig.Game.IsFirstMissionVerbUsed(GameManager.TutorialActionStep.Wrestle));
        }

        [UnityTest]
        public IEnumerator BackyardRescue_AsFirstOfSession_ShowsTheTutorialNotTheControlStrip()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.BackyardRescue);

            Assert.IsTrue(rig.Game.ShowActionTutorial);
            Assert.IsFalse(rig.Game.FirstMissionControlStripVisible,
                "Backyard Rescue's own tutorial already covers this session's first mission - showing " +
                "both at once would be redundant clutter.");
        }

        [UnityTest]
        public IEnumerator UsingEachVerbOnce_HidesTheStrip_PartialProgressDoesNot()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.SnackHeist);

            rig.Cheddar.Bark();
            yield return null;
            Assert.IsTrue(rig.Game.IsFirstMissionVerbUsed(GameManager.TutorialActionStep.Bark));
            Assert.IsTrue(rig.Game.FirstMissionControlStripVisible, "One verb used should not dismiss the reminder.");

            rig.Cheddar.Interact();
            rig.Cheddar.Jump();
            yield return null;
            Assert.IsTrue(rig.Game.FirstMissionControlStripVisible, "Three of four verbs used should still show it.");

            rig.Cheddar.Wrestle();
            yield return null;
            Assert.IsTrue(rig.Game.IsFirstMissionVerbUsed(GameManager.TutorialActionStep.Interact));
            Assert.IsTrue(rig.Game.IsFirstMissionVerbUsed(GameManager.TutorialActionStep.Jump));
            Assert.IsTrue(rig.Game.IsFirstMissionVerbUsed(GameManager.TutorialActionStep.Wrestle));
            Assert.IsFalse(rig.Game.FirstMissionControlStripVisible,
                "Every verb used once (by either dog - this is a light reminder, not the graded tutorial) should retire the strip.");
        }

        [UnityTest]
        public IEnumerator EitherDog_CanSatisfyAVerb_NotBothLikeTheFullTutorial()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.SnackHeist);

            // Unlike Backyard Rescue's tutorial (which requires both dogs independently), this is a
            // lightweight reminder: either dog trying a verb once is enough to check it off.
            rig.Cocoa.Bark();
            yield return null;
            Assert.IsTrue(rig.Game.IsFirstMissionVerbUsed(GameManager.TutorialActionStep.Bark));
        }

        [UnityTest]
        public IEnumerator Timeout_HidesTheStrip()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.SnackHeist);
            Assert.IsTrue(rig.Game.FirstMissionControlStripVisible);

            rig.Game.ForceFirstMissionControlStripElapsed(15f);
            Assert.IsTrue(rig.Game.FirstMissionControlStripVisible, "Should still be full-strength with 5s left.");
            Assert.AreEqual(1f, rig.Game.FirstMissionControlStripAlpha, "Fade only starts in the final 2s.");

            rig.Game.ForceFirstMissionControlStripElapsed(3.5f);
            Assert.IsTrue(rig.Game.FirstMissionControlStripVisible, "Should still be visible before the 20s deadline.");
            Assert.Less(rig.Game.FirstMissionControlStripAlpha, 1f, "The final 1.5s should fade rather than snap.");
            Assert.Greater(rig.Game.FirstMissionControlStripAlpha, 0f);

            rig.Game.ForceFirstMissionControlStripElapsed(2f);
            Assert.IsFalse(rig.Game.FirstMissionControlStripVisible, "Past the 20s deadline it should be gone.");
            Assert.AreEqual(0f, rig.Game.FirstMissionControlStripAlpha);
        }

        [UnityTest]
        public IEnumerator Skip_HidesItImmediately()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.SnackHeist);
            Assert.IsTrue(rig.Game.FirstMissionControlStripVisible);

            rig.Game.SkipFirstMissionControlStrip();
            Assert.IsFalse(rig.Game.FirstMissionControlStripVisible);
        }

        [UnityTest]
        public IEnumerator ReplayingTheFirstMission_DoesNotReshowTheStrip()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.SnackHeist);
            rig.Game.SkipFirstMissionControlStrip();
            Assert.IsFalse(rig.Game.FirstMissionControlStripVisible);

            rig.Game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            Assert.IsFalse(rig.Game.FirstMissionControlStripVisible,
                "Replaying the session's first mission must not re-arm the one-time reminder.");
        }

        [UnityTest]
        public IEnumerator ASecondDifferentMission_NeverShowsTheStrip()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.SnackHeist);
            Assert.IsTrue(rig.Game.FirstMissionControlStripVisible);

            rig.Game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return null;

            Assert.IsFalse(rig.Game.FirstMissionControlStripVisible,
                "This is a once-per-session reminder, not a once-per-mission one.");
        }

        [UnityTest]
        public IEnumerator BackyardRescueFirst_ThenASecondMission_NeverShowsTheStripEither()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.BackyardRescue);
            Assert.IsTrue(rig.Game.ShowActionTutorial);

            rig.Game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            Assert.IsFalse(rig.Game.FirstMissionControlStripVisible,
                "Backyard Rescue's tutorial already used the session's one first-mission teaching moment.");
            Assert.IsFalse(rig.Game.ShowActionTutorial);
        }

        [UnityTest]
        public IEnumerator ResetSession_RearmsTheReminderForTheNextMission()
        {
            var rig = new Rig();
            yield return Boot(rig, GameManager.MissionVariant.SnackHeist);
            rig.Game.SkipFirstMissionControlStrip();

            rig.Game.ResetSession();
            rig.Game.StartMission(GameManager.MissionVariant.SockPanic);
            yield return null;

            Assert.IsTrue(rig.Game.FirstMissionControlStripVisible,
                "A fresh session (e.g. the 'New Session' button) should treat the next mission as first again.");
        }
    }
}
