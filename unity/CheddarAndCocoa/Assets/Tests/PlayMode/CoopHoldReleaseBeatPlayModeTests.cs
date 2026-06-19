using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// Drives the Hold-and-Release beat component from moving transforms (standing in / leaving the
    /// hold and cross zones) to prove the spatial wiring matches the pure-logic puzzle.
    /// </summary>
    public sealed class CoopHoldReleaseBeatPlayModeTests
    {
        private GameObject _root;
        private Transform _anchor;
        private Transform _crosser;
        private CoopHoldReleaseBeat _beat;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("HoldReleaseBeatTest");
            _anchor = new GameObject("Anchor").transform;
            _crosser = new GameObject("Crosser").transform;
            _anchor.SetParent(_root.transform);
            _crosser.SetParent(_root.transform);
            _beat = _root.AddComponent<CoopHoldReleaseBeat>();
            // cross needs 1s of held crossing; anchor patience window 5s; 2u zone radii.
            _beat.Configure(_anchor, _crosser, new Vector2(-3f, 0f), new Vector2(3f, 0f), 1f, 5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator BothDogsInPosition_SolvesTheBeat()
        {
            _anchor.position = new Vector3(-3f, 0f, 0f); // in hold zone
            _crosser.position = new Vector3(3f, 0f, 0f); // in cross corridor
            yield return null;

            for (int i = 0; i < 20 && !_beat.Puzzle.Solved; i++)
            {
                _beat.Tick(0.1f);
                yield return null;
            }

            Assert.IsTrue(_beat.Puzzle.Solved, "With anchor holding and crosser engaged, the beat solves.");
            Assert.AreEqual(0, _beat.Puzzle.Snaps);
        }

        [UnityTest]
        public IEnumerator CrosserAlone_MakesNoProgress()
        {
            _anchor.position = new Vector3(20f, 0f, 0f); // NOT holding
            _crosser.position = new Vector3(3f, 0f, 0f); // engaged but unsupported
            yield return null;

            for (int i = 0; i < 10; i++) { _beat.Tick(0.1f); yield return null; }

            Assert.AreEqual(0f, _beat.Puzzle.CrossProgress, "No progress without the anchor holding.");
            Assert.IsFalse(_beat.Puzzle.Solved);
        }

        [UnityTest]
        public IEnumerator AnchorLeavingMidCross_SnapsItBack()
        {
            _anchor.position = new Vector3(-3f, 0f, 0f);
            _crosser.position = new Vector3(3f, 0f, 0f);
            yield return null;

            _beat.Tick(0.4f); // partial progress
            yield return null;
            Assert.Greater(_beat.Puzzle.CrossProgress, 0f);

            _anchor.position = new Vector3(20f, 0f, 0f); // anchor wanders off
            _beat.Tick(0.1f);
            yield return null;

            Assert.AreEqual(1, _beat.Puzzle.Snaps, "Anchor leaving mid-cross snaps the beat.");
            Assert.AreEqual(0f, _beat.Puzzle.CrossProgress);
        }
    }
}
