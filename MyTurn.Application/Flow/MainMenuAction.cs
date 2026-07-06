using MyTurn.Domain;

namespace MyTurn.Application;

public enum MainMenuAction
{
    [DisplayName("New Game")]
    NewGame,

    [DisplayName("Load Game")]
    LoadGame,

    Exit
}
