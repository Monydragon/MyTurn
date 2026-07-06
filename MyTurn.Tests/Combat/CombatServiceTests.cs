using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.Combat;

[TestFixture]
public sealed class CombatServiceTests
{
    [Test]
    public void GetTurnOrder_SortsLivingCombatantsBySpeed()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor();
        var encounter = new Encounter(1, [CreateEnemy("fast", "Fast Enemy", 20), CreateEnemy("slow", "Slow Enemy", 5)]);
        var state = services.CombatService.StartEncounter(actor, encounter);

        var order = services.CombatService.GetTurnOrder(state);

        Assert.That(order.Select(combatant => combatant.Name), Is.EqualTo(new[] { "Fast Enemy", "Avery", "Slow Enemy" }));
    }

    [Test]
    public void Attack_UsesWeaponAttackDefenseMinimumAndCritical()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor();
        var encounter = new Encounter(1, [CreateEnemy("dummy", "Training Dummy", 1, meleeDefense: 4)]);
        var state = services.CombatService.StartEncounter(actor, encounter);

        var normal = services.CombatService.Attack(state.PlayerCombatant, state.Enemies[0], new FixedRandomSource([5, 100]));

        Assert.That(normal.Damage, Is.EqualTo(5));
    }

    [Test]
    public void Attack_DoublesDamageOnCritical()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor();
        var encounter = new Encounter(1, [CreateEnemy("dummy", "Training Dummy", 1, meleeDefense: 4)]);
        var state = services.CombatService.StartEncounter(actor, encounter);

        var critical = services.CombatService.Attack(state.PlayerCombatant, state.Enemies[0], new FixedRandomSource([5, 1]));

        Assert.That(critical.Damage, Is.EqualTo(10));
    }

    [Test]
    public void Attack_DealsAtLeastOneDamage()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor();
        var encounter = new Encounter(1, [CreateEnemy("dummy", "Training Dummy", 1, meleeDefense: 100)]);
        var state = services.CombatService.StartEncounter(actor, encounter);

        var result = services.CombatService.Attack(state.PlayerCombatant, state.Enemies[0], new FixedRandomSource([3, 100]));

        Assert.That(result.Damage, Is.EqualTo(1));
    }

    [Test]
    public void Defending_ReducesNextIncomingDamage()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor();
        var encounter = new Encounter(1, [CreateEnemy("dummy", "Training Dummy", 1, meleeDefense: 0)]);
        var state = services.CombatService.StartEncounter(actor, encounter);
        state.Enemies[0].Defend();

        var result = services.CombatService.Attack(state.PlayerCombatant, state.Enemies[0], new FixedRandomSource([5, 100]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Damage, Is.EqualTo(4));
            Assert.That(state.Enemies[0].IsDefending, Is.False);
        });
    }

    [Test]
    public void UseConsumable_HealsAndConsumesItem()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor();
        var encounter = new Encounter(1, [CreateEnemy("dummy", "Training Dummy", 1)]);
        var state = services.CombatService.StartEncounter(actor, encounter);
        state.PlayerCombatant.TakeDamage(20);

        var result = services.CombatService.UseConsumable(state, "small-healing-potion");

        Assert.Multiple(() =>
        {
            Assert.That(result.AmountHealed, Is.EqualTo(20));
            Assert.That(actor.Inventory.GetQuantity("small-healing-potion"), Is.EqualTo(2));
        });
    }

    [Test]
    public void ChangeEquipment_DoesNotConsumeTurn()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor();
        var encounter = new Encounter(1, [CreateEnemy("dummy", "Training Dummy", 1)]);
        var state = services.CombatService.StartEncounter(actor, encounter);
        var tunic = (IEquipmentItem)services.ItemDefinitions.Get("cloth-tunic");

        var result = services.CombatService.ChangeEquipment(state, tunic);

        Assert.Multiple(() =>
        {
            Assert.That(result.ConsumesTurn, Is.False);
            Assert.That(actor.Equipment[EquipmentSlot.Body], Is.EqualTo(tunic));
        });
    }

    [Test]
    public void CompleteVictory_GrantsLootAndWeaponSkillExperience()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor();
        var enemy = CreateEnemy(
            "loot-dummy",
            "Loot Dummy",
            1,
            experience: 30,
            lootTable: [new LootDropDefinition(LootDropKind.Currency, "currency", 10, 10, 1)]);
        var state = services.CombatService.StartEncounter(actor, new Encounter(1, [enemy]));
        state.Enemies[0].TakeDamage(999);

        var outcome = services.CombatService.CompleteVictory(state, new FixedRandomSource([1], [10]));

        Assert.Multiple(() =>
        {
            Assert.That(outcome.OutcomeType, Is.EqualTo(BattleOutcomeType.Victory));
            Assert.That(actor.Inventory.Currency, Is.EqualTo(10));
            Assert.That(actor.Skills[SkillType.Melee].Leveling.Experience, Is.EqualTo(30));
            Assert.That(outcome.ExperienceSkill, Is.EqualTo(SkillType.Melee));
        });
    }

    [Test]
    public void CompleteDefeat_ReturnsDefeatWithoutRewards()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor();
        var state = services.CombatService.StartEncounter(actor, new Encounter(1, [CreateEnemy("dummy", "Training Dummy", 1)]));
        state.PlayerCombatant.TakeDamage(999);

        var outcome = services.CombatService.CompleteDefeat(state);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.OutcomeType, Is.EqualTo(BattleOutcomeType.Defeat));
            Assert.That(outcome.Reward.Currency, Is.EqualTo(0));
            Assert.That(outcome.ExperienceAwarded, Is.EqualTo(0));
        });
    }

    private static Actor CreateActor()
    {
        var services = ApplicationServices.CreateDefault();

        return services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Human, CharacterClass.Warrior));
    }

    private static EnemyDefinition CreateEnemy(
        string id,
        string name,
        int speed,
        int meleeDefense = 1,
        int experience = 10,
        IReadOnlyCollection<LootDropDefinition>? lootTable = null)
    {
        return new EnemyDefinition(
            id,
            name,
            new Dictionary<StatType, int>
            {
                [StatType.Health] = 50,
                [StatType.MeleeAttack] = 1,
                [StatType.MeleeDefense] = meleeDefense,
                [StatType.RangedDefense] = 1,
                [StatType.MagicDefense] = 1,
                [StatType.CriticalChance] = 0,
                [StatType.Speed] = speed
            },
            new WeaponDefinition($"{id}-weapon", $"{name} Weapon", WeaponType.Melee, 1, 2, []),
            experience,
            [new WeightedEnemyAction(EnemyActionType.BasicAttack, 1)],
            lootTable ?? []);
    }
}
