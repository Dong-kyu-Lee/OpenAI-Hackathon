using System;
using UnityEngine;

namespace Game.Data.Settings
{
    /// <summary>
    /// 배경음과 효과음의 현재 볼륨을 담는 설정 자산입니다.
    /// 값을 실제 믹서에 적용하는 것은 이 자산의 책임이 아니며,
    /// <see cref="Changed"/>를 구독한 오디오 담당자가 수행합니다.
    /// 저장 역시 이 자산이 하지 않고 App 어셈블리의 설정 담당자가 맡습니다.
    /// </summary>
    /// <remarks>
    /// 에디터에서 Play 중에 바꾼 값은 이 자산 파일에 남습니다. 실제 저장소는 PlayerPrefs이므로
    /// 빌드 동작에는 영향이 없지만, 인스펙터에 보이는 값이 마지막 플레이 값일 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "Game/Settings/Audio Settings")]
    public sealed class AudioSettingsSO : ScriptableObject
    {
        /// <summary>볼륨으로 지정할 수 있는 최솟값입니다.</summary>
        public const float MinVolume = 0f;

        /// <summary>볼륨으로 지정할 수 있는 최댓값입니다.</summary>
        public const float MaxVolume = 1f;

        private const float DefaultVolume = 0.8f;

        [SerializeField, Range(MinVolume, MaxVolume)] private float _bgmVolume = DefaultVolume;
        [SerializeField, Range(MinVolume, MaxVolume)] private float _sfxVolume = DefaultVolume;

        /// <summary>볼륨 중 하나라도 바뀌었을 때 발생합니다.</summary>
        public event Action Changed;

        /// <summary>현재 배경음 볼륨을 가져옵니다.</summary>
        public float BgmVolume => _bgmVolume;

        /// <summary>현재 효과음 볼륨을 가져옵니다.</summary>
        public float SfxVolume => _sfxVolume;

        /// <summary>배경음 볼륨을 설정합니다. 값이 실제로 바뀐 경우에만 통지합니다.</summary>
        /// <param name="volume">0과 1 사이의 볼륨 값입니다. 범위를 벗어나면 잘립니다.</param>
        public void SetBgmVolume(float volume)
        {
            float clamped = Mathf.Clamp(volume, MinVolume, MaxVolume);

            if (Mathf.Approximately(_bgmVolume, clamped))
            {
                return;
            }

            _bgmVolume = clamped;
            Changed?.Invoke();
        }

        /// <summary>효과음 볼륨을 설정합니다. 값이 실제로 바뀐 경우에만 통지합니다.</summary>
        /// <param name="volume">0과 1 사이의 볼륨 값입니다. 범위를 벗어나면 잘립니다.</param>
        public void SetSfxVolume(float volume)
        {
            float clamped = Mathf.Clamp(volume, MinVolume, MaxVolume);

            if (Mathf.Approximately(_sfxVolume, clamped))
            {
                return;
            }

            _sfxVolume = clamped;
            Changed?.Invoke();
        }

        /// <summary>
        /// 저장된 값을 되돌릴 때 사용합니다. 통지는 마지막에 한 번만 발생합니다.
        /// </summary>
        /// <param name="bgmVolume">복원할 배경음 볼륨입니다.</param>
        /// <param name="sfxVolume">복원할 효과음 볼륨입니다.</param>
        public void Restore(float bgmVolume, float sfxVolume)
        {
            _bgmVolume = Mathf.Clamp(bgmVolume, MinVolume, MaxVolume);
            _sfxVolume = Mathf.Clamp(sfxVolume, MinVolume, MaxVolume);
            Changed?.Invoke();
        }
    }
}
