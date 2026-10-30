using Devinno.UI;
using Devinno.UI.App;
using Devinno.UI.Styling;
using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Pages;

/// <summary>설정: 복사본을 고치고 저장할 때 검사한 뒤 원본과 바꾸고 장치·로직·화면에 적용한다.</summary>
public sealed partial class SettingPage : DvPage
{
    public SettingPage() => Edit = AppHub.DataMgr.Setting.Clone();

    // 페이지는 한 번 만들어 보관되므로, 보일 때마다 지금 설정에서 복사본을 새로 만든다.
    // 상단 바의 언어 전환처럼 다른 곳에서 바뀐 설정을 옛 복사본이 저장 때 덮어쓰지 않게 (저장하지 않고 떠난 편집은 버려진다)
    protected override void OnShown() => Edit = AppHub.DataMgr.Setting.Clone();

    public Setting Edit { get; private set; }

    public string Tab { get; set; } = "comm";

    public bool RealDevice => !Edit.Simulation;
    public string Language
    {
        get => Edit.Language;
        set => Edit.Language = value;
    }

    public string ThemeMode
    {
        get => Edit.DarkTheme ? "dark" : "light";
        set => Edit.DarkTheme = value == "dark";
    }

    // 통신 진단 (지금 적용된 설정 기준)
    private static Dryer Dryer => AppHub.DevMgr.Dryer;
    public string CommState => AppHub.LogicMgr.CommLost ? Styles.State.Fault : Styles.State.Run;
    public string CommStatus => Texts.T(AppHub.LogicMgr.CommLost ? "diag.lost" : "diag.ok");
    public string Target => AppHub.DataMgr.Setting.Simulation ? $"127.0.0.1:{Managers.DeviceManager.SimPort} {Texts.T("diag.simsuffix")}" : $"{AppHub.DataMgr.Setting.DryerIp}:{AppHub.DataMgr.Setting.DryerPort}";
    public string LastReceived => Dryer.LastReceived == DateTime.MinValue ? "-" : Dryer.LastReceived.ToString("HH:mm:ss.f");
    public string LastError => Dryer.LastError ?? Texts.T("diag.none");
    public string SimState => AppHub.DevMgr.Simulator is null ? Styles.State.Off : Styles.State.Run;
    public string SimStatus => Texts.T(AppHub.DevMgr.Simulator is null ? "diag.off" : "diag.running");
    public string ConfigPath => Managers.DataManager.SettingPath;

    public void Revert() => Edit = AppHub.DataMgr.Setting.Clone();

    public async void Save()
    {
        var errors = Edit.Validate();
        if (errors.Count > 0) { await App.MessageBox(string.Join("\n", errors), Texts.T("dlg.cannotsave"), DvMessageButtons.Ok); return; }
        var old = AppHub.DataMgr.Setting;
        var commChanged = old.Simulation != Edit.Simulation || old.DryerIp != Edit.DryerIp || old.DryerPort != Edit.DryerPort;
        if (commChanged && await App.MessageBox(Texts.T("set.reconnectq"), Texts.T("set.savetitle"), DvMessageButtons.YesNo) != DvDialogResult.Yes)
            return;

        var s = Edit.Clone();
        AppHub.DataMgr.SaveSetting(s);
        if (commChanged) await Task.Run(() => AppHub.DevMgr.ApplySetting(s));   // 통신 재시작은 화면 스레드 밖에서
        AppHub.LogicMgr.ApplySetting(s);
        App.Theme = s.DarkTheme ? DvTheme.Dark : DvTheme.Light;
        Texts.SetLanguage(s.Language);   // 화면이 다시 그려짐
        await App.MessageBox(Texts.T("set.saved"), Texts.T("set.title"), DvMessageButtons.Ok);
    }
}
