namespace MyTurn.Console.Input;

public sealed class CompositeInputReader : IDisposable
{
    private readonly IReadOnlyList<IInputSource> _sources;

    public CompositeInputReader(IEnumerable<IInputSource> sources)
    {
        _sources = sources.ToArray();
    }

    public InputSnapshot Poll()
    {
        return InputSnapshot.Merge(_sources.Select(source => source.Poll()));
    }

    public void Dispose()
    {
        foreach (var source in _sources)
        {
            source.Dispose();
        }
    }
}
