using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class EncounterGenerator : IEncounterGenerator
{
    private readonly IEnemyDefinitionRegistry _enemies;

    public EncounterGenerator(IEnemyDefinitionRegistry enemies)
    {
        _enemies = enemies;
    }

    public Encounter Generate(int difficulty = 1, int? seed = null)
    {
        var encounterSeed = seed ?? Environment.TickCount;
        var random = new SeededRandomSource(encounterSeed);
        var difficultyBudget = Math.Max(1, difficulty * 2 + random.NextInclusive(0, 2));
        var enemies = ChooseEnemies(random, difficultyBudget);

        return new Encounter(encounterSeed, enemies, difficultyBudget);
    }

    private IReadOnlyList<EnemyDefinition> ChooseEnemies(IRandomSource random, int difficultyBudget)
    {
        var enemies = new List<EnemyDefinition>();
        var remainingBudget = difficultyBudget;

        while (enemies.Count < 4 && remainingBudget > 0)
        {
            var enemy = ChooseEnemy(random, remainingBudget);
            enemies.Add(enemy);
            remainingBudget -= Math.Max(1, enemy.ThreatRating);

            if (enemies.Count > 0 && random.NextInclusive(1, 100) > 65)
            {
                break;
            }
        }

        return enemies.Count == 0 ? [ChooseEnemy(random, difficultyBudget)] : enemies;
    }

    private EnemyDefinition ChooseEnemy(IRandomSource random, int remainingBudget)
    {
        var definitions = _enemies.Definitions
            .Where(definition => definition.Enemy.ThreatRating <= Math.Max(1, remainingBudget))
            .ToArray();

        if (definitions.Length == 0)
        {
            definitions = _enemies.Definitions.ToArray();
        }

        var totalWeight = definitions.Sum(definition => Math.Max(0, definition.Weight));

        if (totalWeight <= 0)
        {
            return definitions[0].Enemy;
        }

        var roll = random.NextInclusive(1, totalWeight);
        var current = 0;

        foreach (var definition in definitions)
        {
            current += Math.Max(0, definition.Weight);

            if (roll <= current)
            {
                return definition.Enemy;
            }
        }

        return definitions[^1].Enemy;
    }
}
