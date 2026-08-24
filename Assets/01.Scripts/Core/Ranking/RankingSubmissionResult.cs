namespace Game.Core.Ranking
{
    /// <summary>랭킹 등록의 성공 여부와 등록 후 최신 목록입니다.</summary>
    public readonly struct RankingSubmissionResult
    {
        public bool Succeeded { get; }
        public string ErrorMessage { get; }
        public RankingEntry SubmittedEntry { get; }
        public RankingSnapshot Snapshot { get; }

        public RankingSubmissionResult(
            bool succeeded,
            string errorMessage,
            RankingEntry submittedEntry,
            RankingSnapshot snapshot)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage ?? string.Empty;
            SubmittedEntry = submittedEntry;
            Snapshot = snapshot;
        }

        public static RankingSubmissionResult Failure(string errorMessage)
        {
            return new RankingSubmissionResult(false, errorMessage, default, null);
        }
    }
}
