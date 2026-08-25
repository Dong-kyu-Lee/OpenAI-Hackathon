using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Pooling;
using Game.Data;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PooledProjectile : MonoBehaviour, IPoolable
    {
        private const int ExplosionHitCapacity = 32;

        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private PooledHitEffect _hitEffectPrefab;
        [SerializeField] private SfxEventChannelSO _sfxChannel;
        [SerializeField] private AudioClip _flightClip;
        [SerializeField, Range(0f, 1f)] private float _flightVolume = 1f;

        private readonly Collider2D[] _explosionHits = new Collider2D[ExplosionHitCapacity];
        private readonly HashSet<IDamageable> _damagedTargets = new();

        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private WeaponDefinitionSO _definition;
        private float _remainingLifetime;
        private bool _isLaunched;
        private bool _isResolvingImpact;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            if (_trailRenderer == null) _trailRenderer = GetComponent<TrailRenderer>();
        }

        private void OnEnable() { ResetState(); }

        private void FixedUpdate()
        {
            if (!_isLaunched) return;
            _remainingLifetime -= Time.fixedDeltaTime;
            if (_remainingLifetime <= 0f) ReturnToPool();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isLaunched || collision.collider == null || !IsInHitLayers(collision.gameObject.layer)) return;

            bool hasContact = collision.contactCount > 0;
            Vector2 impactPoint = hasContact ? collision.GetContact(0).point : transform.position;
            Vector2 impactNormal = hasContact ? collision.GetContact(0).normal : GetTravelBasedNormal();
            ResolveImpact(collision.collider, impactPoint, impactNormal);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isLaunched || other == null || !IsInHitLayers(other.gameObject.layer)) return;
            ResolveImpact(other, other.ClosestPoint(transform.position), GetTravelBasedNormal());
        }

        public void Launch(WeaponDefinitionSO definition, Vector2 direction)
        {
            _definition = definition;
            _remainingLifetime = Mathf.Max(0f, definition.ProjectileLifetime);
            _isLaunched = true;
            _isResolvingImpact = false;
            _collider.enabled = true;
            _rigidbody.linearVelocity = direction.normalized * definition.ProjectileSpeed;
            _sfxChannel?.StartLoop(GetInstanceID(), _flightClip, _flightVolume);
        }

        public void OnDespawned() { ResetState(); }

        private void ResetState()
        {
            _sfxChannel?.StopLoop(GetInstanceID());
            _definition = null;
            _remainingLifetime = 0f;
            _isLaunched = false;
            _isResolvingImpact = false;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }
            _trailRenderer?.Clear();
        }

        private void ResolveImpact(Collider2D directHit, Vector2 impactPoint, Vector2 impactNormal)
        {
            if (_isResolvingImpact || _definition == null) return;

            _isResolvingImpact = true;
            _sfxChannel?.StopLoop(GetInstanceID());
            _collider.enabled = false;
            _rigidbody.linearVelocity = Vector2.zero;

            if (_definition.Impact == WeaponDefinitionSO.ImpactMode.Explosion) ApplyExplosionDamage(impactPoint);
            else ApplyDirectDamage(directHit);

            SpawnHitEffect(impactPoint, impactNormal);
            ReturnToPool();
        }

        private void ApplyDirectDamage(Collider2D directHit)
        {
            IWeaponDamageable weaponDamageable = directHit.GetComponentInParent<IWeaponDamageable>();
            if (weaponDamageable != null)
            {
                weaponDamageable.TakeDamage(_definition.Damage, _definition);
                return;
            }

            directHit.GetComponentInParent<IDamageable>()?.TakeDamage(_definition.Damage);
        }

        private void ApplyExplosionDamage(Vector2 impactPoint)
        {
            _damagedTargets.Clear();
            int hitCount = Physics2D.OverlapCircleNonAlloc(impactPoint, _definition.ExplosionRadius, _explosionHits, _definition.HitLayers);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _explosionHits[i];
                if (hit == null) continue;
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !_damagedTargets.Add(damageable)) continue;
                if (damageable is IWeaponDamageable weaponDamageable) weaponDamageable.TakeDamage(_definition.Damage, _definition);
                else damageable.TakeDamage(_definition.Damage);
            }
        }

        private void SpawnHitEffect(Vector2 position, Vector2 normal)
        {
            if (_hitEffectPrefab == null) return;
            ObjectPoolManager poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("ObjectPoolManager is not available.", this);
                return;
            }

            PooledHitEffect hitEffect = poolManager.Spawn(_hitEffectPrefab, position, Quaternion.identity);
            hitEffect?.Play(position, normal);
        }

        private Vector2 GetTravelBasedNormal()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            return velocity.sqrMagnitude > 0f ? -velocity.normalized : -(Vector2)transform.right;
        }

        private bool IsInHitLayers(int layer)
        {
            return _definition != null && (_definition.HitLayers.value & (1 << layer)) != 0;
        }

        private void ReturnToPool()
        {
            _isLaunched = false;
            _sfxChannel?.StopLoop(GetInstanceID());
            ObjectPoolManager poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("ObjectPoolManager is not available.", this);
                gameObject.SetActive(false);
                return;
            }

            poolManager.Return(this);
        }
    }
}
