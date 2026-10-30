using System.Globalization;
using Devinno.UI.Localization;

namespace HotAirDryerHmi;

/// <summary>
/// 화면 문구 (영어 기본, 한국어 선택). 문자열 표는 Strings/*.json — 파일 이름의 마지막 점 뒤(확장자 앞)가 언어다.
/// 공통 키(앱 틀·알람·통신·검증 메시지, 두 곳 이상이나 페이지 밖 코드에서 쓰는 것)는 Strings/&lt;언어&gt;.json,
/// 한 페이지에서만 쓰는 키는 Strings/&lt;페이지&gt;.&lt;언어&gt;.json (예: MainPage.en.json). 같은 쌍(en·ko)의 키는 같아야 한다.
/// 마크업은 @키 · {L10n.키} 로, 코드(VM·로직의 상태 글·알람·확인창)는 T(키) 로 읽는다.
/// 언어를 바꾸면 앱(app.Strings = Strings)이 화면을 다시 그린다.
/// </summary>
public static class Texts
{
    public static readonly string[] Languages = ["en", "ko"];

    public static DvStrings Strings { get; } = Load();

    /// <summary>날짜·요일 표시에 쓰는 문화권 (언어를 따라감)</summary>
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

    public static string Language => Strings.Language;

    public static void SetLanguage(string language)
    {
        Culture = CultureInfo.GetCultureInfo(language == "ko" ? "ko-KR" : "en-US");
        Strings.Language = language;   // 화면이 붙어 있으면 다시 그려짐
    }

    public static string T(string key) => Strings.Get(key);

    public static string T(string key, params object[] args) => string.Format(Culture, Strings.Get(key), args);

    private static DvStrings Load()
    {
        var strings = new DvStrings { Language = "en", FallbackLanguage = "en" };
        // 모든 Strings/*.json 을 읽는다 — LoadJson 은 항목을 더하므로 언어마다 여러 번 불러도 된다
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Strings"), "*.json").Order())
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var lang = name[(name.LastIndexOf('.') + 1)..];   // "MainPage.en" → en, "en" → en
            strings.LoadJson(lang, File.ReadAllText(file));
        }
        return strings;
    }
}
