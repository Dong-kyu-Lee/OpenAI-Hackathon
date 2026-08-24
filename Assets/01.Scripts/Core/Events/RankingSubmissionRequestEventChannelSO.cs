using System;
using Game.Core.Ranking;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(
        fileName = "RankingSubmissionRequestEventChannel",
        menuName = "Game/Events/Ranking Submission Request Event Channel")]
    public sealed class RankingSubmissionRequestEventChannelSO : ScriptableObject
    {
        public event Action<RankingSubmissionRequest> Raised;

        public void Raise(RankingSubmissionRequest request)
        {
            Raised?.Invoke(request);
        }
    }
}
