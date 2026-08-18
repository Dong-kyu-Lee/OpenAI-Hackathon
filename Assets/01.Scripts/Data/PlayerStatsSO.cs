using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Data/Player Stats", fileName = "PlayerStats")]
    public sealed class PlayerStatsSO : ScriptableObject
    {
        [Header("World Scroll")]
        [SerializeField] private float _worldScrollSpeed;

        [Header("Jump")]
        [SerializeField] private float _jumpClearHeight;
        [SerializeField] private float _gravityScale;
        [SerializeField] private Vector2 _groundCheckSize;
        [SerializeField] private LayerMask _groundLayers;

        [Header("Collider")]
        [SerializeField] private float _standingHeight;
        [SerializeField] private float _slidingHeight;
        [SerializeField] private LayerMask _standingBlockerLayers;

        [Header("Health")]
        [SerializeField] private int _maxHealth;
        [SerializeField] private int _passiveDrainAmount;
        [SerializeField] private float _passiveDrainInterval;
        [SerializeField] private int _obstacleDamageMinimum;
        [SerializeField] private int _obstacleDamageMaximum;
        [SerializeField] private float _obstacleHitInvulnerabilityDuration;
        [SerializeField] private LayerMask _obstacleLayers;

        public float WorldScrollSpeed => _worldScrollSpeed;
        public float JumpClearHeight => _jumpClearHeight;
        public float GravityScale => _gravityScale;
        public Vector2 GroundCheckSize => _groundCheckSize;
        public LayerMask GroundLayers => _groundLayers;
        public float StandingHeight => _standingHeight;
        public float SlidingHeight => _slidingHeight;
        public LayerMask StandingBlockerLayers => _standingBlockerLayers;
        public int MaxHealth => _maxHealth;
        public int PassiveDrainAmount => _passiveDrainAmount;
        public float PassiveDrainInterval => _passiveDrainInterval;
        public int ObstacleDamageMinimum => _obstacleDamageMinimum;
        public int ObstacleDamageMaximum => _obstacleDamageMaximum;
        public float ObstacleHitInvulnerabilityDuration => _obstacleHitInvulnerabilityDuration;
        public LayerMask ObstacleLayers => _obstacleLayers;
    }
}
