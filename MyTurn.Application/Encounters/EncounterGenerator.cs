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
        var enemyCount = Math.Clamp(difficulty + random.NextInclusive(0, 2), 1, 4);
        var enemies = Enumerable.Range(0, enemyCount)
            .Select(_ => ChooseEnemy(random))
            .ToArray();

        return new Encounter(encounterSeed, enemies);
    }

    private EnemyDefinition ChooseEnemy(IRandomSource random)
    {
        var definitions = _enemies.Definitions.ToArray();
        var totalWeight = definitions.Sum(definition => Math.Max(0, definition.Weight));
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
