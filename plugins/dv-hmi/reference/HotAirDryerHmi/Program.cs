using Devinno.UI.App;
using Devinno.UI.OpenTK;
using Devinno.UI.Styling;
using HotAirDryerHmi.Managers;
using HotAirDryerHmi.Pages;

namespace HotAirDryerHmi;

// 조립 순서: 데이터 → 장치 → 로직 → 화면 → 창. 끝낼 때는 거꾸로
public static class Program
{
    static void Main()
    {
        try
        {
            AppHub.DataMgr = new DataManager();
        }
        catch (Exception ex)   // 설정 파일이 깨졌으면 기본값으로 대신하지 않고 알린 뒤 끝냄
        {
            Console.Error.WriteLine($"Could not read the settings file: {DataManager.SettingPath}\n{ex.Message}");
            Environment.ExitCode = 1;
            return;
        }
        AppHub.DevMgr = new DeviceManager(AppHub.DataMgr.Setting);
        AppHub.LogicMgr = new LogicManager();

        var app = CreateApp();
        // 화면 코드(처리기·바인딩 getter·페이지 알림)의 예외는 라이브러리가 잡아 여기로 모은다
        app.HandlerError += (_, e) => Console.Error.WriteLine($"[{e.Kind}] {e.Member} ({e.Tag}): {e.Exception.Message}");
#if DEBUG
        app.HotReload = true;
#endif

        // 터치 장비(Linux)는 GLES 만 있고 테두리 없는 전체 화면으로 띄운다. PC 에서 실행하면 일반 창
        var device = OperatingSystem.IsLinux();
        using var window = new ViewWindow(app, 1280, 800, Texts.T("app.title"), gles: device, fullscreen: device);
        try
        {
            AppHub.DevMgr.Start();
            AppHub.LogicMgr.Start();
            window.Run();
        }
        finally
        {
            AppHub.LogicMgr.Stop();
            AppHub.DevMgr.Dispose();
        }
    }

    /// <summary>앱과 페이지를 만든다. 매니저는 먼저 채워져 있어야 한다 (창 없이 화면을 확인하는 도구·테스트도 쓸 수 있게 공개).</summary>
    public static DvApp CreateApp()
    {
        Texts.SetLanguage(AppHub.DataMgr.Setting.Language);
        var app = new DvApp
        {
            Theme = AppHub.DataMgr.Setting.DarkTheme ? DvTheme.Dark : DvTheme.Light,
            Strings = Texts.Strings,   // 영어 기본, 설정에서 한국어 (화면 문구는 Strings/*.json)
            Registry = Symbols.Registry(),
            PageTransition = DvPageTransition.Fade,
        };
        AppHub.App = app;
        app.Top = new TopBar();
        app.Bottom = new AlarmBar();
        app.Pages.Add(new MainPage());
        app.Pages.Add(new MonitorPage());
        app.Pages.Add(new RecipePage());
        app.Pages.Add(new SchedulePage());
        app.Pages.Add(new AlarmPage());
        app.Pages.Add(new SettingPage());
        return app;
    }
}
