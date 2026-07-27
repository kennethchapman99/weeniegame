using System.Collections.Generic;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Decorative, mission-owned level plates for slices that are not physically the backyard.
    /// Gameplay markers and controller state remain authoritative.
    /// </summary>
    public sealed class MissionLevelAreaArt : MonoBehaviour
    {
        private static readonly Vector2 IndoorFloorWorldSize = new(48f, 36f);
        private static readonly Dictionary<GameObject, int> OutdoorVisualSuppressionCounts = new();
        private static readonly Dictionary<GameObject, bool> OutdoorVisualInitialStates = new();
        private readonly List<GameObject> _suppressedOutdoorVisuals = new();

        public const string KitchenRootName = "KitchenLevelArea";
        public const string CarRideRootName = "CarRideLevelArea";
        public const string TableStealthRootName = "TableStealthLevelArea";
        public const string ChaosMachineRootName = "ChaosMachineLevelArea";
        public const string ThunderstormComfortRootName = "ThunderstormComfortLevelArea";
        public const string BlanketCatchRootName = "BlanketCatchLevelArea";

        public int PlateCount { get; private set; }

        public static MissionLevelAreaArt CreateKitchenArea(Rect bounds, Vector2 counterPosition, Vector2 safeZonePosition)
        {
            var area = CreateRoot(KitchenRootName);
            area.AddPlate("KitchenFloorPlate", FinalGameplayArt.LevelAreaKitchenFloor,
                bounds.center + Vector2.down * 0.3f, IndoorFloorWorldSize, -7, new Color(1f, 1f, 1f, 0.96f));
            area.AddPlate("KitchenCounterWallPlate", FinalGameplayArt.LevelAreaKitchenCounters,
                counterPosition + Vector2.up * 1.1f, new Vector2(28f, 9.2f), -4, Color.white);
            area.AddPlate("KitchenSafeBowlPreviewPlate", FinalGameplayArt.KitchenSafeBowlEmpty,
                safeZonePosition + Vector2.down * 0.2f, new Vector2(8.5f, 4.4f), -5, new Color(1f, 1f, 1f, 0.72f));
            return area;
        }

        /// <summary>
        /// How far the windshield scroller travels before snapping back to an identical frame:
        /// one full scenery plate width (three identical plates sit side by side under the
        /// scroller, so a one-plate shift is seamless).
        /// </summary>
        public const float SceneryWrapDistance = 45.5f;

        /// <summary>Scroller parent for the passing-road strip; the mission controller slides its
        /// localPosition.x and wraps every <see cref="SceneryWrapDistance"/>.</summary>
        public Transform WindshieldScenery { get; private set; }

        /// <summary>
        /// The backseat stage set: a cabin shell that covers the whole camera frame (no backyard
        /// bleed), the bench lane the dogs ride on, and a sprite-masked scrolling scenery strip
        /// behind the windshield. The root sits at the cabin center so the controller can tilt
        /// the whole set during turns by rotating the root transform.
        /// </summary>
        public static MissionLevelAreaArt CreateCarRideArea(Rect bounds)
        {
            var area = CreateRoot(CarRideRootName);
            area.transform.position = bounds.center;
            area.AddPlate("BackseatCabinShellPlate", FinalGameplayArt.BackseatCabinShell,
                bounds.center, new Vector2(56f, 33f), 4, Color.white);
            area.BuildWindshieldScroller(bounds.center + Vector2.up * 12f);
            area.AddPlate("BackseatBenchPlate", FinalGameplayArt.BackseatBench,
                bounds.center + Vector2.down * 1.4f, new Vector2(36f, 11f), 6, Color.white);
            return area;
        }

        private void BuildWindshieldScroller(Vector2 openingCenter)
        {
            Sprite scenerySprite = FinalGameplayArt.Load(FinalGameplayArt.BackseatWindshieldScenery);
            if (scenerySprite == null) return;

            // The opaque scenery rect doubles as the mask shape for the windshield opening, so
            // the strip only shows through the glass while it scrolls.
            var maskObject = new GameObject("BackseatWindshieldMask");
            maskObject.transform.SetParent(transform);
            maskObject.transform.position = new Vector3(openingCenter.x, openingCenter.y, 0.32f);
            maskObject.transform.localScale = WorldScale(scenerySprite, new Vector2(44f, 5.8f));
            maskObject.AddComponent<SpriteMask>().sprite = scenerySprite;

            var scroller = new GameObject("BackseatWindshieldScroller");
            scroller.transform.SetParent(transform);
            scroller.transform.position = new Vector3(openingCenter.x, openingCenter.y, 0.32f);
            WindshieldScenery = scroller.transform;

            for (int i = -1; i <= 1; i++)
            {
                var plate = AddPlate($"BackseatWindshieldSceneryPlate_{i + 1}",
                    FinalGameplayArt.BackseatWindshieldScenery,
                    openingCenter + Vector2.right * (i * SceneryWrapDistance),
                    new Vector2(SceneryWrapDistance, 5.8f), 5, Color.white);
                if (plate == null) continue;
                plate.transform.SetParent(scroller.transform, true);
                plate.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
        }

        /// <summary>
        /// V3.4 indoor-fantasy staging audit: Table Stealth's dinner-table steak heist plays out on
        /// a dining room floor instead of the bare backyard lawn. A single floor plate is enough -
        /// the human/steak markers already carry their own art, so a wall/furniture plate risks
        /// visually competing with them rather than helping.
        /// </summary>
        public static MissionLevelAreaArt CreateTableStealthArea(Rect bounds)
        {
            var area = CreateRoot(TableStealthRootName);
            area.AddPlate("DiningRoomFloorPlate", FinalGameplayArt.LevelAreaDiningRoomFloor,
                bounds.center, IndoorFloorWorldSize, -7, new Color(1f, 1f, 1f, 0.96f));
            return area;
        }

        /// <summary>
        /// V3.4: Chaos Machine's Rube Goldberg contraption and Thunderstorm Comfort's storm-shelter
        /// huddle are both staged in the same cozy den in their briefing art, so they share one
        /// living-room floor pack rather than each needing bespoke art.
        /// </summary>
        public static MissionLevelAreaArt CreateChaosMachineArea(Rect bounds) =>
            CreateLivingRoomArea(ChaosMachineRootName, bounds);

        public static MissionLevelAreaArt CreateThunderstormComfortArea(Rect bounds) =>
            CreateLivingRoomArea(ThunderstormComfortRootName, bounds);

        private static MissionLevelAreaArt CreateLivingRoomArea(string rootName, Rect bounds)
        {
            var area = CreateRoot(rootName);
            area.AddPlate("LivingRoomFloorPlate", FinalGameplayArt.LevelAreaLivingRoomFloor,
                bounds.center, IndoorFloorWorldSize, -7, new Color(1f, 1f, 1f, 0.96f));
            return area;
        }

        /// <summary>
        /// V3.4: Blanket Catch's briefing is explicit ("food's teetering on the counter") - the same
        /// kitchen as Kitchen Falling Food Frenzy, so it reuses that pack's floor art directly rather
        /// than generating a redundant near-duplicate.
        /// </summary>
        public static MissionLevelAreaArt CreateBlanketCatchArea(Rect bounds)
        {
            var area = CreateRoot(BlanketCatchRootName);
            area.AddPlate("KitchenFloorPlate", FinalGameplayArt.LevelAreaKitchenFloor,
                bounds.center, IndoorFloorWorldSize, -7, new Color(1f, 1f, 1f, 0.96f));
            return area;
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        private static MissionLevelAreaArt CreateRoot(string rootName)
        {
            var existing = GameObject.Find(rootName);
            if (existing != null)
                Object.Destroy(existing);

            var root = new GameObject(rootName);
            var area = root.AddComponent<MissionLevelAreaArt>();
            area.SuppressOutdoorVisuals();
            return area;
        }

        /// <summary>
        /// Indoor missions share the large outdoor scene for gameplay infrastructure. Hide only its
        /// decorative roots so pool/patio art, stepping stones, and route marks cannot paint over a
        /// room; keep the pool component, colliders, camera, and mission systems alive.
        ///
        /// Reference counts handle a same-frame indoor-to-indoor mission switch: Unity destroys the
        /// outgoing area at frame end, after the incoming area has already claimed the visuals.
        /// </summary>
        private void SuppressOutdoorVisuals()
        {
            foreach (var candidate in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate == null ||
                    (candidate.name != ArenaArtCatalog.BackyardEnvironmentObjectName &&
                     candidate.name != "PoolVisuals"))
                    continue;

                GameObject visual = candidate.gameObject;
                if (!OutdoorVisualSuppressionCounts.TryGetValue(visual, out int count))
                {
                    OutdoorVisualSuppressionCounts[visual] = 1;
                    OutdoorVisualInitialStates[visual] = visual.activeSelf;
                    visual.SetActive(false);
                }
                else
                {
                    OutdoorVisualSuppressionCounts[visual] = count + 1;
                }
                _suppressedOutdoorVisuals.Add(visual);
            }
        }

        private void OnDestroy()
        {
            foreach (GameObject visual in _suppressedOutdoorVisuals)
            {
                if (ReferenceEquals(visual, null) ||
                    !OutdoorVisualSuppressionCounts.TryGetValue(visual, out int count))
                    continue;
                if (count > 1)
                {
                    OutdoorVisualSuppressionCounts[visual] = count - 1;
                    continue;
                }

                OutdoorVisualSuppressionCounts.Remove(visual);
                bool restore = OutdoorVisualInitialStates.TryGetValue(visual, out bool wasActive) && wasActive;
                OutdoorVisualInitialStates.Remove(visual);
                if (visual != null) visual.SetActive(restore);
            }
            _suppressedOutdoorVisuals.Clear();
        }

        private GameObject AddPlate(string name, string resourcePath, Vector2 position, Vector2 worldSize,
            int sortingOrder, Color tint)
        {
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return null;

            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(position.x, position.y, 0.32f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = WorldScale(sprite, worldSize);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = tint;
            // Indoor/car scenery gets a restrained light drift. The render child changes color
            // only; mission positions, collision, camera bounds, and scrolling remain untouched.
            go.AddComponent<SceneryAmbientMotion>().Configure(
                SceneryAmbientMotion.Profile.LightWash, SceneryAmbientMotion.SeedFor(name));
            PlateCount++;
            return go;
        }

        private static Vector3 WorldScale(Sprite sprite, Vector2 worldSize)
        {
            return new Vector3(
                worldSize.x / Mathf.Max(0.01f, sprite.bounds.size.x),
                worldSize.y / Mathf.Max(0.01f, sprite.bounds.size.y),
                1f);
        }
    }
}
