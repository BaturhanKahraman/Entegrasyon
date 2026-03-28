# Leaf Node Enforcement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tum kategori eslestirmeleri ve attribute atamalari yalnizca leaf (yaprak) kategorilerde yapilabilsin; leaf olmayan kategoriler asla parent olarak secilebilsin ancak attribute/sync alamasin.

**Architecture:** Business layer'daki her ilgili manager'a leaf guard eklenir. `GetValidParentCandidatesAsync` mevcut attribute filtresine ek olarak marketplace sync filtresi alir. UI tarafinda filtreleme ve yardim metinleri guncellenir. Mevcut 4 ihlal eden kayit SQL migration ile duzeltilir.

**Tech Stack:** C# 12, EF Core 8, Blazor Server, MudBlazor, xUnit, Moq, FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-29-leaf-node-enforcement-design.md`

---

## File Structure

| Dosya | Degisiklik |
|-------|-----------|
| `Application/Entegrasyon.Business/Abstract/ICategoryManager.cs` | `IsLeafCategoryAsync` eklenir |
| `Application/Entegrasyon.Business/Concrete/CategoryManager.cs` | `IsLeafCategoryAsync`, `GetValidParentCandidatesAsync` sync filtresi, `AddCategory`/`UpdateCategory` sync guard |
| `Application/Entegrasyon.Business/Concrete/CategoryMatchService.cs` | `CreateCategoryMappingAsync`, `BulkCreateCategoryMappingsAsync` leaf guard |
| `Application/Entegrasyon.Business/Concrete/CategoryAutoMatchService.cs` | Input filtreleme |
| `Application/Entegrasyon.Blazor/Features/Categories/CategoryDialog.razor` | Yardim metni guncelleme |
| `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor.cs` | Leaf filtresi |
| `Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor.cs` | Default leaf filtresi |
| `Application/Entegrasyon.Blazor/Features/Categories/CategoryDetailsPanel.razor` | Non-leaf uyari mesaji |
| `Test/Entegrasyon.Test/Business/CategoryManagerTests.cs` | Leaf guard unit testleri |
| `Test/Entegrasyon.Test/CategoryMatch/CategoryMatchLeafGuardTests.cs` | Match service leaf guard testleri |
| `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/XXXXXXXX_FixLeafNodeViolations.cs` | Data fix migration |

---

### Task 1: CategoryManager — IsLeafCategoryAsync + GetValidParentCandidatesAsync Sync Filtresi (Unit Test)

**Files:**
- Modify: `Test/Entegrasyon.Test/Business/CategoryManagerTests.cs`
- Modify: `Application/Entegrasyon.Business/Abstract/ICategoryManager.cs:39`
- Modify: `Application/Entegrasyon.Business/Concrete/CategoryManager.cs:288-314`

- [ ] **Step 1: Write failing tests for IsLeafCategoryAsync**

Add to `Test/Entegrasyon.Test/Business/CategoryManagerTests.cs`:

```csharp
[Fact]
public async Task IsLeafCategoryAsync_Should_Return_True_When_No_Children()
{
    // Arrange
    var categories = new List<Category>
    {
        new() { Id = 1, Name = "Leaf", SubCategories = new List<Category>() }
    };
    mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

    // Act
    var result = await _categoryManager.IsLeafCategoryAsync(1);

    // Assert
    result.Should().BeTrue();
}

[Fact]
public async Task IsLeafCategoryAsync_Should_Return_False_When_Has_Children()
{
    // Arrange
    var parent = new Category { Id = 1, Name = "Parent" };
    var child = new Category { Id = 2, Name = "Child", SuperCategoryId = 1 };
    var categories = new List<Category> { parent, child };
    mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

    // Act
    var result = await _categoryManager.IsLeafCategoryAsync(1);

    // Assert
    result.Should().BeFalse();
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~IsLeafCategoryAsync"`
Expected: FAIL — method does not exist yet

- [ ] **Step 3: Add IsLeafCategoryAsync to interface**

In `Application/Entegrasyon.Business/Abstract/ICategoryManager.cs`, after line 39 (`Task<List<Category>> GetLeafCategoriesAsync();`), add:

```csharp
    Task<bool> IsLeafCategoryAsync(int categoryId);
```

- [ ] **Step 4: Implement IsLeafCategoryAsync in CategoryManager**

