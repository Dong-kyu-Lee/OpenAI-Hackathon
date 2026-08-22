namespace Game.Core.Flow
{
    /// <summary>
    /// 종료된 스테이지 1회 플레이의 요약 결과입니다.
    /// 스테이지 식별 정보는 담지 않으며, 표시에 필요한 수치만 전달합니다.
    /// </summary>
    public readonly struct StageResult
    {
        /// <summary>클리어 조건을 충족해 종료했는지 여부를 가져옵니다.</summary>
        public bool Cleared { get; }

        /// <summary>플레이 시작부터 종료까지 걸린 시간을 초 단위로 가져옵니다. 일시정지 시간은 포함되지 않습니다.</summary>
        public float ElapsedTime { get; }

        /// <summary>종료 시점까지 누적된 주행 거리를 월드 유닛으로 가져옵니다.</summary>
        public float Distance { get; }

        /// <summary>종료 시점에 마지막으로 통지된 플레이어 체력을 가져옵니다.</summary>
        public int RemainingHealth { get; }

        /// <summary>스테이지 1회 플레이의 결과를 생성합니다.</summary>
        /// <param name="cleared">클리어 조건 충족 여부입니다.</param>
        /// <param name="elapsedTime">플레이에 걸린 시간(초)입니다.</param>
        /// <param name="distance">누적 주행 거리(월드 유닛)입니다.</param>
        /// <param name="remainingHealth">종료 시점의 플레이어 체력입니다.</param>
        public StageResult(bool cleared, float elapsedTime, float distance, int remainingHealth)
        {
            Cleared = cleared;
            ElapsedTime = elapsedTime;
            Distance = distance;
            RemainingHealth = remainingHealth;
        }
    }
}
