# Brand & Category Matching UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve brand matching UX with a master-detail page layout, and fix category sync table to show all marketplace matchings instead of only the last one.

**Architecture:** Master-Detail layout for brand matching (following existing `AttributesPage` pattern). Left panel: searchable/filterable brand list with marketplace status dots. Right panel: all marketplace rows with inline autocomplete for matching. Category sync gets chip-based marketplace display in the DataGrid.

**Tech Stack:** Blazor Server (.NET 8), MudBlazor, EF Core (PostgreSQL), FluentValidation, xUnit + Moq + FluentAssertions

---

## File Structure

| Action | File | Responsibility |
|--------|------|----------------|
| Modify | `Application/Entegrasyon.Business/Abstract/IBrandMatchService.cs` | Add `GetBrandMappingsByBrandIdAsync` |
| Modify | `Application/Entegrasyon.Business/Concrete/BrandMatchService.cs` | Implement `GetBrandMappingsByBrandIdAsync` |
| Rewrite | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor` | Master-Detail layout |
| Rewrite | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor.cs` | Master-Detail logic |
| Create | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingListPanel.razor` | Left panel — brand list |
| Create | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingListPanel.razor.cs` | Left panel code-behind |
| Create | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor` | Right panel — marketplace rows |
| Create | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor.cs` | Right panel code-behind |
| Delete | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingList.razor` | No longer needed |
| Delete | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingList.razor.cs` | No longer needed |
| Delete | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDialog.razor` | No longer needed (inline matching) |
| Delete | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDialog.razor.cs` | No longer needed |
| Modify | `Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor` | Fix marketplace details column |
| Modify | `Test/Entegrasyon.Test/Business/BrandServiceTests.cs` | Add unit test for new method |
| Modify | `Test/Entegrasyon.IntegrationTest/Business/BrandMatchServiceIntegrationTests.cs` | Add integration test |

---

## Task 1: Add `GetBrandMappingsByBrandIdAsync` to Business Layer (TDD)

**Files:**
- Modify: `Application/Entegrasyon.Business/Abstract/IBrandMatchService.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/BrandMatchService.cs`
- Modify: `Test/Entegrasyon.IntegrationTest/Business/BrandMatchServiceIntegrationTests.cs`

- [ ] **Step 1: Write the failing integration test**

In `Test/Entegrasyon.IntegrationTest/Business/BrandMatchServiceIntegrationTests.cs`, add:

```csharp
[Fact]
public async Task GetBrandMappingsByBrandId_ShouldReturnAllMappingsForBrand()
{
    // Arrange — seed a second marketplace and create mappings for Nike (brand id from seed)
    using var scope = _factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

    var secondMarketplace = new MarketPlace { Name = "Hepsiburada", ApiKey = "test", ApiSecret = "test", SellerId = "test" };
    dbContext.MarketPlaces.Add(secondMarketplace);
    await dbContext.SaveChangesAsync();

    var nikeBrand = await dbContext.Brands.FirstAsync(b => b.Name == "Nike");
    var trendyolMp = await dbContext.MarketPlaces.FirstAsync(m => m.Name == "Trendyol");

    dbContext.BrandMarketPlaceMatches.AddRange(
        new BrandMarketPlaceMatch { ApplicationBrandId = nikeBrand.Id, MarketPlaceId = trendyolMp.Id, MarketPlaceBrandId = 1234 },
        new BrandMarketPlaceMatch { ApplicationBrandId = nikeBrand.Id, MarketPlaceId = secondMarketplace.Id, MarketPlaceBrandId = 5678 }
    );
    await dbContext.SaveChangesAsync();

    var sut = scope.ServiceProvider.GetRequiredService<IBrandMatchService>();

    // Act
    var result = await sut.GetBrandMappingsByBrandIdAsync(nikeBrand.Id);

    // Assert
    result.Success.Should().BeTrue();
    result.Data.Should().HaveCount(2);
    result.Data.Should().Contain(m => m.MarketPlaceBrandId == 1234);
    result.Data.Should().Contain(m => m.MarketPlaceBrandId == 5678);
}

[Fact]
public async Task GetBrandMappingsByBrandId_ShouldReturnEmpty_WhenNoMappings()
{
    using var scope = _factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
    var pumaBrand = await dbContext.Brands.FirstAsync(b => b.Name == "Puma");
    var sut = scope.ServiceProvider.GetRequiredService<IBrandMatchService>();

    var result = await sut.GetBrandMappingsByBrandIdAsync(pumaBrand.Id);

    result.Success.Should().BeTrue();
    result.Data.Should().BeEmpty();
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~GetBrandMappingsByBrandId" -v minimal`
Expected: FAIL — method does not exist on interface

