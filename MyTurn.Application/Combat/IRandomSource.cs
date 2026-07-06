namespace MyTurn.Application;

public interface IRandomSource
{
    int NextInclusive(int minValue, int maxValue);
    long NextInt64Inclusive(long minValue, long maxValue);
}
