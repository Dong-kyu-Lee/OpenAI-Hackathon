using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Data/Weapon Loadout", fileName = "WeaponLoadout")]
    public sealed class WeaponLoadoutSO : ScriptableObject
    {
        [SerializeField] private float _switchDuration = 1f;

        public float SwitchDuration => _switchDuration;
    }
}