In `Application/Entegrasyon.Business/Concrete/CategoryManager.cs`, after `GetLeafCategoriesAsync()` method (after line 325), add:

```csharp
    public async Task<bool> IsLeafCategoryAsync(int categoryId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return !await dbContext.Categories.AnyAsync(c => c.SuperCategoryId == categoryId && !c.IsDeleted);
    }
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~IsLeafCategoryAsync"`
Expected: PASS

- [ ] **Step 6: Write failing tests for GetValidParentCandidatesAsync sync filter**

Add to `Test/Entegrasyon.Test/Business/CategoryManagerTests.cs`:

```csharp
[Fact]
public async Task GetValidParentCandidatesAsync_Should_Exclude_Categories_With_Marketplace_Sync()
{
    // Arrange
    var catWithSync = new Category
    {
        Id = 1, Name = "Synced",
        CategoryAttributes = new List<CategoryAttributeCategory>(),
        MarketplaceLinks = new List<CategoryMarketplace>
        {
            new() { MarketPlaceId = 1, IsActive = true }
        }
    };
    var catWithoutSync = new Category
    {
        Id = 2, Name = "Clean",
        CategoryAttributes = new List<CategoryAttributeCategory>(),
        MarketplaceLinks = new List<CategoryMarketplace>()
    };
    var categories = new List<Category> { catWithSync, catWithoutSync };
    mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

    // Act
    var result = await _categoryManager.GetValidParentCandidatesAsync();

    // Assert
    result.Should().HaveCount(1);
    result[0].Id.Should().Be(2);
}
```

- [ ] **Step 7: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Should_Exclude_Categories_With_Marketplace_Sync"`
Expected: FAIL — catWithSync is still included

- [ ] **Step 8: Update GetValidParentCandidatesAsync — add sync filter**

In `Application/Entegrasyon.Business/Concrete/CategoryManager.cs`:

**Overload 1 (lines 288-296):** Replace:
```csharp
        .Where(c => !c.CategoryAttributes.Any())
```
with:
```csharp
        .Where(c => !c.CategoryAttributes.Any())
        .Where(c => !c.MarketplaceLinks.Any(m => m.IsActive))
```

**Overload 2 (lines 298-314):** Replace:
```csharp
        .Where(c => !c.CategoryAttributes.Any())
        .OrderBy(c => c.Name)
```
with:
```csharp
        .Where(c => !c.CategoryAttributes.Any())
        .Where(c => !c.MarketplaceLinks.Any(m => m.IsActive))
        .OrderBy(c => c.Name)
```

- [ ] **Step 9: Run test to verify it passes**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Should_Exclude_Categories_With_Marketplace_Sync"`
Expected: PASS

- [ ] **Step 10: Run all existing tests to verify no regression**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All tests PASS

- [ ] **Step 11: Commit**

```bash
git add Application/Entegrasyon.Business/Abstract/ICategoryManager.cs \
       Application/Entegrasyon.Business/Concrete/CategoryManager.cs \
       Test/Entegrasyon.Test/Business/CategoryManagerTests.cs
git commit -m "feat(categories): add IsLeafCategoryAsync + sync filter to GetValidParentCandidatesAsync"
```

---

### Task 2: CategoryManager — AddCategory/UpdateCategory Sync Guard

**Files:**
- Modify: `Test/Entegrasyon.Test/Business/CategoryManagerTests.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/CategoryManager.cs:36-85`

- [ ] **Step 1: Write failing test for AddCategory sync guard**

Add to `Test/Entegrasyon.Test/Business/CategoryManagerTests.cs`:

```csharp
[Fact]
public async Task AddCategory_Should_Return_Error_When_Parent_Has_Marketplace_Sync()
{
    // Arrange
    var parentId = 10;
    var categoryAttributeCategories = new List<CategoryAttributeCategory>();
    var categoryMarketplaces = new List<CategoryMarketplace>
    {
        new() { CategoryId = parentId, MarketPlaceId = 1, IsActive = true }
    };

    mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(categoryAttributeCategories);
    mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(categoryMarketplaces);

    var dto = new AddCategoryDto { Name = "Child", SuperCategoryId = parentId };

    // Act
    var result = await _categoryManager.AddCategory(dto);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Contain("pazar yeri eşleştirmesi");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Should_Return_Error_When_Parent_Has_Marketplace_Sync"`
