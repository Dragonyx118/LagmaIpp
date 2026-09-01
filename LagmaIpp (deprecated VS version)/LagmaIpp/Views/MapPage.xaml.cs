using LagmaIpp.ViewModels;
using Microsoft.Maui.Graphics;
using System.Text.Json;

namespace LagmaIpp.Views;

// ── Dati GPS ─────────────────────────────────────────────────────
public class GpsData
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double Alt { get; set; }
    public double Speed { get; set; }
    public int Sat { get; set; }
    public double Ts { get; set; }
}

// ── Dati occupancy grid ──────────────────────────────────────────
public class GridData
{
    public int Size { get; set; }
    public double CellM { get; set; }
    public double GridM { get; set; }
    public List<int> RobotCell { get; set; } = new();
    public double RobotTheta { get; set; }
    public List<List<int>> Occupied { get; set; } = new();
    public List<List<int>> Free { get; set; } = new();
    public Dictionary<string, bool> Cliff { get; set; } = new();
    public Dictionary<string, double> Sensors { get; set; } = new();
}

// ── Drawable occupancy grid ──────────────────────────────────────
public class OccupancyGridDrawable : IDrawable
{
    public GridData? Data { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromArgb("#1A1A2E");
        canvas.FillRectangle(dirtyRect);

        if (Data == null || Data.Size == 0) return;

        var size = Data.Size;
        var cellPx = Math.Min(dirtyRect.Width, dirtyRect.Height) / size;
        var offsetX = (dirtyRect.Width - cellPx * size) / 2f;
        var offsetY = (dirtyRect.Height - cellPx * size) / 2f;

        // Celle libere — verde scuro
        canvas.FillColor = Color.FromArgb("#1B5E20");
        foreach (var cell in Data.Free)
        {
            if (cell.Count < 2) continue;
            canvas.FillRectangle(
                offsetX + cell[0] * cellPx,
                offsetY + cell[1] * cellPx,
                cellPx, cellPx);
        }

        // Celle occupate — rosso
        canvas.FillColor = Color.FromArgb("#B71C1C");
        foreach (var cell in Data.Occupied)
        {
            if (cell.Count < 2) continue;
            canvas.FillRectangle(
                offsetX + cell[0] * cellPx,
                offsetY + cell[1] * cellPx,
                cellPx, cellPx);
        }

        // Griglia leggera
        canvas.StrokeColor = Color.FromArgb("#33FFFFFF");
        canvas.StrokeSize = 0.3f;
        for (int i = 0; i <= size; i += 10)
        {
            canvas.DrawLine(offsetX + i * cellPx, offsetY,
                            offsetX + i * cellPx, offsetY + size * cellPx);
            canvas.DrawLine(offsetX, offsetY + i * cellPx,
                            offsetX + size * cellPx, offsetY + i * cellPx);
        }

        // Robot — cerchio ciano con freccia direzione
        if (Data.RobotCell.Count >= 2)
        {
            var rx = offsetX + Data.RobotCell[0] * cellPx;
            var ry = offsetY + Data.RobotCell[1] * cellPx;
            var r = (float)Math.Max(cellPx * 3, 6);

            canvas.FillColor = Color.FromArgb("#00BCD4");
            canvas.StrokeColor = Color.FromArgb("#FFFFFF");
            canvas.StrokeSize = 1.5f;
            canvas.FillCircle(rx, ry, r);
            canvas.DrawCircle(rx, ry, r);

            // Freccia direzione theta
            var theta = Data.RobotTheta;
            var ex = rx + (float)(Math.Cos(theta) * r * 1.8);
            var ey = ry - (float)(Math.Sin(theta) * r * 1.8);
            canvas.StrokeColor = Color.FromArgb("#FFFFFF");
            canvas.StrokeSize = 2f;
            canvas.DrawLine(rx, ry, ex, ey);
        }

        // Origine (0,0)
        var origin = WorldToPixel(0, 0, size, cellPx, offsetX, offsetY);
        canvas.FillColor = Color.FromArgb("#FFD600");
        canvas.FillCircle(origin.X, origin.Y, 4);
    }

