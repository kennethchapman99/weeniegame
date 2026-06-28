using System.Collections;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CheddarAndCocoa.Tests
{
    public sealed class PeeBreakPlayModeTests
    {
        private GameManager _game;
        private PeeBreakMissionController Controller => _game.PeeBreakController;

        [UnityTest]
        public IEnumerator StartState_IsControllerOwnedReadableAndLowPressure()
        {
            yield return LoadMission();
            Assert.IsInstanceOf<PeeBreakMissionController>(_game.ActiveMissionController);
            Assert.AreEqual(PeeBreakMissionController.Beat.DoorStare, Controller.CurrentBeat);
            Assert.AreEqual(SocialStimulus.DoorStare, Controller.Required);
            Assert.Less(Controller.Bladder, 0.2f);
            Assert.AreEqual(1f, Controller.PhoneBattery, 0.001f);
            Assert.IsFalse(Controller.DoorOpen);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual("operation_pee_break", _game.RuntimeSnapshot.MissionId);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa"));
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cheddar"));
            Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("WATCH COCOA"));
            Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("NO BARK"));
            Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("HOLD DOOR STARE"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cheddar:"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("WATCH COCOA"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa:"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("HOLD DOOR STARE"));
            Assert.IsNotNull(GameObject.Find("PeeBreakTeenager"));
            Assert.IsNotNull(GameObject.Find("PeeBreakDoor"));
            AssertPeeBreakScenery("PeeBreakRoomFloor", "The active slice should read as an interior room before labels.");
            AssertPeeBreakScenery("PeeBreakCouchBack", "The Teenager situation should read as couch-bound.");
            AssertPeeBreakScenery("PeeBreakCouchSeat", "The Teenager situation should read as couch-bound.");
            AssertPeeBreakScenery("PeeBreakSideTable", "The phone should sit in a recognizable couch-side cluster.");
            AssertPeeBreakScenery("PeeBreakPhoneGlow", "The phone should visually glow as the boss object.");
            AssertPeeBreakScenery("PeeBreakDoorFrame", "The exit door should read before the station label is parsed.");
            AssertPeeBreakScenery("PeeBreakLeashHook", "The leash should read as hanging near a hook.");
            AssertPeeBreakScenery("PeeBreakHallwayRug", "The blocking lane should read as a hallway/rug zone.");
            AssertPeeBreakDetail("PeeBreakCouchLeftArm", "Couch should have arms, not just a rectangle.");
            AssertPeeBreakDetail("PeeBreakCouchRightArm", "Couch should have arms, not just a rectangle.");
            AssertPeeBreakDetail("PeeBreakCouchCushionLine", "Couch should show cushion structure before labels.");
            AssertPeeBreakDetail("PeeBreakCouchPillowA", "Couch should feel like a lived-in room, not a diagram block.");
            AssertPeeBreakDetail("PeeBreakCouchPillowB", "Couch should feel like a lived-in room, not a diagram block.");
            AssertPeeBreakDetail("PeeBreakTeenagerHead", "Teenager should read as a person on the couch.");
            AssertPeeBreakDetail("PeeBreakTeenagerHair", "Teenager should read as a person on the couch.");
            AssertPeeBreakDetail("PeeBreakTeenagerLegs", "Teenager should read as sitting on the couch.");
            AssertPeeBreakDetail("PeeBreakTeenagerThumbs", "Teenager should read as phone-absorbed.");
            AssertPeeBreakDetail("PeeBreakTeenagerPhoneAttentionBeam", "Beat 1 should visually show the Teenager's attention stuck on the phone.");
            AssertPeeBreakDetail("PeeBreakTeenagerQuestionBubble", "The Teenager should emote visually before labels do all the work.");
            AssertPeeBreakDetail("PeeBreakTeenagerComprehensionTrack", "Teenager understanding should have an in-world progress read.");
            AssertPeeBreakDetail("PeeBreakTeenagerComprehensionFill", "Correct dog jobs should visibly build comprehension.");
            AssertPeeBreakDetail("PeeBreakBeatPip1", "The four-beat mission shape should be visible in-world.");
            AssertPeeBreakDetail("PeeBreakBeatPip2", "The four-beat mission shape should be visible in-world.");
            AssertPeeBreakDetail("PeeBreakBeatPip3", "The four-beat mission shape should be visible in-world.");
            AssertPeeBreakDetail("PeeBreakBeatPip4", "The four-beat mission shape should be visible in-world.");
            Assert.IsFalse(FindLoadedObject("PeeBreakTeenagerConfusionFill").activeSelf,
                "The confusion read should stay quiet until players send an incomplete or wrong signal.");
            AssertPeeBreakDetail("PeeBreakPhoneScreen", "Phone should read as a screen, not only a cyan square.");
            AssertPeeBreakDetail("PeeBreakPhoneReflection", "Phone should carry a screen highlight.");
            AssertPeeBreakDetail("PeeBreakPhoneBatteryShell", "Phone battery should be visible in-world.");
            AssertPeeBreakDetail("PeeBreakPhoneBatteryFill", "Phone drain should have a visual state, not only text.");
            AssertPeeBreakDetail("PeeBreakPhoneChargeBolt", "The charged phone should visibly read as powered.");
            AssertPeeBreakDetail("PeeBreakDoorPanelTop", "Door should have panel structure.");
            AssertPeeBreakDetail("PeeBreakDoorPanelBottom", "Door should have panel structure.");
            AssertPeeBreakDetail("PeeBreakDoorKnob", "Door should have a knob before labels.");
            AssertPeeBreakDetail("PeeBreakHookPeg", "Leash area should have a visible hook.");
            AssertPeeBreakDetail("PeeBreakHangingLeashLoop", "Leash should be visible in the room before Beat 2 labels.");
            AssertPeeBreakDetail("PeeBreakHangingLeashTail", "Leash should hang from the hook before the gameplay station appears.");
            Assert.IsFalse(FindLoadedObject("PeeBreakOpenSunbeam").activeSelf,
                "The open-door sunbeam should wait for the catharsis beat.");
            Assert.IsFalse(FindLoadedObject("PeeBreakOutdoorGrassPatch").activeSelf,
                "The outdoor payoff should wait until the door actually opens.");
            Assert.IsFalse(FindLoadedObject("PeeBreakOutdoorFireHydrant").activeSelf,
                "The hydrant gag should be a climax reward, not start-state clutter.");
            Assert.IsFalse(FindLoadedObject("PeeBreakReliefSparkleA").activeSelf,
                "Relief sparkles should be saved for the catharsis beat.");
            var coach = FindLoadedObject("PeeBreakCheddarCoach");
            Assert.IsNotNull(coach);
            Assert.That(coach.GetComponentInChildren<TextMesh>().text, Does.Contain("NO BARK"));
            Assert.That(coach.GetComponentInChildren<TextMesh>().text, Does.Contain("CHEDDAR"));
            Assert.That(GameObject.Find("PeeBreakPhone").GetComponentInChildren<TextMesh>().text, Does.Contain("100%"));
            Assert.That(GameObject.Find("PeeBreakBladderMeter").GetComponentInChildren<TextMesh>().text, Does.Contain("12%"));
        }

        [UnityTest]
        public IEnumerator ExactCombosAdvanceAndChangeRoleLocks()
        {
            yield return LoadMission();
            var comprehensionFill = FindLoadedObject("PeeBreakTeenagerComprehensionFill");
            float emptyWidth = comprehensionFill.transform.localScale.x;

            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 0.3f);
            Assert.Greater(comprehensionFill.transform.localScale.x, emptyWidth,
                "Holding the exact dog job should visibly fill the Teenager comprehension meter.");
            _game.ForcePeeBreakAdvance(SocialStimulus.BarkRhythm, 0.1f);
            Assert.IsTrue(FindLoadedObject("PeeBreakTeenagerConfusionFill").activeSelf,
                "A wrong signal should visibly light the recoverable confusion read before labels carry it.");

            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            Assert.AreEqual(PeeBreakMissionController.Beat.LeashMessage, Controller.CurrentBeat);
            Assert.AreEqual(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, Controller.Required);
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            Assert.AreEqual(0f, Controller.Puzzle.Comprehension, 0.001f);
            Assert.Greater(Controller.Puzzle.Confusion, 0f);
            _game.ForcePeeBreakAdvance(Controller.Required, 2.1f);
            Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
            Assert.AreEqual(SocialStimulus.UnplugCharger | SocialStimulus.BlockHallway, Controller.Required);
        }

        [UnityTest]
        public IEnumerator BeatTwo_ColdReadSplitsCocoaDoorAndCheddarLeash()
        {
            yield return LoadMission();
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            yield return null;

            Assert.AreEqual(PeeBreakMissionController.Beat.LeashMessage, Controller.CurrentBeat);
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cocoa STARE"));
            Assert.That(_game.ObjectiveLabel, Does.Contain("Cheddar PRESENT LEASH"));
            Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("PRESENT LEASH"));
            Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("HOLD DOOR STARE"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cheddar:"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("PRESENT LEASH"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa:"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("HOLD DOOR STARE"));

            var coach = FindLoadedObject("PeeBreakCheddarCoach");
            var door = GameObject.Find("PeeBreakDoor");
            var leash = GameObject.Find("PeeBreakLeash");
            Assert.IsNotNull(coach);
            Assert.IsNotNull(door);
            Assert.IsNotNull(leash);
            Assert.IsFalse(coach.activeSelf, "Beat 2 should remove the Beat 1 watch pad so Cheddar has only the leash job.");
            Assert.IsTrue(leash.activeSelf, "Beat 2 must expose Cheddar's leash station.");
            Assert.That(door.GetComponentInChildren<TextMesh>().text, Does.Contain("COCOA"));
            Assert.That(door.GetComponentInChildren<TextMesh>().text, Does.Contain("DOOR STARE"));
            Assert.That(leash.GetComponentInChildren<TextMesh>().text, Does.Contain("CHEDDAR"));
            Assert.That(leash.GetComponentInChildren<TextMesh>().text, Does.Contain("PRESENT LEASH"));
            AssertPeeBreakDetail("PeeBreakLeashStrap", "The active leash station should read as a strap before labels.");
            AssertPeeBreakDetail("PeeBreakLeashClip", "The active leash station should show clip hardware.");
        }

        [UnityTest]
        public IEnumerator ObserverRehearsal_ColdPathSurfacesBeatOneBeatTwoAndFirstEarlyBarkMisread()
        {
            yield return LoadMission();
            _game.SetPlaytestOverlayVisible(true);
            yield return null;

            Assert.AreEqual(PeeBreakMissionController.Beat.DoorStare, Controller.CurrentBeat);
            Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("WATCH COCOA"));
            Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("NO BARK"));
            Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("HOLD DOOR STARE"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cheddar:"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("WATCH COCOA"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa:"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("HOLD DOOR STARE"));
            Assert.That(WorldLabel("PeeBreakCheddarCoach"), Does.Contain("NO BARK"));
            Assert.That(WorldLabel("PeeBreakDoor"), Does.Contain("COCOA"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("SCROLLING"));
            Assert.That(_game.PlaytestHotkeysLabel, Does.Contain("F4"));
            Assert.That(_game.PlaytestCountersLabel, Does.Contain("cold-read ? 0"));

            _game.RecordColdReadQuestion("observer rehearsal beat 1 job check");
            Assert.AreEqual(1, _game.ColdReadQuestionCount);
            Assert.That(_game.LastPlaytestEvent, Does.Contain("ColdReadQuestion"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("Cocoa holds DOOR STARE"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("WATCH COCOA"));

            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            yield return null;

            Assert.AreEqual(PeeBreakMissionController.Beat.LeashMessage, Controller.CurrentBeat);
            Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("PRESENT LEASH"));
            Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("HOLD DOOR STARE"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("PRESENT LEASH"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("HOLD DOOR STARE"));
            Assert.That(WorldLabel("PeeBreakLeash"), Does.Contain("PRESENT LEASH"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("LOOKING UP"));

            var cheddar = GameObject.Find("Cheddar");
            var cocoa = GameObject.Find("Cocoa");
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);
            cocoa.transform.position = Controller.DoorPosition;
            cheddar.transform.position = Controller.DoorPosition + Vector2.left * 6f;
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 0.1f);
            yield return null;

            Assert.AreEqual(PeeBreakMissionController.Beat.LeashMessage, Controller.CurrentBeat);
            Assert.That(WorldLabel("PeeBreakDoor"), Does.Contain("NEEDS CHEDDAR LEASH"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("NEEDS LEASH TOO"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("PRESENT LEASH"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("ON TARGET"));
            _game.RecordColdReadQuestion("observer rehearsal beat 2 missing partner check");
            Assert.AreEqual(2, _game.ColdReadQuestionCount);
            Assert.That(_game.LastPlaytestEvent, Does.Contain("Cocoa STARE + Cheddar PRESENT LEASH"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("PRESENT LEASH"));

            cheddar.transform.position = Controller.LeashPosition;
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, 0.1f);
            yield return null;

            Assert.That(WorldLabel("PeeBreakDoor"), Does.Contain("COCOA STARE LOCKED"));
            Assert.That(WorldLabel("PeeBreakLeash"), Does.Contain("CHEDDAR LEASH READY"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("GETTING IT"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("ON TARGET"));

            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, 2.1f);
            yield return null;
            yield return null;

            Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
            Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("BLOCK HALLWAY"));
            Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("UNPLUG CHARGER"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("BLOCK HALLWAY"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("UNPLUG CHARGER"));
            Assert.That(WorldLabel("PeeBreakHallwayBlock"), Does.Contain("BLOCK HALLWAY"));
            Assert.That(WorldLabel("PeeBreakCharger"), Does.Contain("UNPLUG CHARGER"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("PHONE FADING"));
            Assert.IsTrue(FindLoadedObject("PeeBreakChargerCord").activeSelf,
                "Beat 3 should visibly connect the phone to the charger target.");
            AssertPeeBreakDetail("PeeBreakOutletPlate", "Beat 3 charger target should read as a wall outlet.");
            AssertPeeBreakDetail("PeeBreakOutletSlots", "Beat 3 charger target should show outlet slots.");
            AssertPeeBreakDetail("PeeBreakCordPlug", "Beat 3 cord should have a visible plug end.");
            Assert.IsTrue(FindLoadedObject("PeeBreakChargerPluggedEnd").activeSelf,
                "The charger should visibly start plugged into the wall outlet.");

            _game.ForcePeeBreakAdvance(SocialStimulus.BarkRhythm, 2f);
            yield return null;

            Assert.AreEqual(1, Controller.Misreads);
            Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("TENNIS BALL"));
            Assert.That(WorldLabel("PeeBreakMisreadProp"), Does.Contain("WRONG IDEA"));
            Assert.That(WorldLabel("PeeBreakMisreadProp"), Does.Contain("TRY DOG JOBS"));
            Assert.That(_game.LastCue, Does.Contain("Funny, but not outside"));
            Assert.That(_game.LastJuiceLabel, Does.Contain("MISREAD: TENNIS BALL"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("BLOCK HALLWAY"));
            Assert.That(_game.TeamGuidanceLabel, Does.Contain("UNPLUG CHARGER"));

            _game.RecordColdReadQuestion("observer rehearsal first early bark misread");
            Assert.AreEqual(3, _game.ColdReadQuestionCount);
            Assert.That(_game.PlaytestCountersLabel, Does.Contain("cold-read ? 3"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("Cheddar BLOCK HALLWAY + Cocoa UNPLUG CHARGER"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("BLOCK HALLWAY"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("UNPLUG CHARGER"));
        }

        [UnityTest]
        public IEnumerator LiveRehearsal_BeatThreeBeatFourUsePositionsBarksAndRecoveries()
        {
            yield return LoadMission();
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);

            float previousTimeScale = Time.timeScale;
            Time.timeScale = 8f;
            try
            {
                yield return MoveDogTo(cocoa, Controller.DoorPosition);
                yield return MoveDogTo(cheddar, Controller.DoorPosition + Vector2.left * 8f);
                Assert.That(WorldLabel("PeeBreakDoor"), Does.Contain("COCOA STARE LOCKED"));
                Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("WATCH COCOA"));
                Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("HOLD DOOR STARE"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa: ON TARGET"));

                yield return LiveSeconds(0.8f);
                Assert.AreEqual(PeeBreakMissionController.Beat.LeashMessage, Controller.CurrentBeat);
                Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("PRESENT LEASH"));
                Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("HOLD DOOR STARE"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("PRESENT LEASH"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa: ON TARGET"));

                yield return MoveDogTo(cheddar, Controller.LeashPosition);
                Assert.That(WorldLabel("PeeBreakLeash"), Does.Contain("CHEDDAR LEASH READY"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cheddar: ON TARGET"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa: ON TARGET"));

                yield return LiveSeconds(2.2f);
                Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
                Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("BLOCK HALLWAY"));
                Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("UNPLUG CHARGER"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("BLOCK HALLWAY"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("UNPLUG CHARGER"));

                float charged = Controller.PhoneBattery;
                yield return MoveDogTo(cocoa, Controller.ChargerPosition);
                yield return MoveDogTo(cheddar, Controller.HallwayPosition + Vector2.left * 7f);
                Assert.That(WorldLabel("PeeBreakCharger"), Does.Contain("COCOA UNPLUGGING"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa: ON TARGET"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("BLOCK HALLWAY"));

                yield return LiveSeconds(0.4f);
                Assert.Less(Controller.PhoneBattery, charged, "Cocoa at the charger should visibly drain the phone even before Cheddar pins the hallway.");
                Assert.AreEqual(0f, Controller.Puzzle.Comprehension, 0.001f);
                Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
                float drainedByCocoa = Controller.PhoneBattery;

                yield return MoveDogTo(cocoa, Controller.ChargerPosition + Vector2.up * 6f);
                yield return LiveSeconds(0.4f);
                Assert.AreEqual(drainedByCocoa, Controller.PhoneBattery, 0.01f,
                    "Phone battery must stop draining once Cocoa leaves the charger station.");

                int barksBeforeMisread = _game.BarksUsed;
                cheddar.Bark();
                yield return LiveSeconds(2.4f);
                Assert.Greater(_game.BarksUsed, barksBeforeMisread, "The rehearsal must use the dog bark event path, not a force stimulus.");
                Assert.AreEqual(1, Controller.Misreads);
                Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
                Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
                Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("TENNIS BALL"));
                Assert.That(WorldLabel("PeeBreakMisreadProp"), Does.Contain("WRONG IDEA"));
                Assert.That(_game.LastCue, Does.Contain("Funny, but not outside"));

                yield return MoveDogTo(cheddar, Controller.HallwayPosition);
                yield return MoveDogTo(cocoa, Controller.ChargerPosition);
                Assert.That(WorldLabel("PeeBreakHallwayBlock"), Does.Contain("CHEDDAR BLOCK LOCKED"));
                Assert.That(WorldLabel("PeeBreakCharger"), Does.Contain("COCOA UNPLUGGING"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cheddar: ON TARGET"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa: ON TARGET"));

                yield return LiveSeconds(0.9f);
                float heldProgress = Controller.Puzzle.Comprehension;
                Assert.Greater(heldProgress, 0f);
                yield return MoveDogTo(cheddar, Controller.HallwayPosition + Vector2.down * 6f);
                yield return LiveSeconds(0.7f);
                Assert.Less(Controller.Puzzle.Comprehension, heldProgress,
                    "Comprehension must fall when Cheddar leaves the hallway block during Cocoa's unplug.");
                Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);

                yield return MoveDogTo(cheddar, Controller.HallwayPosition);
                yield return LiveSeconds(2.7f);
                Assert.AreEqual(PeeBreakMissionController.Beat.UnitedBark, Controller.CurrentBeat);
                Assert.AreEqual(0f, Controller.PhoneBattery, 0.001f);
                Assert.That(WorldLabel("PeeBreakPhone"), Does.Contain("0%"));
                Assert.IsTrue(FindLoadedObject("PeeBreakPhoneDeadSlash").activeSelf,
                    "The dead phone should have a visual state change beyond the 0% label.");
                Assert.IsTrue(FindLoadedObject("PeeBreakChargerUnpluggedEnd").activeSelf,
                    "After the charger gambit, the plug should visibly be out.");

                yield return MoveDogTo(cocoa, Controller.DoorPosition);
                Assert.That(WorldLabel("PeeBreakDoor"), Does.Contain("COCOA STARE LOCKED"));
                Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("LEASH + BARK"));
                Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("STARE + BARK"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa: ON TARGET"));
                yield return MoveDogTo(cheddar, Controller.LeashPosition);
                Assert.That(WorldLabel("PeeBreakLeash"), Does.Contain("CHEDDAR LEASH READY"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cheddar: ON TARGET"));
                Assert.That(_game.TeamGuidanceLabel, Does.Contain("Cocoa: ON TARGET"));

                int barksBeforeClimax = _game.BarksUsed;
                cheddar.Bark();
                yield return null;
                cocoa.Bark();
                yield return LiveSeconds(2.5f);
                Assert.GreaterOrEqual(_game.BarksUsed, barksBeforeClimax + 2);
                Assert.IsTrue(Controller.DoorOpen);
                Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
                Assert.AreEqual(GameManager.FlowState.EndScreen, _game.CurrentFlow);
                Assert.That(_game.EndSummaryLabel, Does.Contain("Outside, Eventually"));
                Assert.That(_game.EndChallengeLabel, Does.Contain("Replay target"));
                Assert.That(_game.EndChallengeLabel, Does.Contain("0 misreads"));
                Assert.That(_game.MvpLabel, Does.Not.Contain("awaiting dog heroics"));
                Assert.IsTrue(LogContains("PeeBreakDoorOpen"));

                _game.Restart();
                yield return null;
                yield return null;
                Assert.AreEqual(PeeBreakMissionController.Beat.DoorStare, Controller.CurrentBeat);
                Assert.IsFalse(Controller.DoorOpen);
                Assert.Less(Controller.Bladder, 0.2f);
                Assert.AreEqual(1f, Controller.PhoneBattery, 0.001f);
                Assert.AreEqual(0, Controller.Misreads);
                Assert.That(_game.ObjectiveArrows[0].Label, Does.Contain("WATCH COCOA"));
                Assert.That(_game.ObjectiveArrows[1].Label, Does.Contain("HOLD DOOR STARE"));
                Assert.That(WorldLabel("PeeBreakDoor"), Does.Contain("COCOA STAND HERE"));
                Assert.That(WorldLabel("PeeBreakPhone"), Does.Contain("100%"));
                Assert.That(WorldLabel("PeeBreakBladderMeter"), Does.Contain("12%"));
                Assert.IsFalse(FindLoadedObject("PeeBreakMisreadProp").activeSelf);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        [UnityTest]
        public IEnumerator BeatTwo_PartialComboNamesTheMissingPartnerJob()
        {
            yield return LoadMission();
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            yield return null;

            var cheddar = GameObject.Find("Cheddar");
            var cocoa = GameObject.Find("Cocoa");
            var door = GameObject.Find("PeeBreakDoor");
            var leash = GameObject.Find("PeeBreakLeash");
            var teenager = GameObject.Find("PeeBreakTeenager");
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);
            Assert.IsNotNull(door);
            Assert.IsNotNull(leash);
            Assert.IsNotNull(teenager);

            cocoa.transform.position = Controller.DoorPosition;
            cheddar.transform.position = Controller.DoorPosition + Vector2.left * 6f;
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 0.1f);
            Assert.That(teenager.GetComponentInChildren<TextMesh>().text, Does.Contain("NEEDS LEASH TOO"));
            Assert.That(door.GetComponentInChildren<TextMesh>().text, Does.Contain("NEEDS CHEDDAR LEASH"));

            cocoa.transform.position = Controller.DoorPosition + Vector2.left * 6f;
            cheddar.transform.position = Controller.LeashPosition;
            _game.ForcePeeBreakAdvance(SocialStimulus.PresentLeash, 0.1f);
            Assert.That(teenager.GetComponentInChildren<TextMesh>().text, Does.Contain("NEEDS STARE TOO"));
            Assert.That(leash.GetComponentInChildren<TextMesh>().text, Does.Contain("NEEDS COCOA STARE"));

            cocoa.transform.position = Controller.DoorPosition;
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, 0.1f);
            Assert.That(teenager.GetComponentInChildren<TextMesh>().text, Does.Contain("GETTING IT"));
        }

        [UnityTest]
        public IEnumerator LaterBeatPartialCombosNameMissingPartnerJobBeforePlayersReadHud()
        {
            yield return LoadMission();
            var cheddar = GameObject.Find("Cheddar");
            var cocoa = GameObject.Find("Cocoa");
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoa);

            AdvanceToCharger();
            yield return null;

            cheddar.transform.position = Controller.HallwayPosition;
            cocoa.transform.position = Controller.ChargerPosition + Vector2.up * 7f;
            _game.ForcePeeBreakAdvance(SocialStimulus.BlockHallway, 0.1f);
            Assert.That(WorldLabel("PeeBreakHallwayBlock"), Does.Contain("NEEDS COCOA CHARGER"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("NEEDS CHARGER TOO"));

            cheddar.transform.position = Controller.HallwayPosition + Vector2.left * 7f;
            cocoa.transform.position = Controller.ChargerPosition;
            _game.ForcePeeBreakAdvance(SocialStimulus.UnplugCharger, 0.1f);
            Assert.That(WorldLabel("PeeBreakCharger"), Does.Contain("NEEDS CHEDDAR BLOCK"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("NEEDS HALLWAY BLOCK"));

            cheddar.transform.position = Controller.HallwayPosition;
            _game.ForcePeeBreakAdvance(Controller.Required, 2.6f);
            yield return null;

            Assert.AreEqual(PeeBreakMissionController.Beat.UnitedBark, Controller.CurrentBeat);
            cocoa.transform.position = Controller.DoorPosition;
            cheddar.transform.position = Controller.DoorPosition + Vector2.left * 7f;
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 0.1f);
            Assert.That(WorldLabel("PeeBreakDoor"), Does.Contain("NEEDS CHEDDAR LEASH"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("NEEDS LEASH + BARK"));

            cocoa.transform.position = Controller.DoorPosition + Vector2.left * 7f;
            cheddar.transform.position = Controller.LeashPosition;
            _game.ForcePeeBreakAdvance(SocialStimulus.PresentLeash, 0.1f);
            Assert.That(WorldLabel("PeeBreakLeash"), Does.Contain("NEEDS COCOA STARE"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("NEEDS STARE + BARK"));

            cocoa.transform.position = Controller.DoorPosition;
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, 0.1f);
            Assert.That(WorldLabel("PeeBreakDoor"), Does.Contain("BARK TOGETHER"));
            Assert.That(WorldLabel("PeeBreakLeash"), Does.Contain("BARK TOGETHER"));
            Assert.That(WorldLabel("PeeBreakTeenager"), Does.Contain("BARK TOGETHER"));
        }

        [UnityTest]
        public IEnumerator OffMessageMisreadIsFunnyRecoverableFailure()
        {
            yield return LoadMission();
            _game.ForcePeeBreakAdvance(SocialStimulus.BarkRhythm, 4f);
            Assert.AreEqual(1, Controller.Misreads);
            Assert.AreEqual(PeeBreakMissionController.Beat.DoorStare, Controller.CurrentBeat);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
            Assert.AreEqual(0f, Controller.Puzzle.Comprehension, 0.001f);
            Assert.IsTrue(LogContains("PeeBreakMisread"));
            var wrongItem = GameObject.Find("PeeBreakMisreadProp");
            Assert.IsNotNull(wrongItem);
            Assert.That(wrongItem.GetComponentInChildren<TextMesh>().text, Does.Contain("TENNIS BALL"));
            Assert.That(wrongItem.GetComponentInChildren<TextMesh>().text, Does.Contain("WRONG IDEA"));
            Assert.That(wrongItem.GetComponentInChildren<TextMesh>().text, Does.Contain("TRY DOG JOBS"));
            Assert.AreEqual(new Color(0.55f, 1f, 0.28f), wrongItem.GetComponent<SpriteRenderer>().color);
            Assert.Greater(wrongItem.transform.localScale.x, 1.2f);
            var accent = FindLoadedObject("PeeBreakMisreadAccent");
            Assert.IsNotNull(accent);
            Assert.IsTrue(accent.activeSelf, "The first misread should read as a generated tennis ball before players parse the label.");
            Assert.AreEqual(Color.white, accent.GetComponent<SpriteRenderer>().color);
        }

        [UnityTest]
        public IEnumerator ColdReadQuestionMarkerRecordsCurrentBeatAndResetsOnReplay()
        {
            yield return LoadMission();

            Assert.IsFalse(_game.PlaytestOverlayVisible);
            Assert.That(_game.PlaytestHotkeysLabel, Does.Contain("F4"));
            Assert.That(_game.PlaytestHotkeysLabel, Does.Contain("cold-read"));
            _game.RecordColdReadQuestion("Cheddar asked what do I do?");
            Assert.IsTrue(_game.PlaytestOverlayVisible);
            Assert.AreEqual(1, _game.ColdReadQuestionCount);
            Assert.That(_game.PlaytestCountersLabel, Does.Contain("cold-read ? 1"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("ColdReadQuestion"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("OperationPeeBreak"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("Cocoa holds DOOR STARE"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("WATCH COCOA"));
            Assert.That(_game.LastPlaytestEvent, Does.Contain("Cheddar"));

            _game.Restart();
            yield return null;
            Assert.AreEqual(0, _game.ColdReadQuestionCount);
            Assert.That(_game.PlaytestCountersLabel, Does.Contain("cold-read ? 0"));
        }

        [UnityTest]
        public IEnumerator ChargerGambitDropsComprehensionWhenBlockDrops()
        {
            yield return LoadMission();
            AdvanceToCharger();
            _game.ForcePeeBreakAdvance(Controller.Required, 1f);
            float heldProgress = Controller.Puzzle.Comprehension;
            Assert.Greater(heldProgress, 0f);
            _game.ForcePeeBreakAdvance(SocialStimulus.UnplugCharger, 0.5f);
            Assert.Less(Controller.Puzzle.Comprehension, heldProgress);
            Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
        }

        [UnityTest]
        public IEnumerator ChargerOnlyDrainsWhileCocoaUnplugsAndVisiblyPowersDown()
        {
            yield return LoadMission();
            AdvanceToCharger();
            var phone = GameObject.Find("PeeBreakPhone");
            Color chargedColor = phone.GetComponent<SpriteRenderer>().color;

            _game.ForcePeeBreakAdvance(SocialStimulus.BlockHallway, 0.5f);
            Assert.AreEqual(1f, Controller.PhoneBattery, 0.001f,
                "Cheddar blocking alone must not drain the phone.");

            _game.ForcePeeBreakAdvance(Controller.Required, 0.5f);
            Assert.Less(Controller.PhoneBattery, 1f);
            Assert.AreNotEqual(chargedColor, phone.GetComponent<SpriteRenderer>().color);
            Assert.That(phone.GetComponentInChildren<TextMesh>().text, Does.Not.Contain("100%"));

            _game.ForcePeeBreakAdvance(Controller.Required, 2.1f);
            Assert.AreEqual(PeeBreakMissionController.Beat.UnitedBark, Controller.CurrentBeat);
            Assert.AreEqual(0f, Controller.PhoneBattery, 0.001f);
            Assert.That(phone.GetComponentInChildren<TextMesh>().text, Does.Contain("0%"));
        }

        [UnityTest]
        public IEnumerator ClimaxOpensDoorClearsAndReplayResetsEverything()
        {
            yield return LoadMission();
            AdvanceToCharger();
            _game.ForcePeeBreakAdvance(Controller.Required, 2.6f);
            Assert.AreEqual(PeeBreakMissionController.Beat.UnitedBark, Controller.CurrentBeat);
            Controller.ForceAdvance(Controller.Required, 2.3f);
            Assert.IsTrue(Controller.DoorOpen);
            Assert.IsTrue(FindLoadedObject("PeeBreakOpenSunbeam").activeSelf,
                "The controller-owned climax should briefly open the door with a visible sunshine reward before GameManager cleanup.");
            Assert.IsTrue(FindLoadedObject("PeeBreakDoorOutdoorView").activeSelf,
                "The open door should expose an outdoor/backyard payoff, not only shrink the door marker.");
            Assert.IsTrue(FindLoadedObject("PeeBreakDoorOpenPanel").activeSelf,
                "The open door should show a changed door silhouette.");
            AssertPeeBreakDetail("PeeBreakOutdoorGrassPatch", "The open door should reveal a readable outdoor grass payoff.");
            AssertPeeBreakDetail("PeeBreakOutdoorFireHydrant", "The Pee Break climax should land as a dog-authentic hydrant gag.");
            AssertPeeBreakDetail("PeeBreakOutdoorHydrantCap", "The hydrant should have enough silhouette detail to read before labels.");
            AssertPeeBreakDetail("PeeBreakReliefSparkleA", "The door-open payoff should have celebratory motion-ready sparkles.");
            AssertPeeBreakDetail("PeeBreakReliefSparkleB", "The door-open payoff should feel celebratory, not just like a state toggle.");
            AssertPeeBreakDetail("PeeBreakReliefSparkleC", "The door-open payoff should feel celebratory, not just like a state toggle.");
            Assert.That(_game.LastCue, Does.Contain("Relief zoomies"));
            Assert.That(_game.LastJuiceLabel, Does.Contain("RELIEF ZOOMIES"));
            _game.ForcePeeBreakAdvance(Controller.Required, 0.1f);
            yield return null;
            Assert.AreEqual(GameManager.MissionOutcome.Clear, _game.Outcome);
            Assert.AreEqual(GameManager.FlowState.EndScreen, _game.CurrentFlow);
            Assert.That(_game.EndSummaryLabel, Does.Contain("Pee Break Pawfect"));
            Assert.That(_game.EndChallengeLabel, Does.Contain("Challenge beaten"));
            Assert.That(_game.EndChallengeLabel, Does.Contain("FLAWLESS"));
            Assert.That(_game.MvpLabel, Does.Not.Contain("awaiting dog heroics"));
            Assert.IsTrue(_game.LastRoundFlawless, "A clean Pee Break run should invite replay as a flawless couch-test target.");
            Assert.IsTrue(LogContains("PeeBreakDoorOpen"));
            _game.Restart();
            yield return null;
            Assert.AreEqual(PeeBreakMissionController.Beat.DoorStare, Controller.CurrentBeat);
            Assert.AreEqual(0, Controller.Misreads);
            Assert.AreEqual(0, Controller.CompletedBeats);
            Assert.Less(Controller.Bladder, 0.2f);
            Assert.IsFalse(Controller.DoorOpen);
            Assert.AreEqual(GameManager.MissionOutcome.InProgress, _game.Outcome);
        }

        [UnityTest]
        public IEnumerator MissionSwitchCleansOwnedActorsAndReusesResetController()
        {
            yield return LoadMission();
            var originalController = Controller;
            var ownedActors = new[]
            {
                FindLoadedObject("PeeBreakDoor"),
                FindLoadedObject("PeeBreakLeash"),
                FindLoadedObject("PeeBreakHallwayBlock"),
                FindLoadedObject("PeeBreakCharger"),
                FindLoadedObject("PeeBreakCheddarCoach"),
                FindLoadedObject("PeeBreakTeenager"),
                FindLoadedObject("PeeBreakPhone"),
                FindLoadedObject("PeeBreakBladderMeter"),
                FindLoadedObject("PeeBreakMisreadProp"),
                FindLoadedObject("PeeBreakMisreadAccent"),
                FindLoadedObject("PeeBreakRoomFloor"),
                FindLoadedObject("PeeBreakCouchBack"),
                FindLoadedObject("PeeBreakCouchSeat"),
                FindLoadedObject("PeeBreakSideTable"),
                FindLoadedObject("PeeBreakPhoneGlow"),
                FindLoadedObject("PeeBreakChargerCord"),
                FindLoadedObject("PeeBreakDoorFrame"),
                FindLoadedObject("PeeBreakOpenSunbeam"),
                FindLoadedObject("PeeBreakLeashHook"),
                FindLoadedObject("PeeBreakHallwayRug")
            };

            _game.ForcePeeBreakAdvance(SocialStimulus.BarkRhythm, 4f);
            Assert.AreEqual(1, Controller.Misreads);
            _game.StartMission(GameManager.MissionVariant.KitchenFoodFrenzy);
            yield return null;

            foreach (var actor in ownedActors)
            {
                Assert.IsNotNull(actor);
                Assert.IsFalse(actor.activeSelf, $"{actor.name} leaked into the Kitchen mission.");
            }

            _game.StartMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;
            Assert.AreSame(originalController, Controller);
            Assert.AreEqual(0, Controller.Misreads);
            Assert.AreEqual(PeeBreakMissionController.Beat.DoorStare, Controller.CurrentBeat);
            Assert.IsTrue(ownedActors[0].activeSelf);
            Assert.IsTrue(ownedActors[4].activeSelf);
            Assert.IsTrue(ownedActors[5].activeSelf);
            Assert.IsTrue(ownedActors[6].activeSelf);
            Assert.IsTrue(ownedActors[7].activeSelf);
            Assert.IsFalse(ownedActors[8].activeSelf);
            Assert.IsFalse(ownedActors[9].activeSelf);
            Assert.IsTrue(ownedActors[10].activeSelf);
            Assert.IsTrue(ownedActors[11].activeSelf);
            Assert.IsTrue(ownedActors[12].activeSelf);
            Assert.IsTrue(ownedActors[13].activeSelf);
            Assert.IsTrue(ownedActors[14].activeSelf);
            Assert.IsFalse(ownedActors[15].activeSelf);
            Assert.IsTrue(ownedActors[16].activeSelf);
            Assert.IsFalse(ownedActors[17].activeSelf);
            Assert.IsTrue(ownedActors[18].activeSelf);
            Assert.IsTrue(ownedActors[19].activeSelf);
            foreach (var actor in ownedActors)
            {
                int matchingActors = 0;
                foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
                    if (candidate.name == actor.name) matchingActors++;
                Assert.AreEqual(1, matchingActors, $"{actor.name} should be reused, not duplicated.");
            }
        }

        private void AdvanceToCharger()
        {
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
            _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, 2.1f);
            Assert.AreEqual(PeeBreakMissionController.Beat.ChargerGambit, Controller.CurrentBeat);
        }

        private IEnumerator LoadMission()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
            _game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(_game);
            _game.StartMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;
        }

        private IEnumerator MoveDogTo(DogController dog, Vector2 position)
        {
            dog.transform.position = new Vector3(position.x, position.y, dog.transform.position.z);
            if (dog.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return null;
        }

        private IEnumerator LiveSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private bool LogContains(string text)
        {
            foreach (string entry in _game.PlaytestEvents)
                if (entry.Contains(text)) return true;
            return false;
        }

        private static GameObject FindLoadedObject(string name)
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
                if (candidate.name == name && candidate.scene.IsValid()) return candidate;
            return null;
        }

        private static void AssertPeeBreakScenery(string objectName, string message)
        {
            var target = FindLoadedObject(objectName);
            Assert.IsNotNull(target, message);
            Assert.IsNotNull(target.GetComponent<SpriteRenderer>(), $"{objectName} should render as set dressing.");
            Assert.IsNull(target.GetComponent<Collider2D>(), $"{objectName} must stay nonblocking.");
            Assert.Less(target.GetComponent<SpriteRenderer>().sortingOrder, 5,
                $"{objectName} should render behind gameplay markers and dogs.");
        }

        private static void AssertPeeBreakDetail(string objectName, string message)
        {
            var target = FindLoadedObject(objectName);
            Assert.IsNotNull(target, message);
            Assert.IsTrue(target.activeInHierarchy, $"{objectName} should be visible for the current beat.");
            Assert.IsNotNull(target.GetComponent<SpriteRenderer>(), $"{objectName} should render as a silhouette detail.");
            Assert.IsNull(target.GetComponent<Collider2D>(), $"{objectName} must stay decorative and nonblocking.");
            Assert.Less(target.GetComponent<SpriteRenderer>().sortingOrder, 10,
                $"{objectName} should render below dog sprites.");
        }

        private static DogController FindDog(DogId dogId)
        {
            foreach (var identity in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
                if (identity.Id == dogId) return identity.GetComponent<DogController>();
            return null;
        }

        private static string WorldLabel(string objectName)
        {
            var target = FindLoadedObject(objectName);
            Assert.IsNotNull(target, $"{objectName} should exist for the observer rehearsal.");
            var label = target.GetComponentInChildren<TextMesh>();
            Assert.IsNotNull(label, $"{objectName} should expose a readable world label.");
            return label.text;
        }
    }
}
