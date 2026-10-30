using Devinno.UI;
using Devinno.UI.App;

namespace HotAirDryerHmi.Pages;

/// <summary>상단 고정 영역: 장비 이름, 페이지 탭, 통신 상태, 시계. 모든 페이지에서 보인다.</summary>
public sealed partial class TopBar : DvBar
{
    public string Clock => DateTime.Now.ToString("HH:mm:ss");
    public string Today => DateTime.Now.ToString("yyyy-MM-dd ddd", Texts.Culture);
    public string Subtitle => Texts.T("app.device", AppHub.DataMgr.ActiveRecipe ?? Texts.T("recipe.none"));

    public string CommText => Texts.T(AppHub.LogicMgr.CommLost ? "comm.lost" : AppHub.DataMgr.Setting.Simulation ? "comm.sim" : "comm.ok");
    public string CommState => AppHub.LogicMgr.CommLost ? Styles.State.Fault : Styles.State.Run;
    public bool CommBlink => AppHub.LogicMgr.CommLost;

    public int UnAcked => AppHub.DataMgr.Alarms.UnAckedCount;
    public bool HasUnAcked => UnAcked > 0;

    // 탭 강조 = App.CurrentPage 를 읽는 클래스 바인딩. 페이지를 더하면 같은 모양으로 늘린다
    public string MainClass => Tab<MainPage>();
    public string MonitorClass => Tab<MonitorPage>();
    public string RecipeClass => Tab<RecipePage>();
    public string ScheduleClass => Tab<SchedulePage>();
    public string AlarmClass => Tab<AlarmPage>();
    public string SettingClass => Tab<SettingPage>();

    public void ShowMain() => App.Show<MainPage>();
    public void ShowMonitor() => App.Show<MonitorPage>();
    public void ShowRecipe() => App.Show<RecipePage>();
    public void ShowSchedule() => App.Show<SchedulePage>();
    public void ShowAlarm() => App.Show<AlarmPage>();
    public void ShowSetting() => App.Show<SettingPage>();

    private string Tab<T>() where T : DvPage => App.CurrentPage is T ? "tab tab-on" : "tab";
}
