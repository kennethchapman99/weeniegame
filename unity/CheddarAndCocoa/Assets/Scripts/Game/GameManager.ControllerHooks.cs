using UnityEngine;

namespace CheddarAndCocoa.Game
{
    // Compatibility hooks for PlayMode tests and deterministic rehearsals. These forward to the
    // active mission controller instead of reintroducing mission-owned state into GameManager.
    public sealed partial class GameManager : MonoBehaviour
    {
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

        public void ForceWalkCampaign(float seconds, bool doorStare, bool presentLeash)
        {
            if (MissionActive()) WalkCampaignController?.ForceWalkCampaign(seconds, doorStare, presentLeash);
            CheckClear();
        }

        public void ForceBoneReveal()
        {
            if (MissionActive()) BoneRelayController?.ForceBoneReveal();
            CheckClear();
        }

        public void ForceBoneDig(int target)
        {
            if (MissionActive()) BoneRelayController?.ForceBoneDig(target);
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

        public void ForceChaosTrigger()
        {
            if (MissionActive()) ChaosMachineController?.ForceChaosTrigger();
        }

        public void ForceChaosAdvance(float seconds, bool assisting)
        {
            if (MissionActive()) ChaosMachineController?.ForceChaosAdvance(seconds, assisting);
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
    }
}
