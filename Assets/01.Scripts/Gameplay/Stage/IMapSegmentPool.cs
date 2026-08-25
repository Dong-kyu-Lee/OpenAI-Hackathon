using UnityEngine;

namespace Game.Gameplay.Stage
{
    /// <summary>
    /// 맵 스트림 관리자가 세그먼트의 생성 방식에 의존하지 않고 프리팹별 인스턴스를
    /// 대여하고 반환할 수 있게 하는 풀 계약입니다.
    /// </summary>
    public interface IMapSegmentPool
    {
        /// <summary>지정한 원본 프리팹이 이 풀에 등록되어 있는지 확인합니다.</summary>
        /// <param name="prefab">등록 여부를 확인할 원본 프리팹입니다.</param>
        /// <returns>프리팹을 대여할 수 있도록 정의가 등록되어 있으면 <see langword="true"/>입니다.</returns>
        bool IsRegistered(MapSegment prefab);

        /// <summary>지정한 프리팹의 세그먼트를 풀에서 대여하고 요청한 부모 아래에 배치합니다.</summary>
        /// <param name="prefab">대여할 세그먼트 종류를 식별하는 원본 프리팹입니다.</param>
        /// <param name="parent">대여된 세그먼트의 부모입니다. 구현에 따라 <see langword="null"/>을 기본 부모로 처리할 수 있습니다.</param>
        /// <param name="segment">성공 시 활성화 준비가 끝난 세그먼트이며, 실패 시 <see langword="null"/>입니다.</param>
        /// <returns>등록된 프리팹의 세그먼트를 정상적으로 대여했으면 <see langword="true"/>입니다.</returns>
        bool TryRent(MapSegment prefab, Transform parent, out MapSegment segment);

        /// <summary>대여된 세그먼트를 원래 프리팹 풀로 반환합니다.</summary>
        /// <param name="segment">반환할 세그먼트입니다. 알 수 없는 인스턴스, 중복 반환 또는 <see langword="null"/>은 무시될 수 있습니다.</param>
        void Return(MapSegment segment);
    }
}
