using LagmaIpp.Services;
using LagmaIpp.ViewModels;

namespace LagmaIpp.Views;

// ── Modelli locali per gli elenchi ──────────────────────────────

public class MediaItem
{
    public string DisplayName { get; init; } = "";
    public string SubFolder { get; init; } = "";
    public string FileType { get; init; } = "";
    public string TypeIcon { get; init; } = "🎵";
    public string MqttPath { get; init; } = ""; // payload esatto per MQTT
    public bool IsVideo { get; init; }
}

// ── Page ─────────────────────────────────────────────────────────

public partial class MusicPage : ContentPage
{
    private readonly MainViewModel _vm;

    private List<MediaItem> _allMusic = new();
    private List<MediaItem> _allSfx = new();
    private List<MediaItem> _allVideo = new();

    private MediaItem? _currentItem;
    private bool _isPlaying;
    private string _currentTab = "music";

    private static readonly HashSet<string> ImageExts =
        new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

    public MusicPage()
    {
        InitializeComponent();
        _vm = App.ViewModel;
        BindingContext = _vm;

        // Iscrizione ai nuovi eventi separati
        _vm.AudioMusicListUpdated += OnAudioMusicListUpdated;
        _vm.AudioSfxListUpdated += OnAudioSfxListUpdated;
        _vm.VideoListUpdated += OnVideoListUpdated;

        // Caricamento cache (usando le nuove proprietà)
        if (_vm.LastAudioMusicListJson is not null)
            OnAudioMusicListUpdated(_vm.LastAudioMusicListJson);
        if (_vm.LastAudioSfxListJson is not null)
            OnAudioSfxListUpdated(_vm.LastAudioSfxListJson);
        if (_vm.LastVideoListJson is not null)
            OnVideoListUpdated(_vm.LastVideoListJson);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Rimozione iscrizione
        _vm.AudioMusicListUpdated -= OnAudioMusicListUpdated;
        _vm.AudioSfxListUpdated -= OnAudioSfxListUpdated;
        _vm.VideoListUpdated -= OnVideoListUpdated;
    }

    // ════════════════════════════════════════════════════════════════
    //  PARSING LISTE (già sul main thread grazie al MainViewModel)
    // ════════════════════════════════════════════════════════════════

    private void OnAudioMusicListUpdated(string json)
    {
        try
        {
            var musicFiles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new();
            _allMusic.Clear();
            foreach (var f in musicFiles) _allMusic.Add(BuildAudioItem(f));
            ApplyMusicFilter(SearchBarMusic.Text ?? "");
        }
        catch (Exception ex) { Console.WriteLine($"Errore musica: {ex.Message}"); }
    }

    private void OnAudioSfxListUpdated(string json)
    {
        try
        {
            var sfxFiles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new();
            _allSfx.Clear();
            foreach (var f in sfxFiles) _allSfx.Add(BuildAudioItem(f));
            RebuildSfxGrid();
        }
        catch (Exception ex) { Console.WriteLine($"Errore SFX: {ex.Message}"); }
    }

