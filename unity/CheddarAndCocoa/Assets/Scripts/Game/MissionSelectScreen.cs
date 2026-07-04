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
    /// panel (cover, badge, description, how-to-play, challenge, readiness) for the focused tile on
    /// the right. Replaces the IMGUI mission-select rows so text stays crisp at any resolution
    /// instead of scaling a 1080p bitmap layout. This is a pure view: selection, paging, and the
    /// F5/P/Y couch-test shortcut all stay in <see cref="GameManager.TickFlowInput"/>, and the grid
    /// shape is <see cref="GameManager.MissionSelectGridColumns"/> x
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
        private const float DetailCoverHeight = 300f;

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

        private GameManager _game;
        private GameObject _canvasRoot;
        private Canvas _canvas;
        private readonly List<TileSlot> _tiles = new List<TileSlot>();
        private readonly Dictionary<GameManager.MissionVariant, Sprite> _coverCache =
            new Dictionary<GameManager.MissionVariant, Sprite>();
        private int _syncedSelection = -1;

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
        private TextMeshProUGUI _detailReadiness;
        private Button _startButton;
        private TextMeshProUGUI _startLabel;
        private Button _couchFocusButton;

        public void Init(GameManager game) => _game = game;

        public bool Visible => _canvasRoot != null && _canvasRoot.activeSelf;
        public Canvas Canvas => _canvas;
        public int TileCapacity => GameManager.MissionSelectTilesPerPage;
        public Button StartButton => _startButton;
        public Button CouchFocusButton => _couchFocusButton;
        public string PageLabelText => _pageLabel != null ? _pageLabel.text : string.Empty;
        public string DetailNameText => _detailName != null ? _detailName.text : string.Empty;
        public string DetailDescriptionText => _detailDescription != null ? _detailDescription.text : string.Empty;
        public string DetailHowToPlayText => _detailHowTo != null ? _detailHowTo.text : string.Empty;
        public string DetailBadgeCodeText => _detailBadgeCode != null ? _detailBadgeCode.text : string.Empty;
        public string DetailChallengeText => _detailChallenge != null ? _detailChallenge.text : string.Empty;
        public string DetailReadinessText => _detailReadiness != null ? _detailReadiness.text : string.Empty;
        public Sprite DetailCoverSprite => _detailCover != null ? _detailCover.sprite : null;

        /// <summary>
        /// Couch test #4 contract: the description and how-to blocks must render inside their own
        /// rects (auto-sized), never spilling over the challenge/readiness lines and buttons.
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

        /// <summary>Couch test #4 contract: the detail cover shows the whole image (aspect-fit).</summary>
        public bool DetailCoverUncropped
        {
            get
            {
                if (_detailCover == null || _detailCover.sprite == null) return false;
                Vector2 size = _detailCover.rectTransform.sizeDelta;
                return size.x <= _detailCoverArea.x + 0.5f && size.y <= _detailCoverArea.y + 0.5f;
            }
        }

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
        public bool TileIsSelectedAt(int slot) => _tiles[slot].SelectionGlow.enabled;
        public GameManager.MissionVariant TileVariantAt(int slot) => _tiles[slot].Variant;

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
            TextMeshProUGUI title = NewText("Title", _canvasRoot.transform, 46f,
                new Color(1f, 0.95f, 0.4f), TextAlignmentOptions.Center, FontStyles.Bold);
            Place(title.rectTransform, 0f, 14f, ReferenceWidth, 56f);
            title.text = "Cheddar + Cocoa Couch Missions";

            _sessionStats = NewText("SessionStats", _canvasRoot.transform, 23f,
                Color.white, TextAlignmentOptions.Center);
            Place(_sessionStats.rectTransform, 32f, 72f, ReferenceWidth - 64f, 30f);

            TextMeshProUGUI controls = NewText("ControlsHint", _canvasRoot.transform, 19f,
                new Color(0.9f, 0.95f, 1f), TextAlignmentOptions.Center);
            Place(controls.rectTransform, 32f, 104f, ReferenceWidth - 64f, 26f);
            controls.text = $"{_game.CouchTestFocusLabel} • arrows/D-pad move • Enter/Start begins";

            TextMeshProUGUI showcase = NewText("ShowcaseHint", _canvasRoot.transform, 17f,
                new Color(0.72f, 0.82f, 0.88f), TextAlignmentOptions.Center);
            Place(showcase.rectTransform, 32f, 132f, ReferenceWidth - 64f, 24f);
            showcase.text = _game.FamilyShowcaseShortcutLabel;
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

            float nameStripHeight = 64f;
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

            tile.Name = NewText("Name", root, 20f, new Color(0.95f, 0.98f, 1f),
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            Place(tile.Name.rectTransform, 10f, height - nameStripHeight + 6f, width - 20f, 38f);

            tile.Status = NewText("Status", root, 13f, new Color(0.75f, 0.86f, 0.92f),
                TextAlignmentOptions.BottomLeft);
            Place(tile.Status.rectTransform, 10f, height - 24f, width - 20f, 18f);

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
            Place(_detailDescription.rectTransform, x, y, innerWidth, 100f);
            EnableShrinkToFit(_detailDescription, 22f);
            y += 106f;

            TextMeshProUGUI howToHeader = NewText("HowToHeader", _canvasRoot.transform, 19f,
                new Color(1f, 0.82f, 0.3f), TextAlignmentOptions.TopLeft, FontStyles.Bold);
            Place(howToHeader.rectTransform, x, y, innerWidth, 26f);
            howToHeader.text = "HOW TO PLAY";
            y += 30f;

            float buttonHeight = 54f;
            float buttonY = paneY + paneHeight - pad - buttonHeight;
            float readinessY = buttonY - 30f;
            float challengeY = readinessY - 28f;
            float howToHeight = Mathf.Max(80f, challengeY - 8f - y);

            _detailHowTo = NewText("DetailHowTo", _canvasRoot.transform, 22f,
                new Color(0.94f, 0.97f, 1f), TextAlignmentOptions.TopLeft);
            Place(_detailHowTo.rectTransform, x, y, innerWidth, howToHeight);
            EnableShrinkToFit(_detailHowTo, 22f);

            _detailChallenge = NewText("DetailChallenge", _canvasRoot.transform, 18f,
                new Color(0.9f, 0.95f, 1f), TextAlignmentOptions.TopLeft);
            Place(_detailChallenge.rectTransform, x, challengeY, innerWidth, 26f);

            _detailReadiness = NewText("DetailReadiness", _canvasRoot.transform, 18f,
                new Color(0.78f, 0.88f, 0.92f), TextAlignmentOptions.TopLeft);
            Place(_detailReadiness.rectTransform, x, readinessY, innerWidth, 26f);

            _couchFocusButton = BuildButton("CouchFocusButton", x, buttonY, 300f, buttonHeight,
                "Highlight Couch Test", out _);
            _couchFocusButton.onClick.AddListener(() => _game.SelectCouchTestFocusMission());

            float startWidth = 330f;
            _startButton = BuildButton("StartButton", x + innerWidth - startWidth, buttonY, startWidth, buttonHeight,
                string.Empty, out _startLabel);
            _startButton.onClick.AddListener(() => _game.StartSelectedMission());
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

            _sessionStats.text =
                $"{_game.SessionMissionsPlayed} played • {_game.SessionUniqueMissionsCompleted}/{count} tried • " +
                $"{_game.SessionTotalScore} score • {_game.SessionFlawlessClears} flawless";
            _pageLabel.text =
                $"Page {page + 1}/{_game.MissionSelectPageCount} • left/right at a row end flips the page";

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

                FitCover(tile.Cover, CoverSpriteFor(variant), tile.CoverArea);
                tile.AccentStrip.color = accent;
                tile.BadgeBack.color = accent;
                tile.BadgeCode.text = ArenaHud.MissionBadgeCodeFor(variant);
                tile.Name.text = GameManager.BuildMissionDefinition(variant).Name;
                tile.Name.color = selected ? new Color(1f, 0.92f, 0.42f) : new Color(0.95f, 0.98f, 1f);
                tile.Status.text = _game.MissionSelectStatusFor(variant);
                tile.SelectionGlow.enabled = selected;
            }

            SyncDetail();
        }

        /// <summary>
        /// Couch test #3: HOW TO PLAY reads as short bullet steps, with every on-screen label
        /// (SQUIRREL STEALING - BARK!, ESCAPE GAP...) highlighted gold exactly as it appears live.
        /// Pure string build so tests can pin the rendered readout.
        /// </summary>
        public static string BuildHowToPlayText(GameManager.MissionVariant variant)
        {
            string[] steps = MissionInstructionCatalog.HowToPlayStepsFor(variant);
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < steps.Length; i++)
            {
                if (i > 0) builder.Append('\n');
                builder.Append("•  ").Append(MissionInstructionCatalog.HighlightOnScreenLabels(steps[i]));
            }

            return builder.ToString();
        }

        private void SyncDetail()
        {
            GameManager.MissionVariant variant = _game.SelectedMissionVariant;
            Color accent = ArenaHud.MissionBadgeColorFor(variant);

            FitCoverContain(_detailCover, CoverSpriteFor(variant), _detailCoverArea);
            _detailAccent.color = accent;
            _detailBadgeBack.color = accent;
            _detailBadgeCode.text = ArenaHud.MissionBadgeCodeFor(variant);
            _detailName.text = _game.SelectedMissionName;
            _detailMeta.text = $"{_game.MissionSelectDetailsFor(variant)} • {_game.MissionSelectStatusFor(variant)}";
            _detailDescription.text = MissionInstructionCatalog.DescriptionFor(variant);
            _detailHowTo.text = BuildHowToPlayText(variant);
            _detailChallenge.text = _game.SelectedMissionChallengeLabel;
            _detailReadiness.text = _game.SelectedMissionReadinessLabel;
            _startLabel.text = $"Start {_game.SelectedMissionName}";
            _couchFocusButton.gameObject.SetActive(variant != _game.CouchTestFocusVariant);
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

        // Aspect-fill: scale the sprite so it covers the holder (which clips via RectMask2D),
        // matching the IMGUI ScaleAndCrop presentation the tiles had before.
        private static void FitCover(Image cover, Sprite sprite, Vector2 area)
        {
            cover.enabled = sprite != null;
            if (sprite == null) return;
            cover.sprite = sprite;
            float scale = Mathf.Max(area.x / sprite.rect.width, area.y / sprite.rect.height);
            cover.rectTransform.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
        }

        // Aspect-fit: the whole image stays visible inside the holder (dark backing letterboxes
        // the rest). Couch test #4: the detail cover was aspect-filled into a 2.3:1 strip that
        // cropped the mission art down to a sliver.
        private static void FitCoverContain(Image cover, Sprite sprite, Vector2 area)
        {
            cover.enabled = sprite != null;
            if (sprite == null) return;
            cover.sprite = sprite;
            float scale = Mathf.Min(area.x / sprite.rect.width, area.y / sprite.rect.height);
            cover.rectTransform.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
        }

        // TMP auto-size: long step lists shrink to fit their rect instead of overflowing into the
        // challenge/readiness lines and buttons below.
        private static void EnableShrinkToFit(TextMeshProUGUI text, float maxSize)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = 13f;
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
