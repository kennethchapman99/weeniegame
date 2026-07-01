using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Cosmetic-only arena set dressing for couch-test appeal. It adds animated background props and
    /// a mission-reactive color accent without changing gameplay transforms, colliders, or rules.
    /// </summary>
    public sealed class ArenaWowSetDressing : MonoBehaviour
    {
        public const string ObjectName = "ArenaWowSetDressing";

        private GameManager _game;
        private Transform _root;
        private Transform _missionMotifRoot;
        private SpriteRenderer _missionSpotlight;
        private SpriteRenderer _missionSpark;
        private GameManager.MissionVariant _lastVariant;

        public bool Built { get; private set; }
        public int SetPieceCount { get; private set; }
        public int AnimatedSetPieceCount { get; private set; }
        public int AttractCharacterCount { get; private set; }
        public int MissionMotifPieceCount { get; private set; }
        public int AnimatedMissionMotifPieceCount { get; private set; }
        public int GeneratedCartoonSpriteCount { get; private set; }
        public int GeneratedMissionSpriteCount { get; private set; }
        public int ShowcaseScenerySetPieceCount { get; private set; }
        public int AnimatedShowcaseSceneryCount { get; private set; }
        public int CharacterVignetteCount { get; private set; }
        public string MissionMotifName { get; private set; } = string.Empty;
        public Color MissionAccentColor { get; private set; }
        public bool HasMissionReactiveSpotlight => _missionSpotlight != null && _missionSpark != null;
        public bool HasMissionReactiveMotifs => _missionMotifRoot != null && MissionMotifPieceCount >= 1 && GeneratedMissionSpriteCount >= 1;
        public bool HasGeneratedCartoonAssets => GeneratedCartoonSpriteCount >= 3;
        public bool HasNoFrozenDogBackdrops => AttractCharacterCount == 0 && CharacterVignetteCount == 0;
        public bool HasShowcaseSceneryPolish => ShowcaseScenerySetPieceCount >= 26 &&
                                                AnimatedShowcaseSceneryCount >= 20 &&
                                                HasNoFrozenDogBackdrops;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "ArenaScene") return;
            if (FindFirstObjectByType<ArenaWowSetDressing>() != null) return;

            var go = new GameObject(ObjectName);
            go.AddComponent<ArenaWowSetDressing>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 60 && _game == null; i++)
            {
                _game = FindFirstObjectByType<GameManager>();
                if (_game == null) yield return null;
            }

            BuildNow();
        }

        public void BuildNow()
        {
            if (Built) return;
            if (_game == null) _game = FindFirstObjectByType<GameManager>();

            _root = new GameObject("WowSetDressingRoot").transform;
            _root.SetParent(transform);
            _root.localPosition = Vector3.zero;

            BuildBackgroundBands();
            BuildFenceLights();
            BuildPawprintRunway();
            BuildCenterStageAccents();
            BuildLayeredYardDepth();
            BuildFamilyShowcaseVignettes();
            BuildAttractPropParade();
            BuildAnimatedSparkles();
            BuildMissionSpotlight();
            BuildMissionMotifRoot();

            Built = true;
            RefreshMissionAccent(true);
        }

        private void Update()
        {
            if (!Built) return;
            RefreshMissionAccent(false);
        }

        private void BuildBackgroundBands()
        {
            AddRect("WowSkyGlowTop", new Vector3(0f, 27.5f, 5f), new Vector3(124f, 12f, 1f),
                new Color(0.42f, 0.74f, 1f, 0.20f), -18, WowMotionKind.Shimmer, 0.08f, 0.15f);
            AddRect("WowYardWarmth", new Vector3(0f, -23.5f, 4.8f), new Vector3(124f, 13f, 1f),
                new Color(0.55f, 0.90f, 0.36f, 0.16f), -17, WowMotionKind.Shimmer, 0.06f, 1.2f);
            AddRect("WowPatioStageWash", new Vector3(-43f, -16f, 4.7f), new Vector3(22f, 12f, 1f),
                new Color(1f, 0.72f, 0.32f, 0.18f), -16, WowMotionKind.Pulse, 0.08f, 0.4f);
            AddRect("WowHouseWindowGlow", new Vector3(43f, 18f, 4.7f), new Vector3(18f, 9f, 1f),
                new Color(0.95f, 0.58f, 0.20f, 0.18f), -16, WowMotionKind.Pulse, 0.10f, 1.1f);
        }

        private void BuildFenceLights()
        {
            for (int i = 0; i < 13; i++)
            {
                float x = -54f + i * 9f;
                float phase = i * 0.43f;
                AddRect($"WowFenceLight{i:00}", new Vector3(x, 30.5f, 4.4f),
                    new Vector3(0.85f, 0.85f, 1f),
                    i % 2 == 0 ? new Color(1f, 0.92f, 0.32f, 0.72f) : new Color(0.42f, 0.95f, 1f, 0.62f),
                    -8, WowMotionKind.Pulse, 0.18f, phase);
                AddRect($"WowFenceLightCord{i:00}", new Vector3(x + 4.5f, 30.1f, 4.45f),
                    new Vector3(8.2f, 0.12f, 1f), new Color(0.08f, 0.12f, 0.12f, 0.32f),
                    -9, WowMotionKind.None, 0f, phase);
            }
        }

        private void BuildPawprintRunway()
        {
            for (int i = 0; i < 18; i++)
            {
                float t = i / 17f;
                float x = Mathf.Lerp(-34f, 34f, t);
                float y = Mathf.Sin(t * Mathf.PI * 2.4f) * 6.5f - 1.5f;
                float side = i % 2 == 0 ? -0.55f : 0.55f;
                AddRect($"WowPawPrint{i:00}", new Vector3(x, y + side, 4.2f),
                    new Vector3(0.7f, 0.42f, 1f),
                    new Color(0.05f, 0.10f, 0.07f, 0.20f),
                    -5, WowMotionKind.Pulse, 0.05f, i * 0.21f);
            }
        }

        private void BuildCenterStageAccents()
        {
            AddRect("WowCenterPlayWash", new Vector3(0f, -4.8f, 3.9f), new Vector3(36f, 7.5f, 1f),
                new Color(0.10f, 0.32f, 0.16f, 0.26f), 0, WowMotionKind.Shimmer, 0.10f, 0.35f);
            AddRect("WowCenterPartyStripeA", new Vector3(-13f, -0.9f, 3.8f), new Vector3(7.2f, 0.36f, 1f),
                new Color(1f, 0.86f, 0.24f, 0.46f), 2, WowMotionKind.Wag, 0.08f, 0.8f);
            AddRect("WowCenterPartyStripeB", new Vector3(13f, -0.9f, 3.8f), new Vector3(7.2f, 0.36f, 1f),
                new Color(0.26f, 0.92f, 1f, 0.42f), 2, WowMotionKind.Wag, 0.08f, 1.2f);
            AddRect("WowCenterCueSparkA", new Vector3(-18f, -6.2f, 3.7f), new Vector3(1.2f, 1.2f, 1f),
                new Color(1f, 0.92f, 0.30f, 0.58f), 3, WowMotionKind.FloatRotate, 0.24f, 1.6f);
            AddRect("WowCenterCueSparkB", new Vector3(18f, -6.4f, 3.7f), new Vector3(1.2f, 1.2f, 1f),
                new Color(0.26f, 0.88f, 1f, 0.52f), 3, WowMotionKind.FloatRotate, 0.24f, 2.0f);
        }

        private void BuildLayeredYardDepth()
        {
            AddShowcaseRect("WowPorchWelcomeMat", new Vector3(43f, 10.4f, 4.15f), new Vector3(8.2f, 1.05f, 1f),
                new Color(0.92f, 0.38f, 0.18f, 0.42f), -3, WowMotionKind.Pulse, 0.025f, 0.4f);
            AddShowcaseRect("WowPorchStepHighlight", new Vector3(43f, 8.9f, 4.12f), new Vector3(10.4f, 0.42f, 1f),
                new Color(1f, 0.82f, 0.42f, 0.34f), -2, WowMotionKind.Shimmer, 0.07f, 1.1f);
            AddShowcaseRect("WowGardenBedLeft", new Vector3(-37f, -25.6f, 4.25f), new Vector3(16f, 1.5f, 1f),
                new Color(0.13f, 0.42f, 0.17f, 0.38f), -7, WowMotionKind.Shimmer, 0.05f, 0.2f);
            AddShowcaseRect("WowGardenBedRight", new Vector3(37f, -25.6f, 4.25f), new Vector3(16f, 1.5f, 1f),
                new Color(0.12f, 0.46f, 0.20f, 0.36f), -7, WowMotionKind.Shimmer, 0.05f, 0.8f);
            AddShowcaseRect("WowShowcaseSightline", new Vector3(0f, -12.2f, 4.18f), new Vector3(54f, 0.28f, 1f),
                new Color(1f, 0.98f, 0.62f, 0.18f), -6, WowMotionKind.Shimmer, 0.12f, 1.5f);

            for (int i = 0; i < 12; i++)
            {
                float x = -52f + i * 9.4f;
                float height = 0.9f + (i % 4) * 0.22f;
                AddShowcaseRect($"WowBreezyGrassBlade{i:00}", new Vector3(x, -27.6f + (i % 3) * 0.42f, 4.1f),
                    new Vector3(0.24f, height, 1f), new Color(0.38f, 0.92f, 0.34f, 0.34f),
                    -5, WowMotionKind.Wag, 0.045f, i * 0.29f);
            }

            for (int i = 0; i < 9; i++)
            {
                float x = 32f + Mathf.Sin(i * 1.9f) * 16f;
                float y = 10f + i * 2.2f;
                AddShowcaseRect($"WowPorchFirefly{i:00}", new Vector3(x, y, 4.05f),
                    Vector3.one * (0.24f + (i % 3) * 0.05f),
                    i % 2 == 0 ? new Color(1f, 0.96f, 0.42f, 0.54f) : new Color(0.46f, 1f, 0.78f, 0.46f),
                    1, WowMotionKind.FloatRotate, 0.34f, i * 0.62f);
            }
        }

        private void BuildFamilyShowcaseVignettes()
        {
            AddShowcaseRect("WowPhotoBoothFrameA", new Vector3(-52f, 18.2f, 4.22f), new Vector3(7.6f, 5.2f, 1f),
                new Color(1f, 0.74f, 0.24f, 0.22f), -7, WowMotionKind.Pulse, 0.025f, 0.3f);
            AddShowcaseRect("WowPhotoBoothFrameB", new Vector3(52f, -4.2f, 4.22f), new Vector3(7.6f, 5.2f, 1f),
                new Color(0.20f, 0.88f, 1f, 0.20f), -7, WowMotionKind.Pulse, 0.025f, 1.0f);

            AddShowcaseSprite("WowPropSnapshotLeft", FinalGameplayArt.EnvironmentPicnicBlanket,
                new Vector3(-52f, 18.2f, 4.0f), Vector3.one * 0.92f, -2,
                WowMotionKind.Bounce, 0.035f, 0.6f);
            AddShowcaseSprite("WowPropSnapshotRight", FinalGameplayArt.EnvironmentFlowerPatch,
                new Vector3(52f, -4.2f, 4.0f), Vector3.one * 0.84f, -2,
                WowMotionKind.Bounce, 0.035f, 1.3f);
        }

        private void BuildAnimatedSparkles()
        {
            for (int i = 0; i < 14; i++)
            {
                float angle = i * 0.82f;
                float radius = 12f + (i % 4) * 5f;
                var pos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle * 0.7f) * 13f, 4.1f);
                AddRect($"WowFloatingSpark{i:00}", pos,
                    new Vector3(0.34f + (i % 3) * 0.08f, 0.34f + (i % 3) * 0.08f, 1f),
                    i % 3 == 0 ? new Color(1f, 0.94f, 0.35f, 0.52f) : new Color(0.58f, 0.95f, 1f, 0.42f),
                    -4, WowMotionKind.FloatRotate, 0.22f, i * 0.37f);
            }
        }

        private void BuildAttractPropParade()
        {
            AddCartoonSprite("WowBackyardPropsParade", FinalGameplayArt.EnvironmentPicnicBlanket,
                new Vector3(-20f, -13.6f, 3.8f), Vector3.one * 2.3f, -3, WowMotionKind.Bounce, 0.08f, 0.1f);
            AddCartoonSprite("WowAdventurePropsEncore", FinalGameplayArt.EnvironmentSteppingStone,
                new Vector3(16.5f, 9.2f, 3.8f), Vector3.one * 1.45f, -3, WowMotionKind.Bounce, 0.06f, 1.4f);
        }

        private void BuildMissionSpotlight()
        {
            _missionSpotlight = AddRect("WowMissionSpotlight", new Vector3(0f, 0f, 4.0f),
                new Vector3(28f, 18f, 1f), new Color(1f, 1f, 1f, 0.10f),
                0, WowMotionKind.Pulse, 0.12f, 0.2f);
            _missionSpark = AddRect("WowMissionSpark", new Vector3(0f, 7.2f, 3.9f),
                new Vector3(2.2f, 2.2f, 1f), new Color(1f, 1f, 1f, 0.55f),
                4, WowMotionKind.FloatRotate, 0.35f, 0.8f);
        }

        private void BuildMissionMotifRoot()
        {
            _missionMotifRoot = new GameObject("WowMissionMotifRoot").transform;
            _missionMotifRoot.SetParent(_root);
            _missionMotifRoot.localPosition = Vector3.zero;
        }

        private SpriteRenderer AddRect(string name, Vector3 position, Vector3 scale, Color color,
            int sortingOrder, WowMotionKind motionKind, float motionAmount, float phase) =>
            AddRect(name, position, scale, color, sortingOrder, motionKind, motionAmount, phase, _root, true);

        private SpriteRenderer AddShowcaseRect(string name, Vector3 position, Vector3 scale, Color color,
            int sortingOrder, WowMotionKind motionKind, float motionAmount, float phase)
        {
            var renderer = AddRect(name, position, scale, color, sortingOrder, motionKind, motionAmount, phase);
            if (renderer != null)
            {
                ShowcaseScenerySetPieceCount++;
                if (motionKind != WowMotionKind.None) AnimatedShowcaseSceneryCount++;
            }
            return renderer;
        }

        private SpriteRenderer AddRect(string name, Vector3 position, Vector3 scale, Color color,
            int sortingOrder, WowMotionKind motionKind, float motionAmount, float phase, Transform parent, bool countInTotals)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteShapeCache.WhiteSquare;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (countInTotals) SetPieceCount++;

            if (motionKind != WowMotionKind.None)
            {
                go.AddComponent<WowSetPieceMotion>().Begin(renderer, motionKind, motionAmount, phase);
                if (countInTotals) AnimatedSetPieceCount++;
            }

            return renderer;
        }

        private void RefreshMissionAccent(bool force)
        {
            var variant = _game == null
                ? GameManager.MissionVariant.BackyardRescue
                : _game.MissionSelectVisible ? _game.SelectedMissionVariant : _game.ActiveMissionVariant;
            if (!force && variant == _lastVariant) return;

            _lastVariant = variant;
            MissionAccentColor = ArenaHud.MissionBadgeColorFor(variant);
            if (_missionSpotlight != null)
                _missionSpotlight.color = new Color(MissionAccentColor.r, MissionAccentColor.g, MissionAccentColor.b, 0.16f);
            if (_missionSpark != null)
                _missionSpark.color = new Color(1f, Mathf.Lerp(0.78f, MissionAccentColor.g, 0.45f), MissionAccentColor.b, 0.62f);

            RebuildMissionMotif(variant, MissionAccentColor);
        }

        private void RebuildMissionMotif(GameManager.MissionVariant variant, Color accent)
        {
            if (_missionMotifRoot == null) return;

            for (int i = _missionMotifRoot.childCount - 1; i >= 0; i--)
                Destroy(_missionMotifRoot.GetChild(i).gameObject);

            MissionMotifPieceCount = 0;
            AnimatedMissionMotifPieceCount = 0;
            GeneratedMissionSpriteCount = 0;

            switch (variant)
            {
                case GameManager.MissionVariant.OperationPeeBreak:
                    MissionMotifName = "Couch-to-door emergency";
                    BuildPeeBreakMotif(accent);
                    break;
                case GameManager.MissionVariant.KitchenFoodFrenzy:
                case GameManager.MissionVariant.SnackHeist:
                case GameManager.MissionVariant.TableStealth:
                    MissionMotifName = "Food heist stage";
                    BuildFoodMotif(accent);
                    break;
                case GameManager.MissionVariant.SquirrelConspiracy:
                case GameManager.MissionVariant.SquirrelSwitcheroo:
                case GameManager.MissionVariant.EagleShadowPanic:
                case GameManager.MissionVariant.CoyotesFence:
                    MissionMotifName = "Threat-watch lane";
                    BuildThreatMotif(accent);
                    break;
                case GameManager.MissionVariant.LeashWalk:
                case GameManager.MissionVariant.WalkCampaign:
                case GameManager.MissionVariant.CarRide:
                case GameManager.MissionVariant.GateCrash:
                case GameManager.MissionVariant.GreatEscape:
                case GameManager.MissionVariant.ChaosMachine:
                    MissionMotifName = "Adventure route";
                    BuildAdventureMotif(accent);
                    break;
                default:
                    MissionMotifName = "Backyard dog props";
                    BuildBackyardDogMotif(accent);
                    break;
            }
        }

        private void BuildPeeBreakMotif(Color accent)
        {
            AddMotifSprite("MotifPeeBreakCartoon", FinalGameplayArt.PeeBreakOpenDoor, new Vector3(-43f, 25f, 3.55f),
                Vector3.one * 2.8f, -1, WowMotionKind.FloatRotate, 0.03f, 0.1f);
            AddMotifSprite("MotifPeeBreakSparkA", FinalGameplayArt.PickupSparkle, new Vector3(-47.5f, 22.1f, 3.5f),
                Vector3.one * 0.72f, 0, WowMotionKind.Pulse, 0.12f, 0.8f);
            AddMotifSprite("MotifPeeBreakSparkB", FinalGameplayArt.SuccessPop, new Vector3(-39.5f, 28f, 3.5f),
                Vector3.one * 0.78f, 0, WowMotionKind.Pulse, 0.12f, 1.4f);
        }

        private void BuildFoodMotif(Color accent)
        {
            AddMotifSprite("MotifFoodHeistCartoon", FinalGameplayArt.MissionSnackPlate, new Vector3(-43f, 25f, 3.55f),
                Vector3.one * 2.8f, -1, WowMotionKind.Bounce, 0.03f, 0.2f);
            AddMotifSprite("MotifFoodSparkA", FinalGameplayArt.PickupSparkle, new Vector3(-47.5f, 22f, 3.5f),
                Vector3.one * 0.72f, 0, WowMotionKind.Pulse, 0.12f, 0.9f);
            AddMotifSprite("MotifFoodSparkB", FinalGameplayArt.SuccessPop, new Vector3(-39.5f, 27.9f, 3.5f),
                Vector3.one * 0.78f, 0, WowMotionKind.Pulse, 0.12f, 1.5f);
        }

        private void BuildThreatMotif(Color accent)
        {
            AddMotifSprite("MotifThreatWatchCartoon", FinalGameplayArt.WarningAlert, new Vector3(-43f, 25f, 3.55f),
                Vector3.one * 2.8f, -1, WowMotionKind.FloatRotate, 0.03f, 0.3f);
            AddMotifSprite("MotifThreatAlertA", FinalGameplayArt.WarningAlert, new Vector3(-47.5f, 22f, 3.5f),
                Vector3.one * 0.72f, 0, WowMotionKind.Pulse, 0.12f, 0.9f);
            AddMotifSprite("MotifThreatAlertB", FinalGameplayArt.BarkBurst, new Vector3(-39.5f, 27.9f, 3.5f),
                Vector3.one * 0.78f, 0, WowMotionKind.Pulse, 0.12f, 1.5f);
        }

        private void BuildAdventureMotif(Color accent)
        {
            AddMotifSprite("MotifAdventureRouteCartoon", FinalGameplayArt.EnvironmentLeashRoute, new Vector3(-43f, 25f, 3.55f),
                Vector3.one * 2.8f, -1, WowMotionKind.Bounce, 0.03f, 0.4f);
            AddMotifSprite("MotifAdventureSparkA", FinalGameplayArt.PickupSparkle, new Vector3(-47.5f, 22f, 3.5f),
                Vector3.one * 0.72f, 0, WowMotionKind.Pulse, 0.12f, 0.9f);
            AddMotifSprite("MotifAdventureSparkB", FinalGameplayArt.SuccessPop, new Vector3(-39.5f, 27.9f, 3.5f),
                Vector3.one * 0.78f, 0, WowMotionKind.Pulse, 0.12f, 1.5f);
        }

        private void BuildBackyardDogMotif(Color accent)
        {
            AddMotifSprite("MotifBackyardDogCartoon", FinalGameplayArt.EnvironmentPicnicBlanket, new Vector3(-43f, 25f, 3.55f),
                Vector3.one * 2.8f, -1, WowMotionKind.Bounce, 0.03f, 0.5f);
            AddMotifSprite("MotifBackyardSparkA", FinalGameplayArt.PickupSparkle, new Vector3(-47.5f, 22f, 3.5f),
                Vector3.one * 0.72f, 0, WowMotionKind.Pulse, 0.12f, 1.0f);
            AddMotifSprite("MotifBackyardSparkB", FinalGameplayArt.SuccessPop, new Vector3(-39.5f, 27.9f, 3.5f),
                Vector3.one * 0.78f, 0, WowMotionKind.Pulse, 0.12f, 1.6f);
        }

        private SpriteRenderer AddCartoonSprite(string name, string resourcePath, Vector3 position, Vector3 scale,
            int sortingOrder, WowMotionKind motionKind, float motionAmount, float phase)
        {
            var renderer = AddSprite(name, resourcePath, position, scale, sortingOrder, motionKind, motionAmount, phase,
                _root, true);
            if (renderer != null) GeneratedCartoonSpriteCount++;
            return renderer;
        }

        private SpriteRenderer AddMotifSprite(string name, string resourcePath, Vector3 position, Vector3 scale,
            int sortingOrder, WowMotionKind motionKind, float motionAmount, float phase)
        {
            var renderer = AddSprite(name, resourcePath, position, scale, sortingOrder, motionKind, motionAmount, phase,
                _missionMotifRoot, false);
            if (renderer != null)
            {
                MissionMotifPieceCount++;
                GeneratedCartoonSpriteCount++;
                GeneratedMissionSpriteCount++;
                if (motionKind != WowMotionKind.None) AnimatedMissionMotifPieceCount++;
            }
            return renderer;
        }

        private SpriteRenderer AddShowcaseSprite(string name, string resourcePath, Vector3 position, Vector3 scale,
            int sortingOrder, WowMotionKind motionKind, float motionAmount, float phase)
        {
            var renderer = AddSprite(name, resourcePath, position, scale, sortingOrder, motionKind, motionAmount, phase,
                _root, true);
            if (renderer != null)
            {
                ShowcaseScenerySetPieceCount++;
                GeneratedCartoonSpriteCount++;
                if (motionKind != WowMotionKind.None) AnimatedShowcaseSceneryCount++;
            }
            return renderer;
        }

        private SpriteRenderer AddSprite(string name, string resourcePath, Vector3 position, Vector3 scale,
            int sortingOrder, WowMotionKind motionKind, float motionAmount, float phase, Transform parent, bool countInTotals)
        {
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;

            if (countInTotals) SetPieceCount++;
            if (motionKind != WowMotionKind.None)
            {
                go.AddComponent<WowSetPieceMotion>().Begin(renderer, motionKind, motionAmount, phase);
                if (countInTotals) AnimatedSetPieceCount++;
            }

            return renderer;
        }

        private enum WowMotionKind
        {
            None,
            Pulse,
            Shimmer,
            FloatRotate,
            Bounce,
            Wag
        }

        private sealed class WowSetPieceMotion : MonoBehaviour
        {
            private SpriteRenderer _renderer;
            private WowMotionKind _kind;
            private Vector3 _basePosition;
            private Vector3 _baseScale;
            private Color _baseColor;
            private float _amount;
            private float _phase;

            public void Begin(SpriteRenderer renderer, WowMotionKind kind, float amount, float phase)
            {
                _renderer = renderer;
                _kind = kind;
                _amount = amount;
                _phase = phase;
                _basePosition = transform.position;
                _baseScale = transform.localScale;
                _baseColor = renderer != null ? renderer.color : Color.white;
            }

            private void Update()
            {
                float wave = Mathf.Sin(Time.time * 1.35f + _phase);
                switch (_kind)
                {
                    case WowMotionKind.Pulse:
                        transform.localScale = _baseScale * (1f + wave * _amount);
                        SetAlpha(1f + wave * _amount * 1.4f);
                        break;
                    case WowMotionKind.Shimmer:
                        SetAlpha(1f + wave * _amount * 2.2f);
                        break;
                    case WowMotionKind.FloatRotate:
                        transform.position = _basePosition + new Vector3(0f, wave * _amount, 0f);
                        transform.Rotate(0f, 0f, Time.deltaTime * (18f + _phase * 3f));
                        SetAlpha(1f + wave * _amount);
                        break;
                    case WowMotionKind.Bounce:
                        transform.position = _basePosition + new Vector3(0f, Mathf.Abs(wave) * _amount, 0f);
                        SetAlpha(1f + wave * _amount * 0.45f);
                        break;
                    case WowMotionKind.Wag:
                        transform.localRotation = Quaternion.Euler(0f, 0f, wave * (_amount * 60f));
                        SetAlpha(1f + wave * _amount * 0.4f);
                        break;
                }
            }

            private void SetAlpha(float multiplier)
            {
                if (_renderer == null) return;
                var color = _baseColor;
                color.a = Mathf.Clamp01(_baseColor.a * multiplier);
                _renderer.color = color;
            }
        }
    }
}
