using System;
using UnityEngine;

namespace Game.Data.Stage
{
    /// <summary>맵 세그먼트 종류와 실제 프리팹의 공용 연결 정보를 보관합니다.</summary>
    [CreateAssetMenu(fileName = "MapSegmentCatalog", menuName = "Game/Stage/Map Segment Catalog")]
    public sealed class MapSegmentCatalogSO : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private MapSegmentType _type;
            [SerializeField] private GameObject _prefab;

            public MapSegmentType Type => _type;
            public GameObject Prefab => _prefab;
        }

        [SerializeField] private Entry[] _entries;

        /// <summary>카탈로그에 등록된 항목 수를 가져옵니다.</summary>
        public int Count => _entries == null ? default : _entries.Length;

        /// <summary>지정한 종류에 연결된 프리팹을 조회합니다.</summary>
        public bool TryGetPrefab(MapSegmentType type, out GameObject prefab)
        {
            if (_entries != null)
            {
                for (int index = default; index < _entries.Length; index++)
                {
                    if (_entries[index].Type == type && _entries[index].Prefab != null)
                    {
                        prefab = _entries[index].Prefab;
                        return true;
                    }
                }
            }

            prefab = null;
            return false;
        }

        /// <summary>등록 순서에 해당하는 세그먼트 종류와 프리팹을 조회합니다.</summary>
        public bool TryGetEntry(int index, out MapSegmentType type, out GameObject prefab)
        {
            if (_entries != null &&
                index >= default(int) &&
                index < _entries.Length &&
                _entries[index].Prefab != null)
            {
                type = _entries[index].Type;
                prefab = _entries[index].Prefab;
                return true;
            }

            type = default;
            prefab = null;
            return false;
        }
    }
}