using Entegrasyon.Entity.Dtos.Category;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Kategori detay sayfası (GET /categories/{id}) için satış/performans aggregate'i.
/// Hot-path: 3-tablo join (OrderItems → ProductVariants → MainProducts) + zaman penceresi
/// filtresi (Orders.OrderDate) + GroupBy. Index'ler DB Master tarafından garanti edilir.
/// </summary>
public interface ICategoryPerformanceManager
{
    /// <summary>
    /// Verilen kategoriye DOĞRUDAN bağlı ürünlerin (MainProduct.CategoryId == categoryId)
    /// son <paramref name="daysPast"/> günündeki satış performansını hesaplar (UTC).
    /// Alt kategori ağacı DAHİL DEĞİL (V1). Satışı yoksa tüm değerler 0, listeler boş döner.
    /// </summary>
    Task<CategoryPerformanceDto> GetCategoryPerformanceAsync(
        int categoryId,
        int daysPast = 30,
        CancellationToken ct = default);
}
