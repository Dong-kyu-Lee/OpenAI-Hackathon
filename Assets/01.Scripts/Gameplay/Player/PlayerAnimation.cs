using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Player
{
    [RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerSlide), typeof(PlayerHealth))]
    public sealed class PlayerAnimation : MonoBehaviour
    {
        private const string MotionStateParameterName = "MotionState";

        [SerializeField] private PlayerStatsSO _stats;

        private static readonly int MotionStateParameter = Animator.StringToHash(MotionStateParameterName);

        private Animator _animator;
        private Rigidbody2D _rigidbody;
        private PlayerMovement _playerMovement;
        private PlayerSlide _playerSlide;
        private PlayerHealth _playerHealth;
        private AnimationState _currentState;
        private bool _hasCurrentState;

        private enum AnimationState
        {
            RunGun = 0,
            JumpStart = 1,
            JumpStay = 2,
            JumpEnd = 3,
            Slide = 4,
            Dead = 5
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _playerMovement = GetComponent<PlayerMovement>();
            _playerSlide = GetComponent<PlayerSlide>();
            _playerHealth = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            AnimationState nextState = ResolveAnimationState();
            if (_hasCurrentState && nextState == _currentState)
            {
                return;
            }

            _currentState = nextState;
            _hasCurrentState = true;
            _animator.SetInteger(MotionStateParameter, (int)_currentState);
        }

        private AnimationState ResolveAnimationState()
        {
            if (_playerHealth.IsDead)
            {
                return AnimationState.Dead;
            }

            if (!_playerMovement.IsGrounded)
            {
                float apexThreshold = Mathf.Abs(_stats.JumpApexVelocityThreshold);
                float verticalSpeed = _rigidbody.linearVelocity.y;

                if (verticalSpeed > apexThreshold)
                {
                    return AnimationState.JumpStart;
                }

                if (verticalSpeed < -apexThreshold)
                {
                    return AnimationState.JumpEnd;
                }

                return AnimationState.JumpStay;
            }

            if (_playerSlide.IsSliding)
            {
                return AnimationState.Slide;
            }

            return AnimationState.RunGun;
        }
    }
}
