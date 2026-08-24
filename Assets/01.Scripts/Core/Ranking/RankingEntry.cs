namespace Game.Core.Ranking
{
    /// <summary>랭킹보드에 표시할 한 건의 이름과 주행 거리입니다.</summary>
    public readonly struct RankingEntry
    {
        public string PlayerName { get; }
        public int Distance { get; }
        public long SubmissionOrder { get; }

        public RankingEntry(string playerName, int distance, long submissionOrder)
        {
            PlayerName = playerName;
            Distance = distance;
            SubmissionOrder = submissionOrder;
        }
    }
}
