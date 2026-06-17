using UnityEngine;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// IMGUI overlay for the playable arena: mission select, the shared score + countdown while
    /// playing, end actions, and a session summary. OnGUI keeps it Canvas/font-free (matches the
    /// existing DebugHud), which is plenty for a prototype. Game flow mutations are delegated to
    /// <see cref="GameManager"/> so tests can assert the same paths the buttons use.
    /// </summary>
    public sealed class ArenaHud : MonoBehaviour
    {
        private GameManager _game;
        private GUIStyle _hud, _big, _mid, _small;

        public void Init(GameManager game) => _game = game;

        private void OnGUI()
        {
            if (_game == null) return;
            EnsureStyles();

            if (_game.MissionSelectVisible)
            {
                DrawMissionSelect();
                return;
            }

            if (_game.SessionSummaryVisible)
            {
                DrawSessionSummary();
                return;
            }

            // Score + timer, top-center.
            int secs = Mathf.CeilToInt(Mathf.Max(0f, _game.TimeRemaining));
            GUI.Label(new Rect(0, 8, Screen.width, 30), $"SCORE  {_game.Score}", _hud);
            GUI.Label(new Rect(0, 34, Screen.width, 26), $"⏱  {secs}s", _mid);
            string squirrelState = _game.MaxStolenFood > 0 ? $"Stolen {_game.StolenFood}/{_game.MaxStolenFood}" : "No squirrel pressure";
            GUI.Label(new Rect(0, 58, Screen.width, 24), $"MISSION: {_game.ActiveMissionName} / {_game.Phase} | {_game.BreakfastRecovered}/{_game.BreakfastGoal} {_game.MissionItemPlural} | {squirrelState}", _mid);
            GUI.Label(new Rect(0, 82, Screen.width, 24), "Move: WASD / Arrows / Sticks | Bark: Space / Enter / X | Tug/Rescue: Y / Right Shift | End: R Replay, N Next, M Missions", _small);
            GUI.Label(new Rect(0, 106, Screen.width, 24), $"1 Backyard  2 Snack  3 Sock | United barks: {_game.UnitedBarks} | Tug {Mathf.RoundToInt(_game.TugProgress * 100f)}% | Modifier: {_game.ActiveModifierLabel}", _mid);
            GUI.Label(new Rect(0, 130, Screen.width, 24), _game.LastScoreEventLabel, _mid);
            if (_game.ScorePopVisible)
                GUI.Label(new Rect(0, 152, Screen.width, 30), _game.LastScorePopLabel, _big);
            GUI.Label(new Rect(0, 180, Screen.width, 24), $"Objective: {_game.ObjectiveLabel}", _mid);
            GUI.Label(new Rect(0, 204, Screen.width, 24), _game.LastCue, _mid);
            if (!string.IsNullOrEmpty(_game.MissionBanner) && !_game.IsGameOver && !_game.IsLevelClear)
                GUI.Label(new Rect(0, 236, Screen.width, 34), _game.MissionBanner, _big);

            if (_game.IsGameOver || _game.IsLevelClear)
            {
                float w = 640, h = 268;
                var box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
                GUI.Box(box, GUIContent.none);
                GUI.Label(new Rect(box.x, box.y + 14, w, 40), _game.MissionBanner, _big);
                GUI.Label(new Rect(box.x, box.y + 58, w, 30), _game.EndSummaryLabel, _mid);
                GUI.Label(new Rect(box.x, box.y + 84, w, 30), _game.EndReasonLabel, _mid);
                GUI.Label(new Rect(box.x, box.y + 114, w, 28), $"Last swing: {_game.LastScoreEventLabel}   Stars: {_game.StarRating}/3", _mid);
                GUI.Label(new Rect(box.x, box.y + 142, w, 24), _game.SessionSummaryLabel, _small);
                GUI.Label(new Rect(box.x, box.y + 166, w, 26), "R/Enter Replay | N/Right Shoulder Next | M/Esc Mission Select", _mid);

                float y = box.y + h - 42;
                if (GUI.Button(new Rect(box.x + 70, y, 150, 30), _game.EndReplayActionLabel))
                    _game.Restart();
                if (GUI.Button(new Rect(box.x + 245, y, 150, 30), _game.EndNextActionLabel))
                    _game.ChooseNextMission();
                if (GUI.Button(new Rect(box.x + 420, y, 150, 30), _game.EndMissionSelectActionLabel))
                    _game.ReturnToMissionSelect();
            }
        }

        private void DrawMissionSelect()
        {
            float w = 680, h = 360;
            var box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x, box.y + 16, w, 42), "Cheddar + Cocoa Mission Select", _big);
            GUI.Label(new Rect(box.x + 40, box.y + 60, w - 80, 48),
                "Pick a dog emergency. Up/Down chooses, Enter/Start begins, or press 1/2/3 for a mission.",
                _mid);

            DrawMissionRow(box, 0, GameManager.MissionVariant.BackyardRescue, "Backyard Rescue", "Protect weenies, bark off squirrel crime, huddle against the shadow, finish the rope tug.");
            DrawMissionRow(box, 1, GameManager.MissionVariant.SnackHeist, "Snack Heist", "Stash forbidden snacks before the squirrel union steals too much evidence.");
            DrawMissionRow(box, 2, GameManager.MissionVariant.SockPanic, "Sock Panic", "Return scattered socks before laundry order returns.");

            GUI.Label(new Rect(box.x + 40, box.y + 264, w - 80, 42), _game.SelectedMissionBriefing, _small);
            if (GUI.Button(new Rect(box.x + w * 0.5f - 90, box.y + 312, 180, 30), $"Start {_game.SelectedMissionName}"))
                _game.StartSelectedMission();
        }

        private void DrawMissionRow(Rect box, int index, GameManager.MissionVariant variant, string title, string briefing)
        {
            float y = box.y + 120 + index * 46;
            bool selected = _game.SelectedMissionIndex == index;
            string prefix = selected ? "> " : "  ";
            GUI.Label(new Rect(box.x + 48, y, 180, 26), $"{prefix}{index + 1}. {title}", selected ? _hud : _mid);
            GUI.Label(new Rect(box.x + 230, y + 2, 320, 24), briefing, _small);
            if (GUI.Button(new Rect(box.x + 560, y, 80, 26), "Start"))
                _game.StartMission(variant);
        }

        private void DrawSessionSummary()
        {
            float w = 680, h = 270;
            var box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x, box.y + 18, w, 42), "Session Summary", _big);
            GUI.Label(new Rect(box.x + 40, box.y + 76, w - 80, 32), _game.SessionSummaryLabel, _mid);
            GUI.Label(new Rect(box.x + 40, box.y + 116, w - 80, 72), _game.SessionRanksEarnedLabel, _small);
            GUI.Label(new Rect(box.x + 40, box.y + 188, w - 80, 28), "Enter / Start / M returns to mission select for another tiny dog crisis.", _mid);

            if (GUI.Button(new Rect(box.x + w * 0.5f - 100, box.y + h - 44, 200, 30), "Mission Select"))
                _game.ReturnToMissionSelect();
        }

        private void EnsureStyles()
        {
            if (_hud != null) return;
            _hud = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _hud.normal.textColor = Color.white;
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            _mid.normal.textColor = Color.white;
            _mid.wordWrap = true;
            _small = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            _small.normal.textColor = new Color(0.9f, 0.95f, 1f);
            _small.wordWrap = true;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _big.normal.textColor = new Color(1f, 0.95f, 0.4f);
        }
    }
}
