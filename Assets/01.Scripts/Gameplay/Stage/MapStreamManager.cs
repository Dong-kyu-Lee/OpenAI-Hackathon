using System;
using System.Collections.Generic;
using Game.Core.Events;
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
        [SerializeField] private StageMapConfigSO _stageConfig;
        [SerializeField] private VoidEventChannelSO _playerDiedChannel;

        private readonly List<MapSegment> _activeSegments = new();

        private IMapSegmentPool _segmentPool;
        private System.Random _random;
        private string _lastSegmentId;
        private bool _hasValidReferences;
        private bool _isStreaming;
        private MapSegmentSelectionMode _selectionMode;
        private MapSegmentSelectionMode _appliedSelectionMode;
        private int _sequenceIndex;
        private bool _isProcessingAfterScrollStep;
        private bool _isSubscribedToScroll;
        private bool _sequenceExhausted;
        private bool _hasReachedStageEnd;
        private Vector2 _appliedDisplacementForCurrentStep;

        /// <summary>유한 Sequence의 실제 끝이 플레이어 종료 경계에 도달했을 때 발생합니다.</summary>
        public event Action StageEndReached;

        /// <summary>진입 순서대로 관리되는 현재 활성 세그먼트의 읽기 전용 목록을 가져옵니다.</summary>
        public IReadOnlyList<MapSegment> ActiveSegments => _activeSegments;

        /// <summary>세그먼트 선택 순서를 재현하는 테스트용 난수 시드를 가져옵니다.</summary>
        public int TestSeed => _stageConfig == null
            ? default
            : _stageConfig.RandomSeed;

        /// <summary>현재 적용 중인 세그먼트 선택 방식을 가져옵니다.</summary>
        public MapSegmentSelectionMode SelectionMode => _selectionMode;

        /// <summary>맵 반환과 선행 생성을 매 물리 프레임 처리 중인지 여부를 가져옵니다.</summary>
        public bool IsStreaming => _isStreaming;

        /// <summary>현재 스트림에 적용된 스테이지 맵 설정입니다.</summary>
        public StageMapConfigSO StageConfig => _stageConfig;

        private void Awake()
        {
            _segmentPool = _segmentPoolSource as IMapSegmentPool;
            ApplyStageConfigRuntimeState();
            _hasValidReferences = ValidateReferences();
            UpdateScrollSubscription();
        }

        private void OnEnable()
        {
            if (_playerDiedChannel != null)
            {
                _playerDiedChannel.Raised += StopMapMovement;
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
                if (HasFiniteStageReachedEnd())
                {
                    StopMapMovement();
                    _hasReachedStageEnd = true;
                    StageEndReached?.Invoke();
                    return;
                }

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
            if (_playerDiedChannel != null)
            {
                _playerDiedChannel.Raised -= StopMapMovement;
            }

            StopStreaming();
        }

        private void OnDestroy()
        {
            SetScrollSubscription(false);
        }

        /// <summary>스트리밍 시작 전에 공용 게임 씬에서 사용할 스테이지 설정을 적용합니다.</summary>
        public bool SetStageConfig(StageMapConfigSO stageConfig)
        {
            if (stageConfig == null)
            {
                Debug.LogError("Cannot apply a null stage map config.", this);
                return false;
            }

            if (_isStreaming || _activeSegments.Count > default(int))
            {
                Debug.LogError(
                    "Stage map config can only be changed before streaming starts and while no segments are active.",
                    this);
                return false;
            }

            _stageConfig = stageConfig;
            ApplyStageConfigRuntimeState();
            _hasValidReferences = ValidateReferences();
            UpdateScrollSubscription();
            return _hasValidReferences;
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
                ResetSelectionState();
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

        /// <summary>다음 세그먼트 선택 방식을 변경합니다.</summary>
        /// <param name="selectionMode">즉시 적용할 선택 방식입니다.</param>
        public void SetSelectionMode(MapSegmentSelectionMode selectionMode)
        {
            if (_selectionMode == selectionMode &&
                _appliedSelectionMode == selectionMode)
            {
                return;
            }

            _selectionMode = selectionMode;
            _appliedSelectionMode = selectionMode;
            _sequenceExhausted = false;

            if (selectionMode == MapSegmentSelectionMode.Sequence)
            {
                _sequenceIndex = default;
            }
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
            ResetSelectionState();
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
                _stageConfig == null ||
                _stageConfig.SegmentCatalog == null)
            {
                Debug.LogError("MapStreamManager has missing or invalid required references.", this);
                return false;
            }

            if (_stageConfig.SegmentCatalog.Count == default)
            {
                Debug.LogError("MapStreamManager requires at least one catalog entry.", this);
                return false;
            }

            if (!_stageConfig.InfiniteRandom &&
                _stageConfig.Sequence.Count == default)
            {
                Debug.LogError("Sequence mode requires at least one segment type.", this);
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

                if (prefab == null)
                {
                    return _sequenceExhausted;
                }

                if (!TryRentAndPlace(prefab, exitPosition))
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
            SynchronizeSelectionMode();

            return _selectionMode == MapSegmentSelectionMode.Sequence
                ? SelectNextSequentialPrefab()
                : SelectNextRandomPrefab();
        }

        private MapSegment SelectNextRandomPrefab()
        {
            int alternativeCount = default;
            int validCount = default;

            for (int index = default; index < _stageConfig.SegmentCatalog.Count; index++)
            {
                if (!TryGetCatalogPrefab(index, out MapSegment candidate))
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

            for (int index = default; index < _stageConfig.SegmentCatalog.Count; index++)
            {
                if (!TryGetCatalogPrefab(index, out MapSegment candidate) ||
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

        private MapSegment SelectNextSequentialPrefab()
        {
            if (_stageConfig.Sequence.Count == default)
            {
                return null;
            }

            while (_sequenceIndex < _stageConfig.Sequence.Count)
            {
                int candidateIndex = _sequenceIndex;
                _sequenceIndex++;

                if (TryGetCatalogPrefab(
                        _stageConfig.Sequence[candidateIndex],
                        out MapSegment candidate))
                {
                    return candidate;
                }
            }

            if (_stageConfig.ContinueRandomAfterSequence)
            {
                _selectionMode = MapSegmentSelectionMode.Random;
                _appliedSelectionMode = MapSegmentSelectionMode.Random;
                return SelectNextRandomPrefab();
            }

            _sequenceExhausted = true;
            return null;
        }

        private bool TryGetCatalogPrefab(int index, out MapSegment prefab)
        {
            if (_stageConfig.SegmentCatalog.TryGetEntry(
                    index,
                    out _,
                    out GameObject prefabObject) &&
                prefabObject.TryGetComponent(out prefab))
            {
                return true;
            }

            prefab = null;
            return false;
        }

        private bool TryGetCatalogPrefab(MapSegmentType type, out MapSegment prefab)
        {
            if (_stageConfig.SegmentCatalog.TryGetPrefab(
                    type,
                    out GameObject prefabObject) &&
                prefabObject.TryGetComponent(out prefab))
            {
                return true;
            }

            prefab = null;
            return false;
        }

        private void SynchronizeSelectionMode()
        {
            if (_appliedSelectionMode == _selectionMode)
            {
                return;
            }

            _appliedSelectionMode = _selectionMode;

            if (_selectionMode == MapSegmentSelectionMode.Sequence)
            {
                _sequenceIndex = default;
                _sequenceExhausted = false;
            }
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

        private void ResetSelectionState()
        {
            // 같은 테스트 시드로 재시작할 때 난수 선택 순서와 순차 선택 위치를 함께 재현합니다.
            _random = new System.Random(TestSeed);
            _lastSegmentId = null;
            _sequenceIndex = default;
            _sequenceExhausted = false;
            _hasReachedStageEnd = false;
            _appliedSelectionMode = _selectionMode;
        }

        private bool HasFiniteStageReachedEnd()
        {
            if (_hasReachedStageEnd ||
                !_sequenceExhausted ||
                _stageConfig == null ||
                _stageConfig.ContinueRandomAfterSequence ||
                !TryGetLastExitPosition(out Vector3 exitPosition))
            {
                return false;
            }

            return exitPosition.x <= _layoutSettings.StageEndBoundaryX;
        }

        private void StopMapMovement()
        {
            _isStreaming = false;

            if (_scrollController != null)
            {
                _scrollController.StopScrolling();
            }
        }

        private void ApplyStageConfigRuntimeState()
        {
            _selectionMode = _stageConfig != null && _stageConfig.InfiniteRandom
                ? MapSegmentSelectionMode.Random
                : MapSegmentSelectionMode.Sequence;
            ResetSelectionState();
        }

        private void UpdateScrollSubscription()
        {
            SetScrollSubscription(_hasValidReferences);
        }

        private void SetScrollSubscription(bool shouldSubscribe)
        {
            shouldSubscribe &= _scrollController != null;

            if (_isSubscribedToScroll == shouldSubscribe)
            {
                return;
            }

            if (shouldSubscribe)
            {
                _scrollController.AfterScrollStep += ProcessStreamingAfterScroll;
            }
            else if (_scrollController != null)
            {
                _scrollController.AfterScrollStep -= ProcessStreamingAfterScroll;
            }

            _isSubscribedToScroll = shouldSubscribe;
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
