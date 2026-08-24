using System.Collections;
using Game.Core.Events;
using Game.Core.Tutorial;
using Game.Gameplay.Stage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialDirector : MonoBehaviour
    {
        [SerializeField] private MapScrollController _scrollController;
        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField] private InputActionReference _slideAction;

        [Header("Channels")]
        [SerializeField] private TutorialRequestEventChannelSO _requestChannel;
        [SerializeField] private TutorialPresentationEventChannelSO _presentationChannel;
        [SerializeField] private BoolEventChannelSO _tutorialActiveChannel;
        [SerializeField] private StringEventChannelSO _completionChannel;
        [SerializeField] private StringEventChannelSO _continueChannel;
        [SerializeField] private TutorialInputPermissionEventChannelSO _inputPermissionChannel;
        [SerializeField] private VoidEventChannelSO _gameplayStoppedChannel;
        [SerializeField] private VoidEventChannelSO _playerDiedChannel;

        private TutorialRequest _currentRequest;
        private Coroutine _inputCompletionRoutine;
        private bool _isWaiting;
        private bool _isTraversalPhase;

        private void OnEnable()
        {
            if (_requestChannel != null) _requestChannel.Raised += BeginTutorial;
            if (_completionChannel != null) _completionChannel.Raised += OnStepCompleted;
            if (_continueChannel != null) _continueChannel.Raised += OnStepContinue;
            Subscribe(_jumpAction, OnJumpPerformed);
            Subscribe(_slideAction, OnSlidePerformed);
            if (_gameplayStoppedChannel != null) _gameplayStoppedChannel.Raised += CancelTutorial;
            if (_playerDiedChannel != null) _playerDiedChannel.Raised += CancelTutorial;
        }

        private void OnDisable()
        {
            if (_requestChannel != null) _requestChannel.Raised -= BeginTutorial;
            if (_completionChannel != null) _completionChannel.Raised -= OnStepCompleted;
            if (_continueChannel != null) _continueChannel.Raised -= OnStepContinue;
            Unsubscribe(_jumpAction, OnJumpPerformed);
            Unsubscribe(_slideAction, OnSlidePerformed);
            if (_gameplayStoppedChannel != null) _gameplayStoppedChannel.Raised -= CancelTutorial;
            if (_playerDiedChannel != null) _playerDiedChannel.Raised -= CancelTutorial;
            CancelTutorial();
        }

        private void BeginTutorial(TutorialRequest request)
        {
            if (_isWaiting)
            {
                Debug.LogWarning($"Tutorial step '{request.StepId}' was ignored because another step is active.", this);
                return;
            }

            if (request.RequiredAction != TutorialAction.Jump &&
                request.RequiredAction != TutorialAction.Slide &&
                request.RequiredAction != TutorialAction.DestroyObstacle &&
                request.RequiredAction != TutorialAction.CompleteIceBridge)
            {
                Debug.LogWarning($"Tutorial action '{request.RequiredAction}' is not implemented yet.", this);
                return;
            }

            _currentRequest = request;
            _isWaiting = true;
            _isTraversalPhase = false;
            _inputPermissionChannel?.Raise(request.AllowedInputs);
            _tutorialActiveChannel?.Raise(true);
            _scrollController?.Pause();
            _presentationChannel?.Raise(new TutorialPresentation(
                true,
                request.Title,
                request.Message,
                request.InputLabel,
                request.InputHints));
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            ScheduleInputCompletion(TutorialAction.Jump);
        }

        private void OnSlidePerformed(InputAction.CallbackContext context)
        {
            ScheduleInputCompletion(TutorialAction.Slide);
        }

        private void OnStepContinue(string stepId)
        {
            if (!_isWaiting ||
                _isTraversalPhase ||
                _currentRequest.RequiredAction != TutorialAction.CompleteIceBridge ||
                _currentRequest.StepId != stepId)
            {
                return;
            }

            _isTraversalPhase = true;
            _inputPermissionChannel?.Raise(TutorialInputPermission.None);
            _tutorialActiveChannel?.Raise(false);
            _presentationChannel?.Raise(TutorialPresentation.Hidden);
            _scrollController?.Resume();
        }

        private void OnStepCompleted(string stepId)
        {
            if (!_isWaiting || _currentRequest.StepId != stepId)
                return;

            bool isDestructionStep = _currentRequest.RequiredAction == TutorialAction.DestroyObstacle;
            bool isCompletedTraversal =
                _currentRequest.RequiredAction == TutorialAction.CompleteIceBridge &&
                _isTraversalPhase;

            if (isDestructionStep || isCompletedTraversal)
                CompleteTutorial();
        }

        private void ScheduleInputCompletion(TutorialAction action)
        {
            if (!_isWaiting || _currentRequest.RequiredAction != action || _inputCompletionRoutine != null)
                return;

            _inputCompletionRoutine = StartCoroutine(CompleteAfterPhysicsInput());
        }

        private IEnumerator CompleteAfterPhysicsInput()
        {
            yield return new WaitForFixedUpdate();
            _inputCompletionRoutine = null;

            if (_isWaiting)
                CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            StopPendingInputCompletion();
            _inputPermissionChannel?.Raise(TutorialInputPermission.None);
            _tutorialActiveChannel?.Raise(false);
            _presentationChannel?.Raise(TutorialPresentation.Hidden);
            _scrollController?.Resume();
            _currentRequest = default;
            _isWaiting = false;
            _isTraversalPhase = false;
        }

        private void CancelTutorial()
        {
            StopPendingInputCompletion();
            if (!_isWaiting)
                return;

            _inputPermissionChannel?.Raise(TutorialInputPermission.None);
            _tutorialActiveChannel?.Raise(false);
            _presentationChannel?.Raise(TutorialPresentation.Hidden);
            _currentRequest = default;
            _isWaiting = false;
            _isTraversalPhase = false;
        }

        private void StopPendingInputCompletion()
        {
            if (_inputCompletionRoutine == null)
                return;

            StopCoroutine(_inputCompletionRoutine);
            _inputCompletionRoutine = null;
        }

        private static void Subscribe(
            InputActionReference reference,
            System.Action<InputAction.CallbackContext> callback)
        {
            if (reference != null && reference.action != null)
                reference.action.performed += callback;
        }

        private static void Unsubscribe(
            InputActionReference reference,
            System.Action<InputAction.CallbackContext> callback)
        {
            if (reference != null && reference.action != null)
                reference.action.performed -= callback;
        }
    }
}
