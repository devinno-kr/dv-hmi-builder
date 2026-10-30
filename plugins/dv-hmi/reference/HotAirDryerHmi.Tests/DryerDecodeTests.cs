using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Tests;

/// <summary>
/// 건조기 값 해석: 받은 레지스터·코일 그대로를 Dryer.Parse 에 넣는다 (통신 없음).
/// 주소·배율은 메모리 맵 design/devices.json 과 같아야 한다 — 테스트 이름에 원시값과 기대값을 적어 사람이 맞는지 볼 수 있게.
/// </summary>
public class DryerDecodeTests
{
    // 40001~40012 (12개), 00001~00005 (5개)
    static int[] Regs(int index = -1, int value = 0)
    {
        var r = new int[12];
        if (index >= 0) r[index] = value;
        return r;
    }

    static readonly bool[] NoCoils = new bool[5];

    [Fact(DisplayName = "챔버 온도 40001 = 1652 → 165.2 ℃")]
    public void ChamberTempScale() =>
        Assert.Equal(165.2, Dryer.Parse(Regs(0, 1652), NoCoils).ChamberTemp, 3);

    [Fact(DisplayName = "챔버 온도 40001 = 65516 (부호 있는 −20) → −2.0 ℃")]
    public void ChamberTempSigned() =>
        Assert.Equal(-2.0, Dryer.Parse(Regs(0, 65516), NoCoils).ChamberTemp, 3);

    [Fact(DisplayName = "습도 40004 = 455 → 45.5 %RH")]
    public void HumidityScale() =>
        Assert.Equal(45.5, Dryer.Parse(Regs(3, 455), NoCoils).Humidity, 3);

    [Fact(DisplayName = "경보 비트 40011 = 5 → 과열 + 문 열림")]
    public void AlarmBits() =>
        Assert.Equal(DryerAlarm.OverTemp | DryerAlarm.DoorOpen, Dryer.Parse(Regs(10, 5), NoCoils).Alarms);

    [Theory(DisplayName = "상태 코드 40009 → 상태와 단계 표시")]
    [InlineData(0, DryerState.Stopped, 0)]
    [InlineData(1, DryerState.Preheat, 1)]
    [InlineData(2, DryerState.Drying, 2)]
    [InlineData(3, DryerState.Cooling, 3)]
    [InlineData(4, DryerState.Done, 4)]
    [InlineData(9, DryerState.Fault, 0)]
    public void StateCode(int code, DryerState state, int step)
    {
        var d = Dryer.Parse(Regs(8, code), NoCoils);
        Assert.Equal(state, d.State);
        Assert.Equal(step, d.Step);
    }

    [Fact(DisplayName = "코일 00001 켜짐 → 운전 중, 00004 꺼짐 → 문 열림")]
    public void Coils()
    {
        var d = Dryer.Parse(Regs(), [true, false, false, false, false]);
        Assert.True(d.Running);
        Assert.False(d.DoorClosed);
    }
}
