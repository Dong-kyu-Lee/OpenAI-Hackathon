using UnityEngine;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// 한 맵 세그먼트의 연결 앵커, 콘텐츠 루트와 길이를 제공하고 Rigidbody2D를 통해
    /// 스크롤 이동을 적용합니다. 풀 대여·반환과 스크롤 대상 등록은 외부 관리자가 담당합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MapSegment : MonoBehaviour, IMapScrollTarget
    {
        [SerializeField] private string _segmentId;
        [SerializeField] private Transform _entryAnchor;
        [SerializeField] private Transform _exitAnchor;
        [SerializeField] private Transform _groundRoot;
        [SerializeField] private Transform _obstacleRoot;
        [SerializeField] private Transform _pickupRoot;
        [SerializeField] private Transform _decorationRoot;
        [SerializeField] private Transform _despawnBounds;

        private Rigidbody2D _rigidbody;
        private bool _isActive;

        /// <summary>연속 선택 제한과 디버깅에 사용하는 세그먼트 종류 ID를 가져옵니다.</summary>
        public string SegmentId => _segmentId;

        /// <summary>이전 세그먼트의 출구와 맞출 진입 앵커를 가져옵니다.</summary>
        public Transform EntryAnchor => _entryAnchor;

        /// <summary>다음 세그먼트의 진입점이 연결될 출구 앵커를 가져옵니다.</summary>
        public Transform ExitAnchor => _exitAnchor;

        /// <summary>바닥 콘텐츠를 배치하는 루트를 가져옵니다.</summary>
        public Transform GroundRoot => _groundRoot;

        /// <summary>장애물 콘텐츠를 배치하는 루트를 가져옵니다.</summary>
        public Transform ObstacleRoot => _obstacleRoot;

        /// <summary>아이템 콘텐츠를 배치하는 루트를 가져옵니다.</summary>
        public Transform PickupRoot => _pickupRoot;

        /// <summary>장식 콘텐츠를 배치하는 루트를 가져옵니다.</summary>
        public Transform DecorationRoot => _decorationRoot;

        /// <summary>풀 반환 경계 통과 여부를 판단할 Transform을 가져옵니다.</summary>
        public Transform DespawnBounds => _despawnBounds;

        /// <summary>세그먼트가 스크롤 이동을 받을 수 있도록 활성화되었는지 여부를 가져옵니다.</summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// 진입 앵커부터 출구 앵커까지의 로컬 X 거리를 월드 유닛 기준으로 가져옵니다.
        /// 앵커가 없거나 출구가 진입점보다 앞서지 않으면 0을 반환합니다.
        /// </summary>
        public float Length
        {
            get
            {
                if (_entryAnchor == null || _exitAnchor == null)
                {
                    return default;
                }

                float localLength = _exitAnchor.localPosition.x - _entryAnchor.localPosition.x;
                return Mathf.Max(default, localLength);
            }
        }

        private void Awake()
        {
            if (!TryGetComponent(out _rigidbody))
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            // 풀에서 GameObject가 먼저 재활성화되어도 이전 대여의 속도와 활성 상태가
            // 다음 배치에 남지 않도록 명시적인 Activate 호출 전 상태를 초기화합니다.
            _isActive = false;
            ResetRuntimeState();
        }

        /// <summary>잔류 물리 속도를 초기화하고 세그먼트가 스크롤 이동을 받도록 활성화합니다.</summary>
        public void Activate()
        {
            ResetRuntimeState();
            _isActive = true;
        }

        /// <summary>세그먼트의 스크롤 이동을 막고 반환을 위해 잔류 물리 속도를 초기화합니다.</summary>
        public void Deactivate()
        {
            _isActive = false;
            ResetRuntimeState();
        }

        /// <summary>
        /// 풀에서 대여한 세그먼트 루트를 지정한 월드 위치에 즉시 배치합니다.
        /// 생성 프레임부터 Transform과 물리 위치가 같아야 하므로 호출자는 활성화와 스크롤 등록 전에 사용해야 합니다.
        /// </summary>
        /// <param name="worldPosition">세그먼트 루트가 배치될 월드 위치입니다.</param>
        internal void SetWorldPositionForPlacement(Vector3 worldPosition)
        {
            transform.position = worldPosition;

            if (_rigidbody == null)
            {
                return;
            }

            // MovePosition은 다음 물리 갱신에 반영되므로 생성 프레임에는 물리 위치를 직접 맞춰
            // 새 세그먼트가 기존 세그먼트와 동일한 스크롤 이동량을 받도록 합니다.
            _rigidbody.position = worldPosition;
        }

        /// <summary>
        /// 보간된 Transform 표시 위치와 무관하게 현재 물리 위치를 기준으로 진입 앵커의 월드 위치를 조회합니다.
        /// </summary>
        /// <param name="entryPosition">조회에 성공하면 Rigidbody2D 위치에 루트 기준 앵커 오프셋을 더한 월드 위치입니다.</param>
        /// <returns>Rigidbody2D와 진입 앵커가 모두 유효하면 <see langword="true"/>입니다.</returns>
        internal bool TryGetPhysicsEntryPosition(out Vector3 entryPosition)
        {
            return TryGetPhysicsAnchorPosition(_entryAnchor, out entryPosition);
        }

        /// <summary>
        /// 보간된 Transform 표시 위치와 무관하게 현재 물리 위치를 기준으로 출구 앵커의 월드 위치를 조회합니다.
        /// </summary>
        /// <param name="exitPosition">조회에 성공하면 Rigidbody2D 위치에 루트 기준 앵커 오프셋을 더한 월드 위치입니다.</param>
        /// <returns>Rigidbody2D와 출구 앵커가 모두 유효하면 <see langword="true"/>입니다.</returns>
        internal bool TryGetPhysicsExitPosition(out Vector3 exitPosition)
        {
            return TryGetPhysicsAnchorPosition(_exitAnchor, out exitPosition);
        }

        /// <summary>활성 세그먼트를 Rigidbody2D의 현재 위치에서 지정한 만큼 이동시킵니다.</summary>
        /// <param name="displacement">현재 물리 프레임에 적용할 월드 유닛 단위의 이동량입니다.</param>
        public void ApplyScroll(Vector2 displacement)
        {
            if (!_isActive || _rigidbody == null)
            {
                return;
            }

            _rigidbody.MovePosition(_rigidbody.position + displacement);
        }

        private bool TryGetPhysicsAnchorPosition(Transform anchor, out Vector3 anchorPosition)
        {
            if (_rigidbody == null || anchor == null)
            {
                anchorPosition = default;
                return false;
            }

            // 부모 보간으로 이동한 루트와 앵커의 차이를 사용하면 표시용 Transform 이동량은 상쇄되고,
            // 실제 물리 위치에 회전·스케일이 반영된 앵커 오프셋만 더할 수 있습니다.
            Vector3 anchorOffset = anchor.position - transform.position;
            Vector2 physicsPosition = _rigidbody.position;
            anchorPosition = new Vector3(
                physicsPosition.x + anchorOffset.x,
                physicsPosition.y + anchorOffset.y,
                transform.position.z + anchorOffset.z);
            return true;
        }

        private void ResetRuntimeState()
        {
            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = default;
        }
    }
}
