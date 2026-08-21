using UnityEngine;

namespace Game.Data.Stage
{
    /// <summary>
    /// 맵 스크롤을 시작하거나 재시작할 때 적용할 초기 이동 속도를 제공하는 데이터입니다.
    /// 런타임 속도 변경과 누적 거리 관리는 담당하지 않습니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MapScrollSettings",
        menuName = "Game/Data/Stage/Map Scroll Settings")]
    public sealed class MapScrollSettingsSO : ScriptableObject
    {
        [SerializeField] private float _initialSpeed = 8f;

        /// <summary>초기 맵 이동 속도의 크기를 초당 월드 유닛(units/s)으로 가져옵니다.</summary>
        public float InitialSpeed => _initialSpeed;
    }
}
