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
            StartCoroutine(CaptureSequence());
        }

        private IEnumerator CaptureSequence()
        {
            Directory.CreateDirectory(_outputDirectory);
            _game.StartMission(GameManager.MissionVariant.BackyardRescue);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Capture("01-local-gameplay.ppm");

            DogController[] dogs = FindObjectsByType<DogController>(FindObjectsSortMode.None);
            foreach (var dog in dogs)
            {
                var input = dog.GetComponent<GamepadPlayerInput>();
                if (input != null) input.enabled = false;
                dog.GetComponent<Rigidbody2D>().linearVelocity = Vector2.right * 5f;
            }
            yield return new WaitForSecondsRealtime(0.14f);
            yield return Capture("02-run-east.ppm");
            foreach (var dog in dogs) dog.GetComponent<Rigidbody2D>().linearVelocity = Vector2.up * 5f;
            yield return new WaitForSecondsRealtime(0.14f);
            yield return Capture("03-run-north.ppm");
            foreach (var dog in dogs) dog.GetComponent<Rigidbody2D>().linearVelocity = Vector2.down * 5f;
            yield return new WaitForSecondsRealtime(0.14f);
            yield return Capture("04-run-south.ppm");
            foreach (var dog in dogs)
            {
                dog.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                var input = dog.GetComponent<GamepadPlayerInput>();
                if (input != null) input.enabled = true;
            }

            if (dogs.Length > 0) dogs[0].Bark();
            yield return new WaitForSecondsRealtime(0.08f);
            yield return Capture("05-bark.ppm");

            _game.ForcePredatorWarning();
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Capture("06-warning.ppm");
            yield return new WaitForSecondsRealtime(0.8f);

            _game.ForcePredatorAttack();
            yield return null;
            foreach (var dog in dogs)
            {
                dog.transform.position = _game.PredatorObject.transform.position;
                dog.Bark();
            }
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Capture("07-rescue.ppm");
            yield return new WaitForSecondsRealtime(0.9f);

            Camera camera = Camera.main;
            if (camera != null)
            {
                var rig = camera.GetComponent<SharedCameraController>();
                if (rig != null) rig.enabled = false;
                Rect bounds = _game.ArenaBounds;
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, camera.transform.position.z);
                float aspect = Mathf.Max(0.1f, camera.aspect);
                camera.orthographicSize = Mathf.Max(bounds.height * 0.5f + 2f, bounds.width * 0.5f / aspect + 2f);
            }
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Capture("08-full-yard.ppm");

            Debug.Log($"Arena art review captures complete: {_outputDirectory}");
            Application.Quit();
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
