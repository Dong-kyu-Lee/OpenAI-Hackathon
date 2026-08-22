using System;
using Game.Core.Flow;
using UnityEngine;

namespace Game.Core.Events
{
    /// <summary>
    /// 스테이지가 클리어 또는 실패로 종료됐음을 결과와 함께 알리는 채널입니다.
    /// 발신은 스테이지 씬의 진행 담당자가 하고, 상태 머신과 결과 화면이 각각 구독합니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StageResultEventChannel",
        menuName = "Game/Events/Stage Result Event Channel")]
    public sealed class StageResultEventChannelSO : ScriptableObject
    {
        public event Action<StageResult> Raised;

        public void Raise(StageResult value)
        {
            Raised?.Invoke(value);
        }
    }
}
