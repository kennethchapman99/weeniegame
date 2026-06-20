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
    /// The Walk Campaign mission wires the Social-Manipulation co-op puzzle into the real mission flow:
    /// the dogs con the human into a walk by sending ONE clear message built from BOTH of them at once -
    /// Cocoa's door-stare AND Cheddar presenting the leash. Cover only one (or neither) and the human
    /// gets confused and brings the wrong thing; confuse them too many times and the walk is off.
    /// </summary>
    public sealed class CoopWalkCampaignPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator Walk_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(19, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.WalkCampaign) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "The Walk Campaign should be reachable from mission select.");
            Assert.AreEqual("The Walk Campaign", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator Walk_ClearPath_BothDogsHoldTheCombo()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            Assert.AreEqual("walk_campaign", _game.RuntimeSnapshot.MissionId);

            // Both stimuli held together -> the human reads the message and takes them for a walk.
            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: true);

            Assert.IsTrue(_game.WalkCampaignPuzzle.Solved);
            Assert.AreEqual(0, _game.WalkCampaignPuzzle.Misreads);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Walkies Secured"));
        }

        [UnityTest]
        public IEnumerator Walk_OneDogAlone_NeverConvincesTheHuman()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            // Only Cocoa stares - the message is incomplete, so comprehension never builds.
            _game.ForceWalkCampaign(2f, doorStare: true, presentLeash: false);

            Assert.IsFalse(_game.WalkCampaignPuzzle.Solved);
            Assert.IsFalse(_game.WalkCampaignPuzzle.ExactMatch);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Walk_FailPath_TooManyMixedSignals()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            // Hold an incomplete message long enough to misread, three times over.
            for (int i = 0; i < 3; i++)
                _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false);

            Assert.AreEqual(3, _game.WalkCampaignPuzzle.Misreads);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Mixed Signals"));
        }

        [UnityTest]
        public IEnumerator Walk_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;
            _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: false); // a misread
            Assert.Greater(_game.WalkCampaignPuzzle.Misreads, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.WalkCampaign, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.WalkCampaignPuzzle.Misreads);
            Assert.AreEqual(0f, _game.WalkCampaignPuzzle.Comprehension);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator Walk_PositionDriven_BothStationsBuildComprehension()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.WalkCampaign);
            yield return null;

            var cheddarBody = _cheddar.GetComponent<Rigidbody2D>();
            var cocoaBody = _cocoa.GetComponent<Rigidbody2D>();

            // Both dogs cover their stations at once: the human starts getting the message.
            for (int i = 0; i < 30; i++)
            {
                _cocoa.transform.position = _game.WalkDoorZone;
                _cheddar.transform.position = _game.WalkLeashZone;
                if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            Assert.IsTrue(_game.WalkCampaignPuzzle.ExactMatch, "Both stations covered should send the exact message.");
            Assert.Greater(_game.WalkCampaignPuzzle.Comprehension, 0f);
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
    }
}
