namespace LagmaIpp.Models;

/// <summary>
/// Stato completo del robot aggiornato in tempo reale dai messaggi MQTT.
/// Tutti i campi sono plain properties — il ViewModel si occupa della notifica UI.
/// </summary>
public class RobotState
{
    // ════════════════════════════════════════════════════════════════
    //  CONNESSIONE MQTT
    // ════════════════════════════════════════════════════════════════

    public bool MqttConnected { get; set; } = false;
    public string BrokerHost { get; set; } = "100.100.61.49";
    public int BrokerPort { get; set; } = 1883;
    public int MessaggiRx { get; set; } = 0;
    public int MessaggiTx { get; set; } = 0;

    // ════════════════════════════════════════════════════════════════
    //  ESP32 MOTORI (0x08) — stato connessione
    // ════════════════════════════════════════════════════════════════

    /// <summary>ESP32 Motori raggiungibile via MQTT</summary>
    public bool MotoriOnline { get; set; } = false;

    /// <summary>Bus I2C tra Pi e ESP32 Motori funzionante</summary>
    public bool MotoriI2cOk { get; set; } = false;

    /// <summary>ESP32 Motori connesso al broker MQTT</summary>
    public bool MotoriMqttOk { get; set; } = false;

    /// <summary>ESP32 Motori connesso al WiFi</summary>
    public bool MotoriWifiOk { get; set; } = false;

    /// <summary>SSID a cui è connesso ESP32 Motori</summary>
    public string MotoriSsid { get; set; } = "---";

    /// <summary>IP locale ESP32 Motori</summary>
    public string MotoriIp { get; set; } = "---";

    /// <summary>Ultimo messaggio ricevuto da ESP32 Motori</summary>
    public DateTime MotoriUltimoTs { get; set; } = DateTime.MinValue;

    /// <summary>Versione firmware ESP32 Motori (se pubblicata)</summary>
    public string MotoriFirmware { get; set; } = "---";

    // ── Stato motori ─────────────────────────────────────────────

    /// <summary>0=STOP 1=IN MOTO 2=ROTAZIONE</summary>
    public int StatoMotori { get; set; } = 0;
    public int VelocitaGlobale { get; set; } = 150;

    public string StatoMotoriLabel => StatoMotori switch
    {
        0 => "STOP",
        1 => "IN MOTO",
        2 => "ROTAZIONE",
        _ => "???"
    };

    // ── Encoder (tick assoluti) ──────────────────────────────────
    public long EncFL { get; set; } = 0;
    public long EncFR { get; set; } = 0;
    public long EncRL { get; set; } = 0;
    public long EncRR { get; set; } = 0;

    // ── PWM individuali (-127..+127) ─────────────────────────────
    public int VelFL { get; set; } = 0;
    public int VelFR { get; set; } = 0;
    public int VelRL { get; set; } = 0;
    public int VelRR { get; set; } = 0;

    // ── Errori motori ────────────────────────────────────────────
    public int MotoriErroriI2c { get; set; } = 0;
    public int MotoriErroriMqtt { get; set; } = 0;

    // ════════════════════════════════════════════════════════════════
    //  ESP32 SENSORI (0x09) — stato connessione
    // ════════════════════════════════════════════════════════════════

    /// <summary>ESP32 Sensori raggiungibile via MQTT</summary>
    public bool SensoriOnline { get; set; } = false;

    /// <summary>Bus I2C tra Pi e ESP32 Sensori funzionante</summary>
    public bool SensoriI2cOk { get; set; } = false;

    /// <summary>ESP32 Sensori connesso al broker MQTT</summary>
    public bool SensoriMqttOk { get; set; } = false;

    /// <summary>ESP32 Sensori connesso al WiFi</summary>
    public bool SensoriWifiOk { get; set; } = false;

    /// <summary>SSID a cui è connesso ESP32 Sensori</summary>
    public string SensoriSsid { get; set; } = "---";

    /// <summary>IP locale ESP32 Sensori</summary>
    public string SensoriIp { get; set; } = "---";

    /// <summary>Ultimo messaggio ricevuto da ESP32 Sensori</summary>
    public DateTime SensoriUltimoTs { get; set; } = DateTime.MinValue;

    /// <summary>Versione firmware ESP32 Sensori (se pubblicata)</summary>
    public string SensoriFirmware { get; set; } = "---";

    // ── Errori sensori ───────────────────────────────────────────
    public int SensoriErroriI2c { get; set; } = 0;
    public int SensoriErroriMqtt { get; set; } = 0;

    // ── Distanze ultrasuoni (cm, 9999 = n/d) ────────────────────
    public int DistFronte { get; set; } = 9999;
    public int DistRetro { get; set; } = 9999;
    public int DistSinistra { get; set; } = 9999;
    public int DistDestra { get; set; } = 9999;
    public int DistCliffF { get; set; } = 9999;
    public int DistCliffR { get; set; } = 9999;

    public string DistFronteStr => FormatDist(DistFronte);
    public string DistRetroStr => FormatDist(DistRetro);
    public string DistSinistraStr => FormatDist(DistSinistra);
    public string DistDestraStr => FormatDist(DistDestra);
    public string DistCliffFStr => FormatDist(DistCliffF);
    public string DistCliffRStr => FormatDist(DistCliffR);

    private static string FormatDist(int cm)
        => cm >= 9999 ? "---" : $"{cm} cm";

    // ── IMU MPU-6050 ─────────────────────────────────────────────

    /// <summary>MPU-6050 inizializzato correttamente</summary>
    public bool MpuOk { get; set; } = false;
    public DateTime ImuUltimoTs { get; set; } = DateTime.MinValue;

