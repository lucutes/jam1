using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Matas
{
    public class UIAnimationHandler : MonoBehaviour
    {
        [Header("Idle Animation Frames")] [SerializeField]
        private Sprite[] _idleFrames;

        [SerializeField] private float _frameRate = 2f;

        private int _currentFrame;
        private float _timer;

        private Image _uiImage;

        private void Awake()
        {
            _uiImage = GetComponent<Image>();
        }

        private void Update()
        {
            PlayIdleAnimation();
        }

        private void PlayIdleAnimation()
        {
            if (_idleFrames == null ||
                _idleFrames.Length == 0 ||
                _uiImage == null)
                return;

            _timer += Time.deltaTime;

            var frameDuration = 1f / _frameRate;

            if (_timer >= frameDuration)
            {
                _timer -= frameDuration;

                _currentFrame = (_currentFrame + 1) % _idleFrames.Length;

                _uiImage.sprite =
                    _idleFrames[_currentFrame];
            }
        }
    }
}