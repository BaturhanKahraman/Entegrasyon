using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryWizardGeneralStep
{
    [Parameter] public string Name { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> NameChanged { get; set; }

    [Parameter] public int? ParentCategoryId { get; set; }
    [Parameter] public EventCallback<int?> ParentCategoryIdChanged { get; set; }

    [Parameter] public bool IsFavorite { get; set; }
    [Parameter] public EventCallback<bool> IsFavoriteChanged { get; set; }

    [Parameter] public decimal? DefaultVatRate { get; set; }
    [Parameter] public EventCallback<decimal?> DefaultVatRateChanged { get; set; }

    /// <summary>
    /// Edit modunda kendisini üst kategori listesinden hariç tutmak için.
    /// </summary>
    [Parameter] public int? EditCategoryId { get; set; }

    [Parameter] public EventCallback OnFieldChanged { get; set; }

    [Inject] private ICategoryService CategoryService { get; set; } = null!;

    private MudForm _form = null!;
    private bool _isValid;
    private List<Category> _availableCategories = [];

    protected override async Task OnInitializedAsync()
    {
        var candidates = await CategoryService.GetValidParentCandidatesAsync(EditCategoryId);
        _availableCategories = candidates;
    }

    public async Task<bool> ValidateAsync()
    {
        await _form.Validate();
        return _isValid;
    }
}
