// GameplayFirstPlaytestLabBuilder.cs
//
// Generates an editor-only primitive scene for testing level readability and co-op beats without
// relying on photo backgrounds or final art. This is an authoring artifact, not a shipped mission.
//
// Menu:
//   Cheddar & Cocoa > Gameplay First > Build Generated Playtest Lab
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheddarAndCocoa.GameplayFirst
{
    public static class GameplayFirstPlaytestLabBuilder
    {
        const string ScenePath = "Assets/Scenes/GameplayFirstPlaytestLab.unity";
        const string MaterialRoot = "Assets/Generated/GameplayFirst/Materials";

        [MenuItem("Cheddar & Cocoa/Gameplay First/Build Generated Playtest Lab")]
        public static void BuildLab()
        {
            BuildLab(showDialog: true);
        }

        public static void BuildLabBatch()
        {
            BuildLab(showDialog: false);
        }

        static void BuildLab(bool showDialog)
        {
            EnsureGeneratedFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("GameplayFirstPlaytestLabRoot");

            BuildHeader(root.transform);
            BuildArena(root.transform);
            BuildPeeBreakRoom(root.transform);
            BuildPlayableComponentGallery(root.transform);
            BuildCoopBeatLanes(root.transform);
            BuildAcceptanceWall(root.transform);
            BuildCameraAndLight();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Gameplay First Playtest Lab",
                    "Built generated playtest lab:\n" + ScenePath +
                    "\n\nUse this for primitive level layout, role readability, and couch-playtest notes before replacing anything with realistic art.",
                    "OK");
            }
        }

        static void BuildHeader(Transform root)
        {
            FlatLabel(root,
                "GAMEPLAY-FIRST PLAYTEST LAB\nGenerated primitives only. Prove the beat is readable and fun before realistic art.",
                new Vector3(0f, 0.08f, 21f),
                0.52f,
                TextAnchor.MiddleCenter,
                Color.white);
        }

        static void BuildArena(Transform root)
        {
            var arena = new GameObject("ReadableArenaScale");
            arena.transform.SetParent(root);

            Block(arena.transform, "PlayableFloor_120x68", new Vector3(0f, -0.08f, 0f), new Vector3(48f, 0.12f, 28f), ColorFor("yard_green"));
            RingFence(arena.transform, 48f, 28f);

            District(arena.transform, "PATIO", new Vector3(-15f, 0.01f, 7.5f), new Vector3(12f, 0.08f, 7f), ColorFor("patio"));
            District(arena.transform, "OPEN YARD", new Vector3(-2f, 0.02f, -2f), new Vector3(18f, 0.08f, 13f), ColorFor("open_yard"));
            District(arena.transform, "GARDEN / SMELL", new Vector3(15f, 0.03f, -5.5f), new Vector3(11f, 0.08f, 9f), ColorFor("garden"));
            District(arena.transform, "COVER / HIDE", new Vector3(13f, 0.04f, 8f), new Vector3(9f, 0.08f, 6f), ColorFor("cover"));

            Pad(arena.transform, "CheddarSpawn", new Vector3(-5f, 0.08f, -12f), ColorFor("cheddar"), "CHEDDAR\nfast chaos");
            Pad(arena.transform, "CocoaSpawn", new Vector3(5f, 0.08f, -12f), ColorFor("cocoa"), "COCOA\nsteady anchor");
        }

        static void BuildPeeBreakRoom(Transform root)
        {
            var room = new GameObject("OperationPeeBreakGreybox");
            room.transform.SetParent(root);
            Vector3 origin = new Vector3(-33f, 0f, 0f);

            Block(room.transform, "RoomFloor", origin + new Vector3(0f, -0.04f, 0f), new Vector3(22f, 0.08f, 16f), ColorFor("room_floor"));
            Block(room.transform, "BackWall_DoorSide", origin + new Vector3(0f, 0.8f, 7.9f), new Vector3(22f, 1.6f, 0.25f), ColorFor("room_wall"));
            Block(room.transform, "HallwayChokeReadablePath", origin + new Vector3(-4.8f, 0.02f, -1.6f), new Vector3(5.2f, 0.05f, 3.6f), WithAlpha(ColorFor("cheddar"), 0.38f));
            Block(room.transform, "Couch_TeenagerAnchor", origin + new Vector3(0f, 0.35f, 4.8f), new Vector3(9f, 0.7f, 2f), ColorFor("couch"));
            Block(room.transform, "Door_ClimaxTarget", origin + new Vector3(9f, 1.4f, 0f), new Vector3(0.45f, 2.8f, 5f), ColorFor("door"));
            Block(room.transform, "DoorSunbeam_PayoffPreview", origin + new Vector3(7f, 0.04f, -1.2f), new Vector3(3.6f, 0.05f, 5.5f), WithAlpha(ColorFor("door"), 0.42f));
            Block(room.transform, "PhoneBoss", origin + new Vector3(1.6f, 0.35f, 3.1f), new Vector3(1.4f, 0.16f, 0.9f), ColorFor("phone"));
            Block(room.transform, "ChargerCord_CocoaRoute", origin + new Vector3(0.5f, 0.08f, 2.85f), new Vector3(3.9f, 0.06f, 0.14f), ColorFor("charger"));
            Block(room.transform, "TeenagerAttentionCone", origin + new Vector3(2.5f, 0.03f, 1.5f), new Vector3(5.8f, 0.04f, 4.4f), WithAlpha(ColorFor("phone"), 0.28f));
            Block(room.transform, "BladderPressureMeter", origin + new Vector3(0f, 0.12f, -7.2f), new Vector3(8f, 0.16f, 0.5f), ColorFor("bladder"));
            Block(room.transform, "PhoneBatteryMeter", origin + new Vector3(0f, 0.12f, -6.45f), new Vector3(8f, 0.16f, 0.5f), ColorFor("phone"));

            Pad(room.transform, "Beat1_CocoaDoorStare", origin + new Vector3(6f, 0.08f, -1.7f), ColorFor("cocoa"), "1 COCOA\nDOOR STARE");
            Pad(room.transform, "Beat2_CheddarLeash", origin + new Vector3(3f, 0.08f, -4.6f), ColorFor("cheddar"), "2 CHEDDAR\nPRESENT LEASH");
            Pad(room.transform, "Beat3_CheddarBlockHallway", origin + new Vector3(-4.8f, 0.08f, -0.2f), ColorFor("cheddar"), "3 CHEDDAR\nBLOCK HALL");
            Pad(room.transform, "Beat3_CocoaUnplugCharger", origin + new Vector3(-0.6f, 0.08f, 2.6f), ColorFor("cocoa"), "3 COCOA\nUNPLUG");
            Pad(room.transform, "Beat4_UnitedBarkDoor", origin + new Vector3(6.2f, 0.08f, 2.1f), ColorFor("bark"), "4 BOTH\nBARK WINDOW");
            Pad(room.transform, "MisreadResetSpot", origin + new Vector3(-6.9f, 0.08f, 4.8f), ColorFor("failure"), "MISREAD PROP\nRESET JOBS");

            Arrow(room.transform, "Beat2_Dependency_CocoaToCheddar", origin + new Vector3(4.4f, 0.16f, -3.2f), 2.2f, Color.white);
            Arrow(room.transform, "Beat3_RoleFlip_CheddarToCocoa", origin + new Vector3(-3.4f, 0.16f, 1.2f), 2.8f, Color.white);

            FlatLabel(room.transform,
                "Operation Pee Break layout\nExact roles, clear stations, funny reset, visible door payoff",
                origin + new Vector3(0f, 0.08f, 8.9f),
                0.3f,
                TextAnchor.MiddleCenter,
                Color.white);
        }

        static void BuildPlayableComponentGallery(Transform root)
        {
            var gallery = new GameObject("ExistingLevelPlayableComponents");
            gallery.transform.SetParent(root);
            Vector3 origin = new Vector3(0f, 0f, 18.2f);

            ComponentTile(gallery.transform, origin + new Vector3(-18f, 0f, 0f), "KITCHEN\nFALLING FOOD", "counter lane\ncatch zones", ColorFor("kitchen"));
            ComponentTile(gallery.transform, origin + new Vector3(-6f, 0f, 0f), "SOCK PANIC\nTIP + DIVE", "basket opener\npartner sock", ColorFor("basket"));
            ComponentTile(gallery.transform, origin + new Vector3(6f, 0f, 0f), "EAGLE SHADOW\nHIDE + RESCUE", "sweep lane\ncover pads", ColorFor("cover"));
            ComponentTile(gallery.transform, origin + new Vector3(18f, 0f, 0f), "COYOTE FENCE\nPIN + FILL", "weak spot\nbark hold", ColorFor("fence"));
        }

        static void ComponentTile(Transform parent, Vector3 position, string title, string detail, Color color)
        {
            Block(parent, title.Replace('\n', '_') + "_Tile", position, new Vector3(9.5f, 0.08f, 2.4f), WithAlpha(color, 0.62f));
            Pad(parent, title.Replace('\n', '_') + "_RolePadA", position + new Vector3(-2.6f, 0.08f, 0f), ColorFor("cheddar"), "DOG A");
            Pad(parent, title.Replace('\n', '_') + "_RolePadB", position + new Vector3(2.6f, 0.08f, 0f), ColorFor("cocoa"), "DOG B");
            Arrow(parent, title.Replace('\n', '_') + "_Dependency", position + new Vector3(-1.2f, 0.16f, 0f), 2.4f, Color.white);
            FlatLabel(parent, title + "\n" + detail, position + new Vector3(-4.4f, 0.18f, 1.25f), 0.18f, TextAnchor.UpperLeft, Color.white);
        }

        static void BuildCoopBeatLanes(Transform root)
        {
            var lanes = new GameObject("CoopBeatPrototypeLanes");
            lanes.transform.SetParent(root);

            BuildLane(lanes.transform, 0, "HOLD + RELEASE", "Cocoa holds object", "Cheddar crosses/steals", "object snaps back, instant retry", ColorFor("cocoa"));
            BuildLane(lanes.transform, 1, "DISTRACT + SNEAK", "Cheddar pulls attention", "Cocoa takes clean route", "actor turns, drop item", ColorFor("cheddar"));
            BuildLane(lanes.transform, 2, "SMELL + ACT", "one dog reads scent", "partner digs / carries", "wrong scent makes decoy", ColorFor("scent"));
            BuildLane(lanes.transform, 3, "RESCUE AS PUZZLE", "stuck dog wiggles clue", "free dog changes world", "more drama, no hard fail", ColorFor("rescue"));
        }

        static void BuildLane(Transform parent, int index, string title, string opener, string payoff, string fail, Color color)
        {
            float z = 13f - index * 4f;
            float x = 31f;
            Block(parent, title + "_Lane", new Vector3(x, 0.01f, z), new Vector3(23f, 0.08f, 2.5f), WithAlpha(color, 0.45f));
            Pad(parent, title + "_OpeningPad", new Vector3(x - 7f, 0.08f, z), color, "OPENING\n" + opener);
            Pad(parent, title + "_ProgressPad", new Vector3(x + 1f, 0.08f, z), ColorFor("progress"), "PROGRESS\n" + payoff);
            Pad(parent, title + "_FailurePad", new Vector3(x + 8.2f, 0.08f, z), ColorFor("failure"), "FUNNY FAIL\n" + fail);
            FlatLabel(parent, title, new Vector3(x - 11f, 0.08f, z + 1.7f), 0.22f, TextAnchor.MiddleLeft, Color.white);
            Arrow(parent, title + "_DependencyArrow", new Vector3(x - 3f, 0.12f, z), 5.6f, Color.white);
        }

        static void BuildAcceptanceWall(Transform root)
        {
            var wall = new GameObject("CouchPlaytestAcceptanceWall");
            wall.transform.SetParent(root);
            Vector3 origin = new Vector3(0f, 0f, -19f);
            Block(wall.transform, "AcceptanceBackdrop", origin + new Vector3(0f, 0.1f, 0f), new Vector3(44f, 0.15f, 5f), ColorFor("acceptance"));
            FlatLabel(wall.transform,
                "COUCH PLAYTEST PASS CRITERIA\n1. Both players know their current job in five seconds.\n2. Each beat has an opening dog and a progress dog.\n3. Failure is visible, funny, and recoverable.\n4. Success changes the world, not just a counter.\n5. Replace art only after this loop is fun.",
                origin + new Vector3(-20.5f, 0.22f, 1.7f),
                0.25f,
                TextAnchor.UpperLeft,
                Color.white);
        }

        static void RingFence(Transform parent, float width, float depth)
        {
            Color fence = ColorFor("fence");
            Block(parent, "Fence_North", new Vector3(0f, 0.35f, depth * 0.5f), new Vector3(width, 0.7f, 0.35f), fence);
            Block(parent, "Fence_South", new Vector3(0f, 0.35f, -depth * 0.5f), new Vector3(width, 0.7f, 0.35f), fence);
            Block(parent, "Fence_East", new Vector3(width * 0.5f, 0.35f, 0f), new Vector3(0.35f, 0.7f, depth), fence);
            Block(parent, "Fence_West", new Vector3(-width * 0.5f, 0.35f, 0f), new Vector3(0.35f, 0.7f, depth), fence);
        }

        static void District(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            Block(parent, name + "_District", position, scale, color);
            FlatLabel(parent, name, position + new Vector3(-scale.x * 0.42f, 0.08f, scale.z * 0.28f), 0.22f, TextAnchor.MiddleLeft, Color.white);
        }

        static GameObject Block(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            SetMaterial(go, color);
            return go;
        }

        static GameObject Pad(Transform parent, string name, Vector3 position, Color color, string label)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = new Vector3(1.6f, 0.08f, 1.6f);
            SetMaterial(go, color);
            FlatLabel(go.transform, label, new Vector3(0f, 0.35f, 0f), 0.16f, TextAnchor.MiddleCenter, Color.white);
            return go;
        }

        static void Arrow(Transform parent, string name, Vector3 position, float length, Color color)
        {
            var shaft = Block(parent, name + "_Shaft", position, new Vector3(length, 0.08f, 0.12f), color);
            shaft.transform.rotation = Quaternion.identity;
            Block(parent, name + "_HeadA", position + new Vector3(length * 0.5f, 0f, 0.28f), new Vector3(0.65f, 0.08f, 0.12f), color)
                .transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            Block(parent, name + "_HeadB", position + new Vector3(length * 0.5f, 0f, -0.28f), new Vector3(0.65f, 0.08f, 0.12f), color)
                .transform.rotation = Quaternion.Euler(0f, -45f, 0f);
        }

        static void FlatLabel(Transform parent, string text, Vector3 localPos, float size, TextAnchor anchor, Color color)
        {
            var go = new GameObject("label");
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.characterSize = size;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Left;
            mesh.color = color;
        }

        static void BuildCameraAndLight()
        {
            var lightObj = new GameObject("GameplayFirstLight");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var camObj = new GameObject("GameplayFirstCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 25f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.12f, 0.14f);
            camObj.transform.position = new Vector3(0f, 55f, 0f);
            camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        static void SetMaterial(GameObject go, Color color)
        {
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
        }

        static Material MaterialFor(Color color)
        {
            string materialName = "mat_color_" + ColorUtility.ToHtmlStringRGBA(color);
            string path = $"{MaterialRoot}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            ConfigureTransparency(material, color.a < 0.999f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void ConfigureTransparency(Material material, bool transparent)
        {
            if (transparent)
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                material.SetOverrideTag("RenderType", "");
                material.renderQueue = -1;
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
        }

        static Color ColorFor(string key) => key switch
        {
            "yard_green" => new Color(0.22f, 0.43f, 0.24f),
            "patio" => new Color(0.55f, 0.51f, 0.44f),
            "open_yard" => new Color(0.28f, 0.55f, 0.3f),
            "garden" => new Color(0.28f, 0.43f, 0.18f),
            "cover" => new Color(0.16f, 0.36f, 0.29f),
            "fence" => new Color(0.39f, 0.27f, 0.16f),
            "room_floor" => new Color(0.28f, 0.29f, 0.32f),
            "room_wall" => new Color(0.18f, 0.19f, 0.22f),
            "couch" => new Color(0.37f, 0.31f, 0.52f),
            "door" => new Color(1f, 0.76f, 0.24f),
            "phone" => new Color(0.15f, 0.78f, 0.9f),
            "charger" => new Color(0.77f, 0.56f, 1f),
            "bladder" => new Color(0.36f, 0.66f, 1f),
            "kitchen" => new Color(0.57f, 0.33f, 0.18f),
            "basket" => new Color(0.74f, 0.52f, 0.25f),
            "cheddar" => new Color(1f, 0.52f, 0.17f),
            "cocoa" => new Color(0.34f, 0.2f, 0.11f),
            "bark" => new Color(1f, 0.92f, 0.28f),
            "scent" => new Color(0.33f, 0.84f, 0.62f),
            "rescue" => new Color(0.42f, 0.62f, 1f),
            "progress" => new Color(0.35f, 0.75f, 0.42f),
            "failure" => new Color(1f, 0.36f, 0.28f),
            "acceptance" => new Color(0.21f, 0.2f, 0.25f),
            _ => new Color(0.8f, 0.8f, 0.8f)
        };

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static void EnsureGeneratedFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Generated"))
                AssetDatabase.CreateFolder("Assets", "Generated");
            if (!AssetDatabase.IsValidFolder("Assets/Generated/GameplayFirst"))
                AssetDatabase.CreateFolder("Assets/Generated", "GameplayFirst");
            if (!AssetDatabase.IsValidFolder(MaterialRoot))
                AssetDatabase.CreateFolder("Assets/Generated/GameplayFirst", "Materials");
        }
    }
}
