using System;
using System.Text;
using System.Threading;
using Game.Core.Events;
using Game.Core.Ranking;
using Game.Data.Ranking;
using Game.Data.Stage;
using UnityEngine;

namespace Game.App.Ranking
{
    /// <summary>
    /// UI의 랭킹 요청을 검증하고 저장소로 전달합니다. UI와 저장소 구현은 서로 직접 참조하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RankingService : MonoBehaviour
    {
        [SerializeField] private RankingSettingsSO _settings;
        [SerializeField] private StageSelectionStateSO _stageSelectionState;
        [SerializeField] private MonoBehaviour _repositoryComponent;

        [Header("수신 채널")]
        [SerializeField] private VoidEventChannelSO _rankingRefreshRequestedChannel;
        [SerializeField] private RankingSubmissionRequestEventChannelSO _submissionRequestedChannel;

        [Header("발신 채널")]
        [SerializeField] private RankingSnapshotEventChannelSO _rankingSnapshotChannel;
        [SerializeField] private RankingSubmissionResultEventChannelSO _submissionResultChannel;

        private IRankingRepository _repository;
        private CancellationTokenSource _lifetimeCancellation;
        private bool _isSubmitting;
        private bool _isRefreshing;

        private void Awake()
        {
            _repository = _repositoryComponent as IRankingRepository;
            _lifetimeCancellation = new CancellationTokenSource();

            if (_repository == null)
            {
                Debug.LogError("RankingService의 저장소 컴포넌트가 IRankingRepository를 구현하지 않습니다.", this);
            }
        }

        private void OnEnable()
        {
            if (_rankingRefreshRequestedChannel != null)
            {
                _rankingRefreshRequestedChannel.Raised += OnRefreshRequested;
            }

            if (_submissionRequestedChannel != null)
            {
                _submissionRequestedChannel.Raised += OnSubmissionRequested;
            }
        }

        private void OnDisable()
        {
            if (_rankingRefreshRequestedChannel != null)
            {
                _rankingRefreshRequestedChannel.Raised -= OnRefreshRequested;
            }

            if (_submissionRequestedChannel != null)
            {
                _submissionRequestedChannel.Raised -= OnSubmissionRequested;
            }
        }

        private void OnDestroy()
        {
            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }

        private async void OnRefreshRequested()
        {
            if (_isRefreshing)
            {
                return;
            }

            if (!TryGetDependencies(out string errorMessage))
            {
                _rankingSnapshotChannel?.Raise(RankingSnapshot.Failure(errorMessage));
                return;
            }

            _isRefreshing = true;

            try
            {
                RankingSnapshot snapshot = await _repository.GetEntriesAsync(
                    _settings.BoardId,
                    _settings.MaxEntries,
                    _lifetimeCancellation.Token);

                _rankingSnapshotChannel?.Raise(snapshot);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"랭킹을 불러오지 못했습니다: {exception.Message}", this);
                _rankingSnapshotChannel?.Raise(
                    RankingSnapshot.Failure("랭킹을 불러오지 못했습니다. 다시 시도해 주세요."));
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private async void OnSubmissionRequested(RankingSubmissionRequest request)
        {
            if (_isSubmitting)
            {
                _submissionResultChannel?.Raise(
                    RankingSubmissionResult.Failure("랭킹 등록을 처리 중입니다."));
                return;
            }

            if (!TryGetDependencies(out string errorMessage) || !IsEndlessStageSelected())
            {
                string failureMessage = !string.IsNullOrEmpty(errorMessage)
                    ? errorMessage
                    : "무한 모드의 결과만 랭킹에 등록할 수 있습니다.";
                _submissionResultChannel?.Raise(RankingSubmissionResult.Failure(failureMessage));
                return;
            }

            if (!TryNormalizeDistance(request.Distance, out int distance))
            {
                _submissionResultChannel?.Raise(
                    RankingSubmissionResult.Failure("유효하지 않은 주행 거리입니다."));
                return;
            }

            string playerName = NormalizePlayerName(request.PlayerName, _settings.MaxNameLength);
            string guestPrefix = NormalizeGuestPrefix(_settings.GuestPrefix, _settings.MaxNameLength);
            _isSubmitting = true;

            try
            {
                RankingSubmissionResult result = await _repository.SubmitAsync(
                    _settings.BoardId,
                    playerName,
                    distance,
                    guestPrefix,
                    _settings.MaxEntries,
                    _lifetimeCancellation.Token);

                _submissionResultChannel?.Raise(result);

                if (result.Succeeded && result.Snapshot != null)
                {
                    _rankingSnapshotChannel?.Raise(result.Snapshot);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"랭킹 등록에 실패했습니다: {exception.Message}", this);
                _submissionResultChannel?.Raise(
                    RankingSubmissionResult.Failure("랭킹을 저장하지 못했습니다. 다시 시도해 주세요."));
            }
            finally
            {
                _isSubmitting = false;
            }
        }

        private bool TryGetDependencies(out string errorMessage)
        {
            if (_settings == null)
            {
                errorMessage = "랭킹 설정이 연결되지 않았습니다.";
                return false;
            }

            if (_repository == null)
            {
                errorMessage = "랭킹 저장소가 연결되지 않았습니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private bool IsEndlessStageSelected()
        {
            StageDefinitionSO definition = _stageSelectionState == null
                ? null
                : _stageSelectionState.CurrentStageDefinition;
            return definition != null && definition.IsEndlessMode;
        }

        private static bool TryNormalizeDistance(float rawDistance, out int distance)
        {
            if (float.IsNaN(rawDistance) || float.IsInfinity(rawDistance) || rawDistance < 0f)
            {
                distance = default;
                return false;
            }

            distance = rawDistance >= int.MaxValue
                ? int.MaxValue
                : Mathf.FloorToInt(rawDistance);
            return true;
        }

        private static string NormalizePlayerName(string playerName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return string.Empty;
            }

            StringBuilder builder = new(playerName.Length);
            for (int index = 0; index < playerName.Length; index++)
            {
                char character = playerName[index];
                if (!char.IsControl(character))
                {
                    builder.Append(character);
                }
            }

            string normalizedName = builder.ToString().Trim();
            return normalizedName.Length > maxLength
                ? normalizedName.Substring(0, maxLength)
                : normalizedName;
        }

        private static string NormalizeGuestPrefix(string guestPrefix, int maxNameLength)
        {
            const string FallbackPrefix = "GUEST";
            const int GuestSuffixLength = 4;

            string normalizedPrefix = NormalizePlayerName(guestPrefix, maxNameLength - GuestSuffixLength);
            return string.IsNullOrEmpty(normalizedPrefix) ? FallbackPrefix : normalizedPrefix;
        }
    }
}
