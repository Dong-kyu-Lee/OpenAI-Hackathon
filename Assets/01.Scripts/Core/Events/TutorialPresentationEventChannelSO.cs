using System;
using Game.Core.Tutorial;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(
        fileName = "TutorialPresentationEventChannel",
        menuName = "Game/Events/Tutorial Presentation Event Channel")]
    public sealed class TutorialPresentationEventChannelSO : ScriptableObject
    {
        public event Action<TutorialPresentation> Raised;

        public void Raise(TutorialPresentation presentation)
        {
            Raised?.Invoke(presentation);
        }
    }
}
