using Devinno.UI.Alarms;
using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Managers;

/// <summary>
/// 공정 로직: 감시 주기마다 통신 끊김·알람 판정, 고온 정지, 트렌드 기록, 예약 운전을 돌리고 화면을 갱신한다.
/// 장치에는 장치 모델의 명령으로만 접근한다. 화면 버튼은 이 클래스의 명령(StartRun 등)을 부른다.
/// </summary>
public class LogicManager
{
    private System.Threading.Timer? timer;
    private int ticking, applying;
    private DateTime lastTick;
    private DryerAlarm lastBits;
    private DryerState lastState;
    private bool warnOn, levelLowOn;

    private static Setting Setting => AppHub.DataMgr.Setting;
    private static DataManager Data => AppHub.DataMgr;
    private static Dryer Dryer => AppHub.DevMgr.Dryer;

    /// <summary>마지막 감시 주기 기준 통신 끊김</summary>
    public bool CommLost { get; private set; } = true;

    /// <summary>오늘 끝난 배치 수</summary>
    public int TodayBatches { get; private set; }

    /// <summary>오늘 운전한 시간</summary>
    public TimeSpan TodayRun { get; private set; }

    /// <summary>다음 예약 운전 (없으면 null)</summary>
    public (DateTime At, ScheduleItem Item)? NextSchedule { get; private set; }

    public void Start()
    {
        lastTick = DateTime.Now;   // 이 시각 이전에 지난 예약은 실행하지 않음 — 재시작 직후 저절로 켜지지 않게
        var ms = Setting.PollIntervalMs;
        timer = new System.Threading.Timer(_ => Tick(DateTime.Now), null, ms, ms);
    }

    public void Stop()
    {
        timer?.Dispose();
        timer = null;
    }

    /// <summary>감시 주기를 바꾼다 (설정 저장 뒤)</summary>
    public void ApplySetting(Setting s) => timer?.Change(s.PollIntervalMs, s.PollIntervalMs);

    // ---------------------------------------------------------------- 명령 (화면에서)

    /// <summary>운전 시작. 못 하면 이유를 돌려준다 (null = 보냄)</summary>
    public string? StartRun()
    {
        var d = Dryer.Data;
        if (CommLost) return Texts.T("why.commlost");
        if (d.Running) return Texts.T("why.running");
        if (!d.DoorClosed) return Texts.T("why.door");
        if (d.State == DryerState.Fault || d.Alarms != DryerAlarm.None) return Texts.T("why.alarm");
        if (Data.ActiveRecipe is null) return Texts.T("why.norecipe");
        Dryer.RunStart();
        return null;
    }

    /// <summary>
    /// 정지는 언제나 보낸다. 자동 운전 중 사람이 멈추면 수동으로 바꾼다 (예약이 다시 켜지 않게).
    /// 장비가 받았다는 응답이 없으면(통신 끊김이면 라이브러리가 버림) 이유를 돌려준다 — 정지가 조용히 실패하지 않게 화면이 알린다
    /// </summary>
    public async Task<string?> StopRun()
    {
        if (!await Dryer.RunStopAsync()) return Texts.T("why.stopnotsent");
        if (Dryer.Data.AutoMode) Dryer.SetAuto(false);
        return null;
    }

    public void ResetAlarm() => Dryer.Reset();

    /// <summary>설정 온도를 바꾼다. 장비가 받았다는 응답이 없으면 이유를 돌려준다 (null = 바뀜)</summary>
    public async Task<string?> SetTemp(double c)
    {
        if (CommLost) return Texts.T("why.cannotsend");
        if (c >= Setting.TempTripC) return Texts.T("why.overtrip", Setting.TempTripC);
        return await Dryer.SetTempAsync(c) ? null : Texts.T("why.notsent");
    }

    /// <summary>
    /// 레시피를 장비에 보낸다. 장비가 레시피와 설정 온도를 받았다고 응답해야 적용된 것으로 본다.
    /// 못 하면 이유를 돌려준다 (null = 적용됨)
    /// </summary>
    public async Task<string?> ApplyRecipe(Recipe r)
    {
        if (CommLost) return Texts.T("why.cannotsend");
        if (Dryer.Data.Running) return Texts.T("why.runningrecipe");
        var errors = r.Validate(Setting.TempTripC);
        if (errors.Count > 0) return errors[0];
        if (Interlocked.Exchange(ref applying, 1) == 1) return Texts.T("why.applying");   // 화면과 예약이 동시에 보내지 않게
        try
        {
            if (!await Dryer.WriteRecipeAsync(r) || !await Dryer.SetTempAsync(r.Dry.TempC)) return Texts.T("why.notapplied");
            Data.ActiveRecipe = r.Name;
            return null;
        }
        finally { Volatile.Write(ref applying, 0); }
    }

    // ---------------------------------------------------------------- 감시 주기

