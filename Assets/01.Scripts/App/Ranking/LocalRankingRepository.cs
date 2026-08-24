using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game.Core.Ranking;
using UnityEngine;

namespace Game.App.Ranking
{
    /// <summary>작은 로컬 랭킹 목록을 JSON으로 직렬화해 PlayerPrefs에 저장합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class LocalRankingRepository : MonoBehaviour, IRankingRepository
    {
        private const int CurrentSchemaVersion = 1;
        private const int FirstGuestNumber = 1;
        private const int LastGuestNumber = 999;
        private const string StorageKeyPrefix = "Ranking.Local.v1.";
        private const string CorruptBackupSuffix = ".CorruptBackup";

        public Task<RankingSnapshot> GetEntriesAsync(
            string boardId,
            int maxEntries,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<RankingSnapshot>(cancellationToken);
            }

            if (!TryValidateArguments(boardId, maxEntries, out string errorMessage))
            {
                return Task.FromResult(RankingSnapshot.Failure(errorMessage));
            }

            RankingSaveData data = LoadData(GetStorageKey(boardId));
            NormalizeAndSort(data, maxEntries);
            return Task.FromResult(CreateSnapshot(data));
        }

        public Task<RankingSubmissionResult> SubmitAsync(
            string boardId,
            string playerName,
            int distance,
            string guestPrefix,
            int maxEntries,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<RankingSubmissionResult>(cancellationToken);
            }

            if (!TryValidateArguments(boardId, maxEntries, out string errorMessage))
            {
                return Task.FromResult(RankingSubmissionResult.Failure(errorMessage));
            }

            string storageKey = GetStorageKey(boardId);
            RankingSaveData data = LoadData(storageKey);
            string resolvedName = string.IsNullOrWhiteSpace(playerName)
                ? CreateGuestName(data, guestPrefix)
                : playerName;

            StoredRankingEntry storedEntry = new()
            {
                playerName = resolvedName,
                distance = Math.Max(0, distance),
                submissionOrder = data.nextSubmissionOrder
            };

            data.nextSubmissionOrder++;
            data.entries.Add(storedEntry);
            NormalizeAndSort(data, maxEntries);

            try
            {
                PlayerPrefs.SetString(storageKey, JsonUtility.ToJson(data));
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogError($"로컬 랭킹을 저장하지 못했습니다: {exception.Message}", this);
                return Task.FromResult(
                    RankingSubmissionResult.Failure("랭킹을 저장하지 못했습니다. 다시 시도해 주세요."));
            }

            RankingEntry submittedEntry = ToRankingEntry(storedEntry);
            RankingSnapshot snapshot = CreateSnapshot(data);
            return Task.FromResult(
                new RankingSubmissionResult(true, string.Empty, submittedEntry, snapshot));
        }

        private RankingSaveData LoadData(string storageKey)
        {
            if (!PlayerPrefs.HasKey(storageKey))
            {
                return CreateEmptyData();
            }

            string json = PlayerPrefs.GetString(storageKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateEmptyData();
            }

            try
            {
                RankingSaveData data = JsonUtility.FromJson<RankingSaveData>(json);
                if (data == null || data.version != CurrentSchemaVersion)
                {
                    return RecoverFromCorruptData(storageKey, json);
                }

                data.entries ??= new List<StoredRankingEntry>();
                data.nextGuestNumber = ClampGuestNumber(data.nextGuestNumber);
                data.nextSubmissionOrder = Math.Max(1L, data.nextSubmissionOrder);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"로컬 랭킹 데이터를 복원하지 못해 새 목록으로 시작합니다: {exception.Message}", this);
                return RecoverFromCorruptData(storageKey, json);
            }
        }

        private RankingSaveData RecoverFromCorruptData(string storageKey, string json)
        {
            Debug.LogWarning("손상되었거나 지원하지 않는 로컬 랭킹 데이터를 별도 키에 보관했습니다.", this);
            PlayerPrefs.SetString(storageKey + CorruptBackupSuffix, json);
            return CreateEmptyData();
        }

        private static RankingSaveData CreateEmptyData()
        {
            return new RankingSaveData
            {
                version = CurrentSchemaVersion,
                nextGuestNumber = FirstGuestNumber,
                nextSubmissionOrder = 1L,
                entries = new List<StoredRankingEntry>()
            };
        }

        private static string CreateGuestName(RankingSaveData data, string guestPrefix)
        {
            int guestNumber = ClampGuestNumber(data.nextGuestNumber);
            data.nextGuestNumber = guestNumber >= LastGuestNumber
                ? FirstGuestNumber
                : guestNumber + 1;

            return $"{guestPrefix}_{guestNumber:000}";
        }

        private static int ClampGuestNumber(int guestNumber)
        {
            return guestNumber < FirstGuestNumber || guestNumber > LastGuestNumber
                ? FirstGuestNumber
                : guestNumber;
        }

        private static void NormalizeAndSort(RankingSaveData data, int maxEntries)
        {
            data.entries.RemoveAll(entry =>
                entry == null ||
                string.IsNullOrWhiteSpace(entry.playerName) ||
                entry.distance < 0);

            data.entries.Sort(CompareEntries);

            if (data.entries.Count > maxEntries)
            {
                data.entries.RemoveRange(maxEntries, data.entries.Count - maxEntries);
            }

            long highestOrder = 0L;
            for (int index = 0; index < data.entries.Count; index++)
            {
                highestOrder = Math.Max(highestOrder, data.entries[index].submissionOrder);
            }

            data.nextSubmissionOrder = Math.Max(data.nextSubmissionOrder, highestOrder + 1L);
        }

        private static int CompareEntries(StoredRankingEntry left, StoredRankingEntry right)
        {
            int distanceComparison = right.distance.CompareTo(left.distance);
            return distanceComparison != 0
                ? distanceComparison
                : left.submissionOrder.CompareTo(right.submissionOrder);
        }

        private static RankingSnapshot CreateSnapshot(RankingSaveData data)
        {
            RankingEntry[] entries = new RankingEntry[data.entries.Count];
            for (int index = 0; index < data.entries.Count; index++)
            {
                entries[index] = ToRankingEntry(data.entries[index]);
            }

            return new RankingSnapshot(true, entries);
        }

        private static RankingEntry ToRankingEntry(StoredRankingEntry entry)
        {
            return new RankingEntry(entry.playerName, entry.distance, entry.submissionOrder);
        }

        private static bool TryValidateArguments(string boardId, int maxEntries, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(boardId))
            {
                errorMessage = "랭킹 보드 ID가 비어 있습니다.";
                return false;
            }

            if (maxEntries < 1)
            {
                errorMessage = "랭킹 최대 저장 개수는 1 이상이어야 합니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static string GetStorageKey(string boardId)
        {
            return StorageKeyPrefix + boardId.Trim();
        }

        [Serializable]
        private sealed class RankingSaveData
        {
            public int version;
            public int nextGuestNumber;
            public long nextSubmissionOrder;
            public List<StoredRankingEntry> entries;
        }

        [Serializable]
        private sealed class StoredRankingEntry
        {
            public string playerName;
            public int distance;
            public long submissionOrder;
        }
    }
}
