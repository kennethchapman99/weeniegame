// BackyardGreyboxPlannerBuilder.cs
//
// Map-based greybox planner for the backyard level. This SUPERSEDES the old photo-wall
// reference scene: it does not paste reference photos into the scene. Instead it lays the
// real top-down aerial photo down as a map underlay and builds editable greybox markers,
// zones, and route/interaction candidates on top of it.
//
// Menus (Cheddar & Cocoa > Backyard):
//   * Build Map-Based Greybox Planner    - (re)build the whole planner scene
//   * Create/Refresh Capture Point Markers - add markers for new points without nuking manual moves
//   * Save Capture Point Positions        - write dragged marker positions back to the JSON
//
// Data sources (produced by the Python importer):
//   Assets/Reference/BackyardCapture/backyard_capture_points.json  (per-point placement data)
//   Assets/Reference/BackyardCapture/Map/Backyard_Aerial.png       (top-down layout underlay)
//   Assets/Reference/BackyardCapture/Map/export_map.png            (annotated capture-point map)
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheddarAndCocoa.BackyardPlanner
{
    // ---- JSON model (field names match backyard_capture_points.json exactly) ----
    [Serializable] public class MapPosition { public float x; public float y; public bool placed; }
    [Serializable]
    public class CapturePoint
    {
        public string id;
        public List<string> images;
        public List<string> types;
        public List<string> directions;
        public List<string> tags;
        public List<string> gameplay_candidates;
        public bool interactive;
        public int staging_index;
        public MapPosition map_position;
    }
    [Serializable] public class PixelSize { public int width; public int height; }
    [Serializable]
    public class CapturePointsFile
    {
        public string area;
        public string generated_at;
        public string aerial_map_asset;
        public string annotated_map_asset;
        public PixelSize aerial_pixel_size;
        public string position_space;
        public List<CapturePoint> points;
    }

    // Tags a capture point marker with its source data so the inspector and Save action can read it.
    public class BackyardCaptureMarker : MonoBehaviour
    {
        public string pointId;
        [TextArea] public string images;
        [TextArea] public string tags;
    }

    public class BackyardGreyboxMarker : MonoBehaviour { public string kind; public string note; }

    public static class BackyardGreyboxPlannerBuilder
    {
        const string DataPath = "Assets/Reference/BackyardCapture/backyard_capture_points.json";
        const string AerialAsset = "Assets/Reference/BackyardCapture/Map/Backyard_Aerial.png";
        const string AnnotatedAsset = "Assets/Reference/BackyardCapture/Map/export_map.png";
        const string ScenePath = "Assets/Scenes/BackyardGreyboxPlannerScene.unity";

        // World footprint of the aerial map. Map is centred on the origin on the XZ plane.
        const float MapWorldWidth = 40f;
        const float AerialFallbackAspect = 1122f / 1366f; // width / height of Ken's aerial

        // ----------------------------------------------------------------- menus

        [MenuItem("Cheddar & Cocoa/Backyard/Build Map-Based Greybox Planner")]
        public static void BuildPlanner()
        {
            var data = LoadData();
            if (data == null) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("BackyardGreyboxRoot");

            float mapW, mapH;
            BuildAerialUnderlay(root.transform, out mapW, out mapH);
            BuildAnnotatedReference(root.transform, mapW, mapH);

            var captureRoot = new GameObject("CapturePoints");
            captureRoot.transform.SetParent(root.transform);
            foreach (var p in data.points) CreateOrUpdateMarker(captureRoot.transform, p, mapW, mapH, force: true);

            BuildGreyboxZones(root.transform, data, mapW, mapH);
            BuildRouteCandidates(root.transform, data, mapW, mapH);
            BuildInteractiveCandidates(root.transform, data, mapW, mapH);
            BuildReferenceNotes(root.transform, data, mapW, mapH);

            BuildCameraAndLight(mapW, mapH);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Backyard Greybox Planner",
                "Built map-based planner:\n" + ScenePath +
                "\n\nDrag the point_xxx markers onto the aerial map, then run\n" +
                "Cheddar & Cocoa > Backyard > Save Capture Point Positions.", "OK");
        }

        [MenuItem("Cheddar & Cocoa/Backyard/Create-Refresh Capture Point Markers")]
        public static void RefreshMarkers()
        {
            var data = LoadData();
            if (data == null) return;

            var captureRoot = GameObject.Find("CapturePoints");
            if (captureRoot == null)
            {
                var root = GameObject.Find("BackyardGreyboxRoot") ?? new GameObject("BackyardGreyboxRoot");
                captureRoot = new GameObject("CapturePoints");
                captureRoot.transform.SetParent(root.transform);
            }

            float mapW, mapH;
            ResolveMapSize(out mapW, out mapH);

            int added = 0;
            foreach (var p in data.points)
            {
                bool existed = FindChild(captureRoot.transform, p.id) != null;
                CreateOrUpdateMarker(captureRoot.transform, p, mapW, mapH, force: false);
                if (!existed) added++;
            }
            EditorSceneManager.MarkSceneDirty(captureRoot.scene);
            EditorUtility.DisplayDialog("Backyard Greybox Planner",
                $"Capture points in data: {data.points.Count}\nNew markers added: {added}\n" +
                "Existing markers kept where you placed them.", "OK");
        }

        [MenuItem("Cheddar & Cocoa/Backyard/Save Capture Point Positions")]
        public static void SavePositions()
        {
            var data = LoadData();
            if (data == null) return;

            var captureRoot = GameObject.Find("CapturePoints");
            if (captureRoot == null)
            {
                EditorUtility.DisplayDialog("Backyard Greybox Planner",
                    "No CapturePoints group in the open scene. Build the planner first.", "OK");
                return;
            }

            float mapW, mapH;
            ResolveMapSize(out mapW, out mapH);

            int saved = 0;
            foreach (var p in data.points)
            {
                var marker = FindChild(captureRoot.transform, p.id);
                if (marker == null) continue;
                Vector3 w = marker.position;
                // Only treat as "placed on the map" if it sits inside the map footprint.
                bool onMap = Mathf.Abs(w.x) <= mapW * 0.5f + 0.01f && Mathf.Abs(w.z) <= mapH * 0.5f + 0.01f;
                if (!onMap) continue;
                if (p.map_position == null) p.map_position = new MapPosition();
                p.map_position.x = Mathf.Clamp01(w.x / mapW + 0.5f);
                p.map_position.y = Mathf.Clamp01(w.z / mapH + 0.5f); // +Z = north = y=1 in JSON space
                p.map_position.placed = true;
                saved++;
            }

            File.WriteAllText(DataPath, JsonUtility.ToJson(data, true));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Backyard Greybox Planner",
                $"Saved {saved} marker position(s) to\n{DataPath}\n\n" +
                "The Python importer preserves these on the next run.", "OK");
        }

        // ----------------------------------------------------------------- map underlay

        static void BuildAerialUnderlay(Transform parent, out float mapW, out float mapH)
        {
            ResolveMapSize(out mapW, out mapH);
            var plane = MakeTexturedPlane("AerialMapUnderlay", AerialAsset, mapW, mapH, 0f);
            plane.transform.SetParent(parent);
            FlatLabel(plane.transform, "AERIAL UNDERLAY (top-down map)",
                new Vector3(0, 0.05f, mapH * 0.5f + 1.4f), 0.5f);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(AerialAsset) == null)
                Debug.LogWarning("Aerial map texture missing: " + AerialAsset + " (run the importer).");
        }

        static void BuildAnnotatedReference(Transform parent, float mapW, float mapH)
        {
            var plane = MakeTexturedPlane("AnnotatedMapReference", AnnotatedAsset, mapW, mapH, -0.05f);
            plane.transform.SetParent(parent);
            FlatLabel(plane.transform, "ANNOTATED MAP (toggle active)",
                new Vector3(0, 0.05f, mapH * 0.5f + 0.7f), 0.4f);
            plane.SetActive(false); // optional toggleable underlay, off by default
        }

        // ----------------------------------------------------------------- capture points

        static void CreateOrUpdateMarker(Transform parent, CapturePoint p, float mapW, float mapH, bool force)
        {
            var existing = FindChild(parent, p.id);
            if (existing != null && !force)
            {
                UpdateMarkerData(existing.gameObject, p); // keep manual position, refresh metadata + label
                return;
            }

            GameObject marker = existing != null ? existing.gameObject : null;
            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = p.id;
                marker.transform.SetParent(parent);
                marker.transform.localScale = Vector3.one * 1.1f;
                SetColor(marker, p.interactive ? new Color(1f, 0.55f, 0.1f) : new Color(0.2f, 0.7f, 1f));
            }

            Vector3 pos = p.map_position != null && p.map_position.placed
                ? WorldFromNorm(p.map_position.x, p.map_position.y, mapW, mapH, 1.1f)
                : StagingPosition(p.staging_index, mapW, mapH);
            marker.transform.position = pos;
            UpdateMarkerData(marker, p);
        }

        static void UpdateMarkerData(GameObject marker, CapturePoint p)
        {
            var tag = marker.GetComponent<BackyardCaptureMarker>() ?? marker.AddComponent<BackyardCaptureMarker>();
            tag.pointId = p.id;
            tag.images = p.images != null ? string.Join(", ", p.images) : "";
            tag.tags = p.tags != null ? string.Join(", ", p.tags) : "";

            foreach (Transform child in marker.transform)
                if (child.name == "label") { UnityEngine.Object.DestroyImmediate(child.gameObject); break; }
            string status = (p.map_position != null && p.map_position.placed) ? "" : "  (staging)";
            FlatLabel(marker.transform, p.id + status, new Vector3(0, 1.4f, 0), 0.32f);
        }

        // ----------------------------------------------------------------- greybox zones

        static readonly (string kind, Color color)[] ZonePalette =
        {
            ("patio", new Color(0.75f, 0.7f, 0.6f)),
            ("deck", new Color(0.6f, 0.45f, 0.3f)),
            ("garden", new Color(0.4f, 0.6f, 0.3f)),
            ("tree", new Color(0.25f, 0.5f, 0.25f)),
            ("shrub", new Color(0.45f, 0.65f, 0.4f)),
            ("fence", new Color(0.55f, 0.4f, 0.25f)),
            ("open_yard", new Color(0.5f, 0.7f, 0.45f)),
        };

        static void BuildGreyboxZones(Transform parent, CapturePointsFile data, float mapW, float mapH)
        {
            var zonesRoot = new GameObject("GreyboxZones");
            zonesRoot.transform.SetParent(parent);
            // Starter palette parked above the map (north strip) for Ken to drag into place.
            float z = mapH * 0.5f + 4f;
            for (int i = 0; i < ZonePalette.Length; i++)
            {
                float x = -mapW * 0.5f + 3f + i * (mapW / ZonePalette.Length);
                var zone = FlatBlock(zonesRoot.transform, "zone_" + ZonePalette[i].kind,
                    new Vector3(x, 0.1f, z), new Vector3(3.5f, 0.2f, 3.5f), ZonePalette[i].color);
                zone.GetComponent<BackyardGreyboxMarker>().kind = "zone_" + ZonePalette[i].kind;
            }
            // Where photo tags name a zone feature, drop a hint block near that point's marker.
            foreach (var p in data.points)
            {
                if (p.gameplay_candidates == null) continue;
                foreach (var cand in p.gameplay_candidates)
                {
                    var match = Array.FindIndex(ZonePalette, z2 => z2.kind == cand || (cand == "shrub" && z2.kind == "garden"));
                    if (match < 0) continue;
                    Vector3 basePos = p.map_position != null && p.map_position.placed
                        ? WorldFromNorm(p.map_position.x, p.map_position.y, mapW, mapH, 0.1f)
                        : StagingPosition(p.staging_index, mapW, mapH) + new Vector3(0, -1f, 0);
                    FlatBlock(zonesRoot.transform, $"zone_{cand}_{p.id}", basePos,
                        new Vector3(2f, 0.15f, 2f), ZonePalette[match].color);
                }
            }
        }

        // ----------------------------------------------------------------- route candidates

        static void BuildRouteCandidates(Transform parent, CapturePointsFile data, float mapW, float mapH)
        {
            var routeRoot = new GameObject("RouteCandidates");
            routeRoot.transform.SetParent(parent);
            float z = mapH * 0.5f + 8f;
            string[] kinds = { "dogview_route_waypoint", "tunnel_or_secret_route", "hide_spot",
                               "predator_escape_route", "squirrel_route" };
            for (int i = 0; i < kinds.Length; i++)
            {
                float x = -mapW * 0.5f + 3f + i * (mapW / kinds.Length);
                var m = FlatBlock(routeRoot.transform, kinds[i], new Vector3(x, 0.5f, z),
                    new Vector3(1.3f, 1f, 1.3f), new Color(0.9f, 0.85f, 0.2f));
                m.GetComponent<BackyardGreyboxMarker>().kind = kinds[i];
                FlatLabel(m.transform, kinds[i], new Vector3(0, 1.2f, 0), 0.22f);
            }
            // Tunnel/hide tags promote a real route candidate at that point.
            foreach (var p in data.points)
            {
                if (p.tags == null) continue;
                if (p.tags.Contains("tunnel") || p.tags.Contains("hide"))
                {
                    Vector3 basePos = p.map_position != null && p.map_position.placed
                        ? WorldFromNorm(p.map_position.x, p.map_position.y, mapW, mapH, 0.6f)
                        : StagingPosition(p.staging_index, mapW, mapH) + new Vector3(1.4f, 0, 0);
                    var m = FlatBlock(routeRoot.transform, "route_" + p.id, basePos,
                        Vector3.one * 1.1f, new Color(0.95f, 0.7f, 0.1f));
                    m.GetComponent<BackyardGreyboxMarker>().kind = "tunnel_or_hide";
                }
            }
        }

        // ----------------------------------------------------------------- interactive candidates

        static void BuildInteractiveCandidates(Transform parent, CapturePointsFile data, float mapW, float mapH)
        {
            var interRoot = new GameObject("InteractiveCandidates");
            interRoot.transform.SetParent(parent);
            float z = mapH * 0.5f + 12f;
            string[] kinds = { "cover_zone", "fence_gap", "dig_spot", "bark_trigger", "leash_checkpoint" };
            var color = new Color(0.95f, 0.3f, 0.5f);
            for (int i = 0; i < kinds.Length; i++)
            {
                float x = -mapW * 0.5f + 3f + i * (mapW / kinds.Length);
                var m = FlatBlock(interRoot.transform, kinds[i], new Vector3(x, 0.5f, z),
                    new Vector3(1.3f, 1f, 1.3f), color);
                m.GetComponent<BackyardGreyboxMarker>().kind = kinds[i];
                FlatLabel(m.transform, kinds[i], new Vector3(0, 1.2f, 0), 0.22f);
            }
        }

        // ----------------------------------------------------------------- reference notes (text only)

        static void BuildReferenceNotes(Transform parent, CapturePointsFile data, float mapW, float mapH)
        {
            var notesRoot = new GameObject("ReferenceNotes");
            notesRoot.transform.SetParent(parent);
            FlatLabel(notesRoot.transform,
                "REFERENCE NOTES — filenames + tags only (browse photos via contact_sheets/*.html)",
                new Vector3(mapW * 0.5f + 10f, 0, mapH * 0.5f), 0.5f);
            float x = mapW * 0.5f + 6f;
            float top = mapH * 0.5f;
            for (int i = 0; i < data.points.Count; i++)
            {
                var p = data.points[i];
                string text = $"{p.id}\n{(p.images != null ? string.Join(", ", p.images) : "")}\n" +
                              $"tags: {(p.tags != null ? string.Join(", ", p.tags) : "")}";
                FlatLabel(notesRoot.transform, text, new Vector3(x, 0, top - i * 1.6f), 0.2f,
                    TextAnchor.UpperLeft);
            }
        }

        // ----------------------------------------------------------------- helpers

        static CapturePointsFile LoadData()
        {
            if (!File.Exists(DataPath))
            {
                EditorUtility.DisplayDialog("Backyard Greybox Planner",
                    "Capture-point data not found:\n" + DataPath +
                    "\n\nRun the Python importer first (rerun_backyard_greybox_planner_mac.command).", "OK");
                return null;
            }
            AssetDatabase.Refresh();
            var data = JsonUtility.FromJson<CapturePointsFile>(File.ReadAllText(DataPath));
            if (data == null || data.points == null || data.points.Count == 0)
            {
                EditorUtility.DisplayDialog("Backyard Greybox Planner", "Capture-point data has no points.", "OK");
                return null;
            }
            return data;
        }

        static void ResolveMapSize(out float mapW, out float mapH)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AerialAsset);
            float aspect = (tex != null && tex.height > 0) ? (float)tex.width / tex.height : AerialFallbackAspect;
            mapW = MapWorldWidth;
            mapH = MapWorldWidth / Mathf.Max(0.05f, aspect);
        }

        // Normalized (0..1, origin bottom-left) -> world on the XZ plane. Inverse of SavePositions.
        // With straight-down orthographic camera: world +X = screen right (east), world +Z = screen up (north).
        // JSON y=1 = north = world +Z; y=0 = south = world -Z.
        static Vector3 WorldFromNorm(float nx, float ny, float mapW, float mapH, float y)
        {
            return new Vector3((nx - 0.5f) * mapW, y, (ny - 0.5f) * mapH);
        }

        // Unplaced points get parked in a tidy grid to the LEFT (west) of the map.
        static Vector3 StagingPosition(int index, float mapW, float mapH)
        {
            const int cols = 4;
            float spacing = 3f;
            int col = index % cols, row = index / cols;
            float x = -mapW * 0.5f - 4f - col * spacing;
            float z = mapH * 0.5f - row * spacing;
            return new Vector3(x, 1.1f, z);
        }

        static GameObject MakeTexturedPlane(string name, string assetPath, float mapW, float mapH, float y)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane); // already horizontal, faces +Y
            plane.name = name;
            plane.transform.position = new Vector3(0, y, 0);
            plane.transform.localScale = new Vector3(mapW / 10f, 1f, mapH / 10f); // Unity Plane is 10x10
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            var shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "mat_" + name };
            if (tex != null) mat.mainTexture = tex;
            plane.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return plane;
        }

        static GameObject FlatBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = pos;
            cube.transform.localScale = scale;
            SetColor(cube, color);
            if (cube.GetComponent<BackyardGreyboxMarker>() == null) cube.AddComponent<BackyardGreyboxMarker>();
            return cube;
        }

        static void FlatLabel(Transform parent, string text, Vector3 localPos, float size,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("label");
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            // Lies flat on the XZ plane facing +Y to be read by the straight-down orthographic camera.
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = size;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Left;
            tm.color = Color.white;
        }

        static void SetColor(GameObject go, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")
                         ?? Shader.Find("Diffuse");
            var mat = new Material(shader) { name = "mat_" + go.name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform c in parent) if (c.name == name) return c;
            return null;
        }

        static void BuildCameraAndLight(float mapW, float mapH)
        {
            var lightObj = new GameObject("PlannerLight");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            lightObj.transform.rotation = Quaternion.Euler(90, 0, 0); // straight down for flat map

            var camObj = new GameObject("PlannerCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            // Show the whole map plus a bit of the surrounding staging palette.
            cam.orthographicSize = Mathf.Max(mapW, mapH) * 0.7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
            // Straight-down orthographic: +X = screen right (east), +Z = screen up (north). No mirror needed.
            camObj.transform.position = new Vector3(0, Mathf.Max(mapW, mapH) * 2f, 0);
            camObj.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }
}