Expected: FAIL — no sync check in AddCategory

- [ ] **Step 3: Update AddCategory — add sync guard**

In `Application/Entegrasyon.Business/Concrete/CategoryManager.cs`, replace lines 41-46:

```csharp
    if (dto.SuperCategoryId is > 0)
    {
        bool parentHasAttrs = await dbContext.CategoryAttributeCategories
            .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value);
        if (parentHasAttrs)
            return new ErrorDataResult<CategoryDetailDto>(null!, "Seçilen üst kategori özellik içerdiğinden alt kategori eklenemez.");
    }
```

with:

```csharp
    if (dto.SuperCategoryId is > 0)
    {
        bool parentHasAttrs = await dbContext.CategoryAttributeCategories
            .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value);
        if (parentHasAttrs)
            return new ErrorDataResult<CategoryDetailDto>(null!, "Seçilen üst kategori özellik içerdiğinden alt kategori eklenemez.");

        bool parentHasSync = await dbContext.CategoryMarketplaces
            .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value && x.IsActive);
        if (parentHasSync)
            return new ErrorDataResult<CategoryDetailDto>(null!, "Seçilen üst kategori pazar yeri eşleştirmesi içerdiğinden alt kategori eklenemez.");
    }
```

- [ ] **Step 4: Update UpdateCategory — add sync guard**

In `Application/Entegrasyon.Business/Concrete/CategoryManager.cs`, replace lines 69-75:

```csharp
    if (dto.SuperCategoryId is > 0)
    {
        bool parentHasAttrs = await dbContext.CategoryAttributeCategories
            .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value);
        if (parentHasAttrs)
            return new ErrorResult("Seçilen üst kategori özellik içerdiğinden bu işlem yapılamaz.");
    }
```

with:

```csharp
    if (dto.SuperCategoryId is > 0)
    {
        bool parentHasAttrs = await dbContext.CategoryAttributeCategories
            .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value);
        if (parentHasAttrs)
            return new ErrorResult("Seçilen üst kategori özellik içerdiğinden bu işlem yapılamaz.");

        bool parentHasSync = await dbContext.CategoryMarketplaces
            .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value && x.IsActive);
        if (parentHasSync)
            return new ErrorResult("Seçilen üst kategori pazar yeri eşleştirmesi içerdiğinden bu işlem yapılamaz.");
    }
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Should_Return_Error_When_Parent_Has_Marketplace_Sync"`
Expected: PASS

- [ ] **Step 6: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All PASS

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/CategoryManager.cs \
       Test/Entegrasyon.Test/Business/CategoryManagerTests.cs
git commit -m "feat(categories): add marketplace sync guard to AddCategory/UpdateCategory"
```

---

### Task 3: CategoryMatchService — Leaf Guard for CreateCategoryMappingAsync + BulkCreateCategoryMappingsAsync

**Files:**
- Create: `Test/Entegrasyon.Test/CategoryMatch/CategoryMatchLeafGuardTests.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/CategoryMatchService.cs:82-214`

- [ ] **Step 1: Write failing test for CreateCategoryMappingAsync leaf guard**

Create `Test/Entegrasyon.Test/CategoryMatch/CategoryMatchLeafGuardTests.cs`:

```csharp
using Entegrasyon.Business.Concrete;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.Test.CategoryMatch;

public class CategoryMatchLeafGuardTests : BaseTest
{
    private readonly CategoryMatchService _service;

    public CategoryMatchLeafGuardTests()
    {
        _service = new CategoryMatchService(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            MockValidator.Object);
    }

    [Fact]
    public async Task CreateCategoryMappingAsync_Should_Return_Error_When_Category_Is_Not_Leaf()
    {
        // Arrange
        var parent = new Category { Id = 1, Name = "Parent", IsDeleted = false };
        var child = new Category { Id = 2, Name = "Child", SuperCategoryId = 1, IsDeleted = false };
        var categories = new List<Category> { parent, child };
        var marketplaces = new List<CategoryMarketplace>();

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaces);

        var dto = new CreateCategoryMarketplaceMatchDto
        {
            ApplicationCategoryId = 1, // parent — not leaf
            MarketPlaceId = 1,
            MarketPlaceCategoryId = 100,
            MarketPlaceCategoryName = "Test"
        };

