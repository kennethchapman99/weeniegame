using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CheddarAndCocoa.Game
{
    public sealed class AdventureMapHud : MonoBehaviour
    {
        [SerializeField] private string arenaSceneName = "ArenaScene";

        private AdventureMapController _controller;
        private GUIStyle _title;
        private GUIStyle _body;
        private GUIStyle _small;
        private GUIStyle _cardTitle;
        private GUIStyle _cardMeta;
        private Vector2 _missionScroll;
        private readonly Dictionary<string, Sprite> _locationPreviewCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<GameManager.MissionVariant, Sprite> _missionPreviewCache = new Dictionary<GameManager.MissionVariant, Sprite>();

        public AdventureMapController Controller => _controller;

        private void Awake()
        {
            _controller = new AdventureMapController(AdventureProgressService.LoadDefault());
        }

        private void Update()
        {
            if (_controller == null) return;
            var kb = Keyboard.current;
            var pad = Gamepad.current;

            bool prevLocation = false;
            bool nextLocation = false;
            bool prevMission = false;
            bool nextMission = false;
            bool launch = false;

            if (kb != null)
            {
                prevLocation |= kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame;
                nextLocation |= kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
                prevMission |= kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
                nextMission |= kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame;
                launch |= kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
            }

            if (pad != null)
            {
                prevLocation |= pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame;
                nextLocation |= pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame;
                prevMission |= pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame;
                nextMission |= pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame;
                launch |= pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
            }

            if (prevLocation) _controller.SelectPreviousLocation();
            if (nextLocation) _controller.SelectNextLocation();
            if (prevMission) _controller.SelectPreviousMission();
            if (nextMission) _controller.SelectNextMission();
            if (launch) LaunchSelectedMission();
        }

        public bool LaunchSelectedMission()
        {
            if (_controller == null || !_controller.TryQueueSelectedMissionLaunch()) return false;
            SceneManager.LoadScene(arenaSceneName);
            return true;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (_controller == null) return;

            // Same DPI treatment as ArenaHud: lay out in 1080p-referenced virtual space so the map
            // text stays readable on retina/4K displays.
            float uiScale = ArenaHud.UiScaleFor(Screen.width, Screen.height);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f)) * previousMatrix;
            float virtualWidth = Screen.width / uiScale;
            float virtualHeight = Screen.height / uiScale;

            float safe = 18f;
            float width = Mathf.Min(1120f, virtualWidth - safe * 2f);
            float height = Mathf.Min(720f, virtualHeight - safe * 2f);
            var box = new Rect((virtualWidth - width) * 0.5f, (virtualHeight - height) * 0.5f, width, height);

            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 24, box.y + 18, box.width - 48, 34), _controller.BuildHeaderLabel(), _title);
            DrawSelectedLocationHero(new Rect(box.x + 24, box.y + 62, box.width - 48, 156));

            float leftW = Mathf.Min(330f, box.width * 0.35f);
            float y = box.y + 238;
            GUI.Label(new Rect(box.x + 24, y, leftW - 32, 24), "Locations", _body);
            for (int i = 0; i < _controller.Locations.Count; i++)
            {
                DrawLocationCard(new Rect(box.x + 28, y + 32 + i * 78, leftW - 40, 70), i);
            }

            float missionX = box.x + leftW + 20;
            GUI.Label(new Rect(missionX, y, box.width - leftW - 44, 24), "Missions", _body);
            var missionArea = new Rect(missionX, y + 32, box.width - leftW - 44, box.height - (y - box.y) - 124);
            List<string> rows = _controller.BuildMissionRows();
            const float missionCardHeight = 92f;
            _missionScroll = GUI.BeginScrollView(missionArea, _missionScroll,
                new Rect(0, 0, missionArea.width - 20, Mathf.Max(missionArea.height, rows.Count * (missionCardHeight + 10f))));
            if (!_controller.SelectedLocationUnlocked)
            {
                GUI.Label(new Rect(0, 0, missionArea.width - 24, 28), "Locked. Earn more stars in open locations.", _small);
            }
            else if (rows.Count == 0)
            {
                GUI.Label(new Rect(0, 0, missionArea.width - 24, 28), "No missions configured yet.", _small);
            }
            else
            {
                for (int i = 0; i < rows.Count; i++)
                    DrawMissionCard(new Rect(0, i * (missionCardHeight + 10f), missionArea.width - 24, missionCardHeight), i);
            }
            GUI.EndScrollView();

            string launchText = _controller.CanLaunchSelectedMission ? "Start Mission" : "Locked";
            if (GUI.Button(new Rect(missionX, box.y + box.height - 84, 180, 34), launchText)) LaunchSelectedMission();
            GUI.Label(new Rect(box.x + 24, box.y + box.height - 42, box.width - 48, 24),
                "Controls: Left/Right location • Up/Down mission • Enter/Space/Start launch", _small);

            GUI.matrix = previousMatrix;
        }

        public static Sprite LoadLocationPreviewSprite(AdventureLocationDefinition location)
        {
            return FinalGameplayArt.LoadAdventureLocationThumbnail(location);
        }

        public static Sprite LoadMissionPreviewSprite(GameManager.MissionVariant variant)
        {
            return FinalGameplayArt.LoadMissionTile(variant);
        }

        private void DrawSelectedLocationHero(Rect rect)
        {
            var location = _controller.SelectedLocation;
            Sprite sprite = GetLocationPreviewSprite(location);
            DrawCroppedSprite(rect, sprite);
            DrawTintedRect(new Rect(rect.x, rect.y, rect.width, rect.height), new Color(0.02f, 0.03f, 0.03f, 0.38f));
            DrawTintedRect(new Rect(rect.x, rect.yMax - 48f, rect.width, 48f), new Color(0.02f, 0.03f, 0.03f, 0.72f));

            if (location != null)
            {
                Color accent = location.MapColor;
                accent.a = 0.88f;
                DrawTintedRect(new Rect(rect.x, rect.y, 7f, rect.height), accent);
            }

            GUI.Label(new Rect(rect.x + 18f, rect.y + rect.height - 92f, rect.width - 36f, 34f),
                location != null ? location.DisplayName : "Adventure Map", _title);
            GUI.Label(new Rect(rect.x + 18f, rect.y + rect.height - 56f, rect.width - 36f, 44f),
                _controller.BuildSelectedLocationLabel(), _body);
        }

        private void DrawLocationCard(Rect rect, int index)
        {
            if (index < 0 || index >= _controller.Locations.Count) return;

            var location = _controller.Locations[index];
            bool selected = index == _controller.SelectedLocationIndex;
            bool unlocked = _controller.Progress.IsLocationUnlocked(location.Id);
            DrawTintedRect(rect, selected ? new Color(0.96f, 0.88f, 0.42f, 0.22f) : new Color(0.03f, 0.05f, 0.05f, 0.78f));
            DrawCroppedSprite(new Rect(rect.x + 4f, rect.y + 4f, 82f, rect.height - 8f), GetLocationPreviewSprite(location));
            DrawTintedRect(new Rect(rect.x + 4f, rect.y + 4f, 82f, rect.height - 8f),
                unlocked ? new Color(0f, 0f, 0f, 0.12f) : new Color(0f, 0f, 0f, 0.58f));

            Color accent = location.MapColor;
            accent.a = selected ? 0.9f : 0.55f;
            DrawTintedRect(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) _controller.SelectLocation(index);

            GUI.Label(new Rect(rect.x + 96f, rect.y + 8f, rect.width - 104f, 24f), location.DisplayName, _cardTitle);
            GUI.Label(new Rect(rect.x + 96f, rect.y + 34f, rect.width - 104f, 30f),
                unlocked ? "OPEN" : $"LOCKED {_controller.TotalStars}/{location.RequiredStars}", _cardMeta);
        }

        private void DrawMissionCard(Rect rect, int index)
        {
            var location = _controller.SelectedLocation;
            if (location == null || location.Missions == null || index < 0 || index >= location.Missions.Length) return;

            var mission = location.Missions[index];
            bool selected = index == _controller.SelectedMissionIndex;
            DrawTintedRect(rect, selected ? new Color(0.96f, 0.88f, 0.42f, 0.2f) : new Color(0.03f, 0.05f, 0.05f, 0.76f));
            DrawCroppedSprite(new Rect(rect.x + 5f, rect.y + 5f, 136f, rect.height - 10f), GetMissionPreviewSprite(mission));
            DrawTintedRect(new Rect(rect.x + 5f, rect.y + 5f, 136f, rect.height - 10f), new Color(0f, 0f, 0f, 0.1f));

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) _controller.SelectMission(index);

            var record = _controller.Progress.GetMissionProgressOrEmpty(mission);
            string status = record.Completed ? $"{record.BestStars}★  {record.BestScore}  {record.BestRank}" : "NEW";
            GUI.Label(new Rect(rect.x + 154f, rect.y + 10f, rect.width - 164f, 28f),
                AdventureMapController.DisplayNameForMission(mission), _cardTitle);
            GUI.Label(new Rect(rect.x + 154f, rect.y + 40f, rect.width - 164f, 22f),
                MissionInstructionCatalog.DescriptionFor(mission), _cardMeta);
            GUI.Label(new Rect(rect.x + 154f, rect.y + 66f, rect.width - 164f, 20f), status, _small);
        }

        private Sprite GetLocationPreviewSprite(AdventureLocationDefinition location)
        {
            string key = location != null ? location.Id : string.Empty;
            if (!_locationPreviewCache.TryGetValue(key, out Sprite sprite))
            {
                sprite = LoadLocationPreviewSprite(location);
                _locationPreviewCache[key] = sprite;
            }

            return sprite;
        }

        private Sprite GetMissionPreviewSprite(GameManager.MissionVariant variant)
        {
            if (!_missionPreviewCache.TryGetValue(variant, out Sprite sprite))
            {
                sprite = LoadMissionPreviewSprite(variant);
                _missionPreviewCache[variant] = sprite;
            }

            return sprite;
        }

        private static void DrawCroppedSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null)
            {
                DrawTintedRect(rect, new Color(0.05f, 0.08f, 0.09f, 0.85f));
                return;
            }

            GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleAndCrop, true);
        }

        private static void DrawTintedRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            _cardTitle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            _cardMeta = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft, wordWrap = true };
        }
    }
}
