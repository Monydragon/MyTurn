namespace MyTurn.Domain;

public sealed class CombatState
{
    private readonly List<Combatant> _enemies;

    public Actor Player { get; }
    public Combatant PlayerCombatant { get; }
    public IReadOnlyList<Combatant> Enemies => _enemies;
    public int Seed { get; }

    public CombatState(Actor player, Combatant playerCombatant, IEnumerable<Combatant> enemies, int seed)
    {
        Player = player;
        PlayerCombatant = playerCombatant;
        _enemies = enemies.ToList();
        Seed = seed;
    }

    public IReadOnlyList<Combatant> LivingEnemies => _enemies.Where(enemy => enemy.IsAlive).ToArray();
    public bool IsVictory => _enemies.Count > 0 && _enemies.All(enemy => !enemy.IsAlive);
    public bool IsDefeat => !PlayerCombatant.IsAlive;
    public bool IsComplete => IsVictory || IsDefeat;
}
