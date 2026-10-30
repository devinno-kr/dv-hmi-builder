using System.Collections.Concurrent;
using Devinno.Communication.Modbus.TCP;

namespace HotAirDryerHmi.Models;

// 주소표(매뉴얼 표기·이름·형식·배율·쓰는 방식)는 메모리 맵 design/devices.json 의 map 에 있다 — 대시보드 통신 탭이 dryer.xml 과 맞춰 보여 준다.

/// <summary>
/// 열풍 건조기 1대. 자기 통신 마스터·주소·값 해석·쓰기 명령을 갖는다. 화면·알람·파일은 건드리지 않는다.
/// 통신 설정은 dryer.xml (읽기 목록·이름 붙인 쓰기), 접속 주소는 Setting. 주소와 해석은 메모리 맵 (design/devices.json).
/// </summary>
public sealed class Dryer : IDisposable
{
    private static readonly string XmlPath = Path.Combine(AppContext.BaseDirectory, "dryer.xml");

    // dryer.xml 의 Id — 결과를 기다리는 쓰기만
    private const int StopId = 101, SetTempId = 104, RecipeId = 105;

    private readonly object gate = new();
    private int[] regs = new int[12];
    private bool[] coils = new bool[5];
    private DateTime wordsAt = DateTime.MinValue, bitsAt = DateTime.MinValue;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> waiting = new();

    public Dryer(Setting setting)
    {
        Comm.LoadXml(XmlPath);
        Comm.RemoteIP = setting.DryerIp;
        Comm.RemotePort = setting.DryerPort;
        Comm.WordReadReceived += OnWords;
        Comm.BitReadReceived += OnBits;
        // 쓰기마다 결과 이벤트가 꼭 하나 온다 (성공·예외·최종 타임아웃·버려짐). 기다리는 쓰기면 결과를 넘긴다
        Comm.BitWriteReceived += (_, e) => Finish(e.MessageID, true);         // 코일 (FC5)
        Comm.WordWriteReceived += (_, e) => Finish(e.MessageID, true);        // 레지스터 하나 (FC6)
        Comm.MultiWordWriteReceived += (_, e) => Finish(e.MessageID, true);   // 레지스터 여러 개 (FC16)
        Comm.WriteDiscarded += (_, e) => { LastError = $"write {e.MessageID} not sent: {e.Reason}"; Finish(e.MessageID, false); };
        Comm.TimeoutReceived += (_, e) => { if (e.IsFinal) { LastError = $"no response (request {e.MessageID})"; Finish(e.MessageID, false); } };
        Comm.ExceptionReceived += (_, e) => { LastError = $"device exception {e.ExceptionCode} (request {e.MessageID})"; Finish(e.MessageID, false); };
    }

    /// <summary>통신 마스터. 테스트·진단에서만 직접 쓴다</summary>
    internal ModbusTCPMaster Comm { get; } = new();

    /// <summary>마지막으로 받은 값 묶음. 통신 스레드가 통째로 바꿔 끼운다 (화면이 반쯤 바뀐 값을 읽지 않게)</summary>
    public DryerData Data { get; private set; } = DryerData.Empty;

    /// <summary>마지막 수신 시각 — 읽기 목록(레지스터·코일)마다 받은 시각 중 가장 오래된 것. 하나라도 오래되면 그 값들도 낡은 것이므로 통신 끊김은 이것으로 판정한다</summary>
    public DateTime LastReceived
    {
        get
        {
            var w = wordsAt;
            var b = bitsAt;
            return w < b ? w : b;
        }
    }

    /// <summary>마지막 통신 오류 — 설정 화면의 진단에 보인다</summary>
    public string? LastError { get; private set; }

    /// <summary>통신 끊김인지 — 마지막 수신 뒤 timeout 이 지났으면 true</summary>
    public bool IsCommLost(DateTime now, TimeSpan timeout) => now - LastReceived > timeout;

    /// <summary>통신 시작 — 읽기 목록을 주기적으로 읽는다</summary>
    public void Start() => Comm.Start();
    /// <summary>통신 멈춤</summary>
    public void Stop() => Comm.Stop();
    /// <summary>통신 자원 정리 — 프로그램을 끝낼 때</summary>
    public void Dispose() => Comm.Dispose();

    /// <summary>새 접속 설정을 적용한다. 돌고 있었으면 멈췄다가 다시 시작한다.</summary>
    public void ApplySetting(Setting setting)
    {
        var running = Comm.IsStart;
        if (running) Comm.Stop();
        Comm.RemoteIP = setting.DryerIp;
        Comm.RemotePort = setting.DryerPort;
        if (running) Comm.Start();
    }

    // ---- 쓰기 명령 (연결되지 않았으면 라이브러리가 버리고 WriteDiscarded 로 알린다)

    /// <summary>시작 명령 — 응답을 기다리지 않는다</summary>
    public void RunStart() => Comm.NamedWrite("StartCmd", 1);
    /// <summary>정지 명령 — 응답을 기다리지 않는다 (고온 정지 때 로직이 쓴다)</summary>
    public void RunStop() => Comm.NamedWrite("StopCmd", 1);
    /// <summary>리셋 명령</summary>
    public void Reset() => Comm.NamedWrite("ResetCmd", 1);
    /// <summary>자동 모드 명령 — true 자동, false 수동</summary>
    public void SetAuto(bool auto) => Comm.NamedWrite("AutoMode", auto ? 1 : 0);
    // ---- 결과를 기다리는 쓰기: 장비가 받았다고 응답해야 true (버려짐·무응답·예외는 false)

    /// <summary>정지 명령 — 장비가 받았다는 응답을 기다린다 (화면의 정지 버튼)</summary>
    public Task<bool> RunStopAsync() => WriteAndWait(StopId, () => Comm.NamedWrite("StopCmd", 1));

