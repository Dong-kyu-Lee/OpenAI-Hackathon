using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Stage
{
    [CreateAssetMenu(fileName = "StageMapConfig", menuName = "Game/Data/Stage/Stage Map Config")]
    public sealed class StageMapConfigSO : ScriptableObject
    {
        [Header("Generation")]
        [SerializeField] private bool _infiniteRandom;
        [SerializeField] private bool _continueRandomAfterSequence;
        [SerializeField] private MapSegmentCatalogSO _segmentCatalog;
        [Tooltip("유한 스테이지에서 StartSafe 다음에 순서대로 배치할 맵 세그먼트 프리팹입니다. 배열이 비어 있으면 기존 Type Sequence를 사용합니다.")]
        [SerializeField] private GameObject[] _orderedSegmentPrefabs;
        [SerializeField] private int _randomSeed;

        [Header("Legacy Type Sequence")]
        [SerializeField] private MapSegmentType[] _sequence;

        [Header("Stage Rules")]
        [SerializeField] private bool _isTutorial;

        public bool InfiniteRandom => _infiniteRandom;
        public bool ContinueRandomAfterSequence => _continueRandomAfterSequence;
        public MapSegmentCatalogSO SegmentCatalog => _segmentCatalog;
        public IReadOnlyList<GameObject> OrderedSegmentPrefabs =>
            _orderedSegmentPrefabs ?? System.Array.Empty<GameObject>();
        public bool UsesFiniteOrderedSequence =>
            !_infiniteRandom && OrderedSegmentPrefabs.Count > default(int);
        public IReadOnlyList<MapSegmentType> Sequence => _sequence ?? System.Array.Empty<MapSegmentType>();
        public int RandomSeed => _randomSeed;
        public bool IsTutorial => _isTutorial;
    }
}
