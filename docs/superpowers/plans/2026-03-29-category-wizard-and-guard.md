# Category Wizard + Unsaved Changes Guard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Kategori ekleme/duzenleme islemini modal'dan 3 adimli wizard sayfasina tasimak ve tum form sayfalari icin yeniden kullanilabilir unsaved changes guard altyapisi olusturmak.

**Architecture:** UnsavedChangesGuard reusable Blazor component'i NavigationLock + JS interop ile hem uygulama ici hem tarayici seviyesinde koruma saglar. Kategori wizard'i MudStepper ile 3 adim (Genel, Ozellikler, Marketplace) sunar. Mevcut CategoryDialog silinir, CategoryEdit wizard'a donusturulur. AllowCustom kategori bazindan kaldirilip read-only gosterilir.

**Tech Stack:** Blazor Server (.NET 8), MudBlazor (MudStepper, MudDialog), JS Interop, EF Core 8, xUnit + FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-29-category-wizard-and-guard-design.md`

---

## File Structure

| Dosya | Islem | Sorumluluk |
|-------|-------|-----------|
| `wwwroot/js/unsaved-changes.js` | Olustur | beforeunload JS listener |
| `Components/Shared/UnsavedChangesGuard.razor` | Olustur | Guard UI (NavigationLock) |
| `Components/Shared/UnsavedChangesGuard.razor.cs` | Olustur | Guard logic (JS interop + ConfirmDialog) |
| `Features/Categories/CategoryWizard.razor` | Olustur | Wizard ana sayfa (MudStepper) |
| `Features/Categories/CategoryWizard.razor.cs` | Olustur | Wizard state, navigasyon, kaydetme |
| `Features/Categories/CategoryWizardGeneralStep.razor` | Olustur | Adim 1: Genel bilgiler formu |
| `Features/Categories/CategoryWizardGeneralStep.razor.cs` | Olustur | Adim 1: Form state |
| `Features/Categories/CategoryWizardAttributesStep.razor` | Olustur | Adim 2: Attribute yonetimi |
| `Features/Categories/CategoryWizardAttributesStep.razor.cs` | Olustur | Adim 2: Attribute CRUD logic |
| `Features/Categories/CategoryWizardMarketplaceStep.razor` | Olustur | Adim 3: Marketplace eslestirme |
| `Features/Categories/CategoryWizardMarketplaceStep.razor.cs` | Olustur | Adim 3: Eslestirme logic |
| `Features/Categories/CategoryDialog.razor` | Sil | Modal kaldirilacak |
| `Features/Categories/CategoryDialog.razor.cs` | Sil | Modal kaldirilacak |
| `Features/Categories/CategoryEdit.razor` | Sil | Wizard yerine gecti |
| `Features/Categories/CategoryEdit.razor.cs` | Sil | Wizard yerine gecti |
| `Features/Categories/Categories.razor` | Degistir | Add butonu NavigateTo olacak |
| `Features/Categories/Categories.razor.cs` | Degistir | Dialog metodu kaldirilacak |
| `Features/Attributes/AttributesPage.razor.cs` | Degistir | Query param desteyi |
| `Features/Attributes/AttributeDetailPanel.razor.cs` | Degistir | MarketplaceId ile tab secimi |

Not: Tum dosya yollari `Application/Entegrasyon.Blazor/` prefix'i iledir.

---

### Task 1: UnsavedChangesGuard — JS Interop + Component

**Files:**
- Create: `Application/Entegrasyon.Blazor/wwwroot/js/unsaved-changes.js`
- Create: `Application/Entegrasyon.Blazor/Components/Shared/UnsavedChangesGuard.razor`
- Create: `Application/Entegrasyon.Blazor/Components/Shared/UnsavedChangesGuard.razor.cs`

- [ ] **Step 1: Create JS interop file**

Create `Application/Entegrasyon.Blazor/wwwroot/js/unsaved-changes.js`:

```javascript
window.unsavedChangesInterop = {
    _handler: null,

    addBeforeUnloadListener: function () {
        if (this._handler) return;
        this._handler = function (e) {
            e.preventDefault();
            e.returnValue = '';
        };
        window.addEventListener('beforeunload', this._handler);
    },

    removeBeforeUnloadListener: function () {
        if (!this._handler) return;
        window.removeEventListener('beforeunload', this._handler);
        this._handler = null;
    }
};
```

- [ ] **Step 2: Register JS file in App.razor or _Host.cshtml**

Find the main layout file that includes JS scripts. Add before closing `</body>`:

```html
<script src="js/unsaved-changes.js"></script>
```

Check whether the project uses `App.razor`, `_Host.cshtml`, or `_Layout.cshtml` for script inclusion. Search for existing `<script src=` tags to find the right file.

- [ ] **Step 3: Create UnsavedChangesGuard.razor**

Create `Application/Entegrasyon.Blazor/Components/Shared/UnsavedChangesGuard.razor`:

```razor
@if (IsDirty)
{
    <NavigationLock ConfirmExternalNavigation="true" OnBeforeInternalNavigation="OnBeforeInternalNavigation" />
}
```

- [ ] **Step 4: Create UnsavedChangesGuard.razor.cs**

Create `Application/Entegrasyon.Blazor/Components/Shared/UnsavedChangesGuard.razor.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class UnsavedChangesGuard : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Parameter] public bool IsDirty { get; set; }

    [Parameter] public string Message { get; set; } =
        "Kaydedilmemiş değişiklikleriniz var. Sayfadan ayrılmak istediğinize emin misiniz?";

    private bool _previousIsDirty;

    protected override async Task OnParametersSetAsync()
    {
        if (IsDirty != _previousIsDirty)
        {
            _previousIsDirty = IsDirty;
            if (IsDirty)
                await JsRuntime.InvokeVoidAsync("unsavedChangesInterop.addBeforeUnloadListener");
            else
                await JsRuntime.InvokeVoidAsync("unsavedChangesInterop.removeBeforeUnloadListener");
        }
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        if (!IsDirty) return;

        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.ContentText, Message },
            { x => x.ButtonText, "Evet, Ayrıl" },
            { x => x.Color, Color.Warning },
            { x => x.Icon, Icons.Material.Filled.Warning }
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Kaydedilmemiş Değişiklikler", parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            context.PreventNavigation();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("unsavedChangesInterop.removeBeforeUnloadListener");
        }
        catch (JSDisconnectedException)
        {
            // Circuit already disconnected — ignore
        }
    }
}
```

- [ ] **Step 5: Build to verify compilation**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Blazor/wwwroot/js/unsaved-changes.js \
       Application/Entegrasyon.Blazor/Components/Shared/UnsavedChangesGuard.razor \
       Application/Entegrasyon.Blazor/Components/Shared/UnsavedChangesGuard.razor.cs
git commit -m "feat: add UnsavedChangesGuard component with JS interop"
```

