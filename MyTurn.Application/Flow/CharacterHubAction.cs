using MyTurn.Domain;

namespace MyTurn.Application;

public enum CharacterHubAction
{
    [DisplayName("Explore World")]
    ExploreWorld,

    [DisplayName("Fight Encounter")]
    FightEncounter,

    [DisplayName("View Party")]
    ViewParty,

    [DisplayName("Manage Party")]
    ManageParty,

    [DisplayName("View Character")]
    ViewCharacter,

    [DisplayName("View Inventory")]
    ViewInventory,

    [DisplayName("View Stats")]
    ViewStats,

    [DisplayName("View Skills")]
    ViewSkills,

    [DisplayName("View Equipment")]
    ViewEquipment,

    [DisplayName("Use Item")]
    UseItem,

    [DisplayName("Equip Gear")]
    EquipGear,

    [DisplayName("Back To Main Menu")]
    BackToMainMenu,

    Exit
}
