using Game.Core.Events;
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

        private bool _hasTriggered;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (!trigger.isTrigger)
            {
                Debug.LogError("TutorialTrigger requires a Collider2D configured as a trigger.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _hasTriggered = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered || _step == null || _requestChannel == null)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerInputReader>() == null)
            {
                return;
            }

            _hasTriggered = true;
            _requestChannel.Raise(_step.CreateRequest());
        }
    }
}
