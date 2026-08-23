using Game.Data.Enemy;
using UnityEngine;

namespace Game.Gameplay.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyVerticalIdleMovement : MonoBehaviour
    {
        [SerializeField] private EnemyIdleMovementSettingsSO _settings;

        private Rigidbody2D _rigidbody;
        private Transform _parent;
        private Vector3 _startLocalPosition;
        private Vector3 _startWorldPosition;
        private float _elapsedTime;

        private void Awake()
        {
            if (!TryGetComponent(out _rigidbody))
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _parent = transform.parent;
            _startLocalPosition = transform.localPosition;
            _startWorldPosition = transform.position;
            _elapsedTime = default;
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null || _settings == null)
            {
                return;
            }

            float cycleDuration = _settings.CycleDuration;
            _elapsedTime = Mathf.Repeat(_elapsedTime + Time.fixedDeltaTime, cycleDuration);

            float normalizedTime = _elapsedTime / cycleDuration;
            float verticalOffset = Mathf.Sin(normalizedTime * Mathf.PI * 2f)
                * _settings.VerticalDistance;
            Vector2 targetPosition = GetTargetWorldPosition(verticalOffset);

            _rigidbody.MovePosition(targetPosition);
        }

        private Vector2 GetTargetWorldPosition(float verticalOffset)
        {
            if (_parent == null)
            {
                return _startWorldPosition + Vector3.up * verticalOffset;
            }

            Vector3 targetLocalPosition = _startLocalPosition + Vector3.up * verticalOffset;
            return _parent.TransformPoint(targetLocalPosition);
        }
    }
}
