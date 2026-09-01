using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Text;

namespace LagmaIpp.Services;

public class MqttService
{
    // ════════════════════════════════════════════════════════════════
    //  TOPIC — Subscribe (IN)
    // ════════════════════════════════════════════════════════════════
    public const string TOPIC_MOTORI_STATO = "robot/motori/stato";
    public const string TOPIC_SENSORI_DISTANZE = "robot/sensori/distanze";
    public const string TOPIC_SENSORI_IMU = "robot/sensori/imu";
    public const string TOPIC_SENSORI_TCRT = "robot/sensori/tcrt";
    public const string TOPIC_MOTORI_LOG = "robot/motori/log";
    public const string TOPIC_SENSORI_LOG = "robot/sensori/log";
    public const string TOPIC_DRONE_STATUS = "drone/status/#";
    public const string TOPIC_PI_STATO = "pi/stato";

    // ── Audio / Video (Raspberry Pi) — IN ────────────────────────
    public const string TOPIC_AUDIO_LIST = "pi/audio/sounds/list";   // Canale della Musica
    public const string TOPIC_SFX_LIST = "pi/audio/sfx/list";       // Canale degli SFX (metti sfx minuscolo!)
    public const string TOPIC_VIDEO_LIST = "pi/video/list";   // retained, pubblicato dal Pi

    // ════════════════════════════════════════════════════════════════
    //  TOPIC — Publish (OUT)
    // ════════════════════════════════════════════════════════════════
    public const string TOPIC_MOTORI_CMD = "robot/motori/cmd";
    public const string TOPIC_SENSORI_CMD = "robot/sensori/cmd";
    public const string TOPIC_DRONE_RPYT = "drone/cmd/rpyt";
    public const string TOPIC_DRONE_STOP = "drone/cmd/stop";
    public const string TOPIC_AI_INPUT = "ai/text_input";
    public const string TOPIC_PI_CMD = "pi/cmd";

    // ── Audio / Video (Raspberry Pi) — OUT ───────────────────────
    public const string TOPIC_AUDIO_VOLUME = "pi/audio/volume";
    public const string TOPIC_AUDIO_PLAY = "pi/audio/play";
    public const string TOPIC_AUDIO_STOP = "pi/audio/stop";
    public const string TOPIC_AUDIO_REFRESH = "pi/audio/refresh";
    public const string TOPIC_VIDEO_PLAY = "pi/video/play";
    public const string TOPIC_VIDEO_STOP = "pi/video/stop";

    // ════════════════════════════════════════════════════════════════
    //  EVENTI
    // ════════════════════════════════════════════════════════════════
    public event Action<string, string>? MessageReceived;
    public event Action<bool>? ConnectionChanged;

    // ════════════════════════════════════════════════════════════════
    //  STATO
    // ════════════════════════════════════════════════════════════════
    public bool IsConnected { get; private set; }
    public string BrokerHost { get; private set; } = "100.100.61.49";
    public int BrokerPort { get; private set; } = 1883;

    private IManagedMqttClient? _client;

    // ════════════════════════════════════════════════════════════════
    //  CONNESSIONE
    // ════════════════════════════════════════════════════════════════
    public async Task ConnectAsync(string brokerHost, int brokerPort = 1883)
    {
        BrokerHost = brokerHost;
        BrokerPort = brokerPort;

        var factory = new MqttFactory();
        _client = factory.CreateManagedMqttClient();

        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.ConnectedAsync += OnConnectedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;

        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(BrokerHost, BrokerPort)
            .WithClientId($"LagmaIpp_{Environment.MachineName}")
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(15))
            .WithCleanSession()
            .Build();

