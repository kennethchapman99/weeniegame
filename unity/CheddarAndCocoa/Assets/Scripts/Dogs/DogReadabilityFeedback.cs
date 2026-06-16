using UnityEngine;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>
    /// Generated placeholder dog art and pose readability. This keeps Cheddar/Cocoa identifiable at
    /// gameplay zoom without adding external assets or an art pipeline.
    /// </summary>
    [RequireComponent(typeof(DogController))]
    [RequireComponent(typeof(DogIdentity))]
    public sealed class DogReadabilityFeedback : MonoBehaviour
    {
        public enum Pose
        {
            Idle,
            Run,
            Bark,
            Tug,
            Stunned,
            Rescued,
            Proud,
            Sad
        }

        private DogController _dog;
        private DogIdentity _identity;
        private SpriteRenderer _body;
        private SpriteRenderer _chest;
        private SpriteRenderer _tail;
        private SpriteRenderer _marker;
        private TextMesh _label;
        private Vector3 _baseScale;
        private Pose _forcedPose;
        private float _forcedPoseUntil;
        private float _barkUntil;

        public Pose CurrentPose { get; private set; } = Pose.Idle;
        public string CurrentPoseLabel => CurrentPose.ToString();
        public string IdentityLabel => _label != null ? _label.text : string.Empty;

        public void Init(Sprite sprite)
        {
            _dog = GetComponent<DogController>();
            _identity = GetComponent<DogIdentity>();
            _body = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;

            BuildIdentityArt(sprite);
            _dog.OnBark += OnBark;
            ApplyPose(Pose.Idle);
        }

        public void ShowTug() => ForcePose(Pose.Tug, 0.25f);
        public void ShowRescued() => ForcePose(Pose.Rescued, 1.1f);
        public void ShowProudBrief() => ForcePose(Pose.Proud, 1.1f);
        public void ShowProud() => ForcePose(Pose.Proud, 999f);
        public void ShowSad() => ForcePose(Pose.Sad, 999f);
        public void ClearMissionPose() => _forcedPoseUntil = 0f;

        private void OnDestroy()
        {
            if (_dog != null) _dog.OnBark -= OnBark;
        }

        private void OnBark(DogId _) => _barkUntil = Time.time + 0.35f;

        private void ForcePose(Pose pose, float seconds)
        {
            _forcedPose = pose;
            _forcedPoseUntil = Time.time + seconds;
            ApplyPose(pose);
        }

        private void Update()
        {
            var next = ChoosePose();
            ApplyPose(next);
            AnimatePose(next);
        }

        private Pose ChoosePose()
        {
            if (Time.time < _forcedPoseUntil) return _forcedPose;
            if (_dog.Mode == MovementMode.Stunned) return Pose.Stunned;
            if (_dog.Mode == MovementMode.Tug) return Pose.Tug;
            if (Time.time < _barkUntil) return Pose.Bark;

            if (TryGetComponent<Rigidbody2D>(out var rb) && rb.linearVelocity.sqrMagnitude > 0.05f)
                return Pose.Run;

            return Pose.Idle;
        }

        private void ApplyPose(Pose pose)
        {
            if (CurrentPose == pose && _label != null && !string.IsNullOrEmpty(_label.text)) return;
            CurrentPose = pose;

            if (_label != null)
            {
                _label.text = $"{DogTitle()}\n{PoseCopy(pose)}";
                _label.color = pose switch
                {
                    Pose.Stunned => new Color(1f, 0.45f, 0.45f),
                    Pose.Rescued => new Color(0.55f, 1f, 0.7f),
                    Pose.Proud => new Color(1f, 0.96f, 0.35f),
                    Pose.Sad => new Color(0.72f, 0.82f, 1f),
                    Pose.Tug => new Color(1f, 0.85f, 0.35f),
                    _ => Color.white
                };
            }

            if (_marker != null) _marker.enabled = pose == Pose.Bark || pose == Pose.Proud || pose == Pose.Rescued;
        }

        private void AnimatePose(Pose pose)
        {
            float t = Time.time;
            float wag = _identity.Id == DogId.Cheddar ? 42f : 20f;

            transform.localRotation = pose switch
            {
                Pose.Run => Quaternion.Euler(0f, 0f, Mathf.Sin(t * 16f) * 4f),
                Pose.Bark => Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 7f),
                Pose.Stunned => Quaternion.Euler(0f, 0f, 12f),
                Pose.Sad => Quaternion.Euler(0f, 0f, -7f),
                Pose.Proud => Quaternion.Euler(0f, 0f, Mathf.Sin(t * 4f) * 2f),
                Pose.Tug => Quaternion.Euler(0f, 0f, Mathf.Sin(t * 22f) * 5f),
                _ => Quaternion.identity
            };

            float pop = pose switch
            {
                Pose.Bark => 1.18f,
                Pose.Stunned => 0.86f,
                Pose.Rescued => 1.16f,
                Pose.Proud => 1.12f,
                Pose.Sad => 0.92f,
                Pose.Tug => 1.06f,
                _ => 1f
            };
            transform.localScale = new Vector3(_baseScale.x * pop, _baseScale.y * pop, _baseScale.z);

            if (_tail != null)
            {
                float speed = pose == Pose.Run || pose == Pose.Bark || pose == Pose.Proud ? 18f : 6f;
                float amount = pose == Pose.Sad || pose == Pose.Stunned ? 6f : wag;
                _tail.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * speed) * amount);
            }

            if (_label != null) _label.transform.rotation = Quaternion.identity;
        }

        private void BuildIdentityArt(Sprite sprite)
        {
            bool cheddar = _identity.Id == DogId.Cheddar;
            if (_body != null) _body.color = cheddar ? new Color(1f, 0.68f, 0.26f) : new Color(0.31f, 0.16f, 0.08f);

            _chest = MakePart("Chest", sprite, cheddar ? new Color(1f, 0.88f, 0.5f) : new Color(0.98f, 0.86f, 0.65f),
                new Vector3(0.12f, -0.05f, -0.02f), new Vector3(0.35f, 0.62f, 1f), 12);
            _tail = MakePart("Tail", sprite, cheddar ? new Color(1f, 0.82f, 0.32f) : new Color(0.18f, 0.09f, 0.04f),
                new Vector3(-0.63f, 0.12f, 0f), new Vector3(0.18f, 0.5f, 1f), 9);
            _marker = MakePart("Marker", sprite, cheddar ? new Color(1f, 0.95f, 0.35f) : new Color(0.95f, 0.82f, 0.55f),
                new Vector3(0.36f, 0.3f, -0.03f), new Vector3(0.2f, 0.2f, 1f), 13);

            if (!cheddar)
            {
                MakePart("CocoaSpot", sprite, new Color(0.12f, 0.06f, 0.03f),
                    new Vector3(-0.22f, 0.18f, -0.04f), new Vector3(0.28f, 0.24f, 1f), 13);
                MakePart("CocoaQueenCrown", sprite, new Color(1f, 0.86f, 0.18f),
                    new Vector3(0.28f, 0.48f, -0.04f), new Vector3(0.34f, 0.12f, 1f), 14);
            }
            else
            {
                MakePart("CheddarChaosEar", sprite, new Color(0.94f, 0.5f, 0.12f),
                    new Vector3(0.18f, 0.45f, -0.04f), new Vector3(0.22f, 0.24f, 1f), 14);
            }

            var labelGo = new GameObject("DogReadabilityLabel");
            labelGo.transform.SetParent(transform);
            labelGo.transform.localPosition = new Vector3(0f, 0.95f, -0.1f);
            labelGo.transform.localScale = Vector3.one * 0.085f;
            _label = labelGo.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 22;
        }

        private SpriteRenderer MakePart(string name, Sprite sprite, Color color, Vector3 localPosition, Vector3 localScale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private string DogTitle() => _identity.Id == DogId.Cheddar ? "CHEDDAR CHAOS PUP" : "COCOA SPOT QUEEN";

        private static string PoseCopy(Pose pose) => pose switch
        {
            Pose.Idle => "READY",
            Pose.Run => "ZOOM",
            Pose.Bark => "WOOF!",
            Pose.Tug => "TUG!",
            Pose.Stunned => "STUNNED",
            Pose.Rescued => "RESCUED!",
            Pose.Proud => "PROUD!",
            Pose.Sad => "SAD FLOP",
            _ => pose.ToString()
        };
    }
}
