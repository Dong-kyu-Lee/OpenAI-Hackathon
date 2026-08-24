using UnityEngine;

namespace Game.Data.Ranking
{
    /// <summary>랭킹 저장 범위와 이름 입력 규칙을 정의합니다.</summary>
    [CreateAssetMenu(
        fileName = "RankingSettings",
        menuName = "Game/Data/Ranking/Ranking Settings")]
    public sealed class RankingSettingsSO : ScriptableObject
    {
        [SerializeField] private string _boardId = "endless";
        [SerializeField, Min(1)] private int _maxEntries = 100;
        [SerializeField, Min(9)] private int _maxNameLength = 12;
        [SerializeField] private string _guestPrefix = "GUEST";

        public string BoardId => _boardId;
        public int MaxEntries => _maxEntries;
        public int MaxNameLength => _maxNameLength;
        public string GuestPrefix => _guestPrefix;
    }
}