    private static PointF WorldToPixel(double wx, double wy,
        int size, double cellPx, double offsetX, double offsetY)
    {
        var cx = (wx + size * cellPx / 2) / cellPx;
        var cy = (wy + size * cellPx / 2) / cellPx;
        return new PointF(
            (float)(offsetX + cx * cellPx),
            (float)(offsetY + cy * cellPx));
    }
}

// ════════════════════════════════════════════════════════════════
//  MapPage
// ════════════════════════════════════════════════════════════════

public partial class MapPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly OccupancyGridDrawable _gridDrawable = new();

#pragma warning disable CS0414 // assigned but value never read — reserved for future use
    private bool _showingGps = true;
#pragma warning restore CS0414
    private GridData? _lastGrid;

    // HTML Leaflet per GPS map
    private const string LeafletHtmlTemplate = @"
<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<style>
  body {{ margin:0; padding:0; background:#1a1a2e; }}
  #map {{ width:100vw; height:100vh; }}
</style>
</head>
<body>
<div id='map'></div>
<script>
  var map = L.map('map').setView([{LAT}, {LON}], 18);
  L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
    maxZoom: 19,
    attribution: '© OpenStreetMap'
  }}).addTo(map);

  var robotIcon = L.divIcon({{
    html: '<div style=""width:16px;height:16px;background:#00BCD4;border:2px solid white;border-radius:50%;""></div>',
    iconSize:[16,16], iconAnchor:[8,8]
  }});

  var marker = L.marker([{LAT}, {LON}], {{icon: robotIcon}}).addTo(map);
  var path   = L.polyline([[{LAT},{LON}]], {{color:'#00BCD4', weight:2}}).addTo(map);

  function updatePosition(lat, lon) {{
    marker.setLatLng([lat, lon]);
    path.addLatLng([lat, lon]);
    map.panTo([lat, lon]);
  }}
</script>
</body>
</html>";

    public MapPage()
    {
        InitializeComponent();

        _vm = IPlatformApplication.Current!.Services
            .GetRequiredService<MainViewModel>();
        BindingContext = _vm;

        GridCanvas.Drawable = _gridDrawable;

        _vm.Mqtt.MessageReceived += OnMqttMessage;

        LoadLeafletMap(45.4654, 9.1859); // default Milano, verrà aggiornato al primo fix GPS
    }

    // ════════════════════════════════════════════════════════════════
    //  LEAFLET MAP
    // ════════════════════════════════════════════════════════════════

    private void LoadLeafletMap(double lat, double lon)
    {
        var html = $@"<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<style>
  body {{ margin:0; padding:0; }}
  #map {{ width:100vw; height:100vh; }}
</style>
</head>
<body>
<div id='map'></div>
<script>
  var lat = {lat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)};
  var lon = {lon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)};
  var map = L.map('map').setView([lat, lon], 18);
  L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
    maxZoom: 19,
    attribution: '© OpenStreetMap'
  }}).addTo(map);
  var robotIcon = L.divIcon({{
    html: '<div style=""width:16px;height:16px;background:#00BCD4;border:3px solid white;border-radius:50%;box-shadow:0 0 6px #00BCD4;""></div>',
    iconSize:[16,16], iconAnchor:[8,8]
  }});
  var marker = L.marker([lat, lon], {{icon: robotIcon}}).addTo(map);
  var path = L.polyline([[lat, lon]], {{color:'#00BCD4', weight:3, opacity:0.8}}).addTo(map);
  function updatePosition(newLat, newLon) {{
    marker.setLatLng([newLat, newLon]);
    path.addLatLng([newLat, newLon]);
    map.panTo([newLat, newLon]);
  }}