- [ ] **Step 3: Add interface method**

In `Application/Entegrasyon.Business/Abstract/IBrandMatchService.cs`, add to the interface:

```csharp
Task<IDataResult<List<BrandMarketPlaceMatchDto>>> GetBrandMappingsByBrandIdAsync(int brandId);
```

- [ ] **Step 4: Implement the method**

In `Application/Entegrasyon.Business/Concrete/BrandMatchService.cs`, add:

```csharp
public async Task<IDataResult<List<BrandMarketPlaceMatchDto>>> GetBrandMappingsByBrandIdAsync(int brandId)
{
    await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

    var mappings = await dbContext.BrandMarketPlaceMatches
        .Include(m => m.MarketPlace)
        .Include(m => m.ApplicationBrand)
        .Where(m => m.ApplicationBrandId == brandId)
        .ToListAsync();

    var dtos = _mapper.Map<List<BrandMarketPlaceMatchDto>>(mappings);
    return new SuccessDataResult<List<BrandMarketPlaceMatchDto>>(dtos);
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj --filter "FullyQualifiedName~GetBrandMappingsByBrandId" -v minimal`
Expected: PASS (2 tests)

- [ ] **Step 6: Run all existing tests to ensure no regressions**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal`
Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj -v minimal`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/IBrandMatchService.cs \
      Application/Entegrasyon.Business/Concrete/BrandMatchService.cs \
      Test/Entegrasyon.IntegrationTest/Business/BrandMatchServiceIntegrationTests.cs
git commit -m "feat(brand): add GetBrandMappingsByBrandIdAsync for master-detail panel"
```

---

## Task 2: Create BrandMappingListPanel (Left Panel)

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingListPanel.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingListPanel.razor.cs`

- [ ] **Step 1: Create code-behind file**

Create `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingListPanel.razor.cs`:

```csharp
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class BrandMappingListPanel : ComponentBase
{
    [Parameter] public List<BrandDto> Brands { get; set; } = [];
    [Parameter] public List<BrandMarketPlaceMatchDto> AllMappings { get; set; } = [];
    [Parameter] public List<MarketPlace> Marketplaces { get; set; } = [];
    [Parameter] public BrandDto? SelectedBrand { get; set; }
    [Parameter] public EventCallback<BrandDto> SelectedBrandChanged { get; set; }
    [Parameter] public string FilterMode { get; set; } = "all"; // "all", "unmapped", "completed"

    private string _searchText = string.Empty;

    private IEnumerable<BrandDto> FilteredBrands
    {
        get
        {
            var brands = Brands.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchText))
                brands = brands.Where(b => b.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            return FilterMode switch
            {
                "unmapped" => brands.Where(b => GetMappingCount(b.Id) < Marketplaces.Count),
                "completed" => brands.Where(b => GetMappingCount(b.Id) == Marketplaces.Count),
                _ => brands
            };
        }
    }

    private int GetMappingCount(int brandId) =>
        AllMappings.Count(m => m.ApplicationBrandId == brandId);

    private bool IsMappedTo(int brandId, int marketPlaceId) =>
        AllMappings.Any(m => m.ApplicationBrandId == brandId && m.MarketPlaceId == marketPlaceId);

    private bool IsSelected(BrandDto brand) =>
        SelectedBrand?.Id == brand.Id;

    private async Task SelectBrand(BrandDto brand)
    {
        await SelectedBrandChanged.InvokeAsync(brand);
    }

    private void OnSearchChanged(string text)
    {
        _searchText = text;
    }

    private void SetFilter(string mode)
    {
        FilterMode = mode;
    }
}
```

