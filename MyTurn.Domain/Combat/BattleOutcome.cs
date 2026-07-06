namespace MyTurn.Domain;

public sealed record BattleOutcome(
    BattleOutcomeType OutcomeType,
    LootReward Reward,
    SkillType? ExperienceSkill,
    int ExperienceAwarded)
{
    public static BattleOutcome Defeat()
    {
        return new BattleOutcome(BattleOutcomeType.Defeat, LootReward.Empty, null, 0);
    }
}
