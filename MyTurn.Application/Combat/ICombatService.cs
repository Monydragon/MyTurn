using MyTurn.Domain;

namespace MyTurn.Application;

public interface ICombatService
{
    CombatState StartEncounter(Party party, Encounter encounter, int? seed = null);
    CombatState StartEncounter(Actor player, Encounter encounter, int? seed = null);
    IReadOnlyList<Combatant> GetTurnOrder(CombatState state);
    DamageResult Attack(Combatant attacker, Combatant target, IRandomSource random);
    CombatActionResolution Defend(Combatant combatant);
    CombatActionResolution ChangeEquipment(CombatState state, Combatant combatant, IEquipmentItem item);
    CombatActionResolution ChangeEquipment(CombatState state, IEquipmentItem item);
    HealingResult UseConsumable(CombatState state, string itemId);
    EnemyTurnResult ResolveEnemyTurn(CombatState state, Combatant enemy, IRandomSource random);
    BattleOutcome CompleteVictory(CombatState state, IRandomSource random);
    BattleOutcome CompleteDefeat(CombatState state);
}
