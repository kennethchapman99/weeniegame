using UnityEngine;
using UnityEngine.InputSystem;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Bootstrap
{
    /// <summary>
    /// Dead-simple IMGUI overlay for the first playable: a legend that tells you which dog is which,
    /// which controller slot drives it and whether that pad is connected, plus a floating name tag
    /// above each dog and a "WOOF!" flash when it barks. Uses OnGUI so it needs no Canvas, fonts, or
    /// UI packages — just enough to confirm the milestone works. Replace with real UGUI/HUD later.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        private struct Entry
        {
            public DogController dog;
            public DogIdentity identity;
            public int slot;
            public float lastBark; // Time.time of the last bark (for the WOOF flash)
        }

        private Camera _cam;
        private Entry _a, _b;
        private GUIStyle _legend, _tag, _woof;

        public void Init(Camera cam,
            DogController dogA, DogIdentity idA, int slotA,
            DogController dogB, DogIdentity idB, int slotB)
        {
            _cam = cam;
            _a = new Entry { dog = dogA, identity = idA, slot = slotA, lastBark = -10f };
            _b = new Entry { dog = dogB, identity = idB, slot = slotB, lastBark = -10f };

            _a.dog.OnBark += _ => _a.lastBark = Time.time;
            _b.dog.OnBark += _ => _b.lastBark = Time.time;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (_cam == null) return;

            // Legend (top-left).
            GUI.Label(new Rect(12, 10, 520, 120),
                "Cheddar & Cocoa — Controller Test\n" +
                $"P1  CHEDDAR  (golden)  ·  pad {_a.slot}: {PadStatus(_a.slot)}\n" +
                $"P2  COCOA    (brown)   ·  pad {_b.slot}: {PadStatus(_b.slot)}\n" +
                "Left stick: move   ·   X: bark   ·   Y: grab (placeholder)",
                _legend);

            DrawTag(_a, "CHEDDAR");
            DrawTag(_b, "COCOA");
        }

        private void DrawTag(in Entry e, string name)
        {
            if (e.dog == null) return;
            Vector3 sp = _cam.WorldToScreenPoint(e.dog.transform.position + Vector3.up * 0.9f);
            if (sp.z < 0) return;
            float y = Screen.height - sp.y; // GUI y is top-down
            GUI.Label(new Rect(sp.x - 60, y - 14, 120, 22), name, _tag);

            if (Time.time - e.lastBark < 0.6f)
                GUI.Label(new Rect(sp.x - 60, y - 38, 120, 22), "WOOF!", _woof);
        }

        private static string PadStatus(int slot)
        {
            return slot < Gamepad.all.Count ? "connected (" + Gamepad.all[slot].displayName + ")"
                                            : "NOT CONNECTED";
        }

        private void EnsureStyles()
        {
            if (_legend != null) return;
            _legend = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = false };
            _legend.normal.textColor = Color.white;
            _tag = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _tag.normal.textColor = Color.white;
            _woof = new GUIStyle(_tag) { fontSize = 18 };
            _woof.normal.textColor = new Color(1f, 0.95f, 0.4f);
        }
    }
}
