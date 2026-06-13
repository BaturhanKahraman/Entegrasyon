// File: Test/Entegrasyon.Test/MVC/CategoryAttributePanelVmTests.cs
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.Features.Categories.ViewModels;
using FluentAssertions;

namespace Entegrasyon.Test.MVC;

public class CategoryAttributePanelVmTests
{
    private static CategoryAttribute Attr(int id, string name, int usageCount) => new()
    {
        Id = id,
        CategoryAttributeKey = name,
        CategoryAttributeHumanized = name,
        Categories = Enumerable.Range(1, usageCount)
            .Select(i => new CategoryAttributeCategory { CategoryId = i, CategoryAttributeId = id })
            .ToList()
    };

    private static CategoryAttributeDto AttachedDto(int id) =>
        new(id, false, false, false, DateTime.UtcNow, $"key{id}", $"Attr {id}", []);

    [Fact]
    public void Addable_ExcludesAttachedAttributes()
    {
        var vm = new CategoryAttributePanelVm
        {
            CategoryId = 1,
            CategoryName = "Test",
            Pool = [Attr(1, "Beden", 0), Attr(2, "Renk", 0)],
            Attached = [AttachedDto(1)]
        };

        vm.Addable.Should().ContainSingle(a => a.Id == 2);
    }

    [Fact]
    public void Addable_OrdersByCategoryUsageCountDescending()
    {
        var vm = new CategoryAttributePanelVm
        {
            CategoryId = 1,
            CategoryName = "Test",
            Pool = [Attr(1, "Az Kullanılan", 1), Attr(2, "Çok Kullanılan", 5), Attr(3, "Hiç Kullanılmayan", 0)],
            Attached = []
        };

        vm.Addable.Select(a => a.Id).Should().Equal(2, 1, 3);
    }

    [Fact]
    public void Addable_TieOnUsage_OrdersByNameWithTurkishCulture()
    {
        var vm = new CategoryAttributePanelVm
        {
            CategoryId = 1,
            CategoryName = "Test",
            Pool = [Attr(1, "Renk", 2), Attr(2, "Beden", 2), Attr(3, "Çap", 2)],
            Attached = []
        };

        // Aynı kullanım sayısında TR alfabetik: Beden < Çap < Renk
        vm.Addable.Select(a => a.CategoryAttributeHumanized).Should().Equal("Beden", "Çap", "Renk");
    }
}
