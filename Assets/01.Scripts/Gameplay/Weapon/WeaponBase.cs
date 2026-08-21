using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    public abstract class WeaponBase : MonoBehaviour
    {
        private const float MinimumAimMagnitude = 0.0001f;

        [SerializeField] private WeaponDefinitionSO _definition;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private float _nextReadyTime;

        protected WeaponDefinitionSO Definition => _definition;
        protected Transform Muzzle => _muzzle;
        protected Vector2 AimDirection { get; private set; } = Vector2.right;
        protected bool IsCooldownReady => Time.time >= _nextReadyTime;

        public Vector3 MuzzlePosition => _muzzle != null ? _muzzle.position : transform.position;

        public void TickAttack(bool triggerHeld, Vector2 aimDirection, float deltaTime)
        {
            if (_definition == null || _muzzle == null)
            {
                return;
            }

            if (aimDirection.sqrMagnitude >= MinimumAimMagnitude)
            {
                AimDirection = aimDirection.normalized;
            }

            UpdateAimVisual();
            OnTickAttack(triggerHeld, deltaTime);
        }

        public abstract void CancelAttack();

        protected abstract void OnTickAttack(bool triggerHeld, float deltaTime);

        protected void StartCooldown()
        {
            _nextReadyTime = Time.time + Mathf.Max(0f, _definition.Cooldown);
        }

        private void UpdateAimVisual()
        {
            float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipY = AimDirection.x < 0f;
            }
        }
    }
}
