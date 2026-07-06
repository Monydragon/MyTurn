using MyTurn.Domain;

namespace MyTurn.Application;

public sealed record ExplorationResult(
    ExplorationState State,
    WorldRoom Room,
    Encounter? Encounter,
    LootReward Reward,
    string Message)
{
    public static ExplorationResult Blocked(WorldRoom room, string message)
    {
        return new ExplorationResult(ExplorationState.Blocked, room, null, LootReward.Empty, message);
    }
}
