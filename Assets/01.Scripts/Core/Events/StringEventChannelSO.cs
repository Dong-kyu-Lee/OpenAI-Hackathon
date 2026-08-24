using System;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(menuName = "Game/Events/String Event Channel", fileName = "StringEventChannel")]
    public sealed class StringEventChannelSO : ScriptableObject
    {
        public event Action<string> Raised;

        public void Raise(string value)
        {
            Raised?.Invoke(value);
        }
    }
}
