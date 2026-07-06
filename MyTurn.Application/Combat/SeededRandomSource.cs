namespace MyTurn.Application;

public sealed class SeededRandomSource : IRandomSource
{
    private readonly Random _random;

    public SeededRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    public int NextInclusive(int minValue, int maxValue)
    {
        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(minValue), "Minimum value cannot exceed maximum value.");
        }

        return _random.Next(minValue, maxValue + 1);
    }

    public long NextInt64Inclusive(long minValue, long maxValue)
    {
        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(minValue), "Minimum value cannot exceed maximum value.");
        }

        return _random.NextInt64(minValue, maxValue + 1);
    }
}
