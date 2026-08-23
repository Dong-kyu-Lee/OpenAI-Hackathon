using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Gameplay
{
    /// <summary>
    /// 스테이지를 정말 종료할지 한 번 더 묻는 확인 팝업입니다.
    /// 흐름 요청 채널은 알지 못하며 선택 결과만 이벤트로 알립니다.
    /// 이 컴포넌트는 자기 오브젝트를 직접 켜고 끄므로, 팝업 루트 오브젝트에 붙입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuitConfirmPopupUI : MonoBehaviour
    {
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        /// <summary>종료하기를 선택했을 때 발생합니다.</summary>
        public event Action Confirmed;

        /// <summary>돌아가기를 선택했을 때 발생합니다.</summary>
        public event Action Canceled;

        /// <summary>팝업이 열려 있는지 여부를 가져옵니다.</summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>팝업을 엽니다.</summary>
        public void Open()
        {
            gameObject.SetActive(true);
        }

        /// <summary>선택 결과를 알리지 않고 팝업을 닫습니다. 강제로 정리할 때 사용합니다.</summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(Confirm);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(Cancel);
            }
        }

        private void OnDisable()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(Confirm);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(Cancel);
            }
        }

        private void Confirm()
        {
            Close();
            Confirmed?.Invoke();
        }

        private void Cancel()
        {
            Close();
            Canceled?.Invoke();
        }
    }
}
