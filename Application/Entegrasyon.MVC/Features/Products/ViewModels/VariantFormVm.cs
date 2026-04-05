using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.MVC.Features.Products.ViewModels;

public record VariantFormVm(
    Guid ProductId,
    ProductVariantEditDetailDto? Variant,
    List<VariantImageGroup> OtherVariantImages
);

public record VariantImageGroup(Guid VariantId, string Barcode, List<VariantImageDto> Images);

public record VariantImageDto(int Id, string Src, bool IsMain);
