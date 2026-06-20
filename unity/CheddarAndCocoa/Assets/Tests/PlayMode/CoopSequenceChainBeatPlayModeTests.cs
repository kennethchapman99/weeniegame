using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Drives the discrete-interaction chain beat from station positions: interacting at the right
    /// station with the right dog advances the contraption; wrong place or wrong dog does not.
    /// </summary>
    public sealed class CoopSequenceChainBeatPlayModeTests
    {
        private GameObject _go;
        private CoopSequenceChainBeat _beat;

        private static readonly Vector2 LatchStation = new Vector2(-4f, 0f);
        private static readonly Vector2 GateStation = new Vector2(0f, 0f);
        private static readonly Vector2 ThroughStation = new Vector2(4f, 0f);

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ChainBeatTest");
            _beat = _go.AddComponent<CoopSequenceChainBeat>();
            // Garden gate: Cheddar latch, Cocoa gate, Cheddar through; no settle for a clean test.
            _beat.Configure(
                new[] { ChainActor.Cheddar, ChainActor.Cocoa, ChainActor.Cheddar },
                new[] { LatchStation, GateStation, ThroughStation },
                settleTime: 0f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [UnityTest]
        public IEnumerator RightDogAtRightStation_AdvancesTheChainToSolve()
        {
            _beat.Interact(ChainActor.Cheddar, LatchStation);
            yield return null;
            Assert.AreEqual(1, _beat.Puzzle.Step);
            Assert.AreEqual(GateStation, _beat.CurrentStation);

            _beat.Interact(ChainActor.Cocoa, GateStation);
            yield return null;
            Assert.AreEqual(2, _beat.Puzzle.Step);

            _beat.Interact(ChainActor.Cheddar, ThroughStation);
            yield return null;
            Assert.IsTrue(_beat.Puzzle.Solved);
            Assert.AreEqual(0, _beat.Puzzle.Fumbles);
        }

        [UnityTest]
        public IEnumerator InteractingFarFromTheStation_DoesNothing()
        {
            _beat.Interact(ChainActor.Cheddar, new Vector2(30f, 30f)); // right dog, wrong place
            yield return null;
            Assert.AreEqual(0, _beat.Puzzle.Step);
            Assert.AreEqual(0, _beat.Puzzle.Fumbles, "Being far away is a no-op, not a fumble.");
        }

        [UnityTest]
        public IEnumerator WrongDogAtTheStation_Fumbles()
        {
            _beat.Interact(ChainActor.Cocoa, LatchStation); // step 1 belongs to Cheddar
            yield return null;
            Assert.AreEqual(0, _beat.Puzzle.Step);
            Assert.AreEqual(1, _beat.Puzzle.Fumbles);
        }
    }
}
