using System.Collections.Generic;
using Game.Core.Events;
using Game.Data.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.StageSelect
{
    /// <summary>
    /// 씬에 직접 배치된 스테이지 항목들을 카탈로그 순번과 요청 채널에 배선하고,
    /// 뒤로가기 입력을 흐름 요청 채널로 전달합니다.
    /// 항목의 위치와 표시는 씬이, 순번은 카탈로그가 결정합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageSelectUI : MonoBehaviour
    {
        [SerializeField] private StageCatalogSO _stageCatalog;
        [SerializeField] private StageEntryButton[] _entries;
        [SerializeField] private Button _backButton;
        [SerializeField] private StageRequestEventChannelSO _stageRequestedChannel;
        [SerializeField] private VoidEventChannelSO _titleRequestedChannel;

        private void Start()
        {
            BindEntries();
        }

        private void OnEnable()
        {
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(RequestTitle);
            }
        }

        private void OnDisable()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(RequestTitle);
            }
        }

        private void BindEntries()
        {
            if (_stageCatalog == null || _entries == null)
            {
                Debug.LogError("스테이지 항목을 배선할 참조가 비어 있습니다.", this);
                return;
            }

            HashSet<int> boundIndices = new();

            for (int order = 0; order < _entries.Length; order++)
            {
                StageEntryButton entry = _entries[order];
                if (entry == null)
                {
                    Debug.LogError($"{order}번 항목 슬롯이 비어 있어 건너뜁니다.", this);
                    continue;
                }

                StageDefinitionSO definition = entry.Definition;
                if (definition == null)
                {
                    Debug.LogError($"'{entry.name}'에 표시할 스테이지가 지정되지 않았습니다.", entry);
                    continue;
                }

                int stageIndex = _stageCatalog.IndexOf(definition);
                if (stageIndex == StageCatalogSO.InvalidIndex)
                {
                    Debug.LogError(
                        $"'{entry.name}'의 스테이지 '{definition.name}'이(가) 카탈로그에 없습니다.",
                        entry);
                    continue;
                }

                if (!boundIndices.Add(stageIndex))
                {
                    Debug.LogError(
                        $"'{entry.name}'의 스테이지 '{definition.name}'이(가) 중복 배치돼 있습니다.",
                        entry);
                    continue;
                }

                entry.Bind(definition, stageIndex, _stageRequestedChannel);
            }
        }

        private void RequestTitle()
        {
            _titleRequestedChannel?.Raise();
        }
    }
}
