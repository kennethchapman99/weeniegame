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
    /// view contract: crisp scaling setup, one tile per mission on the visible page, paging that
    /// follows GameManager selection, a detail panel fed by the instruction catalog, and buttons
    /// that delegate to the same GameManager flow methods keyboard/gamepad input uses.
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

            Assert.AreEqual(GameManager.MissionSelectTilesPerPage, screen.TileCapacity);
            Assert.AreEqual(GameManager.MissionSelectTilesPerPage, screen.ActiveTileCount,
                "Page 0 of 22 missions should fill every tile slot.");
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
            Assert.That(screen.PageLabelText, Does.StartWith("Page 1/2"));
            Assert.IsTrue(screen.TileIsSelectedAt(0), "The selected mission's tile should highlight.");
            Assert.IsFalse(screen.TileIsSelectedAt(1));

            // Operation Pee Break is the last mission (index 21) -> page 1, slot 9 of a short page.
            game.SelectMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;
            Assert.AreEqual(1, game.SelectedMissionPage);
            Assert.That(screen.PageLabelText, Does.StartWith("Page 2/2"));
            Assert.AreEqual(game.MissionSelectOptionCount - GameManager.MissionSelectTilesPerPage,
                screen.ActiveTileCount, "The short last page should only show the remaining missions.");
            Assert.AreEqual(GameManager.MissionVariant.OperationPeeBreak,
                screen.TileVariantAt(21 - GameManager.MissionSelectTilesPerPage));
            Assert.IsTrue(screen.TileIsSelectedAt(21 - GameManager.MissionSelectTilesPerPage));

            Assert.That(screen.DetailNameText, Does.Contain(game.SelectedMissionName));
            Assert.AreEqual(MissionInstructionCatalog.DescriptionFor(GameManager.MissionVariant.OperationPeeBreak),
                screen.DetailDescriptionText, "The detail panel should show the level-select description.");
            Assert.AreEqual(MissionInstructionCatalog.HowToPlayFor(GameManager.MissionVariant.OperationPeeBreak),
                screen.DetailHowToPlayText, "The detail panel should show the how-to-play instructions.");
            Assert.AreEqual(ArenaHud.MissionBadgeCodeFor(GameManager.MissionVariant.OperationPeeBreak),
                screen.DetailBadgeCodeText, "The detail panel should show the mission badge.");
            Assert.IsNotNull(screen.DetailCoverSprite, "The detail panel should show the mission cover art.");
            Assert.That(screen.DetailChallengeText, Does.Contain("Pawfect"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(screen.DetailReadinessText),
                "The detail panel should keep the readability/readiness gate line.");
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
            Assert.IsTrue(screen.CouchFocusButton.gameObject.activeSelf,
                "The couch-test shortcut button should offer to jump to the focus mission.");
            screen.CouchFocusButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameManager.MissionVariant.OperationPeeBreak, game.SelectedMissionVariant,
                "The couch-focus button must reuse the same GameManager path as the F5/P/Y shortcut.");
            Assert.IsFalse(screen.CouchFocusButton.gameObject.activeSelf,
                "The shortcut button should hide once the focus mission is already selected.");

            screen.StartButton.onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameManager.FlowState.Playing, game.CurrentFlow,
                "The start button must start the selected mission.");
            Assert.AreEqual(GameManager.MissionVariant.OperationPeeBreak, game.ActiveMissionVariant);
        }
    }
}
