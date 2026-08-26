using Game.Core.Events;
using Game.Core.Pooling;
using Game.Data.Stage;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    [DisallowMultipleComponent]
    public sealed class IceBridgeBuilder : MonoBehaviour
    {
        private const float BridgeSize = 1f;
        
        private const float SurfaceHitTolerance = BridgeSize;
private const float OverlapInset = 0.1f;

        [SerializeField] private IceBridge _bridgePrefab;
        [SerializeField] private MapScrollController _scrollController;
        [SerializeField] private MapLayoutSettingsSO _layoutSettings;
        [SerializeField] private LayerMask _surfaceLayers;
        [SerializeField, Min(BridgeSize)] private float _maximumGapWidth = 12f;
        
        [SerializeField] private SfxEventChannelSO _sfxChannel;
        [SerializeField] private AudioClip _bridgeBuiltClip;
[SerializeField] private Vector2EventChannelSO _bridgeBuiltChannel;

public bool TryBuild(Vector2 origin, Vector2 direction, float availableDistance)
        {
            if (_bridgePrefab == null ||
                _scrollController == null ||
                _layoutSettings == null ||
                direction.y >= -Mathf.Epsilon)
            {
                return false;
            }

            float distanceToGround = (_layoutSettings.GroundHeight - origin.y) / direction.y;
            if (distanceToGround <= 0f ||
                distanceToGround > availableDistance + SurfaceHitTolerance)
            {
                return false;
            }

            Vector2 gapPoint = origin + direction * distanceToGround;
            float probeY = _layoutSettings.GroundHeight - BridgeSize * 0.5f;
            Vector2 probePoint = new(gapPoint.x, probeY);

            if (Physics2D.OverlapPoint(probePoint, _surfaceLayers) != null)
                return false;

            RaycastHit2D leftSurface = Physics2D.Raycast(
                probePoint, Vector2.left, _maximumGapWidth, _surfaceLayers);
            RaycastHit2D rightSurface = Physics2D.Raycast(
                probePoint, Vector2.right, _maximumGapWidth, _surfaceLayers);

            if (leftSurface.collider == null || rightSurface.collider == null)
                return false;

            float leftBoundaryX = leftSurface.point.x;
            float cellIndex = Mathf.Floor((gapPoint.x - leftBoundaryX) / BridgeSize);
            float snappedCenterX = leftBoundaryX + (cellIndex + 0.5f) * BridgeSize;
            Vector2 center = new(snappedCenterX, probeY);
            Vector2 overlapSize = Vector2.one * (BridgeSize - OverlapInset);

            if (Physics2D.OverlapBox(center, overlapSize, 0f, _surfaceLayers) != null)
                return false;

            ObjectPoolManager poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("ObjectPoolManager is not available.", this);
                return false;
            }

            IceBridge bridge = poolManager.Spawn(_bridgePrefab, center, Quaternion.identity);
            if (bridge == null)
                return false;

            bridge.Initialize(Vector2.one * BridgeSize, _scrollController, _layoutSettings.DespawnBoundaryX);
            
            _sfxChannel?.PlayOneShot(_bridgeBuiltClip);
_bridgeBuiltChannel?.Raise(center);
            return true;
        }
    }
}
