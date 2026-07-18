using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Guidance role-handoff flourish (G1.4): a one-shot sprite that arcs from one dog's position to
    /// the other's, then destroys itself. Mirrors DogActionFeedback's DogActionParticle life-cycle
    /// (spawn, animate, fade, Destroy) but travels between two explicit points instead of decaying
    /// outward from one, since a handoff needs to visibly land on the dog receiving the turn.
    /// </summary>
    public sealed class DogHandoffSwoosh : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Vector3 _from;
        private Vector3 _to;
        private float _life;
        private float _age;

        public static GameObject Spawn(Sprite sprite, Vector3 from, Vector3 to, float life = 0.4f)
        {
            if (sprite == null) return null;

            var go = new GameObject("DogHandoffSwoosh");
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.6f;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 36;
            renderer.color = Color.white;
            go.AddComponent<DogHandoffSwoosh>().Begin(renderer, from, to, life);
            return go;
        }

        private void Begin(SpriteRenderer renderer, Vector3 from, Vector3 to, float life)
        {
            _renderer = renderer;
            _from = from;
            _to = to;
            _life = Mathf.Max(0.05f, life);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _life);
            float eased = 1f - (1f - t) * (1f - t);
            float arc = Mathf.Sin(t * Mathf.PI) * 0.6f;
            transform.position = Vector3.Lerp(_from, _to, eased) + new Vector3(0f, arc, -0.15f);
            transform.Rotate(0f, 0f, 260f * Time.deltaTime);

            if (_renderer != null)
            {
                Color color = _renderer.color;
                color.a = Mathf.Clamp01(1f - t);
                _renderer.color = color;
            }

            if (_age >= _life) Destroy(gameObject);
        }
    }
}
