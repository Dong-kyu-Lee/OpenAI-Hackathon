using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const float JumpVelocityCoefficient = 2f;
        private const float MinimumGravityMagnitude = 0.0001f;
        private const int GroundHitCapacity = 8;

        [SerializeField] private PlayerStatsSO _stats;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private PlayerSlide _playerSlide;
        [SerializeField] private PlayerHealth _playerHealth;

        private Rigidbody2D _rigidbody;
        private ContactFilter2D _groundContactFilter;
        private readonly Collider2D[] _groundHits = new Collider2D[GroundHitCapacity];
        private bool _jumpRequested;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            if (_playerSlide == null)
            {
                _playerSlide = GetComponent<PlayerSlide>();
            }

            if (_playerHealth == null)
            {
                _playerHealth = GetComponent<PlayerHealth>();
            }

            _rigidbody.gravityScale = _stats.GravityScale;
            _rigidbody.constraints |= RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

            _groundContactFilter = new ContactFilter2D();
            _groundContactFilter.SetLayerMask(_stats.GroundLayers);
            _groundContactFilter.useTriggers = Physics2D.queriesHitTriggers;
        }

        private void FixedUpdate()
        {
            UpdateGroundedState();
            KeepPlayerAnchoredHorizontally();

            if (_playerHealth.IsDead)
            {
                _jumpRequested = false;
                return;
            }

            TryJump();
        }

        public void RequestJump()
        {
            _jumpRequested = true;
        }

        private void UpdateGroundedState()
        {
            int hitCount = Physics2D.OverlapBox(
                _groundCheck.position,
                _stats.GroundCheckSize,
                0f,
                _groundContactFilter,
                _groundHits);

            IsGrounded = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D groundHit = _groundHits[i];
                if (groundHit == null || groundHit.attachedRigidbody == _rigidbody)
                {
                    continue;
                }

                IsGrounded = true;
                return;
            }
        }

        private void KeepPlayerAnchoredHorizontally()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            velocity.x = 0f;
            _rigidbody.linearVelocity = velocity;
        }

        private void TryJump()
        {
            if (!_jumpRequested)
            {
                return;
            }

            _jumpRequested = false;

            if (!IsGrounded || _playerSlide.IsSliding)
            {
                return;
            }

            float gravityMagnitude = Mathf.Abs(Physics2D.gravity.y * _rigidbody.gravityScale);
            if (gravityMagnitude < MinimumGravityMagnitude)
            {
                return;
            }

            Vector2 velocity = _rigidbody.linearVelocity;
            velocity.y = Mathf.Sqrt(JumpVelocityCoefficient * gravityMagnitude * _stats.JumpClearHeight);
            _rigidbody.linearVelocity = velocity;
        }
    }
}
