using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Cosmetic-only arena set dressing for couch-test appeal. The painted backyard plate owns the
    /// background (one-background rule), so this layer only adds authored sprite accents and a
    /// mission-reactive motif — never placeholder rectangles — and changes no gameplay transforms,
    /// colliders, or rules.
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

        /// <summary>
        /// Live count of runtime-primitive (white square / unnamed) sprites in the wow layer. The
        /// one-background rule requires this to stay zero: the painted plate is the background and
        /// every accent on top of it must be authored art.
        /// </summary>
        public int PlaceholderRectCount
        {
            get
            {
                int count = 0;
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                    if (SpriteShapeCache.IsPlaceholder(renderer.sprite)) count++;
                return count;
            }
        }

        public bool HasShowcaseSceneryPolish => PlaceholderRectCount == 0 &&
                                                ShowcaseScenerySetPieceCount >= 2 &&
                                                AnimatedShowcaseSceneryCount >= 2 &&
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

            BuildFamilyShowcaseVignettes();
            BuildAttractPropParade();
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

        private void BuildFamilyShowcaseVignettes()
        {
            AddShowcaseSprite("WowPropSnapshotLeft", FinalGameplayArt.EnvironmentPicnicBlanket,
                new Vector3(-52f, 18.2f, 4.0f), Vector3.one * 0.92f, -2,
                WowMotionKind.Bounce, 0.035f, 0.6f);
            AddShowcaseSprite("WowPropSnapshotRight", FinalGameplayArt.EnvironmentFlowerPatch,
                new Vector3(52f, -4.2f, 4.0f), Vector3.one * 0.84f, -2,
                WowMotionKind.Bounce, 0.035f, 1.3f);
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
            _missionSpotlight = AddSprite("WowMissionSpotlight", FinalGameplayArt.DogFxGroundGlow,
                new Vector3(0f, 0f, 4.0f), WorldSizeScale(FinalGameplayArt.DogFxGroundGlow, new Vector2(28f, 18f)),
                0, WowMotionKind.Pulse, 0.12f, 0.2f, _root, true, new Color(1f, 1f, 1f, 0.10f));
            _missionSpark = AddSprite("WowMissionSpark", FinalGameplayArt.PickupSparkle,
                new Vector3(0f, 7.2f, 3.9f), WorldSizeScale(FinalGameplayArt.PickupSparkle, new Vector2(2.2f, 2.2f)),
                4, WowMotionKind.FloatRotate, 0.35f, 0.8f, _root, true, new Color(1f, 1f, 1f, 0.55f));
        }

        private void BuildMissionMotifRoot()
        {
            _missionMotifRoot = new GameObject("WowMissionMotifRoot").transform;
            _missionMotifRoot.SetParent(_root);
            _missionMotifRoot.localPosition = Vector3.zero;
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
            int sortingOrder, WowMotionKind motionKind, float motionAmount, float phase, Transform parent,
            bool countInTotals, Color? tint = null)
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
            renderer.color = tint ?? Color.white;

            if (countInTotals) SetPieceCount++;
            if (motionKind != WowMotionKind.None)
            {
                go.AddComponent<WowSetPieceMotion>().Begin(renderer, motionKind, motionAmount, phase);
                if (countInTotals) AnimatedSetPieceCount++;
            }

            return renderer;
        }

        private static Vector3 WorldSizeScale(string resourcePath, Vector2 worldSize)
        {
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return Vector3.one;
            return new Vector3(
                worldSize.x / Mathf.Max(0.01f, sprite.bounds.size.x),
                worldSize.y / Mathf.Max(0.01f, sprite.bounds.size.y),
                1f);
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
