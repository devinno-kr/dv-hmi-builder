using System.Text.Json.Serialization;

namespace HotAirDryerHmi.Models;

/// <summary>예약 운전 한 줄: 고른 요일의 시작 시각에 레시피를 장비에 보내고 운전을 시작한다 (LogicManager).</summary>
public class ScheduleItem
{
    /// <summary>번호</summary>
    public int Id { get; set; }
    /// <summary>사용</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>시작 시각</summary>
    public TimeOnly Start { get; set; } = new(8, 0);
    /// <summary>레시피</summary>
    public string RecipeName { get; set; } = "";
    /// <summary>월요일</summary>
    public bool Mon { get; set; } = true;
    /// <summary>화요일</summary>
    public bool Tue { get; set; } = true;
    /// <summary>수요일</summary>
    public bool Wed { get; set; } = true;
    /// <summary>목요일</summary>
    public bool Thu { get; set; } = true;
    /// <summary>금요일</summary>
    public bool Fri { get; set; } = true;
    /// <summary>토요일</summary>
    public bool Sat { get; set; }
    /// <summary>일요일</summary>
    public bool Sun { get; set; }

    /// <summary>그 요일에 켜져 있는지</summary>
    public bool On(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => Mon, DayOfWeek.Tuesday => Tue, DayOfWeek.Wednesday => Wed,
        DayOfWeek.Thursday => Thu, DayOfWeek.Friday => Fri, DayOfWeek.Saturday => Sat, _ => Sun,
    };

    /// <summary>now 이후 처음 돌아오는 시작 시각. 사용 안 함이거나 요일이 없으면 null</summary>
    public DateTime? Next(DateTime now)
    {
        if (!Enabled) return null;
        for (var i = 0; i < 8; i++)
        {
            var day = now.Date.AddDays(i);
            var at = day + Start.ToTimeSpan();
            if (at > now && On(day.DayOfWeek)) return at;
        }
        return null;
    }

    /// <summary>(prev, now] 사이에 시작 시각이 지나갔는지. 지나간 그 순간에만 동작하게 한다 (재시작 직후 저절로 켜지지 않음)</summary>
    public bool PassedBetween(DateTime prev, DateTime now)
    {
        if (!Enabled || now <= prev || now - prev > TimeSpan.FromMinutes(1)) return false;
        var at = now.Date + Start.ToTimeSpan();
        if (at > now) at = at.AddDays(-1);   // 자정을 넘는 감시 간격
        return prev < at && at <= now && On(at.DayOfWeek);
    }

    /// <summary>표에 보이는 다음 실행 시각 (저장하지 않음)</summary>
    [JsonIgnore]
    public string NextText => Next(DateTime.Now)?.ToString(Texts.T("fmt.next"), Texts.Culture) ?? "-";

    /// <summary>편집용 복사본</summary>
    public ScheduleItem Clone() => (ScheduleItem)MemberwiseClone();
}
