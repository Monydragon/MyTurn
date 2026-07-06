namespace MyTurn.Domain;

public sealed class Actor : IActor
{
    public Guid Id { get; }
    public string Name { get; set; }
    public SkillSet Skills { get; }
    public StatSet Stats { get; }
    public EquipmentLoadout Equipment { get; }
    public Inventory Inventory { get; }
    public long Steps { get; private set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public Species Species { get; set; }
    public CharacterClass CharacterClass { get; set; }

    public Actor(
        string name,
        int age,
        Gender gender,
        Species species,
        CharacterClass characterClass,
        SkillSet skills,
        StatSet stats,
        EquipmentLoadout equipment,
        Inventory inventory,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Name = name;
        Age = age;
        Gender = gender;
        Species = species;
        CharacterClass = characterClass;
        Skills = skills;
        Stats = stats;
        Equipment = equipment;
        Inventory = inventory;
    }

    public void AddSteps(long amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Step amount cannot be negative.");
        }

        Steps += amount;
    }

    public void ResetSteps()
    {
        Steps = 0;
    }
}
