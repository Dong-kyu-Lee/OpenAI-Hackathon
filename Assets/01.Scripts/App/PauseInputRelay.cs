using Game.Core.Events;
using Game.Core.Flow;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.App
{
    /// <summary>
    /// 일시정지 입력 하나를 현재 상태에 맞는 일시정지 또는 재개 요청으로 바꿔 채널에 전달합니다.
    /// 상태 전환의 가부 판단은 하지 않으며, 어떤 요청을 낼지만 결정합니다.
    /// 일시정지 화면 위에 옵션이나 확인 팝업이 떠 있는 동안에는 억제 채널을 통해 입력을 무시합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseInputRelay : MonoBehaviour
    {
        [SerializeField] private InputActionReference _pauseAction;
        [SerializeField] private GameStateEventChannelSO _gameStateChangedChannel;
        [SerializeField] private VoidEventChannelSO _pauseRequestedChannel;
        [SerializeField] private VoidEventChannelSO _resumeRequestedChannel;
        [SerializeField] private BoolEventChannelSO _pauseInputSuppressedChannel;

        private GameState _currentState = GameState.Boot;
        private bool _isSuppressed;

        private void OnEnable()
        {
            // UI 씬이 언로드되며 억제 해제를 놓쳤을 경우에 대비해 항상 풀린 상태로 시작합니다.
            _isSuppressed = false;

            if (_gameStateChangedChannel != null)
            {
                _gameStateChangedChannel.Raised += OnGameStateChanged;
            }

            if (_pauseInputSuppressedChannel != null)
            {
                _pauseInputSuppressedChannel.Raised += OnPauseInputSuppressedChanged;
            }

            if (_pauseAction != null && _pauseAction.action != null)
            {
                _pauseAction.action.performed += OnPausePerformed;
                _pauseAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_gameStateChangedChannel != null)
            {
                _gameStateChangedChannel.Raised -= OnGameStateChanged;
            }

            if (_pauseInputSuppressedChannel != null)
            {
                _pauseInputSuppressedChannel.Raised -= OnPauseInputSuppressedChanged;
            }

            if (_pauseAction != null && _pauseAction.action != null)
            {
                _pauseAction.action.performed -= OnPausePerformed;
                _pauseAction.action.Disable();
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            _currentState = state;
        }

        private void OnPauseInputSuppressedChanged(bool isSuppressed)
        {
            _isSuppressed = isSuppressed;
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (_isSuppressed)
            {
                return;
            }

            switch (_currentState)
            {
                case GameState.Playing:
                    _pauseRequestedChannel?.Raise();
                    break;

                case GameState.Paused:
                    _resumeRequestedChannel?.Raise();
                    break;
            }
        }
    }
}
