using Windows.Gaming.Input;
using LagmaIpp.Services;

namespace LagmaIpp.WinUI;

public static class GamepadPoller
{
    private static System.Timers.Timer? _timer;

    public static void Start()
    {
        _timer = new System.Timers.Timer(50); // 20Hz
        _timer.Elapsed += OnTick;
        _timer.Start();
    }

    private static void OnTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var gamepads = Gamepad.Gamepads;
        if (gamepads.Count == 0)
        {
            LagmaIpp.App.Gamepad.OnDisconnected();
            return;
        }

        var gp = gamepads[0];
        var r = gp.GetCurrentReading();

        LagmaIpp.App.Gamepad.UpdateState(s =>
        {
            s.LeftX = (float)r.LeftThumbstickX;
            s.LeftY = (float)r.LeftThumbstickY;
            s.RightX = (float)r.RightThumbstickX;
            s.RightY = (float)r.RightThumbstickY;
            s.LT = (float)r.LeftTrigger;
            s.RT = (float)r.RightTrigger;

            s.South = r.Buttons.HasFlag(GamepadButtons.A);
            s.East = r.Buttons.HasFlag(GamepadButtons.B);
            s.West = r.Buttons.HasFlag(GamepadButtons.X);
            s.North = r.Buttons.HasFlag(GamepadButtons.Y);
            s.LeftBumper = r.Buttons.HasFlag(GamepadButtons.LeftShoulder);
            s.RightBumper = r.Buttons.HasFlag(GamepadButtons.RightShoulder);
            s.DpadUp = r.Buttons.HasFlag(GamepadButtons.DPadUp);
            s.DpadDown = r.Buttons.HasFlag(GamepadButtons.DPadDown);
            s.DpadLeft = r.Buttons.HasFlag(GamepadButtons.DPadLeft);
            s.DpadRight = r.Buttons.HasFlag(GamepadButtons.DPadRight);
            s.Start = r.Buttons.HasFlag(GamepadButtons.Menu);
            s.Select = r.Buttons.HasFlag(GamepadButtons.View);
        });
    }
}