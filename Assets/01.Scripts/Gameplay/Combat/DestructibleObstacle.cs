using System;
using Game.Core.Combat;
using Game.Data;
using Game.Gameplay.Tutorial;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class DestructibleObstacle : MonoBehaviour, IDamageable, IWeaponDamageable, ITutorialDestructionTarget
    {
        [SerializeField, Min(0.01f)] private float _durability = 15f;
        
        [SerializeField] private Game.Core.Events.SfxEventChannelSO _sfxChannel;
        [SerializeField] private AudioClip _destroyedClip;
[SerializeField] private WeaponDefinitionSO _requiredDamageWeapon;

        private Collider2D[] _colliders;
        private Renderer[] _renderers;
        private float _currentDurability;

        public bool IsBroken { get; private set; }
        public event Action<WeaponDefinitionSO> DestroyedByWeapon;

        private void Awake()
        {
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnEnable()
        {
            _currentDurability = _durability;
            IsBroken = false;
            SetContentEnabled(true);
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null);
        }

        public void TakeDamage(float amount, WeaponDefinitionSO sourceWeapon)
        {
            if (IsBroken || amount <= 0f)
            {
                return;
            }

            if (_requiredDamageWeapon != null && sourceWeapon != _requiredDamageWeapon)
            {
                return;
            }

            _currentDurability -= amount;
            if (_currentDurability > 0f)
            {
                return;
            }

            
            _sfxChannel?.PlayOneShot(_destroyedClip);
IsBroken = true;
            SetContentEnabled(false);
            DestroyedByWeapon?.Invoke(sourceWeapon);
        }

        private void SetContentEnabled(bool isEnabled)
        {
            if (_colliders != null)
            {
                for (int index = default; index < _colliders.Length; index++)
                {
                    if (_colliders[index] != null)
                    {
                        _colliders[index].enabled = isEnabled;
                    }
                }
            }

            if (_renderers == null)
            {
                return;
            }

            for (int index = default; index < _renderers.Length; index++)
            {
                if (_renderers[index] != null)
                {
                    _renderers[index].enabled = isEnabled;
                }
            }
        }
    }
}
