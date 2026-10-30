using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Tests;

/// <summary>
/// 예약 운전 시각: 시작 시각이 "지나간 순간"에만 동작한다 (ScheduleItem.PassedBetween).
/// 시각을 직접 넣는다 — 실제 시계·감시 타이머를 쓰지 않는다. 2026-01-05 는 월요일
/// </summary>
public class ScheduleTests
{
    static readonly ScheduleItem Weekday8 = new() { Start = new TimeOnly(8, 0) };   // 월~금 08:00

    static DateTime At(int day, int h, int m, int s) => new(2026, 1, day, h, m, s);

    [Fact(DisplayName = "월 08:00 예약, 감시 07:59:59 → 08:00:01 → 시작")]
    public void PassesOnTime() =>
        Assert.True(Weekday8.PassedBetween(At(5, 7, 59, 59), At(5, 8, 0, 1)));

    [Fact(DisplayName = "월 08:00 예약, 앱을 08:05 에 켬 → 시작 안 함 (지난 예약은 실행하지 않음)")]
    public void NotAfterRestart() =>
        Assert.False(Weekday8.PassedBetween(At(5, 8, 5, 0), At(5, 8, 5, 1)));

    [Fact(DisplayName = "토 08:00 (토요일 꺼짐) → 시작 안 함")]
    public void NotOnSaturday() =>
        Assert.False(Weekday8.PassedBetween(At(10, 7, 59, 59), At(10, 8, 0, 1)));

    [Fact(DisplayName = "감시 간격이 1분 넘게 비면 → 시작 안 함")]
    public void NotAfterLongGap() =>
        Assert.False(Weekday8.PassedBetween(At(5, 7, 58, 0), At(5, 8, 0, 1)));

    [Fact(DisplayName = "월 00:00 예약, 감시 일 23:59:59 → 월 00:00:01 → 시작 (자정을 넘는 감시)")]
    public void AcrossMidnight() =>
        Assert.True(new ScheduleItem { Start = new TimeOnly(0, 0) }.PassedBetween(At(4, 23, 59, 59), At(5, 0, 0, 1)));

    [Fact(DisplayName = "사용 안 함 → 시작 안 함")]
    public void NotWhenDisabled() =>
        Assert.False(new ScheduleItem { Start = new TimeOnly(8, 0), Enabled = false }.PassedBetween(At(5, 7, 59, 59), At(5, 8, 0, 1)));
}
