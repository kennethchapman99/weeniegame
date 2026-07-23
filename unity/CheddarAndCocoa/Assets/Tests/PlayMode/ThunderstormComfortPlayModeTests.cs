using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class ThunderstormComfortPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator Thunderstorm_RunsThroughDedicatedController()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            Assert.IsInstanceOf<ThunderstormComfortMissionController>(
                _game.ActiveMissionController,
                "Thunderstorm Comfort must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.ThunderstormComfort, _game.ActiveMissionController.Variant);
            Assert.AreSame(_game.ThunderstormController.StormState, _game.ThunderstormState);
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(25, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.ThunderstormComfort)
                {
                    found = true;
                    break;
                }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Thunderstorm Comfort should be reachable from mission select.");
            Assert.AreEqual("Thunderstorm Comfort", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_ClearPath_CocoaReassuresCheddarAnswersThroughEveryClap()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            Assert.AreEqual("thunderstorm_comfort", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Huddle"));

            // Keep both dogs huddled together so comfort drains the panic each clap adds.
            _cheddar.transform.position = Vector3.zero;
            _cocoa.transform.position = Vector3.zero;

            var controller = (ThunderstormComfortMissionController)game.ActiveMissionController;
            for (int i = 0; i < 5; i++)
            {
                _cheddar.transform.position = Vector3.zero;
                _cocoa.transform.position = Vector3.zero;
                _cocoa.Bark();
                _cheddar.Bark();
                Assert.IsTrue(controller.ComfortPrepared,
                    "Cocoa's reassurance followed by Cheddar's answer should arm the next clap.");
                game.ForceThunderclap();
                game.ForceComfortStep(2f);
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome,
                "The passed storm should remain live briefly before the end card.");
            Assert.IsTrue(controller.IsPresentingSuccessfulOutcome);
            Assert.IsTrue(HasWorldPop("STORM PASSED"));
            foreach (var feedback in game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose,
                    "Both dogs should show an animated proud read during the held payoff, not frozen dogs.");

            controller.ForceFinishSuccessPresentation();
            yield return null;

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.ThunderstormState.ReadyToClear());
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Weathered The Storm"));
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_RequiresOrderedHuddleBarks_AndMissesRecoverNextClap()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            var controller = (ThunderstormComfortMissionController)_game.ActiveMissionController;
            _cheddar.transform.position = Vector3.zero;
            _cocoa.transform.position = Vector3.right;

            _cheddar.Bark();
            Assert.IsFalse(controller.ComfortPrepared,
                "Cheddar cannot skip Cocoa's steady reassurance opener.");
            Assert.That(_game.LastCue, Does.Contain("Cocoa"));
            Assert.That(_game.LastJuiceLabel, Does.Contain("COCOA"), "The out-of-order bark must produce a visible coach beat.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "The out-of-order bark must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _game.ForceThunderclap();
            yield return null;
            Assert.AreEqual(0, _game.ThunderstormState.ClapsSurvived,
                "Passive proximity without the bark handoff must not bank storm progress.");
            Assert.AreEqual(1, _game.ThunderstormState.ExposedClaps);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _cocoa.Bark();
            yield return new WaitForSeconds(1.75f);
            _cheddar.Bark();
            Assert.IsFalse(controller.ComfortPrepared,
                "Cheddar's answer must land inside Cocoa's readable reassurance window.");

            _cocoa.Bark();
            _cheddar.Bark();
            Assert.IsTrue(controller.ComfortPrepared);
            _cocoa.transform.position = Vector3.right * 12f;
            _game.ForceThunderclap();
            yield return null;
            Assert.AreEqual(0, _game.ThunderstormState.ClapsSurvived,
                "Prepared comfort still requires the pair to hold the physical huddle.");
            Assert.AreEqual(2, _game.ThunderstormState.ExposedClaps);

            _cocoa.transform.position = Vector3.right;
            _cocoa.Bark();
            _cheddar.Bark();
            _game.ForceThunderclap();
            yield return null;
            Assert.AreEqual(1, _game.ThunderstormState.ClapsSurvived,
                "The next properly prepared clap should recover immediately.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_FailPath_PanicMaxesWhenApart()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            // Dogs kept far apart: nothing drains panic, so repeated claps max it out and they bolt.
            _cheddar.transform.position = new Vector3(-12f, 0f, 0f);
            _cocoa.transform.position = new Vector3(12f, 0f, 0f);

            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 12)
            {
                game.ForceThunderclap();
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Spooked By Thunder"));
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_Bolt_FiresADistinctGagNotJustTheEndCard()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            // Dogs kept far apart: nothing drains panic, so repeated claps max it out and one bolts.
            _cheddar.transform.position = new Vector3(-12f, 0f, 0f);
            _cocoa.transform.position = new Vector3(12f, 0f, 0f);

            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 12)
            {
                game.ForceThunderclap();
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.IsTrue(HasWorldPop("BOLTED"),
                "The exact clap that maxes panic should read as its own world-space gag, not just a " +
                "silent meter crossing 1.0 followed by the generic end card.");
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_Bolt_JuicePopSurvivesTheEndRoundClobber()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            _cheddar.transform.position = new Vector3(-12f, 0f, 0f);
            _cocoa.transform.position = new Vector3(12f, 0f, 0f);

            int guard = 0;
            while (game.Outcome == GameManager.MissionOutcome.InProgress && guard++ < 12)
            {
                game.ForceThunderclap();
                yield return null;
            }

            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.GreaterOrEqual(CountLiveJuiceEffects(), 2,
                "CheckBolt's own fail-gag pop and GameManager.EndRound's generic 'SAD FLOP REPLAY!' pop " +
                "fire in the same Update; the bolt's pop must survive instead of being destroyed before " +
                "it ever renders a frame.");
        }

        private static int CountLiveJuiceEffects()
        {
            int count = 0;
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (renderer.gameObject.name.StartsWith(FinalJuiceEffect.EffectNamePrefix)) count++;
            return count;
        }

        private static bool HasWorldPop(string text)
        {
            foreach (var pop in Object.FindObjectsByType<MissionWorldPop>(FindObjectsSortMode.None))
                if (pop.Label.Contains(text)) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_Replay_ResetsStormAndPanic()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;
            game.ForceThunderclap();
            yield return null;

            Assert.Greater(game.ThunderstormState.ClapsSurvived + (game.Panic.CheddarPanic > 0f ? 1 : 0), 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.ThunderstormComfort, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.ThunderstormState.ClapsSurvived);
            Assert.Less(game.Panic.CheddarPanic, 0.1f);
            Assert.Less(game.Panic.CocoaPanic, 0.1f);
            Assert.IsFalse(game.ThunderstormController.ComfortPrepared);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_Snapshot_ReportsExposedClapsAsMistakes()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            Assert.AreEqual(0, game.RuntimeSnapshot.Mistakes);

            // Apart for the first clap (a mistake), then huddled for the rest so the round can still clear.
            _cheddar.transform.position = new Vector3(-12f, 0f, 0f);
            _cocoa.transform.position = new Vector3(12f, 0f, 0f);
            game.ForceThunderclap();
            yield return null;

            Assert.AreEqual(1, game.RuntimeSnapshot.Mistakes,
                "A thunderclap that lands while the dogs are apart should be reported as a mistake, not silently show 0.");

            _cheddar.transform.position = Vector3.zero;
            _cocoa.transform.position = Vector3.zero;
            game.ForceComfortStep(2f);
            _cocoa.Bark();
            _cheddar.Bark();
            game.ForceThunderclap();
            yield return null;

            Assert.AreEqual(1, game.RuntimeSnapshot.Mistakes,
                "A clap weathered while huddled together should not add another mistake.");
        }

        [UnityTest]
        public IEnumerator ThunderstormComfort_Thunderclap_MakesDogsFlinch()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.ThunderstormComfort);
            yield return null;

            // Keep the dogs apart so the huddle-comfort pose doesn't override the flinch.
            _cheddar.transform.position = new Vector3(-12f, 0f, 0f);
            _cocoa.transform.position = new Vector3(12f, 0f, 0f);

            game.ForceThunderclap();
            yield return null;

            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, game.DogFeedback[0].CurrentPose,
                "Dogs should visibly flinch (Sad pose) at a thunderclap.");
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
