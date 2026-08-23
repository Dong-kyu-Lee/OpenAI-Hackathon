using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Stage
{
    /// <summary>한 스테이지에서 사용할 맵 세그먼트 생성 방식과 순서를 저장합니다.</summary>
    [CreateAssetMenu(
        fileName = "StageMapConfig",
        menuName = "Game/Data/Stage/Stage Map Config")]
    public sealed class StageMapConfigSO : ScriptableObject
    {
        [Header("Generation")]
        [Tooltip("활성화하면 카탈로그의 세그먼트를 무작위로 계속 생성합니다. 비활성화하면 Sequence 순서를 반복합니다.")]
        [SerializeField] private bool _infiniteRandom;
        [SerializeField] private MapSegmentCatalogSO _segmentCatalog;
        [SerializeField] private MapSegmentType[] _sequence;
        [SerializeField] private int _randomSeed;

        /// <summary>카탈로그에서 무작위로 세그먼트를 계속 생성할지 여부를 가져옵니다.</summary>
        public bool InfiniteRandom => _infiniteRandom;

        /// <summary>세그먼트 종류와 프리팹 연결 정보를 가져옵니다.</summary>
        public MapSegmentCatalogSO SegmentCatalog => _segmentCatalog;

        /// <summary>지정 순서 모드에서 반복할 세그먼트 종류 목록을 가져옵니다.</summary>
        public IReadOnlyList<MapSegmentType> Sequence =>
            _sequence ?? System.Array.Empty<MapSegmentType>();

        /// <summary>무작위 생성 순서를 재현하기 위한 시드를 가져옵니다.</summary>
        public int RandomSeed => _randomSeed;
    }
}