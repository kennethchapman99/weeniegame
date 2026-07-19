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
    /// G1.4: the handoff flip flourish (MissionContext.SignalRoleHandoff - baton-swoosh visual, both
    /// HUD chips flash, an identity-distinct audio chime for the receiving dog - S5.1) wired into each
    /// mission's own existing mid-mission role-flip moment. GameManager.LastHandoffFromDog/ToDog/
    /// HandoffSignalCount are the deterministic test surface (mirrors LastAudioCueRequested/
    /// AudioCueRequests from earlier work).
    ///
    /// Pee Break's Beat-3 charger flip is deliberately NOT wired: both dogs' roles change to new,
    /// non-swapped assignments simultaneously there (Cheddar leash->hallway, Cocoa stare->charger),
    /// which isn't a "dog A hands off to dog B" moment the way the other six sites are - forcing a
    /// fromDog/toDog pair onto it would misrepresent what's actually happening.
    /// </summary>
    public sealed class RoleHandoffPlayModeTests
    {
        [UnityTest]
        public IEnumerator GreatEscape_EachStep_FiresOneHandoff_ToTheNextOwner()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            Assert.AreEqual(0, game.HandoffSignalCount);

            // Owners = { Cocoa, Cheddar, Cocoa, Cheddar }.
            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner); // Cocoa's step completes
            yield return null;
            Assert.AreEqual(1, game.HandoffSignalCount);
            Assert.AreEqual(DogId.Cocoa, game.LastHandoffFromDog);
            Assert.AreEqual(DogId.Cheddar, game.LastHandoffToDog);

            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner); // Cheddar's step completes
            yield return null;
            Assert.AreEqual(2, game.HandoffSignalCount);
            Assert.AreEqual(DogId.Cheddar, game.LastHandoffFromDog);
            Assert.AreEqual(DogId.Cocoa, game.LastHandoffToDog);
        }

        [UnityTest]
        public IEnumerator GreatEscape_EachStep_FiresTheReceivingDogsOwnIdentityDistinctChime()
        {
            // S5.1: HandoffChimeCheddar/HandoffChimeCocoa replace the single shared placeholder cue -
            // the chime must match whichever dog is now taking over (toDog), not the one handing off.
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            int cheddarChimesBefore = CountCues(game, ArenaFeedbackCatalog.HandoffChimeCheddar);
            int cocoaChimesBefore = CountCues(game, ArenaFeedbackCatalog.HandoffChimeCocoa);

            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner); // Cocoa -> Cheddar
            yield return null;
            Assert.AreEqual(DogId.Cheddar, game.LastHandoffToDog);
            Assert.AreEqual(cheddarChimesBefore + 1, CountCues(game, ArenaFeedbackCatalog.HandoffChimeCheddar),
                "Handing off TO Cheddar must fire Cheddar's own chime.");
            Assert.AreEqual(cocoaChimesBefore, CountCues(game, ArenaFeedbackCatalog.HandoffChimeCocoa),
                "Handing off TO Cheddar must not also fire Cocoa's chime.");

            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner); // Cheddar -> Cocoa
            yield return null;
            Assert.AreEqual(DogId.Cocoa, game.LastHandoffToDog);
            Assert.AreEqual(cocoaChimesBefore + 1, CountCues(game, ArenaFeedbackCatalog.HandoffChimeCocoa),
                "Handing off TO Cocoa must fire Cocoa's own chime.");
            Assert.AreEqual(cheddarChimesBefore + 1, CountCues(game, ArenaFeedbackCatalog.HandoffChimeCheddar),
                "Handing off TO Cocoa must not fire another Cheddar chime.");
        }

        [UnityTest]
        public IEnumerator GreatEscape_WrongDogFumble_FiresNoHandoff()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            ChainActor owner = game.GreatEscapePuzzle.NextOwner;
            ChainActor wrong = owner == ChainActor.Cocoa ? ChainActor.Cheddar : ChainActor.Cocoa;
            game.ForceEscapeStep(wrong);
            yield return null;

            Assert.AreEqual(0, game.HandoffSignalCount, "A harmless wrong-dog fumble is not progress - it must not fire a handoff.");
        }

        [UnityTest]
        public IEnumerator GreatEscape_FinalStep_FiresNoHandoff_NoNextOwnerToPassTo()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            int steps = game.GreatEscapePuzzle.StepCount;
            for (int i = 0; i < steps - 1; i++)
                game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner);
            yield return null;
            int countBeforeFinal = game.HandoffSignalCount;

            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner); // solves it
            yield return null;

            Assert.IsTrue(game.GreatEscapePuzzle.Solved);
            Assert.AreEqual(countBeforeFinal, game.HandoffSignalCount, "Once solved there is no next owner to hand off to.");
        }

        [UnityTest]
        public IEnumerator ChaosMachine_StageAdvance_FiresHandoff_ButPullingTheLeverAloneDoesNot()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.ChaosMachine);
            yield return null;

            game.ForceChaosTrigger(); // Cheddar pulls the lever; Stage stays 0, nothing has advanced yet
            yield return null;
            Assert.AreEqual(0, game.HandoffSignalCount, "Pulling the lever starts the cascade but is not itself a role handoff.");

            // Owners = { Cocoa, Cheddar, Cocoa }.
            game.ForceChaosAdvance(0.5f, assisting: true);
            yield return null;

            Assert.AreEqual(1, game.HandoffSignalCount);
            Assert.AreEqual(DogId.Cocoa, game.LastHandoffFromDog);
            Assert.AreEqual(DogId.Cheddar, game.LastHandoffToDog);
        }

        [UnityTest]
        public IEnumerator ScentSearch_CocoaHotCall_FiresHandoffToCheddar_ButCheddarsDirectionSniffDoesNot()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.ScentSearch);
            yield return null;
            var controller = (ScentSearchMissionController)game.ActiveMissionController;

            game.ForceScentSniff(DogId.Cheddar);
            yield return null;
            Assert.AreEqual(0, game.HandoffSignalCount, "Cheddar's direction sniff is a hint, not a role handoff.");

            int buried = controller.BuriedSpotIndex;
            var cocoa = FindDog(DogId.Cocoa);
            cocoa.transform.position = controller.DigSpots[buried];
            yield return null;

            game.ForceScentSniff(DogId.Cocoa);
            yield return null;

            Assert.AreEqual(1, game.HandoffSignalCount);
            Assert.AreEqual(DogId.Cocoa, game.LastHandoffFromDog);
            Assert.AreEqual(DogId.Cheddar, game.LastHandoffToDog);
        }

        [UnityTest]
        public IEnumerator TableStealth_BurpThenFlop_FiresHandoffsInBothDirections()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.TableStealth);
            yield return null;
            var controller = (TableStealthMissionController)game.ActiveMissionController;
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);

            cheddar.transform.position = controller.HumanZone;
            cheddar.Bark();
            yield return null;

            Assert.AreEqual(1, game.HandoffSignalCount, "Cheddar's burp should open Cocoa's sneak window.");
            Assert.AreEqual(DogId.Cheddar, game.LastHandoffFromDog);
            Assert.AreEqual(DogId.Cocoa, game.LastHandoffToDog);

            cocoa.transform.position = controller.HumanZone;
            cocoa.Interact();
            yield return null;

            Assert.AreEqual(2, game.HandoffSignalCount, "Cocoa's flop should open Cheddar's sneak window.");
            Assert.AreEqual(DogId.Cocoa, game.LastHandoffFromDog);
            Assert.AreEqual(DogId.Cheddar, game.LastHandoffToDog);
        }

        [UnityTest]
        public IEnumerator WeenieRoundup_JumboLift_FiresHandoffFromCocoaToCheddar()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.WeenieRoundup);
            yield return null;
            var cheddar = FindDog(DogId.Cheddar);
            var cocoa = FindDog(DogId.Cocoa);

            int required = game.RuntimeSnapshot.ObjectiveGoal;
            for (int i = 0; i < required - 1; i++)
            {
                DogId carrier = i % 2 == 0 ? DogId.Cheddar : DogId.Cocoa;
                game.ForceWeeniePickup(carrier);
                game.ForceWeenieDeliver(carrier);
            }
            yield return null;
            Assert.AreEqual(0, game.HandoffSignalCount, "The fast parallel-carry loop is symmetric - no single-owner handoff there.");

            Vector2 jumbo = WeenieRoundupMissionController.ComputeSpots(game.ArenaBounds)[required - 1];
            cheddar.transform.position = jumbo;
            cocoa.transform.position = jumbo + Vector2.right;
            yield return null;

            Assert.AreEqual(1, game.HandoffSignalCount, "Cocoa steadying then Cheddar lifting the jumbo is a real handoff.");
            Assert.AreEqual(DogId.Cocoa, game.LastHandoffFromDog);
            Assert.AreEqual(DogId.Cheddar, game.LastHandoffToDog);
        }

        [UnityTest]
        public IEnumerator BlanketCatch_CalledDrop_FiresHandoffFromCocoaToCheddar()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.BlanketCatch);
            yield return null;

            game.ForceBlanketSpan(2f, 0f); // slack, not taut yet
            Assert.IsFalse(game.ForceBlanketCallDrop());
            yield return null;
            Assert.AreEqual(0, game.HandoffSignalCount, "A blocked call (blanket not taut) must not fire a handoff.");

            game.ForceBlanketSpan(7.5f, 0f); // taut
            Assert.IsTrue(game.ForceBlanketCallDrop());
            yield return null;

            Assert.AreEqual(1, game.HandoffSignalCount);
            Assert.AreEqual(DogId.Cocoa, game.LastHandoffFromDog);
            Assert.AreEqual(DogId.Cheddar, game.LastHandoffToDog);
        }

        [UnityTest]
        public IEnumerator Handoff_PulsesBothHudChips_AndReverts()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            Assert.IsFalse(game.HandoffChipFlashVisible);
            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner);
            yield return null;

            Assert.IsTrue(game.HandoffChipFlashVisible);
        }

        [UnityTest]
        public IEnumerator Replay_ClearsHandoffState()
        {
            yield return LoadArena();
            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.GreatEscape);
            yield return null;

            game.ForceEscapeStep(game.GreatEscapePuzzle.NextOwner);
            yield return null;
            Assert.AreEqual(1, game.HandoffSignalCount);

            game.Restart();
            yield return null;

            Assert.AreEqual(0, game.HandoffSignalCount);
            Assert.IsNull(game.LastHandoffFromDog);
            Assert.IsNull(game.LastHandoffToDog);
            Assert.IsFalse(game.HandoffChipFlashVisible);
        }

        private static int CountCues(GameManager game, string cueName)
        {
            int count = 0;
            foreach (string cue in game.AudioCueRequests)
                if (cue == cueName) count++;
            return count;
        }

        private static IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        private static DogController FindDog(DogId dogId)
        {
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == dogId) return id.GetComponent<DogController>();
            }

            return null;
        }
    }
}
