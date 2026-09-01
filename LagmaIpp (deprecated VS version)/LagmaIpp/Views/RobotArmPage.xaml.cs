using LagmaIpp.Services;
using LagmaIpp.ViewModels;

namespace LagmaIpp.Views;

public partial class RobotArmPage : ContentPage
{
    private readonly MainViewModel _vm;
    private bool _liveMode = false;

    private readonly List<(string Name, int[] Values)> _presets = new();

    // Badge borders per evidenziare il servo selezionato via gamepad
    private Border[] _servoBadges = null!;
    private readonly Color _badgeActive = Color.FromArgb("#00D4FF");
    private readonly Color _badgeInactive = Color.FromArgb("#1E2A38");
    private readonly Color _textActive = Color.FromArgb("#00D4FF");
    private readonly Color _textInactive = Color.FromArgb("#3D5068");

    private System.Timers.Timer? _badgeTimer;

    // Preset in corso di riproduzione
    private bool _applyingPreset = false;

    // Chiave Preferences per la persistenza
    private const string PresetKey = "arm_presets_v1";

    public RobotArmPage()
    {
        InitializeComponent();
        _vm = App.ViewModel;
        BindingContext = _vm;

        ArmGamepadBar.AssignedTarget = GamepadTarget.Arm;

        // Badge S0..S5 + CAM
        _servoBadges = new Border[]
        {
            ServoBadge0, ServoBadge1, ServoBadge2,
            ServoBadge3, ServoBadge4, ServoBadge5, ServoBadgeCam
        };

        UpdateLiveModeButton();
        AddDefaultPresets();
        RefreshPresetList();
        StartBadgeTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _badgeTimer?.Stop();
        if (App.Gamepad.Target == GamepadTarget.Arm)
            App.Gamepad.SetTarget(GamepadTarget.None);
    }

    // ════════════════════════════════════════════════════════════════
    //  BADGE SERVO SELEZIONATO
    // ════════════════════════════════════════════════════════════════

    private void StartBadgeTimer()
    {
        _badgeTimer = new System.Timers.Timer(150);
        _badgeTimer.Elapsed += (_, _) => MainThread.BeginInvokeOnMainThread(UpdateServoBadges);
        _badgeTimer.Start();
    }

