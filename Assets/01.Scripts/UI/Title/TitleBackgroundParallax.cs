using Game.Data.Title;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI.Title
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TitleBackgroundParallax : MonoBehaviour
    {
        private const float NormalizedRange = 2f;
        private const float NormalizedCenter = 1f;
        private const int MinimumScreenDimension = 1;

        [SerializeField] private InputActionReference _pointerPositionAction;
        [SerializeField] private TitleParallaxSettingsSO _settings;

        private RectTransform _rectTransform;
        private Vector2 _initialAnchoredPosition;
        private Vector2 _pointerScreenPosition;
        private Vector2 _smoothVelocity;
        private bool _hasPointerPosition;
        private bool _ownsInputAction;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialAnchoredPosition = _rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_settings == null)
            {
                Debug.LogError("Title background parallax requires settings.", this);
                return;
            }

            if (_pointerPositionAction == null || _pointerPositionAction.action == null)
            {
                Debug.LogError("Title background parallax requires a pointer position action.", this);
                return;
            }

            InputAction pointerAction = _pointerPositionAction.action;
            pointerAction.performed += OnPointerPositionPerformed;

            if (!pointerAction.enabled)
            {
                pointerAction.Enable();
                _ownsInputAction = true;
            }
        }

        private void OnDisable()
        {
            if (_pointerPositionAction != null && _pointerPositionAction.action != null)
            {
                InputAction pointerAction = _pointerPositionAction.action;
                pointerAction.performed -= OnPointerPositionPerformed;

                if (_ownsInputAction)
                {
                    pointerAction.Disable();
                }
            }

            _ownsInputAction = false;
            _hasPointerPosition = false;
            _smoothVelocity = Vector2.zero;

            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _initialAnchoredPosition;
            }
        }

        private void Update()
        {
            if (!_hasPointerPosition || _settings == null)
            {
                return;
            }

            float screenWidth = Mathf.Max(MinimumScreenDimension, Screen.width);
            float screenHeight = Mathf.Max(MinimumScreenDimension, Screen.height);
            Vector2 normalizedPointerPosition = new Vector2(
                Mathf.Clamp((_pointerScreenPosition.x / screenWidth * NormalizedRange) - NormalizedCenter, -NormalizedCenter, NormalizedCenter),
                Mathf.Clamp((_pointerScreenPosition.y / screenHeight * NormalizedRange) - NormalizedCenter, -NormalizedCenter, NormalizedCenter));

            Vector2 targetPosition = _initialAnchoredPosition
                + Vector2.Scale(normalizedPointerPosition, _settings.MaxOffset);

            _rectTransform.anchoredPosition = Vector2.SmoothDamp(
                _rectTransform.anchoredPosition,
                targetPosition,
                ref _smoothVelocity,
                _settings.SmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        private void OnPointerPositionPerformed(InputAction.CallbackContext context)
        {
            _pointerScreenPosition = context.ReadValue<Vector2>();
            _hasPointerPosition = true;
        }
    }
}
