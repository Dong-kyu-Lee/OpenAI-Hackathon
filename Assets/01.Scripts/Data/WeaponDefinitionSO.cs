using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Data/Weapon Definition", fileName = "WeaponDefinition")]
    public sealed class WeaponDefinitionSO : ScriptableObject
    {
        public enum FireMode
        {
            Projectile,
            ContinuousRay
        }

        public enum ImpactMode
        {
            Direct,
            Explosion
        }

        public enum Element
        {
            None,
            Ice,
            Fire
        }

        [Header("Identity")]
        [Tooltip("무기의 발사 방식입니다. Projectile은 투사체를 발사하고, ContinuousRay는 레이캐스트 공격을 지속합니다.")]
        [SerializeField] private FireMode _fireMode;
        [Tooltip("투사체가 충돌했을 때의 판정 방식입니다. Direct는 맞은 대상 하나, Explosion은 폭발 범위 안의 대상들에게 피해를 줍니다.")]
        [SerializeField] private ImpactMode _impactMode;
        [Tooltip("무기의 속성입니다. Ice는 IFreezable, Fire는 IBurnable 인터페이스를 추가로 호출합니다.")]
        [SerializeField] private Element _element;

        [Header("Damage and Timing")]
        [Tooltip("공격 피해량입니다. 투사체 무기는 발당 피해, 지속 레이 무기는 초당 피해량(DPS)으로 사용합니다.")]
        [SerializeField] private float _damage = 1f;
        [Tooltip("공격이 끝난 뒤 다음 공격을 시작할 수 있을 때까지의 대기 시간(초)입니다. 무기를 전환해도 남은 시간은 유지됩니다.")]
        [SerializeField] private float _cooldown = 1f;
        [Tooltip("한 번의 점사에서 발사하는 투사체 수입니다. 단발 무기는 1로 설정합니다.")]
        [SerializeField] private int _burstCount = 1;
        [Tooltip("한 번의 점사 안에서 각 투사체를 발사하는 간격(초)입니다.")]
        [SerializeField] private float _burstInterval = 0.1f;
        [Tooltip("ContinuousRay 공격을 한 번에 유지할 수 있는 최대 시간(초)입니다. 투사체 무기에는 사용하지 않습니다.")]
        [SerializeField] private float _maxContinuousDuration = 3f;

        [Header("Range and Projectile")]
        [Tooltip("ContinuousRay 레이캐스트의 최대 사거리입니다. 투사체 무기에는 사용하지 않습니다.")]
        [SerializeField] private float _range = 20f;
        [Tooltip("투사체가 초당 이동하는 월드 거리입니다. ContinuousRay 무기에는 사용하지 않습니다.")]
        [SerializeField] private float _projectileSpeed = 25f;
        [Tooltip("충돌하지 않은 투사체가 자동으로 풀에 반환되기까지의 시간(초)입니다.")]
        [SerializeField] private float _projectileLifetime = 3f;
        [Tooltip("Explosion 판정의 피해 반경입니다. Direct 판정에는 사용하지 않습니다.")]
        [SerializeField] private float _explosionRadius = 2f;
        [Tooltip("투사체와 레이가 충돌 대상으로 인식할 레이어입니다. 장애물, 바닥, 적 레이어를 포함해야 합니다.")]
        [SerializeField] private LayerMask _hitLayers = ~0;

        public FireMode Mode => _fireMode;
        public ImpactMode Impact => _impactMode;
        public Element DamageElement => _element;
        public float Damage => _damage;
        public float Cooldown => _cooldown;
        public int BurstCount => _burstCount;
        public float BurstInterval => _burstInterval;
        public float MaxContinuousDuration => _maxContinuousDuration;
        public float Range => _range;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public float ExplosionRadius => _explosionRadius;
        public LayerMask HitLayers => _hitLayers;
    }
}
