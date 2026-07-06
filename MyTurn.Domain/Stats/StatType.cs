namespace MyTurn.Domain;

public enum StatType
{
    Health,

    [DisplayName("Melee Attack")]
    MeleeAttack,

    [DisplayName("Melee Defense")]
    MeleeDefense,

    [DisplayName("Ranged Attack")]
    RangedAttack,

    [DisplayName("Ranged Defense")]
    RangedDefense,

    [DisplayName("Magic Attack")]
    MagicAttack,

    [DisplayName("Magic Defense")]
    MagicDefense,

    [DisplayName("Critical Chance")]
    CriticalChance,

    Speed
}