    private void UpdateServoBadges()
    {
        bool gpActive = App.Gamepad.Target == GamepadTarget.Arm;
        int selected = App.Gamepad.SelectedServo;

        for (int i = 0; i < _servoBadges.Length; i++)
        {
            bool isSelected = gpActive && (i == selected);
            _servoBadges[i].Stroke = new SolidColorBrush(isSelected ? _badgeActive : _badgeInactive);
            _servoBadges[i].BackgroundColor = isSelected
                ? Color.FromArgb("#001E30")
                : Color.FromArgb("#0A1628");

            if (_servoBadges[i].Content is Label lbl)
                lbl.TextColor = isSelected ? _textActive : _textInactive;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  LIVE MODE
    // ════════════════════════════════════════════════════════════════

    private void OnLiveModeClicked(object sender, EventArgs e)
    {
        _liveMode = !_liveMode;
        UpdateLiveModeButton();
    }

    private void UpdateLiveModeButton()
    {
        BtnLiveMode.Text = _liveMode ? "◀  Live mode ON" : "◀  Live mode";
        BtnLiveMode.BackgroundColor = _liveMode
            ? Color.FromArgb("#00C853")
            : Color.FromArgb("#FF3B5C");
    }

    // ════════════════════════════════════════════════════════════════
    //  CAMERA TILT
    // ════════════════════════════════════════════════════════════════

    private void OnCamUp(object sender, EventArgs e)
        => _vm.ServoCamera = Math.Min(160, _vm.ServoCamera + 10);

    private void OnCamDown(object sender, EventArgs e)
        => _vm.ServoCamera = Math.Max(80, _vm.ServoCamera - 10);

    private void OnCamCenter(object sender, EventArgs e)
        => _vm.ServoCamera = 120;

    // ════════════════════════════════════════════════════════════════
    //  PRESET — persistenza con Preferences
    // ════════════════════════════════════════════════════════════════

    private void AddDefaultPresets()
    {
        var loaded = LoadPresetsFromStorage();
        if (loaded.Count == 0)
        {
            // Default iniziali
            _presets.Add(("Home", new[] { 90, 90, 90, 90, 90, 120, 120 }));
            _presets.Add(("Riposo", new[] { 90, 30, 60, 90, 90, 90, 120 }));
            SavePresetsToStorage();
        }
        else
        {
            _presets.AddRange(loaded);
        }
    }

    private List<(string Name, int[] Values)> LoadPresetsFromStorage()
    {
        var result = new List<(string, int[])>();
        try
        {
            var json = Preferences.Get(PresetKey, "");
            if (string.IsNullOrEmpty(json)) return result;

            var arr = System.Text.Json.JsonSerializer.Deserialize<PresetDto[]>(json);
            if (arr == null) return result;

            foreach (var dto in arr)
            {
                // Compatibilità con preset vecchi a 6 valori (senza camera)
                var vals = dto.Values.Length == 6
                    ? dto.Values.Concat(new[] { 120 }).ToArray()
                    : dto.Values;
                result.Add((dto.Name, vals));
            }
        }
        catch { /* ignora errori di deserializzazione */ }
        return result;
    }

    private void SavePresetsToStorage()
    {
        try
        {
            var dtos = _presets
                .Select(p => new PresetDto { Name = p.Name, Values = p.Values })
                .ToArray();
            var json = System.Text.Json.JsonSerializer.Serialize(dtos);
            Preferences.Set(PresetKey, json);
        }
        catch { }
    }

    private record PresetDto
    {
        public string Name { get; init; } = "";
        public int[] Values { get; init; } = Array.Empty<int>();
    }

    // ── Applica preset: un servo alla volta, 1s di stacco ────────

    private void ApplyPreset(int idx)
    {
        if (idx < 0 || idx >= _presets.Count || _applyingPreset) return;
        _ = ApplyPresetSequentialAsync(idx);
    }

    private async Task ApplyPresetSequentialAsync(int idx)
    {
        _applyingPreset = true;
        var v = _presets[idx].Values;

        var steps = new (Action<int> Set, int Val)[]
        {
            (val => _vm.Servo0     = val, v.Length > 0 ? v[0] : 90),
            (val => _vm.Servo1     = val, v.Length > 1 ? v[1] : 90),
            (val => _vm.Servo2     = val, v.Length > 2 ? v[2] : 90),
            (val => _vm.Servo3     = val, v.Length > 3 ? v[3] : 90),
            (val => _vm.Servo4     = val, v.Length > 4 ? v[4] : 90),
            (val => _vm.Servo5     = val, v.Length > 5 ? v[5] : 120),
            (val => _vm.ServoCamera = val, v.Length > 6 ? v[6] : 120),
        };

        foreach (var (Set, Val) in steps)
        {
            Set(Val);
            await Task.Delay(1000);
        }

        _applyingPreset = false;
    }

    // ── Lista preset dinamica ─────────────────────────────────────

    private void RefreshPresetList()
    {
        PresetList.Children.Clear();
        for (int i = 0; i < _presets.Count; i++)
        {
            var idx = i;
            var preset = _presets[i];

            var row = new Border
            {
                BackgroundColor = Color.FromArgb("#0A0E14"),
                Stroke = new SolidColorBrush(Color.FromArgb("#1E2A38")),
                StrokeThickness = 1,
                Padding = new Thickness(8, 6)
            };
            row.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            { CornerRadius = new CornerRadius(6) };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection(
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto))
            };

            var lbl = new Label
            {
                Text = preset.Name,
                TextColor = Color.FromArgb("#E8EDF2"),
                FontSize = 11,
                VerticalOptions = LayoutOptions.Center
            };
            var btn = new Label
            {
                Text = "▶",
                TextColor = Color.FromArgb("#00D4FF"),
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center
            };
            btn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => ApplyPreset(idx))
            });

            grid.Children.Add(lbl); Grid.SetColumn(lbl, 0);
            grid.Children.Add(btn); Grid.SetColumn(btn, 1);

            row.Content = grid;
            PresetList.Children.Add(row);
        }
    }

    // ── Salva / Elimina ───────────────────────────────────────────

    private async void OnPresetPlayClicked(object sender, EventArgs e) => ApplyPreset(0);

    private async void OnSavePresetClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("Salva Preset", "Nome del preset:", "Salva", "Annulla",
            "es: Posa 1", maxLength: 20);
        if (string.IsNullOrWhiteSpace(name)) return;

        _presets.Add((name, new[] {
            _vm.Servo0, _vm.Servo1, _vm.Servo2,
            _vm.Servo3, _vm.Servo4, _vm.Servo5, _vm.ServoCamera
        }));
        SavePresetsToStorage();
        RefreshPresetList();
    }

    private async void OnDeletePresetClicked(object sender, EventArgs e)
    {
        if (_presets.Count == 0) return;
        var names = _presets.Select(p => p.Name).ToArray();
        var choice = await DisplayActionSheetAsync("Elimina preset", "Annulla", null, names);
        var idx = _presets.FindIndex(p => p.Name == choice);
        if (idx >= 0)
        {
            _presets.RemoveAt(idx);
            SavePresetsToStorage();
            RefreshPresetList();
        }
    }

    private async void OnPresetClicked(object sender, EventArgs e)
    {
        var result = await DisplayActionSheetAsync("Preset rapido", "Annulla", null,
            "Home (tutti 90°)", "Riposo", "Pinza aperta", "Pinza chiusa");
        switch (result)
        {
            case "Home (tutti 90°)": _ = ApplyPresetSequentialAsync(-1); SetAllServos(90, 90, 90, 90, 90, 120, 120); break;
            case "Riposo": _ = ApplyPresetSequentialAsync(-1); SetAllServos(90, 30, 60, 90, 90, 90, 120); break;
            case "Pinza aperta": _vm.Servo5 = 120; break;
            case "Pinza chiusa": _vm.Servo5 = 30; break;
        }
    }

    private void SetAllServos(int s0, int s1, int s2, int s3, int s4, int s5, int cam)
    {
        _vm.Servo0 = s0;
        _vm.Servo1 = s1;
        _vm.Servo2 = s2;
        _vm.Servo3 = s3;
        _vm.Servo4 = s4;
        _vm.Servo5 = s5;
        _vm.ServoCamera = cam;
    }
}