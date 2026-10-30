using Devinno.UI.Alarms;
using Devinno.UI.App;

namespace HotAirDryerHmi.Pages;

/// <summary>알람: 목록과 확인(ACK), 원인별·등급별 통계.</summary>
public sealed partial class AlarmPage : DvPage
{
    private static AlarmLog Log => AppHub.DataMgr.Alarms;

    public IReadOnlyList<AlarmRow> Alarms => Log.Rows;
    public int UnAcked => Log.UnAckedCount;
    public int Total => Log.Rows.Count;
    public int Critical => Log.Rows.Count(r => r.Priority == AlarmPriority.Critical && !r.Acked);
    public string UnAckedTone => UnAcked > 0 ? "danger" : "success";
    public string UnAckedText => Texts.T("alm.unacked", UnAcked);
    public string TotalText => Texts.T("alm.total", Total);

    public List<CountItem> BySource =>
        [.. Log.Rows.GroupBy(r => r.Source ?? Texts.T("src.other")).Select(g => new CountItem(g.Key, g.Count())).OrderByDescending(c => c.Count).Take(6)];

    public List<CountItem> ByPriority =>
    [
        new(Texts.T("pri.critical"), Log.Rows.Count(r => r.Priority == AlarmPriority.Critical)),
        new(Texts.T("pri.major"), Log.Rows.Count(r => r.Priority == AlarmPriority.Major)),
        new(Texts.T("pri.minor"), Log.Rows.Count(r => r.Priority == AlarmPriority.Minor)),
    ];

    // 긴급 알람은 한 번 더 묻는다 (잘못 누름 방지)
    public async void Ack(AlarmRow row)
    {
        if (row.Priority == AlarmPriority.Critical &&
            await App.MessageBox(Texts.T("alm.ackq", row.Message), Texts.T("alm.acktitle"), DvMessageButtons.YesNo) != DvDialogResult.Yes)
            return;
        Log.Ack(row.Id);
    }

    public async void AckAll()
    {
        if (UnAcked == 0) return;
        if (await App.MessageBox(Texts.T("alm.ackallq", UnAcked), Texts.T("btn.ackall"), DvMessageButtons.YesNo) == DvDialogResult.Yes)
            Log.AckAll();
    }
}

public sealed record CountItem(string Name, int Count);
