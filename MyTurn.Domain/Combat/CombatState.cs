namespace MyTurn.Domain;

public sealed class CombatState
{
    private readonly List<Combatant> _partyCombatants;
    private readonly List<Combatant> _enemies;

    public Party Party { get; }
    public IReadOnlyList<Combatant> PartyCombatants => _partyCombatants;
    public Actor Player => Party.Leader;
    public Combatant PlayerCombatant => _partyCombatants[0];
    public IReadOnlyList<Combatant> Enemies => _enemies;
    public int Seed { get; }

    public CombatState(Party party, IEnumerable<Combatant> partyCombatants, IEnumerable<Combatant> enemies, int seed)
    {
        Party = party ?? throw new ArgumentNullException(nameof(party));
        _partyCombatants = partyCombatants.ToList();
        _enemies = enemies.ToList();
        Seed = seed;

        if (_partyCombatants.Count == 0)
        {
            throw new ArgumentException("Combat requires at least one party combatant.", nameof(partyCombatants));
        }
    }

    public IReadOnlyList<Combatant> LivingPartyMembers => _partyCombatants.Where(member => member.IsAlive).ToArray();
    public IReadOnlyList<Combatant> LivingEnemies => _enemies.Where(enemy => enemy.IsAlive).ToArray();
    public bool IsVictory => _enemies.Count > 0 && _enemies.All(enemy => !enemy.IsAlive);
    public bool IsDefeat => _partyCombatants.All(member => !member.IsAlive);
    public bool IsComplete => IsVictory || IsDefeat;
}
