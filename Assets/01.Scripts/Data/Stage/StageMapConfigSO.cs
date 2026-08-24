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
        [SerializeField] private MapSegmentType[] _sequence;
        [SerializeField] private int _randomSeed;

        [Header("Stage Rules")]
        [SerializeField] private bool _isTutorial;

        public bool InfiniteRandom => _infiniteRandom;
        public bool ContinueRandomAfterSequence => _continueRandomAfterSequence;
        public MapSegmentCatalogSO SegmentCatalog => _segmentCatalog;
        public IReadOnlyList<MapSegmentType> Sequence => _sequence ?? System.Array.Empty<MapSegmentType>();
        public int RandomSeed => _randomSeed;
        public bool IsTutorial => _isTutorial;
    }
}
