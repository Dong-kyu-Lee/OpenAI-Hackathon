using System.Collections;
using System.Collections.Generic;
using Game.Data.Settings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.App
{
    /// <summary>
    /// Registers buttons once after each scene load and plays the shared UI click sound.
    /// The one-frame delay includes UI elements instantiated from Start methods.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class UIButtonClickSoundPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip _clickClip;
        [SerializeField] private AudioSettingsSO _audioSettings;

        private readonly Dictionary<Button, UnityAction> _listeners = new();
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(RegisterButtonsAfterSceneInitialization());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopAllCoroutines();
            UnregisterAllButtons();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(RegisterButtonsAfterSceneInitialization());
        }

        private IEnumerator RegisterButtonsAfterSceneInitialization()
        {
            yield return null;

            RemoveDestroyedButtonReferences();

            Button[] buttons = FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Button button in buttons)
            {
                Register(button);
            }
        }

        private void Register(Button button)
        {
            if (button == null || _listeners.ContainsKey(button))
            {
                return;
            }

            UnityAction listener = () => PlayClickSound(button);
            button.onClick.AddListener(listener);
            _listeners.Add(button, listener);
        }

private void PlayClickSound(Button button)
        {
            // Another click listener may hide the panel or change interactability before this
            // listener runs. Reaching onClick already proves that Unity accepted the click.
            if (button == null || _clickClip == null)
            {
                return;
            }

            float volume = _audioSettings != null ? _audioSettings.SfxVolume : 1f;
            _audioSource.PlayOneShot(_clickClip, volume);
        }

        private void RemoveDestroyedButtonReferences()
        {
            List<Button> destroyedButtons = null;

            foreach (Button button in _listeners.Keys)
            {
                if (button != null)
                {
                    continue;
                }

                destroyedButtons ??= new List<Button>();
                destroyedButtons.Add(button);
            }

            if (destroyedButtons == null)
            {
                return;
            }

            foreach (Button button in destroyedButtons)
            {
                _listeners.Remove(button);
            }
        }

        private void UnregisterAllButtons()
        {
            foreach (KeyValuePair<Button, UnityAction> entry in _listeners)
            {
                if (entry.Key != null)
                {
                    entry.Key.onClick.RemoveListener(entry.Value);
                }
            }

            _listeners.Clear();
        }
    }
}
