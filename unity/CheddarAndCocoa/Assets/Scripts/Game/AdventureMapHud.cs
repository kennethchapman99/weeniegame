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
        private Vector2 _missionScroll;

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

            float safe = 18f;
            float width = Mathf.Min(920f, Screen.width - safe * 2f);
            float height = Mathf.Min(620f, Screen.height - safe * 2f);
            var box = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 24, box.y + 18, box.width - 48, 34), _controller.BuildHeaderLabel(), _title);
            GUI.Label(new Rect(box.x + 24, box.y + 56, box.width - 48, 44), _controller.BuildSelectedLocationLabel(), _body);

            float leftW = box.width * 0.42f;
            float y = box.y + 118;
            GUI.Label(new Rect(box.x + 24, y, leftW - 32, 24), "Locations", _body);
            for (int i = 0; i < _controller.Locations.Count; i++)
            {
                GUI.Label(new Rect(box.x + 28, y + 30 + i * 30, leftW - 40, 28), _controller.BuildLocationRowLabel(i), _small);
            }

            float missionX = box.x + leftW + 20;
            GUI.Label(new Rect(missionX, y, box.width - leftW - 44, 24), "Missions", _body);
            var missionArea = new Rect(missionX, y + 30, box.width - leftW - 44, 280);
            List<string> rows = _controller.BuildMissionRows();
            _missionScroll = GUI.BeginScrollView(missionArea, _missionScroll, new Rect(0, 0, missionArea.width - 20, Mathf.Max(280, rows.Count * 30)));
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
                    GUI.Label(new Rect(0, i * 30, missionArea.width - 24, 28), rows[i], _small);
            }
            GUI.EndScrollView();

            string launchText = _controller.CanLaunchSelectedMission ? "Start Mission" : "Locked";
            if (GUI.Button(new Rect(missionX, box.y + box.height - 84, 180, 34), launchText)) LaunchSelectedMission();
            GUI.Label(new Rect(box.x + 24, box.y + box.height - 42, box.width - 48, 24),
                "Controls: Left/Right location • Up/Down mission • Enter/Space/Start launch", _small);
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = true };
        }
    }
}
