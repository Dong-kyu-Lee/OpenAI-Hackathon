using System.Collections;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Data;
using Game.Gameplay.Weapon;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>플레이어 피격 시 모든 자식 스프라이트를 잠시 피격 색상으로 표시합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHitFlash : MonoBehaviour
    {
        [SerializeField] private PlayerStatsSO _stats;
        [SerializeField] private VoidEventChannelSO _playerHitChannel;

        private SpriteRenderer[] _renderers;
        private Color[] _originalColors;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            List<SpriteRenderer> characterRenderers = new(childRenderers.Length);

            for (int index = 0; index < childRenderers.Length; index++)
            {
                if (childRenderers[index].GetComponentInParent<WeaponBase>() == null)
                {
                    characterRenderers.Add(childRenderers[index]);
                }
            }

            _renderers = characterRenderers.ToArray();
            _originalColors = new Color[_renderers.Length];

            for (int index = 0; index < _renderers.Length; index++)
            {
                _originalColors[index] = _renderers[index].color;
            }
        }

        private void OnEnable()
        {
            if (_playerHitChannel != null)
            {
                _playerHitChannel.Raised += PlayFlash;
            }
        }

        private void OnDisable()
        {
            if (_playerHitChannel != null)
            {
                _playerHitChannel.Raised -= PlayFlash;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            RestoreColors();
        }

        private void PlayFlash()
        {
            if (_stats == null || _renderers.Length == default)
            {
                return;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            for (int index = 0; index < _renderers.Length; index++)
            {
                if (_renderers[index] != null)
                {
                    _renderers[index].color = _stats.HitFlashColor;
                }
            }

            yield return new WaitForSeconds(_stats.HitFlashDuration);

            RestoreColors();
            _flashRoutine = null;
        }

        private void RestoreColors()
        {
            if (_renderers == null || _originalColors == null)
            {
                return;
            }

            for (int index = 0; index < _renderers.Length; index++)
            {
                if (_renderers[index] != null)
                {
                    _renderers[index].color = _originalColors[index];
                }
            }
        }
    }
}
