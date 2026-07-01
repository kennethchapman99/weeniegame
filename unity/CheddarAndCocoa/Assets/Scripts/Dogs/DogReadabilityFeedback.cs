using UnityEngine;
using CheddarAndCocoa.Game;

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
        private static readonly Vector3 AuthoredFallbackScale = new Vector3(0.8f, 1.65f, 1f);
        private static readonly Vector3 AuthoredMotionScale = new Vector3(1.12f, 2.3f, 1f);
        private static bool _debugIdentityLabelsVisible;

        public enum Pose
        {
            Idle,
            Run,
            Bark,
            Tug,
            Carry,
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
        private SpriteRenderer _authoredPose;
        private TextMesh _label;
        private MeshRenderer _labelRenderer;
        private Vector3 _baseScale;
        private Vector3 _labelBaseScale;
        private Vector2 _lastIntentDir = Vector2.right;
        private Pose _forcedPose;
        private float _forcedPoseUntil;
        private float _barkUntil;
        private float _barkStartedAt;
        private DogVisualSlot _art;
        private DogActionFeedback _actionFeedback;
        private DogProceduralAudio _proceduralAudio;
        private DogShowcasePolish _showcasePolish;

        public Pose CurrentPose { get; private set; } = Pose.Idle;
        public string CurrentPoseLabel => CurrentPose.ToString();
        public string IdentityLabel => _label != null ? _label.text : string.Empty;
        public bool IdentityTextVisible => _labelRenderer != null && _labelRenderer.enabled;
        public string FacingIntentLabel => _lastIntentDir.x >= 0f ? "FacingRight" : "FacingLeft";
        public string DetailedFacingIntentLabel => $"Facing{CharacterMotionArt.FacingLabel(_lastIntentDir)}";
        public string LastMovementJuiceLabel { get; private set; } = string.Empty;
        public float StrategicLabelScale { get; private set; } = 1f;
        public bool UsesAuthoredPoseArt => _authoredPose != null && _authoredPose.sprite != null;
        public string AuthoredPoseSpriteName => UsesAuthoredPoseArt ? _authoredPose.sprite.name : string.Empty;
        public int MotionFrameIndex { get; private set; } = -1;
        public string MotionClipLabel { get; private set; } = string.Empty;
        public bool IsCarrying { get; private set; }
        public string MotionPersonalityLabel { get; private set; } = string.Empty;
        public DogActionFeedback ActionFeedback => _actionFeedback;
        public DogProceduralAudio ProceduralAudio => _proceduralAudio;
        public bool HasShowcasePersonalityPolish => _showcasePolish != null && _showcasePolish.Built;
        public string ShowcasePersonalitySignature => _showcasePolish != null ? _showcasePolish.PersonalitySignature : string.Empty;
        public string ArtDirectionSignature => _identity == null
            ? string.Empty
            : ArenaArtCatalog.Dog(_identity.Id).ArtDirectionSignature;

        public static void SetDebugIdentityLabelsVisible(bool visible)
        {
            _debugIdentityLabelsVisible = visible;
            foreach (var feedback in Object.FindObjectsByType<DogReadabilityFeedback>(FindObjectsSortMode.None))
            {
                feedback.ApplyLabelVisibility();
            }
        }

        public void Init(Sprite sprite)
        {
            _dog = GetComponent<DogController>();
            _identity = GetComponent<DogIdentity>();
            _body = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            _art = ArenaArtCatalog.Dog(_identity.Id);

            BuildIdentityArt(sprite);
            BuildAuthoredPoseArt();
            _actionFeedback = GetComponent<DogActionFeedback>();
            if (_actionFeedback == null) _actionFeedback = gameObject.AddComponent<DogActionFeedback>();
            _actionFeedback.Initialize(_authoredPose != null ? _authoredPose.transform : transform, sprite);
            _proceduralAudio = GetComponent<DogProceduralAudio>();
            if (_proceduralAudio == null) _proceduralAudio = gameObject.AddComponent<DogProceduralAudio>();
            _proceduralAudio.Initialize(_actionFeedback);
            _showcasePolish = GetComponent<DogShowcasePolish>();
            if (_showcasePolish == null) _showcasePolish = gameObject.AddComponent<DogShowcasePolish>();
            _showcasePolish.Begin();
            _dog.OnBark += OnBark;
            ApplyPose(Pose.Idle);
        }

        public void ShowTug() => ForcePose(Pose.Tug, 0.25f);
        /// <summary>
        /// Tug while facing <paramref name="faceDir"/> so two dogs flanking a rope visibly lean into it
        /// from opposite sides (a readable tug-of-war silhouette) instead of holding stale travel facing.
        /// </summary>
        public void ShowTug(Vector2 faceDir)
        {
            if (faceDir.sqrMagnitude > 0.0001f) _lastIntentDir = faceDir.normalized;
            ForcePose(Pose.Tug, 0.25f);
        }
        public void ShowRescued()
        {
            _actionFeedback?.Trigger(DogFeedbackAction.Rescue);
            ForcePose(Pose.Rescued, 1.1f);
        }
        public void ShowProudBrief() => ForcePose(Pose.Proud, 1.1f);
        public void ShowProud() => ForcePose(Pose.Proud, 999f);
        public void ShowSad() => ForcePose(Pose.Sad, 999f);
        public void ShowPanic() => ForcePose(Pose.Sad, 0.6f);     // brief flinch (e.g. a thunderclap)
        public void ShowComfort() => ForcePose(Pose.Proud, 0.5f); // brief reassurance while huddling
        public void SetCarrying(bool carrying)
        {
            IsCarrying = carrying;
            if (!carrying && CurrentPose == Pose.Carry) ApplyPose(Pose.Idle);
        }
        public void ClearMissionPose()
        {
            _forcedPoseUntil = 0f;
            if (_dog != null && _dog.Mode == MovementMode.Free) ApplyPose(Pose.Idle);
        }

        private void OnDestroy()
        {
            if (_dog != null) _dog.OnBark -= OnBark;
        }

        private void OnBark(DogId _)
        {
            _barkStartedAt = Time.time;
            _barkUntil = Time.time + 0.35f;
            _actionFeedback?.Trigger(DogFeedbackAction.Bark);
        }

        private void ForcePose(Pose pose, float seconds)
        {
            _forcedPose = pose;
            _forcedPoseUntil = Time.time + seconds;
            ApplyPose(pose);
        }

        private void Update()
        {
            if (_actionFeedback != null)
            {
                _actionFeedback.SetSustained(DogFeedbackAction.Tug, _dog.Mode == MovementMode.Tug);
                _actionFeedback.SetSustained(DogFeedbackAction.Carry, IsCarrying);
                _actionFeedback.SetSustained(DogFeedbackAction.Zoomies, _dog.Zoomies);
                _actionFeedback.Tick(Time.deltaTime);
            }
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
            if (IsCarrying) return Pose.Carry;

            if (TryGetComponent<Rigidbody2D>(out var rb) && rb.linearVelocity.sqrMagnitude > 0.05f)
                return Pose.Run;

            return Pose.Idle;
        }

        private void ApplyPose(Pose pose)
        {
            string expectedLabel = $"{DogTitle()}\n{PoseCopy(pose)}";
            if (CurrentPose == pose && _label != null && _label.text == expectedLabel) return;
            CurrentPose = pose;

            if (_authoredPose != null)
            {
                _authoredPose.sprite = ArenaDogPoseSprites.For(_identity.Id, pose);
                _authoredPose.transform.localScale = AuthoredFallbackScale;
            }

            if (_label != null)
            {
                _label.text = expectedLabel;
                _label.color = pose switch
                {
                    Pose.Stunned => new Color(1f, 0.45f, 0.45f),
                    Pose.Rescued => new Color(0.55f, 1f, 0.7f),
                    Pose.Proud => new Color(1f, 0.96f, 0.35f),
                    Pose.Sad => new Color(0.72f, 0.82f, 1f),
                    Pose.Tug => new Color(1f, 0.85f, 0.35f),
                    Pose.Carry => new Color(1f, 0.78f, 0.3f),
                    _ => Color.white
                };
            }

            if (_marker != null) _marker.enabled = _authoredPose == null &&
                (pose == Pose.Bark || pose == Pose.Proud || pose == Pose.Rescued || pose == Pose.Stunned);
        }

        private void AnimatePose(Pose pose)
        {
            float t = Time.time;
            float wag = _identity.Id == DogId.Cheddar ? 42f : 20f;
            Vector2 velocity = Vector2.zero;
            if (TryGetComponent<Rigidbody2D>(out var rb)) velocity = rb.linearVelocity;
            float runFeedbackSpeed = _identity != null && _identity.Tuning != null
                ? _identity.Tuning.runFeedbackSpeed
                : 0.22f;
            if (velocity.magnitude > runFeedbackSpeed) _lastIntentDir = velocity.normalized;
            if (_authoredPose != null) _authoredPose.flipX = _lastIntentDir.x < 0f;
            AnimateAuthoredMotion(pose);

            float speed01 = Mathf.Clamp01(velocity.magnitude / 6f);
            var personality = DogMotionPersonality.At(_identity.Id, pose, t, speed01, _dog.Zoomies);
            MotionPersonalityLabel = personality.Signature;
            ApplyPersonalityMotion(personality);

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
                bool moving = velocity.magnitude > runFeedbackSpeed && pose == Pose.Run;
                _intentArrow.enabled = moving;
                if (moving)
                {
                    _intentArrow.transform.localPosition = new Vector3(_lastIntentDir.x * 0.9f, _lastIntentDir.y * 0.45f, -0.09f);
                    _intentArrow.transform.localRotation = Quaternion.Euler(0f, 0f,
                        Mathf.Atan2(_lastIntentDir.y, _lastIntentDir.x) * Mathf.Rad2Deg);
                }
            }

            if (_label != null)
            {
                _label.transform.rotation = Quaternion.identity;
                StrategicLabelScale = Camera.main != null
                    ? Mathf.Clamp(Camera.main.orthographicSize / 7.5f, 1f, 4f)
                    : 1f;
                _label.transform.localScale = _labelBaseScale * StrategicLabelScale;
            }
            TickMovementJuice(velocity, runFeedbackSpeed);
        }

        private void AnimateAuthoredMotion(Pose pose)
        {
            if (_authoredPose == null || !CharacterMotionArt.TryClip(pose, out var clip))
            {
                MotionFrameIndex = -1;
                MotionClipLabel = string.Empty;
                return;
            }

            float elapsed = clip == CharacterMotionArt.Clip.Bark ? Time.time - _barkStartedAt : Time.time;
            int frame = CharacterMotionArt.FrameAtTime(_identity.Id, clip, elapsed);
            CharacterMotionArt.Facing8 facing = CharacterMotionArt.FacingForDirection(_lastIntentDir, out bool mirror);
            Sprite sprite = CharacterMotionArt.Load(_identity.Id, clip, facing, frame);
            if (sprite == null && facing != CharacterMotionArt.Facing8.E)
            {
                mirror = _lastIntentDir.x < 0f;
                sprite = CharacterMotionArt.Load(_identity.Id, clip, CharacterMotionArt.Facing8.E, frame);
            }
            if (sprite == null)
            {
                MotionFrameIndex = -1;
                MotionClipLabel = string.Empty;
                return;
            }

            _authoredPose.sprite = sprite;
            _authoredPose.transform.localScale = AuthoredMotionScale;
            _authoredPose.flipX = mirror;
            MotionFrameIndex = frame;
            MotionClipLabel = clip.ToString();
        }

        private void ApplyPersonalityMotion(DogMotionPersonality.Sample personality)
        {
            // Keep the Rigidbody/collider root stable. All squash, lean, and bounce belongs to art.
            transform.localRotation = Quaternion.identity;
            transform.localScale = _baseScale;
            if (_authoredPose == null) return;

            Vector3 authoredBase = MotionFrameIndex >= 0 ? AuthoredMotionScale : AuthoredFallbackScale;
            float barkPulse = _dog != null ? _dog.BarkVisualPulse : 1f;
            Vector2 actionScale = _actionFeedback != null ? _actionFeedback.VisualScale : Vector2.one;
            Vector2 actionOffset = _actionFeedback != null ? _actionFeedback.VisualOffset : Vector2.zero;
            float actionRotation = _actionFeedback != null ? _actionFeedback.VisualRotationDegrees : 0f;
            _authoredPose.transform.localScale = new Vector3(
                authoredBase.x * personality.Scale.x * barkPulse * actionScale.x,
                authoredBase.y * personality.Scale.y * barkPulse * actionScale.y,
                authoredBase.z);
            _authoredPose.transform.localRotation = Quaternion.Euler(0f, 0f,
                personality.RotationDegrees + actionRotation);
            _authoredPose.transform.localPosition = new Vector3(actionOffset.x,
                -0.12f + personality.VerticalOffset + actionOffset.y, -0.2f);
        }

        private void TickMovementJuice(Vector2 velocity, float runFeedbackSpeed)
        {
            if (_actionFeedback == null || velocity.magnitude <= runFeedbackSpeed) return;
            int before = _actionFeedback.TotalTrailsEmitted;
            _actionFeedback.TrackMotion(velocity, Time.deltaTime);
            if (_actionFeedback.TotalTrailsEmitted > before)
                LastMovementJuiceLabel = _actionFeedback.CurrentSignature;
        }

        private void BuildIdentityArt(Sprite sprite)
        {
            if (_body != null) _body.color = _art.BodyColor;

            foreach (var part in _art.Parts)
            {
                AssignPartReference(part.Name, MakePart(part, sprite));
            }

            var draftId = _identity.Id == DogId.Cheddar
                ? ArenaDraftArt.SpriteId.CheddarPortrait
                : ArenaDraftArt.SpriteId.CocoaPortrait;
            string badgeName = _identity.Id == DogId.Cheddar
                ? ArenaDraftArt.CheddarPortraitBadgeName
                : ArenaDraftArt.CocoaPortraitBadgeName;
            ArenaDraftArt.AddSpriteBadge(transform, badgeName, draftId,
                new Vector3(-0.32f, -0.08f, 0.03f),
                new Vector3(0.065f, 0.065f, 1f),
                7,
                new Color(1f, 1f, 1f, 0.9f));

            if (_intentArrow != null) _intentArrow.enabled = false;

            var labelSlot = ArenaArtCatalog.DogLabel;
            var labelGo = new GameObject(labelSlot.Name);
            labelGo.transform.SetParent(transform);
            labelGo.transform.localPosition = labelSlot.LocalPosition;
            labelGo.transform.localScale = labelSlot.LocalScale;
            _labelBaseScale = labelSlot.LocalScale;
            _label = labelGo.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = labelSlot.FontSize;
            _label.color = labelSlot.Color;
            _labelRenderer = _label.GetComponent<MeshRenderer>();
            ApplyLabelVisibility();
        }

        private void BuildAuthoredPoseArt()
        {
            Sprite idle = ArenaDogPoseSprites.For(_identity.Id, Pose.Idle);
            if (idle == null) return;

            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                renderer.enabled = false;

            var go = new GameObject($"{_identity.Id}AuthoredPose");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, -0.12f, -0.2f);
            go.transform.localScale = AuthoredFallbackScale;
            _authoredPose = go.AddComponent<SpriteRenderer>();
            _authoredPose.sprite = idle;
            _authoredPose.sortingOrder = 30;
        }

        private SpriteRenderer MakePart(PartSlot part, Sprite sprite)
        {
            var go = new GameObject(part.Name);
            go.transform.SetParent(transform);
            go.transform.localPosition = part.LocalPosition;
            go.transform.localScale = part.LocalScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = part.Color;
            sr.sortingOrder = part.SortingOrder;
            return sr;
        }

        private void AssignPartReference(string name, SpriteRenderer part)
        {
            switch (name)
            {
                case "DachshundHead": _head = part; break;
                case "LongDogSnout": _snout = part; break;
                case "CheddarFloppyEar":
                case "CocoaVelvetEar": _ear = part; break;
                case "CheddarRedCollar":
                case "CocoaTealCollar": _collar = part; break;
                case "ExpressionEye": _eye = part; break;
                case "ChestPatch": _chest = part; break;
                case "TinyFrontFeet": _frontFeet = part; break;
                case "TinyBackFeet": _backFeet = part; break;
                case "TailFlag": _tail = part; break;
                case "MoodSpark": _marker = part; break;
                case "CheddarIntentArrow":
                case "CocoaIntentArrow": _intentArrow = part; break;
            }
        }

        private string DogTitle() => _art.Title;

        private void ApplyLabelVisibility()
        {
            if (_labelRenderer != null) _labelRenderer.enabled = _debugIdentityLabelsVisible;
        }

        private string PoseCopy(Pose pose) => pose switch
        {
            Pose.Idle => _dog != null && _dog.TravelAssist ? "TRAIL READY" : _art.IdlePoseLabel,
            Pose.Run => _dog != null && _dog.TravelAssist ? "TRAIL SPRINT" : _art.RunPoseLabel,
            Pose.Bark => "WOOF!",
            Pose.Tug => "TUG!",
            Pose.Carry => "CARRY!",
            Pose.Stunned => "STUNNED",
            Pose.Rescued => "RESCUED!",
            Pose.Proud => "PROUD!",
            Pose.Sad => "SAD FLOP",
            _ => pose.ToString()
        };

    }
}
