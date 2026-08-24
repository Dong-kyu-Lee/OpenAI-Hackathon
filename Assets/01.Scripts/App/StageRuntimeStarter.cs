using Game.Core.Events;
using Game.Core.Tutorial;
using Game.Data.Stage;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.App
{
    /// <summary>
    /// 스테이지 선택 UI가 저장한 설정을 공용 게임 씬의 맵 스트림에 적용하고 실행합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageRuntimeStarter : MonoBehaviour
    {
        [SerializeField] private StageSelectionStateSO _stageSelectionState;
        [SerializeField] private MapStreamManager _mapStreamManager;
        [SerializeField] private TutorialInputPermissionEventChannelSO _tutorialInputPermissionChannel;

        private void Awake()
        {
            PrepareSelectedStage();
        }

        /// <summary>현재 선택된 스테이지 설정으로 맵 스트리밍을 초기화합니다.</summary>
public bool PrepareSelectedStage()
        {
            if (_stageSelectionState == null || _mapStreamManager == null)
            {
                Debug.LogError("StageRuntimeStarter has missing required references.", this);
                return false;
            }

            StageMapConfigSO stageConfig = _stageSelectionState.CurrentStageConfig;
            if (stageConfig == null)
            {
                Debug.LogError("No stage map config has been selected and no default is assigned.", this);
                return false;
            }

            _tutorialInputPermissionChannel?.Raise(
                stageConfig.IsTutorial
                    ? TutorialInputPermission.None
                    : TutorialInputPermission.All);

            _mapStreamManager.StopStreaming();
            return _mapStreamManager.SetStageConfig(stageConfig) && _mapStreamManager.ResetForRestart();
        }

        /// <summary>현재 선택된 스테이지를 즉시 준비하고 스트리밍을 시작합니다.</summary>
        public bool StartSelectedStage()
        {
            return PrepareSelectedStage() && _mapStreamManager.StartStreaming();
        }
    }
}
