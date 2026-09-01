namespace LagmaIpp.Views;

public partial class NavBar : ContentView
{
    // ── Evento tab selezionata ───────────────────────────────────
    public event Action<string>? TabChanged;

    // ── Tab corrente ─────────────────────────────────────────────
    public string CurrentTab { get; private set; } = "robot";

    // ── Mappa tab → (icona, label) ───────────────────────────────
    private Dictionary<string, (Image icon, Label label, StackLayout container)> _tabs = new();

    public NavBar()
    {
        InitializeComponent();

        _tabs = new()
        {
            ["robot"] = (IconRobot, LblRobot, TabRobot),
            ["braccio"] = (IconBraccio, LblBraccio, TabBraccio),
            ["drone"] = (IconDrone, LblDrone, TabDrone),
            ["mappa"] = (IconMappa, LblMappa, TabMappa),
            ["musica"] = (IconMusica, LblMusica, TabMusica),
            ["mqtt"] = (IconMqtt, LblMqtt, TabMqtt),
        };

        SelectTab("robot");
    }

    // ════════════════════════════════════════════════════════════════
    //  SELEZIONE TAB
    // ════════════════════════════════════════════════════════════════

    public void SelectTab(string tabName)
    {
        CurrentTab = tabName;

        foreach (var (key, (icon, label, _)) in _tabs)
        {
            bool active = key == tabName;
            icon.Opacity = active ? 1.0 : 0.35;
            label.TextColor = active
                ? Color.FromArgb("#00D4FF")
                : Color.FromArgb("#3D5068");
        }

        TabChanged?.Invoke(tabName);
    }

    // ════════════════════════════════════════════════════════════════
    //  TAP HANDLERS
    // ════════════════════════════════════════════════════════════════

    private void OnTabRobotTapped(object? sender, TappedEventArgs e) => SelectTab("robot");
    private void OnTabBraccioTapped(object? sender, TappedEventArgs e) => SelectTab("braccio");
    private void OnTabDroneTapped(object? sender, TappedEventArgs e) => SelectTab("drone");
    private void OnTabMappaTapped(object? sender, TappedEventArgs e) => SelectTab("mappa");
    private void OnTabMusicaTapped(object? sender, TappedEventArgs e) => SelectTab("musica");
    private void OnTabMqttTapped(object? sender, TappedEventArgs e) => SelectTab("mqtt");
}