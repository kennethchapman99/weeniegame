using UnityEngine;

namespace CheddarAndCocoa.CameraRig
{
    /// <summary>
    /// One shared camera that keeps BOTH dogs on screen — the It-Takes-Two couch model (no
    /// split-screen). Follows the midpoint and zooms to fit the pair plus margin, clamped to a
    /// min/max orthographic size and the level bounds.
    ///
    /// RECOMMENDED: in production, drive this with Cinemachine — a CinemachineTargetGroup
    /// containing both dogs + a CinemachineCamera with Group framing does the follow+zoom for
    /// free. This MonoBehaviour is the dependency-light fallback / explicit-control option.
    ///
    /// PROTOTYPE MAP: src/core/camera.ts framed a fixed 960x600 world letterboxed to the screen.
    /// Co-op TV play wants a dynamic frame instead, but reuse the world aspect (1.6:1) and the
    /// recent screen-shake hook (state.shake) for juice.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SharedCameraController : MonoBehaviour
    {
        [SerializeField] private Transform cheddar;
        [SerializeField] private Transform cocoa;

        [Header("Framing")]
        [SerializeField] private float margin = 4f;        // world units of padding around the pair
        [SerializeField] private float minOrthoSize = 6f;  // closest zoom
        [SerializeField] private float maxOrthoSize = 16f; // widest zoom (dogs far apart)
        [SerializeField] private float followLerp = 8f;    // position smoothing
        [SerializeField] private float zoomLerp = 6f;      // size smoothing

        [Header("Level bounds (optional clamp)")]
        [SerializeField] private bool clampToBounds = false;
        [SerializeField] private Rect levelBounds = new Rect(0, 0, 30, 18);

        private Camera _cam;
        private float _shake; // screen-shake magnitude; decays each frame (prototype state.shake)

        private void Awake() => _cam = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (cheddar == null || cocoa == null) return;
            float dt = Time.deltaTime;

            Vector2 mid = (Vector2)(cheddar.position + cocoa.position) * 0.5f;
            float halfSpan = Vector2.Distance(cheddar.position, cocoa.position) * 0.5f + margin;
            float targetSize = Mathf.Clamp(halfSpan, minOrthoSize, maxOrthoSize);

            // Smooth follow + zoom.
            Vector3 target = new Vector3(mid.x, mid.y, transform.position.z);
            if (clampToBounds) target = ClampToBounds(target, targetSize);

            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followLerp * dt));
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetSize, 1f - Mathf.Exp(-zoomLerp * dt));

            // Screen shake (decays). TODO: route addShake() calls here from hits/lands/predators.
            if (_shake > 0.01f)
            {
                transform.position += (Vector3)(Random.insideUnitCircle * _shake);
                _shake = Mathf.Max(0f, _shake - dt * 0.8f);
            }
        }

        /// <summary>Kick the screen shake (cosmetic). Mirrors the TS build's addShake().</summary>
        public void AddShake(float magnitude) => _shake = Mathf.Max(_shake, magnitude);

        private Vector3 ClampToBounds(Vector3 pos, float orthoSize)
        {
            float halfH = orthoSize;
            float halfW = orthoSize * _cam.aspect;
            pos.x = Mathf.Clamp(pos.x, levelBounds.xMin + halfW, levelBounds.xMax - halfW);
            pos.y = Mathf.Clamp(pos.y, levelBounds.yMin + halfH, levelBounds.yMax - halfH);
            return pos;
        }
    }
}
