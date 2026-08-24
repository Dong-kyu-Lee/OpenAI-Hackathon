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
        private Vector3 _authoredLocalPosition;
        private Vector3 _authoredWorldPosition;
        private float _elapsedTime;

        private void Awake()
        {
            if (!TryGetComponent(out _rigidbody))
            {
                enabled = false;
                return;
            }

            _parent = transform.parent;
            _authoredLocalPosition = transform.localPosition;
            _authoredWorldPosition = transform.position;
        }

        private void OnEnable()
        {
            _elapsedTime = default;
            ResetPosition();
        }

        private void OnDisable()
        {
            ResetPosition();
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
                return _authoredWorldPosition + Vector3.up * verticalOffset;
            }

            Vector3 targetLocalPosition = _authoredLocalPosition + Vector3.up * verticalOffset;
            return _parent.TransformPoint(targetLocalPosition);
        }

        private void ResetPosition()
        {
            if (_rigidbody == null)
            {
                return;
            }

            Vector2 resetPosition = GetTargetWorldPosition(default);
            _rigidbody.position = resetPosition;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = default;
        }
    }
}