        // Act
        var result = await _service.CreateCategoryMappingAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("alt kategorileri olduğu için");
    }

    [Fact]
    public async Task CreateCategoryMappingAsync_Should_Succeed_When_Category_Is_Leaf()
    {
        // Arrange
        var leaf = new Category { Id = 1, Name = "Leaf", IsDeleted = false };
        var categories = new List<Category> { leaf };
        var marketplaces = new List<CategoryMarketplace>();

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaces);

        var dto = new CreateCategoryMarketplaceMatchDto
        {
            ApplicationCategoryId = 1, // leaf
            MarketPlaceId = 1,
            MarketPlaceCategoryId = 100,
            MarketPlaceCategoryName = "Test"
        };

        // Act
        var result = await _service.CreateCategoryMappingAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~CategoryMatchLeafGuardTests"`
Expected: FAIL — no leaf check in CreateCategoryMappingAsync

- [ ] **Step 3: Add leaf guard to CreateCategoryMappingAsync**

In `Application/Entegrasyon.Business/Concrete/CategoryMatchService.cs`, after the category null check (after line ~103), add:

```csharp
    // Leaf guard — only leaf categories can have marketplace mappings
    var hasChildren = await dbContext.Categories
        .AnyAsync(c => c.SuperCategoryId == dto.ApplicationCategoryId && !c.IsDeleted);
    if (hasChildren)
    {
        var error = "Bu kategorinin alt kategorileri olduğu için pazar yeri eşleştirmesi yapılamaz.";
        await applicationLogManager.AddLog(error, LogType.Category, LogAction.Add, dto);
        return new ErrorResult(error);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~CategoryMatchLeafGuardTests"`
Expected: PASS

- [ ] **Step 5: Write failing test for BulkCreateCategoryMappingsAsync leaf guard**

Add to `CategoryMatchLeafGuardTests.cs`:

```csharp
[Fact]
public async Task BulkCreateCategoryMappingsAsync_Should_Fail_NonLeaf_Categories()
{
    // Arrange
    var parent = new Category { Id = 1, Name = "Parent", IsDeleted = false };
    var child = new Category { Id = 2, Name = "Child", SuperCategoryId = 1, IsDeleted = false };
    var leaf = new Category { Id = 3, Name = "Leaf", IsDeleted = false };
    var categories = new List<Category> { parent, child, leaf };
    var marketplaces = new List<CategoryMarketplace>();

    mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
    mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaces);

    MockValidator
        .Setup(v => v.Validate(It.IsAny<BulkCategoryMatchDto>()))
        .ReturnsAsync(new FluentValidation.Results.ValidationResult());

    var dto = new BulkCategoryMatchDto
    {
        MarketPlaceId = 1,
        Items = new List<BulkCategoryMatchItemDto>
        {
            new() { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100, MarketPlaceCategoryName = "P" }, // non-leaf
            new() { ApplicationCategoryId = 3, MarketPlaceCategoryId = 200, MarketPlaceCategoryName = "L" }  // leaf
        }
    };

    // Act
    var result = await _service.BulkCreateCategoryMappingsAsync(dto);

    // Assert
    result.Success.Should().BeTrue();
    result.Data.SuccessCount.Should().Be(1);  // only leaf
    result.Data.FailedCount.Should().Be(1);   // non-leaf failed
    result.Data.Errors.Should().ContainSingle(e => e.ApplicationCategoryId == 1);
}
```

- [ ] **Step 6: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~BulkCreateCategoryMappingsAsync_Should_Fail_NonLeaf"`
Expected: FAIL — non-leaf category is created as mapping

- [ ] **Step 7: Add leaf guard to BulkCreateCategoryMappingsAsync**

In `Application/Entegrasyon.Business/Concrete/CategoryMatchService.cs`, inside the `BulkCreateCategoryMappingsAsync` method, after loading `existingCategories` dictionary (around line 155), add:

```csharp
    // Leaf guard — pre-load non-leaf category IDs
    var nonLeafCategoryIds = await dbContext.Categories
        .Where(c => !c.IsDeleted && requestedCategoryIds.Contains(c.SuperCategoryId ?? 0))
        .Select(c => c.SuperCategoryId!.Value)
        .Distinct()
        .ToListAsync();
    var nonLeafSet = nonLeafCategoryIds.ToHashSet();
```

