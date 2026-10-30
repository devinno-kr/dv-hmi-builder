using Devinno.Communication.Modbus.TCP;

namespace HotAirDryerHmi.Simulation;

/// <summary>
/// 가짜 열풍 건조기. Models/Dryer.cs 의 주소표와 같은 주소로 Modbus TCP 슬레이브를 연다.
/// 앱은 127.0.0.1 로, 실제 장비와 똑같은 통신 경로로 여기에 붙으므로, 장비 없이도 화면·로직·통신 코드를 함께 확인할 수 있다.
/// 슬레이브는 이 PC 의 모든 네트워크 주소에서 듣는다 (라이브러리에 주소를 고르는 설정이 없음) — 첫 실행 때 Windows 방화벽 창이 뜰 수 있고, 취소해도 된다.
/// 시간은 빠르게 흐른다: 레시피 1분 = 실제 1초.
/// </summary>
public sealed class DryerSimulator : IDisposable
{
    private const double Ambient = 24;
    private const double SecPerRecipeMinute = 1;

    private readonly ModbusTCPSlave slave;
    private readonly Random rnd = new(7);
    private System.Threading.Timer? timer;

    // 장비 내부 상태
    private double temp = Ambient, inlet = Ambient, humidity = 38, heater, fan, damper = 10, level = 72;
    private int state;           // DryerState 숫자
    private double stepLeft;     // 초
    private double setTemp = 80;
    private bool auto, doorClosed = true;
    private int alarms;
    private double doorEventIn = 40;
    private readonly int[] recipe = [700, 5, 800, 30, 400, 5, 600];   // Holding 110~116 과 같은 형식

    /// <summary>만들어진 시뮬레이터 수 — 시험 한 번에 장비마다 하나만 만들어지는지 확인하는 용도</summary>
    internal static int Instances;

    public DryerSimulator(int port)
    {
        Interlocked.Increment(ref Instances);
        slave = new ModbusTCPSlave
        {
            // 이 PC 에서만 듣는다 — 모든 주소(0.0.0.0)로 들으면 Windows 방화벽이 실행 파일마다 허용을 묻는다
            LocalIP = "127.0.0.1",
            LocalPort = port,
            Slave = 1,
            HoldingRegisters = new int[128],
            Coils = new bool[16],
        };
    }

    public void Start()
    {
        slave.Start();
        timer = new System.Threading.Timer(_ => Step(0.2), null, 0, 200);
    }

    public void Stop()
    {
        timer?.Dispose();
        timer = null;
        slave.Stop();
    }

    public void Dispose()
    {
        Stop();
        slave.Dispose();
    }

    // ---- 테스트용 (internal): 시뮬레이터 값을 직접 정하고 읽는다. 앱 동작에는 쓰이지 않는다

    /// <summary>건조실 온도를 직접 정한다 (℃)</summary>
    internal void SetChamberTemp(double c) { lock (slave.SyncRoot) temp = c; }

    /// <summary>
    /// true 면 마스터 요청에 응답하지 않는다 (통신 끊김 시험용). 슬레이브가 자기 번호가 아닌 요청을 무시하는 동작을 쓴다 —
    /// 앱은 번호 1 로 묻는데 슬레이브 번호를 바꿔 놓으면 접속은 되어 있어도 답이 없다. false 로 돌리면 곧바로 다시 응답한다.
    /// </summary>
    internal bool Silent
    {
        get { lock (slave.SyncRoot) return slave.Slave != 1; }
        set { lock (slave.SyncRoot) slave.Slave = value ? 2 : 1; }
    }

    /// <summary>
    /// 지금의 장비 상태(온도·상태·설정값·레시피 레지스터)를 기억했다가 Dispose 때 되돌린다.
    /// 시뮬레이터는 시험 전체가 함께 쓰므로, 상태를 바꾸는 시험은 맨 앞에서 이것을 잡는다: using var restore = sim.Simulator.Preserve();
    /// </summary>
    internal IDisposable Preserve()
    {
        lock (slave.SyncRoot)
        {
            var (t, st, left, set, al) = (temp, state, stepLeft, setTemp, alarms);
            var rec = (int[])recipe.Clone();
            var h100 = slave.HoldingRegisters![100];
            var h110 = slave.HoldingRegisters[110..117];
            return new Restore(() =>
            {
                lock (slave.SyncRoot)
                {
                    (temp, state, stepLeft, setTemp, alarms) = (t, st, left, set, al);
                    rec.CopyTo(recipe, 0);
                    slave.HoldingRegisters![100] = h100;
                    h110.CopyTo(slave.HoldingRegisters, 110);
                }
            });
        }
    }

    private sealed class Restore(Action undo) : IDisposable
    {
        public void Dispose() => undo();
    }

