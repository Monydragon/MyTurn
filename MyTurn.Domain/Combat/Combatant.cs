namespace MyTurn.Domain;

public sealed class Combatant
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public CombatTeam Team { get; }
    public StatSet Stats { get; }
    public EquipmentLoadout Equipment { get; }
    public Actor? Actor { get; }
    public EnemyDefinition? EnemyDefinition { get; }
    public int MaxHealth => Math.Max(1, Stats[StatType.Health].CurrentValue);
    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public bool IsDefending { get; private set; }

    public Combatant(
        string name,
        CombatTeam team,
        StatSet stats,
        EquipmentLoadout equipment,
        Actor? actor = null,
        EnemyDefinition? enemyDefinition = null)
    {
        Name = name;
        Team = team;
        Stats = stats;
        Equipment = equipment;
        Actor = actor;
        EnemyDefinition = enemyDefinition;
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage amount cannot be negative.");
        }

        CurrentHealth = Math.Max(0, CurrentHealth - amount);
    }

    public int Heal(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Healing amount cannot be negative.");
        }

        var previousHealth = CurrentHealth;
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);

        return CurrentHealth - previousHealth;
    }

    public void Defend()
    {
        IsDefending = true;
    }

    public void ClearDefending()
    {
        IsDefending = false;
    }
}
