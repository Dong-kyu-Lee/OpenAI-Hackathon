using System;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(fileName = "Vector2EventChannel", menuName = "Game/Events/Vector2 Event Channel")]
    public sealed class Vector2EventChannelSO : ScriptableObject
    {
        public event Action<Vector2> Raised;

        public void Raise(Vector2 value)
        {
            Raised?.Invoke(value);
        }
    }
}
