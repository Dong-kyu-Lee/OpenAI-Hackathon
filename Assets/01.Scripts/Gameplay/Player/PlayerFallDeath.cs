using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>플레이어가 설정된 월드 Y 경계 아래로 추락하면 즉시 사망시킵니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerFallDeath : MonoBehaviour
    {
        [SerializeField] private PlayerStatsSO _stats;

        private PlayerHealth _health;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        private void FixedUpdate()
        {
            if (_stats == null ||
                _health == null ||
                _health.IsDead ||
                transform.position.y > _stats.FallDeathY)
            {
                return;
            }

            _health.Kill();
        }
    }
}
