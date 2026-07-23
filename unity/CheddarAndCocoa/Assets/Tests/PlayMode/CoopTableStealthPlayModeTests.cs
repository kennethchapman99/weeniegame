using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// The Table Stealth mission wires the Human-Distraction co-op puzzle into the real mission flow:
    /// Cocoa flops belly-up to hold the human's gaze while Cheddar sneaks the dropped steak, and
    /// sneaking while the human is watching gets the pair spotted (a recoverable exposure).
    /// </summary>
    public sealed class CoopTableStealthPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator TableStealth_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(26, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.TableStealth) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "Table Stealth should be reachable from mission select.");
            Assert.AreEqual("Table Stealth", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator TableStealth_RunsThroughDedicatedController()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            Assert.IsInstanceOf<TableStealthMissionController>(
                _game.ActiveMissionController,
                "Table Stealth must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.TableStealth, _game.ActiveMissionController.Variant);
            Assert.AreSame(_game.TableStealthController.Puzzle, _game.TableStealthPuzzle);
        }

        [UnityTest]
        public IEnumerator TableStealth_ClearPath_DistractThenSneakTheSteak()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            Assert.AreEqual("table_stealth", _game.RuntimeSnapshot.MissionId);
            Assert.That(_game.ObjectiveLabel.ToLowerInvariant(), Does.Contain("human"));

            _game.ForceTableFlop(true);   // Cocoa commits to the belly-flop distraction
            _game.ForceTableSneak(2.0f);  // Cheddar sneaks the steak while the human is held (needs 1.5s)

            Assert.IsTrue(_game.TableStealthPuzzle.Solved);
            Assert.IsInstanceOf<IMissionSuccessPresentationController>(_game.TableStealthController);
            Assert.IsTrue(_game.TableStealthController.IsPresentingSuccessfulOutcome,
                "The stolen steak should remain visible in the live world before the result card.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Steak secured"));
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose,
                    "Both dogs should show an animated proud read during the held payoff, not frozen dogs.");
            _game.ForceTableSuccessPresentationComplete();
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Steak Sneaked"));
            Assert.AreNotEqual("MVP: awaiting dog heroics", _game.MvpLabel,
                "Distracting and sneaking should credit both dogs toward the MVP stat.");
        }

        [UnityTest]
        public IEnumerator TableStealth_WrongDogAttempts_CoachRecoverably_ThenTheRealRolesStillWork()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            _cocoa.Bark();
            Assert.That(_game.LastCue, Does.Contain("Cheddar"));
            Assert.IsTrue(HasWorldPop("CHEDDAR BURPS"), "Cocoa's wrong-dog bark must produce a visible coach beat.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "Cocoa's wrong-dog bark must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            _cheddar.Interact();
            Assert.That(_game.LastCue, Does.Contain("Cocoa"));
            Assert.IsTrue(HasWorldPop("COCOA FLOPS"), "Cheddar's wrong-dog interact must produce a visible coach beat.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "Cheddar's wrong-dog interact must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            // Still recoverable: the real roles work right after.
            _game.ForceTableFlop(true);
            _game.ForceTableSneak(2.0f);
            Assert.IsTrue(_game.TableStealthPuzzle.Solved);
        }

        [UnityTest]
        public IEnumerator TableStealth_FailPath_SpottedTooManyTimes()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            for (int i = 0; i < 4; i++)
            {
                // Sneaking while the human is NOT distracted (no flop) gets the pair spotted.
                _game.ForceTableSneak(0.3f);
            }

            Assert.AreEqual(4, _game.TableStealthPuzzle.Exposures);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Caught At The Table"));
        }

        [UnityTest]
        public IEnumerator TableStealth_HumanActorStatesShowDistractionSpottingAndOutcome()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            var human = GameObject.Find("TableStealthHuman");
            Assert.IsNotNull(human);
            var feedback = human.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback);
            Assert.IsTrue(feedback.HasContextualTextVisibility,
                "The human explanation must be contextual/debug text, not an always-on production crutch.");
            Assert.That(feedback.Label, Does.Contain("WATCHING TABLE"));

            _game.ForceTableFlop(true);
            Assert.That(feedback.Label, Does.Contain("WATCHING COCOA"));

            _game.ForceTableSneak(2.0f);
            Assert.That(feedback.Label, Does.Contain("STEAK GONE"));

            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;
            _game.ForceTableSneak(0.3f);
            Assert.That(feedback.Label, Does.Contain("SPOTTED"));

            for (int i = 0; i < 3; i++) _game.ForceTableSneak(0.3f);
            Assert.That(feedback.Label, Does.Contain("CAUGHT"));
        }

        [UnityTest]
        public IEnumerator TableStealth_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;
            _game.ForceTableSneak(0.3f); // one exposure
            Assert.Greater(_game.TableStealthPuzzle.Exposures, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.TableStealth, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.TableStealthPuzzle.Exposures);
            Assert.AreEqual(0f, _game.TableStealthPuzzle.SneakProgress);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator TableStealth_PositionDriven_CocoaMustInteractFlop_ThenLeavingExposes()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            // Proximity alone is not a belly flop; Cocoa must deliberately Interact.
            _cocoa.transform.position = _game.TableHumanZone;
            _cheddar.transform.position = _game.TableStealZone;
            yield return null;
            Assert.IsFalse(_game.TableStealthController.FlopEngaged);
            Assert.AreEqual(0f, _game.TableStealthPuzzle.SneakProgress);

            _cocoa.Interact();
            yield return null;
            Assert.IsTrue(_game.TableStealthController.FlopEngaged);
            // TugRescueSuccess fires for the flop itself; SignalRoleHandoff's own chime fires right
            // after in the same call (Cocoa -> Cheddar), so it - not this one - is the LAST cue
            // requested. Check containment, not LastAudioCueRequested, to observe both correctly.
            Assert.That(_game.AudioCueRequests, Does.Contain(ArenaFeedbackCatalog.TugRescueSuccess),
                "S5.2: Cocoa's belly-flop decoy was a silent success beat (rumble but no audio) - must fire a cue now.");

            // Cocoa stays flopped by the human while Cheddar works the steak lane.
            // Driven against a real-time deadline so enough deltaTime accumulates for the human's
            // attention to build (per-frame dt in headless batchmode is tiny).
            float deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline)
            {
                _cocoa.transform.position = _game.TableHumanZone;
                _cheddar.transform.position = _game.TableStealZone;
                yield return null;
                if (_game.TableStealthPuzzle.SneakProgress > 0f || _game.TableStealthPuzzle.Solved) break;
            }
            Assert.IsTrue(_game.TableStealthPuzzle.SneakProgress > 0f || _game.TableStealthPuzzle.Solved,
                "With Cocoa flopping and Cheddar in the steak lane, the sneak advances.");

            if (!_game.TableStealthPuzzle.Solved)
            {
                // Cocoa gets up and wanders off; Cheddar keeps sneaking in the open -> spotted.
                int before = _game.TableStealthPuzzle.Exposures;
                deadline = Time.realtimeSinceStartup + 4f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    _cocoa.transform.position = new Vector3(_game.TableHumanZone.x + 40f, _game.TableHumanZone.y, 0f);
                    _cheddar.transform.position = _game.TableStealZone;
                    yield return null;
                    if (_game.TableStealthPuzzle.Exposures > before || _game.TableStealthPuzzle.Solved) break;
                }
                Assert.IsTrue(_game.TableStealthPuzzle.Exposures > before || _game.TableStealthPuzzle.Solved,
                    "Sneaking after Cocoa stops distracting gets the pair spotted.");
                Assert.IsFalse(_game.TableStealthController.FlopEngaged,
                    "Leaving the human must release the committed flop and require another Interact.");
            }
        }

        [UnityTest]
        public IEnumerator TableStealth_CheddarBarkBurp_OpensShortSneakWindowForCocoa()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;

            _cheddar.transform.position = _game.TableHumanZone;
            _cocoa.transform.position = _game.TableStealZone;
            _cheddar.Bark();
            yield return null;

            Assert.IsTrue(_game.TableStealthController.BurpWindowForCocoa,
                "Cheddar's live bark by the human must activate Cocoa's alternate sneak route.");
            Assert.IsTrue(_game.TableStealthPuzzle.HumanDistracted);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa"));

            float deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline && _game.TableStealthPuzzle.SneakProgress <= 0f)
            {
                _cheddar.transform.position = _game.TableHumanZone;
                _cocoa.transform.position = _game.TableStealZone;
                yield return null;
            }

            Assert.Greater(_game.TableStealthPuzzle.SneakProgress, 0f,
                "Cocoa should turn Cheddar's burp distraction into steak progress.");
        }

        private IEnumerator LoadArena()
        {
            _game = null; _cheddar = null; _cocoa = null;
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

        private static bool HasWorldPop(string text)
        {
            foreach (var pop in Object.FindObjectsByType<MissionWorldPop>(FindObjectsSortMode.None))
                if (pop.Label.Contains(text)) return true;
            return false;
        }
    }
}
