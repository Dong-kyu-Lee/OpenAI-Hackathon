using System;
using Game.Data.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Options
{
    /// <summary>
    /// 배경음과 효과음 볼륨을 조절하는 설정 화면입니다. 키 설정은 자식의 목록 컴포넌트가 담당합니다.
    /// 값은 설정 자산에 쓰고, 저장은 App 어셈블리의 설정 담당자가 처리합니다.
    /// 이 컴포넌트는 자기 오브젝트를 직접 켜고 끄므로, 옵션 패널 루트 오브젝트에 붙입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OptionsPanelUI : MonoBehaviour
    {
        [SerializeField] private AudioSettingsSO _audioSettings;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Button _backButton;

        /// <summary>뒤로가기로 화면이 닫혔을 때 발생합니다.</summary>
        public event Action Closed;

        /// <summary>설정 화면이 열려 있는지 여부를 가져옵니다.</summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>설정 화면을 엽니다.</summary>
        public void Open()
        {
            gameObject.SetActive(true);
        }

        /// <summary>설정 화면을 닫습니다. 이미 닫혀 있으면 아무 일도 하지 않습니다.</summary>
        public void Close()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void Awake()
        {
            ApplySliderRange(_bgmSlider);
            ApplySliderRange(_sfxSlider);
        }

        private void OnEnable()
        {
            // 열릴 때마다 현재 값으로 맞춥니다. 이때 변경 통지가 되돌아오지 않도록 알림 없이 씁니다.
            SyncSlidersFromSettings();

            if (_bgmSlider != null)
            {
                _bgmSlider.onValueChanged.AddListener(SetBgmVolume);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.AddListener(SetSfxVolume);
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(Close);
            }
        }

        private void OnDisable()
        {
            if (_bgmSlider != null)
            {
                _bgmSlider.onValueChanged.RemoveListener(SetBgmVolume);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(Close);
            }
        }

        private static void ApplySliderRange(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = AudioSettingsSO.MinVolume;
            slider.maxValue = AudioSettingsSO.MaxVolume;
        }

        private void SyncSlidersFromSettings()
        {
            if (_audioSettings == null)
            {
                Debug.LogError("오디오 설정 자산이 연결되지 않아 슬라이더를 채울 수 없습니다.", this);
                return;
            }

            if (_bgmSlider != null)
            {
                _bgmSlider.SetValueWithoutNotify(_audioSettings.BgmVolume);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.SetValueWithoutNotify(_audioSettings.SfxVolume);
            }
        }

        private void SetBgmVolume(float volume)
        {
            if (_audioSettings != null)
            {
                _audioSettings.SetBgmVolume(volume);
            }
        }

        private void SetSfxVolume(float volume)
        {
            if (_audioSettings != null)
            {
                _audioSettings.SetSfxVolume(volume);
            }
        }
    }
}
