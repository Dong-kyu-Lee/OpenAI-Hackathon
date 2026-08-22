using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.App
{
    /// <summary>
    /// 흐름 씬(타이틀, 스테이지 선택), 스테이지 씬, 게임플레이 UI 씬의 Additive 로드와 언로드를 담당합니다.
    /// 어떤 상태에서 어떤 전환을 호출할지는 판단하지 않으며, 요청받은 씬 구성만 만들어 냅니다.
    /// 시스템 씬은 어떤 경로로도 언로드하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneFlowController : MonoBehaviour
    {
        [SerializeField] private string _uiSceneName = "UI_Scene";

        private string _currentFlowSceneName;
        private string _currentStageSceneName;

        /// <summary>씬 전환이 진행 중인지 여부를 가져옵니다. 진행 중에는 새 전환 요청이 무시됩니다.</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>스테이지 씬과 게임플레이 UI 씬이 올라와 있는지 여부를 가져옵니다.</summary>
        public bool IsStageLoaded => !string.IsNullOrEmpty(_currentStageSceneName);

        /// <summary>
        /// 이미 로드된 채로 시작한 첫 흐름 씬을 등록합니다.
        /// 부트스트랩 씬이 다음 전환에서 정상적으로 언로드되게 하려면 반드시 호출해야 합니다.
        /// </summary>
        /// <param name="sceneName">현재 올라와 있는 흐름 씬의 이름입니다.</param>
        public void SetInitialFlowScene(string sceneName)
        {
            _currentFlowSceneName = sceneName;
        }

        /// <summary>현재 흐름 씬을 언로드하고 새 흐름 씬으로 교체합니다.</summary>
        /// <param name="sceneName">새로 로드할 흐름 씬의 이름입니다.</param>
        /// <param name="onCompleted">전환이 끝난 뒤 호출할 콜백입니다.</param>
        public void SwitchFlowScene(string sceneName, Action onCompleted)
        {
            if (!TryBeginTransition(sceneName))
            {
                return;
            }

            StartCoroutine(SwitchFlowSceneRoutine(sceneName, onCompleted));
        }

        /// <summary>현재 흐름 씬을 언로드하고 스테이지 씬과 게임플레이 UI 씬을 로드합니다.</summary>
        /// <param name="stageSceneName">로드할 스테이지 씬의 이름입니다.</param>
        /// <param name="onCompleted">전환이 끝난 뒤 호출할 콜백입니다.</param>
        public void EnterStage(string stageSceneName, Action onCompleted)
        {
            if (!TryBeginTransition(stageSceneName))
            {
                return;
            }

            StartCoroutine(EnterStageRoutine(stageSceneName, onCompleted));
        }

        /// <summary>
        /// 현재 스테이지 씬만 언로드한 뒤 같은 씬을 다시 로드합니다.
        /// 게임플레이 UI 씬과 시스템 씬은 유지되므로 UI 상태는 그대로 남습니다.
        /// </summary>
        /// <param name="onCompleted">전환이 끝난 뒤 호출할 콜백입니다.</param>
        public void ReloadStage(Action onCompleted)
        {
            if (!IsStageLoaded)
            {
                Debug.LogError("로드된 스테이지 씬이 없어 다시 시작할 수 없습니다.", this);
                return;
            }

            if (!TryBeginTransition(_currentStageSceneName))
            {
                return;
            }

            StartCoroutine(ReloadStageRoutine(onCompleted));
        }

        /// <summary>스테이지 씬과 게임플레이 UI 씬을 언로드하고 흐름 씬으로 돌아갑니다.</summary>
        /// <param name="flowSceneName">돌아갈 흐름 씬의 이름입니다.</param>
        /// <param name="onCompleted">전환이 끝난 뒤 호출할 콜백입니다.</param>
        public void ExitStage(string flowSceneName, Action onCompleted)
        {
            if (!TryBeginTransition(flowSceneName))
            {
                return;
            }

            StartCoroutine(ExitStageRoutine(flowSceneName, onCompleted));
        }

        private IEnumerator SwitchFlowSceneRoutine(string sceneName, Action onCompleted)
        {
            // 카메라와 EventSystem이 한 프레임이라도 겹치지 않도록 항상 언로드를 먼저 끝냅니다.
            yield return UnloadSceneRoutine(_currentFlowSceneName);
            _currentFlowSceneName = null;

            yield return LoadSceneRoutine(sceneName);
            _currentFlowSceneName = sceneName;
            TrySetActiveScene(sceneName);

            EndTransition(onCompleted);
        }

        private IEnumerator EnterStageRoutine(string stageSceneName, Action onCompleted)
        {
            yield return UnloadSceneRoutine(_currentFlowSceneName);
            _currentFlowSceneName = null;

            yield return LoadSceneRoutine(stageSceneName);
            _currentStageSceneName = stageSceneName;

            yield return LoadSceneRoutine(_uiSceneName);

            // 이후 생성되는 게임플레이 오브젝트가 스테이지 씬에 속하도록 활성 씬을 지정합니다.
            TrySetActiveScene(stageSceneName);

            EndTransition(onCompleted);
        }

        private IEnumerator ReloadStageRoutine(Action onCompleted)
        {
            string stageSceneName = _currentStageSceneName;

            yield return UnloadSceneRoutine(stageSceneName);
            _currentStageSceneName = null;

            yield return LoadSceneRoutine(stageSceneName);
            _currentStageSceneName = stageSceneName;
            TrySetActiveScene(stageSceneName);

            EndTransition(onCompleted);
        }

        private IEnumerator ExitStageRoutine(string flowSceneName, Action onCompleted)
        {
            yield return UnloadSceneRoutine(_currentStageSceneName);
            _currentStageSceneName = null;

            yield return UnloadSceneRoutine(_uiSceneName);

            yield return LoadSceneRoutine(flowSceneName);
            _currentFlowSceneName = flowSceneName;
            TrySetActiveScene(flowSceneName);

            EndTransition(onCompleted);
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                yield break;
            }

            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"'{sceneName}' 씬을 로드할 수 없습니다. Build Settings의 Scenes In Build에 등록되어 있는지 확인하세요.",
                    this);
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }

        private IEnumerator UnloadSceneRoutine(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                yield break;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                yield break;
            }

            if (scene == gameObject.scene)
            {
                Debug.LogError($"시스템 씬 '{sceneName}'은 언로드할 수 없습니다.", this);
                yield break;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);

            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }

        private void TrySetActiveScene(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);

            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
            }
        }

        private bool TryBeginTransition(string sceneName)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning($"씬 전환 중이라 '{sceneName}' 요청을 무시했습니다.", this);
                return false;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("전환할 씬 이름이 비어 있습니다.", this);
                return false;
            }

            IsTransitioning = true;
            return true;
        }

        private void EndTransition(Action onCompleted)
        {
            IsTransitioning = false;
            onCompleted?.Invoke();
        }
    }
}
