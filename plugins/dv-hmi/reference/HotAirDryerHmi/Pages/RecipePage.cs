using Devinno.UI.App;
using HotAirDryerHmi.Models;

namespace HotAirDryerHmi.Pages;

/// <summary>레시피: 목록에서 고르고, 복사본을 고친 뒤 저장하거나 장비에 적용한다.</summary>
public sealed partial class RecipePage : DvPage
{
    private string? selected;

    public RecipePage() => Select(AppHub.DataMgr.Recipes.FirstOrDefault()?.Name);

    public List<Recipe> Recipes => AppHub.DataMgr.Recipes;

    /// <summary>목록에서 고른 레시피 이름. 고르면 편집본을 새로 만든다 (고치던 내용은 버림)</summary>
    public string? SelectedName
    {
        get => selected;
        set => Select(value);
    }

    /// <summary>편집 중인 복사본. 저장할 때 원본과 바꾼다</summary>
    public Recipe Edit { get; private set; } = Recipe.Create("-", "", 20, 1, 20, 1, 20, 1, 50);

    public double TripLimit => AppHub.DataMgr.Setting.TempTripC - 1;
    public string TotalText => Texts.T("rec.total", Edit.TotalMinutes);
    public bool IsActive => AppHub.DataMgr.ActiveRecipe == selected;
    public string ActiveText => Texts.T(IsActive ? "rec.applied" : "rec.notapplied");

    public async void Add()
    {
        if (await App.InputBox(Texts.T("rec.newname"), "") is not { Length: > 0 } name) return;
        if (Recipes.Any(r => r.Name == name)) { await App.MessageBox(Texts.T("rec.exists", name), Texts.T("rec.title"), DvMessageButtons.Ok); return; }
        var r = Recipe.Create(name, "", 70, 5, 80, 30, 40, 5, 70);
        AppHub.DataMgr.SaveRecipes([.. Recipes, r]);
        Select(name);
    }

    public async void Copy()
    {
        var name = Texts.T("rec.copysuffix", Edit.Name);
        if (await App.InputBox(Texts.T("rec.copyname"), name) is not { Length: > 0 } n || Recipes.Any(r => r.Name == n)) return;
        var r = Edit.Clone();
        r.Name = n;
        AppHub.DataMgr.SaveRecipes([.. Recipes, r]);
        Select(n);
    }

    public async void Delete()
    {
        if (selected is null || Recipes.Count <= 1) return;
        if (await App.MessageBox(Texts.T("rec.deleteq", selected), Texts.T("rec.deletetitle"), DvMessageButtons.YesNo) != DvDialogResult.Yes) return;
        AppHub.DataMgr.SaveRecipes([.. Recipes.Where(r => r.Name != selected)]);
        Select(Recipes.First().Name);
    }

    public void Revert() => Select(selected);

    public async void Save()
    {
        var errors = Edit.Validate(AppHub.DataMgr.Setting.TempTripC);
        if (Edit.Name != selected && Recipes.Any(r => r.Name == Edit.Name)) errors.Add(Texts.T("rec.exists", Edit.Name));
        if (errors.Count > 0) { await App.MessageBox(string.Join("\n", errors), Texts.T("dlg.cannotsave"), DvMessageButtons.Ok); return; }
        var saved = Edit.Clone();
        AppHub.DataMgr.SaveRecipes([.. Recipes.Select(r => r.Name == selected ? saved : r)]);
        if (AppHub.DataMgr.ActiveRecipe == selected) AppHub.DataMgr.ActiveRecipe = saved.Name;
        Select(saved.Name);
    }

    public async void Apply()
    {
        if (await App.MessageBox(Texts.T("rec.applyq", Edit.Name), Texts.T("rec.applytitle"), DvMessageButtons.YesNo) != DvDialogResult.Yes) return;
        if (await AppHub.LogicMgr.ApplyRecipe(Edit) is { } why) await App.MessageBox(why, Texts.T("dlg.cannotapply"), DvMessageButtons.Ok);
    }

    private void Select(string? name)
    {
        selected = name;
        if (AppHub.DataMgr.FindRecipe(name) is { } r) Edit = r.Clone();
    }
}