Note: Also add the script tag file if modified (App.razor / _Host.cshtml).

---

### Task 2: CategoryWizard — General Step (Adim 1)

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardGeneralStep.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardGeneralStep.razor.cs`

- [ ] **Step 1: Create CategoryWizardGeneralStep.razor.cs**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryWizardGeneralStep : ComponentBase
{
    [Inject] private ICategoryService CategoryManager { get; set; } = null!;

    [Parameter] public string Name { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> NameChanged { get; set; }

    [Parameter] public int? ParentCategoryId { get; set; }
    [Parameter] public EventCallback<int?> ParentCategoryIdChanged { get; set; }

    [Parameter] public bool IsFavorite { get; set; }
    [Parameter] public EventCallback<bool> IsFavoriteChanged { get; set; }

    [Parameter] public decimal? DefaultVatRate { get; set; }
    [Parameter] public EventCallback<decimal?> DefaultVatRateChanged { get; set; }

    [Parameter] public int? EditCategoryId { get; set; }
    [Parameter] public EventCallback OnFieldChanged { get; set; }

    private List<Category> _availableParents = [];
    private MudBlazor.MudForm? _form;

    protected override async Task OnInitializedAsync()
    {
        _availableParents = await CategoryManager.GetValidParentCandidatesAsync(EditCategoryId);
    }

    public async Task<bool> ValidateAsync()
    {
        if (_form is null) return false;
        await _form.Validate();
        return _form.IsValid;
    }

    private async Task OnNameChanged(string value)
    {
        Name = value;
        await NameChanged.InvokeAsync(value);
        await OnFieldChanged.InvokeAsync();
    }

    private async Task OnParentChanged(int? value)
    {
        ParentCategoryId = value;
        await ParentCategoryIdChanged.InvokeAsync(value);
        await OnFieldChanged.InvokeAsync();
    }

    private async Task OnFavoriteChanged(bool value)
    {
        IsFavorite = value;
        await IsFavoriteChanged.InvokeAsync(value);
        await OnFieldChanged.InvokeAsync();
    }

    private async Task OnVatRateChanged(decimal? value)
    {
        DefaultVatRate = value;
        await DefaultVatRateChanged.InvokeAsync(value);
        await OnFieldChanged.InvokeAsync();
    }
}
```

- [ ] **Step 2: Create CategoryWizardGeneralStep.razor**

```razor
<MudForm @ref="_form">
    <MudGrid>
        <MudItem xs="12">
            <MudTextField Value="Name"
                          ValueChanged="OnNameChanged"
                          T="string"
                          Label="Kategori Adı"
                          Required="true"
                          RequiredError="Kategori adı zorunludur."
                          Variant="Variant.Outlined" />
        </MudItem>

        <MudItem xs="12">
            <MudSelect Value="ParentCategoryId"
                       ValueChanged="OnParentChanged"
                       T="int?"
                       Label="Üst Kategori (Opsiyonel)"
                       Clearable="true"
                       Variant="Variant.Outlined"
                       AnchorOrigin="Origin.BottomCenter">
                <MudSelectItem T="int?" Value="@((int?)null)">
                    <em>Bir üst kategori seçebilirsiniz.</em>
                </MudSelectItem>
                @foreach (var cat in _availableParents)
                {
                    <MudSelectItem T="int?" Value="@((int?)cat.Id)">@cat.Name</MudSelectItem>
                }
            </MudSelect>
            <MudText Typo="Typo.caption" Color="Color.Tertiary" Class="mt-1">
                Yalnızca özellik veya pazar yeri eşleştirmesi içermeyen kategoriler üst kategori olabilir.
            </MudText>
        </MudItem>

        <MudItem xs="6">
            <MudCheckBox Value="IsFavorite"
                         ValueChanged="OnFavoriteChanged"
                         T="bool"
                         Label="Favori"
                         Color="Color.Warning" />
        </MudItem>

        <MudItem xs="6">
            <MudSelect Value="DefaultVatRate"
                       ValueChanged="OnVatRateChanged"
                       T="decimal?"
                       Label="Varsayılan KDV Oranı"
                       Clearable="true"
                       Variant="Variant.Outlined">
                <MudSelectItem T="decimal?" Value="@((decimal?)1)">%1</MudSelectItem>
                <MudSelectItem T="decimal?" Value="@((decimal?)10)">%10</MudSelectItem>
                <MudSelectItem T="decimal?" Value="@((decimal?)20)">%20</MudSelectItem>
            </MudSelect>
        </MudItem>
    </MudGrid>
</MudForm>
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardGeneralStep.razor \
       Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardGeneralStep.razor.cs
git commit -m "feat(categories): add CategoryWizardGeneralStep component"
```

---

