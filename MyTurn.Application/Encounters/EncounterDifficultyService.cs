using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class EncounterDifficultyService : IEncounterDifficultyService
{
    public int GetPartyPower(Party party)
    {
        ArgumentNullException.ThrowIfNull(party);

        return party.ActiveMembers.Sum(member =>
        {
            var attack = Math.Max(
                member.Stats[StatType.MeleeAttack].CurrentValue,
                Math.Max(member.Stats[StatType.RangedAttack].CurrentValue, member.Stats[StatType.MagicAttack].CurrentValue));
            var defense = Math.Max(
                member.Stats[StatType.MeleeDefense].CurrentValue,
                Math.Max(member.Stats[StatType.RangedDefense].CurrentValue, member.Stats[StatType.MagicDefense].CurrentValue));
            var health = member.Stats[StatType.Health].CurrentValue / 25;
            var speed = member.Stats[StatType.Speed].CurrentValue / 5;

            return Math.Max(1, attack + defense + health + speed);
        });
    }

    public int GetDifficultyBudget(Party party, int worldDepth = 1)
    {
        var partyPower = GetPartyPower(party);
        var activeCount = Math.Max(1, party.ActiveMembers.Count);
        var depthBonus = Math.Max(0, worldDepth - 1);

        return Math.Max(1, (partyPower / activeCount) + activeCount + depthBonus);
    }
}
