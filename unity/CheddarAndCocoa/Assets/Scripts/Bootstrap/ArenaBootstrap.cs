using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Data;
using CheddarAndCocoa.Input;
using CheddarAndCocoa.CameraRig;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Bootstrap
{
    /// <summary>
    /// Builds the first playable couch-co-op LOOP from code: a small walled arena, two
    /// controller/keyboard-driven dogs, a shared camera, treats to collect, a 60-second round, a
    /// score/timer HUD, and visible bark rings. The ArenaScene file is just one GameObject carrying
    /// this component — everything else is generated at runtime (no art/prefab dependencies).
    ///
    /// This is the playable sibling of the verified <see cref="GameBootstrap"/> (which proves
    /// two-controller movement). It deliberately does not touch that baseline; it composes the same
    /// shared components (DogController, GamepadPlayerInput, SharedCameraController) into a real loop.
    /// Scaffolding for the first playable, NOT the final game-flow architecture.
    /// </summary>
    public sealed class ArenaBootstrap : MonoBehaviour
    {
        [Header("Arena (world units)")]
        [SerializeField] private float fieldWidth = 20f;
        [SerializeField] private float fieldHeight = 12f;
        [SerializeField] private int treatSeed = 1234; // seeded -> deterministic treat layout for tests

        private Sprite _square;
        private Sprite _ring;
        private readonly ArenaMissionTuning _arenaTuning = ArenaMissionTuning.CreateDefault();

        private void Start()
        {
            _square = MakeSquareSprite();
            _ring = MakeRingSprite();

            BuildFloorAndBounds();
            var camGo = BuildCamera();
            var cam = camGo.GetComponent<Camera>();
            var bounds = new Rect(-fieldWidth * 0.5f, -fieldHeight * 0.5f, fieldWidth, fieldHeight);

            // Cheddar — golden chaos puppy (P1: pad 0 / WASD + Space).
            var cheddar = BuildDog(DogId.Cheddar, new Vector2(-4f, 0f), slot: 0,
                GamepadPlayerInput.KeyboardScheme.WasdSpace, CheddarTuning());
            // Cocoa — chocolate spot queen (P2: pad 1 / arrows + Enter/RShift).
            var cocoa = BuildDog(DogId.Cocoa, new Vector2(4f, 0f), slot: 1,
                GamepadPlayerInput.KeyboardScheme.ArrowsEnter, CocoaTuning());

            var cheddarDog = cheddar.GetComponent<DogController>();
            var cocoaDog = cocoa.GetComponent<DogController>();

            var sharedCamera = camGo.GetComponent<SharedCameraController>();
            sharedCamera.Configure(
                _arenaTuning.CameraInitialOrthoSize,
                _arenaTuning.CameraMinOrthoSize,
                _arenaTuning.CameraMaxOrthoSize,
                _arenaTuning.CameraHorizontalMargin,
                _arenaTuning.CameraVerticalMargin,
                _arenaTuning.CameraFollowLerp,
                _arenaTuning.CameraZoomLerp,
                clamp: false,
                bounds);
            sharedCamera.SetTargets(cheddar.transform, cocoa.transform);

            // Visible bark feedback: an expanding ring at the dog on each bark.
            cheddar.AddComponent<BarkEffect>().Init(cheddarDog, _ring, ArenaArtCatalog.Dog(DogId.Cheddar).BarkTint);
            cocoa.AddComponent<BarkEffect>().Init(cocoaDog, _ring, ArenaArtCatalog.Dog(DogId.Cocoa).BarkTint);

            // The round: shared score, 60s timer, treats, restart.
            var game = new GameObject(ArenaArtCatalog.GameManagerObjectName).AddComponent<GameManager>();
            game.Init(
                new[] { cheddarDog, cocoaDog },
                new[] { cheddar.GetComponent<GamepadPlayerInput>(), cocoa.GetComponent<GamepadPlayerInput>() },
                _square, _ring, bounds, treatSeed);

            // Score/timer/game-over overlay.
            var hud = new GameObject(ArenaArtCatalog.ArenaHudObjectName).AddComponent<ArenaHud>();
            hud.Init(game);

            // Controller/keyboard legend + name tags + WOOF flash (reuse the existing debug overlay).
            var dbg = new GameObject(ArenaArtCatalog.DebugHudObjectName).AddComponent<DebugHud>();
            dbg.Init(cam,
                cheddarDog, cheddar.GetComponent<DogIdentity>(), 0,
                cocoaDog, cocoa.GetComponent<DogIdentity>(), 1);
        }

        // --- Tuning (same numbers as GameBootstrap; ported from src/config/balance.ts) ---

        private static DogTuning CheddarTuning()
        {
            var t = ScriptableObject.CreateInstance<DogTuning>();
            t.dog = DogId.Cheddar;
            t.bodyColor = Hex("#e3ab63");
            t.wetBodyColor = Hex("#b07e3f");
            t.baseSpeed = 4.8f; t.floaterSpeed = 4.9f; t.swimSpeed = 1.6f; t.zoomiesMultiplier = 1.85f;
            t.inputDeadzone = 0.25f; t.acceleration = 34f; t.deceleration = 31f; t.turnResponsiveness = 46f; t.stopSpeed = 0.08f; t.runFeedbackSpeed = 0.22f;
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
            t.baseSpeed = 4.55f; t.floaterSpeed = 4.9f; t.swimSpeed = 1.6f; t.zoomiesMultiplier = 1.75f;
            t.inputDeadzone = 0.25f; t.acceleration = 29f; t.deceleration = 39f; t.turnResponsiveness = 52f; t.stopSpeed = 0.08f; t.runFeedbackSpeed = 0.22f;
            t.wrestleWinChance = 0.78f; t.stairTime = 1.05f;
            t.canChairLeap = false; t.barfChance = 0f; t.chewTime = 0.5f;
            return t;
        }

        // --- Builders ---

        private GameObject BuildDog(DogId id, Vector2 pos, int slot,
            GamepadPlayerInput.KeyboardScheme scheme, DogTuning tuning)
        {
            var art = ArenaArtCatalog.Dog(id);
            // Build inactive so component Awakes run only after identity/slot are configured.
            var go = new GameObject(id.ToString());
            go.SetActive(false);
            go.transform.position = pos;
            go.transform.localScale = ArenaArtCatalog.ArenaDogBodyScale; // long, low dachshund placeholder

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _square;
            sr.color = art.BootstrapColor;
            sr.sortingOrder = 10;

            go.AddComponent<Rigidbody2D>();   // DogController sets gravity/rotation in Awake
            go.AddComponent<BoxCollider2D>();  // keeps the dog inside the bounds walls + hits treats

            go.AddComponent<DogIdentity>().Configure(id, tuning);
            go.AddComponent<DogController>();
            go.AddComponent<DogReadabilityFeedback>().Init(_square);
            go.AddComponent<ObjectiveArrowFeedback>().Init(art.ObjectiveArrowColor);
            var input = go.AddComponent<GamepadPlayerInput>();
            input.SetSlot(slot);
            input.SetKeyboardScheme(scheme);

            go.SetActive(true);
            return go;
        }

        private GameObject BuildCamera()
        {
            var go = new GameObject("SharedCamera");
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = _arenaTuning.CameraInitialOrthoSize;
            cam.backgroundColor = ArenaArtCatalog.CameraBackgroundColor; // dark grass
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.AddComponent<AudioListener>();
            go.tag = "MainCamera";
            go.AddComponent<SharedCameraController>();
            return go;
        }

        private void BuildFloorAndBounds()
        {
            var floor = new GameObject("Floor");
            var sr = floor.AddComponent<SpriteRenderer>();
            sr.sprite = _square;
            sr.color = ArenaArtCatalog.FloorColor; // lawn green
            sr.sortingOrder = -10;
            floor.transform.localScale = new Vector3(fieldWidth, fieldHeight, 1f);

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
            go.AddComponent<BoxCollider2D>().size = size;
        }

        // --- Sprite helpers ---

        private static Sprite MakeSquareSprite()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }

        // A soft ring (transparent center, opaque rim) for the bark pulse.
        private static Sprite MakeRingSprite()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c; // 0 center .. 1 edge
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.18f);        // bright near rim
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }
}
