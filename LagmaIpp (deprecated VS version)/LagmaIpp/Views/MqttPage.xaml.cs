using LagmaIpp.ViewModels;
using System.Collections.ObjectModel;

namespace LagmaIpp.Views;

public partial class MqttPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly ObservableCollection<string> _logLines = new();
    private readonly Dictionary<string, (Label valLabel, Label tsLabel, Label countLabel)> _topicCards = new();

    // Throughput tracking
    private int _lastRx = 0;
    private readonly List<float> _throughputHistory = new(Enumerable.Repeat(0f, 60));
    private DateTime _connectedAt = DateTime.MinValue;

    private static readonly string[] MonitoredTopics =
    {
        "robot/motori/stato", "robot/motori/cmd", "robot/motori/log",
        "robot/sensori/distanze", "robot/sensori/imu", "robot/sensori/tcrt",
        "robot/sensori/log", "robot/sensori/cmd",
        "robot/odometria", "robot/mappa/grid", "robot/cliff/stato", "robot/gps",
        "drone/cmd/rpyt", "drone/cmd/stop", "drone/status/bridge",
        "pi/stato", "pi/cmd", "ai/text_input",
    };

    public MqttPage()
    {
        InitializeComponent();
        _vm = App.ViewModel;
        BindingContext = _vm;

        LogView.ItemsSource = _logLines;
        NetworkGraph.Drawable = new ThroughputDrawable(_throughputHistory);

        BuildTopicCards();
        UpdateConnectionUI(_vm.MqttConnected);

        _vm.Mqtt.MessageReceived += OnMessageReceived;
        _vm.Mqtt.ConnectionChanged += OnConnectionChanged;

        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (_, _) => MainThread.BeginInvokeOnMainThread(UpdateStats);
        timer.Start();
    }

    // ══════════════════════════════════════
    //  TOPIC CARDS (stesso stile card del resto)
    // ══════════════════════════════════════

    private void BuildTopicCards()
    {
        foreach (var topic in MonitoredTopics)
        {
            var card = BuildTopicCard(topic, out var valLabel, out var tsLabel, out var cntLabel);
            TopicList.Children.Add(card);
            _topicCards[topic] = (valLabel, tsLabel, cntLabel);
        }
    }

    private static View BuildTopicCard(string topic,
        out Label valLabel, out Label tsLabel, out Label countLabel)
    {
        valLabel = new Label { TextColor = Color.FromArgb("#0078D4"), FontSize = 11, LineBreakMode = LineBreakMode.TailTruncation };
        tsLabel = new Label { TextColor = Color.FromArgb("#8FA3BC"), FontSize = 10 };
        countLabel = new Label { TextColor = Color.FromArgb("#8FA3BC"), FontSize = 10, HorizontalOptions = LayoutOptions.End };

        var topicLbl = new Label
        {
            Text = topic,
            TextColor = Color.FromArgb("#1A2B4A"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
        };

        var hdr = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        hdr.Add(topicLbl, 0); hdr.Add(countLabel, 1);

        var border = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Color.FromArgb("#DDE3EC")),
            Padding = new Thickness(10, 8),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Margin = new Thickness(0, 2),
            Content = new StackLayout { Spacing = 3, Children = { hdr, valLabel, tsLabel } }
        };
        return border;
    }

    // ══════════════════════════════════════
    //  STATS + THROUGHPUT CHART
    // ══════════════════════════════════════

    private void UpdateStats()
    {
        var rxNow = _vm.MessaggiRx;
        var delta = rxNow - _lastRx;
        _lastRx = rxNow;

        _throughputHistory.RemoveAt(0);
        _throughputHistory.Add(delta);
        NetworkGraph.Invalidate();

        LblMsgsPerSec.Text = $"{delta} msg/s";

        if (_connectedAt != DateTime.MinValue)
        {
            var up = DateTime.Now - _connectedAt;
            LblUptime.Text = $"{(int)up.TotalMinutes:D2}:{up.Seconds:D2}";
        }
    }

    private void UpdateConnectionUI(bool connected)
    {
        var green = Color.FromArgb("#00A86B");
        var red = Color.FromArgb("#D93025");
        DotConnesso.Color = connected ? green : red;
        LblStato.Text = connected ? "CONNESSO" : "DISCONNESSO";
        LblStato.TextColor = connected ? green : red;
        BtnConnect.IsEnabled = !connected;
        BtnDisconnect.IsEnabled = connected;
    }

    // ══════════════════════════════════════
    //  MQTT CALLBACKS
    // ══════════════════════════════════════

    private void OnConnectionChanged(bool connected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateConnectionUI(connected);
            if (connected) _connectedAt = DateTime.Now;
            AddLog(connected ? $"✓ Connesso a {_vm.BrokerHost}" : "✗ Disconnesso");
        });
    }

    private void OnMessageReceived(string topic, string payload)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_topicCards.TryGetValue(topic, out var card))
            {
                card.valLabel.Text = payload.Length > 80 ? payload[..80] + "…" : payload;
                card.tsLabel.Text = DateTime.Now.ToString("HH:mm:ss.fff");
                var n = int.TryParse(card.countLabel.Text?.Trim('(', ')'), out int x) ? x + 1 : 1;
                card.countLabel.Text = $"({n})";
            }
            AddLog($"[{DateTime.Now:HH:mm:ss.fff}] {topic}  {(payload.Length > 80 ? payload[..80] + "…" : payload)}");
        });
    }

    private void AddLog(string line)
    {
        _logLines.Add(line);
        if (_logLines.Count > 300) _logLines.RemoveAt(0);
        if (_logLines.Count > 0) LogView.ScrollTo(_logLines.Count - 1);
    }

    // ══════════════════════════════════════
    //  BUTTON HANDLERS
    // ══════════════════════════════════════

    private async void OnConnectClicked(object? s, EventArgs e) => await _vm.ConnectAsync();
    private async void OnDisconnectClicked(object? s, EventArgs e) => await _vm.DisconnectAsync();

    private async void OnPublishClicked(object? s, EventArgs e)
    {
        var topic = EntryTopic.Text?.Trim();
        var payload = EntryPayload.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(topic)) { await DisplayAlertAsync("Errore", "Inserisci un topic.", "OK"); return; }
        await _vm.Mqtt.PublishAsync(topic, payload, SwitchRetain.IsToggled);
        _vm.MessaggiTx++;
        AddLog($"[TX] {topic} → {payload}");
    }

    private void OnClearLogClicked(object? s, EventArgs e) => _logLines.Clear();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Mqtt.MessageReceived += OnMessageReceived;
        _vm.Mqtt.ConnectionChanged += OnConnectionChanged;
        UpdateConnectionUI(_vm.MqttConnected);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Mqtt.MessageReceived -= OnMessageReceived;
        _vm.Mqtt.ConnectionChanged -= OnConnectionChanged;
    }
}

