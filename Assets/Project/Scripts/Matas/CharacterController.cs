using UnityEngine;

namespace Project.Scripts.Matas
{
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterController : MonoBehaviour
    {
        public float moveSpeed = 5f;

        [Header("Sprite")] public bool flipSpriteWhenMovingLeft = true;

        [Header("Animation Sprites")] public Sprite[] idleSprites;
        public Sprite[] walkingSprites;

        public float animationSpeed = 0.15f;

        private Rigidbody rb;
        private SpriteRenderer spriteRenderer;

        private Vector3 movement;

        private Sprite[] currentAnimation;
        private int animationFrame;
        private float animationTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            rb.freezeRotation = true;

            currentAnimation = idleSprites;
        }

        private void Update()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            movement = new Vector3(x, 0f, z).normalized;

            UpdateSpriteFlip();
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            Vector3 newPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;

            newPosition.y = rb.position.y;

            rb.MovePosition(newPosition);
        }

        private void UpdateSpriteFlip()
        {
            if (!flipSpriteWhenMovingLeft)
                return;

            if (movement.x > 0)
            {
                spriteRenderer.flipX = false;
            }
            else if (movement.x < 0)
            {
                spriteRenderer.flipX = true;
            }
        }

        private void UpdateAnimation()
        {
            Sprite[] newAnimation = movement.magnitude > 0
                ? walkingSprites
                : idleSprites;

            if (currentAnimation != newAnimation)
            {
                currentAnimation = newAnimation;
                animationFrame = 0;
                animationTimer = 0f;
            }

            if (currentAnimation.Length == 0)
                return;

            animationTimer += Time.deltaTime;

            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0f;

                animationFrame++;

                if (animationFrame >= currentAnimation.Length)
                    animationFrame = 0;

                spriteRenderer.sprite = currentAnimation[animationFrame];
            }
        }
    }
}