    private void OnVideoListUpdated(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            _allVideo.Clear();
            if (root.TryGetProperty("videos", out var arr))
            {
                foreach (var el in arr.EnumerateArray())
                {
                    var fn = el.GetString() ?? "";
                    var ext = System.IO.Path.GetExtension(fn);
                    bool img = ImageExts.Contains(ext);
                    _allVideo.Add(new MediaItem
                    {
                        DisplayName = System.IO.Path.GetFileNameWithoutExtension(fn),
                        FileType = ext.ToUpperInvariant(),
                        TypeIcon = img ? "🖼️" : "🎬",
                        MqttPath = fn,
                        IsVideo = true
                    });
                }
            }

            VideoList.ItemsSource = null;
            VideoList.ItemsSource = _allVideo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MusicPage] Errore parsing video list: {ex.Message}");
        }
    }

    private static MediaItem BuildAudioItem(string mqttPath)
    {
        var parts = mqttPath.Split('/');
        var folder = parts.Length > 1 ? parts[0] : "";
        var fn = parts.Last();
        return new MediaItem
        {
            DisplayName = System.IO.Path.GetFileNameWithoutExtension(fn),
            SubFolder = folder,
            FileType = System.IO.Path.GetExtension(fn).ToUpperInvariant(),
            TypeIcon = "🎵",
            MqttPath = mqttPath,
            IsVideo = false
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  TAB SWITCHER
    // ════════════════════════════════════════════════════════════════

    private void OnTabMusicClicked(object sender, EventArgs e) => SwitchTab("music");
    private void OnTabSfxClicked(object sender, EventArgs e) => SwitchTab("sfx");
    private void OnTabVideoClicked(object sender, EventArgs e) => SwitchTab("video");

    private void SwitchTab(string tab)
    {
        _currentTab = tab;
        PanelMusic.IsVisible = tab == "music";
        PanelSfx.IsVisible = tab == "sfx";
        PanelVideo.IsVisible = tab == "video";

        SetTabStyle(TabMusicBorder, LblTabMusic, tab == "music");
        SetTabStyle(TabSfxBorder, LblTabSfx, tab == "sfx");
        SetTabStyle(TabVideoBorder, LblTabVideo, tab == "video");
    }

    private static void SetTabStyle(Border border, Label label, bool active)
    {
        border.BackgroundColor = active
            ? Color.FromArgb("#1565C0")
            : Color.FromArgb("#0D1B2E");
        border.Stroke = active
            ? null
            : new SolidColorBrush(Color.FromArgb("#1E2A38"));
        border.StrokeThickness = active ? 0 : 1;
        label.TextColor = active ? Colors.White : Color.FromArgb("#8BA3C4");
    }

    // ════════════════════════════════════════════════════════════════
    //  MUSICA
    // ════════════════════════════════════════════════════════════════

    private void OnMusicSearchChanged(object sender, TextChangedEventArgs e)
        => ApplyMusicFilter(e.NewTextValue ?? "");

    private void ApplyMusicFilter(string query)
    {
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allMusic
            : _allMusic.Where(m =>
                m.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                m.SubFolder.Contains(query, StringComparison.OrdinalIgnoreCase))
              .ToList();

        MusicList.ItemsSource = null;
        MusicList.ItemsSource = filtered;
    }

    private void OnMusicSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MediaItem item) return;
        MusicList.SelectedItem = null;
        PlayItem(item);
    }

    private void OnShuffleClicked(object sender, EventArgs e)
    {
        if (_allMusic.Count == 0) return;
        PlayItem(_allMusic[new Random().Next(_allMusic.Count)]);
    }

    // ════════════════════════════════════════════════════════════════
    //  SOUND FX — griglia bottoni
    // ════════════════════════════════════════════════════════════════

    private void RebuildSfxGrid()
    {
        SfxGrid.Children.Clear();
        foreach (var sfx in _allSfx)
        {
            var btn = new Border
            {
                BackgroundColor = Color.FromArgb("#0D1B2E"),
                Stroke = new SolidColorBrush(Color.FromArgb("#1E2A38")),
                StrokeThickness = 1,
                Padding = new Thickness(14, 10),
                Margin = new Thickness(4),
            };
            btn.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            { CornerRadius = new CornerRadius(10) };
            btn.Content = new Label
            {
                Text = sfx.DisplayName,
                TextColor = Color.FromArgb("#E8EDF2"),
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Center
            };

            var captured = sfx;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => PlayItem(captured);
            btn.GestureRecognizers.Add(tap);
            SfxGrid.Children.Add(btn);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  VIDEO / IMMAGINI
    // ════════════════════════════════════════════════════════════════

    private void OnVideoSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MediaItem item) return;
        VideoList.SelectedItem = null;
        PlayItem(item);
    }

    // ════════════════════════════════════════════════════════════════
    //  PLAYER BAR
    // ════════════════════════════════════════════════════════════════

    private void PlayItem(MediaItem item)
    {
        _currentItem = item;
        _isPlaying = true;
        UpdatePlayerBar();

        if (item.IsVideo)
            _ = _vm.Mqtt.CmdVideoPlay(item.MqttPath);
        else
            _ = _vm.Mqtt.CmdAudioPlay(item.MqttPath);

        _vm.MessaggiTx++;
    }

    private void OnPlayPauseClicked(object sender, EventArgs e)
    {
        if (_currentItem is null) return;
        PlayItem(_currentItem); // ri-invia la riproduzione
    }

    private void OnStopClicked(object sender, EventArgs e)
    {
        _isPlaying = false;
        if (_currentItem?.IsVideo == true)
            _ = _vm.Mqtt.CmdVideoStop();
        else
            _ = _vm.Mqtt.CmdAudioStop();

        _vm.MessaggiTx++;
        UpdatePlayerBar();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await _vm.Mqtt.CmdAudioRefresh();
        _vm.MessaggiTx++;
    }

    private void UpdatePlayerBar()
    {
        if (_currentItem is null)
        {
            CurrentTitle.Text = "—";
            CurrentType.Text = "Nessuna riproduzione";
            BtnPlayPause.Text = "▶  PLAY";
            BtnPlayPause.BackgroundColor = Color.FromArgb("#00C853");
            return;
        }

        CurrentTitle.Text = _currentItem.DisplayName;
        CurrentType.Text = _isPlaying
            ? $"{_currentItem.TypeIcon}  {_currentItem.FileType}  · in riproduzione"
            : $"{_currentItem.TypeIcon}  {_currentItem.FileType}  · fermato";

        BtnPlayPause.Text = "▶  RIPRODUCI";
        BtnPlayPause.BackgroundColor = _isPlaying
            ? Color.FromArgb("#1565C0")
            : Color.FromArgb("#00C853");
    }

    // ════════════════════════════════════════════════════════════════
    //  VOLUME
    // ════════════════════════════════════════════════════════════════

    private void OnMuteClicked(object sender, EventArgs e) => _vm.Volume = 0;
    private void OnMaxVolumeClicked(object sender, EventArgs e) => _vm.Volume = 100;
}