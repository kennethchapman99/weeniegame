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
    /// Skunk Blast Mayhem wires the lure-and-snatch heist into the real mission flow: Cheddar's
    /// bark holds the skunk's attention while Cocoa sneaks in for the clean grab, the tail-lift
    /// telegraph is a shared danger clock both dogs must bail from, a sprayed dog goes STINKY until
    /// rubbed clean at the laundry pile, and the clean partner hauls fresh laundry from the basket
    /// to keep the pile stocked. Getting skunked is a costly detour, not a mission failure.
    /// </summary>
    public sealed class CoopSkunkBlastMayhemPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;
        private int _cheddarIndex;
        private int _cocoaIndex;
        private SkunkBlastMayhemMissionController _controller;

        [UnityTest]
        public IEnumerator SkunkBlast_RunsThroughDedicatedController()
        {
            yield return LoadArena();

            Assert.IsInstanceOf<SkunkBlastMayhemMissionController>(
                _game.ActiveMissionController,
                "Skunk Blast Mayhem must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.SkunkBlastMayhem, _game.ActiveMissionController.Variant);
            Assert.AreSame(_game.SkunkBlastMayhemController.Puzzle, _game.SkunkHeistPuzzle);
            Assert.AreEqual("skunk_blast_mayhem", _game.RuntimeSnapshot.MissionId);
        }

        [Test]
        public void SkunkBlast_RegistryContract_RoundTripsControllerAndDefinition()
        {
            var tuning = ArenaMissionTuning.CreateDefault();
            Assert.IsTrue(MissionControllerRegistry.TryCreate(GameManager.MissionVariant.SkunkBlastMayhem, out var controller));
            Assert.AreEqual(GameManager.MissionVariant.SkunkBlastMayhem, controller.Variant);
            Assert.IsTrue(MissionControllerRegistry.TryBuildDefinition(GameManager.MissionVariant.SkunkBlastMayhem, tuning, out var definition));
            Assert.AreEqual(GameManager.MissionVariant.SkunkBlastMayhem, definition.Variant);
            Assert.AreEqual("Skunk Blast Mayhem", definition.Name);
        }

        [UnityTest]
        public IEnumerator SkunkBlast_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(24, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.SkunkBlastMayhem) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "Skunk Blast Mayhem should be reachable from mission select.");
            Assert.AreEqual("Skunk Blast Mayhem", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator SkunkBlast_StartState_NoDogStinkyAndLaundryFull()
        {
            yield return LoadArena();

            var puzzle = _game.SkunkHeistPuzzle;
            Assert.IsFalse(puzzle.CheddarStinky);
            Assert.IsFalse(puzzle.CocoaStinky);
            Assert.Greater(puzzle.PileFresh, 0);
            Assert.AreEqual(puzzle.PileCapacity, puzzle.PileFresh, "The rub pile should start fully stocked.");
            Assert.AreEqual(puzzle.BasketCapacity, puzzle.BasketSupply, "The laundry basket should start fully stocked.");
            Assert.IsFalse(puzzle.PrizeSecured);
            Assert.AreEqual(1, _game.RuntimeSnapshot.ObjectiveGoal, "The bird is a single-grab prize.");
            Assert.IsFalse(_controller.IsFailed, "Fail-forward: no hard fail at mission start.");

            Assert.IsTrue(_controller.PrizeObject.activeSelf);
            Assert.IsTrue(_controller.BasketObject.activeSelf);
            Assert.IsTrue(_controller.PileObject.activeSelf);
            Assert.IsTrue(_game.PredatorObject.activeSelf, "The skunk (shared predator actor) should be guarding the yard.");
        }

        [UnityTest]
        public IEnumerator SkunkBlast_TailLiftTelegraph_IsReadableThroughThePressureHud()
        {
            yield return LoadArena();

            IMissionPressureHud hud = _controller;
            Assert.IsTrue(hud.PressureVisible);
            Assert.AreEqual("SKUNK CALM", hud.PressureLabel);

            _controller.Puzzle.ForceTailLift();
            _controller.Tick(0.01f, Time.time);
            Assert.AreEqual("TAIL UP - BAIL!", hud.PressureLabel);
            Assert.AreEqual(1f, hud.PressureNormalized, "The shared danger clock should read at maximum while the tail is up.");
        }

        [UnityTest]
        public IEnumerator SkunkBlast_BlastCone_OnlySkunksTheDogStillInRange()
        {
            yield return LoadArena();

            Vector3 skunkPos = _game.PredatorObject.transform.position;
            PlaceDog(_cheddar, skunkPos); // stays right on top of the skunk
            PlaceDog(_cocoa, skunkPos + new Vector3(40f, 40f, 0f)); // well clear of any blast radius

            _controller.Puzzle.ForceTailLift();
            _controller.Tick(_controller.Puzzle.TelegraphSeconds + 0.1f, Time.time);

            Assert.IsTrue(_game.SkunkHeistPuzzle.CheddarStinky, "Cheddar never bailed - he should get sprayed.");
            Assert.IsFalse(_game.SkunkHeistPuzzle.CocoaStinky, "Cocoa was clear across the yard - she must stay clean.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome, "Getting skunked must not end the run.");
            Assert.IsFalse(_controller.IsFailed);
        }

        [UnityTest]
        public IEnumerator SkunkBlast_BlastCone_BothDogsInRangeSkunksBoth()
        {
            yield return LoadArena();

            Vector3 skunkPos = _game.PredatorObject.transform.position;
            PlaceDog(_cheddar, skunkPos);
            PlaceDog(_cocoa, skunkPos);

            _controller.Puzzle.ForceTailLift();
            _controller.Tick(_controller.Puzzle.TelegraphSeconds + 0.1f, Time.time);

            Assert.IsTrue(_game.SkunkHeistPuzzle.BothDogsStinky,
                "Both dogs skunked at once is a valid, funny fail-forward outcome.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator SkunkBlast_StinkyDog_CannotLureUntilClean()
        {
            yield return LoadArena();

            Vector3 skunkPos = _game.PredatorObject.transform.position;
            PlaceDog(_cheddar, skunkPos);
            PlaceDog(_cocoa, skunkPos + new Vector3(40f, 40f, 0f));
            _controller.Puzzle.ForceTailLift();
            _controller.Tick(_controller.Puzzle.TelegraphSeconds + 0.1f, Time.time);
            Assert.IsTrue(_game.SkunkHeistPuzzle.CheddarStinky);

            Assert.IsTrue(_controller.HandleBark(_cheddarIndex));
            Assert.IsFalse(_controller.LureActive, "A stinky Cheddar reeks - the skunk can smell him and he can't lure.");
            Assert.That(_game.LastCue, Does.Contain("reeks"));
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested);
        }

        [UnityTest]
        public IEnumerator SkunkBlast_WrongRoles_CoachBackIntoTheLureAndSnatch()
        {
            yield return LoadArena();

            // Cocoa is the sneaker, not the lure - her bark should coach, not lure the skunk.
            PlaceDog(_cocoa, _game.PredatorObject.transform.position);
            Assert.IsTrue(_controller.HandleBark(_cocoaIndex));
            Assert.IsFalse(_controller.LureActive);
            Assert.That(_game.LastCue, Does.Contain("Cheddar"));

            // Cheddar cannot make the grab - even standing right on the prize, that's Cocoa's role.
            PlaceDog(_cheddar, _controller.PrizeObject.transform.position);
            Assert.IsTrue(_controller.HandleInteract(_cheddarIndex));
            Assert.IsFalse(_controller.Puzzle.PrizeSecured);
            Assert.That(_game.LastCue, Does.Contain("Cocoa"));
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator SkunkBlast_LaundryEconomy_RubDepletesAndHaulRestocks()
        {
            yield return LoadArena();

            var puzzle = _controller.Puzzle;
            PlaceDog(_cheddar, _game.PredatorObject.transform.position);
            PlaceDog(_cocoa, _game.PredatorObject.transform.position + Vector3.right * 40f);
            _controller.Puzzle.ForceTailLift();
            _controller.Tick(puzzle.TelegraphSeconds + 0.1f, Time.time);
            Assert.IsTrue(puzzle.CheddarStinky);
            int freshBefore = puzzle.PileFresh;

            PlaceDog(_cheddar, _controller.PileObject.transform.position);
            Assert.IsTrue(_controller.HandleInteract(_cheddarIndex));
            Assert.AreEqual(freshBefore - 1, puzzle.PileFresh, "One rub should consume exactly one fresh piece.");
            Assert.AreEqual(1, puzzle.PileFunky);

            int basketBefore = puzzle.BasketSupply;
            PlaceDog(_cocoa, _controller.BasketObject.transform.position);
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.AreEqual(basketBefore - 1, puzzle.BasketSupply, "Cocoa should haul exactly one piece from the basket.");
            Assert.AreEqual(freshBefore, puzzle.PileFresh, "...restocking the pile back toward full.");
        }

        [UnityTest]
        public IEnumerator SkunkBlast_ClearPath_LureThenSnatchSecuresTheBird()
        {
            yield return LoadArena();
            Assert.AreEqual("skunk_blast_mayhem", _game.RuntimeSnapshot.MissionId);

            PlaceDog(_cheddar, _game.PredatorObject.transform.position);
            Assert.IsTrue(_controller.HandleBark(_cheddarIndex));
            Assert.IsTrue(_controller.LureActive, "Cheddar's bark near the skunk should hold its attention.");

            PlaceDog(_cocoa, _controller.PrizeObject.transform.position);
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.IsTrue(_controller.Puzzle.PrizeSecured);
            Assert.IsTrue(_controller.IsPresentingSuccessfulOutcome,
                "The bird-secured payoff should hold briefly before the result card replaces the yard.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Bird secured"));

            _controller.ForceFinishSuccessPresentation();
            yield return null;

            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.IsTrue(_game.RuntimeSnapshot.IsClear);
            Assert.AreEqual(1, _game.RuntimeSnapshot.ObjectiveProgress);
        }

        [UnityTest]
        public IEnumerator SkunkBlast_GrabFails_WhileTailIsUpOrSkunkNotLured()
        {
            yield return LoadArena();

            // No lure yet - the skunk is still watching the bird.
            PlaceDog(_cocoa, _controller.PrizeObject.transform.position);
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.IsFalse(_controller.Puzzle.PrizeSecured);
            Assert.That(_game.LastCue, Does.Contain("watching the bird"));

            // Lure it, then catch it mid tail-up - still must not grab.
            PlaceDog(_cheddar, _game.PredatorObject.transform.position);
            _controller.HandleBark(_cheddarIndex);
            _controller.Puzzle.ForceTailLift();
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.IsFalse(_controller.Puzzle.PrizeSecured, "The tail is up - grabbing now must fail.");
            Assert.That(_game.LastCue, Does.Contain("tail is UP"));
        }

        [UnityTest]
        public IEnumerator SkunkBlast_NoHardFail_OnlySharedTimeoutEndsTheRun()
        {
            yield return LoadArena();

            PlaceDog(_cheddar, _game.PredatorObject.transform.position);
            PlaceDog(_cocoa, _game.PredatorObject.transform.position);
            for (int i = 0; i < 3; i++)
            {
                _controller.Puzzle.ForceTailLift();
                _controller.Tick(_controller.Puzzle.TelegraphSeconds + 0.1f, Time.time);
            }

            Assert.IsTrue(_controller.Puzzle.SkunkEvents > 0, "Multiple skunkings should have landed by now.");
            Assert.IsFalse(_controller.IsFailed, "Skunk Blast Mayhem is fail-forward - getting skunked never fails the run.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            // Only the shared debug/timeout hook can end this run.
            _game.ForceGameOver();
            yield return null;
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator SkunkBlast_Snapshot_MatchesInterfaceContract()
        {
            yield return LoadArena();

            var snapshot = _game.RuntimeSnapshot;
            Assert.AreEqual("skunk_blast_mayhem", snapshot.MissionId);
            Assert.AreEqual(0, snapshot.ObjectiveProgress);
            Assert.AreEqual(1, snapshot.ObjectiveGoal);
            Assert.AreEqual(0, snapshot.Mistakes);
            Assert.IsFalse(snapshot.IsClear);
            Assert.IsFalse(snapshot.IsFailed);
        }

        [UnityTest]
        public IEnumerator SkunkBlast_Replay_ResetsStinkAndLaundryState()
        {
            yield return LoadArena();

            PlaceDog(_cheddar, _game.PredatorObject.transform.position);
            _controller.Puzzle.ForceTailLift();
            _controller.Tick(_controller.Puzzle.TelegraphSeconds + 0.1f, Time.time);
            Assert.IsTrue(_controller.Puzzle.CheddarStinky);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.SkunkBlastMayhem, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            var puzzle = _game.SkunkHeistPuzzle;
            Assert.IsFalse(puzzle.CheddarStinky, "Replay must clear any stink carried from the previous attempt.");
            Assert.IsFalse(puzzle.CocoaStinky);
            Assert.AreEqual(puzzle.PileCapacity, puzzle.PileFresh, "Replay must restock the laundry pile.");
            Assert.AreEqual(puzzle.BasketCapacity, puzzle.BasketSupply, "Replay must restock the laundry basket.");
            Assert.IsFalse(puzzle.PrizeSecured);
            Assert.AreEqual(1, _game.MissionReplayCount);
        }

        private static void PlaceDog(DogController dog, Vector3 position)
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

            _game.StartMission(GameManager.MissionVariant.SkunkBlastMayhem);
            yield return null;
            _controller = _game.SkunkBlastMayhemController;
            Assert.IsNotNull(_controller);
            _cheddarIndex = _controller.DogIndexOf(DogId.Cheddar);
            _cocoaIndex = _controller.DogIndexOf(DogId.Cocoa);
        }
    }
}