// ══════════════════════════════════════
//  THROUGHPUT GRAPH DRAWABLE
// ══════════════════════════════════════

public class ThroughputDrawable : IDrawable
{
    private readonly List<float> _data;
    public ThroughputDrawable(List<float> data) => _data = data;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_data.Count == 0) return;

        var w = dirtyRect.Width;
        var h = dirtyRect.Height;
        var max = _data.Max();
        if (max < 1) max = 1;

        // Background grid lines
        canvas.StrokeColor = Color.FromArgb("#DDE3EC");
        canvas.StrokeSize = 0.5f;
        for (int i = 1; i <= 4; i++)
        {
            float y = h - (h * i / 4f);
            canvas.DrawLine(0, y, w, y);
        }

        // Fill area
        var path = new PathF();
        path.MoveTo(0, h);
        for (int i = 0; i < _data.Count; i++)
        {
            float x = w * i / (_data.Count - 1);
            float y = h - (h * _data[i] / max);
            if (i == 0) path.LineTo(x, y);
            else path.LineTo(x, y);
        }
        path.LineTo(w, h);
        path.Close();

        canvas.FillColor = Color.FromArgb("#1A0078D4");
        canvas.FillPath(path);

        // Line
        canvas.StrokeColor = Color.FromArgb("#0078D4");
        canvas.StrokeSize = 2;
        var linePath = new PathF();
        for (int i = 0; i < _data.Count; i++)
        {
            float x = w * i / (_data.Count - 1);
            float y = h - (h * _data[i] / max);
            if (i == 0) linePath.MoveTo(x, y);
            else linePath.LineTo(x, y);
        }
        canvas.DrawPath(linePath);
    }
}