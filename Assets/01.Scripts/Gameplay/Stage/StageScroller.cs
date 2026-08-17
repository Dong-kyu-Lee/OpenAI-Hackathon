using Game.Core.Events;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Stage
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class StageScroller : MonoBehaviour
    {
        [SerializeField] private PlayerStatsSO _playerStats;
        [SerializeField] private VoidEventChannelSO _playerDiedChannel;

        private Rigidbody2D _rigidbody;
        private bool _isScrolling;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _isScrolling = true;

            if (_playerDiedChannel != null)
            {
                _playerDiedChannel.Raised += StopScrolling;
            }
        }

        private void OnDisable()
        {
            if (_playerDiedChannel != null)
            {
                _playerDiedChannel.Raised -= StopScrolling;
            }
        }

        private void FixedUpdate()
        {
            if (!_isScrolling)
            {
                return;
            }

            Vector2 distance = Vector2.left * _playerStats.WorldScrollSpeed * Time.fixedDeltaTime;
            _rigidbody.MovePosition(_rigidbody.position + distance);
        }

        private void StopScrolling()
        {
            _isScrolling = false;
        }
    }
}
