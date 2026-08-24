using Game.Core.Events;
using Game.Core.Flow;
using Game.Core.Ranking;
using Game.Data.Ranking;
using Game.Data.Stage;
using TMPro;
using UnityEngine;

namespace Game.UI.Ranking
{
    /// <summary>무한 모드 종료 후 플레이어 이름을 받아 거리 기록 등록을 요청합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class RankingSubmissionPopupUI : MonoBehaviour
    {
        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private TMP_Text _distanceLabel;
        [SerializeField] private TMP_Text _errorLabel;
        [SerializeField] private UnityEngine.UI.Button _confirmButton;
        [SerializeField] private RankingSettingsSO _settings;
        [SerializeField] private StageSelectionStateSO _stageSelectionState;
        [SerializeField] private StageResultEventChannelSO _stageFinishedChannel;
        [SerializeField] private RankingSubmissionRequestEventChannelSO _submissionRequestedChannel;
        [SerializeField] private RankingSubmissionResultEventChannelSO _submissionResultChannel;
        [SerializeField] private string _distanceFormat = "DISTANCE  {0:N0}";
        [SerializeField] private string _defaultErrorText = "랭킹을 저장하지 못했습니다. 다시 시도해 주세요.";

        private float _pendingDistance;
        private bool _isAwaitingResult;

        private void Awake()
        {
            if (_nameInput != null && _settings != null)
            {
                _nameInput.characterLimit = _settings.MaxNameLength;
            }

            SetPopupVisible(false);
        }

        private void OnEnable()
        {
            if (_stageFinishedChannel != null)
            {
                _stageFinishedChannel.Raised += OnStageFinished;
            }

            if (_submissionResultChannel != null)
            {
                _submissionResultChannel.Raised += OnSubmissionResult;
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(SubmitRanking);
            }
        }

        private void OnDisable()
        {
            if (_stageFinishedChannel != null)
            {
                _stageFinishedChannel.Raised -= OnStageFinished;
            }

            if (_submissionResultChannel != null)
            {
                _submissionResultChannel.Raised -= OnSubmissionResult;
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(SubmitRanking);
            }
        }

        private void OnStageFinished(StageResult result)
        {
            StageDefinitionSO definition = _stageSelectionState == null
                ? null
                : _stageSelectionState.CurrentStageDefinition;

            if (definition == null || !definition.IsEndlessMode)
            {
                SetPopupVisible(false);
                return;
            }

            _pendingDistance = result.Distance;
            _isAwaitingResult = false;

            if (_nameInput != null)
            {
                _nameInput.SetTextWithoutNotify(string.Empty);
            }

            if (_distanceLabel != null)
            {
                _distanceLabel.text = string.Format(_distanceFormat, Mathf.FloorToInt(result.Distance));
            }

            SetError(string.Empty);
            SetConfirmInteractable(true);
            SetPopupVisible(true);
            _nameInput?.ActivateInputField();
        }

        private void SubmitRanking()
        {
            if (_isAwaitingResult)
            {
                return;
            }

            if (_submissionRequestedChannel == null)
            {
                SetError(_defaultErrorText);
                return;
            }

            _isAwaitingResult = true;
            SetConfirmInteractable(false);
            SetError(string.Empty);

            string playerName = _nameInput == null ? string.Empty : _nameInput.text;
            _submissionRequestedChannel.Raise(
                new RankingSubmissionRequest(playerName, _pendingDistance));
        }

        private void OnSubmissionResult(RankingSubmissionResult result)
        {
            if (!_isAwaitingResult)
            {
                return;
            }

            _isAwaitingResult = false;

            if (result.Succeeded)
            {
                SetPopupVisible(false);
                return;
            }

            SetConfirmInteractable(true);
            SetError(string.IsNullOrEmpty(result.ErrorMessage)
                ? _defaultErrorText
                : result.ErrorMessage);
        }

        private void SetConfirmInteractable(bool interactable)
        {
            if (_confirmButton != null)
            {
                _confirmButton.interactable = interactable;
            }
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

        private void SetPopupVisible(bool isVisible)
        {
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(isVisible);
            }
        }
    }
}
