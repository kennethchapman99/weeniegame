using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Drives the stretch-span beat from two dog transforms: the right spacing makes the span taut and
    /// catches a centered item; standing too far apart rips it.
    /// </summary>
    public sealed class CoopStretchSpanBeatPlayModeTests
    {
        private GameObject _root;
        private Transform _a;
        private Transform _b;
        private CoopStretchSpanBeat _beat;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("StretchSpanBeatTest");
            _a = new GameObject("DogA").transform;
            _b = new GameObject("DogB").transform;
            _a.SetParent(_root.transform);
            _b.SetParent(_root.transform);
            _beat = _root.AddComponent<CoopStretchSpanBeat>();
            _beat.Configure(_a, _b, minSeparation: 1.5f, maxSeparation: 5f, catchTolerance: 1.5f,
                catchesNeeded: 2, maxRips: 3);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [UnityTest]
        public IEnumerator RightSpacingCentered_CatchesAndCanSolve()
        {
            // Dogs 3 apart around x=2 -> taut, midpoint 2.
            _a.position = new Vector3(0.5f, 0f, 0f);
            _b.position = new Vector3(3.5f, 0f, 0f);
            _beat.Tick(0.1f);
            yield return null;
            Assert.IsTrue(_beat.Puzzle.Taut);
            Assert.AreEqual(2f, _beat.SpanMidpointX, 0.001f);

            _beat.CatchItem(2.3f); // near midpoint
            _beat.CatchItem(1.7f);
            yield return null;
            Assert.IsTrue(_beat.Puzzle.Solved);
            Assert.AreEqual(0, _beat.Puzzle.Missed);
        }

        [UnityTest]
        public IEnumerator TooFarApart_RipsAndCannotCatch()
        {
            _a.position = new Vector3(-5f, 0f, 0f);
            _b.position = new Vector3(5f, 0f, 0f); // 10 apart -> over max
            _beat.Tick(0.1f);
            yield return null;
            Assert.IsTrue(_beat.Puzzle.Overstretched);
            Assert.AreEqual(1, _beat.Puzzle.Rips);

            _beat.CatchItem(0f);
            yield return null;
            Assert.AreEqual(0, _beat.Puzzle.Caught);
            Assert.AreEqual(1, _beat.Puzzle.Missed);
        }
    }
}
