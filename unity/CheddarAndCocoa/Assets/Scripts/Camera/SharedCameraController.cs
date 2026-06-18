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
        [SerializeField] private float horizontalMargin = 5f; // world units of side padding
        [SerializeField] private float verticalMargin = 4f;   // world units of top/bottom padding
        [SerializeField] private float minOrthoSize = 7.5f;    // local exploration zoom
        [SerializeField] private float maxOrthoSize = 34f;     // strategic full-yard zoom
        [SerializeField] private float followLerp = 9f;    // position smoothing
        [SerializeField] private float zoomLerp = 7f;      // size smoothing

        [Header("Level bounds (optional clamp)")]
        [SerializeField] private bool clampToBounds = false;
        [SerializeField] private Rect levelBounds = new Rect(0, 0, 30, 18);

        private Camera _cam;
        private float _shake; // screen-shake magnitude; decays each frame (prototype state.shake)

        private void Awake() => _cam = GetComponent<Camera>();

        public float HorizontalMargin => horizontalMargin;
        public float VerticalMargin => verticalMargin;
        public float MinOrthoSize => minOrthoSize;
        public float MaxOrthoSize => maxOrthoSize;
        public float FollowLerp => followLerp;
        public float ZoomLerp => zoomLerp;
        public bool IsClampedToBounds => clampToBounds;
        public Rect LevelBounds => levelBounds;

        public void Configure(float initialOrthoSize, float minSize, float maxSize,
            float horizontalPadding, float verticalPadding, float follow, float zoom,
            bool clamp, Rect bounds)
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            minOrthoSize = minSize;
            maxOrthoSize = Mathf.Max(maxSize, minOrthoSize);
            horizontalMargin = horizontalPadding;
            verticalMargin = verticalPadding;
            followLerp = follow;
            zoomLerp = zoom;
            clampToBounds = clamp;
            levelBounds = bounds;
            _cam.orthographicSize = Mathf.Clamp(initialOrthoSize, minOrthoSize, maxOrthoSize);
        }

        /// <summary>Runtime setup — point the camera at both dogs (used by GameBootstrap).</summary>
        public void SetTargets(Transform cheddarT, Transform cocoaT)
        {
            cheddar = cheddarT;
            cocoa = cocoaT;
        }

        private void LateUpdate()
        {
            if (cheddar == null || cocoa == null) return;
            float dt = Time.deltaTime;

            Vector2 a = cheddar.position;
            Vector2 b = cocoa.position;
            Vector2 mid = (a + b) * 0.5f;
            float halfWidth = Mathf.Abs(a.x - b.x) * 0.5f + horizontalMargin;
            float halfHeight = Mathf.Abs(a.y - b.y) * 0.5f + verticalMargin;
            float aspect = _cam != null && _cam.aspect > 0f ? _cam.aspect : 16f / 9f;
            float targetSize = Mathf.Clamp(Mathf.Max(halfHeight, halfWidth / aspect), minOrthoSize, maxOrthoSize);

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
            pos.x = ClampAxis(pos.x, levelBounds.xMin, levelBounds.xMax, halfW);
            pos.y = ClampAxis(pos.y, levelBounds.yMin, levelBounds.yMax, halfH);
            return pos;
        }

        private static float ClampAxis(float value, float min, float max, float viewportHalfSize)
        {
            float safeMin = min + viewportHalfSize;
            float safeMax = max - viewportHalfSize;
            return safeMin <= safeMax ? Mathf.Clamp(value, safeMin, safeMax) : (min + max) * 0.5f;
        }
    }
}
