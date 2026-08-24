using Game.Data.Stage;
using TMPro;
using UnityEngine;

namespace Game.UI.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StageHeaderUI : MonoBehaviour
    {
        [SerializeField] private StageSelectionStateSO _stageSelectionState;
        [SerializeField] private TMP_Text _stageLabel;
        [SerializeField] private string _fallbackLabel = "STAGE";

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_stageLabel == null)
                return;

            StageDefinitionSO definition = _stageSelectionState == null
                ? null
                : _stageSelectionState.CurrentStageDefinition;

            _stageLabel.text = definition == null ||
                               string.IsNullOrWhiteSpace(definition.DisplayName)
                ? _fallbackLabel
                : definition.DisplayName.ToUpperInvariant();
        }
    }
}