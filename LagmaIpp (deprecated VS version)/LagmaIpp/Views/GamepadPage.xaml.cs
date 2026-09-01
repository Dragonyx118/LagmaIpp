using LagmaIpp.Services;

namespace LagmaIpp.Views;

public partial class GamepadBar : ContentView
{
    // ── Target assegnato a questa barra ──────────────────────────
    public GamepadTarget AssignedTarget { get; set; } = GamepadTarget.None;

    private GamepadService _gp => App.Gamepad;
    private bool _active = false;

    // Timer aggiornamento UI (servo index, connected state)
    private System.Timers.Timer? _uiTimer;

    public GamepadBar()
    {
        InitializeComponent();
        StartUiTimer();
    }

    // ════════════════════════════════════════════════════════════
    //  TOGGLE
    // ════════════════════════════════════════════════════════════

    private void OnToggleTapped(object? sender, TappedEventArgs e)
    {
        _active = !_active;

        if (_active)
        {
            // Disattiva gli altri target prima
            _gp.SetTarget(AssignedTarget);
        }
        else
        {
            _gp.SetTarget(GamepadTarget.None);
        }

        RefreshUi();
    }

    // ════════════════════════════════════════════════════════════
    //  UI REFRESH
    // ════════════════════════════════════════════════════════════

    private void StartUiTimer()
    {
        _uiTimer = new System.Timers.Timer(200); // 5Hz per l'UI
        _uiTimer.Elapsed += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshUi);
        _uiTimer.Start();
    }

    private void RefreshUi()
    {
        bool connected = _gp.IsConnected;
        bool isActiveTarget = _gp.Target == AssignedTarget && AssignedTarget != GamepadTarget.None;

        // Se un altro target ha preso il controllo, sincronizza il flag
        if (!isActiveTarget && _active)
            _active = false;

        // Colori base
        var cyanColor = Color.FromArgb("#00D4FF");
        var greenColor = Color.FromArgb("#00C853");
        var dimColor = Color.FromArgb("#3D5068");
        var redColor = Color.FromArgb("#FF3B5C");

        // Status
        if (!connected)
        {
            LblStatus.Text = "Controller — non connesso";
            LblStatus.TextColor = dimColor;
            LblHint.Text = "Collega un gamepad via Bluetooth";
        }
        else if (_active)
        {
            LblStatus.Text = "Controller ATTIVO";
            LblStatus.TextColor = greenColor;
            LblHint.Text = GetHintForTarget();
        }
        else
        {
            LblStatus.Text = "Controller connesso — in attesa";
            LblStatus.TextColor = cyanColor;
            LblHint.Text = "Premi ATTIVA per controllare";
        }

        // Bottone
        LblBtn.Text = _active ? "DISATTIVA" : "ATTIVA";
        LblBtn.TextColor = _active ? redColor : (connected ? cyanColor : dimColor);
        BtnBorder.Stroke = new SolidColorBrush(_active ? redColor : (connected ? cyanColor : dimColor));

        // Indicatore servo (solo ARM)
        bool showServo = _active && AssignedTarget == GamepadTarget.Arm;
        ServoIndicator.IsVisible = showServo;
        if (showServo)
            LblServo.Text = _gp.SelectedServo.ToString();
    }

    private string GetHintForTarget() => AssignedTarget switch
    {
        GamepadTarget.Robot => "LS=muovi  LT/RT=ruota  A=stop",
        GamepadTarget.Drone => "LS=roll/pitch  RS=yaw/thrust  A=land  B=stop",
        GamepadTarget.Arm => "LS Y=muovi servo  Y/X=cambia servo  A=home",
        _ => ""
    };

    // ════════════════════════════════════════════════════════════
    //  ANDROID GAMEPAD POLLING (platform-specific)
    // ════════════════════════════════════════════════════════════

#if ANDROID
    /// <summary>
    /// Chiamato da MainActivity.OnGenericMotionEvent / OnKeyEvent
    /// per passare gli input del controller al GamepadService.
    /// </summary>
    public static void FeedAndroidMotion(Android.Views.MotionEvent e)
    {
        App.Gamepad.UpdateState(s =>
        {
            s.LeftX = GetAxisValue(e, Android.Views.Axis.X);
            s.LeftY = GetAxisValue(e, Android.Views.Axis.Y);
            s.RightX = GetAxisValue(e, Android.Views.Axis.Z);
            s.RightY = GetAxisValue(e, Android.Views.Axis.Rz);
            s.LT = (GetAxisValue(e, Android.Views.Axis.Ltrigger) + 1f) / 2f;
            s.RT = (GetAxisValue(e, Android.Views.Axis.Rtrigger) + 1f) / 2f;

            var hat = GetAxisValue(e, Android.Views.Axis.HatX);
            s.DpadLeft = hat < -0.5f;
            s.DpadRight = hat > 0.5f;
            var hatY = GetAxisValue(e, Android.Views.Axis.HatY);
            s.DpadUp = hatY < -0.5f;
            s.DpadDown = hatY > 0.5f;
        });
    }

    public static void FeedAndroidKey(Android.Views.Keycode keycode, bool pressed)
    {
        App.Gamepad.UpdateState(s =>
        {
            switch (keycode)
            {
                case Android.Views.Keycode.ButtonA: s.South = pressed; break;
                case Android.Views.Keycode.ButtonB: s.East = pressed; break;
                case Android.Views.Keycode.ButtonX: s.West = pressed; break;
                case Android.Views.Keycode.ButtonY: s.North = pressed; break;
                case Android.Views.Keycode.ButtonL1: s.LeftBumper = pressed; break;
                case Android.Views.Keycode.ButtonR1: s.RightBumper = pressed; break;
                case Android.Views.Keycode.ButtonStart: s.Start = pressed; break;
                case Android.Views.Keycode.ButtonSelect: s.Select = pressed; break;
            }
        });
    }

    private static float GetAxisValue(Android.Views.MotionEvent e, Android.Views.Axis axis)
        => e.GetAxisValue(axis);
#endif
}
