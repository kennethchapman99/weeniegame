using UnityEngine;
using UnityEngine.InputSystem;
using CheddarAndCocoa.Dogs;
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
        private GUIStyle _missionHud, _objectiveHud, _statusHud, _playerChip, _tutorialHeading, _promptGlyph, _promptText;
        private Texture2D _uiKitTexture;
        private Sprite _hudPanelFrame, _hudMissionTile, _hudMissionTileSelected, _hudBadgeFrame, _hudButtonPrimary, _hudOverlayPanel;
        private int _pauseSelection;
        private bool _pauseWasOpen;
        public const string PlayerOwnershipLabel = "P1 Cheddar: WASD move, Space bark, E interact, L-Shift jump, Q wrestle  |  P2 Cocoa: Arrows move, Enter bark, R-Shift interact, R-Ctrl jump, R-Alt wrestle";
        public const string PlayerIdentityLabel = "P1 CHEDDAR  •  P2 COCOA";
        public const string PadControlsLabel = "Switch-style: left stick moves  |  Y / West barks  |  X / North interacts  |  A / East jumps  |  B / South wrestles";
        public const int GameplayObjectiveFontSize = 28;
        public const int GameplayIdentityFontSize = 22;
        public const int GameplayStatusFontSize = 20;
        public const float GameplayPlayerChipHeight = 68f;
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

        public readonly struct GameplayHudLayout
        {
            public readonly Rect TopBar;
            public readonly Rect Mission;
            public readonly Rect Status;
            public readonly Rect Objective;
            public readonly Rect CheddarChip;
            public readonly Rect CocoaChip;
            public readonly Rect Transient;

            public GameplayHudLayout(Rect topBar, Rect mission, Rect status, Rect objective,
                Rect cheddarChip, Rect cocoaChip, Rect transient)
            {
                TopBar = topBar;
                Mission = mission;
                Status = status;
                Objective = objective;
                CheddarChip = cheddarChip;
                CocoaChip = cocoaChip;
                Transient = transient;
            }
        }

        public static GameplayHudLayout BuildGameplayHudLayout(float screenWidth, float screenHeight)
        {
            const float margin = 16f;
            float topWidth = Mathf.Max(1f, screenWidth - margin * 2f);
            var topBar = new Rect(margin, margin, topWidth, 102f);
            float statusWidth = Mathf.Min(440f, topWidth * 0.38f);
            var mission = new Rect(topBar.x + 18f, topBar.y + 9f,
                Mathf.Max(1f, topBar.width - statusWidth - 48f), 27f);
            var status = new Rect(topBar.xMax - statusWidth - 18f, topBar.y + 9f, statusWidth, 27f);
            var objective = new Rect(topBar.x + 24f, topBar.y + 39f, topBar.width - 48f, 55f);

            float chipWidth = Mathf.Min(360f, Mathf.Max(1f, (screenWidth - margin * 3f) * 0.5f));
            float chipY = Mathf.Max(margin, screenHeight - margin - GameplayPlayerChipHeight);
            var cheddarChip = new Rect(margin, chipY, chipWidth, GameplayPlayerChipHeight);
            var cocoaChip = new Rect(Mathf.Max(margin, screenWidth - margin - chipWidth), chipY, chipWidth, GameplayPlayerChipHeight);
            float transientWidth = Mathf.Min(720f, topWidth);
            var transient = new Rect((screenWidth - transientWidth) * 0.5f, topBar.yMax + 10f, transientWidth, 46f);
            return new GameplayHudLayout(topBar, mission, status, objective, cheddarChip, cocoaChip, transient);
        }

        public static MissionBriefingLayout BuildMissionBriefingLayout(float screenWidth, float screenHeight)
        {
            Rect card = FitPanel(screenWidth, screenHeight, 1120f, 650f);
            card.y = Mathf.Min(card.y, 190f);
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
            var controls = new Rect(x, y, w, 276f);
            return new MissionBriefingLayout(card, title, presentation, goal, first, roles, ownership, controls);
        }

        public static Rect BuildPressureMeterRect(Rect topBar) =>
            new(topBar.xMax - 408f, topBar.y + 48f, 378f, 38f);

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

        private bool PauseHasActionRow =>
            _game != null && (_game.ActionTutorialAvailable || _game.FirstMissionControlStripVisible);
        private int PauseOptionCount => PauseHasActionRow ? 7 : 6;
        private int PauseResumeIndex => PauseHasActionRow ? 4 : 3;

        private void Update()
        {
            if (_game == null || !_game.IsPaused)
            {
                _pauseWasOpen = false;
                return;
            }

            if (!_pauseWasOpen)
            {
                _pauseSelection = PauseResumeIndex;
                _pauseWasOpen = true;
                return;
            }

            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            bool previous = (keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)) ||
                (pad != null && (pad.dpad.up.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame));
            bool next = (keyboard != null && (keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)) ||
                (pad != null && (pad.dpad.down.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame));
            if (previous) _pauseSelection = (_pauseSelection + PauseOptionCount - 1) % PauseOptionCount;
            else if (next) _pauseSelection = (_pauseSelection + 1) % PauseOptionCount;

            bool confirm = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
                (pad != null && pad.buttonSouth.wasPressedThisFrame);
            bool back = pad != null && pad.buttonEast.wasPressedThisFrame;
            if (back) _game.TogglePause();
            else if (confirm) ActivatePauseOption(_pauseSelection);
        }

        private void ActivatePauseOption(int option)
        {
            if (_game == null) return;
            if (option == 0) _game.SetAudioEnabled(!_game.AudioEnabled);
            else if (option == 1) _game.SetRumbleEnabled(!_game.RumbleEnabled);
            else if (option == 2) _game.SetCameraShakeEnabled(!_game.CameraShakeEnabled);
            else
            {
                int index = 3;
                if (_game.ActionTutorialAvailable)
                {
                    if (option == index)
                    {
                        if (_game.ShowActionTutorial) _game.SkipActionTutorial();
                        else _game.ReplayActionTutorial();
                        return;
                    }
                    index++;
                }
                else if (_game.FirstMissionControlStripVisible)
                {
                    if (option == index)
                    {
                        _game.SkipFirstMissionControlStrip();
                        return;
                    }
                    index++;
                }

                if (option == index) _game.TogglePause();
                else if (option == index + 1) _game.ReturnToMissionSelect();
                else if (option == index + 2) _game.RequestQuit();
            }
        }

        private bool DrawPauseButton(Rect rect, string label, int option) =>
            DrawResultButton(rect, label, _pauseSelection == option);

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
                // F1 / backquote remains the observer's way into diagnostics. The playtest switch
                // only appears after that explicit request, so family-facing play has no debug UI.
                if (_game.PlaytestOverlayVisible)
                {
                    DrawPlaytestOverlay();
                    DrawPlaytestModeToggle();
                }
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

            if (_game.MissionOpeningPresentationVisible)
            {
                DrawOpeningPresentationOverlay();
                return;
            }

            DrawPlayerIdentityChips();
            if (_game.MissionBriefingVisible)
            {
                DrawMissionBriefing();
                return;
            }

            DrawProductionGameplayHud();
            if (_game.ShowActionTutorial) DrawActionTutorial();
            else if (_game.FirstMissionControlStripVisible) DrawFirstMissionControlStrip();
        }

        private void DrawOpeningPresentationOverlay()
        {
            var top = new Rect(0f, 0f, VirtualWidth, 82f);
            var bottom = new Rect(0f, VirtualHeight - 76f, VirtualWidth, 76f);
            DrawTintedRect(top, new Color(0.01f, 0.015f, 0.02f, 0.82f));
            DrawTintedRect(bottom, new Color(0.01f, 0.015f, 0.02f, 0.82f));
            if (_game.ActiveMissionController is IMissionOpeningPresentationController opening &&
                !string.IsNullOrEmpty(opening.OpeningOverlayLabel))
            {
                // The supplied Pee Break explainer has a baked misspelling only during its
                // comprehension shots. The controller times this plaque to those shots so the
                // valid BLADDER URGENCY heading remains unobscured everywhere else.
                var correctionPlaque = new Rect(42f, 125f, 420f, 72f);
                DrawTintedRect(correctionPlaque, new Color(0.01f, 0.015f, 0.02f, 0.9f));
                GUI.Label(correctionPlaque, opening.OpeningOverlayLabel, _hud);
            }
            GUI.Label(new Rect(28f, 12f, VirtualWidth - 56f, 56f),
                $"{_game.ActiveMissionName.ToUpperInvariant()}  •  WATCH THE PLAN", _mid);
            GUI.Label(new Rect(28f, bottom.y + 10f, VirtualWidth - 56f, 52f),
                "BARK OR INTERACT TO SKIP  •  CONTROLS CARD NEXT", _mid);
        }

        private void DrawProductionGameplayHud()
        {
            int secs = Mathf.CeilToInt(Mathf.Max(0f, _game.TimeRemaining));
            var layout = BuildGameplayHudLayout(VirtualWidth, VirtualHeight);
            var snapshot = _game.RuntimeSnapshot;

            DrawHudOverlay(layout.TopBar);
            DrawTintedRect(layout.TopBar, new Color(0.015f, 0.025f, 0.03f, 0.88f));
            DrawTintedRect(new Rect(layout.TopBar.x, layout.TopBar.y, layout.TopBar.width, 6f),
                MissionBadgeColorFor(_game.ActiveMissionVariant));
            GUI.Label(layout.Mission, _game.ActiveMissionName.ToUpperInvariant(), _missionHud);

            string progress = snapshot.ObjectiveGoal > 0
                ? $"{snapshot.ObjectiveProgress}/{snapshot.ObjectiveGoal}  •  "
                : string.Empty;
            GUI.Label(layout.Status, $"{progress}{secs}s  •  SCORE {_game.Score}", _statusHud);
            Rect objectiveRect = layout.Objective;
            if (_game.ActiveMissionController is IMissionPressureHud pressureHud && pressureHud.PressureVisible)
            {
                Rect meter = BuildPressureMeterRect(layout.TopBar);
                objectiveRect.width = Mathf.Max(1f, meter.x - objectiveRect.x - 18f);
                DrawPressureMeter(meter, pressureHud);
            }
            string objectiveText = _game.ObjectiveLabel;
            if (_game.GuidanceRescueActive)
            {
                float rescuePulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
                DrawTintedRect(objectiveRect, new Color(1f, 0.82f, 0.2f, 0.14f + rescuePulse * 0.14f));
                if (!string.IsNullOrEmpty(_game.GuidanceRescueDogName))
                    objectiveText = $"{_game.GuidanceRescueDogName.ToUpperInvariant()}: {objectiveText}";
            }
            GUI.Label(objectiveRect, objectiveText, _objectiveHud);

            string transient = _game.LeadInActive ? _game.LeadInCountdownLabel :
                (_game.ScorePopVisible ? _game.LastScorePopLabel : string.Empty);
            if (!string.IsNullOrEmpty(transient))
            {
                DrawHudOverlay(layout.Transient);
                DrawTintedRect(layout.Transient, new Color(0.015f, 0.025f, 0.03f, 0.86f));
                GUI.Label(layout.Transient, transient, _hud);
            }
        }

        private void DrawPlayerIdentityChips()
        {
            var layout = BuildGameplayHudLayout(VirtualWidth, VirtualHeight);
            bool handoffFlash = _game.HandoffChipFlashVisible;
            DrawPlayerIdentityChip(layout.CheddarChip,
                BuildPlayerIdentityChipLabel("P1  CHEDDAR", _game.PlayerControlSourceLabel(DogId.Cheddar)), CheddarAccent,
                _game.GuidancePartnerDogIndex == 0 || handoffFlash);
            DrawPlayerIdentityChip(layout.CocoaChip,
                BuildPlayerIdentityChipLabel("P2  COCOA", _game.PlayerControlSourceLabel(DogId.Cocoa)), CocoaAccent,
                _game.GuidancePartnerDogIndex == 1 || handoffFlash);
        }

        public static string BuildPlayerIdentityChipLabel(string player, string controlSource) =>
            $"{player}\n{controlSource}";

        private void DrawPlayerIdentityChip(Rect rect, string label, Color accent, bool guidancePulse = false)
        {
            DrawHudOverlay(rect);
            Color background = new Color(0.015f, 0.025f, 0.03f, 0.9f);
            if (guidancePulse)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);
                background = Color.Lerp(background, new Color(accent.r, accent.g, accent.b, 0.4f), 0.25f + pulse * 0.25f);
            }
            DrawTintedRect(rect, background);
            DrawTintedRect(new Rect(rect.x, rect.y, 8f, rect.height), accent);
            GUI.Label(new Rect(rect.x + 18f, rect.y, rect.width - 28f, rect.height), label, _playerChip);
        }

        // Action colors are stable across keyboard and the Switch-style face-button diagram.
        private static readonly Color GlyphBark = new Color(0.16f, 0.55f, 0.95f, 0.95f);      // Y / West
        private static readonly Color GlyphInteract = new Color(0.95f, 0.78f, 0.12f, 0.95f);  // X / North
        private static readonly Color GlyphJump = new Color(0.85f, 0.2f, 0.2f, 0.95f);        // A / East
        private static readonly Color GlyphWrestle = new Color(0.28f, 0.72f, 0.32f, 0.95f);   // B / South
        private static readonly Color CheddarAccent = new Color(1f, 0.62f, 0.12f, 0.95f);
        private static readonly Color CocoaAccent = new Color(0.12f, 0.95f, 0.88f, 0.95f);

        /// <summary>
        /// First-play onboarding for Backyard Rescue. Only one verb is disclosed at a time and each
        /// player has an independent completion cell; the next verb cannot replace the prompt until
        /// both Cheddar and Cocoa have tried the current one.
        /// </summary>
        private void DrawActionTutorial()
        {
            GameManager.TutorialActionStep action = _game.CurrentTutorialAction;
            string actionName;
            string instruction;
            string glyph;
            string cheddarKey;
            string cocoaKey;
            Color glyphColor;
            switch (action)
            {
                case GameManager.TutorialActionStep.Interact:
                    actionName = "INTERACT";
                    instruction = "USE NEARBY OBJECTS";
                    glyph = "X";
                    cheddarKey = "E";
                    cocoaKey = "R-SHIFT";
                    glyphColor = GlyphInteract;
                    break;
                case GameManager.TutorialActionStep.Jump:
                    actionName = "JUMP";
                    instruction = "HOP OVER TROUBLE";
                    glyph = "A";
                    cheddarKey = "L-SHIFT";
                    cocoaKey = "R-CTRL";
                    glyphColor = GlyphJump;
                    break;
                case GameManager.TutorialActionStep.Wrestle:
                    actionName = "WRESTLE";
                    instruction = "TRY SOME SIBLING CHAOS";
                    glyph = "B";
                    cheddarKey = "Q";
                    cocoaKey = "R-ALT";
                    glyphColor = GlyphWrestle;
                    break;
                default:
                    actionName = "BARK";
                    instruction = "MAKE THE YARD REACT";
                    glyph = "Y";
                    cheddarKey = "SPACE";
                    cocoaKey = "ENTER";
                    glyphColor = GlyphBark;
                    break;
            }

            float w = Mathf.Min(920f, VirtualWidth - 32f);
            const float h = 132f;
            var box = new Rect((VirtualWidth - w) * 0.5f, VirtualHeight - h - 94f, w, h);
            DrawHudOverlay(box);
            DrawTintedRect(box, new Color(0.015f, 0.025f, 0.03f, 0.92f));
            GUI.Label(new Rect(box.x + 16f, box.y + 8f, box.width - 32f, 34f),
                $"TRY {actionName} — BOTH PLAYERS  •  {instruction}", _tutorialHeading);

            const float gap = 12f;
            float cellWidth = (box.width - 44f - gap) * 0.5f;
            var cheddarCell = new Rect(box.x + 16f, box.y + 50f, cellWidth, 66f);
            var cocoaCell = new Rect(cheddarCell.xMax + gap, cheddarCell.y, cellWidth, cheddarCell.height);
            DrawActionPrompt(cheddarCell, DogId.Cheddar, "P1 CHEDDAR", glyph, glyphColor,
                actionName, cheddarKey, CheddarAccent);
            DrawActionPrompt(cocoaCell, DogId.Cocoa, "P2 COCOA", glyph, glyphColor,
                actionName, cocoaKey, CocoaAccent);
        }

        private void DrawActionPrompt(Rect cell, DogId dogId, string playerLabel, string glyphLetter,
            Color glyphColor, string actionName, string keyLabel, Color playerAccent)
        {
            bool done = _game.TutorialActionDone(dogId, _game.CurrentTutorialAction);
            DrawTintedRect(cell, done
                ? new Color(0.12f, 0.42f, 0.2f, 0.78f)
                : new Color(0.05f, 0.1f, 0.12f, 0.86f));
            DrawTintedRect(new Rect(cell.x, cell.y, 6f, cell.height), playerAccent);

            float glyphSize = 42f;
            var glyphRect = new Rect(cell.x + 16f, cell.y + (cell.height - glyphSize) * 0.5f, glyphSize, glyphSize);
            DrawTintedRect(glyphRect, done
                ? new Color(0.25f, 0.82f, 0.4f, 0.95f)
                : glyphColor);

            GUI.Label(glyphRect, done ? "OK" : glyphLetter, _promptGlyph);
            var textRect = new Rect(glyphRect.xMax + 12f, cell.y, cell.width - glyphSize - 40f, cell.height);
            GUI.Label(textRect, done
                ? $"{playerLabel}  •  DONE"
                : $"{playerLabel}  •  {actionName}\nPAD {glyphLetter}  /  {keyLabel}", _promptText);
        }

        /// <summary>
        /// F4.1: a stranger's first mission of the session — whichever mission that turns out to be
        /// — gets this compact fading reminder instead of Backyard Rescue's full progressive
        /// tutorial (the two are mutually exclusive per GameManager.FirstMissionControlStripVisible,
        /// so they never draw in the same frame). Reuses DrawPadButton/DrawKey verbatim - the exact
        /// glyph rendering the briefing card's DrawControlGuide already uses - just laid out compact.
        /// </summary>
        private void DrawFirstMissionControlStrip()
        {
            float alpha = _game.FirstMissionControlStripAlpha;
            if (alpha <= 0f) return;

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            float w = Mathf.Min(920f, VirtualWidth - 32f);
            const float h = 140f;
            var box = new Rect((VirtualWidth - w) * 0.5f, VirtualHeight - h - 94f, w, h);
            DrawHudOverlay(box);
            DrawTintedRect(box, new Color(0.015f, 0.025f, 0.03f, 0.92f));
            GUI.Label(new Rect(box.x + 16f, box.y + 6f, box.width - 32f, 26f), "YOUR CONTROLS", _tutorialHeading);

            var verbList = new (string glyph, string label, Color color, GameManager.TutorialActionStep action)[]
            {
                ("Y", "BARK", GlyphBark, GameManager.TutorialActionStep.Bark),
                ("X", "USE", GlyphInteract, GameManager.TutorialActionStep.Interact),
                ("A", "JUMP", GlyphJump, GameManager.TutorialActionStep.Jump),
                ("B", "PLAY", GlyphWrestle, GameManager.TutorialActionStep.Wrestle),
            };

            const float btnSize = 42f;
            const float btnGap = 24f;
            float rowY = box.y + 38f;
            float totalBtnWidth = btnSize * verbList.Length + btnGap * (verbList.Length - 1);
            float btnStartX = box.x + (box.width - totalBtnWidth) * 0.5f;
            for (int i = 0; i < verbList.Length; i++)
            {
                var verb = verbList[i];
                bool done = _game.IsFirstMissionVerbUsed(verb.action);
                var rect = new Rect(btnStartX + i * (btnSize + btnGap), rowY, btnSize, btnSize);
                DrawPadButton(rect, done ? "OK" : verb.glyph, verb.label,
                    done ? new Color(0.25f, 0.82f, 0.4f, 0.95f) : verb.color);
            }

            float keyRowY = rowY + btnSize + 26f;
            float keyGap = 8f;
            float keyWidth = (box.width - 32f - keyGap * 7f) / 8f;
            string[] cheddarKeys = { "SPACE", "E", "L-SHIFT", "Q" };
            string[] cocoaKeys = { "ENTER", "R-SHIFT", "R-CTRL", "R-ALT" };
            for (int i = 0; i < verbList.Length; i++)
            {
                DrawTintedRect(new Rect(box.x + 16f + i * (keyWidth + keyGap), keyRowY, keyWidth, 30f), verbList[i].color);
                GUI.Label(new Rect(box.x + 16f + i * (keyWidth + keyGap), keyRowY, keyWidth, 30f), cheddarKeys[i], _small);
                float x2 = box.x + 16f + (i + 4) * (keyWidth + keyGap);
                DrawTintedRect(new Rect(x2, keyRowY, keyWidth, 30f), verbList[i].color);
                GUI.Label(new Rect(x2, keyRowY, keyWidth, 30f), cocoaKeys[i], _small);
            }

            GUI.color = previousColor;
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
            GUI.Label(layout.Ownership, PlayerIdentityLabel, _small);
            DrawControlGuide(layout.Controls);
        }

        private void DrawPressureMeter(Rect rect, IMissionPressureHud pressureHud)
        {
            DrawTintedRect(rect, new Color(0.02f, 0.035f, 0.045f, 0.96f));
            GUI.Label(new Rect(rect.x + 10f, rect.y - 1f, 178f, rect.height), pressureHud.PressureLabel, _small);
            var track = new Rect(rect.x + 184f, rect.y + 9f, rect.width - 196f, 20f);
            DrawTintedRect(track, new Color(0.12f, 0.16f, 0.18f, 0.96f));
            float fill = Mathf.Clamp01(pressureHud.PressureNormalized);
            DrawTintedRect(new Rect(track.x + 3f, track.y + 3f, Mathf.Max(0f, (track.width - 6f) * fill), track.height - 6f),
                pressureHud.PressureColor);
        }

        private void DrawControlGuide(Rect rect)
        {
            const float gap = 14f;
            float panelWidth = (rect.width - gap) * 0.5f;
            var keyboardPanel = new Rect(rect.x, rect.y, panelWidth, rect.height);
            var padPanel = new Rect(keyboardPanel.xMax + gap, rect.y, panelWidth, rect.height);
            DrawTintedRect(keyboardPanel, new Color(0.04f, 0.07f, 0.09f, 0.96f));
            DrawTintedRect(padPanel, new Color(0.04f, 0.07f, 0.09f, 0.96f));

            GUI.Label(new Rect(keyboardPanel.x + 10f, keyboardPanel.y + 4f, keyboardPanel.width - 20f, 28f),
                "KEYBOARD", _hud);
            DrawKeyboardPlayer(keyboardPanel, keyboardPanel.y + 38f, "P1 CHEDDAR", "W A S D", "SPACE", "E", "L-SHIFT", "Q");
            DrawKeyboardPlayer(keyboardPanel, keyboardPanel.y + 146f, "P2 COCOA", "ARROW KEYS", "ENTER", "R-SHIFT", "R-CTRL", "R-ALT");

            GUI.Label(new Rect(padPanel.x + 10f, padPanel.y + 4f, padPanel.width - 20f, 28f),
                "NINTENDO SWITCH-STYLE CONTROLLER", _hud);
            DrawTintedRect(new Rect(padPanel.x + 54f, padPanel.y + 56f, padPanel.width - 108f, 158f),
                new Color(0.12f, 0.16f, 0.19f, 0.98f));
            GUI.Label(new Rect(padPanel.x + 74f, padPanel.y + 72f, 128f, 54f), "LEFT STICK\nMOVE", _small);
            float cx = padPanel.xMax - 144f;
            float cy = padPanel.y + 124f;
            DrawPadButton(new Rect(cx - 50f, cy - 22f, 42f, 42f), "Y", "BARK", GlyphBark);
            DrawPadButton(new Rect(cx, cy - 70f, 42f, 42f), "X", "INTERACT", GlyphInteract);
            DrawPadButton(new Rect(cx + 50f, cy - 22f, 42f, 42f), "A", "JUMP", GlyphJump);
            DrawPadButton(new Rect(cx, cy + 26f, 42f, 42f), "B", "WRESTLE", GlyphWrestle);
            GUI.Label(new Rect(padPanel.x + 14f, padPanel.yMax - 44f, padPanel.width - 28f, 34f),
                "BARK / INTERACT TO START EARLY", _small);
        }

        private void DrawKeyboardPlayer(Rect panel, float y, string player, string move, string bark, string interact, string jump, string wrestle)
        {
            GUI.Label(new Rect(panel.x + 12f, y, 98f, 30f), player, _small);
            DrawKey(new Rect(panel.x + 112f, y, 92f, 30f), move, "MOVE", new Color(0.25f, 0.3f, 0.34f, 1f));
            DrawKey(new Rect(panel.x + 210f, y, 66f, 30f), bark, "BARK", GlyphBark);
            DrawKey(new Rect(panel.x + 282f, y, 72f, 30f), interact, "USE", GlyphInteract);
            DrawKey(new Rect(panel.x + 360f, y, 72f, 30f), jump, "JUMP", GlyphJump);
            DrawKey(new Rect(panel.x + 438f, y, 70f, 30f), wrestle, "PLAY", GlyphWrestle);
        }

        private void DrawKey(Rect rect, string key, string action, Color color)
        {
            DrawTintedRect(rect, color);
            GUI.Label(rect, key, _small);
            GUI.Label(new Rect(rect.x, rect.yMax + 2f, rect.width, 24f), action, _small);
        }

        private void DrawPadButton(Rect rect, string glyph, string action, Color color)
        {
            DrawTintedRect(rect, color);
            GUI.Label(rect, glyph, _promptGlyph);
            GUI.Label(new Rect(rect.x - 18f, rect.yMax + 1f, rect.width + 36f, 22f), action, _small);
        }

        private void DrawPauseMenu()
        {
            DrawGameplayHud();
            DrawTintedRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight), new Color(0f, 0f, 0f, 0.62f));
            var box = FitPanel(VirtualWidth, VirtualHeight, 660f, 450f);
            float w = box.width;
            float buttonWidth = Mathf.Min(260f, w - 40f);
            float buttonX = box.x + (w - buttonWidth) * 0.5f;
            DrawHudPanel(box);
            GUI.Label(new Rect(box.x, box.y + 18f, w, 44f), "PAWSED", _big);
            GUI.Label(new Rect(box.x + 30f, box.y + 64f, w - 60f, 30f),
                "The tiny dog emergency is safely frozen.", _mid);

            GUI.Label(new Rect(box.x + 30f, box.y + 104f, w - 60f, 28f), "COMFORT", _hud);
            float settingGap = 12f;
            float settingWidth = Mathf.Min(190f, (w - 72f - settingGap * 2f) / 3f);
            float settingX = box.x + (w - settingWidth * 3f - settingGap * 2f) * 0.5f;
            if (DrawPauseButton(new Rect(settingX, box.y + 136f, settingWidth, 44f),
                $"AUDIO: {(_game.AudioEnabled ? "ON" : "OFF")}", 0))
                ActivatePauseOption(0);
            if (DrawPauseButton(new Rect(settingX + settingWidth + settingGap, box.y + 136f, settingWidth, 44f),
                $"RUMBLE: {(_game.RumbleEnabled ? "ON" : "OFF")}", 1))
                ActivatePauseOption(1);
            if (DrawPauseButton(new Rect(settingX + (settingWidth + settingGap) * 2f, box.y + 136f, settingWidth, 44f),
                $"SHAKE: {(_game.CameraShakeEnabled ? "ON" : "OFF")}", 2))
                ActivatePauseOption(2);

            int actionIndex = 3;
            if (_game.ActionTutorialAvailable)
            {
                string tutorialLabel = _game.ShowActionTutorial ? "Skip Tutorial" : "Replay Tutorial";
                if (DrawPauseButton(new Rect(buttonX, box.y + 194f, buttonWidth, 40f), tutorialLabel, actionIndex))
                    ActivatePauseOption(actionIndex);
                actionIndex++;
            }
            else if (_game.FirstMissionControlStripVisible)
            {
                if (DrawPauseButton(new Rect(buttonX, box.y + 194f, buttonWidth, 40f), "Skip Control Reminder", actionIndex))
                    ActivatePauseOption(actionIndex);
                actionIndex++;
            }

            if (DrawPauseButton(new Rect(buttonX, box.y + 252f, buttonWidth, 42f), "Resume", actionIndex))
                ActivatePauseOption(actionIndex);
            if (DrawPauseButton(new Rect(buttonX, box.y + 304f, buttonWidth, 42f), "Mission Select", actionIndex + 1))
                ActivatePauseOption(actionIndex + 1);
            if (DrawPauseButton(new Rect(buttonX, box.y + 356f, buttonWidth, 42f), "Quit Game", actionIndex + 2))
                ActivatePauseOption(actionIndex + 2);

            GUI.Label(new Rect(box.x + 24f, box.yMax - 38f, w - 48f, 24f),
                "D-pad / Arrows choose  •  A / Enter select  •  B / Start resumes", _resultHint);
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
            float h = Mathf.Min(432f, Mathf.Max(1f, VirtualHeight - 24f));
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
            GUI.Label(new Rect(box.x + 12f, box.y + 400f, w - 24f, 20f), _game.GuidanceDebugLabel, _overlay);
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
            GUI.Label(layout.Challenge, _game.SessionGuidanceActivationsLabel, _resultBody);

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
            _missionHud = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _missionHud.normal.textColor = new Color(1f, 0.9f, 0.4f);
            _statusHud = new GUIStyle(GUI.skin.label) { fontSize = GameplayStatusFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            _statusHud.normal.textColor = new Color(0.92f, 0.97f, 1f);
            _objectiveHud = new GUIStyle(GUI.skin.label) { fontSize = GameplayObjectiveFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _objectiveHud.normal.textColor = Color.white;
            _objectiveHud.wordWrap = true;
            _playerChip = new GUIStyle(GUI.skin.label) { fontSize = GameplayIdentityFontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _playerChip.normal.textColor = Color.white;
            _playerChip.wordWrap = true;
            _tutorialHeading = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _tutorialHeading.normal.textColor = new Color(1f, 0.93f, 0.42f);
            _tutorialHeading.wordWrap = true;
            _promptGlyph = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _promptGlyph.normal.textColor = Color.white;
            _promptText = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _promptText.normal.textColor = new Color(0.95f, 0.98f, 1f);
            _promptText.wordWrap = true;
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
