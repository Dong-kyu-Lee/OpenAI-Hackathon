using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI.Options
{
    /// <summary>
    /// 액션 하나에 걸린 키 바인딩을 표시하고, 버튼을 누르면 다음에 입력된 키로 다시 할당합니다.
    /// 저장은 하지 않으며, 실제로 바뀌었을 때 <see cref="Rebound"/>로만 알립니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KeyBindEntryUI : MonoBehaviour
    {
        private const string CancelPath = "<Keyboard>/escape";
        private const string PointerPositionPath = "<Pointer>/position";
        private const string PointerDeltaPath = "<Pointer>/delta";

        [SerializeField] private TMP_Text _actionLabel;
        [SerializeField] private TMP_Text _bindingLabel;
        [SerializeField] private Button _rebindButton;
        [SerializeField] private string _waitingText = "Press any key";
        [SerializeField] private string _emptyBindingText = "None";

        private InputAction _action;
        private int _bindingIndex = -1;
        private bool _wasActionEnabled;
        private InputActionRebindingExtensions.RebindingOperation _operation;

        /// <summary>키가 실제로 다시 할당됐을 때 발생합니다. 취소했을 때는 발생하지 않습니다.</summary>
        public event Action Rebound;

        private bool HasValidBinding =>
            _action != null && _bindingIndex >= 0 && _bindingIndex < _action.bindings.Count;

        /// <summary>표시하고 조작할 액션과 바인딩을 지정합니다.</summary>
        /// <param name="action">대상 액션입니다.</param>
        /// <param name="displayName">화면에 보여 줄 기능 이름입니다.</param>
        /// <param name="bindingIndex">액션 안에서 조작할 바인딩의 순번입니다. 복합 바인딩은 부분마다 순번이 다릅니다.</param>
        public void Bind(InputAction action, string displayName, int bindingIndex)
        {
            _action = action;
            _bindingIndex = bindingIndex;

            if (_actionLabel != null)
            {
                _actionLabel.text = displayName;
            }

            if (!HasValidBinding)
            {
                Debug.LogError($"'{displayName}'의 {bindingIndex}번 바인딩을 찾을 수 없습니다.", this);

                if (_rebindButton != null)
                {
                    _rebindButton.interactable = false;
                }
            }

            RefreshBindingLabel();
        }

        /// <summary>현재 바인딩을 다시 읽어 표시를 갱신합니다.</summary>
        public void RefreshBindingLabel()
        {
            if (_bindingLabel == null)
            {
                return;
            }

            if (!HasValidBinding)
            {
                _bindingLabel.text = _emptyBindingText;
                return;
            }

            string path = _action.bindings[_bindingIndex].effectivePath;

            _bindingLabel.text = string.IsNullOrEmpty(path)
                ? _emptyBindingText
                : InputControlPath.ToHumanReadableString(
                    path,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        /// <summary>이 항목에 적용된 사용자 지정 바인딩을 지우고 기본값으로 되돌립니다.</summary>
        public void RemoveOverride()
        {
            if (!HasValidBinding)
            {
                return;
            }

            _action.RemoveBindingOverride(_bindingIndex);
            RefreshBindingLabel();
        }

        private void OnEnable()
        {
            if (_rebindButton != null)
            {
                _rebindButton.onClick.AddListener(StartRebind);
            }
        }

        private void OnDisable()
        {
            if (_rebindButton != null)
            {
                _rebindButton.onClick.RemoveListener(StartRebind);
            }

            // 대기 중에 화면이 닫히면 조작이 남지 않도록 즉시 정리합니다.
            if (_operation != null)
            {
                DisposeOperation();
                RefreshBindingLabel();

                if (_rebindButton != null)
                {
                    _rebindButton.interactable = true;
                }
            }
        }

        private void StartRebind()
        {
            if (!HasValidBinding || _operation != null)
            {
                return;
            }

            // 활성 상태의 액션은 다시 할당할 수 없으므로 잠시 끄고, 끝난 뒤 원래대로 되돌립니다.
            _wasActionEnabled = _action.enabled;
            _action.Disable();

            if (_bindingLabel != null)
            {
                _bindingLabel.text = _waitingText;
            }

            if (_rebindButton != null)
            {
                _rebindButton.interactable = false;
            }

            _operation = _action.PerformInteractiveRebinding(_bindingIndex)
                .WithControlsExcluding(PointerPositionPath)
                .WithControlsExcluding(PointerDeltaPath)
                .WithCancelingThrough(CancelPath)
                .OnComplete(_ => FinishRebind(true))
                .OnCancel(_ => FinishRebind(false))
                .Start();
        }

        private void FinishRebind(bool isChanged)
        {
            DisposeOperation();
            RefreshBindingLabel();

            if (_rebindButton != null)
            {
                _rebindButton.interactable = true;
            }

            if (isChanged)
            {
                Rebound?.Invoke();
            }
        }

        private void DisposeOperation()
        {
            _operation?.Dispose();
            _operation = null;

            if (_wasActionEnabled && _action != null)
            {
                _action.Enable();
            }
        }
    }
}
