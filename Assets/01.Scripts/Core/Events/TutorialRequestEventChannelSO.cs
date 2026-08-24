using System;
using Game.Core.Tutorial;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(
        fileName = "TutorialRequestEventChannel",
        menuName = "Game/Events/Tutorial Request Event Channel")]
    public sealed class TutorialRequestEventChannelSO : ScriptableObject
    {
        public event Action<TutorialRequest> Raised;

        public void Raise(TutorialRequest request)
        {
            Raised?.Invoke(request);
        }
    }
}
