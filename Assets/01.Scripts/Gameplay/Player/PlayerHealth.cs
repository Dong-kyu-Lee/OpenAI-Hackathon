using Game.Core.Events;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerStatsSO _stats;
        [SerializeField] private IntEventChannelSO _healthChangedChannel;
        [SerializeField] private VoidEventChannelSO _playerDiedChannel;

        private float _passiveDrainElapsed;
        private float _invulnerableUntil;
        private int _currentHealth;

        public bool IsDead { get; private set; }
        public int CurrentHealth => _currentHealth;

        private void Awake()
        {
            _currentHealth = _stats.MaxHealth;
        }

        private void OnEnable()
        {
            RaiseHealthChanged();
        }

        private void Update()
        {
            if (IsDead || _stats.PassiveDrainInterval <= 0f)
            {
                return;
            }

            _passiveDrainElapsed += Time.deltaTime;
            while (_passiveDrainElapsed >= _stats.PassiveDrainInterval)
            {
                _passiveDrainElapsed -= _stats.PassiveDrainInterval;
                ApplyDamage(_stats.PassiveDrainAmount);

                if (IsDead)
                {
                    return;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryTakeObstacleDamage(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTakeObstacleDamage(other);
        }

        private void TryTakeObstacleDamage(Collider2D obstacle)
        {
            if (IsDead || Time.time < _invulnerableUntil || !IsObstacle(obstacle.gameObject.layer))
            {
                return;
            }

            int damage = Random.Range(
                _stats.ObstacleDamageMinimum,
                _stats.ObstacleDamageMaximum + 1);

            _invulnerableUntil = Time.time + _stats.ObstacleHitInvulnerabilityDuration;
            ApplyDamage(damage);
        }

        private void ApplyDamage(int damage)
        {
            if (damage <= 0 || IsDead)
            {
                return;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            RaiseHealthChanged();

            if (_currentHealth == 0)
            {
                IsDead = true;
                _playerDiedChannel?.Raise();
            }
        }

        private bool IsObstacle(int layer)
        {
            return (_stats.ObstacleLayers.value & (1 << layer)) != 0;
        }

        private void RaiseHealthChanged()
        {
            _healthChangedChannel?.Raise(_currentHealth);
        }
    }
}
