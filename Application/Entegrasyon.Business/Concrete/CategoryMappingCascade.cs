namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Kategori ↔ pazaryeri eşlemesi kaldırıldığında (unmap) hangi özellik eşleşmelerinin
/// güvenle silinebileceğini hesaplar. Özellikler kategoriler arası PAYLAŞIMLI olduğundan
/// (controlled-vocab dedup), bir özellik eşleşmesi yalnızca BAŞKA hiçbir hâlâ-eşli kategori
/// onu kullanmıyorsa (orphan) silinebilir. Aksi halde paylaşan kategorinin eşlemesi bozulur.
/// </summary>
public static class CategoryMappingCascade
{
    /// <param name="thisCategoryAttrIds">Kaldırılan kategoriye bağlı özellik id'leri.</param>
    /// <param name="stillUsedAttrIds">Aynı pazaryerine hâlâ eşli diğer kategorilerin kullandığı özellik id'leri.</param>
    /// <returns>Yalnızca bu kategoride kalan, güvenle silinebilecek (orphan) özellik id'leri.</returns>
    public static List<int> OrphanedAttributeIds(IEnumerable<int> thisCategoryAttrIds, IEnumerable<int> stillUsedAttrIds)
        => thisCategoryAttrIds.Distinct().Except(stillUsedAttrIds).ToList();
}
