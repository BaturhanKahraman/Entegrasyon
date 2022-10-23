namespace Entegrasyon.Entity.Dtos.Product;

public record GetProductPageableDto(string? FullTextSearchKey,string? Barcode,int PageIndex=0,int PageSize=50);