    /// <summary>시뮬레이터가 지금 갖고 있는 Holding 레지스터 값 (0 기준 주소)</summary>
    internal int Holding(int address) { lock (slave.SyncRoot) return slave.HoldingRegisters![address]; }

    /// <summary>시뮬레이터가 지금 갖고 있는 코일 값 (0 기준 주소)</summary>
    internal bool Coil(int address) { lock (slave.SyncRoot) return slave.Coils![address]; }

    private void Step(double dt)
    {
        lock (slave.SyncRoot)
        {
            var hr = slave.HoldingRegisters!;
            var co = slave.Coils!;
            ReadCommands(hr, co);
            Simulate(dt);
            Publish(hr, co);
        }
    }

    // 마스터가 쓴 명령을 받아들이고 명령 코일은 0 으로 되돌린다
    private void ReadCommands(int[] hr, bool[] co)
    {
        for (var i = 0; i < recipe.Length; i++) if (hr[110 + i] != 0) recipe[i] = hr[110 + i];
        if (hr[100] != 0) setTemp = hr[100] / 10.0;
        auto = co[13];
        if (co[10])
        {
            co[10] = false;
            if (state is 0 or 4 && doorClosed && alarms == 0) { state = 1; stepLeft = recipe[1] * SecPerRecipeMinute; }
        }
        if (co[11]) { co[11] = false; state = 0; }
        if (co[12]) { co[12] = false; alarms = 0; if (state == 9) state = 0; }
    }

    private void Simulate(double dt)
    {
        // 단계 진행
        if (state is 1 or 2 or 3)
        {
            stepLeft -= dt;
            if (stepLeft <= 0)
            {
                state++;
                if (state <= 3) stepLeft = recipe[state * 2 - 1] * SecPerRecipeMinute;
            }
        }
        var running = state is 1 or 2 or 3;
        var target = state switch { 1 => recipe[0] / 10.0, 2 => recipe[2] / 10.0, 3 => recipe[4] / 10.0, _ => Ambient };
        if (state == 2) setTemp = target;

        // 히터: 비례 + 앞먹임. 냉각·정지면 꺼짐
        var heat = state is 1 or 2 ? Math.Clamp((target - temp) * 1.6 + (target - Ambient) / 1.8, 0, 100) : 0;
        heater += (heat - heater) * 0.08;
        fan += ((running ? (state == 3 ? 100 : recipe[6] / 10.0) : 0) - fan) * 0.25;
        damper += ((running ? 35 + fan * 0.3 : 8) - damper) * 0.2;

        // 열풍 입구 → 건조실 (1차 지연)
        var hotAir = Ambient + heater * 1.9;
        inlet += (hotAir - inlet) * (fan > 5 ? 0.12 : 0.02);
        var chamberTarget = fan > 5 ? inlet - 6 : Ambient + (temp - Ambient) * 0.98;
        temp += (chamberTarget - temp) * (fan > 5 ? 0.05 : 0.01);

        // 습도: 건조 중 내려가고 정지하면 천천히 오른다
        var humTarget = running ? Math.Max(3, 30 - (temp - Ambient) * 0.22) : 38;
        humidity += (humTarget - humidity) * 0.01;

        // 재료: 운전 중 성형기로 빠져나가고, 20% 아래면 로더가 채움
        level -= running ? 0.08 : 0;
        if (level < 20) level = 92;

        // 가끔 정지 중에 문이 열렸다 닫힘 (알람 확인용)
        doorEventIn -= dt;
        if (doorEventIn <= 0)
        {
            if (doorClosed && !running) { doorClosed = false; doorEventIn = 6; }
            else { doorClosed = true; doorEventIn = 90 + rnd.Next(60); }
        }
        alarms = (alarms & ~4) | (doorClosed ? 0 : 4);
        if (temp > 200) { alarms |= 1; state = 9; }
    }

    private void Publish(int[] hr, bool[] co)
    {
        var running = state is 1 or 2 or 3;
        double N(double s) => (rnd.NextDouble() - 0.5) * s;
        hr[0] = W(temp + N(0.4));
        hr[1] = W(temp - 12 - fan * 0.05 + N(0.6));
        hr[2] = W(inlet + N(0.4));
        hr[3] = W(humidity + N(0.3));
        hr[4] = W(heater);
        hr[5] = W(fan);
        hr[6] = W(damper);
        hr[7] = W(level);
        hr[8] = state;
        hr[9] = (int)Math.Max(0, stepLeft);
        hr[10] = alarms;
        hr[11] = W(setTemp);
        co[0] = running;
        co[1] = heater > 1;
        co[2] = fan > 1;
        co[3] = doorClosed;
        co[4] = auto;
    }

    private static int W(double v) => (int)Math.Round(v * 10) & 0xFFFF;
}
