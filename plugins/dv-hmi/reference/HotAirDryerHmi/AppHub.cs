using Devinno.UI.App;
using HotAirDryerHmi.Managers;

namespace HotAirDryerHmi;

/// <summary>앱 전역 접근점. Program.cs 가 시작할 때 한 번 채운다. 페이지 안에서는 라이브러리의 App 속성을 쓰고, 이것은 페이지 밖(매니저·로직)에서 쓴다.</summary>
public static class AppHub
{
    public static DvApp App { get; set; } = null!;
    public static DataManager DataMgr { get; set; } = null!;
    public static DeviceManager DevMgr { get; set; } = null!;
    public static LogicManager LogicMgr { get; set; } = null!;
}