### Task 3: CategoryWizard — Attributes Step (Adim 2) + AllowCustom Fix

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardAttributesStep.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardAttributesStep.razor.cs`

This step is ported from CategoryEdit.razor lines 78-222 and CategoryEdit.razor.cs attribute logic. The key difference: AllowCustom switch is commented out, replaced with read-only MudChip.

- [ ] **Step 1: Create CategoryWizardAttributesStep.razor.cs**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryWizardAttributesStep : ComponentBase
{
    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;

    [Parameter] public List<AttributeModel> Attributes { get; set; } = [];
    [Parameter] public EventCallback<List<AttributeModel>> AttributesChanged { get; set; }
    [Parameter] public EventCallback OnFieldChanged { get; set; }

    private CategoryAttribute? _selectedExistingAttribute;
    private string _newAttributeKey = string.Empty;
    private string _newAttributeHumanized = string.Empty;
    private List<CategoryAttribute> _allAttributes = [];

    protected override async Task OnInitializedAsync()
    {
        _allAttributes = await AttributeManager.GetAllAttributesAsync();
    }

    public bool Validate()
    {
        if (Attributes.Count(a => a.IsVarianter) > 1) return false;
        if (Attributes.Count(a => a.IsSlicer) > 1) return false;
        if (Attributes.Any(a => a.IsVarianter && a.IsSlicer)) return false;
        return true;
    }

    public string? GetValidationError()
    {
        if (Attributes.Count(a => a.IsVarianter) > 1)
            return "Bir kategoride en fazla 1 adet varyant özelliği olabilir.";
        if (Attributes.Count(a => a.IsSlicer) > 1)
            return "Bir kategoride en fazla 1 adet dilimleyici özellik olabilir.";
        if (Attributes.Any(a => a.IsVarianter && a.IsSlicer))
            return "Bir özellik hem varyant hem de dilimleyici olamaz.";
        return null;
    }

    private async Task OnExistingAttributeSelected(CategoryAttribute? attr)
    {
        if (attr is null) return;
        if (Attributes.Any(a => a.Id == attr.Id)) return;

        var model = new AttributeModel
        {
            Id = attr.Id,
            CategoryAttributeKey = attr.CategoryAttributeKey,
            CategoryAttributeHumanized = attr.CategoryAttributeHumanized,
            AllowCustom = attr.AllowCustom,
            Values = attr.CategoryAttributeValues
                .Select(v => new ValueModel { Id = v.Id, Value = v.Value }).ToList()
        };

        Attributes.Add(model);
        _selectedExistingAttribute = null;
        await AttributesChanged.InvokeAsync(Attributes);
        await OnFieldChanged.InvokeAsync();
    }

    private async Task AddNewAttribute()
    {
        if (string.IsNullOrWhiteSpace(_newAttributeKey) || string.IsNullOrWhiteSpace(_newAttributeHumanized)) return;

        var model = new AttributeModel
        {
            Id = 0,
            CategoryAttributeKey = _newAttributeKey.Trim(),
            CategoryAttributeHumanized = _newAttributeHumanized.Trim(),
            AllowCustom = true
        };

        Attributes.Add(model);
        _newAttributeKey = string.Empty;
        _newAttributeHumanized = string.Empty;
        await AttributesChanged.InvokeAsync(Attributes);
        await OnFieldChanged.InvokeAsync();
    }

    private async Task RemoveAttribute(AttributeModel attr)
    {
        Attributes.Remove(attr);
        await AttributesChanged.InvokeAsync(Attributes);
        await OnFieldChanged.InvokeAsync();
    }

    private async Task AddValue(AttributeModel attr)
    {
        if (string.IsNullOrWhiteSpace(attr.NewValueText)) return;
        if (attr.Values.Any(v => v.Value.Equals(attr.NewValueText.Trim(), StringComparison.OrdinalIgnoreCase))) return;

        attr.Values.Add(new ValueModel { Id = 0, Value = attr.NewValueText.Trim() });
        attr.NewValueText = string.Empty;
        await OnFieldChanged.InvokeAsync();
    }

    private async Task RemoveValue(AttributeModel attr, ValueModel val)
    {
        attr.Values.Remove(val);
        await OnFieldChanged.InvokeAsync();
    }

    private async Task OnVarianterChanged(AttributeModel attr, bool value)
    {
        if (value) attr.IsSlicer = false;
        attr.IsVarianter = value;
        await OnFieldChanged.InvokeAsync();
    }

    private async Task OnSlicerChanged(AttributeModel attr, bool value)
    {
        if (value) attr.IsVarianter = false;
        attr.IsSlicer = value;
        await OnFieldChanged.InvokeAsync();
    }
}

public class AttributeModel
{
    public int Id { get; set; }
    public string CategoryAttributeKey { get; set; } = string.Empty;
    public string CategoryAttributeHumanized { get; set; } = string.Empty;
    public bool AllowCustom { get; set; } = true;
    public bool IsRequired { get; set; }
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
    public List<ValueModel> Values { get; set; } = [];
    public string NewValueText { get; set; } = string.Empty;
}

public class ValueModel
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create CategoryWizardAttributesStep.razor**

Port from CategoryEdit.razor lines 78-222 with AllowCustom switch commented out:

```razor
<MudText Typo="Typo.h6" Class="mb-4">Kategori Özellikleri</MudText>

@* Mevcut ozellik ekle *@
<MudGrid Class="mb-4">
    <MudItem xs="8">
        <MudAutocomplete T="CategoryAttribute"
                         Label="Mevcut Özellik Ekle"
                         SearchFunc="@(async (s, ct) => _allAttributes.Where(a => a.CategoryAttributeHumanized.Contains(s ?? "", StringComparison.OrdinalIgnoreCase)))"
                         ToStringFunc="@(a => a?.CategoryAttributeHumanized ?? "")"
                         ValueChanged="OnExistingAttributeSelected"
                         Variant="Variant.Outlined"
                         Dense="true" />
    </MudItem>
</MudGrid>

@* Yeni ozellik olustur *@
<MudGrid Class="mb-4">
    <MudItem xs="4">
        <MudTextField @bind-Value="_newAttributeKey" Label="Anahtar (key)" Variant="Variant.Outlined" Dense="true" />
    </MudItem>
    <MudItem xs="4">
        <MudTextField @bind-Value="_newAttributeHumanized" Label="Görünen Ad" Variant="Variant.Outlined" Dense="true" />
    </MudItem>
    <MudItem xs="4" Class="d-flex align-center">
        <MudButton OnClick="AddNewAttribute" Variant="Variant.Filled" Color="Color.Primary" Size="Size.Small">
            Yeni Özellik Oluştur
        </MudButton>
    </MudItem>
