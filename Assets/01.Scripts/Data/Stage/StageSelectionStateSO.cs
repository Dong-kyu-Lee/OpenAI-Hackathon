using UnityEngine;

namespace Game.Data.Stage
{
    /// <summary>Stage 선택 UI와 공용 게임 씬 사이에서 선택된 맵 설정을 전달합니다.</summary>
    [CreateAssetMenu(
        fileName = "StageSelectionState",
        menuName = "Game/Data/Stage/Stage Selection State")]
    public sealed class StageSelectionStateSO : ScriptableObject
    {
        [SerializeField] private StageMapConfigSO _defaultStageConfig;

        [System.NonSerialized] private StageMapConfigSO _selectedStageConfig;

        /// <summary>선택값이 있으면 선택된 설정을, 없으면 기본 설정을 가져옵니다.</summary>
        public StageMapConfigSO CurrentStageConfig =>
            _selectedStageConfig != null
                ? _selectedStageConfig
                : _defaultStageConfig;

        /// <summary>공용 게임 씬에서 사용할 Stage 설정을 저장합니다.</summary>
        public void SelectStage(StageMapConfigSO stageConfig)
        {
            if (stageConfig == null)
            {
                Debug.LogError("Cannot select a null Stage map config.", this);
                return;
            }

            _selectedStageConfig = stageConfig;
        }

        /// <summary>런타임 선택값을 지워 기본 Stage 설정을 다시 사용하게 합니다.</summary>
        public void ClearSelection()
        {
            _selectedStageConfig = null;
        }
    }
}