using MyTurn.Application;

namespace MyTurn.Tests.Combat;

internal sealed class FixedRandomSource : IRandomSource
{
    private readonly Queue<int> _ints;
    private readonly Queue<long> _longs;

    public FixedRandomSource(IEnumerable<int>? ints = null, IEnumerable<long>? longs = null)
    {
        _ints = new Queue<int>(ints ?? []);
        _longs = new Queue<long>(longs ?? []);
    }

    public int NextInclusive(int minValue, int maxValue)
    {
        return _ints.Count > 0 ? _ints.Dequeue() : minValue;
    }

    public long NextInt64Inclusive(long minValue, long maxValue)
    {
        return _longs.Count > 0 ? _longs.Dequeue() : minValue;
    }
}
