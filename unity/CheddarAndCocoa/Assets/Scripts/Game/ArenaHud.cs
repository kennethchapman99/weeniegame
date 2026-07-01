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
        private GUIStyle _hud, _big, _mid, _small, _overlay, _briefing, _resultHeadline, _resultSubtitle, _resultBody, _resultHint, _resultButton;
        private Texture2D _uiKitTexture;
        private Sprite _hudPanelFrame, _hudMissionTile, _hudMissionTileSelected, _hudBadgeFrame, _hudButtonPrimary, _hudOverlayPanel;
        public const string PlayerOwnershipLabel = "P1 Cheddar: WASD + Space/E  |  P2 Cocoa: Arrows + Enter/Right Shift";
        public const string PadControlsLabel = "Pads: left stick moves  |  X / West barks  |  Y / North interacts";
        public const int ResultHeadlineFontSize = 54;
        public const int ResultSubtitleFontSize = 32;
        public const int ResultBodyFontSize = 26;
        public const int ResultButtonFontSize = 26;
        public const int ResultHintFontSize = 18;

        public readonly struct ResultOverlayLayout
        {
            public readonly Rect Backdrop;
            public readonly Rect Card;
            public readonly Rect Headline;
            public readonly Rect Subtitle;
            public readonly Rect Score;
            public readonly Rect Flavor;
            public readonly Rect Challenge;
            public readonly Rect[] Buttons;
            public readonly Rect[] ButtonHints;

            public ResultOverlayLayout(
                Rect backdrop,
                Rect card,
                Rect headline,
                Rect subtitle,
                Rect score,
                Rect flavor,
                Rect challenge,
                Rect[] buttons,
                Rect[] buttonHints)
            {
                Backdrop = backdrop;
                Card = card;
                Headline = headline;
                Subtitle = subtitle;
                Score = score;
                Flavor = flavor;
                Challenge = challenge;
                Buttons = buttons;
                ButtonHints = buttonHints;
            }
        }

        public bool GeneratedHudSkinLoaded =>
            _hudPanelFrame != null && _hudMissionTile != null && _hudMissionTileSelected != null &&
            _hudBadgeFrame != null && _hudButtonPrimary != null && _hudOverlayPanel != null;

        public static bool GeneratedHudSkinAvailable
        {
            get
            {
                foreach (string path in FinalGameplayArt.HudSkinPack)
                    if (!FinalGameplayArt.Has(path)) return false;
                return true;
            }
        }

        public void Init(GameManager game) => _game = game;
        public void WarmGeneratedHudSkinForTests() => LoadGeneratedHudSkin();

        public static Rect FitPanel(float screenWidth, float screenHeight, float desiredWidth, float desiredHeight, float margin = 8f)
        {
            float availableWidth = Mathf.Max(1f, screenWidth - margin * 2f);
            float availableHeight = Mathf.Max(1f, screenHeight - margin * 2f);
            float width = Mathf.Min(desiredWidth, availableWidth);
            float height = Mathf.Min(desiredHeight, availableHeight);
            return new Rect((screenWidth - width) * 0.5f, (screenHeight - height) * 0.5f, width, height);
        }

        public static ResultOverlayLayout BuildResultOverlayLayout(float screenWidth, float screenHeight)
        {
            float margin = Mathf.Max(18f, Mathf.Min(screenWidth, screenHeight) * 0.035f);
            float cardWidth = Mathf.Clamp(screenWidth * 0.78f, 620f, screenWidth - margin * 2f);
            float cardHeight = Mathf.Clamp(screenHeight * 0.66f, 420f, screenHeight - margin * 2f);
            Rect card = FitPanel(screenWidth, screenHeight, cardWidth, cardHeight, margin);
            float pad = Mathf.Clamp(card.width * 0.055f, 28f, 56f);
            float innerX = card.x + pad;
            float innerW = card.width - pad * 2f;
            float y = card.y + pad;
            Rect headline = new Rect(innerX, y, innerW, 72f);
            y += headline.height + 12f;
            Rect subtitle = new Rect(innerX, y, innerW, 46f);
            y += subtitle.height + 16f;
            Rect score = new Rect(innerX, y, innerW, 72f);
            y += score.height + 12f;
            Rect flavor = new Rect(innerX, y, innerW, 40f);
            y += flavor.height + 8f;
            Rect challenge = new Rect(innerX, y, innerW, 34f);

            float buttonAreaBottom = card.yMax - pad;
            float buttonHintHeight = 24f;
            float buttonHeight = 58f;
            float buttonGap = Mathf.Clamp(card.width * 0.025f, 14f, 26f);
            float buttonWidth = (innerW - buttonGap * 2f) / 3f;
            float buttonY = buttonAreaBottom - buttonHintHeight - 6f - buttonHeight;
            var buttons = new Rect[3];
            var hints = new Rect[3];
            for (int i = 0; i < 3; i++)
            {
                float x = innerX + i * (buttonWidth + buttonGap);
                buttons[i] = new Rect(x, buttonY, buttonWidth, buttonHeight);
                hints[i] = new Rect(x, buttonY + buttonHeight + 5f, buttonWidth, buttonHintHeight);
            }

            return new ResultOverlayLayout(
                new Rect(0f, 0f, screenWidth, screenHeight),
                card,
                headline,
                subtitle,
                score,
                flavor,
                challenge,
                buttons,
                hints);
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

            if (!_game.EndScreenVisible && !_game.SessionSummaryVisible)
            {
                DrawPlaytestModeToggle();
                if (_game.PlaytestOverlayVisible) DrawPlaytestOverlay();
            }
        }

        private void DrawGameplayHud()
        {
            if (_game.EndScreenVisible)
            {
                DrawEndCard();
                return;
            }

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

        }

        private void DrawMissionBriefing()
        {
            var box = FitPanel(Screen.width, Screen.height, 760f, 252f);
            box.y = Mathf.Min(box.y, 304f);
            DrawHudPanel(box);
            GUI.Label(new Rect(box.x + 24f, box.y + 12f, box.width - 48f, 34f), _game.ActiveMissionName, _big);
            GUI.Label(new Rect(box.x + 32f, box.y + 46f, box.width - 64f, 22f),
                _game.MissionPresentationLine,
                _small);
            GUI.Label(new Rect(box.x + 32f, box.y + 70f, box.width - 64f, 62f),
                $"GOAL: {_game.MissionIntroPrompt}", _briefing);
            GUI.Label(new Rect(box.x + 32f, box.y + 136f, box.width - 64f, 26f),
                $"FIRST: {_game.ObjectiveLabel}", _hud);
            GUI.Label(new Rect(box.x + 32f, box.y + 166f, box.width - 64f, 22f),
                $"ROLES: {_game.MissionRoleHint}",
                _small);
            GUI.Label(new Rect(box.x + 32f, box.y + 194f, box.width - 64f, 22f),
                PlayerOwnershipLabel,
                _small);
            GUI.Label(new Rect(box.x + 32f, box.y + 220f, box.width - 64f, 22f),
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
            DrawHudPanel(box);
            GUI.Label(new Rect(box.x, box.y + 20f, w, 42f), "Pawsed", _big);
            GUI.Label(new Rect(box.x + 30f, box.y + 68f, w - 60f, 28f),
                "Escape / Start resumes the tiny dog emergency.", _mid);
            if (DrawSkinnedButton(new Rect(buttonX, box.y + 108f, buttonWidth, 32f), "Resume"))
                _game.TogglePause();
            if (DrawSkinnedButton(new Rect(buttonX, box.y + 148f, buttonWidth, 32f), "Mission Select"))
                _game.ReturnToMissionSelect();
            if (DrawSkinnedButton(new Rect(buttonX, box.y + 188f, buttonWidth, 32f), "Quit Game"))
                _game.RequestQuit();
        }

        private void DrawEndCard()
        {
            var layout = BuildResultOverlayLayout(Screen.width, Screen.height);
            DrawTintedRect(layout.Backdrop, new Color(0f, 0f, 0f, 0.72f));
            DrawHudOverlay(layout.Card);
            DrawTintedRect(layout.Card, new Color(0.015f, 0.025f, 0.03f, 0.92f));
            DrawTintedRect(new Rect(layout.Card.x, layout.Card.y, layout.Card.width, 9f),
                MissionBadgeColorFor(_game.ActiveMissionVariant));
            DrawUiKitAccent(new Rect(layout.Card.xMax - 122f, layout.Card.y + 22f, 78f, 52f));

            GUI.Label(layout.Headline, _game.EndHeadlineLabel, _resultHeadline);
            GUI.Label(layout.Subtitle, _game.EndRank, _resultSubtitle);
            GUI.Label(layout.Score, $"{_game.EndScoreLabel}\n{_game.EndBestScoreLabel}", _resultBody);
            GUI.Label(layout.Flavor, _game.EndReasonLabel, _resultBody);
            GUI.Label(layout.Challenge, _game.EndChallengeLabel, _resultHint);

            if (DrawResultButton(layout.Buttons[0], _game.EndReplayActionLabel, true))
                _game.Restart();
            if (DrawResultButton(layout.Buttons[1], _game.EndNextActionLabel, false))
                _game.ChooseNextMission();
            if (DrawResultButton(layout.Buttons[2], _game.EndMissionSelectActionLabel, false))
                _game.ReturnToMissionSelect();

            GUI.Label(layout.ButtonHints[0], "R / Enter / Start", _resultHint);
            GUI.Label(layout.ButtonHints[1], "N / Right Shoulder", _resultHint);
            GUI.Label(layout.ButtonHints[2], "M / Escape", _resultHint);
        }

        private void DrawPlaytestOverlay()
        {
            float w = Mathf.Min(440f, Mathf.Max(1f, Screen.width - 24f));
            float h = Mathf.Min(410f, Mathf.Max(1f, Screen.height - 24f));
            var box = new Rect(Mathf.Max(12f, Screen.width - w - 12f), 12f, w, h);
            DrawHudOverlay(box);

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
            GUI.Label(new Rect(box.x + 12f, box.y + 334f, w - 24f, 20f), _game.ActiveMissionReadinessLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 356f, w - 24f, 20f), _game.DemoReadinessLabel, _overlay);
            GUI.Label(new Rect(box.x + 12f, box.y + 378f, w - 24f, 20f), _game.PlaytestHotkeysLabel, _overlay);
        }

        private void DrawPlaytestModeToggle()
        {
            string label = _game.PlaytestModeEnabled ? "Playtest Mode: On" : "Playtest Mode: Off";
            if (DrawSkinnedButton(new Rect(12f, Screen.height - 42f, 168f, 30f), label, _small))
                _game.TogglePlaytestOverlay();
        }

        private void DrawMissionSelect()
        {
            int count = _game.MissionSelectOptionCount;
            const int columns = 2;
            int rows = Mathf.CeilToInt(count / (float)columns);
            var box = FitPanel(Screen.width, Screen.height, 900f, MissionSelectPanelHeight(count));
            float w = box.width;
            DrawHudPanel(box);
            DrawTintedRect(new Rect(box.x + 12f, box.y + 10f, w - 24f, 92f), new Color(0.04f, 0.08f, 0.1f, 0.62f));
            DrawTintedRect(new Rect(box.x + 20f, box.y + 124f, w - 40f, rows * 42f + 4f), new Color(0.02f, 0.04f, 0.05f, 0.42f));
            DrawUiKitAccent(new Rect(box.x + w - 116, box.y + 16, 76, 50));
            GUI.Label(new Rect(box.x, box.y + 14, w, 42), "Cheddar + Cocoa Couch Missions", _big);
            GUI.Label(new Rect(box.x + 32, box.y + 56, w - 64, 26),
                $"{_game.SessionMissionsPlayed} played • {_game.SessionUniqueMissionsCompleted}/{count} tried • {_game.SessionTotalScore} score • {_game.SessionFlawlessClears} flawless",
                _mid);
            GUI.Label(new Rect(box.x + 32, box.y + 82, w - 64, 22),
                $"{_game.CouchTestFocusLabel} • arrows/D-pad move • Enter/Start begins",
                _small);
            GUI.Label(new Rect(box.x + 32, box.y + 102, w - 64, 18),
                _game.FamilyShowcaseShortcutLabel,
                _small);

            for (int i = 0; i < count; i++)
            {
                int column = i / rows;
                int row = i % rows;
                float gap = 10f;
                float columnWidth = (w - 54f - gap) * 0.5f;
                var rowRect = new Rect(box.x + 27f + column * (columnWidth + gap), box.y + 126f + row * 42f, columnWidth, 38f);
                DrawMissionRow(rowRect, i, _game.MissionVariantAt(i));
            }

            float footY = box.y + 126f + rows * 42f + 4f;
            DrawSelectedMissionShowcase(new Rect(box.x + 20f, footY - 4f, w - 40f, 128f), _game.SelectedMissionVariant);
            DrawMissionBadge(new Rect(box.x + 34f, footY + 12f, 58f, 58f), _game.SelectedMissionVariant, true);
            GUI.Label(new Rect(box.x + 100, footY, w - 130, 42),
                $"{_game.SelectedMissionName} • {_game.MissionSelectDetailsFor(_game.SelectedMissionVariant)} • {_game.MissionSelectStatusFor(_game.SelectedMissionVariant)}\n{_game.SelectedMissionPresentationLine}", _mid);
            GUI.Label(new Rect(box.x + 104, footY + 42f, w - 144, 52), $"GOAL: {_game.SelectedMissionBriefing}", _briefing);
            GUI.Label(new Rect(box.x + 104f, footY + 94f, w * 0.5f - 126f, 26f),
                _game.SelectedMissionChallengeLabel, _small);
            GUI.Label(new Rect(box.x + 104f, footY + 116f, w * 0.5f - 126f, 20f),
                _game.SelectedMissionReadinessLabel, _small);
            float startWidth = Mathf.Min(240f, Mathf.Max(120f, (w - 52f) * 0.5f));
            float focusWidth = Mathf.Min(238f, Mathf.Max(120f, (w - 52f) * 0.5f));
            float startX = box.x + w - 20f - startWidth;
            float focusX = startX - 12f - focusWidth;
            if (_game.SelectedMissionVariant != _game.CouchTestFocusVariant
                && DrawSkinnedButton(new Rect(focusX, footY + 94f, focusWidth, 32f), "Highlight Couch Test", _small))
                _game.SelectCouchTestFocusMission();
            if (DrawSkinnedButton(new Rect(startX, footY + 94f, startWidth, 32), $"Start {_game.SelectedMissionName}", _small))
                _game.StartSelectedMission();
        }

        private void DrawSelectedMissionShowcase(Rect rect, GameManager.MissionVariant variant)
        {
            Color accent = MissionBadgeColorFor(variant);
            DrawHudOverlay(rect);
            DrawTintedRect(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f),
                new Color(0.02f, 0.04f, 0.05f, 0.34f));
            DrawTintedRect(new Rect(rect.x, rect.y, rect.width, 6f),
                new Color(accent.r, accent.g, accent.b, 0.78f));
            DrawTintedRect(new Rect(rect.x, rect.y + rect.height - 6f, rect.width, 6f),
                new Color(accent.r, accent.g, accent.b, 0.42f));
            DrawTintedRect(new Rect(rect.x + 8f, rect.y + 8f, 76f, 76f),
                new Color(accent.r, accent.g, accent.b, 0.24f));

            float pulse = (Mathf.Sin(Time.unscaledTime * 3.2f) + 1f) * 0.5f;
            for (int i = 0; i < 5; i++)
            {
                float x = rect.x + rect.width - 156f + i * 28f;
                float y = rect.y + 16f + Mathf.Sin(Time.unscaledTime * 2.4f + i * 0.8f) * 8f;
                float size = 8f + pulse * 5f + (i % 2) * 3f;
                DrawTintedRect(new Rect(x, y, size, size),
                    i % 2 == 0
                        ? new Color(1f, 0.95f, 0.32f, 0.44f)
                        : new Color(accent.r, accent.g, accent.b, 0.46f));
            }

            DrawTintedRect(new Rect(rect.x + rect.width - 270f, rect.y + 82f, 248f, 38f),
                new Color(accent.r, accent.g, accent.b, 0.18f));
        }

        public static float MissionSelectPanelHeight(int missionCount)
        {
            int rows = Mathf.CeilToInt(Mathf.Max(1, missionCount) / 2f);
            return 126f + rows * 42f + 142f;
        }

        private void DrawMissionRow(Rect row, int index, GameManager.MissionVariant variant)
        {
            bool selected = _game.SelectedMissionIndex == index;
            string prefix = selected ? "> " : "";
            string key = index < 9 ? (index + 1).ToString() : index == 9 ? "0" : "-";
            var def = GameManager.BuildMissionDefinition(variant);
            string label = $"{prefix}{key}. {def.Name}\n{_game.MissionSelectStatusFor(variant)}";
            Color previous = GUI.color;
            DrawHudMissionTile(row, selected);
            DrawTintedRect(row, selected ? new Color(1f, 0.82f, 0.18f, 0.16f) : new Color(0.1f, 0.14f, 0.16f, 0.18f));
            if (GUI.Button(row, GUIContent.none, GUIStyle.none)) _game.SelectMission(variant);
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
                GameManager.MissionVariant.BackyardRescue => new Color(0.34f, 0.68f, 0.28f, 0.92f),
                GameManager.MissionVariant.OperationPeeBreak => new Color(0.18f, 0.75f, 0.95f, 0.92f),
                GameManager.MissionVariant.KitchenFoodFrenzy => new Color(1f, 0.68f, 0.25f, 0.92f),
                GameManager.MissionVariant.SnackHeist => new Color(0.95f, 0.48f, 0.18f, 0.92f),
                GameManager.MissionVariant.SockPanic => new Color(0.48f, 0.68f, 1f, 0.92f),
                GameManager.MissionVariant.SquirrelConspiracy => new Color(0.72f, 0.58f, 0.24f, 0.92f),
                GameManager.MissionVariant.CoyotesFence => new Color(0.72f, 0.48f, 0.22f, 0.92f),
                GameManager.MissionVariant.EagleShadowPanic => new Color(0.28f, 0.32f, 0.42f, 0.92f),
                GameManager.MissionVariant.WeenieRoundup => new Color(0.93f, 0.34f, 0.38f, 0.92f),
                GameManager.MissionVariant.ScentSearch => new Color(0.42f, 0.72f, 0.42f, 0.92f),
                GameManager.MissionVariant.ThunderstormComfort => new Color(0.46f, 0.38f, 0.74f, 0.92f),
                GameManager.MissionVariant.MarkTheYard => new Color(0.38f, 0.62f, 0.36f, 0.92f),
                GameManager.MissionVariant.LeashWalk => new Color(0.25f, 0.82f, 0.78f, 0.92f),
                GameManager.MissionVariant.CarRide => new Color(0.58f, 0.62f, 0.68f, 0.92f),
                GameManager.MissionVariant.GateCrash => new Color(0.70f, 0.52f, 0.32f, 0.92f),
                GameManager.MissionVariant.TableStealth => new Color(0.76f, 0.30f, 0.32f, 0.92f),
                GameManager.MissionVariant.SquirrelSwitcheroo => new Color(0.64f, 0.46f, 0.20f, 0.92f),
                GameManager.MissionVariant.WalkCampaign => new Color(0.24f, 0.70f, 0.62f, 0.92f),
                GameManager.MissionVariant.BoneRelay => new Color(0.78f, 0.72f, 0.56f, 0.92f),
                GameManager.MissionVariant.GreatEscape => new Color(0.52f, 0.46f, 0.80f, 0.92f),
                GameManager.MissionVariant.ChaosMachine => new Color(0.82f, 0.42f, 0.70f, 0.92f),
                GameManager.MissionVariant.BlanketCatch => new Color(0.86f, 0.54f, 0.50f, 0.92f),
                _ => new Color(0.28f, 0.55f, 0.32f, 0.92f)
            };
        }

        private void DrawMissionBadge(Rect rect, GameManager.MissionVariant variant, bool selected)
        {
            DrawHudBadge(rect);
            Color badge = MissionBadgeColorFor(variant);
            badge.a = selected ? 0.66f : 0.48f;
            DrawTintedRect(rect, badge);
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

        private void DrawHudPanel(Rect rect) => DrawHudSprite(rect, _hudPanelFrame, new Color(1f, 1f, 1f, 0.97f));
        private void DrawHudOverlay(Rect rect) => DrawHudSprite(rect, _hudOverlayPanel, new Color(1f, 1f, 1f, 0.96f));
        private void DrawHudMissionTile(Rect rect, bool selected) =>
            DrawHudSprite(rect, selected ? _hudMissionTileSelected : _hudMissionTile, Color.white);
        private void DrawHudBadge(Rect rect) => DrawHudSprite(rect, _hudBadgeFrame, Color.white);

        private bool DrawSkinnedButton(Rect rect, string label) => DrawSkinnedButton(rect, label, _hud);

        private bool DrawSkinnedButton(Rect rect, string label, GUIStyle labelStyle)
        {
            DrawHudSprite(rect, _hudButtonPrimary, Color.white);
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.Label(rect, label, labelStyle);
            return clicked;
        }

        private bool DrawResultButton(Rect rect, string label, bool focused)
        {
            Color previous = GUI.color;
            DrawHudSprite(rect, _hudButtonPrimary, Color.white);
            DrawTintedRect(rect, focused ? new Color(1f, 0.83f, 0.22f, 0.36f) : new Color(0.08f, 0.16f, 0.18f, 0.32f));
            if (focused)
            {
                DrawTintedRect(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, 4f), new Color(1f, 0.9f, 0.28f, 0.95f));
                DrawTintedRect(new Rect(rect.x - 4f, rect.yMax, rect.width + 8f, 4f), new Color(1f, 0.9f, 0.28f, 0.95f));
                DrawTintedRect(new Rect(rect.x - 4f, rect.y - 4f, 4f, rect.height + 8f), new Color(1f, 0.9f, 0.28f, 0.95f));
                DrawTintedRect(new Rect(rect.xMax, rect.y - 4f, 4f, rect.height + 8f), new Color(1f, 0.9f, 0.28f, 0.95f));
            }

            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.color = previous;
            GUI.Label(rect, label, _resultButton);
            return clicked;
        }

        private static void DrawHudSprite(Rect rect, Sprite sprite, Color color)
        {
            if (sprite == null)
            {
                DrawTintedRect(rect, new Color(0.02f, 0.04f, 0.05f, 0.78f));
                return;
            }

            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, sprite.texture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        private void DrawSessionSummary()
        {
            var layout = BuildResultOverlayLayout(Screen.width, Screen.height);
            var box = layout.Card;
            DrawTintedRect(layout.Backdrop, new Color(0f, 0f, 0f, 0.72f));
            DrawHudOverlay(box);
            DrawTintedRect(box, new Color(0.015f, 0.025f, 0.03f, 0.92f));
            DrawTintedRect(new Rect(box.x, box.y, box.width, 9f), new Color(0.18f, 0.75f, 0.95f, 0.92f));
            GUI.Label(layout.Headline, "SESSION COMPLETE", _resultHeadline);
            GUI.Label(layout.Subtitle, _game.SessionSummaryLabel, _resultSubtitle);
            GUI.Label(layout.Score, _game.SessionRanksEarnedLabel, _resultBody);
            string continuePrompt = _game.SessionAllMissionsCompleted ? "begins a Victory Lap" : "continues";
            GUI.Label(layout.Flavor, $"Enter / Start {continuePrompt}", _resultBody);

            if (DrawResultButton(layout.Buttons[0], _game.SessionContinueActionLabel, true))
                _game.ContinueSession();
            if (DrawResultButton(layout.Buttons[1], "Mission Select", false))
                _game.ReturnToMissionSelect();
            if (DrawResultButton(layout.Buttons[2], "New Session", false))
            {
                _game.ResetSession();
                _game.ReturnToMissionSelect();
            }
            GUI.Label(layout.ButtonHints[0], "Enter / Start", _resultHint);
            GUI.Label(layout.ButtonHints[1], "M / Escape", _resultHint);
            GUI.Label(layout.ButtonHints[2], "Reset route", _resultHint);
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
            LoadGeneratedHudSkin();
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
            _resultHeadline = new GUIStyle(GUI.skin.label) { fontSize = ResultHeadlineFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _resultHeadline.normal.textColor = new Color(1f, 0.92f, 0.26f);
            _resultSubtitle = new GUIStyle(GUI.skin.label) { fontSize = ResultSubtitleFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _resultSubtitle.normal.textColor = Color.white;
            _resultSubtitle.wordWrap = true;
            _resultBody = new GUIStyle(GUI.skin.label) { fontSize = ResultBodyFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _resultBody.normal.textColor = new Color(0.95f, 0.98f, 1f);
            _resultBody.wordWrap = true;
            _resultButton = new GUIStyle(GUI.skin.label) { fontSize = ResultButtonFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _resultButton.normal.textColor = Color.white;
            _resultButton.wordWrap = true;
            _resultHint = new GUIStyle(GUI.skin.label) { fontSize = ResultHintFontSize, alignment = TextAnchor.MiddleCenter };
            _resultHint.normal.textColor = new Color(0.78f, 0.88f, 0.92f);
            _resultHint.wordWrap = true;
        }

        private void LoadGeneratedHudSkin()
        {
            _hudPanelFrame = FinalGameplayArt.Load(FinalGameplayArt.HudPanelFrame);
            _hudMissionTile = FinalGameplayArt.Load(FinalGameplayArt.HudMissionTile);
            _hudMissionTileSelected = FinalGameplayArt.Load(FinalGameplayArt.HudMissionTileSelected);
            _hudBadgeFrame = FinalGameplayArt.Load(FinalGameplayArt.HudBadgeFrame);
            _hudButtonPrimary = FinalGameplayArt.Load(FinalGameplayArt.HudButtonPrimary);
            _hudOverlayPanel = FinalGameplayArt.Load(FinalGameplayArt.HudOverlayPanel);
        }
    }
}
