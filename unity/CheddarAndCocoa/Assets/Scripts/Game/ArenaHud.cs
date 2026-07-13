using UnityEngine;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// IMGUI overlay for the playable arena: the shared score + countdown while playing, end
    /// actions, and a session summary. OnGUI keeps it Canvas/font-free (matches the existing
    /// DebugHud), which is plenty for these in-mission overlays. Mission select renders through the
    /// UGUI/TMP <see cref="MissionSelectScreen"/> instead so its text stays crisp at any resolution.
    /// Game flow mutations are delegated to <see cref="GameManager"/> so tests can assert the same
    /// paths the buttons use.
    /// </summary>
    public sealed class ArenaHud : MonoBehaviour
    {
        // IMGUI lays out in physical pixels, so fixed font sizes shrink into unreadable text on
        // retina/4K displays. OnGUI scales the whole HUD up from a 1920x1080 virtual space instead;
        // never below 1 so smaller windows keep today's layout.
        public const float ReferenceHeight = 1080f;
        public const float ReferenceWidth = 1920f;

        private GameManager _game;
        private float _uiScale = 1f;
        private GUIStyle _hud, _big, _mid, _small, _overlay, _briefing, _resultHeadline, _resultSubtitle, _resultBody, _resultHint, _resultButton;
        private Texture2D _uiKitTexture;
        private Sprite _hudPanelFrame, _hudMissionTile, _hudMissionTileSelected, _hudBadgeFrame, _hudButtonPrimary, _hudOverlayPanel;
        public const string PlayerOwnershipLabel = "P1 Cheddar: WASD + Space/E/L-Shift/Q  |  P2 Cocoa: Arrows + Enter/Right Shift/Right Ctrl/Right Alt";
        public const string PadControlsLabel = "Pads: left stick moves  |  X / West barks  |  Y / North interacts  |  B / East jumps  |  A / South wrestles";
        public const int ResultHeadlineFontSize = 54;
        public const int ResultSubtitleFontSize = 32;
        public const int ResultBodyFontSize = 26;
        public const int ResultButtonFontSize = 26;
        public const int ResultHintFontSize = 18;
        public const int MissionBriefingGoalFontSize = 24;

        public readonly struct MissionBriefingLayout
        {
            public readonly Rect Card;
            public readonly Rect Title;
            public readonly Rect Presentation;
            public readonly Rect Goal;
            public readonly Rect First;
            public readonly Rect Roles;
            public readonly Rect Ownership;
            public readonly Rect Controls;

            public MissionBriefingLayout(Rect card, Rect title, Rect presentation, Rect goal,
                Rect first, Rect roles, Rect ownership, Rect controls)
            {
                Card = card;
                Title = title;
                Presentation = presentation;
                Goal = goal;
                First = first;
                Roles = roles;
                Ownership = ownership;
                Controls = controls;
            }
        }

        public static MissionBriefingLayout BuildMissionBriefingLayout(float screenWidth, float screenHeight)
        {
            Rect card = FitPanel(screenWidth, screenHeight, 920f, 360f);
            card.y = Mathf.Min(card.y, 304f);
            float pad = 28f;
            float x = card.x + pad;
            float w = card.width - pad * 2f;
            float y = card.y + 16f;
            var title = new Rect(x, y, w, 44f);
            y += 48f;
            var presentation = new Rect(x, y, w, 26f);
            y += 32f;
            var goal = new Rect(x, y, w, 88f);
            y += 94f;
            var first = new Rect(x, y, w, 32f);
            y += 38f;
            var roles = new Rect(x, y, w, 26f);
            y += 30f;
            var ownership = new Rect(x, y, w, 24f);
            y += 26f;
            var controls = new Rect(x, y, w, 24f);
            return new MissionBriefingLayout(card, title, presentation, goal, first, roles, ownership, controls);
        }

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

        public static float UiScaleFor(float screenWidth, float screenHeight) =>
            Mathf.Max(1f, Mathf.Min(screenWidth / ReferenceWidth, screenHeight / ReferenceHeight));

        public float UiScale => _uiScale;
        private float VirtualWidth => Screen.width / _uiScale;
        private float VirtualHeight => Screen.height / _uiScale;

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

            _uiScale = UiScaleFor(Screen.width, Screen.height);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(_uiScale, _uiScale, 1f)) * previousMatrix;

            if (_game.IsPaused)
            {
                DrawPauseMenu();
            }
            else if (_game.MissionSelectVisible)
            {
                // MissionSelectScreen (UGUI + TMP) renders the picker; IMGUI draws nothing here.
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

            GUI.matrix = previousMatrix;
        }

        private void DrawGameplayHud()
        {
            if (_game.EndScreenVisible)
            {
                DrawEndCard();
                return;
            }

            int secs = Mathf.CeilToInt(Mathf.Max(0f, _game.TimeRemaining));
            // White labels straight on the bright yard were unreadable from the couch: every
            // always-on HUD block sits on a dark contrast band now.
            DrawTintedRect(new Rect(0f, 0f, VirtualWidth, 140f), new Color(0.02f, 0.05f, 0.06f, 0.62f));
            GUI.Label(new Rect(0, 8, VirtualWidth, 30), $"SCORE  {_game.Score}", _hud);
            GUI.Label(new Rect(0, 34, VirtualWidth, 26), $"Timer  {secs}s", _mid);
            string squirrelState = _game.MaxStolenFood > 0 ? $"Stolen {_game.StolenFood}/{_game.MaxStolenFood}" : "No squirrel pressure";
            GUI.Label(new Rect(0, 58, VirtualWidth, 24), $"MISSION: {_game.ActiveMissionName} / {_game.Phase} | {_game.BreakfastRecovered}/{_game.BreakfastGoal} {_game.MissionItemPlural} | {squirrelState}", _mid);
            GUI.Label(new Rect(0, 82, VirtualWidth, 24), $"{PlayerOwnershipLabel}  |  {PadControlsLabel}  |  F1 Overlay | F2 Audio | F3 Rumble", _small);
            GUI.Label(new Rect(0, 106, VirtualWidth, 24), $"Switch mission: keys 1-9, 0 ({_game.MissionSelectOptionCount} missions) | United barks: {_game.UnitedBarks} | Tug {Mathf.RoundToInt(_game.TugProgress * 100f)}% | Modifier: {_game.ActiveModifierLabel}", _mid);
            GUI.Label(new Rect(0, 130, VirtualWidth, 24), _game.LastScoreEventLabel, _mid);

            DrawTintedRect(new Rect(0f, 148f, VirtualWidth, 108f), new Color(0.02f, 0.05f, 0.06f, 0.5f));
            if (_game.ScorePopVisible)
                GUI.Label(new Rect(0, 152, VirtualWidth, 30), _game.LastScorePopLabel, _big);
            GUI.Label(new Rect(0, 180, VirtualWidth, 24), $"Objective: {_game.ObjectiveLabel}", _mid);
            GUI.Label(new Rect(0, 204, VirtualWidth, 24), _game.TeamGuidanceLabel, _small);
            GUI.Label(new Rect(0, 228, VirtualWidth, 24), _game.LastCue, _mid);
            if (!string.IsNullOrEmpty(_game.MissionBanner) && !_game.IsGameOver && !_game.IsLevelClear && !_game.MissionBriefingVisible)
            {
                DrawTintedRect(new Rect(0f, 258f, VirtualWidth, 40f), new Color(0.02f, 0.05f, 0.06f, 0.6f));
                GUI.Label(new Rect(0, 260, VirtualWidth, 34), _game.MissionBanner, _big);
            }

            if (_game.LeadInActive && !_game.MissionBriefingVisible)
            {
                DrawTintedRect(new Rect(0f, 300f, VirtualWidth, 40f), new Color(0.02f, 0.05f, 0.06f, 0.6f));
                GUI.Label(new Rect(0, 302, VirtualWidth, 34), _game.LeadInCountdownLabel, _big);
            }

            if (_game.MissionBriefingVisible) DrawMissionBriefing();

        }

        private void DrawMissionBriefing()
        {
            var layout = BuildMissionBriefingLayout(VirtualWidth, VirtualHeight);
            // Same treatment as the result overlay: an opaque dark card, not a translucent frame
            // that lets the bright yard wash out white briefing text.
            DrawHudOverlay(layout.Card);
            DrawTintedRect(layout.Card, new Color(0.015f, 0.025f, 0.03f, 0.9f));
            DrawTintedRect(new Rect(layout.Card.x, layout.Card.y, layout.Card.width, 8f),
                MissionBadgeColorFor(_game.ActiveMissionVariant));

            GUI.Label(layout.Title, _game.ActiveMissionName, _big);
            GUI.Label(layout.Presentation, _game.MissionPresentationLine, _mid);
            GUI.Label(layout.Goal, $"GOAL: {_game.MissionIntroPrompt}", _briefing);
            GUI.Label(layout.First, $"FIRST: {_game.ObjectiveLabel}", _hud);
            GUI.Label(layout.Roles, $"ROLES: {_game.MissionRoleHint}", _mid);
            GUI.Label(layout.Ownership, PlayerOwnershipLabel, _small);
            GUI.Label(layout.Controls, _game.LeadInActive
                ? $"Yard is paused for a look around — BARK when both players are ready  |  {PadControlsLabel}"
                : $"Follow each dog's arrow  |  {PadControlsLabel}", _small);
        }

        private void DrawPauseMenu()
        {
            DrawGameplayHud();
            var box = FitPanel(VirtualWidth, VirtualHeight, 520f, 250f);
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
            var layout = BuildResultOverlayLayout(VirtualWidth, VirtualHeight);
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
            float w = Mathf.Min(440f, Mathf.Max(1f, VirtualWidth - 24f));
            float h = Mathf.Min(410f, Mathf.Max(1f, VirtualHeight - 24f));
            var box = new Rect(Mathf.Max(12f, VirtualWidth - w - 12f), 12f, w, h);
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
            if (DrawSkinnedButton(new Rect(12f, VirtualHeight - 42f, 168f, 30f), label, _small))
                _game.TogglePlaytestOverlay();
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
                GameManager.MissionVariant.BabyBirdBedlam => "BRD",
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
                GameManager.MissionVariant.BabyBirdBedlam => new Color(0.55f, 0.72f, 0.92f, 0.92f),
                _ => new Color(0.28f, 0.55f, 0.32f, 0.92f)
            };
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
            var layout = BuildResultOverlayLayout(VirtualWidth, VirtualHeight);
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
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            _mid.normal.textColor = Color.white;
            _mid.wordWrap = true;
            _small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            _small.normal.textColor = new Color(0.9f, 0.95f, 1f);
            _small.wordWrap = true;
            _overlay = new GUIStyle(_small) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            _briefing = new GUIStyle(_mid) { fontSize = MissionBriefingGoalFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
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
