using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.App
{
    /// <summary>
    /// 부트스트랩 씬에만 배치하는 시작 진입점입니다. 시스템 씬을 Additive로 올린 뒤
    /// 이후의 모든 흐름을 <see cref="GameManager"/>에 넘깁니다.
    /// 부트스트랩 씬의 언로드는 자신이 아니라 씬 흐름 담당자가 수행합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string _systemSceneName = "SystemScene";

        private IEnumerator Start()
        {
            if (!SceneManager.GetSceneByName(_systemSceneName).isLoaded)
            {
                if (!Application.CanStreamedLevelBeLoaded(_systemSceneName))
                {
                    Debug.LogError(
                        $"'{_systemSceneName}' 씬을 로드할 수 없습니다. Build Settings의 Scenes In Build에 등록되어 있는지 확인하세요.",
                        this);
                    yield break;
                }

                AsyncOperation operation = SceneManager.LoadSceneAsync(_systemSceneName, LoadSceneMode.Additive);

                while (operation != null && !operation.isDone)
                {
                    yield return null;
                }
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError(
                    $"'{_systemSceneName}' 씬에 GameManager가 없어 흐름을 시작할 수 없습니다.",
                    this);
                yield break;
            }

            GameManager.Instance.BeginFlow(gameObject.scene.name);
        }
    }
}
