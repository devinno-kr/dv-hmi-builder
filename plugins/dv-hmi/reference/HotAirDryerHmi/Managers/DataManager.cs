using System.Text.Json;
using Devinno.UI.Alarms;
using Devinno.UI.Trends;
using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Managers;

/// <summary>앱이 보관하는 데이터: 설정·레시피·예약 파일, 트렌드, 알람. 파일 경로는 여기서만 정한다.</summary>
public class DataManager
{
    /// <summary>앱이 실행 중에 쓰는 파일(설정·레시피·기록)의 최상위 폴더. 배포가 프로그램 폴더를 바꾸거나 지워도 남는다.
    /// Windows: %LOCALAPPDATA%\HotAirDryerHmi, 터치: ~/.local/share/HotAirDryerHmi. 함께 나가는 읽기 전용 파일(Strings, 통신 XML)은 AppContext.BaseDirectory 에 둔다.
    /// internal set: 테스트가 임시 폴더로 돌릴 수 있게</summary>
    public static string UserDataFolder { get; internal set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HotAirDryerHmi");
    private static string ConfigDir => Path.Combine(UserDataFolder, "config");
    public static string SettingPath => Path.Combine(ConfigDir, "setting.json");
    public static string RecipePath => Path.Combine(ConfigDir, "recipes.json");
    public static string SchedulePath => Path.Combine(ConfigDir, "schedules.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>파일을 읽는다. 없으면 기본값으로 만들어 저장한다. 깨졌으면 예외 (기본값으로 대신해 잘못된 주소로 통신하지 않도록).</summary>
    public DataManager()
    {
        Setting = Load(SettingPath, () => new Setting());
        Recipes = Load(RecipePath, Recipe.Defaults);
        Schedules = Load(SchedulePath, DefaultSchedules);
        Trend = new TrendBuffer(Setting.TrendPoints);
    }

    public Setting Setting { get; private set; }
    public List<Recipe> Recipes { get; private set; }
    public List<ScheduleItem> Schedules { get; private set; }

    /// <summary>트렌드 (채널 이름 = TrendChannels). 스레드 안전</summary>
    public TrendBuffer Trend { get; }

    /// <summary>알람 목록. 스레드 안전</summary>
    public AlarmLog Alarms { get; } = new(300);

    /// <summary>지금 장비에 보낸 레시피 이름 (보낸 적 없으면 null)</summary>
    public string? ActiveRecipe { get; set; }

    public void SaveSetting(Setting s)
    {
        var errors = s.Validate();
        if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
        Save(SettingPath, s);
        Setting = s;
    }

    public void SaveRecipes(List<Recipe> recipes)
    {
        Save(RecipePath, recipes);
        Recipes = recipes;
    }

    public void SaveSchedules(List<ScheduleItem> items)
    {
        Save(SchedulePath, items);
        Schedules = items;
    }

    public Recipe? FindRecipe(string? name) => Recipes.FirstOrDefault(r => r.Name == name);

    private static T Load<T>(string path, Func<T> create)
    {
        if (!File.Exists(path))
        {
            var value = create();
            Save(path, value);
            return value;
        }
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? throw new InvalidDataException($"{Path.GetFileName(path)} is empty");
    }

    // 임시 파일에 쓴 뒤 바꿔 끼운다 (쓰는 도중 전원이 나가도 원래 파일은 남음)
    private static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(tmp, path, true);
    }

    private static List<ScheduleItem> DefaultSchedules() =>
    [
        new() { Id = 1, Start = new(7, 30), RecipeName = "PET chips" },
        new() { Id = 2, Start = new(13, 0), RecipeName = "PA66 nylon" },
        new() { Id = 3, Start = new(22, 0), RecipeName = "PC resin", Enabled = false, Sat = true },
    ];
}

/// <summary>트렌드 채널 이름 (DvTrendChannel name 과 같아야 함)</summary>
public static class TrendChannels
{
    public const string Chamber = "chamber";
    public const string SetTemp = "set";
    public const string Exhaust = "exhaust";
    public const string Inlet = "inlet";
    public const string Humidity = "humidity";
    public const string Heater = "heater";
}
