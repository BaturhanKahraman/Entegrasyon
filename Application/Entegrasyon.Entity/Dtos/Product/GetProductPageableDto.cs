namespace Entegrasyon.Entity.Dtos.Product;

public record GetProductPageableDto(string FullTextSearchKey,int PageIndex=0,int PageSize=50);