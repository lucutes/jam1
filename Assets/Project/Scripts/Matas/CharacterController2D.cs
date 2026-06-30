using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Scripts.Matas
{
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterController2D : MonoBehaviour
    {
        [FormerlySerializedAs("MoveSpeed")] [SerializeField]
        private float _moveSpeed = 5f;

        [FormerlySerializedAs("flipSpriteWhenMovingLeft")] [Header("Sprite")] [SerializeField]
        private bool _flipSpriteWhenMovingLeft = true;

        [FormerlySerializedAs("idleSprites")] [Header("Animation Sprites")] [SerializeField]
        private Sprite[] _idleSprites;

        [FormerlySerializedAs("walkingSprites")] [SerializeField]
        private Sprite[] _walkingSprites;

        [SerializeField] private float _animationSpeed = 0.15f;

        private int _animationFrame;
        private float _animationTimer;
        private Sprite[] _currentAnimation;
        private Vector3 _movement;
        private Rigidbody _rb;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _rb.freezeRotation = true;

            _currentAnimation = _idleSprites;
        }

        private void Update()
        {
            var x = Input.GetAxisRaw("Horizontal");
            var z = Input.GetAxisRaw("Vertical");

            _movement = new Vector3(x, 0f, z).normalized;

            UpdateSpriteFlip();
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            var newPosition = _rb.position + _movement * _moveSpeed * Time.fixedDeltaTime;

            newPosition.y = _rb.position.y;

            _rb.MovePosition(newPosition);
        }

        private void UpdateSpriteFlip()
        {
            if (!_flipSpriteWhenMovingLeft)
                return;

            if (_movement.x > 0)
                _spriteRenderer.flipX = false;
            else if (_movement.x < 0) _spriteRenderer.flipX = true;
        }

        private void UpdateAnimation()
        {
            var newAnimation = _movement.magnitude > 0
                ? _walkingSprites
                : _idleSprites;

            if (_currentAnimation != newAnimation)
            {
                _currentAnimation = newAnimation;
                _animationFrame = 0;
                _animationTimer = 0f;
            }

            if (_currentAnimation.Length == 0)
                return;

            _animationTimer += Time.deltaTime;

            if (_animationTimer >= _animationSpeed)
            {
                _animationTimer = 0f;

                _animationFrame++;

                if (_animationFrame >= _currentAnimation.Length)
                    _animationFrame = 0;

                _spriteRenderer.sprite = _currentAnimation[_animationFrame];
            }
        }
    }
}