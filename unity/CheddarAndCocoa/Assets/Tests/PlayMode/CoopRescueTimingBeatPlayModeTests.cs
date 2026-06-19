using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Drives the Rescue beat: a pull only counts when the free dog is next to the held dog and a
    /// weakness window is open; enough well-timed in-range pulls free the dog.
    /// </summary>
    public sealed class CoopRescueTimingBeatPlayModeTests
    {
        private GameObject _root;
        private Transform _held;
        private Transform _free;
        private CoopRescueTimingBeat _beat;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("RescueBeatTest");
            _held = new GameObject("Held").transform;
            _free = new GameObject("Free").transform;
            _held.SetParent(_root.transform);
            _free.SetParent(_root.transform);
            _held.position = Vector3.zero;
            _beat = _root.AddComponent<CoopRescueTimingBeat>();
            _beat.Configure(_held, _free, pullsNeeded: 2, windowDuration: 1f, rescueRange: 2f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [UnityTest]
        public IEnumerator PullFromTooFar_DoesNothing()
        {
            _free.position = new Vector3(20f, 0f, 0f); // way out of range
            _beat.HeldWiggle();
            _beat.FreePull();
            yield return null;
            Assert.AreEqual(0, _beat.Puzzle.Pulls);
            Assert.AreEqual(0, _beat.Puzzle.MissedPulls, "An out-of-range pull is ignored, not a miss.");
        }

        [UnityTest]
        public IEnumerator InRangeWiggleThenPull_LandsAndFreesAfterEnough()
        {
            _free.position = new Vector3(1f, 0f, 0f); // within rescue range

            _beat.HeldWiggle();
            _beat.FreePull(); // good pull 1
            Assert.AreEqual(1, _beat.Puzzle.Pulls);

            _beat.HeldWiggle();
            _beat.FreePull(); // good pull 2 -> freed
            yield return null;

            Assert.IsTrue(_beat.Puzzle.Freed);
            Assert.AreEqual(0, _beat.Puzzle.MissedPulls);
        }

        [UnityTest]
        public IEnumerator InRangePullAfterWindowCloses_Misses()
        {
            _free.position = new Vector3(1f, 0f, 0f);
            _beat.HeldWiggle();
            _beat.Tick(1.5f); // window elapses
            yield return null;
            _beat.FreePull();
            Assert.AreEqual(0, _beat.Puzzle.Pulls);
            Assert.AreEqual(1, _beat.Puzzle.MissedPulls);
        }
    }
}
