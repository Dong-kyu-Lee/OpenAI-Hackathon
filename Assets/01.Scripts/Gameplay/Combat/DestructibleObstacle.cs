using Game.Core.Combat;
using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>피해를 받아 내구도가 소진되면 시각 요소와 충돌을 비활성화하는 장애물입니다.</summary>
    [DisallowMultipleComponent]
    public sealed class DestructibleObstacle : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(0.01f)] private float _durability = 15f;

        private Collider2D[] _colliders;
        private Renderer[] _renderers;
        private float _currentDurability;

        /// <summary>현재 파괴되었는지 여부를 가져옵니다.</summary>
        public bool IsBroken { get; private set; }

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

        /// <summary>내구도에서 피해량을 차감하고 0 이하가 되면 장애물을 파괴 상태로 전환합니다.</summary>
        public void TakeDamage(float amount)
        {
            if (IsBroken || amount <= 0f)
            {
                return;
            }

            _currentDurability -= amount;
            if (_currentDurability > 0f)
            {
                return;
            }

            IsBroken = true;
            SetContentEnabled(false);
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