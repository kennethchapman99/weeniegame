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
    /// The Bone Detail mission wires the Scent-Relay split-information co-op puzzle into the real mission
    /// flow: Cocoa (the reader) calls which look-alike mound holds the bone; Cheddar (the digger) is the
    /// only one who can dig but can't tell them apart, so he must wait for her call. Digging blind or
    /// digging a decoy wastes a dig; too many wasted digs fail the mission.
    /// </summary>
    public sealed class CoopBoneRelayPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;

        [UnityTest]
        public IEnumerator Bone_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(19, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.BoneRelay) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "The Bone Detail should be reachable from mission select.");
            Assert.AreEqual("The Bone Detail", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator Bone_ClearPath_RevealThenDigTheCallThreeTimes()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BoneRelay);
            yield return null;

            Assert.AreEqual("bone_relay", _game.RuntimeSnapshot.MissionId);

            for (int i = 0; i < 3; i++)
            {
                _game.ForceBoneReveal();                              // Cocoa calls the real mound
                Assert.IsTrue(_game.BoneRelayPuzzle.Known);
                _game.ForceBoneDig(_game.BoneRelayPuzzle.CorrectTarget); // Cheddar digs the called mound
            }

            Assert.IsTrue(_game.BoneRelayPuzzle.Solved);
            Assert.AreEqual(3, _game.BoneRelayPuzzle.Finds);
            Assert.AreEqual(0, _game.BoneRelayPuzzle.BlindActs + _game.BoneRelayPuzzle.WrongDigs);
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Bones Recovered"));
        }

        [UnityTest]
        public IEnumerator Bone_DigBeforeCall_IsABlindDig()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BoneRelay);
            yield return null;

            _game.ForceBoneDig(0); // no reveal yet -> blind guess, no find

            Assert.AreEqual(0, _game.BoneRelayPuzzle.Finds);
            Assert.AreEqual(1, _game.BoneRelayPuzzle.BlindActs);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Bone_DigWrongMound_IsAHarmlessDecoy()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BoneRelay);
            yield return null;

            _game.ForceBoneReveal();
            int wrong = (_game.BoneRelayPuzzle.CorrectTarget + 1) % _game.BoneMoundCount;
            _game.ForceBoneDig(wrong);

            Assert.AreEqual(0, _game.BoneRelayPuzzle.Finds);
            Assert.AreEqual(1, _game.BoneRelayPuzzle.WrongDigs);
            Assert.IsTrue(_game.BoneRelayPuzzle.Known, "A wrong dig should not consume the reader's call.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator Bone_FailPath_TooManyWastedDigs()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BoneRelay);
            yield return null;

            for (int i = 0; i < 5; i++)
                _game.ForceBoneDig(0); // five blind digs -> the team gives up

            Assert.AreEqual(5, _game.BoneRelayPuzzle.BlindActs);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, _game.Phase);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Dug Up The Yard"));
        }

        [UnityTest]
        public IEnumerator Bone_Replay_ResetsThePuzzle()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BoneRelay);
            yield return null;
            _game.ForceBoneDig(0); // a blind dig
            Assert.Greater(_game.BoneRelayPuzzle.BlindActs, 0);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.BoneRelay, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.BoneRelayPuzzle.Finds);
            Assert.AreEqual(0, _game.BoneRelayPuzzle.BlindActs);
            Assert.IsFalse(_game.BoneRelayPuzzle.Known);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator Bone_PositionDriven_ReadAtPostThenDigTheCall()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.BoneRelay);
            yield return null;

            var cheddarBody = _cheddar.GetComponent<Rigidbody2D>();
            var cocoaBody = _cocoa.GetComponent<Rigidbody2D>();

            // Keep Cheddar clear of every mound while Cocoa noses the scent post to call the real one.
            int correct = -1;
            for (int i = 0; i < 10; i++)
            {
                _cocoa.transform.position = _game.BoneScentZone;
                _cheddar.transform.position = new Vector3(_game.BoneScentZone.x, _game.BoneScentZone.y, 0f);
                if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            Assert.IsTrue(_game.BoneRelayPuzzle.Known, "Cocoa at the scent post should call the mound.");
            correct = _game.BoneRelayPuzzle.CorrectTarget;

            // Cheddar runs in and digs the called mound; Cocoa stays on the post.
            for (int i = 0; i < 10; i++)
            {
                _cocoa.transform.position = _game.BoneScentZone;
                _cheddar.transform.position = _game.BoneMoundSpot(correct);
                if (cocoaBody != null) cocoaBody.linearVelocity = Vector2.zero;
                if (cheddarBody != null) cheddarBody.linearVelocity = Vector2.zero;
                yield return null;
            }
            Assert.GreaterOrEqual(_game.BoneRelayPuzzle.Finds, 1, "Digging the called mound should find a bone.");
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
