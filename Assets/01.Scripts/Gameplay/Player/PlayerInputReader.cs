using Game.Core.Events;
using Game.Core.Tutorial;
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
        [SerializeField] private TutorialInputPermissionEventChannelSO _tutorialInputPermissionChannel;

        private TutorialInputPermission _allowedInputs = TutorialInputPermission.All;

        private void Awake()
        {
            if (_playerMovement == null) _playerMovement = GetComponent<PlayerMovement>();
            if (_playerSlide == null) _playerSlide = GetComponent<PlayerSlide>();
            if (_playerWeaponController == null) _playerWeaponController = GetComponent<PlayerWeaponController>();
        }

        private void OnEnable()
        {
            if (_tutorialInputPermissionChannel != null)
            {
                _tutorialInputPermissionChannel.Raised += OnInputPermissionChanged;
                _allowedInputs = _tutorialInputPermissionChannel.HasValue
                    ? _tutorialInputPermissionChannel.CurrentValue
                    : TutorialInputPermission.All;
            }

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
            if (_tutorialInputPermissionChannel != null)
                _tutorialInputPermissionChannel.Raised -= OnInputPermissionChanged;

            Unsubscribe(_jumpAction, OnJumpPerformed, null);
            Unsubscribe(_slideAction, OnSlidePerformed, OnSlideCanceled);
            Unsubscribe(_attackAction, OnAttackPerformed, OnAttackCanceled);
            Unsubscribe(_aimPositionAction, OnAimPositionPerformed, null);
            Unsubscribe(_weaponOneAction, OnWeaponOnePerformed, null);
            Unsubscribe(_weaponTwoAction, OnWeaponTwoPerformed, null);
            Unsubscribe(_weaponThreeAction, OnWeaponThreePerformed, null);
            Unsubscribe(_weaponFourAction, OnWeaponFourPerformed, null);
            ClearHeldInputs();
        }

private void OnInputPermissionChanged(TutorialInputPermission permission)
        {
            _allowedInputs = permission;
            if (!Allows(TutorialInputPermission.Attack))
            {
                _playerWeaponController?.SetAttackHeld(false);
            }
        }

        private bool Allows(TutorialInputPermission permission) => (_allowedInputs & permission) != 0;

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (Allows(TutorialInputPermission.Jump)) _playerMovement?.RequestJump();
        }

        private void OnSlidePerformed(InputAction.CallbackContext context)
        {
            if (Allows(TutorialInputPermission.Slide)) _playerSlide?.SetSlideRequested(true);
        }

        private void OnSlideCanceled(InputAction.CallbackContext context) { _playerSlide?.SetSlideRequested(false); }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (Allows(TutorialInputPermission.Attack)) _playerWeaponController?.SetAttackHeld(true);
        }

        private void OnAttackCanceled(InputAction.CallbackContext context) { _playerWeaponController?.SetAttackHeld(false); }

        private void OnAimPositionPerformed(InputAction.CallbackContext context)
        {
            if (Allows(TutorialInputPermission.Aim))
                _playerWeaponController?.SetAimScreenPosition(context.ReadValue<Vector2>());
        }

        private void OnWeaponOnePerformed(InputAction.CallbackContext context) { SelectWeapon(WeaponOneIndex); }
        private void OnWeaponTwoPerformed(InputAction.CallbackContext context) { SelectWeapon(WeaponTwoIndex); }
        private void OnWeaponThreePerformed(InputAction.CallbackContext context) { SelectWeapon(WeaponThreeIndex); }
        private void OnWeaponFourPerformed(InputAction.CallbackContext context) { SelectWeapon(WeaponFourIndex); }

        private void SelectWeapon(int index)
        {
            if (Allows(TutorialInputPermission.WeaponSelection))
                _playerWeaponController?.RequestWeaponSelection(index);
        }

        private void ClearHeldInputs()
        {
            _playerSlide?.SetSlideRequested(false);
            _playerWeaponController?.SetAttackHeld(false);
        }

        private static void Subscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> performed, System.Action<InputAction.CallbackContext> canceled)
        {
            if (reference == null || reference.action == null) return;
            reference.action.performed += performed;
            if (canceled != null) reference.action.canceled += canceled;
            reference.action.Enable();
        }

        private static void Unsubscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> performed, System.Action<InputAction.CallbackContext> canceled)
        {
            if (reference == null || reference.action == null) return;
            reference.action.performed -= performed;
            if (canceled != null) reference.action.canceled -= canceled;
            reference.action.Disable();
        }
    }
}
