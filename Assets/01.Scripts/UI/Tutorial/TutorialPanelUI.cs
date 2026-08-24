using Game.Core.Events;
using Game.Core.Tutorial;
using TMPro;
using UnityEngine;

namespace Game.UI.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private TMP_Text _inputLabelText;
        [SerializeField] private TutorialPresentationEventChannelSO _presentationChannel;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (_presentationChannel != null)
            {
                _presentationChannel.Raised += OnPresentationChanged;
            }
        }

        private void OnDisable()
        {
            if (_presentationChannel != null)
            {
                _presentationChannel.Raised -= OnPresentationChanged;
            }

            SetVisible(false);
        }

        private void OnPresentationChanged(TutorialPresentation presentation)
        {
            if (_titleText != null)
            {
                _titleText.text = presentation.Title;
            }

            if (_messageText != null)
            {
                _messageText.text = presentation.Message;
            }

            if (_inputLabelText != null)
            {
                _inputLabelText.text = presentation.InputLabel;
            }

            SetVisible(presentation.IsVisible);
        }

        private void SetVisible(bool isVisible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(isVisible);
            }
        }
    }
}
