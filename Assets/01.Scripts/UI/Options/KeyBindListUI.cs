using System;
using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI.Options
{
    /// <summary>
    /// 다시 할당할 수 있는 액션 목록을 항목으로 펼쳐 보여주고, 변경이 생기면 저장 요청을 채널로 알립니다.
    /// 어떤 액션을 노출할지는 인스펙터에서 정합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KeyBindListUI : MonoBehaviour
    {
        [SerializeField] private KeyBindEntryUI _entryPrefab;
        [SerializeField] private Transform _entryParent;
        [SerializeField] private RebindableAction[] _rebindableActions;
        [SerializeField] private Button _resetButton;
        [SerializeField] private VoidEventChannelSO _bindingsChangedChannel;

        private readonly List<KeyBindEntryUI> _entries = new();

        private void Start()
        {
            BuildEntries();
        }

        private void OnEnable()
        {
            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(ResetAllBindings);
            }

            RefreshEntries();
        }

        private void OnDisable()
        {
            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveListener(ResetAllBindings);
            }
        }

        private void OnDestroy()
        {
            foreach (KeyBindEntryUI entry in _entries)
            {
                if (entry != null)
                {
                    entry.Rebound -= OnEntryRebound;
                }
            }

            _entries.Clear();
        }

        private void BuildEntries()
        {
            if (_entryPrefab == null || _entryParent == null || _rebindableActions == null)
            {
                Debug.LogError("키 설정 목록을 구성할 참조가 비어 있습니다.", this);
                return;
            }

            for (int index = 0; index < _rebindableActions.Length; index++)
            {
                RebindableAction rebindable = _rebindableActions[index];

                if (rebindable.Action == null || rebindable.Action.action == null)
                {
                    Debug.LogError($"키 설정 목록의 {index}번 항목에 액션이 비어 있어 건너뜁니다.", this);
                    continue;
                }

                KeyBindEntryUI entry = Instantiate(_entryPrefab, _entryParent);
                entry.Bind(rebindable.Action.action, rebindable.DisplayName, rebindable.BindingIndex);
                entry.Rebound += OnEntryRebound;
                _entries.Add(entry);
            }
        }

        private void OnEntryRebound()
        {
            // 한 키를 다른 기능에서 빼앗아 온 경우가 표시에 반영되도록 전체를 갱신합니다.
            RefreshEntries();
            _bindingsChangedChannel?.Raise();
        }

        private void ResetAllBindings()
        {
            foreach (KeyBindEntryUI entry in _entries)
            {
                if (entry != null)
                {
                    entry.RemoveOverride();
                }
            }

            _bindingsChangedChannel?.Raise();
        }

        private void RefreshEntries()
        {
            foreach (KeyBindEntryUI entry in _entries)
            {
                if (entry != null)
                {
                    entry.RefreshBindingLabel();
                }
            }
        }

        /// <summary>키 설정 화면에 노출할 액션 하나와 그 표시 정보를 담습니다.</summary>
        [Serializable]
        private struct RebindableAction
        {
            [SerializeField] private InputActionReference _action;
            [SerializeField] private string _displayName;
            [SerializeField] private int _bindingIndex;

            /// <summary>다시 할당할 대상 액션입니다.</summary>
            public InputActionReference Action => _action;

            /// <summary>화면에 보여 줄 기능 이름입니다.</summary>
            public string DisplayName => _displayName;

            /// <summary>
            /// 액션 안에서 조작할 바인딩의 순번입니다.
            /// 이동처럼 방향이 묶인 복합 바인딩은 부분마다 순번이 달라, 부분 수만큼 항목을 만듭니다.
            /// </summary>
            public int BindingIndex => _bindingIndex;
        }
    }
}
