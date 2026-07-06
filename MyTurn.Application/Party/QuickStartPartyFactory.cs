using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class QuickStartPartyFactory : IQuickStartPartyFactory
{
    private static readonly string[] GivenNames =
    [
        "Ari", "Bryn", "Cora", "Dane", "Elia", "Finn", "Galen", "Hale",
        "Iris", "Jory", "Kael", "Lena", "Mira", "Nico", "Orin", "Perrin"
    ];

    private static readonly string[] FamilyNames =
    [
        "Ashford", "Blackwell", "Duskryn", "Emberlain", "Frostmere", "Goldvale",
        "Highmere", "Ironhart", "Moonridge", "Oakenshield", "Ravenfall", "Stormwell"
    ];

    private static readonly CharacterClass[] ClassRotation =
    [
        CharacterClass.Warrior,
        CharacterClass.Archer,
        CharacterClass.Mage,
        CharacterClass.Warrior
    ];

    private static readonly Species[] SpeciesRotation =
    [
        Species.Human,
        Species.Elf,
        Species.Dwarf,
        Species.Halfling
    ];

    private readonly IActorFactory _actorFactory;
    private readonly IPartyService _partyService;

    public QuickStartPartyFactory(IActorFactory actorFactory, IPartyService partyService)
    {
        _actorFactory = actorFactory;
        _partyService = partyService;
    }

    public Party CreateParty(int activeMemberCount = Party.MaxActiveMembers, int? seed = null)
    {
        if (activeMemberCount is < Party.MinActiveMembers or > Party.MaxActiveMembers)
        {
            throw new ArgumentOutOfRangeException(nameof(activeMemberCount), activeMemberCount, "Active party size must be between 1 and 4.");
        }

        var actors = Enumerable.Range(0, activeMemberCount)
            .Select(index => _actorFactory.Create(CreateActorRequest(index, seed)))
            .ToArray();

        return _partyService.CreateParty(actors);
    }

    public CreateActorRequest CreateActorRequest(int memberIndex, int? seed = null)
    {
        if (memberIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(memberIndex), "Member index cannot be negative.");
        }

        var random = CreateRandom(seed, memberIndex);
        var name = GenerateName(random);
        var characterClass = ClassRotation[memberIndex % ClassRotation.Length];
        var species = SpeciesRotation[memberIndex % SpeciesRotation.Length];
        var gender = Enum.GetValues<Gender>()[random.Next(Enum.GetValues<Gender>().Length)];

        return new CreateActorRequest(name, 24 + random.Next(0, 16), gender, species, characterClass);
    }

    public string GenerateName(int? seed = null)
    {
        return GenerateName(CreateRandom(seed, 0));
    }

    private static string GenerateName(Random random)
    {
        return $"{GivenNames[random.Next(GivenNames.Length)]} {FamilyNames[random.Next(FamilyNames.Length)]}";
    }

    private static Random CreateRandom(int? seed, int salt)
    {
        return new Random(seed.HasValue ? HashCode.Combine(seed.Value, salt) : HashCode.Combine(Environment.TickCount, salt, Guid.NewGuid()));
    }
}
