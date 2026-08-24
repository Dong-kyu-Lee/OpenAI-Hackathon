using System;
using System.Collections.Generic;

namespace Game.Core.Ranking
{
    /// <summary>랭킹 조회 결과와 조회 시점의 정렬된 항목 목록입니다.</summary>
    public sealed class RankingSnapshot
    {
        private readonly RankingEntry[] _entries;

        public bool Succeeded { get; }
        public string ErrorMessage { get; }
        public IReadOnlyList<RankingEntry> Entries => _entries;

        public RankingSnapshot(bool succeeded, RankingEntry[] entries, string errorMessage = "")
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage ?? string.Empty;
            _entries = entries ?? Array.Empty<RankingEntry>();
        }

        public static RankingSnapshot Failure(string errorMessage)
        {
            return new RankingSnapshot(false, Array.Empty<RankingEntry>(), errorMessage);
        }
    }
}
