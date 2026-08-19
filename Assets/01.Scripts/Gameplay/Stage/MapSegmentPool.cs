using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// 등록된 세그먼트 프리팹별로 인스턴스를 프리워밍하고 대여·반환 상태를 추적합니다.
    /// 세그먼트의 배치 순서, 스크롤 등록과 콘텐츠 생성은 담당하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapSegmentPool : MonoBehaviour, IMapSegmentPool
    {
        [Serializable]
        private sealed class PoolDefinition
        {
            [SerializeField] private MapSegment _prefab;
            [SerializeField, Min(0)] private int _initialCapacity;

            /// <summary>이 정의가 관리할 원본 세그먼트 프리팹을 가져옵니다.</summary>
            public MapSegment Prefab => _prefab;

            /// <summary>Awake에서 미리 생성할 인스턴스 수를 가져옵니다.</summary>
            public int InitialCapacity => _initialCapacity;
        }

        private sealed class PoolBucket
        {
            public PoolBucket(MapSegment prefab, int initialCapacity)
            {
                Prefab = prefab;
                InitialCapacity = initialCapacity;
                InactiveSegments = new Stack<MapSegment>(initialCapacity);
                InactiveSet = new HashSet<MapSegment>();
            }

            /// <summary>버킷이 관리하는 원본 세그먼트 프리팹을 가져옵니다.</summary>
            public MapSegment Prefab { get; }

            /// <summary>버킷의 프리워밍 수를 가져옵니다.</summary>
            public int InitialCapacity { get; }

            /// <summary>다음 대여 후보를 가져오기 위한 비활성 세그먼트 스택을 가져옵니다.</summary>
            public Stack<MapSegment> InactiveSegments { get; }

            /// <summary>중복 저장 여부를 검사하는 비활성 세그먼트 집합을 가져옵니다.</summary>
            public HashSet<MapSegment> InactiveSet { get; }
        }

        [SerializeField] private PoolDefinition[] _definitions;
        [SerializeField] private Transform _poolRoot;

        private readonly Dictionary<MapSegment, PoolBucket> _bucketsByPrefab = new();

        // 모든 생성 인스턴스는 원본 버킷에 계속 귀속되어야 하며, 활성 집합과 해당 버킷의
        // 비활성 집합 중 정확히 한 곳에만 존재해야 교차 풀 반환과 중복 대여를 막을 수 있습니다.
        private readonly Dictionary<MapSegment, PoolBucket> _bucketsByInstance = new();
        private readonly HashSet<MapSegment> _activeSegments = new();

        private bool _isInitialized;

        /// <summary>현재 파괴되지 않은 활성·비활성 세그먼트의 전체 개수를 가져옵니다.</summary>
        public int TotalCount => CountActiveSegments() + CountInactiveSegments();

        /// <summary>현재 대여 중이며 파괴되지 않은 세그먼트 개수를 가져옵니다.</summary>
        public int ActiveCount => CountActiveSegments();

        /// <summary>현재 풀에 보관 중이며 파괴되지 않은 세그먼트 개수를 가져옵니다.</summary>
        public int InactiveCount => CountInactiveSegments();

        private void Awake()
        {
            if (!TryCreateBuckets())
            {
                enabled = false;
                return;
            }

            foreach (KeyValuePair<MapSegment, PoolBucket> pair in _bucketsByPrefab)
            {
                PoolBucket bucket = pair.Value;

                for (int index = default; index < bucket.InitialCapacity; index++)
                {
                    CreateAndStoreInactive(bucket);
                }
            }

            _isInitialized = true;
        }

        /// <summary>지정한 프리팹 버킷에서 세그먼트를 대여하고 로컬 Transform을 초기화합니다.</summary>
        /// <param name="prefab">등록된 원본 세그먼트 프리팹입니다.</param>
        /// <param name="parent">대여된 세그먼트의 부모입니다. <see langword="null"/>이면 풀 루트를 사용합니다.</param>
        /// <param name="segment">성공 시 활성화된 세그먼트이며, 실패 시 <see langword="null"/>입니다.</param>
        /// <returns>풀이 초기화되어 있고 등록 프리팹의 인스턴스를 정상적으로 대여했으면 <see langword="true"/>입니다.</returns>
        public bool TryRent(MapSegment prefab, Transform parent, out MapSegment segment)
        {
            segment = null;

            if (!_isInitialized || prefab == null || !_bucketsByPrefab.TryGetValue(prefab, out PoolBucket bucket))
            {
                return false;
            }

            if (!TryTakeInactive(bucket, out segment))
            {
                CreateAndStoreInactive(bucket);

                if (!TryTakeInactive(bucket, out segment))
                {
                    return false;
                }
            }

            if (!_activeSegments.Add(segment))
            {
                StoreInactive(bucket, segment);
                segment = null;
                return false;
            }

            Transform rentalParent = parent != null ? parent : _poolRoot;
            Transform segmentTransform = segment.transform;
            segmentTransform.SetParent(rentalParent, false);
            ResetLocalTransform(segmentTransform);
            segment.gameObject.SetActive(true);
            return true;
        }

        /// <summary>대여 중인 세그먼트를 원본 버킷의 비활성 저장소로 반환합니다.</summary>
        /// <param name="segment">반환할 세그먼트입니다. 외부 인스턴스, 중복 반환 또는 <see langword="null"/>은 무시됩니다.</param>
        public void Return(MapSegment segment)
        {
            if (!_isInitialized || segment == null ||
                !_bucketsByInstance.TryGetValue(segment, out PoolBucket bucket) ||
                bucket.InactiveSet.Contains(segment) ||
                !_activeSegments.Remove(segment))
            {
                return;
            }

            ResetForStorage(segment);
            StoreInactive(bucket, segment);
        }

        /// <summary>지정한 프리팹 버킷에 속한 파괴되지 않은 세그먼트 수를 조회합니다.</summary>
        /// <param name="prefab">개수를 조회할 등록 원본 프리팹입니다.</param>
        /// <param name="totalCount">활성 및 비활성 세그먼트 수의 합입니다.</param>
        /// <param name="activeCount">현재 대여 중인 세그먼트 수입니다.</param>
        /// <param name="inactiveCount">현재 풀에 보관 중인 세그먼트 수입니다.</param>
        /// <returns>해당 프리팹의 버킷이 존재하면 <see langword="true"/>이며, 실패 시 출력값은 모두 0입니다.</returns>
        public bool TryGetCounts(
            MapSegment prefab,
            out int totalCount,
            out int activeCount,
            out int inactiveCount)
        {
            totalCount = default;
            activeCount = default;
            inactiveCount = default;

            if (prefab == null || !_bucketsByPrefab.TryGetValue(prefab, out PoolBucket bucket))
            {
                return false;
            }

            foreach (MapSegment segment in bucket.InactiveSet)
            {
                if (segment != null)
                {
                    inactiveCount++;
                }
            }

            foreach (MapSegment segment in _activeSegments)
            {
                if (segment != null &&
                    _bucketsByInstance.TryGetValue(segment, out PoolBucket instanceBucket) &&
                    ReferenceEquals(instanceBucket, bucket))
                {
                    activeCount++;
                }
            }

            totalCount = activeCount + inactiveCount;
            return true;
        }

        private bool TryCreateBuckets()
        {
            if (_poolRoot == null || _definitions == null || _definitions.Length == default)
            {
                Debug.LogError("MapSegmentPool has missing pool root or pool definitions.", this);
                return false;
            }

            for (int index = default; index < _definitions.Length; index++)
            {
                PoolDefinition definition = _definitions[index];

                if (definition == null || definition.Prefab == null || definition.InitialCapacity < default(int))
                {
                    Debug.LogError("MapSegmentPool has an invalid pool definition.", this);
                    _bucketsByPrefab.Clear();
                    return false;
                }

                if (_bucketsByPrefab.ContainsKey(definition.Prefab))
                {
                    Debug.LogError("MapSegmentPool contains a duplicate segment prefab definition.", this);
                    _bucketsByPrefab.Clear();
                    return false;
                }

                _bucketsByPrefab.Add(
                    definition.Prefab,
                    new PoolBucket(definition.Prefab, definition.InitialCapacity));
            }

            return true;
        }

        private void CreateAndStoreInactive(PoolBucket bucket)
        {
            MapSegment segment = Instantiate(bucket.Prefab, _poolRoot, false);
            _bucketsByInstance.Add(segment, bucket);
            ResetForStorage(segment);
            StoreInactive(bucket, segment);
        }

        private bool TryTakeInactive(PoolBucket bucket, out MapSegment segment)
        {
            while (bucket.InactiveSegments.Count > default(int))
            {
                MapSegment candidate = bucket.InactiveSegments.Pop();
                bucket.InactiveSet.Remove(candidate);

                if (candidate == null)
                {
                    // 파괴된 Unity 오브젝트는 == null이지만 관리 참조는 남을 수 있으므로
                    // 해당 참조를 인스턴스-버킷 표에서도 제거해 이후 상태 추적에 사용하지 않습니다.
                    if (!ReferenceEquals(candidate, null))
                    {
                        _bucketsByInstance.Remove(candidate);
                    }

                    continue;
                }

                if (!_bucketsByInstance.TryGetValue(candidate, out PoolBucket instanceBucket) ||
                    !ReferenceEquals(instanceBucket, bucket) ||
                    _activeSegments.Contains(candidate))
                {
                    continue;
                }

                segment = candidate;
                return true;
            }

            segment = null;
            return false;
        }

        private void ResetForStorage(MapSegment segment)
        {
            segment.Deactivate();

            Transform segmentTransform = segment.transform;
            segmentTransform.SetParent(_poolRoot, false);
            ResetLocalTransform(segmentTransform);
            segment.gameObject.SetActive(false);
        }

        private static void ResetLocalTransform(Transform target)
        {
            target.localPosition = default;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        private static void StoreInactive(PoolBucket bucket, MapSegment segment)
        {
            if (!bucket.InactiveSet.Add(segment))
            {
                return;
            }

            bucket.InactiveSegments.Push(segment);
        }

        private int CountActiveSegments()
        {
            int count = default;

            foreach (MapSegment segment in _activeSegments)
            {
                if (segment != null)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountInactiveSegments()
        {
            int count = default;

            foreach (KeyValuePair<MapSegment, PoolBucket> pair in _bucketsByPrefab)
            {
                foreach (MapSegment segment in pair.Value.InactiveSet)
                {
                    if (segment != null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
