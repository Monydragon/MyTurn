using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class EquipmentService : IEquipmentService
{
    private const string EquipmentModifierPrefix = "equipment";
    private readonly IWeaponDefinitionRegistry _weaponDefinitions;

    public EquipmentService(IWeaponDefinitionRegistry weaponDefinitions)
    {
        _weaponDefinitions = weaponDefinitions;
    }

    public IWeapon ChangeEquippedWeapon(Actor actor, WeaponType weaponType)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var weapon = _weaponDefinitions.Get(weaponType);
        Equip(actor, weapon);

        return weapon;
    }

    public IEquipmentItem Equip(Actor actor, IEquipmentItem item)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(item);

        var sourceId = GetEquipmentSourceId(item.Slot);
        actor.Stats.RemoveModifiersBySource(sourceId);
        actor.Equipment.Equip(item);
        actor.Stats.ApplyModifiers(item.StatModifiers.Select(modifier =>
            new StatModifier(modifier.StatType, modifier.Value, sourceId)));

        return item;
    }

    public IEquipmentItem? Unequip(Actor actor, EquipmentSlot slot)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (slot == EquipmentSlot.Weapon)
        {
            throw new InvalidOperationException("A weapon must remain equipped.");
        }

        var sourceId = GetEquipmentSourceId(slot);
        actor.Stats.RemoveModifiersBySource(sourceId);

        return actor.Equipment.Unequip(slot);
    }

    public static string GetEquipmentSourceId(EquipmentSlot slot)
    {
        return $"{EquipmentModifierPrefix}:{slot}";
    }
}
