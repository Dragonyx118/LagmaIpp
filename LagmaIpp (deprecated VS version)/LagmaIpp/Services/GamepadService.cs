using System.Timers;

namespace LagmaIpp.Services;

/// <summary>
/// Modalità target del gamepad — solo uno può essere attivo alla volta.
/// </summary>
public enum GamepadTarget { None, Robot, Drone, Arm }

/// <summary>
/// Snapshot dello stato dei pulsanti/assi del controller (platform-agnostic).
/// Aggiornato ogni frame dal polling platform-specific.
/// </summary>
public class GamepadState
{
    // ── Assi analogici [-1..1] ───────────────────────────────────
    public float LeftX;   // strafe sx/dx (macchinina), roll (drone), servo increment (braccio)
    public float LeftY;   // avanti/indietro (macchinina), pitch (drone)
    public float RightX;  // yaw (drone), non usato (braccio)
    public float RightY;  // thrust (drone)
    public float LT;      // trigger sinistro [0..1] → ruota sx (macchinina)
    public float RT;      // trigger destro  [0..1] → ruota dx (macchinina)

    // ── Pulsanti digitali ────────────────────────────────────────
    public bool South;        // A / Cross
    public bool East;         // B / Circle
    public bool North;        // Y / Triangle
    public bool West;         // X / Square
    public bool LeftBumper;   // LB / L1
    public bool RightBumper;  // RB / R1
    public bool Start;        // Start / Options
    public bool Select;       // Back / Share
    public bool DpadUp;
    public bool DpadDown;
    public bool DpadLeft;
    public bool DpadRight;
}

/// <summary>
/// Gestisce il loop di polling del gamepad e dispatcha i comandi
/// al ViewModel in base al target attivo (Robot / Drone / Braccio).
/// </summary>
public class GamepadService : IDisposable
{
    // ── Evento pubblico: stato aggiornato ───────────────────────
    public event Action<GamepadState>? StateUpdated;

    // ── Stato corrente ───────────────────────────────────────────
    public GamepadState State { get; } = new();
    public bool IsConnected { get; private set; }
    public GamepadTarget Target { get; private set; } = GamepadTarget.None;

    // ── Servo selezionato per il braccio ─────────────────────────
    public int SelectedServo { get; private set; } = 0; // 0..5
    private const int ServoCount = 7; // 0-5 braccio + 6 camera

    // ── Riferimento al ViewModel ─────────────────────────────────
    private ViewModels.MainViewModel? _vm;

    // ── Timer polling ────────────────────────────────────────────
    private System.Timers.Timer? _pollTimer;
    private const int PollMs = 50; // 20Hz

    // ── Anti-rimbalzo per pulsanti ───────────────────────────────
    private bool _prevNorth, _prevWest, _prevSelect, _prevStart;
    private bool _prevDpadUp, _prevDpadDown;
    private bool _prevSouth, _prevEast;

    // ── Deadzone per gli assi ────────────────────────────────────
    private const float Deadzone = 0.12f;

    public GamepadService() { }

    // ════════════════════════════════════════════════════════════
    //  API PUBBLICA
    // ════════════════════════════════════════════════════════════

    /// <summary>Inizializza il servizio con il ViewModel.</summary>
    public void Initialize(ViewModels.MainViewModel vm)
    {
        _vm = vm;
    }

    /// <summary>Attiva/disattiva il controllo per il target specificato.</summary>
    public void SetTarget(GamepadTarget target)
    {
        Target = target;
        if (target == GamepadTarget.None)
            StopPolling();
        else
            StartPolling();
    }

    /// <summary>Aggiorna lo stato del gamepad dall'esterno (platform layer).</summary>
    public void UpdateState(Action<GamepadState> updater)
    {
        updater(State);
        IsConnected = true;
    }

    /// <summary>Segnala che il gamepad si è disconnesso.</summary>
    public void OnDisconnected()
    {
        IsConnected = false;
        // reset assi
        State.LeftX = State.LeftY = State.RightX = State.RightY = 0;
        State.LT = State.RT = 0;
    }

    // ════════════════════════════════════════════════════════════
    //  POLLING TIMER
    // ════════════════════════════════════════════════════════════

    private void StartPolling()
    {
        if (_pollTimer != null) return;
        _pollTimer = new System.Timers.Timer(PollMs);
        _pollTimer.Elapsed += OnPollTick;
        _pollTimer.Start();
    }

