using System;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(menuName = "Game/Events/Void Event Channel", fileName = "VoidEventChannel")]
    public sealed class VoidEventChannelSO : ScriptableObject
    {
        public event Action Raised;

        public void Raise()
        {
            Raised?.Invoke();
        }
    }
}
