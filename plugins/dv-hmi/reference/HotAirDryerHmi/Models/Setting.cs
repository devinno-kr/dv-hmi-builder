namespace HotAirDryerHmi.Models;

/// <summary>앱 설정. 저장·불러오기는 DataManager 가 한다 (config/setting.json).</summary>
public class Setting
{
    // 통신
    /// <summary>시뮬레이션 — 장비 대신 앱 안의 가짜 건조기에 붙는다 (장비가 없을 때 확인용)</summary>
    public bool Simulation { get; set; } = true;
    /// <summary>장비 IP 주소</summary>
    public string DryerIp { get; set; } = "127.0.0.1";
    /// <summary>장비 포트</summary>
    public int DryerPort { get; set; } = 1502;

    // 감시
    /// <summary>감시 주기 (ms) — 통신 끊김·알람 판정, 트렌드 기록, 화면 갱신. 통신 읽기 주기가 아님 (dryer.xml)</summary>
    public int PollIntervalMs { get; set; } = 500;
    /// <summary>통신 끊김 판정 (초) — 마지막 수신 뒤 이 시간이 지나면 통신 끊김</summary>
    public int CommTimeoutSec { get; set; } = 5;

    // 알람 기준
    /// <summary>경고 온도 (℃)</summary>
    public double TempWarnC { get; set; } = 175;
    /// <summary>정지 온도 (℃) — 넘으면 운전을 멈춘다</summary>
    public double TempTripC { get; set; } = 190;
    /// <summary>재료 부족 경고 (%) — 호퍼 재료가 이 아래로 내려가면 경고</summary>
    public double LevelLowPct { get; set; } = 15;

    // 기록·화면
    /// <summary>트렌드 보관 개수</summary>
    public int TrendPoints { get; set; } = 7200;
    /// <summary>어두운 화면</summary>
    public bool DarkTheme { get; set; } = true;
    /// <summary>화면 언어 — en (기본) 또는 ko</summary>
    public string Language { get; set; } = "en";
    /// <summary>예약 운전 사용</summary>
    public bool ScheduleEnabled { get; set; } = true;

    /// <summary>편집용 복사본. 설정 화면은 복사본을 고치고 저장할 때 원본과 바꾼다.</summary>
    public Setting Clone() => (Setting)MemberwiseClone();

    /// <summary>잘못된 값 목록. 비어 있으면 정상.</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(DryerIp)) errors.Add(Texts.T("val.ip"));
        if (DryerPort is < 1 or > 65535) errors.Add(Texts.T("val.port"));
        if (PollIntervalMs < 100) errors.Add(Texts.T("val.poll"));
        if (CommTimeoutSec * 1000 <= PollIntervalMs) errors.Add(Texts.T("val.timeout"));
        if (TempWarnC >= TempTripC) errors.Add(Texts.T("val.warntrip"));
        if (LevelLowPct is < 0 or > 100) errors.Add(Texts.T("val.level"));
        if (TrendPoints < 60) errors.Add(Texts.T("val.trend"));
        if (!Texts.Languages.Contains(Language)) errors.Add(Texts.T("val.language"));
        return errors;
    }
}
