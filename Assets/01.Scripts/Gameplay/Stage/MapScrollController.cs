using System.Collections.Generic;
using Game.Data.Stage;
using UnityEngine;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// 등록된 맵 대상에 물리 프레임별 동일한 월드 이동량을 전달하고 속도, 누적 거리와
    /// 실행·일시정지 상태를 관리합니다. 대상의 생성, 반환과 등록 시점 결정은 담당하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapScrollController : MonoBehaviour
    {
        [SerializeField] private MapScrollSettingsSO _settings;

        /// <summary>기존 대상의 물리 이동을 처리한 직후 실제 이동량과 함께 호출됩니다.</summary>
        internal event System.Action<Vector2> AfterScrollStep;
        internal event System.Action ScrollingStopped;


        private readonly List<IMapScrollTarget> _targets = new();
        private readonly List<IMapScrollTarget> _pendingRegistrations = new();
        private readonly List<IMapScrollTarget> _pendingUnregistrations = new();

        private float _currentSpeed;
        private float _distanceTravelled;
        private bool _isRunning;
        private bool _isPaused;
        private bool _isApplyingScroll;

        /// <summary>현재 맵 이동 속도의 크기를 초당 월드 유닛(units/s)으로 가져옵니다.</summary>
        public float CurrentSpeed => _currentSpeed;

        /// <summary>마지막 재시작 초기화 이후 누적된 이동 거리를 월드 유닛으로 가져옵니다.</summary>
        public float DistanceTravelled => _distanceTravelled;

        /// <summary>스크롤이 시작되어 정지되지 않은 상태인지 여부를 가져옵니다.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>실행 중인 스크롤이 일시정지된 상태인지 여부를 가져옵니다.</summary>
        public bool IsPaused => _isPaused;

        private void Awake()
        {
            ResetForRestart();
        }

        private void FixedUpdate()
        {
            if (!_isRunning || _isPaused)
            {
                return;
            }

            float travelledThisStep = _currentSpeed * Time.fixedDeltaTime;
            Vector2 displacement = Vector2.left * travelledThisStep;

            // 이동 콜백 안에서 대상이 등록 상태를 바꿔도 현재 반복 컬렉션이 변하지 않도록
            // 변경 요청을 보류하고 모든 대상 처리가 끝난 뒤 한 번에 반영합니다.
            _isApplyingScroll = true;

            try
            {
                for (int index = 0; index < _targets.Count; index++)
                {
                    IMapScrollTarget target = _targets[index];

                    if (IsUnavailable(target))
                    {
                        QueueUnregistration(target);
                        continue;
                    }

                    target.ApplyScroll(displacement);
                }
            }
            finally
            {
                _isApplyingScroll = false;
                ApplyPendingTargetChanges();
            }

            _distanceTravelled += travelledThisStep;

            // 기존 대상의 이동이 모두 예약된 뒤 실제 이동량을 전달해,
            // 이 시점에 생성되는 세그먼트가 같은 양만큼 즉시 따라잡게 합니다.
            AfterScrollStep?.Invoke(displacement);
        }

        /// <summary>맵 이동 속도의 크기를 변경하며 음수 입력은 절댓값으로 정규화합니다.</summary>
        /// <param name="speed">초당 월드 유닛(units/s) 단위의 새 속도입니다.</param>
        public void SetSpeed(float speed)
        {
            _currentSpeed = Mathf.Abs(speed);
        }

        /// <summary>현재 속도로 스크롤을 시작하며 기존 일시정지 상태를 해제합니다.</summary>
        public void StartScrolling()
        {
            _isRunning = true;
            _isPaused = false;
        }

                /// <summary>스크롤을 정지하고 일시정지 상태를 해제합니다. 속도와 누적 거리는 유지됩니다.</summary>
public void StopScrolling()
        {
            _isRunning = false;
            _isPaused = false;
            ScrollingStopped?.Invoke();
        }

        /// <summary>실행 중인 스크롤을 일시정지합니다. 실행 중이 아니면 아무 작업도 하지 않습니다.</summary>
        public void Pause()
        {
            if (_isRunning)
            {
                _isPaused = true;
            }
        }

        /// <summary>실행 중인 스크롤의 일시정지를 해제합니다. 실행 중이 아니면 아무 작업도 하지 않습니다.</summary>
        public void Resume()
        {
            if (_isRunning)
            {
                _isPaused = false;
            }
        }

        /// <summary>
        /// 스크롤을 정지하고 설정의 초기 속도와 0 월드 유닛의 누적 거리로 재설정합니다.
        /// 설정이 연결되지 않았다면 초기 속도는 0이 됩니다.
        /// </summary>
        public void ResetForRestart()
        {
            StopScrolling();
            _currentSpeed = _settings == null ? default : Mathf.Abs(_settings.InitialSpeed);
            _distanceTravelled = default;
        }

        /// <summary>매 물리 프레임 동일한 이동량을 받을 대상을 등록합니다.</summary>
        /// <param name="target">등록할 대상입니다. <see langword="null"/>, 파괴된 Unity 오브젝트와 중복 등록은 무시됩니다.</param>
        public void RegisterTarget(IMapScrollTarget target)
        {
            if (IsUnavailable(target))
            {
                return;
            }

            if (_isApplyingScroll)
            {
                _pendingUnregistrations.Remove(target);

                if (!_targets.Contains(target) && !_pendingRegistrations.Contains(target))
                {
                    _pendingRegistrations.Add(target);
                }

                return;
            }

            if (!_targets.Contains(target))
            {
                _targets.Add(target);
            }
        }

        /// <summary>대상이 이후 물리 프레임부터 이동량을 받지 않도록 등록을 해제합니다.</summary>
        /// <param name="target">등록 해제할 대상입니다. <see langword="null"/> 또는 미등록 대상은 무시됩니다.</param>
        public void UnregisterTarget(IMapScrollTarget target)
        {
            if (target is null)
            {
                return;
            }

            if (_isApplyingScroll)
            {
                _pendingRegistrations.Remove(target);
                QueueUnregistration(target);
                return;
            }

            _targets.Remove(target);
        }

        private void QueueUnregistration(IMapScrollTarget target)
        {
            if (_targets.Contains(target) && !_pendingUnregistrations.Contains(target))
            {
                _pendingUnregistrations.Add(target);
            }
        }

        private void ApplyPendingTargetChanges()
        {
            for (int index = 0; index < _pendingUnregistrations.Count; index++)
            {
                _targets.Remove(_pendingUnregistrations[index]);
            }

            _pendingUnregistrations.Clear();

            for (int index = 0; index < _pendingRegistrations.Count; index++)
            {
                IMapScrollTarget target = _pendingRegistrations[index];

                if (!IsUnavailable(target) && !_targets.Contains(target))
                {
                    _targets.Add(target);
                }
            }

            _pendingRegistrations.Clear();
        }


        private static bool IsUnavailable(IMapScrollTarget target)
        {
            return target is null || target is Object unityObject && unityObject == null;
        }
    }

}
