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
    /// Burr Maze wires the stealth patrol-cone beat into the real mission flow: a facing-direction
    /// vision cone sweeps a patrol route, a dog lingering in it too long gets both dogs swept back to
    /// the last safe checkpoint (a stealth-retry loop, never a hard fail), hiding spots hard-reset an
    /// in-progress notice-dwell, bramble patches cake a dog in burrs asymmetrically (a real movement
    /// speed penalty, not just a meter), a partner-held burr-pick requires both dogs stationary and
    /// exposed, bark redirects the patrol's attention at the cost of the barker's own cover, and a
    /// second faster patrol activates partway through on a distinct real route.
    /// </summary>
    public sealed class CoopBurrMazePlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;
        private int _cheddarIndex;
        private int _cocoaIndex;
        private BurrMazeMissionController _controller;

        [UnityTest]
        public IEnumerator BurrMaze_RunsThroughDedicatedController()
        {
            yield return LoadArena();

            Assert.IsInstanceOf<BurrMazeMissionController>(
                _game.ActiveMissionController,
                "Burr Maze must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.BurrMaze, _game.ActiveMissionController.Variant);
            Assert.AreSame(_game.BurrMazeController.Puzzle, _game.BurrMazePuzzle);
            Assert.AreEqual("burr_maze", _game.RuntimeSnapshot.MissionId);
        }

        [Test]
        public void BurrMaze_RegistryContract_RoundTripsControllerAndDefinition()
        {
            var tuning = ArenaMissionTuning.CreateDefault();
            Assert.IsTrue(MissionControllerRegistry.TryCreate(GameManager.MissionVariant.BurrMaze, out var controller));
            Assert.AreEqual(GameManager.MissionVariant.BurrMaze, controller.Variant);
            Assert.IsTrue(MissionControllerRegistry.TryBuildDefinition(GameManager.MissionVariant.BurrMaze, tuning, out var definition));
            Assert.AreEqual(GameManager.MissionVariant.BurrMaze, definition.Variant);
            Assert.AreEqual("Burr Maze", definition.Name);
        }

        [UnityTest]
        public IEnumerator BurrMaze_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(26, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.BurrMaze) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "Burr Maze should be reachable from mission select.");
            Assert.AreEqual("Burr Maze", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator BurrMaze_StartState_NoBurrsNoDetectionFirstCheckpointPrimaryPatrolOnly()
        {
            yield return LoadArena();

            var puzzle = _game.BurrMazePuzzle;
            Assert.AreEqual(0f, puzzle.CheddarBurr, 0.001f);
            Assert.AreEqual(0f, puzzle.CocoaBurr, 0.001f);
            Assert.AreEqual(0, puzzle.Detections);
            Assert.AreEqual(0, puzzle.CheckpointIndex);
            Assert.IsFalse(puzzle.Cleared);
            Assert.IsFalse(puzzle.TwistPatrolActive);
            Assert.IsFalse(_controller.IsFailed);
            Assert.IsTrue(_controller.CatObject.activeSelf, "The primary cat patrol should be active from mission start.");
            Assert.IsFalse(_controller.KittenObject.activeSelf, "The twist kitten patrol should not be active yet.");

            float distCheddar = Vector2.Distance(_cheddar.transform.position, _controller.CheckpointPosition(0));
            float distCocoa = Vector2.Distance(_cocoa.transform.position, _controller.CheckpointPosition(0));
            Assert.LessOrEqual(distCheddar, 3f, "Cheddar should start near the first checkpoint.");
            Assert.LessOrEqual(distCocoa, 3f, "Cocoa should start near the first checkpoint.");
        }

        [UnityTest]
        public IEnumerator ConeDetection_StandingAheadOfThePatrolEventuallyTriggersDetection()
        {
            yield return LoadArena();
            ParkSafely(_cocoa);

            int guard = 0;
            while (_controller.Puzzle.Detections == 0 && guard++ < 30)
            {
                PlaceCheddarAheadOfCat(1f);
                _controller.Tick(0.1f, Time.time);
            }

            Assert.Greater(_controller.Puzzle.Detections, 0, "Lingering ahead of the sweeping cone should eventually trigger a detection.");
            Assert.Greater(guard, 3, "Detection should take a brief dwell, not fire instantly on the very first tick.");
        }

        [UnityTest]
        public IEnumerator ConeDetection_NeverInConeNeverTriggers()
        {
            yield return LoadArena();
            ParkSafely(_cheddar);
            ParkSafely(_cocoa);

            for (int i = 0; i < 30; i++) _controller.Tick(0.1f, Time.time);

            Assert.AreEqual(0, _controller.Puzzle.Detections, "Dogs that never enter the cone should never be detected.");
        }

        [UnityTest]
        public IEnumerator ConeDetection_LeavingTheConeResetsTheDwell()
        {
            yield return LoadArena();
            ParkSafely(_cocoa);

            for (int i = 0; i < 8; i++)
            {
                PlaceCheddarAheadOfCat(1f);
                _controller.Tick(0.1f, Time.time);
            }
            Assert.AreEqual(0, _controller.Puzzle.Detections, "Not yet detected after a partial dwell.");
            Assert.IsTrue(_controller.Puzzle.IsAboutToBeNoticed(DogId.Cheddar), "Partial dwell should already read as about-to-be-noticed.");

            ParkSafely(_cheddar); // leaves the cone entirely
            _controller.Tick(0.1f, Time.time);
            Assert.IsFalse(_controller.Puzzle.IsAboutToBeNoticed(DogId.Cheddar), "Leaving the cone should reset the dwell.");
        }

        [UnityTest]
        public IEnumerator HidingSpot_ResetsAnInProgressDwell()
        {
            yield return LoadArena();
            ParkSafely(_cocoa);

            for (int i = 0; i < 8; i++)
            {
                PlaceCheddarAheadOfCat(1f);
                _controller.Tick(0.1f, Time.time);
            }
            Assert.IsTrue(_controller.Puzzle.IsAboutToBeNoticed(DogId.Cheddar), "Partial dwell before ducking into cover.");

            PlaceDog(_cheddar, _controller.HidePosition(0));
            _controller.Tick(0.1f, Time.time);
            Assert.IsFalse(_controller.Puzzle.IsAboutToBeNoticed(DogId.Cheddar), "Ducking into a hiding spot must hard-reset the dwell.");
            Assert.AreEqual(0, _controller.Puzzle.Detections);
        }

        [UnityTest]
        public IEnumerator Detection_ResetsBothDogsToTheLastCheckpoint_AndIsNotTreatedAsFailure()
        {
            yield return LoadArena();
            ParkSafely(_cocoa);

            int guard = 0;
            while (_controller.Puzzle.Detections == 0 && guard++ < 30)
            {
                PlaceCheddarAheadOfCat(1f);
                _controller.Tick(0.1f, Time.time);
            }
            Assert.Greater(_controller.Puzzle.Detections, 0);

            Assert.IsFalse(_controller.IsFailed, "A detection is a stealth-retry loop, never the controller's own fail condition.");
            Assert.IsNull(_controller.FailReason);
            float distCheddar = Vector2.Distance(_cheddar.transform.position, _controller.CheckpointPosition(_controller.Puzzle.CheckpointIndex));
            Assert.LessOrEqual(distCheddar, 2.5f, "A detection should sweep the dog back to the last safe checkpoint.");
        }

        [UnityTest]
        public IEnumerator BurrAccrual_BrambleContact_CheddarRisesMeasurablyFasterThanCocoa()
        {
            yield return LoadArena();

            PlaceDog(_cheddar, _controller.BramblePosition(0));
            PlaceDog(_cocoa, _controller.BramblePosition(0));
            _controller.Tick(0.8f, Time.time);

            Assert.AreEqual(0, _controller.Puzzle.Detections, "This short exposure should stay well under the detection dwell threshold.");
            Assert.Greater(_controller.Puzzle.CheddarBurr, 0f, "Cheddar should have picked up burrs from bramble contact.");
            Assert.Greater(_controller.Puzzle.CocoaBurr, 0f, "Cocoa should have picked up burrs from bramble contact too.");
            Assert.Greater(_controller.Puzzle.CheddarBurr, _controller.Puzzle.CocoaBurr,
                "Cheddar's reckless bramble charges should cake him faster than Cocoa over identical exposure.");
        }

        [UnityTest]
        public IEnumerator BurrPenalty_CakedDog_IsRealSlowerOnTheActualDogController()
        {
            yield return LoadArena();
            ParkSafely(_cheddar);
            ParkSafely(_cocoa);

            // Bypass geometry to cake Cheddar deterministically, then let one real Tick propagate the
            // resulting speed penalty onto the live DogController - the mission-specific wiring this
            // integration test exists to prove (the pure math is covered by the puzzle unit tests).
            _controller.Puzzle.Advance(4f,
                new CoopBurrMazePuzzle.DogExposure(true, false, false, false),
                new CoopBurrMazePuzzle.DogExposure(false, false, false, false));
            Assert.IsTrue(_controller.Puzzle.IsBurrCaked(DogId.Cheddar));

            _controller.Tick(0.05f, Time.time);

            Assert.AreEqual(0.6f, _cheddar.SpeedPenaltyMultiplier, 0.001f, "A caked dog's real movement speed multiplier should reflect the penalty.");
            Assert.AreEqual(1f, _cocoa.SpeedPenaltyMultiplier, 0.001f, "A clean dog should carry no speed penalty.");
            Assert.Less(_cheddar.MaxSpeedUnitsPerSecond, _cocoa.MaxSpeedUnitsPerSecond,
                "The caked dog's real max speed should measurably drop below the clean partner's.");
        }

        [UnityTest]
        public IEnumerator BurrPick_ReducesTargetBurrWhileBothStationaryAndClose()
        {
            yield return LoadArena();

            Vector2 spot = _game.ArenaBounds.center + new Vector2(20f, 15f); // well clear of any patrol/bramble/hide zone
            _controller.Puzzle.Advance(3f,
                new CoopBurrMazePuzzle.DogExposure(true, false, false, false),
                new CoopBurrMazePuzzle.DogExposure(false, false, false, false));
            float before = _controller.Puzzle.CheddarBurr;
            Assert.Greater(before, 0f);

            PlaceDog(_cheddar, spot);
            PlaceDog(_cocoa, spot);
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex), "Cocoa starts picking Cheddar's burrs.");
            Assert.AreEqual(DogId.Cheddar, _controller.Puzzle.PickTarget);

            _controller.Tick(1f, Time.time);
            Assert.Less(_controller.Puzzle.CheddarBurr, before, "An active, valid burr-pick should reduce the target's meter over time.");
        }

        [UnityTest]
        public IEnumerator BurrPick_FailsWhenTooFarApart()
        {
            yield return LoadArena();

            _controller.Puzzle.Advance(3f,
                new CoopBurrMazePuzzle.DogExposure(true, false, false, false),
                new CoopBurrMazePuzzle.DogExposure(false, false, false, false));

            PlaceDog(_cheddar, _game.ArenaBounds.center);
            PlaceDog(_cocoa, _game.ArenaBounds.center + new Vector2(20f, 0f));
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.IsNull(_controller.Puzzle.PickTarget, "Too far apart - no pick should engage.");
        }

        [UnityTest]
        public IEnumerator BurrPick_BreaksIfEitherDogStopsBeingStationary()
        {
            yield return LoadArena();

            Vector2 spot = _game.ArenaBounds.center + new Vector2(20f, 15f);
            _controller.Puzzle.Advance(3f,
                new CoopBurrMazePuzzle.DogExposure(true, false, false, false),
                new CoopBurrMazePuzzle.DogExposure(false, false, false, false));
            PlaceDog(_cheddar, spot);
            PlaceDog(_cocoa, spot);
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.IsNotNull(_controller.Puzzle.PickTarget);

            if (_cocoa.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = new Vector2(6f, 0f);
            _controller.Tick(0.1f, Time.time);

            Assert.IsNull(_controller.Puzzle.PickTarget, "A dog moving mid-pick should break the hold - it requires both dogs stationary.");
            Assert.AreEqual(0, _controller.Puzzle.Detections, "This break came from movement, not a patrol detection.");
        }

        [UnityTest]
        public IEnumerator BurrPick_APatrolDetectionInterruptsAnInProgressPick()
        {
            yield return LoadArena();

            // A fixed point 3 units ahead of the cat's own starting position: as the cat patrols
            // toward it, it stays squarely in front (angle ~0) the whole approach, and both dogs stay
            // put here the entire test - so the ONLY thing that can end the pick is the detection
            // itself, never a proximity/stationary break.
            Vector2 spot = (Vector2)_controller.CatObject.transform.position + Vector2.right * 3f;
            _controller.Puzzle.Advance(3f,
                new CoopBurrMazePuzzle.DogExposure(true, false, false, false),
                new CoopBurrMazePuzzle.DogExposure(false, false, false, false));
            float before = _controller.Puzzle.CheddarBurr;
            Assert.Greater(before, 0f);

            PlaceDog(_cheddar, spot);
            PlaceDog(_cocoa, spot);
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.IsNotNull(_controller.Puzzle.PickTarget);

            int guard = 0;
            while (_controller.Puzzle.Detections == 0 && guard++ < 30)
                _controller.Tick(0.1f, Time.time);

            Assert.Greater(_controller.Puzzle.Detections, 0, "The stationary pair should eventually be swept by the approaching patrol.");
            Assert.IsNull(_controller.Puzzle.PickTarget, "The detection should interrupt the in-progress pick.");
            Assert.Greater(_controller.Puzzle.CheddarBurr, 0f, "The pick was interrupted, not completed - burrs should remain.");
        }

        [UnityTest]
        public IEnumerator BarkLure_RedirectsThePatrolTowardTheBarker_WithoutResettingHisOwnDwell()
        {
            yield return LoadArena();
            ParkSafely(_cocoa);

            for (int i = 0; i < 8; i++)
            {
                PlaceCheddarAheadOfCat(1f);
                _controller.Tick(0.1f, Time.time);
            }
            Assert.AreEqual(0, _controller.Puzzle.Detections, "Should not have been detected yet.");
            Assert.IsTrue(_controller.Puzzle.IsAboutToBeNoticed(DogId.Cheddar), "Should have built up real dwell by now.");

            Vector2 barkerPos = _cheddar.transform.position; // stays put for the bark itself
            Vector2 catBefore = _controller.CatObject.transform.position;
            Assert.IsTrue(_controller.HandleBark(_cheddarIndex));
            Assert.IsTrue(_controller.Puzzle.LureActive);
            Assert.AreEqual(DogId.Cheddar, _controller.Puzzle.LureDog);
            Assert.IsTrue(_controller.Puzzle.IsAboutToBeNoticed(DogId.Cheddar),
                "Barking must NOT reset the barker's own notice-dwell - it is not a hiding action.");

            _controller.Tick(0.3f, Time.time);
            float distBefore = Vector2.Distance(catBefore, barkerPos);
            float distAfter = Vector2.Distance(_controller.CatObject.transform.position, barkerPos);
            Assert.Less(distAfter, distBefore, "The lured cat should visibly move closer to the barking dog's position.");
        }

        [UnityTest]
        public IEnumerator BarkLure_RequiresBeingOutOfCover()
        {
            yield return LoadArena();

            PlaceDog(_cheddar, _controller.HidePosition(0));
            Assert.IsTrue(_controller.HandleBark(_cheddarIndex));
            Assert.IsFalse(_controller.Puzzle.LureActive, "Barking from inside cover should not lure the cat.");
        }

        [UnityTest]
        public IEnumerator TwistPatrol_ActivatesOnASecondRealActorWithADistinctRoute()
        {
            yield return LoadArena();

            Assert.IsFalse(_controller.KittenObject.activeSelf, "The twist patrol should be dormant before activation.");
            float catStartY = _controller.CatObject.transform.position.y;

            _controller.Puzzle.ForceTwistPatrolActive();
            _controller.Tick(0.5f, Time.time);
            Vector2 kittenAfterFirstTick = _controller.KittenObject.transform.position;

            Assert.IsTrue(_controller.KittenObject.activeSelf, "The twist patrol should spawn a real, active second actor.");
            Assert.AreNotSame(_controller.CatObject, _controller.KittenObject, "The twist patrol must be a distinct actor from the primary cat.");

            _controller.Tick(0.5f, Time.time);
            Vector2 kittenAfterSecondTick = _controller.KittenObject.transform.position;
            float catAfterY = _controller.CatObject.transform.position.y;

            Assert.Greater(Mathf.Abs(kittenAfterSecondTick.y - kittenAfterFirstTick.y), 0.01f,
                "The kitten should keep moving along its own vertical route tick over tick.");
            Assert.AreEqual(catStartY, catAfterY, 0.01f, "The primary cat's route stays on its own fixed horizontal lane, unaffected by the twist patrol.");
        }

        [UnityTest]
        public IEnumerator ClearPath_ReachingEveryCheckpointClearsTheMission()
        {
            yield return LoadArena();

            for (int cp = 1; cp <= 3; cp++)
            {
                Vector2 target = _controller.CheckpointPosition(cp);
                PlaceDog(_cheddar, target);
                PlaceDog(_cocoa, target + new Vector2(0.2f, 0f));
                _controller.Tick(0.05f, Time.time);
                Assert.AreEqual(cp, _controller.Puzzle.CheckpointIndex, $"Should have banked checkpoint {cp}.");
            }

            Assert.IsTrue(_controller.Puzzle.Cleared);
            Assert.IsTrue(_controller.IsPresentingSuccessfulOutcome);
            _controller.ForceFinishSuccessPresentation();
            _controller.Tick(0.01f, Time.time);
            Assert.IsTrue(_controller.IsComplete);
        }

        [UnityTest]
        public IEnumerator Snapshot_MatchesInterfaceContract()
        {
            yield return LoadArena();

            var snapshot = _game.RuntimeSnapshot;
            Assert.AreEqual("burr_maze", snapshot.MissionId);
            Assert.AreEqual(0, snapshot.ObjectiveProgress);
            Assert.AreEqual(3, snapshot.ObjectiveGoal);
            Assert.AreEqual(0, snapshot.Mistakes);
            Assert.IsFalse(snapshot.IsClear);
            Assert.IsFalse(snapshot.IsFailed);
        }

        [UnityTest]
        public IEnumerator Replay_ResetsBurrsDetectionsCheckpointAndTwistState()
        {
            yield return LoadArena();

            PlaceDog(_cheddar, _controller.BramblePosition(0));
            _controller.Tick(2f, Time.time);
            _controller.Puzzle.ForceTwistPatrolActive();
            _controller.Puzzle.ForceCheckpoint(2);
            _controller.Puzzle.ForceDetection();
            Assert.Greater(_controller.Puzzle.CheddarBurr, 0f);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.BurrMaze, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            var puzzle = _game.BurrMazePuzzle;
            Assert.AreEqual(0f, puzzle.CheddarBurr, 0.001f, "Replay must clear burrs carried from the previous attempt.");
            Assert.AreEqual(0f, puzzle.CocoaBurr, 0.001f);
            Assert.AreEqual(0, puzzle.Detections, "Replay must clear detections.");
            Assert.AreEqual(0, puzzle.CheckpointIndex, "Replay must reset checkpoint progress.");
            Assert.IsFalse(puzzle.TwistPatrolActive, "Replay must deactivate the twist patrol.");
            Assert.IsFalse(_controller.KittenObject.activeSelf);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        private void PlaceCheddarAheadOfCat(float ahead)
        {
            Vector2 catPos = _controller.CatObject.transform.position;
            PlaceDog(_cheddar, catPos + Vector2.right * ahead);
        }

        private void ParkSafely(DogController dog) =>
            PlaceDog(dog, _game.ArenaBounds.center + new Vector2(-40f, -25f));

        private static void PlaceDog(DogController dog, Vector2 position)
        {
            dog.transform.position = position;
            if (dog.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
        }

        private IEnumerator LoadArena()
        {
            _game = null; _cheddar = null; _cocoa = null; _controller = null;
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

            _game.StartMission(GameManager.MissionVariant.BurrMaze);
            yield return null;
            _controller = _game.BurrMazeController;
            Assert.IsNotNull(_controller);
            _cheddarIndex = _controller.DogIndexOf(DogId.Cheddar);
            _cocoaIndex = _controller.DogIndexOf(DogId.Cocoa);
        }
    }
}
