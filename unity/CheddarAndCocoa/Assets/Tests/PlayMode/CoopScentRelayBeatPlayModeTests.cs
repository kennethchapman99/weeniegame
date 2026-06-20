using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Drives the scent-relay beat from positions: the reader must be at the scent source to reveal,
    /// the digger digs the station it stands on, and digging blind (no reveal) fails.
    /// </summary>
    public sealed class CoopScentRelayBeatPlayModeTests
    {
        private GameObject _root;
        private Transform _reader;
        private Transform _digger;
        private CoopScentRelayBeat _beat;

        private static readonly Vector2 Scent = new Vector2(0f, -8f);
        private static readonly Vector2[] Targets = { new Vector2(-5f, 0f), new Vector2(0f, 5f), new Vector2(5f, 0f) };

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ScentRelayBeatTest");
            _reader = new GameObject("Reader").transform;
            _digger = new GameObject("Digger").transform;
            _reader.SetParent(_root.transform);
            _digger.SetParent(_root.transform);
            _beat = _root.AddComponent<CoopScentRelayBeat>();
            _beat.Configure(_reader, _digger, Scent, Targets, findsNeeded: 2, seed: 7);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [UnityTest]
        public IEnumerator DiggingBlind_Fails_AndRevealRequiresTheScentSource()
        {
            // Digger on a station but nobody revealed -> blind.
            _digger.position = Targets[0];
            _beat.DiggerDig();
            yield return null;
            Assert.AreEqual(0, _beat.Puzzle.Finds);
            Assert.AreEqual(1, _beat.Puzzle.BlindActs);

            // Reader tries to reveal from far away -> no reveal.
            _reader.position = new Vector3(30f, 30f, 0f);
            _beat.ReaderReveal();
            yield return null;
            Assert.IsFalse(_beat.Puzzle.Known);
        }

        [UnityTest]
        public IEnumerator ReadAtScentThenDigTheSignaledStation_Relays_ToSolve()
        {
            int guard = 0;
            while (!_beat.Puzzle.Solved && guard++ < 20)
            {
                _reader.position = Scent;
                _beat.ReaderReveal();
                Assert.IsTrue(_beat.RevealedTargetPosition.HasValue);

                _digger.position = _beat.RevealedTargetPosition.Value;
                _beat.DiggerDig();
                yield return null;
            }

            Assert.IsTrue(_beat.Puzzle.Solved);
            Assert.AreEqual(0, _beat.Puzzle.WrongDigs);
            Assert.AreEqual(0, _beat.Puzzle.BlindActs);
        }
    }
}
