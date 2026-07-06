namespace MyTurn.Console.Input;

public interface IInputSource : IDisposable
{
    InputSnapshot Poll();
}
