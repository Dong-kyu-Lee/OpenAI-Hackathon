using System.Collections.Generic;
using Game.Core.Events;
using Game.Data.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.StageSelect
{
    /// <summary>
    /// 스테이지 카탈로그를 목록 항목으로 펼쳐 보여주고, 뒤로가기 입력을 흐름 요청 채널로 전달합니다.
    /// 어떤 스테이지가 선택됐는지는 각 항목이 직접 채널로 알립니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageSelectUI : MonoBehaviour
    {
        [SerializeField] private StageCatalogSO _stageCatalog;
        [SerializeField] private StageEntryButton _entryPrefab;
        [SerializeField] private Transform _entryParent;
        [SerializeField] private Button _backButton;
        [SerializeField] private StageRequestEventChannelSO _stageRequestedChannel;
        [SerializeField] private VoidEventChannelSO _titleRequestedChannel;

        private readonly List<StageEntryButton> _entries = new();

        private void Start()
        {
            BuildEntries();
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

        private void BuildEntries()
        {
            if (_stageCatalog == null || _entryPrefab == null || _entryParent == null)
            {
                Debug.LogError("스테이지 목록을 구성할 참조가 비어 있습니다.", this);
                return;
            }

            for (int index = 0; index < _stageCatalog.Count; index++)
            {
                if (!_stageCatalog.TryGet(index, out StageDefinitionSO definition))
                {
                    Debug.LogError($"카탈로그의 {index}번 항목이 비어 있어 건너뜁니다.", this);
                    continue;
                }

                StageEntryButton entry = Instantiate(_entryPrefab, _entryParent);
                entry.Bind(definition, index, _stageRequestedChannel);
                _entries.Add(entry);
            }
        }

        private void RequestTitle()
        {
            _titleRequestedChannel?.Raise();
        }
    }
}
