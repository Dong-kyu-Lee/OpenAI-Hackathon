using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Player
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PlayerSlide : MonoBehaviour
    {
        [SerializeField] private PlayerStatsSO _stats;

        private BoxCollider2D _boxCollider;
        private Vector2 _standingSize;
        private Vector2 _standingOffset;
        private float _feetToColliderCenter;
        private bool _slideRequested;

        public bool IsSliding { get; private set; }

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider2D>();
            _feetToColliderCenter = _boxCollider.offset.y - (_boxCollider.size.y * 0.5f);
            ApplyStandingCollider();
        }

        private void FixedUpdate()
        {
            if (_slideRequested)
            {
                ApplySlidingCollider();
                return;
            }

            if (IsSliding && HasStandingClearance())
            {
                ApplyStandingCollider();
            }
        }

        public void SetSlideRequested(bool isRequested)
        {
            _slideRequested = isRequested;
        }

        private void ApplyStandingCollider()
        {
            SetColliderHeight(_stats.StandingHeight);
            _standingSize = _boxCollider.size;
            _standingOffset = _boxCollider.offset;
            IsSliding = false;
        }

        private void ApplySlidingCollider()
        {
            if (IsSliding)
            {
                return;
            }

            SetColliderHeight(_stats.SlidingHeight);
            IsSliding = true;
        }

        private void SetColliderHeight(float height)
        {
            Vector2 size = _boxCollider.size;
            size.y = height;
            _boxCollider.size = size;

            Vector2 offset = _boxCollider.offset;
            offset.y = _feetToColliderCenter + (height * 0.5f);
            _boxCollider.offset = offset;
        }

        private bool HasStandingClearance()
        {
            Collider2D blocker = Physics2D.OverlapBox(
                transform.TransformPoint(_standingOffset),
                _standingSize,
                0f,
                _stats.StandingBlockerLayers);

            return blocker == null;
        }
    }
}
