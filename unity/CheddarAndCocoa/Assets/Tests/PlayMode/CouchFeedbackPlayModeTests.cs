using System.Collections;
using CheddarAndCocoa.Bootstrap;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Game;
using CheddarAndCocoa.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace CheddarAndCocoa.Tests
{
    /// <summary>End-user comfort feedback must reach both couch players and remain optional.</summary>
    public sealed class CouchFeedbackPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator ShowcaseFive_AreOrderedByCurrentQuality_AndKeepTheCouchReadabilityContract()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
            yield return null;

            new GameObject("Boot").AddComponent<ArenaBootstrap>();
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            var expected = new[]
            {
                GameManager.MissionVariant.OperationPeeBreak,
                GameManager.MissionVariant.KitchenFoodFrenzy,
                GameManager.MissionVariant.CarRide,
                GameManager.MissionVariant.BabyBirdBedlam,
                GameManager.MissionVariant.GateCrash,
            };

            Assert.AreEqual(expected[0], game.SelectedMissionVariant,
                "Cold start should focus the highest-quality deep slice.");
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], game.MissionVariantAt(i),
                    $"Showcase slot {i + 1} should follow the reviewed quality order.");
                var definition = GameManager.BuildMissionDefinition(expected[i]);
                Assert.IsNotEmpty(definition.IntroPrompt);
                Assert.IsNotEmpty(definition.RoleHint);
                StringAssert.DoesNotContain("%", definition.IntroPrompt,
                    $"{expected[i]} should explain the job without a changing number.");
                string preview = MissionSelectScreen.BuildHowToPlayText(expected[i]);
                Assert.IsNotEmpty(preview);
                Assert.That(preview.Split('\n').Length, Is.InRange(1, 4),
                    $"{expected[i]} needs a couch-readable progressive team-plan preview.");
                Assert.IsNotNull(FinalGameplayArt.LoadMissionTile(expected[i]),
                    $"{expected[i]} needs authored picker art before it belongs in the showcase five.");
            }
        }

        [UnityTest]
        public IEnumerator EveryRegisteredMission_PassesTheSameColdStartReadabilityAudit()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
            yield return null;

            new GameObject("Boot").AddComponent<ArenaBootstrap>();
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            Assert.AreEqual(24, game.MissionSelectOptionCount,
                "The roster stays frozen while the couch-readability pass is in progress.");

            for (int i = 0; i < game.MissionSelectOptionCount; i++)
            {
                var variant = game.MissionVariantAt(i);
                var definition = GameManager.BuildMissionDefinition(variant);
                game.StartMission(variant);
                yield return null;

                Assert.IsNotNull(game.ActiveMissionController, $"{variant} must start through IMissionController.");
                Assert.AreEqual(variant, game.ActiveMissionController.Variant);
                Assert.IsNotEmpty(game.ObjectiveLabel, $"{variant} needs an immediate current objective.");
                StringAssert.DoesNotContain("%", game.ObjectiveLabel,
                    $"{variant} should reserve continuous values for visual meters.");
                Assert.IsNotEmpty(definition.IntroPrompt, $"{variant} needs an opening goal card.");
                Assert.IsNotEmpty(definition.RoleHint, $"{variant} needs an opening two-dog role read.");
                Assert.IsNotEmpty(definition.MechanicTag, $"{variant} needs a distinct mechanic identity.");
                Assert.IsNotEmpty(definition.SceneCue, $"{variant} needs a distinct world identity.");
                Assert.IsNotEmpty(MissionInstructionCatalog.DescriptionFor(variant));
                Assert.Greater(MissionInstructionCatalog.HowToPlayStepsFor(variant).Length, 0,
                    $"{variant} needs at least one concise sequence beat for discovery support.");

                int activePromotedCues = 0;
                foreach (var overlay in Object.FindObjectsByType<ArtSpriteOverlay>(FindObjectsSortMode.None))
                    if (overlay.gameObject.activeInHierarchy && overlay.HasRuntimeSprite && overlay.Visible)
                        activePromotedCues++;
                if (variant == GameManager.MissionVariant.OperationPeeBreak)
                {
                    foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                        if (renderer.gameObject.activeInHierarchy && renderer.sprite != null &&
                            renderer.gameObject.name.StartsWith("PeeBreakGenerated", System.StringComparison.Ordinal))
                            activePromotedCues++;
                }
                foreach (var attachment in Object.FindObjectsByType<MissionPropArtAttachment>(FindObjectsSortMode.None))
                {
                    if (!attachment.gameObject.activeInHierarchy || !attachment.HasRuntimeSprite) continue;
                    if (!attachment.DebugOnlyFallback) continue;
                    var fallback = MissionPropArt.FindFallbackRenderer(attachment.gameObject);
                    Assert.IsNotNull(fallback);
                    Assert.AreEqual(0f, fallback.color.a, 0.001f,
                        $"{variant}/{attachment.gameObject.name} leaked an explicit station pad into normal play.");
                }
                Assert.Greater(activePromotedCues, 0,
                    $"{variant} should start with at least one world-integrated promoted prop or cue.");
            }
        }

        [UnityTest]
        public IEnumerator PromotedStationPads_HideGeometryInPlay_AndReturnForF1Debug()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
            yield return null;

            new GameObject("Boot").AddComponent<ArenaBootstrap>();
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            game.StartMission(GameManager.MissionVariant.MarkTheYard);
            yield return null;

            var zone = GameObject.Find("TerritoryZone_0");
            Assert.IsNotNull(zone);
            var attachment = zone.GetComponent<MissionPropArtAttachment>();
            var fallback = MissionPropArt.FindFallbackRenderer(zone);
            Assert.IsNotNull(attachment);
            Assert.IsTrue(attachment.DebugOnlyFallback);
            Assert.IsTrue(attachment.HasRuntimeSprite,
                "The world-integrated zone art must remain visible when its circle is hidden.");
            Assert.AreEqual(0f, fallback.color.a, 0.001f,
                "Normal couch play should not show the oversized fallback station circle.");

            game.SetPlaytestOverlayVisible(true);
            yield return null;
            Assert.Greater(fallback.color.a, 0.01f,
                "F1 diagnostics should restore the underlying authored station geometry.");

            game.SetPlaytestOverlayVisible(false);
            yield return null;
            Assert.AreEqual(0f, fallback.color.a, 0.001f);
            Assert.IsTrue(attachment.HasRuntimeSprite);
        }

        [UnityTest]
        public IEnumerator ContinuousMissionState_UsesControllerOwnedMeters_NotRawPercentages()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
            yield return null;

            new GameObject("Boot").AddComponent<ArenaBootstrap>();
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            Assert.IsNotNull(game);
            var variants = new[]
            {
                GameManager.MissionVariant.BackyardRescue,
                GameManager.MissionVariant.GateCrash,
                GameManager.MissionVariant.TableStealth,
                GameManager.MissionVariant.WalkCampaign,
                GameManager.MissionVariant.CarRide,
                GameManager.MissionVariant.ThunderstormComfort,
                GameManager.MissionVariant.OperationPeeBreak,
            };

            foreach (var variant in variants)
            {
                game.StartMission(variant);
                yield return null;

                Assert.IsInstanceOf<IMissionPressureHud>(game.ActiveMissionController,
                    $"{variant} exposes continuous state and should own its visual HUD meter.");
                var meter = (IMissionPressureHud)game.ActiveMissionController;
                Assert.IsNotEmpty(meter.PressureLabel, $"{variant} needs a short couch-readable meter label.");
                Assert.That(meter.PressureNormalized, Is.InRange(0f, 1f),
                    $"{variant}'s HUD meter must stay normalized.");
                StringAssert.DoesNotContain("%", game.ObjectiveLabel,
                    $"{variant} should use the meter, not a changing percentage in objective copy.");

                switch (variant)
                {
                    case GameManager.MissionVariant.GateCrash:
                        game.ForceGateHold(true);
                        game.ForceGateCross(0.3f);
                        break;
                    case GameManager.MissionVariant.TableStealth:
                        game.ForceTableFlop(true);
                        game.ForceTableSneak(0.5f);
                        break;
                    case GameManager.MissionVariant.WalkCampaign:
                        game.ForceWalkCampaign(0.8f, doorStare: true, presentLeash: true);
                        break;
                    case GameManager.MissionVariant.CarRide:
                        var carRide = (CarRideMissionController)game.ActiveMissionController;
                        carRide.ForceBeginRoadEvent(CarRideMissionController.RoadEventKind.TurnRight);
                        carRide.ForceTurnSlide(0.25f);
                        break;
                    case GameManager.MissionVariant.ThunderstormComfort:
                        game.ForceThunderclap();
                        break;
                }

                Assert.That(meter.PressureNormalized, Is.InRange(0f, 1f),
                    $"{variant}'s meter must remain valid after live-state advancement.");
                StringAssert.DoesNotContain("%", game.ObjectiveLabel);
            }

            var phone = GameObject.Find("PeeBreakPhone");
            Assert.IsNotNull(phone);
            var phoneLabel = phone.GetComponentInChildren<TextMesh>(true);
            Assert.IsNotNull(phoneLabel);
            StringAssert.DoesNotContain("%", phoneLabel.text,
                "The in-world phone battery bar should carry the value without printing a percentage.");
        }

        [UnityTest]
        public IEnumerator TeamRumble_TargetsBothBoundPlayerPads_AndStopsWhenDisabled()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
            yield return null;

            var cheddarPad = InputSystem.AddDevice<Gamepad>();
            var cocoaPad = InputSystem.AddDevice<Gamepad>();
            new GameObject("Boot").AddComponent<ArenaBootstrap>();
            yield return null;
            yield return null;

            var game = Object.FindFirstObjectByType<GameManager>();
            DogController cheddar = null;
            GamepadPlayerInput cocoaInput = null;
            foreach (var id in Object.FindObjectsByType<DogIdentity>(FindObjectsSortMode.None))
            {
                if (id.Id == DogId.Cheddar) cheddar = id.GetComponent<DogController>();
                else if (id.Id == DogId.Cocoa) cocoaInput = id.GetComponent<GamepadPlayerInput>();
            }
            Assert.IsNotNull(game);
            Assert.IsNotNull(cheddar);
            Assert.IsNotNull(cocoaInput);

            game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            yield return null; // let each GamepadPlayerInput claim its stable player device
            Assert.AreEqual("PAD READY", game.PlayerControlSourceLabel(DogId.Cheddar));
            Assert.AreEqual("PAD READY", game.PlayerControlSourceLabel(DogId.Cocoa));

            cheddar.Bark();
            Assert.AreEqual(2, game.ActiveRumblePadCount,
                "Shared feedback should vibrate both distinct P1/P2 pads, not only Gamepad.current.");

            game.SetRumbleEnabled(false);
            Assert.AreEqual(0, game.ActiveRumblePadCount,
                "Turning rumble off from pause must stop every player pad immediately.");

            InputSystem.RemoveDevice(cheddarPad);
            yield return null;
            Assert.AreEqual("PAD LOST", game.PlayerControlSourceLabel(DogId.Cheddar),
                "A dropped P1 controller must be called out instead of silently looking like keyboard-only play.");
            Assert.AreEqual("PAD READY", game.PlayerControlSourceLabel(DogId.Cocoa),
                "P2 should stay visibly ready when only P1 disconnects.");

            InputSystem.AddDevice<Gamepad>();
            yield return null;
            Assert.AreEqual("PAD READY", game.PlayerControlSourceLabel(DogId.Cheddar),
                "An unclaimed replacement controller should restore the disconnected player's ready state.");
            Assert.AreEqual(cocoaPad.deviceId, cocoaInput.BoundGamepadDeviceId,
                "P1 replacement must not steal or relabel P2's still-connected controller.");
        }
    }
}
