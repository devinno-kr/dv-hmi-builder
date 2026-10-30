using Devinno.UI.Alarms;
using Devinno.UI.App;

namespace HotAirDryerHmi.Pages;

/// <summary>하단 고정 영역: 가장 급한 미확인 알람 한 줄과 확인 버튼.</summary>
public sealed partial class AlarmBar : DvBar
{
    public IReadOnlyList<AlarmRow> Alarms => AppHub.DataMgr.Alarms.Rows;

    public void Ack(AlarmRow row) => AppHub.DataMgr.Alarms.Ack(row.Id);

    public void ShowAlarm() => App.Show<AlarmPage>();
}