- [ ] **Step 2: Create razor file**

Create `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingListPanel.razor`:

```razor
@using Entegrasyon.Entity.Dtos.Brand

<MudPaper Elevation="1" Class="pa-2">
    <MudToolBar Dense="true" Gutters="false" Class="mb-2">
        <MudTextField T="string"
                      Placeholder="Marka ara..."
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Search"
                      ValueChanged="OnSearchChanged"
                      DebounceInterval="400"
                      Clearable="true"
                      Style="flex: 1;" />
    </MudToolBar>

    <MudChipSet T="string" Class="mb-2" SelectionMode="SelectionMode.SingleSelection" SelectedValue="@FilterMode" SelectedValueChanged="SetFilter">
        <MudChip T="string" Value="@("all")" Variant="Variant.Text" Size="Size.Small">Tümü (@Brands.Count)</MudChip>
        <MudChip T="string" Value="@("unmapped")" Variant="Variant.Text" Size="Size.Small" Color="Color.Warning">Eksik</MudChip>
        <MudChip T="string" Value="@("completed")" Variant="Variant.Text" Size="Size.Small" Color="Color.Success">Tamamlanan</MudChip>
    </MudChipSet>

    <MudList T="BrandDto" Dense="true" Style="max-height: 500px; overflow-y: auto;">
        @foreach (var brand in FilteredBrands)
        {
            <MudListItem T="BrandDto"
                         OnClick="@(() => SelectBrand(brand))"
                         Class="@(IsSelected(brand) ? "mud-primary-text" : "")">
                <div style="display: flex; justify-content: space-between; align-items: center; width: 100%;">
                    <MudText Typo="Typo.body2" Style="@(IsSelected(brand) ? "font-weight: bold;" : "")">
                        @brand.Name
                    </MudText>
                    <div style="display: flex; gap: 4px;">
                        @foreach (var mp in Marketplaces)
                        {
                            <MudIcon Icon="@Icons.Material.Filled.Circle"
                                     Size="Size.Small"
                                     Color="@(IsMappedTo(brand.Id, mp.Id) ? Color.Success : Color.Default)"
                                     Style="font-size: 10px;"
                                     Title="@mp.Name" />
                        }
                    </div>
                </div>
            </MudListItem>
        }
    </MudList>
</MudPaper>
```

- [ ] **Step 3: Verify build compiles**

Run: `dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj --no-restore -v minimal`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingListPanel.razor \
      Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingListPanel.razor.cs
