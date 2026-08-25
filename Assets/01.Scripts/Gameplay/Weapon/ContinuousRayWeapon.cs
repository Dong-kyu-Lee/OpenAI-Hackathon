using Game.Core.Combat;
using Game.Core.Pooling;
using Game.Data;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    public sealed class ContinuousRayWeapon : WeaponBase
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private LaserBeamVisual _beamVisual;
        [SerializeField] private PooledHitEffect _hitEffectPrefab;
        [SerializeField] private IceBridgeBuilder _iceBridgeBuilder;

        private PooledHitEffect _hitEffectInstance;
        private float _firingElapsed;
        private bool _isFiring;

        private void Awake()
        {
            if (_iceBridgeBuilder == null)
            {
                _iceBridgeBuilder = GetComponent<IceBridgeBuilder>();
            }

            SetLineVisible(false);
        }

        private void OnDisable()
        {
            CancelAttack();
        }

        public override void CancelAttack()
        {
            StopAttack(_isFiring);
        }

        protected override void OnTickAttack(bool triggerHeld, float deltaTime)
        {
            if (!_isFiring)
            {
                if (!triggerHeld || !IsCooldownReady)
                {
                    return;
                }

                _isFiring = true;
                _firingElapsed = 0f;
                SetLineVisible(true);
            }

            if (!triggerHeld || _firingElapsed >= Definition.MaxContinuousDuration)
            {
                StopAttack(true);
                return;
            }

            float appliedDuration = Mathf.Min(
                deltaTime,
                Definition.MaxContinuousDuration - _firingElapsed);

            _firingElapsed += appliedDuration;
            ProcessRay(appliedDuration);

            if (_firingElapsed >= Definition.MaxContinuousDuration)
            {
                StopAttack(true);
            }
        }

        private void ProcessRay(float appliedDuration)
        {
            Vector2 origin = Muzzle.position;
            RaycastHit2D hit = Physics2D.Raycast(
                origin,
                AimDirection,
                Definition.Range,
                Definition.HitLayers);

            Vector3 endPosition = hit.collider != null
                ? hit.point
                : origin + AimDirection * Definition.Range;

            if (_beamVisual != null)
            {
                _beamVisual.SetBeam(origin, endPosition);
            }
            else if (_lineRenderer != null)
            {
                _lineRenderer.SetPosition(0, origin);
                _lineRenderer.SetPosition(1, endPosition);
            }

            UpdateHitEffect(hit);

            if (Definition.DamageElement == WeaponDefinitionSO.Element.Ice &&
                _iceBridgeBuilder != null)
            {
                float availableDistance = hit.collider == null
                    ? Definition.Range
                    : hit.distance;
                _iceBridgeBuilder.TryBuild(origin, AimDirection, availableDistance);
            }

            if (hit.collider == null)
            {
                return;
            }

            float appliedDamage = Definition.Damage * appliedDuration;
            IWeaponDamageable weaponDamageable = hit.collider.GetComponentInParent<IWeaponDamageable>();
            if (weaponDamageable != null)
            {
                weaponDamageable.TakeDamage(appliedDamage, Definition);
            }
            else
            {
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(appliedDamage);
            }

            if (Definition.DamageElement == WeaponDefinitionSO.Element.Ice)
            {
                IFreezable freezable = hit.collider.GetComponentInParent<IFreezable>();
                freezable?.Freeze();
            }
            else if (Definition.DamageElement == WeaponDefinitionSO.Element.Fire)
            {
                IBurnable burnable = hit.collider.GetComponentInParent<IBurnable>();
                burnable?.ApplyBurn(appliedDuration);
            }
        }

        private void UpdateHitEffect(RaycastHit2D hit)
        {
            if (_hitEffectPrefab == null)
            {
                return;
            }

            if (hit.collider == null)
            {
                // 레이가 허공을 향한 동안에도 인스턴스는 유지하고 방출만 멈춰 풀 스래싱을 막는다.
                _hitEffectInstance?.SetEmitting(false);
                return;
            }

            if (_hitEffectInstance != null)
            {
                _hitEffectInstance.Follow(hit.point, hit.normal);
                _hitEffectInstance.SetEmitting(true);
                return;
            }

            ObjectPoolManager poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("ObjectPoolManager is not available.", this);
                return;
            }

            _hitEffectInstance = poolManager.Spawn(
                _hitEffectPrefab,
                hit.point,
                Quaternion.identity);

            _hitEffectInstance?.Play(hit.point, hit.normal);
        }

        private void ReleaseHitEffect()
        {
            if (_hitEffectInstance == null)
            {
                return;
            }

            _hitEffectInstance.Release();
            _hitEffectInstance = null;
        }

        private void StopAttack(bool startCooldown)
        {
            ReleaseHitEffect();

            if (!_isFiring)
            {
                SetLineVisible(false);
                return;
            }

            _isFiring = false;
            _firingElapsed = 0f;
            SetLineVisible(false);

            if (startCooldown)
            {
                StartCooldown();
            }
        }

        private void SetLineVisible(bool isVisible)
        {
            if (_beamVisual != null)
            {
                if (isVisible)
                {
                    _beamVisual.Play();
                }
                else
                {
                    _beamVisual.Stop();
                }

                return;
            }

            if (_lineRenderer == null)
            {
                return;
            }

            _lineRenderer.positionCount = 2;
            _lineRenderer.enabled = isVisible;
        }
    }
}
