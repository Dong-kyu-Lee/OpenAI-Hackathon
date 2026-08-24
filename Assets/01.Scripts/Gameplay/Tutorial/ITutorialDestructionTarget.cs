using System;
using Game.Data;

namespace Game.Gameplay.Tutorial
{
    public interface ITutorialDestructionTarget
    {
        event Action<WeaponDefinitionSO> DestroyedByWeapon;
    }
}
