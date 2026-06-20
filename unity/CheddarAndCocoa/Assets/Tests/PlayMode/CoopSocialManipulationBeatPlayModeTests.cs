using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Drives the social-manipulation beat from positions: only when BOTH dogs cover their stimulus
    /// stations is the required combo active and the human gets the message; one dog alone stalls.
    /// </summary>
    public sealed class CoopSocialManipulationBeatPlayModeTests
    {
        private GameObject _root;
        private Transform _cheddar;
        private Transform _cocoa;
        private CoopSocialManipulationBeat _beat;

        private static readonly Vector2 DoorPos = new Vector2(-4f, 0f);
        private static readonly Vector2 LeashPos = new Vector2(4f, 0f);
        private const SocialStimulus Walk = SocialStimulus.DoorStare | SocialStimulus.PresentLeash;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("SocialBeatTest");
            _cheddar = new GameObject("Cheddar").transform;
            _cocoa = new GameObject("Cocoa").transform;
            _cheddar.SetParent(_root.transform);
            _cocoa.SetParent(_root.transform);
            _beat = _root.AddComponent<CoopSocialManipulationBeat>();
            _beat.Configure(
                new[]
                {
                    new CoopSocialManipulationBeat.StimulusStation { Flag = SocialStimulus.DoorStare, Owner = ChainActor.Cocoa, Position = DoorPos },
                    new CoopSocialManipulationBeat.StimulusStation { Flag = SocialStimulus.PresentLeash, Owner = ChainActor.Cheddar, Position = LeashPos },
                },
                Walk, comprehendNeeded: 1f, confusionMax: 6f, _cheddar, _cocoa, range: 2f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [UnityTest]
        public IEnumerator BothDogsCoverStations_HumanGetsTheMessage()
        {
            _cocoa.position = DoorPos;
            _cheddar.position = LeashPos;
            Assert.AreEqual(Walk, _beat.CurrentActiveSet());

            int guard = 0;
            while (!_beat.Puzzle.Solved && guard++ < 20)
            {
                _beat.Tick(0.5f);
                yield return null;
            }
            Assert.IsTrue(_beat.Puzzle.Solved);
            Assert.AreEqual(0, _beat.Puzzle.Misreads);
        }

        [UnityTest]
        public IEnumerator OneDogAlone_StallsAndConfuses()
        {
            _cocoa.position = DoorPos;                    // door-stare only
            _cheddar.position = new Vector3(40f, 0f, 0f); // leash not presented
            Assert.AreEqual(SocialStimulus.DoorStare, _beat.CurrentActiveSet());

            for (int i = 0; i < 4; i++) { _beat.Tick(0.5f); yield return null; }
            Assert.IsFalse(_beat.Puzzle.Solved);
            Assert.Greater(_beat.Puzzle.Confusion, 0f);
        }
    }
}
