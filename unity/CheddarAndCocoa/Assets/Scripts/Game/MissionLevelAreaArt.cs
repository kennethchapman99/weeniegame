using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Decorative, mission-owned level plates for slices that are not physically the backyard.
    /// Gameplay markers and controller state remain authoritative.
    /// </summary>
    public sealed class MissionLevelAreaArt : MonoBehaviour
    {
        public const string KitchenRootName = "KitchenLevelArea";
        public const string CarRideRootName = "CarRideLevelArea";

        public int PlateCount { get; private set; }

        public static MissionLevelAreaArt CreateKitchenArea(Rect bounds, Vector2 counterPosition, Vector2 safeZonePosition)
        {
            var area = CreateRoot(KitchenRootName);
            area.AddPlate("KitchenFloorPlate", FinalGameplayArt.LevelAreaKitchenFloor,
                bounds.center + Vector2.down * 0.3f, new Vector2(33f, 25f), -7, new Color(1f, 1f, 1f, 0.96f));
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
                bounds.center, new Vector2(56f, 33f), -8, Color.white);
            area.BuildWindshieldScroller(bounds.center + Vector2.up * 12f);
            area.AddPlate("BackseatBenchPlate", FinalGameplayArt.BackseatBench,
                bounds.center + Vector2.down * 1.4f, new Vector2(36f, 11f), -6, Color.white);
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
                    new Vector2(SceneryWrapDistance, 5.8f), -7, Color.white);
                if (plate == null) continue;
                plate.transform.SetParent(scroller.transform, true);
                plate.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        private static MissionLevelAreaArt CreateRoot(string rootName)
        {
            var existing = GameObject.Find(rootName);
            if (existing != null)
                Object.Destroy(existing);

            var root = new GameObject(rootName);
            return root.AddComponent<MissionLevelAreaArt>();
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
