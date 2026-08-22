namespace Game.Core.Flow
{
    /// <summary>
    /// 게임 전체 흐름의 단계를 나타냅니다. 상태 전환은 App 어셈블리의 게임 상태 머신만 수행하며,
    /// 다른 어셈블리는 이벤트 채널로 전달받은 값을 읽기만 합니다.
    /// </summary>
    public enum GameState
    {
        /// <summary>부트스트랩이 시스템 씬을 준비하는 중입니다.</summary>
        Boot = 0,

        /// <summary>타이틀 화면이 표시된 상태입니다.</summary>
        Title = 1,

        /// <summary>스테이지 선택 화면이 표시된 상태입니다.</summary>
        StageSelect = 2,

        /// <summary>씬 전환이 진행 중이며 어떤 입력도 받지 않는 상태입니다.</summary>
        Loading = 3,

        /// <summary>스테이지를 플레이하는 중입니다.</summary>
        Playing = 4,

        /// <summary>플레이 도중 일시정지된 상태입니다.</summary>
        Paused = 5,

        /// <summary>스테이지가 끝나 결과 화면이 표시된 상태입니다.</summary>
        Result = 6,
    }
}