</MudGrid>

<MudDivider Class="my-4" />

@* Eklenmis ozellikler listesi *@
@if (Attributes.Count == 0)
{
    <MudAlert Severity="Severity.Info" Dense="true">Henüz özellik eklenmedi.</MudAlert>
}
else
{
    @foreach (var attr in Attributes)
    {
        <MudPaper Class="pa-4 mb-3" Elevation="1">
            <MudGrid>
                <MudItem xs="10">
                    <MudText Typo="Typo.subtitle1"><b>@attr.CategoryAttributeHumanized</b> (@attr.CategoryAttributeKey)</MudText>
                </MudItem>
                <MudItem xs="2" Class="d-flex justify-end">
                    <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                   Color="Color.Error"
                                   Size="Size.Small"
                                   OnClick="() => RemoveAttribute(attr)" />
                </MudItem>

                <MudItem xs="12">
                    <MudStack Row="true" Spacing="4" AlignItems="AlignItems.Center">
                        <MudCheckBox @bind-Value="attr.IsRequired" T="bool" Label="Zorunlu" Color="Color.Primary" Dense="true" />

                        <MudCheckBox Value="attr.IsVarianter" T="bool" Label="Varyant"
                                     ValueChanged="(bool v) => OnVarianterChanged(attr, v)"
                                     Color="Color.Secondary" Dense="true" />

                        <MudCheckBox Value="attr.IsSlicer" T="bool" Label="Dilimleyici"
                                     ValueChanged="(bool v) => OnSlicerChanged(attr, v)"
                                     Color="Color.Tertiary" Dense="true" />

                        @* AllowCustom: Kategori bazinda degil, attribute bazinda global ayarlanir.
                           Gerekirse /attributes sayfasindan yonetilir.
                           Ileride kategori bazinda ihtiyac olursa bu blok acilabilir.
                        <MudSwitch @bind-Value="attr.AllowCustom"
                                   T="bool"
                                   Label="Serbest Değer"
                                   Color="Color.Tertiary" /> *@
                        <MudChip T="string" Size="Size.Small"
                                 Color="@(attr.AllowCustom ? Color.Info : Color.Default)">
                            Serbest değer: @(attr.AllowCustom ? "Evet" : "Hayır")
                        </MudChip>
                    </MudStack>
                </MudItem>

                @* Degerler (chips) *@
                <MudItem xs="12">
                    <MudStack Row="true" Wrap="Wrap.Wrap" Spacing="1">
                        @foreach (var val in attr.Values)
                        {
                            <MudChip T="string" Color="Color.Primary" Size="Size.Small"
                                     OnClose="() => RemoveValue(attr, val)">
                                @val.Value
                            </MudChip>
                        }
                    </MudStack>
                </MudItem>

                @* Yeni deger ekle *@
                <MudItem xs="8">
                    <MudTextField @bind-Value="attr.NewValueText"
                                  Label="Yeni Değer"
                                  Variant="Variant.Outlined"
                                  Dense="true"
                                  Margin="Margin.Dense" />
                </MudItem>
                <MudItem xs="4" Class="d-flex align-center">
                    <MudIconButton Icon="@Icons.Material.Filled.Add"
                                   Color="Color.Primary"
                                   Size="Size.Small"
                                   OnClick="() => AddValue(attr)" />
                </MudItem>
            </MudGrid>
        </MudPaper>
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardAttributesStep.razor \
       Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardAttributesStep.razor.cs
git commit -m "feat(categories): add CategoryWizardAttributesStep with AllowCustom read-only"
```

---

### Task 4: CategoryWizard — Marketplace Step (Adim 3)

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardMarketplaceStep.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardMarketplaceStep.razor.cs`

- [ ] **Step 1: Create CategoryWizardMarketplaceStep.razor.cs**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.MarketPlace;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryWizardMarketplaceStep : ComponentBase
{
    [Inject] private ICategoryMatchService MatchService { get; set; } = null!;
    [Inject] private IMarketplaceSearchService SearchService { get; set; } = null!;
    [Inject] private IDbContextFactory<Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts.IntegrationDbContext> ContextFactory { get; set; } = null!;

    [Parameter] public int CategoryId { get; set; }
    [Parameter] public int? SelectedMarketplaceId { get; set; }
    [Parameter] public EventCallback<int?> SelectedMarketplaceIdChanged { get; set; }
    [Parameter] public EventCallback OnFieldChanged { get; set; }

    private List<MarketPlace> _marketplaces = [];
    private List<CategoryMarketplaceMappingDto> _existingMappings = [];
    private int? _selectedMarketplace;
    private int? _marketplaceCategoryId;
    private string? _marketplaceCategoryName;
    private string _searchText = string.Empty;
    private List<MarketplaceCategorySearchResult> _searchResults = [];
    private bool _isSearching;

    protected override async Task OnInitializedAsync()
    {
        using var dbContext = ContextFactory.CreateDbContext();
        _marketplaces = await dbContext.MarketPlaces.Where(m => !m.IsDeleted && m.IsEnabled).ToListAsync();

        if (CategoryId > 0)
        {
            // Duzenleme modunda mevcut eslestirmeleri yukle
            foreach (var mp in _marketplaces)
            {
                var mappings = await MatchService.GetAllCategoryMappingsAsync(mp.Id);
                _existingMappings.AddRange(mappings.Where(m => m.CategoryId == CategoryId));
            }
        }
    }

    private async Task OnMarketplaceSelected(int? value)
    {
        _selectedMarketplace = value;
        SelectedMarketplaceId = value;
        await SelectedMarketplaceIdChanged.InvokeAsync(value);
        _searchResults = [];
        _marketplaceCategoryId = null;
        _marketplaceCategoryName = null;
    }

    private async Task SearchMarketplaceCategories()
    {
        if (_selectedMarketplace is null || string.IsNullOrWhiteSpace(_searchText)) return;

        _isSearching = true;
        try
        {
            var result = await SearchService.SearchCategoriesAsync(_selectedMarketplace.Value, _searchText);
            _searchResults = result.Success ? result.Data : [];
        }
        finally
        {
            _isSearching = false;
        }
    }

    private void OnMarketplaceCategorySelected(MarketplaceCategorySearchResult cat)
    {
        _marketplaceCategoryId = cat.Id;
        _marketplaceCategoryName = cat.FullPath ?? cat.Name;
    }

    public bool HasMapping => _marketplaceCategoryId.HasValue && _selectedMarketplace.HasValue;

    public CreateCategoryMarketplaceMatchDto? GetMappingDto()
    {
        if (!HasMapping || CategoryId <= 0) return null;

        return new CreateCategoryMarketplaceMatchDto
        {
            ApplicationCategoryId = CategoryId,
            MarketPlaceId = _selectedMarketplace!.Value,
            MarketPlaceCategoryId = _marketplaceCategoryId!.Value,
            MarketPlaceCategoryName = _marketplaceCategoryName
        };
    }
}
```

- [ ] **Step 2: Create CategoryWizardMarketplaceStep.razor**

```razor
<MudText Typo="Typo.h6" Class="mb-4">Pazar Yeri Eşleştirme (Opsiyonel)</MudText>

<MudAlert Severity="Severity.Info" Dense="true" Class="mb-4">
    Bu adım opsiyoneldir. Kategoriyi bir pazar yerine eşleştirmek isterseniz aşağıdan seçim yapabilirsiniz.
</MudAlert>

@* Mevcut eslestirmeler *@
@if (_existingMappings.Count > 0)
{
    <MudText Typo="Typo.subtitle1" Class="mb-2"><b>Mevcut Eşleştirmeler</b></MudText>
    <MudSimpleTable Dense="true" Hover="true" Bordered="true" Class="mb-4">
        <thead>
            <tr>
                <th>Pazar Yeri</th>
                <th>Eşleşen Kategori</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var mapping in _existingMappings)
            {
                <tr>
                    <td>@(_marketplaces.FirstOrDefault(m => m.Id == mapping.MarketPlaceId)?.Name ?? "?")</td>
                    <td>@mapping.MarketPlaceCategoryName</td>
                </tr>
            }
        </tbody>
    </MudSimpleTable>
}

@* Yeni eslestirme *@
<MudGrid>
    <MudItem xs="12">
        <MudSelect T="int?" Value="_selectedMarketplace" ValueChanged="OnMarketplaceSelected"
                   Label="Pazar Yeri Seçin" Variant="Variant.Outlined" Clearable="true">
            @foreach (var mp in _marketplaces)
            {
                <MudSelectItem T="int?" Value="@((int?)mp.Id)">@mp.Name</MudSelectItem>
            }
        </MudSelect>
    </MudItem>

    @if (_selectedMarketplace.HasValue)
    {
        <MudItem xs="8">
            <MudTextField @bind-Value="_searchText"
                          Label="Pazar yeri kategorisi ara..."
                          Variant="Variant.Outlined"
                          Adornment="Adornment.End"
                          AdornmentIcon="@Icons.Material.Filled.Search"
                          OnAdornmentClick="SearchMarketplaceCategories"
                          OnKeyDown="@(async (e) => { if (e.Key == "Enter") await SearchMarketplaceCategories(); })" />
        </MudItem>
        <MudItem xs="4" Class="d-flex align-center">
            <MudButton OnClick="SearchMarketplaceCategories" Variant="Variant.Filled" Color="Color.Primary"
                       Disabled="_isSearching">
                @if (_isSearching) { <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" /> }
                Ara
            </MudButton>
        </MudItem>

        @if (_searchResults.Count > 0)
        {
            <MudItem xs="12">
                <MudList T="MarketplaceCategorySearchResult" Dense="true" Class="border-solid border rounded mud-border-lines-default" Style="max-height: 300px; overflow-y: auto;">
                    @foreach (var cat in _searchResults)
                    {
                        <MudListItem T="MarketplaceCategorySearchResult"
                                     OnClick="() => OnMarketplaceCategorySelected(cat)"
                                     Class="@(_marketplaceCategoryId == cat.Id ? "mud-primary-text" : "")">
                            @(cat.FullPath ?? cat.Name)
                        </MudListItem>
                    }
                </MudList>
            </MudItem>
        }

        @if (_marketplaceCategoryName is not null)
        {
            <MudItem xs="12">
                <MudAlert Severity="Severity.Success" Dense="true">
                    Seçilen: <b>@_marketplaceCategoryName</b>
                </MudAlert>
            </MudItem>
        }
    }
</MudGrid>
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardMarketplaceStep.razor \
       Application/Entegrasyon.Blazor/Features/Categories/CategoryWizardMarketplaceStep.razor.cs
git commit -m "feat(categories): add CategoryWizardMarketplaceStep component"
```

---

### Task 5: CategoryWizard — Main Page Assembly + Guard

**Files:**
- Create: `Application/Entegrasyon.Blazor/Features/Categories/CategoryWizard.razor`
- Create: `Application/Entegrasyon.Blazor/Features/Categories/CategoryWizard.razor.cs`

- [ ] **Step 1: Create CategoryWizard.razor.cs**

```csharp
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryWizard : ComponentBase
{
    [Inject] private ICategoryService CategoryManager { get; set; } = null!;
    [Inject] private ICategoryAttributeCategoryManager AttributeCategoryManager { get; set; } = null!;
    [Inject] private ICategoryMatchService MatchService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Parameter] public int? Id { get; set; }

    private bool IsEditMode => Id is > 0;
    private int _stepIndex;
    private bool _isDirty;
    private bool _isSaving;

    // Step 1: General
    private string _name = string.Empty;
    private int? _parentCategoryId;
    private bool _isFavorite;
    private decimal? _defaultVatRate;

    // Step 2: Attributes
    private List<AttributeModel> _attributes = [];

    // Step 3: Marketplace
    private int _savedCategoryId;
    private int? _selectedMarketplaceId;

    // Step component refs
    private CategoryWizardGeneralStep? _generalStep;
    private CategoryWizardAttributesStep? _attributesStep;
    private CategoryWizardMarketplaceStep? _marketplaceStep;

    protected override async Task OnInitializedAsync()
    {
        if (IsEditMode)
        {
            await LoadCategoryData();
        }
    }

    private async Task LoadCategoryData()
    {
        var result = await CategoryManager.GetCategoryEditPageData(Id!.Value);
        if (!result.Success || result.Data is null)
        {
            Snackbar.Add("Kategori bulunamadı.", Severity.Error);
            NavigationManager.NavigateTo("/categories");
            return;
        }

        var data = result.Data;
        _name = data.Category.Name;
        _parentCategoryId = data.Category.SuperCategoryId;
        _isFavorite = data.Category.IsFavorite;
        _defaultVatRate = data.Category.DefaultVatRate;
        _savedCategoryId = data.Category.Id;

        _attributes = data.CategoryAttributes.Select(a => new AttributeModel
        {
            Id = a.Id,
            CategoryAttributeKey = a.CategoryAttributeKey,
            CategoryAttributeHumanized = a.CategoryAttributeHumanized,
            AllowCustom = a.AllowCustom,
            IsRequired = a.IsRequired,
            IsVarianter = a.IsVarianter,
            IsSlicer = a.IsSlicer,
            Values = a.Values.Select(v => new ValueModel { Id = v.Id, Value = v.Value }).ToList()
        }).ToList();
    }

    private void MarkDirty() => _isDirty = true;

    private async Task NextStep()
    {
        if (_stepIndex == 0)
        {
            if (_generalStep is null || !await _generalStep.ValidateAsync())
            {
                Snackbar.Add("Lütfen zorunlu alanları doldurun.", Severity.Warning);
                return;
            }
        }
        else if (_stepIndex == 1)
        {
            if (_attributesStep is not null)
            {
                var error = _attributesStep.GetValidationError();
                if (error is not null)
                {
                    Snackbar.Add(error, Severity.Warning);
                    return;
                }
            }
        }

        if (_stepIndex < 2) _stepIndex++;
    }

    private void PreviousStep()
    {
        if (_stepIndex > 0) _stepIndex--;
    }

    private async Task Save()
    {
        if (_isSaving) return;
        _isSaving = true;

        try
        {
            if (IsEditMode)
                await UpdateCategory();
            else
                await CreateCategory();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task CreateCategory()
    {
        var dto = new AddCategoryDto
        {
            Name = _name,
            SuperCategoryId = _parentCategoryId,
            IsFavorite = _isFavorite,
            DefaultVatRate = _defaultVatRate
        };

        var result = await CategoryManager.AddCategory(dto);
        if (!result.Success)
        {
            Snackbar.Add(result.Message, Severity.Error);
            return;
        }

        _savedCategoryId = result.Data.Id;

        // Attribute'lari ekle
        if (_attributes.Count > 0)
        {
            var attrDtos = _attributes.Select(ToAddCategoryAttributeDto).ToList();
            var attrResult = await AttributeCategoryManager.AddCategoryAttributeForCategory(_savedCategoryId, attrDtos);
            if (!attrResult.Success)
            {
                Snackbar.Add($"Kategori oluşturuldu ama özellikler eklenemedi: {attrResult.Message}", Severity.Warning);
            }
        }

        // Marketplace eslestirme
        await SaveMarketplaceMapping();

        Snackbar.Add("Kategori başarıyla oluşturuldu.", Severity.Success);
        _isDirty = false;
        await NavigateAfterSave();
    }

    private async Task UpdateCategory()
    {
        var dto = new EditCategoryDto
        {
            Id = Id!.Value,
            Name = _name,
            SuperCategoryId = _parentCategoryId,
            IsFavorite = _isFavorite,
            DefaultVatRate = _defaultVatRate
        };

        var result = await CategoryManager.UpdateCategory(dto);
        if (!result.Success)
        {
            Snackbar.Add(result.Message, Severity.Error);
            return;
        }

        // Attribute'lari guncelle
        var attrDtos = _attributes.Select(ToAddCategoryAttributeDto).ToList();
        var attrResult = await AttributeCategoryManager.AddCategoryAttributeForCategory(Id!.Value, attrDtos);
        if (!attrResult.Success)
        {
            Snackbar.Add($"Kategori güncellendi ama özellikler kaydedilemedi: {attrResult.Message}", Severity.Warning);
        }

        // Marketplace eslestirme
        await SaveMarketplaceMapping();

        Snackbar.Add("Kategori başarıyla güncellendi.", Severity.Success);
        _isDirty = false;
        await NavigateAfterSave();
    }

    private async Task SaveMarketplaceMapping()
    {
        if (_marketplaceStep is null || !_marketplaceStep.HasMapping) return;

        var mappingDto = _marketplaceStep.GetMappingDto();
        if (mappingDto is null) return;

        mappingDto.ApplicationCategoryId = _savedCategoryId;
        var mappingResult = await MatchService.CreateCategoryMappingAsync(mappingDto);
        if (!mappingResult.Success)
        {
            Snackbar.Add($"Pazar yeri eşleştirmesi yapılamadı: {mappingResult.Message}", Severity.Warning);
        }
    }

    private async Task NavigateAfterSave()
    {
        if (_selectedMarketplaceId.HasValue)
        {
            var parameters = new DialogParameters<ConfirmDialog>
            {
                { x => x.ContentText, "Attribute eşleştirmesine geçmek ister misiniz?" },
                { x => x.ButtonText, "Evet" },
                { x => x.Color, Color.Primary }
            };
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>("Attribute Eşleştirme", parameters, options);
            var result = await dialog.Result;

            if (result is not null && !result.Canceled)
            {
                NavigationManager.NavigateTo(
                    $"/attributes?categoryId={_savedCategoryId}&marketplaceId={_selectedMarketplaceId.Value}",
                    replace: true);
                return;
            }
        }

        NavigationManager.NavigateTo("/categories", replace: true);
    }

    private static AddCategoryAttributeDto ToAddCategoryAttributeDto(AttributeModel a) => new(
        id: a.Id,
        categoryAttributeKey: a.CategoryAttributeKey,
        categoryAttributeHumanized: a.CategoryAttributeHumanized,
        allowCustom: a.AllowCustom,
        isRequired: a.IsRequired,
        isVarianter: a.IsVarianter,
        isSlicer: a.IsSlicer,
        values: a.Values.Select(v => new AddCategoryAttributeValueDto(v.Id, v.Value)).ToList()
    );
}
```

- [ ] **Step 2: Create CategoryWizard.razor**

```razor
@page "/categories/add"
@page "/categories/edit/{Id:int}"
@attribute [Authorize]

<UnsavedChangesGuard IsDirty="_isDirty" />

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">
        @(IsEditMode ? "Kategori Düzenle" : "Yeni Kategori Ekle")
    </MudText>

    <MudStepper @bind-ActiveIndex="_stepIndex" NonLinear="@IsEditMode">
        <ChildContent>
            <MudStep Title="Genel Bilgiler" Icon="@Icons.Material.Filled.Info">
                <CategoryWizardGeneralStep @ref="_generalStep"
                    @bind-Name="_name"
                    @bind-ParentCategoryId="_parentCategoryId"
                    @bind-IsFavorite="_isFavorite"
                    @bind-DefaultVatRate="_defaultVatRate"
                    EditCategoryId="Id"
                    OnFieldChanged="MarkDirty" />
            </MudStep>

            <MudStep Title="Özellikler" Icon="@Icons.Material.Filled.List">
                <CategoryWizardAttributesStep @ref="_attributesStep"
                    @bind-Attributes="_attributes"
                    OnFieldChanged="MarkDirty" />
            </MudStep>

            <MudStep Title="Pazar Yeri Eşleştirme" Icon="@Icons.Material.Filled.Sync" Optional="true">
                <CategoryWizardMarketplaceStep @ref="_marketplaceStep"
                    CategoryId="@(_savedCategoryId > 0 ? _savedCategoryId : (Id ?? 0))"
                    @bind-SelectedMarketplaceId="_selectedMarketplaceId"
                    OnFieldChanged="MarkDirty" />
            </MudStep>
        </ChildContent>

        <ActionContent>
            @* Custom action buttons — bos birakilir, asagida kendi butonlarimiz var *@
        </ActionContent>
    </MudStepper>

    <MudStack Row="true" Justify="Justify.SpaceBetween" Class="mt-4 mb-8">
        <MudButton OnClick="() => NavigationManager.NavigateTo(&quot;/categories&quot;)"
                   Variant="Variant.Text" Color="Color.Default">
            İptal
        </MudButton>

        <MudStack Row="true" Spacing="2">
            @if (_stepIndex > 0)
            {
                <MudButton OnClick="PreviousStep" Variant="Variant.Outlined" Color="Color.Default">
                    Geri
                </MudButton>
            }

            @if (_stepIndex < 2)
            {
                <MudButton OnClick="NextStep" Variant="Variant.Filled" Color="Color.Primary">
                    İleri
                </MudButton>
            }

            <MudButton OnClick="Save" Variant="Variant.Filled" Color="Color.Success"
                       Disabled="_isSaving">
                @if (_isSaving) { <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" /> }
                Kaydet
            </MudButton>
        </MudStack>
    </MudStack>
</MudContainer>
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Categories/CategoryWizard.razor \
       Application/Entegrasyon.Blazor/Features/Categories/CategoryWizard.razor.cs
git commit -m "feat(categories): add CategoryWizard main page with 3-step stepper + guard"
```

---

### Task 6: Wire Up Navigation — Remove Dialog, Update Categories Page

**Files:**
- Delete: `Application/Entegrasyon.Blazor/Features/Categories/CategoryDialog.razor`
- Delete: `Application/Entegrasyon.Blazor/Features/Categories/CategoryDialog.razor.cs`
- Delete: `Application/Entegrasyon.Blazor/Features/Categories/CategoryEdit.razor`
- Delete: `Application/Entegrasyon.Blazor/Features/Categories/CategoryEdit.razor.cs`
- Modify: `Application/Entegrasyon.Blazor/Features/Categories/Categories.razor`
- Modify: `Application/Entegrasyon.Blazor/Features/Categories/Categories.razor.cs`

- [ ] **Step 1: Update Categories.razor — change Add button to navigate**

In `Application/Entegrasyon.Blazor/Features/Categories/Categories.razor`, find line 17 where `OnAddClicked="@OpenAddCategoryDialog"` is used.

Replace the `OnAddClicked` event with navigation. The CategoryTreePanel likely has an "Add" button callback. Change it to:

```razor
OnAddClicked="@(() => NavigationManager.NavigateTo("/categories/add"))"
```

Also find any "Edit" handler that opens dialog or navigates to CategoryEdit. Change to:

```razor
OnEditClicked="@((id) => NavigationManager.NavigateTo($"/categories/edit/{id}"))"
```

- [ ] **Step 2: Update Categories.razor.cs — remove dialog methods**

In `Application/Entegrasyon.Blazor/Features/Categories/Categories.razor.cs`:

1. Remove the entire `OpenAddCategoryDialog` method (lines 48-69)
2. Remove any `OpenEditCategoryDialog` method if exists
3. Remove `IDialogService` injection if it's only used for CategoryDialog
4. Add `[Inject] private NavigationManager NavigationManager { get; set; } = null!;` if not already present

- [ ] **Step 3: Delete old dialog and edit files**

```bash
rm Application/Entegrasyon.Blazor/Features/Categories/CategoryDialog.razor
rm Application/Entegrasyon.Blazor/Features/Categories/CategoryDialog.razor.cs
rm Application/Entegrasyon.Blazor/Features/Categories/CategoryEdit.razor
rm Application/Entegrasyon.Blazor/Features/Categories/CategoryEdit.razor.cs
```

- [ ] **Step 4: Search for any remaining CategoryDialog references**

Run: `grep -r "CategoryDialog" Application/Entegrasyon.Blazor/ --include="*.razor" --include="*.cs"`

Fix any remaining references. Common places:
- `_Imports.razor` — might reference CategoryDialog namespace
- Other pages that open CategoryDialog

- [ ] **Step 5: Search for CategoryEdit route references**

Run: `grep -r "categories/edit" Application/Entegrasyon.Blazor/ --include="*.razor" --include="*.cs"`

The route `/categories/edit/{Id:int}` is now on CategoryWizard.razor. Verify no broken references.

- [ ] **Step 6: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded — no broken references

- [ ] **Step 7: Commit**

```bash
git add -A Application/Entegrasyon.Blazor/Features/Categories/
git commit -m "feat(categories): wire wizard navigation, remove CategoryDialog and old CategoryEdit"
```

---

### Task 7: AllowCustom Fix — Comment Out in Remaining Code

**Files:**
- Search and modify any remaining AllowCustom MudSwitch occurrences

- [ ] **Step 1: Search for AllowCustom MudSwitch**

Run: `grep -rn "MudSwitch.*AllowCustom\|AllowCustom.*MudSwitch" Application/Entegrasyon.Blazor/ --include="*.razor"`

Since CategoryDialog and CategoryEdit are deleted in Task 6, and the wizard already has it commented out (Task 3), verify there are no remaining editable AllowCustom switches.

If any are found (e.g., in other components), comment them out with the same explanation:

```razor
@* AllowCustom: Kategori bazinda degil, attribute bazinda global ayarlanir.
   Gerekirse /attributes sayfasindan yonetilir.
   Ileride kategori bazinda ihtiyac olursa bu blok acilabilir. *@
```

- [ ] **Step 2: Verify AllowCustom in save DTOs uses attribute value**

Check that `ToAddCategoryAttributeDto` in `CategoryWizard.razor.cs` reads `AllowCustom` from the attribute model (which was set from the original attribute, not user-modified). This was already done in Task 5 Step 1.

- [ ] **Step 3: Build and run tests**

Run: `dotnet build Entegrasyon.sln && dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: Build succeeded, all tests pass

- [ ] **Step 4: Commit (if changes were needed)**

```bash
git add -A
git commit -m "fix(categories): ensure AllowCustom is read-only in all category forms"
```

---

### Task 8: Attributes Page — Deep Link with Query Parameters

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Features/Attributes/AttributesPage.razor.cs`
- Modify: `Application/Entegrasyon.Blazor/Features/Attributes/AttributeDetailPanel.razor.cs`

- [ ] **Step 1: Add query parameters to AttributesPage**

In `Application/Entegrasyon.Blazor/Features/Attributes/AttributesPage.razor.cs`, add:

```csharp
[SupplyParameterFromQuery] public int? CategoryId { get; set; }
[SupplyParameterFromQuery] public int? MarketplaceId { get; set; }
```

In `OnInitializedAsync` (or `OnParametersSetAsync`), if `CategoryId` has a value:

1. Load the category's attributes
2. Auto-select the first attribute in the list (to show in detail panel)
3. If `MarketplaceId` also has a value, pass it to the detail panel to auto-select the marketplace tab

Read the existing code first to understand how category selection and attribute loading work, then integrate the query param logic.

- [ ] **Step 2: Update AttributeDetailPanel to accept MarketplaceId**

In `Application/Entegrasyon.Blazor/Features/Attributes/AttributeDetailPanel.razor.cs`, add a parameter:

```csharp
[Parameter] public int? InitialMarketplaceId { get; set; }
```

In `OnParametersSetAsync`, if `InitialMarketplaceId` has a value and the tabs include marketplace-specific tabs (like "Trendyol", "N11"), auto-select that tab.

Read the existing tab structure to determine how marketplace tabs are indexed/selected.

- [ ] **Step 3: Build to verify**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Attributes/AttributesPage.razor.cs \
       Application/Entegrasyon.Blazor/Features/Attributes/AttributeDetailPanel.razor.cs
git commit -m "feat(attributes): add deep link support with categoryId and marketplaceId query params"
```

---

### Task 9: Final Verification + Cleanup

**Files:** No changes — verification only

- [ ] **Step 1: Run unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All PASS

- [ ] **Step 2: Build full solution**

Run: `dotnet build Entegrasyon.sln`
Expected: 0 errors

- [ ] **Step 3: Search for dead code references**

Run:
```bash
grep -r "CategoryDialog" Application/ --include="*.cs" --include="*.razor"
grep -r "OpenAddCategoryDialog" Application/ --include="*.cs" --include="*.razor"
```
Expected: No results (all references cleaned up)

- [ ] **Step 4: Verify no orphan files**

Run:
```bash
ls Application/Entegrasyon.Blazor/Features/Categories/
```
Expected: CategoryWizard*, CategoryWizardGeneralStep*, CategoryWizardAttributesStep*, CategoryWizardMarketplaceStep*, Categories.razor*, CategoryTreePanel*, CategoryTreeItem*, CategoryDetailsPanel*

No CategoryDialog* or old CategoryEdit* files.

- [ ] **Step 5: Manual smoke test checklist**

1. Navigate to `/categories/add` — wizard opens, 3 steps visible
2. Fill Step 1 (name), click "Ileri" — goes to Step 2
3. Add an attribute — AllowCustom is read-only chip, not switch
4. Click "Ileri" — goes to Step 3 (marketplace)
5. Skip marketplace, click "Kaydet" — redirects to `/categories`
6. Navigate to `/categories/edit/{existingId}` — pre-filled data
7. Change name, click browser back — guard dialog appears
8. Close tab — native browser dialog appears
9. Add marketplace mapping, save — dialog asks about attribute sync
10. Click "Evet" — redirects to `/attributes?categoryId=X&marketplaceId=Y`
