using Devinno.UI.App;
using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Pages;

/// <summary>예약 운전: 표에서 요일·시각·레시피를 바로 고치고 저장한다. 자동 모드일 때 LogicManager 가 실행한다.</summary>
public sealed partial class SchedulePage : DvPage
{
    public SchedulePage() => Items = Copy(AppHub.DataMgr.Schedules);

    /// <summary>편집 중인 복사본. 저장하면 원본과 바꾼다</summary>
    public List<ScheduleItem> Items { get; private set; }

    public List<string> RecipeNames => [.. AppHub.DataMgr.Recipes.Select(r => r.Name)];

    public bool Enabled
    {
        get => AppHub.DataMgr.Setting.ScheduleEnabled;
        set
        {
            var s = AppHub.DataMgr.Setting.Clone();
            s.ScheduleEnabled = value;
            AppHub.DataMgr.SaveSetting(s);
        }
    }

    public bool AutoMode => AppHub.DevMgr.Dryer.Data.AutoMode;
    public string ModeText => Texts.T(AutoMode ? "sch.auto" : "sch.manual");
    public string ModeTone => AutoMode ? "success" : "warning";

    // 다음 예약 (저장된 예약 기준)
    private (DateTime At, ScheduleItem Item)? Next => AppHub.LogicMgr.NextSchedule;
    public string NextTime => Next is { } n ? n.At.ToString("HH:mm") : "--:--";
    public string NextDay => Next is { } n ? n.At.ToString(Texts.T("fmt.nextday"), Texts.Culture) : Texts.T("sch.none");
    public string NextRecipe => Next?.Item.RecipeName ?? "-";
    public string NextIn => Next is { } n ? Texts.T("sch.in", (int)(n.At - DateTime.Now).TotalHours, (n.At - DateTime.Now).Minutes) : "";

    /// <summary>요일별 예약 수 (이번 주 차트)</summary>
    public List<DayCount> Week =>
    [
        .. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }
            .Select(d => new DayCount(Texts.T("day." + d.ToString()[..3].ToLowerInvariant()), Items.Count(i => i.Enabled && i.On(d)))),
    ];

    public void Add()
    {
        var id = Items.Count == 0 ? 1 : Items.Max(i => i.Id) + 1;
        Items = [.. Items, new ScheduleItem { Id = id, RecipeName = RecipeNames.FirstOrDefault() ?? "" }];
    }

    public async void Remove(ScheduleItem row)
    {
        if (await App.MessageBox(Texts.T("sch.deleteq", row.Start.ToString("HH:mm")), Texts.T("sch.deletetitle"), DvMessageButtons.YesNo) == DvDialogResult.Yes)
            Items = [.. Items.Where(i => i != row)];
    }

    public void Revert() => Items = Copy(AppHub.DataMgr.Schedules);

    public async void Save()
    {
        var missing = Items.Where(i => AppHub.DataMgr.FindRecipe(i.RecipeName) is null).Select(i => i.Start.ToString("HH:mm")).ToList();
        if (missing.Count > 0) { await App.MessageBox(Texts.T("sch.missing", string.Join(", ", missing)), Texts.T("dlg.cannotsave"), DvMessageButtons.Ok); return; }
        AppHub.DataMgr.SaveSchedules(Copy(Items));
        await App.MessageBox(Texts.T("sch.saved"), Texts.T("nav.schedule"), DvMessageButtons.Ok);
    }

    private static List<ScheduleItem> Copy(List<ScheduleItem> items) => [.. items.Select(i => i.Clone())];
}

public sealed record DayCount(string Day, int Count);
