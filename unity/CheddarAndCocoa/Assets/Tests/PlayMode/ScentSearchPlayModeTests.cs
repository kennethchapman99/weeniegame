using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ScentSearchPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator ScentSearch_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(26, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.ScentSearch)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Scent Search should be reachable from mission select.");
            Assert.AreEqual("Scent Search", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator ScentSearch_ClearPath_CocoaCallsAndCheddarDigsEveryBone_WithLivePayoff()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            Assert.IsInstanceOf<ScentSearchMissionController>(game.ActiveMissionController,
                "Scent Search must run entirely through its own IMissionController.");
            Assert.AreEqual("scent_search", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Cocoa"));
            int required = game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(required, 0);

            var controller = (ScentSearchMissionController)game.ActiveMissionController;
            for (int i = 0; i < required; i++)
            {
                int buried = controller.BuriedSpotIndex;
                _cocoa.transform.position = controller.DigSpots[buried];
                game.ForceScentSniff(DogId.Cocoa);
                Assert.AreEqual(buried, controller.CalledSpotIndex,
                    "Cocoa's red-hot bark should create Cheddar's exact digging opening.");

                _cheddar.transform.position = controller.DigSpots[buried];
                _cheddar.Interact();
                yield return null;
            }

            Assert.AreEqual(required, game.ScentSearchState.Found);
            Assert.AreEqual(0, game.ScentSearchState.WastedDigs);
            Assert.Greater(game.ScentSearchState.Sniffs, 0);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome,
                "The uncovered cache should remain live briefly before the end screen.");
            Assert.IsTrue(controller.IsPresentingSuccessfulOutcome);
            foreach (var feedback in game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose,
                    "Both dogs should show an animated proud read during the held payoff, not frozen dogs.");

            controller.ForceFinishSuccessPresentation();
            yield return null;

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Master Sniffers"));
        }

        [UnityTest]
        public IEnumerator ScentSearch_RolesRequireCocoaCallThenCheddarDig_WithoutPunishingMisreads()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            var controller = (ScentSearchMissionController)_game.ActiveMissionController;
            int buried = controller.BuriedSpotIndex;
            Vector2 mound = controller.DigSpots[buried];

            _cheddar.transform.position = mound;
            _game.ForceScentSniff(DogId.Cheddar);
            Assert.AreEqual(-1, controller.CalledSpotIndex,
                "Cheddar's excited directional sniff must not replace Cocoa's exact tracking call.");
            Assert.That(_game.LastCue, Does.Contain("Cocoa"));

            _cheddar.Interact();
            yield return null;
            Assert.AreEqual(0, _game.ScentSearchState.Found);
            Assert.AreEqual(0, _game.ScentSearchState.WastedDigs,
                "A premature role attempt should coach the handoff instead of spending a cold-dig life.");
            Assert.That(_game.LastJuiceLabel, Does.Contain("WAIT"), "The premature dig must produce a visible coach beat.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "The premature dig must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _cocoa.transform.position = mound;
            _game.ForceScentSniff(DogId.Cocoa);
            Assert.AreEqual(buried, controller.CalledSpotIndex);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, _game.DogFeedback[1].CurrentPose);

            _cocoa.Interact();
            yield return null;
            Assert.AreEqual(0, _game.ScentSearchState.Found,
                "Cocoa owns the precise call, not the dirt-flinging dig.");
            Assert.That(_game.LastJuiceLabel, Does.Contain("DIG"), "Cocoa's dig attempt must produce a visible coach beat.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "Cocoa's dig attempt must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.ScentSearchState.WastedDigs);

            _cheddar.Interact();
            yield return null;
            Assert.AreEqual(1, _game.ScentSearchState.Found);
            Assert.AreEqual(-1, controller.CalledSpotIndex,
                "Each newly buried bone should require a fresh Cocoa tracking call.");
        }

        [UnityTest]
        public IEnumerator ScentSearch_FailPath_TooManyColdDigsEndMission()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            for (int i = 0; i < 4; i++)
            {
                game.ForceScentDigWrong(DogId.Cheddar);
                yield return null;
            }

            Assert.AreEqual(4, game.ScentSearchState.WastedDigs);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Dug Up The Whole Yard"));
        }

        [UnityTest]
        public IEnumerator ScentSearch_Replay_ResetsScentRuntimeState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;
            game.ForceScentSniff(DogId.Cheddar);
            game.ForceScentDigCorrect(DogId.Cheddar);
            yield return null;

            Assert.AreEqual(1, game.ScentSearchState.Found);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.ScentSearch, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.ScentSearchState.Found);
            Assert.AreEqual(0, game.ScentSearchState.WastedDigs);
            Assert.AreEqual(0, game.ScentSearchState.Sniffs);
            Assert.AreEqual(-1, game.ScentSearchController.CalledSpotIndex);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator ScentSearch_DiggingUpABone_ShowsTheAuthoredDigLoop()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            game.ForceScentDigCorrect(DogId.Cheddar);
            yield return null;

            Assert.AreEqual(1, game.ScentSearchState.Found);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Dig, game.DogFeedback[0].CurrentPose,
                "Digging up a bone should show the authored dirt-flinging motion.");
            Assert.That(game.DogFeedback[0].AuthoredPoseSpriteName, Does.Contain("cheddar_dig_e_"));
            Assert.AreEqual("Dig", game.DogFeedback[0].MotionClipLabel);
        }

        [UnityTest]
        public IEnumerator ScentSearch_ColdDig_ShowsDigMotionWithWarningFeedback()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            _game.ForceScentDigWrong(DogId.Cheddar);
            yield return null;

            Assert.AreEqual(1, _game.ScentSearchState.WastedDigs);
            Assert.AreEqual(DogReadabilityFeedback.Pose.Dig, _game.DogFeedback[0].CurrentPose);
            Assert.That(_game.DogFeedback[0].AuthoredPoseSpriteName, Does.Contain("cheddar_dig_e_"));
            Assert.AreEqual("Dig", _game.DogFeedback[0].MotionClipLabel);
            Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, _game.LastAudioCueRequested);
            Assert.AreEqual("cold_dig", _game.LastRumbleRequested);
        }

        [UnityTest]
        public IEnumerator ScentSearch_ReselectedBuriedSpot_ClearsStaleColdArtInsteadOfKeepingIt()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;

            var controller = _game.ScentSearchController;
            _game.ForceScentDigWrong(DogId.Cheddar);
            yield return null;

            int coldSpot = -1;
            for (int i = 0; i < _game.DigSpots.Length; i++)
            {
                if (controller.DigResourcePathAt(i).Contains("cold")) { coldSpot = i; break; }
            }
            Assert.GreaterOrEqual(coldSpot, 0, "The forced wrong dig should have left a spot showing the cold-scent art.");

            int guard = 0;
            while (controller.BuriedSpotIndex != coldSpot && guard++ < 500)
                controller.ForceReselectBuriedSpot();
            Assert.AreEqual(coldSpot, controller.BuriedSpotIndex,
                "Guard exhausted trying to re-roll the buried spot onto the previously-cold one.");

            Assert.That(controller.DigResourcePathAt(coldSpot), Does.Not.Contain("cold"),
                "Re-picking a previously cold-dug spot as the new hiding place should clear its stale cold art.");
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
