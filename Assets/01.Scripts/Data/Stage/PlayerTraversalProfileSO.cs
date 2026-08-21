using UnityEngine;

namespace Game.Data.Stage
{
    /// <summary>
    /// 플레이어 구현을 직접 참조하지 않고 맵의 간격과 통과 가능성을 계산할 수 있도록
    /// 플레이어의 이동 능력과 입력 반응 시간을 제공하는 읽기 전용 데이터입니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerTraversalProfile",
        menuName = "Game/Data/Stage/Player Traversal Profile")]
    public sealed class PlayerTraversalProfileSO : ScriptableObject
    {
        [SerializeField] private Vector2 _colliderSize = new Vector2(0.8f, 1.4f);
        [SerializeField] private float _jumpDuration = 0.8f;
        [SerializeField] private float _maxJumpHeight = 2.2f;
        [SerializeField] private float _maxJumpDistance = 6.4f;
        [SerializeField] private bool _supportsHorizontalMovement = false;
        [SerializeField] private float _horizontalMoveDuration = 0f;
        [SerializeField] private bool _supportsSlide = true;
        [SerializeField] private float _slideDuration = 0.65f;
        [SerializeField] private float _inputDelay = 0.05f;

        /// <summary>플레이어 충돌 영역의 가로·세로 크기를 월드 유닛으로 가져옵니다.</summary>
        public Vector2 ColliderSize => _colliderSize;

        /// <summary>점프 시작부터 착지까지 걸리는 시간을 초 단위로 가져옵니다.</summary>
        public float JumpDuration => _jumpDuration;

        /// <summary>점프로 도달할 수 있는 최대 높이를 월드 유닛으로 가져옵니다.</summary>
        public float MaxJumpHeight => _maxJumpHeight;

        /// <summary>한 번의 점프로 이동할 수 있는 최대 수평 거리를 월드 유닛으로 가져옵니다.</summary>
        public float MaxJumpDistance => _maxJumpDistance;

        /// <summary>플레이어가 직접 수평 이동할 수 있는지 여부를 가져옵니다.</summary>
        public bool SupportsHorizontalMovement => _supportsHorizontalMovement;

        /// <summary>수평 이동 동작에 걸리는 시간을 초 단위로 가져옵니다.</summary>
        public float HorizontalMoveDuration => _horizontalMoveDuration;

        /// <summary>플레이어가 슬라이드 동작을 지원하는지 여부를 가져옵니다.</summary>
        public bool SupportsSlide => _supportsSlide;

        /// <summary>슬라이드 동작의 지속 시간을 초 단위로 가져옵니다.</summary>
        public float SlideDuration => _slideDuration;

        /// <summary>입력 후 행동이 시작될 때까지의 지연 시간을 초 단위로 가져옵니다.</summary>
        public float InputDelay => _inputDelay;
    }
}
