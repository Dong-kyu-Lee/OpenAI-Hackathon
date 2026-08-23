using System.Collections;
using Game.Core.Combat;
using Game.Data.Enemy;
using UnityEngine;

namespace Game.Gameplay.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class EnemyObstacleDestruction : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyObstacleStatsSO _stats;
        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _destructionClip;

        private Collider2D[] _colliders;
        private EnemyVerticalIdleMovement _idleMovement;
        private SpriteRenderer _spriteRenderer;
        private Coroutine _destructionRoutine;
        private Sprite _idleSprite;
        private float _currentDurability;
        private bool _isDestroyed;

        private void Awake()
        {
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _idleMovement = GetComponent<EnemyVerticalIdleMovement>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _idleSprite = _spriteRenderer.sprite;

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
        }

        private void OnEnable()
        {
            ResetState();
        }

        private void OnDisable()
        {
            StopDestructionRoutine();
        }

        public void TakeDamage(float amount)
        {
            if (_isDestroyed || amount <= 0f || _stats == null)
            {
                return;
            }

            _currentDurability -= amount;

            if (_currentDurability <= 0f)
            {
                BeginDestruction();
            }
        }

        private void BeginDestruction()
        {
            _isDestroyed = true;
            SetCollidersEnabled(false);

            if (_idleMovement != null)
            {
                _idleMovement.enabled = false;
            }

            if (_animator == null || _destructionClip == null)
            {
                _spriteRenderer.enabled = false;
                return;
            }

            _animator.enabled = true;
            int stateHash = Animator.StringToHash(_destructionClip.name);
            _animator.Play(stateHash, default, default);
            _destructionRoutine = StartCoroutine(CompleteDestruction());
        }

        private IEnumerator CompleteDestruction()
        {
            yield return new WaitForSeconds(_destructionClip.length);

            _animator.enabled = false;
            _spriteRenderer.enabled = false;
            _destructionRoutine = null;
        }

        private void ResetState()
        {
            StopDestructionRoutine();

            _currentDurability = _stats != null ? _stats.Durability : default;
            _isDestroyed = false;

            if (_animator != null)
            {
                _animator.enabled = false;
            }

            _spriteRenderer.sprite = _idleSprite;
            _spriteRenderer.enabled = true;
            SetCollidersEnabled(true);

            if (_idleMovement != null)
            {
                _idleMovement.enabled = true;
            }
        }

        private void SetCollidersEnabled(bool isEnabled)
        {
            for (int index = default; index < _colliders.Length; index++)
            {
                if (_colliders[index] != null)
                {
                    _colliders[index].enabled = isEnabled;
                }
            }
        }

        private void StopDestructionRoutine()
        {
            if (_destructionRoutine == null)
            {
                return;
            }

            StopCoroutine(_destructionRoutine);
            _destructionRoutine = null;
        }
    }
}
