namespace MyTurn.Domain;

public interface IActor : IEntity
{
    SkillSet Skills { get; }
    StatSet Stats { get; }
    EquipmentLoadout Equipment { get; }
    Inventory Inventory { get; }
    long Steps { get; }
    int Age { get; set; }
    Gender Gender { get; set; }
    Species Species { get; set; }
    CharacterClass CharacterClass { get; set; }
    void AddSteps(long amount);
    void ResetSteps();
}
