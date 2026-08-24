using Game.Core.Events;
using Game.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealthHudUI : MonoBehaviour
    {
        [SerializeField] private PlayerStatsSO _stats;
        [SerializeField] private IntEventChannelSO _healthChangedChannel;
        [SerializeField] private TMP_Text _healthLabel;
        [SerializeField] private Image[] _healthSegments;

        private void OnEnable()
        {
            if (_healthChangedChannel != null)
                _healthChangedChannel.Raised += Refresh;

            Refresh(_stats == null ? 0 : _stats.MaxHealth);
        }

        private void OnDisable()
        {
            if (_healthChangedChannel != null)
                _healthChangedChannel.Raised -= Refresh;
        }

        private void Refresh(int currentHealth)
        {
            int maximumHealth = _stats == null ? 0 : _stats.MaxHealth;
            int clampedHealth = Mathf.Clamp(currentHealth, 0, maximumHealth);

            if (_healthLabel != null)
                _healthLabel.text = $"HP  {clampedHealth} / {maximumHealth}";

            float normalizedHealth = maximumHealth <= 0
                ? 0f
                : (float)clampedHealth / maximumHealth;

            int fillCount = _healthSegments == null ? 0 : _healthSegments.Length;
            for (int i = 0; i < fillCount; i++)
            {
                Image fill = _healthSegments[i];
                if (fill == null)
                    continue;

                fill.enabled = true;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.fillAmount = normalizedHealth;
            }
        }
    }
}