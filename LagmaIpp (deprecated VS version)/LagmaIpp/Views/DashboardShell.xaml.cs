using LagmaIpp.ViewModels;

namespace LagmaIpp.Views;

public partial class DashboardShell : ContentPage
{
    private readonly MainViewModel _vm;

    // Lazy-loaded page instances
    private RobotPage? _robotPage;
    private RobotArmPage? _braccioPage;
    private DronePage? _dronePage;
    private MapPage? _mapPage;
    private MusicPage? _musicPage;
    private MqttPage? _mqttPage;

    // Nav border references for active highlight
    private Border? _activeNavItem;
    private BoxView? _activeIndicator;

    private string _currentPage = "";

    public DashboardShell()
    {
        InitializeComponent();
        _vm = App.ViewModel;
        BindingContext = _vm;

        // Wire status updates
        _vm.PropertyChanged += OnVmPropertyChanged;

        // Default: Robot page
        ShowPage("robot");
        SetNavActive(NavRobot, IndRobot, null);
    }

    // ══════════════════════════════════════
    //  NAV HANDLERS
    // ══════════════════════════════════════

    private void OnNavRobotTapped(object? s, TappedEventArgs e)
    {
        ShowPage("robot");
        SetNavActive(NavRobot, IndRobot, null);
        PageTitleLabel.Text = "Robot Control";
    }

    private void OnNavBraccioTapped(object? s, TappedEventArgs e)
    {
        ShowPage("braccio");
        SetNavActive(NavBraccio, IndBraccio, null);
        PageTitleLabel.Text = "Braccio";
    }

    private void OnNavDroneTapped(object? s, TappedEventArgs e)
    {
        ShowPage("drone");
        SetNavActive(NavDrone, IndDrone, null);
        PageTitleLabel.Text = "Drone";
    }

    private void OnNavMappaTapped(object? s, TappedEventArgs e)
    {
        ShowPage("mappa");
        SetNavActive(NavMappa, IndMappa, null);
        PageTitleLabel.Text = "Mappa";
    }

    private void OnNavMusicaTapped(object? s, TappedEventArgs e)
    {
        ShowPage("musica");
        SetNavActive(NavMusica, IndMusica, null);
        PageTitleLabel.Text = "Media";
    }

    private void OnNavMqttTapped(object? s, TappedEventArgs e)
    {
        ShowPage("mqtt");
        SetNavActive(NavMqtt, IndMqtt, null);
        PageTitleLabel.Text = "MQTT";
    }

    // ══════════════════════════════════════
    //  PAGE SWAP
    // ══════════════════════════════════════

    private void ShowPage(string name)
    {
        if (_currentPage == name) return;
        _currentPage = name;

        ContentPage page = name switch
        {
            "robot" => _robotPage ??= new RobotPage(),
            "braccio" => _braccioPage ??= new RobotArmPage(),
            "drone" => _dronePage ??= new DronePage(),
            "mappa" => _mapPage ??= new MapPage(),
            "musica" => _musicPage ??= new MusicPage(),
            "mqtt" => _mqttPage ??= new MqttPage(),
            _ => _robotPage ??= new RobotPage()
        };

        PageContent.Content = page.Content;
    }

    // ══════════════════════════════════════
    //  ACTIVE NAV HIGHLIGHT
    // ══════════════════════════════════════

    private void SetNavActive(Border newItem, BoxView indicator, Label? label)
    {
        // Reset previous
        if (_activeNavItem != null)
            _activeNavItem.BackgroundColor = Colors.Transparent;
        if (_activeIndicator != null)
            _activeIndicator.BackgroundColor = Colors.Transparent;

        // Set new
        newItem.BackgroundColor = Color.FromArgb("#0078D4");
        indicator.BackgroundColor = Colors.White;

        _activeNavItem = newItem;
        _activeIndicator = indicator;
    }

    // ══════════════════════════════════════
    //  STATUS BAR UPDATES
    // ══════════════════════════════════════

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.MqttConnected):
                    UpdateMqttStatus();
                    break;
                case nameof(MainViewModel.MessaggiRx):
                    HeaderRx.Text = _vm.MessaggiRx.ToString();
                    break;
                case nameof(MainViewModel.MessaggiTx):
                    HeaderTx.Text = _vm.MessaggiTx.ToString();
                    break;
                case nameof(MainViewModel.PiOnline):
                case nameof(MainViewModel.PiTempCpu):
                    UpdatePiStatus();
                    break;
            }
        });
    }

    private void UpdateMqttStatus()
    {
        var connected = _vm.MqttConnected;
        var green = Color.FromArgb("#00A86B");
        var red = Color.FromArgb("#D93025");

        HeaderMqttDot.Color = connected ? green : red;
        HeaderMqttLabel.Text = connected ? "CONNESSO" : "DISCONNESSO";
        HeaderMqttLabel.TextColor = connected ? green : red;

        SidebarMqttDot.Color = connected ? green : red;
        SidebarMqttLabel.Text = connected ? "ON" : "OFF";
        SidebarMqttLabel.TextColor = connected ? green : red;
    }

    private void UpdatePiStatus()
    {
        var online = _vm.PiOnline;
        var green = Color.FromArgb("#00A86B");
        var dim = Color.FromArgb("#5A6E8A");

        SidebarPiDot.Color = online ? green : dim;
        SidebarPiLabel.Text = online ? "Online" : "---";
        SidebarPiLabel.TextColor = online ? green : dim;
        SidebarTempLabel.Text = _vm.PiTempStr;
        HeaderBatteryLabel.Text = online ? $"{_vm.PiCpuPercent:F0}% CPU" : "---";
    }
}