namespace Game.Core.Ranking
{
    /// <summary>종료 화면에서 랭킹 등록을 요청할 때 전달하는 값입니다.</summary>
    public readonly struct RankingSubmissionRequest
    {
        public string PlayerName { get; }
        public float Distance { get; }

        public RankingSubmissionRequest(string playerName, float distance)
        {
            PlayerName = playerName;
            Distance = distance;
        }
    }
}
