using System;
using Game.Core.Ranking;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(
        fileName = "RankingSubmissionResultEventChannel",
        menuName = "Game/Events/Ranking Submission Result Event Channel")]
    public sealed class RankingSubmissionResultEventChannelSO : ScriptableObject
    {
        public event Action<RankingSubmissionResult> Raised;

        public void Raise(RankingSubmissionResult result)
        {
            Raised?.Invoke(result);
        }
    }
}