</script>
</body>
</html>";

        GpsMapView.Source = new HtmlWebViewSource { Html = html };
    }

    private void UpdateMarkerOnMap(double lat, double lon)
    {
        // Chiama la funzione JS nella WebView per spostare il marker
        var js = $"updatePosition({lat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}, " +
                               $"{lon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)});";
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await GpsMapView.EvaluateJavaScriptAsync(js); }
            catch { /* WebView non ancora pronta */ }
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  MQTT PARSING
    // ════════════════════════════════════════════════════════════════

    private void OnMqttMessage(string topic, string payload)
    {
        switch (topic)
        {
            case "robot/gps":
                ParseGps(payload);
                break;

            case "robot/mappa/grid":
                ParseGrid(payload);
                break;

            case "robot/odometria":
                ParseOdometria(payload);
                break;

            case "robot/cliff/stato":
                ParseCliff(payload);
                break;
        }
    }

    private bool _mapInitialized = false;

    private void ParseGps(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var lat = root.GetDoubleOrDefault("lat", 0);
            var lon = root.GetDoubleOrDefault("lon", 0);
            var alt = root.GetDoubleOrDefault("alt", 0);
            var speed = root.GetDoubleOrDefault("speed", 0);
            var sat = root.GetIntOrDefault("sat", 0);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                GpsOfflineBadge.IsVisible = false;
                GpsLatLabel.Text = $"Lat: {lat:F6}°";
                GpsLonLabel.Text = $"Lon: {lon:F6}°";
                GpsAltLabel.Text = $"Alt: {alt:F1} m";
                GpsSatLabel.Text = $"Sat: {sat}";
                GpsSpeedLabel.Text = $"Vel: {speed:F2} kn";

                if (!_mapInitialized)
                {
                    _mapInitialized = true;
                    LoadLeafletMap(lat, lon);
                }
                else
                {
                    UpdateMarkerOnMap(lat, lon);
                }
            });
        }
        catch { }
    }

    private void ParseGrid(string json)
    {
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<GridData>(json, opts);
            if (data == null) return;

            _lastGrid = data;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _gridDrawable.Data = data;
                GridCanvas.Invalidate();

                // Aggiorna label sensori
                UpdateSensorLabels(data.Sensors);

                // Cliff warning
                var cliffActive = data.Cliff.GetValueOrDefault("cliff_f") ||
                                  data.Cliff.GetValueOrDefault("cliff_r");
                CliffWarning.IsVisible = cliffActive;
                SensCliff.Text = cliffActive ? "⚠ Cliff: PERICOLO!" : "⚠ Cliff: OK";
                SensCliff.TextColor = cliffActive
                    ? Color.FromArgb("#FFD600")
                    : Color.FromArgb("#00C853");
            });
        }
        catch { /* parsing fallito */ }
    }

    private void ParseOdometria(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var x = root.GetDoubleOrDefault("x", 0);
            var y = root.GetDoubleOrDefault("y", 0);
            var theta = root.GetDoubleOrDefault("theta", 0);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OdoX.Text = $"{x:F2} m";
                OdoY.Text = $"{y:F2} m";
                OdoTheta.Text = $"{theta * 180 / Math.PI:F1}°";
            });
        }
        catch { /* parsing fallito */ }
    }

    private void ParseCliff(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var cliffF = root.GetBoolOrDefault("cliff_f", false);
            var cliffR = root.GetBoolOrDefault("cliff_r", false);
            var cliffActive = cliffF || cliffR;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                CliffWarning.IsVisible = cliffActive;
                SensCliff.Text = cliffActive ? "⚠ Cliff: PERICOLO!" : "⚠ Cliff: OK";
                SensCliff.TextColor = cliffActive
                    ? Color.FromArgb("#FFD600")
                    : Color.FromArgb("#00C853");
            });
        }
        catch { /* parsing fallito */ }
    }

    private void UpdateSensorLabels(Dictionary<string, double> sensors)
    {
        SensFronte.Text = $"▲ Fronte:   {FormatSens(sensors, "FRONTE")}";
        SensRetro.Text = $"▼ Retro:    {FormatSens(sensors, "RETRO")}";
        SensSinistra.Text = $"◀ Sinistra: {FormatSens(sensors, "SINISTRA")}";
        SensDestra.Text = $"▶ Destra:   {FormatSens(sensors, "DESTRA")}";

        // Colore in base alla distanza
        SetSensorColor(SensFronte, sensors.GetValueOrDefault("FRONTE", 9999));
        SetSensorColor(SensRetro, sensors.GetValueOrDefault("RETRO", 9999));
        SetSensorColor(SensSinistra, sensors.GetValueOrDefault("SINISTRA", 9999));
        SetSensorColor(SensDestra, sensors.GetValueOrDefault("DESTRA", 9999));
    }

    private static string FormatSens(Dictionary<string, double> s, string key)
    {
        var v = s.GetValueOrDefault(key, 9999);
        return v >= 9999 ? "---" : $"{v:F0} cm";
    }

    private static void SetSensorColor(Label label, double cm)
    {
        label.TextColor = cm switch
        {
            >= 9999 => Color.FromArgb("#9E9E9E"),
            < 20 => Color.FromArgb("#FF1744"),
            < 50 => Color.FromArgb("#FFD600"),
            _ => Color.FromArgb("#00C853"),
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  TAB SWITCH
    // ════════════════════════════════════════════════════════════════

    private void OnTabGpsClicked(object sender, EventArgs e)
    {
        _showingGps = true;
        TabGps.IsVisible = true;
        TabGrid.IsVisible = false;
        BtnTabGps.BackgroundColor = Color.FromArgb("#1976D2");
        BtnTabGps.TextColor = Colors.White;
        BtnTabGrid.BackgroundColor = Color.FromArgb("#E0E0E0");
        BtnTabGrid.TextColor = Color.FromArgb("#000000");
    }

    private void OnTabGridClicked(object sender, EventArgs e)
    {
        _showingGps = false;
        TabGps.IsVisible = false;
        TabGrid.IsVisible = true;
        BtnTabGrid.BackgroundColor = Color.FromArgb("#1976D2");
        BtnTabGrid.TextColor = Colors.White;
        BtnTabGps.BackgroundColor = Color.FromArgb("#E0E0E0");
        BtnTabGps.TextColor = Color.FromArgb("#000000");

        // Richiedi mappa aggiornata al Pi
        _ = _vm.Mqtt.PublishAsync("robot/mappa/get", "1");
    }

    // ════════════════════════════════════════════════════════════════
    //  TOOLBAR
    // ════════════════════════════════════════════════════════════════

    private async void OnResetOdoClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync(
            "Reset odometria",
            "Azzera la posizione x/y/theta del robot?",
            "Sì", "No");

        if (confirm)
        {
            await _vm.Mqtt.PublishAsync("robot/odometria/reset", "1");
            OdoX.Text = "0.00 m";
            OdoY.Text = "0.00 m";
            OdoTheta.Text = "0.0°";
        }
    }

    private async void OnSaveMapClicked(object sender, EventArgs e)
    {
        if (_lastGrid == null)
        {
            await DisplayAlertAsync("Salva mappa", "Nessuna mappa disponibile.", "OK");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(_lastGrid);
            var path = Path.Combine(
                FileSystem.AppDataDirectory,
                $"mappa_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await File.WriteAllTextAsync(path, json);
            await DisplayAlertAsync("Salva mappa", $"Mappa salvata in:\n{path}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Errore", ex.Message, "OK");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════════════════

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Richiedi mappa subito
        _ = _vm.Mqtt.PublishAsync("robot/mappa/get", "1");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Mqtt.MessageReceived -= OnMqttMessage;
    }
}