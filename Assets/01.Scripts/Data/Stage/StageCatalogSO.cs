using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Stage
{
    /// <summary>
    /// 선택 가능한 스테이지의 목록과 순서를 제공하는 데이터입니다.
    /// 스테이지 선택 화면과 상태 머신은 이 목록의 순번만으로 서로 통신합니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StageCatalog",
        menuName = "Game/Data/Stage/Stage Catalog")]
    public sealed class StageCatalogSO : ScriptableObject
    {
        [SerializeField] private StageDefinitionSO[] _stages;

        /// <summary>등록된 스테이지를 표시 순서대로 담은 읽기 전용 목록을 가져옵니다.</summary>
        public IReadOnlyList<StageDefinitionSO> Stages => _stages;

        /// <summary>등록된 스테이지의 개수를 가져옵니다.</summary>
        public int Count => _stages == null ? default : _stages.Length;

        /// <summary>순번에 해당하는 스테이지를 가져옵니다.</summary>
        /// <param name="stageIndex">조회할 스테이지의 순번입니다.</param>
        /// <param name="definition">조회에 성공하면 해당 스테이지가, 실패하면 <see langword="null"/>이 담깁니다.</param>
        /// <returns>순번이 범위 안이고 비어 있지 않은 항목이면 <see langword="true"/>입니다.</returns>
        public bool TryGet(int stageIndex, out StageDefinitionSO definition)
        {
            if (_stages == null || stageIndex < 0 || stageIndex >= _stages.Length)
            {
                definition = null;
                return false;
            }

            definition = _stages[stageIndex];
            return definition != null;
        }
    }
}
