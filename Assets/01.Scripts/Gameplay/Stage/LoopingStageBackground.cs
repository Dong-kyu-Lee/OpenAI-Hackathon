using Game.Data.Stage;
using UnityEngine;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// 스테이지별 배경 스프라이트 두 장을 번갈아 재사용해 맵 세그먼트 뒤쪽을 이어지는 배경으로 채웁니다.
    /// 이동량은 맵 스크롤 컨트롤러에서 그대로 전달받아 맵 세그먼트와 항상 같은 속도로 흐릅니다.
    /// 스크롤 속도 계산과 스테이지 설정 선택은 담당하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoopingStageBackground : MonoBehaviour, IMapScrollTarget
    {
        [SerializeField] private MapStreamManager _streamManager;
        [SerializeField] private MapScrollController _scrollController;
        [SerializeField] private MapLayoutSettingsSO _layoutSettings;
        [SerializeField] private SpriteRenderer _backgroundA;
        [SerializeField] private SpriteRenderer _backgroundB;

        private StageMapConfigSO _appliedStageConfig;
        private bool _hasValidReferences;
        private bool _isInitialized;
        private bool _isRegistered;

        private void Awake()
        {
            _hasValidReferences = ValidateReferences();
        }

        private void OnEnable()
        {
            RegisterScrollTarget();
        }

        private void OnDisable()
        {
            UnregisterScrollTarget();
        }

        /// <summary>현재 물리 프레임의 이동량을 두 배경에 적용하고 경계를 지난 배경을 반대편 뒤로 이어 붙입니다.</summary>
        /// <param name="displacement">맵 세그먼트와 동일한 월드 유닛 이동량입니다.</param>
        public void ApplyScroll(Vector2 displacement)
        {
            // 스테이지 설정은 다른 컴포넌트가 Awake 이후에도 교체할 수 있으므로 이동 직전에 갱신 여부를 확인합니다.
            RefreshStageBackgroundIfChanged();

            if (!_isInitialized)
            {
                return;
            }

            MoveRenderer(_backgroundA, displacement);
            MoveRenderer(_backgroundB, displacement);
            RecycleExpiredBackgrounds();
        }

        private bool ValidateReferences()
        {
            if (_streamManager == null ||
                _scrollController == null ||
                _layoutSettings == null ||
                _backgroundA == null ||
                _backgroundB == null)
            {
                Debug.LogError("LoopingStageBackground has missing required references.", this);
                return false;
            }

            return true;
        }

        private void RefreshStageBackgroundIfChanged()
        {
            if (!_hasValidReferences)
            {
                return;
            }

            StageMapConfigSO stageConfig = _streamManager.StageConfig;

            if (ReferenceEquals(stageConfig, _appliedStageConfig))
            {
                return;
            }

            // 실패한 설정도 적용 대상으로 기록해 같은 오류 로그가 매 물리 프레임 반복되지 않게 합니다.
            _appliedStageConfig = stageConfig;
            _isInitialized = TryApplyStageBackground(stageConfig);
        }

        private bool TryApplyStageBackground(StageMapConfigSO stageConfig)
        {
            if (stageConfig == null ||
                stageConfig.BackgroundA == null ||
                stageConfig.BackgroundB == null)
            {
                Debug.LogError(
                    "The current stage map config requires both background sprites.",
                    this);
                return false;
            }

            _backgroundA.sprite = stageConfig.BackgroundA;
            _backgroundB.sprite = stageConfig.BackgroundB;

            if (_backgroundA.bounds.size.x <= default(float) ||
                _backgroundB.bounds.size.x <= default(float))
            {
                Debug.LogError("Looping background sprites must have a positive world width.", this);
                return false;
            }

            WarnIfCoverageIsInsufficient();

            // 첫 배경의 왼쪽 끝을 반환 경계에 맞춰, 시작 순간 카메라 왼쪽에 빈 공간이 생기지 않게 합니다.
            AlignRendererLeftEdge(_backgroundA, _layoutSettings.BackgroundRecycleBoundaryX);
            PlaceRendererAfter(_backgroundB, _backgroundA);
            return true;
        }

        private void WarnIfCoverageIsInsufficient()
        {
            float requiredWidth =
                _layoutSettings.BackgroundCoverageBoundaryX - _layoutSettings.BackgroundRecycleBoundaryX;
            float totalWidth = _backgroundA.bounds.size.x + _backgroundB.bounds.size.x;

            if (totalWidth >= requiredWidth)
            {
                return;
            }

            // 반환 직후에는 두 장만으로 화면을 덮어야 하므로, 폭 합계가 모자라면 반드시 빈 공간이 드러납니다.
            Debug.LogError(
                $"Looping background sprites are too narrow. Required total width is {requiredWidth} " +
                $"world units but the current total is {totalWidth}.",
                this);
        }

        private void RecycleExpiredBackgrounds()
        {
            SpriteRenderer leftRenderer = GetLeftRenderer();
            SpriteRenderer rightRenderer = GetOtherRenderer(leftRenderer);

            while (leftRenderer.bounds.max.x <= _layoutSettings.BackgroundRecycleBoundaryX)
            {
                PlaceRendererAfter(leftRenderer, rightRenderer);

                SpriteRenderer previousRightRenderer = rightRenderer;
                rightRenderer = leftRenderer;
                leftRenderer = previousRightRenderer;
            }
        }

        private SpriteRenderer GetLeftRenderer()
        {
            return _backgroundA.bounds.min.x <= _backgroundB.bounds.min.x
                ? _backgroundA
                : _backgroundB;
        }

        private SpriteRenderer GetOtherRenderer(SpriteRenderer renderer)
        {
            return renderer == _backgroundA ? _backgroundB : _backgroundA;
        }

        private static void MoveRenderer(SpriteRenderer renderer, Vector2 displacement)
        {
            renderer.transform.position += (Vector3)displacement;
        }

        private static void AlignRendererLeftEdge(SpriteRenderer rendererToMove, float leftEdgeX)
        {
            float offsetX = leftEdgeX - rendererToMove.bounds.min.x;
            rendererToMove.transform.position += Vector3.right * offsetX;
        }

        private static void PlaceRendererAfter(
            SpriteRenderer rendererToMove,
            SpriteRenderer rightRenderer)
        {
            float offsetX = rightRenderer.bounds.max.x - rendererToMove.bounds.min.x;
            rendererToMove.transform.position += Vector3.right * offsetX;
        }

        private void RegisterScrollTarget()
        {
            if (_isRegistered || !_hasValidReferences)
            {
                return;
            }

            _scrollController.RegisterTarget(this);
            _isRegistered = true;
        }

        private void UnregisterScrollTarget()
        {
            if (!_isRegistered)
            {
                return;
            }

            if (_scrollController != null)
            {
                _scrollController.UnregisterTarget(this);
            }

            _isRegistered = false;
        }
    }
}