git commit -m "feat(brand): add BrandMappingListPanel component for master-detail left panel"
```

---

## Task 3: Create BrandMappingDetailPanel (Right Panel)

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor`
- Create: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor.cs`

- [ ] **Step 1: Create code-behind file**

Create `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor.cs`:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class BrandMappingDetailPanel : ComponentBase
{
    [Parameter] public BrandDto? Brand { get; set; }
    [Parameter] public List<BrandMarketPlaceMatchDto> BrandMappings { get; set; } = [];
    [Parameter] public List<MarketPlace> Marketplaces { get; set; } = [];
    [Parameter] public EventCallback OnMappingChanged { get; set; }

    [Inject] private IMarketplaceSearchService SearchService { get; set; } = null!;
    [Inject] private IBrandMatchService BrandMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private readonly Dictionary<int, MarketplaceBrandSearchResult?> _selectedResults = new();
    private readonly HashSet<int> _savingMarketplaces = [];

    private int MappedCount => BrandMappings.Count;
    private int TotalCount => Marketplaces.Count;

    private BrandMarketPlaceMatchDto? GetMapping(int marketPlaceId) =>
        BrandMappings.FirstOrDefault(m => m.MarketPlaceId == marketPlaceId);

    private async Task<IEnumerable<MarketplaceBrandSearchResult>> SearchBrands(
        int marketPlaceId, string value, CancellationToken ct)
    {
        var result = await SearchService.SearchBrandsAsync(marketPlaceId, value ?? "", ct);
        return result.Success ? result.Data ?? [] : [];
    }

    private async Task SaveMapping(int marketPlaceId)
    {
        if (Brand is null) return;
        if (!_selectedResults.TryGetValue(marketPlaceId, out var selected) || selected is null) return;

        _savingMarketplaces.Add(marketPlaceId);
        try
        {
            var dto = new CreateBrandMarketPlaceMatchDto
            {
                ApplicationBrandId = Brand.Id,
                MarketPlaceId = marketPlaceId,
                MarketPlaceBrandId = selected.Id
            };

            var result = await BrandMatchService.CreateBrandMappingAsync(dto);
            if (result.Success)
            {
                Snackbar.Add("Eşleştirme kaydedildi.", Severity.Success);
                _selectedResults.Remove(marketPlaceId);
                await OnMappingChanged.InvokeAsync();
            }
            else
            {
                Snackbar.Add($"Hata: {result.Message}", Severity.Error);
            }
        }
        finally
        {
            _savingMarketplaces.Remove(marketPlaceId);
        }
    }

    private async Task RemoveMapping(int marketPlaceId)
    {
        if (Brand is null) return;

        var result = await BrandMatchService.RemoveBrandMappingAsync(Brand.Id, marketPlaceId);
        if (result.Success)
        {
            Snackbar.Add("Eşleştirme kaldırıldı.", Severity.Success);
            await OnMappingChanged.InvokeAsync();
        }
        else
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
        }
    }

    private void OnResultSelected(int marketPlaceId, MarketplaceBrandSearchResult? value)
    {
        _selectedResults[marketPlaceId] = value;
    }
}
```

- [ ] **Step 2: Create razor file**

Create `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor`:

