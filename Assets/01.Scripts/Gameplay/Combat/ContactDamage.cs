using UnityEngine;

namespace Game.Gameplay.Combat
{
    /// <summary>접촉한 대상에게 적용할 고정 피해량을 제공합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField, Min(0)] private int _damage = 5;

        /// <summary>접촉 시 적용할 피해량을 가져옵니다.</summary>
        public int Damage => _damage;
    }
}