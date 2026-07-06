using SDL2;

namespace MyTurn.Console.Input;

public sealed class SdlGameControllerInputSource : IInputSource
{
    private readonly ControllerCommandMapper _mapper = new();
    private readonly HashSet<GameCommand> _previousHeldCommands = [];
    private readonly bool _isInitialized;
    private IntPtr _controller;
    private IntPtr _joystick;
    private string? _controllerName;

    public SdlGameControllerInputSource(string? mappingFilePath = null)
    {
        try
        {
            SDL.SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
            SDL.SDL_SetHint("SDL_XINPUT_ENABLED", "1");

            _isInitialized = SDL.SDL_InitSubSystem(SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_JOYSTICK | SDL.SDL_INIT_EVENTS) == 0;

            if (!_isInitialized)
            {
                return;
            }

            SDL.SDL_GameControllerEventState(1);

            if (!string.IsNullOrWhiteSpace(mappingFilePath) && File.Exists(mappingFilePath))
            {
                SDL.SDL_GameControllerAddMappingsFromFile(mappingFilePath);
            }

            OpenFirstController();
        }
        catch
        {
            _isInitialized = false;
        }
    }

    public InputSnapshot Poll()
    {
        if (!_isInitialized)
        {
            return InputSnapshot.Empty;
        }

        try
        {
            PumpEvents();
            EnsureAttachedController();

            if (_controller == IntPtr.Zero && _joystick == IntPtr.Zero)
            {
                return InputSnapshot.Empty;
            }

            SDL.SDL_GameControllerUpdate();
            SDL.SDL_JoystickUpdate();

            var held = _mapper.Map(_controller != IntPtr.Zero
                ? ReadControllerState()
                : ReadJoystickState());
            var pressed = held.Except(_previousHeldCommands).ToArray();

            _previousHeldCommands.Clear();
            _previousHeldCommands.UnionWith(held);

            return new InputSnapshot(pressed, held, _controllerName);
        }
        catch
        {
            CloseController();
            return InputSnapshot.Empty;
        }
    }

    public void Dispose()
    {
        CloseController();

        if (_isInitialized)
        {
            SDL.SDL_QuitSubSystem(SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_JOYSTICK | SDL.SDL_INIT_EVENTS);
        }
    }

    private void PumpEvents()
    {
        while (SDL.SDL_PollEvent(out var sdlEvent) == 1)
        {
            var eventType = (SDL.SDL_EventType)sdlEvent.type;

            if (eventType == SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED && _controller == IntPtr.Zero)
            {
                OpenFirstController();
            }
            else if (eventType == SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED)
            {
                EnsureAttachedController();
            }
        }

        SDL.SDL_PumpEvents();
    }

    private void EnsureAttachedController()
    {
        if (_controller != IntPtr.Zero && SDL.SDL_GameControllerGetAttached(_controller) == SDL.SDL_bool.SDL_FALSE)
        {
            CloseController();
        }

        if (_joystick != IntPtr.Zero && SDL.SDL_JoystickGetAttached(_joystick) == SDL.SDL_bool.SDL_FALSE)
        {
            CloseController();
        }

        if (_controller == IntPtr.Zero && _joystick == IntPtr.Zero)
        {
            OpenFirstController();
        }
    }

    private void OpenFirstController()
    {
        CloseController();

        var joystickCount = SDL.SDL_NumJoysticks();

        for (var index = 0; index < joystickCount; index++)
        {
            if (SDL.SDL_IsGameController(index) != SDL.SDL_bool.SDL_TRUE)
            {
                continue;
            }

            _controller = SDL.SDL_GameControllerOpen(index);

            if (_controller != IntPtr.Zero)
            {
                _controllerName = SDL.SDL_GameControllerName(_controller)
                    ?? SDL.SDL_GameControllerNameForIndex(index)
                    ?? "Controller";
                _previousHeldCommands.Clear();
                return;
            }
        }

        for (var index = 0; index < joystickCount; index++)
        {
            _joystick = SDL.SDL_JoystickOpen(index);

            if (_joystick != IntPtr.Zero)
            {
                _controllerName = SDL.SDL_JoystickName(_joystick)
                    ?? SDL.SDL_JoystickNameForIndex(index)
                    ?? "Controller";
                _previousHeldCommands.Clear();
                return;
            }
        }
    }