    /// <summary>설정 온도 쓰기 — 장비가 받았다는 응답을 기다린다</summary>
    public Task<bool> SetTempAsync(double c) => WriteAndWait(SetTempId, () => Comm.NamedWrite("SetTemp", Raw(c)));

    /// <summary>레시피 쓰기 — 7개 값을 한 번에, 장비가 받았다는 응답을 기다린다</summary>
    public Task<bool> WriteRecipeAsync(Recipe r) => WriteAndWait(RecipeId, () => Comm.NamedWrite("Recipe",
    [
        Raw(r.Preheat.TempC), r.Preheat.Minutes,
        Raw(r.Dry.TempC), r.Dry.Minutes,
        Raw(r.Cool.TempC), r.Cool.Minutes,
        Raw(r.FanPct),
    ]));

    private async Task<bool> WriteAndWait(int id, Action send)
    {
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!waiting.TryAdd(id, result))   // 같은 쓰기가 아직 결과를 기다리는 중 (정지를 두 번 누름 등) — 그 결과를 같이 기다린다
            return waiting.TryGetValue(id, out var pending) && await pending.Task.WaitAsync(TimeSpan.FromSeconds(10)).ContinueWith(t => t.IsCompletedSuccessfully && t.Result);
        try
        {
            send();   // 연결이 없으면 이 안에서 WriteDiscarded 가 바로 온다
            return await result.Task.WaitAsync(TimeSpan.FromSeconds(10));   // 결과 이벤트는 꼭 오지만, 화면이 영영 기다리지 않게
        }
        catch (TimeoutException) { return false; }
        finally { waiting.TryRemove(id, out _); }
    }

    private void Finish(int id, bool ok)
    {
        if (waiting.TryGetValue(id, out var result)) result.TrySetResult(ok);
    }

    private static long Raw(double v) => (long)Math.Round(v * 10);

    // ---- 값 해석 (통신 스레드). 처리기 예외는 라이브러리가 삼키므로 직접 잡아 LastError 에 남긴다

    internal void OnWords(object? sender, ModbusTCPMaster.WordReadEventArgs e)
    {
        try
        {
            if (e.MessageID != 1) return;
            lock (gate)
            {
                regs = [.. e.ReceiveData];
                Data = Parse(regs, coils);
            }
            wordsAt = DateTime.Now;   // 마지막에: 값이 다 들어간 뒤에만 "받았다"
        }
        catch (Exception ex) { LastError = ex.Message; }
    }

    internal void OnBits(object? sender, ModbusTCPMaster.BitReadEventArgs e)
    {
        try
        {
            if (e.MessageID != 2) return;
            lock (gate)
            {
                coils = [.. e.ReceiveData];
                Data = Parse(regs, coils);
            }
            bitsAt = DateTime.Now;
        }
        catch (Exception ex) { LastError = ex.Message; }
    }

    internal static DryerData Parse(int[] r, bool[] c) => new()
    {
        ChamberTemp = (short)r[0] / 10.0,
        ExhaustTemp = (short)r[1] / 10.0,
        InletTemp = (short)r[2] / 10.0,
        Humidity = r[3] / 10.0,
        HeaterPct = r[4] / 10.0,
        FanPct = r[5] / 10.0,
        DamperPct = r[6] / 10.0,
        LevelPct = r[7] / 10.0,
        State = (DryerState)r[8],
        StepRemainSec = r[9],
        Alarms = (DryerAlarm)r[10],
        SetTemp = (short)r[11] / 10.0,
        Running = c[0],
        HeaterOn = c[1],
        FanOn = c[2],
        DoorClosed = c[3],
        AutoMode = c[4],
    };
}

/// <summary>건조기 상태 (주소 40009 의 값)</summary>
public enum DryerState
{
    /// <summary>정지</summary>
    Stopped = 0,
    /// <summary>예열</summary>
    Preheat = 1,
    /// <summary>건조</summary>
    Drying = 2,
    /// <summary>냉각</summary>
    Cooling = 3,
    /// <summary>완료</summary>
    Done = 4,
    /// <summary>고장</summary>
    Fault = 9,
}

/// <summary>장비 경보 비트 (주소 40011, 여러 개가 함께 켜질 수 있음)</summary>
[Flags]
public enum DryerAlarm
{
    /// <summary>없음</summary>
    None = 0,
    /// <summary>과열</summary>
    OverTemp = 1,
    /// <summary>송풍기 과부하</summary>
    FanOverload = 2,
    /// <summary>문 열림</summary>
    DoorOpen = 4,
    /// <summary>온도 센서 이상</summary>
    SensorFault = 8,
}

/// <summary>건조기 값 한 묶음 (바꾸지 않는 객체).</summary>
public sealed record DryerData
{
    public static readonly DryerData Empty = new();

    public double ChamberTemp { get; init; }
    public double ExhaustTemp { get; init; }
    public double InletTemp { get; init; }
    public double Humidity { get; init; }
    public double HeaterPct { get; init; }
    public double FanPct { get; init; }
    public double DamperPct { get; init; }
    public double LevelPct { get; init; }
    public DryerState State { get; init; }
    public int StepRemainSec { get; init; }
    public DryerAlarm Alarms { get; init; }
    public double SetTemp { get; init; }
    public bool Running { get; init; }
    public bool HeaterOn { get; init; }
    public bool FanOn { get; init; }
    public bool DoorClosed { get; init; }
    public bool AutoMode { get; init; }

    /// <summary>StepBar 의 current (1부터, 0 = 시작 전)</summary>
    public int Step => State switch
    {
        DryerState.Preheat => 1, DryerState.Drying => 2, DryerState.Cooling => 3, DryerState.Done => 4, _ => 0,
    };
}
