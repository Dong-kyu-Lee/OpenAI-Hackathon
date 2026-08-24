using Game.Core.Ranking;
using TMPro;
using UnityEngine;

namespace Game.UI.Ranking
{
    /// <summary>타이틀 랭킹보드의 한 줄을 표시합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class RankingEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rankLabel;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _distanceLabel;

        public void Bind(int rank, RankingEntry entry)
        {
            if (_rankLabel != null)
            {
                _rankLabel.text = rank.ToString();
            }

            if (_nameLabel != null)
            {
                _nameLabel.text = entry.PlayerName;
            }

            if (_distanceLabel != null)
            {
                _distanceLabel.text = entry.Distance.ToString("N0");
            }
        }
    }
}
