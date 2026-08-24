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
        [SerializeField] private VoidEventChannelSO _gameplayStoppedChannel;
        [SerializeField] private VoidEventChannelSO _playerDiedChannel;

        private TutorialRequest _currentRequest;
        private bool _isWaiting;

        private void OnEnable()
        {
            if (_requestChannel != null)
            {
                _requestChannel.Raised += BeginTutorial;
            }

            Subscribe(_jumpAction, OnJumpPerformed);
            Subscribe(_slideAction, OnSlidePerformed);

            if (_gameplayStoppedChannel != null)
            {
                _gameplayStoppedChannel.Raised += CancelTutorial;
            }

            if (_playerDiedChannel != null)
            {
                _playerDiedChannel.Raised += CancelTutorial;
            }
        }

        private void OnDisable()
        {
            if (_requestChannel != null)
            {
                _requestChannel.Raised -= BeginTutorial;
            }

            Unsubscribe(_jumpAction, OnJumpPerformed);
            Unsubscribe(_slideAction, OnSlidePerformed);

            if (_gameplayStoppedChannel != null)
            {
                _gameplayStoppedChannel.Raised -= CancelTutorial;
            }

            if (_playerDiedChannel != null)
            {
                _playerDiedChannel.Raised -= CancelTutorial;
            }

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
                request.RequiredAction != TutorialAction.Slide)
            {
                Debug.LogWarning($"Tutorial action '{request.RequiredAction}' is not implemented yet.", this);
                return;
            }

            _currentRequest = request;
            _isWaiting = true;
            _tutorialActiveChannel?.Raise(true);
            _scrollController?.Pause();
            _presentationChannel?.Raise(new TutorialPresentation(
                true,
                request.Title,
                request.Message,
                request.InputLabel));
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            TryComplete(TutorialAction.Jump);
        }

        private void OnSlidePerformed(InputAction.CallbackContext context)
        {
            TryComplete(TutorialAction.Slide);
        }

        private void TryComplete(TutorialAction performedAction)
        {
            if (!_isWaiting || _currentRequest.RequiredAction != performedAction)
            {
                return;
            }

            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            _tutorialActiveChannel?.Raise(false);
            _presentationChannel?.Raise(TutorialPresentation.Hidden);
            _scrollController?.Resume();
            _currentRequest = default;
            _isWaiting = false;
        }

        private void CancelTutorial()
        {
            if (!_isWaiting)
            {
                return;
            }

            _tutorialActiveChannel?.Raise(false);
            _presentationChannel?.Raise(TutorialPresentation.Hidden);
            _currentRequest = default;
            _isWaiting = false;
        }

        private static void Subscribe(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> callback)
        {
            if (actionReference != null && actionReference.action != null)
            {
                actionReference.action.performed += callback;
            }
        }

        private static void Unsubscribe(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> callback)
        {
            if (actionReference != null && actionReference.action != null)
            {
                actionReference.action.performed -= callback;
            }
        }
    }
}
