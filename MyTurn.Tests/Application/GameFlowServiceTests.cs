using MyTurn.Application;

namespace MyTurn.Tests.Application;

[TestFixture]
public sealed class GameFlowServiceTests
{
    [Test]
    public void MainMenu_NewGame_StartsCharacterCreation()
    {
        var service = new GameFlowService();

        var nextState = service.GetNextState(MainMenuAction.NewGame);

        Assert.That(nextState, Is.EqualTo(GameFlowState.CharacterCreation));
    }

    [Test]
    public void MainMenu_Exit_ExitsGame()
    {
        var service = new GameFlowService();

        var nextState = service.GetNextState(MainMenuAction.Exit);

        Assert.That(nextState, Is.EqualTo(GameFlowState.Exit));
    }

    [Test]
    public void CharacterCreation_LeadsToCharacterHub()
    {
        var service = new GameFlowService();

        var nextState = service.GetStateAfterCharacterCreation();

        Assert.That(nextState, Is.EqualTo(GameFlowState.CharacterHub));
    }

    [TestCase(CharacterHubAction.ViewCharacter)]
    [TestCase(CharacterHubAction.FightEncounter)]
    [TestCase(CharacterHubAction.ExploreWorld)]
    [TestCase(CharacterHubAction.ViewInventory)]
    [TestCase(CharacterHubAction.ViewStats)]
    [TestCase(CharacterHubAction.ViewSkills)]
    [TestCase(CharacterHubAction.ViewEquipment)]
    [TestCase(CharacterHubAction.UseItem)]
    [TestCase(CharacterHubAction.EquipGear)]
    public void CharacterHub_ContentActions_StayInCharacterHub(CharacterHubAction action)
    {
        var service = new GameFlowService();

        var nextState = service.GetNextState(action);

        Assert.That(nextState, Is.EqualTo(GameFlowState.CharacterHub));
    }

    [Test]
    public void CharacterHub_BackToMainMenu_ReturnsToMainMenu()
    {
        var service = new GameFlowService();

        var nextState = service.GetNextState(CharacterHubAction.BackToMainMenu);

        Assert.That(nextState, Is.EqualTo(GameFlowState.MainMenu));
    }
}
