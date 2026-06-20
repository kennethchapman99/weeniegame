using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Drives the chaos-machine cascade from junction positions: with both dogs pre-positioned at
    /// their junctions the cascade runs to the end; pull a dog off its junction and it stalls there.
    /// </summary>
    public sealed class CoopChaosMachineBeatPlayModeTests
    {
        private GameObject _root;
        private Transform _cheddar;
        private Transform _cocoa;
        private CoopChaosMachineBeat _beat;

        // towel(Cheddar) -> basket(Cocoa) -> route(Cheddar)
        private static readonly Vector2 J0 = new Vector2(-4f, 0f);
        private static readonly Vector2 J1 = new Vector2(0f, 0f);
        private static readonly Vector2 J2 = new Vector2(4f, 0f);

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ChaosMachineBeatTest");
            _cheddar = new GameObject("Cheddar").transform;
            _cocoa = new GameObject("Cocoa").transform;
            _cheddar.SetParent(_root.transform);
            _cocoa.SetParent(_root.transform);
            _beat = _root.AddComponent<CoopChaosMachineBeat>();
            _beat.Configure(
                new[] { ChainActor.Cheddar, ChainActor.Cocoa, ChainActor.Cheddar },
                new[] { J0, J1, J2 },
                _cheddar, _cocoa, windowPerStage: 1f, assistRange: 2f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [UnityTest]
        public IEnumerator BothDogsPrePositioned_CascadeRunsToTheEnd()
        {
            // Cheddar covers J0 & J2; Cocoa covers J1. Put Cheddar at J0 first; Cocoa parked at J1;
            // then Cheddar dashes to J2 after J1 clears. Simplest: keep one dog roaming to cover both
            // Cheddar junctions — here we just position per stage as the cascade advances.
            _cheddar.position = J0;
            _cocoa.position = J1;
            _beat.Trigger();

            int guard = 0;
            while (!_beat.Puzzle.Solved && guard++ < 30)
            {
                // Cheddar follows the cascade to whichever Cheddar-owned junction is active.
                if (_beat.Puzzle.Stage == 0) _cheddar.position = J0;
                else if (_beat.Puzzle.Stage == 2) _cheddar.position = J2;
                _beat.Tick(0.5f);
                yield return null;
            }

            Assert.IsTrue(_beat.Puzzle.Solved);
            Assert.AreEqual(0, _beat.Puzzle.Stalls);
        }

        [UnityTest]
        public IEnumerator MissingHelperAtAJunction_StallsThereVisibly()
        {
            _cheddar.position = J0;          // covers stage 0
            _cocoa.position = new Vector3(50f, 0f, 0f); // NOT at J1
            _beat.Trigger();

            int guard = 0;
            while (_beat.Puzzle.Running && guard++ < 30)
            {
                _beat.Tick(0.5f);
                yield return null;
            }

            Assert.IsFalse(_beat.Puzzle.Running);
            Assert.AreEqual(1, _beat.Puzzle.StalledStage, "Stalls at the basket junction Cocoa skipped.");
            Assert.AreEqual(J1, _beat.ActiveJunction);
        }
    }
}
