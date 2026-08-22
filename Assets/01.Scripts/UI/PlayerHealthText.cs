using Game.Core.Events;
using Game.Data;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>플레이어 체력 변경 이벤트를 받아 현재 체력을 화면 텍스트로 표시합니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class PlayerHealthText : MonoBehaviour
    {
        [SerializeField] private PlayerStatsSO _stats;
        [SerializeField] private IntEventChannelSO _healthChangedChannel;

        private TextMeshProUGUI _label;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (_healthChangedChannel != null)
            {
                _healthChangedChannel.Raised += UpdateHealthText;
            }

            UpdateHealthText(_stats == null ? default : _stats.MaxHealth);
        }

        private void OnDisable()
        {
            if (_healthChangedChannel != null)
            {
                _healthChangedChannel.Raised -= UpdateHealthText;
            }
        }

        private void UpdateHealthText(int currentHealth)
        {
            if (_label == null)
            {
                return;
            }

            int maximumHealth = _stats == null ? default : _stats.MaxHealth;
            _label.text = $"HP: {currentHealth} / {maximumHealth}";
        }
    }
}
