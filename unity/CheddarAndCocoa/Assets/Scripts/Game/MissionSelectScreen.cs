using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// UGUI + TextMeshPro mission picker: a paged grid of picture tiles on the left and a detail
    /// panel (cover, badge, description, how-to-play, and replay challenge) for the focused tile on
    /// the right. Replaces the IMGUI mission-select rows so text stays crisp at any resolution
    /// instead of scaling a 1080p bitmap layout. This is a pure view: selection, paging, and the
    /// directional/controller shortcuts all stay in <see cref="GameManager.TickFlowInput"/>, and
    /// the grid shape is <see cref="GameManager.MissionSelectGridColumns"/> x
    /// <see cref="GameManager.MissionSelectGridRowsPerPage"/> per page so directional navigation and
    /// the visible tiles can never disagree. Buttons call the same GameManager methods the IMGUI
    /// buttons used, so tests assert one flow.
    /// </summary>
    public sealed class MissionSelectScreen : MonoBehaviour
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        // Couch test #4: the grid's old 1046px share squeezed the detail pane until its text
        // overlapped itself and the cover cropped — the split is now 946/896 and the detail text
        // auto-sizes into its rects instead of overflowing them.
        private const float ContentTop = 170f;
        private const float ContentHeight = 892f;
        private const float GridX = 26f;
        private const float GridWidth = 946f;
        private const float TileGap = 14f;
        private const float PageLabelHeight = 30f;
        private const float DetailX = 998f;
        private const float DetailWidth = 896f;
        // CF1.9 (2026-07-20, freeform finding: title-card art "crops down most of the image"):
        // was 300f/1.36f. A PIL offline simulation of this exact cover-fit math (see the task's
        // commit message for the measured numbers) showed the old 300/1.36 combination only
        // showed ~18% of the square cover art's area - the extra 36% overzoom beyond the minimum
        // cover-fit was pure unnecessary crop, and 300px left little vertical room besides. Taller
        // height (steals 40px from the text pane below, verified the "how to play" text still
        // fits at its font floor) plus a near-1.0 zoom (small 5% margin so no backing strip can
        // show through a rounding edge) together roughly double the visible art (~34%) with zero
        // change to DetailCoverFillsWidth/DetailCoverUsesTitleFreeCrop, both of which have large
        // margin to spare at these values.
        private const float DetailCoverHeight = 340f;
        private const float TileArtworkZoom = 1.35f;
        private const float TileArtworkVerticalShift = -0.12f;
        private const float DetailArtworkZoom = 1.05f;
        private const float DetailArtworkVerticalShift = -0.10f;
        private const float DetailTextMinimumSize = 19f;

        // CF1.9: small icon chips shown next to team-plan bullets that have chip art
        // (MissionInstructionCatalog-adjacent data, see TeamPlanChipsFor below). Sized to sit
        // comfortably inside one bullet row of the shared howTo area.
        private const float ChipIconSize = 30f;
        private const float ChipIconGap = 10f;
        private const int MaxTeamPlanChipRows = 4;

        private sealed class TileSlot
        {
            public GameObject Root;
            public Image Frame;
            public Image SelectionGlow;
            public Image Cover;
            public Vector2 CoverArea;
            public Image AccentStrip;
            public Image BadgeBack;
            public TextMeshProUGUI BadgeCode;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Status;
            public GameManager.MissionVariant Variant;
        }

        private sealed class TeamPlanChipRow
        {
            public GameObject Root;
            public Image Icon;
            public TextMeshProUGUI Text;
        }

        private GameManager _game;
        private GameObject _canvasRoot;
        private Canvas _canvas;
        private readonly List<TileSlot> _tiles = new List<TileSlot>();
        private readonly Dictionary<GameManager.MissionVariant, Sprite> _coverCache =
            new Dictionary<GameManager.MissionVariant, Sprite>();
        private int _syncedSelection = -1;

        private TextMeshProUGUI _title;
        private TextMeshProUGUI _recommendedHint;
        private TextMeshProUGUI _controlsHint;
        private TextMeshProUGUI _sessionStats;
        private TextMeshProUGUI _pageLabel;
        private Image _detailCover;
        private Vector2 _detailCoverArea;
        private Image _detailAccent;
        private Image _detailBadgeBack;
        private TextMeshProUGUI _detailBadgeCode;
        private TextMeshProUGUI _detailName;
        private TextMeshProUGUI _detailMeta;
        private TextMeshProUGUI _detailDescription;
        private TextMeshProUGUI _detailHowTo;
        private TextMeshProUGUI _detailChallenge;
        private Button _startButton;
        private TextMeshProUGUI _startLabel;
        private Button _recommendedButton;
        private readonly List<TeamPlanChipRow> _chipRows = new List<TeamPlanChipRow>();
        private float _howToAreaX, _howToAreaY, _howToAreaWidth, _howToAreaHeight;

        public void Init(GameManager game) => _game = game;

        public bool Visible => _canvasRoot != null && _canvasRoot.activeSelf;
        public Canvas Canvas => _canvas;
        public int TileCapacity => GameManager.MissionSelectTilesPerPage;
        public Button StartButton => _startButton;
        public Button RecommendedButton => _recommendedButton;
        public string TitleText => _title != null ? _title.text : string.Empty;
        public string RecommendedHintText => _recommendedHint != null ? _recommendedHint.text : string.Empty;
        public string ControlsHintText => _controlsHint != null ? _controlsHint.text : string.Empty;
        public string PageLabelText => _pageLabel != null ? _pageLabel.text : string.Empty;
        public string DetailNameText => _detailName != null ? _detailName.text : string.Empty;
        public string DetailDescriptionText => _detailDescription != null ? _detailDescription.text : string.Empty;
        public string DetailHowToPlayText => _detailHowTo != null ? _detailHowTo.text : string.Empty;
        public string DetailBadgeCodeText => _detailBadgeCode != null ? _detailBadgeCode.text : string.Empty;
        public string DetailChallengeText => _detailChallenge != null ? _detailChallenge.text : string.Empty;
        public float DetailDescriptionFontFloor => _detailDescription != null ? _detailDescription.fontSizeMin : 0f;
        public float DetailHowToFontFloor => _detailHowTo != null ? _detailHowTo.fontSizeMin : 0f;
        public Sprite DetailCoverSprite => _detailCover != null ? _detailCover.sprite : null;

        /// <summary>CF1.9: the fixed 896-wide letterbox mask size the cover art is cropped into.</summary>
        public Vector2 DetailCoverAreaSize => _detailCoverArea;

        /// <summary>CF1.9: the cover-fit-scaled display size of the current cover sprite (pre-crop).</summary>
        public Vector2 DetailCoverDisplaySize =>
            _detailCover != null ? _detailCover.rectTransform.sizeDelta : Vector2.zero;

        /// <summary>
        /// CF1.9: true whenever the picker renders the single opaque team-plan text block (every
        /// mission without chip data - the unchanged legacy path). False when per-bullet chip rows
        /// are showing instead (their text is still mirrored into <see cref="DetailHowToPlayText"/>
        /// so existing content assertions keep working either way).
        /// </summary>
        public bool DetailHowToVisible => _detailHowTo != null && _detailHowTo.gameObject.activeSelf;

        /// <summary>Fixed pool size regardless of how many rows a given mission actually uses.</summary>
        public int TeamPlanChipRowCapacity => _chipRows.Count;
        public bool TeamPlanChipRowActiveAt(int row) => _chipRows[row].Root.activeSelf;
        public bool TeamPlanChipIconEnabledAt(int row) => _chipRows[row].Icon.enabled;
        public Sprite TeamPlanChipSpriteAt(int row) => _chipRows[row].Icon.sprite;
        public string TeamPlanChipTextAt(int row) => _chipRows[row].Text.text;
        public Vector2 TeamPlanChipIconSizeAt(int row) => _chipRows[row].Icon.rectTransform.sizeDelta;
        public Vector2 TeamPlanChipIconAnchoredPositionAt(int row) =>
            _chipRows[row].Icon.rectTransform.anchoredPosition;
        public Vector2 TeamPlanChipTextAnchoredPositionAt(int row) =>
            _chipRows[row].Text.rectTransform.anchoredPosition;

        /// <summary>
        /// Couch test #4 contract: the description and how-to blocks must render inside their own
        /// rects (auto-sized), never spilling over the challenge line and buttons.
        /// </summary>
        public bool DetailTextFitsItsRects
        {
            get
            {
                if (_detailDescription == null || _detailHowTo == null) return false;
                return RenderedTextFits(_detailDescription) && RenderedTextFits(_detailHowTo);
            }
        }

        private static bool RenderedTextFits(TextMeshProUGUI text)
        {
            // textBounds measures the actually rendered glyphs, i.e. after auto-size shrinking.
            text.ForceMeshUpdate();
            return text.textBounds.size.y <= text.rectTransform.sizeDelta.y + 0.5f;
        }

        /// <summary>
        /// Mission-tile art currently contains a baked title ribbon. The picker presents its own
        /// crisp TMP title, so both grid and detail artwork deliberately crop that lower ribbon.
        /// </summary>
        public bool DetailCoverUsesTitleFreeCrop
        {
            get
            {
                if (_detailCover == null || _detailCover.sprite == null) return false;
                Vector2 size = _detailCover.rectTransform.sizeDelta;
                return size.y > _detailCoverArea.y * 1.2f &&
                       _detailCover.rectTransform.anchoredPosition.y < -_detailCoverArea.y * 0.08f;
            }
        }

        /// <summary>
        /// V3.3: the detail pane is a wide 896x300 window holding a 1:1 square cover. A cover-fit
        /// scale must fill the full window width so no DetailCoverBacking void shows on either side.
        /// </summary>
        public bool DetailCoverFillsWidth =>
            _detailCover != null && _detailCover.sprite != null &&
            _detailCover.rectTransform.sizeDelta.x >= _detailCoverArea.x - 0.5f;

        public int ActiveTileCount
        {
            get
            {
                int count = 0;
                foreach (TileSlot tile in _tiles)
                    if (tile.Root.activeSelf) count++;
                return count;
            }
        }

        public Sprite TileCoverSpriteAt(int slot) => _tiles[slot].Cover.sprite;
        public string TileNameAt(int slot) => _tiles[slot].Name.text;
        public string TileStatusAt(int slot) => _tiles[slot].Status.text;
        public bool TileIsSelectedAt(int slot) => _tiles[slot].SelectionGlow.enabled;
        public GameManager.MissionVariant TileVariantAt(int slot) => _tiles[slot].Variant;
        public bool TileCoverUsesTitleFreeCropAt(int slot)
        {
            TileSlot tile = _tiles[slot];
            return tile.Cover.sprite != null && tile.Cover.rectTransform.sizeDelta.y > tile.CoverArea.y * 1.2f &&
                   tile.Cover.rectTransform.anchoredPosition.y < -tile.CoverArea.y * 0.08f;
        }

        private void LateUpdate()
        {
            if (_game == null) return;

            bool visible = _game.MissionSelectVisible;
            if (_canvasRoot == null)
            {
                if (!visible) return;
                Build();
            }

            if (_canvasRoot.activeSelf != visible)
            {
                _canvasRoot.SetActive(visible);
                // Session stats and NEW/RETRY statuses change between missions, so force a full
                // refresh every time the screen comes back.
                _syncedSelection = -1;
            }

            if (!visible || _syncedSelection == _game.SelectedMissionIndex) return;
            _syncedSelection = _game.SelectedMissionIndex;
            Sync();
        }

        // --- Build (runtime-generated like the rest of the arena; the scene stays asset-free) ---

        private void Build()
        {
            _canvasRoot = new GameObject("MissionSelectCanvas", typeof(RectTransform));
            _canvasRoot.transform.SetParent(transform, false);
            _canvas = _canvasRoot.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 40;
            var scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _canvasRoot.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            Image backdrop = NewImage("Backdrop", _canvasRoot.transform, new Color(0.02f, 0.05f, 0.06f, 0.96f));
            Stretch(backdrop.rectTransform);

            BuildHeader();
            BuildGrid();
            BuildDetailPanel();
        }

        private void BuildHeader()
        {
            _title = NewText("Title", _canvasRoot.transform, 46f,
                new Color(1f, 0.95f, 0.4f), TextAlignmentOptions.Center, FontStyles.Bold);
            Place(_title.rectTransform, 0f, 10f, ReferenceWidth, 56f);
            _title.text = "Cheddar + Cocoa Adventures";

            _recommendedHint = NewText("RecommendedHint", _canvasRoot.transform, 24f,
                new Color(1f, 0.87f, 0.34f), TextAlignmentOptions.Center, FontStyles.Bold);
            Place(_recommendedHint.rectTransform, 32f, 68f, ReferenceWidth - 64f, 30f);
            _recommendedHint.text = $"START HERE: {_game.CouchTestFocusName} — a four-part teamwork story";

            _controlsHint = NewText("ControlsHint", _canvasRoot.transform, 19f,
                new Color(0.9f, 0.95f, 1f), TextAlignmentOptions.Center);
            Place(_controlsHint.rectTransform, 32f, 102f, ReferenceWidth - 64f, 26f);
            _controlsHint.text = "D-pad / arrow keys choose  •  A / Enter starts  •  Y selects Recommended";

            _sessionStats = NewText("SessionStats", _canvasRoot.transform, 18f,
                new Color(0.72f, 0.82f, 0.88f), TextAlignmentOptions.Center);
            Place(_sessionStats.rectTransform, 32f, 134f, ReferenceWidth - 64f, 24f);
        }

        private void BuildGrid()
        {
            Image gridPanel = NewImage("GridPanel", _canvasRoot.transform, new Color(0.02f, 0.04f, 0.05f, 0.42f));
            Place(gridPanel.rectTransform, GridX, ContentTop, GridWidth, ContentHeight);

            float tilesHeight = ContentHeight - PageLabelHeight - 6f;
            float tileWidth = (GridWidth - (GameManager.MissionSelectGridColumns - 1) * TileGap)
                              / GameManager.MissionSelectGridColumns;
            float tileHeight = (tilesHeight - (GameManager.MissionSelectGridRowsPerPage - 1) * TileGap)
                               / GameManager.MissionSelectGridRowsPerPage;

            for (int slot = 0; slot < GameManager.MissionSelectTilesPerPage; slot++)
            {
                int row = slot / GameManager.MissionSelectGridColumns;
                int column = slot % GameManager.MissionSelectGridColumns;
                float x = GridX + column * (tileWidth + TileGap);
                float y = ContentTop + row * (tileHeight + TileGap);
                _tiles.Add(BuildTile(slot, x, y, tileWidth, tileHeight));
            }

            _pageLabel = NewText("PageLabel", _canvasRoot.transform, 18f,
                new Color(0.9f, 0.95f, 1f), TextAlignmentOptions.Center);
            Place(_pageLabel.rectTransform, GridX, ContentTop + ContentHeight - PageLabelHeight, GridWidth, PageLabelHeight);
        }

        private TileSlot BuildTile(int slot, float x, float y, float width, float height)
        {
            var tile = new TileSlot();
            var root = NewRect($"MissionTile_{slot}", _canvasRoot.transform);
            Place(root, x, y, width, height);
            tile.Root = root.gameObject;

            tile.SelectionGlow = NewImage("SelectionGlow", root, new Color(1f, 0.86f, 0.28f, 0.95f));
            Stretch(tile.SelectionGlow.rectTransform, -4f);

            tile.Frame = NewImage("Frame", root, Color.white);
            Stretch(tile.Frame.rectTransform);
            Sprite tileSkin = FinalGameplayArt.Load(FinalGameplayArt.HudMissionTile);
            if (tileSkin != null) tile.Frame.sprite = tileSkin;
            else tile.Frame.color = new Color(0.04f, 0.06f, 0.07f, 0.9f);

            float nameStripHeight = 72f;
            float coverHeight = height - nameStripHeight - 6f;
            RectTransform coverHolder = NewRect("CoverHolder", root);
            Place(coverHolder, 4f, 6f, width - 8f, coverHeight);
            coverHolder.gameObject.AddComponent<RectMask2D>();
            Image coverBacking = NewImage("CoverBacking", coverHolder, new Color(0.05f, 0.08f, 0.09f, 0.9f));
            Stretch(coverBacking.rectTransform);
            tile.Cover = NewCenteredImage("Cover", coverHolder);
            tile.CoverArea = new Vector2(width - 8f, coverHeight);

            tile.AccentStrip = NewImage("AccentStrip", root, Color.white);
            Place(tile.AccentStrip.rectTransform, 0f, 0f, width, 6f);

            (tile.BadgeBack, tile.BadgeCode) = BuildBadge(root, 10f, 14f, 52f, 34f, 16f);

            Image nameStrip = NewImage("NameStrip", root, new Color(0f, 0f, 0f, 0.55f));
            Place(nameStrip.rectTransform, 0f, height - nameStripHeight, width, nameStripHeight);

            tile.Name = NewText("Name", root, 21f, new Color(0.95f, 0.98f, 1f),
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            Place(tile.Name.rectTransform, 10f, height - nameStripHeight + 5f, width - 20f, 44f);

            tile.Status = NewText("Status", root, 16f, new Color(0.75f, 0.86f, 0.92f),
                TextAlignmentOptions.BottomLeft);
            Place(tile.Status.rectTransform, 10f, height - 26f, width - 20f, 20f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = tile.Frame;
            tile.Frame.raycastTarget = true;
            int capturedSlot = slot;
            button.onClick.AddListener(() =>
            {
                if (_game != null) _game.SelectMission(_tiles[capturedSlot].Variant);
            });

            return tile;
        }

        private void BuildDetailPanel()
        {
            RectTransform coverHolder = NewRect("DetailCoverHolder", _canvasRoot.transform);
            Place(coverHolder, DetailX, ContentTop, DetailWidth, DetailCoverHeight);
            coverHolder.gameObject.AddComponent<RectMask2D>();
            Image coverBacking = NewImage("DetailCoverBacking", coverHolder, new Color(0.05f, 0.08f, 0.09f, 0.9f));
            Stretch(coverBacking.rectTransform);
            _detailCover = NewCenteredImage("DetailCover", coverHolder);
            _detailCoverArea = new Vector2(DetailWidth, DetailCoverHeight);

            _detailAccent = NewImage("DetailAccent", _canvasRoot.transform, Color.white);
            Place(_detailAccent.rectTransform, DetailX, ContentTop, DetailWidth, 6f);

            (_detailBadgeBack, _detailBadgeCode) = BuildBadge(
                (RectTransform)_canvasRoot.transform, DetailX + 14f, ContentTop + 16f, 64f, 40f, 19f);

            float paneY = ContentTop + DetailCoverHeight + 12f;
            float paneHeight = ContentHeight - DetailCoverHeight - 12f;
            Image textPane = NewImage("DetailTextPane", _canvasRoot.transform, new Color(0.02f, 0.04f, 0.05f, 0.86f));
            Place(textPane.rectTransform, DetailX, paneY, DetailWidth, paneHeight);

            const float pad = 26f;
            float x = DetailX + pad;
            float innerWidth = DetailWidth - pad * 2f;
            float y = paneY + pad - 6f;

            _detailName = NewText("DetailName", _canvasRoot.transform, 42f,
                new Color(1f, 0.95f, 0.4f), TextAlignmentOptions.TopLeft, FontStyles.Bold);
            Place(_detailName.rectTransform, x, y, innerWidth, 52f);
            y += 54f;

            _detailMeta = NewText("DetailMeta", _canvasRoot.transform, 19f,
                new Color(0.9f, 0.95f, 1f), TextAlignmentOptions.TopLeft);
            Place(_detailMeta.rectTransform, x, y, innerWidth, 28f);
            y += 34f;

            _detailDescription = NewText("DetailDescription", _canvasRoot.transform, 22f,
                new Color(0.94f, 0.97f, 1f), TextAlignmentOptions.TopLeft);
            Place(_detailDescription.rectTransform, x, y, innerWidth, 92f);
            EnableShrinkToFit(_detailDescription, 22f, 20f);
            y += 98f;

            TextMeshProUGUI howToHeader = NewText("HowToHeader", _canvasRoot.transform, 19f,
                new Color(1f, 0.82f, 0.3f), TextAlignmentOptions.TopLeft, FontStyles.Bold);
            Place(howToHeader.rectTransform, x, y, innerWidth, 26f);
            howToHeader.text = "YOUR TEAM PLAN";
            y += 30f;

            float buttonHeight = 64f;
            float buttonY = paneY + paneHeight - pad - buttonHeight;
            float challengeY = buttonY - 34f;
            float howToHeight = Mathf.Max(80f, challengeY - 8f - y);

            _detailHowTo = NewText("DetailHowTo", _canvasRoot.transform, 22f,
                new Color(0.94f, 0.97f, 1f), TextAlignmentOptions.TopLeft);
            Place(_detailHowTo.rectTransform, x, y, innerWidth, howToHeight);
            EnableShrinkToFit(_detailHowTo, 22f, DetailTextMinimumSize);

            // CF1.9: chip rows share this exact rect. Only one of _detailHowTo (no chip data) or
            // the chip row pool (chip data present) is active at a time - see SyncTeamPlanChips.
            _howToAreaX = x;
            _howToAreaY = y;
            _howToAreaWidth = innerWidth;
            _howToAreaHeight = howToHeight;
            BuildTeamPlanChipRows();

            _detailChallenge = NewText("DetailChallenge", _canvasRoot.transform, 19f,
                new Color(0.9f, 0.95f, 1f), TextAlignmentOptions.TopLeft);
            Place(_detailChallenge.rectTransform, x, challengeY, innerWidth, 28f);

            _recommendedButton = BuildButton("RecommendedButton", x, buttonY, 300f, buttonHeight,
                "Play Recommended", out _);
            _recommendedButton.onClick.AddListener(() =>
            {
                _game.SelectCouchTestFocusMission();
                _game.StartSelectedMission();
            });

            float startWidth = 330f;
            _startButton = BuildButton("StartButton", x + innerWidth - startWidth, buttonY, startWidth, buttonHeight,
                string.Empty, out _startLabel);
            _startButton.onClick.AddListener(() => _game.StartSelectedMission());
        }

        // CF1.9: a fixed pool of MaxTeamPlanChipRows (icon + text) rows, built once and reused
        // across missions/syncs. Inactive by default so a chip-less mission's picker is byte-for-
        // byte identical to before this task - only SyncTeamPlanChips activates rows, and only for
        // a mission with chip data.
        private void BuildTeamPlanChipRows()
        {
            for (int i = 0; i < MaxTeamPlanChipRows; i++)
            {
                RectTransform root = NewRect($"TeamPlanChipRow_{i}", _canvasRoot.transform);
                root.gameObject.SetActive(false);
                var row = new TeamPlanChipRow { Root = root.gameObject };

                row.Icon = NewImage("ChipIcon", root, Color.white);
                row.Text = NewText("ChipText", root, 22f, new Color(0.94f, 0.97f, 1f),
                    TextAlignmentOptions.TopLeft);
                EnableShrinkToFit(row.Text, 22f, DetailTextMinimumSize);

                _chipRows.Add(row);
            }
        }

        private Button BuildButton(string name, float x, float y, float width, float height,
            string label, out TextMeshProUGUI labelText)
        {
            Image bg = NewImage(name, _canvasRoot.transform, Color.white);
            Place(bg.rectTransform, x, y, width, height);
            Sprite skin = FinalGameplayArt.Load(FinalGameplayArt.HudButtonPrimary);
            if (skin != null) bg.sprite = skin;
            else bg.color = new Color(0.1f, 0.18f, 0.2f, 0.95f);
            bg.raycastTarget = true;

            labelText = NewText("Label", bg.rectTransform, 20f, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(labelText.rectTransform);
            labelText.text = label;

            Button button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            return button;
        }

        private (Image, TextMeshProUGUI) BuildBadge(RectTransform parent, float x, float y,
            float width, float height, float fontSize)
        {
            Image back = NewImage("BadgeBack", parent, Color.white);
            Place(back.rectTransform, x, y, width, height);
            Image inset = NewImage("BadgeInset", back.rectTransform, new Color(0.02f, 0.04f, 0.05f, 0.72f));
            Stretch(inset.rectTransform, 4f);
            TextMeshProUGUI code = NewText("BadgeCode", back.rectTransform, fontSize,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(code.rectTransform);
            return (back, code);
        }

        // --- Sync (view follows GameManager; nothing here mutates game state) ---

        private void Sync()
        {
            int count = _game.MissionSelectOptionCount;
            int page = _game.SelectedMissionPage;
            int pageStart = page * GameManager.MissionSelectTilesPerPage;

            _sessionStats.text = _game.SessionMissionsPlayed == 0
                ? "Pick an adventure, grab two controllers, and play together."
                : $"Tonight: {_game.SessionMissionsPlayed} played • " +
                  $"{_game.SessionUniqueMissionsCompleted}/{count} explored • " +
                  $"{_game.SessionFlawlessClears} flawless";
            _pageLabel.text =
                $"Adventure Library  •  Page {page + 1} of {_game.MissionSelectPageCount}";

            for (int slot = 0; slot < _tiles.Count; slot++)
            {
                TileSlot tile = _tiles[slot];
                int index = pageStart + slot;
                bool active = index < count;
                tile.Root.SetActive(active);
                if (!active) continue;

                GameManager.MissionVariant variant = _game.MissionVariantAt(index);
                bool selected = index == _game.SelectedMissionIndex;
                tile.Variant = variant;
                Color accent = ArenaHud.MissionBadgeColorFor(variant);

                FitTileCover(tile.Cover, CoverSpriteFor(variant), tile.CoverArea);
                tile.AccentStrip.color = accent;
                tile.BadgeBack.color = accent;
                tile.BadgeCode.text = ArenaHud.MissionBadgeCodeFor(variant);
                tile.Name.text = GameManager.BuildMissionDefinition(variant).Name;
                tile.Name.color = selected ? new Color(1f, 0.92f, 0.42f) : new Color(0.95f, 0.98f, 1f);
                string status = _game.MissionSelectStatusFor(variant);
                tile.Status.text = variant == _game.CouchTestFocusVariant
                    ? $"RECOMMENDED • {status}"
                    : status;
                tile.SelectionGlow.enabled = selected;
            }

            SyncDetail();
        }

        /// <summary>
        /// Builds a pre-mission preview rather than dumping every tutorial instruction on the
        /// selection screen. The full catalog still drives in-game teaching; here players get at
        /// most four high-value team beats, with live world labels highlighted verbatim.
        /// </summary>
        public static string BuildHowToPlayText(GameManager.MissionVariant variant)
        {
            string[] steps = PreviewStepsFor(variant);
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < steps.Length; i++)
            {
                if (i > 0) builder.Append('\n');
                builder.Append("•  ").Append(MissionInstructionCatalog.HighlightOnScreenLabels(steps[i]));
            }

            return builder.ToString();
        }

        private static string[] PreviewStepsFor(GameManager.MissionVariant variant)
        {
            // The deep slice's catalog deliberately explains all four authored beats plus failure
            // behavior. Merge that into four sofa-readable lines without losing the role flip or
            // united-bark payoff players need before the next acceptance session.
            if (variant == GameManager.MissionVariant.OperationPeeBreak)
            {
                return new[]
                {
                    "Cocoa starts by holding a door stare until the Teenager looks up.",
                    "Then Cocoa keeps staring while Cheddar presents the leash - both signals must happen together.",
                    "Swap roles: Cheddar blocks the hallway while Cocoa unplugs the charger.",
                    "Finish at the door with the leash and bark together. Early barks cause a funny misread, not a failure."
                };
            }

            // Backyard Rescue has seven optional/subsystem notes. The picker only needs its main
            // arc; contextual world prompts teach the trap hand-off and pool when they become live.
            if (variant == GameManager.MissionVariant.BackyardRescue)
            {
                string[] all = MissionInstructionCatalog.HowToPlayStepsFor(variant);
                return new[] { all[0], all[1], all[2], all[5] };
            }

            string[] steps = MissionInstructionCatalog.HowToPlayStepsFor(variant);
            if (steps.Length <= 4) return steps;

            var preview = new string[4];
            System.Array.Copy(steps, preview, preview.Length);
            return preview;
        }

        private void SyncDetail()
        {
            GameManager.MissionVariant variant = _game.SelectedMissionVariant;
            Color accent = ArenaHud.MissionBadgeColorFor(variant);

            FitDetailCover(_detailCover, CoverSpriteFor(variant), _detailCoverArea);
            _detailAccent.color = accent;
            _detailBadgeBack.color = accent;
            _detailBadgeCode.text = ArenaHud.MissionBadgeCodeFor(variant);
            _detailName.text = _game.SelectedMissionName;
            string recommendation = variant == _game.CouchTestFocusVariant
                ? "<b><color=#ffd75e>RECOMMENDED</color></b> • "
                : string.Empty;
            _detailMeta.text = $"{recommendation}{_game.MissionSelectDetailsFor(variant)} • {_game.MissionSelectStatusFor(variant)}";
            _detailDescription.text = MissionInstructionCatalog.DescriptionFor(variant);
            // Always kept in sync, even when the chip rows are what's actually on screen (below):
            // DetailHowToPlayText/DetailHowToFontFloor are read directly off this component by
            // existing tests regardless of its visibility, so its content/sizing must never depend
            // on whether chips are active for the current mission.
            _detailHowTo.text = BuildHowToPlayText(variant);
            SyncTeamPlanChips(variant);
            _detailChallenge.text = _game.SelectedMissionChallengeLabel;

            const float pad = 26f;
            float x = DetailX + pad;
            float innerWidth = DetailWidth - pad * 2f;
            float buttonY = ContentTop + ContentHeight - pad - 64f;
            bool isRecommended = variant == _game.CouchTestFocusVariant;
            _recommendedButton.gameObject.SetActive(!isRecommended);
            if (isRecommended)
            {
                Place(_startButton.GetComponent<RectTransform>(), x, buttonY, innerWidth, 64f);
                _startLabel.text = "Start Recommended Adventure";
            }
            else
            {
                const float startWidth = 330f;
                Place(_startButton.GetComponent<RectTransform>(), x + innerWidth - startWidth, buttonY, startWidth, 64f);
                _startLabel.text = $"Start {_game.SelectedMissionName}";
            }
        }

        /// <summary>
        /// CF1.9: renders team-plan chips - a small icon of the real on-screen signal object next
        /// to a bullet, loaded from the same <see cref="FinalGameplayArt"/> resources gameplay
        /// itself uses - when <see cref="TeamPlanChipsFor"/> has data for the current mission. A
        /// mission with no chip data (everything but Operation Pee Break today) leaves every row
        /// inactive and the single <see cref="_detailHowTo"/> block active, i.e. renders exactly
        /// as it did before this task. This is the fallback CF2.8's roster-wide rollout depends on.
        /// </summary>
        private void SyncTeamPlanChips(GameManager.MissionVariant variant)
        {
            string[] steps = PreviewStepsFor(variant);
            string[] chipSprites = TeamPlanChipsFor(variant);
            bool useChips = chipSprites != null && chipSprites.Length > 0;
            _detailHowTo.gameObject.SetActive(!useChips);

            for (int row = 0; row < _chipRows.Count; row++)
            {
                bool rowActive = useChips && row < steps.Length;
                _chipRows[row].Root.SetActive(rowActive);
                if (!rowActive) continue;

                float rowHeight = _howToAreaHeight / steps.Length;
                float rowY = _howToAreaY + row * rowHeight;

                string spritePath = row < chipSprites.Length ? chipSprites[row] : null;
                Sprite iconSprite = string.IsNullOrEmpty(spritePath) ? null : FinalGameplayArt.Load(spritePath);
                bool showIcon = iconSprite != null;

                Image icon = _chipRows[row].Icon;
                icon.enabled = showIcon;
                float reservedIconWidth = 0f;
                if (showIcon)
                {
                    icon.sprite = iconSprite;
                    float iconSize = Mathf.Min(ChipIconSize, rowHeight - 4f);
                    Place(icon.rectTransform, _howToAreaX, rowY + (rowHeight - iconSize) * 0.5f,
                        iconSize, iconSize);
                    reservedIconWidth = ChipIconSize + ChipIconGap;
                }

                TextMeshProUGUI text = _chipRows[row].Text;
                Place(text.rectTransform, _howToAreaX + reservedIconWidth, rowY,
                    _howToAreaWidth - reservedIconWidth, rowHeight);
                text.text = "•  " + MissionInstructionCatalog.HighlightOnScreenLabels(steps[row]);
            }
        }

        /// <summary>
        /// CF1.9 chip data contract: one sprite-path entry per <see cref="PreviewStepsFor"/> bullet
        /// (null/empty entries render text-only - "just the important ones"), or null for a mission
        /// with no chip data at all (renders exactly as before this task). CF2.8 rolls this same
        /// shape out roster-wide: every entry below is a sprite path already loaded by that exact
        /// mission's own *MissionController.cs (grepped per mission, not guessed), so the chip is a
        /// genuine preview of the real on-screen signal, matched to the bullet index it illustrates.
        /// A mission is left returning null when fewer than 2 of its previewed bullets have a
        /// clearly load-bearing existing sprite to point to (BackyardRescue below) - per the task's
        /// own explicit fallback, this is not an oversight, it's "don't force coverage."
        /// </summary>
        private static string[] TeamPlanChipsFor(GameManager.MissionVariant variant)
        {
            switch (variant)
            {
                case GameManager.MissionVariant.OperationPeeBreak:
                    return new[]
                    {
                        FinalGameplayArt.PeeBreakOpenDoor,     // Beat 1: Cocoa's door stare
                        FinalGameplayArt.PeeBreakLeash,        // Beat 2: Cheddar presents the leash
                        FinalGameplayArt.PeeBreakPhoneCharger, // Beat 3: Cocoa unplugs the charger
                        // Beat 4 is the united-bark finish at the door; Pee Break has no dedicated
                        // bark-burst prop sprite, so this reuses the shared VFX/bark_burst art
                        // gameplay itself plays on every bark (see DogReadabilityFeedback/
                        // ArenaFeedbackCatalog) - a real on-screen thing players already recognize,
                        // not a placeholder guess.
                        FinalGameplayArt.BarkBurst,
                    };

                // CF2.8 audit: BackyardRescueMissionController.cs only ever loads
                // BackyardTrapGap*/BackyardWeenie* sprites. The picker's 4 previewed bullets
                // (collect weenies / bark the squirrel / predator warning huddle / rope-tug finish)
                // only match ONE of those - the weenie-collection bullet - because the shared
                // squirrel/predator/rope actors here render with their generated draft-art parts,
                // never a FinalGameplayArt override (confirmed: no Eagle*/Squirrel*/Rope* load call
                // anywhere in this controller). Fewer than 2 genuine matches - stays text-only
                // rather than pairing a real weenie chip with two invented ones.
                case GameManager.MissionVariant.BackyardRescue:
                    return null;

                case GameManager.MissionVariant.SnackHeist:
                    return new[]
                    {
                        FinalGameplayArt.SnackHeistPlateTargeted, // "run him into snack plates"
                        FinalGameplayArt.SnackHeistGuardLane,     // "Cocoa is the guard...bark"
                        // Remaining two bullets (the watched-final-snack rule, wrong-role penalty)
                        // have no distinct on-screen sprite of their own - left text-only.
                    };

                case GameManager.MissionVariant.SockPanic:
                    return new[]
                    {
                        FinalGameplayArt.SockPanicBasketClosed, // "LAUNDRY BASKET" before the tip
                        FinalGameplayArt.SockPanicSockExposed,  // "dive onto the exposed sock"
                        FinalGameplayArt.SockPanicBasketFumble, // "the basket flops shut"
                        FinalGameplayArt.SockPanicSockSaved,    // "Rescue 5 socks"
                    };

                case GameManager.MissionVariant.SquirrelConspiracy:
                    return new[]
                    {
                        FinalGameplayArt.SquirrelConspiracyCutoffOpen,     // "glowing HOLD CUTOFF"
                        FinalGameplayArt.SquirrelConspiracyCutoffHeld,     // "physically holding..."
                        FinalGameplayArt.SquirrelConspiracyStashRevealed,  // "reveal the hidden stash"
                        FinalGameplayArt.SquirrelConspiracyStashCracked,   // "Crack the case"
                    };

                case GameManager.MissionVariant.EagleShadowPanic:
                    return new[]
                    {
                        FinalGameplayArt.EagleShadowCoverSafe,       // "inside HIDE HERE cover"
                        FinalGameplayArt.EagleShadowTalonGripOpen,   // "WIGGLE the grip open"
                        FinalGameplayArt.EagleShadowTalonGripFreed,  // "wiggle/pull handoff...finish"
                        FinalGameplayArt.EagleShadowCoverSpotted,    // "exposures caught in the open"
                    };

                case GameManager.MissionVariant.CoyotesFence:
                    return new[]
                    {
                        // CoyotesFenceMissionController applies GapPinned to the PREDATOR object
                        // itself the instant a bark pins it (line-verified), so this is literally
                        // what the coyote looks like during "BARK to pin it".
                        FinalGameplayArt.CoyotesFenceGapPinned,
                        FinalGameplayArt.CoyotesFenceGapOpen, // the active "WEAK SPOT"
                        null, // "neither dog can perform both jobs..." - no distinct visual
                        FinalGameplayArt.CoyotesFenceFakeSnack, // "the fake snack lure"
                    };

                case GameManager.MissionVariant.WeenieRoundup:
                    return new[]
                    {
                        FinalGameplayArt.WeenieRoundupLoose,    // "walk into one to pick it up"
                        FinalGameplayArt.WeenieRoundupCarried,  // "team haul...Cheddar grabs it"
                        FinalGameplayArt.WeenieRoundupDropped,  // "the jumbo fumble"
                        FinalGameplayArt.WeenieRoundupBowlFull, // "the live bowl-full payoff"
                    };

                case GameManager.MissionVariant.ScentSearch:
                    return new[]
                    {
                        FinalGameplayArt.ScentSearchDigUnknown, // patches before a call lands
                        FinalGameplayArt.ScentSearchScentHot,   // "a red-hot bark calls the mound"
                        null, // "Cheddar follows Cocoa's call...glowing mound" - no distinct sprite
                        FinalGameplayArt.ScentSearchBoneFound,  // "Find 3 bones"
                    };

                case GameManager.MissionVariant.ThunderstormComfort:
                    return new[]
                    {
                        FinalGameplayArt.ThunderstormCloudWaiting, // calm "before each clap"
                        FinalGameplayArt.ThunderstormComfortHuddle, // "steady reassurance"/huddle
                        FinalGameplayArt.ThunderstormThunderclap,  // "Hold the...huddle" through claps
                        FinalGameplayArt.ThunderstormStormCleared, // "the live storm-passed beat"
                    };

                case GameManager.MissionVariant.MarkTheYard:
                    return new[]
                    {
                        FinalGameplayArt.MarkYardZoneUnclaimed,  // "a grey zone...mark it green"
                        FinalGameplayArt.MarkYardSquirrelWatch,  // "the reclaim squirrel"
                        FinalGameplayArt.MarkYardZoneClaimed,    // "reach another zone" success
                        FinalGameplayArt.MarkYardZoneStolen,     // "A stolen mark"
                    };

                case GameManager.MissionVariant.LeashWalk:
                    return new[]
                    {
                        // No physical leash prop in this mission (CF2.5 audit: it's an abstract
                        // max-distance constraint, nothing rendered) - first bullet stays text-only.
                        null,
                        FinalGameplayArt.LeashWalkCheckpointWaiting, // "reach the marker"
                        FinalGameplayArt.LeashWalkCheckpointReached, // "stand...together to bank it"
                        FinalGameplayArt.LeashWalkSnapWarning,       // "the leash snaps taut"
                    };

                case GameManager.MissionVariant.CarRide:
                    return new[]
                    {
                        FinalGameplayArt.CarDashboardDriver, // "Watch the driver: the dashboard"
                        FinalGameplayArt.SeatCooler,         // "jump over the cooler and toy bin"
                        // Brake-plant and the finishing "driver eases up" bullets don't have a
                        // sprite distinct from the dashboard already shown above - left text-only.
                    };

                case GameManager.MissionVariant.GateCrash:
                    return new[]
                    {
                        FinalGameplayArt.GateCrashGateClosed, // "the heavy gate" before anchoring
                        FinalGameplayArt.GateCrashToyWaiting, // "squeezing through...toward the toy"
                        FinalGameplayArt.GateCrashGateSnap,   // "the gate snaps shut"
                        FinalGameplayArt.GateCrashToyClaimed, // "until Cheddar claims the toy"
                    };

                case GameManager.MissionVariant.TableStealth:
                    return new[]
                    {
                        FinalGameplayArt.TableStealthHumanWatching,      // "a human is watching"
                        FinalGameplayArt.TableStealthHumanDistracted,    // "choose the opening"
                        FinalGameplayArt.TableStealthSteakSneakProgress, // "sneak progress"
                        FinalGameplayArt.TableStealthHumanSpotted,       // "gets the pair spotted"
                    };

                case GameManager.MissionVariant.SquirrelSwitcheroo:
                    return new[]
                    {
                        FinalGameplayArt.SwitcherooStashGuarded, // "guarding its buried stash"
                        FinalGameplayArt.SwitcherooDecoyChased,  // "the squirrel commits"
                        FinalGameplayArt.SwitcherooStashOpen,    // "that chase window...real stash"
                        FinalGameplayArt.SwitcherooDecoyBackfire, // "over-baiting...wise up"
                    };

                case GameManager.MissionVariant.WalkCampaign:
                    return new[]
                    {
                        FinalGameplayArt.WalkCampaignLeashPresented, // "presses Interact to present it"
                        FinalGameplayArt.WalkCampaignHumanGettingIt, // "for the human to understand"
                        FinalGameplayArt.WalkCampaignHumanMisread,   // "fetch a funny wrong item"
                        FinalGameplayArt.WalkCampaignHumanWalkies,   // "the live WALKIES payoff"
                    };

                case GameManager.MissionVariant.BoneRelay:
                    return new[]
                    {
                        FinalGameplayArt.BoneRelayMoundUnknown,     // "look-alike dirt mounds"
                        FinalGameplayArt.BoneRelayScentPostCalled,  // "the scent post and BARKS"
                        FinalGameplayArt.BoneRelayMoundCalled,      // "Cocoa's glowing call"
                        FinalGameplayArt.BoneRelayMoundFound,       // "three finds expose the stash"
                    };

                case GameManager.MissionVariant.GreatEscape:
                    return new[]
                    {
                        FinalGameplayArt.GreatEscapeStationWaiting,     // the contraption chain, idle
                        FinalGameplayArt.GreatEscapeStationCocoaActive, // "the glowing station...owner"
                        FinalGameplayArt.GreatEscapeStationFumble,      // "a harmless, readable CLANK"
                        FinalGameplayArt.GreatEscapeStationCompleted,   // "the completed contraption"
                    };

                case GameManager.MissionVariant.ChaosMachine:
                    return new[]
                    {
                        FinalGameplayArt.ChaosLeverReady,          // "reaches the lever"
                        FinalGameplayArt.ChaosJunctionTowelDrop,   // "the glowing junction"
                        FinalGameplayArt.ChaosJunctionBasketTip,   // "visibly jams at that...junction"
                        FinalGameplayArt.ChaosJunctionToyLaunch,   // "a clean toy launch"
                    };

                case GameManager.MissionVariant.BlanketCatch:
                    return new[]
                    {
                        FinalGameplayArt.BlanketCatchTaut,    // "pull the blanket taut"
                        FinalGameplayArt.BlanketSnackFalling, // "call the next snack down"
                        FinalGameplayArt.BlanketSnackCaught,  // "hold the catch together"
                        FinalGameplayArt.BlanketCatchRipping, // "Rip the blanket too many times"
                    };

                case GameManager.MissionVariant.KitchenFoodFrenzy:
                    return new[]
                    {
                        FinalGameplayArt.KitchenCounterReady,   // "reach the marked COUNTER route"
                        FinalGameplayArt.KitchenSafeBowlCatch,  // "catch gold food in the SAFE BOWL"
                        FinalGameplayArt.KitchenFoodGoodFalling, // "survive the DINNER RUSH...catch gold"
                    };

                case GameManager.MissionVariant.BabyBirdBedlam:
                    return new[]
                    {
                        FinalGameplayArt.BabyBirdChickFalling,   // "Chicks drop from THE NEST"
                        FinalGameplayArt.BabyBirdChickShaking,   // "shakes it down"
                        FinalGameplayArt.BabyBirdMotherAttacking, // "PARENT BIRD DIVE flashes"
                        FinalGameplayArt.BabyBirdChickPecked,    // "An un-repelled dive PECKS"
                    };

                // SkunkBlastMayhemMissionController reuses Sock Panic's authored laundry-basket art
                // for its own basket/pile (both are literally laundry baskets in-fiction), which is a
                // genuine match for the last two previewed bullets. The lure/snatch beat and the
                // tail-lift telegraph both play out on the shared generated predator body (no
                // FinalGameplayArt override), so those two rows stay text-only rather than guessing.
                case GameManager.MissionVariant.SkunkBlastMayhem:
                    return new[]
                    {
                        null,
                        null,
                        FinalGameplayArt.SockPanicBasketOpen,   // "Interact at the laundry pile to rub off the stink"
                        FinalGameplayArt.SockPanicBasketClosed, // "hauls fresh laundry from the basket"
                    };

                // BurrMazeMissionController has no bespoke cat/cone/checkpoint art yet, but its
                // hiding bushes and bramble patches genuinely reuse existing FinalGameplayArt props
                // (Bush - the same "cover" fiction Backyard Rescue's environment pass uses it for -
                // and Grass, tinted into a bramble read) - a real match for two of the four
                // previewed bullets.
                case GameManager.MissionVariant.BurrMaze:
                    return new[]
                    {
                        FinalGameplayArt.Bush, // "duck into a HIDE HERE bush to break its notice"
                        FinalGameplayArt.Grass, // "bramble patches cake a dog in burrs"
                        null, // "SPOTTED! - both dogs reset to the last CHECKPOINT" - no distinct sprite
                        null, // "bark to lure the cat's attention" - plays on the shared predator actor
                    };

                default:
                    return null;
            }
        }

        private Sprite CoverSpriteFor(GameManager.MissionVariant variant)
        {
            if (!_coverCache.TryGetValue(variant, out Sprite sprite))
            {
                sprite = FinalGameplayArt.LoadMissionTile(variant);
                _coverCache[variant] = sprite;
            }

            return sprite;
        }

        // The generated square covers contain their own title ribbon across the lower fifth. Zoom
        // and bias the art down so the mask shows the illustration while our readable TMP strip
        // owns the mission name. This also keeps all tiles consistent if title-free covers replace
        // the current generated set later.
        private static void FitTileCover(Image cover, Sprite sprite, Vector2 area)
        {
            cover.enabled = sprite != null;
            if (sprite == null) return;
            cover.sprite = sprite;
            float scale = Mathf.Max(area.x / sprite.rect.width, area.y / sprite.rect.height) * TileArtworkZoom;
            cover.rectTransform.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
            cover.rectTransform.anchoredPosition = new Vector2(0f, area.y * TileArtworkVerticalShift);
        }

        // V3.3: the detail pane is a wide 896x300 letterbox holding a 1:1 square cover. The original
        // Mathf.Min ("contain") scale fit the whole square inside the shorter dimension (height),
        // leaving the square's full width (~408px) stranded in the middle of the 896px-wide window
        // with DetailCoverBacking's near-black fill exposed on both sides - never caught because this
        // sandbox has no GPU/display to actually render the picker. Mathf.Max ("cover", matching
        // FitTileCover's own approach) fills the width and crops top/bottom instead, verified against
        // a pixel-accurate offline recomposite of this exact math across a dozen mission covers.
        private static void FitDetailCover(Image cover, Sprite sprite, Vector2 area)
        {
            cover.enabled = sprite != null;
            if (sprite == null) return;
            cover.sprite = sprite;
            float scale = Mathf.Max(area.x / sprite.rect.width, area.y / sprite.rect.height) * DetailArtworkZoom;
            cover.rectTransform.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
            cover.rectTransform.anchoredPosition = new Vector2(0f, area.y * DetailArtworkVerticalShift);
        }

        // TMP auto-size remains a safety net, but the preview copy is deliberately short enough
        // that sofa text never collapses to the old 13pt developer-dashboard size.
        private static void EnableShrinkToFit(TextMeshProUGUI text, float maxSize, float minSize)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var eventSystem = new GameObject("MissionSelectEventSystem");
            eventSystem.AddComponent<EventSystem>();
            // The project runs Input System-only (activeInputHandler 2), so the legacy
            // StandaloneInputModule would throw; use the Input System UI module.
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        // --- Small builders (top-left coordinates in 1920x1080 reference space, like the IMGUI
        // rects this screen replaces, so layout numbers stay easy to compare) ---

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            RectTransform rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image NewCenteredImage(string name, Transform parent)
        {
            RectTransform rect = NewRect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, Color color,
            TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = NewRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
