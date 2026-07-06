using MyTurn.Domain;

namespace MyTurn.Application;

public interface ILootService
{
    LootReward RollLoot(IEnumerable<EnemyDefinition> enemies, IRandomSource random);
}
