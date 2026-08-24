using System;

namespace Game.Core.Tutorial
{
    [Flags]
    public enum TutorialInputPermission
    {
        None = 0,
        Jump = 1 << 0,
        Slide = 1 << 1,
        Attack = 1 << 2,
        Aim = 1 << 3,
        WeaponSelection = 1 << 4,
        All = Jump | Slide | Attack | Aim | WeaponSelection
    }
}
