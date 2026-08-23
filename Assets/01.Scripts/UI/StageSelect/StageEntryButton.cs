using Game.Core.Events;
using Game.Data.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.StageSelect
{
    /// <summary>
    /// 스테이지 하나를 표시하고, 눌리면 자신에게 할당된 순번으로 플레이를 요청하는 항목입니다.
    /// 어떤 스테이지를 표시할지는 씬에 배치할 때 인스펙터로 지정하고,
    /// 순번과 요청 채널은 화면을 배선하는 쪽이 <see cref="Bind"/>로 주입합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageEntryButton : MonoBehaviour
    {
        [SerializeField] private StageDefinitionSO _definition;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private Image _thumbnail;

        private StageRequestEventChannelSO _stageRequestedChannel;
        private int _stageIndex = StageCatalogSO.InvalidIndex;

        /// <summary>이 항목이 표시하도록 지정된 스테이지를 가져옵니다.</summary>
        public StageDefinitionSO Definition => _definition;

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
            _definition = definition;
            _stageIndex = stageIndex;
            _stageRequestedChannel = stageRequestedChannel;

            ApplyDisplay(definition);
        }

        private void ApplyDisplay(StageDefinitionSO definition)
        {
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

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 스테이지를 지정하는 즉시 이름과 썸네일을 씬 뷰에 반영합니다.
        /// 배치가 의도대로 됐는지 플레이 없이 눈으로 확인하기 위한 미리보기입니다.
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ApplyDisplay(_definition);
        }
#endif
    }
}