Then inside the `foreach` loop, after the "Category not found" check and before the "Already mapped" check, add:

```csharp
        // Non-leaf guard
        if (nonLeafSet.Contains(item.ApplicationCategoryId))
        {
            result.FailedCount++;
            result.Errors.Add(new BulkCategoryMatchErrorDto
            {
                ApplicationCategoryId = item.ApplicationCategoryId,
                CategoryName = categoryName,
                ErrorMessage = "Bu kategorinin alt kategorileri olduğu için pazar yeri eşleştirmesi yapılamaz."
            });
            continue;
        }
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~CategoryMatchLeafGuardTests"`
Expected: All PASS

- [ ] **Step 9: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All PASS

- [ ] **Step 10: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/CategoryMatchService.cs \
       Test/Entegrasyon.Test/CategoryMatch/CategoryMatchLeafGuardTests.cs
git commit -m "feat(categories): add leaf guard to CreateCategoryMappingAsync + BulkCreateCategoryMappingsAsync"
```

---

### Task 4: CategoryAutoMatchService — Leaf Filter on Input

**Files:**
- Modify: `Test/Entegrasyon.Test/CategoryMatch/CategoryAutoMatchServiceTests.cs`
- Modify: `Application/Entegrasyon.Business/Concrete/CategoryAutoMatchService.cs:21-43`

- [ ] **Step 1: Write failing test for auto match leaf filter**

Add to `Test/Entegrasyon.Test/CategoryMatch/CategoryAutoMatchServiceTests.cs`:

```csharp
[Fact]
public async Task GetAutoMatchSuggestionsAsync_Should_Filter_NonLeaf_Categories()
{
    // Arrange
    var parent = new Category { Id = 1, Name = "Parent", IsDeleted = false };
    var child = new Category { Id = 2, Name = "Child", SuperCategoryId = 1, IsDeleted = false };
    var categories = new List<Category> { parent, child };
    mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

    var request = new CategoryAutoMatchRequestDto
    {
        MarketPlaceId = 1,
        Categories = new List<CategoryAutoMatchItemDto>
        {
            new() { CategoryId = 1, CategoryName = "Parent", ParentCategoryName = null } // non-leaf
        }
    };

    // Act
    var result = await _service.GetAutoMatchSuggestionsAsync(request);

    // Assert
    result.Success.Should().BeTrue();
    result.Data.Should().BeEmpty(); // filtered out because non-leaf
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Should_Filter_NonLeaf_Categories"`
Expected: FAIL — non-leaf categories are not filtered

- [ ] **Step 3: Add leaf filter to GetAutoMatchSuggestionsAsync**

In `Application/Entegrasyon.Business/Concrete/CategoryAutoMatchService.cs`, at the beginning of `GetAutoMatchSuggestionsAsync` (after the empty check around line 24), add:

```csharp
    // Leaf guard — only suggest matches for leaf categories
    using var dbContext = contextFactory.CreateDbContext();
    var requestedIds = request.Categories.Select(c => c.CategoryId).ToList();
    var nonLeafIds = await dbContext.Categories
        .Where(c => !c.IsDeleted && requestedIds.Contains(c.SuperCategoryId ?? 0))
        .Select(c => c.SuperCategoryId!.Value)
        .Distinct()
        .ToHashSetAsync(ct);

    request = request with
    {
        Categories = request.Categories
            .Where(c => !nonLeafIds.Contains(c.CategoryId))
            .ToList()
    };

    if (request.Categories.Count == 0)
        return new SuccessDataResult<List<CategoryAutoMatchSuggestionDto>>([], "Eslestirilebilecek yaprak kategori bulunamadi.");
```

Not: `CategoryAutoMatchRequestDto` record ise `with` kullanilir; class ise direkt property set edilir. Mevcut DTO tipine gore ayarla.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj --filter "FullyQualifiedName~Should_Filter_NonLeaf_Categories"`
Expected: PASS

- [ ] **Step 5: Run all tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All PASS

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Business/Concrete/CategoryAutoMatchService.cs \
       Test/Entegrasyon.Test/CategoryMatch/CategoryAutoMatchServiceTests.cs
git commit -m "feat(categories): filter non-leaf categories from auto match suggestions"
```

---

### Task 5: UI Guncellemeleri — CategoryDialog, BulkCategoryMatchPage, CategorySync, CategoryDetailsPanel

**Files:**
- Modify: `Application/Entegrasyon.Blazor/Features/Categories/CategoryDialog.razor:44-46`
- Modify: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor.cs:60-65`
- Modify: `Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor.cs`
- Modify: `Application/Entegrasyon.Blazor/Features/Categories/CategoryDetailsPanel.razor:62-98`

- [ ] **Step 1: Update CategoryDialog help text**

In `Application/Entegrasyon.Blazor/Features/Categories/CategoryDialog.razor`, replace line 45:

```razor
Yalnızca özellik içermeyen kategoriler üst kategori olabilir.
```

with:

```razor
Yalnızca özellik veya pazar yeri eşleştirmesi içermeyen kategoriler üst kategori olabilir.
```

- [ ] **Step 2: Add leaf filter to BulkCategoryMatchPage**

In `Application/Entegrasyon.Blazor/Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor.cs`, in the `LoadUnmappedCategories()` method, update the filtering logic. After the existing unmapped filter (around line 63), add a leaf filter:

```csharp
// Existing: filter unmapped
var unmapped = allCategories
    .Where(c => !mappedCategoryIds.Contains(c.Id))
    .ToList();

// New: filter to leaf only (no subcategories)
var allCategoryIds = allCategories.Select(c => c.Id).ToHashSet();
var parentIds = allCategories
    .Where(c => c.SuperCategoryId.HasValue)
    .Select(c => c.SuperCategoryId!.Value)
    .ToHashSet();

_unmappedCategories = unmapped
    .Where(c => !parentIds.Contains(c.Id))
    .OrderBy(c => c.Name)
    .ToList();
```

- [ ] **Step 3: Set default leaf filter in CategorySync**

In `Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor.cs`, change the `_showLeafOnly` default value from `false` to `true`:

```csharp
private bool _showLeafOnly = true;
```

- [ ] **Step 4: Add non-leaf warning to CategoryDetailsPanel**

In `Application/Entegrasyon.Blazor/Features/Categories/CategoryDetailsPanel.razor`, before the attributes card section (around line 62), add a non-leaf info alert:

```razor
@if (Category?.SubCategories?.Any() == true)
{
    <MudAlert Severity="Severity.Info" Class="mb-4" Dense="true">
        Bu kategori alt kategorilere sahiptir. Ozellikler ve pazar yeri eslestirmeleri yalnizca yaprak kategorilere eklenebilir.
    </MudAlert>
}
```

- [ ] **Step 5: Build to verify no compilation errors**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add Application/Entegrasyon.Blazor/Features/Categories/CategoryDialog.razor \
       Application/Entegrasyon.Blazor/Features/MarketplaceSync/BulkCategoryMatch/BulkCategoryMatchPage.razor.cs \
       Application/Entegrasyon.Blazor/Features/MarketplaceSync/CategorySync.razor.cs \
       Application/Entegrasyon.Blazor/Features/Categories/CategoryDetailsPanel.razor
git commit -m "feat(ui): update UI for leaf node enforcement — help text, filters, warnings"
```

---

### Task 6: Data Migration — Fix Existing Leaf Node Violations

**Files:**
- Create: `Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/XXXXXXXX_FixLeafNodeViolations.cs` (generated via `dotnet ef migrations add`)

- [ ] **Step 1: Create empty migration**

Run:
```bash
dotnet ef migrations add FixLeafNodeViolations \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor \
  --context IntegrationDbContext
```

- [ ] **Step 2: Replace migration Up method with data fix SQL**

Open the generated migration file and replace the `Up` method body:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ── Attribute ihlalleri ──
    // "Ilk Kategori" (id=1) ve "Gomlek" (id=10): attribute'lari leaf cocuklara kopyala

    // Step 1: Copy attributes from parent to leaf children (children that have no children themselves)
    migrationBuilder.Sql(@"
        INSERT INTO ""CategoryAttributeCategories"" (""CategoryId"", ""CategoryAttributeId"", ""IsRequired"", ""IsVarianter"", ""IsSlicer"", ""IsDeleted"", ""DeletedAt"", ""CreatedAt"", ""UpdatedAt"")
        SELECT child.""Id"", cac.""CategoryAttributeId"", cac.""IsRequired"", false, false, false, '0001-01-01T00:00:00Z', NOW(), NOW()
        FROM ""Categories"" child
        INNER JOIN ""CategoryAttributeCategories"" cac ON cac.""CategoryId"" = child.""SuperCategoryId""
        WHERE child.""SuperCategoryId"" IN (1, 10)
          AND child.""IsDeleted"" = false
          AND NOT EXISTS (
              SELECT 1 FROM ""Categories"" grandchild
              WHERE grandchild.""SuperCategoryId"" = child.""Id"" AND grandchild.""IsDeleted"" = false
          )
          AND NOT EXISTS (
              SELECT 1 FROM ""CategoryAttributeCategories"" existing
              WHERE existing.""CategoryId"" = child.""Id""
                AND existing.""CategoryAttributeId"" = cac.""CategoryAttributeId""
          );
    ");

    // Step 2: Remove attributes from parent categories
    migrationBuilder.Sql(@"
        DELETE FROM ""CategoryAttributeCategories""
        WHERE ""CategoryId"" IN (1, 10);
    ");

    // ── Sync ihlalleri ──
    // ""Akilli Telefon"" (id=35) ve ""Akilli Saat"" (id=37): sync kayitlarini sil
    migrationBuilder.Sql(@"
        UPDATE ""CategoryMarketplaces""
        SET ""IsActive"" = false
        WHERE ""CategoryId"" IN (35, 37) AND ""IsActive"" = true;
    ");
}
```

- [ ] **Step 3: Add Down method (reversibility)**

```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    // Data migration — manual rollback if needed
    // Attribute ve sync verisi geri yüklenemez, sadece log amaçlı
}
```

- [ ] **Step 4: Apply migration to database**

Run:
```bash
dotnet ef database update \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor \
  --context IntegrationDbContext
```
Expected: `Applying migration 'XXXXXXXX_FixLeafNodeViolations'. Done.`

- [ ] **Step 5: Verify no violations remain**

Run:
```bash
ssh baturhan@192.168.1.78 'docker exec -i postgres_db psql -U baturhan -d IntegrationDb' <<'SQL'
-- Cocugu olan + attribute'u olan kategoriler (should be 0)
SELECT c."Id", c."Name"
FROM "Categories" c
INNER JOIN "Categories" sub ON sub."SuperCategoryId" = c."Id" AND sub."IsDeleted" = false
INNER JOIN "CategoryAttributeCategories" cac ON cac."CategoryId" = c."Id"
WHERE c."IsDeleted" = false;

-- Cocugu olan + aktif sync'i olan kategoriler (should be 0)
SELECT c."Id", c."Name"
FROM "Categories" c
INNER JOIN "Categories" sub ON sub."SuperCategoryId" = c."Id" AND sub."IsDeleted" = false
INNER JOIN "CategoryMarketplaces" cm ON cm."CategoryId" = c."Id" AND cm."IsActive" = true
WHERE c."IsDeleted" = false;
SQL
```
Expected: Both queries return 0 rows

- [ ] **Step 6: Verify model snapshot is synced**

Run:
```bash
dotnet ef migrations has-pending-model-changes \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor \
  --context IntegrationDbContext
```
Expected: "No changes have been made to the model since the last migration."

- [ ] **Step 7: Commit**

```bash
git add Application/Entegrasyon.DataAccess/Concrete/EntityFrameworkCore/Migrations/
git commit -m "fix(data): migrate leaf node violations — move attributes to children, deactivate parent syncs"
```

---

### Task 7: Run All Tests + Final Verification

**Files:** No changes — verification only

- [ ] **Step 1: Run unit tests**

Run: `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj`
Expected: All PASS

- [ ] **Step 2: Run integration tests**

Run: `dotnet test Test/Entegrasyon.IntegrationTest/Entegrasyon.IntegrationTest.csproj`
Expected: All PASS

- [ ] **Step 3: Build solution**

Run: `dotnet build Entegrasyon.sln`
Expected: Build succeeded, 0 errors

- [ ] **Step 4: Verify no pending model changes**

Run:
```bash
dotnet ef migrations has-pending-model-changes \
  -p Application/Entegrasyon.DataAccess \
  --startup-project Application/Entegrasyon.Blazor \
  --context IntegrationDbContext
```
Expected: "No changes have been made to the model since the last migration."
