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
        private GUIStyle _hud, _big, _mid, _small, _overlay;
        private Texture2D _uiKitTexture;

        public void Init(GameManager game) => _game = game;

        public static Rect FitPanel(float screenWidth, float screenHeight, float desiredWidth, float desiredHeight, float margin = 8f)
        {
            float availableWidth = Mathf.Max(1f, screenWidth - margin * 2f);
            float availableHeight = Mathf.Max(1f, screenHeight - margin * 2f);
            float width = Mathf.Min(desiredWidth, availableWidth);
            float height = Mathf.Min(desiredHeight, availableHeight);
            return new Rect((screenWidth - width) * 0.5f, (screenHeight - height) * 0.5f, width, height);
        }

        private void OnGUI()
        {
            if (_game == null) return;
            EnsureStyles();

            if (_game.IsPaused)
            {
                DrawPauseMenu();
            }
            else if (_game.MissionSelectVisible)
            {
                DrawMissionSelect();
            }
            else if (_game.SessionSummaryVisible)
            {
                DrawSessionSummary();
            }
            else
            {
                DrawGameplayHud();
            }

            DrawPlaytestModeToggle();
            if (_game.PlaytestOverlayVisible) DrawPlaytestOverlay();
        }

        private void DrawGameplayHud()
        {
            int secs = Mathf.CeilToInt(Mathf.Max(0f, _game.TimeRemaining));
            GUI.Label(new Rect(0, 8, Screen.width, 30), $"SCORE  {_game.Score}", _hud);
            GUI.Label(new Rect(0, 34, Screen.width, 26), $"Timer  {secs}s", _mid);
            string squirrelState = _game.MaxStolenFood > 0 ? $"Stolen {_game.StolenFood}/{_game.MaxStolenFood}" : "No squirrel pressure";
            GUI.Label(new Rect(0, 58, Screen.width, 24), $"MISSION: {_game.ActiveMissionName} / {_game.Phase} | {_game.BreakfastRecovered}/{_game.BreakfastGoal} {_game.MissionItemPlural} | {squirrelState}", _mid);
            GUI.Label(new Rect(0, 82, Screen.width, 24), "Move: WASD / Arrows / Sticks | Bark: Space / Enter / X | Interact: E / Right Shift / Y | F1 Overlay | F2 Audio | F3 Rumble", _small);
            GUI.Label(new Rect(0, 106, Screen.width, 24), $"Switch mission: keys 1-9, 0 ({_game.MissionSelectOptionCount} missions) | United barks: {_game.UnitedBarks} | Tug {Mathf.RoundToInt(_game.TugProgress * 100f)}% | Modifier: {_game.ActiveModifierLabel}", _mid);
            GUI.Label(new Rect(0, 130, Screen.width, 24), _game.LastScoreEventLabel, _mid);
            if (_game.ScorePopVisible)
                GUI.Label(new Rect(0, 152, Screen.width, 30), _game.LastScorePopLabel, _big);
            GUI.Label(new Rect(0, 180, Screen.width, 24), $"Objective: {_game.ObjectiveLabel}", _mid);
            GUI.Label(new Rect(0, 204, Screen.width, 24), _game.LastCue, _mid);
            if (!string.IsNullOrEmpty(_game.MissionBanner) && !_game.IsGameOver && !_game.IsLevelClear)
                GUI.Label(new Rect(0, 236, Screen.width, 34), _game.MissionBanner, _big);

            if (_game.IsGameOver || _game.IsLevelClear) DrawEndCard();
        }

        private void DrawPauseMenu()
        {
            DrawGameplayHud();
            var box = FitPanel(Screen.width, Screen.height, 520f, 250f);
            float w = box.width;
            float buttonWidth = Mathf.Min(200f, w - 40f);
            float buttonX = box.x + (w - buttonWidth) * 0.5f;
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x, box.y + 20f, w, 42f), "Pawsed", _big);
            GUI.Label(new Rect(box.x + 30f, box.y + 68f, w - 60f, 28f),
                "Escape / Start resumes the tiny dog emergency.", _mid);
            if (GUI.Button(new Rect(buttonX, box.y + 108f, buttonWidth, 32f), "Resume"))
                _game.TogglePause();
            if (GUI.Button(new Rect(buttonX, box.y + 148f, buttonWidth, 32f), "Mission Select"))
                _game.ReturnToMissionSelect();
            if (GUI.Button(new Rect(buttonX, box.y + 188f, buttonWidth, 32f), "Quit Game"))
                _game.RequestQuit();
        }

        private void DrawEndCard()
        {
            var box = FitPanel(Screen.width, Screen.height, 640f, 296f);
            float w = box.width, h = box.height;
            GUI.Box(box, GUIContent.none);
            DrawUiKitAccent(new Rect(box.x + w - 96, box.y + 14, 56, 38));
            GUI.Label(new Rect(box.x, box.y + 14, w, 40), _game.MissionBanner, _big);
            GUI.Label(new Rect(box.x, box.y + 58, w, 30), _game.EndSummaryLabel, _mid);
            GUI.Label(new Rect(box.x, box.y + 84, w, 30), _game.EndReasonLabel, _mid);
            string best = $"Session best ({_game.ActiveMissionName}): {_game.BestScoreForMission(_game.ActiveMissionVariant)}"
                + (_game.LastRoundFlawless ? "  *** FLAWLESS! ***" : "")
                + (_game.LastRoundWasBest ? "  ** NEW BEST! **" : "");
            GUI.Label(new Rect(box.x, box.y + 114, w, 28), $"Last swing: {_game.LastScoreEventLabel}   Stars: {_game.StarRating}/3", _mid);
            GUI.Label(new Rect(box.x, box.y + 140, w, 24), best, _game.LastRoundWasBest ? _hud : _small);
            GUI.Label(new Rect(box.x, box.y + 166, w, 24), $"{_game.MvpLabel}   |   {_game.SessionSummaryLabel}", _small);
            GUI.Label(new Rect(box.x, box.y + 190, w, 26), "R/Enter Replay | N/Right Shoulder Next | M/Esc Mission Select", _mid);

            float y = box.y + h - 42f;
            float gap = 12f;
            float buttonWidth = (w - 40f - gap * 2f) / 3f;
            float buttonX = box.x + 20f;
            if (GUI.Button(new Rect(buttonX, y, buttonWidth, 30), _game.EndReplayActionLabel))
                _game.Restart();
            if (GUI.Button(new Rect(buttonX + buttonWidth + gap, y, buttonWidth, 30), _game.EndNextActionLabel))
                _game.ChooseNextMission();
            if (GUI.Button(new Rect(buttonX + (buttonWidth + gap) * 2f, y, buttonWidth, 30), _game.EndMissionSelectActionLabel))
                _game.ReturnToMissionSelect();
        }

        private void DrawPlaytestOverlay()
        {
            float w = Mathf.Min(440f, Mathf.Max(1f, Screen.width - 24f));
            float h = Mathf.Min(322f, Mathf.Max(1f, Screen.height - 24f));
            var box = new Rect(Mathf.Max(12f, Screen.width - w - 12f), 12f, w, h);
            GUI.Box(box, GUIContent.none);

            int secs = Mathf.CeilToInt(Mathf.Max(0f, _game.TimeRemaining));
            GUI.Label(new Rect(box.x + 12f, box.y + 8f, w - 24f, 22f), "PLAYTEST MODE", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 32f, w - 24f, 20f), $"Mission: {_game.ActiveMissionVariant} / {_game.CurrentFlow} / {_game.Phase}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 54f, w - 24f, 20f), $"Timer: {secs}s   Score: {_game.Score}   Last score: {_game.LastScoreEventLabel}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 76f, w - 24f, 34f), $"Objective: {_game.ObjectiveLabel}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 112f, w - 24f, 20f), _game.FailPressureLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 134f, w - 24f, 20f), _game.DogPositionsLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 156f, w - 24f, 20f), _game.PlaytestCountersLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 178f, w - 24f, 20f), _game.MissionFailureSummaryLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 200f, w - 24f, 20f), $"Session: {_game.SessionMissionsPlayed} played / {_game.SessionTotalScore} score / {_game.SessionStarsEarned} stars", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 222f, w - 24f, 20f), $"Outcome: {_game.Outcome}   Rank: {_game.EndRank}   {(_game.LastRoundFlawless ? "FLAWLESS" : "")}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 244f, w - 24f, 20f), $"{_game.MvpLabel}   Flawless clears: {_game.SessionFlawlessClears}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 266f, w - 24f, 26f), $"Event: {_game.LastPlaytestEvent}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 290f, w - 24f, 20f), $"Audio: {(_game.AudioEnabled ? "on" : "off")} {_game.LastAudioCueRequested}   Rumble: {(_game.RumbleEnabled ? "on" : "off")} {_game.LastRumbleRequested}", _overlay);
        }

        private void DrawPlaytestModeToggle()
        {
            string label = _game.PlaytestModeEnabled ? "Playtest Mode: On" : "Playtest Mode: Off";
            if (GUI.Button(new Rect(12f, Screen.height - 42f, 168f, 30f), label))
                _game.TogglePlaytestOverlay();
        }

        private void DrawMissionSelect()
        {
            int count = _game.MissionSelectOptionCount;
            const int columns = 2;
            int rows = Mathf.CeilToInt(count / (float)columns);
            var box = FitPanel(Screen.width, Screen.height, 900f, 458f);
            float w = box.width;
            GUI.Box(box, GUIContent.none);
            DrawUiKitAccent(new Rect(box.x + w - 116, box.y + 16, 76, 50));
            GUI.Label(new Rect(box.x, box.y + 14, w, 42), "Cheddar + Cocoa Mission Select", _big);
            GUI.Label(new Rect(box.x + 32, box.y + 56, w - 64, 26),
                $"{_game.SessionMissionsPlayed} played • {_game.SessionUniqueMissionsCompleted}/{count} tried • {_game.SessionTotalScore} score • {_game.SessionFlawlessClears} flawless",
                _mid);
            GUI.Label(new Rect(box.x + 32, box.y + 82, w - 64, 22),
                "Up/Down chooses • Enter/Start begins • number keys 1-0 launch missions 1-10",
                _small);

            for (int i = 0; i < count; i++)
            {
                int column = i / rows;
                int row = i % rows;
                float gap = 10f;
                float columnWidth = (w - 54f - gap) * 0.5f;
                var rowRect = new Rect(box.x + 27f + column * (columnWidth + gap), box.y + 110f + row * 42f, columnWidth, 38f);
                DrawMissionRow(rowRect, i, _game.MissionVariantAt(i));
            }

            float footY = box.y + 110f + rows * 42f + 4f;
            GUI.Label(new Rect(box.x + 30, footY, w - 60, 24),
                $"{_game.SelectedMissionName} • {_game.MissionSelectDetailsFor(_game.SelectedMissionVariant)} • {_game.MissionSelectStatusFor(_game.SelectedMissionVariant)}", _mid);
            GUI.Label(new Rect(box.x + 40, footY + 24f, w - 80, 34), _game.SelectedMissionBriefing, _small);
            if (GUI.Button(new Rect(box.x + w * 0.5f - 110, footY + 58f, 220, 30), $"Start {_game.SelectedMissionName}"))
                _game.StartSelectedMission();
        }

        private void DrawMissionRow(Rect row, int index, GameManager.MissionVariant variant)
        {
            bool selected = _game.SelectedMissionIndex == index;
            string prefix = selected ? "> " : "";
            string key = index < 9 ? (index + 1).ToString() : index == 9 ? "0" : "-";
            var def = GameManager.BuildMissionDefinition(variant);
            string label = $"{prefix}{key}. {def.Name}\n{_game.MissionSelectStatusFor(variant)}";
            Color previous = GUI.color;
            if (selected) GUI.color = new Color(1f, 0.9f, 0.35f);
            if (GUI.Button(row, label)) _game.SelectMission(variant);
            GUI.color = previous;
        }

        private void DrawSessionSummary()
        {
            var box = FitPanel(Screen.width, Screen.height, 680f, 292f);
            float w = box.width, h = box.height;
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x, box.y + 18, w, 42), "Session Summary", _big);
            GUI.Label(new Rect(box.x + 40, box.y + 76, w - 80, 32), _game.SessionSummaryLabel, _mid);
            GUI.Label(new Rect(box.x + 40, box.y + 116, w - 80, 72), _game.SessionRanksEarnedLabel, _small);
            GUI.Label(new Rect(box.x + 40, box.y + 188, w - 80, 28), "Enter / Start continues • M / Escape opens mission select", _mid);

            float buttonGap = 10f;
            float buttonWidth = (w - 40f - buttonGap * 2f) / 3f;
            float buttonStart = box.x + (w - buttonWidth * 3f - buttonGap * 2f) * 0.5f;
            if (GUI.Button(new Rect(buttonStart, box.y + h - 44, buttonWidth, 30), "Continue Session"))
                _game.ContinueSession();
            if (GUI.Button(new Rect(buttonStart + buttonWidth + buttonGap, box.y + h - 44, buttonWidth, 30), "Mission Select"))
                _game.ReturnToMissionSelect();
            if (GUI.Button(new Rect(buttonStart + (buttonWidth + buttonGap) * 2f, box.y + h - 44, buttonWidth, 30), "New Session"))
            {
                _game.ResetSession();
                _game.ReturnToMissionSelect();
            }
        }

        private void DrawUiKitAccent(Rect rect)
        {
            if (_uiKitTexture == null) return;

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.38f);
            GUI.DrawTexture(rect, _uiKitTexture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (_hud != null) return;
            _uiKitTexture = ArenaDraftArt.LoadTexture(ArenaDraftArt.SpriteId.UiKit);
            _hud = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _hud.normal.textColor = Color.white;
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            _mid.normal.textColor = Color.white;
            _mid.wordWrap = true;
            _small = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            _small.normal.textColor = new Color(0.9f, 0.95f, 1f);
            _small.wordWrap = true;
            _overlay = new GUIStyle(_small) { alignment = TextAnchor.MiddleLeft };
            _big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _big.normal.textColor = new Color(1f, 0.95f, 0.4f);
        }
    }
}
