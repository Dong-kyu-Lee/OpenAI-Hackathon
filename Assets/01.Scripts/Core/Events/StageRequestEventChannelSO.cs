using System;
using UnityEngine;

namespace Game.Core.Events
{
    /// <summary>
    /// 스테이지 선택 화면이 특정 스테이지의 플레이를 요청할 때 사용하는 채널입니다.
    /// 값은 스테이지 카탈로그 안의 순번이며, 순번을 실제 씬으로 해석하는 것은 수신 측의 책임입니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StageRequestEventChannel",
        menuName = "Game/Events/Stage Request Event Channel")]
    public sealed class StageRequestEventChannelSO : ScriptableObject
    {
        public event Action<int> Raised;

        public void Raise(int stageIndex)
        {
            Raised?.Invoke(stageIndex);
        }
    }
}
