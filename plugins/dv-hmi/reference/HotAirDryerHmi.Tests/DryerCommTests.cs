using HotAirDryerHmi.Models;
using HotAirDryerHmi.Simulation;

namespace HotAirDryerHmi.Tests;

/// <summary>
/// 건조기 통신: 앱의 Dryer 가 실제 통신 경로(Modbus TCP, 127.0.0.1)로 시뮬레이터에 붙어 값을 읽고 명령을 보낸다.
/// 시뮬레이터는 SimulatorFixture 가 하나만 켜고 끈다 (시험 전체가 공유). 상태를 바꾸는 시험은 Preserve() 로 되돌린다. 실제 장비·COM 포트는 쓰지 않는다.
/// </summary>
[Collection(SimulatorCollection.Name)]
public class DryerCommTests(SimulatorFixture sim)
{
    static Setting To(int port) => new() { Simulation = false, DryerIp = SimulatorFixture.Host, DryerPort = port };

    // 연결되고 읽기 목록(레지스터·코일)을 모두 한 번 받을 때까지 — 그 전에는 쓰기가 NotConnected 로 실패하고, 끊김 판정이 참이다
    static bool Received(Dryer dryer) => Wait.Until(() => dryer.LastReceived > DateTime.MinValue);

    [Fact(DisplayName = "시뮬레이터: 127.0.0.1 의 빈 포트에서 켜지고 응답한다")]
    public void SimulatorStartedOnLoopback()
    {
        Assert.Equal($"{SimulatorFixture.Host}:{sim.Port}", sim.StartCheck);   // 준비 단계의 확인을 통과했다
        Assert.True(sim.Port > 0);
        Assert.True(SimulatorFixture.Accepts(sim.Port));
    }

    [Fact(DisplayName = "시뮬레이터는 장비마다 하나만 만들어진다 (시험 전체가 공유)")]
    public void OneSimulatorPerDevice() => Assert.Equal(1, DryerSimulator.Instances);

    [Fact(DisplayName = "시뮬레이터 챔버 온도 165.2 ℃ → 통신으로 읽은 앱 값 165.2 ℃")]
    public void DecodeOverRealPath()
    {
        using var restore = sim.Simulator.Preserve();
        sim.Simulator.SetChamberTemp(165.2);
        using var dryer = new Dryer(To(sim.Port));
        dryer.Start();
        Assert.True(Received(dryer), $"처음 값을 못 받음: {dryer.LastError}");
        // 시뮬레이터는 ±0.2 잡음을 섞어 내보낸다
        Assert.True(Wait.Until(() => Math.Abs(dryer.Data.ChamberTemp - 165.2) <= 0.5),
            $"챔버 온도 {dryer.Data.ChamberTemp}, 마지막 오류 {dryer.LastError}");
        Assert.False(dryer.IsCommLost(DateTime.Now, TimeSpan.FromSeconds(2)));
    }

    [Fact(DisplayName = "앱이 설정 온도 80.0 ℃ 를 씀 → 시뮬레이터 Holding 100 = 800")]
    public async Task SetTempReachesSimulator()
    {
        using var restore = sim.Simulator.Preserve();
        using var dryer = new Dryer(To(sim.Port));
        dryer.Start();
        Assert.True(Received(dryer), $"처음 값을 못 받음: {dryer.LastError}");
        Assert.True(await dryer.SetTempAsync(80.0), $"쓰기 실패: {dryer.LastError}");
        Assert.True(Wait.Until(() => sim.Simulator.Holding(100) == 800), $"Holding 100 = {sim.Simulator.Holding(100)}");
    }

    [Fact(DisplayName = "앱이 레시피(70/5, 80/30, 40/5, 팬 60) 를 씀 → 시뮬레이터 Holding 110~116 = 700,5,800,30,400,5,600")]
    public async Task RecipeReachesSimulator()
    {
        using var restore = sim.Simulator.Preserve();
        using var dryer = new Dryer(To(sim.Port));
        dryer.Start();
        Assert.True(Received(dryer), $"처음 값을 못 받음: {dryer.LastError}");
        var r = Recipe.Create("시험", "시험 재료", 70, 5, 80, 30, 40, 5, 60);
        Assert.True(await dryer.WriteRecipeAsync(r), $"쓰기 실패: {dryer.LastError}");
        int[] want = [700, 5, 800, 30, 400, 5, 600];
        Assert.True(Wait.Until(() => Enumerable.Range(0, 7).All(i => sim.Simulator.Holding(110 + i) == want[i])));
    }

    [Fact(DisplayName = "앱이 시작 명령 → 시뮬레이터 예열 상태(1)·앱 값 Preheat, 정지 명령 → 정지(0)")]
    public async Task StartThenStopCommand()
    {
        using var restore = sim.Simulator.Preserve();
        using var dryer = new Dryer(To(sim.Port));
        dryer.Start();
        Assert.True(Wait.Until(() => dryer.Data.DoorClosed), "문 닫힘 값을 못 받음");   // 문이 열려 있으면 시작하지 않는다
        dryer.RunStart();
        Assert.True(Wait.Until(() => sim.Simulator.Holding(8) == 1 && dryer.Data.State == DryerState.Preheat),
            $"시뮬레이터 상태 {sim.Simulator.Holding(8)}, 앱 상태 {dryer.Data.State}");
        Assert.True(await dryer.RunStopAsync(), $"정지 쓰기 실패: {dryer.LastError}");
        Assert.True(Wait.Until(() => sim.Simulator.Holding(8) == 0 && dryer.Data.State == DryerState.Stopped));
    }

    [Fact(DisplayName = "시뮬레이터가 응답을 멈춤 → 마지막 수신 뒤 통신 끊김 시간이 지나면 통신 끊김, 정지 명령 쓰기는 실패, 응답을 되찾으면 다시 수신")]
    public async Task CommLostWhenSimulatorGoesSilent()
    {
        using var restore = sim.Simulator.Preserve();
        var setting = To(sim.Port);
        setting.CommTimeoutSec = 1;   // 앱의 통신 끊김 판정 시간을 시험에서 짧게 — 끊김이 판정되는지만 본다
        var timeout = TimeSpan.FromSeconds(setting.CommTimeoutSec);
        using var dryer = new Dryer(setting);
        dryer.Start();
        Assert.True(Wait.Until(() => dryer.LastReceived > DateTime.MinValue && !dryer.IsCommLost(DateTime.Now, timeout)),
            "처음 값을 못 받음");

        sim.Simulator.Silent = true;   // 공용 시뮬레이터가 답을 멈춘다 (끄거나 새로 만들지 않는다)
        try
        {
            Assert.True(Wait.Until(() => dryer.IsCommLost(DateTime.Now, timeout)), "통신 끊김으로 판정하지 않음");
            Assert.False(await dryer.RunStopAsync());
        }
        finally { sim.Simulator.Silent = false; }

        Assert.True(Wait.Until(() => !dryer.IsCommLost(DateTime.Now, timeout)), "응답을 되찾았는데 다시 받지 못함");
    }
}
