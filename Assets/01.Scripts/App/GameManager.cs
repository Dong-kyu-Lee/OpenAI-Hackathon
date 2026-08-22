using Game.Core.Events;
using Game.Core.Flow;
using Game.Data.Stage;
using UnityEngine;

namespace Game.App
{
    /// <summary>
    /// 게임 전체 흐름의 상태 머신입니다. 요청 채널을 구독해 상태 전환의 가부를 판단하고,
    /// 실제 씬 구성은 <see cref="SceneFlowController"/>에 위임하며, 결정된 상태를 채널로 방송합니다.
    /// 시스템 씬에 하나만 존재한다고 가정합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        private const float PausedTimeScale = 0f;
        private const float RunningTimeScale = 1f;

        [SerializeField] private SceneFlowController _sceneFlow;
        [SerializeField] private StageCatalogSO _stageCatalog;
        [SerializeField] private string _titleSceneName = "Title";
        [SerializeField] private string _stageSelectSceneName = "StageSelect";

        [Header("발신 채널")]
        [SerializeField] private GameStateEventChannelSO _gameStateChangedChannel;
        [SerializeField] private VoidEventChannelSO _gameplayStartedChannel;
        [SerializeField] private VoidEventChannelSO _gameplayStoppedChannel;

        [Header("수신 채널")]
        [SerializeField] private StageRequestEventChannelSO _stageRequestedChannel;
        [SerializeField] private StageResultEventChannelSO _stageFinishedChannel;
        [SerializeField] private VoidEventChannelSO _titleRequestedChannel;
        [SerializeField] private VoidEventChannelSO _stageSelectRequestedChannel;
        [SerializeField] private VoidEventChannelSO _pauseRequestedChannel;
        [SerializeField] private VoidEventChannelSO _resumeRequestedChannel;
        [SerializeField] private VoidEventChannelSO _retryRequestedChannel;
        [SerializeField] private VoidEventChannelSO _quitRequestedChannel;

        /// <summary>시스템 씬에 존재하는 유일한 상태 머신 인스턴스를 가져옵니다.</summary>
        public static GameManager Instance { get; private set; }

        /// <summary>현재 게임 흐름 상태를 가져옵니다.</summary>
        public GameState CurrentState { get; private set; } = GameState.Boot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("GameManager는 하나만 활성화될 수 있습니다.", this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 부트스트랩이 시스템 씬 로드를 끝낸 뒤 호출합니다. 부트스트랩 씬을 첫 흐름 씬으로 등록하고
        /// 타이틀 화면으로 전환합니다.
        /// </summary>
        /// <param name="bootstrapSceneName">타이틀 전환과 함께 언로드할 부트스트랩 씬의 이름입니다.</param>
        public void BeginFlow(string bootstrapSceneName)
        {
            if (CurrentState != GameState.Boot)
            {
                Debug.LogWarning("이미 흐름이 시작되어 BeginFlow 요청을 무시했습니다.", this);
                return;
            }

            if (_sceneFlow == null)
            {
                Debug.LogError("SceneFlowController가 연결되지 않아 흐름을 시작할 수 없습니다.", this);
                return;
            }

            _sceneFlow.SetInitialFlowScene(bootstrapSceneName);
            SetTimeScale(RunningTimeScale);
            SetState(GameState.Loading);
            _sceneFlow.SwitchFlowScene(_titleSceneName, EnterTitle);
        }

        private void EnterTitle()
        {
            SetState(GameState.Title);
        }

        private void EnterStageSelect()
        {
            SetState(GameState.StageSelect);
        }

        private void OnTitleRequested()
        {
            if (CurrentState != GameState.StageSelect)
            {
                return;
            }

            SetState(GameState.Loading);
            _sceneFlow.SwitchFlowScene(_titleSceneName, EnterTitle);
        }

        private void OnStageSelectRequested()
        {
            switch (CurrentState)
            {
                case GameState.Title:
                    SetState(GameState.Loading);
                    _sceneFlow.SwitchFlowScene(_stageSelectSceneName, EnterStageSelect);
                    break;

                case GameState.Paused:
                case GameState.Result:
                    SetTimeScale(RunningTimeScale);
                    _gameplayStoppedChannel?.Raise();
                    SetState(GameState.Loading);
                    _sceneFlow.ExitStage(_stageSelectSceneName, EnterStageSelect);
                    break;
            }
        }

        private void OnStageRequested(int stageIndex)
        {
            if (CurrentState != GameState.StageSelect)
            {
                return;
            }

            if (_stageCatalog == null || !_stageCatalog.TryGet(stageIndex, out StageDefinitionSO definition))
            {
                Debug.LogError($"카탈로그에서 {stageIndex}번 스테이지를 찾을 수 없습니다.", this);
                return;
            }

            if (string.IsNullOrEmpty(definition.SceneName))
            {
                Debug.LogError($"'{definition.name}'에 씬 이름이 설정되지 않았습니다.", this);
                return;
            }

            SetState(GameState.Loading);
            _sceneFlow.EnterStage(definition.SceneName, StartGameplay);
        }

        private void OnRetryRequested()
        {
            if (CurrentState != GameState.Paused && CurrentState != GameState.Result)
            {
                return;
            }

            SetTimeScale(RunningTimeScale);
            _gameplayStoppedChannel?.Raise();
            SetState(GameState.Loading);
            _sceneFlow.ReloadStage(StartGameplay);
        }

        private void OnPauseRequested()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            SetTimeScale(PausedTimeScale);
            SetState(GameState.Paused);
        }

