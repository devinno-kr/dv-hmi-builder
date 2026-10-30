using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Tests;

/// <summary>레시피 검증: 장비에 보내기 전에 막는 값 (Recipe.Validate). 문구가 아니라 오류 개수로 확인한다 (화면 언어와 무관하게)</summary>
public class RecipeValidateTests
{
    const double Trip = 190;   // 설정의 기본 고온 정지 온도

    [Fact(DisplayName = "기본 레시피 4개 → 고온 정지 190 ℃ 에서 오류 없음")]
    public void DefaultsAreValid() =>
        Assert.All(Recipe.Defaults(), r => Assert.Empty(r.Validate(Trip)));

    [Fact(DisplayName = "건조 온도 190 ℃ (고온 정지와 같음) → 막음")]
    public void DryAtTrip() =>
        Assert.Single(Recipe.Create("T", "PET", 140, 10, 190, 40, 60, 10, 80).Validate(Trip));

    [Fact(DisplayName = "건조 온도 189 ℃ → 통과")]
    public void DryBelowTrip() =>
        Assert.Empty(Recipe.Create("T", "PET", 140, 10, 189, 40, 60, 10, 80).Validate(Trip));

    [Fact(DisplayName = "냉각 온도 19 ℃ → 막음 (20 ℃ 이상)")]
    public void CoolTooLow() =>
        Assert.Single(Recipe.Create("T", "PET", 140, 10, 165, 40, 19, 10, 80).Validate(Trip));

    [Theory(DisplayName = "송풍량 → 20~100 % 만 통과")]
    [InlineData(19, false)]
    [InlineData(20, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void FanRange(double fan, bool ok) =>
        Assert.Equal(ok, Recipe.Create("T", "PET", 140, 10, 165, 40, 60, 10, fan).Validate(Trip).Count == 0);
}
