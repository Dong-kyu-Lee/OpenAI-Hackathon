using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Ranking;
using TMPro;
using UnityEngine;

namespace Game.UI.Ranking
{
    /// <summary>타이틀 화면에서 로컬 랭킹 목록을 요청하고 ScrollRect Content에 표시합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class RankingBoardPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Transform _content;
        [SerializeField] private RankingEntryUI _entryPrefab;
        [SerializeField] private UnityEngine.UI.Button _closeButton;
        [SerializeField] private TMP_Text _emptyLabel;
        [SerializeField] private TMP_Text _errorLabel;
        [SerializeField] private VoidEventChannelSO _rankingRefreshRequestedChannel;
        [SerializeField] private RankingSnapshotEventChannelSO _rankingSnapshotChannel;
        [SerializeField] private string _emptyText = "NO RECORDS";
        [SerializeField] private string _defaultErrorText = "랭킹을 불러오지 못했습니다.";

        private readonly List<RankingEntryUI> _entryViews = new();

        public event Action Closed;

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_rankingSnapshotChannel != null)
            {
                _rankingSnapshotChannel.Raised += OnRankingSnapshot;
            }
        }

        private void OnDisable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
            }

            if (_rankingSnapshotChannel != null)
            {
                _rankingSnapshotChannel.Raised -= OnRankingSnapshot;
            }
        }

        public void Open()
        {
            SetError(string.Empty);
            SetPanelVisible(true);
            _rankingRefreshRequestedChannel?.Raise();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            SetPanelVisible(false);
            Closed?.Invoke();
        }

        private void OnRankingSnapshot(RankingSnapshot snapshot)
        {
            if (!IsOpen || snapshot == null)
            {
                return;
            }

            if (!snapshot.Succeeded)
            {
                SetEntryCount(0);
                SetEmptyVisible(false);
                SetError(string.IsNullOrEmpty(snapshot.ErrorMessage)
                    ? _defaultErrorText
                    : snapshot.ErrorMessage);
                return;
            }

            SetError(string.Empty);
            EnsureEntryCapacity(snapshot.Entries.Count);

            for (int index = 0; index < _entryViews.Count; index++)
            {
                bool shouldShow = index < snapshot.Entries.Count;
                RankingEntryUI entryView = _entryViews[index];
                entryView.gameObject.SetActive(shouldShow);

                if (shouldShow)
                {
                    entryView.Bind(index + 1, snapshot.Entries[index]);
                }
            }

            SetEmptyVisible(snapshot.Entries.Count == 0);
        }

        private void EnsureEntryCapacity(int requiredCount)
        {
            if (_entryPrefab == null || _content == null)
            {
                if (requiredCount > 0)
                {
                    SetError(_defaultErrorText);
                    Debug.LogError("랭킹 항목 프리팹 또는 Content가 연결되지 않았습니다.", this);
                }

                return;
            }

            while (_entryViews.Count < requiredCount)
            {
                RankingEntryUI entryView = Instantiate(_entryPrefab, _content);
                _entryViews.Add(entryView);
            }
        }

        private void SetEntryCount(int visibleCount)
        {
            for (int index = 0; index < _entryViews.Count; index++)
            {
                _entryViews[index].gameObject.SetActive(index < visibleCount);
            }
        }

        private void SetEmptyVisible(bool isVisible)
        {
            if (_emptyLabel == null)
            {
                return;
            }

            _emptyLabel.text = _emptyText;
            _emptyLabel.gameObject.SetActive(isVisible);
        }

        private void SetError(string message)
        {
            if (_errorLabel == null)
            {
                return;
            }

            _errorLabel.text = message;
            _errorLabel.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(isVisible);
            }
        }
    }
}
