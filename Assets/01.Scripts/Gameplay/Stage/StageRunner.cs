using Game.Core.Events;
using Game.Core.Flow;
using Game.Data.Stage;
using UnityEngine;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// 스테이지 씬마다 하나씩 배치해, 흐름 담당자가 보낸 시작·정지 신호를 맵 스트리밍과 스크롤 구동으로 옮기고
    /// 클리어 거리 도달과 플레이어 사망을 감시해 스테이지 결과를 알립니다.
    /// 씬 로드와 언로드, 상태 전환은 담당하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageRunner : MonoBehaviour
    {
        [SerializeField] private StageDefinitionSO _stageDefinition;
        [SerializeField] private StageSelectionStateSO _stageSelectionState;
        [SerializeField] private MapStreamManager _streamManager;
        [SerializeField] private MapScrollController _scrollController;

        [Header("수신 채널")]
        [SerializeField] private VoidEventChannelSO _gameplayStartedChannel;
        [SerializeField] private VoidEventChannelSO _gameplayStoppedChannel;
        [SerializeField] private VoidEventChannelSO _playerDiedChannel;
        [SerializeField] private IntEventChannelSO _playerHealthChangedChannel;

        [Header("발신 채널")]
        [SerializeField] private StageResultEventChannelSO _stageFinishedChannel;

        private float _elapsedTime;
        private int _lastKnownHealth;
        private bool _isRunning;
        private bool _hasFinished;

        /// <summary>스테이지가 진행 중이며 아직 종료 판정이 나지 않았는지 여부를 가져옵니다.</summary>
        public bool IsRunning => _isRunning;

        private void OnEnable()
        {
            if (_gameplayStartedChannel != null)
            {
                _gameplayStartedChannel.Raised += OnGameplayStarted;
            }

            if (_gameplayStoppedChannel != null)
            {
                _gameplayStoppedChannel.Raised += OnGameplayStopped;
            }

            if (_playerDiedChannel != null)
            {
                _playerDiedChannel.Raised += OnPlayerDied;
            }

            if (_playerHealthChangedChannel != null)
            {
                _playerHealthChangedChannel.Raised += OnPlayerHealthChanged;
            }

            if (_streamManager != null)
            {
                _streamManager.StageEndReached += OnStageEndReached;
            }
        }

        private void OnDisable()
        {
            if (_gameplayStartedChannel != null)
            {
                _gameplayStartedChannel.Raised -= OnGameplayStarted;
            }

            if (_gameplayStoppedChannel != null)
            {
                _gameplayStoppedChannel.Raised -= OnGameplayStopped;
            }

            if (_playerDiedChannel != null)
            {
                _playerDiedChannel.Raised -= OnPlayerDied;
            }

            if (_playerHealthChangedChannel != null)
            {
                _playerHealthChangedChannel.Raised -= OnPlayerHealthChanged;
            }

            if (_streamManager != null)
            {
                _streamManager.StageEndReached -= OnStageEndReached;
            }
        }

        private void Update()
        {
            if (!_isRunning || _hasFinished)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;

            StageDefinitionSO stageDefinition = ResolveStageDefinition();
            if (stageDefinition == null || _scrollController == null)
            {
                return;
            }

            if (stageDefinition.MapConfig != null &&
                stageDefinition.MapConfig.UsesFiniteOrderedSequence)
            {
                return;
            }

            if (!stageDefinition.IsEndlessMode &&
                stageDefinition.ClearDistance > 0f &&
                _scrollController.DistanceTravelled >= stageDefinition.ClearDistance)
            {
                Finish(true);
            }
        }

        private void OnGameplayStarted()
        {
            if (_streamManager == null)
            {
                Debug.LogError("MapStreamManager가 연결되지 않아 스테이지를 시작할 수 없습니다.", this);
                return;
            }

            _elapsedTime = default;
            _hasFinished = false;

            if (!_streamManager.StartStreaming())
            {
                Debug.LogError("맵 스트리밍 시작에 실패해 스테이지를 진행할 수 없습니다.", this);
                return;
            }

            _isRunning = true;
        }

        private void OnGameplayStopped()
        {
            _isRunning = false;

            if (_streamManager != null)
            {
                _streamManager.StopStreaming();
            }
        }

        private void OnPlayerDied()
        {
            if (!_isRunning || _hasFinished)
            {
                return;
            }

            Finish(false);
        }

        private void OnStageEndReached()
        {
            StageDefinitionSO stageDefinition = ResolveStageDefinition();

            if (_isRunning &&
                !_hasFinished &&
                (stageDefinition == null || !stageDefinition.IsEndlessMode))
            {
                Finish(true);
            }
        }

        private void OnPlayerHealthChanged(int currentHealth)
        {
            _lastKnownHealth = currentHealth;
        }

        private StageDefinitionSO ResolveStageDefinition()
        {
            return _stageSelectionState != null &&
                _stageSelectionState.CurrentStageDefinition != null
                    ? _stageSelectionState.CurrentStageDefinition
                    : _stageDefinition;
        }

        private void Finish(bool cleared)
        {
            _hasFinished = true;
            _isRunning = false;

            // 결과 화면 뒤로 맵이 그대로 남아야 하므로 세그먼트를 반환하지 않고 이동만 멈춥니다.
            if (_scrollController != null)
            {
                _scrollController.StopScrolling();
            }

            float distance = _scrollController == null ? default : _scrollController.DistanceTravelled;
            _stageFinishedChannel?.Raise(new StageResult(cleared, _elapsedTime, distance, _lastKnownHealth));
        }
    }
}
