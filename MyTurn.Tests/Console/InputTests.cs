using MyTurn.Presentation.Input;

namespace MyTurn.Tests.Console;

[TestFixture]
public sealed class InputTests
{
    [Test]
    public void ControllerCommandMapper_MapsFaceButtonsShouldersAndDpad()
    {
        var mapper = new ControllerCommandMapper();

        var commands = mapper.Map(new ControllerInputState(
            A: true,
            Back: true,
            X: true,
            Y: true,
            Start: true,
            LeftShoulder: true,
            RightShoulder: true,
            DpadUp: true,
            DpadRight: true));

        Assert.Multiple(() =>
        {
            Assert.That(commands, Does.Contain(GameCommand.Confirm));
            Assert.That(commands, Does.Contain(GameCommand.Cancel));
            Assert.That(commands, Does.Contain(GameCommand.Secondary));
            Assert.That(commands, Does.Contain(GameCommand.Primary));
            Assert.That(commands, Does.Contain(GameCommand.Menu));
            Assert.That(commands, Does.Contain(GameCommand.Previous));
            Assert.That(commands, Does.Contain(GameCommand.Next));
            Assert.That(commands, Does.Contain(GameCommand.MoveUp));
            Assert.That(commands, Does.Contain(GameCommand.MoveRight));
        });
    }

    [Test]
    public void ControllerCommandMapper_UsesDeadzoneForStickMovement()
    {
        var mapper = new ControllerCommandMapper();

        var insideDeadzone = mapper.Map(new ControllerInputState(LeftX: 8_000, LeftY: -8_000));
        var outsideDeadzone = mapper.Map(new ControllerInputState(LeftX: 20_000, LeftY: -20_000));

        Assert.Multiple(() =>
        {
            Assert.That(insideDeadzone, Does.Not.Contain(GameCommand.MoveRight));
            Assert.That(insideDeadzone, Does.Not.Contain(GameCommand.MoveUp));
            Assert.That(outsideDeadzone, Does.Contain(GameCommand.MoveRight));
            Assert.That(outsideDeadzone, Does.Contain(GameCommand.MoveUp));
        });
    }

    [Test]
    public void MovementRepeatController_MovesImmediatelyThenAfterInitialAndRepeatDelays()
    {
        var repeat = new MovementRepeatController();
        var start = DateTimeOffset.UtcNow;
        var held = new InputSnapshot(
            [GameCommand.MoveRight],
            [GameCommand.MoveRight]);
        var stillHeld = new InputSnapshot(
            [],
            [GameCommand.MoveRight]);

        var first = repeat.TryConsume(held, start, out var firstCommand);
        var tooSoon = repeat.TryConsume(stillHeld, start + TimeSpan.FromMilliseconds(100), out _);
        var afterInitial = repeat.TryConsume(stillHeld, start + TimeSpan.FromMilliseconds(140), out var secondCommand);
        var afterRepeat = repeat.TryConsume(stillHeld, start + TimeSpan.FromMilliseconds(230), out var thirdCommand);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(firstCommand, Is.EqualTo(GameCommand.MoveRight));
            Assert.That(tooSoon, Is.False);
            Assert.That(afterInitial, Is.True);
            Assert.That(secondCommand, Is.EqualTo(GameCommand.MoveRight));
            Assert.That(afterRepeat, Is.True);
            Assert.That(thirdCommand, Is.EqualTo(GameCommand.MoveRight));
        });
    }

    [Test]
    public void MenuController_NavigatesWrapsConfirmsAndCancels()
    {
        var menu = new MenuController(3);
        var now = DateTimeOffset.UtcNow;

        var up = menu.Handle(new InputSnapshot([GameCommand.MoveUp], [GameCommand.MoveUp]), now);
        var confirm = menu.Handle(new InputSnapshot([GameCommand.Confirm], [GameCommand.Confirm]), now);
        var cancel = menu.Handle(new InputSnapshot([GameCommand.Cancel], [GameCommand.Cancel]), now);

        Assert.Multiple(() =>
        {
            Assert.That(up.Kind, Is.EqualTo(MenuInteractionKind.Moved));
            Assert.That(up.SelectedIndex, Is.EqualTo(2));
            Assert.That(confirm.Kind, Is.EqualTo(MenuInteractionKind.Confirmed));
            Assert.That(confirm.SelectedIndex, Is.EqualTo(2));
            Assert.That(cancel.Kind, Is.EqualTo(MenuInteractionKind.Cancelled));
        });
    }

    [Test]
    public void InputSnapshot_MergeCombinesKeyboardAndControllerCommands()
    {
        var keyboard = new InputSnapshot([GameCommand.Confirm], [GameCommand.Confirm]);
        var controller = new InputSnapshot([GameCommand.MoveUp], [GameCommand.MoveUp], "Wireless Controller");

        var merged = InputSnapshot.Merge([keyboard, controller]);

        Assert.Multiple(() =>
        {
            Assert.That(merged.PressedCommands, Does.Contain(GameCommand.Confirm));
            Assert.That(merged.PressedCommands, Does.Contain(GameCommand.MoveUp));
            Assert.That(merged.ControllerName, Is.EqualTo("Wireless Controller"));
        });
    }

    [Test]
    public void AutosaveThrottle_DebouncesNormalSavesAndAllowsForcedSaves()
    {
        var saveCount = 0;
        var throttle = new AutosaveThrottle(TimeSpan.FromMilliseconds(750));
        var start = DateTimeOffset.UtcNow;

        throttle.MarkChanged();
        var firstTry = throttle.TrySave(start + TimeSpan.FromMilliseconds(200), () => saveCount++);
        var secondTry = throttle.TrySave(start + TimeSpan.FromMilliseconds(800), () => saveCount++);
        throttle.MarkChanged();
        throttle.ForceSave(start + TimeSpan.FromMilliseconds(900), () => saveCount++);

        Assert.Multiple(() =>
        {
            Assert.That(firstTry, Is.True);
            Assert.That(secondTry, Is.False);
            Assert.That(saveCount, Is.EqualTo(2));
        });
    }
}
