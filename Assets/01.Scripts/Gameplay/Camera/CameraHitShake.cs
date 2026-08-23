using Game.Core.Events;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay.CameraEffects
{
    /// <summary>플레이어 피격 이벤트에 반응해 카메라에 짧고 약한 위치 흔들림을 적용합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class CameraHitShake : MonoBehaviour
    {
        [SerializeField] private PlayerStatsSO _stats;
        [SerializeField] private VoidEventChannelSO _playerHitChannel;

        private Vector3 _appliedOffset;
        private float _remainingDuration;

        private void OnEnable()
        {
            if (_playerHitChannel != null)
            {
                _playerHitChannel.Raised += StartShake;
            }
        }

        private void OnDisable()
        {
            if (_playerHitChannel != null)
            {
                _playerHitChannel.Raised -= StartShake;
            }

            RemoveAppliedOffset();
            _remainingDuration = default;
        }

        private void LateUpdate()
        {
            RemoveAppliedOffset();

            if (_stats == null || _remainingDuration <= 0f)
            {
                return;
            }

            _remainingDuration = Mathf.Max(0f, _remainingDuration - Time.deltaTime);
            _appliedOffset = (Vector3)(Random.insideUnitCircle * _stats.HitShakeStrength);
            transform.localPosition += _appliedOffset;
        }

        private void StartShake()
        {
            if (_stats != null)
            {
                _remainingDuration = _stats.HitShakeDuration;
            }
        }

        private void RemoveAppliedOffset()
        {
            if (_appliedOffset == Vector3.zero)
            {
                return;
            }

            transform.localPosition -= _appliedOffset;
            _appliedOffset = Vector3.zero;
        }
    }
}
