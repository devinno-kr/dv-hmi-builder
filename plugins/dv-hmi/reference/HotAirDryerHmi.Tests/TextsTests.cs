using System.Text.Json;

namespace HotAirDryerHmi.Tests;

/// <summary>화면 문구 파일: Strings/*.json 을 모두 읽고, 같은 쌍(en·ko)의 키가 같다</summary>
public class TextsTests
{
    static readonly string Dir = Path.Combine(AppContext.BaseDirectory, "Strings");

    [Fact(DisplayName = "공통 키와 페이지 키가 모두 읽힌다 (nav.main, rec.list, set.title)")]
    public void CommonAndPageKeysLoad()
    {
        Assert.Equal("Operation", Texts.Strings.Get("nav.main"));   // 없는 키는 키 이름(또는 빈 글)이 돌아온다
        foreach (var key in new[] { "nav.main", "rec.list", "set.title" })
            Assert.NotEqual(key, Texts.Strings.Get(key));
    }

    [Fact(DisplayName = "Strings 의 모든 en 파일에 같은 이름의 ko 파일이 있고 키가 같다")]
    public void EnAndKoPairsHaveSameKeys()
    {
        var en = Directory.GetFiles(Dir, "*en.json");
        Assert.NotEmpty(en);
        foreach (var file in en)
        {
            var ko = file[..^"en.json".Length] + "ko.json";
            Assert.True(File.Exists(ko), $"{Path.GetFileName(ko)} 없음");
            Assert.Equal(Keys(file), Keys(ko));
        }
    }

    static string[] Keys(string file) => JsonDocument.Parse(File.ReadAllText(file)).RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray();
}
