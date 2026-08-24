using System.Threading;
using System.Threading.Tasks;

namespace Game.Core.Ranking
{
    /// <summary>
    /// 랭킹 데이터 공급자의 경계입니다. 로컬 저장소와 외부 SDK 어댑터가 같은 계약을 구현합니다.
    /// </summary>
    public interface IRankingRepository
    {
        Task<RankingSnapshot> GetEntriesAsync(
            string boardId,
            int maxEntries,
            CancellationToken cancellationToken);

        Task<RankingSubmissionResult> SubmitAsync(
            string boardId,
            string playerName,
            int distance,
            string guestPrefix,
            int maxEntries,
            CancellationToken cancellationToken);
    }
}