        private void OnResumeRequested()
        {
            if (CurrentState != GameState.Paused)
            {
                return;
            }

            SetTimeScale(RunningTimeScale);
            SetState(GameState.Playing);
        }

        private void OnStageFinished(StageResult result)
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            // 결과 화면 뒤로 방금 플레이한 스테이지가 그대로 보이도록 언로드하지 않고 시간만 멈춥니다.
            SetTimeScale(PausedTimeScale);
            SetState(GameState.Result);
        }

        private void OnQuitRequested()
        {
            if (CurrentState != GameState.Title)
            {
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void StartGameplay()
        {
            SetTimeScale(RunningTimeScale);
            SetState(GameState.Playing);

            // 스테이지 씬의 진행 담당자가 로드 중에 구독을 마친 뒤 신호를 받도록 상태 방송 다음에 둡니다.
            _gameplayStartedChannel?.Raise();
        }

        private void SetState(GameState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            CurrentState = nextState;
            _gameStateChangedChannel?.Raise(nextState);
        }

        private static void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
        }

        private void Subscribe()
        {
            if (_stageRequestedChannel != null)
            {
                _stageRequestedChannel.Raised += OnStageRequested;
            }

            if (_stageFinishedChannel != null)
            {
                _stageFinishedChannel.Raised += OnStageFinished;
            }

            if (_titleRequestedChannel != null)
            {
                _titleRequestedChannel.Raised += OnTitleRequested;
            }

            if (_stageSelectRequestedChannel != null)
            {
                _stageSelectRequestedChannel.Raised += OnStageSelectRequested;
            }

            if (_pauseRequestedChannel != null)
            {
                _pauseRequestedChannel.Raised += OnPauseRequested;
            }

            if (_resumeRequestedChannel != null)
            {
                _resumeRequestedChannel.Raised += OnResumeRequested;
            }

            if (_retryRequestedChannel != null)
            {
                _retryRequestedChannel.Raised += OnRetryRequested;
            }

            if (_quitRequestedChannel != null)
            {
                _quitRequestedChannel.Raised += OnQuitRequested;
            }
        }

        private void Unsubscribe()
        {
            if (_stageRequestedChannel != null)
            {
                _stageRequestedChannel.Raised -= OnStageRequested;
            }

            if (_stageFinishedChannel != null)
            {
                _stageFinishedChannel.Raised -= OnStageFinished;
            }

            if (_titleRequestedChannel != null)
            {
                _titleRequestedChannel.Raised -= OnTitleRequested;
            }

            if (_stageSelectRequestedChannel != null)
            {
                _stageSelectRequestedChannel.Raised -= OnStageSelectRequested;
            }

            if (_pauseRequestedChannel != null)
            {
                _pauseRequestedChannel.Raised -= OnPauseRequested;
            }

            if (_resumeRequestedChannel != null)
            {
                _resumeRequestedChannel.Raised -= OnResumeRequested;
            }

            if (_retryRequestedChannel != null)
            {
                _retryRequestedChannel.Raised -= OnRetryRequested;
            }

            if (_quitRequestedChannel != null)
            {
                _quitRequestedChannel.Raised -= OnQuitRequested;
            }
        }
    }
}
