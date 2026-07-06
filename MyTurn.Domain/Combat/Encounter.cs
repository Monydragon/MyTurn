namespace MyTurn.Domain;

public sealed record Encounter(
    int Seed,
    IReadOnlyList<EnemyDefinition> Enemies,
    int DifficultyBudget = 1)
{
    public int ThreatRating => Enemies.Sum(enemy => Math.Max(1, enemy.ThreatRating));
}