```razor
@using Entegrasyon.Entity.Dtos.Brand
@using Entegrasyon.Entity.Dtos.Marketplace

@if (Brand is null)
{
    <MudPaper Elevation="1" Class="pa-8" Style="min-height: 400px; display: flex; align-items: center; justify-content: center;">
        <MudStack AlignItems="AlignItems.Center" Spacing="2">
            <MudIcon Icon="@Icons.Material.Filled.TouchApp" Size="Size.Large" Color="Color.Default" />
            <MudText Typo="Typo.body1" Color="Color.Secondary">Soldaki listeden bir marka seçin</MudText>
        </MudStack>
    </MudPaper>
}
else
{
    <MudPaper Elevation="1" Class="pa-4">
        <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 16px;">
            <MudText Typo="Typo.h6">@Brand.Name</MudText>
            <MudChip T="string" Size="Size.Small"
                     Color="@(MappedCount == TotalCount ? Color.Success : Color.Warning)">
                @MappedCount / @TotalCount Eşleşti
            </MudChip>
        </div>

        <MudStack Spacing="2">
            @foreach (var mp in Marketplaces)
            {
                var mapping = GetMapping(mp.Id);
                <MudPaper Elevation="0" Outlined="true" Class="pa-3">
                    <div style="display: flex; align-items: center; gap: 12px;">
                        <div style="width: 100px; flex-shrink: 0;">
                            <MarketplaceLogo Name="@mp.Name" Size="Size.Small" />
                            <MudText Typo="Typo.caption" Align="Align.Center">@mp.Name</MudText>
                        </div>

                        @if (mapping is not null)
                        {
                            <MudPaper Elevation="0" Class="pa-2 flex-grow-1"
                                      Style="background: var(--mud-palette-success-lighten); border-radius: 4px;">
                                <MudText Typo="Typo.body2">
                                    <MudIcon Icon="@Icons.Material.Filled.Check" Size="Size.Small" Color="Color.Success" />
                                    @mapping.MarketPlaceBrandName (#@mapping.MarketPlaceBrandId)
                                </MudText>
                            </MudPaper>
                            <MudIconButton Icon="@Icons.Material.Filled.Close"
                                           Color="Color.Error"
                                           Size="Size.Small"
                                           OnClick="@(() => RemoveMapping(mp.Id))"
                                           Title="Eşleştirmeyi kaldır" />
                        }
                        else
                        {
                            <MudAutocomplete T="MarketplaceBrandSearchResult"
                                             Value="@(_selectedResults.GetValueOrDefault(mp.Id))"
                                             ValueChanged="@(v => OnResultSelected(mp.Id, v))"
                                             SearchFunc="@((value, ct) => SearchBrands(mp.Id, value, ct))"
                                             Placeholder="Marketplace markası ara..."
                                             Variant="Variant.Outlined"
                                             Dense="true"
                                             ToStringFunc="@(x => x?.Name ?? "")"
                                             DebounceInterval="300"
                                             MinCharacters="0"
                                             Class="flex-grow-1" />
                            <MudIconButton Icon="@Icons.Material.Filled.Save"
                                           Color="Color.Primary"
                                           Size="Size.Small"
                                           Disabled="@(!_selectedResults.ContainsKey(mp.Id) || _selectedResults[mp.Id] is null || _savingMarketplaces.Contains(mp.Id))"
                                           OnClick="@(() => SaveMapping(mp.Id))"
                                           Title="Kaydet" />
                        }
                    </div>
                </MudPaper>
            }
        </MudStack>

        <MudAlert Severity="Severity.Info" Dense="true" Class="mt-4" NoIcon="true">
            <MudText Typo="Typo.caption">Her satırda marketplace markasını arayıp seçin. Yeşil = eşleştirilmiş, gri = bekleyen.</MudText>
        </MudAlert>
    </MudPaper>
}
```

- [ ] **Step 3: Verify build compiles**

Run: `dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj --no-restore -v minimal`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor \
      Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDetailPanel.razor.cs
git commit -m "feat(brand): add BrandMappingDetailPanel component for master-detail right panel"
```

---

## Task 4: Rewrite BrandMappingPage with Master-Detail Layout

**Files:**
- Rewrite: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor`
- Rewrite: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor.cs`
- Delete: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingList.razor`
- Delete: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingList.razor.cs`
- Delete: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDialog.razor`
- Delete: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDialog.razor.cs`

- [ ] **Step 1: Rewrite page code-behind**

Replace `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor.cs` with:

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class BrandMappingPage
{
    [Inject] private IBrandMatchService BrandMatchService { get; set; } = null!;
    [Inject] private IBrandService BrandService { get; set; } = null!;
    [Inject] private IMarketPlaceManager MarketPlaceManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<BrandDto> _brands = [];
    private List<BrandMarketPlaceMatchDto> _allMappings = [];
    private List<MarketPlace> _marketplaces = [];
    private BrandMappingSummaryDto _summary = new();
    private BrandDto? _selectedBrand;
    private List<BrandMarketPlaceMatchDto> _selectedBrandMappings = [];
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadAllData();
    }

    private async Task LoadAllData()
    {
        _isLoading = true;

        var brandsResult = await BrandService.GetBrandListDetails();
        if (brandsResult.Success && brandsResult.Data is not null)
            _brands = brandsResult.Data.Select(b => new BrandDto { Id = b.Id, Name = b.Name }).ToList();

        var mpResult = await MarketPlaceManager.GetAllAsync();
        if (mpResult.Success && mpResult.Data is not null)
            _marketplaces = mpResult.Data;

        var summaryResult = await BrandMatchService.GetBrandMappingsSummaryAsync();
        if (summaryResult.Success && summaryResult.Data is not null)
            _summary = summaryResult.Data;

        await LoadAllMappings();

        _isLoading = false;
    }

    private async Task LoadAllMappings()
    {
        _allMappings = [];
        foreach (var mp in _marketplaces)
        {
            var result = await BrandMatchService.GetAllBrandMappingsAsync(mp.Id);
            if (result.Success && result.Data is not null)
                _allMappings.AddRange(result.Data);
        }
    }

    private async Task OnBrandSelected(BrandDto brand)
    {
        _selectedBrand = brand;
        await LoadSelectedBrandMappings();
    }

    private async Task LoadSelectedBrandMappings()
    {
        if (_selectedBrand is null)
        {
            _selectedBrandMappings = [];
            return;
        }

        var result = await BrandMatchService.GetBrandMappingsByBrandIdAsync(_selectedBrand.Id);
        _selectedBrandMappings = result.Success && result.Data is not null ? result.Data : [];
    }

    private async Task OnMappingChanged()
    {
        await LoadAllMappings();
        await LoadSelectedBrandMappings();

        var summaryResult = await BrandMatchService.GetBrandMappingsSummaryAsync();
        if (summaryResult.Success && summaryResult.Data is not null)
            _summary = summaryResult.Data;

        StateHasChanged();
    }
}
```

