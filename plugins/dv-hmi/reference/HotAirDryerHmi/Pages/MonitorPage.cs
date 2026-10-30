using Devinno.UI.App;
using Devinno.UI.Trends;
using HotAirDryerHmi.Managers;
using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Pages;

/// <summary>모니터링: 계기(게이지·링·탱크)와 온도 트렌드, 최근 추이 스파크라인.</summary>
public sealed partial class MonitorPage : DvPage
{
    private static DryerData D => AppHub.DevMgr.Dryer.Data;
    private static bool Lost => AppHub.LogicMgr.CommLost;
    private static Setting S => AppHub.DataMgr.Setting;

    public double Chamber => Lost ? 0 : D.ChamberTemp;
    public double Inlet => Lost ? 0 : D.InletTemp;
    public double Exhaust => Lost ? 0 : D.ExhaustTemp;
    public double Heater => Lost ? 0 : D.HeaterPct;
    public double Fan => Lost ? 0 : D.FanPct;
    public double Damper => Lost ? 0 : D.DamperPct;
    public double Level => Lost ? 0 : D.LevelPct;
    public double Humidity => Lost ? 0 : D.Humidity;
    public string SetText => Lost ? Texts.T("comm.lost") : Texts.T("mon.setinfo", D.SetTemp, S.TempWarnC);

    // 구간 색은 설정값에서 만든다 (from < to, 오름차순이어야 함 — Setting.Validate 가 보장)
    public string TempZones => $"0:{S.TempWarnC:0}:token(tone-success) {S.TempWarnC:0}:{S.TempTripC:0}:token(tone-warning) {S.TempTripC:0}:220:token(tone-danger)";
    public string LevelZones => $"0:{S.LevelLowPct:0}:token(tone-danger) {S.LevelLowPct:0}:100:token(tone-info)";

    // 트렌드
    public TrendBuffer Trend => AppHub.DataMgr.Trend;
    public int WindowSec { get; set; } = 120;

    public IReadOnlyList<double> HumidityRecent => Recent(TrendChannels.Humidity);
    public IReadOnlyList<double> HeaterRecent => Recent(TrendChannels.Heater);
    public IReadOnlyList<double> ExhaustRecent => Recent(TrendChannels.Exhaust);
    public string HumidityText => Lost ? "--" : $"{D.Humidity:0.0}";
    public string HeaterText => Lost ? "--" : $"{D.HeaterPct:0}";
    public string ExhaustText => Lost ? "--" : $"{D.ExhaustTemp:0.0}";

    private IReadOnlyList<double> Recent(string ch) =>
        [.. Trend.Snapshot(ch).TakeLast(120).Select(p => p.Value).Where(v => !double.IsNaN(v))];
}
