using System.Collections;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// The struggle card must only replace its movement diagram with a face button when that
    /// button is genuinely actionable in the current state. These cover the four showcase
    /// missions added after Kitchen's original IMissionCoachHint proof.
    /// </summary>
    public sealed class ShowcaseCoachHintPlayModeTests
    {
        private GameManager _game;

        [UnityTest]
        public IEnumerator PeeBreak_RevealsBarkOnlyAfterTheClimaxSetupIsHeld()
        {
            yield return LoadMission(GameManager.MissionVariant.OperationPeeBreak);
            var controller = _game.PeeBreakController;

            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, 2.1f);
            _game.ForcePeeBreakAdvance(controller.Required, 2.6f);
            Assert.AreEqual(PeeBreakMissionController.Beat.UnitedBark, controller.CurrentBeat);

            Assert.IsNull(_game.CoachActionFor(DogId.Cheddar));
            Assert.IsNull(_game.CoachActionFor(DogId.Cocoa),
                "The card must lead with the two positional setup jobs, not spoil the bark payoff.");

            FindDog(DogId.Cheddar).transform.position = controller.LeashPosition;
            FindDog(DogId.Cocoa).transform.position = controller.DoorStareAnchor;

            Assert.AreEqual(GameManager.TutorialActionStep.Bark, _game.CoachActionFor(DogId.Cheddar));
            Assert.AreEqual(GameManager.TutorialActionStep.Bark, _game.CoachActionFor(DogId.Cocoa));
        }

        [UnityTest]
        public IEnumerator CarRide_CoachesTheBrakeHandoffInTruthfulOrder()
        {
            yield return LoadMission(GameManager.MissionVariant.CarRide);
            var controller = _game.CarRideController;
            int cocoa = controller.DogIndexOf(DogId.Cocoa);
            int cheddar = controller.DogIndexOf(DogId.Cheddar);

            controller.ForceBeginRoadEvent(CarRideMissionController.RoadEventKind.Brake);
            Assert.AreEqual(GameManager.TutorialActionStep.Interact, _game.CoachActionFor(DogId.Cocoa));
            Assert.IsNull(_game.CoachActionFor(DogId.Cheddar), "Cheddar cannot tuck before Cocoa plants.");

            FindDog(DogId.Cheddar).transform.position = FindDog(DogId.Cocoa).transform.position + Vector3.right;
            controller.ForceBrace(cocoa);
            Assert.IsNull(_game.CoachActionFor(DogId.Cocoa), "Cocoa should hold after planting.");
            Assert.AreEqual(GameManager.TutorialActionStep.Interact, _game.CoachActionFor(DogId.Cheddar));

            controller.ForceBrace(cheddar);
            Assert.IsNull(_game.CoachActionFor(DogId.Cheddar), "The ready pair should hold, not keep pressing.");
        }

        [UnityTest]
        public IEnumerator BabyBird_CoachesGrabShakeAndInRangeGuardBark()
        {
            yield return LoadMission(GameManager.MissionVariant.BabyBirdBedlam);
            var controller = _game.BabyBirdBedlamController;
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);

            controller.ForceChickLand(0f);
            cheddar.transform.position = new Vector2(0f, controller.EntryTarget.y);
            Assert.AreEqual(GameManager.TutorialActionStep.Interact, _game.CoachActionFor(DogId.Cheddar));
            Assert.IsNull(_game.CoachActionFor(DogId.Cocoa));

            controller.ForceChickGrab();
            Assert.AreEqual(GameManager.TutorialActionStep.Interact, _game.CoachActionFor(DogId.Cheddar),
                "A held chick is shaken with the same Interact action.");

            controller.ForceParentDive();
            cocoa.transform.position = _game.PredatorObject.transform.position;
            Assert.AreEqual(GameManager.TutorialActionStep.Bark, _game.CoachActionFor(DogId.Cocoa));

            cocoa.transform.position += Vector3.right * 10f;
            Assert.IsNull(_game.CoachActionFor(DogId.Cocoa),
                "Out of repel range, movement is the honest instruction before Bark.");
        }

        [UnityTest]
        public IEnumerator GateCrash_CoachesInteractOnlyAtCocoasAnchor()
        {
            yield return LoadMission(GameManager.MissionVariant.GateCrash);
            var controller = _game.GateCrashController;
            var cocoa = FindDog(DogId.Cocoa);

            cocoa.transform.position = controller.GateFloorAnchor + Vector2.right * 8f;
            Assert.IsNull(_game.CoachActionFor(DogId.Cocoa));
            Assert.IsNull(_game.CoachActionFor(DogId.Cheddar));

            cocoa.transform.position = controller.GateFloorAnchor;
            Assert.AreEqual(GameManager.TutorialActionStep.Interact, _game.CoachActionFor(DogId.Cocoa));

            controller.ForceGateHold(true);
            Assert.IsNull(_game.CoachActionFor(DogId.Cocoa), "Once anchored, Cocoa's instruction is to hold.");
        }

        private IEnumerator LoadMission(GameManager.MissionVariant variant)
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
            _game.StartMission(variant);
            yield return null;
        }

        private static DogController FindDog(DogId dogId)
        {
            foreach (var identity in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
                if (identity.Id == dogId) return identity.GetComponent<DogController>();
            return null;
        }
    }
}
