using Game.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Title
{
    /// <summary>
    /// 타이틀 씬의 버튼 입력을 흐름 요청 채널로 전달합니다.
    /// 상태 전환은 App의 상태 머신이 판단하므로 여기서는 요청만 보냅니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleScreenUI : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private VoidEventChannelSO _stageSelectRequestedChannel;
        [SerializeField] private VoidEventChannelSO _quitRequestedChannel;

        private void Awake()
        {
            // 설정 화면은 아직 구현 범위가 아니므로 버튼을 비활성 상태로 둡니다.
            if (_settingsButton != null)
            {
                _settingsButton.interactable = false;
            }
        }

        private void OnEnable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(RequestStageSelect);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(RequestQuit);
            }
        }

        private void OnDisable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(RequestStageSelect);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(RequestQuit);
            }
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