    private ControllerInputState ReadControllerState()
    {
        var controllerState = new ControllerInputState(
            A: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A),
            B: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B),
            X: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X),
            Y: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y),
            Back: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK),
            Start: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START),
            LeftShoulder: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER),
            RightShoulder: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER),
            DpadUp: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP),
            DpadDown: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN),
            DpadLeft: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT),
            DpadRight: IsButtonPressed(SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT),
            LeftX: SDL.SDL_GameControllerGetAxis(_controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX),
            LeftY: SDL.SDL_GameControllerGetAxis(_controller, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY));

        var controllerJoystick = SDL.SDL_GameControllerGetJoystick(_controller);

        return controllerJoystick == IntPtr.Zero
            ? controllerState
            : MergeStates(controllerState, ReadJoystickState(controllerJoystick));
    }

    private bool IsButtonPressed(SDL.SDL_GameControllerButton button)
    {
        return SDL.SDL_GameControllerGetButton(_controller, button) != 0;
    }

    private ControllerInputState ReadJoystickState()
    {
        return ReadJoystickState(_joystick);
    }

    private static ControllerInputState MergeStates(ControllerInputState primary, ControllerInputState fallback)
    {
        return new ControllerInputState(
            A: primary.A || fallback.A,
            B: primary.B || fallback.B,
            X: primary.X || fallback.X,
            Y: primary.Y || fallback.Y,
            Back: primary.Back || fallback.Back,
            Start: primary.Start || fallback.Start,
            LeftShoulder: primary.LeftShoulder || fallback.LeftShoulder,
            RightShoulder: primary.RightShoulder || fallback.RightShoulder,
            DpadUp: primary.DpadUp || fallback.DpadUp,
            DpadDown: primary.DpadDown || fallback.DpadDown,
            DpadLeft: primary.DpadLeft || fallback.DpadLeft,
            DpadRight: primary.DpadRight || fallback.DpadRight,
            LeftX: Math.Abs(primary.LeftX) >= Math.Abs(fallback.LeftX) ? primary.LeftX : fallback.LeftX,
            LeftY: Math.Abs(primary.LeftY) >= Math.Abs(fallback.LeftY) ? primary.LeftY : fallback.LeftY);
    }

    private static ControllerInputState ReadJoystickState(IntPtr joystick)
    {
        var buttonCount = SDL.SDL_JoystickNumButtons(joystick);
        var axisCount = SDL.SDL_JoystickNumAxes(joystick);
        var hatCount = SDL.SDL_JoystickNumHats(joystick);
        var hat = hatCount > 0 ? SDL.SDL_JoystickGetHat(joystick, 0) : 0;

        return new ControllerInputState(
            A: IsJoystickButtonPressed(joystick, 0, buttonCount),
            B: IsJoystickButtonPressed(joystick, 1, buttonCount),
            X: IsJoystickButtonPressed(joystick, 2, buttonCount),
            Y: IsJoystickButtonPressed(joystick, 3, buttonCount),
            Back: IsJoystickButtonPressed(joystick, 6, buttonCount) || IsJoystickButtonPressed(joystick, 8, buttonCount),
            LeftShoulder: IsJoystickButtonPressed(joystick, 4, buttonCount),
            RightShoulder: IsJoystickButtonPressed(joystick, 5, buttonCount),
            Start: IsJoystickButtonPressed(joystick, 7, buttonCount) || IsJoystickButtonPressed(joystick, 9, buttonCount),
            DpadUp: (hat & SDL.SDL_HAT_UP) != 0 || IsJoystickButtonPressed(joystick, 11, buttonCount),
            DpadDown: (hat & SDL.SDL_HAT_DOWN) != 0 || IsJoystickButtonPressed(joystick, 12, buttonCount),
            DpadLeft: (hat & SDL.SDL_HAT_LEFT) != 0 || IsJoystickButtonPressed(joystick, 13, buttonCount),
            DpadRight: (hat & SDL.SDL_HAT_RIGHT) != 0 || IsJoystickButtonPressed(joystick, 14, buttonCount),
            LeftX: axisCount > 0 ? SDL.SDL_JoystickGetAxis(joystick, 0) : (short)0,
            LeftY: axisCount > 1 ? SDL.SDL_JoystickGetAxis(joystick, 1) : (short)0);
    }

    private static bool IsJoystickButtonPressed(IntPtr joystick, int buttonIndex, int buttonCount)
    {
        return buttonIndex < buttonCount && SDL.SDL_JoystickGetButton(joystick, buttonIndex) != 0;
    }

    private void CloseController()
    {
        if (_controller != IntPtr.Zero)
        {
            SDL.SDL_GameControllerClose(_controller);
            _controller = IntPtr.Zero;
        }

        if (_joystick != IntPtr.Zero)
        {
            SDL.SDL_JoystickClose(_joystick);
            _joystick = IntPtr.Zero;
        }

        _controllerName = null;
        _previousHeldCommands.Clear();
    }
}
