namespace MyTurn.Domain;

public enum CombatActionType
{
    Attack,

    [DisplayName("Use Consumable")]
    UseConsumable,

    Defend,

    [DisplayName("Change Equipment")]
    ChangeEquipment,

    [DisplayName("View Combatants")]
    ViewCombatants
}