    internal void Tick(DateTime now)
    {
        if (Interlocked.Exchange(ref ticking, 1) == 1) return;   // 앞 주기가 아직 돌고 있으면 건너뜀
        try
        {
            var d = Dryer.Data;
            var lost = Dryer.IsCommLost(now, TimeSpan.FromSeconds(Setting.CommTimeoutSec));
            if (lost && !CommLost) Data.Alarms.Raise(AlarmPriority.Major, Texts.T("alm.commlost"), Texts.T("src.comm"));
            CommLost = lost;

            if (!lost)   // 판정은 통신이 될 때만 — 끊긴 동안의 옛 값으로 알람·정지를 내지 않는다
            {
                Judge(d);
                Record(now, d);
            }
            else Record(now, null);

            if (now.Date != lastTick.Date) { TodayBatches = 0; TodayRun = TimeSpan.Zero; }
            if (!lost && d.Running) TodayRun += now - lastTick;
            RunSchedule(now, d, lost);
            lastTick = now;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[tick] {ex}"); }   // 타이머 스레드의 예외는 프로그램을 끝낸다 — 잡고 다음 주기에 다시 돈다
        finally
        {
            AppHub.App.Invalidate();
            Volatile.Write(ref ticking, 0);
        }
    }

    private void Judge(DryerData d)
    {
        // 장비 경보 비트 — 새로 켜진 비트만 알람
        var rising = d.Alarms & ~lastBits;
        if (rising.HasFlag(DryerAlarm.OverTemp)) Data.Alarms.Raise(AlarmPriority.Critical, Texts.T("alm.overtemp"), Texts.T("src.heater"));
        if (rising.HasFlag(DryerAlarm.FanOverload)) Data.Alarms.Raise(AlarmPriority.Major, Texts.T("alm.fan"), Texts.T("src.fan"));
        if (rising.HasFlag(DryerAlarm.DoorOpen)) Data.Alarms.Raise(AlarmPriority.Minor, Texts.T("alm.door"), Texts.T("src.hopper"));
        if (rising.HasFlag(DryerAlarm.SensorFault)) Data.Alarms.Raise(AlarmPriority.Major, Texts.T("alm.sensor"), Texts.T("src.sensor"));
        lastBits = d.Alarms;

        // 앱의 기준값 — 고온 정지는 앱도 한 번 더 지킨다 (2℃ 히스테리시스)
        if (d.ChamberTemp >= Setting.TempTripC && d.Running)
        {
            Dryer.RunStop();
            Data.Alarms.Raise(AlarmPriority.Critical, Texts.T("alm.trip", d.ChamberTemp, Setting.TempTripC), Texts.T("src.heater"));
        }
        if (!warnOn && d.ChamberTemp >= Setting.TempWarnC)
        {
            warnOn = true;
            Data.Alarms.Raise(AlarmPriority.Major, Texts.T("alm.warn", d.ChamberTemp), Texts.T("src.heater"));
        }
        else if (warnOn && d.ChamberTemp < Setting.TempWarnC - 2) warnOn = false;

        if (!levelLowOn && d.LevelPct < Setting.LevelLowPct)
        {
            levelLowOn = true;
            Data.Alarms.Raise(AlarmPriority.Minor, Texts.T("alm.level", d.LevelPct), Texts.T("src.hopper"));
        }
        else if (levelLowOn && d.LevelPct > Setting.LevelLowPct + 5) levelLowOn = false;

        if (d.State == DryerState.Done && lastState != DryerState.Done) TodayBatches++;
        lastState = d.State;
    }

    private static void Record(DateTime now, DryerData? d)
    {
        var t = Data.Trend;
        t.Add(TrendChannels.Chamber, now, d?.ChamberTemp ?? double.NaN);   // 끊긴 동안은 NaN — 선이 끊겨 보인다
        t.Add(TrendChannels.SetTemp, now, d?.SetTemp ?? double.NaN);
        t.Add(TrendChannels.Exhaust, now, d?.ExhaustTemp ?? double.NaN);
        t.Add(TrendChannels.Inlet, now, d?.InletTemp ?? double.NaN);
        t.Add(TrendChannels.Humidity, now, d?.Humidity ?? double.NaN);
        t.Add(TrendChannels.Heater, now, d?.HeaterPct ?? double.NaN);
    }

    // 예약은 시작 시각이 "지나간 순간"에만 동작한다. 자동 모드·예약 사용일 때만. 통신이 끊겼거나 문이 열려 있으면 시작하지 않고 알린다
    private void RunSchedule(DateTime now, DryerData d, bool lost)
    {
        var items = Data.Schedules;
        NextSchedule = items.Select(i => (At: i.Next(now), Item: i)).Where(x => x.At is not null)
            .OrderBy(x => x.At).Select(x => ((DateTime, ScheduleItem)?)(x.At!.Value, x.Item)).FirstOrDefault();
        if (!Setting.ScheduleEnabled || !d.AutoMode) return;
        foreach (var item in items.Where(i => i.PassedBetween(lastTick, now)))
        {
            var recipe = Data.FindRecipe(item.RecipeName);
            var why = recipe is null ? Texts.T("alm.norecipe", item.RecipeName) : lost ? Texts.T("comm.lost") : d.Running ? Texts.T("why.running") : null;
            if (why is not null) ScheduleFailed(item, why);
            else _ = StartScheduled(item, recipe!);
        }
    }

    // 예약 시작: 레시피가 적용됐다는 응답을 받은 뒤에만 시작한다. 부른 쪽이 Task 를 버리므로 예외는 안에서 잡는다
    private async Task StartScheduled(ScheduleItem item, Recipe recipe)
    {
        string? why;
        try { why = await ApplyRecipe(recipe) ?? StartRun(); }
        catch (Exception ex) { why = ex.Message; }
        if (why is not null) ScheduleFailed(item, why);
        AppHub.App.Invalidate();
    }

    private static void ScheduleFailed(ScheduleItem item, string why) =>
        Data.Alarms.Raise(AlarmPriority.Minor, Texts.T("alm.schedule", item.Start.ToString("HH:mm"), why), Texts.T("src.schedule"));
}
