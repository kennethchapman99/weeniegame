using UnityEngine;
using UnityEngine.InputSystem;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// IMGUI overlay for the playable arena: the shared score + countdown while playing, and a
    /// final-score / restart card on game over. OnGUI keeps it Canvas/font-free (matches the existing
    /// DebugHud), which is plenty for a prototype. Reads the <see cref="GameManager"/>; never mutates
    /// round state except by calling Restart on the player's request (logic/render split).
    /// </summary>
    public sealed class ArenaHud : MonoBehaviour
    {
        private GameManager _game;
        private GUIStyle _hud, _big, _mid;

        public void Init(GameManager game) => _game = game;

        private void Update()
        {
            if (_game == null || (!_game.IsGameOver && !_game.IsLevelClear)) return;

            // Restart on R, gamepad Start, or Enter so any input device can play again.
            bool restart = false;
            var kb = Keyboard.current;
            if (kb != null) restart |= kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
            var pad = Gamepad.current;
            if (pad != null) restart |= pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame;
            if (restart) _game.Restart();
        }

        private void OnGUI()
        {
            if (_game == null) return;
            EnsureStyles();

            // Score + timer, top-center.
            int secs = Mathf.CeilToInt(Mathf.Max(0f, _game.TimeRemaining));
            GUI.Label(new Rect(0, 8, Screen.width, 30), $"SCORE  {_game.Score}", _hud);
            GUI.Label(new Rect(0, 34, Screen.width, 26), $"⏱  {secs}s", _mid);
            GUI.Label(new Rect(0, 58, Screen.width, 24), $"Mission: {_game.Phase} | {_game.BreakfastRecovered}/{_game.BreakfastGoal} Breakfast/Weenies | Stolen {_game.StolenFood}/{_game.MaxStolenFood}", _mid);
            GUI.Label(new Rect(0, 82, Screen.width, 24), $"United barks: {_game.UnitedBarks} | Tug {Mathf.RoundToInt(_game.TugProgress * 100f)}% | Modifier: {_game.ActiveModifierLabel}", _mid);
            GUI.Label(new Rect(0, 106, Screen.width, 24), _game.LastScoreEventLabel, _mid);
            GUI.Label(new Rect(0, 130, Screen.width, 24), _game.LastCue, _mid);
            if (!string.IsNullOrEmpty(_game.MissionBanner) && !_game.IsGameOver && !_game.IsLevelClear)
                GUI.Label(new Rect(0, 162, Screen.width, 34), _game.MissionBanner, _big);

            if (_game.IsGameOver || _game.IsLevelClear)
            {
                float w = 520, h = 178;
                var box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
                GUI.Box(box, GUIContent.none);
                GUI.Label(new Rect(box.x, box.y + 14, w, 40), _game.MissionBanner, _big);
                GUI.Label(new Rect(box.x, box.y + 58, w, 30), _game.EndSummaryLabel, _mid);
                GUI.Label(new Rect(box.x, box.y + 88, w, 28), $"Last swing: {_game.LastScoreEventLabel}   Stars: {_game.StarRating}/3", _mid);
                GUI.Label(new Rect(box.x, box.y + 118, w, 26), _game.ReplayPromptLabel, _mid);

                if (GUI.Button(new Rect(box.x + w * 0.5f - 70, box.y + h - 36, 140, 28), "Replay"))
                    _game.Restart();
            }
        }

        private void EnsureStyles()
        {
            if (_hud != null) return;
            _hud = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _hud.normal.textColor = Color.white;
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            _mid.normal.textColor = Color.white;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _big.normal.textColor = new Color(1f, 0.95f, 0.4f);
        }
    }
}
