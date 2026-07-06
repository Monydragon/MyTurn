namespace MyTurn.Domain;

public sealed class LevelContainer : IEntity
{
    private int _currentLevel = 1;
    private int _maxLevel = 100;

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; }

    public int CurrentLevel
    {
        get => _currentLevel;
        private set => _currentLevel = Math.Clamp(value, 1, MaxLevel);
    }

    public int MaxLevel
    {
        get => _maxLevel;
        private set
        {
            _maxLevel = Math.Max(1, value);
            _currentLevel = Math.Clamp(_currentLevel, 1, _maxLevel);
        }
    }

    public int Experience { get; private set; }
    public int ExperienceToNextLevel => (int)(100 * Math.Pow(1.5, CurrentLevel - 1));

    public LevelContainer(string name, int currentLevel, int experience, int maxLevel = 100)
    {
        Name = name;
        MaxLevel = maxLevel;
        CurrentLevel = currentLevel;
        Experience = Math.Max(0, experience);
    }

    public void AddExperience(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Experience amount cannot be negative.");
        }

        Experience += amount;

        while (Experience >= ExperienceToNextLevel && CurrentLevel < MaxLevel)
        {
            Experience -= ExperienceToNextLevel;
            CurrentLevel++;
        }
    }

    public void LoseExperience(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Experience amount cannot be negative.");
        }

        Experience -= amount;

        while (Experience < 0 && CurrentLevel > 1)
        {
            CurrentLevel--;
            Experience += ExperienceToNextLevel;
        }

        if (Experience < 0)
        {
            Experience = 0;
        }
    }

    public void SetLevel(int level)
    {
        CurrentLevel = level;
        Experience = 0;
    }

    public void SetExperience(int experience)
    {
        if (experience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(experience), "Experience cannot be negative.");
        }

        Experience = experience;

        while (Experience >= ExperienceToNextLevel && CurrentLevel < MaxLevel)
        {
            Experience -= ExperienceToNextLevel;
            CurrentLevel++;
        }
    }

    public void LevelUp()
    {
        CurrentLevel++;
        Experience = 0;
    }

    public void LevelDown()
    {
        if (CurrentLevel <= 1)
        {
            return;
        }

        CurrentLevel--;
        Experience = 0;
    }

    public override string ToString()
    {
        return $"{Name} - Level {CurrentLevel}";
    }
}
