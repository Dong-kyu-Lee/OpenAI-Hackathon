using Game.Core.Tutorial;
using UnityEngine;

namespace Game.Data.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialStep", menuName = "Game/Data/Tutorial/Tutorial Step")]
    public sealed class TutorialStepSO : ScriptableObject
    {
        [SerializeField] private string _stepId;
        [SerializeField] private string _title;
        [SerializeField, TextArea] private string _message;
        [SerializeField] private string _inputLabel;
        [SerializeField] private TutorialAction _requiredAction;
        [SerializeField] private TutorialInputPermission _allowedInputs;
        [SerializeField] private TutorialInputHint[] _inputHints;

        public string StepId => _stepId;
        public TutorialAction RequiredAction => _requiredAction;
        public TutorialInputPermission AllowedInputs => _allowedInputs;

        public TutorialRequest CreateRequest()
        {
            return new TutorialRequest(
                _stepId,
                _title,
                _message,
                _inputLabel,
                _requiredAction,
                _allowedInputs,
                _inputHints);
        }
    }
}