- [ ] **Step 2: Rewrite page razor file**

Replace `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor` with:

```razor
@page "/marketplace/sync/brands"
@rendermode InteractiveServer
@using Entegrasyon.Entity.Dtos.Brand

<PageTitle>Marka Eşleştirme - Entegrasyon</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="py-4">
    <MudText Typo="Typo.h5" Class="mb-4">Marka Eşleştirme</MudText>

    <!-- Summary Stats -->
    <MudGrid Spacing="2" Class="mb-4">
        <MudItem xs="12" sm="4">
            <MudPaper Elevation="1" Class="pa-3">
                <MudText Typo="Typo.subtitle2">Toplam Marka</MudText>
                <MudText Typo="Typo.h6" Class="mt-1">@_summary.TotalBrands</MudText>
            </MudPaper>
        </MudItem>
        <MudItem xs="12" sm="4">
            <MudPaper Elevation="1" Class="pa-3">
                <MudText Typo="Typo.subtitle2" Color="Color.Success">Eşleştirilen</MudText>
                <MudText Typo="Typo.h6" Color="Color.Success" Class="mt-1">@_summary.MappedBrands</MudText>
            </MudPaper>
        </MudItem>
        <MudItem xs="12" sm="4">
            <MudPaper Elevation="1" Class="pa-3">
                <MudText Typo="Typo.subtitle2" Color="Color.Warning">Eksik</MudText>
                <MudText Typo="Typo.h6" Color="Color.Warning" Class="mt-1">@_summary.UnmappedBrands</MudText>
            </MudPaper>
        </MudItem>
    </MudGrid>

    @if (_isLoading)
    {
        <MudProgressLinear Indeterminate="true" Class="mb-4" />
    }
    else
    {
        <!-- Master-Detail Layout -->
        <MudGrid Spacing="2">
            <MudItem xs="12" md="5">
                <BrandMappingListPanel Brands="_brands"
                                       AllMappings="_allMappings"
                                       Marketplaces="_marketplaces"
                                       SelectedBrand="_selectedBrand"
                                       SelectedBrandChanged="OnBrandSelected" />
            </MudItem>
            <MudItem xs="12" md="7">
                <BrandMappingDetailPanel Brand="_selectedBrand"
                                         BrandMappings="_selectedBrandMappings"
                                         Marketplaces="_marketplaces"
                                         OnMappingChanged="OnMappingChanged" />
            </MudItem>
        </MudGrid>
    }
</MudContainer>
```

- [ ] **Step 3: Delete old components**

