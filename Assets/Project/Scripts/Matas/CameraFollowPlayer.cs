using UnityEngine;

namespace Project.Scripts.Matas
{
    public class CameraFollowPlayer : MonoBehaviour
    {
        [Header("Target")] [SerializeField] private Transform _player;

        [Header("Camera Settings")] [SerializeField]
        private Vector3 _offset = new(0f, 3f, -6f);

        [SerializeField] private float _smoothTime = 0.15f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            var targetPosition = _player.position + _offset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _velocity,
                _smoothTime
            );
        }
    }
}