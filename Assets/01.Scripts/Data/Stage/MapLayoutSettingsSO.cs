using UnityEngine;

namespace Game.Data.Stage
{
    /// <summary>
    /// 맵 세그먼트의 수직 배치 기준과 반환·선행 생성 경계를 월드 좌표로 제공하는
    /// 스트리밍 레이아웃 데이터입니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MapLayoutSettings",
        menuName = "Game/Data/Stage/Map Layout Settings")]
    public sealed class MapLayoutSettingsSO : ScriptableObject
    {
        [SerializeField] private float _groundHeight = 0f;
        [SerializeField] private float _despawnBoundaryX = -12f;
        [SerializeField] private float _preloadDistance = 32f;
        [Tooltip("유한 Sequence의 마지막 출구가 이 X 좌표에 도달하면 맵 이동을 멈춥니다.")]
        [SerializeField] private float _stageEndBoundaryX;

        [Header("Background")]
        [Tooltip("배경 이미지의 오른쪽 끝이 이 X 좌표를 지나면 반대편 배경 뒤로 이동시킵니다. 카메라 왼쪽 끝보다 살짝 바깥에 둡니다.")]
        [SerializeField] private float _backgroundRecycleBoundaryX = -10f;
        [Tooltip("배경이 항상 덮고 있어야 하는 오른쪽 X 한계입니다. 카메라 오른쪽 끝보다 살짝 바깥에 둡니다.")]
        [SerializeField] private float _backgroundCoverageBoundaryX = 10f;

        /// <summary>세그먼트 진입 앵커를 배치할 월드 Y 좌표를 월드 유닛으로 가져옵니다.</summary>
        public float GroundHeight => _groundHeight;

        /// <summary>세그먼트를 풀로 반환하기 시작하는 월드 X 경계를 월드 유닛으로 가져옵니다.</summary>
        public float DespawnBoundaryX => _despawnBoundaryX;

        /// <summary>세그먼트의 마지막 출구가 도달해야 하는 월드 X 선행 생성 경계를 월드 유닛으로 가져옵니다.</summary>
        public float PreloadDistance => _preloadDistance;

        /// <summary>유한 스테이지의 마지막 출구가 도달해야 하는 종료 X 좌표입니다.</summary>
        public float StageEndBoundaryX => _stageEndBoundaryX;

        /// <summary>배경 이미지를 반대편 뒤로 이동시키는 월드 X 경계를 월드 유닛으로 가져옵니다.</summary>
        public float BackgroundRecycleBoundaryX => _backgroundRecycleBoundaryX;

        /// <summary>배경 이미지가 끊김 없이 덮어야 하는 오른쪽 월드 X 한계를 월드 유닛으로 가져옵니다.</summary>
        public float BackgroundCoverageBoundaryX => _backgroundCoverageBoundaryX;
    }
}
