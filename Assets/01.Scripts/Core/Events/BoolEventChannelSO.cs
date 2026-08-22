using System;
using UnityEngine;

namespace Game.Core.Events
{
    /// <summary>
    /// 켜짐과 꺼짐 두 상태를 방송하는 채널입니다.
    /// 매 프레임 흘려보내는 값이 아니라 상태가 바뀌는 순간에만 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "BoolEventChannel", menuName = "Game/Events/Bool Event Channel")]
    public sealed class BoolEventChannelSO : ScriptableObject
    {
        public event Action<bool> Raised;

        public void Raise(bool value)
        {
            Raised?.Invoke(value);
        }
    }
}
