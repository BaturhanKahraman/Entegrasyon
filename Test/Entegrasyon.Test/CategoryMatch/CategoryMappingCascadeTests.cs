using Entegrasyon.Business.Concrete;
using FluentAssertions;

namespace Entegrasyon.UnitTest.CategoryMatch;

/// <summary>
/// Unmap cascade'in çekirdek riskli mantığı: özellikler kategoriler arası PAYLAŞIMLI
/// (controlled-vocab dedup). Bir özellik eşleşmesi yalnızca BAŞKA hâlâ-eşli kategori
/// onu kullanmıyorsa silinmeli (orphan). Paylaşılan özellik korunmalı.
/// </summary>
public class CategoryMappingCascadeTests
{
    [Fact]
    public void OrphanedAttributeIds_ReturnsAttribute_WhenNoOtherCategoryUsesIt()
    {
        // Bu kategorinin özellikleri: 10, 11. Başka eşli kategori hiçbirini kullanmıyor.
        var orphans = CategoryMappingCascade.OrphanedAttributeIds(
            thisCategoryAttrIds: new[] { 10, 11 },
            stillUsedAttrIds: Array.Empty<int>());

        orphans.Should().BeEquivalentTo(new[] { 10, 11 });
    }

    [Fact]
    public void OrphanedAttributeIds_PreservesAttribute_WhenSharedWithStillMappedCategory()
    {
        // Özellik 10 başka hâlâ-eşli kategoride de kullanılıyor → korunmalı; 11 orphan.
        var orphans = CategoryMappingCascade.OrphanedAttributeIds(
            thisCategoryAttrIds: new[] { 10, 11 },
            stillUsedAttrIds: new[] { 10 });

        orphans.Should().BeEquivalentTo(new[] { 11 });
    }

    [Fact]
    public void OrphanedAttributeIds_Empty_WhenAllShared()
    {
        var orphans = CategoryMappingCascade.OrphanedAttributeIds(
            thisCategoryAttrIds: new[] { 10, 11 },
            stillUsedAttrIds: new[] { 10, 11, 12 });

        orphans.Should().BeEmpty();
    }

    [Fact]
    public void OrphanedAttributeIds_Deduplicates_Input()
    {
        var orphans = CategoryMappingCascade.OrphanedAttributeIds(
            thisCategoryAttrIds: new[] { 10, 10, 11 },
            stillUsedAttrIds: Array.Empty<int>());

        orphans.Should().BeEquivalentTo(new[] { 10, 11 });
    }
}
