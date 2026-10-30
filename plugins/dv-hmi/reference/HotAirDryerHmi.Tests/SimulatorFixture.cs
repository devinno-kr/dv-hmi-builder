using System.Net;
using System.Net.Sockets;
using HotAirDryerHmi.Simulation;

namespace HotAirDryerHmi.Tests;

/// <summary>
/// 시뮬레이터 공용 준비·정리: 장비마다 하나를 시험 전체가 함께 쓴다 — 테스트 전에 켜고 전부 끝나면 끈다 (xUnit collection fixture — --filter 로 일부만 돌려도 켜고 끈다).
/// 장비를 건드리는 시험은 모두 이 collection 에 있어 한 번에 하나씩 차례로 돈다 (xunit.runner.json 이 collection 병렬을 끈다).
/// 상태를 바꾸는 시험은 Simulator.Preserve() 로 되돌리고, 통신 끊김은 Simulator.Silent 로 만든다 — 시험이 자기 시뮬레이터를 따로 만들지 않는다.
/// 시작 확인: 127.0.0.1 의 빈 포트에서 켜졌고 접속에 응한다. 마지막 확인: 끈 뒤 그 포트가 더 이상 접속을 받지 않는다. 어긋나면 던진다.
/// COM 포트는 열지 않는다 — 실제 장비에 붙지 않는다.
/// </summary>
public sealed class SimulatorFixture : IAsyncLifetime
{
    public const string Host = "127.0.0.1";

    public int Port { get; private set; }
    internal DryerSimulator Simulator { get; private set; } = null!;

    /// <summary>시작 확인을 통과한 접속 대상 (보이는 테스트가 이것을 확인한다)</summary>
    public string? StartCheck { get; private set; }

    /// <summary>127.0.0.1 의 빈 포트 하나 (잡았다가 놓는다)</summary>
    public static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        try { return ((IPEndPoint)l.LocalEndpoint).Port; }
        finally { l.Stop(); }
    }

    /// <summary>127.0.0.1:port 에 접속이 되는지</summary>
    public static bool Accepts(int port)
    {
        try
        {
            using var c = new TcpClient();
            return c.ConnectAsync(IPAddress.Loopback, port).Wait(1000) && c.Connected;
        }
        catch { return false; }
    }

    public Task InitializeAsync()
    {
        Port = FreePort();
        Simulator = new DryerSimulator(Port);
        Simulator.Start();
        if (!Wait.Until(() => Accepts(Port)))
            throw new InvalidOperationException($"시뮬레이터가 {Host}:{Port} 에서 접속을 받지 않는다");
        StartCheck = $"{Host}:{Port}";
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Simulator.Dispose();
        if (!Wait.Until(() => !Accepts(Port)))
            throw new InvalidOperationException($"시뮬레이터를 껐는데 {Host}:{Port} 가 아직 접속을 받는다");
        return Task.CompletedTask;
    }
}

[CollectionDefinition(Name)]
public class SimulatorCollection : ICollectionFixture<SimulatorFixture>
{
    public const string Name = "시뮬레이터";
}

/// <summary>통신은 비동기 — 고정 대기 대신 50 ms 마다 확인하고, 조건이 되면 바로 돌아온다 (기한은 넉넉히 10 초)</summary>
public static class Wait
{
    public static bool Until(Func<bool> condition, int timeoutMs = 10000)
    {
        var end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < end)
        {
            if (condition()) return true;
            Thread.Sleep(50);
        }
        return condition();
    }
}
