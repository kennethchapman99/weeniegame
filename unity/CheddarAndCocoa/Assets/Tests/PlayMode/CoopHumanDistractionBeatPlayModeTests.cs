using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Drives the dual-method distraction beat: when Cocoa commits to the belly-rub, Cheddar (the
    /// sneaker) gets a clean run in the lane; Cheddar's burst burp lets Cocoa sneak in windows.
    /// </summary>
    public sealed class CoopHumanDistractionBeatPlayModeTests
    {
        private GameObject _root;
        private Transform _cheddar;
        private Transform _cocoa;
        private CoopHumanDistractionBeat _beat;

        private static readonly Vector2 Lane = new Vector2(6f, 0f);

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("HumanDistractBeatTest");
            _cheddar = new GameObject("Cheddar").transform;
            _cocoa = new GameObject("Cocoa").transform;
            _cheddar.SetParent(_root.transform);
            _cocoa.SetParent(_root.transform);
            _beat = _root.AddComponent<CoopHumanDistractionBeat>();
            _beat.Configure(_cheddar, _cocoa, Lane,
                sneakNeeded: 2f, attentionThreshold: 0.5f, attentionDecay: 0.5f,
                burpSpike: 0.7f, burpCooldown: 2f, flopRise: 1.2f, flopStamina: 8f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [UnityTest]
        public IEnumerator CocoaFlops_CheddarSneaksTheLaneToSolve()
        {
            _cocoa.position = new Vector3(-6f, 0f, 0f); // off doing the flop
            _cheddar.position = Lane;                   // sneaker in the lane
            _beat.SetCocoaFlop(true);
            Assert.AreEqual(_cheddar, _beat.Sneaker);

            int guard = 0;
            while (!_beat.Puzzle.Solved && guard++ < 60)
            {
                _beat.Tick(0.5f);
                yield return null;
            }

            Assert.IsTrue(_beat.Puzzle.Solved);
            Assert.Greater(_beat.Puzzle.Attention, 0f);
        }

        [UnityTest]
        public IEnumerator NoDistraction_CheddarInLaneMakesNoProgress()
        {
            // Cocoa not flopped -> Cocoa is the sneaker; she's not in the lane, and nobody distracts.
            _cocoa.position = new Vector3(-20f, 0f, 0f);
            _cheddar.position = Lane;
            for (int i = 0; i < 6; i++) { _beat.Tick(0.5f); yield return null; }
            Assert.AreEqual(0f, _beat.Puzzle.SneakProgress);
        }

        [UnityTest]
        public IEnumerator CheddarBurp_LetsCocoaSneakDuringTheWindow()
        {
            _cocoa.position = Lane; // Cocoa is the sneaker when not flopped
            Assert.AreEqual(_cocoa, _beat.Sneaker);

            _beat.CheddarBurp(); // spike attention
            _beat.Tick(0.3f);    // still distracted -> Cocoa progresses
            Assert.Greater(_beat.Puzzle.SneakProgress, 0f);
            yield return null;
        }
    }
}
