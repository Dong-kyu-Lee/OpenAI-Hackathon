using UnityEngine;

namespace Game.Gameplay.Weapon
{
    public sealed class LaserBeamVisual : MonoBehaviour
    {
        private const int MinSegmentCount = 2;
        private const float MinBeamLength = 0.0001f;
        private const float NoiseSeedRange = 100f;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("UV Scroll")]
        [SerializeField] private float _scrollSpeed = -4f;
        [SerializeField] private float _tilesPerUnit = 0.5f;

        [Header("Width Pulse")]
        [SerializeField] private float _widthMultiplier = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _pulseAmount = 0.15f;
        [SerializeField] private float _pulseSpeed = 12f;

        [Header("Ignition Punch")]
        [SerializeField] private float _ignitionScale = 2.2f;
        [SerializeField] private float _ignitionDuration = 0.08f;

        [Header("Brightness Flicker")]
        [SerializeField] private float _brightness = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float _flickerAmount = 0.25f;
        [SerializeField] private float _flickerSpeed = 9f;

        [Header("Beam Wobble")]
        [SerializeField] private int _segmentCount = 14;
        [SerializeField] private float _wobbleAmplitude = 0.09f;
        [SerializeField] private float _wobbleFrequency = 2.5f;
        [SerializeField] private float _wobbleSpeed = 7f;

        private LineRenderer _lineRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3[] _positions;
        private Color _materialBaseColor = Color.white;
        private Color _materialEmissionColor = Color.black;
        private bool _hasBaseMap;
        private bool _hasBaseColor;
        private bool _hasEmissionColor;
        private float _scrollOffset;
        private float _noiseSeed;
        private float _playStartTime;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Play()
        {
            EnsureInitialized();
            if (_lineRenderer == null)
            {
                return;
            }

            _playStartTime = Time.time;
            _scrollOffset = 0f;
            _lineRenderer.enabled = true;
        }

        public void Stop()
        {
            EnsureInitialized();
            if (_lineRenderer == null)
            {
                return;
            }

            _lineRenderer.enabled = false;
        }

        public void SetBeam(Vector3 start, Vector3 end)
        {
            if (_lineRenderer == null)
            {
                return;
            }

            Vector3 delta = end - start;
            float length = delta.magnitude;
            Vector3 direction = length > MinBeamLength ? delta / length : Vector3.right;

            ApplyPositions(start, direction, length);
            ApplyWidth();
            ApplyMaterial(length, Time.deltaTime);
        }

        private void EnsureInitialized()
        {
            if (_lineRenderer != null)
            {
                return;
            }

            _lineRenderer = GetComponent<LineRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
            _noiseSeed = Random.value * NoiseSeedRange;
            CacheMaterialProperties();
        }

        private void CacheMaterialProperties()
        {
            Material material = _lineRenderer != null ? _lineRenderer.sharedMaterial : null;
            if (material == null)
            {
                return;
            }

            _hasBaseMap = material.HasProperty(BaseMapId);
            _hasBaseColor = material.HasProperty(BaseColorId);
            _hasEmissionColor = material.HasProperty(EmissionColorId);

            if (_hasBaseColor)
            {
                _materialBaseColor = material.GetColor(BaseColorId);
            }

            if (_hasEmissionColor)
            {
                _materialEmissionColor = material.GetColor(EmissionColorId);
            }
        }

        private void ApplyPositions(Vector3 start, Vector3 direction, float length)
        {
            int segmentCount = Mathf.Max(MinSegmentCount, _segmentCount);
            if (_positions == null || _positions.Length != segmentCount)
            {
                _positions = new Vector3[segmentCount];
            }

            // 2D 사이드뷰라 빔에 수직인 축은 화면 평면 안의 법선 하나로 충분하다.
            Vector3 normal = new Vector3(-direction.y, direction.x, 0f);
            int lastIndex = segmentCount - 1;
            float noiseTime = Time.time * _wobbleSpeed;

            for (int i = 0; i < segmentCount; i++)
            {
                float ratio = (float)i / lastIndex;
                Vector3 point = start + direction * (length * ratio);

                if (_wobbleAmplitude > 0f && i > 0 && i < lastIndex)
                {
                    // 양 끝은 총구와 피격 지점에 붙어 있어야 하므로 진폭을 0으로 수렴시킨다.
                    float envelope = Mathf.Sin(ratio * Mathf.PI);
                    float noise = Mathf.PerlinNoise(_noiseSeed + ratio * _wobbleFrequency, noiseTime) * 2f - 1f;
                    point += normal * (noise * _wobbleAmplitude * envelope);
                }

                _positions[i] = point;
            }

            _lineRenderer.positionCount = segmentCount;
            _lineRenderer.SetPositions(_positions);
        }

        private void ApplyWidth()
        {
            float pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
            float ignition = 1f;

            if (_ignitionDuration > 0f)
            {
                float ignitionRatio = (Time.time - _playStartTime) / _ignitionDuration;
                if (ignitionRatio < 1f)
                {
                    ignition = Mathf.Lerp(_ignitionScale, 1f, ignitionRatio);
                }
            }

            _lineRenderer.widthMultiplier = _widthMultiplier * pulse * ignition;
        }

        private void ApplyMaterial(float length, float deltaTime)
        {
            _scrollOffset = Mathf.Repeat(_scrollOffset + _scrollSpeed * deltaTime, 1f);
            _lineRenderer.GetPropertyBlock(_propertyBlock);

            // 셰이더가 스크롤을 직접 하는 경우(Laser2D)에는 여기서 _BaseMap_ST를 덮어쓰면
            // 머테리얼의 Tiling/Offset 설정까지 같이 지워진다. 속도가 0이면 손대지 않는다.
            if (_hasBaseMap && !Mathf.Approximately(_scrollSpeed, 0f))
            {
                // Tile 모드는 LineRenderer가 길이에 비례한 UV를 이미 만들어 주므로 타일링을 건드리지 않는다.
                float tilingX = _lineRenderer.textureMode == LineTextureMode.Stretch
                    ? Mathf.Max(1f, length * _tilesPerUnit)
                    : 1f;

                _propertyBlock.SetVector(BaseMapStId, new Vector4(tilingX, 1f, _scrollOffset, 0f));
            }

            float flickerNoise = Mathf.PerlinNoise(_noiseSeed, Time.time * _flickerSpeed) * 2f - 1f;
            float intensity = _brightness * (1f + flickerNoise * _flickerAmount);

            if (_hasBaseColor)
            {
                _propertyBlock.SetColor(BaseColorId, ScaleRgb(_materialBaseColor, intensity));
            }

            if (_hasEmissionColor)
            {
                _propertyBlock.SetColor(EmissionColorId, ScaleRgb(_materialEmissionColor, intensity));
            }

            _lineRenderer.SetPropertyBlock(_propertyBlock);
        }

        private static Color ScaleRgb(Color color, float scale)
        {
            return new Color(color.r * scale, color.g * scale, color.b * scale, color.a);
        }
    }
}
