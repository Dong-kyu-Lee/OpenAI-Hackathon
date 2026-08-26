using System;
using UnityEngine;

namespace Game.Core.Events
{
    [CreateAssetMenu(menuName = "Game/Events/SFX Event Channel", fileName = "SfxEventChannel")]
    public sealed class SfxEventChannelSO : ScriptableObject
    {
        public event Action<AudioClip, float> OneShotRequested;
        public event Action<int, AudioClip, float> LoopStarted;
        public event Action<int> LoopStopped;

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null)
            {
                OneShotRequested?.Invoke(clip, Mathf.Clamp01(volumeScale));
            }
        }

        public void StartLoop(int playbackId, AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null)
            {
                LoopStarted?.Invoke(playbackId, clip, Mathf.Clamp01(volumeScale));
            }
        }

        public void StopLoop(int playbackId)
        {
            LoopStopped?.Invoke(playbackId);
        }
    }
}
