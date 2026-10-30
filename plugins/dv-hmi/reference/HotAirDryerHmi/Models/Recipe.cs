using System.Text.Json.Serialization;

namespace HotAirDryerHmi.Models;

/// <summary>건조 레시피: 예열 → 건조 → 냉각 세 단계와 송풍량. 장비에는 Dryer.WriteRecipe 로 보낸다.</summary>
public class Recipe
{
    /// <summary>레시피 이름</summary>
    public string Name { get; set; } = "New recipe";
    /// <summary>재료</summary>
    public string Material { get; set; } = "";
    /// <summary>단계 (예열·건조·냉각)</summary>
    public List<RecipeStep> Steps { get; set; } = [];
    /// <summary>송풍량 (%)</summary>
    public double FanPct { get; set; } = 70;

    /// <summary>예열 단계</summary>
    [JsonIgnore] public RecipeStep Preheat => Steps[0];
    /// <summary>건조 단계</summary>
    [JsonIgnore] public RecipeStep Dry => Steps[1];
    /// <summary>냉각 단계</summary>
    [JsonIgnore] public RecipeStep Cool => Steps[2];
    /// <summary>전체 시간 (분)</summary>
    [JsonIgnore] public int TotalMinutes => Steps.Sum(s => s.Minutes);

    /// <summary>레시피 만들기 — 예열·건조·냉각 값과 송풍량으로</summary>
    public static Recipe Create(string name, string material, double preT, int preMin, double dryT, int dryMin, double coolT, int coolMin, double fan) => new()
    {
        Name = name,
        Material = material,
        FanPct = fan,
        Steps =
        [
            new() { Kind = "preheat", TempC = preT, Minutes = preMin },
            new() { Kind = "dry", TempC = dryT, Minutes = dryMin },
            new() { Kind = "cool", TempC = coolT, Minutes = coolMin },
        ],
    };

    /// <summary>처음 레시피 목록</summary>
    public static List<Recipe> Defaults() =>
    [
        Create("PET chips", "PET", 140, 10, 165, 40, 60, 10, 80),
        Create("PA66 nylon", "PA66", 70, 5, 80, 30, 40, 5, 60),
        Create("ABS pellets", "ABS", 70, 5, 85, 20, 40, 5, 70),
        Create("PC resin", "PC", 100, 10, 120, 30, 50, 10, 75),
    ];

    /// <summary>편집용 복사본</summary>
    public Recipe Clone()
    {
        var r = (Recipe)MemberwiseClone();
        r.Steps = [.. Steps.Select(s => s.Clone())];
        return r;
    }

    /// <summary>잘못된 값 목록. 비어 있으면 정상.</summary>
    public List<string> Validate(double tripC)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name)) errors.Add(Texts.T("val.recname"));
        if (Steps.Count != 3) errors.Add(Texts.T("val.steps"));
        foreach (var s in Steps)
        {
            if (s.TempC < 20 || s.TempC >= tripC) errors.Add(Texts.T("val.steptemp", s.Label, tripC));
            if (s.Minutes is < 1 or > 600) errors.Add(Texts.T("val.stepmin", s.Label));
        }
        if (FanPct is < 20 or > 100) errors.Add(Texts.T("val.fan"));
        return errors;
    }
}

/// <summary>레시피 한 단계. 레시피 화면의 표에서 바로 고친다.</summary>
public class RecipeStep
{
    /// <summary>단계 종류 — preheat · dry · cool</summary>
    public string Kind { get; set; } = "";
    /// <summary>화면에 보이는 단계 이름 (화면 언어)</summary>
    [JsonIgnore] public string Label => Texts.T("step." + Kind);
    /// <summary>온도 (℃)</summary>
    public double TempC { get; set; }
    /// <summary>시간 (분)</summary>
    public int Minutes { get; set; }

    /// <summary>복사본</summary>
    public RecipeStep Clone() => (RecipeStep)MemberwiseClone();
}
