using Game.Data;

namespace Game.Gameplay.Combat
{
    public interface IWeaponDamageable
    {
        void TakeDamage(float amount, WeaponDefinitionSO sourceWeapon);
    }
}
