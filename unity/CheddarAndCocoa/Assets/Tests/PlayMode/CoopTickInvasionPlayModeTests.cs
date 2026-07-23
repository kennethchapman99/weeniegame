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
    /// Tick Invasion wires the mutual-grooming survival beat into the real mission flow: both dogs
    /// accumulate ticks continuously, grooming a nearby partner trades ticks between them, an erratic
    /// dog resists a normal groom until barked calm, a pool dive instantly resets a dog but leaves
    /// them wet and double-accumulating, a Super Tick locks onto one dog and resists grooming from
    /// either direction, and either dog maxing out ticks is a hard fail (unlike Skunk Blast Mayhem's
    /// fail-forward design).
    /// </summary>
    public sealed class CoopTickInvasionPlayModeTests
    {
        private GameManager _game;
        private DogController _cheddar;
        private DogController _cocoa;
        private int _cheddarIndex;
        private int _cocoaIndex;
        private TickInvasionMissionController _controller;

        [UnityTest]
        public IEnumerator TickInvasion_RunsThroughDedicatedController()
        {
            yield return LoadArena();

            Assert.IsInstanceOf<TickInvasionMissionController>(
                _game.ActiveMissionController,
                "Tick Invasion must run entirely through its own IMissionController.");
            Assert.AreEqual(GameManager.MissionVariant.TickInvasion, _game.ActiveMissionController.Variant);
            Assert.AreSame(_game.TickInvasionController.Puzzle, _game.TickInvasionPuzzle);
            Assert.AreEqual("tick_invasion", _game.RuntimeSnapshot.MissionId);
        }

        [Test]
        public void TickInvasion_RegistryContract_RoundTripsControllerAndDefinition()
        {
            var tuning = ArenaMissionTuning.CreateDefault();
            Assert.IsTrue(MissionControllerRegistry.TryCreate(GameManager.MissionVariant.TickInvasion, out var controller));
            Assert.AreEqual(GameManager.MissionVariant.TickInvasion, controller.Variant);
            Assert.IsTrue(MissionControllerRegistry.TryBuildDefinition(GameManager.MissionVariant.TickInvasion, tuning, out var definition));
            Assert.AreEqual(GameManager.MissionVariant.TickInvasion, definition.Variant);
            Assert.AreEqual("Tick Invasion", definition.Name);
        }

        [UnityTest]
        public IEnumerator TickInvasion_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            Assert.AreEqual(26, _game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < _game.MissionSelectOptionCount; i++)
            {
                if (_game.SelectedMissionVariant == GameManager.MissionVariant.TickInvasion) { found = true; break; }
                _game.SelectNextMission();
                yield return null;
            }
            Assert.IsTrue(found, "Tick Invasion should be reachable from mission select.");
            Assert.AreEqual("Tick Invasion", _game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator TickInvasion_StartState_BothDogsCleanNoWetNoSuperTick()
        {
            yield return LoadArena();

            var puzzle = _game.TickInvasionPuzzle;
            // A frame elapses between StartMission and this assertion (GameManager's own Update
            // already ticks the active controller), so ticks may have accumulated a hair above
            // zero via a real, tiny Time.deltaTime - assert "effectively zero", not bit-exact.
            Assert.AreEqual(0f, puzzle.CheddarTicks, 0.001f);
            Assert.AreEqual(0f, puzzle.CocoaTicks, 0.001f);
            Assert.IsFalse(puzzle.CheddarErratic);
            Assert.IsFalse(puzzle.CocoaErratic);
            Assert.IsFalse(puzzle.IsCheddarWet);
            Assert.IsFalse(puzzle.IsCocoaWet);
            Assert.IsNull(puzzle.SuperTickTarget);
            Assert.IsFalse(_controller.IsFailed);
            Assert.IsTrue(_controller.PoolObject.activeSelf, "The pool dive station should be active from mission start.");
            Assert.AreEqual(1, _game.RuntimeSnapshot.ObjectiveGoal);
        }

        [UnityTest]
        public IEnumerator TickInvasion_UsesAuthoredPoolAndInfestationArt()
        {
            yield return LoadArena();

            foreach (string path in FinalGameplayArt.TickInvasionArtPack)
                Assert.IsTrue(FinalGameplayArt.Has(path), $"Missing Tick Invasion production art: {path}");

            Assert.IsNotNull(_controller.PoolArt);
            Assert.IsTrue(_controller.PoolArt.HasRuntimeSprite);
            Assert.AreEqual("tick_invasion_pool", _controller.PoolArt.RuntimeSpriteName);
            Assert.IsTrue(_controller.CheddarArt.HasAuthoredArt);
            Assert.IsTrue(_controller.CocoaArt.HasAuthoredArt);
            Assert.IsFalse(_controller.CheddarArt.Visible,
                "A clean dog should not start visually covered in ticks.");

            _controller.Tick(10f, Time.time);
            Assert.IsTrue(_controller.CheddarArt.Visible);
            Assert.AreEqual("tick_invasion_swarm", _controller.CheddarArt.CurrentSpriteName);
            Assert.Greater(_controller.CheddarArt.Pressure, _controller.CocoaArt.Pressure,
                "Cheddar's faster accumulation should be visible as the stronger infestation read.");
        }

        [UnityTest]
        public IEnumerator TickInvasion_GroomingUsesDistinctAuthoredCharacterKeyframes()
        {
            yield return LoadArena();

            Assert.IsTrue(_controller.CheddarGroomAnimation.HasAuthoredFrames);
            Assert.IsTrue(_controller.CocoaGroomAnimation.HasAuthoredFrames);
            Assert.IsTrue(FinalGameplayArt.Has(CharacterMotionArt.ResourcePath(
                DogId.Cheddar, CharacterMotionArt.Clip.Groom, CharacterMotionArt.Facing8.E, 0)));
            Assert.IsTrue(FinalGameplayArt.Has(CharacterMotionArt.ResourcePath(
                DogId.Cheddar, CharacterMotionArt.Clip.Groom, CharacterMotionArt.Facing8.E, 1)));
            Assert.IsTrue(FinalGameplayArt.Has(CharacterMotionArt.ResourcePath(
                DogId.Cocoa, CharacterMotionArt.Clip.Groom, CharacterMotionArt.Facing8.E, 0)));
            Assert.IsTrue(FinalGameplayArt.Has(CharacterMotionArt.ResourcePath(
                DogId.Cocoa, CharacterMotionArt.Clip.Groom, CharacterMotionArt.Facing8.E, 1)));

            _controller.CheddarGroomAnimation.ShowFrameForTests(0);
            string cheddarFirst = _controller.CheddarGroomAnimation.CurrentSpriteName;
            _controller.CheddarGroomAnimation.ShowFrameForTests(1);
            Assert.AreNotEqual(cheddarFirst, _controller.CheddarGroomAnimation.CurrentSpriteName,
                "Cheddar's grooming beat needs two distinct authored silhouettes.");
            _controller.CheddarGroomAnimation.Hide();

            _controller.CocoaGroomAnimation.ShowFrameForTests(0);
            string cocoaFirst = _controller.CocoaGroomAnimation.CurrentSpriteName;
            _controller.CocoaGroomAnimation.ShowFrameForTests(1);
            Assert.AreNotEqual(cocoaFirst, _controller.CocoaGroomAnimation.CurrentSpriteName,
                "Cocoa's grooming beat needs two distinct authored silhouettes.");
            _controller.CocoaGroomAnimation.Hide();
        }

        [UnityTest]
        public IEnumerator TickInvasion_SuccessfulGroomTemporarilyOverridesTheGroomersPose()
        {
            yield return LoadArena();

            Vector3 center = _game.ArenaBounds.center;
            PlaceDog(_cheddar, center - Vector3.right * 0.5f);
            PlaceDog(_cocoa, center + Vector3.right * 0.5f);
            _controller.Tick(4f, Time.time);

            Assert.IsTrue(_controller.HandleInteract(_cheddarIndex));
            Assert.IsTrue(_controller.CheddarGroomAnimation.IsPlaying);
            Assert.IsFalse(_controller.CocoaGroomAnimation.IsPlaying);
            Assert.AreEqual("cheddar_groom_e_00", _controller.CheddarGroomAnimation.CurrentSpriteName);
            Assert.IsTrue(_cheddar.GetComponent<DogReadabilityFeedback>().HasMissionArtOverride);

            _controller.Cleanup();
            Assert.IsFalse(_controller.CheddarGroomAnimation.IsPlaying);
            Assert.IsFalse(_cheddar.GetComponent<DogReadabilityFeedback>().HasMissionArtOverride,
                "Mission cleanup must restore the normal shared dog pose renderer.");
        }

        [UnityTest]
        public IEnumerator TickInvasion_SuperTickAndActionsUseDistinctAuthoredVisuals()
        {
            yield return LoadArena();

            _controller.Puzzle.ForceSuperTick(DogId.Cheddar);
            _controller.Tick(0.01f, Time.time);
            Assert.AreEqual("tick_invasion_super_tick", _controller.CheddarArt.CurrentSpriteName);

            PlaceDog(_cheddar, _controller.PoolObject.transform.position);
            Assert.IsTrue(_controller.HandleInteract(_cheddarIndex));
            Assert.IsNotNull(GameObject.Find("ArtVfx_tick_invasion_rinse_burst"),
                "Pool success should produce a nonverbal rinse burst, not only HUD text.");

            PlaceDog(_cheddar, _game.ArenaBounds.center);
            PlaceDog(_cocoa, _game.ArenaBounds.center);
            _controller.Tick(4f, Time.time);
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.IsNotNull(GameObject.Find("ArtVfx_tick_invasion_groom_burst"),
                "A successful groom should produce its authored brush-and-paws burst.");
        }

        [UnityTest]
        public IEnumerator TickInvasion_TickAccumulation_IsReadableThroughThePressureHud()
        {
            yield return LoadArena();

            IMissionPressureHud hud = _controller;
            Assert.IsTrue(hud.PressureVisible);
            Assert.That(hud.PressureLabel, Does.Contain("TICKS 0%"));

            _controller.Tick(25f, Time.time); // Cheddar's faster rate should make him the "worse" read
            Assert.That(hud.PressureLabel, Does.Contain("Cheddar"));
            Assert.Greater(hud.PressureNormalized, 0f);
        }

        [UnityTest]
        public IEnumerator TickInvasion_Groom_ReducesPartnerTicksAndIncreasesGroomerSlightly()
        {
            yield return LoadArena();

            Vector3 center = _game.ArenaBounds.center;
            PlaceDog(_cheddar, center);
            PlaceDog(_cocoa, center);
            _controller.Tick(10f, Time.time);
            var puzzle = _controller.Puzzle;
            float cheddarBefore = puzzle.CheddarTicks;
            float cocoaBefore = puzzle.CocoaTicks;
            Assert.Greater(cheddarBefore, 0f);

            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex), "Cocoa grooms Cheddar while standing close.");
            Assert.Less(puzzle.CheddarTicks, cheddarBefore, "Grooming should reduce the target's ticks.");
            Assert.Greater(puzzle.CocoaTicks, cocoaBefore, "The groomer should pick up a small transfer.");
        }

        [UnityTest]
        public IEnumerator TickInvasion_Groom_FailsWhenTooFarApart()
        {
            yield return LoadArena();

            Vector3 center = _game.ArenaBounds.center;
            PlaceDog(_cheddar, center);
            PlaceDog(_cocoa, center + new Vector3(20f, 0f, 0f));
            _controller.Tick(10f, Time.time);
            float cheddarBefore = _controller.Puzzle.CheddarTicks;

            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.AreEqual(cheddarBefore, _controller.Puzzle.CheddarTicks, "Too far apart - no groom should land.");
            Assert.That(_game.LastCue, Does.Contain("closer"));
        }

        [UnityTest]
        public IEnumerator TickInvasion_Erratic_BlocksGroomUntilBarkedCalm()
        {
            yield return LoadArena();

            Vector3 center = _game.ArenaBounds.center;
            PlaceDog(_cheddar, center);
            PlaceDog(_cocoa, center);
            _controller.Tick(25f, Time.time); // pushes Cheddar (0.026/s) past the 0.62 erratic threshold
            Assert.IsTrue(_controller.Puzzle.CheddarErratic, "Cheddar should be erratic by now.");

            float before = _controller.Puzzle.CheddarTicks;
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.AreEqual(before, _controller.Puzzle.CheddarTicks, "An erratic partner resists a bare groom.");
            Assert.That(_game.LastCue, Does.Contain("erratic"));

            Assert.IsTrue(_controller.HandleBark(_cocoaIndex), "Cocoa barks Cheddar calm.");
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex), "With the hold active, the groom should now land.");
            Assert.Less(_controller.Puzzle.CheddarTicks, before, "The held groom should have reduced Cheddar's ticks.");
        }

        [UnityTest]
        public IEnumerator TickInvasion_PoolDive_InstantResetAndWetState()
        {
            yield return LoadArena();

            Vector3 poolPos = _controller.PoolObject.transform.position;
            PlaceDog(_cheddar, poolPos);
            _controller.Tick(10f, Time.time);
            Assert.Greater(_controller.Puzzle.CheddarTicks, 0f);

            Assert.IsTrue(_controller.HandleInteract(_cheddarIndex));
            Assert.AreEqual(0f, _controller.Puzzle.CheddarTicks, "Pool dive should instantly reset ticks.");
            Assert.IsTrue(_controller.Puzzle.IsCheddarWet, "Cheddar should be wet right after diving.");
            Assert.That(_game.LastCue, Does.Contain("wet"));
        }

        [UnityTest]
        public IEnumerator TickInvasion_PoolDive_CocoaRecoversFasterThanCheddar()
        {
            yield return LoadArena();

            Vector3 poolPos = _controller.PoolObject.transform.position;
            PlaceDog(_cheddar, poolPos);
            PlaceDog(_cocoa, poolPos);
            Assert.IsTrue(_controller.HandleInteract(_cheddarIndex));
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex));
            Assert.IsTrue(_controller.Puzzle.IsCheddarWet);
            Assert.IsTrue(_controller.Puzzle.IsCocoaWet);

            _controller.Tick(_controller.Puzzle.CocoaWetSeconds + 0.5f, Time.time);
            Assert.IsFalse(_controller.Puzzle.IsCocoaWet, "Cocoa's shorter recovery window should have cleared.");
            Assert.IsTrue(_controller.Puzzle.IsCheddarWet, "Cheddar's longer recovery window should still be active.");
        }

        [UnityTest]
        public IEnumerator TickInvasion_SuperTick_ResistsNormalGroomAndOnlyPoolDiveClearsIt()
        {
            yield return LoadArena();

            Vector3 center = _game.ArenaBounds.center;
            PlaceDog(_cheddar, center);
            PlaceDog(_cocoa, center);
            _controller.Puzzle.ForceSuperTick(DogId.Cheddar);
            Assert.AreEqual(DogId.Cheddar, _controller.Puzzle.SuperTickTarget);

            float cheddarBefore = _controller.Puzzle.CheddarTicks;
            Assert.IsTrue(_controller.HandleInteract(_cocoaIndex), "Cocoa's groom attempt is handled but should not land.");
            Assert.AreEqual(cheddarBefore, _controller.Puzzle.CheddarTicks, "Super Tick cannot be groomed off.");
            Assert.That(_game.LastCue, Does.Contain("Super Tick"));

            PlaceDog(_cheddar, _controller.PoolObject.transform.position);
            Assert.IsTrue(_controller.HandleInteract(_cheddarIndex));
            Assert.IsNull(_controller.Puzzle.SuperTickTarget, "Only a pool dive should clear the Super Tick.");
            Assert.AreEqual(0f, _controller.Puzzle.CheddarTicks);
        }

        [UnityTest]
        public IEnumerator TickInvasion_SuperTick_PartnerHasNoGroomReliefWhileActive()
        {
            yield return LoadArena();

            Vector3 center = _game.ArenaBounds.center;
            PlaceDog(_cheddar, center);
            PlaceDog(_cocoa, center);
            _controller.Puzzle.ForceSuperTick(DogId.Cheddar);

            float cocoaBefore = _controller.Puzzle.CocoaTicks;
            Assert.IsTrue(_controller.HandleInteract(_cheddarIndex), "Cheddar's groom attempt on Cocoa is handled but should not land either.");
            Assert.AreEqual(cocoaBefore, _controller.Puzzle.CocoaTicks,
                "Cheddar is too overwhelmed by his own Super Tick to groom Cocoa - she has no relief available.");
        }

        [UnityTest]
        public IEnumerator TickInvasion_FailPath_EitherDogMaxingOutTicksFailsWithDistinctReason()
        {
            yield return LoadArena();

            _controller.Tick(45f, Time.time); // Cheddar's 0.026/s rate maxes him out well before Cocoa
            Assert.IsTrue(_controller.IsFailed);
            Assert.AreEqual(DogId.Cheddar, _controller.Puzzle.FailedDog);
            Assert.That(_controller.FailReason, Does.Contain("Cheddar"));

            yield return null; // let GameManager's own CheckClear observe the controller-owned fail
            Assert.AreEqual(GameManager.MissionOutcome.Failed, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator TickInvasion_FailPath_CocoaMaxingOutGetsHerOwnDignifiedReason()
        {
            yield return LoadArena();

            // Keep Cheddar clean by repeatedly grooming him while Cocoa's ticks alone climb to max
            // via repeated forced Super Ticks resolved only by Cheddar's own pool dives is overkill -
            // simplest deterministic path: drive Cocoa's ticks directly through repeated Groom-then-
            // Advance cycles is also indirect. Use the puzzle's own configured Cocoa rate directly:
            // a single long Advance still tips Cheddar first (he's faster), so instead groom Cheddar
            // clean every step while ticking forward until Cocoa alone crosses the max.
            Vector3 center = _game.ArenaBounds.center;
            PlaceDog(_cheddar, center);
            PlaceDog(_cocoa, center);
            int guard = 0;
            while (!_controller.IsFailed && guard++ < 400)
            {
                _controller.Tick(1f, Time.time);
                if (_controller.Puzzle.CheddarTicks > 0.05f) _controller.HandleInteract(_cocoaIndex); // Cocoa grooms Cheddar clean
                if (_controller.IsFailed) break;
            }

            Assert.IsTrue(_controller.IsFailed, "Cocoa should eventually max out while Cheddar is kept groomed clean.");
            Assert.AreEqual(DogId.Cocoa, _controller.Puzzle.FailedDog);
            Assert.That(_controller.FailReason, Does.Contain("Cocoa"));
        }

        [UnityTest]
        public IEnumerator TickInvasion_ClearPath_SurvivingTheFullRoundWithMutualGroomingClears()
        {
            yield return LoadArena();

            Vector3 center = _game.ArenaBounds.center;
            PlaceDog(_cheddar, center);
            PlaceDog(_cocoa, center);

            int guard = 0;
            while (!_controller.Puzzle.Cleared && !_controller.IsFailed && guard++ < 60)
            {
                _controller.Tick(2f, Time.time);
                _controller.HandleInteract(_cheddarIndex); // Cheddar grooms Cocoa
                _controller.HandleInteract(_cocoaIndex);   // Cocoa grooms Cheddar
            }

            Assert.IsFalse(_controller.IsFailed, "Steady mutual grooming should keep both dogs under the max.");
            Assert.IsTrue(_controller.Puzzle.Cleared, "Surviving the full round without maxing out should clear the mission.");
            Assert.IsTrue(_controller.IsPresentingSuccessfulOutcome);

            _controller.ForceFinishSuccessPresentation();
            _controller.Tick(0.01f, Time.time);
            Assert.IsTrue(_controller.IsComplete);
        }

        [UnityTest]
        public IEnumerator TickInvasion_Snapshot_MatchesInterfaceContract()
        {
            yield return LoadArena();

            var snapshot = _game.RuntimeSnapshot;
            Assert.AreEqual("tick_invasion", snapshot.MissionId);
            Assert.AreEqual(0, snapshot.ObjectiveProgress);
            Assert.AreEqual(1, snapshot.ObjectiveGoal);
            Assert.AreEqual(0, snapshot.Mistakes);
            Assert.IsFalse(snapshot.IsClear);
            Assert.IsFalse(snapshot.IsFailed);
        }

        [UnityTest]
        public IEnumerator TickInvasion_Replay_ResetsTicksWetnessAndSuperTickState()
        {
            yield return LoadArena();

            Vector3 center = _game.ArenaBounds.center;
            PlaceDog(_cheddar, center);
            _controller.Tick(10f, Time.time);
            _controller.Puzzle.ForceSuperTick(DogId.Cocoa);
            Assert.Greater(_controller.Puzzle.CheddarTicks, 0f);
            Assert.IsNotNull(_controller.Puzzle.SuperTickTarget);

            _game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.TickInvasion, _game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0, _game.Score);
            var puzzle = _game.TickInvasionPuzzle;
            // See the start-state test above for why this uses a tolerance rather than bit-exact 0.
            Assert.AreEqual(0f, puzzle.CheddarTicks, 0.001f, "Replay must clear ticks carried from the previous attempt.");
            Assert.AreEqual(0f, puzzle.CocoaTicks, 0.001f);
            Assert.IsFalse(puzzle.IsCheddarWet, "Replay must clear wet state.");
            Assert.IsFalse(puzzle.IsCocoaWet);
            Assert.IsNull(puzzle.SuperTickTarget, "Replay must clear the Super Tick.");
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

            _game.StartMission(GameManager.MissionVariant.TickInvasion);
            yield return null;
            _controller = _game.TickInvasionController;
            Assert.IsNotNull(_controller);
            _cheddarIndex = _controller.DogIndexOf(DogId.Cheddar);
            _cocoaIndex = _controller.DogIndexOf(DogId.Cocoa);
        }
    }
}
