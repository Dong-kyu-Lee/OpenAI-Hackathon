using Game.Core.Combat;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    public sealed class ContinuousRayWeapon : WeaponBase
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private LaserBeamVisual _beamVisual;

        private float _firingElapsed;
        private bool _isFiring;

        private void Awake()
        {
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

            if (hit.collider == null)
            {
                return;
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(Definition.Damage * appliedDuration);

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

        private void StopAttack(bool startCooldown)
        {
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
