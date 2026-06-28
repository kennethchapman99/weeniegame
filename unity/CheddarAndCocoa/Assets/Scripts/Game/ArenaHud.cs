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
        private GUIStyle _hud, _big, _mid, _small, _overlay, _briefing;
        private Texture2D _uiKitTexture;
        public const string PlayerOwnershipLabel = "P1 Cheddar: WASD + Space/E  |  P2 Cocoa: Arrows + Enter/Right Shift";
        public const string PadControlsLabel = "Pads: left stick moves  |  X / West barks  |  Y / North interacts";

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
            GUI.Label(new Rect(0, 82, Screen.width, 24), $"{PlayerOwnershipLabel}  |  {PadControlsLabel}  |  F1 Overlay | F2 Audio | F3 Rumble", _small);
            GUI.Label(new Rect(0, 106, Screen.width, 24), $"Switch mission: keys 1-9, 0 ({_game.MissionSelectOptionCount} missions) | United barks: {_game.UnitedBarks} | Tug {Mathf.RoundToInt(_game.TugProgress * 100f)}% | Modifier: {_game.ActiveModifierLabel}", _mid);
            GUI.Label(new Rect(0, 130, Screen.width, 24), _game.LastScoreEventLabel, _mid);
            if (_game.ScorePopVisible)
                GUI.Label(new Rect(0, 152, Screen.width, 30), _game.LastScorePopLabel, _big);
            GUI.Label(new Rect(0, 180, Screen.width, 24), $"Objective: {_game.ObjectiveLabel}", _mid);
            GUI.Label(new Rect(0, 204, Screen.width, 24), _game.TeamGuidanceLabel, _small);
            GUI.Label(new Rect(0, 228, Screen.width, 24), _game.LastCue, _mid);
            if (!string.IsNullOrEmpty(_game.MissionBanner) && !_game.IsGameOver && !_game.IsLevelClear && !_game.MissionBriefingVisible)
                GUI.Label(new Rect(0, 260, Screen.width, 34), _game.MissionBanner, _big);

            if (_game.MissionBriefingVisible) DrawMissionBriefing();

            if (_game.IsGameOver || _game.IsLevelClear) DrawEndCard();
        }

        private void DrawMissionBriefing()
        {
            var box = FitPanel(Screen.width, Screen.height, 760f, 210f);
            box.y = Mathf.Min(box.y, 304f);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 24f, box.y + 12f, box.width - 48f, 34f), _game.ActiveMissionName, _big);
            GUI.Label(new Rect(box.x + 32f, box.y + 48f, box.width - 64f, 68f),
                $"GOAL: {_game.MissionIntroPrompt}", _briefing);
            GUI.Label(new Rect(box.x + 32f, box.y + 118f, box.width - 64f, 26f),
                $"FIRST: {_game.ObjectiveLabel}", _hud);
            GUI.Label(new Rect(box.x + 32f, box.y + 148f, box.width - 64f, 22f),
                PlayerOwnershipLabel,
                _small);
            GUI.Label(new Rect(box.x + 32f, box.y + 174f, box.width - 64f, 22f),
                $"Follow each dog's arrow  |  {PadControlsLabel}",
                _small);
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
            var box = FitPanel(Screen.width, Screen.height, 640f, 318f);
            float w = box.width, h = box.height;
            GUI.Box(box, GUIContent.none);
            DrawUiKitAccent(new Rect(box.x + w - 96, box.y + 14, 56, 38));
            GUI.Label(new Rect(box.x, box.y + 14, w, 40), _game.MissionBanner, _big);
            GUI.Label(new Rect(box.x, box.y + 58, w, 30), _game.EndSummaryLabel, _mid);
            GUI.Label(new Rect(box.x, box.y + 84, w, 30), _game.EndReasonLabel, _mid);
            string best = $"Session best ({_game.ActiveMissionName}): {_game.BestScoreForMission(_game.ActiveMissionVariant)}"
                + (_game.LastRoundFlawless ? $"  •  {_game.FlawlessRivalryLabel}" : "")
                + (_game.LastRoundWasBest ? "  ** NEW BEST! **" : "");
            GUI.Label(new Rect(box.x, box.y + 114, w, 28), $"Last swing: {_game.LastScoreEventLabel}   Stars: {_game.StarRating}/3", _mid);
            GUI.Label(new Rect(box.x, box.y + 140, w, 24), best, _game.LastRoundWasBest ? _hud : _small);
            GUI.Label(new Rect(box.x, box.y + 166, w, 24), $"{_game.MvpLabel}   |   {_game.SessionSummaryLabel}", _small);
            GUI.Label(new Rect(box.x, box.y + 190, w, 24), _game.EndChallengeLabel, _small);
            GUI.Label(new Rect(box.x, box.y + 214, w, 26), "R/Enter Replay | N/Right Shoulder Next | M/Esc Mission Select", _mid);

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
            float h = Mathf.Min(388f, Mathf.Max(1f, Screen.height - 24f));
            var box = new Rect(Mathf.Max(12f, Screen.width - w - 12f), 12f, w, h);
            GUI.Box(box, GUIContent.none);

            int secs = Mathf.CeilToInt(Mathf.Max(0f, _game.TimeRemaining));
            GUI.Label(new Rect(box.x + 12f, box.y + 8f, w - 24f, 22f), "PLAYTEST MODE", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 32f, w - 24f, 20f), $"Mission: {_game.ActiveMissionVariant} / {_game.CurrentFlow} / {_game.Phase}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 54f, w - 24f, 20f), $"Timer: {secs}s   Score: {_game.Score}   Last score: {_game.LastScoreEventLabel}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 76f, w - 24f, 34f), $"Objective: {_game.ObjectiveLabel}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 112f, w - 24f, 20f), $"Guidance: {_game.TeamGuidanceLabel}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 134f, w - 24f, 20f), _game.FailPressureLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 156f, w - 24f, 20f), _game.DogPositionsLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 178f, w - 24f, 20f), _game.PlaytestCountersLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 200f, w - 24f, 20f), _game.MissionFailureSummaryLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 222f, w - 24f, 20f), $"Session: {_game.SessionMissionsPlayed} played / {_game.SessionTotalScore} score / {_game.SessionStarsEarned} stars", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 244f, w - 24f, 20f), $"Outcome: {_game.Outcome}   Rank: {_game.EndRank}   {(_game.LastRoundFlawless ? "FLAWLESS" : "")}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 266f, w - 24f, 20f), $"{_game.MvpLabel}   Flawless clears: {_game.SessionFlawlessClears}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 288f, w - 24f, 26f), $"Event: {_game.LastPlaytestEvent}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 312f, w - 24f, 20f), $"Audio: {(_game.AudioEnabled ? "on" : "off")} {_game.LastAudioCueRequested}   Rumble: {(_game.RumbleEnabled ? "on" : "off")} {_game.LastRumbleRequested}", _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 334f, w - 24f, 20f), _game.DemoReadinessLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 356f, w - 24f, 20f), _game.PlaytestHotkeysLabel, _overlay);
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
            var box = FitPanel(Screen.width, Screen.height, 900f, MissionSelectPanelHeight(count));
            float w = box.width;
            GUI.Box(box, GUIContent.none);
            DrawTintedRect(new Rect(box.x + 12f, box.y + 10f, w - 24f, 92f), new Color(0.04f, 0.08f, 0.1f, 0.62f));
            DrawTintedRect(new Rect(box.x + 20f, box.y + 108f, w - 40f, rows * 42f + 4f), new Color(0.02f, 0.04f, 0.05f, 0.42f));
            DrawUiKitAccent(new Rect(box.x + w - 116, box.y + 16, 76, 50));
            GUI.Label(new Rect(box.x, box.y + 14, w, 42), "Cheddar + Cocoa Couch Missions", _big);
            GUI.Label(new Rect(box.x + 32, box.y + 56, w - 64, 26),
                $"{_game.SessionMissionsPlayed} played • {_game.SessionUniqueMissionsCompleted}/{count} tried • {_game.SessionTotalScore} score • {_game.SessionFlawlessClears} flawless",
                _mid);
            GUI.Label(new Rect(box.x + 32, box.y + 82, w - 64, 22),
                $"{_game.CouchTestFocusLabel} • arrows/D-pad move • Enter/Start begins",
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
            DrawTintedRect(new Rect(box.x + 20f, footY - 4f, w - 40f, 128f), new Color(0.08f, 0.13f, 0.14f, 0.7f));
            DrawMissionBadge(new Rect(box.x + 34f, footY + 12f, 58f, 58f), _game.SelectedMissionVariant, true);
            GUI.Label(new Rect(box.x + 100, footY, w - 130, 24),
                $"{_game.SelectedMissionName} • {_game.MissionSelectDetailsFor(_game.SelectedMissionVariant)} • {_game.MissionSelectStatusFor(_game.SelectedMissionVariant)}", _mid);
            GUI.Label(new Rect(box.x + 104, footY + 24f, w - 144, 70), $"GOAL: {_game.SelectedMissionBriefing}", _briefing);
            GUI.Label(new Rect(box.x + 104f, footY + 94f, w * 0.5f - 126f, 26f),
                _game.SelectedMissionChallengeLabel, _small);
            float startWidth = Mathf.Min(240f, Mathf.Max(120f, (w - 52f) * 0.5f));
            float focusWidth = Mathf.Min(238f, Mathf.Max(120f, (w - 52f) * 0.5f));
            float startX = box.x + w - 20f - startWidth;
            float focusX = startX - 12f - focusWidth;
            if (_game.SelectedMissionVariant != _game.CouchTestFocusVariant
                && GUI.Button(new Rect(focusX, footY + 94f, focusWidth, 32f), "Highlight Couch Test"))
                _game.SelectCouchTestFocusMission();
            if (GUI.Button(new Rect(startX, footY + 94f, startWidth, 32), $"Start {_game.SelectedMissionName}"))
                _game.StartSelectedMission();
        }

        public static float MissionSelectPanelHeight(int missionCount)
        {
            int rows = Mathf.CeilToInt(Mathf.Max(1, missionCount) / 2f);
            return 110f + rows * 42f + 142f;
        }

        private void DrawMissionRow(Rect row, int index, GameManager.MissionVariant variant)
        {
            bool selected = _game.SelectedMissionIndex == index;
            string prefix = selected ? "> " : "";
            string key = index < 9 ? (index + 1).ToString() : index == 9 ? "0" : "-";
            var def = GameManager.BuildMissionDefinition(variant);
            string label = $"{prefix}{key}. {def.Name}\n{_game.MissionSelectStatusFor(variant)}";
            Color previous = GUI.color;
            DrawTintedRect(row, selected ? new Color(1f, 0.82f, 0.18f, 0.24f) : new Color(0.1f, 0.14f, 0.16f, 0.46f));
            if (GUI.Button(row, GUIContent.none)) _game.SelectMission(variant);
            DrawMissionBadge(new Rect(row.x + 6f, row.y + 5f, 30f, 28f), variant, selected);
            if (selected) GUI.color = new Color(1f, 0.92f, 0.42f);
            GUI.Label(new Rect(row.x + 42f, row.y + 2f, row.width - 46f, row.height - 2f), label, _small);
            GUI.color = previous;
        }

        public static string MissionBadgeCodeFor(GameManager.MissionVariant variant)
        {
            return variant switch
            {
                GameManager.MissionVariant.BackyardRescue => "YRD",
                GameManager.MissionVariant.SnackHeist => "SNK",
                GameManager.MissionVariant.SockPanic => "SOX",
                GameManager.MissionVariant.SquirrelConspiracy => "SQL",
                GameManager.MissionVariant.EagleShadowPanic => "EGL",
                GameManager.MissionVariant.CoyotesFence => "FNC",
                GameManager.MissionVariant.WeenieRoundup => "BWL",
                GameManager.MissionVariant.ScentSearch => "SNT",
                GameManager.MissionVariant.ThunderstormComfort => "HUG",
                GameManager.MissionVariant.MarkTheYard => "MRK",
                GameManager.MissionVariant.LeashWalk => "LSH",
                GameManager.MissionVariant.CarRide => "CAR",
                GameManager.MissionVariant.GateCrash => "GTE",
                GameManager.MissionVariant.TableStealth => "TBL",
                GameManager.MissionVariant.SquirrelSwitcheroo => "SWP",
                GameManager.MissionVariant.WalkCampaign => "WLK",
                GameManager.MissionVariant.BoneRelay => "BNE",
                GameManager.MissionVariant.GreatEscape => "ESC",
                GameManager.MissionVariant.ChaosMachine => "MCH",
                GameManager.MissionVariant.BlanketCatch => "BLK",
                GameManager.MissionVariant.KitchenFoodFrenzy => "KIT",
                GameManager.MissionVariant.OperationPeeBreak => "PEE",
                _ => "DOG"
            };
        }

        public static Color MissionBadgeColorFor(GameManager.MissionVariant variant)
        {
            return variant switch
            {
                GameManager.MissionVariant.OperationPeeBreak => new Color(0.18f, 0.75f, 0.95f, 0.92f),
                GameManager.MissionVariant.KitchenFoodFrenzy => new Color(1f, 0.68f, 0.25f, 0.92f),
                GameManager.MissionVariant.SnackHeist => new Color(0.95f, 0.48f, 0.18f, 0.92f),
                GameManager.MissionVariant.SockPanic => new Color(0.48f, 0.68f, 1f, 0.92f),
                GameManager.MissionVariant.CoyotesFence => new Color(0.72f, 0.48f, 0.22f, 0.92f),
                GameManager.MissionVariant.EagleShadowPanic => new Color(0.28f, 0.32f, 0.42f, 0.92f),
                GameManager.MissionVariant.LeashWalk => new Color(0.25f, 0.82f, 0.78f, 0.92f),
                _ => new Color(0.28f, 0.55f, 0.32f, 0.92f)
            };
        }

        private void DrawMissionBadge(Rect rect, GameManager.MissionVariant variant, bool selected)
        {
            DrawTintedRect(rect, MissionBadgeColorFor(variant));
            float inset = selected ? 4f : 5f;
            DrawTintedRect(new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f),
                selected ? new Color(0.02f, 0.04f, 0.05f, 0.84f) : new Color(0.02f, 0.04f, 0.05f, 0.62f));
            GUI.Label(rect, MissionBadgeCodeFor(variant), selected ? _hud : _small);
        }

        private static void DrawTintedRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
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
            string continuePrompt = _game.SessionAllMissionsCompleted ? "begins a Victory Lap" : "continues";
            GUI.Label(new Rect(box.x + 40, box.y + 188, w - 80, 28), $"Enter / Start {continuePrompt} • M / Escape opens mission select", _mid);

            float buttonGap = 10f;
            float buttonWidth = (w - 40f - buttonGap * 2f) / 3f;
            float buttonStart = box.x + (w - buttonWidth * 3f - buttonGap * 2f) * 0.5f;
            if (GUI.Button(new Rect(buttonStart, box.y + h - 44, buttonWidth, 30), _game.SessionContinueActionLabel))
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
            _briefing = new GUIStyle(_mid) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _briefing.normal.textColor = Color.white;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _big.normal.textColor = new Color(1f, 0.95f, 0.4f);
        }
    }
}
