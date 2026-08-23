using System;
using System.Runtime.Serialization;
using Game.Core.Events;
using Game.UI.Options;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Title
{
    /// <summary>
    /// 타이틀 씬의 버튼 입력을 흐름 요청 채널로 전달합니다.
    /// 상태 전환은 App의 상태 머신이 판단하므로 여기서는 요청만 보냅니다.
    /// 설정 화면은 씬 전환이 아니라 타이틀 위에 겹쳐 뜨므로, 여닫기는 이 컴포넌트가 조율합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleScreenUI : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private VoidEventChannelSO _stageSelectRequestedChannel;
        [SerializeField] private VoidEventChannelSO _quitRequestedChannel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _backgroundPanel;

        private OptionsPanelUI _optionsPanel;

        private void Awake()
        {
            if (_settingsPanel != null)
            {
                _optionsPanel = _settingsPanel.GetComponent<OptionsPanelUI>();

                if (_optionsPanel == null)
                {
                    // 뒤로가기 통지를 받을 수 없으면 배경 패널이 켜진 채로 남습니다.
                    Debug.LogError("설정 패널에 OptionsPanelUI가 없어 닫힘 통지를 받을 수 없습니다.", this);
                }
            }

            CloseSettings();
        }

        private void OnEnable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(RequestStageSelect);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OpenSettings);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(RequestQuit);
            }

            if (_optionsPanel != null)
            {
                _optionsPanel.Closed += OnSettingsClosed;
            }
        }

        private void OnDisable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(RequestStageSelect);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OpenSettings);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(RequestQuit);
            }

            if (_optionsPanel != null)
            {
                _optionsPanel.Closed -= OnSettingsClosed;
            }
        }

        private void OpenSettings()
        {
            if (_settingsPanel == null)
            {
                Debug.LogError("설정 패널이 연결되지 않아 설정 화면을 열 수 없습니다.", this);
                return;
            }

            if (_optionsPanel != null)
            {
                _optionsPanel.Open();
            }
            else
            {
                _settingsPanel.SetActive(true);
            }

            SetBackgroundVisible(true);
        }

        private void CloseSettings()
        {
            // 설정 패널이 스스로 닫히면 Closed로 배경을 끄지만, 시작 시점에는 통지가 오지 않습니다.
            if (_optionsPanel != null)
            {
                _optionsPanel.Close();
            }
            else if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }

            SetBackgroundVisible(false);
        }

        private void OnSettingsClosed()
        {
            SetBackgroundVisible(false);
        }

        private void SetBackgroundVisible(bool isVisible)
        {
            if (_backgroundPanel == null)
            {
                Debug.LogWarning("배경 패널이 연결되지 않아 설정 화면 배경을 켜고 끌 수 없습니다.", this);
                return;
            }

            _backgroundPanel.SetActive(isVisible);
        }

        private void RequestStageSelect()
        {
            _stageSelectRequestedChannel?.Raise();
        }

        private void RequestQuit()
        {
            _quitRequestedChannel?.Raise();
        }
    }
}
