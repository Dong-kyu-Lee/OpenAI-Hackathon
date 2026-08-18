using System;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(menuName = "Game/Events/Int Event Channel", fileName = "IntEventChannel")]
    public sealed class IntEventChannelSO : ScriptableObject
    {
        public event Action<int> Raised;

        public void Raise(int value)
        {
            Raised?.Invoke(value);
        }
    }
}
