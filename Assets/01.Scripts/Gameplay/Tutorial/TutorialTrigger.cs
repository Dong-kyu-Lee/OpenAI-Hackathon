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
        [SerializeField] private MonoBehaviour _destructionTargetSource;
        [SerializeField] private WeaponDefinitionSO _requiredWeapon;
        [SerializeField] private StringEventChannelSO _completionChannel;

        private ITutorialDestructionTarget _destructionTarget;
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

            _destructionTarget = _destructionTargetSource as ITutorialDestructionTarget;
            if (_destructionTargetSource != null && _destructionTarget == null)
                Debug.LogError("Destruction Target Source must implement ITutorialDestructionTarget.", this);
        }

        private void OnEnable()
        {
            _hasTriggered = false;
            _conditionCompleted = false;
            if (_destructionTarget != null) _destructionTarget.DestroyedByWeapon += OnTargetDestroyed;
        }

        private void OnDisable()
        {
            if (_destructionTarget != null) _destructionTarget.DestroyedByWeapon -= OnTargetDestroyed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered || _step == null || _requestChannel == null) return;
            if (other.GetComponentInParent<PlayerInputReader>() == null) return;

            _hasTriggered = true;
            _requestChannel.Raise(_step.CreateRequest());
            if (_conditionCompleted) RaiseCompletion();
        }

        private void OnTargetDestroyed(WeaponDefinitionSO sourceWeapon)
        {
            if (_requiredWeapon != null && sourceWeapon != _requiredWeapon) return;
            _conditionCompleted = true;
            if (_hasTriggered) RaiseCompletion();
        }

        private void RaiseCompletion()
        {
            if (_completionChannel != null && _step != null) _completionChannel.Raise(_step.StepId);
        }
    }
}
