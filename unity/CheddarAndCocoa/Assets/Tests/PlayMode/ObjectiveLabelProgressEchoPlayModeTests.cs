using System;
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
    /// CF2.2 regression guard. The roster-wide HUD/meter-occlusion audit
    /// (docs/AGENT-WORK-QUEUE-COUCHFIX.md, CF2.2) found that every mission already mirrors its key
    /// mid-play progress counter into the screen-space <see cref="GameManager.ObjectiveLabel"/> top
    /// bar - that duplication is WHY none of the 23 controllers share Operation Pee Break's
    /// pre-CF1.2 bug (a world-anchored meter the camera could scroll off-screen with nothing
    /// visible to fall back on). This test encodes that invariant directly instead of just relying
    /// on the audit's prose: for every mission, force one small, deterministic step of progress via
    /// the mission's own existing Force* hook (no new plumbing) and assert ObjectiveLabel visibly
    /// changed to reflect it. A future mission that adds a load-bearing counter without echoing it
    /// into ObjectiveLabel fails here instead of waiting for the next couch test to surface it.
    /// </summary>
    public sealed class ObjectiveLabelProgressEchoPlayModeTests
    {
        [UnityTest]
        public IEnumerator EveryMission_ObjectiveLabelChangesWhenProgressAdvances()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);

            DogController cheddar = null;
            DogController cocoa = null;
            foreach (var id in UnityEngine.Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) cheddar = id.GetComponent<DogController>();
                if (id.Id == DogId.Cocoa) cocoa = id.GetComponent<DogController>();
            }
            Assert.IsNotNull(cheddar, "Needs Cheddar's DogController to drive dog-position-dependent hooks.");
            Assert.IsNotNull(cocoa, "Needs Cocoa's DogController to drive dog-position-dependent hooks.");

            foreach (GameManager.MissionVariant variant in Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                game.StartMission(variant);
                yield return null;

                string initialLabel = game.ObjectiveLabel;
                Assert.IsFalse(string.IsNullOrEmpty(initialLabel), $"{variant} needs a non-empty starting objective.");

                // Read ObjectiveLabel synchronously, with no frame boundary in between: several
                // controllers' Tick() recomputes hold/anchor state from the (unrelated, still
                // out-of-range) real dog positions every frame and would otherwise clobber the
                // forced value before this test ever observes it (GateCrash's _anchorEngaged is
                // exactly this shape).
                PokeProgress(game, variant, cheddar, cocoa);

                string advancedLabel = game.ObjectiveLabel;
                Assert.IsFalse(string.IsNullOrEmpty(advancedLabel), $"{variant} needs a non-empty objective after progress.");
                Assert.AreNotEqual(initialLabel, advancedLabel,
                    $"{variant}'s ObjectiveLabel should change once its progress counter advances - this is " +
                    "the screen-space echo CF2.2's audit found every mission relies on instead of a " +
                    "world-anchored-only meter the camera could hide.");
            }
        }

        /// <summary>
        /// Drives one small, deterministic step of "real progress" per mission using that
        /// controller's own existing Force*/HandleBark hook (reused, not new plumbing) - just
        /// enough to move whatever counter that mission's ObjectiveLabel already reports.
        /// </summary>
        private static void PokeProgress(GameManager game, GameManager.MissionVariant variant, DogController cheddar, DogController cocoa)
        {
            switch (variant)
            {
                case GameManager.MissionVariant.BackyardRescue:
                    game.ForceSquirrelStealAttempt();
                    break;
                case GameManager.MissionVariant.SnackHeist:
                    // ForceStealAttempt()/ForceSteal() both resolve the theft transiently within
                    // the same call (they self-clear _squirrelTarget), so the label reverts before
                    // the next frame reads it. ForceCollectTreat() bumps the persistent Recovered
                    // count the default-branch ObjectiveLabel actually reports.
                    game.ForceCollectTreat();
                    break;
                case GameManager.MissionVariant.SockPanic:
                    game.ForceSockBasketTip(DogId.Cocoa);
                    break;
                case GameManager.MissionVariant.SquirrelConspiracy:
                    game.ForceSquirrelConspiracyTaunt();
                    break;
                case GameManager.MissionVariant.EagleShadowPanic:
                    game.ForceEagleShadowSafeHide();
                    break;
                case GameManager.MissionVariant.CoyotesFence:
                    game.ForceCoyoteBarkPressure(DogId.Cocoa);
                    break;
                case GameManager.MissionVariant.WeenieRoundup:
                    game.ForceWeeniePickup(DogId.Cheddar);
                    game.ForceWeenieDeliver(DogId.Cheddar);
                    break;
                case GameManager.MissionVariant.ScentSearch:
                    game.ForceScentDigCorrect(DogId.Cheddar);
                    break;
                case GameManager.MissionVariant.ThunderstormComfort:
                    // HandleBark's huddle check has no force-bypass, so (unlike every other poke
                    // here) this one needs the dogs actually together before the real bark path.
                    cocoa.transform.position = game.ArenaBounds.center;
                    cheddar.transform.position = game.ArenaBounds.center;
                    cocoa.Bark();
                    cheddar.Bark();
                    break;
                case GameManager.MissionVariant.MarkTheYard:
                    game.ForceClaimZone(DogId.Cheddar);
                    break;
                case GameManager.MissionVariant.LeashWalk:
                    game.ForceReachCheckpoint();
                    break;
                case GameManager.MissionVariant.CarRide:
                    game.ForceCarEventSurvived();
                    break;
                case GameManager.MissionVariant.GateCrash:
                    game.ForceGateHold(true);
                    break;
                case GameManager.MissionVariant.TableStealth:
                    // ForceTableFlop(true) only arms the sustained hold; Attention still has to
                    // accrue over real Advance() time via Tick(), which we deliberately don't run
                    // here. ForceTableBurp() spikes Attention past the distraction threshold in one
                    // synchronous call instead.
                    game.ForceTableBurp();
                    break;
                case GameManager.MissionVariant.SquirrelSwitcheroo:
                    // 1s would push Commitment to the 1.0 ceiling and immediately trip the
                    // overbait-backfire reset in the same call (CommitRate=1, OverbaitTolerance=
                    // 0.6s); 0.7s clears the 0.6 Committed threshold while staying under 1.0.
                    game.ForceSwitcherooBait(0.7f, true);
                    break;
                case GameManager.MissionVariant.WalkCampaign:
                    game.ForceWalkCampaign(0.1f, true, true);
                    break;
                case GameManager.MissionVariant.BoneRelay:
                    game.ForceBoneReveal();
                    break;
                case GameManager.MissionVariant.GreatEscape:
                    game.ForceEscapeStep(ChainActor.Cocoa);
                    break;
                case GameManager.MissionVariant.ChaosMachine:
                    game.ForceChaosTrigger();
                    break;
                case GameManager.MissionVariant.BlanketCatch:
                    game.ForceBlanketSpan(7.5f, 0f);
                    game.ForceBlanketCatch(0f);
                    break;
                case GameManager.MissionVariant.BabyBirdBedlam:
                    game.ForceChickLand(0f);
                    break;
                case GameManager.MissionVariant.KitchenFoodFrenzy:
                    game.ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind.Good);
                    break;
                case GameManager.MissionVariant.OperationPeeBreak:
                    game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 2.6f);
                    break;
                default:
                    Assert.Fail($"No progress poke wired for {variant} - add one so this guard covers the full roster.");
                    break;
            }
        }
    }
}
