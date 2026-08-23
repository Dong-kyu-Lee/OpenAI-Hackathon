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

        private void Start()
        {
            StartSelectedStage();
        }

        /// <summary>현재 선택된 스테이지 설정으로 맵 스트리밍을 초기화하고 시작합니다.</summary>
        public bool StartSelectedStage()
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

            _mapStreamManager.StopStreaming();

            if (!_mapStreamManager.SetStageConfig(stageConfig) ||
                !_mapStreamManager.ResetForRestart())
            {
                return false;
            }

            return _mapStreamManager.StartStreaming();
        }
    }
}
