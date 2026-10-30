using Devinno.UI;
using Devinno.UI.App;
using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Pages;

/// <summary>
/// 운전 화면 (시작 페이지). VM 속성은 장치 모델·매니저를 읽는 계산 속성으로 쓰고(값을 복사해 두지 않음),
/// 버튼 처리기는 한 줄로 매니저를 부른다.
/// </summary>
public sealed partial class MainPage : DvPage
{
    private static DryerData D => AppHub.DevMgr.Dryer.Data;
    private static bool Lost => AppHub.LogicMgr.CommLost;
    private static string V(double v, string f = "0.0") => Lost ? "--" : v.ToString(f);

    // KPI
    public string ChamberText => V(D.ChamberTemp);
    public string SetDelta => Lost ? Texts.T("comm.lost") : Texts.T("main.set", D.SetTemp);
    public string TempTone => Lost ? Styles.Tone.Neutral
        : D.ChamberTemp >= AppHub.DataMgr.Setting.TempWarnC ? Styles.Tone.Danger
        : D.Running ? Styles.Tone.Warning : Styles.Tone.Info;
    public string HumidityText => V(D.Humidity);
    public string HumDelta => Lost ? "-" : Texts.T(!D.Running ? "hum.idle" : D.Humidity <= 10 ? "hum.good" : "hum.drying");
    public string HeaterText => V(D.HeaterPct, "0");
    public string HeaterDelta => Lost ? "-" : Texts.T("main.inlet", D.InletTemp);
    public string BatchText => AppHub.LogicMgr.TodayBatches.ToString();
    public string RunTimeText => Texts.T("main.runtime", AppHub.LogicMgr.TodayRun.ToString(@"hh\:mm"));

    // 공정도
    public bool FanOn => !Lost && D.FanOn;
    public bool HeaterOn => !Lost && D.HeaterOn;
    public bool DamperOpen => !Lost && D.DamperPct > 15;
    public double Level => Lost ? 0 : D.LevelPct;
    public bool Flowing => !Lost && D.Running;
    public string ChamberShort => $"{V(D.ChamberTemp)}℃";
    public string HeaterShort => $"{V(D.HeaterPct, "0")}%";
    public string HumidityShort => $"{V(D.Humidity)} %RH";
    public string LevelShort => Texts.T("proc.material", V(D.LevelPct, "0"));
    public string RecipeBadge => AppHub.DataMgr.ActiveRecipe ?? Texts.T("recipe.none");

    // 건조 단계
    public string Steps => Texts.T("main.steplist");
    public int Step => Lost ? 0 : D.Step;
    public string StateText => Texts.T(Lost ? "comm.lost" : D.State switch
    {
        DryerState.Preheat => "state.preheat", DryerState.Drying => "state.drying", DryerState.Cooling => "state.cooling",
        DryerState.Done => "state.done", DryerState.Fault => "state.fault", _ => "state.stopped",
    });
    public string StateTone => Lost ? Styles.Tone.Neutral : D.State switch
    {
        DryerState.Fault => Styles.Tone.Danger,
        DryerState.Done => Styles.Tone.Success,
        DryerState.Stopped => Styles.Tone.Neutral,
        _ => Styles.Tone.Warning,
    };
    public string RemainText => D.Running && !Lost ? $"{D.StepRemainSec / 60:00}:{D.StepRemainSec % 60:00}" : "--:--";
    public double BatchProgress
    {
        get
        {
            var r = AppHub.DataMgr.FindRecipe(AppHub.DataMgr.ActiveRecipe);
            if (r is null || Lost) return 0;
            if (D.State == DryerState.Done) return 100;
            if (!D.Running) return 0;
            var done = r.Steps.Take(D.Step - 1).Sum(s => s.Minutes) + (r.Steps[D.Step - 1].Minutes - D.StepRemainSec / 60.0);
            return Math.Clamp(done / r.TotalMinutes * 100, 0, 100);
        }
    }

    // 운전
    public string Mode
    {
        get => D.AutoMode ? "auto" : "manual";
        set => AppHub.DevMgr.Dryer.SetAuto(value == "auto");
    }
    public bool CannotOperate => Lost;

    public async void Start()
    {
        if (AppHub.LogicMgr.StartRun() is { } why)
            await App.MessageBox(why, Texts.T("dlg.start"), DvMessageButtons.Ok);
    }

    public async void Stop()
    {
        if (await AppHub.LogicMgr.StopRun() is { } why)
            await App.MessageBox(why, Texts.T("dlg.stop"), DvMessageButtons.Ok);
    }
    public void Reset() => AppHub.LogicMgr.ResetAlarm();

    public async void EditSetTemp()
    {
        if (Lost) return;
        var trip = AppHub.DataMgr.Setting.TempTripC;
        if (await App.InputNumber(Texts.T("dlg.settemp"), Math.Clamp(D.SetTemp, 20, trip - 1), 20, trip - 1) is { } v
            && await AppHub.LogicMgr.SetTemp(v) is { } why)
            await App.MessageBox(why, Texts.T("dlg.settemp"), DvMessageButtons.Ok);
    }

    // 설비 상태 (state 키 = state-* 토큰)
    public string FanState => Lost ? Styles.State.Off : D.Alarms.HasFlag(DryerAlarm.FanOverload) ? Styles.State.Fault : D.FanOn ? Styles.State.Run : Styles.State.Stop;
    public string FanStatus => Lost ? "-" : D.FanOn ? $"{D.FanPct:0}%" : Texts.T("st.stopped");
    public string HeaterState => Lost ? Styles.State.Off : D.Alarms.HasFlag(DryerAlarm.OverTemp) ? Styles.State.Fault : D.HeaterOn ? Styles.State.Run : Styles.State.Stop;
    public string HeaterStatus => Lost ? "-" : D.HeaterOn ? $"{D.HeaterPct:0}%" : Texts.T("st.off");
    public string DamperState => Lost ? Styles.State.Off : DamperOpen ? Styles.State.Run : Styles.State.Stop;
    public string DamperStatus => Lost ? "-" : $"{D.DamperPct:0}%";
    public string DoorState => Lost ? Styles.State.Off : D.DoorClosed ? Styles.State.Run : Styles.State.Warn;
    public string DoorStatus => Lost ? "-" : Texts.T(D.DoorClosed ? "door.closed" : "door.open");
    public bool DoorBlink => !Lost && !D.DoorClosed;
    public string ModeState => Lost ? Styles.State.Off : D.AutoMode ? Styles.State.Run : Styles.State.Manual;
    public string ModeStatus => Lost ? "-" : Texts.T(D.AutoMode ? "mode.auto" : "mode.manual");
}
