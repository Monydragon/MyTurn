using MyTurn.Domain;

namespace MyTurn.Application;

public interface ITreasureLootService
{
    LootReward ClaimTreasure(Actor actor, WorldSession session, WorldRoom room);
}
