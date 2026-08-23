using Game.Core.Pooling;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    /// <summary>끊어진 지면을 임시로 연결하고 맵과 같은 이동량을 적용받는 풀링형 얼음 바닥입니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class IceBridge : MonoBehaviour, IMapScrollTarget, IPoolable
    {
        private Rigidbody2D _rigidbody;
        private MapScrollController _scrollController;
        private float _despawnBoundaryX;
        private float _halfWidth;
        private bool _isInitialized;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (!_isInitialized || _rigidbody.position.x + _halfWidth > _despawnBoundaryX)
            {
                return;
            }

            ReturnToPool();
        }

        /// <summary>얼음 바닥 크기와 스크롤 생명주기를 초기화합니다.</summary>
        public void Initialize(
            Vector2 size,
            MapScrollController scrollController,
            float despawnBoundaryX)
        {
            transform.localScale = new Vector3(size.x, size.y, 1f);
            _halfWidth = size.x * 0.5f;
            _despawnBoundaryX = despawnBoundaryX;
            _scrollController = scrollController;
            _isInitialized = true;
            _scrollController.RegisterTarget(this);
        }

        /// <inheritdoc />
        public void ApplyScroll(Vector2 displacement)
        {
            if (_isInitialized)
            {
                _rigidbody.MovePosition(_rigidbody.position + displacement);
            }
        }

        /// <inheritdoc />
        public void OnDespawned()
        {
            _scrollController?.UnregisterTarget(this);
            _scrollController = null;
            _isInitialized = false;
            _rigidbody.linearVelocity = Vector2.zero;
        }

        private void ReturnToPool()
        {
            ObjectPoolManager poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("ObjectPoolManager is not available.", this);
                enabled = false;
                return;
            }

            poolManager.Return(this);
        }
    }
}