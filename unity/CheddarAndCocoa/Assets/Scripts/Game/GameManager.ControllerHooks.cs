using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    // Compatibility hooks for PlayMode tests and deterministic rehearsals. These forward to the
    // active mission controller instead of reintroducing mission-owned state into GameManager.
    public sealed partial class GameManager : MonoBehaviour
    {
        /// <summary>Advances the guidance-escalation stall clock by an exact amount for deterministic tests.</summary>
        public void ForceGuidanceStall(float seconds)
        {
            if (MissionActive()) TickGuidance(seconds);
        }

        public void ForceGateHold(bool held = true)
        {
            if (MissionActive()) GateCrashController?.ForceGateHold(held);
            CheckClear();
        }

        public void ForceGateCross(float seconds)
        {
            if (MissionActive()) GateCrashController?.ForceGateCross(seconds);
            CheckClear();
        }

        public void ForceGateSuccessPresentationComplete()
        {
            if (MissionActive()) GateCrashController?.ForceFinishSuccessPresentation();
            CheckClear();
        }

        public bool ForceMarkInteraction(DogId dogId, int zoneIndex)
        {
            return MissionActive() && MarkTheYardController != null &&
                   MarkTheYardController.ForceMarkInteraction(dogId, zoneIndex);
        }

        public bool ForceMarkYardDefenseBark(DogId dogId)
        {
            return MissionActive() && MarkTheYardController != null &&
                   MarkTheYardController.ForceDefenseBark(dogId);
        }

        public void ForceMarkYardSuccessPresentationComplete()
        {
            if (MissionActive()) MarkTheYardController?.ForceFinishSuccessPresentation();
            CheckClear();
        }

        public void ForceTableFlop(bool flopped = true)
        {
            if (MissionActive()) TableStealthController?.ForceTableFlop(flopped);
        }

        public void ForceTableBurp()
        {
            if (MissionActive()) TableStealthController?.ForceTableBurp();
        }

        public void ForceTableSneak(float seconds)
        {
            if (MissionActive()) TableStealthController?.ForceTableSneak(seconds);
            CheckClear();
        }

        public void ForceTableSuccessPresentationComplete()
        {
            if (MissionActive()) TableStealthController?.ForceFinishSuccessPresentation();
            CheckClear();
        }

        public void ForceSwitcherooBait(float seconds, bool baiting = true)
        {
            if (MissionActive()) SquirrelSwitcherooController?.ForceSwitcherooBait(seconds, baiting);
            CheckClear();
        }

        public void ForceSwitcherooStrike()
        {
            if (MissionActive()) SquirrelSwitcherooController?.ForceSwitcherooStrike();
            CheckClear();
        }

        public void ForceSwitcherooSuccessPresentationComplete()
        {
            if (MissionActive()) SquirrelSwitcherooController?.ForceFinishSuccessPresentation();
            CheckClear();
        }

        public void ForceWalkCampaign(float seconds, bool doorStare, bool presentLeash)
        {
            if (MissionActive()) WalkCampaignController?.ForceWalkCampaign(seconds, doorStare, presentLeash);
            CheckClear();
        }

        public void ForceWalkCampaignSuccessPresentationComplete()
        {
            if (MissionActive()) WalkCampaignController?.ForceFinishSuccessPresentation();
            CheckClear();
        }

        public void ForceBoneReveal()
        {
            if (MissionActive()) BoneRelayController?.ForceBoneReveal();
            CheckClear();
        }

        public bool ForceBoneCall()
        {
            return MissionActive() && BoneRelayController != null && BoneRelayController.ForceCocoaCall();
        }

        public void ForceBoneDig(int target)
        {
            if (MissionActive()) BoneRelayController?.ForceBoneDig(target);
            CheckClear();
        }

        public void ForceBoneSuccessPresentationComplete()
        {
            if (MissionActive()) BoneRelayController?.ForceFinishSuccessPresentation();
            CheckClear();
        }

        public void ForceEscapeStep(ChainActor actor)
        {
            if (MissionActive()) GreatEscapeController?.ForceEscapeStep(actor);
            CheckClear();
        }

        public void ForceEscapeIdle(float seconds)
        {
            if (MissionActive()) GreatEscapeController?.ForceEscapeIdle(seconds);
            CheckClear();
        }

        public void ForceEscapeSuccessPresentationComplete()
        {
            if (MissionActive()) GreatEscapeController?.ForceFinishSuccessPresentation();
            CheckClear();
        }

        public void ForceChaosTrigger()
        {
            if (MissionActive()) ChaosMachineController?.ForceChaosTrigger();
        }

        public void ForceChaosAdvance(float seconds, bool assisting)
        {
            if (MissionActive()) ChaosMachineController?.ForceChaosAdvance(seconds, assisting);
            CheckClear();
        }

        public void ForceChaosSuccessPresentationComplete()
        {
            if (MissionActive()) ChaosMachineController?.ForceFinishSuccessPresentation();
            CheckClear();
        }

        public void ForceBlanketSpan(float separation, float midpointX)
        {
            if (MissionActive()) BlanketCatchController?.ForceBlanketSpan(separation, midpointX);
            CheckClear();
        }

        public void ForceBlanketCatch(float itemX)
        {
            if (MissionActive()) BlanketCatchController?.ForceBlanketCatch(itemX);
            CheckClear();
        }

        public bool ForceBlanketCallDrop()
        {
            return MissionActive() && BlanketCatchController != null && BlanketCatchController.ForceCocoaCallDrop();
        }

        public void ForceBlanketSuccessPresentationComplete()
        {
            if (MissionActive()) BlanketCatchController?.ForceFinishSuccessPresentation();
            CheckClear();
        }

        public void ForceChickLand(float x)
        {
            if (MissionActive()) BabyBirdBedlamController?.ForceChickLand(x);
        }

        public void ForceChickGrab()
        {
            if (MissionActive()) BabyBirdBedlamController?.ForceChickGrab();
        }

        public void ForceChickShake()
        {
            if (MissionActive()) BabyBirdBedlamController?.ForceChickShake();
            CheckClear();
        }

        public void ForceParentDive()
        {
            if (MissionActive()) BabyBirdBedlamController?.ForceParentDive();
        }

        public void ForceParentRepel()
        {
            if (MissionActive()) BabyBirdBedlamController?.ForceParentRepel();
        }

        public void ForceDiveAdvance(float seconds)
        {
            if (MissionActive()) BabyBirdBedlamController?.ForceDiveAdvance(seconds);
            CheckClear();
        }

        public void ForceChickAirlift()
        {
            if (MissionActive()) BabyBirdBedlamController?.ForceChickAirlift();
        }
    }
}
