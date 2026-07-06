namespace MyTurn.Console.Input;

public sealed class MenuController
{
    private readonly MovementRepeatController _repeatController;

    public MenuController(int itemCount, int selectedIndex = 0)
    {
        if (itemCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), "Menu requires at least one item.");
        }

        ItemCount = itemCount;
        SelectedIndex = Math.Clamp(selectedIndex, 0, itemCount - 1);
        _repeatController = new MovementRepeatController();
    }

    public int ItemCount { get; }
    public int SelectedIndex { get; private set; }

    public MenuInteraction Handle(InputSnapshot snapshot, DateTimeOffset now)
    {
        if (snapshot.IsPressed(GameCommand.Confirm))
        {
            return MenuInteraction.Confirmed(SelectedIndex);
        }

        if (snapshot.IsPressed(GameCommand.Cancel))
        {
            return MenuInteraction.Cancelled();
        }

        if (snapshot.IsPressed(GameCommand.Next))
        {
            Move(1);
            return MenuInteraction.Moved(SelectedIndex);
        }

        if (snapshot.IsPressed(GameCommand.Previous))
        {
            Move(-1);
            return MenuInteraction.Moved(SelectedIndex);
        }

        if (_repeatController.TryConsume(snapshot, now, out var command))
        {
            switch (command)
            {
                case GameCommand.MoveDown:
                case GameCommand.MoveRight:
                    Move(1);
                    return MenuInteraction.Moved(SelectedIndex);
                case GameCommand.MoveUp:
                case GameCommand.MoveLeft:
                    Move(-1);
                    return MenuInteraction.Moved(SelectedIndex);
            }
        }

        return MenuInteraction.None(SelectedIndex);
    }

    private void Move(int offset)
    {
        SelectedIndex = (SelectedIndex + offset + ItemCount) % ItemCount;
    }
}

public readonly record struct MenuInteraction(MenuInteractionKind Kind, int SelectedIndex)
{
    public static MenuInteraction None(int selectedIndex) => new(MenuInteractionKind.None, selectedIndex);
    public static MenuInteraction Moved(int selectedIndex) => new(MenuInteractionKind.Moved, selectedIndex);
    public static MenuInteraction Confirmed(int selectedIndex) => new(MenuInteractionKind.Confirmed, selectedIndex);
    public static MenuInteraction Cancelled() => new(MenuInteractionKind.Cancelled, -1);
}

public enum MenuInteractionKind
{
    None,
    Moved,
    Confirmed,
    Cancelled
}
