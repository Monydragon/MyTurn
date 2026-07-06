using MyTurn.Domain;

namespace MyTurn.Application;

public interface ITreasureLootService
{
    LootReward ClaimTreasure(Party party, WorldSession session, WorldRoom room);
    LootReward ClaimTreasure(Actor actor, WorldSession session, WorldRoom room);
}
