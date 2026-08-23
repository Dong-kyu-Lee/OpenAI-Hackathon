using UnityEngine;

namespace Game.Data.Enemy
{
    [CreateAssetMenu(
        fileName = "EnemyIdleMovementSettings",
        menuName = "Game/Data/Enemy/Idle Movement Settings")]
    public sealed class EnemyIdleMovementSettingsSO : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _verticalDistance = 1f;
        [SerializeField, Min(0.01f)] private float _cycleDuration = 2f;

        public float VerticalDistance => _verticalDistance;
        public float CycleDuration => _cycleDuration;
    }
}
