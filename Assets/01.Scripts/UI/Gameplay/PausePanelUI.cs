using Game.Core.Events;
using Game.Core.Flow;
using Game.UI.Options;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Gameplay
{
    /// <summary>
    /// 일시정지 상태일 때만 일시정지 패널을 보여주고, 버튼 입력을 흐름 요청 채널로 전달합니다.
    /// 옵션 화면과 종료 확인 팝업의 여닫기도 이 컴포넌트가 조율하며,
    /// 그 화면들이 떠 있는 동안에는 일시정지 입력을 억제하도록 채널로 알립니다.
    /// 상태 방송을 놓치지 않도록 이 컴포넌트는 항상 활성인 오브젝트에 두고 패널만 켜고 끕니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PausePanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private GameObject _backgroundPanel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _stageSelectButton;
        [SerializeField] private Button _optionsButton;

        [Header("하위 화면")]
        [SerializeField] private OptionsPanelUI _optionsPanel;
        [SerializeField] private QuitConfirmPopupUI _quitConfirmPopup;

        [Header("채널")]
        [SerializeField] private GameStateEventChannelSO _gameStateChangedChannel;
        [SerializeField] private VoidEventChannelSO _resumeRequestedChannel;
        [SerializeField] private VoidEventChannelSO _retryRequestedChannel;
        [SerializeField] private VoidEventChannelSO _stageSelectRequestedChannel;
        [SerializeField] private BoolEventChannelSO _pauseInputSuppressedChannel;

        private bool _isPaused;
        private bool _isPauseInputSuppressed;

        private void Awake()
        {
            CloseSubViews();
            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            if (_gameStateChangedChannel != null)
            {
                _gameStateChangedChannel.Raised += OnGameStateChanged;
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(RequestResume);
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(RequestRetry);
            }

            if (_stageSelectButton != null)
            {
                _stageSelectButton.onClick.AddListener(OpenQuitConfirm);
            }

            if (_optionsButton != null)
            {
                _optionsButton.onClick.AddListener(OpenOptions);
            }

            if (_optionsPanel != null)
            {
                _optionsPanel.Closed += OnSubViewClosed;
            }

            if (_quitConfirmPopup != null)
            {
                _quitConfirmPopup.Confirmed += OnQuitConfirmed;
                _quitConfirmPopup.Canceled += OnSubViewClosed;
            }
        }

        private void OnDisable()
        {
            if (_gameStateChangedChannel != null)
            {
                _gameStateChangedChannel.Raised -= OnGameStateChanged;
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(RequestResume);
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(RequestRetry);
            }

            if (_stageSelectButton != null)
            {
                _stageSelectButton.onClick.RemoveListener(OpenQuitConfirm);
            }

            if (_optionsButton != null)
            {
                _optionsButton.onClick.RemoveListener(OpenOptions);
            }

            if (_optionsPanel != null)
            {
                _optionsPanel.Closed -= OnSubViewClosed;
            }

            if (_quitConfirmPopup != null)
            {
                _quitConfirmPopup.Confirmed -= OnQuitConfirmed;
                _quitConfirmPopup.Canceled -= OnSubViewClosed;
            }

            // UI 씬이 내려갈 때 억제가 켜진 채로 남으면 일시정지 입력이 영영 막힙니다.
            SetPauseInputSuppressed(false);
        }

        private void OnGameStateChanged(GameState state)
        {
            _isPaused = state == GameState.Paused;

            if (!_isPaused)
            {
                CloseSubViews();
            }

            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            bool isOptionsOpen = _optionsPanel != null && _optionsPanel.IsOpen;
            bool isQuitConfirmOpen = _quitConfirmPopup != null && _quitConfirmPopup.IsOpen;

            // 옵션은 일시정지 메뉴를 대체하고, 종료 확인 팝업은 그 위에 겹쳐서 뜹니다.
            SetPanelVisible(_isPaused && !isOptionsOpen);
            SetPauseInputSuppressed(_isPaused && (isOptionsOpen || isQuitConfirmOpen));
        }

        private void CloseSubViews()
        {
            if (_optionsPanel != null)
            {
                _optionsPanel.Close();
            }

            if (_quitConfirmPopup != null)
            {
                _quitConfirmPopup.Close();
            }
        }

        private void SetPanelVisible(bool isVisible)
        {
            if(_backgroundPanel == null) Debug.LogWarning("backgroundPanel is null");
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(isVisible);
                _backgroundPanel.SetActive(isVisible);
            }
        }

        private void SetPauseInputSuppressed(bool isSuppressed)
        {
            if (_isPauseInputSuppressed == isSuppressed)
            {
                return;
            }

            _isPauseInputSuppressed = isSuppressed;
            _pauseInputSuppressedChannel?.Raise(isSuppressed);
        }

        private void OpenOptions()
        {
            if (_optionsPanel == null)
            {
                Debug.LogError("옵션 패널이 연결되지 않아 설정 화면을 열 수 없습니다.", this);
                return;
            }

            _optionsPanel.Open();
            RefreshVisibility();
        }

        private void OpenQuitConfirm()
        {
            if (_quitConfirmPopup == null)
            {
                // 확인 절차 없이 나가게 되므로 연결 누락을 반드시 알립니다.
                Debug.LogError("종료 확인 팝업이 연결되지 않아 확인 없이 스테이지를 나갑니다.", this);
                RequestStageSelect();
                return;
            }

            _quitConfirmPopup.Open();
            RefreshVisibility();
        }

        private void OnSubViewClosed()
        {
            RefreshVisibility();
        }

        private void OnQuitConfirmed()
        {
            RefreshVisibility();
            RequestStageSelect();
        }

        private void RequestResume()
        {
            _resumeRequestedChannel?.Raise();
        }

        private void RequestRetry()
        {
            _retryRequestedChannel?.Raise();
        }

        private void RequestStageSelect()
        {
            _stageSelectRequestedChannel?.Raise();
        }
    }
}
