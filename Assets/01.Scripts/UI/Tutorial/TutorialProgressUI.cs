using Game.Core.Events;
using Game.Core.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialProgressUI : MonoBehaviour
    {
        [SerializeField] private TutorialPresentationEventChannelSO _presentationChannel;
        [SerializeField] private TMP_Text _stepNumberText;
        [SerializeField] private Image[] _progressIcons;
        [SerializeField] private Sprite _activeSprite;
        [SerializeField] private Sprite _completeSprite;
        [SerializeField] private Sprite _inactiveSprite;

        private int _currentStepIndex = -1;
        private bool _wasVisible;

        private void OnEnable()
        {
            if (_presentationChannel != null)
                _presentationChannel.Raised += OnPresentationChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (_presentationChannel != null)
                _presentationChannel.Raised -= OnPresentationChanged;

            _currentStepIndex = -1;
            _wasVisible = false;
        }

        private void OnPresentationChanged(TutorialPresentation presentation)
        {
            if (presentation.IsVisible && !_wasVisible)
            {
                int lastIndex = (_progressIcons == null ? 0 : _progressIcons.Length) - 1;
                _currentStepIndex = Mathf.Min(_currentStepIndex + 1, lastIndex);
                Refresh();
            }

            _wasVisible = presentation.IsVisible;
        }

        private void Refresh()
        {
            if (_stepNumberText != null)
                _stepNumberText.text = (_currentStepIndex + 1).ToString("00");

            if (_progressIcons == null)
                return;

            for (int i = 0; i < _progressIcons.Length; i++)
            {
                Image icon = _progressIcons[i];
                if (icon == null)
                    continue;

                icon.sprite = i < _currentStepIndex
                    ? _completeSprite
                    : i == _currentStepIndex
                        ? _activeSprite
                        : _inactiveSprite;
            }
        }
    }
}