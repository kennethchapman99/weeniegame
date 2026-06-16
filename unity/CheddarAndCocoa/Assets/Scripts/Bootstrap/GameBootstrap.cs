using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Data;
using CheddarAndCocoa.Input;
using CheddarAndCocoa.CameraRig;

namespace CheddarAndCocoa.Bootstrap
{
    /// <summary>
    /// Builds the entire ControllerTestScene from code on Play — floor + bounds, a shared camera,
    /// and two controller-driven placeholder dogs (Cheddar = P1/pad0, Cocoa = P2/pad1) + a debug
    /// HUD. Everything is generated at runtime (no art/prefab/scene-object dependencies), so the
    /// scene file is just one GameObject carrying this component. Deliberately boring + solid:
    /// the point is to prove two local controllers move two dogs independently.
    ///
    /// This is bootstrap scaffolding for the first playable, NOT the real game-flow architecture.
    /// As real scenes/prefabs come online, this gets replaced by authored content.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Field (world units)")]
        [SerializeField] private float fieldWidth = 20f;
        [SerializeField] private float fieldHeight = 12f;

        private Sprite _square;

        private void Start()
        {
            _square = MakeSquareSprite();

            BuildFloorAndBounds();
            var camGo = BuildCamera();
            var cam = camGo.GetComponent<Camera>();

            // Cheddar — golden chaos puppy (P1, pad 0). Colors from src/config/dogs.ts.
            var cheddar = BuildDog(DogId.Cheddar, Hex("#e3ab63"), new Vector2(-4f, 0f), slot: 0,
                CheddarTuning());
            // Cocoa — chocolate spot queen (P2, pad 1).
            var cocoa = BuildDog(DogId.Cocoa, Hex("#5e3a20"), new Vector2(4f, 0f), slot: 1,
                CocoaTuning());

            camGo.GetComponent<SharedCameraController>().SetTargets(cheddar.transform, cocoa.transform);

            // Debug overlay so we can tell which dog is which + see bark/connection state.
            var hudGo = new GameObject("DebugHud");
            var hud = hudGo.AddComponent<DebugHud>();
            hud.Init(cam,
                cheddar.GetComponent<DogController>(), cheddar.GetComponent<DogIdentity>(), 0,
                cocoa.GetComponent<DogController>(), cocoa.GetComponent<DogIdentity>(), 1);
        }

        // --- Tuning (ported from src/config/balance.ts; asymmetry is wrestle odds + stair time) ---

        private static DogTuning CheddarTuning()
        {
            var t = ScriptableObject.CreateInstance<DogTuning>();
            t.dog = DogId.Cheddar;
            t.bodyColor = Hex("#e3ab63");
            t.wetBodyColor = Hex("#b07e3f");
            t.baseSpeed = 4.4f; t.floaterSpeed = 4.9f; t.swimSpeed = 1.6f; t.zoomiesMultiplier = 1.85f;
            t.wrestleWinChance = 0.70f; t.stairTime = 0.5f;
            t.canChairLeap = true; t.barfChance = 0.18f; t.chewTime = 0.25f;
            return t;
        }

        private static DogTuning CocoaTuning()
        {
            var t = ScriptableObject.CreateInstance<DogTuning>();
            t.dog = DogId.Cocoa;
            t.bodyColor = Hex("#5e3a20");
            t.wetBodyColor = Hex("#3c2410");
            t.baseSpeed = 4.4f; t.floaterSpeed = 4.9f; t.swimSpeed = 1.6f; t.zoomiesMultiplier = 1.85f;
            t.wrestleWinChance = 0.78f; t.stairTime = 1.05f;
            t.canChairLeap = false; t.barfChance = 0f; t.chewTime = 0.5f;
            return t;
        }

        // --- Builders ---

        private GameObject BuildDog(DogId id, Color color, Vector2 pos, int slot, DogTuning tuning)
        {
            // Build inactive so component Awakes run only after we've configured identity/slot.
            var go = new GameObject(id.ToString());
            go.SetActive(false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.4f, 0.8f, 1f); // dachshund-ish placeholder

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _square;
            sr.color = color;
            sr.sortingOrder = 10;

            go.AddComponent<Rigidbody2D>();          // DogController sets gravity/rotation in Awake
            go.AddComponent<BoxCollider2D>();          // keeps the dog inside the bounds walls

            go.AddComponent<DogIdentity>().Configure(id, tuning);
            go.AddComponent<DogController>();
            go.AddComponent<GamepadPlayerInput>().SetSlot(slot);

            go.SetActive(true);
            return go;
        }

        private GameObject BuildCamera()
        {
            var go = new GameObject("SharedCamera");
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = fieldHeight * 0.5f + 1f;
            cam.backgroundColor = Hex("#243a1c"); // dark grass
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.tag = "MainCamera";
            go.AddComponent<SharedCameraController>();
            return go;
        }

        private void BuildFloorAndBounds()
        {
            // Floor: a tinted quad behind the dogs.
            var floor = new GameObject("Floor");
            var sr = floor.AddComponent<SpriteRenderer>();
            sr.sprite = _square;
            sr.color = Hex("#3c6b2f"); // lawn green
            sr.sortingOrder = -10;
            floor.transform.localScale = new Vector3(fieldWidth, fieldHeight, 1f);

            // Bounds: four static edge walls so dogs can't drive off the field.
            float hw = fieldWidth * 0.5f, hh = fieldHeight * 0.5f, t = 0.5f;
            MakeWall("Wall_Top", new Vector2(0, hh + t * 0.5f), new Vector2(fieldWidth + t * 2, t));
            MakeWall("Wall_Bottom", new Vector2(0, -hh - t * 0.5f), new Vector2(fieldWidth + t * 2, t));
            MakeWall("Wall_Left", new Vector2(-hw - t * 0.5f, 0), new Vector2(t, fieldHeight));
            MakeWall("Wall_Right", new Vector2(hw + t * 0.5f, 0), new Vector2(t, fieldHeight));
        }

        private static void MakeWall(string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.position = center;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        // --- Helpers ---

        private static Sprite MakeSquareSprite()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var px = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }
}
