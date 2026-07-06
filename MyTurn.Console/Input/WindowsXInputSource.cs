using System.Runtime.InteropServices;

namespace MyTurn.Console.Input;

public sealed class WindowsXInputSource : IInputSource
{
    private const int MaxControllerCount = 4;
    private const uint ErrorSuccess = 0;

    private readonly ControllerCommandMapper _mapper = new();
    private readonly HashSet<GameCommand> _previousHeldCommands = [];
    private int? _controllerIndex;
    private bool _isUnavailable;

    public InputSnapshot Poll()
    {
        if (_isUnavailable || !OperatingSystem.IsWindows())
        {
            return InputSnapshot.Empty;
        }

        if (!TryReadConnectedState(out var state))
        {
            _controllerIndex = null;
            _previousHeldCommands.Clear();
            return InputSnapshot.Empty;
        }

        var held = _mapper.Map(ToControllerState(state.Gamepad));
        var pressed = held.Except(_previousHeldCommands).ToArray();

        _previousHeldCommands.Clear();
        _previousHeldCommands.UnionWith(held);

        return new InputSnapshot(pressed, held, $"XInput Controller {_controllerIndex!.Value + 1}");
    }

    public void Dispose()
    {
    }

    private bool TryReadConnectedState(out XInputState state)
    {
        if (_controllerIndex is not null && TryGetState(_controllerIndex.Value, out state))
        {
            return true;
        }

        for (var index = 0; index < MaxControllerCount; index++)
        {
            if (TryGetState(index, out state))
            {
                _controllerIndex = index;
                return true;
            }
        }

        state = default;
        return false;
    }

    private bool TryGetState(int controllerIndex, out XInputState state)
    {
        try
        {
            if (XInput14.GetState(controllerIndex, out state) == ErrorSuccess)
            {
                return true;
            }

            return false;
        }
        catch (DllNotFoundException)
        {
            return TryGetStateFromLegacyDll(controllerIndex, out state);
        }
        catch (EntryPointNotFoundException)
        {
            return TryGetStateFromLegacyDll(controllerIndex, out state);
        }
        catch (BadImageFormatException)
        {
            _isUnavailable = true;
            state = default;
            return false;
        }
    }

    private bool TryGetStateFromLegacyDll(int controllerIndex, out XInputState state)
    {
        try
        {
            return XInput910.GetState(controllerIndex, out state) == ErrorSuccess;
        }
        catch (DllNotFoundException)
        {
            _isUnavailable = true;
            state = default;
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            _isUnavailable = true;
            state = default;
            return false;
        }
        catch (BadImageFormatException)
        {
            _isUnavailable = true;
            state = default;
            return false;
        }
    }

    private static ControllerInputState ToControllerState(XInputGamepad gamepad)
    {
        return new ControllerInputState(
            A: HasButton(gamepad.Buttons, XInputButton.A),
            B: HasButton(gamepad.Buttons, XInputButton.B),
            X: HasButton(gamepad.Buttons, XInputButton.X),
            Y: HasButton(gamepad.Buttons, XInputButton.Y),
            Back: HasButton(gamepad.Buttons, XInputButton.Back),
            Start: HasButton(gamepad.Buttons, XInputButton.Start),
            LeftShoulder: HasButton(gamepad.Buttons, XInputButton.LeftShoulder),
            RightShoulder: HasButton(gamepad.Buttons, XInputButton.RightShoulder),
            DpadUp: HasButton(gamepad.Buttons, XInputButton.DpadUp),
            DpadDown: HasButton(gamepad.Buttons, XInputButton.DpadDown),
            DpadLeft: HasButton(gamepad.Buttons, XInputButton.DpadLeft),
            DpadRight: HasButton(gamepad.Buttons, XInputButton.DpadRight),
            LeftX: gamepad.LeftThumbX,
            LeftY: InvertAxis(gamepad.LeftThumbY));
    }

    private static bool HasButton(ushort buttons, XInputButton button)
    {
        return (buttons & (ushort)button) != 0;
    }

    private static short InvertAxis(short value)
    {
        return value == short.MinValue ? short.MaxValue : (short)-value;
    }

    private static class XInput14
    {
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        public static extern uint GetState(int dwUserIndex, out XInputState pState);
    }

    private static class XInput910
    {
        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        public static extern uint GetState(int dwUserIndex, out XInputState pState);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short LeftThumbX;
        public short LeftThumbY;
        public short RightThumbX;
        public short RightThumbY;
    }

    [Flags]
    private enum XInputButton : ushort
    {
        DpadUp = 0x0001,
        DpadDown = 0x0002,
        DpadLeft = 0x0004,
        DpadRight = 0x0008,
        Start = 0x0010,
        Back = 0x0020,
        LeftShoulder = 0x0100,
        RightShoulder = 0x0200,
        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000
    }
}
