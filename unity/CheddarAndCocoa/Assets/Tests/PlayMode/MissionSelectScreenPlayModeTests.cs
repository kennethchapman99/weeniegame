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
            Assert.AreEqual(GameManager.MissionVariant.OperationPeeBreak, screen.TileVariantAt(0));
            Assert.AreEqual(GameManager.MissionVariant.KitchenFoodFrenzy, screen.TileVariantAt(1));
            Assert.AreEqual(GameManager.MissionVariant.CarRide, screen.TileVariantAt(2));
            Assert.AreEqual(GameManager.MissionVariant.BabyBirdBedlam, screen.TileVariantAt(3));
            Assert.AreEqual(GameManager.MissionVariant.GateCrash, screen.TileVariantAt(4));
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
            Assert.That(screen.PageLabelText, Does.Contain("Page 1 of 3"));
            Assert.IsTrue(screen.TileIsSelectedAt(5), "The selected mission's tile should highlight.");
            Assert.IsFalse(screen.TileIsSelectedAt(0));

            // Blanket Catch sits on page 2, which is a full page again now that Tick Invasion's
            // append pushed the short last page out to page 3 (Skunk Blast Mayhem had briefly
            // landed the roster on an exact multiple of the tile-per-page count).
            game.SelectMission(GameManager.MissionVariant.BlanketCatch);
            yield return null;
            int blanketSlot = game.SelectedMissionIndex - GameManager.MissionSelectTilesPerPage;
            Assert.AreEqual(1, game.SelectedMissionPage);
            Assert.That(screen.PageLabelText, Does.Contain("Page 2 of 3"));
            Assert.AreEqual(GameManager.MissionSelectTilesPerPage,
                screen.ActiveTileCount, "Page 2 is a full page again now that the short page moved to page 3.");
            Assert.AreEqual(GameManager.MissionVariant.BlanketCatch, screen.TileVariantAt(blanketSlot));
            Assert.IsTrue(screen.TileIsSelectedAt(blanketSlot));

            // Tick Invasion alone occupies the new short third page.
            game.SelectMission(GameManager.MissionVariant.TickInvasion);
            yield return null;
            int tickSlot = game.SelectedMissionIndex - 2 * GameManager.MissionSelectTilesPerPage;
            Assert.AreEqual(2, game.SelectedMissionPage);
            Assert.That(screen.PageLabelText, Does.Contain("Page 3 of 3"));
            Assert.AreEqual(game.MissionSelectOptionCount - 2 * GameManager.MissionSelectTilesPerPage,
                screen.ActiveTileCount, "The new short last page should only show Tick Invasion.");
            Assert.AreEqual(GameManager.MissionVariant.TickInvasion, screen.TileVariantAt(tickSlot));
            Assert.IsTrue(screen.TileIsSelectedAt(tickSlot));

            game.SelectMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;
            int peeBreakSlot = game.SelectedMissionIndex;

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
            Assert.IsTrue(screen.DetailCoverFillsWidth,
                "V3.3: a contain-fit square cover in the wide detail window used to strand ~55% of the " +
                "width as bare DetailCoverBacking on both sides; the cover-fit crop must fill it edge to edge.");

            // A shorter plan stays fitting too (and gets the full-size font path).
            game.SelectMission(GameManager.MissionVariant.GateCrash);
            yield return null;
            Assert.IsTrue(screen.DetailTextFitsItsRects);
            Assert.IsTrue(screen.DetailCoverUsesTitleFreeCrop);
            Assert.IsTrue(screen.DetailCoverFillsWidth);
        }

        /// <summary>
        /// CF1.9 (couch-test freeform finding: title-card art "seems to crop down most of the
        /// image... detail is lost"). Pins the new, less-cropped FitDetailCover math: at the old
        /// V3.3 constants (300px letterbox, 1.36 zoom) the visible fraction of the square cover's
        /// area was ~18% (verified offline with a pixel-accurate PIL simulation of this exact
        /// cover-fit math); the new constants recover roughly double that. This would fail if the
        /// crop constants regressed back toward the old values.
        /// </summary>
        [UnityTest]
        public IEnumerator Screen_DetailCoverCropIsReducedFromV33Baseline()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            game.SelectMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;

            Sprite sprite = screen.DetailCoverSprite;
            Assert.IsNotNull(sprite);
            Vector2 displaySize = screen.DetailCoverDisplaySize;
            Vector2 area = screen.DetailCoverAreaSize;

            float visibleWidthFraction = area.x / displaySize.x;
            float visibleHeightFraction = area.y / displaySize.y;
            float visibleAreaFraction = visibleWidthFraction * visibleHeightFraction;

            Assert.Greater(visibleAreaFraction, 0.30f,
                "CF1.9: the detail cover should show more than 30% of the square art's area - the " +
                "old V3.3 constants (300px/1.36 zoom) only showed ~18%, matching the couch-test " +
                "complaint that most of the image was cropped away.");
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

        /// <summary>
        /// CF1.9 (couch-test freeform finding: "the Team plan should ideally show little visual
        /// clips of what things will actually look like on-screen"). Operation Pee Break is the
        /// first mission wired with team-plan chips - one real on-screen prop sprite per bullet,
        /// loaded from the same FinalGameplayArt resources gameplay itself uses.
        /// </summary>
        [UnityTest]
        public IEnumerator Screen_TeamPlanChips_PeeBreakShowsFourSignalChips()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            game.SelectMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;

            Assert.IsFalse(screen.DetailHowToVisible,
                "A mission with chip data replaces the single opaque text block with per-bullet rows.");

            string[] expectedSprites =
            {
                FinalGameplayArt.PeeBreakOpenDoor,
                FinalGameplayArt.PeeBreakLeash,
                FinalGameplayArt.PeeBreakPhoneCharger,
                FinalGameplayArt.BarkBurst,
            };

            Assert.AreEqual(4, screen.TeamPlanChipRowCapacity);
            for (int row = 0; row < expectedSprites.Length; row++)
            {
                Assert.IsTrue(screen.TeamPlanChipRowActiveAt(row), $"Row {row} should be active.");
                Assert.IsTrue(screen.TeamPlanChipIconEnabledAt(row), $"Row {row} should show its icon.");
                Sprite expected = FinalGameplayArt.Load(expectedSprites[row]);
                Assert.IsNotNull(expected, $"Expected sprite for row {row} should exist on disk.");
                Sprite actual = screen.TeamPlanChipSpriteAt(row);
                Assert.IsNotNull(actual, $"Row {row} should have loaded a sprite.");
                Assert.AreEqual(expected.name, actual.name,
                    $"Row {row} chip should show the real on-screen prop art gameplay itself uses.");

                Vector2 iconSize = screen.TeamPlanChipIconSizeAt(row);
                Assert.Greater(iconSize.x, 0f);
                Assert.Greater(iconSize.y, 0f);
            }

            Assert.That(screen.TeamPlanChipTextAt(0), Does.Contain("door stare"));
            Assert.That(screen.TeamPlanChipTextAt(1), Does.Contain("presents the leash"));
            Assert.That(screen.TeamPlanChipTextAt(2), Does.Contain("Swap roles"));
            Assert.That(screen.TeamPlanChipTextAt(3), Does.Contain("bark together"));
            for (int row = 0; row < expectedSprites.Length; row++)
                Assert.That(screen.TeamPlanChipTextAt(row), Does.StartWith("•  "));

            // Rows read top-to-bottom in bullet order: each row's anchored Y sits strictly below
            // (more negative, since Place() stores -y in a top-left-pivoted rect) the previous one.
            for (int row = 1; row < expectedSprites.Length; row++)
            {
                Assert.Less(screen.TeamPlanChipIconAnchoredPositionAt(row).y,
                    screen.TeamPlanChipIconAnchoredPositionAt(row - 1).y,
                    $"Row {row} should sit below row {row - 1}.");
            }

            // DetailHowToPlayText/DetailHowToFontFloor stay correct even while hidden - existing
            // tests read these directly and must keep working regardless of chip mode.
            Assert.AreEqual(MissionSelectScreen.BuildHowToPlayText(GameManager.MissionVariant.OperationPeeBreak),
                screen.DetailHowToPlayText);
            Assert.GreaterOrEqual(screen.DetailHowToFontFloor, 19f);
        }

        /// <summary>
        /// CF1.9 fallback contract CF2.8 depends on: a mission with no chip data yet renders its
        /// team plan exactly as it did before this task - plain text bullets, no icons, no layout
        /// change. CF2.8 rolled chip data out to 21 of the 22 non-Pee-Break missions (every one
        /// with 2+ genuinely load-bearing existing sprites for its previewed bullets); Backyard
        /// Rescue is the one documented, deliberate holdout - its previewed bullets only match a
        /// single distinct FinalGameplayArt sprite (the shared squirrel/predator/rope actors render
        /// with generated draft art, never a promoted override, in this controller), so it is now
        /// the roster's canonical "stays text-only" example instead of GateCrash/KitchenFoodFrenzy,
        /// which CF2.8 gave real chips per this same audit.
        /// </summary>
        [UnityTest]
        public IEnumerator Screen_TeamPlanChips_ChipLessMissionRendersTextOnly()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            game.SelectMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;

            Assert.IsTrue(screen.DetailHowToVisible,
                "A chip-less mission must keep showing the single text block, exactly as before CF1.9.");
            Assert.AreEqual(MissionSelectScreen.BuildHowToPlayText(GameManager.MissionVariant.BackyardRescue),
                screen.DetailHowToPlayText);
            for (int row = 0; row < screen.TeamPlanChipRowCapacity; row++)
            {
                Assert.IsFalse(screen.TeamPlanChipRowActiveAt(row),
                    $"Row {row} must stay hidden for a mission with no chip data.");
            }

            // Switching from a chip mission back to a chip-less one must fully clear chip state.
            game.SelectMission(GameManager.MissionVariant.OperationPeeBreak);
            yield return null;
            Assert.IsFalse(screen.DetailHowToVisible);
            game.SelectMission(GameManager.MissionVariant.BackyardRescue);
            yield return null;
            Assert.IsTrue(screen.DetailHowToVisible);
            for (int row = 0; row < screen.TeamPlanChipRowCapacity; row++)
                Assert.IsFalse(screen.TeamPlanChipRowActiveAt(row));
        }

        /// <summary>
        /// CF2.8 roster pass: every mission's team plan should either use real chips (an icon per
        /// bullet loaded from that mission's own on-screen art, or - for a bullet with no matching
        /// sprite - a graceful text-only row within the same chip block) or, for the one documented
        /// holdout (Backyard Rescue), the original plain-text block. This loops the full roster
        /// (not a hand-picked sample) so a future mission that regresses into a broken icon
        /// reference or an inconsistent row count fails here instead of at a couch test.
        /// </summary>
        [UnityTest]
        public IEnumerator Screen_TeamPlanChips_RosterWide_EveryMissionResolvesOrGracefullyFallsBack()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            int chippedCount = 0;
            int textOnlyCount = 0;

            foreach (GameManager.MissionVariant variant in System.Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                game.SelectMission(variant);
                yield return null;

                int previewedBullets = MissionSelectScreen.BuildHowToPlayText(variant).Split('\n').Length;

                if (!screen.DetailHowToVisible)
                {
                    chippedCount++;
                    for (int row = 0; row < previewedBullets; row++)
                    {
                        Assert.IsTrue(screen.TeamPlanChipRowActiveAt(row),
                            $"{variant}: row {row} of {previewedBullets} previewed bullets should be an active chip row.");
                        Assert.That(screen.TeamPlanChipTextAt(row), Does.StartWith("•  "),
                            $"{variant}: chip row {row} should still carry the bullet's own text.");
                        if (screen.TeamPlanChipIconEnabledAt(row))
                        {
                            Sprite icon = screen.TeamPlanChipSpriteAt(row);
                            Assert.IsNotNull(icon, $"{variant}: row {row} enabled its icon but has no sprite.");
                            Assert.AreNotSame(SpriteShapeCache.WhiteSquare, icon,
                                $"{variant}: row {row}'s chip must not fall back to the runtime white square.");
                        }
                    }
                    for (int row = previewedBullets; row < screen.TeamPlanChipRowCapacity; row++)
                    {
                        Assert.IsFalse(screen.TeamPlanChipRowActiveAt(row),
                            $"{variant}: row {row} is beyond this mission's {previewedBullets} previewed bullets and must stay inactive.");
                    }
                }
                else
                {
                    textOnlyCount++;
                    for (int row = 0; row < screen.TeamPlanChipRowCapacity; row++)
                    {
                        Assert.IsFalse(screen.TeamPlanChipRowActiveAt(row),
                            $"{variant}: a text-only mission must keep every chip row inactive.");
                    }
                }
            }

            // CF2.8's audit result: 22 missions with chip data (Pee Break from CF1.9 + 21 from this
            // roster pass) and exactly one documented text-only holdout (Backyard Rescue). Skunk
            // Blast Mayhem (2026-07-22) added a 23rd chipped mission, reusing Sock Panic's authored
            // laundry-basket art for its own basket/pile rows. Tick Invasion (2026-07-22) has no
            // bespoke groom/pool/erratic/Super-Tick art to reuse yet, so it stays text-only alongside
            // Backyard Rescue rather than guessing at a mismatched icon. Burr Maze (2026-07-22) added
            // a 24th chipped mission, reusing the existing Bush and Grass backyard props for its
            // hiding-spot and bramble-patch bullets (its cat patrol/cone and checkpoint-reset bullets
            // stay text-only rows within that same chip block, same as Coyotes Fence/Scent Search's
            // mixed null entries).
            Assert.AreEqual(24, chippedCount, "Expected 24 missions to render team-plan chips.");
            Assert.AreEqual(2, textOnlyCount, "Expected Backyard Rescue and Tick Invasion to stay text-only.");
        }

        /// <summary>
        /// CF2.8 spot check #1: a mission where every previewed bullet has a matching sprite (all 4
        /// rows active with icons) - Sock Panic's basket/sock state art lines up cleanly with all
        /// four of its bullets.
        /// </summary>
        [UnityTest]
        public IEnumerator Screen_TeamPlanChips_SockPanicShowsFourFullyMatchedChips()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            game.SelectMission(GameManager.MissionVariant.SockPanic);
            yield return null;

            Assert.IsFalse(screen.DetailHowToVisible);
            string[] expectedSprites =
            {
                FinalGameplayArt.SockPanicBasketClosed,
                FinalGameplayArt.SockPanicSockExposed,
                FinalGameplayArt.SockPanicBasketFumble,
                FinalGameplayArt.SockPanicSockSaved,
            };
            for (int row = 0; row < expectedSprites.Length; row++)
            {
                Assert.IsTrue(screen.TeamPlanChipRowActiveAt(row));
                Assert.IsTrue(screen.TeamPlanChipIconEnabledAt(row), $"Row {row} should show an icon.");
                Sprite expected = FinalGameplayArt.Load(expectedSprites[row]);
                Assert.IsNotNull(expected, $"Expected sprite for row {row} should exist on disk.");
                Assert.AreEqual(expected.name, screen.TeamPlanChipSpriteAt(row).name,
                    $"Row {row} should show Sock Panic's own on-screen state art.");
            }
        }

        /// <summary>
        /// CF2.8 spot check #2: a mission whose chip array has a deliberate null in the MIDDLE (not
        /// just trailing) - Coyotes Fence's third bullet ("neither dog can perform both jobs...")
        /// has no distinct sprite, so that row must render text-only while rows 0/1/3 around it
        /// still show icons, proving index alignment survives an interior gap.
        /// </summary>
        [UnityTest]
        public IEnumerator Screen_TeamPlanChips_CoyotesFenceHandlesAnInteriorTextOnlyRow()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            game.SelectMission(GameManager.MissionVariant.CoyotesFence);
            yield return null;

            Assert.IsFalse(screen.DetailHowToVisible);
            Assert.IsTrue(screen.TeamPlanChipRowActiveAt(0));
            Assert.IsTrue(screen.TeamPlanChipIconEnabledAt(0));
            Assert.AreEqual(FinalGameplayArt.Load(FinalGameplayArt.CoyotesFenceGapPinned).name,
                screen.TeamPlanChipSpriteAt(0).name);

            Assert.IsTrue(screen.TeamPlanChipRowActiveAt(2), "Row 2 stays an active row (text-only).");
            Assert.IsFalse(screen.TeamPlanChipIconEnabledAt(2), "Row 2 has no matching sprite - icon must stay off.");
            Assert.That(screen.TeamPlanChipTextAt(2), Does.StartWith("•  "),
                "Row 2 must still show its bullet text even without an icon.");

            Assert.IsTrue(screen.TeamPlanChipRowActiveAt(3));
            Assert.IsTrue(screen.TeamPlanChipIconEnabledAt(3));
            Assert.AreEqual(FinalGameplayArt.Load(FinalGameplayArt.CoyotesFenceFakeSnack).name,
                screen.TeamPlanChipSpriteAt(3).name);
        }

        /// <summary>
        /// CF2.8 spot check #3: a mission with a SHORTER chip array than its previewed bullet count
        /// (2 sprites for 4 bullets) - Snack Heist only has 2 genuinely load-bearing sprites, so
        /// rows 2/3 must stay active-but-icon-less (graceful trailing fallback) rather than the
        /// whole mission losing its chips.
        /// </summary>
        [UnityTest]
        public IEnumerator Screen_TeamPlanChips_SnackHeistHandlesAShorterChipArray()
        {
            yield return LoadArena();

            var game = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<MissionSelectScreen>();

            game.SelectMission(GameManager.MissionVariant.SnackHeist);
            yield return null;

            Assert.IsFalse(screen.DetailHowToVisible);
            Assert.IsTrue(screen.TeamPlanChipIconEnabledAt(0));
            Assert.AreEqual(FinalGameplayArt.Load(FinalGameplayArt.SnackHeistPlateTargeted).name,
                screen.TeamPlanChipSpriteAt(0).name);
            Assert.IsTrue(screen.TeamPlanChipIconEnabledAt(1));
            Assert.AreEqual(FinalGameplayArt.Load(FinalGameplayArt.SnackHeistGuardLane).name,
                screen.TeamPlanChipSpriteAt(1).name);

            Assert.IsTrue(screen.TeamPlanChipRowActiveAt(2), "Row 2 stays an active row (text-only).");
            Assert.IsFalse(screen.TeamPlanChipIconEnabledAt(2));
            Assert.IsTrue(screen.TeamPlanChipRowActiveAt(3), "Row 3 stays an active row (text-only).");
            Assert.IsFalse(screen.TeamPlanChipIconEnabledAt(3));
        }
    }
}
