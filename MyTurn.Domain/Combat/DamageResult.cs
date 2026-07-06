namespace MyTurn.Domain;

public sealed record DamageResult(
    Combatant Attacker,
    Combatant Target,
    int Damage,
    bool IsCritical,
    bool TargetDefeated);
