using System.Text;
using Game.Core.Events;
using Game.Core.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private VoidEventChannelSO _bindingsChangedChannel;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField] private InputActionReference _slideAction;
        [SerializeField] private InputActionReference _attackAction;
        [SerializeField] private InputActionReference _weaponOneAction;
        [SerializeField] private InputActionReference _weaponTwoAction;
        [SerializeField] private InputActionReference _weaponThreeAction;
        [SerializeField] private InputActionReference _weaponFourAction;
        [SerializeField] private string _bindingGroup = "Keyboard&Mouse";
        [SerializeField] private string _unboundText = "Unbound";

        private TutorialPresentation _currentPresentation;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (_presentationChannel != null)
                _presentationChannel.Raised += OnPresentationChanged;
            if (_bindingsChangedChannel != null)
                _bindingsChangedChannel.Raised += RefreshInputLabel;
        }

        private void OnDisable()
        {
            if (_presentationChannel != null)
                _presentationChannel.Raised -= OnPresentationChanged;
            if (_bindingsChangedChannel != null)
                _bindingsChangedChannel.Raised -= RefreshInputLabel;

            _currentPresentation = TutorialPresentation.Hidden;
            SetVisible(false);
        }

        private void OnPresentationChanged(TutorialPresentation presentation)
        {
            _currentPresentation = presentation;

            if (_titleText != null) _titleText.text = presentation.Title;
            if (_messageText != null) _messageText.text = presentation.Message;

            RefreshInputLabel();
            SetVisible(presentation.IsVisible);
        }

        private void RefreshInputLabel()
        {
            if (_inputLabelText == null)
                return;

            TutorialInputHint[] hints = _currentPresentation.InputHints;
            if (hints == null || hints.Length == 0)
            {
                _inputLabelText.text = _currentPresentation.InputLabel;
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hints.Length; i++)
            {
                if (i > 0) builder.AppendLine();

                ResolveHint(hints[i], out string label, out InputActionReference action);
                builder.Append(label);
                builder.Append(": ");
                builder.Append(GetBindingDisplayString(action));
            }

            _inputLabelText.text = builder.ToString();
        }

        private void ResolveHint(
            TutorialInputHint hint,
            out string label,
            out InputActionReference action)
        {
            switch (hint)
            {
                case TutorialInputHint.Jump:
                    label = "점프";
                    action = _jumpAction;
                    return;
                case TutorialInputHint.Slide:
                    label = "슬라이드";
                    action = _slideAction;
                    return;
                case TutorialInputHint.Attack:
                    label = "공격";
                    action = _attackAction;
                    return;
                case TutorialInputHint.WeaponOne:
                    label = "Machine Gun";
                    action = _weaponOneAction;
                    return;
                case TutorialInputHint.WeaponTwo:
                    label = "Rocket Launcher";
                    action = _weaponTwoAction;
                    return;
                case TutorialInputHint.WeaponThree:
                    label = "Ice Gun";
                    action = _weaponThreeAction;
                    return;
                case TutorialInputHint.WeaponFour:
                    label = "Fire Gun";
                    action = _weaponFourAction;
                    return;
                default:
                    label = hint.ToString();
                    action = null;
                    return;
            }
        }

        private string GetBindingDisplayString(InputActionReference reference)
        {
            if (reference == null || reference.action == null)
                return _unboundText;

            string display = string.IsNullOrWhiteSpace(_bindingGroup)
                ? reference.action.GetBindingDisplayString()
                : reference.action.GetBindingDisplayString(InputBinding.MaskByGroup(_bindingGroup));

            return string.IsNullOrWhiteSpace(display) ? _unboundText : display;
        }

        private void SetVisible(bool isVisible)
        {
            if (_panelRoot != null) _panelRoot.SetActive(isVisible);
        }
    }
}