    public double AccX { get; set; } = 0;
    public double AccY { get; set; } = 0;
    public double AccZ { get; set; } = 0;
    public double GyrX { get; set; } = 0;
    public double GyrY { get; set; } = 0;
    public double GyrZ { get; set; } = 0;

    // ── TCRT5000 ─────────────────────────────────────────────────
    public bool TcrtSx { get; set; } = false;
    public bool TcrtCen { get; set; } = false;
    public bool TcrtDx { get; set; } = false;

    // ── Relè pompa ───────────────────────────────────────────────
    public bool ReleOn { get; set; } = false;

    // ── PCA9685 + servo ──────────────────────────────────────────

    /// <summary>PCA9685 inizializzato correttamente</summary>
    public bool Pca9685Ok { get; set; } = false;

    /// <summary>
    /// Posizioni servo correnti (0..180°)
    /// CH0=Base CH1=Spalla CH2=Gomito CH3=PolsoV CH4=PolsoR CH5=Pinza
    /// </summary>
    public int[] ServoPos { get; set; } = { 90, 90, 90, 90, 90, 120 };

    public static readonly string[] ServoNomi =
        { "Base", "Spalla", "Gomito", "Polso V", "Polso R", "Pinza" };

    // ════════════════════════════════════════════════════════════════
    //  DRONE ESP32
    // ════════════════════════════════════════════════════════════════

    public bool DroneOnline { get; set; } = false;
    public bool DroneVolante { get; set; } = false;
    public int DroneThrust { get; set; } = 0;
    public int DroneRoll { get; set; } = 0;
    public int DronePitch { get; set; } = 0;
    public int DroneYaw { get; set; } = 0;
    public string DroneStatus { get; set; } = "---";
    public string DroneSsid { get; set; } = "ESP-DRONE_90E5B199B123";

    // ════════════════════════════════════════════════════════════════
    //  MODALITÀ AUTONOME
    // ════════════════════════════════════════════════════════════════

    public bool ModalitaPerlustrazione { get; set; } = false;
    public bool ModalitaInseguiLinea { get; set; } = false;

    // ════════════════════════════════════════════════════════════════
    //  RASPBERRY PI — stato sistema
    // ════════════════════════════════════════════════════════════════

    public bool PiOnline { get; set; } = false;
    public DateTime PiUltimoTs { get; set; } = DateTime.MinValue;

    /// <summary>Temperatura CPU in °C</summary>
    public double PiTempCpu { get; set; } = 0;

    /// <summary>Uso CPU in % (0-100)</summary>
    public double PiCpuPercent { get; set; } = 0;

    /// <summary>RAM usata in MB</summary>
    public int PiRamUsataMb { get; set; } = 0;

    /// <summary>RAM totale in MB</summary>
    public int PiRamTotaleMb { get; set; } = 0;

    /// <summary>Spazio disco usato in GB</summary>
    public double PiDiskUsatoGb { get; set; } = 0;

    /// <summary>Spazio disco totale in GB</summary>
    public double PiDiskTotaleGb { get; set; } = 0;

    /// <summary>IP locale del Pi sulla rete corrente</summary>
    public string PiIpLocale { get; set; } = "---";

    /// <summary>Mosquitto MQTT broker attivo sul Pi</summary>
    public bool PiMosquittoOk { get; set; } = false;

    /// <summary>Tailscale attivo sul Pi</summary>
    public bool PiTailscaleOk { get; set; } = false;

    /// <summary>mjpg-streamer attivo sul Pi</summary>
    public bool PiMjpgOk { get; set; } = false;

    /// <summary>Ollama LLM attivo sul Pi/PC esterno</summary>
    public bool PiOllamaOk { get; set; } = false;

    // Helper display
    public string PiTempStr => PiOnline ? $"{PiTempCpu:F1} °C" : "---";
    public string PiCpuStr => PiOnline ? $"{PiCpuPercent:F0}%" : "---";
    public string PiRamStr => PiOnline ? $"{PiRamUsataMb} / {PiRamTotaleMb} MB" : "---";
    public string PiDiskStr => PiOnline ? $"{PiDiskUsatoGb:F1} / {PiDiskTotaleGb:F1} GB" : "---";

    // ════════════════════════════════════════════════════════════════
    //  LOG
    // ════════════════════════════════════════════════════════════════

    public List<string> Log { get; set; } = new();

    public void AddLog(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        Log.Add(line);
        if (Log.Count > 200)
            Log.RemoveAt(0);
    }

    // ════════════════════════════════════════════════════════════════
    //  TIMEOUT CHECK
    // ════════════════════════════════════════════════════════════════

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public void CheckTimeouts()
    {
        var now = DateTime.Now;

        if (MotoriOnline && (now - MotoriUltimoTs) > Timeout)
        {
            MotoriOnline = false;
            MotoriMqttOk = false;
            StatoMotori = 0;
            VelFL = VelFR = VelRL = VelRR = 0;
        }

        if (SensoriOnline && (now - SensoriUltimoTs) > Timeout)
        {
            SensoriOnline = false;
            SensoriMqttOk = false;
            DistFronte = DistRetro = DistSinistra =
            DistDestra = DistCliffF = DistCliffR = 9999;
        }

        if (DroneOnline && (now - PiUltimoTs) > Timeout)
            DroneOnline = false;

        if (PiOnline && (now - PiUltimoTs) > Timeout)
        {
            PiOnline = false;
            PiMosquittoOk = false;
            PiMjpgOk = false;
        }
    }
}