using Game.Core.Pooling;
using Game.Data.Stage;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    /// <summary>IceGun 레이가 빈 지면을 가리킬 때 닿은 위치에 1x1 얼음 바닥을 생성합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class IceBridgeBuilder : MonoBehaviour
    {
        private const float BridgeSize = 1f;
        private const float OverlapInset = 0.1f;

        [SerializeField] private IceBridge _bridgePrefab;
        [SerializeField] private MapScrollController _scrollController;
        [SerializeField] private MapLayoutSettingsSO _layoutSettings;
        [SerializeField] private LayerMask _surfaceLayers;
        [SerializeField, Min(BridgeSize)] private float _maximumGapWidth = 12f;

        /// <summary>레이가 빈 지면을 통과하면 해당 위치를 중심으로 1x1 얼음 바닥을 생성합니다.</summary>
        public bool TryBuild(Vector2 origin, Vector2 direction, float availableDistance)
        {
            if (_bridgePrefab == null ||
                _scrollController == null ||
                _layoutSettings == null ||
                direction.y >= -Mathf.Epsilon)
            {
                return false;
            }

            float distanceToGround =
                (_layoutSettings.GroundHeight - origin.y) / direction.y;

            if (distanceToGround <= 0f || distanceToGround > availableDistance)
            {
                return false;
            }

            Vector2 gapPoint = origin + direction * distanceToGround;
            float probeY = _layoutSettings.GroundHeight - BridgeSize * 0.5f;
            Vector2 probePoint = new(gapPoint.x, probeY);

            if (Physics2D.OverlapPoint(probePoint, _surfaceLayers) != null)
            {
                return false;
            }

            RaycastHit2D leftSurface = Physics2D.Raycast(
                probePoint,
                Vector2.left,
                _maximumGapWidth,
                _surfaceLayers);
            RaycastHit2D rightSurface = Physics2D.Raycast(
                probePoint,
                Vector2.right,
                _maximumGapWidth,
                _surfaceLayers);

            if (leftSurface.collider == null || rightSurface.collider == null)
            {
                return false;
            }

            float leftBoundaryX = leftSurface.point.x;
            float cellIndex = Mathf.Floor(
                (gapPoint.x - leftBoundaryX) / BridgeSize);
            float snappedCenterX =
                leftBoundaryX + (cellIndex + 0.5f) * BridgeSize;
            Vector2 center = new(
                snappedCenterX,
                probeY);
            Vector2 overlapSize = Vector2.one * (BridgeSize - OverlapInset);

            if (Physics2D.OverlapBox(center, overlapSize, 0f, _surfaceLayers) != null)
            {
                return false;
            }

            ObjectPoolManager poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("ObjectPoolManager is not available.", this);
                return false;
            }

            IceBridge bridge = poolManager.Spawn(
                _bridgePrefab,
                center,
                Quaternion.identity);
            if (bridge == null)
            {
                return false;
            }

            bridge.Initialize(
                Vector2.one * BridgeSize,
                _scrollController,
                _layoutSettings.DespawnBoundaryX);
            return true;
        }
    }
}
