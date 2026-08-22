using UnityEngine;

namespace Game.Data.Stage
{
    /// <summary>
    /// 스테이지 1개의 식별 정보와 클리어 조건을 제공하는 데이터입니다.
    /// 스테이지 선택 화면의 표시와 상태 머신의 씬 해석이 모두 이 값을 참조합니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StageDefinition",
        menuName = "Game/Data/Stage/Stage Definition")]
    public sealed class StageDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private string _sceneName;
        [SerializeField] private Sprite _thumbnail;
        [SerializeField, Min(0f)] private float _clearDistance;

        /// <summary>스테이지 선택 화면에 표시할 이름을 가져옵니다.</summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// 로드할 스테이지 씬의 이름을 가져옵니다.
        /// Build Settings에 등록된 씬 파일명과 정확히 일치해야 합니다.
        /// </summary>
        public string SceneName => _sceneName;

        /// <summary>스테이지 선택 화면에 표시할 미리보기 이미지를 가져옵니다. 연결하지 않아도 됩니다.</summary>
        public Sprite Thumbnail => _thumbnail;

        /// <summary>클리어로 판정할 누적 주행 거리를 월드 유닛으로 가져옵니다.</summary>
        public float ClearDistance => _clearDistance;
    }
}
