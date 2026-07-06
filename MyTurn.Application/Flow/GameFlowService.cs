namespace MyTurn.Application;

public sealed class GameFlowService : IGameFlowService
{
    public GameFlowState GetNextState(MainMenuAction action)
    {
        return action switch
        {
            MainMenuAction.QuickStart => GameFlowState.CharacterHub,
            MainMenuAction.NewGame => GameFlowState.CharacterCreation,
            MainMenuAction.LoadGame => GameFlowState.LoadGame,
            MainMenuAction.Exit => GameFlowState.Exit,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported main menu action.")
        };
    }

    public GameFlowState GetNextState(CharacterHubAction action)
    {
        return action switch
        {
            CharacterHubAction.BackToMainMenu => GameFlowState.MainMenu,
            CharacterHubAction.Exit => GameFlowState.Exit,
            CharacterHubAction.ExploreWorld => GameFlowState.CharacterHub,
            CharacterHubAction.FightEncounter => GameFlowState.CharacterHub,
            CharacterHubAction.ViewParty => GameFlowState.CharacterHub,
            CharacterHubAction.ManageParty => GameFlowState.CharacterHub,
            CharacterHubAction.ViewCharacter => GameFlowState.CharacterHub,
            CharacterHubAction.ViewInventory => GameFlowState.CharacterHub,
            CharacterHubAction.ViewStats => GameFlowState.CharacterHub,
            CharacterHubAction.ViewSkills => GameFlowState.CharacterHub,
            CharacterHubAction.ViewEquipment => GameFlowState.CharacterHub,
            CharacterHubAction.UseItem => GameFlowState.CharacterHub,
            CharacterHubAction.EquipGear => GameFlowState.CharacterHub,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported character hub action.")
        };
    }

    public GameFlowState GetStateAfterCharacterCreation()
    {
        return GameFlowState.CharacterHub;
    }
}
