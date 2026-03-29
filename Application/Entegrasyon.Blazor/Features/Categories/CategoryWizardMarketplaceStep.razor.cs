using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryWizardMarketplaceStep
{
    [Parameter] public int CategoryId { get; set; }
    [Parameter] public string? CategoryName { get; set; }
    [Parameter] public EventCallback OnFieldChanged { get; set; }

    [Inject] private ICategoryMatchService CategoryMatchService { get; set; } = null!;
    [Inject] private IDbContextFactory<IntegrationDbContext> DbContextFactory { get; set; } = null!;

    private List<MarketPlace> _marketplaces = [];
    private List<CategoryMarketplaceMappingDto> _existingMappings = [];

    public bool RedirectToMatching { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        _marketplaces = await context.MarketPlaces
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Name)
            .ToListAsync();

        if (CategoryId > 0)
        {
            foreach (var mp in _marketplaces)
            {
                var mappings = await CategoryMatchService.GetAllCategoryMappingsAsync(mp.Id);
                _existingMappings.AddRange(mappings.Where(m => m.ApplicationCategoryId == CategoryId));
            }
        }
    }
}
