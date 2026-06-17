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
        private SpriteRenderer _head;
        private SpriteRenderer _snout;
        private SpriteRenderer _ear;
        private SpriteRenderer _collar;
        private SpriteRenderer _eye;
        private SpriteRenderer _frontFeet;
        private SpriteRenderer _backFeet;
        private SpriteRenderer _tail;
        private SpriteRenderer _marker;
        private SpriteRenderer _intentArrow;
        private TextMesh _label;
        private Vector3 _baseScale;
        private Vector2 _lastIntentDir = Vector2.right;
        private Pose _forcedPose;
        private float _forcedPoseUntil;
        private float _barkUntil;

        public Pose CurrentPose { get; private set; } = Pose.Idle;
        public string CurrentPoseLabel => CurrentPose.ToString();
        public string IdentityLabel => _label != null ? _label.text : string.Empty;
        public string FacingIntentLabel => _lastIntentDir.x >= 0f ? "FacingRight" : "FacingLeft";
        public string ArtDirectionSignature => _identity == null
            ? string.Empty
            : _identity.Id == DogId.Cheddar
                ? "long-low-golden-chaos-puppy-red-collar"
                : "long-low-chocolate-spot-queen-teal-collar";

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

            if (_marker != null) _marker.enabled = pose == Pose.Bark || pose == Pose.Proud || pose == Pose.Rescued || pose == Pose.Stunned;
        }

        private void AnimatePose(Pose pose)
        {
            float t = Time.time;
            float wag = _identity.Id == DogId.Cheddar ? 42f : 20f;
            Vector2 velocity = Vector2.zero;
            if (TryGetComponent<Rigidbody2D>(out var rb)) velocity = rb.linearVelocity;
            if (velocity.sqrMagnitude > 0.05f) _lastIntentDir = velocity.normalized;

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

            if (_eye != null)
            {
                _eye.transform.localScale = pose switch
                {
                    Pose.Stunned => new Vector3(0.05f, 0.18f, 1f),
                    Pose.Sad => new Vector3(0.08f, 0.08f, 1f),
                    Pose.Bark => new Vector3(0.13f, 0.12f, 1f),
                    Pose.Proud => new Vector3(0.12f, 0.12f, 1f),
                    _ => new Vector3(0.1f, 0.1f, 1f)
                };
                _eye.transform.localRotation = pose == Pose.Stunned ? Quaternion.Euler(0f, 0f, 45f) : Quaternion.identity;
            }

            if (_snout != null)
            {
                float chew = pose == Pose.Bark ? Mathf.Sin(t * 40f) * 0.04f : 0f;
                _snout.transform.localPosition = new Vector3(0.78f + chew, 0.03f, -0.05f);
            }

            if (_head != null)
            {
                float headTilt = pose switch
                {
                    Pose.Bark => -8f,
                    Pose.Proud => 5f,
                    Pose.Sad => -5f,
                    _ => 0f
                };
                _head.transform.localRotation = Quaternion.Euler(0f, 0f, headTilt);
            }

            if (_ear != null)
            {
                float flop = pose == Pose.Run || pose == Pose.Bark ? Mathf.Sin(t * 18f) * 10f : -8f;
                _ear.transform.localRotation = Quaternion.Euler(0f, 0f, flop);
            }

            if (_frontFeet != null && _backFeet != null)
            {
                float step = pose == Pose.Run ? Mathf.Sin(t * 16f) * 0.04f : 0f;
                _frontFeet.transform.localPosition = new Vector3(0.38f, -0.53f + step, 0f);
                _backFeet.transform.localPosition = new Vector3(-0.42f, -0.53f - step, 0f);
            }

            if (_collar != null)
            {
                float collarPop = pose == Pose.Bark || pose == Pose.Proud ? 1.08f : 1f;
                _collar.transform.localScale = new Vector3(0.12f, 0.7f * collarPop, 1f);
            }

            if (_intentArrow != null)
            {
                bool moving = velocity.sqrMagnitude > 0.05f && pose == Pose.Run;
                _intentArrow.enabled = moving;
                if (moving)
                {
                    _intentArrow.transform.localPosition = new Vector3(_lastIntentDir.x * 0.9f, _lastIntentDir.y * 0.45f, -0.09f);
                    _intentArrow.transform.localRotation = Quaternion.Euler(0f, 0f,
                        Mathf.Atan2(_lastIntentDir.y, _lastIntentDir.x) * Mathf.Rad2Deg);
                }
            }

            if (_label != null) _label.transform.rotation = Quaternion.identity;
        }

        private void BuildIdentityArt(Sprite sprite)
        {
            bool cheddar = _identity.Id == DogId.Cheddar;
            Color body = cheddar ? new Color(1f, 0.67f, 0.22f) : new Color(0.28f, 0.13f, 0.06f);
            Color muzzle = cheddar ? new Color(1f, 0.82f, 0.42f) : new Color(0.7f, 0.43f, 0.24f);
            if (_body != null) _body.color = body;

            _head = MakePart("DachshundHead", sprite, body,
                new Vector3(0.52f, 0.1f, -0.03f), new Vector3(0.38f, 0.52f, 1f), 14);
            _snout = MakePart("LongDogSnout", sprite, muzzle,
                new Vector3(0.78f, 0.03f, -0.05f), new Vector3(0.34f, 0.22f, 1f), 15);
            _ear = MakePart(cheddar ? "CheddarFloppyEar" : "CocoaVelvetEar", sprite,
                cheddar ? new Color(0.91f, 0.44f, 0.08f) : new Color(0.13f, 0.06f, 0.03f),
                new Vector3(0.38f, 0.3f, -0.06f), new Vector3(0.2f, 0.46f, 1f), 16);
            _collar = MakePart(cheddar ? "CheddarRedCollar" : "CocoaTealCollar", sprite,
                cheddar ? new Color(0.95f, 0.18f, 0.1f) : new Color(0.08f, 0.78f, 0.84f),
                new Vector3(0.2f, 0.02f, -0.07f), new Vector3(0.12f, 0.7f, 1f), 17);
            _eye = MakePart("ExpressionEye", sprite, Color.black,
                new Vector3(0.62f, 0.18f, -0.08f), new Vector3(0.1f, 0.1f, 1f), 18);
            _chest = MakePart("ChestPatch", sprite, cheddar ? new Color(1f, 0.9f, 0.52f) : new Color(0.96f, 0.84f, 0.64f),
                new Vector3(0.08f, -0.08f, -0.02f), new Vector3(0.28f, 0.52f, 1f), 12);
            _frontFeet = MakePart("TinyFrontFeet", sprite, cheddar ? new Color(0.87f, 0.41f, 0.08f) : new Color(0.12f, 0.06f, 0.03f),
                new Vector3(0.38f, -0.53f, 0f), new Vector3(0.18f, 0.22f, 1f), 8);
            _backFeet = MakePart("TinyBackFeet", sprite, cheddar ? new Color(0.87f, 0.41f, 0.08f) : new Color(0.12f, 0.06f, 0.03f),
                new Vector3(-0.42f, -0.53f, 0f), new Vector3(0.2f, 0.22f, 1f), 8);
            _tail = MakePart("TailFlag", sprite, cheddar ? new Color(1f, 0.82f, 0.28f) : new Color(0.16f, 0.07f, 0.03f),
                new Vector3(-0.68f, 0.16f, 0f), new Vector3(0.16f, 0.58f, 1f), 9);
            _marker = MakePart("MoodSpark", sprite, cheddar ? new Color(1f, 0.95f, 0.25f) : new Color(0.95f, 0.82f, 0.55f),
                new Vector3(0.24f, 0.54f, -0.08f), new Vector3(0.18f, 0.18f, 1f), 19);
            _intentArrow = MakePart(cheddar ? "CheddarIntentArrow" : "CocoaIntentArrow", sprite,
                cheddar ? new Color(1f, 0.15f, 0.08f, 0.78f) : new Color(0.08f, 0.8f, 0.9f, 0.78f),
                new Vector3(0.9f, 0f, -0.09f), new Vector3(0.24f, 0.1f, 1f), 21);
            _intentArrow.enabled = false;

            if (!cheddar)
            {
                MakePart("CocoaQueenSpotA", sprite, new Color(0.08f, 0.035f, 0.02f),
                    new Vector3(-0.2f, 0.2f, -0.04f), new Vector3(0.26f, 0.22f, 1f), 13);
                MakePart("CocoaQueenSpotB", sprite, new Color(0.74f, 0.47f, 0.28f),
                    new Vector3(-0.48f, -0.04f, -0.04f), new Vector3(0.22f, 0.18f, 1f), 13);
                MakePart("CocoaQueenCrown", sprite, new Color(1f, 0.86f, 0.18f),
                    new Vector3(0.46f, 0.58f, -0.08f), new Vector3(0.34f, 0.12f, 1f), 20);
            }
            else
            {
                MakePart("CheddarChaosTuft", sprite, new Color(1f, 0.88f, 0.22f),
                    new Vector3(0.52f, 0.54f, -0.08f), new Vector3(0.14f, 0.24f, 1f), 20);
                MakePart("CheddarMischiefFlash", sprite, new Color(1f, 0.32f, 0.08f),
                    new Vector3(-0.18f, 0.25f, -0.04f), new Vector3(0.34f, 0.12f, 1f), 13);
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

        private string PoseCopy(Pose pose) => pose switch
        {
            Pose.Idle => _identity.Id == DogId.Cheddar ? "WIGGLE READY" : "QUEEN READY",
            Pose.Run => _identity.Id == DogId.Cheddar ? "CHAOS ZOOM" : "SPOT PATROL",
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
