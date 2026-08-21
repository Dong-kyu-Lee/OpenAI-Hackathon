using Game.Gameplay.Weapon;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private const int WeaponOneIndex = 0;
        private const int WeaponTwoIndex = 1;
        private const int WeaponThreeIndex = 2;
        private const int WeaponFourIndex = 3;

        [SerializeField] private InputActionReference _jumpAction;
        [SerializeField] private InputActionReference _slideAction;
        [SerializeField] private InputActionReference _attackAction;
        [SerializeField] private InputActionReference _aimPositionAction;
        [SerializeField] private InputActionReference _weaponOneAction;
        [SerializeField] private InputActionReference _weaponTwoAction;
        [SerializeField] private InputActionReference _weaponThreeAction;
        [SerializeField] private InputActionReference _weaponFourAction;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private PlayerSlide _playerSlide;
        [SerializeField] private PlayerWeaponController _playerWeaponController;

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

            if (_playerWeaponController == null)
            {
                _playerWeaponController = GetComponent<PlayerWeaponController>();
            }
        }

        private void OnEnable()
        {
            Subscribe(_jumpAction, OnJumpPerformed, null);
            Subscribe(_slideAction, OnSlidePerformed, OnSlideCanceled);
            Subscribe(_attackAction, OnAttackPerformed, OnAttackCanceled);
            Subscribe(_aimPositionAction, OnAimPositionPerformed, null);
            Subscribe(_weaponOneAction, OnWeaponOnePerformed, null);
            Subscribe(_weaponTwoAction, OnWeaponTwoPerformed, null);
            Subscribe(_weaponThreeAction, OnWeaponThreePerformed, null);
            Subscribe(_weaponFourAction, OnWeaponFourPerformed, null);
        }

        private void OnDisable()
        {
            Unsubscribe(_jumpAction, OnJumpPerformed, null);
            Unsubscribe(_slideAction, OnSlidePerformed, OnSlideCanceled);
            Unsubscribe(_attackAction, OnAttackPerformed, OnAttackCanceled);
            Unsubscribe(_aimPositionAction, OnAimPositionPerformed, null);
            Unsubscribe(_weaponOneAction, OnWeaponOnePerformed, null);
            Unsubscribe(_weaponTwoAction, OnWeaponTwoPerformed, null);
            Unsubscribe(_weaponThreeAction, OnWeaponThreePerformed, null);
            Unsubscribe(_weaponFourAction, OnWeaponFourPerformed, null);

            _playerWeaponController?.SetAttackHeld(false);
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

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            _playerWeaponController?.SetAttackHeld(true);
        }

        private void OnAttackCanceled(InputAction.CallbackContext context)
        {
            _playerWeaponController?.SetAttackHeld(false);
        }

        private void OnAimPositionPerformed(InputAction.CallbackContext context)
        {
            _playerWeaponController?.SetAimScreenPosition(context.ReadValue<Vector2>());
        }

        private void OnWeaponOnePerformed(InputAction.CallbackContext context)
        {
            _playerWeaponController?.RequestWeaponSelection(WeaponOneIndex);
        }

        private void OnWeaponTwoPerformed(InputAction.CallbackContext context)
        {
            _playerWeaponController?.RequestWeaponSelection(WeaponTwoIndex);
        }

        private void OnWeaponThreePerformed(InputAction.CallbackContext context)
        {
            _playerWeaponController?.RequestWeaponSelection(WeaponThreeIndex);
        }

        private void OnWeaponFourPerformed(InputAction.CallbackContext context)
        {
            _playerWeaponController?.RequestWeaponSelection(WeaponFourIndex);
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
