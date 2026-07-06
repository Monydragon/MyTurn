using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class CombatService : ICombatService
{
    
    private readonly IEquipmentService _equipmentService;
    private readonly IItemDefinitionRegistry _items;
    private readonly ILootService _lootService;
    private readonly IStatDefinitionRegistry _statDefinitions;

    public CombatService(
        IEquipmentService equipmentService,
        IItemDefinitionRegistry items,
        ILootService lootService,
        IStatDefinitionRegistry statDefinitions)
    {
        _equipmentService = equipmentService;
        _items = items;
        _lootService = lootService;
        _statDefinitions = statDefinitions;
    }

    public CombatState StartEncounter(Actor player, Encounter encounter, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(encounter);

        var playerCombatant = new Combatant(
            player.Name,
            CombatTeam.Player,
            player.Stats,
            player.Equipment,
            player);

        var enemies = encounter.Enemies.Select(CreateEnemyCombatant);

        return new CombatState(player, playerCombatant, enemies, seed ?? encounter.Seed);
    }

    public IReadOnlyList<Combatant> GetTurnOrder(CombatState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Enemies
            .Append(state.PlayerCombatant)
            .Where(combatant => combatant.IsAlive)
            .OrderByDescending(combatant => combatant.Stats[StatType.Speed].CurrentValue)
            .ThenBy(combatant => combatant.Team)
            .ThenBy(combatant => combatant.Name)
            .ToArray();
    }

    public DamageResult Attack(Combatant attacker, Combatant target, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(random);

        if (!attacker.IsAlive)
        {
            throw new InvalidOperationException("A defeated combatant cannot attack.");
        }

        if (!target.IsAlive)
        {
            throw new InvalidOperationException("A defeated combatant cannot be targeted.");
        }

        var weapon = attacker.Equipment.EquippedWeapon;
        var weaponDamage = random.NextInclusive(weapon.MinDamage, weapon.MaxDamage);
        var attack = attacker.Stats[GetAttackStat(weapon.WeaponType)].CurrentValue;
        var defense = target.Stats[GetDefenseStat(weapon.WeaponType)].CurrentValue;
        var damage = Math.Max(1, weaponDamage + attack - defense / 2);
        var isCritical = random.NextInclusive(1, 100) <= Math.Max(0, attacker.Stats[StatType.CriticalChance].CurrentValue);

        if (isCritical)
        {
            damage *= 2;
        }

        if (target.IsDefending)
        {
            damage = Math.Max(1, (int)Math.Ceiling(damage * 0.5));
            target.ClearDefending();
        }

        target.TakeDamage(damage);

        return new DamageResult(attacker, target, damage, isCritical, !target.IsAlive);
    }

    public CombatActionResolution Defend(Combatant combatant)
    {
        ArgumentNullException.ThrowIfNull(combatant);
        combatant.Defend();

        return new CombatActionResolution(CombatActionType.Defend, true);
    }

    public CombatActionResolution ChangeEquipment(CombatState state, IEquipmentItem item)
    {
        ArgumentNullException.ThrowIfNull(state);
        _equipmentService.Equip(state.Player, item);

        return new CombatActionResolution(CombatActionType.ChangeEquipment, false);
    }

    public HealingResult UseConsumable(CombatState state, string itemId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (_items.Get(itemId) is not ConsumableDefinition consumable)
        {
            throw new InvalidOperationException($"Item '{itemId}' is not a consumable.");
        }

        if (!state.Player.Inventory.Remove(itemId))
        {
            throw new InvalidOperationException($"Item '{consumable.Name}' is not available.");
        }

        var amountHealed = state.PlayerCombatant.Heal(consumable.HealingAmount);

        return new HealingResult(state.PlayerCombatant, consumable, amountHealed);
    }

    public EnemyTurnResult ResolveEnemyTurn(CombatState state, Combatant enemy, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(random);

        if (!enemy.IsAlive || state.IsComplete)
        {
            return new EnemyTurnResult(enemy, EnemyActionType.Defend, null);
        }

        var action = ChooseEnemyAction(enemy, random);

        if (action == EnemyActionType.Defend)
        {
            enemy.Defend();
            return new EnemyTurnResult(enemy, action, null);
        }

        return new EnemyTurnResult(enemy, action, Attack(enemy, state.PlayerCombatant, random));
    }

    public BattleOutcome CompleteVictory(CombatState state, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(random);

        if (!state.IsVictory)
        {
            throw new InvalidOperationException("Victory rewards can only be granted after all enemies are defeated.");
        }

        var defeatedEnemies = state.Enemies
            .Select(enemy => enemy.EnemyDefinition)
            .OfType<EnemyDefinition>()
            .ToArray();
        var reward = _lootService.RollLoot(defeatedEnemies, random);

        state.Player.Inventory.AddCurrency(reward.Currency);

        foreach (var itemReward in reward.Items)
        {
            state.Player.Inventory.Add(itemReward.Item, itemReward.Quantity);
        }

        var experience = defeatedEnemies.Sum(enemy => enemy.ExperienceReward);
        var skillType = GetSkillType(state.Player.Equipment.EquippedWeapon.WeaponType);
        state.Player.Skills[skillType].Leveling.AddExperience(experience);

        return new BattleOutcome(BattleOutcomeType.Victory, reward, skillType, experience);
    }

    public BattleOutcome CompleteDefeat(CombatState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return BattleOutcome.Defeat();
    }

    private Combatant CreateEnemyCombatant(EnemyDefinition enemy)
    {
        var stats = new StatSet(_statDefinitions.Definitions.Select(definition =>
        {
            var value = enemy.Stats.TryGetValue(definition.StatType, out var enemyValue)
                ? enemyValue
                : definition.BaseValue;

            return new Stat(definition.StatType, value, Math.Max(value, definition.MaxValue));
        }));

        return new Combatant(
            enemy.Name,
            CombatTeam.Enemy,
            stats,
            new EquipmentLoadout(enemy.Weapon),
            enemyDefinition: enemy);
    }

    private static EnemyActionType ChooseEnemyAction(Combatant enemy, IRandomSource random)
    {
        var actions = enemy.EnemyDefinition?.Actions ?? [];
        var totalWeight = actions.Sum(action => Math.Max(0, action.Weight));

        if (totalWeight <= 0)
        {
            return EnemyActionType.BasicAttack;
        }

        var roll = random.NextInclusive(1, totalWeight);
        var current = 0;

        foreach (var action in actions)
        {
            current += Math.Max(0, action.Weight);

            if (roll <= current)
            {
                return action.ActionType;
            }
        }

        return EnemyActionType.BasicAttack;
    }

    private static StatType GetAttackStat(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Melee => StatType.MeleeAttack,
            WeaponType.Ranged => StatType.RangedAttack,
            WeaponType.Magic => StatType.MagicAttack,
            _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, "Unsupported weapon type.")
        };
    }

    private static StatType GetDefenseStat(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Melee => StatType.MeleeDefense,
            WeaponType.Ranged => StatType.RangedDefense,
            WeaponType.Magic => StatType.MagicDefense,
            _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, "Unsupported weapon type.")
        };
    }

    private static SkillType GetSkillType(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Melee => SkillType.Melee,
            WeaponType.Ranged => SkillType.Ranged,
            WeaponType.Magic => SkillType.Magic,
            _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, "Unsupported weapon type.")
        };
    }
}
