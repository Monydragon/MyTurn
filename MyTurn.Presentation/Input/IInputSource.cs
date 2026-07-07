namespace MyTurn.Presentation.Input;

public interface IInputSource : IDisposable
{
    InputSnapshot Poll();
}
