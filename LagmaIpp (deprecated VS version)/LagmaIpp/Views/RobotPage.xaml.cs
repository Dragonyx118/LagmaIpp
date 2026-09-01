using LagmaIpp.Services;
using LagmaIpp.ViewModels;
using Microsoft.Maui.Platform;

#if WINDOWS
using Microsoft.UI.Xaml.Input;
using Windows.System;
#endif

namespace LagmaIpp.Views;

public partial class RobotPage : ContentPage
{
    private readonly MainViewModel _vm;

    // Traccia i tasti premuti per evitare ripetizioni a raffica
    private readonly HashSet<string> _keysHeld = new();
    private bool _keyboardActive = false;

    public RobotPage()
    {
        InitializeComponent();
        _vm = App.ViewModel;
        BindingContext = _vm;

        // Configura la GamepadBar per la macchinina
        RobotGamepadBar.AssignedTarget = GamepadTarget.Robot;
    }

    // ══════════════════════════════════════════════════════════════
    //  PAGE LIFECYCLE — registra/deregistra eventi tastiera
    // ══════════════════════════════════════════════════════════════

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RegisterKeyboardEvents();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        UnregisterKeyboardEvents();
        // Disattiva il gamepad quando si lascia la pagina
        if (App.Gamepad.Target == GamepadTarget.Robot)
            App.Gamepad.SetTarget(GamepadTarget.None);
    }

    // ══════════════════════════════════════════════════════════════
    //  KEYBOARD SUPPORT
    // ══════════════════════════════════════════════════════════════

    private void RegisterKeyboardEvents()
    {
#if WINDOWS
        if (Application.Current?.Windows?.FirstOrDefault()?.Handler?.PlatformView
            is Microsoft.UI.Xaml.Window win)
        {
            win.Content.KeyDown += OnWindowKeyDown;
            win.Content.KeyUp   += OnWindowKeyUp;
            win.Content.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }
#endif
    }

    private void UnregisterKeyboardEvents()
    {
#if WINDOWS
        if (Application.Current?.Windows?.FirstOrDefault()?.Handler?.PlatformView
            is Microsoft.UI.Xaml.Window win)
        {
            win.Content.KeyDown -= OnWindowKeyDown;
            win.Content.KeyUp   -= OnWindowKeyUp;
        }
#endif
    }

#if WINDOWS
    private async void OnWindowKeyDown(object sender,
                                       Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var key = e.Key.ToString();
        if (_keysHeld.Contains(key)) return;
        _keysHeld.Add(key);

        SetKeyboardActive(true);

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.W: case Windows.System.VirtualKey.Up:
                    await _vm.Avanti(); break;
                case Windows.System.VirtualKey.S: case Windows.System.VirtualKey.Down:
                    await _vm.Indietro(); break;
                case Windows.System.VirtualKey.A: case Windows.System.VirtualKey.Left:
                    await _vm.Sinistra(); break;
                case Windows.System.VirtualKey.D: case Windows.System.VirtualKey.Right:
                    await _vm.Destra(); break;
                case Windows.System.VirtualKey.Q:
                    await _vm.Mecanum(-_vm.VelocitaGlobale, _vm.VelocitaGlobale, 0); break;
                case Windows.System.VirtualKey.E:
                    await _vm.Mecanum(_vm.VelocitaGlobale, _vm.VelocitaGlobale, 0); break;
                case Windows.System.VirtualKey.Z:
                    await _vm.RotaSx(); break;
                case Windows.System.VirtualKey.X:
                    await _vm.RotaDx(); break;
                case Windows.System.VirtualKey.Space:
                    await _vm.Stop(); break;
            }
        });
    }

    private async void OnWindowKeyUp(object sender,
                                     Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var key = e.Key.ToString();
        _keysHeld.Remove(key);

        bool anyMovementHeld = _keysHeld.Any(k => k is "W" or "S" or "A" or "D"
            or "Up" or "Down" or "Left" or "Right"
            or "Q" or "E" or "Z" or "X");

        if (!anyMovementHeld)
        {
            SetKeyboardActive(false);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _vm.Stop();
            });
        }
    }
#endif

    private void SetKeyboardActive(bool active)
    {
        if (_keyboardActive == active) return;
        _keyboardActive = active;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            KeyboardIndicator.TextColor = active
                ? Color.FromArgb("#0078D4")
                : Color.FromArgb("#8FA3BC");
            KeyboardActiveDot.BackgroundColor = active
                ? Color.FromArgb("#00A86B")
                : Color.FromArgb("#8FA3BC");
        });
    }

    // ══════════════════════════════════════════════════════════════
    //  MOVEMENT BUTTON COMMANDS
    // ══════════════════════════════════════════════════════════════

    private async void OnAvantiClicked(object? s, EventArgs e) => await _vm.Avanti();
    private async void OnIndietroClicked(object? s, EventArgs e) => await _vm.Indietro();
    private async void OnSinistraClicked(object? s, EventArgs e) => await _vm.Sinistra();
    private async void OnDestraClicked(object? s, EventArgs e) => await _vm.Destra();
    private async void OnRotaSxClicked(object? s, EventArgs e) => await _vm.RotaSx();
    private async void OnRotaDxClicked(object? s, EventArgs e) => await _vm.RotaDx();
    private async void OnStopClicked(object? s, EventArgs e) => await _vm.Stop();

    private async void OnSxAvantiClicked(object? s, EventArgs e)
    {
        var vel = _vm.VelocitaGlobale;
        await _vm.Mecanum(-vel, vel, 0);
    }

    private async void OnDxAvantiClicked(object? s, EventArgs e)
    {
        var vel = _vm.VelocitaGlobale;
        await _vm.Mecanum(vel, vel, 0);
    }
    // ════════════════════════════════════════════════════════════════
    //  CAMERA TILT
    // ════════════════════════════════════════════════════════════════

    private void OnCamUp(object sender, EventArgs e)
        => App.ViewModel.ServoCamera = Math.Min(160, App.ViewModel.ServoCamera + 10);

    private void OnCamDown(object sender, EventArgs e)
        => App.ViewModel.ServoCamera = Math.Max(80, App.ViewModel.ServoCamera - 10);

    private void OnCamCenter(object sender, EventArgs e)
        => App.ViewModel.ServoCamera = 120;
}
