using UnityEngine;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// 맵 스크롤 컨트롤러가 한 물리 프레임의 동일한 월드 이동량을 적용할 수 있는 대상 계약입니다.
    /// 대상의 등록·해제와 생명주기 관리는 구현체가 아닌 외부 스트림 관리자가 담당합니다.
    /// </summary>
    public interface IMapScrollTarget
    {
        /// <summary>현재 물리 프레임에 적용할 월드 좌표 이동량을 전달합니다.</summary>
        /// <param name="displacement">월드 유닛 단위의 2차원 이동량입니다.</param>
        void ApplyScroll(Vector2 displacement);
    }
}
