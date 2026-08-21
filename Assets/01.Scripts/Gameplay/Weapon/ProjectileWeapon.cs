using Game.Core.Pooling;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    public sealed class ProjectileWeapon : WeaponBase
    {
        [SerializeField] private PooledProjectile _projectilePrefab;

        private float _nextShotTime;
        private int _shotsRemaining;
        private bool _isBursting;

        private void OnDisable()
        {
            CancelAttack();
        }

        public override void CancelAttack()
        {
            if (!_isBursting)
            {
                return;
            }

            _isBursting = false;
            _shotsRemaining = 0;
            StartCooldown();
        }

        protected override void OnTickAttack(bool triggerHeld, float deltaTime)
        {
            if (!_isBursting && triggerHeld && IsCooldownReady)
            {
                _shotsRemaining = Mathf.Max(1, Definition.BurstCount);
                _nextShotTime = Time.time;
                _isBursting = true;
            }

            while (_isBursting && Time.time >= _nextShotTime)
            {
                FireProjectile();
                _shotsRemaining--;

                if (_shotsRemaining <= 0)
                {
                    _isBursting = false;
                    StartCooldown();
                    return;
                }

                _nextShotTime += Mathf.Max(0f, Definition.BurstInterval);
            }
        }

        private void FireProjectile()
        {
            if (_projectilePrefab == null)
            {
                Debug.LogError("Projectile prefab is not assigned.", this);
                return;
            }

            ObjectPoolManager poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("ObjectPoolManager is not available.", this);
                return;
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, AimDirection);
            PooledProjectile projectile = poolManager.Spawn(
                _projectilePrefab,
                Muzzle.position,
                rotation);

            projectile?.Launch(Definition, AimDirection);
        }
    }
}
