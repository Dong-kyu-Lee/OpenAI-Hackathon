using System.Collections.Generic;
using Game.Core.Events;
using Game.Data.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.App
{
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        private sealed class LoopPlayback
        {
            public AudioSource Source;
            public float VolumeScale;
        }

        [SerializeField] private AudioSettingsSO _audioSettings;
        [SerializeField] private SfxEventChannelSO _sfxChannel;
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("씬별 BGM")]
        [SerializeField] private string _titleSceneName = "Title";
        [SerializeField] private AudioClip _titleBgm;
        [SerializeField] private string _stageSelectSceneName = "StageSelect";
        [SerializeField] private AudioClip _stageSelectBgm;
        [SerializeField] private string _gameplaySceneName = "MapTest";
        [SerializeField] private AudioClip _gameplayBgm;

        private readonly Dictionary<int, LoopPlayback> _loopPlaybacks = new();

        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("AudioManager는 하나만 활성화할 수 있습니다.", this);
                enabled = false;
                return;
            }

            Instance = this;
            ConfigureSources();
            ApplyVolumes();
        }

        private void OnEnable()
        {
            if (_audioSettings != null) _audioSettings.Changed += ApplyVolumes;
            if (_sfxChannel != null)
            {
                _sfxChannel.OneShotRequested += PlaySfx;
                _sfxChannel.LoopStarted += StartLoopSfx;
                _sfxChannel.LoopStopped += StopLoopSfx;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            PlayBgmForLoadedScenes();
        }

        private void OnDisable()
        {
            if (_audioSettings != null) _audioSettings.Changed -= ApplyVolumes;
            if (_sfxChannel != null)
            {
                _sfxChannel.OneShotRequested -= PlaySfx;
                _sfxChannel.LoopStarted -= StartLoopSfx;
                _sfxChannel.LoopStopped -= StopLoopSfx;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopAllLoopSfx();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayBgm(AudioClip clip, bool restart = false)
        {
            if (_bgmSource == null || clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying && !restart) return;

            _bgmSource.loop = true;
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void StopBgm()
        {
            if (_bgmSource == null) return;
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (_sfxSource == null || clip == null) return;
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        private void StartLoopSfx(int playbackId, AudioClip clip, float volumeScale)
        {
            if (clip == null) return;

            if (!_loopPlaybacks.TryGetValue(playbackId, out LoopPlayback playback))
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                playback = new LoopPlayback { Source = source };
                _loopPlaybacks.Add(playbackId, playback);
            }

            playback.VolumeScale = Mathf.Clamp01(volumeScale);
            playback.Source.volume = GetSfxVolume() * playback.VolumeScale;
            if (playback.Source.clip == clip && playback.Source.isPlaying) return;

            playback.Source.clip = clip;
            playback.Source.Play();
        }

        private void StopLoopSfx(int playbackId)
        {
            if (!_loopPlaybacks.TryGetValue(playbackId, out LoopPlayback playback)) return;

            playback.Source.Stop();
            playback.Source.clip = null;
        }

        private void StopAllLoopSfx()
        {
            foreach (LoopPlayback playback in _loopPlaybacks.Values)
            {
                if (playback.Source == null) continue;
                playback.Source.Stop();
                Destroy(playback.Source);
            }

            _loopPlaybacks.Clear();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryPlaySceneBgm(scene.name);
        }

        private void PlayBgmForLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (TryPlaySceneBgm(SceneManager.GetSceneAt(i).name)) return;
            }
        }

        private bool TryPlaySceneBgm(string sceneName)
        {
            if (sceneName == _titleSceneName)
            {
                PlayBgm(_titleBgm);
                return true;
            }

            if (sceneName == _stageSelectSceneName)
            {
                PlayBgm(_stageSelectBgm);
                return true;
            }

            if (sceneName == _gameplaySceneName)
            {
                PlayBgm(_gameplayBgm);
                return true;
            }

            return false;
        }

        private void ConfigureSources()
        {
            if (_bgmSource != null)
            {
                _bgmSource.playOnAwake = false;
                _bgmSource.loop = true;
                _bgmSource.spatialBlend = 0f;
            }

            if (_sfxSource != null)
            {
                _sfxSource.playOnAwake = false;
                _sfxSource.loop = false;
                _sfxSource.spatialBlend = 0f;
            }
        }

        private void ApplyVolumes()
        {
            if (_bgmSource != null)
                _bgmSource.volume = _audioSettings != null ? _audioSettings.BgmVolume : 1f;

            float sfxVolume = GetSfxVolume();
            if (_sfxSource != null) _sfxSource.volume = sfxVolume;

            foreach (LoopPlayback playback in _loopPlaybacks.Values)
            {
                if (playback.Source != null)
                    playback.Source.volume = sfxVolume * playback.VolumeScale;
            }
        }

        private float GetSfxVolume()
        {
            return _audioSettings != null ? _audioSettings.SfxVolume : 1f;
        }
    }
}
