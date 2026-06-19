using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Drives the Distract-and-Sneak beat from transforms: both dogs in their zones bank checkpoints,
    /// the sneaker alone makes no progress, and pulling the distractor out mid-segment gets spotted.
    /// </summary>
    public sealed class CoopDistractSneakBeatPlayModeTests
    {
        private GameObject _root;
        private Transform _distractor;
        private Transform _sneaker;
        private CoopDistractSneakBeat _beat;

        private static readonly Vector2 EnemyZone = new Vector2(-5f, 0f);
        private static readonly Vector2 SneakLane = new Vector2(5f, 0f);

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("DistractSneakBeatTest");
            _distractor = new GameObject("Distractor").transform;
            _sneaker = new GameObject("Sneaker").transform;
            _distractor.SetParent(_root.transform);
            _sneaker.SetParent(_root.transform);
            _beat = _root.AddComponent<CoopDistractSneakBeat>();
            _beat.Configure(_distractor, _sneaker, EnemyZone, SneakLane, segments: 3, segmentTime: 0.5f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [UnityTest]
        public IEnumerator BurstAndRest_BanksCheckpointsToSolve()
        {
            for (int i = 0; i < 3 && !_beat.Puzzle.Solved; i++)
            {
                // Both in position: distractor in enemy zone, sneaker in lane -> bank a checkpoint.
                _distractor.position = EnemyZone;
                _sneaker.position = SneakLane;
                _beat.Tick(0.5f);
                yield return null;

                // Rest: distractor steps out (sheds annoyance); sneaker safe at the checkpoint.
                _distractor.position = new Vector3(40f, 0f, 0f);
                _beat.Tick(0.6f);
                yield return null;
            }

            Assert.IsTrue(_beat.Puzzle.Solved);
            Assert.AreEqual(0, _beat.Puzzle.Spotted);
        }

        [UnityTest]
        public IEnumerator SneakerAlone_MakesNoProgress()
        {
            _distractor.position = new Vector3(40f, 0f, 0f); // not distracting
            _sneaker.position = SneakLane;
            for (int i = 0; i < 5; i++) { _beat.Tick(0.5f); yield return null; }
            Assert.AreEqual(0, _beat.Puzzle.Segment);
        }

        [UnityTest]
        public IEnumerator DistractorLeavingMidSegment_GetsTheSneakerSpotted()
        {
            // Long segment so the sneaker stays exposed.
            _beat.Configure(_distractor, _sneaker, EnemyZone, SneakLane, segments: 3, segmentTime: 3f);
            _distractor.position = EnemyZone;
            _sneaker.position = SneakLane;
            _beat.Tick(0.5f); // some exposed progress
            yield return null;
            Assert.Greater(_beat.Puzzle.SegmentProgress, 0f);

            _distractor.position = new Vector3(40f, 0f, 0f); // stops distracting while sneaker exposed
            for (int i = 0; i < 3; i++) { _beat.Tick(0.6f); yield return null; } // watchfulness climbs > 1
            Assert.AreEqual(1, _beat.Puzzle.Spotted);
        }
    }
}
