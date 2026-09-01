using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LagmaIpp.Models;
using LagmaIpp.Services;

namespace LagmaIpp.ViewModels;

/// <summary>
/// ViewModel centrale — collega MqttService e RobotState alla UI.
/// Implementa INotifyPropertyChanged per il binding MAUI.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    // ════════════════════════════════════════════════════════════════
    //  DIPENDENZE
    // ════════════════════════════════════════════════════════════════

    public readonly MqttService Mqtt = new();
    public readonly RobotState Robot = new();

    // ════════════════════════════════════════════════════════════════
    //  INPC
    // ════════════════════════════════════════════════════════════════

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
    }

    // ════════════════════════════════════════════════════════════════
    //  PROPRIETÀ BINDABILI — Connessione
    // ════════════════════════════════════════════════════════════════

    private string _brokerHost = "100.100.61.49";
    public string BrokerHost
    {
        get => _brokerHost;
        set => Set(ref _brokerHost, value);
    }

    private int _brokerPort = 1883;
    public int BrokerPort
    {
        get => _brokerPort;
        set => Set(ref _brokerPort, value);
    }

    private bool _mqttConnected;
    public bool MqttConnected
    {
        get => _mqttConnected;
        set
        {
            Set(ref _mqttConnected, value);
            OnPropertyChanged(nameof(ConnessioneLabel));
            OnPropertyChanged(nameof(ConnessioneColore));
        }
    }

    public string ConnessioneLabel => MqttConnected ? "CONNESSO" : "DISCONNESSO";
    public string ConnessioneColore => MqttConnected ? "#00C853" : "#D50000";

    private int _messaggiRx;
    public int MessaggiRx { get => _messaggiRx; set => Set(ref _messaggiRx, value); }

    private int _messaggiTx;
    public int MessaggiTx { get => _messaggiTx; set => Set(ref _messaggiTx, value); }

    // ════════════════════════════════════════════════════════════════
    //  PROPRIETÀ BINDABILI — ESP32 Motori (0x08)
    // ════════════════════════════════════════════════════════════════

    private bool _motoriOnline;
    private bool _motoriI2cOk;
    private bool _motoriMqttOk;
    private bool _motoriWifiOk;
    private string _motoriIp = "---";
    private string _motoriSsid = "---";
    private string _motoriFirmware = "---";
    private int _motoriErroriI2c;
    private int _motoriErroriMqtt;

    public bool MotoriOnline { get => _motoriOnline; set => Set(ref _motoriOnline, value); }
    public bool MotoriI2cOk { get => _motoriI2cOk; set => Set(ref _motoriI2cOk, value); }
    public bool MotoriMqttOk { get => _motoriMqttOk; set => Set(ref _motoriMqttOk, value); }
    public bool MotoriWifiOk { get => _motoriWifiOk; set => Set(ref _motoriWifiOk, value); }
    public string MotoriIp { get => _motoriIp; set => Set(ref _motoriIp, value); }
    public string MotoriSsid { get => _motoriSsid; set => Set(ref _motoriSsid, value); }
    public string MotoriFirmware { get => _motoriFirmware; set => Set(ref _motoriFirmware, value); }
    public int MotoriErroriI2c { get => _motoriErroriI2c; set => Set(ref _motoriErroriI2c, value); }
    public int MotoriErroriMqtt { get => _motoriErroriMqtt; set => Set(ref _motoriErroriMqtt, value); }

    // ── Stato motori ─────────────────────────────────────────────

    private string _statoMotoriLabel = "STOP";
    public string StatoMotoriLabel { get => _statoMotoriLabel; set => Set(ref _statoMotoriLabel, value); }

    private int _velocitaGlobale = 150;
    public int VelocitaGlobale
    {
        get => _velocitaGlobale;
        set
        {
            Set(ref _velocitaGlobale, value);
            _ = Mqtt.CmdVelocita(value);
            MessaggiTx++;
        }
    }

    private long _encFL, _encFR, _encRL, _encRR;
    public long EncFL { get => _encFL; set => Set(ref _encFL, value); }
    public long EncFR { get => _encFR; set => Set(ref _encFR, value); }
    public long EncRL { get => _encRL; set => Set(ref _encRL, value); }
    public long EncRR { get => _encRR; set => Set(ref _encRR, value); }

    private int _velFL, _velFR, _velRL, _velRR;
    public int VelFL { get => _velFL; set => Set(ref _velFL, value); }
    public int VelFR { get => _velFR; set => Set(ref _velFR, value); }
    public int VelRL { get => _velRL; set => Set(ref _velRL, value); }
    public int VelRR { get => _velRR; set => Set(ref _velRR, value); }

    // ════════════════════════════════════════════════════════════════
    //  PROPRIETÀ BINDABILI — ESP32 Sensori (0x09)
    // ════════════════════════════════════════════════════════════════

    private bool _sensoriOnline;
    private bool _sensoriI2cOk;
    private bool _sensoriMqttOk;
    private bool _sensoriWifiOk;
    private string _sensoriIp = "---";
    private string _sensoriSsid = "---";
    private string _sensoriFirmware = "---";
    private int _sensoriErroriI2c;
    private int _sensoriErroriMqtt;
    private bool _mpuOk;
    private bool _pca9685Ok;

    public bool SensoriOnline { get => _sensoriOnline; set => Set(ref _sensoriOnline, value); }
    public bool SensoriI2cOk { get => _sensoriI2cOk; set => Set(ref _sensoriI2cOk, value); }
    public bool SensoriMqttOk { get => _sensoriMqttOk; set => Set(ref _sensoriMqttOk, value); }
    public bool SensoriWifiOk { get => _sensoriWifiOk; set => Set(ref _sensoriWifiOk, value); }
    public string SensoriIp { get => _sensoriIp; set => Set(ref _sensoriIp, value); }
    public string SensoriSsid { get => _sensoriSsid; set => Set(ref _sensoriSsid, value); }
    public string SensoriFirmware { get => _sensoriFirmware; set => Set(ref _sensoriFirmware, value); }
    public int SensoriErroriI2c { get => _sensoriErroriI2c; set => Set(ref _sensoriErroriI2c, value); }
    public int SensoriErroriMqtt { get => _sensoriErroriMqtt; set => Set(ref _sensoriErroriMqtt, value); }
    public bool MpuOk { get => _mpuOk; set => Set(ref _mpuOk, value); }
    public bool Pca9685Ok { get => _pca9685Ok; set => Set(ref _pca9685Ok, value); }

    // ── Distanze ultrasuoni ──────────────────────────────────────

    private string _distFronte = "---";
    private string _distRetro = "---";
    private string _distSinistra = "---";
    private string _distDestra = "---";
    private string _distCliffF = "---";
    private string _distCliffR = "---";

    public string DistFronte { get => _distFronte; set => Set(ref _distFronte, value); }
    public string DistRetro { get => _distRetro; set => Set(ref _distRetro, value); }
    public string DistSinistra { get => _distSinistra; set => Set(ref _distSinistra, value); }
    public string DistDestra { get => _distDestra; set => Set(ref _distDestra, value); }
    public string DistCliffF { get => _distCliffF; set => Set(ref _distCliffF, value); }
    public string DistCliffR { get => _distCliffR; set => Set(ref _distCliffR, value); }

    // ── IMU ──────────────────────────────────────────────────────

    private double _accX, _accY, _accZ;
    private double _gyrX, _gyrY, _gyrZ;

    public double AccX { get => _accX; set => Set(ref _accX, value); }
    public double AccY { get => _accY; set => Set(ref _accY, value); }
    public double AccZ { get => _accZ; set => Set(ref _accZ, value); }
    public double GyrX { get => _gyrX; set => Set(ref _gyrX, value); }
    public double GyrY { get => _gyrY; set => Set(ref _gyrY, value); }
    public double GyrZ { get => _gyrZ; set => Set(ref _gyrZ, value); }

    // ── TCRT ─────────────────────────────────────────────────────

    private bool _tcrtSx, _tcrtCen, _tcrtDx;
    public bool TcrtSx { get => _tcrtSx; set => Set(ref _tcrtSx, value); }
    public bool TcrtCen { get => _tcrtCen; set => Set(ref _tcrtCen, value); }
    public bool TcrtDx { get => _tcrtDx; set => Set(ref _tcrtDx, value); }

    // ════════════════════════════════════════════════════════════════
    //  PROPRIETÀ BINDABILI — Braccio servo
    // ════════════════════════════════════════════════════════════════

    private int _s0 = 90, _s1 = 90, _s2 = 90, _s3 = 90, _s4 = 90, _s5 = 120;

    public int Servo0 { get => _s0; set { Set(ref _s0, value); _ = Mqtt.CmdServo(0, value); MessaggiTx++; } }
    public int Servo1 { get => _s1; set { Set(ref _s1, value); _ = Mqtt.CmdServo(1, value); MessaggiTx++; } }
    public int Servo2 { get => _s2; set { Set(ref _s2, value); _ = Mqtt.CmdServo(2, value); MessaggiTx++; } }
    public int Servo3 { get => _s3; set { Set(ref _s3, value); _ = Mqtt.CmdServo(3, value); MessaggiTx++; } }
    public int Servo4 { get => _s4; set { Set(ref _s4, value); _ = Mqtt.CmdServo(4, value); MessaggiTx++; } }
    public int Servo5 { get => _s5; set { Set(ref _s5, value); _ = Mqtt.CmdServo(5, value); MessaggiTx++; } }

    // ── Servo telecamera (canale 6, 80°–160°) ────────────────────
    private int _s6 = 120;
    public int ServoCamera
    {
        get => _s6;
        set
        {
            value = Math.Clamp(value, 80, 160);
            Set(ref _s6, value);
            _ = Mqtt.CmdServo(6, value);
            MessaggiTx++;
        }
    }

    private bool _releOn;
    public bool ReleOn
    {
        get => _releOn;
        set { Set(ref _releOn, value); _ = Mqtt.CmdRele(value); MessaggiTx++; }
    }

    // ════════════════════════════════════════════════════════════════
    //  PROPRIETÀ BINDABILI — Drone
    // ════════════════════════════════════════════════════════════════

    private bool _droneOnline;
    private string _droneStatus = "---";
    private int _droneThrust;

    public bool DroneOnline { get => _droneOnline; set => Set(ref _droneOnline, value); }
    public string DroneStatus { get => _droneStatus; set => Set(ref _droneStatus, value); }
    public int DroneThrust { get => _droneThrust; set => Set(ref _droneThrust, value); }

    // ════════════════════════════════════════════════════════════════
    //  PROPRIETÀ BINDABILI — Volume Audio (Raspberry Pi)
    // ════════════════════════════════════════════════════════════════

    private int _volume = 70;
    public int Volume
    {
        get => _volume;
        set
        {
            Set(ref _volume, value);
            _ = Mqtt.CmdAudioVolume(value);
            MessaggiTx++;
        }
    }

    //  NUOVE PROPRIETÀ E EVENTI SEPARATI
    public string? LastAudioMusicListJson { get; private set; }
    public string? LastAudioSfxListJson { get; private set; }
    public string? LastVideoListJson { get; private set; }

    // Eventi di notifica separati per l'interfaccia grafica
    public event Action<string>? AudioMusicListUpdated;
    public event Action<string>? AudioSfxListUpdated;
    public event Action<string>? VideoListUpdated;

    // ════════════════════════════════════════════════════════════════
    //  PROPRIETÀ BINDABILI — Modalità autonome
    // ════════════════════════════════════════════════════════════════

    private bool _modalitaPerlustrazione;
    public bool ModalitaPerlustrazione
    {
        get => _modalitaPerlustrazione;
        set { Set(ref _modalitaPerlustrazione, value); _ = Mqtt.CmdModalita("perlustrazione", value); MessaggiTx++; }
    }

    private bool _modalitaInseguiLinea;
    public bool ModalitaInseguiLinea
    {
        get => _modalitaInseguiLinea;
        set { Set(ref _modalitaInseguiLinea, value); _ = Mqtt.CmdModalita("insegui_linea", value); MessaggiTx++; }
    }

    // ════════════════════════════════════════════════════════════════
    //  PROPRIETÀ BINDABILI — Raspberry Pi
    // ════════════════════════════════════════════════════════════════

    private bool _piOnline;
    private double _piTempCpu;
    private double _piCpuPercent;
    private int _piRamUsata;
    private int _piRamTotale;
    private double _piDiskUsato;
    private double _piDiskTotale;
    private string _piIp = "100.100.61.49";
    private bool _piMosquittoOk;
    private bool _piTailscaleOk;
    private bool _piMjpgOk;
    private bool _piOllamaOk;

    public bool PiOnline { get => _piOnline; set => Set(ref _piOnline, value); }
    public double PiTempCpu { get => _piTempCpu; set => Set(ref _piTempCpu, value); }
    public double PiCpuPercent { get => _piCpuPercent; set => Set(ref _piCpuPercent, value); }
    public int PiRamUsata { get => _piRamUsata; set => Set(ref _piRamUsata, value); }
    public int PiRamTotale { get => _piRamTotale; set => Set(ref _piRamTotale, value); }
    public double PiDiskUsato { get => _piDiskUsato; set => Set(ref _piDiskUsato, value); }
    public double PiDiskTotale { get => _piDiskTotale; set => Set(ref _piDiskTotale, value); }
    public string PiIp { get => _piIp; set => Set(ref _piIp, value); }
    public bool PiMosquittoOk { get => _piMosquittoOk; set => Set(ref _piMosquittoOk, value); }
    public bool PiTailscaleOk { get => _piTailscaleOk; set => Set(ref _piTailscaleOk, value); }
    public bool PiMjpgOk { get => _piMjpgOk; set => Set(ref _piMjpgOk, value); }
    public bool PiOllamaOk { get => _piOllamaOk; set => Set(ref _piOllamaOk, value); }

    public string PiTempStr => _piOnline ? $"{_piTempCpu:F1} °C" : "---";
    public string PiCpuStr => _piOnline ? $"{_piCpuPercent:F0}%" : "---";
    public string PiRamStr => _piOnline ? $"{_piRamUsata} / {_piRamTotale} MB" : "---";
    public string PiDiskStr => _piOnline ? $"{_piDiskUsato:F1} / {_piDiskTotale:F1} GB" : "---";

    // ════════════════════════════════════════════════════════════════
    //  LOG
    // ════════════════════════════════════════════════════════════════

    public ObservableCollection<string> LogLines { get; } = new();

    private void AddLog(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        LogLines.Add(line);
        if (LogLines.Count > 200)
            LogLines.RemoveAt(0);
    }

    // ════════════════════════════════════════════════════════════════
    //  INIT
    // ════════════════════════════════════════════════════════════════

    public MainViewModel()
    {
        Mqtt.MessageReceived += OnMqttMessage;
        Mqtt.ConnectionChanged += OnConnectionChanged;

        var timer = new System.Timers.Timer(2000);
        timer.Elapsed += (_, _) =>
        {
            try { MainThread.BeginInvokeOnMainThread(CheckTimeouts); }
            catch { /* thread UI non ancora pronto, ignora */ }
        };
        timer.Start();
    }

    public async Task ConnectAsync()
    {
        AddLog($"Connessione a {BrokerHost}:{BrokerPort}...");
        await Mqtt.ConnectAsync(BrokerHost, BrokerPort);

        // Timer timeout — avviato solo dopo la connessione
        var timer = new System.Timers.Timer(2000);
        timer.Elapsed += (_, _) =>
        {
            if (MainThread.IsMainThread)
                CheckTimeouts();
            else
                MainThread.BeginInvokeOnMainThread(CheckTimeouts);
        };
        timer.Start();
    }

    public async Task DisconnectAsync()
    {
        await Mqtt.DisconnectAsync();
        AddLog("Disconnesso.");
    }

    // ════════════════════════════════════════════════════════════════
    //  COMANDI MOTORI
    // ════════════════════════════════════════════════════════════════

    public async Task Stop() { await Mqtt.CmdStop(); MessaggiTx++; StatoMotoriLabel = "STOP"; }
    public async Task Avanti() { await Mqtt.CmdAvanti(); MessaggiTx++; StatoMotoriLabel = "AVANTI"; }
    public async Task Indietro() { await Mqtt.CmdIndietro(); MessaggiTx++; StatoMotoriLabel = "INDIETRO"; }
    public async Task Sinistra() { await Mqtt.CmdSinistra(); MessaggiTx++; StatoMotoriLabel = "SINISTRA"; }
    public async Task Destra() { await Mqtt.CmdDestra(); MessaggiTx++; StatoMotoriLabel = "DESTRA"; }
    public async Task RotaSx() { await Mqtt.CmdRotaSx(); MessaggiTx++; StatoMotoriLabel = "RUOTA SX"; }
    public async Task RotaDx() { await Mqtt.CmdRotaDx(); MessaggiTx++; StatoMotoriLabel = "RUOTA DX"; }

    public async Task Mecanum(int vx, int vy, int vr)
    {
        await Mqtt.CmdMecanum(vx, vy, vr);
        MessaggiTx++;
    }

    // ════════════════════════════════════════════════════════════════
    //  COMANDI DRONE
    // ════════════════════════════════════════════════════════════════

    public async Task DroneRpyt(int roll, int pitch, int yaw, int thrust)
    {
        await Mqtt.CmdDroneRpyt(roll, pitch, yaw, thrust);
        MessaggiTx++;
    }

    public async Task DroneLand() { await Mqtt.CmdDroneLand(); MessaggiTx++; }
    public async Task DroneStop() { await Mqtt.CmdDroneStop(); MessaggiTx++; }

    // ════════════════════════════════════════════════════════════════
    //  COMANDI PI
    // ════════════════════════════════════════════════════════════════

    public async Task Emozione(string tipo)
    {
        await Mqtt.CmdEmozione(tipo);
        MessaggiTx++;
        AddLog($"Emozione: {tipo}");
    }

    public async Task Media(string tipo, string? path = null)
    {
        await Mqtt.CmdMedia(tipo, path);
        MessaggiTx++;
        AddLog($"Media: {tipo} {path}");
    }

    public async Task AiInput(string testo)
    {
        await Mqtt.CmdAiInput(testo);
        MessaggiTx++;
        AddLog($"AI input: {testo}");
    }

    // ════════════════════════════════════════════════════════════════
    //  PARSING MESSAGGI MQTT
    // ════════════════════════════════════════════════════════════════

    private void OnConnectionChanged(bool connected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MqttConnected = connected;
            AddLog(connected ? $"MQTT connesso a {BrokerHost}" : "MQTT disconnesso");
        });
    }

    private void OnMqttMessage(string topic, string payload)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MessaggiRx++;
            try
            {
                switch (topic)
                {
                    case MqttService.TOPIC_MOTORI_STATO: ParseMotoriStato(payload); break;
                    case MqttService.TOPIC_SENSORI_DISTANZE: ParseDistanze(payload); break;
                    case MqttService.TOPIC_SENSORI_IMU: ParseImu(payload); break;
                    case MqttService.TOPIC_SENSORI_TCRT: ParseTcrt(payload); break;
                    case MqttService.TOPIC_PI_STATO: ParsePiStato(payload); break;
                    case MqttService.TOPIC_MOTORI_LOG: AddLog($"[MOTORI] {payload}"); break;
                    case MqttService.TOPIC_SENSORI_LOG: AddLog($"[SENSORI] {payload}"); break;
                    //  NUOVI CASI SEPARATI NEL METODO OnMqttMessage
                    case MqttService.TOPIC_AUDIO_LIST: // Gestisce la Musica (sounds/list)
                        LastAudioMusicListJson = payload;
                        AudioMusicListUpdated?.Invoke(payload);
                        break;

                    case MqttService.TOPIC_SFX_LIST: // Gestisce gli Effetti Sonori (sfx/list)
                        LastAudioSfxListJson = payload;
                        AudioSfxListUpdated?.Invoke(payload);
                        break;
                    case MqttService.TOPIC_VIDEO_LIST:
                        LastVideoListJson = payload;
                        VideoListUpdated?.Invoke(payload);
                        break;
                    default:
                        if (topic.StartsWith("drone/status"))
                            ParseDroneStatus(topic, payload);
                        break;
                }
            }
            catch (Exception ex) { AddLog($"Errore parsing {topic}: {ex.Message}"); }
        });
    }

    // ── Parser singoli ───────────────────────────────────────────

    private void ParseMotoriStato(string json)
    {
        // {"online":true,"fl":0,"fr":0,"rl":0,"rr":0,
        //  "vel":150,"stato":0,"vfl":0,"vfr":0,"vrl":0,"vrr":0,
        //  "wifi":true,"mqtt":true,"ip":"
        //  .43.x","ssid":"LAPTOP1234",
        //  "i2c_ok":true,"fw":"1.0"}
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        MotoriOnline = root.GetBoolOrDefault("online", true);
        MotoriMqttOk = root.GetBoolOrDefault("mqtt", false);
        MotoriWifiOk = root.GetBoolOrDefault("wifi", false);
        MotoriI2cOk = root.GetBoolOrDefault("i2c_ok", false);
        MotoriIp = root.GetStringOrDefault("ip", "---");
        MotoriSsid = root.GetStringOrDefault("ssid", "---");
        MotoriFirmware = root.GetStringOrDefault("fw", "---");
        MotoriErroriI2c = root.GetIntOrDefault("i2c_err", 0);
        MotoriErroriMqtt = root.GetIntOrDefault("mqtt_err", 0);

        Robot.MotoriUltimoTs = DateTime.Now;

        EncFL = root.GetLongOrDefault("fl", 0);
        EncFR = root.GetLongOrDefault("fr", 0);
        EncRL = root.GetLongOrDefault("rl", 0);
        EncRR = root.GetLongOrDefault("rr", 0);

        VelocitaGlobale = root.GetIntOrDefault("vel", 150);
        StatoMotoriLabel = root.GetIntOrDefault("stato", 0) switch
        {
            1 => "IN MOTO",
            2 => "ROTAZIONE",
            _ => "STOP"
        };

        VelFL = root.GetIntOrDefault("vfl", 0);
        VelFR = root.GetIntOrDefault("vfr", 0);
        VelRL = root.GetIntOrDefault("vrl", 0);
        VelRR = root.GetIntOrDefault("vrr", 0);
    }

    private void ParseDistanze(string json)
    {
        // {"fr":120,"re":9999,"sx":45,"dx":200,"clf":9999,"clr":9999,
        //  "wifi":true,"mqtt":true,"ip":"...","ssid":"...","i2c_ok":true}
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        SensoriOnline = true;
        SensoriMqttOk = root.GetBoolOrDefault("mqtt", false);
        SensoriWifiOk = root.GetBoolOrDefault("wifi", false);
        SensoriI2cOk = root.GetBoolOrDefault("i2c_ok", false);
        SensoriIp = root.GetStringOrDefault("ip", "---");
        SensoriSsid = root.GetStringOrDefault("ssid", "---");
        SensoriFirmware = root.GetStringOrDefault("fw", "---");

        Robot.SensoriUltimoTs = DateTime.Now;

        DistFronte = FormatDist(root.GetIntOrDefault("fr", 9999));
        DistRetro = FormatDist(root.GetIntOrDefault("re", 9999));
        DistSinistra = FormatDist(root.GetIntOrDefault("sx", 9999));
        DistDestra = FormatDist(root.GetIntOrDefault("dx", 9999));
        DistCliffF = FormatDist(root.GetIntOrDefault("clf", 9999));
        DistCliffR = FormatDist(root.GetIntOrDefault("clr", 9999));
    }

    private void ParseImu(string json)
    {
        // {"ax":0.01,"ay":-0.02,"az":1.00,"gx":0.1,"gy":0.0,"gz":0.0,"mpu_ok":true}
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        MpuOk = root.GetBoolOrDefault("mpu_ok", true);
        AccX = root.GetDoubleOrDefault("ax", 0);
        AccY = root.GetDoubleOrDefault("ay", 0);
        AccZ = root.GetDoubleOrDefault("az", 0);
        GyrX = root.GetDoubleOrDefault("gx", 0);
        GyrY = root.GetDoubleOrDefault("gy", 0);
        GyrZ = root.GetDoubleOrDefault("gz", 0);
    }

    private void ParseTcrt(string json)
    {
        // {"sx":0,"cen":0,"dx":1,"pca_ok":true}
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Pca9685Ok = root.GetBoolOrDefault("pca_ok", true);
        TcrtSx = root.GetIntOrDefault("sx", 0) == 1;
        TcrtCen = root.GetIntOrDefault("cen", 0) == 1;
        TcrtDx = root.GetIntOrDefault("dx", 0) == 1;
    }

    private void ParsePiStato(string json)
    {
        // {"temp":52.3,"cpu":18.5,"ram_used":612,"ram_total":3900,
        //  "disk_used":8.2,"disk_total":32.0,"ip":"100.100.61.49",
        //  "mosquitto":true,"tailscale":true,"mjpg":true,"ollama":false}
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        PiOnline = true;
        Robot.PiUltimoTs = DateTime.Now;

        PiTempCpu = root.GetDoubleOrDefault("temp", 0);
        PiCpuPercent = root.GetDoubleOrDefault("cpu", 0);
        PiRamUsata = root.GetIntOrDefault("ram_used", 0);
        PiRamTotale = root.GetIntOrDefault("ram_total", 0);
        PiDiskUsato = root.GetDoubleOrDefault("disk_used", 0);
        PiDiskTotale = root.GetDoubleOrDefault("disk_total", 0);
        PiIp = root.GetStringOrDefault("ip", "100.100.61.49");
        PiMosquittoOk = root.GetBoolOrDefault("mosquitto", false);
        PiTailscaleOk = root.GetBoolOrDefault("tailscale", false);
        PiMjpgOk = root.GetBoolOrDefault("mjpg", false);
        PiOllamaOk = root.GetBoolOrDefault("ollama", false);

        OnPropertyChanged(nameof(PiTempStr));
        OnPropertyChanged(nameof(PiCpuStr));
        OnPropertyChanged(nameof(PiRamStr));
        OnPropertyChanged(nameof(PiDiskStr));
    }

    private void ParseDroneStatus(string topic, string payload)
    {
        DroneOnline = true;
        DroneStatus = payload;
        AddLog($"[DRONE] {topic}: {payload}");
    }

    // ════════════════════════════════════════════════════════════════
    //  TIMEOUT CHECK
    // ════════════════════════════════════════════════════════════════

    private void CheckTimeouts()
    {
        var now = DateTime.Now;
        var timeout = TimeSpan.FromSeconds(5);

        if (MotoriOnline && (now - Robot.MotoriUltimoTs) > timeout)
        {
            MotoriOnline = false;
            MotoriMqttOk = false;
            VelFL = VelFR = VelRL = VelRR = 0;
            StatoMotoriLabel = "STOP";
            AddLog("WARN: ESP32 Motori timeout");
        }

        if (SensoriOnline && (now - Robot.SensoriUltimoTs) > timeout)
        {
            SensoriOnline = false;
            SensoriMqttOk = false;
            DistFronte = DistRetro = DistSinistra =
            DistDestra = DistCliffF = DistCliffR = "---";
            AddLog("WARN: ESP32 Sensori timeout");
        }

        if (DroneOnline && (now - Robot.PiUltimoTs) > timeout)
        {
            DroneOnline = false;
            AddLog("WARN: Drone timeout");
        }

        if (PiOnline && (now - Robot.PiUltimoTs) > timeout)
        {
            PiOnline = false;
            PiMosquittoOk = false;
            PiMjpgOk = false;
            OnPropertyChanged(nameof(PiTempStr));
            OnPropertyChanged(nameof(PiCpuStr));
            OnPropertyChanged(nameof(PiRamStr));
            OnPropertyChanged(nameof(PiDiskStr));
            AddLog("WARN: Pi timeout");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  UTILITY
    // ════════════════════════════════════════════════════════════════

    private static string FormatDist(int cm)
        => cm >= 9999 ? "---" : $"{cm} cm";
}

// ════════════════════════════════════════════════════════════════
//  ESTENSIONI JsonElement — parsing sicuro senza eccezioni
// ════════════════════════════════════════════════════════════════

internal static class JsonElementExtensions
{
    public static int GetIntOrDefault(this JsonElement el, string key, int def = 0)
        => el.TryGetProperty(key, out var v) && v.TryGetInt32(out var i) ? i : def;

    public static long GetLongOrDefault(this JsonElement el, string key, long def = 0)
        => el.TryGetProperty(key, out var v) && v.TryGetInt64(out var l) ? l : def;

    public static double GetDoubleOrDefault(this JsonElement el, string key, double def = 0)
        => el.TryGetProperty(key, out var v) && v.TryGetDouble(out var d) ? d : def;

    public static bool GetBoolOrDefault(this JsonElement el, string key, bool def = false)
        => el.TryGetProperty(key, out var v)
            ? v.ValueKind == JsonValueKind.True
            : def;

    public static string GetStringOrDefault(this JsonElement el, string key, string def = "")
        => el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? def : def;
}