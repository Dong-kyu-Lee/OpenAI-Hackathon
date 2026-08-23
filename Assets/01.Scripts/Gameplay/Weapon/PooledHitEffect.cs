using Game.Core.Pooling;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    [DisallowMultipleComponent]
    public sealed class PooledHitEffect : MonoBehaviour, IPoolable
    {
        private const float MinimumNormalMagnitude = 0.0001f;

        [Tooltip("이펙트의 루트 ParticleSystem입니다. 비워두면 같은 GameObject에서 자동으로 찾습니다.")]
        [SerializeField] private ParticleSystem _rootParticle;
        [Tooltip("피격 표면의 법선 방향으로 이펙트를 회전시킵니다.")]
        [SerializeField] private bool _alignToSurfaceNormal = true;
        [Tooltip("파티클이 끝난 뒤 풀에 반환하기까지 추가로 대기할 시간(초)입니다.")]
        [SerializeField] private float _extraLingerTime;

        private float _oneShotDuration;
        private float _maxParticleLifetime;
        private float _remainingTime;
        private bool _isLooping;
        private bool _isPlaying;
        private bool _isReleasing;
        private bool _isInitialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            // 풀에서 재사용될 때 Awake는 다시 호출되지 않으므로 여기서 상태를 되돌린다.
            _isPlaying = false;
            _isReleasing = false;
            _remainingTime = 0f;
        }

        private void OnDisable()
        {
            // CFX_AutoDestructShuriken 처럼 외부 스크립트가 직접 SetActive(false)를 호출하면
            // Update가 멈춰 풀에 반환되지 못하고 인스턴스가 그대로 새어 나간다. 여기서 회수한다.
            // 풀이 반환 과정에서 비활성화한 경우에는 _isPlaying이 이미 false라 다시 반환하지 않는다.
            if (!_isPlaying)
            {
                return;
            }

            // 씬 언로드나 플레이 종료 중에는 풀 자료구조를 건드리지 않는다.
            if (!gameObject.scene.isLoaded)
            {
                return;
            }

            ReturnToPool();
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            // 루프 이펙트는 Release()로 종료를 요청받기 전까지 스스로 반환하지 않는다.
            if (_isLooping && !_isReleasing)
            {
                return;
            }

            _remainingTime -= Time.deltaTime;
            if (_remainingTime > 0f)
            {
                return;
            }

            ReturnToPool();
        }

        public void Play(Vector3 position, Vector3 normal)
        {
            EnsureInitialized();
            ApplyTransform(position, normal);

            _isPlaying = true;
            _isReleasing = false;
            _remainingTime = _isLooping
                ? 0f
                : _oneShotDuration + Mathf.Max(0f, _extraLingerTime);

            if (_rootParticle == null)
            {
                return;
            }

            _rootParticle.Clear(true);
            _rootParticle.Play(true);
        }

        public void Follow(Vector3 position, Vector3 normal)
        {
            if (!_isPlaying || _isReleasing)
            {
                return;
            }

            ApplyTransform(position, normal);
        }

        public void SetEmitting(bool isEmitting)
        {
            if (!_isPlaying || _isReleasing || _rootParticle == null)
            {
                return;
            }

            if (isEmitting)
            {
                if (!_rootParticle.isEmitting)
                {
                    _rootParticle.Play(true);
                }

                return;
            }

            if (_rootParticle.isEmitting)
            {
                _rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public void Release()
        {
            if (!_isPlaying || _isReleasing)
            {
                return;
            }

            // 즉시 지우면 남은 파티클이 뚝 끊기므로 방출만 멈추고 수명만큼 더 살려 둔다.
            _isReleasing = true;
            _remainingTime = _maxParticleLifetime + Mathf.Max(0f, _extraLingerTime);
            _rootParticle?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public void OnDespawned()
        {
            _isPlaying = false;
            _isReleasing = false;
            _remainingTime = 0f;

            if (_rootParticle == null)
            {
                return;
            }

            _rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _rootParticle.Clear(true);
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            if (_rootParticle == null)
            {
                _rootParticle = GetComponent<ParticleSystem>();
            }

            if (_rootParticle == null)
            {
                // 재생할 파티클이 없으면 다음 Update에서 바로 풀로 돌아가게 둔다.
                Debug.LogError("Root ParticleSystem is not assigned.", this);
                _isLooping = false;
                return;
            }

            _isLooping = _rootParticle.main.loop;

            // 재생 시간을 코드에 상수로 두지 않고 프리팹의 파티클 설정에서 계산한다.
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                float startDelay = GetMaxValue(main.startDelay);
                float startLifetime = GetMaxValue(main.startLifetime);

                _maxParticleLifetime = Mathf.Max(_maxParticleLifetime, startLifetime);
                _oneShotDuration = Mathf.Max(
                    _oneShotDuration,
                    startDelay + main.duration + startLifetime);
            }
        }

        private void ApplyTransform(Vector3 position, Vector3 normal)
        {
            Quaternion rotation = transform.rotation;
            if (_alignToSurfaceNormal && normal.sqrMagnitude >= MinimumNormalMagnitude)
            {
                rotation = Quaternion.FromToRotation(Vector3.up, normal);
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        private void ReturnToPool()
        {
            _isPlaying = false;
            _isReleasing = false;

            ObjectPoolManager poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("ObjectPoolManager is not available.", this);
                gameObject.SetActive(false);
                return;
            }

            poolManager.Return(this);
        }

        private static float GetMaxValue(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;
                case ParticleSystemCurveMode.Curve:
                case ParticleSystemCurveMode.TwoCurves:
                    return curve.curveMultiplier;
                default:
                    return curve.constantMax;
            }
        }
    }
}
