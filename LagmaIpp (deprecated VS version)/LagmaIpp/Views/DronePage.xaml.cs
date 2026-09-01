using LagmaIpp.Services;
using LagmaIpp.ViewModels;

namespace LagmaIpp.Views;

public partial class DronePage : ContentPage
{
    private readonly MainViewModel _vm;

    // ── Valori RPYT correnti ─────────────────────────────────────
    private int _roll, _pitch, _yaw, _thrust;

    // ── Joystick sinistro: Pitch (Y) + Roll (X) ─────────────────
    private double _joyLeftStartX, _joyLeftStartY;

    // ── Joystick destro: Yaw (X) + Thrust (Y) ───────────────────
    private double _joyRightStartX, _joyRightStartY;

    private const double JoyRadius = 50.0;
    private const int SendHz = 20;

    private System.Timers.Timer? _sendTimer;
    private System.Timers.Timer? _pingTimer;
    private long _lastPingMs;

    public DronePage()
    {
        InitializeComponent();

        _vm = IPlatformApplication.Current!.Services
            .GetRequiredService<MainViewModel>();
        BindingContext = _vm;

        SetupJoysticks();
        StartTimers();

        // Collega la GamepadBar al target drone (già presente nel XAML)
        DroneGamepadBar.AssignedTarget = GamepadTarget.Drone;
    }

    // ════════════════════════════════════════════════════════════════
    //  JOYSTICK TOUCH
    // ════════════════════════════════════════════════════════════════

    private void SetupJoysticks()
    {
        var panLeft = new PanGestureRecognizer();
        panLeft.PanUpdated += OnJoyLeftPan;
        DroneJoyLeftOuter.GestureRecognizers.Add(panLeft);

        var panRight = new PanGestureRecognizer();
        panRight.PanUpdated += OnJoyRightPan;
        DroneJoyRightOuter.GestureRecognizers.Add(panRight);
    }

    private void OnJoyLeftPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _joyLeftStartX = e.TotalX;
                _joyLeftStartY = e.TotalY;
                break;

            case GestureStatus.Running:
                var dx = e.TotalX - _joyLeftStartX;
                var dy = e.TotalY - _joyLeftStartY;
                Clamp(dx, dy, out var normX, out var normY);

                DroneJoyLeftDot.TranslationX = normX * JoyRadius;
                DroneJoyLeftDot.TranslationY = normY * JoyRadius;

                _roll = (int)(normX * 100);
                _pitch = (int)(-normY * 100); // su = pitch positivo

                UpdateValLabels();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _roll = _pitch = 0;
                DroneJoyLeftDot.TranslationX = 0;
                DroneJoyLeftDot.TranslationY = 0;
                UpdateValLabels();
                break;
        }
    }

    private void OnJoyRightPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _joyRightStartX = e.TotalX;
                _joyRightStartY = e.TotalY;
                break;

            case GestureStatus.Running:
                var dx = e.TotalX - _joyRightStartX;
                var dy = e.TotalY - _joyRightStartY;
                Clamp(dx, dy, out var normX, out var normY);

                DroneJoyRightDot.TranslationX = normX * JoyRadius;
                DroneJoyRightDot.TranslationY = normY * JoyRadius;

                _yaw = (int)(normX * 100);
                _thrust = (int)(-normY * 100); // su = thrust positivo
                _thrust = Math.Clamp(_thrust, 0, 100);

                UpdateValLabels();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _yaw = 0;
                _thrust = 0;
                DroneJoyRightDot.TranslationX = 0;
                DroneJoyRightDot.TranslationY = 0;
                UpdateValLabels();
                break;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  UTILITY
    // ════════════════════════════════════════════════════════════════

    private static void Clamp(double dx, double dy,
                               out double normX, out double normY)
    {
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist > JoyRadius)
        {
            normX = dx / dist;
            normY = dy / dist;
        }
        else
        {
            normX = dx / JoyRadius;
            normY = dy / JoyRadius;
        }
    }

    private void UpdateValLabels()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RollVal.Text = _roll.ToString();
            PitchVal.Text = _pitch.ToString();
            ThrustVal.Text = $"{_thrust}%";
            YawVal.Text = _yaw.ToString();
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  TIMER
    // ════════════════════════════════════════════════════════════════

    private void StartTimers()
    {
        // Invio comandi RPYT a 20Hz
        _sendTimer = new System.Timers.Timer(1000.0 / SendHz);
        _sendTimer.Elapsed += async (_, _) =>
        {
            if (_roll != 0 || _pitch != 0 || _yaw != 0 || _thrust != 0)
                await _vm.DroneRpyt(_roll, _pitch, _yaw, _thrust);
        };
        _sendTimer.Start();

        // Ping ogni secondo
        _pingTimer = new System.Timers.Timer(1000);
        _pingTimer.Elapsed += async (_, _) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await _vm.Mqtt.PublishAsync("drone/ping", "ping");
            sw.Stop();
            _lastPingMs = sw.ElapsedMilliseconds;
            MainThread.BeginInvokeOnMainThread(()
                => PingLabel.Text = $"{_lastPingMs} ms/ping");
        };
        _pingTimer.Start();
    }

    // ════════════════════════════════════════════════════════════════
    //  PULSANTI
    // ════════════════════════════════════════════════════════════════

    private async void OnDroneStopClicked(object sender, EventArgs e)
    {
        _roll = _pitch = _yaw = _thrust = 0;
        UpdateValLabels();
        await _vm.DroneStop();
    }

    private async void OnDroneLandClicked(object sender, EventArgs e)
    {
        _thrust = 0;
        UpdateValLabels();
        await _vm.DroneLand();
    }

    // ════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════════════════

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _sendTimer?.Stop();
        _pingTimer?.Stop();
        _ = _vm.DroneStop();
    }
}