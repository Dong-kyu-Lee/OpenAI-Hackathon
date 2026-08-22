using Game.Core.Events;
using Game.Core.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Gameplay
{
    /// <summary>
    /// 스테이지 종료 결과를 화면에 표시하고, 다시하기와 스테이지 선택 입력을 흐름 요청 채널로 전달합니다.
    /// 결과 값은 종료 채널에서, 표시 여부는 상태 채널에서 각각 받습니다.
    /// 상태 방송을 놓치지 않도록 이 컴포넌트는 항상 활성인 오브젝트에 두고 패널만 켜고 끕니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultPanelUI : MonoBehaviour
    {
        private const string TimeFormat = @"{0:00}:{1:00}.{2:00}";
        private const int SecondsPerMinute = 60;
        private const int HundredthsPerSecond = 100;

        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _outcomeLabel;
        [SerializeField] private TMP_Text _timeLabel;
        [SerializeField] private TMP_Text _distanceLabel;
        [SerializeField] private TMP_Text _remainingHealthLabel;
        [SerializeField] private string _clearedText = "STAGE CLEAR";
        [SerializeField] private string _failedText = "STAGE FAILED";
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _stageSelectButton;
        [SerializeField] private GameStateEventChannelSO _gameStateChangedChannel;
        [SerializeField] private StageResultEventChannelSO _stageFinishedChannel;
        [SerializeField] private VoidEventChannelSO _retryRequestedChannel;
        [SerializeField] private VoidEventChannelSO _stageSelectRequestedChannel;

        private void Awake()
        {
            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            if (_gameStateChangedChannel != null)
            {
                _gameStateChangedChannel.Raised += OnGameStateChanged;
            }

            if (_stageFinishedChannel != null)
            {
                _stageFinishedChannel.Raised += OnStageFinished;
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(RequestRetry);
            }

            if (_stageSelectButton != null)
            {
                _stageSelectButton.onClick.AddListener(RequestStageSelect);
            }
        }

        private void OnDisable()
        {
            if (_gameStateChangedChannel != null)
            {
                _gameStateChangedChannel.Raised -= OnGameStateChanged;
            }

            if (_stageFinishedChannel != null)
            {
                _stageFinishedChannel.Raised -= OnStageFinished;
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(RequestRetry);
            }

            if (_stageSelectButton != null)
            {
                _stageSelectButton.onClick.RemoveListener(RequestStageSelect);
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            SetPanelVisible(state == GameState.Result);
        }

        private void OnStageFinished(StageResult result)
        {
            // 표시는 상태 채널이 결정하므로 여기서는 값만 채웁니다.
            if (_outcomeLabel != null)
            {
                _outcomeLabel.text = result.Cleared ? _clearedText : _failedText;
            }

            if (_timeLabel != null)
            {
                _timeLabel.text = FormatTime(result.ElapsedTime);
            }

            if (_distanceLabel != null)
            {
                _distanceLabel.text = Mathf.FloorToInt(result.Distance).ToString();
            }

            if (_remainingHealthLabel != null)
            {
                _remainingHealthLabel.text = result.RemainingHealth.ToString();
            }
        }

        private static string FormatTime(float elapsedTime)
        {
            int totalSeconds = Mathf.FloorToInt(elapsedTime);
            int minutes = totalSeconds / SecondsPerMinute;
            int seconds = totalSeconds % SecondsPerMinute;
            int hundredths = Mathf.FloorToInt((elapsedTime - totalSeconds) * HundredthsPerSecond);

            return string.Format(TimeFormat, minutes, seconds, hundredths);
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(isVisible);
            }
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
