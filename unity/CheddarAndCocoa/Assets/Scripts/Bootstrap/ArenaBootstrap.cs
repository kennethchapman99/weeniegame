using UnityEngine;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Data;
using CheddarAndCocoa.Input;
using CheddarAndCocoa.CameraRig;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Bootstrap
{
    /// <summary>
    /// Builds the first playable couch-co-op LOOP from code: a substantial backyard, two
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
        // A real backyard, not a demo box. At this scale a dog is under two percent of the yard
        // width, and the close camera must scroll to reveal the whole property.
        [SerializeField] private float fieldWidth = ArenaWorldScale.BackyardWidth;
        [SerializeField] private float fieldHeight = ArenaWorldScale.BackyardHeight;
        [SerializeField] private int treatSeed = 1234; // seeded -> deterministic treat layout for tests

        private Sprite _square;
        private Sprite _ring;
        private readonly ArenaMissionTuning _arenaTuning = ArenaMissionTuning.CreateDefault();

        private void Start()
        {
            _square = MakeSquareSprite();
            _ring = MakeRingSprite();

            BuildFloorAndBounds();
            BuildBackyardEnvironment();
            var camGo = BuildCamera();
            var cam = camGo.GetComponent<Camera>();
            var bounds = new Rect(-fieldWidth * 0.5f, -fieldHeight * 0.5f, fieldWidth, fieldHeight);

            // Cheddar — golden chaos puppy (P1: pad 0 / WASD + Space).
            var cheddar = BuildDog(DogId.Cheddar, new Vector2(-10f, 0f), slot: 0,
                GamepadPlayerInput.KeyboardScheme.WasdSpace, CheddarTuning());
            // Cocoa — chocolate spot queen (P2: pad 1 / arrows + Enter/RShift).
            var cocoa = BuildDog(DogId.Cocoa, new Vector2(10f, 0f), slot: 1,
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
                clamp: true,
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

            game.gameObject.AddComponent<BackyardRescueArtEnhancer>().Init(game);
            game.gameObject.AddComponent<DynamicTreatArtEnhancer>().Init(game);

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
            t.baseSpeed = 6.2f; t.floaterSpeed = 6.3f; t.swimSpeed = 2.1f; t.zoomiesMultiplier = 1.85f;
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
            t.baseSpeed = 5.9f; t.floaterSpeed = 6.3f; t.swimSpeed = 2.1f; t.zoomiesMultiplier = 1.75f;
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

        // Deterministic, purely decorative backyard dressing so the large yard reads as a real,
        // lived-in place (patio, pond, tree, garden beds, bush cover, fence line) rather than an
        // empty box. No colliders: the perimeter walls already handle collision, and keeping these
        // non-solid guarantees treats stay reachable and movement/collection tests stay green.
        // Coordinates are expressed as fractions of the field so they scale with the arena size.
        private void BuildBackyardEnvironment()
        {
            var root = new GameObject(ArenaArtCatalog.BackyardEnvironmentObjectName);
            float hw = fieldWidth * 0.5f, hh = fieldHeight * 0.5f;
            Vector2 F(float fx, float fy) => new Vector2(fx * hw, fy * hh);

            // Stone patio off the back door (bottom-right), with a darker rug accent.
            Prop(root, "Patio", F(0.52f, -0.6f), new Vector2(fieldWidth * 0.26f, fieldHeight * 0.3f), Hex("#9b9384"), -9);
            Prop(root, "PatioRug", F(0.52f, -0.62f), new Vector2(fieldWidth * 0.16f, fieldHeight * 0.16f), Hex("#7d5a3a"), -8);

            // Koi pond (top-left) with a lighter shallow rim.
            Prop(root, "Pond", F(-0.55f, 0.58f), new Vector2(fieldWidth * 0.2f, fieldHeight * 0.24f), Hex("#2f6f9e"), -9);
            Prop(root, "PondShallows", F(-0.55f, 0.58f), new Vector2(fieldWidth * 0.13f, fieldHeight * 0.15f), Hex("#4f97c4"), -8);

            // Big shade tree (top-right): large enough to remain a navigation landmark.
            Prop(root, "TreeTrunk", F(0.68f, 0.52f), new Vector2(2.2f, 4.4f), Hex("#5a3a1f"), -7);
            Prop(root, "TreeCanopy", F(0.68f, 0.66f), new Vector2(fieldWidth * 0.16f, fieldHeight * 0.2f), Hex("#2c5a23"), -6);
            Prop(root, "TreeCanopyHi", F(0.62f, 0.7f), new Vector2(fieldWidth * 0.1f, fieldHeight * 0.12f), Hex("#3a7330"), -5);

            // Garden beds along the left fence line.
            Prop(root, "GardenBed", F(-0.92f, 0f), new Vector2(fieldWidth * 0.05f, fieldHeight * 0.7f), Hex("#5b3d22"), -8);
            for (int i = 0; i < 5; i++)
            {
                float fy = -0.6f + i * 0.3f;
                Prop(root, $"Flower_{i}", F(-0.92f, fy), new Vector2(1.2f, 1.2f), i % 2 == 0 ? Hex("#d8557f") : Hex("#e8c24a"), -7);
            }

            // Mid-yard landmarks divide the large property into readable districts instead of
            // presenting an undifferentiated green plane.
            Prop(root, "PicnicBlanket", F(-0.18f, -0.48f), new Vector2(fieldWidth * 0.12f, fieldHeight * 0.13f), Hex("#c96b55"), -9);
            Prop(root, "Sandbox", F(0.18f, 0.34f), new Vector2(fieldWidth * 0.11f, fieldHeight * 0.12f), Hex("#c9a968"), -9);
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                Prop(root, $"SteppingStone_{i}", F(Mathf.Lerp(-0.32f, 0.4f, t), Mathf.Lerp(-0.72f, 0.2f, t)),
                    new Vector2(2.6f, 1.5f), Hex("#8c887e"), -8);
            }

            // Bush cover clumps. The first three sit on the Eagle Shadow "HIDE HERE" cover zones
            // (GameManager._eagleCoverZones) so the hiding spots read as real backyard cover; the
            // rest are scatter dressing. Decorative only — no colliders.
            var bushes = new[]
            {
                F(-0.7f, -0.64f), F(0.7f, -0.64f), F(0f, 0.68f),
                F(-0.4f, 0.05f), F(0.35f, -0.1f), F(0.0f, -0.05f),
            };
            for (int i = 0; i < bushes.Length; i++)
            {
                bool coverBush = i < 3;
                Prop(root, coverBush ? $"CoverBush_{i}" : $"Bush_{i}", bushes[i],
                    coverBush ? new Vector2(7.2f, 5.2f) : new Vector2(4.2f, 3.1f),
                    coverBush ? Hex("#2f6b27") : Hex("#356b2a"), -7);
            }

            // Decorative fence posts just inside the walls so the boundary reads as a backyard fence.
            BuildFenceLine(root, hw, hh);
        }

        private void BuildFenceLine(GameObject root, float hw, float hh)
        {
            var post = Hex("#b89b6a");
            const float inset = 0.6f, step = 3f, size = 0.55f;
            for (float x = -hw + inset; x <= hw - inset; x += step)
            {
                Prop(root, "FencePost", new Vector2(x, hh - inset), new Vector2(size, size), post, -6);
                Prop(root, "FencePost", new Vector2(x, -hh + inset), new Vector2(size, size), post, -6);
            }
            for (float y = -hh + inset; y <= hh - inset; y += step)
            {
                Prop(root, "FencePost", new Vector2(-hw + inset, y), new Vector2(size, size), post, -6);
                Prop(root, "FencePost", new Vector2(hw - inset, y), new Vector2(size, size), post, -6);
            }
        }

        private void Prop(GameObject root, string name, Vector2 pos, Vector2 size, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _square;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
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
