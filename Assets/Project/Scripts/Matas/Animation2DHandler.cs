using UnityEngine;

namespace Project.Scripts.Matas
{
    public class Animation2DHandler : MonoBehaviour
    {
        [Header("Idle Animation Frames")] [SerializeField]
        private Sprite[] _idleFrames;

        [SerializeField] private float _frameRate = 2f;

        private int _currentFrame;

        private SpriteRenderer _spriteRenderer;
        private float _timer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            PlayIdleAnimation();
        }

        private void PlayIdleAnimation()
        {
            _timer += Time.deltaTime;

            var frameDuration = 1f / _frameRate;

            if (_timer >= frameDuration)
            {
                _timer -= frameDuration;

                _currentFrame = (_currentFrame + 1) % _idleFrames.Length;

                _spriteRenderer.sprite =
                    _idleFrames[_currentFrame];
            }
        }
    }
}