using HotAirDryerHmi.Models;
using HotAirDryerHmi.Simulation;

namespace HotAirDryerHmi.Managers;

/// <summary>
/// 장치를 만들고 통신을 시작·정지·해제한다. 공정 로직은 LogicManager 가 한다.
/// 설정이 시뮬레이션이면 가짜 건조기를 먼저 띄우고, 건조기는 실제 장비와 똑같이 Modbus TCP 로 거기에 붙는다.
/// </summary>
public class DeviceManager : IDisposable
{
    public DeviceManager(Setting setting)
    {
        Dryer = new Dryer(Target(setting));
        if (setting.Simulation) Simulator = new DryerSimulator(SimPort);
    }

    /// <summary>시뮬레이션에서 가짜 건조기가 여는 포트 (127.0.0.1)</summary>
    public const int SimPort = 1502;

    public Dryer Dryer { get; }

    /// <summary>시뮬레이션일 때만 있음</summary>
    public DryerSimulator? Simulator { get; private set; }

    public void Start()
    {
        Simulator?.Start();
        Dryer.Start();
    }

    public void Stop()
    {
        Dryer.Stop();
        Simulator?.Stop();
    }

    public void Dispose()
    {
        Dryer.Dispose();
        Simulator?.Dispose();
    }

    /// <summary>새 통신 설정을 적용한다. 시뮬레이션을 켜고 끄는 것도 여기서.</summary>
    public void ApplySetting(Setting setting)
    {
        if (setting.Simulation && Simulator is null)
        {
            Simulator = new DryerSimulator(SimPort);
            Simulator.Start();
        }
        else if (!setting.Simulation && Simulator is not null)
        {
            Simulator.Dispose();
            Simulator = null;
        }
        Dryer.ApplySetting(Target(setting));
    }

    // 시뮬레이션이면 접속 대상을 가짜 건조기로 바꾼 복사본
    private static Setting Target(Setting setting)
    {
        if (!setting.Simulation) return setting;
        var s = setting.Clone();
        s.DryerIp = "127.0.0.1";
        s.DryerPort = SimPort;
        return s;
    }
}
