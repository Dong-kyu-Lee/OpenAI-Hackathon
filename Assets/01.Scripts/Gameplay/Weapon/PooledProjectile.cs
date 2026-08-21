using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Pooling;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PooledProjectile : MonoBehaviour, IPoolable
    {
        private const int ExplosionHitCapacity = 32;

        [SerializeField] private TrailRenderer _trailRenderer;

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

            if (_trailRenderer == null)
            {
                _trailRenderer = GetComponent<TrailRenderer>();
            }
        }

        private void OnEnable()
        {
            ResetState();
        }

        private void FixedUpdate()
        {
            if (!_isLaunched)
            {
                return;
            }

            _remainingLifetime -= Time.fixedDeltaTime;
            if (_remainingLifetime <= 0f)
            {
                ReturnToPool();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isLaunched || collision.collider == null || !IsInHitLayers(collision.gameObject.layer))
            {
                return;
            }

            Vector2 impactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

            ResolveImpact(collision.collider, impactPoint);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isLaunched || other == null || !IsInHitLayers(other.gameObject.layer))
            {
                return;
            }

            ResolveImpact(other, other.ClosestPoint(transform.position));
        }

        public void Launch(WeaponDefinitionSO definition, Vector2 direction)
        {
            _definition = definition;
            _remainingLifetime = Mathf.Max(0f, definition.ProjectileLifetime);
            _isLaunched = true;
            _isResolvingImpact = false;
            _collider.enabled = true;
            _rigidbody.linearVelocity = direction.normalized * definition.ProjectileSpeed;
        }

        public void OnDespawned()
        {
            ResetState();
        }

        private void ResetState()
        {
            _definition = null;
            _remainingLifetime = 0f;
            _isLaunched = false;
            _isResolvingImpact = false;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _trailRenderer?.Clear();
        }

        private void ResolveImpact(Collider2D directHit, Vector2 impactPoint)
        {
            if (_isResolvingImpact || _definition == null)
            {
                return;
            }

            _isResolvingImpact = true;
            _collider.enabled = false;
            _rigidbody.linearVelocity = Vector2.zero;

            if (_definition.Impact == WeaponDefinitionSO.ImpactMode.Explosion)
            {
                ApplyExplosionDamage(impactPoint);
            }
            else
            {
                ApplyDirectDamage(directHit);
            }

            ReturnToPool();
        }

        private void ApplyDirectDamage(Collider2D directHit)
        {
            IDamageable damageable = directHit.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(_definition.Damage);
        }

        private void ApplyExplosionDamage(Vector2 impactPoint)
        {
            _damagedTargets.Clear();

            int hitCount = Physics2D.OverlapCircleNonAlloc(
                impactPoint,
                _definition.ExplosionRadius,
                _explosionHits,
                _definition.HitLayers);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _explosionHits[i];
                if (hit == null)
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !_damagedTargets.Add(damageable))
                {
                    continue;
                }

                damageable.TakeDamage(_definition.Damage);
            }
        }

        private bool IsInHitLayers(int layer)
        {
            return _definition != null
                && (_definition.HitLayers.value & (1 << layer)) != 0;
        }

        private void ReturnToPool()
        {
            _isLaunched = false;

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
