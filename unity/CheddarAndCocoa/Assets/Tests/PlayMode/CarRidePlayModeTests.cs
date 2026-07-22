using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    public sealed class CarRidePlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator CarRide_AppearsInMissionSelectRotation()
        {
            yield return LoadArena();
            var game = _game;

            Assert.AreEqual(24, game.MissionSelectOptionCount);

            bool found = false;
            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                if (game.SelectedMissionVariant == GameManager.MissionVariant.CarRide) { found = true; break; }
                game.SelectNextMission();
                yield return null;
            }

            Assert.IsTrue(found, "Car Ride Chaos should be reachable from mission select.");
            Assert.AreEqual("Car Ride Chaos", game.SelectedMissionName);
        }

        [UnityTest]
        public IEnumerator CarRide_ClearPath_RidesOutEveryRoadEvent()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;

            Assert.IsInstanceOf<CarRideMissionController>(game.ActiveMissionController,
                "Car Ride Chaos must run entirely through its own IMissionController.");
            Assert.AreEqual("car_ride", game.RuntimeSnapshot.MissionId);
            Assert.That(game.ObjectiveLabel, Does.Contain("Ride home"));
            int required = game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(required, 0);

            int guard = 0;
            while (game.CarRideState.EventsResolved < required && guard++ < 30)
            {
                game.ForceCarEventSurvived();
                yield return null;
            }

            Assert.AreEqual(required, game.CarRideState.EventsResolved);
            Assert.AreEqual(0, game.CarRideState.Tumbles);
            var controller = (CarRideMissionController)game.ActiveMissionController;
            Assert.IsInstanceOf<IMissionSuccessPresentationController>(controller);
            Assert.IsTrue(controller.IsPresentingSuccessfulOutcome,
                "The home arrival should remain visible before the result card replaces the backseat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.That(game.ObjectiveLabel, Does.Contain("We're home"));
            Assert.IsTrue(HasWorldPop("WE'RE HOME"));

            controller.ForceFinishSuccessPresentation();
            yield return null;

            Assert.AreEqual(GameManager.MissionOutcome.Clear, game.Outcome);
            Assert.IsTrue(game.RuntimeSnapshot.IsClear);
            Assert.That(game.EndSummaryLabel, Does.Contain("Smooth Riders"));
            Assert.AreNotEqual("MVP: awaiting dog heroics", game.MvpLabel,
                "Riding out road events together should credit both dogs toward the MVP stat.");
        }

        [UnityTest]
        public IEnumerator CarRide_Driver_CarriesPersistentRideProgressBaselineBetweenEvents()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            var controller = (CarRideMissionController)game.ActiveMissionController;
            int required = game.RuntimeSnapshot.ObjectiveGoal;
            Assert.Greater(required, 2, "This test needs at least one event resolved and one remaining.");

            var dashboard = GameObject.Find("Car Ride Driver");
            Assert.IsNotNull(dashboard);
            var feedback = dashboard.GetComponent<MissionActorFeedback>();
            Assert.IsNotNull(feedback, "The driver needs a MissionActorFeedback to carry its calm-phase label.");

            Assert.AreEqual(0f, controller.RideProgress, "Ride progress should read 0 before any event resolves.");
            string beforeLabel = feedback.Label;
            Assert.That(beforeLabel, Does.Contain("stops from home"),
                "The calm-phase dashboard baseline should show the world how far the ride has left.");

            game.ForceCarEventSurvived();
            yield return null;

            Assert.Greater(controller.RideProgress, 0f,
                "Resolving a road event should move the persistent ride-progress baseline.");
            string afterLabel = feedback.Label;
            Assert.AreNotEqual(beforeLabel, afterLabel,
                "The dashboard's calm-phase copy must change once the world reflects more ride progress, " +
                "not just the HUD's EventsResolved/RequiredEvents count.");
        }

        [UnityTest]
        public IEnumerator CarRide_FailPath_TooManyTumbles()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;

            for (int i = 0; i < 5; i++)
            {
                game.ForceCarTumble(i % 2);
                yield return null;
            }

            Assert.AreEqual(5, game.CarRideState.Tumbles);
            Assert.AreEqual(GameManager.MissionOutcome.Failed, game.Outcome);
            Assert.AreEqual(GameManager.State.GameOver, game.Phase);
            Assert.IsTrue(game.RuntimeSnapshot.IsFailed);
            Assert.That(game.EndSummaryLabel, Does.Contain("Car Sick"));
        }

        [UnityTest]
        public IEnumerator CarRide_BrakeEvent_BracedDogsRideItOut()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            var controller = (CarRideMissionController)_game.ActiveMissionController;

            controller.ForceBeginRoadEvent(CarRideMissionController.RoadEventKind.Brake);
            yield return null;
            Assert.IsTrue(controller.IsTelegraphing, "A brake should telegraph before it fires.");

            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            cocoa.transform.position = Vector3.zero;
            cheddar.transform.position = Vector3.right;
            int cocoaIndex = controller.DogIndexOf(DogId.Cocoa);
            int cheddarIndex = controller.DogIndexOf(DogId.Cheddar);
            controller.ForceBrace(cocoaIndex);
            controller.ForceBrace(cheddarIndex);
            Assert.IsTrue(controller.IsDogBraced(cocoaIndex));
            Assert.IsTrue(controller.IsDogBraced(cheddarIndex));
            Assert.IsTrue(controller.CheddarTuckedForBrake);

            controller.ForceResolveRoadEvent();
            yield return null;

            Assert.AreEqual(0, controller.State.Tumbles, "Braced dogs must survive the brake slam.");
            Assert.AreEqual(1, controller.State.EventsResolved);
            Assert.IsTrue(HasWorldPop("ANCHORED"), "Cocoa's held anchor should celebrate visibly.");
            Assert.IsTrue(HasWorldPop("TUCKED SAFE"), "Cheddar's successful tuck should celebrate visibly.");
            Assert.AreEqual(ArenaFeedbackCatalog.TugRescueSuccess, _game.LastAudioCueRequested,
                "S5.2: the brace-team-ready moment was a silent success beat (rumble but no audio) - must fire a cue now.");
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose);
        }

        [UnityTest]
        public IEnumerator CarRide_BrakeRequiresCocoaAnchorThenNearbyCheddarTuck_AndRecoversNextBrake()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            var controller = (CarRideMissionController)_game.ActiveMissionController;
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            int cheddarIndex = controller.DogIndexOf(DogId.Cheddar);
            int cocoaIndex = controller.DogIndexOf(DogId.Cocoa);

            controller.ForceBeginRoadEvent(CarRideMissionController.RoadEventKind.Brake);
            cheddar.transform.position = Vector3.zero;
            cocoa.transform.position = Vector3.right * 8f;
            controller.ForceBrace(cheddarIndex);
            Assert.IsFalse(controller.CheddarTuckedForBrake,
                "Cheddar cannot create his own brake solution before Cocoa plants.");
            Assert.That(_game.LastCue, Does.Contain("Cocoa"));
            Assert.That(_game.LastJuiceLabel, Does.Contain("PLANTS"), "The too-early tuck must produce a visible coach beat.");
            Assert.IsTrue(HasWorldPop("TOO SOON"), "The too-early tuck must produce a visible world pop.");
            Assert.AreEqual(ArenaFeedbackCatalog.UiButtonDisabled, _game.LastAudioCueRequested,
                "The too-early tuck must produce an audible coach beat.");
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);

            controller.ForceBrace(cocoaIndex);
            controller.ForceBrace(cheddarIndex);
            Assert.IsFalse(controller.CheddarTuckedForBrake,
                "Cheddar must physically reach Cocoa's anchor, not answer from across the bench.");

            cheddar.transform.position = cocoa.transform.position + Vector3.right;
            controller.ForceBrace(cheddarIndex);
            Assert.IsTrue(controller.CheddarTuckedForBrake);
            cheddar.transform.position = Vector3.left * 8f;
            controller.ForceResolveRoadEvent();
            yield return null;

            Assert.AreEqual(1, controller.State.Tumbles,
                "Cocoa should ride out her anchor while Cheddar gets flung after breaking the tuck hold.");
            Assert.AreEqual(1, controller.State.EventsResolved);

            controller.ForceBeginRoadEvent(CarRideMissionController.RoadEventKind.Brake);
            cocoa.transform.position = Vector3.zero;
            cheddar.transform.position = Vector3.right;
            controller.ForceBrace(cocoaIndex);
            controller.ForceBrace(cheddarIndex);
            Assert.IsTrue(controller.CheddarTuckedForBrake);
            controller.ForceResolveRoadEvent();
            yield return null;

            Assert.AreEqual(1, controller.State.Tumbles,
                "A correctly coordinated retry should add no new tumble.");
            Assert.AreEqual(2, controller.State.EventsResolved);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator CarRide_BrakeEvent_UnbracedDogsAreFlungForward()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            var controller = (CarRideMissionController)_game.ActiveMissionController;

            controller.ForceBeginRoadEvent(CarRideMissionController.RoadEventKind.Brake);
            controller.ForceResolveRoadEvent();
            yield return null;

            Assert.AreEqual(2, controller.State.Tumbles, "Both unbraced dogs should tumble on the brake slam.");
            Assert.IsTrue(HasWorldPop("FLUNG FORWARD"));
            Assert.IsTrue(FindDog(DogId.Cheddar).IsWrestleStunned, "Cheddar should be knocked into a stun by the stop.");
            Assert.IsTrue(FindDog(DogId.Cocoa).IsWrestleStunned, "Cocoa should be knocked into a stun by the stop.");
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, feedback.CurrentPose);
        }

        [UnityTest]
        public IEnumerator CarRide_TurnSlide_SlidesEverything_CheddarSlidesHardest()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            var controller = (CarRideMissionController)_game.ActiveMissionController;

            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            // Keep both away from the sliding junk so this test isolates the slide itself.
            Vector2 center = _game.ArenaBounds.center;
            cheddar.transform.position = center + new Vector2(-2f, 2f);
            cocoa.transform.position = center + new Vector2(2f, 2f);
            var cooler = GameObject.Find("SeatObstacle_Cooler");
            float cheddarX = cheddar.transform.position.x;
            float cocoaX = cocoa.transform.position.x;
            float coolerX = cooler.transform.position.x;

            for (int i = 0; i < 4; i++)
                controller.ForceTurnSlide(0.1f, CarRideMissionController.RoadEventKind.TurnRight);
            yield return null;

            float cheddarSlide = cheddarX - cheddar.transform.position.x;
            float cocoaSlide = cocoaX - cocoa.transform.position.x;
            float coolerSlide = coolerX - cooler.transform.position.x;
            Assert.Greater(cheddarSlide, 0f, "A right turn should slide Cheddar toward the left door.");
            Assert.Greater(cocoaSlide, 0f, "A right turn should slide Cocoa toward the left door.");
            Assert.Greater(cheddarSlide, cocoaSlide,
                "Chaos-puppy Cheddar slides harder than planted veteran Cocoa.");
            Assert.Greater(coolerSlide, cheddarSlide, "Loose seat junk slides faster than any dog.");

            var cabin = GameObject.Find(MissionLevelAreaArt.CarRideRootName);
            Assert.IsNotNull(cabin);
            float tilt = Mathf.DeltaAngle(0f, cabin.transform.rotation.eulerAngles.z);
            Assert.Greater(Mathf.Abs(tilt), 0.5f, "A turn should visibly tilt the whole cabin set.");
        }

        [UnityTest]
        public IEnumerator CarRide_SlidingJunk_BonksGroundedDogs_JumpClearsIt()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            var controller = (CarRideMissionController)_game.ActiveMissionController;

            var cheddar = FindDog(DogId.Cheddar);
            var cooler = GameObject.Find("SeatObstacle_Cooler");
            cheddar.transform.position = cooler.transform.position;
            controller.ForceTurnSlide(0.05f, CarRideMissionController.RoadEventKind.TurnRight);
            yield return null;

            Assert.AreEqual(1, controller.State.Tumbles, "A grounded dog in the junk's path gets bonked.");
            Assert.IsTrue(HasWorldPop("BONK"));
            Assert.IsTrue(cheddar.IsWrestleStunned, "A bonk knocks the dog into a brief stun.");

            _game.Restart();
            yield return null;
            controller = (CarRideMissionController)_game.ActiveMissionController;
            cheddar = FindDog(DogId.Cheddar);
            cooler = GameObject.Find("SeatObstacle_Cooler");

            cheddar.Jump();
            cheddar.transform.position = cooler.transform.position;
            controller.ForceTurnSlide(0.05f, CarRideMissionController.RoadEventKind.TurnRight);
            yield return null;

            Assert.AreEqual(0, controller.State.Tumbles, "A jumping dog clears the sliding junk.");
            Assert.IsTrue(HasWorldPop("CLEAN HOP"));
        }

        [UnityTest]
        public IEnumerator CarRide_Brace_PlantsAgainstTheSlide_AndDoorSquishHurts()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            var controller = (CarRideMissionController)_game.ActiveMissionController;

            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            Vector2 center = _game.ArenaBounds.center;
            cheddar.transform.position = center + new Vector2(-2f, 2f);
            cocoa.transform.position = center + new Vector2(2f, 2f);

            controller.ForceBrace(controller.DogIndexOf(DogId.Cheddar));
            float cheddarX = cheddar.transform.position.x;
            float cocoaX = cocoa.transform.position.x;
            controller.ForceTurnSlide(0.1f, CarRideMissionController.RoadEventKind.TurnRight);
            yield return null;

            Assert.AreEqual(cheddarX, cheddar.transform.position.x, 0.001f,
                "A braced dog plants its claws and does not slide.");
            Assert.AreNotEqual(cocoaX, cocoa.transform.position.x,
                "The unbraced dog still slides.");

            // Door squish: a right turn pins an unbraced dog against the left edge.
            cocoa.transform.position = new Vector3(center.x - 15.8f, center.y + 2f, 0f);
            controller.ForceTurnSlide(0.05f, CarRideMissionController.RoadEventKind.TurnRight);
            yield return null;

            Assert.AreEqual(1, controller.State.Tumbles, "Getting pinned against the door is a tumble.");
            Assert.IsTrue(HasWorldPop("DOOR SQUISH"));
        }

        [UnityTest]
        public IEnumerator CarRide_UnitedBark_MakesTheDriverEaseUp()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            var controller = (CarRideMissionController)_game.ActiveMissionController;

            Assert.IsFalse(controller.DriverEased);
            controller.OnUnitedBark();
            Assert.IsTrue(controller.DriverEased, "A united bark during cruise should ease the driver.");

            controller.ForceEventSurvived();
            yield return null;
            Assert.IsFalse(controller.DriverEased, "The ease is spent once the road event resolves.");
        }

        [UnityTest]
        public IEnumerator CarRide_Replay_ResetsRideState()
        {
            yield return LoadArena();
            var game = _game;

            game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;
            game.ForceCarEventSurvived();
            game.ForceCarTumble();
            yield return null;
            Assert.Greater(game.CarRideState.EventsResolved + game.CarRideState.Tumbles, 0);

            game.Restart();
            yield return null;

            Assert.AreEqual(GameManager.MissionVariant.CarRide, game.ActiveMissionVariant);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, game.Outcome);
            Assert.AreEqual(0, game.Score);
            Assert.AreEqual(0, game.CarRideState.EventsResolved);
            Assert.AreEqual(0, game.CarRideState.Tumbles);
            Assert.AreEqual(1, game.MissionReplayCount);
        }

        [UnityTest]
        public IEnumerator CarRide_SmoothAndTumble_HaveReadablePackReactions()
        {
            yield return LoadArena();
            _game.StartMission(GameManager.MissionVariant.CarRide);
            yield return null;

            _game.ForceCarEventSurvived();
            yield return null;
            Assert.IsTrue(HasWorldPop("SMOOTH"));
            foreach (var feedback in _game.DogFeedback)
                Assert.AreEqual(DogReadabilityFeedback.Pose.Proud, feedback.CurrentPose);
            Assert.AreEqual(ArenaFeedbackCatalog.TugRescueSuccess, _game.LastAudioCueRequested);

            _game.ForceCarTumble();
            yield return null;
            Assert.IsTrue(HasWorldPop("TUMBLE"));
            Assert.AreEqual(DogReadabilityFeedback.Pose.Sad, _game.DogFeedback[0].CurrentPose,
                "The tumbling dog should visibly panic.");
            Assert.AreEqual(ArenaFeedbackCatalog.ThreatWarning, _game.LastAudioCueRequested);
        }

        private static DogController FindDog(DogId id)
        {
            foreach (var identity in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
                if (identity.Id == id && identity.TryGetComponent<DogController>(out var dog))
                    return dog;
            Assert.Fail($"Missing dog {id}");
            return null;
        }

        private static bool HasWorldPop(string text)
        {
            foreach (var pop in Object.FindObjectsByType<MissionWorldPop>(FindObjectsSortMode.None))
                if (pop.Label.Contains(text)) return true;
            return false;
        }

        private IEnumerator LoadArena()
        {
            _game = null;
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
        }
    }
}
