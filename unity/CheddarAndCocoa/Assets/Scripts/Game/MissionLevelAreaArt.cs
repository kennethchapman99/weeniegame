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

        public static MissionLevelAreaArt CreateCarRideArea(Rect bounds, Vector2 carPosition)
        {
            var area = CreateRoot(CarRideRootName);
            area.AddPlate("CarInteriorCabinPlate", FinalGameplayArt.LevelAreaCarInterior,
                carPosition + Vector2.down * 3.8f, new Vector2(25f, 21f), -6, new Color(1f, 1f, 1f, 0.96f));
            area.AddPlate("CarBalanceLanePlate", FinalGameplayArt.LevelAreaCarBalanceLane,
                bounds.center + Vector2.down * 5.6f, new Vector2(18f, 7.2f), -3, Color.white);
            area.AddPlate("CarWindowMotionCuePlate", FinalGameplayArt.CarRideLurchLeft,
                carPosition + new Vector2(-7.8f, 1.2f), new Vector2(5.2f, 3f), -4, new Color(1f, 1f, 1f, 0.54f));
            area.AddPlate("CarSpillHazardPreviewPlate", FinalGameplayArt.CarRideSpill,
                carPosition + new Vector2(6.9f, -5.8f), new Vector2(5.4f, 3.2f), -2, new Color(1f, 1f, 1f, 0.82f));
            return area;
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

        private void AddPlate(string name, string resourcePath, Vector2 position, Vector2 worldSize,
            int sortingOrder, Color tint)
        {
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return;

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
