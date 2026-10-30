using Devinno.UI.Components;

namespace HotAirDryerHmi.Pages;

/// <summary>공정도에 쓰는 사용자 심볼. 라이브러리에 없는 장비(전기 히터)를 정의해 등록한다.</summary>
public static class Symbols
{
    /// <summary>전기 히터: 몸체 + 발열 코일. in(왼쪽) → out(오른쪽)</summary>
    public const string Heater = """
        <DvSymbol name="Heater" view-box="0 0 76 56">
          <DvParam name="running" type="bool"/>
          <DvShape class="accent" path="M0 22 H10 V34 H0 Z"/>
          <DvShape class="accent" path="M66 22 H76 V34 H66 Z"/>
          <DvShape class="accent" path="M22 0 H30 V8 H22 Z"/>
          <DvShape class="accent" path="M46 0 H54 V8 H46 Z"/>
          <DvShape class="state" when="running" path="M8 8 H68 V50 H8 Z"/>
          <DvShape class="detail" path="M14 29 L20 17 L26 41 L32 17 L38 41 L44 17 L50 41 L56 17 L62 29"/>
          <DvPort name="in" side="left" at="0.5" role="in"/>
          <DvPort name="out" side="right" at="0.5" role="out"/>
          <DvPass from="in" to="out"/>
        </DvSymbol>
        """;

    public static ComponentRegistry Registry()
    {
        var registry = new ComponentRegistry();
        registry.Register(Heater);
        return registry;
    }
}
