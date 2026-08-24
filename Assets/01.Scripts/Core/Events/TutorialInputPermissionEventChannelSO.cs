using System;
using Game.Core.Tutorial;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(fileName = "TutorialInputPermissionEventChannel", menuName = "Game/Events/Tutorial Input Permission Event Channel")]
    public sealed class TutorialInputPermissionEventChannelSO : ScriptableObject
    {
        public event Action<TutorialInputPermission> Raised;

        public bool HasValue { get; private set; }
        public TutorialInputPermission CurrentValue { get; private set; } = TutorialInputPermission.All;

        public void Raise(TutorialInputPermission value)
        {
            CurrentValue = value;
            HasValue = true;
            Raised?.Invoke(value);
        }
    }
}
