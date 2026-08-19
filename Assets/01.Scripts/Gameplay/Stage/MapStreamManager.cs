using System;
using System.Collections.Generic;
using Game.Data.Stage;
using UnityEngine;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// 안전 시작 세그먼트부터 선행 생성 경계까지 맵을 연결하고 활성 순서를 유지하며,
    /// 제거 경계를 지난 세그먼트를 스크롤 대상에서 해제해 풀로 반환합니다.
    /// 세그먼트 인스턴스 생성과 실제 이동 계산은 각각 풀과 스크롤 컨트롤러가 담당합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapStreamManager : MonoBehaviour
    {
        [SerializeField] private MapLayoutSettingsSO _layoutSettings;
        [SerializeField] private MapScrollController _scrollController;
        [SerializeField] private MonoBehaviour _segmentPoolSource;
        [SerializeField] private Transform _segmentParent;
        [SerializeField] private MapSegment _startSafePrefab;
        [SerializeField] private MapSegment[] _candidatePrefabs;
        [SerializeField] private int _testSeed;

        private readonly List<MapSegment> _activeSegments = new();

        private IMapSegmentPool _segmentPool;
        private System.Random _random;
        private string _lastSegmentId;
        private bool _hasValidReferences;
        private bool _isStreaming;
        private bool _isProcessingAfterScrollStep;
        private Vector2 _appliedDisplacementForCurrentStep;

        /// <summary>진입 순서대로 관리되는 현재 활성 세그먼트의 읽기 전용 목록을 가져옵니다.</summary>
        public IReadOnlyList<MapSegment> ActiveSegments => _activeSegments;

        /// <summary>세그먼트 선택 순서를 재현하는 테스트용 난수 시드를 가져옵니다.</summary>
        public int TestSeed => _testSeed;

        /// <summary>맵 반환과 선행 생성을 매 물리 프레임 처리 중인지 여부를 가져옵니다.</summary>
        public bool IsStreaming => _isStreaming;

        private void Awake()
        {
            _segmentPool = _segmentPoolSource as IMapSegmentPool;
            _random = new System.Random(_testSeed);
            _hasValidReferences = ValidateReferences();

            if (_hasValidReferences)
            {
                _scrollController.AfterScrollStep += ProcessStreamingAfterScroll;
            }
        }

        private void ProcessStreamingAfterScroll(Vector2 appliedDisplacement)
        {
            if (!_isStreaming)
            {
                return;
            }

            _isProcessingAfterScrollStep = true;
            _appliedDisplacementForCurrentStep = appliedDisplacement;

            try
            {
                ReturnExpiredSegments();

                if (_activeSegments.Count == default && !TryBuildInitialStream())
                {
                    FailStreaming("The map stream could not recover after all active segments were returned.");
                    return;
                }

                if (!TryFillPreloadDistance())
                {
                    FailStreaming("The map stream could not maintain its preload distance.");
                }
            }
            finally
            {
                _isProcessingAfterScrollStep = false;
                _appliedDisplacementForCurrentStep = default;
            }
        }

        private void OnDisable()
        {
            StopStreaming();
        }

        private void OnDestroy()
        {
            if (_scrollController != null)
            {
                _scrollController.AfterScrollStep -= ProcessStreamingAfterScroll;
            }
        }

        /// <summary>초기 맵 또는 부족한 선행 구간을 준비하고 맵 스크롤을 시작합니다.</summary>
        /// <returns>필수 참조와 세그먼트 준비가 모두 유효해 스트리밍을 시작했으면 <see langword="true"/>입니다. 실패 시 대여된 세그먼트는 반환됩니다.</returns>
        public bool StartStreaming()
        {
            if (!_hasValidReferences)
            {
                return false;
            }

            bool isReady;

            if (_activeSegments.Count == default)
            {
                ResetRandom();
                isReady = TryBuildInitialStream();
            }
            else
            {
                isReady = TryFillPreloadDistance();
            }

            if (!isReady)
            {
                // 부분 구성에 성공한 세그먼트도 남기지 않아 다음 시작이 동일한 상태에서 재시도되게 합니다.
                ReturnAllActiveSegments();
                return false;
            }

            _isStreaming = true;
            _scrollController.StartScrolling();
            return true;
        }

        /// <summary>맵 스크롤과 스트리밍을 정지하고 모든 활성 세그먼트를 풀로 반환합니다.</summary>
        public void StopStreaming()
        {
            _isStreaming = false;

            if (_scrollController != null)
            {
                _scrollController.StopScrolling();
            }

            ReturnAllActiveSegments();
        }

        /// <summary>
        /// 활성 세그먼트, 난수 순서, 스크롤 속도와 누적 거리를 초기화한 뒤 안전 시작 맵을 준비합니다.
        /// 준비만 수행하므로 호출자는 성공 후 <see cref="StartStreaming"/>을 호출해야 실제 이동이 시작됩니다.
        /// </summary>
        /// <returns>필수 참조가 유효하고 안전 시작 맵을 끝까지 준비했으면 <see langword="true"/>입니다. 실패 시 활성 세그먼트는 반환됩니다.</returns>
        public bool ResetForRestart()
        {
            _isStreaming = false;

            if (_scrollController != null)
            {
                _scrollController.StopScrolling();
            }

            if (!_hasValidReferences)
            {
                ReturnAllActiveSegments();
                return false;
            }

            ReturnAllActiveSegments();
            ResetRandom();
            _scrollController.ResetForRestart();

            if (TryBuildInitialStream())
            {
                return true;
            }

            ReturnAllActiveSegments();
            return false;
        }

        private bool ValidateReferences()
        {
            if (_layoutSettings == null ||
                _scrollController == null ||
                _segmentPoolSource == null ||
                _segmentPool == null ||
                _segmentParent == null ||
                _startSafePrefab == null ||
                _candidatePrefabs == null)
            {
                Debug.LogError("MapStreamManager has missing or invalid required references.", this);
                return false;
            }

            return true;
        }

        private bool TryBuildInitialStream()
        {
            // 첫 진입 앵커를 제거 경계에 두어 시작 세그먼트 뒤쪽에 불필요한 빈 공간이 생기지 않게 합니다.
            Vector3 initialEntryPosition = new(
                _layoutSettings.DespawnBoundaryX,
                _layoutSettings.GroundHeight,
                _segmentParent.position.z);

            if (!TryRentAndPlace(_startSafePrefab, initialEntryPosition))
            {
                return false;
            }

            return TryFillPreloadDistance();
        }

        private bool TryFillPreloadDistance()
        {
            if (!TryGetLastExitPosition(out Vector3 exitPosition))
            {
                return false;
            }

            // 마지막 출구가 월드 X 선행 생성 경계에 닿을 때까지 연결해 카메라 앞쪽의 빈 공간을 방지합니다.
            while (exitPosition.x < _layoutSettings.PreloadDistance)
            {
                MapSegment prefab = SelectNextPrefab();

                if (prefab == null || !TryRentAndPlace(prefab, exitPosition))
                {
                    return false;
                }

                if (!TryGetLastExitPosition(out exitPosition))
                {
                    return false;
                }
            }

            return true;
        }

        private MapSegment SelectNextPrefab()
        {
            int alternativeCount = default;
            int validCount = default;

            for (int index = default; index < _candidatePrefabs.Length; index++)
            {
                MapSegment candidate = _candidatePrefabs[index];

                if (candidate == null)
                {
                    continue;
                }

                validCount++;

                if (!string.Equals(candidate.SegmentId, _lastSegmentId, StringComparison.Ordinal))
                {
                    alternativeCount++;
                }
            }

            // 대안이 하나라도 있을 때만 직전 ID를 제외해 반복을 줄이되, 유일 후보라면 스트림을 끊지 않습니다.
            bool excludePrevious = validCount > 1 && alternativeCount > default(int);
            int selectionCount = excludePrevious ? alternativeCount : validCount;

            if (selectionCount == default)
            {
                return null;
            }

            int selectedIndex = _random.Next(selectionCount);

            for (int index = default; index < _candidatePrefabs.Length; index++)
            {
                MapSegment candidate = _candidatePrefabs[index];

                if (candidate == null ||
                    excludePrevious && string.Equals(candidate.SegmentId, _lastSegmentId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (selectedIndex == default)
                {
                    return candidate;
                }

                selectedIndex--;
            }

            return null;
        }

        private bool TryRentAndPlace(MapSegment prefab, Vector3 desiredEntryPosition)
        {
            if (!_segmentPool.TryRent(prefab, _segmentParent, out MapSegment segment) || segment == null)
            {
                return false;
            }

            if (segment.EntryAnchor == null ||
                segment.ExitAnchor == null ||
                segment.DespawnBounds == null ||
                segment.Length <= default(float))
            {
                segment.Deactivate();
                _segmentPool.Return(segment);
                Debug.LogError("A rented map segment has invalid anchors, despawn bounds, or length.", this);
                return false;
            }

            Transform segmentTransform = segment.transform;
            // 루트 피벗이 달라도 진입 앵커의 월드 위치가 직전 출구와 정확히 일치하도록 차이만큼 보정합니다.
            Vector3 rootPosition = segmentTransform.position + desiredEntryPosition - segment.EntryAnchor.position;
            segment.SetWorldPositionForPlacement(rootPosition);

            if (!segment.TryGetPhysicsExitPosition(out Vector3 exitPosition) ||
                exitPosition.x <= desiredEntryPosition.x)
            {
                segment.Deactivate();
                _segmentPool.Return(segment);
                Debug.LogError("A rented map segment does not advance the stream on the world X axis.", this);
                return false;
            }

            segment.Activate();
            _scrollController.RegisterTarget(segment);
            _activeSegments.Add(segment);

            // 기존 대상의 이동이 끝난 뒤 생성된 세그먼트는 같은 이동량만큼 즉시 맞춰
            // 다음 물리 프레임을 기다리지 않고 직전 출구에 연결된 상태를 유지합니다.
            if (_isProcessingAfterScrollStep)
            {
                Vector3 caughtUpPosition =
                    segment.transform.position + (Vector3)_appliedDisplacementForCurrentStep;
                segment.SetWorldPositionForPlacement(caughtUpPosition);
            }

            _lastSegmentId = segment.SegmentId;
            return true;
        }

        private void ReturnExpiredSegments()
        {
            int index = default;

            while (index < _activeSegments.Count)
            {
                MapSegment segment = _activeSegments[index];

                if (segment == null)
                {
                    _activeSegments.RemoveAt(index);
                    continue;
                }

                if (segment.DespawnBounds == null)
                {
                    ReturnSegment(segment);
                    _activeSegments.RemoveAt(index);
                    continue;
                }

                if (segment.DespawnBounds.position.x > _layoutSettings.DespawnBoundaryX)
                {
                    index++;
                    continue;
                }

                ReturnSegment(segment);
                _activeSegments.RemoveAt(index);
            }
        }

        private void ReturnAllActiveSegments()
        {
            for (int index = _activeSegments.Count - 1; index >= default(int); index--)
            {
                ReturnSegment(_activeSegments[index]);
            }

            _activeSegments.Clear();
            _lastSegmentId = null;
        }

        private void ReturnSegment(MapSegment segment)
        {
            if (segment == null)
            {
                return;
            }

            if (_scrollController != null)
            {
                _scrollController.UnregisterTarget(segment);
            }

            segment.Deactivate();

            if (_segmentPool != null && _segmentPoolSource != null)
            {
                _segmentPool.Return(segment);
            }
        }

        private bool TryGetLastExitPosition(out Vector3 exitPosition)
        {
            if (_activeSegments.Count == default)
            {
                exitPosition = default;
                return false;
            }

            MapSegment lastSegment = _activeSegments[_activeSegments.Count - 1];

            if (lastSegment == null)
            {
                exitPosition = default;
                return false;
            }

            return lastSegment.TryGetPhysicsExitPosition(out exitPosition);
        }

        private void ResetRandom()
        {
            // 같은 테스트 시드로 재시작할 때 후보 선택 순서도 처음부터 동일하게 재현합니다.
            _random = new System.Random(_testSeed);
            _lastSegmentId = null;
        }

        private void FailStreaming(string message)
        {
            _isStreaming = false;

            if (_scrollController != null)
            {
                _scrollController.StopScrolling();
            }

            // 중간 실패 시 활성 목록과 풀 대여 상태를 함께 비워 불완전한 스트림이 남지 않게 합니다.
            ReturnAllActiveSegments();
            Debug.LogError(message, this);
        }
    }
}
