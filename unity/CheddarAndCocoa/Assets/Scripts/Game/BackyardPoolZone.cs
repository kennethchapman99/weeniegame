using System.Collections;
using System.Collections.Generic;
using CheddarAndCocoa.Dogs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// The HUGE backyard pool, ported from the frozen TS build (scenes/pool.ts + poolGeometry.ts +
    /// the pool branches of systems/movement.ts): dogs run on drifting floaties, fall into open
    /// water and have to swim (slow), and exiting at the deck edge roots them in a shake before
    /// they come out wet. Active only in yard-staged missions so interior slices (kitchen, car,
    /// pee break) never dunk anybody.
    /// </summary>
    public sealed class BackyardPoolZone : MonoBehaviour
    {
        public const string ObjectName = "BackyardPoolZone";

        // Reference tuning (config/balance.ts): POOL.shake / POOL.wetTimer.
        public const float ShakeSeconds = 0.9f;
        public const float WetSeconds = 4.5f;

        // Fraction of yard_photo_pool_patio.png occupied by the blue water region (center
        // row/column pixel scan). The plate is scaled so this region equals WaterRect.
        public const float PhotoWaterFractionX = 0.548f;
        public const float PhotoWaterFractionY = 0.583f;

        // Open-water rectangle in world units. Top-left quadrant of the 120x68 yard, sized so it
        // reads HUGE from the couch while every fixed mission staging point (claim zones, dig
        // spots, hide bushes, home bowl, weenie spots) stays on dry land.
        public static readonly Rect WaterRect = new Rect(-35f, 3f, 34f, 23f);

        /// <summary>Fraction of the yard covered by open water — the "HUGE" acceptance number.</summary>
        public static float WaterAreaFractionOfYard =>
            (WaterRect.width * WaterRect.height) /
            (ArenaWorldScale.BackyardWidth * ArenaWorldScale.BackyardHeight);

        /// <summary>Missions staged in the open yard. Interior/route missions keep dry paws.</summary>
        public static readonly GameManager.MissionVariant[] YardMissions =
        {
            GameManager.MissionVariant.BackyardRescue,
            GameManager.MissionVariant.WeenieRoundup,
            GameManager.MissionVariant.MarkTheYard,
            GameManager.MissionVariant.ScentSearch,
            GameManager.MissionVariant.ThunderstormComfort,
            GameManager.MissionVariant.EagleShadowPanic,
            GameManager.MissionVariant.CoyotesFence,
            GameManager.MissionVariant.SnackHeist,
            GameManager.MissionVariant.SockPanic,
            GameManager.MissionVariant.SquirrelConspiracy,
            GameManager.MissionVariant.SquirrelSwitcheroo
        };

        public struct FloaterState
        {
            public Vector2 Center;
            public Vector2 Radii;
            public float DriftPerSecond;
        }

        private GameManager _game;
        private Transform _visualRoot;
        private readonly List<FloaterState> _floaters = new List<FloaterState>();
        private readonly List<Transform> _floaterVisuals = new List<Transform>();
        private readonly Dictionary<DogController, float> _shakeTimers = new Dictionary<DogController, float>();
        private DogController[] _dogs = System.Array.Empty<DogController>();
        private float _nextDogScanAt;
        private float _nextRippleAt;
        private bool _lastActive;

        public bool Built { get; private set; }
        public bool ActiveForCurrentMission { get; private set; }
        public int FloaterCount => _floaters.Count;

        public FloaterState FloaterAt(int index) => _floaters[index];

        // ---- Pure geometry (poolGeometry.ts port) — static so PlayMode tests hit the same math ----

        public static bool InPoolRect(Vector2 p) => WaterRect.Contains(p);

        /// <summary>Ellipse hit-test with the prototype's 0.9 vertical squeeze.</summary>
        public static bool OnFloater(Vector2 p, Vector2 center, Vector2 radii)
        {
            float dx = (p.x - center.x) / Mathf.Max(0.01f, radii.x);
            float dy = (p.y - center.y) / Mathf.Max(0.01f, radii.y * 0.9f);
            return dx * dx + dy * dy < 1f;
        }

        public static bool InWater(Vector2 p, IReadOnlyList<FloaterState> floaters)
        {
            if (!InPoolRect(p)) return false;
            if (floaters != null)
            {
                for (int i = 0; i < floaters.Count; i++)
                {
                    if (OnFloater(p, floaters[i].Center, floaters[i].Radii)) return false;
                }
            }
            return true;
        }

        /// <summary>Closest deck point just outside the water — the swim exit target.</summary>
        public static Vector2 NearestDeckPoint(Vector2 p)
        {
            const float lip = 0.8f;
            Vector2[] exits =
            {
                new Vector2(WaterRect.xMin - lip, Mathf.Clamp(p.y, WaterRect.yMin, WaterRect.yMax)),
                new Vector2(WaterRect.xMax + lip, Mathf.Clamp(p.y, WaterRect.yMin, WaterRect.yMax)),
                new Vector2(Mathf.Clamp(p.x, WaterRect.xMin, WaterRect.xMax), WaterRect.yMin - lip),
                new Vector2(Mathf.Clamp(p.x, WaterRect.xMin, WaterRect.xMax), WaterRect.yMax + lip)
            };
            Vector2 best = exits[0];
            float bestSqr = float.MaxValue;
            foreach (var exit in exits)
            {
                float sqr = (exit - p).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = exit;
                }
            }
            return best;
        }

        /// <summary>Test hook (repo Force* pattern): advance a rooted shake without real frames.</summary>
        public void ForceShakeElapsed(DogController dog, float seconds)
        {
            if (dog == null || dog.Mode != MovementMode.Shaking) return;
            if (!_shakeTimers.TryGetValue(dog, out float remaining)) remaining = ShakeSeconds;
            _shakeTimers[dog] = remaining - seconds;
        }

        public bool AnyFloaterUnder(Vector2 p)
        {
            for (int i = 0; i < _floaters.Count; i++)
            {
                if (OnFloater(p, _floaters[i].Center, _floaters[i].Radii)) return true;
            }
            return false;
        }

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
            if (FindFirstObjectByType<BackyardPoolZone>() != null) return;
            new GameObject(ObjectName).AddComponent<BackyardPoolZone>();
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

            _floaters.Clear();
            _floaters.Add(new FloaterState { Center = WaterRect.center + new Vector2(-9f, 3.5f), Radii = new Vector2(4.6f, 2.9f), DriftPerSecond = 1.1f });
            _floaters.Add(new FloaterState { Center = WaterRect.center + new Vector2(4f, -4f), Radii = new Vector2(5.2f, 3.2f), DriftPerSecond = -0.9f });
            _floaters.Add(new FloaterState { Center = WaterRect.center + new Vector2(10f, 5f), Radii = new Vector2(3.8f, 2.5f), DriftPerSecond = 0.7f });

            BuildVisuals();
            Built = true;
        }

        private void BuildVisuals()
        {
            _visualRoot = new GameObject("PoolVisuals").transform;
            _visualRoot.SetParent(transform);

            // Water plate: the photo-derived pool-patio art from Ken and Sue's real yard. The BLUE
            // WATER inside the photo is only part of the image (54.8% of its width, 58.3% of its
            // height, centered — measured from the PNG's center row/column), so the sprite is
            // scaled so that the VISIBLE WATER matches the gameplay WaterRect exactly and the
            // photo's patio border lands outside it as real, dry deck. Couch test #4: scaling the
            // whole image to the rect made dogs "swim" on visibly dry concrete. One-background
            // rule: authored sprite, not a runtime rectangle. Sits above the painted plate (-8)
            // and below every gameplay prop. The pond fallback is full-bleed water, so it maps to
            // the rect 1:1.
            Sprite water = FinalGameplayArt.Load(FinalGameplayArt.EnvironmentPhotoPoolPatio);
            bool photoPlate = water != null;
            if (water == null) water = FinalGameplayArt.Load(FinalGameplayArt.EnvironmentPond);
            if (water != null)
            {
                float waterFractionX = photoPlate ? PhotoWaterFractionX : 1f;
                float waterFractionY = photoPlate ? PhotoWaterFractionY : 1f;
                var go = new GameObject("PoolWater");
                go.transform.SetParent(_visualRoot);
                go.transform.position = new Vector3(WaterRect.center.x, WaterRect.center.y, 0.1f);
                go.transform.localScale = new Vector3(
                    WaterRect.width / Mathf.Max(0.01f, waterFractionX * water.bounds.size.x),
                    WaterRect.height / Mathf.Max(0.01f, waterFractionY * water.bounds.size.y),
                    1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = water;
                sr.sortingOrder = -6;
                sr.color = Color.white;
            }

            // Floaties: pool-donut inner tubes dogs can stand on. Couch test #4: the BarkRing VFX
            // sprite used here before read as a giant paw-print bark target, not a pool toy.
            Color[] tints =
            {
                new Color(1f, 0.55f, 0.68f),  // pink donut
                new Color(1f, 0.85f, 0.35f),  // yellow donut
                new Color(0.45f, 0.9f, 0.85f) // teal donut
            };
            for (int i = 0; i < _floaters.Count; i++)
            {
                Sprite donut = PoolRuntimeArt.Donut(tints[i % tints.Length]);
                var go = new GameObject($"PoolFloater_{i}");
                go.transform.SetParent(_visualRoot);
                go.transform.position = new Vector3(_floaters[i].Center.x, _floaters[i].Center.y, 0.05f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = donut;
                sr.sortingOrder = -5;
                sr.color = Color.white;
                if (donut != null)
                {
                    go.transform.localScale = new Vector3(
                        _floaters[i].Radii.x * 2f / Mathf.Max(0.01f, donut.bounds.size.x),
                        _floaters[i].Radii.y * 2f / Mathf.Max(0.01f, donut.bounds.size.y),
                        1f);
                }
                _floaterVisuals.Add(go.transform);
            }
        }

        private void Update()
        {
            if (!Built) return;
            if (_game == null)
            {
                _game = FindFirstObjectByType<GameManager>();
                if (_game == null) return;
            }

            ActiveForCurrentMission = !_game.MissionSelectVisible &&
                System.Array.IndexOf(YardMissions, _game.ActiveMissionVariant) >= 0;

            if (_visualRoot != null && _visualRoot.gameObject.activeSelf != ActiveForCurrentMission)
                _visualRoot.gameObject.SetActive(ActiveForCurrentMission);

            RefreshDogs();

            if (!ActiveForCurrentMission)
            {
                // Scene-state reset rule: leaving the yard dries everybody off instantly.
                if (_lastActive) ReleaseAllDogs();
                _lastActive = false;
                return;
            }
            _lastActive = true;

            DriftFloaters(Time.deltaTime);
            foreach (var dog in _dogs)
            {
                if (dog != null) TickDog(dog, Time.deltaTime);
            }
        }

        private void RefreshDogs()
        {
            if (_dogs.Length > 0 && Time.time < _nextDogScanAt) return;
            _nextDogScanAt = Time.time + 2f;
            _dogs = FindObjectsByType<DogController>(FindObjectsSortMode.None);
        }

        private void ReleaseAllDogs()
        {
            foreach (var dog in _dogs)
            {
                if (dog == null) continue;
                dog.SetOnFloater(false);
                if (dog.Mode == MovementMode.Swimming || dog.Mode == MovementMode.Shaking)
                    dog.SetMode(MovementMode.Free);
            }
            _shakeTimers.Clear();
        }

        private void DriftFloaters(float dt)
        {
            for (int i = 0; i < _floaters.Count; i++)
            {
                FloaterState f = _floaters[i];
                f.Center.x += f.DriftPerSecond * dt;
                float minX = WaterRect.xMin + f.Radii.x + 0.6f;
                float maxX = WaterRect.xMax - f.Radii.x - 0.6f;
                if (f.Center.x <= minX || f.Center.x >= maxX)
                {
                    f.Center.x = Mathf.Clamp(f.Center.x, minX, maxX);
                    f.DriftPerSecond = -f.DriftPerSecond;
                }
                _floaters[i] = f;
                if (i < _floaterVisuals.Count && _floaterVisuals[i] != null)
                {
                    float bob = Mathf.Sin(Time.time * 1.4f + i * 2.1f) * 0.14f;
                    _floaterVisuals[i].position = new Vector3(f.Center.x, f.Center.y + bob, 0.05f);
                }
            }
        }

        private void TickDog(DogController dog, float dt)
        {
            Vector2 pos = dog.transform.position;
            bool onFloater = AnyFloaterUnder(pos);
            dog.SetOnFloater(dog.Mode == MovementMode.Free && onFloater);

            switch (dog.Mode)
            {
                case MovementMode.Free:
                    if (InPoolRect(pos) && !onFloater) SplashIn(dog, pos);
                    break;

                case MovementMode.Swimming:
                    if (!InPoolRect(pos))
                    {
                        StartShake(dog, pos);
                    }
                    else if (Time.time >= _nextRippleAt)
                    {
                        _nextRippleAt = Time.time + 0.4f;
                        BackyardArtVfxPulse.Spawn(new Vector3(pos.x, pos.y - 0.3f, 0f),
                            RuntimeArtSpriteFactory.RuntimeSpriteId.BarkRing,
                            new Vector3(0.03f, 0.018f, 1f), 28, new Color(0.85f, 0.96f, 1f, 0.5f), 0.5f);
                    }
                    break;

                case MovementMode.Shaking:
                    if (!_shakeTimers.TryGetValue(dog, out float remaining)) remaining = ShakeSeconds;
                    remaining -= dt;
                    if (remaining <= 0f)
                    {
                        _shakeTimers.Remove(dog);
                        dog.SetMode(MovementMode.Free);
                        dog.SetWet(WetSeconds);
                        // Step clear of the lip so the next input doesn't dunk them right back.
                        Vector2 away = (pos - WaterRect.center).normalized;
                        dog.transform.position = pos + away * 0.9f;
                    }
                    else
                    {
                        _shakeTimers[dog] = remaining;
                        BackyardArtVfxPulse.Spawn(
                            new Vector3(pos.x + Random.Range(-0.6f, 0.6f), pos.y + Random.Range(-0.1f, 0.7f), 0f),
                            RuntimeArtSpriteFactory.RuntimeSpriteId.PickupSparkle,
                            new Vector3(0.012f, 0.012f, 1f), 40, new Color(0.8f, 0.94f, 1f, 0.85f), 0.3f, 240f);
                    }
                    break;
            }
        }

        private void SplashIn(DogController dog, Vector2 pos)
        {
            dog.SetOnFloater(false);
            dog.SetMode(MovementMode.Swimming);
            BackyardArtVfxPulse.Spawn(new Vector3(pos.x, pos.y, 0f),
                RuntimeArtSpriteFactory.RuntimeSpriteId.BarkRing,
                new Vector3(0.06f, 0.04f, 1f), 40, new Color(0.85f, 0.96f, 1f, 0.9f), 0.55f);
            BackyardArtVfxPulse.Spawn(new Vector3(pos.x, pos.y + 0.5f, 0f),
                RuntimeArtSpriteFactory.RuntimeSpriteId.FailPuff,
                new Vector3(0.035f, 0.035f, 1f), 41, new Color(0.75f, 0.92f, 1f, 0.85f), 0.5f, 40f);
        }

        private void StartShake(DogController dog, Vector2 pos)
        {
            dog.SetMode(MovementMode.Shaking);
            _shakeTimers[dog] = ShakeSeconds;
            BackyardArtVfxPulse.Spawn(new Vector3(pos.x, pos.y + 0.8f, 0f),
                RuntimeArtSpriteFactory.RuntimeSpriteId.BarkBurst,
                new Vector3(0.04f, 0.04f, 1f), 41, new Color(0.8f, 0.94f, 1f, 0.9f), ShakeSeconds, 30f);
        }
    }
}
