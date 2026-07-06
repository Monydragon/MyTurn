namespace MyTurn.Console.Input;

public sealed class AutosaveThrottle
{
    private readonly TimeSpan _interval;
    private bool _hasPendingChanges;
    private DateTimeOffset _lastSaveAt;

    public AutosaveThrottle(TimeSpan? interval = null)
    {
        _interval = interval ?? TimeSpan.FromMilliseconds(750);
        _lastSaveAt = DateTimeOffset.MinValue;
    }

    public void MarkChanged()
    {
        _hasPendingChanges = true;
    }

    public bool TrySave(DateTimeOffset now, Action save)
    {
        ArgumentNullException.ThrowIfNull(save);

        if (!_hasPendingChanges || now - _lastSaveAt < _interval)
        {
            return false;
        }

        ForceSave(now, save);
        return true;
    }

    public void ForceSave(DateTimeOffset now, Action save)
    {
        ArgumentNullException.ThrowIfNull(save);

        save();
        _hasPendingChanges = false;
        _lastSaveAt = now;
    }
}
