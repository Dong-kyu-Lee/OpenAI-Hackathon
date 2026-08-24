using UnityEngine;

namespace Game.Data.Title
{
    [CreateAssetMenu(
        fileName = "TitleParallaxSettings",
        menuName = "Game/Data/Title/Title Parallax Settings")]
    public sealed class TitleParallaxSettingsSO : ScriptableObject
    {
        private const float MinimumSmoothTime = 0.01f;

        [SerializeField] private Vector2 _maxOffset = new Vector2(60f, 35f);
        [SerializeField, Min(MinimumSmoothTime)] private float _smoothTime = 0.12f;

        public Vector2 MaxOffset => new Vector2(
            Mathf.Max(0f, _maxOffset.x),
            Mathf.Max(0f, _maxOffset.y));

        public float SmoothTime => Mathf.Max(MinimumSmoothTime, _smoothTime);
    }
}