```bash
rm Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingList.razor \
   Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingList.razor.cs \
   Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDialog.razor \
   Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDialog.razor.cs
```

- [ ] **Step 4: Verify build compiles**

Run: `dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj --no-restore -v minimal`
Expected: Build succeeded. Fix any compilation errors if they arise.

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor \
      Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingPage.razor.cs
git add -u Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingList.razor \
           Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingList.razor.cs \
           Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDialog.razor \
           Application/Entegrasyon.Blazor/Features/MarketplaceSync/BrandMappingDialog.razor.cs
git commit -m "feat(brand): rewrite BrandMappingPage with master-detail layout, remove old components"
```

---

## Task 5: Fix CategorySync Marketplace Details Column

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor`

- [ ] **Step 1: Identify the current marketplace details column**

In `CategorySync.razor`, find the "Marketplace Detayları" `TemplateColumn`. The current implementation only shows the last marketplace or a single link.

- [ ] **Step 2: Replace the marketplace details column**

Replace the existing marketplace details TemplateColumn with a version that shows all marketplace links as chips:

```razor
<TemplateColumn Title="Marketplace Detayları">
    <CellTemplate>
        @if (context.Item.MarketplaceLinks is not null && context.Item.MarketplaceLinks.Any(l => l.IsActive))
        {
            <MudStack Row="true" Spacing="1" AlignItems="AlignItems.Center">
                @foreach (var link in context.Item.MarketplaceLinks.Where(l => l.IsActive))
                {
                    <MudChip T="string" Size="Size.Small" Color="Color.Primary" Variant="Variant.Outlined">
                        <MarketplaceLogo Name="@(link.MarketPlace?.Name ?? "")" Size="Size.Small" Class="mr-1" />
                        @(link.MarketPlace?.Name ?? "MP#" + link.MarketPlaceId)
                    </MudChip>
                }
                <MudText Typo="Typo.caption" Color="Color.Secondary">
                    (@context.Item.MarketplaceLinks.Count(l => l.IsActive) eşleşme)
                </MudText>
            </MudStack>
        }
        else
        {
            <MudChip T="string" Size="Size.Small" Color="Color.Warning" Variant="Variant.Text">
                <MudIcon Icon="@Icons.Material.Filled.Warning" Size="Size.Small" Class="mr-1" />
                Eşleştirilmemiş
            </MudChip>
        }
    </CellTemplate>
</TemplateColumn>
```

- [ ] **Step 3: Verify the CategorySync code-behind loads MarketPlace navigation property**

Check that `_categories` loading includes `.Include(c => c.MarketplaceLinks).ThenInclude(l => l.MarketPlace)` in the query. If not, update the relevant business method or the data loading in `CategorySync.razor.cs`.

- [ ] **Step 4: Verify build compiles**

Run: `dotnet build Application/Entegrasyon.Blazor/Entegrasyon.Blazor.csproj --no-restore -v minimal`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor
git commit -m "fix(category-sync): show all marketplace chips instead of only last match"
```

---

## Task 6: Run Full Test Suite & Final Verification

- [ ] **Step 1: Run unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj -v minimal`
Expected: All tests pass

- [ ] **Step 2: Run integration tests**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj -v minimal`
Expected: All tests pass (including new `GetBrandMappingsByBrandId` tests)

- [ ] **Step 3: Verify complete build**

Run: `dotnet build Entegrasyon.sln -v minimal`
Expected: Build succeeded with 0 errors

- [ ] **Step 4: Manual verification checklist**

Start the app and verify:
- [ ] Navigate to `/marketplace/sync/brands` — master-detail layout visible
- [ ] Search brands in left panel — filters correctly
- [ ] Click a brand — right panel shows all marketplaces
- [ ] Create a mapping via autocomplete — saves and refreshes
- [ ] Delete a mapping — removes and refreshes
- [ ] Navigate to `/marketplace/sync/categories` — all marketplace chips visible per category
- [ ] Categories with no mapping show "Eşleştirilmemiş" badge
