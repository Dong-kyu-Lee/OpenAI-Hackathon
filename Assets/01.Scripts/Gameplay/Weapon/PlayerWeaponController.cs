using Game.Core.Events;
using Game.Core.Flow;
using Game.Data;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponLoadoutSO _loadout;
        [SerializeField] private WeaponBase[] _weapons;
        [SerializeField] private Camera _aimCamera;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private GameStateEventChannelSO _gameStateChangedChannel;

        private Vector2 _aimScreenPosition;
        private float _switchCompleteTime;
        private int _currentWeaponIndex;
        private int _pendingWeaponIndex;
        private bool _isAttackHeld;
        private bool _isAttackBlockedUntilRelease;
        private bool _isGameplayActive;
        private bool _isSwitching;

        private WeaponBase CurrentWeapon => IsValidWeaponIndex(_currentWeaponIndex)
            ? _weapons[_currentWeaponIndex]
            : null;

        private void Awake()
        {
            if (_playerHealth == null)
            {
                _playerHealth = GetComponent<PlayerHealth>();
            }

            if (_aimCamera == null)
            {
                _aimCamera = Camera.main;
            }

            if (_weapons == null)
            {
                _weapons = new WeaponBase[0];
            }

            _currentWeaponIndex = FindFirstWeaponIndex();
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i] != null)
                {
                    _weapons[i].gameObject.SetActive(i == _currentWeaponIndex);
                }
            }
        }

        private void Update()
        {
            if (!_isGameplayActive)
            {
                CancelCurrentAttack();
                return;
            }

            if (_playerHealth != null && _playerHealth.IsDead)
            {
                CancelCurrentAttack();
                return;
            }

            if (_isSwitching)
            {
                TryCompleteSwitch();
                return;
            }

            WeaponBase currentWeapon = CurrentWeapon;
            if (currentWeapon == null || _aimCamera == null)
            {
                return;
            }

            Vector2 aimDirection = CalculateAimDirection(currentWeapon.MuzzlePosition);
            bool canAttack = _isAttackHeld && !_isAttackBlockedUntilRelease;
            currentWeapon.TickAttack(canAttack, aimDirection, Time.deltaTime);
        }

        private void OnDisable()
        {
            if (_gameStateChangedChannel != null)
            {
                _gameStateChangedChannel.Raised -= OnGameStateChanged;
            }

            _isGameplayActive = false;
            CancelCurrentAttack();
        }

        private void OnEnable()
        {
            if (_gameStateChangedChannel != null)
            {
                _gameStateChangedChannel.Raised += OnGameStateChanged;
            }
        }

        public void SetAttackHeld(bool isHeld)
        {
            _isAttackHeld = isHeld;
            if (!isHeld)
            {
                _isAttackBlockedUntilRelease = false;
            }
        }

        public void SetAimScreenPosition(Vector2 screenPosition)
        {
            _aimScreenPosition = screenPosition;
        }

        public void RequestWeaponSelection(int weaponIndex)
        {
            if (!IsValidWeaponIndex(weaponIndex))
            {
                return;
            }

            if (!_isSwitching && weaponIndex == _currentWeaponIndex)
            {
                return;
            }

            CancelCurrentAttack();

            WeaponBase currentWeapon = CurrentWeapon;
            if (currentWeapon != null)
            {
                currentWeapon.gameObject.SetActive(false);
            }

            _pendingWeaponIndex = weaponIndex;
            float switchDuration = _loadout != null
                ? Mathf.Max(0f, _loadout.SwitchDuration)
                : 0f;

            _switchCompleteTime = Time.time + switchDuration;
            _isSwitching = true;
            _isAttackBlockedUntilRelease = _isAttackHeld;
        }

        private void TryCompleteSwitch()
        {
            if (Time.time < _switchCompleteTime)
            {
                return;
            }

            _currentWeaponIndex = _pendingWeaponIndex;
            _isSwitching = false;
            CurrentWeapon?.gameObject.SetActive(true);
        }

        private Vector2 CalculateAimDirection(Vector3 muzzlePosition)
        {
            float distanceFromCamera = Mathf.Abs(_aimCamera.transform.position.z - muzzlePosition.z);
            Vector3 screenPoint = new Vector3(
                _aimScreenPosition.x,
                _aimScreenPosition.y,
                distanceFromCamera);

            Vector3 worldPoint = _aimCamera.ScreenToWorldPoint(screenPoint);
            return worldPoint - muzzlePosition;
        }

        private void CancelCurrentAttack()
        {
            CurrentWeapon?.CancelAttack();
        }

        private void OnGameStateChanged(GameState state)
        {
            _isGameplayActive = state == GameState.Playing;

            if (_isGameplayActive)
            {
                if (_isAttackHeld)
                {
                    _isAttackBlockedUntilRelease = true;
                }

                return;
            }

            _isAttackBlockedUntilRelease |= _isAttackHeld;
            CancelCurrentAttack();
        }

        private int FindFirstWeaponIndex()
        {
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i] != null)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsValidWeaponIndex(int weaponIndex)
        {
            return _weapons != null
                && weaponIndex >= 0
                && weaponIndex < _weapons.Length
                && _weapons[weaponIndex] != null;
        }
    }
}
