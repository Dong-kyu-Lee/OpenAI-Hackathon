using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class IceBridgeTutorialCondition : MonoBehaviour
    {
        [SerializeField] private string _stepId;
        [SerializeField] private Transform _gapLeftBoundary;
        [SerializeField] private Transform _gapRightBoundary;
        [SerializeField, Min(1)] private int _requiredCellCount = 6;
        [SerializeField] private Vector2EventChannelSO _bridgeBuiltChannel;
        [SerializeField] private StringEventChannelSO _continueChannel;

        private readonly HashSet<int> _builtCells = new();
        private bool _isReady;

        private void OnEnable()
        {
            _builtCells.Clear();
            _isReady = false;
            if (_bridgeBuiltChannel != null)
                _bridgeBuiltChannel.Raised += OnBridgeBuilt;
        }

        private void OnDisable()
        {
            if (_bridgeBuiltChannel != null)
                _bridgeBuiltChannel.Raised -= OnBridgeBuilt;
        }

        private void OnBridgeBuilt(Vector2 position)
        {
            if (_isReady || _gapLeftBoundary == null || _gapRightBoundary == null)
                return;

            float left = _gapLeftBoundary.position.x;
            float right = _gapRightBoundary.position.x;
            if (position.x < left || position.x >= right)
                return;

            int index = Mathf.FloorToInt(position.x - left);
            if (index < 0 || index >= _requiredCellCount)
                return;

            _builtCells.Add(index);
            if (_builtCells.Count < _requiredCellCount)
                return;

            _isReady = true;
            _continueChannel?.Raise(_stepId);
        }
    }
}
