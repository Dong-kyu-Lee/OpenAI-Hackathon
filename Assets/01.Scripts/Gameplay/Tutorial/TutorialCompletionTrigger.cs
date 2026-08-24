using Game.Core.Events;
using Game.Gameplay.Player;
using UnityEngine;

namespace Game.Gameplay.Tutorial
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class TutorialCompletionTrigger : MonoBehaviour
    {
        [SerializeField] private string _stepId;
        [SerializeField] private StringEventChannelSO _completionChannel;

        private bool _hasCompleted;

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (!trigger.isTrigger)
            {
                Debug.LogError("TutorialCompletionTrigger requires a trigger Collider2D.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _hasCompleted = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasCompleted || other.GetComponentInParent<PlayerInputReader>() == null)
                return;

            _hasCompleted = true;
            _completionChannel?.Raise(_stepId);
        }
    }
}
