using Game.Core.Events;
using Game.Data.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.StageSelect
{
    /// <summary>
    /// 스테이지 하나를 표시하고, 눌리면 자신에게 할당된 순번으로 플레이를 요청하는 목록 항목입니다.
    /// 표시할 스테이지와 요청 채널은 목록을 구성하는 쪽이 <see cref="Bind"/>로 주입합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageEntryButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private Image _thumbnail;

        private StageRequestEventChannelSO _stageRequestedChannel;
        private int _stageIndex = -1;

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(RequestStage);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(RequestStage);
            }
        }

        /// <summary>표시할 스테이지와 클릭 시 사용할 요청 채널을 설정합니다.</summary>
        /// <param name="definition">표시할 스테이지 데이터입니다.</param>
        /// <param name="stageIndex">카탈로그 안에서 이 스테이지의 순번입니다.</param>
        /// <param name="stageRequestedChannel">클릭 시 순번을 실어 보낼 채널입니다.</param>
        public void Bind(
            StageDefinitionSO definition,
            int stageIndex,
            StageRequestEventChannelSO stageRequestedChannel)
        {
            _stageIndex = stageIndex;
            _stageRequestedChannel = stageRequestedChannel;

            if (_nameLabel != null)
            {
                _nameLabel.text = definition == null ? string.Empty : definition.DisplayName;
            }

            if (_thumbnail != null)
            {
                Sprite sprite = definition == null ? null : definition.Thumbnail;
                _thumbnail.sprite = sprite;
                _thumbnail.enabled = sprite != null;
            }
        }

        private void RequestStage()
        {
            if (_stageIndex < 0)
            {
                return;
            }

            _stageRequestedChannel?.Raise(_stageIndex);
        }
    }
}
