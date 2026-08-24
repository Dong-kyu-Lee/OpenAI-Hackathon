using System;
using Game.Core.Ranking;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(
        fileName = "RankingSnapshotEventChannel",
        menuName = "Game/Events/Ranking Snapshot Event Channel")]
    public sealed class RankingSnapshotEventChannelSO : ScriptableObject
    {
        public event Action<RankingSnapshot> Raised;

        public void Raise(RankingSnapshot snapshot)
        {
            Raised?.Invoke(snapshot);
        }
    }
}
