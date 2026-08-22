using System;
using Game.Core.Flow;
using UnityEngine;

namespace Game.Core.Events
{
    /// <summary>
    /// 게임 상태가 바뀔 때마다 새 상태를 방송하는 채널입니다.
    /// 발신은 App의 게임 상태 머신만 담당하고, UI는 구독만 합니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameStateEventChannel",
        menuName = "Game/Events/Game State Event Channel")]
    public sealed class GameStateEventChannelSO : ScriptableObject
    {
        public event Action<GameState> Raised;

        public void Raise(GameState value)
        {
            Raised?.Invoke(value);
        }
    }
}
