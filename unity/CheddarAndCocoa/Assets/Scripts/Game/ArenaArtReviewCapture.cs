using System;
using System.Collections;
using System.IO;
using System.Text;
using CheddarAndCocoa.CameraRig;
using CheddarAndCocoa.Dogs;
using CheddarAndCocoa.Input;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Opt-in standalone capture sequence for reviewing gameplay art without OS screenshot access.</summary>
    public sealed class ArenaArtReviewCapture : MonoBehaviour
    {
        public const string ArgumentPrefix = "--arena-art-review=";
        private GameManager _game;
        private string _outputDirectory;
        private Vector2? _focusOverride;

        public static bool TryAttach(GameManager game)
        {
            string output = OutputDirectoryFromArgs(Environment.GetCommandLineArgs());
            if (string.IsNullOrEmpty(output) || game == null) return false;
            game.gameObject.AddComponent<ArenaArtReviewCapture>().Init(game, output);
            return true;
        }

        public static string OutputDirectoryFromArgs(string[] args)
        {
            if (args == null) return null;
            foreach (string arg in args)
                if (!string.IsNullOrEmpty(arg) && arg.StartsWith(ArgumentPrefix, StringComparison.Ordinal))
                    return arg.Substring(ArgumentPrefix.Length);
            return null;
        }

        private void Init(GameManager game, string outputDirectory)
        {
            _game = game;
            _outputDirectory = Path.GetFullPath(outputDirectory);
            // Capture runs are usually launched from a script/CI shell; without this the standalone
            // player pauses when its window never gains focus and the sequence never starts.
            Application.runInBackground = true;
            StartCoroutine(CaptureSequence());
        }

        private IEnumerator CaptureSequence()
        {
            Directory.CreateDirectory(_outputDirectory);
            var manifest = new StringBuilder();
            manifest.AppendLine("# Arena Art Review Capture");
            manifest.AppendLine();
            manifest.AppendLine($"Generated: {DateTime.UtcNow:O}");
            manifest.AppendLine();

            int index = 1;
            foreach (GameManager.MissionVariant variant in Enum.GetValues(typeof(GameManager.MissionVariant)))
            {
                _focusOverride = null;
                string slug = Slug(variant.ToString());
                _game.StartMission(variant);
                yield return new WaitForSecondsRealtime(0.25f);
                FocusCameraOnActiveMission(variant);
                yield return new WaitForSecondsRealtime(0.08f);
                string start = $"{index:00}-{slug}-start.ppm";
                yield return Capture(start);
                manifest.AppendLine($"- `{start}` — {variant} start");

                DriveMainInteraction(variant);
                yield return new WaitForSecondsRealtime(0.2f);
                FocusCameraOnActiveMission(variant);
                string main = $"{index:00}-{slug}-main.ppm";
                yield return Capture(main);
                manifest.AppendLine($"- `{main}` — {variant} main interaction");

                DrivePayoff(variant);
                yield return new WaitForSecondsRealtime(0.35f);
                FocusCameraOnActiveMission(variant);
                string payoff = $"{index:00}-{slug}-payoff.ppm";
                yield return Capture(payoff);
                manifest.AppendLine($"- `{payoff}` — {variant} payoff/state change");
                index++;
            }

            File.WriteAllText(Path.Combine(_outputDirectory, "arena-art-review-manifest.md"),
                manifest.ToString(), Encoding.UTF8);

            Debug.Log($"Arena art review captures complete: {_outputDirectory}");
            Application.Quit();
        }

        private void DriveMainInteraction(GameManager.MissionVariant variant)
        {
            switch (variant)
            {
                case GameManager.MissionVariant.BackyardRescue:
                case GameManager.MissionVariant.SnackHeist:
                    _game.ForceSquirrelStealAttempt();
                    break;
                case GameManager.MissionVariant.SockPanic:
                    _game.ForceSockBasketTip(DogId.Cocoa);
                    break;
                case GameManager.MissionVariant.SquirrelConspiracy:
                    _game.ForceSquirrelConspiracyHerd(DogId.Cheddar);
                    break;
                case GameManager.MissionVariant.EagleShadowPanic:
                    _game.ForceEagleShadowSweepPass();
                    break;
                case GameManager.MissionVariant.CoyotesFence:
                    _game.ForceCoyoteProwlReach();
                    break;
                case GameManager.MissionVariant.WeenieRoundup:
                    StageDogsAtCurrentObjective();
                    _game.ForceWeeniePickup(DogId.Cheddar);
                    break;
                case GameManager.MissionVariant.ScentSearch:
                    _game.ForceScentSniff(DogId.Cheddar);
                    break;
                case GameManager.MissionVariant.ThunderstormComfort:
                    _game.ForceThunderclap();
                    break;
                case GameManager.MissionVariant.MarkTheYard:
                    _game.ForceClaimZone(DogId.Cheddar);
                    break;
                case GameManager.MissionVariant.LeashWalk:
                    StageDogsAtCurrentObjective();
                    _game.ForceReachCheckpoint();
                    break;
                case GameManager.MissionVariant.CarRide:
                    var carRide = _game.CarRideController;
                    if (carRide != null)
                    {
                        carRide.ForceBeginRoadEvent(CarRideMissionController.RoadEventKind.TurnRight);
                        carRide.ForceTurnSlide(1f, CarRideMissionController.RoadEventKind.TurnRight);
                    }
                    break;
                case GameManager.MissionVariant.GateCrash:
                    StageDogsAtCurrentObjective();
                    _game.ForceGateHold(true);
                    break;
                case GameManager.MissionVariant.TableStealth:
                    StageDogsAtCurrentObjective();
                    _game.ForceTableFlop(true);
                    break;
                case GameManager.MissionVariant.SquirrelSwitcheroo:
                    StageDogsAtCurrentObjective();
                    _game.ForceSwitcherooBait(0.7f);
                    break;
                case GameManager.MissionVariant.WalkCampaign:
                    StageDogsAtCurrentObjective();
                    _game.ForceWalkCampaign(1f, doorStare: true, presentLeash: false);
                    break;
                case GameManager.MissionVariant.BoneRelay:
                    StageDogsAtCurrentObjective();
                    _game.ForceBoneReveal();
                    break;
                case GameManager.MissionVariant.GreatEscape:
                    StageDogsAtCurrentObjective();
                    _game.ForceEscapeStep(_game.GreatEscapePuzzle.NextOwner);
                    break;
                case GameManager.MissionVariant.ChaosMachine:
                    _game.ForceChaosTrigger();
                    StageDogsAtCurrentObjective();
                    break;
                case GameManager.MissionVariant.BlanketCatch:
                    StageDogsAtCurrentObjective();
                    _game.ForceBlanketSpan(6f, 0f);
                    break;
                case GameManager.MissionVariant.KitchenFoodFrenzy:
                    StageDogsAtCurrentObjective();
                    _game.ForceKitchenDrop(KitchenFoodFrenzyMissionState.FoodKind.Good);
                    break;
                case GameManager.MissionVariant.OperationPeeBreak:
                    _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare, 1f);
                    break;
                case GameManager.MissionVariant.BabyBirdBedlam:
                    StageDogsAtCurrentObjective();
                    _game.ForceChickLand(0f);
                    _game.ForceChickGrab();
                    _game.ForceParentDive();
                    break;
                case GameManager.MissionVariant.TickInvasion:
                    StageDogsAround(_game.ArenaBounds.center);
                    _game.TickInvasionController?.Tick(15f, Time.time);
                    break;
            }
        }

        private void DrivePayoff(GameManager.MissionVariant variant)
        {
            switch (variant)
            {
                case GameManager.MissionVariant.BackyardRescue:
                    _game.ForceBackyardTrapRedirect(DogId.Cheddar, true);
                    _game.ForceBackyardTrapRecovery(DogId.Cocoa);
                    break;
                case GameManager.MissionVariant.SnackHeist:
                    _game.ForceCollectTreat();
                    break;
                case GameManager.MissionVariant.SockPanic:
                    _game.ForceSockBasketTip(DogId.Cocoa);
                    break;
                case GameManager.MissionVariant.SquirrelConspiracy:
                    for (int i = 0; i < 4; i++) _game.ForceSquirrelConspiracyHerd(i % 2 == 0 ? DogId.Cheddar : DogId.Cocoa);
                    _game.ForceSquirrelConspiracyFindStash(DogId.Cocoa);
                    break;
                case GameManager.MissionVariant.EagleShadowPanic:
                    _game.ForceEagleShadowUnitedFront();
                    break;
                case GameManager.MissionVariant.CoyotesFence:
                    _game.ForceCoyoteRepair(DogId.Cheddar);
                    _game.ForceCoyoteFinalBlock();
                    break;
                case GameManager.MissionVariant.WeenieRoundup:
                    StageDogsAround(_game.BowlPosition);
                    _focusOverride = _game.BowlPosition;
                    _game.ForceWeenieDeliver(DogId.Cheddar);
                    break;
                case GameManager.MissionVariant.ScentSearch:
                    _game.ForceScentDigCorrect(DogId.Cheddar);
                    break;
                case GameManager.MissionVariant.ThunderstormComfort:
                    _game.ForceComfortStep(3f);
                    break;
                case GameManager.MissionVariant.MarkTheYard:
                    for (int i = 0; i < 5; i++) _game.ForceClaimZone(i % 2 == 0 ? DogId.Cheddar : DogId.Cocoa);
                    break;
                case GameManager.MissionVariant.LeashWalk:
                    StageDogsAtCurrentObjective();
                    _game.ForceReachCheckpoint();
                    break;
                case GameManager.MissionVariant.CarRide:
                    _game.ForceCarTumble();
                    break;
                case GameManager.MissionVariant.GateCrash:
                    StageDogsAtCurrentObjective();
                    _game.ForceGateHold(true);
                    _game.ForceGateCross(1f);
                    break;
                case GameManager.MissionVariant.TableStealth:
                    StageDogsAtCurrentObjective();
                    _game.ForceTableSneak(2f);
                    break;
                case GameManager.MissionVariant.SquirrelSwitcheroo:
                    StageDogsAtCurrentObjective();
                    _game.ForceSwitcherooStrike();
                    break;
                case GameManager.MissionVariant.WalkCampaign:
                    StageDogsAtCurrentObjective();
                    _game.ForceWalkCampaign(3f, doorStare: true, presentLeash: true);
                    break;
                case GameManager.MissionVariant.BoneRelay:
                    StageDogsAtCurrentObjective();
                    _game.ForceBoneDig(_game.BoneRelayPuzzle.CorrectTarget);
                    break;
                case GameManager.MissionVariant.GreatEscape:
                    StageDogsAtCurrentObjective();
                    for (int i = 0; i < _game.EscapeStationCount; i++)
                        _game.ForceEscapeStep(_game.GreatEscapePuzzle.NextOwner);
                    break;
                case GameManager.MissionVariant.ChaosMachine:
                    StageDogsAtCurrentObjective();
                    _game.ForceChaosAdvance(0.5f, assisting: true);
                    break;
                case GameManager.MissionVariant.BlanketCatch:
                    StageDogsAtCurrentObjective();
                    _game.ForceBlanketCatch(0f);
                    break;
                case GameManager.MissionVariant.KitchenFoodFrenzy:
                    StageDogsAtCurrentObjective();
                    _game.ForceKitchenCatch(DogId.Cocoa, true);
                    break;
                case GameManager.MissionVariant.OperationPeeBreak:
                    var peeBreak = _game.PeeBreakController;
                    if (peeBreak != null)
                    {
                        _game.ForcePeeBreakAdvance(SocialStimulus.DoorStare | SocialStimulus.PresentLeash, 2.1f);
                        _game.ForcePeeBreakAdvance(peeBreak.Required, 2.6f);
                        _game.ForcePeeBreakAdvance(peeBreak.Required, 2.3f);
                    }
                    break;
                case GameManager.MissionVariant.BabyBirdBedlam:
                    StageDogsAtCurrentObjective();
                    _game.ForceParentRepel();
                    for (int shake = 0; shake < 3; shake++) _game.ForceChickShake();
                    break;
                case GameManager.MissionVariant.TickInvasion:
                    if (_game.TickInvasionController != null)
                    {
                        Vector2 pool = _game.TickInvasionController.PoolObject.transform.position;
                        StageDogsAround(pool);
                        _focusOverride = pool;
                        _game.TickInvasionController.Puzzle.ForceSuperTick(DogId.Cheddar);
                        _game.TickInvasionController.Tick(0.01f, Time.time);
                    }
                    break;
            }
        }

        private void FocusCameraOnActiveMission(GameManager.MissionVariant variant)
        {
            Camera camera = Camera.main;
            if (camera == null || _game == null) return;

            var rig = camera.GetComponent<SharedCameraController>();
            if (rig != null) rig.enabled = false;

            bool peeBreak = variant == GameManager.MissionVariant.OperationPeeBreak;
            float orthoSize = peeBreak ? 8.5f : 8f;

            Vector2 focus = _game.ArenaBounds.center;
            if (_focusOverride.HasValue)
            {
                focus = _focusOverride.Value;
                _focusOverride = null;
            }
            else if (peeBreak && _game.PeeBreakController != null)
            {
                focus = _game.PeeBreakController.DoorPosition + new Vector2(-4f, -1.4f);
            }
            else if (_game.ActiveMissionController != null &&
                     _game.ActiveMissionController.TryGetObjectiveTarget(0, out Transform target, out _, out _)
                     && target != null)
            {
                focus = target.position;
            }
            else
            {
                var dogs = FindObjectsByType<DogController>(FindObjectsSortMode.None);
                if (dogs.Length > 0) focus = dogs[0].transform.position;
            }

            // Real play never shows past the yard's edge: SharedCameraController is always
            // configured with clamp:true (see ArenaBootstrap.BuildScene). This tool disables that
            // rig above and places the camera manually, so it needs the same clamp - otherwise a
            // fence/edge-adjacent objective (Coyotes Fence, Weenie Roundup's bowl) reads as a flat
            // camera-background void that no player would ever actually see. Pee Break's indoor
            // framing is intentionally bespoke (see DEEP-SLICE-OPERATION-PEE-BREAK.md) and skips it.
            if (!peeBreak) focus = ClampFocusToBounds(focus, _game.ArenaBounds, orthoSize, camera.aspect);

            camera.transform.position = new Vector3(focus.x, focus.y, camera.transform.position.z);
            camera.orthographicSize = orthoSize;
        }

        /// <summary>
        /// Mirrors SharedCameraController's own bounds clamp so a manually-placed capture camera
        /// never shows past the yard's edge (see the ArenaBootstrap clamp:true call it stands in for).
        /// Pure/static and exposed for testing, same pattern as <see cref="OutputDirectoryFromArgs"/>.
        /// </summary>
        public static Vector2 ClampFocusToBounds(Vector2 focus, Rect bounds, float orthoSize, float aspect)
        {
            focus.x = ClampAxis(focus.x, bounds.xMin, bounds.xMax, orthoSize * aspect);
            focus.y = ClampAxis(focus.y, bounds.yMin, bounds.yMax, orthoSize);
            return focus;
        }

        private static float ClampAxis(float value, float min, float max, float viewportHalfSize)
        {
            float safeMin = min + viewportHalfSize;
            float safeMax = max - viewportHalfSize;
            return safeMin <= safeMax ? Mathf.Clamp(value, safeMin, safeMax) : (min + max) * 0.5f;
        }

        private bool StageDogsAtCurrentObjective()
        {
            if (_game == null || _game.ActiveMissionController == null) return false;
            if (!_game.ActiveMissionController.TryGetObjectiveTarget(0, out Transform target, out _, out _) || target == null)
                return false;

            StageDogsAround(target.position);
            _focusOverride = target.position;
            return true;
        }

        private static void StageDogsAround(Vector2 point)
        {
            var dogs = FindObjectsByType<DogController>(FindObjectsSortMode.None);
            if (dogs == null || dogs.Length == 0) return;
            for (int i = 0; i < dogs.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                if (dogs[i].TryGetComponent<DogIdentity>(out var identity))
                    side = identity.Id == DogId.Cheddar ? -1f : 1f;
                dogs[i].transform.position = point + new Vector2(side * 1.1f, -0.65f);
                if (dogs[i].TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            }
        }

        private static string Slug(string value)
        {
            if (string.IsNullOrEmpty(value)) return "mission";
            var builder = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsUpper(c) && i > 0) builder.Append('-');
                builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
            }
            return builder.ToString();
        }

        private IEnumerator Capture(string fileName)
        {
            string path = Path.Combine(_outputDirectory, fileName);
            if (File.Exists(path)) File.Delete(path);
            yield return new WaitForEndOfFrame();
            Camera camera = Camera.main;
            if (camera == null) yield break;

            const int width = 1920;
            const int height = 1080;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            WritePpm(path, texture);
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            Destroy(target);
            Destroy(texture);
            yield return null;
        }

        private static void WritePpm(string path, Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            using var output = new BufferedStream(File.Create(path));
            byte[] header = Encoding.ASCII.GetBytes($"P6\n{texture.width} {texture.height}\n255\n");
            output.Write(header, 0, header.Length);
            byte[] row = new byte[texture.width * 3];
            for (int y = texture.height - 1; y >= 0; y--)
            {
                int source = y * texture.width;
                for (int x = 0; x < texture.width; x++)
                {
                    Color32 pixel = pixels[source + x];
                    int target = x * 3;
                    row[target] = pixel.r;
                    row[target + 1] = pixel.g;
                    row[target + 2] = pixel.b;
                }
                output.Write(row, 0, row.Length);
            }
        }
    }
}
