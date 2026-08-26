using Game.Core.Events;
using Game.Data;
using Game.Data.Stage;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerStatsSO _stats;
        [SerializeField] private IntEventChannelSO _healthChangedChannel;
        [SerializeField] private VoidEventChannelSO _playerDiedChannel;
        [SerializeField] private VoidEventChannelSO _playerHitChannel;
        [SerializeField] private StageSelectionStateSO _stageSelectionState;
        [SerializeField] private SfxEventChannelSO _sfxChannel;
        [SerializeField] private AudioClip _hitVoiceClip;
        [SerializeField] private AudioClip _bumpClip;
        [SerializeField] private AudioClip _deathClip;

        private float _passiveDrainElapsed;
        private float _invulnerableUntil;
        private int _currentHealth;
        private bool _isPassiveDrainEnabled;

        public bool IsDead { get; private set; }
        public int CurrentHealth => _currentHealth;

        public void Kill()
        {
            if (IsDead) return;
            _currentHealth = default;
            RaiseHealthChanged();
            IsDead = true;
            _sfxChannel?.PlayOneShot(_deathClip);
            _playerDiedChannel?.Raise();
        }

        private void Awake()
        {
            _currentHealth = _stats.MaxHealth;
            StageDefinitionSO stageDefinition = _stageSelectionState == null
                ? null
                : _stageSelectionState.CurrentStageDefinition;
            _isPassiveDrainEnabled = stageDefinition == null || !stageDefinition.IsEndlessMode;
        }

        private void OnEnable() { RaiseHealthChanged(); }

        private void Update()
        {
            if (IsDead || !_isPassiveDrainEnabled || _stats.PassiveDrainInterval <= 0f) return;

            _passiveDrainElapsed += Time.deltaTime;
            while (_passiveDrainElapsed >= _stats.PassiveDrainInterval)
            {
                _passiveDrainElapsed -= _stats.PassiveDrainInterval;
                ApplyDamage(_stats.PassiveDrainAmount);
                if (IsDead) return;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision) { TryTakeObstacleDamage(collision.collider); }
        private void OnTriggerEnter2D(Collider2D other) { TryTakeObstacleDamage(other); }

        private void TryTakeObstacleDamage(Collider2D obstacle)
        {
            if (IsDead || Time.time < _invulnerableUntil) return;

            int damage;
            if (obstacle.TryGetComponent(out ContactDamage contactDamage))
                damage = contactDamage.Damage;
            else
            {
                if (!IsObstacle(obstacle.gameObject.layer)) return;
                damage = Random.Range(_stats.ObstacleDamageMinimum, _stats.ObstacleDamageMaximum + 1);
            }

            _invulnerableUntil = Time.time + _stats.ObstacleHitInvulnerabilityDuration;
            if (!ApplyDamage(damage)) return;

            if (!IsDead)
            {
                _sfxChannel?.PlayOneShot(_bumpClip, 0.6f);
                _sfxChannel?.PlayOneShot(_hitVoiceClip);
            }

            _playerHitChannel?.Raise();
        }

        private bool ApplyDamage(int damage)
        {
            if (damage <= 0 || IsDead) return false;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            RaiseHealthChanged();

            if (_currentHealth == 0)
            {
                IsDead = true;
                _sfxChannel?.PlayOneShot(_deathClip);
                _playerDiedChannel?.Raise();
            }

            return true;
        }

        private bool IsObstacle(int layer)
        {
            return (_stats.ObstacleLayers.value & (1 << layer)) != 0;
        }

        private void RaiseHealthChanged() { _healthChangedChannel?.Raise(_currentHealth); }
    }
}