        var managedOptions = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(3))
            .WithClientOptions(clientOptions)
            .Build();

        // ── Subscribe a tutti i topic IN ─────────────────────────
        await _client.SubscribeAsync(TOPIC_MOTORI_STATO, MqttQualityOfServiceLevel.AtMostOnce);
        await _client.SubscribeAsync(TOPIC_SENSORI_DISTANZE, MqttQualityOfServiceLevel.AtMostOnce);
        await _client.SubscribeAsync(TOPIC_SENSORI_IMU, MqttQualityOfServiceLevel.AtMostOnce);
        await _client.SubscribeAsync(TOPIC_SENSORI_TCRT, MqttQualityOfServiceLevel.AtMostOnce);
        await _client.SubscribeAsync(TOPIC_MOTORI_LOG, MqttQualityOfServiceLevel.AtMostOnce);
        await _client.SubscribeAsync(TOPIC_SENSORI_LOG, MqttQualityOfServiceLevel.AtMostOnce);
        await _client.SubscribeAsync(TOPIC_DRONE_STATUS, MqttQualityOfServiceLevel.AtMostOnce);
        await _client.SubscribeAsync(TOPIC_PI_STATO, MqttQualityOfServiceLevel.AtMostOnce);
        await _client.SubscribeAsync("robot/gps", MqttQualityOfServiceLevel.AtMostOnce);

        // ── LISTE AUDIO/VIDEO — retained: arrivano subito dopo la connessione
        await _client.SubscribeAsync(TOPIC_AUDIO_LIST);
        await _client.SubscribeAsync(TOPIC_SFX_LIST);
        await _client.SubscribeAsync(TOPIC_VIDEO_LIST, MqttQualityOfServiceLevel.AtMostOnce);

        await _client.StartAsync(managedOptions);
    }

    public async Task DisconnectAsync()
    {
        if (_client is not null)
            await _client.StopAsync();
    }

    // ════════════════════════════════════════════════════════════════
    //  PUBLISH
    // ════════════════════════════════════════════════════════════════
    public async Task PublishAsync(string topic, string payload, bool retain = false)
    {
        if (_client is null) return;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithRetainFlag(retain)
            .Build();

        await _client.EnqueueAsync(message);
    }

    // ════════════════════════════════════════════════════════════════
    //  COMANDI — Motori
    // ════════════════════════════════════════════════════════════════
    public Task CmdStop() => PublishAsync(TOPIC_MOTORI_CMD, """{"cmd":"stop"}""");
    public Task CmdAvanti() => PublishAsync(TOPIC_MOTORI_CMD, """{"cmd":"avanti"}""");
    public Task CmdIndietro() => PublishAsync(TOPIC_MOTORI_CMD, """{"cmd":"indietro"}""");
    public Task CmdSinistra() => PublishAsync(TOPIC_MOTORI_CMD, """{"cmd":"sinistra"}""");
    public Task CmdDestra() => PublishAsync(TOPIC_MOTORI_CMD, """{"cmd":"destra"}""");
    public Task CmdRotaSx() => PublishAsync(TOPIC_MOTORI_CMD, """{"cmd":"ruota_sx"}""");
    public Task CmdRotaDx() => PublishAsync(TOPIC_MOTORI_CMD, """{"cmd":"ruota_dx"}""");

    public Task CmdVelocita(int vel)
        => PublishAsync(TOPIC_MOTORI_CMD, $"{{\"cmd\":\"velocita\",\"val\":{vel}}}");

    public Task CmdMecanum(int vx, int vy, int vr)
        => PublishAsync(TOPIC_MOTORI_CMD,
            $"{{\"cmd\":\"mecanum\",\"vx\":{vx},\"vy\":{vy},\"vr\":{vr}}}");

    // ════════════════════════════════════════════════════════════════
    //  COMANDI — Braccio
    // ════════════════════════════════════════════════════════════════
    public Task CmdServo(int ch, int ang)
        => PublishAsync(TOPIC_SENSORI_CMD, $"{{\"cmd\":\"servo\",\"ch\":{ch},\"ang\":{ang}}}");

    public Task CmdServoRel(int ch, int delta)
        => PublishAsync(TOPIC_SENSORI_CMD, $"{{\"cmd\":\"servo_rel\",\"ch\":{ch},\"delta\":{delta}}}");

    // ════════════════════════════════════════════════════════════════
    //  COMANDI — Relè
    // ════════════════════════════════════════════════════════════════
    public Task CmdRele(bool on)
        => PublishAsync(TOPIC_SENSORI_CMD, $"{{\"cmd\":\"rele\",\"val\":{(on ? 1 : 0)}}}");

    // ════════════════════════════════════════════════════════════════
    //  COMANDI — Drone
    // ════════════════════════════════════════════════════════════════
    public Task CmdDroneRpyt(int roll, int pitch, int yaw, int thrust)
        => PublishAsync(TOPIC_DRONE_RPYT,
            $"{{\"roll\":{roll},\"pitch\":{pitch},\"yaw\":{yaw},\"thrust\":{thrust}}}");

    public Task CmdDroneStop() => PublishAsync(TOPIC_DRONE_STOP, """{"cmd":"stop"}""");
    public Task CmdDroneLand() => PublishAsync(TOPIC_DRONE_STOP, """{"cmd":"land"}""");

    // ════════════════════════════════════════════════════════════════
    //  COMANDI — Audio/Video (Raspberry Pi)
    // ════════════════════════════════════════════════════════════════

    /// <summary>Volume 0–100 inviato al Pi.</summary>
    public Task CmdAudioVolume(int vol)
    {
        vol = Math.Clamp(vol, 0, 100);
        return PublishAsync(TOPIC_AUDIO_VOLUME, vol.ToString());
    }

    /// <summary>Riproduce un file audio. mqttPath = "music/brano.mp3" o "SFX/boom.wav".</summary>
    public Task CmdAudioPlay(string mqttPath) => PublishAsync(TOPIC_AUDIO_PLAY, mqttPath);

    /// <summary>Ferma l'audio in corso.</summary>
    public Task CmdAudioStop() => PublishAsync(TOPIC_AUDIO_STOP, "stop");

    /// <summary>Chiede al Pi di ripubblicare le liste audio e video.</summary>
    public Task CmdAudioRefresh() => PublishAsync(TOPIC_AUDIO_REFRESH, "1");

    /// <summary>Riproduce un file video/immagine. mqttPath = solo il nome file.</summary>
    public Task CmdVideoPlay(string mqttPath) => PublishAsync(TOPIC_VIDEO_PLAY, mqttPath);

    /// <summary>Ferma il video in corso.</summary>
    public Task CmdVideoStop() => PublishAsync(TOPIC_VIDEO_STOP, "stop");

    // ════════════════════════════════════════════════════════════════
    //  COMANDI — Pi
    // ════════════════════════════════════════════════════════════════
    public Task CmdModalita(string tipo, bool attiva)
        => PublishAsync(TOPIC_PI_CMD,
            $"{{\"cmd\":\"modalita\",\"tipo\":\"{tipo}\",\"attiva\":{(attiva ? "true" : "false")}}}");

    public Task CmdEmozione(string emozione)
        => PublishAsync(TOPIC_PI_CMD, $"{{\"cmd\":\"emozione\",\"tipo\":\"{emozione}\"}}");

    public Task CmdMedia(string tipo, string? path = null)
        => PublishAsync(TOPIC_PI_CMD,
            path is null
                ? $"{{\"cmd\":\"media\",\"tipo\":\"{tipo}\"}}"
                : $"{{\"cmd\":\"media\",\"tipo\":\"{tipo}\",\"path\":\"{path}\"}}");

    public Task CmdAiInput(string testo) => PublishAsync(TOPIC_AI_INPUT, testo);

    // ════════════════════════════════════════════════════════════════
    //  CALLBACK
    // ════════════════════════════════════════════════════════════════

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
        MessageReceived?.Invoke(topic, payload);
        return Task.CompletedTask;
    }

    private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        IsConnected = true;
        ConnectionChanged?.Invoke(true);
        return Task.CompletedTask;
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        IsConnected = false;
        ConnectionChanged?.Invoke(false);
        return Task.CompletedTask;
    }
}