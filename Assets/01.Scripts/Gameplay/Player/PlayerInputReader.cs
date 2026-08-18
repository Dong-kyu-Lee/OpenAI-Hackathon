using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField] private InputActionReference _slideAction;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private PlayerSlide _playerSlide;

        private void Awake()
        {
            if (_playerMovement == null)
            {
                _playerMovement = GetComponent<PlayerMovement>();
            }

            if (_playerSlide == null)
            {
                _playerSlide = GetComponent<PlayerSlide>();
            }
        }

        private void OnEnable()
        {
            Subscribe(_jumpAction, OnJumpPerformed, null);
            Subscribe(_slideAction, OnSlidePerformed, OnSlideCanceled);
        }

        private void OnDisable()
        {
            Unsubscribe(_jumpAction, OnJumpPerformed, null);
            Unsubscribe(_slideAction, OnSlidePerformed, OnSlideCanceled);
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            _playerMovement.RequestJump();
        }

        private void OnSlidePerformed(InputAction.CallbackContext context)
        {
            _playerSlide.SetSlideRequested(true);
        }

        private void OnSlideCanceled(InputAction.CallbackContext context)
        {
            _playerSlide.SetSlideRequested(false);
        }

        private static void Subscribe(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> performed,
            System.Action<InputAction.CallbackContext> canceled)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed += performed;
            if (canceled != null)
            {
                actionReference.action.canceled += canceled;
            }

            actionReference.action.Enable();
        }

        private static void Unsubscribe(
            InputActionReference actionReference,
            System.Action<InputAction.CallbackContext> performed,
            System.Action<InputAction.CallbackContext> canceled)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return;
            }

            actionReference.action.performed -= performed;
            if (canceled != null)
            {
                actionReference.action.canceled -= canceled;
            }

            actionReference.action.Disable();
        }
    }
}
