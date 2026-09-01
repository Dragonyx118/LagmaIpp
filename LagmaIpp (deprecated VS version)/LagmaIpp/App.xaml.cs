using LagmaIpp.Services;

namespace LagmaIpp;

public partial class App : Application
{
    // Singleton ViewModel accessibile da tutta l'app
    public static LagmaIpp.ViewModels.MainViewModel ViewModel { get; private set; } = null!;
    // Singleton GamepadService
    public static GamepadService Gamepad { get; private set; } = null!;

    public App(LagmaIpp.ViewModels.MainViewModel vm, GamepadService gamepad)
    {
        InitializeComponent();
        ViewModel = vm;
        Gamepad = gamepad;
        Gamepad.Initialize(vm);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Created += (_, _) => _ = ViewModel.ConnectAsync();
        return window;
    }
}
