using CheddarAndCocoa.Game;
using UnityEngine;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>
    /// Short mission-authored mutual-grooming action. Tick Invasion owns when it plays; the dog
    /// readability component owns the shared character renderer that displays it.
    /// </summary>
    [RequireComponent(typeof(DogIdentity))]
    [RequireComponent(typeof(DogReadabilityFeedback))]
    public sealed class DogGroomAnimation : MonoBehaviour
    {
        private const float Duration = 0.68f;

        private readonly Sprite[] _frames = new Sprite[2];
        private DogId _dog;
        private DogReadabilityFeedback _feedback;
        private float _startedAt;
        private bool _mirror;

        public bool IsPlaying { get; private set; }
        public bool HasAuthoredFrames => _frames[0] != null && _frames[1] != null;
        public int CurrentFrameIndex { get; private set; } = -1;
        public string CurrentSpriteName =>
            CurrentFrameIndex >= 0 && CurrentFrameIndex < _frames.Length && _frames[CurrentFrameIndex] != null
                ? _frames[CurrentFrameIndex].name
                : string.Empty;

        public void Init()
        {
            _dog = GetComponent<DogIdentity>().Id;
            _feedback = GetComponent<DogReadabilityFeedback>();
            for (int i = 0; i < _frames.Length; i++)
                _frames[i] = CharacterMotionArt.Load(_dog, CharacterMotionArt.Clip.Groom,
                    CharacterMotionArt.Facing8.E, i);
            Hide();
        }

        public void Play(Transform partner)
        {
            if (!HasAuthoredFrames || _feedback == null) return;
            Vector2 towardPartner = partner != null
                ? (Vector2)(partner.position - transform.position)
                : Vector2.right;
            _mirror = towardPartner.x < 0f;
            _startedAt = Time.time;
            IsPlaying = true;
            ShowFrame(0);
        }

        public void Hide()
        {
            IsPlaying = false;
            CurrentFrameIndex = -1;
            _feedback?.ClearMissionArtOverride();
        }

        /// <summary>Deterministic visual seam used by art integration tests and capture tooling.</summary>
        public void ShowFrameForTests(int frame)
        {
            if (!HasAuthoredFrames || _feedback == null) return;
            IsPlaying = true;
            ShowFrame(Mathf.Clamp(frame, 0, _frames.Length - 1));
        }

        private void Update()
        {
            if (!IsPlaying) return;
            float elapsed = Time.time - _startedAt;
            if (elapsed >= Duration)
            {
                Hide();
                return;
            }
            ShowFrame(CharacterMotionArt.FrameAtTime(_dog, CharacterMotionArt.Clip.Groom, elapsed));
        }

        private void ShowFrame(int frame)
        {
            CurrentFrameIndex = Mathf.Clamp(frame, 0, _frames.Length - 1);
            _feedback.SetMissionArtOverride(_frames[CurrentFrameIndex], _mirror,
                CurrentFrameIndex, CharacterMotionArt.Clip.Groom.ToString());
        }
    }
}
