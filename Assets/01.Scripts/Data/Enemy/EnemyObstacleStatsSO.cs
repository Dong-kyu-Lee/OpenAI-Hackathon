using UnityEngine;

namespace Game.Data.Enemy
{
    [CreateAssetMenu(
        fileName = "EnemyObstacleStats",
        menuName = "Game/Data/Enemy/Obstacle Stats")]
    public sealed class EnemyObstacleStatsSO : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _durability = 5f;

        public float Durability => _durability;
    }
}
