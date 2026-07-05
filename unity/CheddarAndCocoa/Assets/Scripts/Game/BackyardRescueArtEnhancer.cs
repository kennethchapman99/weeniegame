using System.Collections;
using CheddarAndCocoa.Dogs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Additive art pass for the playable backyard slice. It layers real/draft sprites, shadows, and
    /// art-driven VFX over the existing generated gameplay objects without changing collision or rules.
    /// </summary>
    public sealed class BackyardRescueArtEnhancer : MonoBehaviour
    {
        private GameManager _game;
        private ArtSpriteOverlay _ropeOverlay;
        private string _lastScoreLabel = string.Empty;
        private GameManager.FeedbackKind _lastFeedback;
        private float _nextAmbientAt;
        private DogController _cocoa;
        private float _nextSunbeamCheckAt;
        private float _nextSquirrelMumbleAt;

        public bool Enhanced { get; private set; }
        public int OverlayCount { get; private set; }
        public int EnvironmentArtOverlayCount { get; private set; }
        public int BuildingArtOverlayCount { get; private set; }
        public bool UsesNoDogPaintedBackyardPlate { get; private set; }
        public int VfxSpawnCount { get; private set; }
        // Running gag from the design bible: "Cocoa has legal ownership of all sunbeams." Purely
        // decorative - same risk profile as the ambient leaf pop, no gameplay effect.
        public int SunbeamClaimCount { get; private set; }
        // Running gag: "The squirrel has villain monologues nobody understands." Purely decorative.
        public int SquirrelMumbleCount { get; private set; }
        public string LastEnhancementSummary { get; private set; } = string.Empty;
        public bool BackyardThreatsHaveReadableShadows =>
            HasReadableShadow(_game != null ? _game.SquirrelObject : null, "CouchReadableSquirrelShadow") &&
            HasReadableShadow(_game != null ? _game.PredatorObject : null, "CouchReadableEagleShadow");
        // The squirrel and eagle are drawn by ThreatReadabilityAnimator alone; the enhancer must not
        // layer a second semi-transparent reference sprite over the animated character.
        public bool ThreatVisualsHaveSingleOwner =>
            HasSingleThreatVisualOwner(_game != null ? _game.SquirrelObject : null) &&
            HasSingleThreatVisualOwner(_game != null ? _game.PredatorObject : null);

        // One-background rule: the painted plate renders at full opacity and no runtime-primitive
        // rectangle stays visible anywhere in the yard environment. The bootstrap rectangles remain
        // in the hierarchy only as invisible anchors for the authored overlays.
        public bool PaintedPlateIsSoleYardBackground
        {
            get
            {
                var env = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
                if (env == null) return false;
                var plate = env.transform.Find("ActualNoDogBackyardPlate");
                if (plate == null || !plate.TryGetComponent<SpriteRenderer>(out var plateRenderer)) return false;
                if (plateRenderer.color.a < 0.999f) return false;

                foreach (var renderer in env.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer == plateRenderer) continue;
                    if (SpriteShapeCache.IsPlaceholder(renderer.sprite) && renderer.color.a > 0.001f)
                        return false;
                }
                return true;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "ArenaScene") return;
            var go = new GameObject("BackyardRescueArtEnhancer");
            go.AddComponent<BackyardRescueArtEnhancer>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 60 && _game == null; i++)
            {
                _game = FindFirstObjectByType<GameManager>();
                if (_game == null) yield return null;
            }

            if (_game == null)
            {
                LastEnhancementSummary = "No GameManager found";
                yield break;
            }

            EnhanceNow();
        }

        public void EnhanceNow()
        {
            if (_game == null) _game = FindFirstObjectByType<GameManager>();
            if (_game == null) return;
            if (Enhanced)
            {
                AddPaintedBackyardPlate();
                return;
            }

            OverlayCount = 0;
            EnhanceDogShadows();
            AddPaintedBackyardPlate();
            AddReadableGroundShadow(_game.SquirrelObject, "CouchReadableSquirrelShadow",
                new Vector3(0f, -0.34f, 0.07f), new Vector3(1.05f, 0.16f, 1f), 4, 0.18f);
            AddReadableGroundShadow(_game.PredatorObject, "CouchReadableEagleShadow",
                new Vector3(0f, -1.42f, 0.07f), new Vector3(1.95f, 0.25f, 1f), 5, 0.2f);

            _ropeOverlay = AddOverlay(_game.RopeObject, RuntimeArtSpriteFactory.RuntimeSpriteId.RopeToy, new Vector3(0f, 0f, -0.32f), new Vector3(0.035f, 0.035f, 1f), 32, new Color(1f, 1f, 1f, 0.92f));
            AddGeneratedEnvironmentPropArt();
            AddGeneratedBuildingPropArt();

            Enhanced = true;
            _nextAmbientAt = Time.time + 2.5f;
            _nextSunbeamCheckAt = Time.time + 6f;
            _nextSquirrelMumbleAt = Time.time + 8f;
            LastEnhancementSummary = $"Art overlays active: {OverlayCount}, environment overlays: {EnvironmentArtOverlayCount}, building overlays: {BuildingArtOverlayCount}";
            _lastFeedback = _game.LastFeedback;
            _lastScoreLabel = _game.LastScoreEventLabel;
        }

        private void Update()
        {
            if (_game == null || !Enhanced) return;

            if (_game.LastFeedback != _lastFeedback)
            {
                ReactToFeedback(_game.LastFeedback);
                _lastFeedback = _game.LastFeedback;
            }

            if (_game.LastScoreEventLabel != _lastScoreLabel)
            {
                ReactToScore(_game.LastScoreEventLabel);
                _lastScoreLabel = _game.LastScoreEventLabel;
            }

            AddPaintedBackyardPlate();

            if (Time.time >= _nextAmbientAt)
            {
                _nextAmbientAt = Time.time + 2.5f;
                if (_game.ActiveMissionVariant == GameManager.MissionVariant.BackyardRescue)
                    SpawnAmbientLeafPop();
            }

            if (Time.time >= _nextSunbeamCheckAt)
            {
                _nextSunbeamCheckAt = Time.time + 6f;
                if (_game.ActiveMissionVariant == GameManager.MissionVariant.BackyardRescue)
                    TrySpawnSunbeamClaim();
            }

            if (Time.time >= _nextSquirrelMumbleAt)
            {
                _nextSquirrelMumbleAt = Time.time + 8f;
                if (_game.ActiveMissionVariant == GameManager.MissionVariant.BackyardRescue)
                    TrySpawnSquirrelMumble();
            }
        }

        private ArtSpriteOverlay AddOverlay(GameObject target, RuntimeArtSpriteFactory.RuntimeSpriteId spriteId, Vector3 localPosition, Vector3 localScale, int sortingOrder, Color tint)
        {
            if (target == null) return null;
            Sprite sprite = RuntimeArtSpriteFactory.Get(spriteId);
            if (sprite == null) return null;

            var overlay = target.GetComponent<ArtSpriteOverlay>() ?? target.AddComponent<ArtSpriteOverlay>();
            overlay.Init(sprite, localPosition, localScale, sortingOrder, tint, true);
            OverlayCount++;
            return overlay;
        }

        private void SetOverlaySprite(ArtSpriteOverlay overlay, RuntimeArtSpriteFactory.RuntimeSpriteId spriteId)
        {
            if (overlay == null) return;
            Sprite sprite = RuntimeArtSpriteFactory.Get(spriteId);
            if (sprite != null) overlay.SetSprite(sprite);
        }

        private void EnhanceDogShadows()
        {
            foreach (var feedback in FindObjectsByType<DogReadabilityFeedback>(FindObjectsSortMode.None))
            {
                if (feedback == null) continue;
                var shadow = feedback.transform.Find("ActualDogShadow");
                if (shadow != null) continue;

                var go = new GameObject("ActualDogShadow");
                go.transform.SetParent(feedback.transform);
                go.transform.localPosition = new Vector3(0f, -0.55f, 0.08f);
                go.transform.localScale = new Vector3(1.35f, 0.22f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteShapeCache.WhiteSquare;
                sr.color = new Color(0f, 0f, 0f, 0.18f);
                sr.sortingOrder = 3;
                OverlayCount++;
            }
        }

        private void AddPaintedBackyardPlate()
        {
            var env = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            if (env == null) return;
            if (env.transform.Find("ActualNoDogBackyardPlate") != null)
            {
                UsesNoDogPaintedBackyardPlate = true;
                return;
            }

            Sprite sprite = FinalGameplayArt.Load(FinalGameplayArt.EnvironmentBackyardPlate);
            if (sprite == null) return;

            // One-background rule: the plate covers the entire yard at full opacity, and every
            // bootstrap rectangle behind it goes fully invisible (they stay in the hierarchy only
            // as positional anchors for authored overlays and gameplay staging).
            var go = new GameObject("ActualNoDogBackyardPlate");
            go.transform.SetParent(env.transform);
            go.transform.localPosition = new Vector3(0f, 0f, -0.42f);
            go.transform.localRotation = Quaternion.identity;
            float xScale = ArenaWorldScale.BackyardWidth / Mathf.Max(0.01f, sprite.bounds.size.x);
            float yScale = ArenaWorldScale.BackyardHeight / Mathf.Max(0.01f, sprite.bounds.size.y);
            go.transform.localScale = new Vector3(xScale, yScale, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -8;
            renderer.color = Color.white;

            foreach (var fallback in env.GetComponentsInChildren<SpriteRenderer>())
            {
                if (fallback == renderer || fallback.sortingOrder > 0) continue;
                if (!SpriteShapeCache.IsPlaceholder(fallback.sprite)) continue;
                var color = fallback.color;
                color.a = 0f;
                fallback.color = color;
            }

            OverlayCount++;
            EnvironmentArtOverlayCount++;
            UsesNoDogPaintedBackyardPlate = true;
        }

        private void AddGeneratedEnvironmentPropArt()
        {
            var env = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            if (env == null) return;
            var root = env.transform;

            AddEnvironmentOverlay(root, "HousePatioDistrict", FinalGameplayArt.EnvironmentHousePatio,
                new Vector2(32f, 18f), -4, new Color(1f, 1f, 1f, 0.96f));
            AddEnvironmentOverlay(root, "BackDoorExterior", FinalGameplayArt.EnvironmentBackDoor,
                new Vector2(4.6f, 6.5f), -3, Color.white);
            AddEnvironmentOverlay(root, "PeeBreakOutdoorPayoffPath", FinalGameplayArt.EnvironmentPeeBreakPath,
                new Vector2(18f, 6f), -3, new Color(1f, 1f, 1f, 0.9f),
                GameManager.MissionVariant.OperationPeeBreak);
            // Snack/laundry districts are mission staging: in every other mission the generated
            // table/laundry art was "weird stuff around" (couch feedback), so they only render
            // while their mission is being played.
            AddEnvironmentOverlay(root, "SnackDistrictZone", FinalGameplayArt.EnvironmentSnackTable,
                new Vector2(14f, 8f), -4, Color.white,
                GameManager.MissionVariant.SnackHeist);
            AddEnvironmentOverlay(root, "LaundryDistrictZone", FinalGameplayArt.EnvironmentLaundryCorner,
                new Vector2(14f, 9f), -4, Color.white,
                GameManager.MissionVariant.SockPanic);
            // One-background rule, couch test #2: no OpenLawnDistrict overlay. Stretching the
            // yard_lawn_landmarks icon across a quarter of the yard drew a giant nonsense X panel
            // over the painted plate; the plate alone is the lawn.
            // Couch feedback #3: the flat vector shade-tree/garden-bed overlays clashed hard with
            // the painted plate (which already paints the big tree, swing, and beds), so they are
            // retired. The pond overlays are gone too — BackyardPoolZone owns the water now.
            AddEnvironmentOverlay(root, "PicnicBlanket", FinalGameplayArt.EnvironmentPicnicBlanket,
                new Vector2(10.5f, 6.5f), -4, Color.white);
            AddEnvironmentOverlay(root, "Sandbox", FinalGameplayArt.EnvironmentSandbox,
                new Vector2(9f, 6.5f), -4, Color.white);
            // Threat lanes are gameplay cues for exactly one mission each; in every other mission
            // they were stretched trapezoid rows of visual noise (couch test #2).
            AddEnvironmentOverlay(root, "EagleShadowSweepLane", FinalGameplayArt.EnvironmentThreatLane,
                new Vector2(34f, 7f), -4, new Color(1f, 1f, 1f, 0.82f),
                GameManager.MissionVariant.EagleShadowPanic);
            AddEnvironmentOverlay(root, "CoyoteFencePressureLane", FinalGameplayArt.EnvironmentThreatLane,
                new Vector2(30f, 5f), -4, new Color(1f, 1f, 1f, 0.72f),
                GameManager.MissionVariant.CoyotesFence);
            // The giant lane-warning banner is retired: the eagle shadow, huddle labels, and
            // sweep-lane cue already carry the warning without a screen-wide arrow strip.
            AddEnvironmentOverlay(root, "FenceRailTop", FinalGameplayArt.EnvironmentFenceRun,
                new Vector2(58f, 4.2f), -4, new Color(1f, 1f, 1f, 0.86f));
            AddEnvironmentOverlay(root, "FenceRailBottom", FinalGameplayArt.EnvironmentFenceRun,
                new Vector2(58f, 4.2f), -4, new Color(1f, 1f, 1f, 0.82f));

            for (int i = 0; i < 6; i++)
                AddEnvironmentOverlay(root, $"ScentTrailPatch_{i}", FinalGameplayArt.EnvironmentScentTrail,
                    new Vector2(4.2f, 3.2f), -3, new Color(1f, 1f, 1f, 0.88f),
                    GameManager.MissionVariant.ScentSearch);
            for (int i = 0; i < 5; i++)
                AddEnvironmentOverlay(root, $"LeashRouteStone_{i}", FinalGameplayArt.EnvironmentLeashRoute,
                    new Vector2(4.7f, 3.5f), -3, new Color(1f, 1f, 1f, 0.86f),
                    GameManager.MissionVariant.LeashWalk, GameManager.MissionVariant.WalkCampaign);
            for (int i = 0; i < 9; i++)
                AddEnvironmentOverlay(root, $"SteppingStone_{i}", FinalGameplayArt.EnvironmentSteppingStone,
                    new Vector2(3.2f, 2.2f), -3, new Color(1f, 1f, 1f, 0.88f));
            for (int i = 0; i < 5; i++)
                AddEnvironmentOverlay(root, $"Flower_{i}", FinalGameplayArt.EnvironmentFlowerPatch,
                    new Vector2(1.9f, 2.1f), -2, Color.white);
            // The cover bushes mark the Eagle Shadow hide zones; with the placeholder rectangles
            // invisible they need authored bush art to keep "HIDE HERE" readable.
            for (int i = 0; i < 3; i++)
                AddEnvironmentOverlay(root, $"CoverBush_{i}", FinalGameplayArt.Bush,
                    new Vector2(7.6f, 5.6f), -3, Color.white);
        }

        private void AddGeneratedBuildingPropArt()
        {
            var env = GameObject.Find(ArenaArtCatalog.BackyardEnvironmentObjectName);
            if (env == null) return;
            var root = env.transform;

            AddBuildingOverlay(root, "HousePatioDistrict", FinalGameplayArt.BuildingHomeExterior,
                new Vector2(20f, 13f), -2, new Color(1f, 1f, 1f, 0.98f), new Vector3(0f, 1.2f, -0.29f));
            AddBuildingOverlay(root, "BackDoorExterior", FinalGameplayArt.BuildingBackPorchEntry,
                new Vector2(6.2f, 7.6f), -1, Color.white, new Vector3(0f, 0.35f, -0.3f));
            AddWorldBuildingArt("ActualBuildingArtYardShed", FinalGameplayArt.BuildingYardShedStorage,
                new Vector3(-36f, 12f, 0.16f), new Vector2(8.5f, 7.4f), -3, new Color(1f, 1f, 1f, 0.94f));
        }

        private void AddEnvironmentOverlay(Transform environmentRoot, string targetName, string resourcePath,
            Vector2 worldSize, int sortingOrder, Color tint,
            params GameManager.MissionVariant[] onlyDuringMissions)
        {
            var target = environmentRoot.Find(targetName);
            if (target == null || target.Find("ActualEnvironmentArtOverlay") != null) return;

            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return;

            var go = new GameObject("ActualEnvironmentArtOverlay");
            go.transform.SetParent(target);
            go.transform.localPosition = new Vector3(0f, 0f, -0.24f);
            go.transform.localRotation = Quaternion.identity;
            Vector3 inheritedScale = target.lossyScale;
            float xScale = worldSize.x / Mathf.Max(0.01f, sprite.bounds.size.x * Mathf.Abs(inheritedScale.x));
            float yScale = worldSize.y / Mathf.Max(0.01f, sprite.bounds.size.y * Mathf.Abs(inheritedScale.y));
            go.transform.localScale = new Vector3(xScale, yScale, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = tint;

            if (target.TryGetComponent<SpriteRenderer>(out var fallback) &&
                SpriteShapeCache.IsPlaceholder(fallback.sprite))
            {
                var color = fallback.color;
                color.a = 0f;
                fallback.color = color;
            }

            if (onlyDuringMissions != null && onlyDuringMissions.Length > 0)
                MissionScopedScenery.Attach(go, onlyDuringMissions);

            OverlayCount++;
            EnvironmentArtOverlayCount++;
        }

        private void AddBuildingOverlay(Transform environmentRoot, string targetName, string resourcePath,
            Vector2 worldSize, int sortingOrder, Color tint, Vector3 localPosition)
        {
            var target = environmentRoot.Find(targetName);
            if (target == null || target.Find("ActualBuildingArtOverlay") != null) return;

            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return;

            var go = new GameObject("ActualBuildingArtOverlay");
            go.transform.SetParent(target);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            Vector3 inheritedScale = target.lossyScale;
            float xScale = worldSize.x / Mathf.Max(0.01f, sprite.bounds.size.x * Mathf.Abs(inheritedScale.x));
            float yScale = worldSize.y / Mathf.Max(0.01f, sprite.bounds.size.y * Mathf.Abs(inheritedScale.y));
            go.transform.localScale = new Vector3(xScale, yScale, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = tint;

            OverlayCount++;
            BuildingArtOverlayCount++;
        }

        private void AddWorldBuildingArt(string name, string resourcePath, Vector3 position, Vector2 worldSize, int sortingOrder, Color tint)
        {
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null || GameObject.Find(name) != null) return;

            var go = new GameObject(name);
            go.transform.position = position;
            float xScale = worldSize.x / Mathf.Max(0.01f, sprite.bounds.size.x);
            float yScale = worldSize.y / Mathf.Max(0.01f, sprite.bounds.size.y);
            go.transform.localScale = new Vector3(xScale, yScale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = tint;
            OverlayCount++;
            BuildingArtOverlayCount++;
        }

        private void ReactToFeedback(GameManager.FeedbackKind feedback)
        {
            switch (feedback)
            {
                // Squirrel/eagle state art is owned by ThreatReadabilityAnimator (driven through
                // SetActorState labels); the enhancer only adds transient VFX around the beat.
                case GameManager.FeedbackKind.SquirrelStealing:
                    SpawnAt(_game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert, 0.032f, new Color(1f, 0.88f, 0.35f, 0.75f));
                    break;
                case GameManager.FeedbackKind.SquirrelScared:
                    SpawnAt(_game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.SuccessPop, 0.033f, new Color(0.7f, 1f, 0.65f, 0.78f));
                    break;
                case GameManager.FeedbackKind.SquirrelStoleFood:
                    SpawnAt(_game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.FailPuff, 0.034f, new Color(1f, 0.35f, 0.25f, 0.78f));
                    break;
                case GameManager.FeedbackKind.PredatorHuddle:
                    SpawnAt(_game.PredatorObject, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert, 0.04f, new Color(1f, 0.2f, 0.2f, 0.7f));
                    break;
                case GameManager.FeedbackKind.PredatorAttack:
                    SpawnAt(_game.PredatorObject, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert, 0.04f, new Color(1f, 0.2f, 0.2f, 0.74f));
                    break;
                case GameManager.FeedbackKind.PartnerRescue:
                    SpawnAt(_game.PredatorObject, RuntimeArtSpriteFactory.RuntimeSpriteId.RescueBurst, 0.04f, new Color(0.55f, 1f, 0.7f, 0.82f));
                    break;
                case GameManager.FeedbackKind.UnitedBark:
                    SpawnTeamBarkBurst();
                    break;
                case GameManager.FeedbackKind.TugTogether:
                    _ropeOverlay?.Pulse(0.45f, 0.14f);
                    SpawnAt(_game.RopeObject, RuntimeArtSpriteFactory.RuntimeSpriteId.SuccessPop, 0.045f, new Color(1f, 0.92f, 0.35f, 0.9f));
                    break;
                case GameManager.FeedbackKind.LevelClear:
                    SetOverlaySprite(_ropeOverlay, RuntimeArtSpriteFactory.RuntimeSpriteId.RopeComplete);
                    SpawnAt(_game.RopeObject, RuntimeArtSpriteFactory.RuntimeSpriteId.SuccessPop, 0.08f, new Color(0.65f, 1f, 0.55f, 0.9f));
                    SpawnTeamBarkBurst();
                    break;
                case GameManager.FeedbackKind.GameOver:
                    SpawnAt(_game.PredatorObject != null && _game.PredatorObject.activeInHierarchy ? _game.PredatorObject : _game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.FailPuff, 0.07f, new Color(1f, 0.35f, 0.3f, 0.9f));
                    break;
            }
        }

        private void ReactToScore(string label)
        {
            if (string.IsNullOrEmpty(label)) return;
            if (label.Contains("WEENIE") || label.Contains("SNACK") || label.Contains("SOCK"))
                SpawnAt(_game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.PickupSparkle, 0.04f, new Color(1f, 0.95f, 0.45f, 0.85f));
        }

        private static bool HasSingleThreatVisualOwner(GameObject actor)
        {
            if (actor == null) return false;
            // Mission prop attachments may add their own overlay later; the invariant here is that
            // the enhancer itself never layers reference art over the animated threat characters.
            return actor.GetComponent<ThreatReadabilityAnimator>() != null;
        }

        private void SpawnTeamBarkBurst()
        {
            Vector3 pos = Vector3.zero;
            int count = 0;
            foreach (var dog in FindObjectsByType<DogController>(FindObjectsSortMode.None))
            {
                pos += dog.transform.position;
                count++;
            }
            if (count > 0) pos /= count;
            pos += Vector3.up * 0.65f;
            BackyardArtVfxPulse.Spawn(pos, RuntimeArtSpriteFactory.RuntimeSpriteId.BarkRing, new Vector3(0.11f, 0.11f, 1f), 59, new Color(1f, 1f, 1f, 0.65f), 0.65f, 90f);
            BackyardArtVfxPulse.Spawn(pos, RuntimeArtSpriteFactory.RuntimeSpriteId.BarkBurst, new Vector3(0.075f, 0.075f, 1f), 60, new Color(1f, 1f, 1f, 0.9f), 0.55f, 120f);
            VfxSpawnCount += 2;
        }

        private void SpawnAt(GameObject target, RuntimeArtSpriteFactory.RuntimeSpriteId spriteId, float scale, Color tint)
        {
            if (target == null) return;
            BackyardArtVfxPulse.Spawn(target.transform.position + Vector3.up * 0.8f, spriteId, new Vector3(scale, scale, 1f), 58, tint, 0.55f, 60f);
            VfxSpawnCount++;
        }

        private void AddReadableGroundShadow(GameObject target, string name, Vector3 localPosition,
            Vector3 localScale, int sortingOrder, float alpha)
        {
            if (target == null || target.transform.Find(name) != null) return;

            var go = new GameObject(name);
            go.transform.SetParent(target.transform);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteShapeCache.WhiteSquare;
            sr.color = new Color(0f, 0f, 0f, alpha);
            sr.sortingOrder = sortingOrder;
            OverlayCount++;
        }

        private static bool HasReadableShadow(GameObject target, string name)
        {
            if (target == null) return false;
            var shadow = target.transform.Find(name);
            if (shadow == null) return false;
            if (!shadow.TryGetComponent<SpriteRenderer>(out var renderer)) return false;
            return renderer.enabled && renderer.color.a > 0.05f && shadow.localScale.x > shadow.localScale.y * 3f;
        }

        private void SpawnAmbientLeafPop()
        {
            Vector3 pos = new Vector3(Mathf.Sin(Time.time * 0.71f) * 22f, Mathf.Cos(Time.time * 0.53f) * 12f, 0f);
            BackyardArtVfxPulse.Spawn(pos, RuntimeArtSpriteFactory.RuntimeSpriteId.PickupSparkle, new Vector3(0.025f, 0.025f, 1f), 6, new Color(0.75f, 1f, 0.55f, 0.35f), 0.8f, 30f);
            VfxSpawnCount++;
        }

        /// <summary>Public so tests can trigger the gag directly instead of waiting out the 6s cooldown.</summary>
        public void TrySpawnSunbeamClaim()
        {
            if (_cocoa == null)
            {
                var found = GameObject.Find("Cocoa");
                if (found == null) return;
                _cocoa = found.GetComponent<DogController>();
                if (_cocoa == null) return;
            }

            if (_cocoa.Busy) return;
            if (!_cocoa.TryGetComponent<Rigidbody2D>(out var body) || body.linearVelocity.sqrMagnitude > 0.05f) return;

            BackyardArtVfxPulse.Spawn(_cocoa.transform.position, RuntimeArtSpriteFactory.RuntimeSpriteId.SuccessPop,
                new Vector3(0.03f, 0.03f, 1f), 21, new Color(1f, 0.92f, 0.55f, 0.4f), 1.1f, 20f);
            if (_cocoa.TryGetComponent<DogReadabilityFeedback>(out var feedback)) feedback.ShowProudBrief();
            SunbeamClaimCount++;
        }

        /// <summary>Public so tests can trigger the gag directly instead of waiting out the 8s cooldown.</summary>
        public void TrySpawnSquirrelMumble()
        {
            var squirrel = _game.SquirrelObject;
            if (squirrel == null || !squirrel.activeSelf) return;

            // Never compete with the real gameplay-relevant urgency badge (e.g. an active steal).
            if (squirrel.TryGetComponent<ActorSignalBadge>(out var badge) && badge.IsShowing) return;

            Vector3 pos = squirrel.transform.position + Vector3.up * 1.1f;
            BackyardArtVfxPulse.Spawn(pos, RuntimeArtSpriteFactory.RuntimeSpriteId.BarkRing,
                new Vector3(0.018f, 0.018f, 1f), 23, new Color(0.55f, 0.42f, 0.6f, 0.4f), 0.9f, 40f);
            SquirrelMumbleCount++;
        }
    }
}
