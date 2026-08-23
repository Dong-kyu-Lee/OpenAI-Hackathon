using Game.Core.Events;
using Game.Data.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.App
{
    /// <summary>
    /// 게임 설정값의 저장과 복원을 담당합니다. 시작할 때 저장된 볼륨과 키 바인딩을 되살리고,
    /// 이후 값이 바뀔 때마다 다시 저장합니다. 설정 화면을 직접 알지는 않습니다.
    /// 시스템 씬의 상태 머신과 같은 오브젝트에 두며 싱글톤이 아닙니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsController : MonoBehaviour
    {
        private const string BgmVolumeKey = "Settings.BgmVolume";
        private const string SfxVolumeKey = "Settings.SfxVolume";
        private const string InputBindingsKey = "Settings.InputBindings";

        [SerializeField] private AudioSettingsSO _audioSettings;
        [SerializeField] private InputActionAsset _inputActions;

        [Header("수신 채널")]
        [SerializeField] private VoidEventChannelSO _bindingsChangedChannel;

        private void Awake()
        {
            // 구독보다 먼저 복원해서, 복원이 그대로 다시 저장으로 이어지지 않게 합니다.
            RestoreAudioSettings();
            RestoreInputBindings();
        }

        private void OnEnable()
        {
            if (_audioSettings != null)
            {
                _audioSettings.Changed += SaveAudioSettings;
            }

            if (_bindingsChangedChannel != null)
            {
                _bindingsChangedChannel.Raised += SaveInputBindings;
            }
        }

        private void OnDisable()
        {
            if (_audioSettings != null)
            {
                _audioSettings.Changed -= SaveAudioSettings;
            }

            if (_bindingsChangedChannel != null)
            {
                _bindingsChangedChannel.Raised -= SaveInputBindings;
            }
        }

        private void RestoreAudioSettings()
        {
            if (_audioSettings == null)
            {
                Debug.LogError("오디오 설정 자산이 연결되지 않아 볼륨을 복원할 수 없습니다.", this);
                return;
            }

            // 저장된 값이 없으면 자산에 들어 있는 기본값을 그대로 씁니다.
            float bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, _audioSettings.BgmVolume);
            float sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, _audioSettings.SfxVolume);

            _audioSettings.Restore(bgmVolume, sfxVolume);
        }

        private void RestoreInputBindings()
        {
            if (_inputActions == null)
            {
                Debug.LogError("입력 액션 자산이 연결되지 않아 키 설정을 복원할 수 없습니다.", this);
                return;
            }

            string json = PlayerPrefs.GetString(InputBindingsKey, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            _inputActions.LoadBindingOverridesFromJson(json);
        }

        private void SaveAudioSettings()
        {
            if (_audioSettings == null)
            {
                return;
            }

            PlayerPrefs.SetFloat(BgmVolumeKey, _audioSettings.BgmVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, _audioSettings.SfxVolume);
            PlayerPrefs.Save();
        }

        private void SaveInputBindings()
        {
            if (_inputActions == null)
            {
                return;
            }

            PlayerPrefs.SetString(InputBindingsKey, _inputActions.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }
    }
}
