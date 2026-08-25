using System;
using Game.Core.Events;
using Game.Data;
using Game.Data.Tutorial;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Tutorial
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class TutorialTrigger : MonoBehaviour
    {
        [SerializeField] private TutorialStepSO _step;
        [SerializeField] private TutorialRequestEventChannelSO _requestChannel;
        [Header("Optional destruction condition")]
        [SerializeField] private MonoBehaviour[] _destructionTargetSources;
        [SerializeField] private WeaponDefinitionSO _requiredWeapon;
        [SerializeField] private StringEventChannelSO _completionChannel;

        private ITutorialDestructionTarget[] _destructionTargets;
        private Action<WeaponDefinitionSO>[] _destructionHandlers;
        private bool[] _isTargetDestroyed;
        private int _destroyedCount;
        private bool _hasTriggered;
        private bool _conditionCompleted;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (!trigger.isTrigger)
            {
                Debug.LogError("TutorialTrigger requires a Collider2D configured as a trigger.", this);
                enabled = false;
            }

            CacheDestructionTargets();
        }

        private void OnEnable()
        {
            _hasTriggered = false;
            _conditionCompleted = false;
            _destroyedCount = default;

            for (int index = default; index < _destructionTargets.Length; index++)
            {
                _isTargetDestroyed[index] = false;
                _destructionTargets[index].DestroyedByWeapon += _destructionHandlers[index];
            }
        }

        private void OnDisable()
        {
            for (int index = default; index < _destructionTargets.Length; index++)
            {
                _destructionTargets[index].DestroyedByWeapon -= _destructionHandlers[index];
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered || _step == null || _requestChannel == null) return;
            if (other.GetComponentInParent<PlayerInputReader>() == null) return;

            _hasTriggered = true;
            _requestChannel.Raise(_step.CreateRequest());
            if (_conditionCompleted) RaiseCompletion();
        }

        private void CacheDestructionTargets()
        {
            int sourceCount = _destructionTargetSources != null ? _destructionTargetSources.Length : default;
            var targets = new ITutorialDestructionTarget[sourceCount];
            int targetCount = default;

            for (int index = default; index < sourceCount; index++)
            {
                MonoBehaviour source = _destructionTargetSources[index];
                if (source == null) continue;

                // Dragging a GameObject into a MonoBehaviour slot binds whichever component comes first,
                // so fall back to the target sitting next to it on the same GameObject.
                ITutorialDestructionTarget target =
                    source as ITutorialDestructionTarget ?? source.GetComponent<ITutorialDestructionTarget>();

                if (target == null)
                {
                    Debug.LogError(
                        $"'{source.gameObject.name}' has no ITutorialDestructionTarget component.", this);
                    continue;
                }

                if (Array.IndexOf(targets, target, default, targetCount) >= 0) continue;

                targets[targetCount] = target;
                targetCount++;
            }

            _destructionTargets = new ITutorialDestructionTarget[targetCount];
            _destructionHandlers = new Action<WeaponDefinitionSO>[targetCount];
            _isTargetDestroyed = new bool[targetCount];

            for (int index = default; index < targetCount; index++)
            {
                int targetIndex = index;
                _destructionTargets[index] = targets[index];
                _destructionHandlers[index] = sourceWeapon => OnTargetDestroyed(targetIndex, sourceWeapon);
            }
        }

        private void OnTargetDestroyed(int targetIndex, WeaponDefinitionSO sourceWeapon)
        {
            if (_requiredWeapon != null && sourceWeapon != _requiredWeapon) return;
            if (_isTargetDestroyed[targetIndex]) return;

            _isTargetDestroyed[targetIndex] = true;
            _destroyedCount++;
            if (_destroyedCount < _destructionTargets.Length) return;

            _conditionCompleted = true;
            if (_hasTriggered) RaiseCompletion();
        }

        private void RaiseCompletion()
        {
            if (_completionChannel != null && _step != null) _completionChannel.Raise(_step.StepId);
        }
    }
}
