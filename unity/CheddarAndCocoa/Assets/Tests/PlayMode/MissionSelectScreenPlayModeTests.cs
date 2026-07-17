using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Tests
{
    /// <summary>
    /// The mission picker is a UGUI Canvas + TextMeshPro paged picture-tile grid with a detail
    /// panel, replacing the IMGUI rows whose text scaled as a blurry bitmap. These tests pin the
    /// view contract: crisp scaling setup, readable production-facing copy, one tile per mission
    /// on the visible page, paging that follows GameManager selection, a concise detail preview,
    /// and buttons that delegate to the same GameManager flow methods keyboard/gamepad input uses.
    /// </summary>
    public sealed class MissionSelectScreenPlayModeTests
    {
        private static IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync("ArenaScene", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Screen_BuildsUguiCanvasWithTmpTilesForCurrentPage()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();
            Assert.IsNotNull(game);
            Assert.IsNotNull(screen, "ArenaScene should install the UGUI mission select screen.");
            Assert.IsTrue(game.MissionSelectVisible);
            Assert.IsTrue(screen.Visible, "The picker should be visible on the mission select flow state.");

            Assert.IsNotNull(screen.Canvas, "Mission select should render on a UGUI Canvas, not IMGUI.");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, screen.Canvas.renderMode);
            var scaler = screen.Canvas.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode,
                "Text must scale with the screen (crisp vector TMP), not draw at fixed pixels.");
            Assert.AreEqual(new Vector2(MissionSelectScreen.ReferenceWidth, MissionSelectScreen.ReferenceHeight),
                scaler.referenceResolution);
            Assert.Greater(screen.GetComponentsInChildren<TMP_Text>(true).Length, 20,
                "Mission select copy should render through TextMeshPro.");
            Assert.AreEqual("Cheddar + Cocoa Adventures", screen.TitleText);
            Assert.That(screen.RecommendedHintText, Does.StartWith("START HERE:"));
            Assert.That(screen.RecommendedHintText, Does.Contain(game.CouchTestFocusName));
            Assert.That(screen.ControlsHintText, Does.Contain("Y selects Recommended"));
            string playerFacingHeader =
                $"{screen.TitleText} {screen.RecommendedHintText} {screen.ControlsHintText}".ToLowerInvariant();
            Assert.That(playerFacingHeader, Does.Not.Contain("couch test"));
            Assert.That(playerFacingHeader, Does.Not.Contain("family shortcut"));
            Assert.That(playerFacingHeader, Does.Not.Contain("f5"));

            Assert.AreEqual(GameManager.MissionSelectTilesPerPage, screen.TileCapacity);
            Assert.AreEqual(GameManager.MissionSelectTilesPerPage, screen.ActiveTileCount,
                "The full first page should fill every tile slot.");
            for (int slot = 0; slot < screen.ActiveTileCount; slot++)
            {
                Assert.IsNotNull(screen.TileCoverSpriteAt(slot),
                    $"Tile {slot} should show the mission's authored cover art.");
                Assert.AreNotSame(SpriteShapeCache.WhiteSquare, screen.TileCoverSpriteAt(slot),
                    $"Tile {slot} must not fall back to the runtime white square.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(screen.TileNameAt(slot)),
                    $"Tile {slot} should carry the mission name.");
                Assert.AreEqual(game.MissionVariantAt(slot), screen.TileVariantAt(slot),
                    "Tiles must follow GameManager's mission order.");
                Assert.IsTrue(screen.TileCoverUsesTitleFreeCropAt(slot),
                    $"Tile {slot} should crop its baked-in title ribbon so the TMP title is not duplicated.");
            }
        }

        [UnityTest]
        public IEnumerator Screen_PagesAndSelectionFollowGameManager()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            game.SelectMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            Assert.That(screen.PageLabelText, Does.Contain("Page 1 of 2"));
            Assert.IsTrue(screen.TileIsSelectedAt(0), "The selected mission's tile should highlight.");
            Assert.IsFalse(screen.TileIsSelectedAt(1));

            // Operation Pee Break sits on page 1, in the short page of remainders.
            game.SelectMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;
            int peeBreakSlot = game.SelectedMissionIndex - GameManager.MissionSelectTilesPerPage;
            Assert.AreEqual(1, game.SelectedMissionPage);
            Assert.That(screen.PageLabelText, Does.Contain("Page 2 of 2"));
            Assert.AreEqual(game.MissionSelectOptionCount - GameManager.MissionSelectTilesPerPage,
                screen.ActiveTileCount, "The short last page should only show the remaining missions.");
            Assert.AreEqual(GameManager.MissionVariant.OperationPeeBreak, screen.TileVariantAt(peeBreakSlot));
            Assert.IsTrue(screen.TileIsSelectedAt(peeBreakSlot));

            Assert.That(screen.DetailNameText, Does.Contain(game.SelectedMissionName));
            Assert.AreEqual(MissionInstructionCatalog.DescriptionFor(GameManager.MissionVariant.OperationPeeBreak),
                screen.DetailDescriptionText, "The detail panel should show the level-select description.");
            Assert.AreEqual(MissionSelectScreen.BuildHowToPlayText(GameManager.MissionVariant.OperationPeeBreak),
                screen.DetailHowToPlayText, "The detail panel should show the how-to-play instructions.");
            Assert.That(screen.DetailHowToPlayText, Does.StartWith("•  "),
                "Couch test #3: how-to-play renders as bullet steps, not a wall of text.");
            Assert.That(screen.DetailHowToPlayText, Does.Contain("\n•  "),
                "Every step gets its own bullet line.");
            Assert.AreEqual(4, screen.DetailHowToPlayText.Split('\n').Length,
                "The picker should preview the four teamwork beats instead of dumping the full tutorial.");
            Assert.That(screen.DetailHowToPlayText, Does.Contain("Swap roles"));
            Assert.That(screen.DetailHowToPlayText, Does.Contain("bark together"));
            Assert.AreEqual(ArenaHud.MissionBadgeCodeFor(GameManager.MissionVariant.OperationPeeBreak),
                screen.DetailBadgeCodeText, "The detail panel should show the mission badge.");
            Assert.IsNotNull(screen.DetailCoverSprite, "The detail panel should show the mission cover art.");
            Assert.That(screen.DetailChallengeText, Does.Contain("Pawfect"));
            Assert.GreaterOrEqual(screen.DetailDescriptionFontFloor, 20f,
                "Description text must remain readable from a couch instead of shrinking to 13pt.");
            Assert.GreaterOrEqual(screen.DetailHowToFontFloor, 19f,
                "Team-plan text must remain readable from a couch instead of shrinking to 13pt.");
            Assert.That(screen.TileStatusAt(peeBreakSlot), Does.StartWith("RECOMMENDED"),
                "The deep slice should be visibly recommended without developer-facing couch-test language.");
            Assert.IsFalse(screen.RecommendedButton.gameObject.activeSelf,
                "Selecting the recommended adventure should collapse the two competing actions into one start button.");
            Assert.Greater(screen.StartButton.GetComponent<RectTransform>().rect.width, 800f,
                "The recommended adventure should get one unambiguous full-width start action.");
        }

        [Test]
        public void MissionPreview_EveryAdventureUsesAtMostFourTeamSteps()
        {
            foreach (GameManager.MissionVariant variant in System.Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                string preview = MissionSelectScreen.BuildHowToPlayText(variant);
                Assert.IsFalse(string.IsNullOrWhiteSpace(preview), $"{variant} needs a player-facing team plan.");
                Assert.LessOrEqual(preview.Split('\n').Length, 4,
                    $"{variant} should use progressive teaching instead of a pre-mission instruction dump.");
            }
        }

        [UnityTest]
        public IEnumerator Screen_DetailPanel_FitsConcisePlanAndCropsBakedCoverTitle()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            // Backyard Rescue used to dump seven steps over the challenge/buttons, while its
            // square cover presented a second baked title alongside the TMP mission name.
            game.SelectMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            Assert.IsTrue(screen.DetailTextFitsItsRects,
                "The longest how-to list must auto-size into its rect, never over the rows below.");
            Assert.IsTrue(screen.DetailCoverUsesTitleFreeCrop,
                "The detail art should crop its baked title ribbon because TMP owns the readable mission title.");

            // A shorter plan stays fitting too (and gets the full-size font path).
            game.SelectMission(GameManager.MissionVariant.GateCrash);
            yield return null;
            Assert.IsTrue(screen.DetailTextFitsItsRects);
            Assert.IsTrue(screen.DetailCoverUsesTitleFreeCrop);
        }

        [UnityTest]
        public IEnumerator Screen_HidesDuringMissionsAndRefreshesOnReturn()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();
            Assert.IsTrue(screen.Visible);

            game.StartMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsFalse(screen.Visible, "The picker canvas must leave the screen during play.");

            game.ForceGameOver();
            game.ReturnToMissionSelect();
            yield return null;
            yield return null;
            Assert.IsTrue(screen.Visible, "The picker should come back after a mission ends.");
            Assert.That(screen.DetailNameText, Does.Contain(game.SelectedMissionName));
        }

        [UnityTest]
        public IEnumerator Screen_ButtonsDelegateToGameFlow()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            game.SelectMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            Assert.IsTrue(screen.RecommendedButton.gameObject.activeSelf,
                "A one-action recommended front door should remain available while browsing the library.");
            screen.RecommendedButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow,
                "Play Recommended should launch the recommended adventure directly.");
            Assert.AreEqual(GameManager.MissionVariant.OperationPeeBreak, game.ActiveMissionVariant);

            game.ForceGameOver();
            game.ReturnToMissionSelect();
            game.SelectMission(GameManager.MissionVariant.SnackHeist);
            yield return null;
            screen.StartButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow,
                "The start button must start the selected mission.");
            Assert.AreEqual(GameManager.MissionVariant.SnackHeist, game.ActiveMissionVariant);
        }
    }
}