    private void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer?.Dispose();
        _pollTimer = null;
        // Invia stop ai device
        _ = SendStopAll();
    }

    private void OnPollTick(object? sender, ElapsedEventArgs e)
    {
        if (_vm == null || Target == GamepadTarget.None) return;

        StateUpdated?.Invoke(State);

        switch (Target)
        {
            case GamepadTarget.Robot: _ = HandleRobotAsync(); break;
            case GamepadTarget.Drone: _ = HandleDroneAsync(); break;
            case GamepadTarget.Arm: HandleArm(); break;
        }

        SavePrevButtons();
    }

    // ════════════════════════════════════════════════════════════
    //  HANDLER — MACCHININA (mecanum wheels)
    // ════════════════════════════════════════════════════════════
    //
    //  Left stick:  Vy = avanti/indietro,  Vx = strafe sx/dx
    //  LT / RT:     rotazione sx / dx
    //  South (A):   STOP
    //  Start:       toggle modalità perlustrazione
    //
    //  Mecanum: CmdMecanum(vx, vy, vr)
    //    vx = strafe  (+dx, -sx)
    //    vy = avanti  (+avanti, -indietro)
    //    vr = rotazione (+dx, -sx)

    private async Task HandleRobotAsync()
    {
        if (_vm == null) return;

        var vel = _vm.VelocitaGlobale;

        float lx = ApplyDeadzone(State.LeftX);
        float ly = ApplyDeadzone(State.LeftY);
        float lt = State.LT;  // [0..1]
        float rt = State.RT;  // [0..1]

        // Rotazione: LT → sx, RT → dx
        float rotation = (rt - lt);

        // Se tutto a zero → STOP
        bool anyMovement = Math.Abs(lx) > 0.01f
                        || Math.Abs(ly) > 0.01f
                        || Math.Abs(rotation) > 0.05f;

        if (State.South && !_prevSouth)
        {
            // A = STOP immediato
            await _vm.Stop();
            return;
        }

        if (!anyMovement)
        {
            // nessun input → non inviare (risparmia MQTT)
            return;
        }

        int vx = (int)(lx * vel);
        int vy = (int)(-ly * vel);      // Y invertito (su = negativo sul joystick)
        int vr = (int)(rotation * vel);

        await _vm.Mecanum(vx, vy, vr);
    }

    // ════════════════════════════════════════════════════════════
    //  HANDLER — DRONE
    // ════════════════════════════════════════════════════════════
    //
    //  Left stick:   Roll (X) + Pitch (Y, invertito)
    //  Right stick:  Yaw (X) + Thrust (Y, invertito, clamp 0..100)
    //  East (B):     STOP/Kill
    //  South (A):    Landing

    private async Task HandleDroneAsync()
    {
        if (_vm == null) return;

        float lx = ApplyDeadzone(State.LeftX);
        float ly = ApplyDeadzone(State.LeftY);
        float rx = ApplyDeadzone(State.RightX);
        float ry = ApplyDeadzone(State.RightY);

        if (State.East && !_prevEast)
        {
            await _vm.DroneStop();
            return;
        }

        if (State.South && !_prevSouth)
        {
            await _vm.DroneLand();
            return;
        }

        int roll = (int)(lx * 100);
        int pitch = (int)(-ly * 100);
        int yaw = (int)(rx * 100);
        int thrust = Math.Clamp((int)(-ry * 100), 0, 100);

        bool anyInput = roll != 0 || pitch != 0 || yaw != 0 || thrust != 0;
        if (!anyInput) return;

        await _vm.DroneRpyt(roll, pitch, yaw, thrust);
    }

    // ════════════════════════════════════════════════════════════
    //  HANDLER — BRACCIO
    // ════════════════════════════════════════════════════════════
    //
    //  North (Y):    servo successivo (+1)
    //  West (X):     servo precedente (-1)
    //  Left stick Y: incrementa/decrementa il servo selezionato
    //  Dpad Up/Down: fine-tuning ±1°
    //  South (A):    preset Home (tutti 90°)

    private void HandleArm()
    {
        if (_vm == null) return;

        // Cambia servo selezionato
        if (State.North && !_prevNorth)
            SelectedServo = (SelectedServo + 1) % ServoCount;

        if (State.West && !_prevWest)
            SelectedServo = (SelectedServo - 1 + ServoCount) % ServoCount;

        // Preset Home
        if (State.South && !_prevSouth)
        {
            _vm.Servo0 = _vm.Servo1 = _vm.Servo2 = _vm.Servo3 = _vm.Servo4 = 90;
            _vm.Servo5 = 120;
            return;
        }

        // Movimento grosso con stick sinistro Y
        float ly = ApplyDeadzone(State.LeftY);
        int bigDelta = (int)(-ly * 3); // max ±3° per frame a 20Hz

        // Movimento fine con D-Pad
        int fineDelta = 0;
        if (State.DpadUp && !_prevDpadUp) fineDelta = +1;
        if (State.DpadDown && !_prevDpadDown) fineDelta = -1;

        int delta = bigDelta + fineDelta;
        if (delta == 0) return;

        AdjustServo(SelectedServo, delta);
    }

    private void AdjustServo(int index, int delta)
    {
        if (_vm == null) return;
        switch (index)
        {
            case 0: _vm.Servo0 = Clamp180(_vm.Servo0 + delta); break;
            case 1: _vm.Servo1 = Clamp180(_vm.Servo1 + delta); break;
            case 2: _vm.Servo2 = Clamp180(_vm.Servo2 + delta); break;
            case 3: _vm.Servo3 = Clamp180(_vm.Servo3 + delta); break;
            case 4: _vm.Servo4 = Clamp180(_vm.Servo4 + delta); break;
            case 5: _vm.Servo5 = Clamp180(_vm.Servo5 + delta); break;
            case 6: _vm.ServoCamera = Math.Clamp(_vm.ServoCamera + delta, 80, 160); break;
        }
    }

    private static int Clamp180(int val) => Math.Clamp(val, 0, 180);

    // ════════════════════════════════════════════════════════════
    //  UTILITY
    // ════════════════════════════════════════════════════════════

    private static float ApplyDeadzone(float v)
        => Math.Abs(v) < Deadzone ? 0f : v;

    private async Task SendStopAll()
    {
        if (_vm == null) return;
        await _vm.Stop();
    }

    private void SavePrevButtons()
    {
        _prevNorth = State.North;
        _prevWest = State.West;
        _prevSouth = State.South;
        _prevEast = State.East;
        _prevSelect = State.Select;
        _prevStart = State.Start;
        _prevDpadUp = State.DpadUp;
        _prevDpadDown = State.DpadDown;
    }

    public void Dispose()
    {
        StopPolling();
    }
}