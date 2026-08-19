using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Sandbox.Stage
{
    /// <summary>
    /// 샌드박스에서 맵 런타임을 재시작 상태로 준비한 뒤 자동으로 스트리밍을 시작합니다.
    /// 실제 게임 시작 흐름을 대신하지 않으며, MapRuntime 동작을 Play Mode에서 빠르게 확인하는 용도로만 사용합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapRuntimeTestStarter : MonoBehaviour
    {
        private MapStreamManager _streamManager;

        private void Awake()
        {
            if (TryGetComponent(out _streamManager))
            {
                return;
            }

            Debug.LogError(
                "MapRuntimeTestStarter requires a MapStreamManager on the same GameObject.",
                this);
            enabled = false;
        }

        private void Start()
        {
            // 재시작 준비가 초기 세그먼트와 난수 순서를 확정한 뒤에만 이동을 시작해야 테스트 결과를 재현할 수 있습니다.
            if (!_streamManager.ResetForRestart())
            {
                Debug.LogError("MapRuntimeTestStarter failed to reset the map runtime for restart.", this);
                enabled = false;
                return;
            }

            if (_streamManager.StartStreaming())
            {
                return;
            }

            Debug.LogError("MapRuntimeTestStarter failed to start map streaming.", this);
            enabled = false;
        }

        private void OnDisable()
        {
            if (_streamManager == null)
            {
                return;
            }

            // 테스트 컴포넌트가 꺼질 때 활성 세그먼트와 스크롤을 함께 정리해 다음 실행이 깨끗한 상태에서 시작되게 합니다.
            _streamManager.StopStreaming();
        }
    }
}
