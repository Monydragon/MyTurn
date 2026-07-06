namespace MyTurn.Application;

public interface IGameFlowService
{
    GameFlowState GetNextState(MainMenuAction action);
    GameFlowState GetNextState(CharacterHubAction action);
    GameFlowState GetStateAfterCharacterCreation();
}
